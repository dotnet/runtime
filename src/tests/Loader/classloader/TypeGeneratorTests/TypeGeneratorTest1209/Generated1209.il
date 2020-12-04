// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) .ver 4:0:0:0 }
.assembly extern TestFramework { .publickeytoken = ( B0 3F 5F 7F 11 D5 0A 3A )  }

//TYPES IN FORWARDER ASSEMBLIES:

//TEST ASSEMBLY:
.assembly Generated1209 { .hash algorithm 0x00008004 }
.module Generated1209.exe

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
.class public G3_C1681`1<T0> 
		extends class G2_C669`2<class BaseClass1,class BaseClass0>
		implements class IBase1`1<class BaseClass1> 
{
	.method public hidebysig virtual instance string Method4() cil managed noinlining { 
		ldstr "G3_C1681::Method4.17422()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class BaseClass1>.Method4'() cil managed noinlining { 
		.override method instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G3_C1681::Method4.MI.17423()"
		ret
	}
	.method public hidebysig virtual instance string Method5() cil managed noinlining { 
		ldstr "G3_C1681::Method5.17424()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G3_C1681::Method6.17425<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod4757() cil managed noinlining { 
		ldstr "G3_C1681::ClassMethod4757.17426()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod4758() cil managed noinlining { 
		ldstr "G3_C1681::ClassMethod4758.17427()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod4759<M0>() cil managed noinlining { 
		ldstr "G3_C1681::ClassMethod4759.17428<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod4760<M0>() cil managed noinlining { 
		ldstr "G3_C1681::ClassMethod4760.17429<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class G2_C669`2<class BaseClass1,class BaseClass0>::.ctor()
		ret
	}
}
.class public abstract G2_C669`2<T0, T1> 
		extends class G1_C12`2<!T0,class BaseClass1>
		implements class IBase2`2<!T1,!T1>, class IBase1`1<class BaseClass1> 
{
	.method public hidebysig virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G2_C669::Method7.11155<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig virtual instance string Method4() cil managed noinlining { 
		ldstr "G2_C669::Method4.11156()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class BaseClass1>.Method4'() cil managed noinlining { 
		.override method instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G2_C669::Method4.MI.11157()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method5() cil managed noinlining { 
		ldstr "G2_C669::Method5.11158()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class BaseClass1>.Method5'() cil managed noinlining { 
		.override method instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G2_C669::Method5.MI.11159()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G2_C669::Method6.11160<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class BaseClass1>.Method6'<M0>() cil managed noinlining { 
		.override method instance string class IBase1`1<class BaseClass1>::Method6<[1]>()
		ldstr "G2_C669::Method6.MI.11161<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod2699<M0>() cil managed noinlining { 
		ldstr "G2_C669::ClassMethod2699.11162<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C12<T0,class BaseClass1>.ClassMethod1347'<M0>() cil managed noinlining { 
		.override method instance string class G1_C12`2<!T0,class BaseClass1>::ClassMethod1347<[1]>()
		ldstr "G2_C669::ClassMethod1347.MI.11163<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class G1_C12`2<!T0,class BaseClass1>::.ctor()
		ret
	}
}
.class interface public abstract IBase1`1<+T0> 
{
	.method public hidebysig newslot abstract virtual instance string Method4() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method5() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method6<M0>() cil managed { }
}
.class public G1_C12`2<T0, T1> 
		implements class IBase2`2<!T0,!T1>, class IBase1`1<class BaseClass0> 
{
	.method public hidebysig virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G1_C12::Method7.4863<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase2<T0,T1>.Method7'<M0>() cil managed noinlining { 
		.override method instance string class IBase2`2<!T0,!T1>::Method7<[1]>()
		ldstr "G1_C12::Method7.MI.4864<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string Method4() cil managed noinlining { 
		ldstr "G1_C12::Method4.4865()"
		ret
	}
	.method public hidebysig virtual instance string Method5() cil managed noinlining { 
		ldstr "G1_C12::Method5.4866()"
		ret
	}
	.method public hidebysig virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G1_C12::Method6.4867<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class BaseClass0>.Method6'<M0>() cil managed noinlining { 
		.override method instance string class IBase1`1<class BaseClass0>::Method6<[1]>()
		ldstr "G1_C12::Method6.MI.4868<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1346<M0>() cil managed noinlining { 
		ldstr "G1_C12::ClassMethod1346.4869<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1347<M0>() cil managed noinlining { 
		ldstr "G1_C12::ClassMethod1347.4870<"
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
.class interface public abstract IBase2`2<+T0, -T1> 
{
	.method public hidebysig newslot abstract virtual instance string Method7<M0>() cil managed { }
}
.class private auto ansi beforefieldinit Framework {
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
	.method static void M.G3_C1681.T<T0,(class G3_C1681`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1681.T<T0,(class G3_C1681`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1681`1<!!T0>::ClassMethod1346<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1681`1<!!T0>::ClassMethod1347<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1681`1<!!T0>::ClassMethod2699<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1681`1<!!T0>::ClassMethod4757()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1681`1<!!T0>::ClassMethod4758()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1681`1<!!T0>::ClassMethod4759<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1681`1<!!T0>::ClassMethod4760<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1681`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1681`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1681`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1681`1<!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1681.A<(class G3_C1681`1<class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1681.A<(class G3_C1681`1<class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1681`1<class BaseClass0>::ClassMethod1346<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1681`1<class BaseClass0>::ClassMethod1347<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1681`1<class BaseClass0>::ClassMethod2699<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1681`1<class BaseClass0>::ClassMethod4757()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1681`1<class BaseClass0>::ClassMethod4758()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1681`1<class BaseClass0>::ClassMethod4759<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1681`1<class BaseClass0>::ClassMethod4760<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1681`1<class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1681`1<class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1681`1<class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1681`1<class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1681.B<(class G3_C1681`1<class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1681.B<(class G3_C1681`1<class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1681`1<class BaseClass1>::ClassMethod1346<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1681`1<class BaseClass1>::ClassMethod1347<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1681`1<class BaseClass1>::ClassMethod2699<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1681`1<class BaseClass1>::ClassMethod4757()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1681`1<class BaseClass1>::ClassMethod4758()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1681`1<class BaseClass1>::ClassMethod4759<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1681`1<class BaseClass1>::ClassMethod4760<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1681`1<class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1681`1<class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1681`1<class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1681`1<class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C669.T.T<T0,T1,(class G2_C669`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C669.T.T<T0,T1,(class G2_C669`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C669`2<!!T0,!!T1>::ClassMethod1346<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C669`2<!!T0,!!T1>::ClassMethod1347<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C669`2<!!T0,!!T1>::ClassMethod2699<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C669`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C669`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C669`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C669`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C669.A.T<T1,(class G2_C669`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C669.A.T<T1,(class G2_C669`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C669`2<class BaseClass0,!!T1>::ClassMethod1346<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C669`2<class BaseClass0,!!T1>::ClassMethod1347<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C669`2<class BaseClass0,!!T1>::ClassMethod2699<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C669`2<class BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C669`2<class BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C669`2<class BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C669`2<class BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C669.A.A<(class G2_C669`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C669.A.A<(class G2_C669`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C669`2<class BaseClass0,class BaseClass0>::ClassMethod1346<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C669`2<class BaseClass0,class BaseClass0>::ClassMethod1347<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C669`2<class BaseClass0,class BaseClass0>::ClassMethod2699<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C669`2<class BaseClass0,class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C669`2<class BaseClass0,class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C669`2<class BaseClass0,class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C669`2<class BaseClass0,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C669.A.B<(class G2_C669`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C669.A.B<(class G2_C669`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C669`2<class BaseClass0,class BaseClass1>::ClassMethod1346<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C669`2<class BaseClass0,class BaseClass1>::ClassMethod1347<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C669`2<class BaseClass0,class BaseClass1>::ClassMethod2699<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C669`2<class BaseClass0,class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C669`2<class BaseClass0,class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C669`2<class BaseClass0,class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C669`2<class BaseClass0,class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C669.B.T<T1,(class G2_C669`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C669.B.T<T1,(class G2_C669`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C669`2<class BaseClass1,!!T1>::ClassMethod1346<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C669`2<class BaseClass1,!!T1>::ClassMethod1347<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C669`2<class BaseClass1,!!T1>::ClassMethod2699<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C669`2<class BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C669`2<class BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C669`2<class BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C669`2<class BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C669.B.A<(class G2_C669`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C669.B.A<(class G2_C669`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C669`2<class BaseClass1,class BaseClass0>::ClassMethod1346<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C669`2<class BaseClass1,class BaseClass0>::ClassMethod1347<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C669`2<class BaseClass1,class BaseClass0>::ClassMethod2699<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C669`2<class BaseClass1,class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C669`2<class BaseClass1,class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C669`2<class BaseClass1,class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C669`2<class BaseClass1,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C669.B.B<(class G2_C669`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C669.B.B<(class G2_C669`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C669`2<class BaseClass1,class BaseClass1>::ClassMethod1346<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C669`2<class BaseClass1,class BaseClass1>::ClassMethod1347<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C669`2<class BaseClass1,class BaseClass1>::ClassMethod2699<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C669`2<class BaseClass1,class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C669`2<class BaseClass1,class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C669`2<class BaseClass1,class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C669`2<class BaseClass1,class BaseClass1>::Method7<object>()
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
	.method static void M.G1_C12.T.T<T0,T1,(class G1_C12`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C12.T.T<T0,T1,(class G1_C12`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C12`2<!!T0,!!T1>::ClassMethod1346<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C12`2<!!T0,!!T1>::ClassMethod1347<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C12`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C12`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C12`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C12`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C12.A.T<T1,(class G1_C12`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C12.A.T<T1,(class G1_C12`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C12`2<class BaseClass0,!!T1>::ClassMethod1346<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C12`2<class BaseClass0,!!T1>::ClassMethod1347<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C12`2<class BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C12`2<class BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C12`2<class BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C12`2<class BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C12.A.A<(class G1_C12`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C12.A.A<(class G1_C12`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C12`2<class BaseClass0,class BaseClass0>::ClassMethod1346<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C12`2<class BaseClass0,class BaseClass0>::ClassMethod1347<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C12`2<class BaseClass0,class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C12`2<class BaseClass0,class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C12`2<class BaseClass0,class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C12`2<class BaseClass0,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C12.A.B<(class G1_C12`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C12.A.B<(class G1_C12`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C12`2<class BaseClass0,class BaseClass1>::ClassMethod1346<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C12`2<class BaseClass0,class BaseClass1>::ClassMethod1347<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C12`2<class BaseClass0,class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C12`2<class BaseClass0,class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C12`2<class BaseClass0,class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C12`2<class BaseClass0,class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C12.B.T<T1,(class G1_C12`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C12.B.T<T1,(class G1_C12`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C12`2<class BaseClass1,!!T1>::ClassMethod1346<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C12`2<class BaseClass1,!!T1>::ClassMethod1347<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C12`2<class BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C12`2<class BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C12`2<class BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C12`2<class BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C12.B.A<(class G1_C12`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C12.B.A<(class G1_C12`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C12`2<class BaseClass1,class BaseClass0>::ClassMethod1346<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C12`2<class BaseClass1,class BaseClass0>::ClassMethod1347<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C12`2<class BaseClass1,class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C12`2<class BaseClass1,class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C12`2<class BaseClass1,class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C12`2<class BaseClass1,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C12.B.B<(class G1_C12`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C12.B.B<(class G1_C12`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C12`2<class BaseClass1,class BaseClass1>::ClassMethod1346<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C12`2<class BaseClass1,class BaseClass1>::ClassMethod1347<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C12`2<class BaseClass1,class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C12`2<class BaseClass1,class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C12`2<class BaseClass1,class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C12`2<class BaseClass1,class BaseClass1>::Method7<object>()
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
	.method public hidebysig static void MethodCallingTest() cil managed
	{
		.maxstack  10
		.locals init (object V_0)
		ldstr "========================== Method Calling Test =========================="
		call void [mscorlib]System.Console::WriteLine(string)
		newobj instance void class G3_C1681`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass1,class BaseClass1>::ClassMethod1347<object>()
		ldstr "G2_C669::ClassMethod1347.MI.11163<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G3_C1681`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass1,class BaseClass1>::ClassMethod1346<object>()
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G3_C1681`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass1,class BaseClass1>::Method6<object>()
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G3_C1681`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass1,class BaseClass1>::Method5()
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G3_C1681`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass1,class BaseClass1>::Method4()
		ldstr "G3_C1681::Method4.17422()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G3_C1681`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C669::Method7.11155<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G3_C1681`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1681`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G3_C1681::Method4.MI.17423()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1681`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G3_C1681::Method5.17424()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1681`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G3_C1681::Method6.17425<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1681`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C669::Method7.11155<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1681`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C669`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C669`2<class BaseClass1,class BaseClass0>::ClassMethod2699<object>()
		ldstr "G2_C669::ClassMethod2699.11162<System.Object>()"
		ldstr "class G2_C669`2<class BaseClass1,class BaseClass0> on type class G3_C1681`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C669`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C669`2<class BaseClass1,class BaseClass0>::Method6<object>()
		ldstr "G2_C669::Method6.11160<System.Object>()"
		ldstr "class G2_C669`2<class BaseClass1,class BaseClass0> on type class G3_C1681`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C669`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C669`2<class BaseClass1,class BaseClass0>::Method5()
		ldstr "G3_C1681::Method5.17424()"
		ldstr "class G2_C669`2<class BaseClass1,class BaseClass0> on type class G3_C1681`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C669`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C669`2<class BaseClass1,class BaseClass0>::Method4()
		ldstr "G3_C1681::Method4.17422()"
		ldstr "class G2_C669`2<class BaseClass1,class BaseClass0> on type class G3_C1681`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C669`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C669`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G2_C669::Method7.11155<System.Object>()"
		ldstr "class G2_C669`2<class BaseClass1,class BaseClass0> on type class G3_C1681`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C669`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C669`2<class BaseClass1,class BaseClass0>::ClassMethod1347<object>()
		ldstr "G2_C669::ClassMethod1347.MI.11163<System.Object>()"
		ldstr "class G2_C669`2<class BaseClass1,class BaseClass0> on type class G3_C1681`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C669`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C669`2<class BaseClass1,class BaseClass0>::ClassMethod1346<object>()
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()"
		ldstr "class G2_C669`2<class BaseClass1,class BaseClass0> on type class G3_C1681`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C669::Method7.11155<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1681`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G3_C1681::Method4.MI.17423()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1681`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G3_C1681::Method5.17424()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1681`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G3_C1681::Method6.17425<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1681`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G3_C1681`1<class BaseClass0>
		callvirt instance string class G3_C1681`1<class BaseClass0>::ClassMethod4760<object>()
		ldstr "G3_C1681::ClassMethod4760.17429<System.Object>()"
		ldstr "class G3_C1681`1<class BaseClass0> on type class G3_C1681`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1681`1<class BaseClass0>
		callvirt instance string class G3_C1681`1<class BaseClass0>::ClassMethod4759<object>()
		ldstr "G3_C1681::ClassMethod4759.17428<System.Object>()"
		ldstr "class G3_C1681`1<class BaseClass0> on type class G3_C1681`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1681`1<class BaseClass0>
		callvirt instance string class G3_C1681`1<class BaseClass0>::ClassMethod4758()
		ldstr "G3_C1681::ClassMethod4758.17427()"
		ldstr "class G3_C1681`1<class BaseClass0> on type class G3_C1681`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1681`1<class BaseClass0>
		callvirt instance string class G3_C1681`1<class BaseClass0>::ClassMethod4757()
		ldstr "G3_C1681::ClassMethod4757.17426()"
		ldstr "class G3_C1681`1<class BaseClass0> on type class G3_C1681`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1681`1<class BaseClass0>
		callvirt instance string class G3_C1681`1<class BaseClass0>::Method6<object>()
		ldstr "G3_C1681::Method6.17425<System.Object>()"
		ldstr "class G3_C1681`1<class BaseClass0> on type class G3_C1681`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1681`1<class BaseClass0>
		callvirt instance string class G3_C1681`1<class BaseClass0>::Method5()
		ldstr "G3_C1681::Method5.17424()"
		ldstr "class G3_C1681`1<class BaseClass0> on type class G3_C1681`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1681`1<class BaseClass0>
		callvirt instance string class G3_C1681`1<class BaseClass0>::Method4()
		ldstr "G3_C1681::Method4.17422()"
		ldstr "class G3_C1681`1<class BaseClass0> on type class G3_C1681`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1681`1<class BaseClass0>
		callvirt instance string class G3_C1681`1<class BaseClass0>::ClassMethod2699<object>()
		ldstr "G2_C669::ClassMethod2699.11162<System.Object>()"
		ldstr "class G3_C1681`1<class BaseClass0> on type class G3_C1681`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1681`1<class BaseClass0>
		callvirt instance string class G3_C1681`1<class BaseClass0>::Method7<object>()
		ldstr "G2_C669::Method7.11155<System.Object>()"
		ldstr "class G3_C1681`1<class BaseClass0> on type class G3_C1681`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1681`1<class BaseClass0>
		callvirt instance string class G3_C1681`1<class BaseClass0>::ClassMethod1347<object>()
		ldstr "G2_C669::ClassMethod1347.MI.11163<System.Object>()"
		ldstr "class G3_C1681`1<class BaseClass0> on type class G3_C1681`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1681`1<class BaseClass0>
		callvirt instance string class G3_C1681`1<class BaseClass0>::ClassMethod1346<object>()
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()"
		ldstr "class G3_C1681`1<class BaseClass0> on type class G3_C1681`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G3_C1681`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass1,class BaseClass1>::ClassMethod1347<object>()
		ldstr "G2_C669::ClassMethod1347.MI.11163<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G3_C1681`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass1,class BaseClass1>::ClassMethod1346<object>()
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G3_C1681`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass1,class BaseClass1>::Method6<object>()
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G3_C1681`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass1,class BaseClass1>::Method5()
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G3_C1681`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass1,class BaseClass1>::Method4()
		ldstr "G3_C1681::Method4.17422()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G3_C1681`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C669::Method7.11155<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G3_C1681`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1681`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G3_C1681::Method4.MI.17423()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1681`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G3_C1681::Method5.17424()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1681`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G3_C1681::Method6.17425<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1681`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C669::Method7.11155<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1681`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C669`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C669`2<class BaseClass1,class BaseClass0>::ClassMethod2699<object>()
		ldstr "G2_C669::ClassMethod2699.11162<System.Object>()"
		ldstr "class G2_C669`2<class BaseClass1,class BaseClass0> on type class G3_C1681`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C669`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C669`2<class BaseClass1,class BaseClass0>::Method6<object>()
		ldstr "G2_C669::Method6.11160<System.Object>()"
		ldstr "class G2_C669`2<class BaseClass1,class BaseClass0> on type class G3_C1681`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C669`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C669`2<class BaseClass1,class BaseClass0>::Method5()
		ldstr "G3_C1681::Method5.17424()"
		ldstr "class G2_C669`2<class BaseClass1,class BaseClass0> on type class G3_C1681`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C669`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C669`2<class BaseClass1,class BaseClass0>::Method4()
		ldstr "G3_C1681::Method4.17422()"
		ldstr "class G2_C669`2<class BaseClass1,class BaseClass0> on type class G3_C1681`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C669`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C669`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G2_C669::Method7.11155<System.Object>()"
		ldstr "class G2_C669`2<class BaseClass1,class BaseClass0> on type class G3_C1681`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C669`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C669`2<class BaseClass1,class BaseClass0>::ClassMethod1347<object>()
		ldstr "G2_C669::ClassMethod1347.MI.11163<System.Object>()"
		ldstr "class G2_C669`2<class BaseClass1,class BaseClass0> on type class G3_C1681`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C669`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C669`2<class BaseClass1,class BaseClass0>::ClassMethod1346<object>()
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()"
		ldstr "class G2_C669`2<class BaseClass1,class BaseClass0> on type class G3_C1681`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C669::Method7.11155<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1681`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G3_C1681::Method4.MI.17423()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1681`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G3_C1681::Method5.17424()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1681`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G3_C1681::Method6.17425<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1681`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G3_C1681`1<class BaseClass1>
		callvirt instance string class G3_C1681`1<class BaseClass1>::ClassMethod4760<object>()
		ldstr "G3_C1681::ClassMethod4760.17429<System.Object>()"
		ldstr "class G3_C1681`1<class BaseClass1> on type class G3_C1681`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1681`1<class BaseClass1>
		callvirt instance string class G3_C1681`1<class BaseClass1>::ClassMethod4759<object>()
		ldstr "G3_C1681::ClassMethod4759.17428<System.Object>()"
		ldstr "class G3_C1681`1<class BaseClass1> on type class G3_C1681`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1681`1<class BaseClass1>
		callvirt instance string class G3_C1681`1<class BaseClass1>::ClassMethod4758()
		ldstr "G3_C1681::ClassMethod4758.17427()"
		ldstr "class G3_C1681`1<class BaseClass1> on type class G3_C1681`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1681`1<class BaseClass1>
		callvirt instance string class G3_C1681`1<class BaseClass1>::ClassMethod4757()
		ldstr "G3_C1681::ClassMethod4757.17426()"
		ldstr "class G3_C1681`1<class BaseClass1> on type class G3_C1681`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1681`1<class BaseClass1>
		callvirt instance string class G3_C1681`1<class BaseClass1>::Method6<object>()
		ldstr "G3_C1681::Method6.17425<System.Object>()"
		ldstr "class G3_C1681`1<class BaseClass1> on type class G3_C1681`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1681`1<class BaseClass1>
		callvirt instance string class G3_C1681`1<class BaseClass1>::Method5()
		ldstr "G3_C1681::Method5.17424()"
		ldstr "class G3_C1681`1<class BaseClass1> on type class G3_C1681`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1681`1<class BaseClass1>
		callvirt instance string class G3_C1681`1<class BaseClass1>::Method4()
		ldstr "G3_C1681::Method4.17422()"
		ldstr "class G3_C1681`1<class BaseClass1> on type class G3_C1681`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1681`1<class BaseClass1>
		callvirt instance string class G3_C1681`1<class BaseClass1>::ClassMethod2699<object>()
		ldstr "G2_C669::ClassMethod2699.11162<System.Object>()"
		ldstr "class G3_C1681`1<class BaseClass1> on type class G3_C1681`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1681`1<class BaseClass1>
		callvirt instance string class G3_C1681`1<class BaseClass1>::Method7<object>()
		ldstr "G2_C669::Method7.11155<System.Object>()"
		ldstr "class G3_C1681`1<class BaseClass1> on type class G3_C1681`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1681`1<class BaseClass1>
		callvirt instance string class G3_C1681`1<class BaseClass1>::ClassMethod1347<object>()
		ldstr "G2_C669::ClassMethod1347.MI.11163<System.Object>()"
		ldstr "class G3_C1681`1<class BaseClass1> on type class G3_C1681`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1681`1<class BaseClass1>
		callvirt instance string class G3_C1681`1<class BaseClass1>::ClassMethod1346<object>()
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()"
		ldstr "class G3_C1681`1<class BaseClass1> on type class G3_C1681`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G1_C12`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C12`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C12`2<class BaseClass0,class BaseClass0>::ClassMethod1347<object>()
		ldstr "G1_C12::ClassMethod1347.4870<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass0> on type class G1_C12`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C12`2<class BaseClass0,class BaseClass0>::ClassMethod1346<object>()
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass0> on type class G1_C12`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C12`2<class BaseClass0,class BaseClass0>::Method6<object>()
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass0> on type class G1_C12`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C12`2<class BaseClass0,class BaseClass0>::Method5()
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass0> on type class G1_C12`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C12`2<class BaseClass0,class BaseClass0>::Method4()
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass0> on type class G1_C12`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C12`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass0> on type class G1_C12`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G1_C12`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G1_C12::Method4.4865()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C12`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G1_C12::Method5.4866()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C12`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G1_C12::Method6.MI.4868<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C12`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G1_C12`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G1_C12`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C12`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass0,class BaseClass1>::ClassMethod1347<object>()
		ldstr "G1_C12::ClassMethod1347.4870<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass1> on type class G1_C12`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass0,class BaseClass1>::ClassMethod1346<object>()
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass1> on type class G1_C12`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass0,class BaseClass1>::Method6<object>()
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass1> on type class G1_C12`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass0,class BaseClass1>::Method5()
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass1> on type class G1_C12`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass0,class BaseClass1>::Method4()
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass1> on type class G1_C12`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass1> on type class G1_C12`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G1_C12`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G1_C12::Method4.4865()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C12`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G1_C12::Method5.4866()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C12`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G1_C12::Method6.MI.4868<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C12`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G1_C12`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C12`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C12`2<class BaseClass1,class BaseClass0>::ClassMethod1347<object>()
		ldstr "G1_C12::ClassMethod1347.4870<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass0> on type class G1_C12`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C12`2<class BaseClass1,class BaseClass0>::ClassMethod1346<object>()
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass0> on type class G1_C12`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C12`2<class BaseClass1,class BaseClass0>::Method6<object>()
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass0> on type class G1_C12`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C12`2<class BaseClass1,class BaseClass0>::Method5()
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass0> on type class G1_C12`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C12`2<class BaseClass1,class BaseClass0>::Method4()
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass0> on type class G1_C12`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C12`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass0> on type class G1_C12`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G1_C12`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G1_C12::Method4.4865()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C12`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G1_C12::Method5.4866()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C12`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G1_C12::Method6.MI.4868<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C12`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G1_C12`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G1_C12`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G1_C12`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G1_C12`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass1,class BaseClass1>::ClassMethod1347<object>()
		ldstr "G1_C12::ClassMethod1347.4870<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G1_C12`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass1,class BaseClass1>::ClassMethod1346<object>()
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G1_C12`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass1,class BaseClass1>::Method6<object>()
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G1_C12`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass1,class BaseClass1>::Method5()
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G1_C12`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass1,class BaseClass1>::Method4()
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G1_C12`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G1_C12`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G1_C12`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G1_C12::Method4.4865()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C12`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G1_C12::Method5.4866()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C12`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G1_C12::Method6.MI.4868<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C12`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G1_C12`2<class BaseClass1,class BaseClass1>"
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
		newobj instance void class G3_C1681`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G2_C669::ClassMethod1347.MI.11163<System.Object>()#G3_C1681::Method4.17422()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G2_C669::Method7.11155<System.Object>()#"
		call void Framework::M.G1_C12.T.T<class BaseClass1,class BaseClass1,class G3_C1681`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G2_C669::ClassMethod1347.MI.11163<System.Object>()#G3_C1681::Method4.17422()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G2_C669::Method7.11155<System.Object>()#"
		call void Framework::M.G1_C12.B.T<class BaseClass1,class G3_C1681`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G2_C669::ClassMethod1347.MI.11163<System.Object>()#G3_C1681::Method4.17422()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G2_C669::Method7.11155<System.Object>()#"
		call void Framework::M.G1_C12.B.B<class G3_C1681`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G3_C1681`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G3_C1681`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G3_C1681`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1681::Method4.MI.17423()#G3_C1681::Method5.17424()#G3_C1681::Method6.17425<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G3_C1681`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1681::Method4.MI.17423()#G3_C1681::Method5.17424()#G3_C1681::Method6.17425<System.Object>()#"
		call void Framework::M.IBase1.A<class G3_C1681`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C669::Method7.11155<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G3_C1681`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C669::Method7.11155<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G3_C1681`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C669::Method7.11155<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G3_C1681`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G2_C669::ClassMethod1347.MI.11163<System.Object>()#G2_C669::ClassMethod2699.11162<System.Object>()#G3_C1681::Method4.17422()#G3_C1681::Method5.17424()#G2_C669::Method6.11160<System.Object>()#G2_C669::Method7.11155<System.Object>()#"
		call void Framework::M.G2_C669.T.T<class BaseClass1,class BaseClass0,class G3_C1681`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G2_C669::ClassMethod1347.MI.11163<System.Object>()#G2_C669::ClassMethod2699.11162<System.Object>()#G3_C1681::Method4.17422()#G3_C1681::Method5.17424()#G2_C669::Method6.11160<System.Object>()#G2_C669::Method7.11155<System.Object>()#"
		call void Framework::M.G2_C669.B.T<class BaseClass0,class G3_C1681`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G2_C669::ClassMethod1347.MI.11163<System.Object>()#G2_C669::ClassMethod2699.11162<System.Object>()#G3_C1681::Method4.17422()#G3_C1681::Method5.17424()#G2_C669::Method6.11160<System.Object>()#G2_C669::Method7.11155<System.Object>()#"
		call void Framework::M.G2_C669.B.A<class G3_C1681`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C669::Method7.11155<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G3_C1681`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C669::Method7.11155<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass0,class G3_C1681`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C669::Method7.11155<System.Object>()#"
		call void Framework::M.IBase2.A.A<class G3_C1681`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1681::Method4.MI.17423()#G3_C1681::Method5.17424()#G3_C1681::Method6.17425<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass1,class G3_C1681`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1681::Method4.MI.17423()#G3_C1681::Method5.17424()#G3_C1681::Method6.17425<System.Object>()#"
		call void Framework::M.IBase1.B<class G3_C1681`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G2_C669::ClassMethod1347.MI.11163<System.Object>()#G2_C669::ClassMethod2699.11162<System.Object>()#G3_C1681::ClassMethod4757.17426()#G3_C1681::ClassMethod4758.17427()#G3_C1681::ClassMethod4759.17428<System.Object>()#G3_C1681::ClassMethod4760.17429<System.Object>()#G3_C1681::Method4.17422()#G3_C1681::Method5.17424()#G3_C1681::Method6.17425<System.Object>()#G2_C669::Method7.11155<System.Object>()#"
		call void Framework::M.G3_C1681.T<class BaseClass0,class G3_C1681`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G2_C669::ClassMethod1347.MI.11163<System.Object>()#G2_C669::ClassMethod2699.11162<System.Object>()#G3_C1681::ClassMethod4757.17426()#G3_C1681::ClassMethod4758.17427()#G3_C1681::ClassMethod4759.17428<System.Object>()#G3_C1681::ClassMethod4760.17429<System.Object>()#G3_C1681::Method4.17422()#G3_C1681::Method5.17424()#G3_C1681::Method6.17425<System.Object>()#G2_C669::Method7.11155<System.Object>()#"
		call void Framework::M.G3_C1681.A<class G3_C1681`1<class BaseClass0>>(!!0,string)
		newobj instance void class G3_C1681`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G2_C669::ClassMethod1347.MI.11163<System.Object>()#G3_C1681::Method4.17422()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G2_C669::Method7.11155<System.Object>()#"
		call void Framework::M.G1_C12.T.T<class BaseClass1,class BaseClass1,class G3_C1681`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G2_C669::ClassMethod1347.MI.11163<System.Object>()#G3_C1681::Method4.17422()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G2_C669::Method7.11155<System.Object>()#"
		call void Framework::M.G1_C12.B.T<class BaseClass1,class G3_C1681`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G2_C669::ClassMethod1347.MI.11163<System.Object>()#G3_C1681::Method4.17422()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G2_C669::Method7.11155<System.Object>()#"
		call void Framework::M.G1_C12.B.B<class G3_C1681`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G3_C1681`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G3_C1681`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G3_C1681`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1681::Method4.MI.17423()#G3_C1681::Method5.17424()#G3_C1681::Method6.17425<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G3_C1681`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1681::Method4.MI.17423()#G3_C1681::Method5.17424()#G3_C1681::Method6.17425<System.Object>()#"
		call void Framework::M.IBase1.A<class G3_C1681`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C669::Method7.11155<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G3_C1681`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C669::Method7.11155<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G3_C1681`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C669::Method7.11155<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G3_C1681`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G2_C669::ClassMethod1347.MI.11163<System.Object>()#G2_C669::ClassMethod2699.11162<System.Object>()#G3_C1681::Method4.17422()#G3_C1681::Method5.17424()#G2_C669::Method6.11160<System.Object>()#G2_C669::Method7.11155<System.Object>()#"
		call void Framework::M.G2_C669.T.T<class BaseClass1,class BaseClass0,class G3_C1681`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G2_C669::ClassMethod1347.MI.11163<System.Object>()#G2_C669::ClassMethod2699.11162<System.Object>()#G3_C1681::Method4.17422()#G3_C1681::Method5.17424()#G2_C669::Method6.11160<System.Object>()#G2_C669::Method7.11155<System.Object>()#"
		call void Framework::M.G2_C669.B.T<class BaseClass0,class G3_C1681`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G2_C669::ClassMethod1347.MI.11163<System.Object>()#G2_C669::ClassMethod2699.11162<System.Object>()#G3_C1681::Method4.17422()#G3_C1681::Method5.17424()#G2_C669::Method6.11160<System.Object>()#G2_C669::Method7.11155<System.Object>()#"
		call void Framework::M.G2_C669.B.A<class G3_C1681`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C669::Method7.11155<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G3_C1681`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C669::Method7.11155<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass0,class G3_C1681`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C669::Method7.11155<System.Object>()#"
		call void Framework::M.IBase2.A.A<class G3_C1681`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1681::Method4.MI.17423()#G3_C1681::Method5.17424()#G3_C1681::Method6.17425<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass1,class G3_C1681`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1681::Method4.MI.17423()#G3_C1681::Method5.17424()#G3_C1681::Method6.17425<System.Object>()#"
		call void Framework::M.IBase1.B<class G3_C1681`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G2_C669::ClassMethod1347.MI.11163<System.Object>()#G2_C669::ClassMethod2699.11162<System.Object>()#G3_C1681::ClassMethod4757.17426()#G3_C1681::ClassMethod4758.17427()#G3_C1681::ClassMethod4759.17428<System.Object>()#G3_C1681::ClassMethod4760.17429<System.Object>()#G3_C1681::Method4.17422()#G3_C1681::Method5.17424()#G3_C1681::Method6.17425<System.Object>()#G2_C669::Method7.11155<System.Object>()#"
		call void Framework::M.G3_C1681.T<class BaseClass1,class G3_C1681`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G2_C669::ClassMethod1347.MI.11163<System.Object>()#G2_C669::ClassMethod2699.11162<System.Object>()#G3_C1681::ClassMethod4757.17426()#G3_C1681::ClassMethod4758.17427()#G3_C1681::ClassMethod4759.17428<System.Object>()#G3_C1681::ClassMethod4760.17429<System.Object>()#G3_C1681::Method4.17422()#G3_C1681::Method5.17424()#G3_C1681::Method6.17425<System.Object>()#G2_C669::Method7.11155<System.Object>()#"
		call void Framework::M.G3_C1681.B<class G3_C1681`1<class BaseClass1>>(!!0,string)
		newobj instance void class G1_C12`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G1_C12::ClassMethod1347.4870<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Framework::M.G1_C12.T.T<class BaseClass0,class BaseClass0,class G1_C12`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G1_C12::ClassMethod1347.4870<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Framework::M.G1_C12.A.T<class BaseClass0,class G1_C12`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G1_C12::ClassMethod1347.4870<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Framework::M.G1_C12.A.A<class G1_C12`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G1_C12`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass0,class G1_C12`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Framework::M.IBase2.A.A<class G1_C12`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.MI.4868<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G1_C12`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.MI.4868<System.Object>()#"
		call void Framework::M.IBase1.A<class G1_C12`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G1_C12`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G1_C12`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G1_C12`2<class BaseClass0,class BaseClass0>>(!!0,string)
		newobj instance void class G1_C12`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G1_C12::ClassMethod1347.4870<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Framework::M.G1_C12.T.T<class BaseClass0,class BaseClass1,class G1_C12`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G1_C12::ClassMethod1347.4870<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Framework::M.G1_C12.A.T<class BaseClass1,class G1_C12`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G1_C12::ClassMethod1347.4870<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Framework::M.G1_C12.A.B<class G1_C12`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G1_C12`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G1_C12`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G1_C12`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.MI.4868<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G1_C12`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.MI.4868<System.Object>()#"
		call void Framework::M.IBase1.A<class G1_C12`2<class BaseClass0,class BaseClass1>>(!!0,string)
		newobj instance void class G1_C12`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G1_C12::ClassMethod1347.4870<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Framework::M.G1_C12.T.T<class BaseClass1,class BaseClass0,class G1_C12`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G1_C12::ClassMethod1347.4870<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Framework::M.G1_C12.B.T<class BaseClass0,class G1_C12`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G1_C12::ClassMethod1347.4870<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Framework::M.G1_C12.B.A<class G1_C12`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass0,class G1_C12`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass0,class G1_C12`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Framework::M.IBase2.B.A<class G1_C12`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.MI.4868<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G1_C12`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.MI.4868<System.Object>()#"
		call void Framework::M.IBase1.A<class G1_C12`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G1_C12`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass0,class G1_C12`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Framework::M.IBase2.A.A<class G1_C12`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G1_C12`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G1_C12`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G1_C12`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G1_C12`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G1_C12`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G1_C12`2<class BaseClass1,class BaseClass0>>(!!0,string)
		newobj instance void class G1_C12`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G1_C12::ClassMethod1347.4870<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Framework::M.G1_C12.T.T<class BaseClass1,class BaseClass1,class G1_C12`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G1_C12::ClassMethod1347.4870<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Framework::M.G1_C12.B.T<class BaseClass1,class G1_C12`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G1_C12::ClassMethod1347.4870<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Framework::M.G1_C12.B.B<class G1_C12`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G1_C12`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G1_C12`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G1_C12`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.MI.4868<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G1_C12`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.MI.4868<System.Object>()#"
		call void Framework::M.IBase1.A<class G1_C12`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G1_C12`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G1_C12`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G1_C12`2<class BaseClass1,class BaseClass1>>(!!0,string)
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
		newobj instance void class G3_C1681`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass1,class BaseClass1>::ClassMethod1347<object>()
		calli default string(class G3_C1681`1<class BaseClass0>)
		ldstr "G2_C669::ClassMethod1347.MI.11163<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G3_C1681`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass1,class BaseClass1>::ClassMethod1346<object>()
		calli default string(class G3_C1681`1<class BaseClass0>)
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G3_C1681`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass1,class BaseClass1>::Method6<object>()
		calli default string(class G3_C1681`1<class BaseClass0>)
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G3_C1681`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass1,class BaseClass1>::Method5()
		calli default string(class G3_C1681`1<class BaseClass0>)
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G3_C1681`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass1,class BaseClass1>::Method4()
		calli default string(class G3_C1681`1<class BaseClass0>)
		ldstr "G3_C1681::Method4.17422()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G3_C1681`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1681`1<class BaseClass0>)
		ldstr "G2_C669::Method7.11155<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G3_C1681`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1681`1<class BaseClass0>)
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1681`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G3_C1681`1<class BaseClass0>)
		ldstr "G3_C1681::Method4.MI.17423()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1681`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G3_C1681`1<class BaseClass0>)
		ldstr "G3_C1681::Method5.17424()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1681`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1681`1<class BaseClass0>)
		ldstr "G3_C1681::Method6.17425<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1681`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1681`1<class BaseClass0>)
		ldstr "G2_C669::Method7.11155<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1681`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C669`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C669`2<class BaseClass1,class BaseClass0>::ClassMethod2699<object>()
		calli default string(class G3_C1681`1<class BaseClass0>)
		ldstr "G2_C669::ClassMethod2699.11162<System.Object>()"
		ldstr "class G2_C669`2<class BaseClass1,class BaseClass0> on type class G3_C1681`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C669`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C669`2<class BaseClass1,class BaseClass0>::Method6<object>()
		calli default string(class G3_C1681`1<class BaseClass0>)
		ldstr "G2_C669::Method6.11160<System.Object>()"
		ldstr "class G2_C669`2<class BaseClass1,class BaseClass0> on type class G3_C1681`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C669`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C669`2<class BaseClass1,class BaseClass0>::Method5()
		calli default string(class G3_C1681`1<class BaseClass0>)
		ldstr "G3_C1681::Method5.17424()"
		ldstr "class G2_C669`2<class BaseClass1,class BaseClass0> on type class G3_C1681`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C669`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C669`2<class BaseClass1,class BaseClass0>::Method4()
		calli default string(class G3_C1681`1<class BaseClass0>)
		ldstr "G3_C1681::Method4.17422()"
		ldstr "class G2_C669`2<class BaseClass1,class BaseClass0> on type class G3_C1681`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C669`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C669`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1681`1<class BaseClass0>)
		ldstr "G2_C669::Method7.11155<System.Object>()"
		ldstr "class G2_C669`2<class BaseClass1,class BaseClass0> on type class G3_C1681`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C669`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C669`2<class BaseClass1,class BaseClass0>::ClassMethod1347<object>()
		calli default string(class G3_C1681`1<class BaseClass0>)
		ldstr "G2_C669::ClassMethod1347.MI.11163<System.Object>()"
		ldstr "class G2_C669`2<class BaseClass1,class BaseClass0> on type class G3_C1681`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C669`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C669`2<class BaseClass1,class BaseClass0>::ClassMethod1346<object>()
		calli default string(class G3_C1681`1<class BaseClass0>)
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()"
		ldstr "class G2_C669`2<class BaseClass1,class BaseClass0> on type class G3_C1681`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1681`1<class BaseClass0>)
		ldstr "G2_C669::Method7.11155<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1681`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G3_C1681`1<class BaseClass0>)
		ldstr "G3_C1681::Method4.MI.17423()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1681`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G3_C1681`1<class BaseClass0>)
		ldstr "G3_C1681::Method5.17424()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1681`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G3_C1681`1<class BaseClass0>)
		ldstr "G3_C1681::Method6.17425<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1681`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1681`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1681`1<class BaseClass0>::ClassMethod4760<object>()
		calli default string(class G3_C1681`1<class BaseClass0>)
		ldstr "G3_C1681::ClassMethod4760.17429<System.Object>()"
		ldstr "class G3_C1681`1<class BaseClass0> on type class G3_C1681`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1681`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1681`1<class BaseClass0>::ClassMethod4759<object>()
		calli default string(class G3_C1681`1<class BaseClass0>)
		ldstr "G3_C1681::ClassMethod4759.17428<System.Object>()"
		ldstr "class G3_C1681`1<class BaseClass0> on type class G3_C1681`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1681`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1681`1<class BaseClass0>::ClassMethod4758()
		calli default string(class G3_C1681`1<class BaseClass0>)
		ldstr "G3_C1681::ClassMethod4758.17427()"
		ldstr "class G3_C1681`1<class BaseClass0> on type class G3_C1681`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1681`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1681`1<class BaseClass0>::ClassMethod4757()
		calli default string(class G3_C1681`1<class BaseClass0>)
		ldstr "G3_C1681::ClassMethod4757.17426()"
		ldstr "class G3_C1681`1<class BaseClass0> on type class G3_C1681`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1681`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1681`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1681`1<class BaseClass0>)
		ldstr "G3_C1681::Method6.17425<System.Object>()"
		ldstr "class G3_C1681`1<class BaseClass0> on type class G3_C1681`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1681`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1681`1<class BaseClass0>::Method5()
		calli default string(class G3_C1681`1<class BaseClass0>)
		ldstr "G3_C1681::Method5.17424()"
		ldstr "class G3_C1681`1<class BaseClass0> on type class G3_C1681`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1681`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1681`1<class BaseClass0>::Method4()
		calli default string(class G3_C1681`1<class BaseClass0>)
		ldstr "G3_C1681::Method4.17422()"
		ldstr "class G3_C1681`1<class BaseClass0> on type class G3_C1681`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1681`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1681`1<class BaseClass0>::ClassMethod2699<object>()
		calli default string(class G3_C1681`1<class BaseClass0>)
		ldstr "G2_C669::ClassMethod2699.11162<System.Object>()"
		ldstr "class G3_C1681`1<class BaseClass0> on type class G3_C1681`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1681`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1681`1<class BaseClass0>::Method7<object>()
		calli default string(class G3_C1681`1<class BaseClass0>)
		ldstr "G2_C669::Method7.11155<System.Object>()"
		ldstr "class G3_C1681`1<class BaseClass0> on type class G3_C1681`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1681`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1681`1<class BaseClass0>::ClassMethod1347<object>()
		calli default string(class G3_C1681`1<class BaseClass0>)
		ldstr "G2_C669::ClassMethod1347.MI.11163<System.Object>()"
		ldstr "class G3_C1681`1<class BaseClass0> on type class G3_C1681`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1681`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1681`1<class BaseClass0>::ClassMethod1346<object>()
		calli default string(class G3_C1681`1<class BaseClass0>)
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()"
		ldstr "class G3_C1681`1<class BaseClass0> on type class G3_C1681`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G3_C1681`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass1,class BaseClass1>::ClassMethod1347<object>()
		calli default string(class G3_C1681`1<class BaseClass1>)
		ldstr "G2_C669::ClassMethod1347.MI.11163<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G3_C1681`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass1,class BaseClass1>::ClassMethod1346<object>()
		calli default string(class G3_C1681`1<class BaseClass1>)
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G3_C1681`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass1,class BaseClass1>::Method6<object>()
		calli default string(class G3_C1681`1<class BaseClass1>)
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G3_C1681`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass1,class BaseClass1>::Method5()
		calli default string(class G3_C1681`1<class BaseClass1>)
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G3_C1681`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass1,class BaseClass1>::Method4()
		calli default string(class G3_C1681`1<class BaseClass1>)
		ldstr "G3_C1681::Method4.17422()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G3_C1681`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1681`1<class BaseClass1>)
		ldstr "G2_C669::Method7.11155<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G3_C1681`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1681`1<class BaseClass1>)
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1681`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G3_C1681`1<class BaseClass1>)
		ldstr "G3_C1681::Method4.MI.17423()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1681`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G3_C1681`1<class BaseClass1>)
		ldstr "G3_C1681::Method5.17424()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1681`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1681`1<class BaseClass1>)
		ldstr "G3_C1681::Method6.17425<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1681`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1681`1<class BaseClass1>)
		ldstr "G2_C669::Method7.11155<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1681`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C669`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C669`2<class BaseClass1,class BaseClass0>::ClassMethod2699<object>()
		calli default string(class G3_C1681`1<class BaseClass1>)
		ldstr "G2_C669::ClassMethod2699.11162<System.Object>()"
		ldstr "class G2_C669`2<class BaseClass1,class BaseClass0> on type class G3_C1681`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C669`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C669`2<class BaseClass1,class BaseClass0>::Method6<object>()
		calli default string(class G3_C1681`1<class BaseClass1>)
		ldstr "G2_C669::Method6.11160<System.Object>()"
		ldstr "class G2_C669`2<class BaseClass1,class BaseClass0> on type class G3_C1681`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C669`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C669`2<class BaseClass1,class BaseClass0>::Method5()
		calli default string(class G3_C1681`1<class BaseClass1>)
		ldstr "G3_C1681::Method5.17424()"
		ldstr "class G2_C669`2<class BaseClass1,class BaseClass0> on type class G3_C1681`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C669`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C669`2<class BaseClass1,class BaseClass0>::Method4()
		calli default string(class G3_C1681`1<class BaseClass1>)
		ldstr "G3_C1681::Method4.17422()"
		ldstr "class G2_C669`2<class BaseClass1,class BaseClass0> on type class G3_C1681`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C669`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C669`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1681`1<class BaseClass1>)
		ldstr "G2_C669::Method7.11155<System.Object>()"
		ldstr "class G2_C669`2<class BaseClass1,class BaseClass0> on type class G3_C1681`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C669`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C669`2<class BaseClass1,class BaseClass0>::ClassMethod1347<object>()
		calli default string(class G3_C1681`1<class BaseClass1>)
		ldstr "G2_C669::ClassMethod1347.MI.11163<System.Object>()"
		ldstr "class G2_C669`2<class BaseClass1,class BaseClass0> on type class G3_C1681`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C669`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C669`2<class BaseClass1,class BaseClass0>::ClassMethod1346<object>()
		calli default string(class G3_C1681`1<class BaseClass1>)
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()"
		ldstr "class G2_C669`2<class BaseClass1,class BaseClass0> on type class G3_C1681`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1681`1<class BaseClass1>)
		ldstr "G2_C669::Method7.11155<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1681`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G3_C1681`1<class BaseClass1>)
		ldstr "G3_C1681::Method4.MI.17423()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1681`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G3_C1681`1<class BaseClass1>)
		ldstr "G3_C1681::Method5.17424()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1681`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G3_C1681`1<class BaseClass1>)
		ldstr "G3_C1681::Method6.17425<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1681`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1681`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1681`1<class BaseClass1>::ClassMethod4760<object>()
		calli default string(class G3_C1681`1<class BaseClass1>)
		ldstr "G3_C1681::ClassMethod4760.17429<System.Object>()"
		ldstr "class G3_C1681`1<class BaseClass1> on type class G3_C1681`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1681`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1681`1<class BaseClass1>::ClassMethod4759<object>()
		calli default string(class G3_C1681`1<class BaseClass1>)
		ldstr "G3_C1681::ClassMethod4759.17428<System.Object>()"
		ldstr "class G3_C1681`1<class BaseClass1> on type class G3_C1681`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1681`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1681`1<class BaseClass1>::ClassMethod4758()
		calli default string(class G3_C1681`1<class BaseClass1>)
		ldstr "G3_C1681::ClassMethod4758.17427()"
		ldstr "class G3_C1681`1<class BaseClass1> on type class G3_C1681`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1681`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1681`1<class BaseClass1>::ClassMethod4757()
		calli default string(class G3_C1681`1<class BaseClass1>)
		ldstr "G3_C1681::ClassMethod4757.17426()"
		ldstr "class G3_C1681`1<class BaseClass1> on type class G3_C1681`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1681`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1681`1<class BaseClass1>::Method6<object>()
		calli default string(class G3_C1681`1<class BaseClass1>)
		ldstr "G3_C1681::Method6.17425<System.Object>()"
		ldstr "class G3_C1681`1<class BaseClass1> on type class G3_C1681`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1681`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1681`1<class BaseClass1>::Method5()
		calli default string(class G3_C1681`1<class BaseClass1>)
		ldstr "G3_C1681::Method5.17424()"
		ldstr "class G3_C1681`1<class BaseClass1> on type class G3_C1681`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1681`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1681`1<class BaseClass1>::Method4()
		calli default string(class G3_C1681`1<class BaseClass1>)
		ldstr "G3_C1681::Method4.17422()"
		ldstr "class G3_C1681`1<class BaseClass1> on type class G3_C1681`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1681`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1681`1<class BaseClass1>::ClassMethod2699<object>()
		calli default string(class G3_C1681`1<class BaseClass1>)
		ldstr "G2_C669::ClassMethod2699.11162<System.Object>()"
		ldstr "class G3_C1681`1<class BaseClass1> on type class G3_C1681`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1681`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1681`1<class BaseClass1>::Method7<object>()
		calli default string(class G3_C1681`1<class BaseClass1>)
		ldstr "G2_C669::Method7.11155<System.Object>()"
		ldstr "class G3_C1681`1<class BaseClass1> on type class G3_C1681`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1681`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1681`1<class BaseClass1>::ClassMethod1347<object>()
		calli default string(class G3_C1681`1<class BaseClass1>)
		ldstr "G2_C669::ClassMethod1347.MI.11163<System.Object>()"
		ldstr "class G3_C1681`1<class BaseClass1> on type class G3_C1681`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1681`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1681`1<class BaseClass1>::ClassMethod1346<object>()
		calli default string(class G3_C1681`1<class BaseClass1>)
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()"
		ldstr "class G3_C1681`1<class BaseClass1> on type class G3_C1681`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G1_C12`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C12`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass0,class BaseClass0>::ClassMethod1347<object>()
		calli default string(class G1_C12`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C12::ClassMethod1347.4870<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass0> on type class G1_C12`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass0,class BaseClass0>::ClassMethod1346<object>()
		calli default string(class G1_C12`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass0> on type class G1_C12`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass0,class BaseClass0>::Method6<object>()
		calli default string(class G1_C12`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass0> on type class G1_C12`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass0,class BaseClass0>::Method5()
		calli default string(class G1_C12`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass0> on type class G1_C12`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass0,class BaseClass0>::Method4()
		calli default string(class G1_C12`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass0> on type class G1_C12`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G1_C12`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass0> on type class G1_C12`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G1_C12`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G1_C12`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G1_C12`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C12::Method4.4865()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C12`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G1_C12`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C12::Method5.4866()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C12`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G1_C12`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C12::Method6.MI.4868<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C12`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G1_C12`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G1_C12`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G1_C12`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C12`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass0,class BaseClass1>::ClassMethod1347<object>()
		calli default string(class G1_C12`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C12::ClassMethod1347.4870<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass1> on type class G1_C12`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass0,class BaseClass1>::ClassMethod1346<object>()
		calli default string(class G1_C12`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass1> on type class G1_C12`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass0,class BaseClass1>::Method6<object>()
		calli default string(class G1_C12`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass1> on type class G1_C12`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass0,class BaseClass1>::Method5()
		calli default string(class G1_C12`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass1> on type class G1_C12`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass0,class BaseClass1>::Method4()
		calli default string(class G1_C12`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass1> on type class G1_C12`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G1_C12`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass1> on type class G1_C12`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G1_C12`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G1_C12`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G1_C12`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C12::Method4.4865()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C12`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G1_C12`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C12::Method5.4866()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C12`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G1_C12`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C12::Method6.MI.4868<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C12`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G1_C12`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C12`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass1,class BaseClass0>::ClassMethod1347<object>()
		calli default string(class G1_C12`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C12::ClassMethod1347.4870<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass0> on type class G1_C12`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass1,class BaseClass0>::ClassMethod1346<object>()
		calli default string(class G1_C12`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass0> on type class G1_C12`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass1,class BaseClass0>::Method6<object>()
		calli default string(class G1_C12`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass0> on type class G1_C12`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass1,class BaseClass0>::Method5()
		calli default string(class G1_C12`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass0> on type class G1_C12`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass1,class BaseClass0>::Method4()
		calli default string(class G1_C12`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass0> on type class G1_C12`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G1_C12`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass0> on type class G1_C12`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G1_C12`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G1_C12`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G1_C12`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C12::Method4.4865()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C12`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G1_C12`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C12::Method5.4866()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C12`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G1_C12`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C12::Method6.MI.4868<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C12`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G1_C12`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G1_C12`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G1_C12`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G1_C12`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G1_C12`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G1_C12`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G1_C12`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass1,class BaseClass1>::ClassMethod1347<object>()
		calli default string(class G1_C12`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C12::ClassMethod1347.4870<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G1_C12`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass1,class BaseClass1>::ClassMethod1346<object>()
		calli default string(class G1_C12`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G1_C12`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass1,class BaseClass1>::Method6<object>()
		calli default string(class G1_C12`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G1_C12`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass1,class BaseClass1>::Method5()
		calli default string(class G1_C12`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C12::Method5.4866()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G1_C12`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass1,class BaseClass1>::Method4()
		calli default string(class G1_C12`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C12::Method4.4865()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G1_C12`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G1_C12`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass1,class BaseClass1> on type class G1_C12`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G1_C12`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G1_C12`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G1_C12`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C12::Method4.4865()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C12`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G1_C12`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C12::Method5.4866()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C12`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G1_C12`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C12::Method6.MI.4868<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C12`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G1_C12`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C12::Method7.MI.4864<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G1_C12`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldstr "========================================================================\n\n"
		call void [mscorlib]System.Console::WriteLine(string)
		ret
	}
	.method public hidebysig static int32  Main() cil managed
	{
		.entrypoint
		.maxstack  10
		call void Framework::MethodCallingTest()
		call void Framework::ConstrainedCallsTest()
		call void Framework::StructConstrainedInterfaceCallsTest()
		call void Framework::CalliTest()
		ldc.i4 100
		ret
	}
}
