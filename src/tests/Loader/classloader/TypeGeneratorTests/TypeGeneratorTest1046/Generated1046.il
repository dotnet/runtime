// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) .ver 4:0:0:0 }
.assembly extern TestFramework { .publickeytoken = ( B0 3F 5F 7F 11 D5 0A 3A )  }

//TYPES IN FORWARDER ASSEMBLIES:

//TEST ASSEMBLY:
.assembly extern xunit.core {}

.class public Generated1046.BaseClass0 
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void [mscorlib]System.Object::.ctor()
		ret
	}
}
.class public Generated1046.BaseClass1 
		extends Generated1046.BaseClass0
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void Generated1046.BaseClass0::.ctor()
		ret
	}
}
.class public Generated1046.G3_C1518`1<T0> 
		extends class Generated1046.G2_C536`2<!T0,class Generated1046.BaseClass1>
		implements class Generated1046.IBase2`2<!T0,class Generated1046.BaseClass1> 
{
	.method public hidebysig virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G3_C1518::Method7.16579<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase2<T0,class Generated1046.BaseClass1>.Method7'<M0>() cil managed noinlining { 
		.override method instance string class Generated1046.IBase2`2<!T0,class Generated1046.BaseClass1>::Method7<[1]>()
		ldstr "G3_C1518::Method7.MI.16580<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod4428() cil managed noinlining { 
		ldstr "G3_C1518::ClassMethod4428.16581()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C536<T0,class Generated1046.BaseClass1>.ClassMethod2437'() cil managed noinlining { 
		.override method instance string class Generated1046.G2_C536`2<!T0,class Generated1046.BaseClass1>::ClassMethod2437()
		ldstr "G3_C1518::ClassMethod2437.MI.16582()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C536<T0,class Generated1046.BaseClass1>.ClassMethod2438'<M0>() cil managed noinlining { 
		.override method instance string class Generated1046.G2_C536`2<!T0,class Generated1046.BaseClass1>::ClassMethod2438<[1]>()
		ldstr "G3_C1518::ClassMethod2438.MI.16583<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class Generated1046.G2_C536`2<!T0,class Generated1046.BaseClass1>::.ctor()
		ret
	}
}
.class public Generated1046.G2_C536`2<T0, T1> 
		extends class Generated1046.G1_C10`2<class Generated1046.BaseClass1,!T1>
		implements class Generated1046.IBase1`1<class Generated1046.BaseClass0>, class Generated1046.IBase2`2<!T0,class Generated1046.BaseClass0> 
{
	.method public hidebysig virtual instance string Method4() cil managed noinlining { 
		ldstr "G2_C536::Method4.9921()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class Generated1046.BaseClass0>.Method4'() cil managed noinlining { 
		.override method instance string class Generated1046.IBase1`1<class Generated1046.BaseClass0>::Method4()
		ldstr "G2_C536::Method4.MI.9922()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method5() cil managed noinlining { 
		ldstr "G2_C536::Method5.9923()"
		ret
	}
	.method public hidebysig virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G2_C536::Method6.9924<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G2_C536::Method7.9925<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod2437() cil managed noinlining { 
		ldstr "G2_C536::ClassMethod2437.9926()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod2438<M0>() cil managed noinlining { 
		ldstr "G2_C536::ClassMethod2438.9927<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C10<class Generated1046.BaseClass1,T1>.ClassMethod1341'() cil managed noinlining { 
		.override method instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,!T1>::ClassMethod1341()
		ldstr "G2_C536::ClassMethod1341.MI.9928()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C10<class Generated1046.BaseClass1,T1>.ClassMethod1342'<M0>() cil managed noinlining { 
		.override method instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,!T1>::ClassMethod1342<[1]>()
		ldstr "G2_C536::ClassMethod1342.MI.9929<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C10<class Generated1046.BaseClass1,T1>.ClassMethod1343'<M0>() cil managed noinlining { 
		.override method instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,!T1>::ClassMethod1343<[1]>()
		ldstr "G2_C536::ClassMethod1343.MI.9930<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class Generated1046.G1_C10`2<class Generated1046.BaseClass1,!T1>::.ctor()
		ret
	}
}
.class interface public abstract Generated1046.IBase2`2<+T0, -T1> 
{
	.method public hidebysig newslot abstract virtual instance string Method7<M0>() cil managed { }
}
.class public Generated1046.G1_C10`2<T0, T1> 
		implements class Generated1046.IBase2`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>, class Generated1046.IBase1`1<!T0> 
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
		.override method instance string class Generated1046.IBase1`1<!T0>::Method4()
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
		.override method instance string class Generated1046.IBase1`1<!T0>::Method6<[1]>()
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
.class interface public abstract Generated1046.IBase1`1<+T0> 
{
	.method public hidebysig newslot abstract virtual instance string Method4() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method5() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method6<M0>() cil managed { }
}
.class public auto ansi beforefieldinit Generated1046.Generated1046 {
	.method static void M.BaseClass0<(Generated1046.BaseClass0)W>(!!W inst, string exp) cil managed {
		.maxstack 5
		.locals init (string[] actualResults)
		ldc.i4.s    0
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.BaseClass0<(Generated1046.BaseClass0)W>(!!W inst, string exp)"
		ldc.i4.s 0
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.BaseClass1<(Generated1046.BaseClass1)W>(!!W inst, string exp) cil managed {
		.maxstack 5
		.locals init (string[] actualResults)
		ldc.i4.s    0
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.BaseClass1<(Generated1046.BaseClass1)W>(!!W inst, string exp)"
		ldc.i4.s 0
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1518.T<T0,(class Generated1046.G3_C1518`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1518.T<T0,(class Generated1046.G3_C1518`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G3_C1518`1<!!T0>::ClassMethod1340()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G3_C1518`1<!!T0>::ClassMethod1341()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G3_C1518`1<!!T0>::ClassMethod1342<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G3_C1518`1<!!T0>::ClassMethod1343<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G3_C1518`1<!!T0>::ClassMethod2437()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G3_C1518`1<!!T0>::ClassMethod2438<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G3_C1518`1<!!T0>::ClassMethod4428()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G3_C1518`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G3_C1518`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G3_C1518`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G3_C1518`1<!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1518.A<(class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1518.A<(class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>::ClassMethod1340()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>::ClassMethod1341()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>::ClassMethod1342<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>::ClassMethod1343<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>::ClassMethod2437()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>::ClassMethod2438<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>::ClassMethod4428()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1518.B<(class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1518.B<(class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>::ClassMethod1340()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>::ClassMethod1341()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>::ClassMethod1342<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>::ClassMethod1343<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>::ClassMethod2437()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>::ClassMethod2438<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>::ClassMethod4428()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C536.T.T<T0,T1,(class Generated1046.G2_C536`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 15
		.locals init (string[] actualResults)
		ldc.i4.s    10
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C536.T.T<T0,T1,(class Generated1046.G2_C536`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 10
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G2_C536`2<!!T0,!!T1>::ClassMethod1340()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G2_C536`2<!!T0,!!T1>::ClassMethod1341()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G2_C536`2<!!T0,!!T1>::ClassMethod1342<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G2_C536`2<!!T0,!!T1>::ClassMethod1343<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G2_C536`2<!!T0,!!T1>::ClassMethod2437()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G2_C536`2<!!T0,!!T1>::ClassMethod2438<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G2_C536`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G2_C536`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G2_C536`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G2_C536`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C536.A.T<T1,(class Generated1046.G2_C536`2<class Generated1046.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 15
		.locals init (string[] actualResults)
		ldc.i4.s    10
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C536.A.T<T1,(class Generated1046.G2_C536`2<class Generated1046.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 10
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass0,!!T1>::ClassMethod1340()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass0,!!T1>::ClassMethod1341()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass0,!!T1>::ClassMethod1342<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass0,!!T1>::ClassMethod1343<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass0,!!T1>::ClassMethod2437()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass0,!!T1>::ClassMethod2438<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C536.A.A<(class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 15
		.locals init (string[] actualResults)
		ldc.i4.s    10
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C536.A.A<(class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 10
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>::ClassMethod1340()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>::ClassMethod1341()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>::ClassMethod1342<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>::ClassMethod1343<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>::ClassMethod2437()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>::ClassMethod2438<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C536.A.B<(class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 15
		.locals init (string[] actualResults)
		ldc.i4.s    10
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C536.A.B<(class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 10
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::ClassMethod1340()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::ClassMethod1341()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::ClassMethod1342<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::ClassMethod1343<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::ClassMethod2437()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::ClassMethod2438<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C536.B.T<T1,(class Generated1046.G2_C536`2<class Generated1046.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 15
		.locals init (string[] actualResults)
		ldc.i4.s    10
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C536.B.T<T1,(class Generated1046.G2_C536`2<class Generated1046.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 10
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass1,!!T1>::ClassMethod1340()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass1,!!T1>::ClassMethod1341()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass1,!!T1>::ClassMethod1342<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass1,!!T1>::ClassMethod1343<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass1,!!T1>::ClassMethod2437()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass1,!!T1>::ClassMethod2438<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C536.B.A<(class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 15
		.locals init (string[] actualResults)
		ldc.i4.s    10
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C536.B.A<(class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 10
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::ClassMethod1340()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::ClassMethod1341()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::ClassMethod1342<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::ClassMethod1343<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::ClassMethod2437()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::ClassMethod2438<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C536.B.B<(class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 15
		.locals init (string[] actualResults)
		ldc.i4.s    10
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C536.B.B<(class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 10
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::ClassMethod1340()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::ClassMethod1341()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::ClassMethod1342<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::ClassMethod1343<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::ClassMethod2437()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::ClassMethod2438<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.T.T<T0,T1,(class Generated1046.IBase2`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.T.T<T0,T1,(class Generated1046.IBase2`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.IBase2`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.T<T1,(class Generated1046.IBase2`2<class Generated1046.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.T<T1,(class Generated1046.IBase2`2<class Generated1046.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.IBase2`2<class Generated1046.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.A<(class Generated1046.IBase2`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.A<(class Generated1046.IBase2`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.IBase2`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.B<(class Generated1046.IBase2`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.B<(class Generated1046.IBase2`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.IBase2`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.T<T1,(class Generated1046.IBase2`2<class Generated1046.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.T<T1,(class Generated1046.IBase2`2<class Generated1046.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.IBase2`2<class Generated1046.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.A<(class Generated1046.IBase2`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.A<(class Generated1046.IBase2`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.IBase2`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.B<(class Generated1046.IBase2`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.B<(class Generated1046.IBase2`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.IBase2`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C10.T.T<T0,T1,(class Generated1046.G1_C10`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C10.T.T<T0,T1,(class Generated1046.G1_C10`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G1_C10`2<!!T0,!!T1>::ClassMethod1340()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G1_C10`2<!!T0,!!T1>::ClassMethod1341()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G1_C10`2<!!T0,!!T1>::ClassMethod1342<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G1_C10`2<!!T0,!!T1>::ClassMethod1343<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G1_C10`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G1_C10`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G1_C10`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G1_C10`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C10.A.T<T1,(class Generated1046.G1_C10`2<class Generated1046.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C10.A.T<T1,(class Generated1046.G1_C10`2<class Generated1046.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass0,!!T1>::ClassMethod1340()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass0,!!T1>::ClassMethod1341()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass0,!!T1>::ClassMethod1342<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass0,!!T1>::ClassMethod1343<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C10.A.A<(class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C10.A.A<(class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>::ClassMethod1340()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>::ClassMethod1341()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>::ClassMethod1342<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>::ClassMethod1343<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C10.A.B<(class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C10.A.B<(class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::ClassMethod1340()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::ClassMethod1341()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::ClassMethod1342<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::ClassMethod1343<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C10.B.T<T1,(class Generated1046.G1_C10`2<class Generated1046.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C10.B.T<T1,(class Generated1046.G1_C10`2<class Generated1046.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,!!T1>::ClassMethod1340()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,!!T1>::ClassMethod1341()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,!!T1>::ClassMethod1342<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,!!T1>::ClassMethod1343<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C10.B.A<(class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C10.B.A<(class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::ClassMethod1340()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::ClassMethod1341()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::ClassMethod1342<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::ClassMethod1343<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C10.B.B<(class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C10.B.B<(class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::ClassMethod1340()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::ClassMethod1341()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::ClassMethod1342<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::ClassMethod1343<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.T<T0,(class Generated1046.IBase1`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.T<T0,(class Generated1046.IBase1`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.IBase1`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.IBase1`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.IBase1`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.A<(class Generated1046.IBase1`1<class Generated1046.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.A<(class Generated1046.IBase1`1<class Generated1046.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.IBase1`1<class Generated1046.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.IBase1`1<class Generated1046.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.IBase1`1<class Generated1046.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.B<(class Generated1046.IBase1`1<class Generated1046.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.B<(class Generated1046.IBase1`1<class Generated1046.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.IBase1`1<class Generated1046.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.IBase1`1<class Generated1046.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1046.IBase1`1<class Generated1046.BaseClass1>::Method6<object>()
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
		newobj instance void class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::ClassMethod1343<object>()
		ldstr "G2_C536::ClassMethod1343.MI.9930<System.Object>()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::ClassMethod1342<object>()
		ldstr "G2_C536::ClassMethod1342.MI.9929<System.Object>()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::ClassMethod1341()
		ldstr "G2_C536::ClassMethod1341.MI.9928()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::ClassMethod1340()
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::Method6<object>()
		ldstr "G2_C536::Method6.9924<System.Object>()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::Method5()
		ldstr "G1_C10::Method5.4847()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::Method4()
		ldstr "G2_C536::Method4.9921()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::Method7<object>()
		ldstr "G3_C1518::Method7.16579<System.Object>()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1046.IBase2`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::Method7<object>()
		ldstr "G3_C1518::Method7.16579<System.Object>()"
		ldstr "class IBase2`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1046.IBase1`1<class Generated1046.BaseClass1>::Method4()
		ldstr "G1_C10::Method4.MI.4846()"
		ldstr "class IBase1`1<class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1046.IBase1`1<class Generated1046.BaseClass1>::Method5()
		ldstr "G1_C10::Method5.4847()"
		ldstr "class IBase1`1<class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1046.IBase1`1<class Generated1046.BaseClass1>::Method6<object>()
		ldstr "G1_C10::Method6.MI.4849<System.Object>()"
		ldstr "class IBase1`1<class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1046.IBase2`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::Method7<object>()
		ldstr "G3_C1518::Method7.MI.16580<System.Object>()"
		ldstr "class IBase2`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1046.IBase1`1<class Generated1046.BaseClass0>::Method4()
		ldstr "G2_C536::Method4.MI.9922()"
		ldstr "class IBase1`1<class Generated1046.BaseClass0> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1046.IBase1`1<class Generated1046.BaseClass0>::Method5()
		ldstr "G2_C536::Method5.9923()"
		ldstr "class IBase1`1<class Generated1046.BaseClass0> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1046.IBase1`1<class Generated1046.BaseClass0>::Method6<object>()
		ldstr "G2_C536::Method6.9924<System.Object>()"
		ldstr "class IBase1`1<class Generated1046.BaseClass0> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::ClassMethod2438<object>()
		ldstr "G3_C1518::ClassMethod2438.MI.16583<System.Object>()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::ClassMethod2437()
		ldstr "G3_C1518::ClassMethod2437.MI.16582()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::Method7<object>()
		ldstr "G3_C1518::Method7.16579<System.Object>()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::Method6<object>()
		ldstr "G2_C536::Method6.9924<System.Object>()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::Method5()
		ldstr "G2_C536::Method5.9923()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::Method4()
		ldstr "G2_C536::Method4.9921()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::ClassMethod1343<object>()
		ldstr "G2_C536::ClassMethod1343.MI.9930<System.Object>()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::ClassMethod1342<object>()
		ldstr "G2_C536::ClassMethod1342.MI.9929<System.Object>()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::ClassMethod1341()
		ldstr "G2_C536::ClassMethod1341.MI.9928()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::ClassMethod1340()
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1046.IBase2`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>::Method7<object>()
		ldstr "G3_C1518::Method7.16579<System.Object>()"
		ldstr "class IBase2`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>
		callvirt instance string class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>::ClassMethod4428()
		ldstr "G3_C1518::ClassMethod4428.16581()"
		ldstr "class G3_C1518`1<class Generated1046.BaseClass0> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>
		callvirt instance string class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>::Method7<object>()
		ldstr "G3_C1518::Method7.16579<System.Object>()"
		ldstr "class G3_C1518`1<class Generated1046.BaseClass0> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>
		callvirt instance string class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>::ClassMethod2438<object>()
		ldstr "G3_C1518::ClassMethod2438.MI.16583<System.Object>()"
		ldstr "class G3_C1518`1<class Generated1046.BaseClass0> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>
		callvirt instance string class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>::ClassMethod2437()
		ldstr "G3_C1518::ClassMethod2437.MI.16582()"
		ldstr "class G3_C1518`1<class Generated1046.BaseClass0> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>
		callvirt instance string class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>::Method6<object>()
		ldstr "G2_C536::Method6.9924<System.Object>()"
		ldstr "class G3_C1518`1<class Generated1046.BaseClass0> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>
		callvirt instance string class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>::Method5()
		ldstr "G2_C536::Method5.9923()"
		ldstr "class G3_C1518`1<class Generated1046.BaseClass0> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>
		callvirt instance string class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>::Method4()
		ldstr "G2_C536::Method4.9921()"
		ldstr "class G3_C1518`1<class Generated1046.BaseClass0> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>
		callvirt instance string class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>::ClassMethod1343<object>()
		ldstr "G2_C536::ClassMethod1343.MI.9930<System.Object>()"
		ldstr "class G3_C1518`1<class Generated1046.BaseClass0> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>
		callvirt instance string class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>::ClassMethod1342<object>()
		ldstr "G2_C536::ClassMethod1342.MI.9929<System.Object>()"
		ldstr "class G3_C1518`1<class Generated1046.BaseClass0> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>
		callvirt instance string class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>::ClassMethod1341()
		ldstr "G2_C536::ClassMethod1341.MI.9928()"
		ldstr "class G3_C1518`1<class Generated1046.BaseClass0> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>
		callvirt instance string class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>::ClassMethod1340()
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G3_C1518`1<class Generated1046.BaseClass0> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::ClassMethod1343<object>()
		ldstr "G2_C536::ClassMethod1343.MI.9930<System.Object>()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::ClassMethod1342<object>()
		ldstr "G2_C536::ClassMethod1342.MI.9929<System.Object>()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::ClassMethod1341()
		ldstr "G2_C536::ClassMethod1341.MI.9928()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::ClassMethod1340()
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::Method6<object>()
		ldstr "G2_C536::Method6.9924<System.Object>()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::Method5()
		ldstr "G1_C10::Method5.4847()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::Method4()
		ldstr "G2_C536::Method4.9921()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::Method7<object>()
		ldstr "G3_C1518::Method7.16579<System.Object>()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1046.IBase2`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::Method7<object>()
		ldstr "G3_C1518::Method7.MI.16580<System.Object>()"
		ldstr "class IBase2`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1046.IBase1`1<class Generated1046.BaseClass1>::Method4()
		ldstr "G1_C10::Method4.MI.4846()"
		ldstr "class IBase1`1<class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1046.IBase1`1<class Generated1046.BaseClass1>::Method5()
		ldstr "G1_C10::Method5.4847()"
		ldstr "class IBase1`1<class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1046.IBase1`1<class Generated1046.BaseClass1>::Method6<object>()
		ldstr "G1_C10::Method6.MI.4849<System.Object>()"
		ldstr "class IBase1`1<class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1046.IBase2`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::Method7<object>()
		ldstr "G3_C1518::Method7.MI.16580<System.Object>()"
		ldstr "class IBase2`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1046.IBase1`1<class Generated1046.BaseClass0>::Method4()
		ldstr "G2_C536::Method4.MI.9922()"
		ldstr "class IBase1`1<class Generated1046.BaseClass0> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1046.IBase1`1<class Generated1046.BaseClass0>::Method5()
		ldstr "G2_C536::Method5.9923()"
		ldstr "class IBase1`1<class Generated1046.BaseClass0> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1046.IBase1`1<class Generated1046.BaseClass0>::Method6<object>()
		ldstr "G2_C536::Method6.9924<System.Object>()"
		ldstr "class IBase1`1<class Generated1046.BaseClass0> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::ClassMethod2438<object>()
		ldstr "G3_C1518::ClassMethod2438.MI.16583<System.Object>()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::ClassMethod2437()
		ldstr "G3_C1518::ClassMethod2437.MI.16582()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::Method7<object>()
		ldstr "G3_C1518::Method7.16579<System.Object>()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::Method6<object>()
		ldstr "G2_C536::Method6.9924<System.Object>()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::Method5()
		ldstr "G2_C536::Method5.9923()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::Method4()
		ldstr "G2_C536::Method4.9921()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::ClassMethod1343<object>()
		ldstr "G2_C536::ClassMethod1343.MI.9930<System.Object>()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::ClassMethod1342<object>()
		ldstr "G2_C536::ClassMethod1342.MI.9929<System.Object>()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::ClassMethod1341()
		ldstr "G2_C536::ClassMethod1341.MI.9928()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::ClassMethod1340()
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1046.IBase2`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::Method7<object>()
		ldstr "G3_C1518::Method7.16579<System.Object>()"
		ldstr "class IBase2`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1046.IBase2`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>::Method7<object>()
		ldstr "G3_C1518::Method7.16579<System.Object>()"
		ldstr "class IBase2`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>::ClassMethod4428()
		ldstr "G3_C1518::ClassMethod4428.16581()"
		ldstr "class G3_C1518`1<class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>::Method7<object>()
		ldstr "G3_C1518::Method7.16579<System.Object>()"
		ldstr "class G3_C1518`1<class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>::ClassMethod2438<object>()
		ldstr "G3_C1518::ClassMethod2438.MI.16583<System.Object>()"
		ldstr "class G3_C1518`1<class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>::ClassMethod2437()
		ldstr "G3_C1518::ClassMethod2437.MI.16582()"
		ldstr "class G3_C1518`1<class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>::Method6<object>()
		ldstr "G2_C536::Method6.9924<System.Object>()"
		ldstr "class G3_C1518`1<class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>::Method5()
		ldstr "G2_C536::Method5.9923()"
		ldstr "class G3_C1518`1<class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>::Method4()
		ldstr "G2_C536::Method4.9921()"
		ldstr "class G3_C1518`1<class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>::ClassMethod1343<object>()
		ldstr "G2_C536::ClassMethod1343.MI.9930<System.Object>()"
		ldstr "class G3_C1518`1<class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>::ClassMethod1342<object>()
		ldstr "G2_C536::ClassMethod1342.MI.9929<System.Object>()"
		ldstr "class G3_C1518`1<class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>::ClassMethod1341()
		ldstr "G2_C536::ClassMethod1341.MI.9928()"
		ldstr "class G3_C1518`1<class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>::ClassMethod1340()
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G3_C1518`1<class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>
		callvirt instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::ClassMethod1343<object>()
		ldstr "G2_C536::ClassMethod1343.MI.9930<System.Object>()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>
		callvirt instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::ClassMethod1342<object>()
		ldstr "G2_C536::ClassMethod1342.MI.9929<System.Object>()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>
		callvirt instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::ClassMethod1341()
		ldstr "G2_C536::ClassMethod1341.MI.9928()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>
		callvirt instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::ClassMethod1340()
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>
		callvirt instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::Method6<object>()
		ldstr "G2_C536::Method6.9924<System.Object>()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>
		callvirt instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::Method5()
		ldstr "G1_C10::Method5.4847()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>
		callvirt instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::Method4()
		ldstr "G2_C536::Method4.9921()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>
		callvirt instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::Method7<object>()
		ldstr "G2_C536::Method7.9925<System.Object>()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1046.IBase2`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::Method7<object>()
		ldstr "G2_C536::Method7.9925<System.Object>()"
		ldstr "class IBase2`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1046.IBase1`1<class Generated1046.BaseClass1>::Method4()
		ldstr "G1_C10::Method4.MI.4846()"
		ldstr "class IBase1`1<class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1046.IBase1`1<class Generated1046.BaseClass1>::Method5()
		ldstr "G1_C10::Method5.4847()"
		ldstr "class IBase1`1<class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1046.IBase1`1<class Generated1046.BaseClass1>::Method6<object>()
		ldstr "G1_C10::Method6.MI.4849<System.Object>()"
		ldstr "class IBase1`1<class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1046.IBase2`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::Method7<object>()
		ldstr "G2_C536::Method7.9925<System.Object>()"
		ldstr "class IBase2`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1046.IBase1`1<class Generated1046.BaseClass0>::Method4()
		ldstr "G2_C536::Method4.MI.9922()"
		ldstr "class IBase1`1<class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1046.IBase1`1<class Generated1046.BaseClass0>::Method5()
		ldstr "G2_C536::Method5.9923()"
		ldstr "class IBase1`1<class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1046.IBase1`1<class Generated1046.BaseClass0>::Method6<object>()
		ldstr "G2_C536::Method6.9924<System.Object>()"
		ldstr "class IBase1`1<class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>
		callvirt instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>::ClassMethod2438<object>()
		ldstr "G2_C536::ClassMethod2438.9927<System.Object>()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>
		callvirt instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>::ClassMethod2437()
		ldstr "G2_C536::ClassMethod2437.9926()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>
		callvirt instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>::Method7<object>()
		ldstr "G2_C536::Method7.9925<System.Object>()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>
		callvirt instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>::Method6<object>()
		ldstr "G2_C536::Method6.9924<System.Object>()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>
		callvirt instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>::Method5()
		ldstr "G2_C536::Method5.9923()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>
		callvirt instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>::Method4()
		ldstr "G2_C536::Method4.9921()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>
		callvirt instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>::ClassMethod1343<object>()
		ldstr "G2_C536::ClassMethod1343.MI.9930<System.Object>()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>
		callvirt instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>::ClassMethod1342<object>()
		ldstr "G2_C536::ClassMethod1342.MI.9929<System.Object>()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>
		callvirt instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>::ClassMethod1341()
		ldstr "G2_C536::ClassMethod1341.MI.9928()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>
		callvirt instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>::ClassMethod1340()
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1046.IBase2`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>::Method7<object>()
		ldstr "G2_C536::Method7.9925<System.Object>()"
		ldstr "class IBase2`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::ClassMethod1343<object>()
		ldstr "G2_C536::ClassMethod1343.MI.9930<System.Object>()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::ClassMethod1342<object>()
		ldstr "G2_C536::ClassMethod1342.MI.9929<System.Object>()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::ClassMethod1341()
		ldstr "G2_C536::ClassMethod1341.MI.9928()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::ClassMethod1340()
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::Method6<object>()
		ldstr "G2_C536::Method6.9924<System.Object>()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::Method5()
		ldstr "G1_C10::Method5.4847()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::Method4()
		ldstr "G2_C536::Method4.9921()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::Method7<object>()
		ldstr "G2_C536::Method7.9925<System.Object>()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1046.IBase2`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::Method7<object>()
		ldstr "G2_C536::Method7.9925<System.Object>()"
		ldstr "class IBase2`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1046.IBase1`1<class Generated1046.BaseClass1>::Method4()
		ldstr "G1_C10::Method4.MI.4846()"
		ldstr "class IBase1`1<class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1046.IBase1`1<class Generated1046.BaseClass1>::Method5()
		ldstr "G1_C10::Method5.4847()"
		ldstr "class IBase1`1<class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1046.IBase1`1<class Generated1046.BaseClass1>::Method6<object>()
		ldstr "G1_C10::Method6.MI.4849<System.Object>()"
		ldstr "class IBase1`1<class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1046.IBase2`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::Method7<object>()
		ldstr "G2_C536::Method7.9925<System.Object>()"
		ldstr "class IBase2`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1046.IBase1`1<class Generated1046.BaseClass0>::Method4()
		ldstr "G2_C536::Method4.MI.9922()"
		ldstr "class IBase1`1<class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1046.IBase1`1<class Generated1046.BaseClass0>::Method5()
		ldstr "G2_C536::Method5.9923()"
		ldstr "class IBase1`1<class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1046.IBase1`1<class Generated1046.BaseClass0>::Method6<object>()
		ldstr "G2_C536::Method6.9924<System.Object>()"
		ldstr "class IBase1`1<class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::ClassMethod2438<object>()
		ldstr "G2_C536::ClassMethod2438.9927<System.Object>()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::ClassMethod2437()
		ldstr "G2_C536::ClassMethod2437.9926()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::Method7<object>()
		ldstr "G2_C536::Method7.9925<System.Object>()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::Method6<object>()
		ldstr "G2_C536::Method6.9924<System.Object>()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::Method5()
		ldstr "G2_C536::Method5.9923()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::Method4()
		ldstr "G2_C536::Method4.9921()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::ClassMethod1343<object>()
		ldstr "G2_C536::ClassMethod1343.MI.9930<System.Object>()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::ClassMethod1342<object>()
		ldstr "G2_C536::ClassMethod1342.MI.9929<System.Object>()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::ClassMethod1341()
		ldstr "G2_C536::ClassMethod1341.MI.9928()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::ClassMethod1340()
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1046.IBase2`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>::Method7<object>()
		ldstr "G2_C536::Method7.9925<System.Object>()"
		ldstr "class IBase2`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>
		callvirt instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::ClassMethod1343<object>()
		ldstr "G2_C536::ClassMethod1343.MI.9930<System.Object>()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>
		callvirt instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::ClassMethod1342<object>()
		ldstr "G2_C536::ClassMethod1342.MI.9929<System.Object>()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>
		callvirt instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::ClassMethod1341()
		ldstr "G2_C536::ClassMethod1341.MI.9928()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>
		callvirt instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::ClassMethod1340()
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>
		callvirt instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::Method6<object>()
		ldstr "G2_C536::Method6.9924<System.Object>()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>
		callvirt instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::Method5()
		ldstr "G1_C10::Method5.4847()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>
		callvirt instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::Method4()
		ldstr "G2_C536::Method4.9921()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>
		callvirt instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::Method7<object>()
		ldstr "G2_C536::Method7.9925<System.Object>()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1046.IBase2`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::Method7<object>()
		ldstr "G2_C536::Method7.9925<System.Object>()"
		ldstr "class IBase2`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1046.IBase1`1<class Generated1046.BaseClass1>::Method4()
		ldstr "G1_C10::Method4.MI.4846()"
		ldstr "class IBase1`1<class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1046.IBase1`1<class Generated1046.BaseClass1>::Method5()
		ldstr "G1_C10::Method5.4847()"
		ldstr "class IBase1`1<class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1046.IBase1`1<class Generated1046.BaseClass1>::Method6<object>()
		ldstr "G1_C10::Method6.MI.4849<System.Object>()"
		ldstr "class IBase1`1<class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1046.IBase2`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::Method7<object>()
		ldstr "G2_C536::Method7.9925<System.Object>()"
		ldstr "class IBase2`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1046.IBase1`1<class Generated1046.BaseClass0>::Method4()
		ldstr "G2_C536::Method4.MI.9922()"
		ldstr "class IBase1`1<class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1046.IBase1`1<class Generated1046.BaseClass0>::Method5()
		ldstr "G2_C536::Method5.9923()"
		ldstr "class IBase1`1<class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1046.IBase1`1<class Generated1046.BaseClass0>::Method6<object>()
		ldstr "G2_C536::Method6.9924<System.Object>()"
		ldstr "class IBase1`1<class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>
		callvirt instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::ClassMethod2438<object>()
		ldstr "G2_C536::ClassMethod2438.9927<System.Object>()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>
		callvirt instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::ClassMethod2437()
		ldstr "G2_C536::ClassMethod2437.9926()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>
		callvirt instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::Method7<object>()
		ldstr "G2_C536::Method7.9925<System.Object>()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>
		callvirt instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::Method6<object>()
		ldstr "G2_C536::Method6.9924<System.Object>()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>
		callvirt instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::Method5()
		ldstr "G2_C536::Method5.9923()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>
		callvirt instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::Method4()
		ldstr "G2_C536::Method4.9921()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>
		callvirt instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::ClassMethod1343<object>()
		ldstr "G2_C536::ClassMethod1343.MI.9930<System.Object>()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>
		callvirt instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::ClassMethod1342<object>()
		ldstr "G2_C536::ClassMethod1342.MI.9929<System.Object>()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>
		callvirt instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::ClassMethod1341()
		ldstr "G2_C536::ClassMethod1341.MI.9928()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>
		callvirt instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::ClassMethod1340()
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1046.IBase2`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::Method7<object>()
		ldstr "G2_C536::Method7.9925<System.Object>()"
		ldstr "class IBase2`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1046.IBase2`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>::Method7<object>()
		ldstr "G2_C536::Method7.9925<System.Object>()"
		ldstr "class IBase2`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::ClassMethod1343<object>()
		ldstr "G2_C536::ClassMethod1343.MI.9930<System.Object>()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::ClassMethod1342<object>()
		ldstr "G2_C536::ClassMethod1342.MI.9929<System.Object>()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::ClassMethod1341()
		ldstr "G2_C536::ClassMethod1341.MI.9928()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::ClassMethod1340()
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::Method6<object>()
		ldstr "G2_C536::Method6.9924<System.Object>()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::Method5()
		ldstr "G1_C10::Method5.4847()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::Method4()
		ldstr "G2_C536::Method4.9921()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::Method7<object>()
		ldstr "G2_C536::Method7.9925<System.Object>()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1046.IBase2`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::Method7<object>()
		ldstr "G2_C536::Method7.9925<System.Object>()"
		ldstr "class IBase2`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1046.IBase1`1<class Generated1046.BaseClass1>::Method4()
		ldstr "G1_C10::Method4.MI.4846()"
		ldstr "class IBase1`1<class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1046.IBase1`1<class Generated1046.BaseClass1>::Method5()
		ldstr "G1_C10::Method5.4847()"
		ldstr "class IBase1`1<class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1046.IBase1`1<class Generated1046.BaseClass1>::Method6<object>()
		ldstr "G1_C10::Method6.MI.4849<System.Object>()"
		ldstr "class IBase1`1<class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1046.IBase2`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::Method7<object>()
		ldstr "G2_C536::Method7.9925<System.Object>()"
		ldstr "class IBase2`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1046.IBase1`1<class Generated1046.BaseClass0>::Method4()
		ldstr "G2_C536::Method4.MI.9922()"
		ldstr "class IBase1`1<class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1046.IBase1`1<class Generated1046.BaseClass0>::Method5()
		ldstr "G2_C536::Method5.9923()"
		ldstr "class IBase1`1<class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1046.IBase1`1<class Generated1046.BaseClass0>::Method6<object>()
		ldstr "G2_C536::Method6.9924<System.Object>()"
		ldstr "class IBase1`1<class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::ClassMethod2438<object>()
		ldstr "G2_C536::ClassMethod2438.9927<System.Object>()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::ClassMethod2437()
		ldstr "G2_C536::ClassMethod2437.9926()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::Method7<object>()
		ldstr "G2_C536::Method7.9925<System.Object>()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::Method6<object>()
		ldstr "G2_C536::Method6.9924<System.Object>()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::Method5()
		ldstr "G2_C536::Method5.9923()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::Method4()
		ldstr "G2_C536::Method4.9921()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::ClassMethod1343<object>()
		ldstr "G2_C536::ClassMethod1343.MI.9930<System.Object>()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::ClassMethod1342<object>()
		ldstr "G2_C536::ClassMethod1342.MI.9929<System.Object>()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::ClassMethod1341()
		ldstr "G2_C536::ClassMethod1341.MI.9928()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::ClassMethod1340()
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1046.IBase2`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::Method7<object>()
		ldstr "G2_C536::Method7.9925<System.Object>()"
		ldstr "class IBase2`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1046.IBase2`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>::Method7<object>()
		ldstr "G2_C536::Method7.9925<System.Object>()"
		ldstr "class IBase2`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>
		callvirt instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>::ClassMethod1343<object>()
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>
		callvirt instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>::ClassMethod1342<object>()
		ldstr "G1_C10::ClassMethod1342.4852<System.Object>()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>
		callvirt instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>::ClassMethod1341()
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>
		callvirt instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>::ClassMethod1340()
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>
		callvirt instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>::Method6<object>()
		ldstr "G1_C10::Method6.4848<System.Object>()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>
		callvirt instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>::Method5()
		ldstr "G1_C10::Method5.4847()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>
		callvirt instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>::Method4()
		ldstr "G1_C10::Method4.4845()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>
		callvirt instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>::Method7<object>()
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1046.IBase2`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::Method7<object>()
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1046.IBase1`1<class Generated1046.BaseClass0>::Method4()
		ldstr "G1_C10::Method4.MI.4846()"
		ldstr "class IBase1`1<class Generated1046.BaseClass0> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1046.IBase1`1<class Generated1046.BaseClass0>::Method5()
		ldstr "G1_C10::Method5.4847()"
		ldstr "class IBase1`1<class Generated1046.BaseClass0> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1046.IBase1`1<class Generated1046.BaseClass0>::Method6<object>()
		ldstr "G1_C10::Method6.MI.4849<System.Object>()"
		ldstr "class IBase1`1<class Generated1046.BaseClass0> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1046.IBase2`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::Method7<object>()
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::ClassMethod1343<object>()
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::ClassMethod1342<object>()
		ldstr "G1_C10::ClassMethod1342.4852<System.Object>()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::ClassMethod1341()
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::ClassMethod1340()
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::Method6<object>()
		ldstr "G1_C10::Method6.4848<System.Object>()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::Method5()
		ldstr "G1_C10::Method5.4847()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::Method4()
		ldstr "G1_C10::Method4.4845()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::Method7<object>()
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1046.IBase2`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::Method7<object>()
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1046.IBase1`1<class Generated1046.BaseClass0>::Method4()
		ldstr "G1_C10::Method4.MI.4846()"
		ldstr "class IBase1`1<class Generated1046.BaseClass0> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1046.IBase1`1<class Generated1046.BaseClass0>::Method5()
		ldstr "G1_C10::Method5.4847()"
		ldstr "class IBase1`1<class Generated1046.BaseClass0> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1046.IBase1`1<class Generated1046.BaseClass0>::Method6<object>()
		ldstr "G1_C10::Method6.MI.4849<System.Object>()"
		ldstr "class IBase1`1<class Generated1046.BaseClass0> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1046.IBase2`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::Method7<object>()
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>
		callvirt instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::ClassMethod1343<object>()
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>
		callvirt instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::ClassMethod1342<object>()
		ldstr "G1_C10::ClassMethod1342.4852<System.Object>()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>
		callvirt instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::ClassMethod1341()
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>
		callvirt instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::ClassMethod1340()
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>
		callvirt instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::Method6<object>()
		ldstr "G1_C10::Method6.4848<System.Object>()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>
		callvirt instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::Method5()
		ldstr "G1_C10::Method5.4847()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>
		callvirt instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::Method4()
		ldstr "G1_C10::Method4.4845()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>
		callvirt instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::Method7<object>()
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1046.IBase2`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::Method7<object>()
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1046.IBase1`1<class Generated1046.BaseClass1>::Method4()
		ldstr "G1_C10::Method4.MI.4846()"
		ldstr "class IBase1`1<class Generated1046.BaseClass1> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1046.IBase1`1<class Generated1046.BaseClass1>::Method5()
		ldstr "G1_C10::Method5.4847()"
		ldstr "class IBase1`1<class Generated1046.BaseClass1> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1046.IBase1`1<class Generated1046.BaseClass1>::Method6<object>()
		ldstr "G1_C10::Method6.MI.4849<System.Object>()"
		ldstr "class IBase1`1<class Generated1046.BaseClass1> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1046.IBase2`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::Method7<object>()
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1046.IBase1`1<class Generated1046.BaseClass0>::Method4()
		ldstr "G1_C10::Method4.MI.4846()"
		ldstr "class IBase1`1<class Generated1046.BaseClass0> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1046.IBase1`1<class Generated1046.BaseClass0>::Method5()
		ldstr "G1_C10::Method5.4847()"
		ldstr "class IBase1`1<class Generated1046.BaseClass0> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1046.IBase1`1<class Generated1046.BaseClass0>::Method6<object>()
		ldstr "G1_C10::Method6.MI.4849<System.Object>()"
		ldstr "class IBase1`1<class Generated1046.BaseClass0> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::ClassMethod1343<object>()
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::ClassMethod1342<object>()
		ldstr "G1_C10::ClassMethod1342.4852<System.Object>()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::ClassMethod1341()
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::ClassMethod1340()
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::Method6<object>()
		ldstr "G1_C10::Method6.4848<System.Object>()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::Method5()
		ldstr "G1_C10::Method5.4847()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::Method4()
		ldstr "G1_C10::Method4.4845()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		callvirt instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::Method7<object>()
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1046.IBase2`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::Method7<object>()
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1046.IBase1`1<class Generated1046.BaseClass1>::Method4()
		ldstr "G1_C10::Method4.MI.4846()"
		ldstr "class IBase1`1<class Generated1046.BaseClass1> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1046.IBase1`1<class Generated1046.BaseClass1>::Method5()
		ldstr "G1_C10::Method5.4847()"
		ldstr "class IBase1`1<class Generated1046.BaseClass1> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1046.IBase1`1<class Generated1046.BaseClass1>::Method6<object>()
		ldstr "G1_C10::Method6.MI.4849<System.Object>()"
		ldstr "class IBase1`1<class Generated1046.BaseClass1> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1046.IBase2`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::Method7<object>()
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1046.IBase1`1<class Generated1046.BaseClass0>::Method4()
		ldstr "G1_C10::Method4.MI.4846()"
		ldstr "class IBase1`1<class Generated1046.BaseClass0> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1046.IBase1`1<class Generated1046.BaseClass0>::Method5()
		ldstr "G1_C10::Method5.4847()"
		ldstr "class IBase1`1<class Generated1046.BaseClass0> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1046.IBase1`1<class Generated1046.BaseClass0>::Method6<object>()
		ldstr "G1_C10::Method6.MI.4849<System.Object>()"
		ldstr "class IBase1`1<class Generated1046.BaseClass0> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>"
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
		newobj instance void class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G2_C536::ClassMethod1341.MI.9928()#G2_C536::ClassMethod1342.MI.9929<System.Object>()#G2_C536::ClassMethod1343.MI.9930<System.Object>()#G2_C536::Method4.9921()#G1_C10::Method5.4847()#G2_C536::Method6.9924<System.Object>()#G3_C1518::Method7.16579<System.Object>()#"
		call void Generated1046.Generated1046::M.G1_C10.T.T<class Generated1046.BaseClass1,class Generated1046.BaseClass1,class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G2_C536::ClassMethod1341.MI.9928()#G2_C536::ClassMethod1342.MI.9929<System.Object>()#G2_C536::ClassMethod1343.MI.9930<System.Object>()#G2_C536::Method4.9921()#G1_C10::Method5.4847()#G2_C536::Method6.9924<System.Object>()#G3_C1518::Method7.16579<System.Object>()#"
		call void Generated1046.Generated1046::M.G1_C10.B.T<class Generated1046.BaseClass1,class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G2_C536::ClassMethod1341.MI.9928()#G2_C536::ClassMethod1342.MI.9929<System.Object>()#G2_C536::ClassMethod1343.MI.9930<System.Object>()#G2_C536::Method4.9921()#G1_C10::Method5.4847()#G2_C536::Method6.9924<System.Object>()#G3_C1518::Method7.16579<System.Object>()#"
		call void Generated1046.Generated1046::M.G1_C10.B.B<class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1518::Method7.16579<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase2.T.T<class Generated1046.BaseClass1,class Generated1046.BaseClass1,class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1518::Method7.16579<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase2.B.T<class Generated1046.BaseClass1,class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1518::Method7.16579<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase2.B.B<class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method4.MI.4846()#G1_C10::Method5.4847()#G1_C10::Method6.MI.4849<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase1.T<class Generated1046.BaseClass1,class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method4.MI.4846()#G1_C10::Method5.4847()#G1_C10::Method6.MI.4849<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase1.B<class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1518::Method7.MI.16580<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase2.T.T<class Generated1046.BaseClass0,class Generated1046.BaseClass1,class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1518::Method7.MI.16580<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase2.A.T<class Generated1046.BaseClass1,class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1518::Method7.MI.16580<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase2.A.B<class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C536::Method4.MI.9922()#G2_C536::Method5.9923()#G2_C536::Method6.9924<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase1.T<class Generated1046.BaseClass0,class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C536::Method4.MI.9922()#G2_C536::Method5.9923()#G2_C536::Method6.9924<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase1.A<class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G2_C536::ClassMethod1341.MI.9928()#G2_C536::ClassMethod1342.MI.9929<System.Object>()#G2_C536::ClassMethod1343.MI.9930<System.Object>()#G3_C1518::ClassMethod2437.MI.16582()#G3_C1518::ClassMethod2438.MI.16583<System.Object>()#G2_C536::Method4.9921()#G2_C536::Method5.9923()#G2_C536::Method6.9924<System.Object>()#G3_C1518::Method7.16579<System.Object>()#"
		call void Generated1046.Generated1046::M.G2_C536.T.T<class Generated1046.BaseClass0,class Generated1046.BaseClass1,class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G2_C536::ClassMethod1341.MI.9928()#G2_C536::ClassMethod1342.MI.9929<System.Object>()#G2_C536::ClassMethod1343.MI.9930<System.Object>()#G3_C1518::ClassMethod2437.MI.16582()#G3_C1518::ClassMethod2438.MI.16583<System.Object>()#G2_C536::Method4.9921()#G2_C536::Method5.9923()#G2_C536::Method6.9924<System.Object>()#G3_C1518::Method7.16579<System.Object>()#"
		call void Generated1046.Generated1046::M.G2_C536.A.T<class Generated1046.BaseClass1,class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G2_C536::ClassMethod1341.MI.9928()#G2_C536::ClassMethod1342.MI.9929<System.Object>()#G2_C536::ClassMethod1343.MI.9930<System.Object>()#G3_C1518::ClassMethod2437.MI.16582()#G3_C1518::ClassMethod2438.MI.16583<System.Object>()#G2_C536::Method4.9921()#G2_C536::Method5.9923()#G2_C536::Method6.9924<System.Object>()#G3_C1518::Method7.16579<System.Object>()#"
		call void Generated1046.Generated1046::M.G2_C536.A.B<class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1518::Method7.16579<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase2.T.T<class Generated1046.BaseClass0,class Generated1046.BaseClass0,class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1518::Method7.16579<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase2.A.T<class Generated1046.BaseClass0,class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1518::Method7.16579<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase2.A.A<class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G2_C536::ClassMethod1341.MI.9928()#G2_C536::ClassMethod1342.MI.9929<System.Object>()#G2_C536::ClassMethod1343.MI.9930<System.Object>()#G3_C1518::ClassMethod2437.MI.16582()#G3_C1518::ClassMethod2438.MI.16583<System.Object>()#G3_C1518::ClassMethod4428.16581()#G2_C536::Method4.9921()#G2_C536::Method5.9923()#G2_C536::Method6.9924<System.Object>()#G3_C1518::Method7.16579<System.Object>()#"
		call void Generated1046.Generated1046::M.G3_C1518.T<class Generated1046.BaseClass0,class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G2_C536::ClassMethod1341.MI.9928()#G2_C536::ClassMethod1342.MI.9929<System.Object>()#G2_C536::ClassMethod1343.MI.9930<System.Object>()#G3_C1518::ClassMethod2437.MI.16582()#G3_C1518::ClassMethod2438.MI.16583<System.Object>()#G3_C1518::ClassMethod4428.16581()#G2_C536::Method4.9921()#G2_C536::Method5.9923()#G2_C536::Method6.9924<System.Object>()#G3_C1518::Method7.16579<System.Object>()#"
		call void Generated1046.Generated1046::M.G3_C1518.A<class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>>(!!0,string)
		newobj instance void class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G2_C536::ClassMethod1341.MI.9928()#G2_C536::ClassMethod1342.MI.9929<System.Object>()#G2_C536::ClassMethod1343.MI.9930<System.Object>()#G2_C536::Method4.9921()#G1_C10::Method5.4847()#G2_C536::Method6.9924<System.Object>()#G3_C1518::Method7.16579<System.Object>()#"
		call void Generated1046.Generated1046::M.G1_C10.T.T<class Generated1046.BaseClass1,class Generated1046.BaseClass1,class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G2_C536::ClassMethod1341.MI.9928()#G2_C536::ClassMethod1342.MI.9929<System.Object>()#G2_C536::ClassMethod1343.MI.9930<System.Object>()#G2_C536::Method4.9921()#G1_C10::Method5.4847()#G2_C536::Method6.9924<System.Object>()#G3_C1518::Method7.16579<System.Object>()#"
		call void Generated1046.Generated1046::M.G1_C10.B.T<class Generated1046.BaseClass1,class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G2_C536::ClassMethod1341.MI.9928()#G2_C536::ClassMethod1342.MI.9929<System.Object>()#G2_C536::ClassMethod1343.MI.9930<System.Object>()#G2_C536::Method4.9921()#G1_C10::Method5.4847()#G2_C536::Method6.9924<System.Object>()#G3_C1518::Method7.16579<System.Object>()#"
		call void Generated1046.Generated1046::M.G1_C10.B.B<class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1518::Method7.MI.16580<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase2.T.T<class Generated1046.BaseClass1,class Generated1046.BaseClass1,class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1518::Method7.MI.16580<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase2.B.T<class Generated1046.BaseClass1,class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1518::Method7.MI.16580<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase2.B.B<class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method4.MI.4846()#G1_C10::Method5.4847()#G1_C10::Method6.MI.4849<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase1.T<class Generated1046.BaseClass1,class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method4.MI.4846()#G1_C10::Method5.4847()#G1_C10::Method6.MI.4849<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase1.B<class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1518::Method7.MI.16580<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase2.T.T<class Generated1046.BaseClass0,class Generated1046.BaseClass1,class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1518::Method7.MI.16580<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase2.A.T<class Generated1046.BaseClass1,class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1518::Method7.MI.16580<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase2.A.B<class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C536::Method4.MI.9922()#G2_C536::Method5.9923()#G2_C536::Method6.9924<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase1.T<class Generated1046.BaseClass0,class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C536::Method4.MI.9922()#G2_C536::Method5.9923()#G2_C536::Method6.9924<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase1.A<class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G2_C536::ClassMethod1341.MI.9928()#G2_C536::ClassMethod1342.MI.9929<System.Object>()#G2_C536::ClassMethod1343.MI.9930<System.Object>()#G3_C1518::ClassMethod2437.MI.16582()#G3_C1518::ClassMethod2438.MI.16583<System.Object>()#G2_C536::Method4.9921()#G2_C536::Method5.9923()#G2_C536::Method6.9924<System.Object>()#G3_C1518::Method7.16579<System.Object>()#"
		call void Generated1046.Generated1046::M.G2_C536.T.T<class Generated1046.BaseClass1,class Generated1046.BaseClass1,class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G2_C536::ClassMethod1341.MI.9928()#G2_C536::ClassMethod1342.MI.9929<System.Object>()#G2_C536::ClassMethod1343.MI.9930<System.Object>()#G3_C1518::ClassMethod2437.MI.16582()#G3_C1518::ClassMethod2438.MI.16583<System.Object>()#G2_C536::Method4.9921()#G2_C536::Method5.9923()#G2_C536::Method6.9924<System.Object>()#G3_C1518::Method7.16579<System.Object>()#"
		call void Generated1046.Generated1046::M.G2_C536.B.T<class Generated1046.BaseClass1,class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G2_C536::ClassMethod1341.MI.9928()#G2_C536::ClassMethod1342.MI.9929<System.Object>()#G2_C536::ClassMethod1343.MI.9930<System.Object>()#G3_C1518::ClassMethod2437.MI.16582()#G3_C1518::ClassMethod2438.MI.16583<System.Object>()#G2_C536::Method4.9921()#G2_C536::Method5.9923()#G2_C536::Method6.9924<System.Object>()#G3_C1518::Method7.16579<System.Object>()#"
		call void Generated1046.Generated1046::M.G2_C536.B.B<class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1518::Method7.16579<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase2.T.T<class Generated1046.BaseClass1,class Generated1046.BaseClass0,class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1518::Method7.16579<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase2.B.T<class Generated1046.BaseClass0,class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1518::Method7.16579<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase2.B.A<class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1518::Method7.16579<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase2.T.T<class Generated1046.BaseClass0,class Generated1046.BaseClass0,class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1518::Method7.16579<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase2.A.T<class Generated1046.BaseClass0,class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1518::Method7.16579<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase2.A.A<class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G2_C536::ClassMethod1341.MI.9928()#G2_C536::ClassMethod1342.MI.9929<System.Object>()#G2_C536::ClassMethod1343.MI.9930<System.Object>()#G3_C1518::ClassMethod2437.MI.16582()#G3_C1518::ClassMethod2438.MI.16583<System.Object>()#G3_C1518::ClassMethod4428.16581()#G2_C536::Method4.9921()#G2_C536::Method5.9923()#G2_C536::Method6.9924<System.Object>()#G3_C1518::Method7.16579<System.Object>()#"
		call void Generated1046.Generated1046::M.G3_C1518.T<class Generated1046.BaseClass1,class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G2_C536::ClassMethod1341.MI.9928()#G2_C536::ClassMethod1342.MI.9929<System.Object>()#G2_C536::ClassMethod1343.MI.9930<System.Object>()#G3_C1518::ClassMethod2437.MI.16582()#G3_C1518::ClassMethod2438.MI.16583<System.Object>()#G3_C1518::ClassMethod4428.16581()#G2_C536::Method4.9921()#G2_C536::Method5.9923()#G2_C536::Method6.9924<System.Object>()#G3_C1518::Method7.16579<System.Object>()#"
		call void Generated1046.Generated1046::M.G3_C1518.B<class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>>(!!0,string)
		newobj instance void class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G2_C536::ClassMethod1341.MI.9928()#G2_C536::ClassMethod1342.MI.9929<System.Object>()#G2_C536::ClassMethod1343.MI.9930<System.Object>()#G2_C536::Method4.9921()#G1_C10::Method5.4847()#G2_C536::Method6.9924<System.Object>()#G2_C536::Method7.9925<System.Object>()#"
		call void Generated1046.Generated1046::M.G1_C10.T.T<class Generated1046.BaseClass1,class Generated1046.BaseClass0,class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G2_C536::ClassMethod1341.MI.9928()#G2_C536::ClassMethod1342.MI.9929<System.Object>()#G2_C536::ClassMethod1343.MI.9930<System.Object>()#G2_C536::Method4.9921()#G1_C10::Method5.4847()#G2_C536::Method6.9924<System.Object>()#G2_C536::Method7.9925<System.Object>()#"
		call void Generated1046.Generated1046::M.G1_C10.B.T<class Generated1046.BaseClass0,class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G2_C536::ClassMethod1341.MI.9928()#G2_C536::ClassMethod1342.MI.9929<System.Object>()#G2_C536::ClassMethod1343.MI.9930<System.Object>()#G2_C536::Method4.9921()#G1_C10::Method5.4847()#G2_C536::Method6.9924<System.Object>()#G2_C536::Method7.9925<System.Object>()#"
		call void Generated1046.Generated1046::M.G1_C10.B.A<class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C536::Method7.9925<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase2.T.T<class Generated1046.BaseClass1,class Generated1046.BaseClass1,class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C536::Method7.9925<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase2.B.T<class Generated1046.BaseClass1,class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C536::Method7.9925<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase2.B.B<class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method4.MI.4846()#G1_C10::Method5.4847()#G1_C10::Method6.MI.4849<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase1.T<class Generated1046.BaseClass1,class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method4.MI.4846()#G1_C10::Method5.4847()#G1_C10::Method6.MI.4849<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase1.B<class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C536::Method7.9925<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase2.T.T<class Generated1046.BaseClass0,class Generated1046.BaseClass1,class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C536::Method7.9925<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase2.A.T<class Generated1046.BaseClass1,class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C536::Method7.9925<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase2.A.B<class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C536::Method4.MI.9922()#G2_C536::Method5.9923()#G2_C536::Method6.9924<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase1.T<class Generated1046.BaseClass0,class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C536::Method4.MI.9922()#G2_C536::Method5.9923()#G2_C536::Method6.9924<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase1.A<class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G2_C536::ClassMethod1341.MI.9928()#G2_C536::ClassMethod1342.MI.9929<System.Object>()#G2_C536::ClassMethod1343.MI.9930<System.Object>()#G2_C536::ClassMethod2437.9926()#G2_C536::ClassMethod2438.9927<System.Object>()#G2_C536::Method4.9921()#G2_C536::Method5.9923()#G2_C536::Method6.9924<System.Object>()#G2_C536::Method7.9925<System.Object>()#"
		call void Generated1046.Generated1046::M.G2_C536.T.T<class Generated1046.BaseClass0,class Generated1046.BaseClass0,class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G2_C536::ClassMethod1341.MI.9928()#G2_C536::ClassMethod1342.MI.9929<System.Object>()#G2_C536::ClassMethod1343.MI.9930<System.Object>()#G2_C536::ClassMethod2437.9926()#G2_C536::ClassMethod2438.9927<System.Object>()#G2_C536::Method4.9921()#G2_C536::Method5.9923()#G2_C536::Method6.9924<System.Object>()#G2_C536::Method7.9925<System.Object>()#"
		call void Generated1046.Generated1046::M.G2_C536.A.T<class Generated1046.BaseClass0,class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G2_C536::ClassMethod1341.MI.9928()#G2_C536::ClassMethod1342.MI.9929<System.Object>()#G2_C536::ClassMethod1343.MI.9930<System.Object>()#G2_C536::ClassMethod2437.9926()#G2_C536::ClassMethod2438.9927<System.Object>()#G2_C536::Method4.9921()#G2_C536::Method5.9923()#G2_C536::Method6.9924<System.Object>()#G2_C536::Method7.9925<System.Object>()#"
		call void Generated1046.Generated1046::M.G2_C536.A.A<class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C536::Method7.9925<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase2.T.T<class Generated1046.BaseClass0,class Generated1046.BaseClass0,class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C536::Method7.9925<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase2.A.T<class Generated1046.BaseClass0,class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C536::Method7.9925<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase2.A.A<class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>>(!!0,string)
		newobj instance void class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G2_C536::ClassMethod1341.MI.9928()#G2_C536::ClassMethod1342.MI.9929<System.Object>()#G2_C536::ClassMethod1343.MI.9930<System.Object>()#G2_C536::Method4.9921()#G1_C10::Method5.4847()#G2_C536::Method6.9924<System.Object>()#G2_C536::Method7.9925<System.Object>()#"
		call void Generated1046.Generated1046::M.G1_C10.T.T<class Generated1046.BaseClass1,class Generated1046.BaseClass1,class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G2_C536::ClassMethod1341.MI.9928()#G2_C536::ClassMethod1342.MI.9929<System.Object>()#G2_C536::ClassMethod1343.MI.9930<System.Object>()#G2_C536::Method4.9921()#G1_C10::Method5.4847()#G2_C536::Method6.9924<System.Object>()#G2_C536::Method7.9925<System.Object>()#"
		call void Generated1046.Generated1046::M.G1_C10.B.T<class Generated1046.BaseClass1,class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G2_C536::ClassMethod1341.MI.9928()#G2_C536::ClassMethod1342.MI.9929<System.Object>()#G2_C536::ClassMethod1343.MI.9930<System.Object>()#G2_C536::Method4.9921()#G1_C10::Method5.4847()#G2_C536::Method6.9924<System.Object>()#G2_C536::Method7.9925<System.Object>()#"
		call void Generated1046.Generated1046::M.G1_C10.B.B<class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C536::Method7.9925<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase2.T.T<class Generated1046.BaseClass1,class Generated1046.BaseClass1,class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C536::Method7.9925<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase2.B.T<class Generated1046.BaseClass1,class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C536::Method7.9925<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase2.B.B<class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method4.MI.4846()#G1_C10::Method5.4847()#G1_C10::Method6.MI.4849<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase1.T<class Generated1046.BaseClass1,class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method4.MI.4846()#G1_C10::Method5.4847()#G1_C10::Method6.MI.4849<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase1.B<class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C536::Method7.9925<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase2.T.T<class Generated1046.BaseClass0,class Generated1046.BaseClass1,class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C536::Method7.9925<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase2.A.T<class Generated1046.BaseClass1,class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C536::Method7.9925<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase2.A.B<class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C536::Method4.MI.9922()#G2_C536::Method5.9923()#G2_C536::Method6.9924<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase1.T<class Generated1046.BaseClass0,class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C536::Method4.MI.9922()#G2_C536::Method5.9923()#G2_C536::Method6.9924<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase1.A<class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G2_C536::ClassMethod1341.MI.9928()#G2_C536::ClassMethod1342.MI.9929<System.Object>()#G2_C536::ClassMethod1343.MI.9930<System.Object>()#G2_C536::ClassMethod2437.9926()#G2_C536::ClassMethod2438.9927<System.Object>()#G2_C536::Method4.9921()#G2_C536::Method5.9923()#G2_C536::Method6.9924<System.Object>()#G2_C536::Method7.9925<System.Object>()#"
		call void Generated1046.Generated1046::M.G2_C536.T.T<class Generated1046.BaseClass0,class Generated1046.BaseClass1,class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G2_C536::ClassMethod1341.MI.9928()#G2_C536::ClassMethod1342.MI.9929<System.Object>()#G2_C536::ClassMethod1343.MI.9930<System.Object>()#G2_C536::ClassMethod2437.9926()#G2_C536::ClassMethod2438.9927<System.Object>()#G2_C536::Method4.9921()#G2_C536::Method5.9923()#G2_C536::Method6.9924<System.Object>()#G2_C536::Method7.9925<System.Object>()#"
		call void Generated1046.Generated1046::M.G2_C536.A.T<class Generated1046.BaseClass1,class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G2_C536::ClassMethod1341.MI.9928()#G2_C536::ClassMethod1342.MI.9929<System.Object>()#G2_C536::ClassMethod1343.MI.9930<System.Object>()#G2_C536::ClassMethod2437.9926()#G2_C536::ClassMethod2438.9927<System.Object>()#G2_C536::Method4.9921()#G2_C536::Method5.9923()#G2_C536::Method6.9924<System.Object>()#G2_C536::Method7.9925<System.Object>()#"
		call void Generated1046.Generated1046::M.G2_C536.A.B<class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C536::Method7.9925<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase2.T.T<class Generated1046.BaseClass0,class Generated1046.BaseClass0,class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C536::Method7.9925<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase2.A.T<class Generated1046.BaseClass0,class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C536::Method7.9925<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase2.A.A<class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>>(!!0,string)
		newobj instance void class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G2_C536::ClassMethod1341.MI.9928()#G2_C536::ClassMethod1342.MI.9929<System.Object>()#G2_C536::ClassMethod1343.MI.9930<System.Object>()#G2_C536::Method4.9921()#G1_C10::Method5.4847()#G2_C536::Method6.9924<System.Object>()#G2_C536::Method7.9925<System.Object>()#"
		call void Generated1046.Generated1046::M.G1_C10.T.T<class Generated1046.BaseClass1,class Generated1046.BaseClass0,class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G2_C536::ClassMethod1341.MI.9928()#G2_C536::ClassMethod1342.MI.9929<System.Object>()#G2_C536::ClassMethod1343.MI.9930<System.Object>()#G2_C536::Method4.9921()#G1_C10::Method5.4847()#G2_C536::Method6.9924<System.Object>()#G2_C536::Method7.9925<System.Object>()#"
		call void Generated1046.Generated1046::M.G1_C10.B.T<class Generated1046.BaseClass0,class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G2_C536::ClassMethod1341.MI.9928()#G2_C536::ClassMethod1342.MI.9929<System.Object>()#G2_C536::ClassMethod1343.MI.9930<System.Object>()#G2_C536::Method4.9921()#G1_C10::Method5.4847()#G2_C536::Method6.9924<System.Object>()#G2_C536::Method7.9925<System.Object>()#"
		call void Generated1046.Generated1046::M.G1_C10.B.A<class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C536::Method7.9925<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase2.T.T<class Generated1046.BaseClass1,class Generated1046.BaseClass1,class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C536::Method7.9925<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase2.B.T<class Generated1046.BaseClass1,class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C536::Method7.9925<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase2.B.B<class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method4.MI.4846()#G1_C10::Method5.4847()#G1_C10::Method6.MI.4849<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase1.T<class Generated1046.BaseClass1,class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method4.MI.4846()#G1_C10::Method5.4847()#G1_C10::Method6.MI.4849<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase1.B<class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C536::Method7.9925<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase2.T.T<class Generated1046.BaseClass0,class Generated1046.BaseClass1,class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C536::Method7.9925<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase2.A.T<class Generated1046.BaseClass1,class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C536::Method7.9925<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase2.A.B<class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C536::Method4.MI.9922()#G2_C536::Method5.9923()#G2_C536::Method6.9924<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase1.T<class Generated1046.BaseClass0,class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C536::Method4.MI.9922()#G2_C536::Method5.9923()#G2_C536::Method6.9924<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase1.A<class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G2_C536::ClassMethod1341.MI.9928()#G2_C536::ClassMethod1342.MI.9929<System.Object>()#G2_C536::ClassMethod1343.MI.9930<System.Object>()#G2_C536::ClassMethod2437.9926()#G2_C536::ClassMethod2438.9927<System.Object>()#G2_C536::Method4.9921()#G2_C536::Method5.9923()#G2_C536::Method6.9924<System.Object>()#G2_C536::Method7.9925<System.Object>()#"
		call void Generated1046.Generated1046::M.G2_C536.T.T<class Generated1046.BaseClass1,class Generated1046.BaseClass0,class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G2_C536::ClassMethod1341.MI.9928()#G2_C536::ClassMethod1342.MI.9929<System.Object>()#G2_C536::ClassMethod1343.MI.9930<System.Object>()#G2_C536::ClassMethod2437.9926()#G2_C536::ClassMethod2438.9927<System.Object>()#G2_C536::Method4.9921()#G2_C536::Method5.9923()#G2_C536::Method6.9924<System.Object>()#G2_C536::Method7.9925<System.Object>()#"
		call void Generated1046.Generated1046::M.G2_C536.B.T<class Generated1046.BaseClass0,class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G2_C536::ClassMethod1341.MI.9928()#G2_C536::ClassMethod1342.MI.9929<System.Object>()#G2_C536::ClassMethod1343.MI.9930<System.Object>()#G2_C536::ClassMethod2437.9926()#G2_C536::ClassMethod2438.9927<System.Object>()#G2_C536::Method4.9921()#G2_C536::Method5.9923()#G2_C536::Method6.9924<System.Object>()#G2_C536::Method7.9925<System.Object>()#"
		call void Generated1046.Generated1046::M.G2_C536.B.A<class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C536::Method7.9925<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase2.T.T<class Generated1046.BaseClass1,class Generated1046.BaseClass0,class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C536::Method7.9925<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase2.B.T<class Generated1046.BaseClass0,class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C536::Method7.9925<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase2.B.A<class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C536::Method7.9925<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase2.T.T<class Generated1046.BaseClass0,class Generated1046.BaseClass0,class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C536::Method7.9925<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase2.A.T<class Generated1046.BaseClass0,class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C536::Method7.9925<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase2.A.A<class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>>(!!0,string)
		newobj instance void class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G2_C536::ClassMethod1341.MI.9928()#G2_C536::ClassMethod1342.MI.9929<System.Object>()#G2_C536::ClassMethod1343.MI.9930<System.Object>()#G2_C536::Method4.9921()#G1_C10::Method5.4847()#G2_C536::Method6.9924<System.Object>()#G2_C536::Method7.9925<System.Object>()#"
		call void Generated1046.Generated1046::M.G1_C10.T.T<class Generated1046.BaseClass1,class Generated1046.BaseClass1,class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G2_C536::ClassMethod1341.MI.9928()#G2_C536::ClassMethod1342.MI.9929<System.Object>()#G2_C536::ClassMethod1343.MI.9930<System.Object>()#G2_C536::Method4.9921()#G1_C10::Method5.4847()#G2_C536::Method6.9924<System.Object>()#G2_C536::Method7.9925<System.Object>()#"
		call void Generated1046.Generated1046::M.G1_C10.B.T<class Generated1046.BaseClass1,class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G2_C536::ClassMethod1341.MI.9928()#G2_C536::ClassMethod1342.MI.9929<System.Object>()#G2_C536::ClassMethod1343.MI.9930<System.Object>()#G2_C536::Method4.9921()#G1_C10::Method5.4847()#G2_C536::Method6.9924<System.Object>()#G2_C536::Method7.9925<System.Object>()#"
		call void Generated1046.Generated1046::M.G1_C10.B.B<class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C536::Method7.9925<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase2.T.T<class Generated1046.BaseClass1,class Generated1046.BaseClass1,class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C536::Method7.9925<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase2.B.T<class Generated1046.BaseClass1,class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C536::Method7.9925<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase2.B.B<class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method4.MI.4846()#G1_C10::Method5.4847()#G1_C10::Method6.MI.4849<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase1.T<class Generated1046.BaseClass1,class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method4.MI.4846()#G1_C10::Method5.4847()#G1_C10::Method6.MI.4849<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase1.B<class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C536::Method7.9925<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase2.T.T<class Generated1046.BaseClass0,class Generated1046.BaseClass1,class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C536::Method7.9925<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase2.A.T<class Generated1046.BaseClass1,class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C536::Method7.9925<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase2.A.B<class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C536::Method4.MI.9922()#G2_C536::Method5.9923()#G2_C536::Method6.9924<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase1.T<class Generated1046.BaseClass0,class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C536::Method4.MI.9922()#G2_C536::Method5.9923()#G2_C536::Method6.9924<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase1.A<class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G2_C536::ClassMethod1341.MI.9928()#G2_C536::ClassMethod1342.MI.9929<System.Object>()#G2_C536::ClassMethod1343.MI.9930<System.Object>()#G2_C536::ClassMethod2437.9926()#G2_C536::ClassMethod2438.9927<System.Object>()#G2_C536::Method4.9921()#G2_C536::Method5.9923()#G2_C536::Method6.9924<System.Object>()#G2_C536::Method7.9925<System.Object>()#"
		call void Generated1046.Generated1046::M.G2_C536.T.T<class Generated1046.BaseClass1,class Generated1046.BaseClass1,class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G2_C536::ClassMethod1341.MI.9928()#G2_C536::ClassMethod1342.MI.9929<System.Object>()#G2_C536::ClassMethod1343.MI.9930<System.Object>()#G2_C536::ClassMethod2437.9926()#G2_C536::ClassMethod2438.9927<System.Object>()#G2_C536::Method4.9921()#G2_C536::Method5.9923()#G2_C536::Method6.9924<System.Object>()#G2_C536::Method7.9925<System.Object>()#"
		call void Generated1046.Generated1046::M.G2_C536.B.T<class Generated1046.BaseClass1,class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G2_C536::ClassMethod1341.MI.9928()#G2_C536::ClassMethod1342.MI.9929<System.Object>()#G2_C536::ClassMethod1343.MI.9930<System.Object>()#G2_C536::ClassMethod2437.9926()#G2_C536::ClassMethod2438.9927<System.Object>()#G2_C536::Method4.9921()#G2_C536::Method5.9923()#G2_C536::Method6.9924<System.Object>()#G2_C536::Method7.9925<System.Object>()#"
		call void Generated1046.Generated1046::M.G2_C536.B.B<class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C536::Method7.9925<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase2.T.T<class Generated1046.BaseClass1,class Generated1046.BaseClass0,class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C536::Method7.9925<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase2.B.T<class Generated1046.BaseClass0,class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C536::Method7.9925<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase2.B.A<class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C536::Method7.9925<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase2.T.T<class Generated1046.BaseClass0,class Generated1046.BaseClass0,class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C536::Method7.9925<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase2.A.T<class Generated1046.BaseClass0,class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C536::Method7.9925<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase2.A.A<class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>>(!!0,string)
		newobj instance void class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G1_C10::ClassMethod1342.4852<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G1_C10::Method4.4845()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated1046.Generated1046::M.G1_C10.T.T<class Generated1046.BaseClass0,class Generated1046.BaseClass0,class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G1_C10::ClassMethod1342.4852<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G1_C10::Method4.4845()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated1046.Generated1046::M.G1_C10.A.T<class Generated1046.BaseClass0,class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G1_C10::ClassMethod1342.4852<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G1_C10::Method4.4845()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated1046.Generated1046::M.G1_C10.A.A<class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase2.T.T<class Generated1046.BaseClass1,class Generated1046.BaseClass1,class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase2.B.T<class Generated1046.BaseClass1,class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase2.B.B<class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method4.MI.4846()#G1_C10::Method5.4847()#G1_C10::Method6.MI.4849<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase1.T<class Generated1046.BaseClass0,class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method4.MI.4846()#G1_C10::Method5.4847()#G1_C10::Method6.MI.4849<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase1.A<class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase2.T.T<class Generated1046.BaseClass0,class Generated1046.BaseClass1,class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase2.A.T<class Generated1046.BaseClass1,class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase2.A.B<class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>>(!!0,string)
		newobj instance void class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G1_C10::ClassMethod1342.4852<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G1_C10::Method4.4845()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated1046.Generated1046::M.G1_C10.T.T<class Generated1046.BaseClass0,class Generated1046.BaseClass1,class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G1_C10::ClassMethod1342.4852<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G1_C10::Method4.4845()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated1046.Generated1046::M.G1_C10.A.T<class Generated1046.BaseClass1,class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G1_C10::ClassMethod1342.4852<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G1_C10::Method4.4845()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated1046.Generated1046::M.G1_C10.A.B<class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase2.T.T<class Generated1046.BaseClass1,class Generated1046.BaseClass1,class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase2.B.T<class Generated1046.BaseClass1,class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase2.B.B<class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method4.MI.4846()#G1_C10::Method5.4847()#G1_C10::Method6.MI.4849<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase1.T<class Generated1046.BaseClass0,class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method4.MI.4846()#G1_C10::Method5.4847()#G1_C10::Method6.MI.4849<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase1.A<class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase2.T.T<class Generated1046.BaseClass0,class Generated1046.BaseClass1,class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase2.A.T<class Generated1046.BaseClass1,class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase2.A.B<class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>>(!!0,string)
		newobj instance void class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G1_C10::ClassMethod1342.4852<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G1_C10::Method4.4845()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated1046.Generated1046::M.G1_C10.T.T<class Generated1046.BaseClass1,class Generated1046.BaseClass0,class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G1_C10::ClassMethod1342.4852<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G1_C10::Method4.4845()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated1046.Generated1046::M.G1_C10.B.T<class Generated1046.BaseClass0,class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G1_C10::ClassMethod1342.4852<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G1_C10::Method4.4845()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated1046.Generated1046::M.G1_C10.B.A<class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase2.T.T<class Generated1046.BaseClass1,class Generated1046.BaseClass1,class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase2.B.T<class Generated1046.BaseClass1,class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase2.B.B<class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method4.MI.4846()#G1_C10::Method5.4847()#G1_C10::Method6.MI.4849<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase1.T<class Generated1046.BaseClass1,class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method4.MI.4846()#G1_C10::Method5.4847()#G1_C10::Method6.MI.4849<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase1.B<class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase2.T.T<class Generated1046.BaseClass0,class Generated1046.BaseClass1,class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase2.A.T<class Generated1046.BaseClass1,class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase2.A.B<class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method4.MI.4846()#G1_C10::Method5.4847()#G1_C10::Method6.MI.4849<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase1.T<class Generated1046.BaseClass0,class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method4.MI.4846()#G1_C10::Method5.4847()#G1_C10::Method6.MI.4849<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase1.A<class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>>(!!0,string)
		newobj instance void class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G1_C10::ClassMethod1342.4852<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G1_C10::Method4.4845()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated1046.Generated1046::M.G1_C10.T.T<class Generated1046.BaseClass1,class Generated1046.BaseClass1,class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G1_C10::ClassMethod1342.4852<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G1_C10::Method4.4845()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated1046.Generated1046::M.G1_C10.B.T<class Generated1046.BaseClass1,class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G1_C10::ClassMethod1342.4852<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G1_C10::Method4.4845()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated1046.Generated1046::M.G1_C10.B.B<class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase2.T.T<class Generated1046.BaseClass1,class Generated1046.BaseClass1,class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase2.B.T<class Generated1046.BaseClass1,class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase2.B.B<class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method4.MI.4846()#G1_C10::Method5.4847()#G1_C10::Method6.MI.4849<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase1.T<class Generated1046.BaseClass1,class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method4.MI.4846()#G1_C10::Method5.4847()#G1_C10::Method6.MI.4849<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase1.B<class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase2.T.T<class Generated1046.BaseClass0,class Generated1046.BaseClass1,class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase2.A.T<class Generated1046.BaseClass1,class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase2.A.B<class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method4.MI.4846()#G1_C10::Method5.4847()#G1_C10::Method6.MI.4849<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase1.T<class Generated1046.BaseClass0,class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method4.MI.4846()#G1_C10::Method5.4847()#G1_C10::Method6.MI.4849<System.Object>()#"
		call void Generated1046.Generated1046::M.IBase1.A<class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>>(!!0,string)
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
		newobj instance void class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::ClassMethod1343<object>()
		calli default string(class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>)
		ldstr "G2_C536::ClassMethod1343.MI.9930<System.Object>()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::ClassMethod1342<object>()
		calli default string(class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>)
		ldstr "G2_C536::ClassMethod1342.MI.9929<System.Object>()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::ClassMethod1341()
		calli default string(class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>)
		ldstr "G2_C536::ClassMethod1341.MI.9928()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::ClassMethod1340()
		calli default string(class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>)
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::Method6<object>()
		calli default string(class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>)
		ldstr "G2_C536::Method6.9924<System.Object>()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::Method5()
		calli default string(class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>)
		ldstr "G1_C10::Method5.4847()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::Method4()
		calli default string(class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>)
		ldstr "G2_C536::Method4.9921()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::Method7<object>()
		calli default string(class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>)
		ldstr "G3_C1518::Method7.16579<System.Object>()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1046.IBase2`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::Method7<object>()
		calli default string(class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>)
		ldstr "G3_C1518::Method7.16579<System.Object>()"
		ldstr "class IBase2`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1046.IBase1`1<class Generated1046.BaseClass1>::Method4()
		calli default string(class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>)
		ldstr "G1_C10::Method4.MI.4846()"
		ldstr "class IBase1`1<class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1046.IBase1`1<class Generated1046.BaseClass1>::Method5()
		calli default string(class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>)
		ldstr "G1_C10::Method5.4847()"
		ldstr "class IBase1`1<class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1046.IBase1`1<class Generated1046.BaseClass1>::Method6<object>()
		calli default string(class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>)
		ldstr "G1_C10::Method6.MI.4849<System.Object>()"
		ldstr "class IBase1`1<class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1046.IBase2`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::Method7<object>()
		calli default string(class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>)
		ldstr "G3_C1518::Method7.MI.16580<System.Object>()"
		ldstr "class IBase2`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1046.IBase1`1<class Generated1046.BaseClass0>::Method4()
		calli default string(class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>)
		ldstr "G2_C536::Method4.MI.9922()"
		ldstr "class IBase1`1<class Generated1046.BaseClass0> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1046.IBase1`1<class Generated1046.BaseClass0>::Method5()
		calli default string(class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>)
		ldstr "G2_C536::Method5.9923()"
		ldstr "class IBase1`1<class Generated1046.BaseClass0> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1046.IBase1`1<class Generated1046.BaseClass0>::Method6<object>()
		calli default string(class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>)
		ldstr "G2_C536::Method6.9924<System.Object>()"
		ldstr "class IBase1`1<class Generated1046.BaseClass0> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::ClassMethod2438<object>()
		calli default string(class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>)
		ldstr "G3_C1518::ClassMethod2438.MI.16583<System.Object>()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::ClassMethod2437()
		calli default string(class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>)
		ldstr "G3_C1518::ClassMethod2437.MI.16582()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::Method7<object>()
		calli default string(class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>)
		ldstr "G3_C1518::Method7.16579<System.Object>()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::Method6<object>()
		calli default string(class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>)
		ldstr "G2_C536::Method6.9924<System.Object>()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::Method5()
		calli default string(class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>)
		ldstr "G2_C536::Method5.9923()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::Method4()
		calli default string(class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>)
		ldstr "G2_C536::Method4.9921()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::ClassMethod1343<object>()
		calli default string(class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>)
		ldstr "G2_C536::ClassMethod1343.MI.9930<System.Object>()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::ClassMethod1342<object>()
		calli default string(class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>)
		ldstr "G2_C536::ClassMethod1342.MI.9929<System.Object>()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::ClassMethod1341()
		calli default string(class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>)
		ldstr "G2_C536::ClassMethod1341.MI.9928()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::ClassMethod1340()
		calli default string(class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>)
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1046.IBase2`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>::Method7<object>()
		calli default string(class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>)
		ldstr "G3_C1518::Method7.16579<System.Object>()"
		ldstr "class IBase2`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>::ClassMethod4428()
		calli default string(class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>)
		ldstr "G3_C1518::ClassMethod4428.16581()"
		ldstr "class G3_C1518`1<class Generated1046.BaseClass0> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>::Method7<object>()
		calli default string(class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>)
		ldstr "G3_C1518::Method7.16579<System.Object>()"
		ldstr "class G3_C1518`1<class Generated1046.BaseClass0> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>::ClassMethod2438<object>()
		calli default string(class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>)
		ldstr "G3_C1518::ClassMethod2438.MI.16583<System.Object>()"
		ldstr "class G3_C1518`1<class Generated1046.BaseClass0> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>::ClassMethod2437()
		calli default string(class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>)
		ldstr "G3_C1518::ClassMethod2437.MI.16582()"
		ldstr "class G3_C1518`1<class Generated1046.BaseClass0> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>::Method6<object>()
		calli default string(class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>)
		ldstr "G2_C536::Method6.9924<System.Object>()"
		ldstr "class G3_C1518`1<class Generated1046.BaseClass0> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>::Method5()
		calli default string(class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>)
		ldstr "G2_C536::Method5.9923()"
		ldstr "class G3_C1518`1<class Generated1046.BaseClass0> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>::Method4()
		calli default string(class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>)
		ldstr "G2_C536::Method4.9921()"
		ldstr "class G3_C1518`1<class Generated1046.BaseClass0> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>::ClassMethod1343<object>()
		calli default string(class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>)
		ldstr "G2_C536::ClassMethod1343.MI.9930<System.Object>()"
		ldstr "class G3_C1518`1<class Generated1046.BaseClass0> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>::ClassMethod1342<object>()
		calli default string(class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>)
		ldstr "G2_C536::ClassMethod1342.MI.9929<System.Object>()"
		ldstr "class G3_C1518`1<class Generated1046.BaseClass0> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>::ClassMethod1341()
		calli default string(class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>)
		ldstr "G2_C536::ClassMethod1341.MI.9928()"
		ldstr "class G3_C1518`1<class Generated1046.BaseClass0> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>::ClassMethod1340()
		calli default string(class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>)
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G3_C1518`1<class Generated1046.BaseClass0> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::ClassMethod1343<object>()
		calli default string(class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>)
		ldstr "G2_C536::ClassMethod1343.MI.9930<System.Object>()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::ClassMethod1342<object>()
		calli default string(class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>)
		ldstr "G2_C536::ClassMethod1342.MI.9929<System.Object>()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::ClassMethod1341()
		calli default string(class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>)
		ldstr "G2_C536::ClassMethod1341.MI.9928()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::ClassMethod1340()
		calli default string(class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>)
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::Method6<object>()
		calli default string(class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>)
		ldstr "G2_C536::Method6.9924<System.Object>()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::Method5()
		calli default string(class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>)
		ldstr "G1_C10::Method5.4847()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::Method4()
		calli default string(class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>)
		ldstr "G2_C536::Method4.9921()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::Method7<object>()
		calli default string(class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>)
		ldstr "G3_C1518::Method7.16579<System.Object>()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1046.IBase2`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::Method7<object>()
		calli default string(class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>)
		ldstr "G3_C1518::Method7.MI.16580<System.Object>()"
		ldstr "class IBase2`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1046.IBase1`1<class Generated1046.BaseClass1>::Method4()
		calli default string(class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>)
		ldstr "G1_C10::Method4.MI.4846()"
		ldstr "class IBase1`1<class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1046.IBase1`1<class Generated1046.BaseClass1>::Method5()
		calli default string(class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>)
		ldstr "G1_C10::Method5.4847()"
		ldstr "class IBase1`1<class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1046.IBase1`1<class Generated1046.BaseClass1>::Method6<object>()
		calli default string(class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>)
		ldstr "G1_C10::Method6.MI.4849<System.Object>()"
		ldstr "class IBase1`1<class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1046.IBase2`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::Method7<object>()
		calli default string(class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>)
		ldstr "G3_C1518::Method7.MI.16580<System.Object>()"
		ldstr "class IBase2`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1046.IBase1`1<class Generated1046.BaseClass0>::Method4()
		calli default string(class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>)
		ldstr "G2_C536::Method4.MI.9922()"
		ldstr "class IBase1`1<class Generated1046.BaseClass0> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1046.IBase1`1<class Generated1046.BaseClass0>::Method5()
		calli default string(class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>)
		ldstr "G2_C536::Method5.9923()"
		ldstr "class IBase1`1<class Generated1046.BaseClass0> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1046.IBase1`1<class Generated1046.BaseClass0>::Method6<object>()
		calli default string(class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>)
		ldstr "G2_C536::Method6.9924<System.Object>()"
		ldstr "class IBase1`1<class Generated1046.BaseClass0> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::ClassMethod2438<object>()
		calli default string(class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>)
		ldstr "G3_C1518::ClassMethod2438.MI.16583<System.Object>()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::ClassMethod2437()
		calli default string(class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>)
		ldstr "G3_C1518::ClassMethod2437.MI.16582()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::Method7<object>()
		calli default string(class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>)
		ldstr "G3_C1518::Method7.16579<System.Object>()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::Method6<object>()
		calli default string(class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>)
		ldstr "G2_C536::Method6.9924<System.Object>()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::Method5()
		calli default string(class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>)
		ldstr "G2_C536::Method5.9923()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::Method4()
		calli default string(class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>)
		ldstr "G2_C536::Method4.9921()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::ClassMethod1343<object>()
		calli default string(class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>)
		ldstr "G2_C536::ClassMethod1343.MI.9930<System.Object>()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::ClassMethod1342<object>()
		calli default string(class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>)
		ldstr "G2_C536::ClassMethod1342.MI.9929<System.Object>()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::ClassMethod1341()
		calli default string(class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>)
		ldstr "G2_C536::ClassMethod1341.MI.9928()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::ClassMethod1340()
		calli default string(class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>)
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1046.IBase2`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::Method7<object>()
		calli default string(class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>)
		ldstr "G3_C1518::Method7.16579<System.Object>()"
		ldstr "class IBase2`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1046.IBase2`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>::Method7<object>()
		calli default string(class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>)
		ldstr "G3_C1518::Method7.16579<System.Object>()"
		ldstr "class IBase2`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>::ClassMethod4428()
		calli default string(class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>)
		ldstr "G3_C1518::ClassMethod4428.16581()"
		ldstr "class G3_C1518`1<class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>::Method7<object>()
		calli default string(class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>)
		ldstr "G3_C1518::Method7.16579<System.Object>()"
		ldstr "class G3_C1518`1<class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>::ClassMethod2438<object>()
		calli default string(class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>)
		ldstr "G3_C1518::ClassMethod2438.MI.16583<System.Object>()"
		ldstr "class G3_C1518`1<class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>::ClassMethod2437()
		calli default string(class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>)
		ldstr "G3_C1518::ClassMethod2437.MI.16582()"
		ldstr "class G3_C1518`1<class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>::Method6<object>()
		calli default string(class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>)
		ldstr "G2_C536::Method6.9924<System.Object>()"
		ldstr "class G3_C1518`1<class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>::Method5()
		calli default string(class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>)
		ldstr "G2_C536::Method5.9923()"
		ldstr "class G3_C1518`1<class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>::Method4()
		calli default string(class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>)
		ldstr "G2_C536::Method4.9921()"
		ldstr "class G3_C1518`1<class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>::ClassMethod1343<object>()
		calli default string(class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>)
		ldstr "G2_C536::ClassMethod1343.MI.9930<System.Object>()"
		ldstr "class G3_C1518`1<class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>::ClassMethod1342<object>()
		calli default string(class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>)
		ldstr "G2_C536::ClassMethod1342.MI.9929<System.Object>()"
		ldstr "class G3_C1518`1<class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>::ClassMethod1341()
		calli default string(class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>)
		ldstr "G2_C536::ClassMethod1341.MI.9928()"
		ldstr "class G3_C1518`1<class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>::ClassMethod1340()
		calli default string(class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>)
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G3_C1518`1<class Generated1046.BaseClass1> on type class Generated1046.G3_C1518`1<class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::ClassMethod1343<object>()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>)
		ldstr "G2_C536::ClassMethod1343.MI.9930<System.Object>()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::ClassMethod1342<object>()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>)
		ldstr "G2_C536::ClassMethod1342.MI.9929<System.Object>()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::ClassMethod1341()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>)
		ldstr "G2_C536::ClassMethod1341.MI.9928()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::ClassMethod1340()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>)
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::Method6<object>()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>)
		ldstr "G2_C536::Method6.9924<System.Object>()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::Method5()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>)
		ldstr "G1_C10::Method5.4847()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::Method4()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>)
		ldstr "G2_C536::Method4.9921()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::Method7<object>()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>)
		ldstr "G2_C536::Method7.9925<System.Object>()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1046.IBase2`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::Method7<object>()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>)
		ldstr "G2_C536::Method7.9925<System.Object>()"
		ldstr "class IBase2`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1046.IBase1`1<class Generated1046.BaseClass1>::Method4()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>)
		ldstr "G1_C10::Method4.MI.4846()"
		ldstr "class IBase1`1<class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1046.IBase1`1<class Generated1046.BaseClass1>::Method5()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>)
		ldstr "G1_C10::Method5.4847()"
		ldstr "class IBase1`1<class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1046.IBase1`1<class Generated1046.BaseClass1>::Method6<object>()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>)
		ldstr "G1_C10::Method6.MI.4849<System.Object>()"
		ldstr "class IBase1`1<class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1046.IBase2`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::Method7<object>()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>)
		ldstr "G2_C536::Method7.9925<System.Object>()"
		ldstr "class IBase2`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1046.IBase1`1<class Generated1046.BaseClass0>::Method4()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>)
		ldstr "G2_C536::Method4.MI.9922()"
		ldstr "class IBase1`1<class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1046.IBase1`1<class Generated1046.BaseClass0>::Method5()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>)
		ldstr "G2_C536::Method5.9923()"
		ldstr "class IBase1`1<class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1046.IBase1`1<class Generated1046.BaseClass0>::Method6<object>()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>)
		ldstr "G2_C536::Method6.9924<System.Object>()"
		ldstr "class IBase1`1<class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>::ClassMethod2438<object>()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>)
		ldstr "G2_C536::ClassMethod2438.9927<System.Object>()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>::ClassMethod2437()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>)
		ldstr "G2_C536::ClassMethod2437.9926()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>::Method7<object>()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>)
		ldstr "G2_C536::Method7.9925<System.Object>()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>::Method6<object>()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>)
		ldstr "G2_C536::Method6.9924<System.Object>()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>::Method5()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>)
		ldstr "G2_C536::Method5.9923()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>::Method4()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>)
		ldstr "G2_C536::Method4.9921()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>::ClassMethod1343<object>()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>)
		ldstr "G2_C536::ClassMethod1343.MI.9930<System.Object>()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>::ClassMethod1342<object>()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>)
		ldstr "G2_C536::ClassMethod1342.MI.9929<System.Object>()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>::ClassMethod1341()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>)
		ldstr "G2_C536::ClassMethod1341.MI.9928()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>::ClassMethod1340()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>)
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1046.IBase2`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>::Method7<object>()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>)
		ldstr "G2_C536::Method7.9925<System.Object>()"
		ldstr "class IBase2`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::ClassMethod1343<object>()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>)
		ldstr "G2_C536::ClassMethod1343.MI.9930<System.Object>()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::ClassMethod1342<object>()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>)
		ldstr "G2_C536::ClassMethod1342.MI.9929<System.Object>()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::ClassMethod1341()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>)
		ldstr "G2_C536::ClassMethod1341.MI.9928()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::ClassMethod1340()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>)
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::Method6<object>()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>)
		ldstr "G2_C536::Method6.9924<System.Object>()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::Method5()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>)
		ldstr "G1_C10::Method5.4847()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::Method4()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>)
		ldstr "G2_C536::Method4.9921()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::Method7<object>()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>)
		ldstr "G2_C536::Method7.9925<System.Object>()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1046.IBase2`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::Method7<object>()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>)
		ldstr "G2_C536::Method7.9925<System.Object>()"
		ldstr "class IBase2`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1046.IBase1`1<class Generated1046.BaseClass1>::Method4()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>)
		ldstr "G1_C10::Method4.MI.4846()"
		ldstr "class IBase1`1<class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1046.IBase1`1<class Generated1046.BaseClass1>::Method5()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>)
		ldstr "G1_C10::Method5.4847()"
		ldstr "class IBase1`1<class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1046.IBase1`1<class Generated1046.BaseClass1>::Method6<object>()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>)
		ldstr "G1_C10::Method6.MI.4849<System.Object>()"
		ldstr "class IBase1`1<class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1046.IBase2`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::Method7<object>()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>)
		ldstr "G2_C536::Method7.9925<System.Object>()"
		ldstr "class IBase2`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1046.IBase1`1<class Generated1046.BaseClass0>::Method4()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>)
		ldstr "G2_C536::Method4.MI.9922()"
		ldstr "class IBase1`1<class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1046.IBase1`1<class Generated1046.BaseClass0>::Method5()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>)
		ldstr "G2_C536::Method5.9923()"
		ldstr "class IBase1`1<class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1046.IBase1`1<class Generated1046.BaseClass0>::Method6<object>()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>)
		ldstr "G2_C536::Method6.9924<System.Object>()"
		ldstr "class IBase1`1<class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::ClassMethod2438<object>()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>)
		ldstr "G2_C536::ClassMethod2438.9927<System.Object>()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::ClassMethod2437()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>)
		ldstr "G2_C536::ClassMethod2437.9926()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::Method7<object>()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>)
		ldstr "G2_C536::Method7.9925<System.Object>()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::Method6<object>()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>)
		ldstr "G2_C536::Method6.9924<System.Object>()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::Method5()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>)
		ldstr "G2_C536::Method5.9923()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::Method4()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>)
		ldstr "G2_C536::Method4.9921()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::ClassMethod1343<object>()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>)
		ldstr "G2_C536::ClassMethod1343.MI.9930<System.Object>()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::ClassMethod1342<object>()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>)
		ldstr "G2_C536::ClassMethod1342.MI.9929<System.Object>()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::ClassMethod1341()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>)
		ldstr "G2_C536::ClassMethod1341.MI.9928()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::ClassMethod1340()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>)
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1046.IBase2`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>::Method7<object>()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>)
		ldstr "G2_C536::Method7.9925<System.Object>()"
		ldstr "class IBase2`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::ClassMethod1343<object>()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>)
		ldstr "G2_C536::ClassMethod1343.MI.9930<System.Object>()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::ClassMethod1342<object>()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>)
		ldstr "G2_C536::ClassMethod1342.MI.9929<System.Object>()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::ClassMethod1341()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>)
		ldstr "G2_C536::ClassMethod1341.MI.9928()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::ClassMethod1340()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>)
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::Method6<object>()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>)
		ldstr "G2_C536::Method6.9924<System.Object>()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::Method5()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>)
		ldstr "G1_C10::Method5.4847()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::Method4()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>)
		ldstr "G2_C536::Method4.9921()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::Method7<object>()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>)
		ldstr "G2_C536::Method7.9925<System.Object>()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1046.IBase2`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::Method7<object>()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>)
		ldstr "G2_C536::Method7.9925<System.Object>()"
		ldstr "class IBase2`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1046.IBase1`1<class Generated1046.BaseClass1>::Method4()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>)
		ldstr "G1_C10::Method4.MI.4846()"
		ldstr "class IBase1`1<class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1046.IBase1`1<class Generated1046.BaseClass1>::Method5()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>)
		ldstr "G1_C10::Method5.4847()"
		ldstr "class IBase1`1<class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1046.IBase1`1<class Generated1046.BaseClass1>::Method6<object>()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>)
		ldstr "G1_C10::Method6.MI.4849<System.Object>()"
		ldstr "class IBase1`1<class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1046.IBase2`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::Method7<object>()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>)
		ldstr "G2_C536::Method7.9925<System.Object>()"
		ldstr "class IBase2`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1046.IBase1`1<class Generated1046.BaseClass0>::Method4()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>)
		ldstr "G2_C536::Method4.MI.9922()"
		ldstr "class IBase1`1<class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1046.IBase1`1<class Generated1046.BaseClass0>::Method5()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>)
		ldstr "G2_C536::Method5.9923()"
		ldstr "class IBase1`1<class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1046.IBase1`1<class Generated1046.BaseClass0>::Method6<object>()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>)
		ldstr "G2_C536::Method6.9924<System.Object>()"
		ldstr "class IBase1`1<class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::ClassMethod2438<object>()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>)
		ldstr "G2_C536::ClassMethod2438.9927<System.Object>()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::ClassMethod2437()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>)
		ldstr "G2_C536::ClassMethod2437.9926()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::Method7<object>()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>)
		ldstr "G2_C536::Method7.9925<System.Object>()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::Method6<object>()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>)
		ldstr "G2_C536::Method6.9924<System.Object>()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::Method5()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>)
		ldstr "G2_C536::Method5.9923()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::Method4()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>)
		ldstr "G2_C536::Method4.9921()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::ClassMethod1343<object>()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>)
		ldstr "G2_C536::ClassMethod1343.MI.9930<System.Object>()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::ClassMethod1342<object>()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>)
		ldstr "G2_C536::ClassMethod1342.MI.9929<System.Object>()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::ClassMethod1341()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>)
		ldstr "G2_C536::ClassMethod1341.MI.9928()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::ClassMethod1340()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>)
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1046.IBase2`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::Method7<object>()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>)
		ldstr "G2_C536::Method7.9925<System.Object>()"
		ldstr "class IBase2`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1046.IBase2`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>::Method7<object>()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>)
		ldstr "G2_C536::Method7.9925<System.Object>()"
		ldstr "class IBase2`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::ClassMethod1343<object>()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>)
		ldstr "G2_C536::ClassMethod1343.MI.9930<System.Object>()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::ClassMethod1342<object>()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>)
		ldstr "G2_C536::ClassMethod1342.MI.9929<System.Object>()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::ClassMethod1341()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>)
		ldstr "G2_C536::ClassMethod1341.MI.9928()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::ClassMethod1340()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>)
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::Method6<object>()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>)
		ldstr "G2_C536::Method6.9924<System.Object>()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::Method5()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>)
		ldstr "G1_C10::Method5.4847()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::Method4()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>)
		ldstr "G2_C536::Method4.9921()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::Method7<object>()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>)
		ldstr "G2_C536::Method7.9925<System.Object>()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1046.IBase2`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::Method7<object>()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>)
		ldstr "G2_C536::Method7.9925<System.Object>()"
		ldstr "class IBase2`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1046.IBase1`1<class Generated1046.BaseClass1>::Method4()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>)
		ldstr "G1_C10::Method4.MI.4846()"
		ldstr "class IBase1`1<class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1046.IBase1`1<class Generated1046.BaseClass1>::Method5()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>)
		ldstr "G1_C10::Method5.4847()"
		ldstr "class IBase1`1<class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1046.IBase1`1<class Generated1046.BaseClass1>::Method6<object>()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>)
		ldstr "G1_C10::Method6.MI.4849<System.Object>()"
		ldstr "class IBase1`1<class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1046.IBase2`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::Method7<object>()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>)
		ldstr "G2_C536::Method7.9925<System.Object>()"
		ldstr "class IBase2`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1046.IBase1`1<class Generated1046.BaseClass0>::Method4()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>)
		ldstr "G2_C536::Method4.MI.9922()"
		ldstr "class IBase1`1<class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1046.IBase1`1<class Generated1046.BaseClass0>::Method5()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>)
		ldstr "G2_C536::Method5.9923()"
		ldstr "class IBase1`1<class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1046.IBase1`1<class Generated1046.BaseClass0>::Method6<object>()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>)
		ldstr "G2_C536::Method6.9924<System.Object>()"
		ldstr "class IBase1`1<class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::ClassMethod2438<object>()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>)
		ldstr "G2_C536::ClassMethod2438.9927<System.Object>()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::ClassMethod2437()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>)
		ldstr "G2_C536::ClassMethod2437.9926()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::Method7<object>()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>)
		ldstr "G2_C536::Method7.9925<System.Object>()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::Method6<object>()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>)
		ldstr "G2_C536::Method6.9924<System.Object>()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::Method5()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>)
		ldstr "G2_C536::Method5.9923()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::Method4()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>)
		ldstr "G2_C536::Method4.9921()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::ClassMethod1343<object>()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>)
		ldstr "G2_C536::ClassMethod1343.MI.9930<System.Object>()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::ClassMethod1342<object>()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>)
		ldstr "G2_C536::ClassMethod1342.MI.9929<System.Object>()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::ClassMethod1341()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>)
		ldstr "G2_C536::ClassMethod1341.MI.9928()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::ClassMethod1340()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>)
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1046.IBase2`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::Method7<object>()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>)
		ldstr "G2_C536::Method7.9925<System.Object>()"
		ldstr "class IBase2`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1046.IBase2`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>::Method7<object>()
		calli default string(class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>)
		ldstr "G2_C536::Method7.9925<System.Object>()"
		ldstr "class IBase2`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0> on type class Generated1046.G2_C536`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>::ClassMethod1343<object>()
		calli default string(class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>)
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>::ClassMethod1342<object>()
		calli default string(class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>)
		ldstr "G1_C10::ClassMethod1342.4852<System.Object>()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>::ClassMethod1341()
		calli default string(class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>)
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>::ClassMethod1340()
		calli default string(class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>)
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>::Method6<object>()
		calli default string(class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>)
		ldstr "G1_C10::Method6.4848<System.Object>()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>::Method5()
		calli default string(class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>)
		ldstr "G1_C10::Method5.4847()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>::Method4()
		calli default string(class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>)
		ldstr "G1_C10::Method4.4845()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>::Method7<object>()
		calli default string(class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>)
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1046.IBase2`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::Method7<object>()
		calli default string(class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>)
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1046.IBase1`1<class Generated1046.BaseClass0>::Method4()
		calli default string(class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>)
		ldstr "G1_C10::Method4.MI.4846()"
		ldstr "class IBase1`1<class Generated1046.BaseClass0> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1046.IBase1`1<class Generated1046.BaseClass0>::Method5()
		calli default string(class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>)
		ldstr "G1_C10::Method5.4847()"
		ldstr "class IBase1`1<class Generated1046.BaseClass0> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1046.IBase1`1<class Generated1046.BaseClass0>::Method6<object>()
		calli default string(class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>)
		ldstr "G1_C10::Method6.MI.4849<System.Object>()"
		ldstr "class IBase1`1<class Generated1046.BaseClass0> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1046.IBase2`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::Method7<object>()
		calli default string(class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>)
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::ClassMethod1343<object>()
		calli default string(class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>)
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::ClassMethod1342<object>()
		calli default string(class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>)
		ldstr "G1_C10::ClassMethod1342.4852<System.Object>()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::ClassMethod1341()
		calli default string(class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>)
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::ClassMethod1340()
		calli default string(class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>)
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::Method6<object>()
		calli default string(class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>)
		ldstr "G1_C10::Method6.4848<System.Object>()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::Method5()
		calli default string(class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>)
		ldstr "G1_C10::Method5.4847()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::Method4()
		calli default string(class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>)
		ldstr "G1_C10::Method4.4845()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::Method7<object>()
		calli default string(class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>)
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1046.IBase2`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::Method7<object>()
		calli default string(class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>)
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1046.IBase1`1<class Generated1046.BaseClass0>::Method4()
		calli default string(class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>)
		ldstr "G1_C10::Method4.MI.4846()"
		ldstr "class IBase1`1<class Generated1046.BaseClass0> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1046.IBase1`1<class Generated1046.BaseClass0>::Method5()
		calli default string(class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>)
		ldstr "G1_C10::Method5.4847()"
		ldstr "class IBase1`1<class Generated1046.BaseClass0> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1046.IBase1`1<class Generated1046.BaseClass0>::Method6<object>()
		calli default string(class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>)
		ldstr "G1_C10::Method6.MI.4849<System.Object>()"
		ldstr "class IBase1`1<class Generated1046.BaseClass0> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1046.IBase2`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::Method7<object>()
		calli default string(class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>)
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::ClassMethod1343<object>()
		calli default string(class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>)
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::ClassMethod1342<object>()
		calli default string(class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>)
		ldstr "G1_C10::ClassMethod1342.4852<System.Object>()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::ClassMethod1341()
		calli default string(class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>)
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::ClassMethod1340()
		calli default string(class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>)
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::Method6<object>()
		calli default string(class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>)
		ldstr "G1_C10::Method6.4848<System.Object>()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::Method5()
		calli default string(class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>)
		ldstr "G1_C10::Method5.4847()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::Method4()
		calli default string(class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>)
		ldstr "G1_C10::Method4.4845()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>::Method7<object>()
		calli default string(class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>)
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1046.IBase2`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::Method7<object>()
		calli default string(class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>)
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1046.IBase1`1<class Generated1046.BaseClass1>::Method4()
		calli default string(class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>)
		ldstr "G1_C10::Method4.MI.4846()"
		ldstr "class IBase1`1<class Generated1046.BaseClass1> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1046.IBase1`1<class Generated1046.BaseClass1>::Method5()
		calli default string(class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>)
		ldstr "G1_C10::Method5.4847()"
		ldstr "class IBase1`1<class Generated1046.BaseClass1> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1046.IBase1`1<class Generated1046.BaseClass1>::Method6<object>()
		calli default string(class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>)
		ldstr "G1_C10::Method6.MI.4849<System.Object>()"
		ldstr "class IBase1`1<class Generated1046.BaseClass1> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1046.IBase2`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::Method7<object>()
		calli default string(class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>)
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1046.IBase1`1<class Generated1046.BaseClass0>::Method4()
		calli default string(class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>)
		ldstr "G1_C10::Method4.MI.4846()"
		ldstr "class IBase1`1<class Generated1046.BaseClass0> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1046.IBase1`1<class Generated1046.BaseClass0>::Method5()
		calli default string(class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>)
		ldstr "G1_C10::Method5.4847()"
		ldstr "class IBase1`1<class Generated1046.BaseClass0> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1046.IBase1`1<class Generated1046.BaseClass0>::Method6<object>()
		calli default string(class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>)
		ldstr "G1_C10::Method6.MI.4849<System.Object>()"
		ldstr "class IBase1`1<class Generated1046.BaseClass0> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::ClassMethod1343<object>()
		calli default string(class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>)
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::ClassMethod1342<object>()
		calli default string(class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>)
		ldstr "G1_C10::ClassMethod1342.4852<System.Object>()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::ClassMethod1341()
		calli default string(class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>)
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::ClassMethod1340()
		calli default string(class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>)
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::Method6<object>()
		calli default string(class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>)
		ldstr "G1_C10::Method6.4848<System.Object>()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::Method5()
		calli default string(class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>)
		ldstr "G1_C10::Method5.4847()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::Method4()
		calli default string(class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>)
		ldstr "G1_C10::Method4.4845()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::Method7<object>()
		calli default string(class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>)
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1046.IBase2`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>::Method7<object>()
		calli default string(class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>)
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1046.IBase1`1<class Generated1046.BaseClass1>::Method4()
		calli default string(class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>)
		ldstr "G1_C10::Method4.MI.4846()"
		ldstr "class IBase1`1<class Generated1046.BaseClass1> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1046.IBase1`1<class Generated1046.BaseClass1>::Method5()
		calli default string(class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>)
		ldstr "G1_C10::Method5.4847()"
		ldstr "class IBase1`1<class Generated1046.BaseClass1> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1046.IBase1`1<class Generated1046.BaseClass1>::Method6<object>()
		calli default string(class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>)
		ldstr "G1_C10::Method6.MI.4849<System.Object>()"
		ldstr "class IBase1`1<class Generated1046.BaseClass1> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1046.IBase2`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1>::Method7<object>()
		calli default string(class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>)
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class Generated1046.BaseClass0,class Generated1046.BaseClass1> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1046.IBase1`1<class Generated1046.BaseClass0>::Method4()
		calli default string(class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>)
		ldstr "G1_C10::Method4.MI.4846()"
		ldstr "class IBase1`1<class Generated1046.BaseClass0> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1046.IBase1`1<class Generated1046.BaseClass0>::Method5()
		calli default string(class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>)
		ldstr "G1_C10::Method5.4847()"
		ldstr "class IBase1`1<class Generated1046.BaseClass0> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1046.IBase1`1<class Generated1046.BaseClass0>::Method6<object>()
		calli default string(class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>)
		ldstr "G1_C10::Method6.MI.4849<System.Object>()"
		ldstr "class IBase1`1<class Generated1046.BaseClass0> on type class Generated1046.G1_C10`2<class Generated1046.BaseClass1,class Generated1046.BaseClass1>"
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
		call void Generated1046.Generated1046::MethodCallingTest()
		call void Generated1046.Generated1046::ConstrainedCallsTest()
		call void Generated1046.Generated1046::StructConstrainedInterfaceCallsTest()
		call void Generated1046.Generated1046::CalliTest()
		ldc.i4 100
		ret
	}
}
