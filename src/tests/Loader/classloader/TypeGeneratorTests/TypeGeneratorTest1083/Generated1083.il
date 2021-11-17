// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) .ver 4:0:0:0 }
.assembly extern TestFramework { .publickeytoken = ( B0 3F 5F 7F 11 D5 0A 3A )  }

//TYPES IN FORWARDER ASSEMBLIES:

//TEST ASSEMBLY:
.assembly Generated1083 { .hash algorithm 0x00008004 }
.assembly extern xunit.core {}

.class public BaseClass0 
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void [mscorlib]System.Object::.ctor()
		ret
	}
}
.class public BaseClass1 
		extends BaseClass0
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void BaseClass0::.ctor()
		ret
	}
}
.class public G3_C1555`1<T0> 
		extends class G2_C564`2<class BaseClass0,class BaseClass1>
		implements class IBase2`2<class BaseClass1,!T0> 
{
	.method public hidebysig virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G3_C1555::Method7.16781<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod4504() cil managed noinlining { 
		ldstr "G3_C1555::ClassMethod4504.16782()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod4505<M0>() cil managed noinlining { 
		ldstr "G3_C1555::ClassMethod4505.16783<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod4506<M0>() cil managed noinlining { 
		ldstr "G3_C1555::ClassMethod4506.16784<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C564<class BaseClass0,class BaseClass1>.ClassMethod2500'() cil managed noinlining { 
		.override method instance string class G2_C564`2<class BaseClass0,class BaseClass1>::ClassMethod2500()
		ldstr "G3_C1555::ClassMethod2500.MI.16785()"
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class G2_C564`2<class BaseClass0,class BaseClass1>::.ctor()
		ret
	}
}
.class public G2_C564`2<T0, T1> 
		extends class G1_C10`2<!T1,!T1>
		implements class IBase2`2<class BaseClass0,!T1>, class IBase1`1<!T1> 
{
	.method public hidebysig newslot virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G2_C564::Method7.10193<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig virtual instance string Method4() cil managed noinlining { 
		ldstr "G2_C564::Method4.10194()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method5() cil managed noinlining { 
		ldstr "G2_C564::Method5.10195()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<T1>.Method5'() cil managed noinlining { 
		.override method instance string class IBase1`1<!T1>::Method5()
		ldstr "G2_C564::Method5.MI.10196()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G2_C564::Method6.10197<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<T1>.Method6'<M0>() cil managed noinlining { 
		.override method instance string class IBase1`1<!T1>::Method6<[1]>()
		ldstr "G2_C564::Method6.MI.10198<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod2500() cil managed noinlining { 
		ldstr "G2_C564::ClassMethod2500.10199()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod2501() cil managed noinlining { 
		ldstr "G2_C564::ClassMethod2501.10200()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod2502<M0>() cil managed noinlining { 
		ldstr "G2_C564::ClassMethod2502.10201<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C10<T1,T1>.ClassMethod1340'() cil managed noinlining { 
		.override method instance string class G1_C10`2<!T1,!T1>::ClassMethod1340()
		ldstr "G2_C564::ClassMethod1340.MI.10202()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C10<T1,T1>.ClassMethod1341'() cil managed noinlining { 
		.override method instance string class G1_C10`2<!T1,!T1>::ClassMethod1341()
		ldstr "G2_C564::ClassMethod1341.MI.10203()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C10<T1,T1>.ClassMethod1342'<M0>() cil managed noinlining { 
		.override method instance string class G1_C10`2<!T1,!T1>::ClassMethod1342<[1]>()
		ldstr "G2_C564::ClassMethod1342.MI.10204<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class G1_C10`2<!T1,!T1>::.ctor()
		ret
	}
}
.class interface public abstract IBase2`2<+T0, -T1> 
{
	.method public hidebysig newslot abstract virtual instance string Method7<M0>() cil managed { }
}
.class public G1_C10`2<T0, T1> 
		implements class IBase2`2<class BaseClass1,class BaseClass1>, class IBase1`1<!T0> 
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
		.override method instance string class IBase1`1<!T0>::Method4()
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
		.override method instance string class IBase1`1<!T0>::Method6<[1]>()
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
.class interface public abstract IBase1`1<+T0> 
{
	.method public hidebysig newslot abstract virtual instance string Method4() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method5() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method6<M0>() cil managed { }
}
.class public auto ansi beforefieldinit Generated1083 {
	.method static void M.BaseClass0<(BaseClass0)W>(!!W inst, string exp) cil managed {
		.maxstack 5
		.locals init (string[] actualResults)
		ldc.i4.s    0
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.BaseClass0<(BaseClass0)W>(!!W inst, string exp)"
		ldc.i4.s 0
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.BaseClass1<(BaseClass1)W>(!!W inst, string exp) cil managed {
		.maxstack 5
		.locals init (string[] actualResults)
		ldc.i4.s    0
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.BaseClass1<(BaseClass1)W>(!!W inst, string exp)"
		ldc.i4.s 0
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1555.T<T0,(class G3_C1555`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 19
		.locals init (string[] actualResults)
		ldc.i4.s    14
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1555.T<T0,(class G3_C1555`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 14
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1555`1<!!T0>::ClassMethod1340()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1555`1<!!T0>::ClassMethod1341()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1555`1<!!T0>::ClassMethod1342<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1555`1<!!T0>::ClassMethod1343<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1555`1<!!T0>::ClassMethod2500()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1555`1<!!T0>::ClassMethod2501()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1555`1<!!T0>::ClassMethod2502<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1555`1<!!T0>::ClassMethod4504()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1555`1<!!T0>::ClassMethod4505<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1555`1<!!T0>::ClassMethod4506<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1555`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1555`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1555`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1555`1<!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1555.A<(class G3_C1555`1<class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 19
		.locals init (string[] actualResults)
		ldc.i4.s    14
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1555.A<(class G3_C1555`1<class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 14
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1555`1<class BaseClass0>::ClassMethod1340()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1555`1<class BaseClass0>::ClassMethod1341()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1555`1<class BaseClass0>::ClassMethod1342<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1555`1<class BaseClass0>::ClassMethod1343<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1555`1<class BaseClass0>::ClassMethod2500()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1555`1<class BaseClass0>::ClassMethod2501()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1555`1<class BaseClass0>::ClassMethod2502<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1555`1<class BaseClass0>::ClassMethod4504()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1555`1<class BaseClass0>::ClassMethod4505<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1555`1<class BaseClass0>::ClassMethod4506<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1555`1<class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1555`1<class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1555`1<class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1555`1<class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1555.B<(class G3_C1555`1<class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 19
		.locals init (string[] actualResults)
		ldc.i4.s    14
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1555.B<(class G3_C1555`1<class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 14
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1555`1<class BaseClass1>::ClassMethod1340()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1555`1<class BaseClass1>::ClassMethod1341()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1555`1<class BaseClass1>::ClassMethod1342<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1555`1<class BaseClass1>::ClassMethod1343<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1555`1<class BaseClass1>::ClassMethod2500()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1555`1<class BaseClass1>::ClassMethod2501()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1555`1<class BaseClass1>::ClassMethod2502<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1555`1<class BaseClass1>::ClassMethod4504()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1555`1<class BaseClass1>::ClassMethod4505<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1555`1<class BaseClass1>::ClassMethod4506<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1555`1<class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1555`1<class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1555`1<class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1555`1<class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C564.T.T<T0,T1,(class G2_C564`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C564.T.T<T0,T1,(class G2_C564`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C564`2<!!T0,!!T1>::ClassMethod1340()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C564`2<!!T0,!!T1>::ClassMethod1341()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C564`2<!!T0,!!T1>::ClassMethod1342<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C564`2<!!T0,!!T1>::ClassMethod1343<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C564`2<!!T0,!!T1>::ClassMethod2500()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C564`2<!!T0,!!T1>::ClassMethod2501()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C564`2<!!T0,!!T1>::ClassMethod2502<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C564`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C564`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C564`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C564`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C564.A.T<T1,(class G2_C564`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C564.A.T<T1,(class G2_C564`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C564`2<class BaseClass0,!!T1>::ClassMethod1340()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C564`2<class BaseClass0,!!T1>::ClassMethod1341()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C564`2<class BaseClass0,!!T1>::ClassMethod1342<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C564`2<class BaseClass0,!!T1>::ClassMethod1343<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C564`2<class BaseClass0,!!T1>::ClassMethod2500()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C564`2<class BaseClass0,!!T1>::ClassMethod2501()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C564`2<class BaseClass0,!!T1>::ClassMethod2502<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C564`2<class BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C564`2<class BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C564`2<class BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C564`2<class BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C564.A.A<(class G2_C564`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C564.A.A<(class G2_C564`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C564`2<class BaseClass0,class BaseClass0>::ClassMethod1340()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C564`2<class BaseClass0,class BaseClass0>::ClassMethod1341()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C564`2<class BaseClass0,class BaseClass0>::ClassMethod1342<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C564`2<class BaseClass0,class BaseClass0>::ClassMethod1343<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C564`2<class BaseClass0,class BaseClass0>::ClassMethod2500()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C564`2<class BaseClass0,class BaseClass0>::ClassMethod2501()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C564`2<class BaseClass0,class BaseClass0>::ClassMethod2502<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C564`2<class BaseClass0,class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C564`2<class BaseClass0,class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C564`2<class BaseClass0,class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C564`2<class BaseClass0,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C564.A.B<(class G2_C564`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C564.A.B<(class G2_C564`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C564`2<class BaseClass0,class BaseClass1>::ClassMethod1340()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C564`2<class BaseClass0,class BaseClass1>::ClassMethod1341()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C564`2<class BaseClass0,class BaseClass1>::ClassMethod1342<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C564`2<class BaseClass0,class BaseClass1>::ClassMethod1343<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C564`2<class BaseClass0,class BaseClass1>::ClassMethod2500()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C564`2<class BaseClass0,class BaseClass1>::ClassMethod2501()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C564`2<class BaseClass0,class BaseClass1>::ClassMethod2502<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C564`2<class BaseClass0,class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C564`2<class BaseClass0,class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C564`2<class BaseClass0,class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C564`2<class BaseClass0,class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C564.B.T<T1,(class G2_C564`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C564.B.T<T1,(class G2_C564`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C564`2<class BaseClass1,!!T1>::ClassMethod1340()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C564`2<class BaseClass1,!!T1>::ClassMethod1341()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C564`2<class BaseClass1,!!T1>::ClassMethod1342<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C564`2<class BaseClass1,!!T1>::ClassMethod1343<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C564`2<class BaseClass1,!!T1>::ClassMethod2500()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C564`2<class BaseClass1,!!T1>::ClassMethod2501()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C564`2<class BaseClass1,!!T1>::ClassMethod2502<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C564`2<class BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C564`2<class BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C564`2<class BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C564`2<class BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C564.B.A<(class G2_C564`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C564.B.A<(class G2_C564`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C564`2<class BaseClass1,class BaseClass0>::ClassMethod1340()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C564`2<class BaseClass1,class BaseClass0>::ClassMethod1341()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C564`2<class BaseClass1,class BaseClass0>::ClassMethod1342<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C564`2<class BaseClass1,class BaseClass0>::ClassMethod1343<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C564`2<class BaseClass1,class BaseClass0>::ClassMethod2500()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C564`2<class BaseClass1,class BaseClass0>::ClassMethod2501()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C564`2<class BaseClass1,class BaseClass0>::ClassMethod2502<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C564`2<class BaseClass1,class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C564`2<class BaseClass1,class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C564`2<class BaseClass1,class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C564`2<class BaseClass1,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C564.B.B<(class G2_C564`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C564.B.B<(class G2_C564`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C564`2<class BaseClass1,class BaseClass1>::ClassMethod1340()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C564`2<class BaseClass1,class BaseClass1>::ClassMethod1341()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C564`2<class BaseClass1,class BaseClass1>::ClassMethod1342<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C564`2<class BaseClass1,class BaseClass1>::ClassMethod1343<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C564`2<class BaseClass1,class BaseClass1>::ClassMethod2500()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C564`2<class BaseClass1,class BaseClass1>::ClassMethod2501()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C564`2<class BaseClass1,class BaseClass1>::ClassMethod2502<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C564`2<class BaseClass1,class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C564`2<class BaseClass1,class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C564`2<class BaseClass1,class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C564`2<class BaseClass1,class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.T.T<T0,T1,(class IBase2`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.T.T<T0,T1,(class IBase2`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class IBase2`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.T<T1,(class IBase2`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.T<T1,(class IBase2`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class IBase2`2<class BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.A<(class IBase2`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.A<(class IBase2`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.B<(class IBase2`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.B<(class IBase2`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.T<T1,(class IBase2`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.T<T1,(class IBase2`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class IBase2`2<class BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.A<(class IBase2`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.A<(class IBase2`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.B<(class IBase2`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.B<(class IBase2`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C10.T.T<T0,T1,(class G1_C10`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C10.T.T<T0,T1,(class G1_C10`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<!!T0,!!T1>::ClassMethod1340()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<!!T0,!!T1>::ClassMethod1341()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<!!T0,!!T1>::ClassMethod1342<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<!!T0,!!T1>::ClassMethod1343<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C10.A.T<T1,(class G1_C10`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C10.A.T<T1,(class G1_C10`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass0,!!T1>::ClassMethod1340()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass0,!!T1>::ClassMethod1341()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass0,!!T1>::ClassMethod1342<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass0,!!T1>::ClassMethod1343<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C10.A.A<(class G1_C10`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C10.A.A<(class G1_C10`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass0,class BaseClass0>::ClassMethod1340()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass0,class BaseClass0>::ClassMethod1341()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass0,class BaseClass0>::ClassMethod1342<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass0,class BaseClass0>::ClassMethod1343<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass0,class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass0,class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass0,class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass0,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C10.A.B<(class G1_C10`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C10.A.B<(class G1_C10`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass0,class BaseClass1>::ClassMethod1340()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass0,class BaseClass1>::ClassMethod1341()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass0,class BaseClass1>::ClassMethod1342<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass0,class BaseClass1>::ClassMethod1343<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass0,class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass0,class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass0,class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass0,class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C10.B.T<T1,(class G1_C10`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C10.B.T<T1,(class G1_C10`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass1,!!T1>::ClassMethod1340()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass1,!!T1>::ClassMethod1341()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass1,!!T1>::ClassMethod1342<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass1,!!T1>::ClassMethod1343<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C10.B.A<(class G1_C10`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C10.B.A<(class G1_C10`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass1,class BaseClass0>::ClassMethod1340()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass1,class BaseClass0>::ClassMethod1341()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass1,class BaseClass0>::ClassMethod1342<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass1,class BaseClass0>::ClassMethod1343<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass1,class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass1,class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass1,class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass1,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C10.B.B<(class G1_C10`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C10.B.B<(class G1_C10`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass1,class BaseClass1>::ClassMethod1340()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass1,class BaseClass1>::ClassMethod1341()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass1,class BaseClass1>::ClassMethod1342<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass1,class BaseClass1>::ClassMethod1343<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass1,class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass1,class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass1,class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass1,class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.T<T0,(class IBase1`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.T<T0,(class IBase1`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class IBase1`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class IBase1`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class IBase1`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.A<(class IBase1`1<class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.A<(class IBase1`1<class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class IBase1`1<class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class IBase1`1<class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class IBase1`1<class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.B<(class IBase1`1<class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.B<(class IBase1`1<class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class IBase1`1<class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class IBase1`1<class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class IBase1`1<class BaseClass1>::Method6<object>()
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
		newobj instance void class G3_C1555`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass1,class BaseClass1>::ClassMethod1343<object>()
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G3_C1555`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass1,class BaseClass1>::ClassMethod1342<object>()
		ldstr "G2_C564::ClassMethod1342.MI.10204<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G3_C1555`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass1,class BaseClass1>::ClassMethod1341()
		ldstr "G2_C564::ClassMethod1341.MI.10203()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G3_C1555`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass1,class BaseClass1>::ClassMethod1340()
		ldstr "G2_C564::ClassMethod1340.MI.10202()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G3_C1555`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass1,class BaseClass1>::Method6<object>()
		ldstr "G1_C10::Method6.4848<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G3_C1555`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass1,class BaseClass1>::Method5()
		ldstr "G1_C10::Method5.4847()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G3_C1555`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass1,class BaseClass1>::Method4()
		ldstr "G2_C564::Method4.10194()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G3_C1555`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G3_C1555`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G3_C1555::Method7.16781<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1555`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G2_C564::Method4.10194()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1555`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G2_C564::Method5.MI.10196()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1555`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C564::Method6.MI.10198<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1555`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G3_C1555::Method7.16781<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1555`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C564::Method4.10194()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1555`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C564::Method5.MI.10196()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1555`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C564::Method6.MI.10198<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1555`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C564`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C564`2<class BaseClass0,class BaseClass1>::ClassMethod2502<object>()
		ldstr "G2_C564::ClassMethod2502.10201<System.Object>()"
		ldstr "class G2_C564`2<class BaseClass0,class BaseClass1> on type class G3_C1555`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C564`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C564`2<class BaseClass0,class BaseClass1>::ClassMethod2501()
		ldstr "G2_C564::ClassMethod2501.10200()"
		ldstr "class G2_C564`2<class BaseClass0,class BaseClass1> on type class G3_C1555`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C564`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C564`2<class BaseClass0,class BaseClass1>::ClassMethod2500()
		ldstr "G3_C1555::ClassMethod2500.MI.16785()"
		ldstr "class G2_C564`2<class BaseClass0,class BaseClass1> on type class G3_C1555`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C564`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C564`2<class BaseClass0,class BaseClass1>::Method6<object>()
		ldstr "G2_C564::Method6.10197<System.Object>()"
		ldstr "class G2_C564`2<class BaseClass0,class BaseClass1> on type class G3_C1555`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C564`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C564`2<class BaseClass0,class BaseClass1>::Method5()
		ldstr "G2_C564::Method5.10195()"
		ldstr "class G2_C564`2<class BaseClass0,class BaseClass1> on type class G3_C1555`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C564`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C564`2<class BaseClass0,class BaseClass1>::Method4()
		ldstr "G2_C564::Method4.10194()"
		ldstr "class G2_C564`2<class BaseClass0,class BaseClass1> on type class G3_C1555`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C564`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C564`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G3_C1555::Method7.16781<System.Object>()"
		ldstr "class G2_C564`2<class BaseClass0,class BaseClass1> on type class G3_C1555`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C564`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C564`2<class BaseClass0,class BaseClass1>::ClassMethod1343<object>()
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G2_C564`2<class BaseClass0,class BaseClass1> on type class G3_C1555`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C564`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C564`2<class BaseClass0,class BaseClass1>::ClassMethod1342<object>()
		ldstr "G2_C564::ClassMethod1342.MI.10204<System.Object>()"
		ldstr "class G2_C564`2<class BaseClass0,class BaseClass1> on type class G3_C1555`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C564`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C564`2<class BaseClass0,class BaseClass1>::ClassMethod1341()
		ldstr "G2_C564::ClassMethod1341.MI.10203()"
		ldstr "class G2_C564`2<class BaseClass0,class BaseClass1> on type class G3_C1555`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C564`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C564`2<class BaseClass0,class BaseClass1>::ClassMethod1340()
		ldstr "G2_C564::ClassMethod1340.MI.10202()"
		ldstr "class G2_C564`2<class BaseClass0,class BaseClass1> on type class G3_C1555`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G3_C1555`1<class BaseClass0>
		callvirt instance string class G3_C1555`1<class BaseClass0>::ClassMethod4506<object>()
		ldstr "G3_C1555::ClassMethod4506.16784<System.Object>()"
		ldstr "class G3_C1555`1<class BaseClass0> on type class G3_C1555`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1555`1<class BaseClass0>
		callvirt instance string class G3_C1555`1<class BaseClass0>::ClassMethod4505<object>()
		ldstr "G3_C1555::ClassMethod4505.16783<System.Object>()"
		ldstr "class G3_C1555`1<class BaseClass0> on type class G3_C1555`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1555`1<class BaseClass0>
		callvirt instance string class G3_C1555`1<class BaseClass0>::ClassMethod4504()
		ldstr "G3_C1555::ClassMethod4504.16782()"
		ldstr "class G3_C1555`1<class BaseClass0> on type class G3_C1555`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1555`1<class BaseClass0>
		callvirt instance string class G3_C1555`1<class BaseClass0>::Method7<object>()
		ldstr "G3_C1555::Method7.16781<System.Object>()"
		ldstr "class G3_C1555`1<class BaseClass0> on type class G3_C1555`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1555`1<class BaseClass0>
		callvirt instance string class G3_C1555`1<class BaseClass0>::ClassMethod2502<object>()
		ldstr "G2_C564::ClassMethod2502.10201<System.Object>()"
		ldstr "class G3_C1555`1<class BaseClass0> on type class G3_C1555`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1555`1<class BaseClass0>
		callvirt instance string class G3_C1555`1<class BaseClass0>::ClassMethod2501()
		ldstr "G2_C564::ClassMethod2501.10200()"
		ldstr "class G3_C1555`1<class BaseClass0> on type class G3_C1555`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1555`1<class BaseClass0>
		callvirt instance string class G3_C1555`1<class BaseClass0>::ClassMethod2500()
		ldstr "G3_C1555::ClassMethod2500.MI.16785()"
		ldstr "class G3_C1555`1<class BaseClass0> on type class G3_C1555`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1555`1<class BaseClass0>
		callvirt instance string class G3_C1555`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C564::Method6.10197<System.Object>()"
		ldstr "class G3_C1555`1<class BaseClass0> on type class G3_C1555`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1555`1<class BaseClass0>
		callvirt instance string class G3_C1555`1<class BaseClass0>::Method5()
		ldstr "G2_C564::Method5.10195()"
		ldstr "class G3_C1555`1<class BaseClass0> on type class G3_C1555`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1555`1<class BaseClass0>
		callvirt instance string class G3_C1555`1<class BaseClass0>::Method4()
		ldstr "G2_C564::Method4.10194()"
		ldstr "class G3_C1555`1<class BaseClass0> on type class G3_C1555`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1555`1<class BaseClass0>
		callvirt instance string class G3_C1555`1<class BaseClass0>::ClassMethod1343<object>()
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G3_C1555`1<class BaseClass0> on type class G3_C1555`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1555`1<class BaseClass0>
		callvirt instance string class G3_C1555`1<class BaseClass0>::ClassMethod1342<object>()
		ldstr "G2_C564::ClassMethod1342.MI.10204<System.Object>()"
		ldstr "class G3_C1555`1<class BaseClass0> on type class G3_C1555`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1555`1<class BaseClass0>
		callvirt instance string class G3_C1555`1<class BaseClass0>::ClassMethod1341()
		ldstr "G2_C564::ClassMethod1341.MI.10203()"
		ldstr "class G3_C1555`1<class BaseClass0> on type class G3_C1555`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1555`1<class BaseClass0>
		callvirt instance string class G3_C1555`1<class BaseClass0>::ClassMethod1340()
		ldstr "G2_C564::ClassMethod1340.MI.10202()"
		ldstr "class G3_C1555`1<class BaseClass0> on type class G3_C1555`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G3_C1555::Method7.16781<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G3_C1555`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G3_C1555::Method7.16781<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1555`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G3_C1555`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass1,class BaseClass1>::ClassMethod1343<object>()
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G3_C1555`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass1,class BaseClass1>::ClassMethod1342<object>()
		ldstr "G2_C564::ClassMethod1342.MI.10204<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G3_C1555`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass1,class BaseClass1>::ClassMethod1341()
		ldstr "G2_C564::ClassMethod1341.MI.10203()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G3_C1555`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass1,class BaseClass1>::ClassMethod1340()
		ldstr "G2_C564::ClassMethod1340.MI.10202()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G3_C1555`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass1,class BaseClass1>::Method6<object>()
		ldstr "G1_C10::Method6.4848<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G3_C1555`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass1,class BaseClass1>::Method5()
		ldstr "G1_C10::Method5.4847()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G3_C1555`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass1,class BaseClass1>::Method4()
		ldstr "G2_C564::Method4.10194()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G3_C1555`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G3_C1555`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G3_C1555::Method7.16781<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1555`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G2_C564::Method4.10194()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1555`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G2_C564::Method5.MI.10196()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1555`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C564::Method6.MI.10198<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1555`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G3_C1555::Method7.16781<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1555`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C564::Method4.10194()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1555`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C564::Method5.MI.10196()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1555`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C564::Method6.MI.10198<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1555`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C564`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C564`2<class BaseClass0,class BaseClass1>::ClassMethod2502<object>()
		ldstr "G2_C564::ClassMethod2502.10201<System.Object>()"
		ldstr "class G2_C564`2<class BaseClass0,class BaseClass1> on type class G3_C1555`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C564`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C564`2<class BaseClass0,class BaseClass1>::ClassMethod2501()
		ldstr "G2_C564::ClassMethod2501.10200()"
		ldstr "class G2_C564`2<class BaseClass0,class BaseClass1> on type class G3_C1555`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C564`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C564`2<class BaseClass0,class BaseClass1>::ClassMethod2500()
		ldstr "G3_C1555::ClassMethod2500.MI.16785()"
		ldstr "class G2_C564`2<class BaseClass0,class BaseClass1> on type class G3_C1555`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C564`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C564`2<class BaseClass0,class BaseClass1>::Method6<object>()
		ldstr "G2_C564::Method6.10197<System.Object>()"
		ldstr "class G2_C564`2<class BaseClass0,class BaseClass1> on type class G3_C1555`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C564`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C564`2<class BaseClass0,class BaseClass1>::Method5()
		ldstr "G2_C564::Method5.10195()"
		ldstr "class G2_C564`2<class BaseClass0,class BaseClass1> on type class G3_C1555`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C564`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C564`2<class BaseClass0,class BaseClass1>::Method4()
		ldstr "G2_C564::Method4.10194()"
		ldstr "class G2_C564`2<class BaseClass0,class BaseClass1> on type class G3_C1555`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C564`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C564`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G3_C1555::Method7.16781<System.Object>()"
		ldstr "class G2_C564`2<class BaseClass0,class BaseClass1> on type class G3_C1555`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C564`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C564`2<class BaseClass0,class BaseClass1>::ClassMethod1343<object>()
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G2_C564`2<class BaseClass0,class BaseClass1> on type class G3_C1555`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C564`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C564`2<class BaseClass0,class BaseClass1>::ClassMethod1342<object>()
		ldstr "G2_C564::ClassMethod1342.MI.10204<System.Object>()"
		ldstr "class G2_C564`2<class BaseClass0,class BaseClass1> on type class G3_C1555`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C564`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C564`2<class BaseClass0,class BaseClass1>::ClassMethod1341()
		ldstr "G2_C564::ClassMethod1341.MI.10203()"
		ldstr "class G2_C564`2<class BaseClass0,class BaseClass1> on type class G3_C1555`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C564`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C564`2<class BaseClass0,class BaseClass1>::ClassMethod1340()
		ldstr "G2_C564::ClassMethod1340.MI.10202()"
		ldstr "class G2_C564`2<class BaseClass0,class BaseClass1> on type class G3_C1555`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G3_C1555`1<class BaseClass1>
		callvirt instance string class G3_C1555`1<class BaseClass1>::ClassMethod4506<object>()
		ldstr "G3_C1555::ClassMethod4506.16784<System.Object>()"
		ldstr "class G3_C1555`1<class BaseClass1> on type class G3_C1555`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1555`1<class BaseClass1>
		callvirt instance string class G3_C1555`1<class BaseClass1>::ClassMethod4505<object>()
		ldstr "G3_C1555::ClassMethod4505.16783<System.Object>()"
		ldstr "class G3_C1555`1<class BaseClass1> on type class G3_C1555`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1555`1<class BaseClass1>
		callvirt instance string class G3_C1555`1<class BaseClass1>::ClassMethod4504()
		ldstr "G3_C1555::ClassMethod4504.16782()"
		ldstr "class G3_C1555`1<class BaseClass1> on type class G3_C1555`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1555`1<class BaseClass1>
		callvirt instance string class G3_C1555`1<class BaseClass1>::Method7<object>()
		ldstr "G3_C1555::Method7.16781<System.Object>()"
		ldstr "class G3_C1555`1<class BaseClass1> on type class G3_C1555`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1555`1<class BaseClass1>
		callvirt instance string class G3_C1555`1<class BaseClass1>::ClassMethod2502<object>()
		ldstr "G2_C564::ClassMethod2502.10201<System.Object>()"
		ldstr "class G3_C1555`1<class BaseClass1> on type class G3_C1555`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1555`1<class BaseClass1>
		callvirt instance string class G3_C1555`1<class BaseClass1>::ClassMethod2501()
		ldstr "G2_C564::ClassMethod2501.10200()"
		ldstr "class G3_C1555`1<class BaseClass1> on type class G3_C1555`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1555`1<class BaseClass1>
		callvirt instance string class G3_C1555`1<class BaseClass1>::ClassMethod2500()
		ldstr "G3_C1555::ClassMethod2500.MI.16785()"
		ldstr "class G3_C1555`1<class BaseClass1> on type class G3_C1555`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1555`1<class BaseClass1>
		callvirt instance string class G3_C1555`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C564::Method6.10197<System.Object>()"
		ldstr "class G3_C1555`1<class BaseClass1> on type class G3_C1555`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1555`1<class BaseClass1>
		callvirt instance string class G3_C1555`1<class BaseClass1>::Method5()
		ldstr "G2_C564::Method5.10195()"
		ldstr "class G3_C1555`1<class BaseClass1> on type class G3_C1555`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1555`1<class BaseClass1>
		callvirt instance string class G3_C1555`1<class BaseClass1>::Method4()
		ldstr "G2_C564::Method4.10194()"
		ldstr "class G3_C1555`1<class BaseClass1> on type class G3_C1555`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1555`1<class BaseClass1>
		callvirt instance string class G3_C1555`1<class BaseClass1>::ClassMethod1343<object>()
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G3_C1555`1<class BaseClass1> on type class G3_C1555`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1555`1<class BaseClass1>
		callvirt instance string class G3_C1555`1<class BaseClass1>::ClassMethod1342<object>()
		ldstr "G2_C564::ClassMethod1342.MI.10204<System.Object>()"
		ldstr "class G3_C1555`1<class BaseClass1> on type class G3_C1555`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1555`1<class BaseClass1>
		callvirt instance string class G3_C1555`1<class BaseClass1>::ClassMethod1341()
		ldstr "G2_C564::ClassMethod1341.MI.10203()"
		ldstr "class G3_C1555`1<class BaseClass1> on type class G3_C1555`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1555`1<class BaseClass1>
		callvirt instance string class G3_C1555`1<class BaseClass1>::ClassMethod1340()
		ldstr "G2_C564::ClassMethod1340.MI.10202()"
		ldstr "class G3_C1555`1<class BaseClass1> on type class G3_C1555`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C564`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C10`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C10`2<class BaseClass0,class BaseClass0>::ClassMethod1343<object>()
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass0> on type class G2_C564`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C10`2<class BaseClass0,class BaseClass0>::ClassMethod1342<object>()
		ldstr "G2_C564::ClassMethod1342.MI.10204<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass0> on type class G2_C564`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C10`2<class BaseClass0,class BaseClass0>::ClassMethod1341()
		ldstr "G2_C564::ClassMethod1341.MI.10203()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass0> on type class G2_C564`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C10`2<class BaseClass0,class BaseClass0>::ClassMethod1340()
		ldstr "G2_C564::ClassMethod1340.MI.10202()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass0> on type class G2_C564`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C10`2<class BaseClass0,class BaseClass0>::Method6<object>()
		ldstr "G1_C10::Method6.4848<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass0> on type class G2_C564`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C10`2<class BaseClass0,class BaseClass0>::Method5()
		ldstr "G1_C10::Method5.4847()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass0> on type class G2_C564`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C10`2<class BaseClass0,class BaseClass0>::Method4()
		ldstr "G2_C564::Method4.10194()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass0> on type class G2_C564`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C10`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass0> on type class G2_C564`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C564`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C564::Method4.10194()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C564`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C564::Method5.MI.10196()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C564`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C564::Method6.MI.10198<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C564`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C564::Method7.10193<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C564`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C564`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C564`2<class BaseClass0,class BaseClass0>::ClassMethod2502<object>()
		ldstr "G2_C564::ClassMethod2502.10201<System.Object>()"
		ldstr "class G2_C564`2<class BaseClass0,class BaseClass0> on type class G2_C564`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C564`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C564`2<class BaseClass0,class BaseClass0>::ClassMethod2501()
		ldstr "G2_C564::ClassMethod2501.10200()"
		ldstr "class G2_C564`2<class BaseClass0,class BaseClass0> on type class G2_C564`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C564`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C564`2<class BaseClass0,class BaseClass0>::ClassMethod2500()
		ldstr "G2_C564::ClassMethod2500.10199()"
		ldstr "class G2_C564`2<class BaseClass0,class BaseClass0> on type class G2_C564`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C564`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C564`2<class BaseClass0,class BaseClass0>::Method6<object>()
		ldstr "G2_C564::Method6.10197<System.Object>()"
		ldstr "class G2_C564`2<class BaseClass0,class BaseClass0> on type class G2_C564`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C564`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C564`2<class BaseClass0,class BaseClass0>::Method5()
		ldstr "G2_C564::Method5.10195()"
		ldstr "class G2_C564`2<class BaseClass0,class BaseClass0> on type class G2_C564`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C564`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C564`2<class BaseClass0,class BaseClass0>::Method4()
		ldstr "G2_C564::Method4.10194()"
		ldstr "class G2_C564`2<class BaseClass0,class BaseClass0> on type class G2_C564`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C564`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C564`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C564::Method7.10193<System.Object>()"
		ldstr "class G2_C564`2<class BaseClass0,class BaseClass0> on type class G2_C564`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C564`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C564`2<class BaseClass0,class BaseClass0>::ClassMethod1343<object>()
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G2_C564`2<class BaseClass0,class BaseClass0> on type class G2_C564`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C564`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C564`2<class BaseClass0,class BaseClass0>::ClassMethod1342<object>()
		ldstr "G2_C564::ClassMethod1342.MI.10204<System.Object>()"
		ldstr "class G2_C564`2<class BaseClass0,class BaseClass0> on type class G2_C564`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C564`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C564`2<class BaseClass0,class BaseClass0>::ClassMethod1341()
		ldstr "G2_C564::ClassMethod1341.MI.10203()"
		ldstr "class G2_C564`2<class BaseClass0,class BaseClass0> on type class G2_C564`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C564`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C564`2<class BaseClass0,class BaseClass0>::ClassMethod1340()
		ldstr "G2_C564::ClassMethod1340.MI.10202()"
		ldstr "class G2_C564`2<class BaseClass0,class BaseClass0> on type class G2_C564`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C564::Method7.10193<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C564`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C564`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass1,class BaseClass1>::ClassMethod1343<object>()
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G2_C564`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass1,class BaseClass1>::ClassMethod1342<object>()
		ldstr "G2_C564::ClassMethod1342.MI.10204<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G2_C564`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass1,class BaseClass1>::ClassMethod1341()
		ldstr "G2_C564::ClassMethod1341.MI.10203()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G2_C564`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass1,class BaseClass1>::ClassMethod1340()
		ldstr "G2_C564::ClassMethod1340.MI.10202()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G2_C564`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass1,class BaseClass1>::Method6<object>()
		ldstr "G1_C10::Method6.4848<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G2_C564`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass1,class BaseClass1>::Method5()
		ldstr "G1_C10::Method5.4847()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G2_C564`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass1,class BaseClass1>::Method4()
		ldstr "G2_C564::Method4.10194()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G2_C564`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G2_C564`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C564`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G2_C564::Method4.10194()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C564`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G2_C564::Method5.MI.10196()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C564`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C564::Method6.MI.10198<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C564`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C564::Method7.10193<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C564`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C564::Method4.10194()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C564`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C564::Method5.MI.10196()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C564`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C564::Method6.MI.10198<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C564`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C564`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C564`2<class BaseClass0,class BaseClass1>::ClassMethod2502<object>()
		ldstr "G2_C564::ClassMethod2502.10201<System.Object>()"
		ldstr "class G2_C564`2<class BaseClass0,class BaseClass1> on type class G2_C564`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C564`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C564`2<class BaseClass0,class BaseClass1>::ClassMethod2501()
		ldstr "G2_C564::ClassMethod2501.10200()"
		ldstr "class G2_C564`2<class BaseClass0,class BaseClass1> on type class G2_C564`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C564`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C564`2<class BaseClass0,class BaseClass1>::ClassMethod2500()
		ldstr "G2_C564::ClassMethod2500.10199()"
		ldstr "class G2_C564`2<class BaseClass0,class BaseClass1> on type class G2_C564`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C564`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C564`2<class BaseClass0,class BaseClass1>::Method6<object>()
		ldstr "G2_C564::Method6.10197<System.Object>()"
		ldstr "class G2_C564`2<class BaseClass0,class BaseClass1> on type class G2_C564`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C564`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C564`2<class BaseClass0,class BaseClass1>::Method5()
		ldstr "G2_C564::Method5.10195()"
		ldstr "class G2_C564`2<class BaseClass0,class BaseClass1> on type class G2_C564`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C564`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C564`2<class BaseClass0,class BaseClass1>::Method4()
		ldstr "G2_C564::Method4.10194()"
		ldstr "class G2_C564`2<class BaseClass0,class BaseClass1> on type class G2_C564`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C564`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C564`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C564::Method7.10193<System.Object>()"
		ldstr "class G2_C564`2<class BaseClass0,class BaseClass1> on type class G2_C564`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C564`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C564`2<class BaseClass0,class BaseClass1>::ClassMethod1343<object>()
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G2_C564`2<class BaseClass0,class BaseClass1> on type class G2_C564`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C564`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C564`2<class BaseClass0,class BaseClass1>::ClassMethod1342<object>()
		ldstr "G2_C564::ClassMethod1342.MI.10204<System.Object>()"
		ldstr "class G2_C564`2<class BaseClass0,class BaseClass1> on type class G2_C564`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C564`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C564`2<class BaseClass0,class BaseClass1>::ClassMethod1341()
		ldstr "G2_C564::ClassMethod1341.MI.10203()"
		ldstr "class G2_C564`2<class BaseClass0,class BaseClass1> on type class G2_C564`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C564`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C564`2<class BaseClass0,class BaseClass1>::ClassMethod1340()
		ldstr "G2_C564::ClassMethod1340.MI.10202()"
		ldstr "class G2_C564`2<class BaseClass0,class BaseClass1> on type class G2_C564`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C564`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C10`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C10`2<class BaseClass0,class BaseClass0>::ClassMethod1343<object>()
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass0> on type class G2_C564`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C10`2<class BaseClass0,class BaseClass0>::ClassMethod1342<object>()
		ldstr "G2_C564::ClassMethod1342.MI.10204<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass0> on type class G2_C564`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C10`2<class BaseClass0,class BaseClass0>::ClassMethod1341()
		ldstr "G2_C564::ClassMethod1341.MI.10203()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass0> on type class G2_C564`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C10`2<class BaseClass0,class BaseClass0>::ClassMethod1340()
		ldstr "G2_C564::ClassMethod1340.MI.10202()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass0> on type class G2_C564`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C10`2<class BaseClass0,class BaseClass0>::Method6<object>()
		ldstr "G1_C10::Method6.4848<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass0> on type class G2_C564`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C10`2<class BaseClass0,class BaseClass0>::Method5()
		ldstr "G1_C10::Method5.4847()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass0> on type class G2_C564`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C10`2<class BaseClass0,class BaseClass0>::Method4()
		ldstr "G2_C564::Method4.10194()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass0> on type class G2_C564`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C10`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass0> on type class G2_C564`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C564`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C564::Method4.10194()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C564`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C564::Method5.MI.10196()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C564`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C564::Method6.MI.10198<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C564`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C564::Method7.10193<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C564`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C564`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C564`2<class BaseClass1,class BaseClass0>::ClassMethod2502<object>()
		ldstr "G2_C564::ClassMethod2502.10201<System.Object>()"
		ldstr "class G2_C564`2<class BaseClass1,class BaseClass0> on type class G2_C564`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C564`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C564`2<class BaseClass1,class BaseClass0>::ClassMethod2501()
		ldstr "G2_C564::ClassMethod2501.10200()"
		ldstr "class G2_C564`2<class BaseClass1,class BaseClass0> on type class G2_C564`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C564`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C564`2<class BaseClass1,class BaseClass0>::ClassMethod2500()
		ldstr "G2_C564::ClassMethod2500.10199()"
		ldstr "class G2_C564`2<class BaseClass1,class BaseClass0> on type class G2_C564`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C564`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C564`2<class BaseClass1,class BaseClass0>::Method6<object>()
		ldstr "G2_C564::Method6.10197<System.Object>()"
		ldstr "class G2_C564`2<class BaseClass1,class BaseClass0> on type class G2_C564`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C564`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C564`2<class BaseClass1,class BaseClass0>::Method5()
		ldstr "G2_C564::Method5.10195()"
		ldstr "class G2_C564`2<class BaseClass1,class BaseClass0> on type class G2_C564`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C564`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C564`2<class BaseClass1,class BaseClass0>::Method4()
		ldstr "G2_C564::Method4.10194()"
		ldstr "class G2_C564`2<class BaseClass1,class BaseClass0> on type class G2_C564`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C564`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C564`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G2_C564::Method7.10193<System.Object>()"
		ldstr "class G2_C564`2<class BaseClass1,class BaseClass0> on type class G2_C564`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C564`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C564`2<class BaseClass1,class BaseClass0>::ClassMethod1343<object>()
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G2_C564`2<class BaseClass1,class BaseClass0> on type class G2_C564`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C564`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C564`2<class BaseClass1,class BaseClass0>::ClassMethod1342<object>()
		ldstr "G2_C564::ClassMethod1342.MI.10204<System.Object>()"
		ldstr "class G2_C564`2<class BaseClass1,class BaseClass0> on type class G2_C564`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C564`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C564`2<class BaseClass1,class BaseClass0>::ClassMethod1341()
		ldstr "G2_C564::ClassMethod1341.MI.10203()"
		ldstr "class G2_C564`2<class BaseClass1,class BaseClass0> on type class G2_C564`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C564`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C564`2<class BaseClass1,class BaseClass0>::ClassMethod1340()
		ldstr "G2_C564::ClassMethod1340.MI.10202()"
		ldstr "class G2_C564`2<class BaseClass1,class BaseClass0> on type class G2_C564`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C564::Method7.10193<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C564`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C564`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass1,class BaseClass1>::ClassMethod1343<object>()
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G2_C564`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass1,class BaseClass1>::ClassMethod1342<object>()
		ldstr "G2_C564::ClassMethod1342.MI.10204<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G2_C564`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass1,class BaseClass1>::ClassMethod1341()
		ldstr "G2_C564::ClassMethod1341.MI.10203()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G2_C564`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass1,class BaseClass1>::ClassMethod1340()
		ldstr "G2_C564::ClassMethod1340.MI.10202()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G2_C564`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass1,class BaseClass1>::Method6<object>()
		ldstr "G1_C10::Method6.4848<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G2_C564`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass1,class BaseClass1>::Method5()
		ldstr "G1_C10::Method5.4847()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G2_C564`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass1,class BaseClass1>::Method4()
		ldstr "G2_C564::Method4.10194()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G2_C564`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G2_C564`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C564`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G2_C564::Method4.10194()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C564`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G2_C564::Method5.MI.10196()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C564`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C564::Method6.MI.10198<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C564`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C564::Method7.10193<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C564`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C564::Method4.10194()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C564`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C564::Method5.MI.10196()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C564`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C564::Method6.MI.10198<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C564`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C564`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C564`2<class BaseClass1,class BaseClass1>::ClassMethod2502<object>()
		ldstr "G2_C564::ClassMethod2502.10201<System.Object>()"
		ldstr "class G2_C564`2<class BaseClass1,class BaseClass1> on type class G2_C564`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C564`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C564`2<class BaseClass1,class BaseClass1>::ClassMethod2501()
		ldstr "G2_C564::ClassMethod2501.10200()"
		ldstr "class G2_C564`2<class BaseClass1,class BaseClass1> on type class G2_C564`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C564`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C564`2<class BaseClass1,class BaseClass1>::ClassMethod2500()
		ldstr "G2_C564::ClassMethod2500.10199()"
		ldstr "class G2_C564`2<class BaseClass1,class BaseClass1> on type class G2_C564`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C564`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C564`2<class BaseClass1,class BaseClass1>::Method6<object>()
		ldstr "G2_C564::Method6.10197<System.Object>()"
		ldstr "class G2_C564`2<class BaseClass1,class BaseClass1> on type class G2_C564`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C564`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C564`2<class BaseClass1,class BaseClass1>::Method5()
		ldstr "G2_C564::Method5.10195()"
		ldstr "class G2_C564`2<class BaseClass1,class BaseClass1> on type class G2_C564`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C564`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C564`2<class BaseClass1,class BaseClass1>::Method4()
		ldstr "G2_C564::Method4.10194()"
		ldstr "class G2_C564`2<class BaseClass1,class BaseClass1> on type class G2_C564`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C564`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C564`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C564::Method7.10193<System.Object>()"
		ldstr "class G2_C564`2<class BaseClass1,class BaseClass1> on type class G2_C564`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C564`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C564`2<class BaseClass1,class BaseClass1>::ClassMethod1343<object>()
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G2_C564`2<class BaseClass1,class BaseClass1> on type class G2_C564`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C564`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C564`2<class BaseClass1,class BaseClass1>::ClassMethod1342<object>()
		ldstr "G2_C564::ClassMethod1342.MI.10204<System.Object>()"
		ldstr "class G2_C564`2<class BaseClass1,class BaseClass1> on type class G2_C564`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C564`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C564`2<class BaseClass1,class BaseClass1>::ClassMethod1341()
		ldstr "G2_C564::ClassMethod1341.MI.10203()"
		ldstr "class G2_C564`2<class BaseClass1,class BaseClass1> on type class G2_C564`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C564`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C564`2<class BaseClass1,class BaseClass1>::ClassMethod1340()
		ldstr "G2_C564::ClassMethod1340.MI.10202()"
		ldstr "class G2_C564`2<class BaseClass1,class BaseClass1> on type class G2_C564`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G1_C10`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C10`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C10`2<class BaseClass0,class BaseClass0>::ClassMethod1343<object>()
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass0> on type class G1_C10`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C10`2<class BaseClass0,class BaseClass0>::ClassMethod1342<object>()
		ldstr "G1_C10::ClassMethod1342.4852<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass0> on type class G1_C10`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C10`2<class BaseClass0,class BaseClass0>::ClassMethod1341()
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass0> on type class G1_C10`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C10`2<class BaseClass0,class BaseClass0>::ClassMethod1340()
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass0> on type class G1_C10`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C10`2<class BaseClass0,class BaseClass0>::Method6<object>()
		ldstr "G1_C10::Method6.4848<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass0> on type class G1_C10`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C10`2<class BaseClass0,class BaseClass0>::Method5()
		ldstr "G1_C10::Method5.4847()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass0> on type class G1_C10`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C10`2<class BaseClass0,class BaseClass0>::Method4()
		ldstr "G1_C10::Method4.4845()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass0> on type class G1_C10`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C10`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass0> on type class G1_C10`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G1_C10`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G1_C10::Method4.MI.4846()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C10`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G1_C10::Method5.4847()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C10`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G1_C10::Method6.MI.4849<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C10`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G1_C10`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G1_C10`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass0,class BaseClass1>::ClassMethod1343<object>()
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G1_C10`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass0,class BaseClass1>::ClassMethod1342<object>()
		ldstr "G1_C10::ClassMethod1342.4852<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G1_C10`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass0,class BaseClass1>::ClassMethod1341()
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G1_C10`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass0,class BaseClass1>::ClassMethod1340()
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G1_C10`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass0,class BaseClass1>::Method6<object>()
		ldstr "G1_C10::Method6.4848<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G1_C10`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass0,class BaseClass1>::Method5()
		ldstr "G1_C10::Method5.4847()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G1_C10`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass0,class BaseClass1>::Method4()
		ldstr "G1_C10::Method4.4845()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G1_C10`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G1_C10`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G1_C10`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G1_C10::Method4.MI.4846()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C10`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G1_C10::Method5.4847()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C10`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G1_C10::Method6.MI.4849<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C10`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G1_C10`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G1_C10`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C10`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C10`2<class BaseClass1,class BaseClass0>::ClassMethod1343<object>()
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass0> on type class G1_C10`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C10`2<class BaseClass1,class BaseClass0>::ClassMethod1342<object>()
		ldstr "G1_C10::ClassMethod1342.4852<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass0> on type class G1_C10`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C10`2<class BaseClass1,class BaseClass0>::ClassMethod1341()
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass0> on type class G1_C10`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C10`2<class BaseClass1,class BaseClass0>::ClassMethod1340()
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass0> on type class G1_C10`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C10`2<class BaseClass1,class BaseClass0>::Method6<object>()
		ldstr "G1_C10::Method6.4848<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass0> on type class G1_C10`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C10`2<class BaseClass1,class BaseClass0>::Method5()
		ldstr "G1_C10::Method5.4847()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass0> on type class G1_C10`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C10`2<class BaseClass1,class BaseClass0>::Method4()
		ldstr "G1_C10::Method4.4845()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass0> on type class G1_C10`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C10`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass0> on type class G1_C10`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G1_C10`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G1_C10::Method4.MI.4846()"
		ldstr "class IBase1`1<class BaseClass1> on type class G1_C10`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G1_C10::Method5.4847()"
		ldstr "class IBase1`1<class BaseClass1> on type class G1_C10`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G1_C10::Method6.MI.4849<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G1_C10`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G1_C10`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G1_C10::Method4.MI.4846()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C10`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G1_C10::Method5.4847()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C10`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G1_C10::Method6.MI.4849<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C10`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G1_C10`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass1,class BaseClass1>::ClassMethod1343<object>()
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G1_C10`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass1,class BaseClass1>::ClassMethod1342<object>()
		ldstr "G1_C10::ClassMethod1342.4852<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G1_C10`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass1,class BaseClass1>::ClassMethod1341()
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G1_C10`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass1,class BaseClass1>::ClassMethod1340()
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G1_C10`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass1,class BaseClass1>::Method6<object>()
		ldstr "G1_C10::Method6.4848<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G1_C10`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass1,class BaseClass1>::Method5()
		ldstr "G1_C10::Method5.4847()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G1_C10`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass1,class BaseClass1>::Method4()
		ldstr "G1_C10::Method4.4845()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G1_C10`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G1_C10`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G1_C10`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G1_C10::Method4.MI.4846()"
		ldstr "class IBase1`1<class BaseClass1> on type class G1_C10`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G1_C10::Method5.4847()"
		ldstr "class IBase1`1<class BaseClass1> on type class G1_C10`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G1_C10::Method6.MI.4849<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G1_C10`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G1_C10`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G1_C10::Method4.MI.4846()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C10`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G1_C10::Method5.4847()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C10`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G1_C10::Method6.MI.4849<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C10`2<class BaseClass1,class BaseClass1>"
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
		newobj instance void class G3_C1555`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C564::ClassMethod1340.MI.10202()#G2_C564::ClassMethod1341.MI.10203()#G2_C564::ClassMethod1342.MI.10204<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G2_C564::Method4.10194()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated1083::M.G1_C10.T.T<class BaseClass1,class BaseClass1,class G3_C1555`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C564::ClassMethod1340.MI.10202()#G2_C564::ClassMethod1341.MI.10203()#G2_C564::ClassMethod1342.MI.10204<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G2_C564::Method4.10194()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated1083::M.G1_C10.B.T<class BaseClass1,class G3_C1555`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C564::ClassMethod1340.MI.10202()#G2_C564::ClassMethod1341.MI.10203()#G2_C564::ClassMethod1342.MI.10204<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G2_C564::Method4.10194()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated1083::M.G1_C10.B.B<class G3_C1555`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1555::Method7.16781<System.Object>()#"
		call void Generated1083::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G3_C1555`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1555::Method7.16781<System.Object>()#"
		call void Generated1083::M.IBase2.B.T<class BaseClass1,class G3_C1555`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1555::Method7.16781<System.Object>()#"
		call void Generated1083::M.IBase2.B.B<class G3_C1555`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C564::Method4.10194()#G2_C564::Method5.MI.10196()#G2_C564::Method6.MI.10198<System.Object>()#"
		call void Generated1083::M.IBase1.T<class BaseClass1,class G3_C1555`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C564::Method4.10194()#G2_C564::Method5.MI.10196()#G2_C564::Method6.MI.10198<System.Object>()#"
		call void Generated1083::M.IBase1.B<class G3_C1555`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1555::Method7.16781<System.Object>()#"
		call void Generated1083::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G3_C1555`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1555::Method7.16781<System.Object>()#"
		call void Generated1083::M.IBase2.A.T<class BaseClass1,class G3_C1555`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1555::Method7.16781<System.Object>()#"
		call void Generated1083::M.IBase2.A.B<class G3_C1555`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C564::Method4.10194()#G2_C564::Method5.MI.10196()#G2_C564::Method6.MI.10198<System.Object>()#"
		call void Generated1083::M.IBase1.T<class BaseClass0,class G3_C1555`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C564::Method4.10194()#G2_C564::Method5.MI.10196()#G2_C564::Method6.MI.10198<System.Object>()#"
		call void Generated1083::M.IBase1.A<class G3_C1555`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C564::ClassMethod1340.MI.10202()#G2_C564::ClassMethod1341.MI.10203()#G2_C564::ClassMethod1342.MI.10204<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G3_C1555::ClassMethod2500.MI.16785()#G2_C564::ClassMethod2501.10200()#G2_C564::ClassMethod2502.10201<System.Object>()#G2_C564::Method4.10194()#G2_C564::Method5.10195()#G2_C564::Method6.10197<System.Object>()#G3_C1555::Method7.16781<System.Object>()#"
		call void Generated1083::M.G2_C564.T.T<class BaseClass0,class BaseClass1,class G3_C1555`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C564::ClassMethod1340.MI.10202()#G2_C564::ClassMethod1341.MI.10203()#G2_C564::ClassMethod1342.MI.10204<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G3_C1555::ClassMethod2500.MI.16785()#G2_C564::ClassMethod2501.10200()#G2_C564::ClassMethod2502.10201<System.Object>()#G2_C564::Method4.10194()#G2_C564::Method5.10195()#G2_C564::Method6.10197<System.Object>()#G3_C1555::Method7.16781<System.Object>()#"
		call void Generated1083::M.G2_C564.A.T<class BaseClass1,class G3_C1555`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C564::ClassMethod1340.MI.10202()#G2_C564::ClassMethod1341.MI.10203()#G2_C564::ClassMethod1342.MI.10204<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G3_C1555::ClassMethod2500.MI.16785()#G2_C564::ClassMethod2501.10200()#G2_C564::ClassMethod2502.10201<System.Object>()#G2_C564::Method4.10194()#G2_C564::Method5.10195()#G2_C564::Method6.10197<System.Object>()#G3_C1555::Method7.16781<System.Object>()#"
		call void Generated1083::M.G2_C564.A.B<class G3_C1555`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C564::ClassMethod1340.MI.10202()#G2_C564::ClassMethod1341.MI.10203()#G2_C564::ClassMethod1342.MI.10204<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G3_C1555::ClassMethod2500.MI.16785()#G2_C564::ClassMethod2501.10200()#G2_C564::ClassMethod2502.10201<System.Object>()#G3_C1555::ClassMethod4504.16782()#G3_C1555::ClassMethod4505.16783<System.Object>()#G3_C1555::ClassMethod4506.16784<System.Object>()#G2_C564::Method4.10194()#G2_C564::Method5.10195()#G2_C564::Method6.10197<System.Object>()#G3_C1555::Method7.16781<System.Object>()#"
		call void Generated1083::M.G3_C1555.T<class BaseClass0,class G3_C1555`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C564::ClassMethod1340.MI.10202()#G2_C564::ClassMethod1341.MI.10203()#G2_C564::ClassMethod1342.MI.10204<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G3_C1555::ClassMethod2500.MI.16785()#G2_C564::ClassMethod2501.10200()#G2_C564::ClassMethod2502.10201<System.Object>()#G3_C1555::ClassMethod4504.16782()#G3_C1555::ClassMethod4505.16783<System.Object>()#G3_C1555::ClassMethod4506.16784<System.Object>()#G2_C564::Method4.10194()#G2_C564::Method5.10195()#G2_C564::Method6.10197<System.Object>()#G3_C1555::Method7.16781<System.Object>()#"
		call void Generated1083::M.G3_C1555.A<class G3_C1555`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1555::Method7.16781<System.Object>()#"
		call void Generated1083::M.IBase2.T.T<class BaseClass1,class BaseClass0,class G3_C1555`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1555::Method7.16781<System.Object>()#"
		call void Generated1083::M.IBase2.B.T<class BaseClass0,class G3_C1555`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1555::Method7.16781<System.Object>()#"
		call void Generated1083::M.IBase2.B.A<class G3_C1555`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1555::Method7.16781<System.Object>()#"
		call void Generated1083::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G3_C1555`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1555::Method7.16781<System.Object>()#"
		call void Generated1083::M.IBase2.A.T<class BaseClass0,class G3_C1555`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1555::Method7.16781<System.Object>()#"
		call void Generated1083::M.IBase2.A.A<class G3_C1555`1<class BaseClass0>>(!!0,string)
		newobj instance void class G3_C1555`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C564::ClassMethod1340.MI.10202()#G2_C564::ClassMethod1341.MI.10203()#G2_C564::ClassMethod1342.MI.10204<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G2_C564::Method4.10194()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated1083::M.G1_C10.T.T<class BaseClass1,class BaseClass1,class G3_C1555`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C564::ClassMethod1340.MI.10202()#G2_C564::ClassMethod1341.MI.10203()#G2_C564::ClassMethod1342.MI.10204<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G2_C564::Method4.10194()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated1083::M.G1_C10.B.T<class BaseClass1,class G3_C1555`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C564::ClassMethod1340.MI.10202()#G2_C564::ClassMethod1341.MI.10203()#G2_C564::ClassMethod1342.MI.10204<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G2_C564::Method4.10194()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated1083::M.G1_C10.B.B<class G3_C1555`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1555::Method7.16781<System.Object>()#"
		call void Generated1083::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G3_C1555`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1555::Method7.16781<System.Object>()#"
		call void Generated1083::M.IBase2.B.T<class BaseClass1,class G3_C1555`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1555::Method7.16781<System.Object>()#"
		call void Generated1083::M.IBase2.B.B<class G3_C1555`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C564::Method4.10194()#G2_C564::Method5.MI.10196()#G2_C564::Method6.MI.10198<System.Object>()#"
		call void Generated1083::M.IBase1.T<class BaseClass1,class G3_C1555`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C564::Method4.10194()#G2_C564::Method5.MI.10196()#G2_C564::Method6.MI.10198<System.Object>()#"
		call void Generated1083::M.IBase1.B<class G3_C1555`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1555::Method7.16781<System.Object>()#"
		call void Generated1083::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G3_C1555`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1555::Method7.16781<System.Object>()#"
		call void Generated1083::M.IBase2.A.T<class BaseClass1,class G3_C1555`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1555::Method7.16781<System.Object>()#"
		call void Generated1083::M.IBase2.A.B<class G3_C1555`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C564::Method4.10194()#G2_C564::Method5.MI.10196()#G2_C564::Method6.MI.10198<System.Object>()#"
		call void Generated1083::M.IBase1.T<class BaseClass0,class G3_C1555`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C564::Method4.10194()#G2_C564::Method5.MI.10196()#G2_C564::Method6.MI.10198<System.Object>()#"
		call void Generated1083::M.IBase1.A<class G3_C1555`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C564::ClassMethod1340.MI.10202()#G2_C564::ClassMethod1341.MI.10203()#G2_C564::ClassMethod1342.MI.10204<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G3_C1555::ClassMethod2500.MI.16785()#G2_C564::ClassMethod2501.10200()#G2_C564::ClassMethod2502.10201<System.Object>()#G2_C564::Method4.10194()#G2_C564::Method5.10195()#G2_C564::Method6.10197<System.Object>()#G3_C1555::Method7.16781<System.Object>()#"
		call void Generated1083::M.G2_C564.T.T<class BaseClass0,class BaseClass1,class G3_C1555`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C564::ClassMethod1340.MI.10202()#G2_C564::ClassMethod1341.MI.10203()#G2_C564::ClassMethod1342.MI.10204<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G3_C1555::ClassMethod2500.MI.16785()#G2_C564::ClassMethod2501.10200()#G2_C564::ClassMethod2502.10201<System.Object>()#G2_C564::Method4.10194()#G2_C564::Method5.10195()#G2_C564::Method6.10197<System.Object>()#G3_C1555::Method7.16781<System.Object>()#"
		call void Generated1083::M.G2_C564.A.T<class BaseClass1,class G3_C1555`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C564::ClassMethod1340.MI.10202()#G2_C564::ClassMethod1341.MI.10203()#G2_C564::ClassMethod1342.MI.10204<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G3_C1555::ClassMethod2500.MI.16785()#G2_C564::ClassMethod2501.10200()#G2_C564::ClassMethod2502.10201<System.Object>()#G2_C564::Method4.10194()#G2_C564::Method5.10195()#G2_C564::Method6.10197<System.Object>()#G3_C1555::Method7.16781<System.Object>()#"
		call void Generated1083::M.G2_C564.A.B<class G3_C1555`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C564::ClassMethod1340.MI.10202()#G2_C564::ClassMethod1341.MI.10203()#G2_C564::ClassMethod1342.MI.10204<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G3_C1555::ClassMethod2500.MI.16785()#G2_C564::ClassMethod2501.10200()#G2_C564::ClassMethod2502.10201<System.Object>()#G3_C1555::ClassMethod4504.16782()#G3_C1555::ClassMethod4505.16783<System.Object>()#G3_C1555::ClassMethod4506.16784<System.Object>()#G2_C564::Method4.10194()#G2_C564::Method5.10195()#G2_C564::Method6.10197<System.Object>()#G3_C1555::Method7.16781<System.Object>()#"
		call void Generated1083::M.G3_C1555.T<class BaseClass1,class G3_C1555`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C564::ClassMethod1340.MI.10202()#G2_C564::ClassMethod1341.MI.10203()#G2_C564::ClassMethod1342.MI.10204<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G3_C1555::ClassMethod2500.MI.16785()#G2_C564::ClassMethod2501.10200()#G2_C564::ClassMethod2502.10201<System.Object>()#G3_C1555::ClassMethod4504.16782()#G3_C1555::ClassMethod4505.16783<System.Object>()#G3_C1555::ClassMethod4506.16784<System.Object>()#G2_C564::Method4.10194()#G2_C564::Method5.10195()#G2_C564::Method6.10197<System.Object>()#G3_C1555::Method7.16781<System.Object>()#"
		call void Generated1083::M.G3_C1555.B<class G3_C1555`1<class BaseClass1>>(!!0,string)
		newobj instance void class G2_C564`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C564::ClassMethod1340.MI.10202()#G2_C564::ClassMethod1341.MI.10203()#G2_C564::ClassMethod1342.MI.10204<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G2_C564::Method4.10194()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated1083::M.G1_C10.T.T<class BaseClass0,class BaseClass0,class G2_C564`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C564::ClassMethod1340.MI.10202()#G2_C564::ClassMethod1341.MI.10203()#G2_C564::ClassMethod1342.MI.10204<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G2_C564::Method4.10194()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated1083::M.G1_C10.A.T<class BaseClass0,class G2_C564`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C564::ClassMethod1340.MI.10202()#G2_C564::ClassMethod1341.MI.10203()#G2_C564::ClassMethod1342.MI.10204<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G2_C564::Method4.10194()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated1083::M.G1_C10.A.A<class G2_C564`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1083::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G2_C564`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1083::M.IBase2.B.T<class BaseClass1,class G2_C564`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1083::M.IBase2.B.B<class G2_C564`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C564::Method4.10194()#G2_C564::Method5.MI.10196()#G2_C564::Method6.MI.10198<System.Object>()#"
		call void Generated1083::M.IBase1.T<class BaseClass0,class G2_C564`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C564::Method4.10194()#G2_C564::Method5.MI.10196()#G2_C564::Method6.MI.10198<System.Object>()#"
		call void Generated1083::M.IBase1.A<class G2_C564`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C564::Method7.10193<System.Object>()#"
		call void Generated1083::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C564`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C564::Method7.10193<System.Object>()#"
		call void Generated1083::M.IBase2.A.T<class BaseClass1,class G2_C564`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C564::Method7.10193<System.Object>()#"
		call void Generated1083::M.IBase2.A.B<class G2_C564`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C564::ClassMethod1340.MI.10202()#G2_C564::ClassMethod1341.MI.10203()#G2_C564::ClassMethod1342.MI.10204<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G2_C564::ClassMethod2500.10199()#G2_C564::ClassMethod2501.10200()#G2_C564::ClassMethod2502.10201<System.Object>()#G2_C564::Method4.10194()#G2_C564::Method5.10195()#G2_C564::Method6.10197<System.Object>()#G2_C564::Method7.10193<System.Object>()#"
		call void Generated1083::M.G2_C564.T.T<class BaseClass0,class BaseClass0,class G2_C564`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C564::ClassMethod1340.MI.10202()#G2_C564::ClassMethod1341.MI.10203()#G2_C564::ClassMethod1342.MI.10204<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G2_C564::ClassMethod2500.10199()#G2_C564::ClassMethod2501.10200()#G2_C564::ClassMethod2502.10201<System.Object>()#G2_C564::Method4.10194()#G2_C564::Method5.10195()#G2_C564::Method6.10197<System.Object>()#G2_C564::Method7.10193<System.Object>()#"
		call void Generated1083::M.G2_C564.A.T<class BaseClass0,class G2_C564`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C564::ClassMethod1340.MI.10202()#G2_C564::ClassMethod1341.MI.10203()#G2_C564::ClassMethod1342.MI.10204<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G2_C564::ClassMethod2500.10199()#G2_C564::ClassMethod2501.10200()#G2_C564::ClassMethod2502.10201<System.Object>()#G2_C564::Method4.10194()#G2_C564::Method5.10195()#G2_C564::Method6.10197<System.Object>()#G2_C564::Method7.10193<System.Object>()#"
		call void Generated1083::M.G2_C564.A.A<class G2_C564`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C564::Method7.10193<System.Object>()#"
		call void Generated1083::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G2_C564`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C564::Method7.10193<System.Object>()#"
		call void Generated1083::M.IBase2.A.T<class BaseClass0,class G2_C564`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C564::Method7.10193<System.Object>()#"
		call void Generated1083::M.IBase2.A.A<class G2_C564`2<class BaseClass0,class BaseClass0>>(!!0,string)
		newobj instance void class G2_C564`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C564::ClassMethod1340.MI.10202()#G2_C564::ClassMethod1341.MI.10203()#G2_C564::ClassMethod1342.MI.10204<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G2_C564::Method4.10194()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated1083::M.G1_C10.T.T<class BaseClass1,class BaseClass1,class G2_C564`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C564::ClassMethod1340.MI.10202()#G2_C564::ClassMethod1341.MI.10203()#G2_C564::ClassMethod1342.MI.10204<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G2_C564::Method4.10194()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated1083::M.G1_C10.B.T<class BaseClass1,class G2_C564`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C564::ClassMethod1340.MI.10202()#G2_C564::ClassMethod1341.MI.10203()#G2_C564::ClassMethod1342.MI.10204<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G2_C564::Method4.10194()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated1083::M.G1_C10.B.B<class G2_C564`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1083::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G2_C564`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1083::M.IBase2.B.T<class BaseClass1,class G2_C564`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1083::M.IBase2.B.B<class G2_C564`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C564::Method4.10194()#G2_C564::Method5.MI.10196()#G2_C564::Method6.MI.10198<System.Object>()#"
		call void Generated1083::M.IBase1.T<class BaseClass1,class G2_C564`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C564::Method4.10194()#G2_C564::Method5.MI.10196()#G2_C564::Method6.MI.10198<System.Object>()#"
		call void Generated1083::M.IBase1.B<class G2_C564`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C564::Method7.10193<System.Object>()#"
		call void Generated1083::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C564`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C564::Method7.10193<System.Object>()#"
		call void Generated1083::M.IBase2.A.T<class BaseClass1,class G2_C564`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C564::Method7.10193<System.Object>()#"
		call void Generated1083::M.IBase2.A.B<class G2_C564`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C564::Method4.10194()#G2_C564::Method5.MI.10196()#G2_C564::Method6.MI.10198<System.Object>()#"
		call void Generated1083::M.IBase1.T<class BaseClass0,class G2_C564`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C564::Method4.10194()#G2_C564::Method5.MI.10196()#G2_C564::Method6.MI.10198<System.Object>()#"
		call void Generated1083::M.IBase1.A<class G2_C564`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C564::ClassMethod1340.MI.10202()#G2_C564::ClassMethod1341.MI.10203()#G2_C564::ClassMethod1342.MI.10204<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G2_C564::ClassMethod2500.10199()#G2_C564::ClassMethod2501.10200()#G2_C564::ClassMethod2502.10201<System.Object>()#G2_C564::Method4.10194()#G2_C564::Method5.10195()#G2_C564::Method6.10197<System.Object>()#G2_C564::Method7.10193<System.Object>()#"
		call void Generated1083::M.G2_C564.T.T<class BaseClass0,class BaseClass1,class G2_C564`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C564::ClassMethod1340.MI.10202()#G2_C564::ClassMethod1341.MI.10203()#G2_C564::ClassMethod1342.MI.10204<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G2_C564::ClassMethod2500.10199()#G2_C564::ClassMethod2501.10200()#G2_C564::ClassMethod2502.10201<System.Object>()#G2_C564::Method4.10194()#G2_C564::Method5.10195()#G2_C564::Method6.10197<System.Object>()#G2_C564::Method7.10193<System.Object>()#"
		call void Generated1083::M.G2_C564.A.T<class BaseClass1,class G2_C564`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C564::ClassMethod1340.MI.10202()#G2_C564::ClassMethod1341.MI.10203()#G2_C564::ClassMethod1342.MI.10204<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G2_C564::ClassMethod2500.10199()#G2_C564::ClassMethod2501.10200()#G2_C564::ClassMethod2502.10201<System.Object>()#G2_C564::Method4.10194()#G2_C564::Method5.10195()#G2_C564::Method6.10197<System.Object>()#G2_C564::Method7.10193<System.Object>()#"
		call void Generated1083::M.G2_C564.A.B<class G2_C564`2<class BaseClass0,class BaseClass1>>(!!0,string)
		newobj instance void class G2_C564`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C564::ClassMethod1340.MI.10202()#G2_C564::ClassMethod1341.MI.10203()#G2_C564::ClassMethod1342.MI.10204<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G2_C564::Method4.10194()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated1083::M.G1_C10.T.T<class BaseClass0,class BaseClass0,class G2_C564`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C564::ClassMethod1340.MI.10202()#G2_C564::ClassMethod1341.MI.10203()#G2_C564::ClassMethod1342.MI.10204<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G2_C564::Method4.10194()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated1083::M.G1_C10.A.T<class BaseClass0,class G2_C564`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C564::ClassMethod1340.MI.10202()#G2_C564::ClassMethod1341.MI.10203()#G2_C564::ClassMethod1342.MI.10204<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G2_C564::Method4.10194()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated1083::M.G1_C10.A.A<class G2_C564`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1083::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G2_C564`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1083::M.IBase2.B.T<class BaseClass1,class G2_C564`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1083::M.IBase2.B.B<class G2_C564`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C564::Method4.10194()#G2_C564::Method5.MI.10196()#G2_C564::Method6.MI.10198<System.Object>()#"
		call void Generated1083::M.IBase1.T<class BaseClass0,class G2_C564`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C564::Method4.10194()#G2_C564::Method5.MI.10196()#G2_C564::Method6.MI.10198<System.Object>()#"
		call void Generated1083::M.IBase1.A<class G2_C564`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C564::Method7.10193<System.Object>()#"
		call void Generated1083::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C564`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C564::Method7.10193<System.Object>()#"
		call void Generated1083::M.IBase2.A.T<class BaseClass1,class G2_C564`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C564::Method7.10193<System.Object>()#"
		call void Generated1083::M.IBase2.A.B<class G2_C564`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C564::ClassMethod1340.MI.10202()#G2_C564::ClassMethod1341.MI.10203()#G2_C564::ClassMethod1342.MI.10204<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G2_C564::ClassMethod2500.10199()#G2_C564::ClassMethod2501.10200()#G2_C564::ClassMethod2502.10201<System.Object>()#G2_C564::Method4.10194()#G2_C564::Method5.10195()#G2_C564::Method6.10197<System.Object>()#G2_C564::Method7.10193<System.Object>()#"
		call void Generated1083::M.G2_C564.T.T<class BaseClass1,class BaseClass0,class G2_C564`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C564::ClassMethod1340.MI.10202()#G2_C564::ClassMethod1341.MI.10203()#G2_C564::ClassMethod1342.MI.10204<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G2_C564::ClassMethod2500.10199()#G2_C564::ClassMethod2501.10200()#G2_C564::ClassMethod2502.10201<System.Object>()#G2_C564::Method4.10194()#G2_C564::Method5.10195()#G2_C564::Method6.10197<System.Object>()#G2_C564::Method7.10193<System.Object>()#"
		call void Generated1083::M.G2_C564.B.T<class BaseClass0,class G2_C564`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C564::ClassMethod1340.MI.10202()#G2_C564::ClassMethod1341.MI.10203()#G2_C564::ClassMethod1342.MI.10204<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G2_C564::ClassMethod2500.10199()#G2_C564::ClassMethod2501.10200()#G2_C564::ClassMethod2502.10201<System.Object>()#G2_C564::Method4.10194()#G2_C564::Method5.10195()#G2_C564::Method6.10197<System.Object>()#G2_C564::Method7.10193<System.Object>()#"
		call void Generated1083::M.G2_C564.B.A<class G2_C564`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C564::Method7.10193<System.Object>()#"
		call void Generated1083::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G2_C564`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C564::Method7.10193<System.Object>()#"
		call void Generated1083::M.IBase2.A.T<class BaseClass0,class G2_C564`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C564::Method7.10193<System.Object>()#"
		call void Generated1083::M.IBase2.A.A<class G2_C564`2<class BaseClass1,class BaseClass0>>(!!0,string)
		newobj instance void class G2_C564`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C564::ClassMethod1340.MI.10202()#G2_C564::ClassMethod1341.MI.10203()#G2_C564::ClassMethod1342.MI.10204<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G2_C564::Method4.10194()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated1083::M.G1_C10.T.T<class BaseClass1,class BaseClass1,class G2_C564`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C564::ClassMethod1340.MI.10202()#G2_C564::ClassMethod1341.MI.10203()#G2_C564::ClassMethod1342.MI.10204<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G2_C564::Method4.10194()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated1083::M.G1_C10.B.T<class BaseClass1,class G2_C564`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C564::ClassMethod1340.MI.10202()#G2_C564::ClassMethod1341.MI.10203()#G2_C564::ClassMethod1342.MI.10204<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G2_C564::Method4.10194()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated1083::M.G1_C10.B.B<class G2_C564`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1083::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G2_C564`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1083::M.IBase2.B.T<class BaseClass1,class G2_C564`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1083::M.IBase2.B.B<class G2_C564`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C564::Method4.10194()#G2_C564::Method5.MI.10196()#G2_C564::Method6.MI.10198<System.Object>()#"
		call void Generated1083::M.IBase1.T<class BaseClass1,class G2_C564`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C564::Method4.10194()#G2_C564::Method5.MI.10196()#G2_C564::Method6.MI.10198<System.Object>()#"
		call void Generated1083::M.IBase1.B<class G2_C564`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C564::Method7.10193<System.Object>()#"
		call void Generated1083::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C564`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C564::Method7.10193<System.Object>()#"
		call void Generated1083::M.IBase2.A.T<class BaseClass1,class G2_C564`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C564::Method7.10193<System.Object>()#"
		call void Generated1083::M.IBase2.A.B<class G2_C564`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C564::Method4.10194()#G2_C564::Method5.MI.10196()#G2_C564::Method6.MI.10198<System.Object>()#"
		call void Generated1083::M.IBase1.T<class BaseClass0,class G2_C564`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C564::Method4.10194()#G2_C564::Method5.MI.10196()#G2_C564::Method6.MI.10198<System.Object>()#"
		call void Generated1083::M.IBase1.A<class G2_C564`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C564::ClassMethod1340.MI.10202()#G2_C564::ClassMethod1341.MI.10203()#G2_C564::ClassMethod1342.MI.10204<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G2_C564::ClassMethod2500.10199()#G2_C564::ClassMethod2501.10200()#G2_C564::ClassMethod2502.10201<System.Object>()#G2_C564::Method4.10194()#G2_C564::Method5.10195()#G2_C564::Method6.10197<System.Object>()#G2_C564::Method7.10193<System.Object>()#"
		call void Generated1083::M.G2_C564.T.T<class BaseClass1,class BaseClass1,class G2_C564`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C564::ClassMethod1340.MI.10202()#G2_C564::ClassMethod1341.MI.10203()#G2_C564::ClassMethod1342.MI.10204<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G2_C564::ClassMethod2500.10199()#G2_C564::ClassMethod2501.10200()#G2_C564::ClassMethod2502.10201<System.Object>()#G2_C564::Method4.10194()#G2_C564::Method5.10195()#G2_C564::Method6.10197<System.Object>()#G2_C564::Method7.10193<System.Object>()#"
		call void Generated1083::M.G2_C564.B.T<class BaseClass1,class G2_C564`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C564::ClassMethod1340.MI.10202()#G2_C564::ClassMethod1341.MI.10203()#G2_C564::ClassMethod1342.MI.10204<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G2_C564::ClassMethod2500.10199()#G2_C564::ClassMethod2501.10200()#G2_C564::ClassMethod2502.10201<System.Object>()#G2_C564::Method4.10194()#G2_C564::Method5.10195()#G2_C564::Method6.10197<System.Object>()#G2_C564::Method7.10193<System.Object>()#"
		call void Generated1083::M.G2_C564.B.B<class G2_C564`2<class BaseClass1,class BaseClass1>>(!!0,string)
		newobj instance void class G1_C10`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G1_C10::ClassMethod1342.4852<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G1_C10::Method4.4845()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated1083::M.G1_C10.T.T<class BaseClass0,class BaseClass0,class G1_C10`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G1_C10::ClassMethod1342.4852<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G1_C10::Method4.4845()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated1083::M.G1_C10.A.T<class BaseClass0,class G1_C10`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G1_C10::ClassMethod1342.4852<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G1_C10::Method4.4845()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated1083::M.G1_C10.A.A<class G1_C10`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1083::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G1_C10`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1083::M.IBase2.B.T<class BaseClass1,class G1_C10`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1083::M.IBase2.B.B<class G1_C10`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method4.MI.4846()#G1_C10::Method5.4847()#G1_C10::Method6.MI.4849<System.Object>()#"
		call void Generated1083::M.IBase1.T<class BaseClass0,class G1_C10`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method4.MI.4846()#G1_C10::Method5.4847()#G1_C10::Method6.MI.4849<System.Object>()#"
		call void Generated1083::M.IBase1.A<class G1_C10`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1083::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G1_C10`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1083::M.IBase2.A.T<class BaseClass1,class G1_C10`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1083::M.IBase2.A.B<class G1_C10`2<class BaseClass0,class BaseClass0>>(!!0,string)
		newobj instance void class G1_C10`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G1_C10::ClassMethod1342.4852<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G1_C10::Method4.4845()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated1083::M.G1_C10.T.T<class BaseClass0,class BaseClass1,class G1_C10`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G1_C10::ClassMethod1342.4852<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G1_C10::Method4.4845()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated1083::M.G1_C10.A.T<class BaseClass1,class G1_C10`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G1_C10::ClassMethod1342.4852<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G1_C10::Method4.4845()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated1083::M.G1_C10.A.B<class G1_C10`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1083::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G1_C10`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1083::M.IBase2.B.T<class BaseClass1,class G1_C10`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1083::M.IBase2.B.B<class G1_C10`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method4.MI.4846()#G1_C10::Method5.4847()#G1_C10::Method6.MI.4849<System.Object>()#"
		call void Generated1083::M.IBase1.T<class BaseClass0,class G1_C10`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method4.MI.4846()#G1_C10::Method5.4847()#G1_C10::Method6.MI.4849<System.Object>()#"
		call void Generated1083::M.IBase1.A<class G1_C10`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1083::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G1_C10`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1083::M.IBase2.A.T<class BaseClass1,class G1_C10`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1083::M.IBase2.A.B<class G1_C10`2<class BaseClass0,class BaseClass1>>(!!0,string)
		newobj instance void class G1_C10`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G1_C10::ClassMethod1342.4852<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G1_C10::Method4.4845()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated1083::M.G1_C10.T.T<class BaseClass1,class BaseClass0,class G1_C10`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G1_C10::ClassMethod1342.4852<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G1_C10::Method4.4845()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated1083::M.G1_C10.B.T<class BaseClass0,class G1_C10`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G1_C10::ClassMethod1342.4852<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G1_C10::Method4.4845()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated1083::M.G1_C10.B.A<class G1_C10`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1083::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G1_C10`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1083::M.IBase2.B.T<class BaseClass1,class G1_C10`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1083::M.IBase2.B.B<class G1_C10`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method4.MI.4846()#G1_C10::Method5.4847()#G1_C10::Method6.MI.4849<System.Object>()#"
		call void Generated1083::M.IBase1.T<class BaseClass1,class G1_C10`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method4.MI.4846()#G1_C10::Method5.4847()#G1_C10::Method6.MI.4849<System.Object>()#"
		call void Generated1083::M.IBase1.B<class G1_C10`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1083::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G1_C10`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1083::M.IBase2.A.T<class BaseClass1,class G1_C10`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1083::M.IBase2.A.B<class G1_C10`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method4.MI.4846()#G1_C10::Method5.4847()#G1_C10::Method6.MI.4849<System.Object>()#"
		call void Generated1083::M.IBase1.T<class BaseClass0,class G1_C10`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method4.MI.4846()#G1_C10::Method5.4847()#G1_C10::Method6.MI.4849<System.Object>()#"
		call void Generated1083::M.IBase1.A<class G1_C10`2<class BaseClass1,class BaseClass0>>(!!0,string)
		newobj instance void class G1_C10`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G1_C10::ClassMethod1342.4852<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G1_C10::Method4.4845()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated1083::M.G1_C10.T.T<class BaseClass1,class BaseClass1,class G1_C10`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G1_C10::ClassMethod1342.4852<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G1_C10::Method4.4845()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated1083::M.G1_C10.B.T<class BaseClass1,class G1_C10`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G1_C10::ClassMethod1342.4852<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G1_C10::Method4.4845()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated1083::M.G1_C10.B.B<class G1_C10`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1083::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G1_C10`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1083::M.IBase2.B.T<class BaseClass1,class G1_C10`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1083::M.IBase2.B.B<class G1_C10`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method4.MI.4846()#G1_C10::Method5.4847()#G1_C10::Method6.MI.4849<System.Object>()#"
		call void Generated1083::M.IBase1.T<class BaseClass1,class G1_C10`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method4.MI.4846()#G1_C10::Method5.4847()#G1_C10::Method6.MI.4849<System.Object>()#"
		call void Generated1083::M.IBase1.B<class G1_C10`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1083::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G1_C10`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1083::M.IBase2.A.T<class BaseClass1,class G1_C10`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1083::M.IBase2.A.B<class G1_C10`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method4.MI.4846()#G1_C10::Method5.4847()#G1_C10::Method6.MI.4849<System.Object>()#"
		call void Generated1083::M.IBase1.T<class BaseClass0,class G1_C10`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method4.MI.4846()#G1_C10::Method5.4847()#G1_C10::Method6.MI.4849<System.Object>()#"
		call void Generated1083::M.IBase1.A<class G1_C10`2<class BaseClass1,class BaseClass1>>(!!0,string)
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
		newobj instance void class G3_C1555`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass1,class BaseClass1>::ClassMethod1343<object>()
		calli default string(class G3_C1555`1<class BaseClass0>)
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G3_C1555`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass1,class BaseClass1>::ClassMethod1342<object>()
		calli default string(class G3_C1555`1<class BaseClass0>)
		ldstr "G2_C564::ClassMethod1342.MI.10204<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G3_C1555`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass1,class BaseClass1>::ClassMethod1341()
		calli default string(class G3_C1555`1<class BaseClass0>)
		ldstr "G2_C564::ClassMethod1341.MI.10203()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G3_C1555`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass1,class BaseClass1>::ClassMethod1340()
		calli default string(class G3_C1555`1<class BaseClass0>)
		ldstr "G2_C564::ClassMethod1340.MI.10202()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G3_C1555`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass1,class BaseClass1>::Method6<object>()
		calli default string(class G3_C1555`1<class BaseClass0>)
		ldstr "G1_C10::Method6.4848<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G3_C1555`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass1,class BaseClass1>::Method5()
		calli default string(class G3_C1555`1<class BaseClass0>)
		ldstr "G1_C10::Method5.4847()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G3_C1555`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass1,class BaseClass1>::Method4()
		calli default string(class G3_C1555`1<class BaseClass0>)
		ldstr "G2_C564::Method4.10194()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G3_C1555`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1555`1<class BaseClass0>)
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G3_C1555`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1555`1<class BaseClass0>)
		ldstr "G3_C1555::Method7.16781<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1555`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G3_C1555`1<class BaseClass0>)
		ldstr "G2_C564::Method4.10194()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1555`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G3_C1555`1<class BaseClass0>)
		ldstr "G2_C564::Method5.MI.10196()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1555`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G3_C1555`1<class BaseClass0>)
		ldstr "G2_C564::Method6.MI.10198<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1555`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1555`1<class BaseClass0>)
		ldstr "G3_C1555::Method7.16781<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1555`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G3_C1555`1<class BaseClass0>)
		ldstr "G2_C564::Method4.10194()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1555`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G3_C1555`1<class BaseClass0>)
		ldstr "G2_C564::Method5.MI.10196()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1555`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1555`1<class BaseClass0>)
		ldstr "G2_C564::Method6.MI.10198<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1555`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C564`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C564`2<class BaseClass0,class BaseClass1>::ClassMethod2502<object>()
		calli default string(class G3_C1555`1<class BaseClass0>)
		ldstr "G2_C564::ClassMethod2502.10201<System.Object>()"
		ldstr "class G2_C564`2<class BaseClass0,class BaseClass1> on type class G3_C1555`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C564`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C564`2<class BaseClass0,class BaseClass1>::ClassMethod2501()
		calli default string(class G3_C1555`1<class BaseClass0>)
		ldstr "G2_C564::ClassMethod2501.10200()"
		ldstr "class G2_C564`2<class BaseClass0,class BaseClass1> on type class G3_C1555`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C564`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C564`2<class BaseClass0,class BaseClass1>::ClassMethod2500()
		calli default string(class G3_C1555`1<class BaseClass0>)
		ldstr "G3_C1555::ClassMethod2500.MI.16785()"
		ldstr "class G2_C564`2<class BaseClass0,class BaseClass1> on type class G3_C1555`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C564`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C564`2<class BaseClass0,class BaseClass1>::Method6<object>()
		calli default string(class G3_C1555`1<class BaseClass0>)
		ldstr "G2_C564::Method6.10197<System.Object>()"
		ldstr "class G2_C564`2<class BaseClass0,class BaseClass1> on type class G3_C1555`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C564`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C564`2<class BaseClass0,class BaseClass1>::Method5()
		calli default string(class G3_C1555`1<class BaseClass0>)
		ldstr "G2_C564::Method5.10195()"
		ldstr "class G2_C564`2<class BaseClass0,class BaseClass1> on type class G3_C1555`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C564`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C564`2<class BaseClass0,class BaseClass1>::Method4()
		calli default string(class G3_C1555`1<class BaseClass0>)
		ldstr "G2_C564::Method4.10194()"
		ldstr "class G2_C564`2<class BaseClass0,class BaseClass1> on type class G3_C1555`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C564`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C564`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1555`1<class BaseClass0>)
		ldstr "G3_C1555::Method7.16781<System.Object>()"
		ldstr "class G2_C564`2<class BaseClass0,class BaseClass1> on type class G3_C1555`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C564`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C564`2<class BaseClass0,class BaseClass1>::ClassMethod1343<object>()
		calli default string(class G3_C1555`1<class BaseClass0>)
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G2_C564`2<class BaseClass0,class BaseClass1> on type class G3_C1555`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C564`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C564`2<class BaseClass0,class BaseClass1>::ClassMethod1342<object>()
		calli default string(class G3_C1555`1<class BaseClass0>)
		ldstr "G2_C564::ClassMethod1342.MI.10204<System.Object>()"
		ldstr "class G2_C564`2<class BaseClass0,class BaseClass1> on type class G3_C1555`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C564`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C564`2<class BaseClass0,class BaseClass1>::ClassMethod1341()
		calli default string(class G3_C1555`1<class BaseClass0>)
		ldstr "G2_C564::ClassMethod1341.MI.10203()"
		ldstr "class G2_C564`2<class BaseClass0,class BaseClass1> on type class G3_C1555`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C564`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C564`2<class BaseClass0,class BaseClass1>::ClassMethod1340()
		calli default string(class G3_C1555`1<class BaseClass0>)
		ldstr "G2_C564::ClassMethod1340.MI.10202()"
		ldstr "class G2_C564`2<class BaseClass0,class BaseClass1> on type class G3_C1555`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1555`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1555`1<class BaseClass0>::ClassMethod4506<object>()
		calli default string(class G3_C1555`1<class BaseClass0>)
		ldstr "G3_C1555::ClassMethod4506.16784<System.Object>()"
		ldstr "class G3_C1555`1<class BaseClass0> on type class G3_C1555`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1555`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1555`1<class BaseClass0>::ClassMethod4505<object>()
		calli default string(class G3_C1555`1<class BaseClass0>)
		ldstr "G3_C1555::ClassMethod4505.16783<System.Object>()"
		ldstr "class G3_C1555`1<class BaseClass0> on type class G3_C1555`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1555`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1555`1<class BaseClass0>::ClassMethod4504()
		calli default string(class G3_C1555`1<class BaseClass0>)
		ldstr "G3_C1555::ClassMethod4504.16782()"
		ldstr "class G3_C1555`1<class BaseClass0> on type class G3_C1555`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1555`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1555`1<class BaseClass0>::Method7<object>()
		calli default string(class G3_C1555`1<class BaseClass0>)
		ldstr "G3_C1555::Method7.16781<System.Object>()"
		ldstr "class G3_C1555`1<class BaseClass0> on type class G3_C1555`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1555`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1555`1<class BaseClass0>::ClassMethod2502<object>()
		calli default string(class G3_C1555`1<class BaseClass0>)
		ldstr "G2_C564::ClassMethod2502.10201<System.Object>()"
		ldstr "class G3_C1555`1<class BaseClass0> on type class G3_C1555`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1555`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1555`1<class BaseClass0>::ClassMethod2501()
		calli default string(class G3_C1555`1<class BaseClass0>)
		ldstr "G2_C564::ClassMethod2501.10200()"
		ldstr "class G3_C1555`1<class BaseClass0> on type class G3_C1555`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1555`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1555`1<class BaseClass0>::ClassMethod2500()
		calli default string(class G3_C1555`1<class BaseClass0>)
		ldstr "G3_C1555::ClassMethod2500.MI.16785()"
		ldstr "class G3_C1555`1<class BaseClass0> on type class G3_C1555`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1555`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1555`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1555`1<class BaseClass0>)
		ldstr "G2_C564::Method6.10197<System.Object>()"
		ldstr "class G3_C1555`1<class BaseClass0> on type class G3_C1555`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1555`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1555`1<class BaseClass0>::Method5()
		calli default string(class G3_C1555`1<class BaseClass0>)
		ldstr "G2_C564::Method5.10195()"
		ldstr "class G3_C1555`1<class BaseClass0> on type class G3_C1555`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1555`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1555`1<class BaseClass0>::Method4()
		calli default string(class G3_C1555`1<class BaseClass0>)
		ldstr "G2_C564::Method4.10194()"
		ldstr "class G3_C1555`1<class BaseClass0> on type class G3_C1555`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1555`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1555`1<class BaseClass0>::ClassMethod1343<object>()
		calli default string(class G3_C1555`1<class BaseClass0>)
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G3_C1555`1<class BaseClass0> on type class G3_C1555`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1555`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1555`1<class BaseClass0>::ClassMethod1342<object>()
		calli default string(class G3_C1555`1<class BaseClass0>)
		ldstr "G2_C564::ClassMethod1342.MI.10204<System.Object>()"
		ldstr "class G3_C1555`1<class BaseClass0> on type class G3_C1555`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1555`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1555`1<class BaseClass0>::ClassMethod1341()
		calli default string(class G3_C1555`1<class BaseClass0>)
		ldstr "G2_C564::ClassMethod1341.MI.10203()"
		ldstr "class G3_C1555`1<class BaseClass0> on type class G3_C1555`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1555`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1555`1<class BaseClass0>::ClassMethod1340()
		calli default string(class G3_C1555`1<class BaseClass0>)
		ldstr "G2_C564::ClassMethod1340.MI.10202()"
		ldstr "class G3_C1555`1<class BaseClass0> on type class G3_C1555`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1555`1<class BaseClass0>)
		ldstr "G3_C1555::Method7.16781<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G3_C1555`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1555`1<class BaseClass0>)
		ldstr "G3_C1555::Method7.16781<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1555`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G3_C1555`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass1,class BaseClass1>::ClassMethod1343<object>()
		calli default string(class G3_C1555`1<class BaseClass1>)
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G3_C1555`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass1,class BaseClass1>::ClassMethod1342<object>()
		calli default string(class G3_C1555`1<class BaseClass1>)
		ldstr "G2_C564::ClassMethod1342.MI.10204<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G3_C1555`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass1,class BaseClass1>::ClassMethod1341()
		calli default string(class G3_C1555`1<class BaseClass1>)
		ldstr "G2_C564::ClassMethod1341.MI.10203()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G3_C1555`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass1,class BaseClass1>::ClassMethod1340()
		calli default string(class G3_C1555`1<class BaseClass1>)
		ldstr "G2_C564::ClassMethod1340.MI.10202()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G3_C1555`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass1,class BaseClass1>::Method6<object>()
		calli default string(class G3_C1555`1<class BaseClass1>)
		ldstr "G1_C10::Method6.4848<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G3_C1555`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass1,class BaseClass1>::Method5()
		calli default string(class G3_C1555`1<class BaseClass1>)
		ldstr "G1_C10::Method5.4847()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G3_C1555`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass1,class BaseClass1>::Method4()
		calli default string(class G3_C1555`1<class BaseClass1>)
		ldstr "G2_C564::Method4.10194()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G3_C1555`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1555`1<class BaseClass1>)
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G3_C1555`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1555`1<class BaseClass1>)
		ldstr "G3_C1555::Method7.16781<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1555`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G3_C1555`1<class BaseClass1>)
		ldstr "G2_C564::Method4.10194()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1555`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G3_C1555`1<class BaseClass1>)
		ldstr "G2_C564::Method5.MI.10196()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1555`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G3_C1555`1<class BaseClass1>)
		ldstr "G2_C564::Method6.MI.10198<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1555`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1555`1<class BaseClass1>)
		ldstr "G3_C1555::Method7.16781<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1555`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G3_C1555`1<class BaseClass1>)
		ldstr "G2_C564::Method4.10194()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1555`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G3_C1555`1<class BaseClass1>)
		ldstr "G2_C564::Method5.MI.10196()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1555`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1555`1<class BaseClass1>)
		ldstr "G2_C564::Method6.MI.10198<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1555`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C564`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C564`2<class BaseClass0,class BaseClass1>::ClassMethod2502<object>()
		calli default string(class G3_C1555`1<class BaseClass1>)
		ldstr "G2_C564::ClassMethod2502.10201<System.Object>()"
		ldstr "class G2_C564`2<class BaseClass0,class BaseClass1> on type class G3_C1555`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C564`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C564`2<class BaseClass0,class BaseClass1>::ClassMethod2501()
		calli default string(class G3_C1555`1<class BaseClass1>)
		ldstr "G2_C564::ClassMethod2501.10200()"
		ldstr "class G2_C564`2<class BaseClass0,class BaseClass1> on type class G3_C1555`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C564`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C564`2<class BaseClass0,class BaseClass1>::ClassMethod2500()
		calli default string(class G3_C1555`1<class BaseClass1>)
		ldstr "G3_C1555::ClassMethod2500.MI.16785()"
		ldstr "class G2_C564`2<class BaseClass0,class BaseClass1> on type class G3_C1555`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C564`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C564`2<class BaseClass0,class BaseClass1>::Method6<object>()
		calli default string(class G3_C1555`1<class BaseClass1>)
		ldstr "G2_C564::Method6.10197<System.Object>()"
		ldstr "class G2_C564`2<class BaseClass0,class BaseClass1> on type class G3_C1555`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C564`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C564`2<class BaseClass0,class BaseClass1>::Method5()
		calli default string(class G3_C1555`1<class BaseClass1>)
		ldstr "G2_C564::Method5.10195()"
		ldstr "class G2_C564`2<class BaseClass0,class BaseClass1> on type class G3_C1555`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C564`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C564`2<class BaseClass0,class BaseClass1>::Method4()
		calli default string(class G3_C1555`1<class BaseClass1>)
		ldstr "G2_C564::Method4.10194()"
		ldstr "class G2_C564`2<class BaseClass0,class BaseClass1> on type class G3_C1555`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C564`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C564`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1555`1<class BaseClass1>)
		ldstr "G3_C1555::Method7.16781<System.Object>()"
		ldstr "class G2_C564`2<class BaseClass0,class BaseClass1> on type class G3_C1555`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C564`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C564`2<class BaseClass0,class BaseClass1>::ClassMethod1343<object>()
		calli default string(class G3_C1555`1<class BaseClass1>)
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G2_C564`2<class BaseClass0,class BaseClass1> on type class G3_C1555`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C564`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C564`2<class BaseClass0,class BaseClass1>::ClassMethod1342<object>()
		calli default string(class G3_C1555`1<class BaseClass1>)
		ldstr "G2_C564::ClassMethod1342.MI.10204<System.Object>()"
		ldstr "class G2_C564`2<class BaseClass0,class BaseClass1> on type class G3_C1555`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C564`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C564`2<class BaseClass0,class BaseClass1>::ClassMethod1341()
		calli default string(class G3_C1555`1<class BaseClass1>)
		ldstr "G2_C564::ClassMethod1341.MI.10203()"
		ldstr "class G2_C564`2<class BaseClass0,class BaseClass1> on type class G3_C1555`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C564`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C564`2<class BaseClass0,class BaseClass1>::ClassMethod1340()
		calli default string(class G3_C1555`1<class BaseClass1>)
		ldstr "G2_C564::ClassMethod1340.MI.10202()"
		ldstr "class G2_C564`2<class BaseClass0,class BaseClass1> on type class G3_C1555`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1555`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1555`1<class BaseClass1>::ClassMethod4506<object>()
		calli default string(class G3_C1555`1<class BaseClass1>)
		ldstr "G3_C1555::ClassMethod4506.16784<System.Object>()"
		ldstr "class G3_C1555`1<class BaseClass1> on type class G3_C1555`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1555`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1555`1<class BaseClass1>::ClassMethod4505<object>()
		calli default string(class G3_C1555`1<class BaseClass1>)
		ldstr "G3_C1555::ClassMethod4505.16783<System.Object>()"
		ldstr "class G3_C1555`1<class BaseClass1> on type class G3_C1555`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1555`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1555`1<class BaseClass1>::ClassMethod4504()
		calli default string(class G3_C1555`1<class BaseClass1>)
		ldstr "G3_C1555::ClassMethod4504.16782()"
		ldstr "class G3_C1555`1<class BaseClass1> on type class G3_C1555`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1555`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1555`1<class BaseClass1>::Method7<object>()
		calli default string(class G3_C1555`1<class BaseClass1>)
		ldstr "G3_C1555::Method7.16781<System.Object>()"
		ldstr "class G3_C1555`1<class BaseClass1> on type class G3_C1555`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1555`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1555`1<class BaseClass1>::ClassMethod2502<object>()
		calli default string(class G3_C1555`1<class BaseClass1>)
		ldstr "G2_C564::ClassMethod2502.10201<System.Object>()"
		ldstr "class G3_C1555`1<class BaseClass1> on type class G3_C1555`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1555`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1555`1<class BaseClass1>::ClassMethod2501()
		calli default string(class G3_C1555`1<class BaseClass1>)
		ldstr "G2_C564::ClassMethod2501.10200()"
		ldstr "class G3_C1555`1<class BaseClass1> on type class G3_C1555`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1555`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1555`1<class BaseClass1>::ClassMethod2500()
		calli default string(class G3_C1555`1<class BaseClass1>)
		ldstr "G3_C1555::ClassMethod2500.MI.16785()"
		ldstr "class G3_C1555`1<class BaseClass1> on type class G3_C1555`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1555`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1555`1<class BaseClass1>::Method6<object>()
		calli default string(class G3_C1555`1<class BaseClass1>)
		ldstr "G2_C564::Method6.10197<System.Object>()"
		ldstr "class G3_C1555`1<class BaseClass1> on type class G3_C1555`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1555`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1555`1<class BaseClass1>::Method5()
		calli default string(class G3_C1555`1<class BaseClass1>)
		ldstr "G2_C564::Method5.10195()"
		ldstr "class G3_C1555`1<class BaseClass1> on type class G3_C1555`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1555`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1555`1<class BaseClass1>::Method4()
		calli default string(class G3_C1555`1<class BaseClass1>)
		ldstr "G2_C564::Method4.10194()"
		ldstr "class G3_C1555`1<class BaseClass1> on type class G3_C1555`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1555`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1555`1<class BaseClass1>::ClassMethod1343<object>()
		calli default string(class G3_C1555`1<class BaseClass1>)
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G3_C1555`1<class BaseClass1> on type class G3_C1555`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1555`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1555`1<class BaseClass1>::ClassMethod1342<object>()
		calli default string(class G3_C1555`1<class BaseClass1>)
		ldstr "G2_C564::ClassMethod1342.MI.10204<System.Object>()"
		ldstr "class G3_C1555`1<class BaseClass1> on type class G3_C1555`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1555`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1555`1<class BaseClass1>::ClassMethod1341()
		calli default string(class G3_C1555`1<class BaseClass1>)
		ldstr "G2_C564::ClassMethod1341.MI.10203()"
		ldstr "class G3_C1555`1<class BaseClass1> on type class G3_C1555`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1555`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1555`1<class BaseClass1>::ClassMethod1340()
		calli default string(class G3_C1555`1<class BaseClass1>)
		ldstr "G2_C564::ClassMethod1340.MI.10202()"
		ldstr "class G3_C1555`1<class BaseClass1> on type class G3_C1555`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C564`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C10`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass0,class BaseClass0>::ClassMethod1343<object>()
		calli default string(class G2_C564`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass0> on type class G2_C564`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass0,class BaseClass0>::ClassMethod1342<object>()
		calli default string(class G2_C564`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C564::ClassMethod1342.MI.10204<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass0> on type class G2_C564`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass0,class BaseClass0>::ClassMethod1341()
		calli default string(class G2_C564`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C564::ClassMethod1341.MI.10203()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass0> on type class G2_C564`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass0,class BaseClass0>::ClassMethod1340()
		calli default string(class G2_C564`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C564::ClassMethod1340.MI.10202()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass0> on type class G2_C564`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass0,class BaseClass0>::Method6<object>()
		calli default string(class G2_C564`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C10::Method6.4848<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass0> on type class G2_C564`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass0,class BaseClass0>::Method5()
		calli default string(class G2_C564`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C10::Method5.4847()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass0> on type class G2_C564`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass0,class BaseClass0>::Method4()
		calli default string(class G2_C564`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C564::Method4.10194()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass0> on type class G2_C564`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C564`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass0> on type class G2_C564`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C564`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C564`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G2_C564`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C564::Method4.10194()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C564`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G2_C564`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C564::Method5.MI.10196()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C564`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G2_C564`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C564::Method6.MI.10198<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C564`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C564`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C564::Method7.10193<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C564`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C564`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C564`2<class BaseClass0,class BaseClass0>::ClassMethod2502<object>()
		calli default string(class G2_C564`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C564::ClassMethod2502.10201<System.Object>()"
		ldstr "class G2_C564`2<class BaseClass0,class BaseClass0> on type class G2_C564`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C564`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C564`2<class BaseClass0,class BaseClass0>::ClassMethod2501()
		calli default string(class G2_C564`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C564::ClassMethod2501.10200()"
		ldstr "class G2_C564`2<class BaseClass0,class BaseClass0> on type class G2_C564`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C564`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C564`2<class BaseClass0,class BaseClass0>::ClassMethod2500()
		calli default string(class G2_C564`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C564::ClassMethod2500.10199()"
		ldstr "class G2_C564`2<class BaseClass0,class BaseClass0> on type class G2_C564`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C564`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C564`2<class BaseClass0,class BaseClass0>::Method6<object>()
		calli default string(class G2_C564`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C564::Method6.10197<System.Object>()"
		ldstr "class G2_C564`2<class BaseClass0,class BaseClass0> on type class G2_C564`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C564`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C564`2<class BaseClass0,class BaseClass0>::Method5()
		calli default string(class G2_C564`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C564::Method5.10195()"
		ldstr "class G2_C564`2<class BaseClass0,class BaseClass0> on type class G2_C564`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C564`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C564`2<class BaseClass0,class BaseClass0>::Method4()
		calli default string(class G2_C564`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C564::Method4.10194()"
		ldstr "class G2_C564`2<class BaseClass0,class BaseClass0> on type class G2_C564`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C564`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C564`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C564`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C564::Method7.10193<System.Object>()"
		ldstr "class G2_C564`2<class BaseClass0,class BaseClass0> on type class G2_C564`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C564`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C564`2<class BaseClass0,class BaseClass0>::ClassMethod1343<object>()
		calli default string(class G2_C564`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G2_C564`2<class BaseClass0,class BaseClass0> on type class G2_C564`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C564`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C564`2<class BaseClass0,class BaseClass0>::ClassMethod1342<object>()
		calli default string(class G2_C564`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C564::ClassMethod1342.MI.10204<System.Object>()"
		ldstr "class G2_C564`2<class BaseClass0,class BaseClass0> on type class G2_C564`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C564`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C564`2<class BaseClass0,class BaseClass0>::ClassMethod1341()
		calli default string(class G2_C564`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C564::ClassMethod1341.MI.10203()"
		ldstr "class G2_C564`2<class BaseClass0,class BaseClass0> on type class G2_C564`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C564`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C564`2<class BaseClass0,class BaseClass0>::ClassMethod1340()
		calli default string(class G2_C564`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C564::ClassMethod1340.MI.10202()"
		ldstr "class G2_C564`2<class BaseClass0,class BaseClass0> on type class G2_C564`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C564`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C564::Method7.10193<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C564`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C564`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass1,class BaseClass1>::ClassMethod1343<object>()
		calli default string(class G2_C564`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G2_C564`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass1,class BaseClass1>::ClassMethod1342<object>()
		calli default string(class G2_C564`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C564::ClassMethod1342.MI.10204<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G2_C564`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass1,class BaseClass1>::ClassMethod1341()
		calli default string(class G2_C564`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C564::ClassMethod1341.MI.10203()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G2_C564`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass1,class BaseClass1>::ClassMethod1340()
		calli default string(class G2_C564`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C564::ClassMethod1340.MI.10202()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G2_C564`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass1,class BaseClass1>::Method6<object>()
		calli default string(class G2_C564`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C10::Method6.4848<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G2_C564`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass1,class BaseClass1>::Method5()
		calli default string(class G2_C564`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C10::Method5.4847()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G2_C564`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass1,class BaseClass1>::Method4()
		calli default string(class G2_C564`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C564::Method4.10194()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G2_C564`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C564`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G2_C564`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C564`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C564`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G2_C564`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C564::Method4.10194()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C564`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G2_C564`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C564::Method5.MI.10196()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C564`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G2_C564`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C564::Method6.MI.10198<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C564`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C564`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C564::Method7.10193<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C564`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G2_C564`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C564::Method4.10194()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C564`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G2_C564`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C564::Method5.MI.10196()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C564`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G2_C564`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C564::Method6.MI.10198<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C564`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C564`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C564`2<class BaseClass0,class BaseClass1>::ClassMethod2502<object>()
		calli default string(class G2_C564`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C564::ClassMethod2502.10201<System.Object>()"
		ldstr "class G2_C564`2<class BaseClass0,class BaseClass1> on type class G2_C564`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C564`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C564`2<class BaseClass0,class BaseClass1>::ClassMethod2501()
		calli default string(class G2_C564`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C564::ClassMethod2501.10200()"
		ldstr "class G2_C564`2<class BaseClass0,class BaseClass1> on type class G2_C564`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C564`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C564`2<class BaseClass0,class BaseClass1>::ClassMethod2500()
		calli default string(class G2_C564`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C564::ClassMethod2500.10199()"
		ldstr "class G2_C564`2<class BaseClass0,class BaseClass1> on type class G2_C564`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C564`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C564`2<class BaseClass0,class BaseClass1>::Method6<object>()
		calli default string(class G2_C564`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C564::Method6.10197<System.Object>()"
		ldstr "class G2_C564`2<class BaseClass0,class BaseClass1> on type class G2_C564`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C564`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C564`2<class BaseClass0,class BaseClass1>::Method5()
		calli default string(class G2_C564`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C564::Method5.10195()"
		ldstr "class G2_C564`2<class BaseClass0,class BaseClass1> on type class G2_C564`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C564`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C564`2<class BaseClass0,class BaseClass1>::Method4()
		calli default string(class G2_C564`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C564::Method4.10194()"
		ldstr "class G2_C564`2<class BaseClass0,class BaseClass1> on type class G2_C564`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C564`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C564`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C564`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C564::Method7.10193<System.Object>()"
		ldstr "class G2_C564`2<class BaseClass0,class BaseClass1> on type class G2_C564`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C564`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C564`2<class BaseClass0,class BaseClass1>::ClassMethod1343<object>()
		calli default string(class G2_C564`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G2_C564`2<class BaseClass0,class BaseClass1> on type class G2_C564`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C564`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C564`2<class BaseClass0,class BaseClass1>::ClassMethod1342<object>()
		calli default string(class G2_C564`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C564::ClassMethod1342.MI.10204<System.Object>()"
		ldstr "class G2_C564`2<class BaseClass0,class BaseClass1> on type class G2_C564`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C564`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C564`2<class BaseClass0,class BaseClass1>::ClassMethod1341()
		calli default string(class G2_C564`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C564::ClassMethod1341.MI.10203()"
		ldstr "class G2_C564`2<class BaseClass0,class BaseClass1> on type class G2_C564`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C564`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C564`2<class BaseClass0,class BaseClass1>::ClassMethod1340()
		calli default string(class G2_C564`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C564::ClassMethod1340.MI.10202()"
		ldstr "class G2_C564`2<class BaseClass0,class BaseClass1> on type class G2_C564`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C564`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C10`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass0,class BaseClass0>::ClassMethod1343<object>()
		calli default string(class G2_C564`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass0> on type class G2_C564`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass0,class BaseClass0>::ClassMethod1342<object>()
		calli default string(class G2_C564`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C564::ClassMethod1342.MI.10204<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass0> on type class G2_C564`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass0,class BaseClass0>::ClassMethod1341()
		calli default string(class G2_C564`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C564::ClassMethod1341.MI.10203()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass0> on type class G2_C564`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass0,class BaseClass0>::ClassMethod1340()
		calli default string(class G2_C564`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C564::ClassMethod1340.MI.10202()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass0> on type class G2_C564`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass0,class BaseClass0>::Method6<object>()
		calli default string(class G2_C564`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C10::Method6.4848<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass0> on type class G2_C564`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass0,class BaseClass0>::Method5()
		calli default string(class G2_C564`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C10::Method5.4847()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass0> on type class G2_C564`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass0,class BaseClass0>::Method4()
		calli default string(class G2_C564`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C564::Method4.10194()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass0> on type class G2_C564`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C564`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass0> on type class G2_C564`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C564`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C564`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G2_C564`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C564::Method4.10194()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C564`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G2_C564`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C564::Method5.MI.10196()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C564`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G2_C564`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C564::Method6.MI.10198<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C564`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C564`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C564::Method7.10193<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C564`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C564`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C564`2<class BaseClass1,class BaseClass0>::ClassMethod2502<object>()
		calli default string(class G2_C564`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C564::ClassMethod2502.10201<System.Object>()"
		ldstr "class G2_C564`2<class BaseClass1,class BaseClass0> on type class G2_C564`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C564`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C564`2<class BaseClass1,class BaseClass0>::ClassMethod2501()
		calli default string(class G2_C564`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C564::ClassMethod2501.10200()"
		ldstr "class G2_C564`2<class BaseClass1,class BaseClass0> on type class G2_C564`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C564`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C564`2<class BaseClass1,class BaseClass0>::ClassMethod2500()
		calli default string(class G2_C564`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C564::ClassMethod2500.10199()"
		ldstr "class G2_C564`2<class BaseClass1,class BaseClass0> on type class G2_C564`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C564`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C564`2<class BaseClass1,class BaseClass0>::Method6<object>()
		calli default string(class G2_C564`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C564::Method6.10197<System.Object>()"
		ldstr "class G2_C564`2<class BaseClass1,class BaseClass0> on type class G2_C564`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C564`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C564`2<class BaseClass1,class BaseClass0>::Method5()
		calli default string(class G2_C564`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C564::Method5.10195()"
		ldstr "class G2_C564`2<class BaseClass1,class BaseClass0> on type class G2_C564`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C564`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C564`2<class BaseClass1,class BaseClass0>::Method4()
		calli default string(class G2_C564`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C564::Method4.10194()"
		ldstr "class G2_C564`2<class BaseClass1,class BaseClass0> on type class G2_C564`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C564`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C564`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G2_C564`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C564::Method7.10193<System.Object>()"
		ldstr "class G2_C564`2<class BaseClass1,class BaseClass0> on type class G2_C564`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C564`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C564`2<class BaseClass1,class BaseClass0>::ClassMethod1343<object>()
		calli default string(class G2_C564`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G2_C564`2<class BaseClass1,class BaseClass0> on type class G2_C564`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C564`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C564`2<class BaseClass1,class BaseClass0>::ClassMethod1342<object>()
		calli default string(class G2_C564`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C564::ClassMethod1342.MI.10204<System.Object>()"
		ldstr "class G2_C564`2<class BaseClass1,class BaseClass0> on type class G2_C564`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C564`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C564`2<class BaseClass1,class BaseClass0>::ClassMethod1341()
		calli default string(class G2_C564`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C564::ClassMethod1341.MI.10203()"
		ldstr "class G2_C564`2<class BaseClass1,class BaseClass0> on type class G2_C564`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C564`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C564`2<class BaseClass1,class BaseClass0>::ClassMethod1340()
		calli default string(class G2_C564`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C564::ClassMethod1340.MI.10202()"
		ldstr "class G2_C564`2<class BaseClass1,class BaseClass0> on type class G2_C564`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C564`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C564::Method7.10193<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C564`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C564`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass1,class BaseClass1>::ClassMethod1343<object>()
		calli default string(class G2_C564`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G2_C564`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass1,class BaseClass1>::ClassMethod1342<object>()
		calli default string(class G2_C564`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C564::ClassMethod1342.MI.10204<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G2_C564`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass1,class BaseClass1>::ClassMethod1341()
		calli default string(class G2_C564`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C564::ClassMethod1341.MI.10203()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G2_C564`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass1,class BaseClass1>::ClassMethod1340()
		calli default string(class G2_C564`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C564::ClassMethod1340.MI.10202()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G2_C564`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass1,class BaseClass1>::Method6<object>()
		calli default string(class G2_C564`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C10::Method6.4848<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G2_C564`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass1,class BaseClass1>::Method5()
		calli default string(class G2_C564`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C10::Method5.4847()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G2_C564`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass1,class BaseClass1>::Method4()
		calli default string(class G2_C564`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C564::Method4.10194()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G2_C564`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C564`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G2_C564`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C564`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C564`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G2_C564`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C564::Method4.10194()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C564`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G2_C564`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C564::Method5.MI.10196()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C564`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G2_C564`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C564::Method6.MI.10198<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C564`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C564`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C564::Method7.10193<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C564`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G2_C564`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C564::Method4.10194()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C564`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G2_C564`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C564::Method5.MI.10196()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C564`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G2_C564`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C564::Method6.MI.10198<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C564`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C564`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C564`2<class BaseClass1,class BaseClass1>::ClassMethod2502<object>()
		calli default string(class G2_C564`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C564::ClassMethod2502.10201<System.Object>()"
		ldstr "class G2_C564`2<class BaseClass1,class BaseClass1> on type class G2_C564`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C564`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C564`2<class BaseClass1,class BaseClass1>::ClassMethod2501()
		calli default string(class G2_C564`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C564::ClassMethod2501.10200()"
		ldstr "class G2_C564`2<class BaseClass1,class BaseClass1> on type class G2_C564`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C564`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C564`2<class BaseClass1,class BaseClass1>::ClassMethod2500()
		calli default string(class G2_C564`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C564::ClassMethod2500.10199()"
		ldstr "class G2_C564`2<class BaseClass1,class BaseClass1> on type class G2_C564`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C564`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C564`2<class BaseClass1,class BaseClass1>::Method6<object>()
		calli default string(class G2_C564`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C564::Method6.10197<System.Object>()"
		ldstr "class G2_C564`2<class BaseClass1,class BaseClass1> on type class G2_C564`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C564`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C564`2<class BaseClass1,class BaseClass1>::Method5()
		calli default string(class G2_C564`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C564::Method5.10195()"
		ldstr "class G2_C564`2<class BaseClass1,class BaseClass1> on type class G2_C564`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C564`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C564`2<class BaseClass1,class BaseClass1>::Method4()
		calli default string(class G2_C564`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C564::Method4.10194()"
		ldstr "class G2_C564`2<class BaseClass1,class BaseClass1> on type class G2_C564`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C564`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C564`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C564`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C564::Method7.10193<System.Object>()"
		ldstr "class G2_C564`2<class BaseClass1,class BaseClass1> on type class G2_C564`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C564`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C564`2<class BaseClass1,class BaseClass1>::ClassMethod1343<object>()
		calli default string(class G2_C564`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G2_C564`2<class BaseClass1,class BaseClass1> on type class G2_C564`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C564`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C564`2<class BaseClass1,class BaseClass1>::ClassMethod1342<object>()
		calli default string(class G2_C564`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C564::ClassMethod1342.MI.10204<System.Object>()"
		ldstr "class G2_C564`2<class BaseClass1,class BaseClass1> on type class G2_C564`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C564`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C564`2<class BaseClass1,class BaseClass1>::ClassMethod1341()
		calli default string(class G2_C564`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C564::ClassMethod1341.MI.10203()"
		ldstr "class G2_C564`2<class BaseClass1,class BaseClass1> on type class G2_C564`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C564`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C564`2<class BaseClass1,class BaseClass1>::ClassMethod1340()
		calli default string(class G2_C564`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C564::ClassMethod1340.MI.10202()"
		ldstr "class G2_C564`2<class BaseClass1,class BaseClass1> on type class G2_C564`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G1_C10`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C10`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass0,class BaseClass0>::ClassMethod1343<object>()
		calli default string(class G1_C10`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass0> on type class G1_C10`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass0,class BaseClass0>::ClassMethod1342<object>()
		calli default string(class G1_C10`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C10::ClassMethod1342.4852<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass0> on type class G1_C10`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass0,class BaseClass0>::ClassMethod1341()
		calli default string(class G1_C10`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass0> on type class G1_C10`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass0,class BaseClass0>::ClassMethod1340()
		calli default string(class G1_C10`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass0> on type class G1_C10`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass0,class BaseClass0>::Method6<object>()
		calli default string(class G1_C10`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C10::Method6.4848<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass0> on type class G1_C10`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass0,class BaseClass0>::Method5()
		calli default string(class G1_C10`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C10::Method5.4847()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass0> on type class G1_C10`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass0,class BaseClass0>::Method4()
		calli default string(class G1_C10`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C10::Method4.4845()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass0> on type class G1_C10`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G1_C10`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass0> on type class G1_C10`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G1_C10`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G1_C10`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G1_C10`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C10::Method4.MI.4846()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C10`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G1_C10`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C10::Method5.4847()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C10`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G1_C10`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C10::Method6.MI.4849<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C10`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G1_C10`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G1_C10`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G1_C10`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass0,class BaseClass1>::ClassMethod1343<object>()
		calli default string(class G1_C10`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G1_C10`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass0,class BaseClass1>::ClassMethod1342<object>()
		calli default string(class G1_C10`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C10::ClassMethod1342.4852<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G1_C10`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass0,class BaseClass1>::ClassMethod1341()
		calli default string(class G1_C10`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G1_C10`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass0,class BaseClass1>::ClassMethod1340()
		calli default string(class G1_C10`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G1_C10`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass0,class BaseClass1>::Method6<object>()
		calli default string(class G1_C10`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C10::Method6.4848<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G1_C10`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass0,class BaseClass1>::Method5()
		calli default string(class G1_C10`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C10::Method5.4847()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G1_C10`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass0,class BaseClass1>::Method4()
		calli default string(class G1_C10`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C10::Method4.4845()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G1_C10`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G1_C10`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G1_C10`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G1_C10`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G1_C10`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G1_C10`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C10::Method4.MI.4846()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C10`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G1_C10`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C10::Method5.4847()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C10`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G1_C10`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C10::Method6.MI.4849<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C10`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G1_C10`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G1_C10`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G1_C10`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C10`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass1,class BaseClass0>::ClassMethod1343<object>()
		calli default string(class G1_C10`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass0> on type class G1_C10`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass1,class BaseClass0>::ClassMethod1342<object>()
		calli default string(class G1_C10`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C10::ClassMethod1342.4852<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass0> on type class G1_C10`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass1,class BaseClass0>::ClassMethod1341()
		calli default string(class G1_C10`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass0> on type class G1_C10`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass1,class BaseClass0>::ClassMethod1340()
		calli default string(class G1_C10`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass0> on type class G1_C10`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass1,class BaseClass0>::Method6<object>()
		calli default string(class G1_C10`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C10::Method6.4848<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass0> on type class G1_C10`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass1,class BaseClass0>::Method5()
		calli default string(class G1_C10`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C10::Method5.4847()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass0> on type class G1_C10`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass1,class BaseClass0>::Method4()
		calli default string(class G1_C10`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C10::Method4.4845()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass0> on type class G1_C10`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G1_C10`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass0> on type class G1_C10`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G1_C10`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G1_C10`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G1_C10`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C10::Method4.MI.4846()"
		ldstr "class IBase1`1<class BaseClass1> on type class G1_C10`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G1_C10`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C10::Method5.4847()"
		ldstr "class IBase1`1<class BaseClass1> on type class G1_C10`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G1_C10`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C10::Method6.MI.4849<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G1_C10`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G1_C10`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G1_C10`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G1_C10`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C10::Method4.MI.4846()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C10`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G1_C10`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C10::Method5.4847()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C10`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G1_C10`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C10::Method6.MI.4849<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C10`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G1_C10`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass1,class BaseClass1>::ClassMethod1343<object>()
		calli default string(class G1_C10`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G1_C10`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass1,class BaseClass1>::ClassMethod1342<object>()
		calli default string(class G1_C10`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C10::ClassMethod1342.4852<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G1_C10`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass1,class BaseClass1>::ClassMethod1341()
		calli default string(class G1_C10`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G1_C10`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass1,class BaseClass1>::ClassMethod1340()
		calli default string(class G1_C10`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G1_C10`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass1,class BaseClass1>::Method6<object>()
		calli default string(class G1_C10`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C10::Method6.4848<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G1_C10`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass1,class BaseClass1>::Method5()
		calli default string(class G1_C10`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C10::Method5.4847()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G1_C10`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass1,class BaseClass1>::Method4()
		calli default string(class G1_C10`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C10::Method4.4845()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G1_C10`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G1_C10`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G1_C10`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G1_C10`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G1_C10`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G1_C10`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C10::Method4.MI.4846()"
		ldstr "class IBase1`1<class BaseClass1> on type class G1_C10`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G1_C10`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C10::Method5.4847()"
		ldstr "class IBase1`1<class BaseClass1> on type class G1_C10`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G1_C10`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C10::Method6.MI.4849<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G1_C10`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G1_C10`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G1_C10`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G1_C10`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C10::Method4.MI.4846()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C10`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G1_C10`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C10::Method5.4847()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C10`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G1_C10`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C10::Method6.MI.4849<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C10`2<class BaseClass1,class BaseClass1>"
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
		call void Generated1083::MethodCallingTest()
		call void Generated1083::ConstrainedCallsTest()
		call void Generated1083::StructConstrainedInterfaceCallsTest()
		call void Generated1083::CalliTest()
		ldc.i4 100
		ret
	}
}
