// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) .ver 4:0:0:0 }
.assembly extern TestFramework { .publickeytoken = ( B0 3F 5F 7F 11 D5 0A 3A )  }

//TYPES IN FORWARDER ASSEMBLIES:

//TEST ASSEMBLY:
.assembly Generated1027 { .hash algorithm 0x00008004 }
.module Generated1027.exe

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
.class public G3_C1499`1<T0> 
		extends class G2_C514`2<!T0,class BaseClass0>
		implements class IBase1`1<class BaseClass0> 
{
	.method public hidebysig newslot virtual instance string Method4() cil managed noinlining { 
		ldstr "G3_C1499::Method4.16471()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class BaseClass0>.Method4'() cil managed noinlining { 
		.override method instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G3_C1499::Method4.MI.16472()"
		ret
	}
	.method public hidebysig virtual instance string Method5() cil managed noinlining { 
		ldstr "G3_C1499::Method5.16473()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G3_C1499::Method6.16474<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class BaseClass0>.Method6'<M0>() cil managed noinlining { 
		.override method instance string class IBase1`1<class BaseClass0>::Method6<[1]>()
		ldstr "G3_C1499::Method6.MI.16475<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod4387() cil managed noinlining { 
		ldstr "G3_C1499::ClassMethod4387.16476()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod4388() cil managed noinlining { 
		ldstr "G3_C1499::ClassMethod4388.16477()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod4389<M0>() cil managed noinlining { 
		ldstr "G3_C1499::ClassMethod4389.16478<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod4390<M0>() cil managed noinlining { 
		ldstr "G3_C1499::ClassMethod4390.16479<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C514<T0,class BaseClass0>.ClassMethod2386'() cil managed noinlining { 
		.override method instance string class G2_C514`2<!T0,class BaseClass0>::ClassMethod2386()
		ldstr "G3_C1499::ClassMethod2386.MI.16480()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C514<T0,class BaseClass0>.ClassMethod2387'() cil managed noinlining { 
		.override method instance string class G2_C514`2<!T0,class BaseClass0>::ClassMethod2387()
		ldstr "G3_C1499::ClassMethod2387.MI.16481()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C514<T0,class BaseClass0>.ClassMethod2388'<M0>() cil managed noinlining { 
		.override method instance string class G2_C514`2<!T0,class BaseClass0>::ClassMethod2388<[1]>()
		ldstr "G3_C1499::ClassMethod2388.MI.16482<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C514<T0,class BaseClass0>.ClassMethod2389'<M0>() cil managed noinlining { 
		.override method instance string class G2_C514`2<!T0,class BaseClass0>::ClassMethod2389<[1]>()
		ldstr "G3_C1499::ClassMethod2389.MI.16483<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class G2_C514`2<!T0,class BaseClass0>::.ctor()
		ret
	}
}
.class public G2_C514`2<T0, T1> 
		extends class G1_C9`2<!T1,!T1>
		implements class IBase1`1<class BaseClass1>, class IBase2`2<class BaseClass0,!T1> 
{
	.method public hidebysig newslot virtual instance string Method4() cil managed noinlining { 
		ldstr "G2_C514::Method4.9706()"
		ret
	}
	.method public hidebysig virtual instance string Method5() cil managed noinlining { 
		ldstr "G2_C514::Method5.9707()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G2_C514::Method6.9708<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class BaseClass1>.Method6'<M0>() cil managed noinlining { 
		.override method instance string class IBase1`1<class BaseClass1>::Method6<[1]>()
		ldstr "G2_C514::Method6.MI.9709<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G2_C514::Method7.9710<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod2386() cil managed noinlining { 
		ldstr "G2_C514::ClassMethod2386.9711()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod2387() cil managed noinlining { 
		ldstr "G2_C514::ClassMethod2387.9712()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod2388<M0>() cil managed noinlining { 
		ldstr "G2_C514::ClassMethod2388.9713<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod2389<M0>() cil managed noinlining { 
		ldstr "G2_C514::ClassMethod2389.9714<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C9<T1,T1>.ClassMethod1336'() cil managed noinlining { 
		.override method instance string class G1_C9`2<!T1,!T1>::ClassMethod1336()
		ldstr "G2_C514::ClassMethod1336.MI.9715()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C9<T1,T1>.ClassMethod1337'() cil managed noinlining { 
		.override method instance string class G1_C9`2<!T1,!T1>::ClassMethod1337()
		ldstr "G2_C514::ClassMethod1337.MI.9716()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C9<T1,T1>.ClassMethod1338'<M0>() cil managed noinlining { 
		.override method instance string class G1_C9`2<!T1,!T1>::ClassMethod1338<[1]>()
		ldstr "G2_C514::ClassMethod1338.MI.9717<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C9<T1,T1>.ClassMethod1339'<M0>() cil managed noinlining { 
		.override method instance string class G1_C9`2<!T1,!T1>::ClassMethod1339<[1]>()
		ldstr "G2_C514::ClassMethod1339.MI.9718<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class G1_C9`2<!T1,!T1>::.ctor()
		ret
	}
}
.class interface public abstract IBase1`1<+T0> 
{
	.method public hidebysig newslot abstract virtual instance string Method4() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method5() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method6<M0>() cil managed { }
}
.class public abstract G1_C9`2<T0, T1> 
		implements class IBase2`2<class BaseClass0,!T0>, class IBase1`1<class BaseClass0> 
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
	.method public hidebysig newslot virtual instance string 'IBase1<class BaseClass0>.Method4'() cil managed noinlining { 
		.override method instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G1_C9::Method4.MI.4835()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method5() cil managed noinlining { 
		ldstr "G1_C9::Method5.4836()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class BaseClass0>.Method5'() cil managed noinlining { 
		.override method instance string class IBase1`1<class BaseClass0>::Method5()
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
	.method public hidebysig newslot virtual instance string 'IBase1<class BaseClass0>.Method6'<M0>() cil managed noinlining { 
		.override method instance string class IBase1`1<class BaseClass0>::Method6<[1]>()
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
	.method static void M.G3_C1499.T<T0,(class G3_C1499`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 21
		.locals init (string[] actualResults)
		ldc.i4.s    16
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1499.T<T0,(class G3_C1499`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 16
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1499`1<!!T0>::ClassMethod1336()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1499`1<!!T0>::ClassMethod1337()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1499`1<!!T0>::ClassMethod1338<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1499`1<!!T0>::ClassMethod1339<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1499`1<!!T0>::ClassMethod2386()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1499`1<!!T0>::ClassMethod2387()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1499`1<!!T0>::ClassMethod2388<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1499`1<!!T0>::ClassMethod2389<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1499`1<!!T0>::ClassMethod4387()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1499`1<!!T0>::ClassMethod4388()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1499`1<!!T0>::ClassMethod4389<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1499`1<!!T0>::ClassMethod4390<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1499`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1499`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     14
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1499`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     15
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1499`1<!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1499.A<(class G3_C1499`1<class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 21
		.locals init (string[] actualResults)
		ldc.i4.s    16
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1499.A<(class G3_C1499`1<class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 16
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1499`1<class BaseClass0>::ClassMethod1336()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1499`1<class BaseClass0>::ClassMethod1337()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1499`1<class BaseClass0>::ClassMethod1338<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1499`1<class BaseClass0>::ClassMethod1339<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1499`1<class BaseClass0>::ClassMethod2386()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1499`1<class BaseClass0>::ClassMethod2387()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1499`1<class BaseClass0>::ClassMethod2388<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1499`1<class BaseClass0>::ClassMethod2389<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1499`1<class BaseClass0>::ClassMethod4387()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1499`1<class BaseClass0>::ClassMethod4388()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1499`1<class BaseClass0>::ClassMethod4389<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1499`1<class BaseClass0>::ClassMethod4390<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1499`1<class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1499`1<class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     14
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1499`1<class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     15
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1499`1<class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1499.B<(class G3_C1499`1<class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 21
		.locals init (string[] actualResults)
		ldc.i4.s    16
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1499.B<(class G3_C1499`1<class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 16
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1499`1<class BaseClass1>::ClassMethod1336()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1499`1<class BaseClass1>::ClassMethod1337()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1499`1<class BaseClass1>::ClassMethod1338<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1499`1<class BaseClass1>::ClassMethod1339<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1499`1<class BaseClass1>::ClassMethod2386()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1499`1<class BaseClass1>::ClassMethod2387()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1499`1<class BaseClass1>::ClassMethod2388<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1499`1<class BaseClass1>::ClassMethod2389<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1499`1<class BaseClass1>::ClassMethod4387()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1499`1<class BaseClass1>::ClassMethod4388()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1499`1<class BaseClass1>::ClassMethod4389<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1499`1<class BaseClass1>::ClassMethod4390<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1499`1<class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1499`1<class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     14
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1499`1<class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     15
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1499`1<class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C514.T.T<T0,T1,(class G2_C514`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 17
		.locals init (string[] actualResults)
		ldc.i4.s    12
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C514.T.T<T0,T1,(class G2_C514`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 12
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C514`2<!!T0,!!T1>::ClassMethod1336()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C514`2<!!T0,!!T1>::ClassMethod1337()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C514`2<!!T0,!!T1>::ClassMethod1338<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C514`2<!!T0,!!T1>::ClassMethod1339<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C514`2<!!T0,!!T1>::ClassMethod2386()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C514`2<!!T0,!!T1>::ClassMethod2387()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C514`2<!!T0,!!T1>::ClassMethod2388<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C514`2<!!T0,!!T1>::ClassMethod2389<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C514`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C514`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C514`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C514`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C514.A.T<T1,(class G2_C514`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 17
		.locals init (string[] actualResults)
		ldc.i4.s    12
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C514.A.T<T1,(class G2_C514`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 12
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C514`2<class BaseClass0,!!T1>::ClassMethod1336()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C514`2<class BaseClass0,!!T1>::ClassMethod1337()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C514`2<class BaseClass0,!!T1>::ClassMethod1338<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C514`2<class BaseClass0,!!T1>::ClassMethod1339<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C514`2<class BaseClass0,!!T1>::ClassMethod2386()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C514`2<class BaseClass0,!!T1>::ClassMethod2387()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C514`2<class BaseClass0,!!T1>::ClassMethod2388<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C514`2<class BaseClass0,!!T1>::ClassMethod2389<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C514`2<class BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C514`2<class BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C514`2<class BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C514`2<class BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C514.A.A<(class G2_C514`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 17
		.locals init (string[] actualResults)
		ldc.i4.s    12
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C514.A.A<(class G2_C514`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 12
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C514`2<class BaseClass0,class BaseClass0>::ClassMethod1336()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C514`2<class BaseClass0,class BaseClass0>::ClassMethod1337()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C514`2<class BaseClass0,class BaseClass0>::ClassMethod1338<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C514`2<class BaseClass0,class BaseClass0>::ClassMethod1339<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C514`2<class BaseClass0,class BaseClass0>::ClassMethod2386()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C514`2<class BaseClass0,class BaseClass0>::ClassMethod2387()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C514`2<class BaseClass0,class BaseClass0>::ClassMethod2388<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C514`2<class BaseClass0,class BaseClass0>::ClassMethod2389<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C514`2<class BaseClass0,class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C514`2<class BaseClass0,class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C514`2<class BaseClass0,class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C514`2<class BaseClass0,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C514.A.B<(class G2_C514`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 17
		.locals init (string[] actualResults)
		ldc.i4.s    12
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C514.A.B<(class G2_C514`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 12
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C514`2<class BaseClass0,class BaseClass1>::ClassMethod1336()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C514`2<class BaseClass0,class BaseClass1>::ClassMethod1337()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C514`2<class BaseClass0,class BaseClass1>::ClassMethod1338<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C514`2<class BaseClass0,class BaseClass1>::ClassMethod1339<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C514`2<class BaseClass0,class BaseClass1>::ClassMethod2386()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C514`2<class BaseClass0,class BaseClass1>::ClassMethod2387()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C514`2<class BaseClass0,class BaseClass1>::ClassMethod2388<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C514`2<class BaseClass0,class BaseClass1>::ClassMethod2389<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C514`2<class BaseClass0,class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C514`2<class BaseClass0,class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C514`2<class BaseClass0,class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C514`2<class BaseClass0,class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C514.B.T<T1,(class G2_C514`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 17
		.locals init (string[] actualResults)
		ldc.i4.s    12
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C514.B.T<T1,(class G2_C514`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 12
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C514`2<class BaseClass1,!!T1>::ClassMethod1336()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C514`2<class BaseClass1,!!T1>::ClassMethod1337()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C514`2<class BaseClass1,!!T1>::ClassMethod1338<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C514`2<class BaseClass1,!!T1>::ClassMethod1339<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C514`2<class BaseClass1,!!T1>::ClassMethod2386()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C514`2<class BaseClass1,!!T1>::ClassMethod2387()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C514`2<class BaseClass1,!!T1>::ClassMethod2388<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C514`2<class BaseClass1,!!T1>::ClassMethod2389<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C514`2<class BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C514`2<class BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C514`2<class BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C514`2<class BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C514.B.A<(class G2_C514`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 17
		.locals init (string[] actualResults)
		ldc.i4.s    12
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C514.B.A<(class G2_C514`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 12
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C514`2<class BaseClass1,class BaseClass0>::ClassMethod1336()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C514`2<class BaseClass1,class BaseClass0>::ClassMethod1337()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C514`2<class BaseClass1,class BaseClass0>::ClassMethod1338<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C514`2<class BaseClass1,class BaseClass0>::ClassMethod1339<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C514`2<class BaseClass1,class BaseClass0>::ClassMethod2386()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C514`2<class BaseClass1,class BaseClass0>::ClassMethod2387()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C514`2<class BaseClass1,class BaseClass0>::ClassMethod2388<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C514`2<class BaseClass1,class BaseClass0>::ClassMethod2389<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C514`2<class BaseClass1,class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C514`2<class BaseClass1,class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C514`2<class BaseClass1,class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C514`2<class BaseClass1,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C514.B.B<(class G2_C514`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 17
		.locals init (string[] actualResults)
		ldc.i4.s    12
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C514.B.B<(class G2_C514`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 12
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C514`2<class BaseClass1,class BaseClass1>::ClassMethod1336()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C514`2<class BaseClass1,class BaseClass1>::ClassMethod1337()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C514`2<class BaseClass1,class BaseClass1>::ClassMethod1338<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C514`2<class BaseClass1,class BaseClass1>::ClassMethod1339<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C514`2<class BaseClass1,class BaseClass1>::ClassMethod2386()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C514`2<class BaseClass1,class BaseClass1>::ClassMethod2387()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C514`2<class BaseClass1,class BaseClass1>::ClassMethod2388<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C514`2<class BaseClass1,class BaseClass1>::ClassMethod2389<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C514`2<class BaseClass1,class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C514`2<class BaseClass1,class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C514`2<class BaseClass1,class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C514`2<class BaseClass1,class BaseClass1>::Method7<object>()
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
	.method static void M.G1_C9.T.T<T0,T1,(class G1_C9`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C9.T.T<T0,T1,(class G1_C9`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<!!T0,!!T1>::ClassMethod1336()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<!!T0,!!T1>::ClassMethod1337()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<!!T0,!!T1>::ClassMethod1338<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<!!T0,!!T1>::ClassMethod1339<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C9.A.T<T1,(class G1_C9`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C9.A.T<T1,(class G1_C9`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass0,!!T1>::ClassMethod1336()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass0,!!T1>::ClassMethod1337()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass0,!!T1>::ClassMethod1338<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass0,!!T1>::ClassMethod1339<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C9.A.A<(class G1_C9`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C9.A.A<(class G1_C9`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass0,class BaseClass0>::ClassMethod1336()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass0,class BaseClass0>::ClassMethod1337()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass0,class BaseClass0>::ClassMethod1338<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass0,class BaseClass0>::ClassMethod1339<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass0,class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass0,class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass0,class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass0,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C9.A.B<(class G1_C9`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C9.A.B<(class G1_C9`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass0,class BaseClass1>::ClassMethod1336()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass0,class BaseClass1>::ClassMethod1337()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass0,class BaseClass1>::ClassMethod1338<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass0,class BaseClass1>::ClassMethod1339<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass0,class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass0,class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass0,class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass0,class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C9.B.T<T1,(class G1_C9`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C9.B.T<T1,(class G1_C9`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass1,!!T1>::ClassMethod1336()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass1,!!T1>::ClassMethod1337()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass1,!!T1>::ClassMethod1338<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass1,!!T1>::ClassMethod1339<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C9.B.A<(class G1_C9`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C9.B.A<(class G1_C9`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass1,class BaseClass0>::ClassMethod1336()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass1,class BaseClass0>::ClassMethod1337()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass1,class BaseClass0>::ClassMethod1338<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass1,class BaseClass0>::ClassMethod1339<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass1,class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass1,class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass1,class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass1,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C9.B.B<(class G1_C9`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C9.B.B<(class G1_C9`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass1,class BaseClass1>::ClassMethod1336()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass1,class BaseClass1>::ClassMethod1337()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass1,class BaseClass1>::ClassMethod1338<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass1,class BaseClass1>::ClassMethod1339<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass1,class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass1,class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass1,class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C9`2<class BaseClass1,class BaseClass1>::Method7<object>()
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
		newobj instance void class G3_C1499`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C9`2<class BaseClass0,class BaseClass0>::ClassMethod1339<object>()
		ldstr "G2_C514::ClassMethod1339.MI.9718<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G3_C1499`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C9`2<class BaseClass0,class BaseClass0>::ClassMethod1338<object>()
		ldstr "G2_C514::ClassMethod1338.MI.9717<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G3_C1499`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C9`2<class BaseClass0,class BaseClass0>::ClassMethod1337()
		ldstr "G2_C514::ClassMethod1337.MI.9716()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G3_C1499`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C9`2<class BaseClass0,class BaseClass0>::ClassMethod1336()
		ldstr "G2_C514::ClassMethod1336.MI.9715()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G3_C1499`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C9`2<class BaseClass0,class BaseClass0>::Method6<object>()
		ldstr "G1_C9::Method6.4838<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G3_C1499`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C9`2<class BaseClass0,class BaseClass0>::Method5()
		ldstr "G3_C1499::Method5.16473()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G3_C1499`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C9`2<class BaseClass0,class BaseClass0>::Method4()
		ldstr "G1_C9::Method4.4834()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G3_C1499`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C9`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G1_C9::Method7.4833<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G3_C1499`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C514::Method7.9710<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1499`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G3_C1499::Method4.MI.16472()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1499`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G3_C1499::Method5.16473()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1499`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G3_C1499::Method6.MI.16475<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1499`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C514::Method7.9710<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1499`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C514`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C514`2<class BaseClass0,class BaseClass0>::ClassMethod2389<object>()
		ldstr "G3_C1499::ClassMethod2389.MI.16483<System.Object>()"
		ldstr "class G2_C514`2<class BaseClass0,class BaseClass0> on type class G3_C1499`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C514`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C514`2<class BaseClass0,class BaseClass0>::ClassMethod2388<object>()
		ldstr "G3_C1499::ClassMethod2388.MI.16482<System.Object>()"
		ldstr "class G2_C514`2<class BaseClass0,class BaseClass0> on type class G3_C1499`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C514`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C514`2<class BaseClass0,class BaseClass0>::ClassMethod2387()
		ldstr "G3_C1499::ClassMethod2387.MI.16481()"
		ldstr "class G2_C514`2<class BaseClass0,class BaseClass0> on type class G3_C1499`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C514`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C514`2<class BaseClass0,class BaseClass0>::ClassMethod2386()
		ldstr "G3_C1499::ClassMethod2386.MI.16480()"
		ldstr "class G2_C514`2<class BaseClass0,class BaseClass0> on type class G3_C1499`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C514`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C514`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C514::Method7.9710<System.Object>()"
		ldstr "class G2_C514`2<class BaseClass0,class BaseClass0> on type class G3_C1499`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C514`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C514`2<class BaseClass0,class BaseClass0>::Method6<object>()
		ldstr "G2_C514::Method6.9708<System.Object>()"
		ldstr "class G2_C514`2<class BaseClass0,class BaseClass0> on type class G3_C1499`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C514`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C514`2<class BaseClass0,class BaseClass0>::Method5()
		ldstr "G3_C1499::Method5.16473()"
		ldstr "class G2_C514`2<class BaseClass0,class BaseClass0> on type class G3_C1499`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C514`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C514`2<class BaseClass0,class BaseClass0>::Method4()
		ldstr "G2_C514::Method4.9706()"
		ldstr "class G2_C514`2<class BaseClass0,class BaseClass0> on type class G3_C1499`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C514`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C514`2<class BaseClass0,class BaseClass0>::ClassMethod1339<object>()
		ldstr "G2_C514::ClassMethod1339.MI.9718<System.Object>()"
		ldstr "class G2_C514`2<class BaseClass0,class BaseClass0> on type class G3_C1499`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C514`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C514`2<class BaseClass0,class BaseClass0>::ClassMethod1338<object>()
		ldstr "G2_C514::ClassMethod1338.MI.9717<System.Object>()"
		ldstr "class G2_C514`2<class BaseClass0,class BaseClass0> on type class G3_C1499`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C514`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C514`2<class BaseClass0,class BaseClass0>::ClassMethod1337()
		ldstr "G2_C514::ClassMethod1337.MI.9716()"
		ldstr "class G2_C514`2<class BaseClass0,class BaseClass0> on type class G3_C1499`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C514`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C514`2<class BaseClass0,class BaseClass0>::ClassMethod1336()
		ldstr "G2_C514::ClassMethod1336.MI.9715()"
		ldstr "class G2_C514`2<class BaseClass0,class BaseClass0> on type class G3_C1499`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G2_C514::Method4.9706()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1499`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G3_C1499::Method5.16473()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1499`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C514::Method6.MI.9709<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1499`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G3_C1499`1<class BaseClass0>
		callvirt instance string class G3_C1499`1<class BaseClass0>::ClassMethod4390<object>()
		ldstr "G3_C1499::ClassMethod4390.16479<System.Object>()"
		ldstr "class G3_C1499`1<class BaseClass0> on type class G3_C1499`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1499`1<class BaseClass0>
		callvirt instance string class G3_C1499`1<class BaseClass0>::ClassMethod4389<object>()
		ldstr "G3_C1499::ClassMethod4389.16478<System.Object>()"
		ldstr "class G3_C1499`1<class BaseClass0> on type class G3_C1499`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1499`1<class BaseClass0>
		callvirt instance string class G3_C1499`1<class BaseClass0>::ClassMethod4388()
		ldstr "G3_C1499::ClassMethod4388.16477()"
		ldstr "class G3_C1499`1<class BaseClass0> on type class G3_C1499`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1499`1<class BaseClass0>
		callvirt instance string class G3_C1499`1<class BaseClass0>::ClassMethod4387()
		ldstr "G3_C1499::ClassMethod4387.16476()"
		ldstr "class G3_C1499`1<class BaseClass0> on type class G3_C1499`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1499`1<class BaseClass0>
		callvirt instance string class G3_C1499`1<class BaseClass0>::Method6<object>()
		ldstr "G3_C1499::Method6.16474<System.Object>()"
		ldstr "class G3_C1499`1<class BaseClass0> on type class G3_C1499`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1499`1<class BaseClass0>
		callvirt instance string class G3_C1499`1<class BaseClass0>::Method5()
		ldstr "G3_C1499::Method5.16473()"
		ldstr "class G3_C1499`1<class BaseClass0> on type class G3_C1499`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1499`1<class BaseClass0>
		callvirt instance string class G3_C1499`1<class BaseClass0>::Method4()
		ldstr "G3_C1499::Method4.16471()"
		ldstr "class G3_C1499`1<class BaseClass0> on type class G3_C1499`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1499`1<class BaseClass0>
		callvirt instance string class G3_C1499`1<class BaseClass0>::ClassMethod2389<object>()
		ldstr "G3_C1499::ClassMethod2389.MI.16483<System.Object>()"
		ldstr "class G3_C1499`1<class BaseClass0> on type class G3_C1499`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1499`1<class BaseClass0>
		callvirt instance string class G3_C1499`1<class BaseClass0>::ClassMethod2388<object>()
		ldstr "G3_C1499::ClassMethod2388.MI.16482<System.Object>()"
		ldstr "class G3_C1499`1<class BaseClass0> on type class G3_C1499`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1499`1<class BaseClass0>
		callvirt instance string class G3_C1499`1<class BaseClass0>::ClassMethod2387()
		ldstr "G3_C1499::ClassMethod2387.MI.16481()"
		ldstr "class G3_C1499`1<class BaseClass0> on type class G3_C1499`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1499`1<class BaseClass0>
		callvirt instance string class G3_C1499`1<class BaseClass0>::ClassMethod2386()
		ldstr "G3_C1499::ClassMethod2386.MI.16480()"
		ldstr "class G3_C1499`1<class BaseClass0> on type class G3_C1499`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1499`1<class BaseClass0>
		callvirt instance string class G3_C1499`1<class BaseClass0>::Method7<object>()
		ldstr "G2_C514::Method7.9710<System.Object>()"
		ldstr "class G3_C1499`1<class BaseClass0> on type class G3_C1499`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1499`1<class BaseClass0>
		callvirt instance string class G3_C1499`1<class BaseClass0>::ClassMethod1339<object>()
		ldstr "G2_C514::ClassMethod1339.MI.9718<System.Object>()"
		ldstr "class G3_C1499`1<class BaseClass0> on type class G3_C1499`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1499`1<class BaseClass0>
		callvirt instance string class G3_C1499`1<class BaseClass0>::ClassMethod1338<object>()
		ldstr "G2_C514::ClassMethod1338.MI.9717<System.Object>()"
		ldstr "class G3_C1499`1<class BaseClass0> on type class G3_C1499`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1499`1<class BaseClass0>
		callvirt instance string class G3_C1499`1<class BaseClass0>::ClassMethod1337()
		ldstr "G2_C514::ClassMethod1337.MI.9716()"
		ldstr "class G3_C1499`1<class BaseClass0> on type class G3_C1499`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1499`1<class BaseClass0>
		callvirt instance string class G3_C1499`1<class BaseClass0>::ClassMethod1336()
		ldstr "G2_C514::ClassMethod1336.MI.9715()"
		ldstr "class G3_C1499`1<class BaseClass0> on type class G3_C1499`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G3_C1499`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C9`2<class BaseClass0,class BaseClass0>::ClassMethod1339<object>()
		ldstr "G2_C514::ClassMethod1339.MI.9718<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G3_C1499`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C9`2<class BaseClass0,class BaseClass0>::ClassMethod1338<object>()
		ldstr "G2_C514::ClassMethod1338.MI.9717<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G3_C1499`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C9`2<class BaseClass0,class BaseClass0>::ClassMethod1337()
		ldstr "G2_C514::ClassMethod1337.MI.9716()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G3_C1499`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C9`2<class BaseClass0,class BaseClass0>::ClassMethod1336()
		ldstr "G2_C514::ClassMethod1336.MI.9715()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G3_C1499`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C9`2<class BaseClass0,class BaseClass0>::Method6<object>()
		ldstr "G1_C9::Method6.4838<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G3_C1499`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C9`2<class BaseClass0,class BaseClass0>::Method5()
		ldstr "G3_C1499::Method5.16473()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G3_C1499`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C9`2<class BaseClass0,class BaseClass0>::Method4()
		ldstr "G1_C9::Method4.4834()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G3_C1499`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C9`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G1_C9::Method7.4833<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G3_C1499`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C514::Method7.9710<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1499`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G3_C1499::Method4.MI.16472()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1499`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G3_C1499::Method5.16473()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1499`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G3_C1499::Method6.MI.16475<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1499`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C514::Method7.9710<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1499`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C514`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C514`2<class BaseClass1,class BaseClass0>::ClassMethod2389<object>()
		ldstr "G3_C1499::ClassMethod2389.MI.16483<System.Object>()"
		ldstr "class G2_C514`2<class BaseClass1,class BaseClass0> on type class G3_C1499`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C514`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C514`2<class BaseClass1,class BaseClass0>::ClassMethod2388<object>()
		ldstr "G3_C1499::ClassMethod2388.MI.16482<System.Object>()"
		ldstr "class G2_C514`2<class BaseClass1,class BaseClass0> on type class G3_C1499`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C514`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C514`2<class BaseClass1,class BaseClass0>::ClassMethod2387()
		ldstr "G3_C1499::ClassMethod2387.MI.16481()"
		ldstr "class G2_C514`2<class BaseClass1,class BaseClass0> on type class G3_C1499`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C514`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C514`2<class BaseClass1,class BaseClass0>::ClassMethod2386()
		ldstr "G3_C1499::ClassMethod2386.MI.16480()"
		ldstr "class G2_C514`2<class BaseClass1,class BaseClass0> on type class G3_C1499`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C514`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C514`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G2_C514::Method7.9710<System.Object>()"
		ldstr "class G2_C514`2<class BaseClass1,class BaseClass0> on type class G3_C1499`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C514`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C514`2<class BaseClass1,class BaseClass0>::Method6<object>()
		ldstr "G2_C514::Method6.9708<System.Object>()"
		ldstr "class G2_C514`2<class BaseClass1,class BaseClass0> on type class G3_C1499`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C514`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C514`2<class BaseClass1,class BaseClass0>::Method5()
		ldstr "G3_C1499::Method5.16473()"
		ldstr "class G2_C514`2<class BaseClass1,class BaseClass0> on type class G3_C1499`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C514`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C514`2<class BaseClass1,class BaseClass0>::Method4()
		ldstr "G2_C514::Method4.9706()"
		ldstr "class G2_C514`2<class BaseClass1,class BaseClass0> on type class G3_C1499`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C514`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C514`2<class BaseClass1,class BaseClass0>::ClassMethod1339<object>()
		ldstr "G2_C514::ClassMethod1339.MI.9718<System.Object>()"
		ldstr "class G2_C514`2<class BaseClass1,class BaseClass0> on type class G3_C1499`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C514`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C514`2<class BaseClass1,class BaseClass0>::ClassMethod1338<object>()
		ldstr "G2_C514::ClassMethod1338.MI.9717<System.Object>()"
		ldstr "class G2_C514`2<class BaseClass1,class BaseClass0> on type class G3_C1499`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C514`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C514`2<class BaseClass1,class BaseClass0>::ClassMethod1337()
		ldstr "G2_C514::ClassMethod1337.MI.9716()"
		ldstr "class G2_C514`2<class BaseClass1,class BaseClass0> on type class G3_C1499`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C514`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C514`2<class BaseClass1,class BaseClass0>::ClassMethod1336()
		ldstr "G2_C514::ClassMethod1336.MI.9715()"
		ldstr "class G2_C514`2<class BaseClass1,class BaseClass0> on type class G3_C1499`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G2_C514::Method4.9706()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1499`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G3_C1499::Method5.16473()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1499`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C514::Method6.MI.9709<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1499`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G3_C1499`1<class BaseClass1>
		callvirt instance string class G3_C1499`1<class BaseClass1>::ClassMethod4390<object>()
		ldstr "G3_C1499::ClassMethod4390.16479<System.Object>()"
		ldstr "class G3_C1499`1<class BaseClass1> on type class G3_C1499`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1499`1<class BaseClass1>
		callvirt instance string class G3_C1499`1<class BaseClass1>::ClassMethod4389<object>()
		ldstr "G3_C1499::ClassMethod4389.16478<System.Object>()"
		ldstr "class G3_C1499`1<class BaseClass1> on type class G3_C1499`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1499`1<class BaseClass1>
		callvirt instance string class G3_C1499`1<class BaseClass1>::ClassMethod4388()
		ldstr "G3_C1499::ClassMethod4388.16477()"
		ldstr "class G3_C1499`1<class BaseClass1> on type class G3_C1499`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1499`1<class BaseClass1>
		callvirt instance string class G3_C1499`1<class BaseClass1>::ClassMethod4387()
		ldstr "G3_C1499::ClassMethod4387.16476()"
		ldstr "class G3_C1499`1<class BaseClass1> on type class G3_C1499`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1499`1<class BaseClass1>
		callvirt instance string class G3_C1499`1<class BaseClass1>::Method6<object>()
		ldstr "G3_C1499::Method6.16474<System.Object>()"
		ldstr "class G3_C1499`1<class BaseClass1> on type class G3_C1499`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1499`1<class BaseClass1>
		callvirt instance string class G3_C1499`1<class BaseClass1>::Method5()
		ldstr "G3_C1499::Method5.16473()"
		ldstr "class G3_C1499`1<class BaseClass1> on type class G3_C1499`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1499`1<class BaseClass1>
		callvirt instance string class G3_C1499`1<class BaseClass1>::Method4()
		ldstr "G3_C1499::Method4.16471()"
		ldstr "class G3_C1499`1<class BaseClass1> on type class G3_C1499`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1499`1<class BaseClass1>
		callvirt instance string class G3_C1499`1<class BaseClass1>::ClassMethod2389<object>()
		ldstr "G3_C1499::ClassMethod2389.MI.16483<System.Object>()"
		ldstr "class G3_C1499`1<class BaseClass1> on type class G3_C1499`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1499`1<class BaseClass1>
		callvirt instance string class G3_C1499`1<class BaseClass1>::ClassMethod2388<object>()
		ldstr "G3_C1499::ClassMethod2388.MI.16482<System.Object>()"
		ldstr "class G3_C1499`1<class BaseClass1> on type class G3_C1499`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1499`1<class BaseClass1>
		callvirt instance string class G3_C1499`1<class BaseClass1>::ClassMethod2387()
		ldstr "G3_C1499::ClassMethod2387.MI.16481()"
		ldstr "class G3_C1499`1<class BaseClass1> on type class G3_C1499`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1499`1<class BaseClass1>
		callvirt instance string class G3_C1499`1<class BaseClass1>::ClassMethod2386()
		ldstr "G3_C1499::ClassMethod2386.MI.16480()"
		ldstr "class G3_C1499`1<class BaseClass1> on type class G3_C1499`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1499`1<class BaseClass1>
		callvirt instance string class G3_C1499`1<class BaseClass1>::Method7<object>()
		ldstr "G2_C514::Method7.9710<System.Object>()"
		ldstr "class G3_C1499`1<class BaseClass1> on type class G3_C1499`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1499`1<class BaseClass1>
		callvirt instance string class G3_C1499`1<class BaseClass1>::ClassMethod1339<object>()
		ldstr "G2_C514::ClassMethod1339.MI.9718<System.Object>()"
		ldstr "class G3_C1499`1<class BaseClass1> on type class G3_C1499`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1499`1<class BaseClass1>
		callvirt instance string class G3_C1499`1<class BaseClass1>::ClassMethod1338<object>()
		ldstr "G2_C514::ClassMethod1338.MI.9717<System.Object>()"
		ldstr "class G3_C1499`1<class BaseClass1> on type class G3_C1499`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1499`1<class BaseClass1>
		callvirt instance string class G3_C1499`1<class BaseClass1>::ClassMethod1337()
		ldstr "G2_C514::ClassMethod1337.MI.9716()"
		ldstr "class G3_C1499`1<class BaseClass1> on type class G3_C1499`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1499`1<class BaseClass1>
		callvirt instance string class G3_C1499`1<class BaseClass1>::ClassMethod1336()
		ldstr "G2_C514::ClassMethod1336.MI.9715()"
		ldstr "class G3_C1499`1<class BaseClass1> on type class G3_C1499`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C514`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C9`2<class BaseClass0,class BaseClass0>::ClassMethod1339<object>()
		ldstr "G2_C514::ClassMethod1339.MI.9718<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C514`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C9`2<class BaseClass0,class BaseClass0>::ClassMethod1338<object>()
		ldstr "G2_C514::ClassMethod1338.MI.9717<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C514`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C9`2<class BaseClass0,class BaseClass0>::ClassMethod1337()
		ldstr "G2_C514::ClassMethod1337.MI.9716()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C514`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C9`2<class BaseClass0,class BaseClass0>::ClassMethod1336()
		ldstr "G2_C514::ClassMethod1336.MI.9715()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C514`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C9`2<class BaseClass0,class BaseClass0>::Method6<object>()
		ldstr "G1_C9::Method6.4838<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C514`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C9`2<class BaseClass0,class BaseClass0>::Method5()
		ldstr "G2_C514::Method5.9707()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C514`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C9`2<class BaseClass0,class BaseClass0>::Method4()
		ldstr "G1_C9::Method4.4834()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C514`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C9`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G1_C9::Method7.4833<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C514`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C514::Method7.9710<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C514`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C514::Method4.9706()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C514`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C514::Method5.9707()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C514`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C514::Method6.MI.9709<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C514`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C514::Method7.9710<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C514`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C514`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C514`2<class BaseClass0,class BaseClass0>::ClassMethod2389<object>()
		ldstr "G2_C514::ClassMethod2389.9714<System.Object>()"
		ldstr "class G2_C514`2<class BaseClass0,class BaseClass0> on type class G2_C514`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C514`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C514`2<class BaseClass0,class BaseClass0>::ClassMethod2388<object>()
		ldstr "G2_C514::ClassMethod2388.9713<System.Object>()"
		ldstr "class G2_C514`2<class BaseClass0,class BaseClass0> on type class G2_C514`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C514`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C514`2<class BaseClass0,class BaseClass0>::ClassMethod2387()
		ldstr "G2_C514::ClassMethod2387.9712()"
		ldstr "class G2_C514`2<class BaseClass0,class BaseClass0> on type class G2_C514`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C514`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C514`2<class BaseClass0,class BaseClass0>::ClassMethod2386()
		ldstr "G2_C514::ClassMethod2386.9711()"
		ldstr "class G2_C514`2<class BaseClass0,class BaseClass0> on type class G2_C514`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C514`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C514`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C514::Method7.9710<System.Object>()"
		ldstr "class G2_C514`2<class BaseClass0,class BaseClass0> on type class G2_C514`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C514`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C514`2<class BaseClass0,class BaseClass0>::Method6<object>()
		ldstr "G2_C514::Method6.9708<System.Object>()"
		ldstr "class G2_C514`2<class BaseClass0,class BaseClass0> on type class G2_C514`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C514`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C514`2<class BaseClass0,class BaseClass0>::Method5()
		ldstr "G2_C514::Method5.9707()"
		ldstr "class G2_C514`2<class BaseClass0,class BaseClass0> on type class G2_C514`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C514`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C514`2<class BaseClass0,class BaseClass0>::Method4()
		ldstr "G2_C514::Method4.9706()"
		ldstr "class G2_C514`2<class BaseClass0,class BaseClass0> on type class G2_C514`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C514`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C514`2<class BaseClass0,class BaseClass0>::ClassMethod1339<object>()
		ldstr "G2_C514::ClassMethod1339.MI.9718<System.Object>()"
		ldstr "class G2_C514`2<class BaseClass0,class BaseClass0> on type class G2_C514`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C514`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C514`2<class BaseClass0,class BaseClass0>::ClassMethod1338<object>()
		ldstr "G2_C514::ClassMethod1338.MI.9717<System.Object>()"
		ldstr "class G2_C514`2<class BaseClass0,class BaseClass0> on type class G2_C514`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C514`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C514`2<class BaseClass0,class BaseClass0>::ClassMethod1337()
		ldstr "G2_C514::ClassMethod1337.MI.9716()"
		ldstr "class G2_C514`2<class BaseClass0,class BaseClass0> on type class G2_C514`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C514`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C514`2<class BaseClass0,class BaseClass0>::ClassMethod1336()
		ldstr "G2_C514::ClassMethod1336.MI.9715()"
		ldstr "class G2_C514`2<class BaseClass0,class BaseClass0> on type class G2_C514`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G2_C514::Method4.9706()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C514`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G2_C514::Method5.9707()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C514`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C514::Method6.MI.9709<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C514`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C514`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C9`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C9`2<class BaseClass1,class BaseClass1>::ClassMethod1339<object>()
		ldstr "G2_C514::ClassMethod1339.MI.9718<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass1,class BaseClass1> on type class G2_C514`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C9`2<class BaseClass1,class BaseClass1>::ClassMethod1338<object>()
		ldstr "G2_C514::ClassMethod1338.MI.9717<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass1,class BaseClass1> on type class G2_C514`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C9`2<class BaseClass1,class BaseClass1>::ClassMethod1337()
		ldstr "G2_C514::ClassMethod1337.MI.9716()"
		ldstr "class G1_C9`2<class BaseClass1,class BaseClass1> on type class G2_C514`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C9`2<class BaseClass1,class BaseClass1>::ClassMethod1336()
		ldstr "G2_C514::ClassMethod1336.MI.9715()"
		ldstr "class G1_C9`2<class BaseClass1,class BaseClass1> on type class G2_C514`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C9`2<class BaseClass1,class BaseClass1>::Method6<object>()
		ldstr "G1_C9::Method6.4838<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass1,class BaseClass1> on type class G2_C514`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C9`2<class BaseClass1,class BaseClass1>::Method5()
		ldstr "G2_C514::Method5.9707()"
		ldstr "class G1_C9`2<class BaseClass1,class BaseClass1> on type class G2_C514`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C9`2<class BaseClass1,class BaseClass1>::Method4()
		ldstr "G1_C9::Method4.4834()"
		ldstr "class G1_C9`2<class BaseClass1,class BaseClass1> on type class G2_C514`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C9`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C9::Method7.4833<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass1,class BaseClass1> on type class G2_C514`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C514::Method7.9710<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C514`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C514::Method4.9706()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C514`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C514::Method5.9707()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C514`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C514::Method6.MI.9709<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C514`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C514`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C514`2<class BaseClass0,class BaseClass1>::ClassMethod2389<object>()
		ldstr "G2_C514::ClassMethod2389.9714<System.Object>()"
		ldstr "class G2_C514`2<class BaseClass0,class BaseClass1> on type class G2_C514`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C514`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C514`2<class BaseClass0,class BaseClass1>::ClassMethod2388<object>()
		ldstr "G2_C514::ClassMethod2388.9713<System.Object>()"
		ldstr "class G2_C514`2<class BaseClass0,class BaseClass1> on type class G2_C514`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C514`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C514`2<class BaseClass0,class BaseClass1>::ClassMethod2387()
		ldstr "G2_C514::ClassMethod2387.9712()"
		ldstr "class G2_C514`2<class BaseClass0,class BaseClass1> on type class G2_C514`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C514`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C514`2<class BaseClass0,class BaseClass1>::ClassMethod2386()
		ldstr "G2_C514::ClassMethod2386.9711()"
		ldstr "class G2_C514`2<class BaseClass0,class BaseClass1> on type class G2_C514`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C514`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C514`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C514::Method7.9710<System.Object>()"
		ldstr "class G2_C514`2<class BaseClass0,class BaseClass1> on type class G2_C514`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C514`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C514`2<class BaseClass0,class BaseClass1>::Method6<object>()
		ldstr "G2_C514::Method6.9708<System.Object>()"
		ldstr "class G2_C514`2<class BaseClass0,class BaseClass1> on type class G2_C514`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C514`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C514`2<class BaseClass0,class BaseClass1>::Method5()
		ldstr "G2_C514::Method5.9707()"
		ldstr "class G2_C514`2<class BaseClass0,class BaseClass1> on type class G2_C514`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C514`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C514`2<class BaseClass0,class BaseClass1>::Method4()
		ldstr "G2_C514::Method4.9706()"
		ldstr "class G2_C514`2<class BaseClass0,class BaseClass1> on type class G2_C514`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C514`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C514`2<class BaseClass0,class BaseClass1>::ClassMethod1339<object>()
		ldstr "G2_C514::ClassMethod1339.MI.9718<System.Object>()"
		ldstr "class G2_C514`2<class BaseClass0,class BaseClass1> on type class G2_C514`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C514`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C514`2<class BaseClass0,class BaseClass1>::ClassMethod1338<object>()
		ldstr "G2_C514::ClassMethod1338.MI.9717<System.Object>()"
		ldstr "class G2_C514`2<class BaseClass0,class BaseClass1> on type class G2_C514`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C514`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C514`2<class BaseClass0,class BaseClass1>::ClassMethod1337()
		ldstr "G2_C514::ClassMethod1337.MI.9716()"
		ldstr "class G2_C514`2<class BaseClass0,class BaseClass1> on type class G2_C514`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C514`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C514`2<class BaseClass0,class BaseClass1>::ClassMethod1336()
		ldstr "G2_C514::ClassMethod1336.MI.9715()"
		ldstr "class G2_C514`2<class BaseClass0,class BaseClass1> on type class G2_C514`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G2_C514::Method4.9706()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C514`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G2_C514::Method5.9707()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C514`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C514::Method6.MI.9709<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C514`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C514`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C9`2<class BaseClass0,class BaseClass0>::ClassMethod1339<object>()
		ldstr "G2_C514::ClassMethod1339.MI.9718<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C514`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C9`2<class BaseClass0,class BaseClass0>::ClassMethod1338<object>()
		ldstr "G2_C514::ClassMethod1338.MI.9717<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C514`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C9`2<class BaseClass0,class BaseClass0>::ClassMethod1337()
		ldstr "G2_C514::ClassMethod1337.MI.9716()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C514`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C9`2<class BaseClass0,class BaseClass0>::ClassMethod1336()
		ldstr "G2_C514::ClassMethod1336.MI.9715()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C514`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C9`2<class BaseClass0,class BaseClass0>::Method6<object>()
		ldstr "G1_C9::Method6.4838<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C514`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C9`2<class BaseClass0,class BaseClass0>::Method5()
		ldstr "G2_C514::Method5.9707()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C514`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C9`2<class BaseClass0,class BaseClass0>::Method4()
		ldstr "G1_C9::Method4.4834()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C514`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C9`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G1_C9::Method7.4833<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C514`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C514::Method7.9710<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C514`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C514::Method4.9706()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C514`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C514::Method5.9707()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C514`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C514::Method6.MI.9709<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C514`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C514::Method7.9710<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C514`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C514`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C514`2<class BaseClass1,class BaseClass0>::ClassMethod2389<object>()
		ldstr "G2_C514::ClassMethod2389.9714<System.Object>()"
		ldstr "class G2_C514`2<class BaseClass1,class BaseClass0> on type class G2_C514`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C514`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C514`2<class BaseClass1,class BaseClass0>::ClassMethod2388<object>()
		ldstr "G2_C514::ClassMethod2388.9713<System.Object>()"
		ldstr "class G2_C514`2<class BaseClass1,class BaseClass0> on type class G2_C514`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C514`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C514`2<class BaseClass1,class BaseClass0>::ClassMethod2387()
		ldstr "G2_C514::ClassMethod2387.9712()"
		ldstr "class G2_C514`2<class BaseClass1,class BaseClass0> on type class G2_C514`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C514`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C514`2<class BaseClass1,class BaseClass0>::ClassMethod2386()
		ldstr "G2_C514::ClassMethod2386.9711()"
		ldstr "class G2_C514`2<class BaseClass1,class BaseClass0> on type class G2_C514`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C514`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C514`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G2_C514::Method7.9710<System.Object>()"
		ldstr "class G2_C514`2<class BaseClass1,class BaseClass0> on type class G2_C514`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C514`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C514`2<class BaseClass1,class BaseClass0>::Method6<object>()
		ldstr "G2_C514::Method6.9708<System.Object>()"
		ldstr "class G2_C514`2<class BaseClass1,class BaseClass0> on type class G2_C514`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C514`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C514`2<class BaseClass1,class BaseClass0>::Method5()
		ldstr "G2_C514::Method5.9707()"
		ldstr "class G2_C514`2<class BaseClass1,class BaseClass0> on type class G2_C514`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C514`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C514`2<class BaseClass1,class BaseClass0>::Method4()
		ldstr "G2_C514::Method4.9706()"
		ldstr "class G2_C514`2<class BaseClass1,class BaseClass0> on type class G2_C514`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C514`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C514`2<class BaseClass1,class BaseClass0>::ClassMethod1339<object>()
		ldstr "G2_C514::ClassMethod1339.MI.9718<System.Object>()"
		ldstr "class G2_C514`2<class BaseClass1,class BaseClass0> on type class G2_C514`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C514`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C514`2<class BaseClass1,class BaseClass0>::ClassMethod1338<object>()
		ldstr "G2_C514::ClassMethod1338.MI.9717<System.Object>()"
		ldstr "class G2_C514`2<class BaseClass1,class BaseClass0> on type class G2_C514`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C514`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C514`2<class BaseClass1,class BaseClass0>::ClassMethod1337()
		ldstr "G2_C514::ClassMethod1337.MI.9716()"
		ldstr "class G2_C514`2<class BaseClass1,class BaseClass0> on type class G2_C514`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C514`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C514`2<class BaseClass1,class BaseClass0>::ClassMethod1336()
		ldstr "G2_C514::ClassMethod1336.MI.9715()"
		ldstr "class G2_C514`2<class BaseClass1,class BaseClass0> on type class G2_C514`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G2_C514::Method4.9706()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C514`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G2_C514::Method5.9707()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C514`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C514::Method6.MI.9709<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C514`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C514`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C9`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C9`2<class BaseClass1,class BaseClass1>::ClassMethod1339<object>()
		ldstr "G2_C514::ClassMethod1339.MI.9718<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass1,class BaseClass1> on type class G2_C514`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C9`2<class BaseClass1,class BaseClass1>::ClassMethod1338<object>()
		ldstr "G2_C514::ClassMethod1338.MI.9717<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass1,class BaseClass1> on type class G2_C514`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C9`2<class BaseClass1,class BaseClass1>::ClassMethod1337()
		ldstr "G2_C514::ClassMethod1337.MI.9716()"
		ldstr "class G1_C9`2<class BaseClass1,class BaseClass1> on type class G2_C514`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C9`2<class BaseClass1,class BaseClass1>::ClassMethod1336()
		ldstr "G2_C514::ClassMethod1336.MI.9715()"
		ldstr "class G1_C9`2<class BaseClass1,class BaseClass1> on type class G2_C514`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C9`2<class BaseClass1,class BaseClass1>::Method6<object>()
		ldstr "G1_C9::Method6.4838<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass1,class BaseClass1> on type class G2_C514`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C9`2<class BaseClass1,class BaseClass1>::Method5()
		ldstr "G2_C514::Method5.9707()"
		ldstr "class G1_C9`2<class BaseClass1,class BaseClass1> on type class G2_C514`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C9`2<class BaseClass1,class BaseClass1>::Method4()
		ldstr "G1_C9::Method4.4834()"
		ldstr "class G1_C9`2<class BaseClass1,class BaseClass1> on type class G2_C514`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C9`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C9::Method7.4833<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass1,class BaseClass1> on type class G2_C514`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C514::Method7.9710<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C514`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C514::Method4.9706()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C514`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C514::Method5.9707()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C514`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C514::Method6.MI.9709<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C514`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C514`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C514`2<class BaseClass1,class BaseClass1>::ClassMethod2389<object>()
		ldstr "G2_C514::ClassMethod2389.9714<System.Object>()"
		ldstr "class G2_C514`2<class BaseClass1,class BaseClass1> on type class G2_C514`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C514`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C514`2<class BaseClass1,class BaseClass1>::ClassMethod2388<object>()
		ldstr "G2_C514::ClassMethod2388.9713<System.Object>()"
		ldstr "class G2_C514`2<class BaseClass1,class BaseClass1> on type class G2_C514`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C514`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C514`2<class BaseClass1,class BaseClass1>::ClassMethod2387()
		ldstr "G2_C514::ClassMethod2387.9712()"
		ldstr "class G2_C514`2<class BaseClass1,class BaseClass1> on type class G2_C514`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C514`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C514`2<class BaseClass1,class BaseClass1>::ClassMethod2386()
		ldstr "G2_C514::ClassMethod2386.9711()"
		ldstr "class G2_C514`2<class BaseClass1,class BaseClass1> on type class G2_C514`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C514`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C514`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C514::Method7.9710<System.Object>()"
		ldstr "class G2_C514`2<class BaseClass1,class BaseClass1> on type class G2_C514`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C514`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C514`2<class BaseClass1,class BaseClass1>::Method6<object>()
		ldstr "G2_C514::Method6.9708<System.Object>()"
		ldstr "class G2_C514`2<class BaseClass1,class BaseClass1> on type class G2_C514`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C514`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C514`2<class BaseClass1,class BaseClass1>::Method5()
		ldstr "G2_C514::Method5.9707()"
		ldstr "class G2_C514`2<class BaseClass1,class BaseClass1> on type class G2_C514`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C514`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C514`2<class BaseClass1,class BaseClass1>::Method4()
		ldstr "G2_C514::Method4.9706()"
		ldstr "class G2_C514`2<class BaseClass1,class BaseClass1> on type class G2_C514`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C514`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C514`2<class BaseClass1,class BaseClass1>::ClassMethod1339<object>()
		ldstr "G2_C514::ClassMethod1339.MI.9718<System.Object>()"
		ldstr "class G2_C514`2<class BaseClass1,class BaseClass1> on type class G2_C514`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C514`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C514`2<class BaseClass1,class BaseClass1>::ClassMethod1338<object>()
		ldstr "G2_C514::ClassMethod1338.MI.9717<System.Object>()"
		ldstr "class G2_C514`2<class BaseClass1,class BaseClass1> on type class G2_C514`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C514`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C514`2<class BaseClass1,class BaseClass1>::ClassMethod1337()
		ldstr "G2_C514::ClassMethod1337.MI.9716()"
		ldstr "class G2_C514`2<class BaseClass1,class BaseClass1> on type class G2_C514`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C514`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C514`2<class BaseClass1,class BaseClass1>::ClassMethod1336()
		ldstr "G2_C514::ClassMethod1336.MI.9715()"
		ldstr "class G2_C514`2<class BaseClass1,class BaseClass1> on type class G2_C514`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G2_C514::Method4.9706()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C514`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G2_C514::Method5.9707()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C514`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C514::Method6.MI.9709<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C514`2<class BaseClass1,class BaseClass1>"
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
		newobj instance void class G3_C1499`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C514::ClassMethod1336.MI.9715()#G2_C514::ClassMethod1337.MI.9716()#G2_C514::ClassMethod1338.MI.9717<System.Object>()#G2_C514::ClassMethod1339.MI.9718<System.Object>()#G1_C9::Method4.4834()#G3_C1499::Method5.16473()#G1_C9::Method6.4838<System.Object>()#G1_C9::Method7.4833<System.Object>()#"
		call void Framework::M.G1_C9.T.T<class BaseClass0,class BaseClass0,class G3_C1499`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C514::ClassMethod1336.MI.9715()#G2_C514::ClassMethod1337.MI.9716()#G2_C514::ClassMethod1338.MI.9717<System.Object>()#G2_C514::ClassMethod1339.MI.9718<System.Object>()#G1_C9::Method4.4834()#G3_C1499::Method5.16473()#G1_C9::Method6.4838<System.Object>()#G1_C9::Method7.4833<System.Object>()#"
		call void Framework::M.G1_C9.A.T<class BaseClass0,class G3_C1499`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C514::ClassMethod1336.MI.9715()#G2_C514::ClassMethod1337.MI.9716()#G2_C514::ClassMethod1338.MI.9717<System.Object>()#G2_C514::ClassMethod1339.MI.9718<System.Object>()#G1_C9::Method4.4834()#G3_C1499::Method5.16473()#G1_C9::Method6.4838<System.Object>()#G1_C9::Method7.4833<System.Object>()#"
		call void Framework::M.G1_C9.A.A<class G3_C1499`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C514::Method7.9710<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G3_C1499`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C514::Method7.9710<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass0,class G3_C1499`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C514::Method7.9710<System.Object>()#"
		call void Framework::M.IBase2.A.A<class G3_C1499`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1499::Method4.MI.16472()#G3_C1499::Method5.16473()#G3_C1499::Method6.MI.16475<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G3_C1499`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1499::Method4.MI.16472()#G3_C1499::Method5.16473()#G3_C1499::Method6.MI.16475<System.Object>()#"
		call void Framework::M.IBase1.A<class G3_C1499`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C514::Method7.9710<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G3_C1499`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C514::Method7.9710<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G3_C1499`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C514::Method7.9710<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G3_C1499`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C514::ClassMethod1336.MI.9715()#G2_C514::ClassMethod1337.MI.9716()#G2_C514::ClassMethod1338.MI.9717<System.Object>()#G2_C514::ClassMethod1339.MI.9718<System.Object>()#G3_C1499::ClassMethod2386.MI.16480()#G3_C1499::ClassMethod2387.MI.16481()#G3_C1499::ClassMethod2388.MI.16482<System.Object>()#G3_C1499::ClassMethod2389.MI.16483<System.Object>()#G2_C514::Method4.9706()#G3_C1499::Method5.16473()#G2_C514::Method6.9708<System.Object>()#G2_C514::Method7.9710<System.Object>()#"
		call void Framework::M.G2_C514.T.T<class BaseClass0,class BaseClass0,class G3_C1499`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C514::ClassMethod1336.MI.9715()#G2_C514::ClassMethod1337.MI.9716()#G2_C514::ClassMethod1338.MI.9717<System.Object>()#G2_C514::ClassMethod1339.MI.9718<System.Object>()#G3_C1499::ClassMethod2386.MI.16480()#G3_C1499::ClassMethod2387.MI.16481()#G3_C1499::ClassMethod2388.MI.16482<System.Object>()#G3_C1499::ClassMethod2389.MI.16483<System.Object>()#G2_C514::Method4.9706()#G3_C1499::Method5.16473()#G2_C514::Method6.9708<System.Object>()#G2_C514::Method7.9710<System.Object>()#"
		call void Framework::M.G2_C514.A.T<class BaseClass0,class G3_C1499`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C514::ClassMethod1336.MI.9715()#G2_C514::ClassMethod1337.MI.9716()#G2_C514::ClassMethod1338.MI.9717<System.Object>()#G2_C514::ClassMethod1339.MI.9718<System.Object>()#G3_C1499::ClassMethod2386.MI.16480()#G3_C1499::ClassMethod2387.MI.16481()#G3_C1499::ClassMethod2388.MI.16482<System.Object>()#G3_C1499::ClassMethod2389.MI.16483<System.Object>()#G2_C514::Method4.9706()#G3_C1499::Method5.16473()#G2_C514::Method6.9708<System.Object>()#G2_C514::Method7.9710<System.Object>()#"
		call void Framework::M.G2_C514.A.A<class G3_C1499`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C514::Method4.9706()#G3_C1499::Method5.16473()#G2_C514::Method6.MI.9709<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass1,class G3_C1499`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C514::Method4.9706()#G3_C1499::Method5.16473()#G2_C514::Method6.MI.9709<System.Object>()#"
		call void Framework::M.IBase1.B<class G3_C1499`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C514::ClassMethod1336.MI.9715()#G2_C514::ClassMethod1337.MI.9716()#G2_C514::ClassMethod1338.MI.9717<System.Object>()#G2_C514::ClassMethod1339.MI.9718<System.Object>()#G3_C1499::ClassMethod2386.MI.16480()#G3_C1499::ClassMethod2387.MI.16481()#G3_C1499::ClassMethod2388.MI.16482<System.Object>()#G3_C1499::ClassMethod2389.MI.16483<System.Object>()#G3_C1499::ClassMethod4387.16476()#G3_C1499::ClassMethod4388.16477()#G3_C1499::ClassMethod4389.16478<System.Object>()#G3_C1499::ClassMethod4390.16479<System.Object>()#G3_C1499::Method4.16471()#G3_C1499::Method5.16473()#G3_C1499::Method6.16474<System.Object>()#G2_C514::Method7.9710<System.Object>()#"
		call void Framework::M.G3_C1499.T<class BaseClass0,class G3_C1499`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C514::ClassMethod1336.MI.9715()#G2_C514::ClassMethod1337.MI.9716()#G2_C514::ClassMethod1338.MI.9717<System.Object>()#G2_C514::ClassMethod1339.MI.9718<System.Object>()#G3_C1499::ClassMethod2386.MI.16480()#G3_C1499::ClassMethod2387.MI.16481()#G3_C1499::ClassMethod2388.MI.16482<System.Object>()#G3_C1499::ClassMethod2389.MI.16483<System.Object>()#G3_C1499::ClassMethod4387.16476()#G3_C1499::ClassMethod4388.16477()#G3_C1499::ClassMethod4389.16478<System.Object>()#G3_C1499::ClassMethod4390.16479<System.Object>()#G3_C1499::Method4.16471()#G3_C1499::Method5.16473()#G3_C1499::Method6.16474<System.Object>()#G2_C514::Method7.9710<System.Object>()#"
		call void Framework::M.G3_C1499.A<class G3_C1499`1<class BaseClass0>>(!!0,string)
		newobj instance void class G3_C1499`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C514::ClassMethod1336.MI.9715()#G2_C514::ClassMethod1337.MI.9716()#G2_C514::ClassMethod1338.MI.9717<System.Object>()#G2_C514::ClassMethod1339.MI.9718<System.Object>()#G1_C9::Method4.4834()#G3_C1499::Method5.16473()#G1_C9::Method6.4838<System.Object>()#G1_C9::Method7.4833<System.Object>()#"
		call void Framework::M.G1_C9.T.T<class BaseClass0,class BaseClass0,class G3_C1499`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C514::ClassMethod1336.MI.9715()#G2_C514::ClassMethod1337.MI.9716()#G2_C514::ClassMethod1338.MI.9717<System.Object>()#G2_C514::ClassMethod1339.MI.9718<System.Object>()#G1_C9::Method4.4834()#G3_C1499::Method5.16473()#G1_C9::Method6.4838<System.Object>()#G1_C9::Method7.4833<System.Object>()#"
		call void Framework::M.G1_C9.A.T<class BaseClass0,class G3_C1499`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C514::ClassMethod1336.MI.9715()#G2_C514::ClassMethod1337.MI.9716()#G2_C514::ClassMethod1338.MI.9717<System.Object>()#G2_C514::ClassMethod1339.MI.9718<System.Object>()#G1_C9::Method4.4834()#G3_C1499::Method5.16473()#G1_C9::Method6.4838<System.Object>()#G1_C9::Method7.4833<System.Object>()#"
		call void Framework::M.G1_C9.A.A<class G3_C1499`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C514::Method7.9710<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G3_C1499`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C514::Method7.9710<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass0,class G3_C1499`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C514::Method7.9710<System.Object>()#"
		call void Framework::M.IBase2.A.A<class G3_C1499`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1499::Method4.MI.16472()#G3_C1499::Method5.16473()#G3_C1499::Method6.MI.16475<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G3_C1499`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1499::Method4.MI.16472()#G3_C1499::Method5.16473()#G3_C1499::Method6.MI.16475<System.Object>()#"
		call void Framework::M.IBase1.A<class G3_C1499`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C514::Method7.9710<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G3_C1499`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C514::Method7.9710<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G3_C1499`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C514::Method7.9710<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G3_C1499`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C514::ClassMethod1336.MI.9715()#G2_C514::ClassMethod1337.MI.9716()#G2_C514::ClassMethod1338.MI.9717<System.Object>()#G2_C514::ClassMethod1339.MI.9718<System.Object>()#G3_C1499::ClassMethod2386.MI.16480()#G3_C1499::ClassMethod2387.MI.16481()#G3_C1499::ClassMethod2388.MI.16482<System.Object>()#G3_C1499::ClassMethod2389.MI.16483<System.Object>()#G2_C514::Method4.9706()#G3_C1499::Method5.16473()#G2_C514::Method6.9708<System.Object>()#G2_C514::Method7.9710<System.Object>()#"
		call void Framework::M.G2_C514.T.T<class BaseClass1,class BaseClass0,class G3_C1499`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C514::ClassMethod1336.MI.9715()#G2_C514::ClassMethod1337.MI.9716()#G2_C514::ClassMethod1338.MI.9717<System.Object>()#G2_C514::ClassMethod1339.MI.9718<System.Object>()#G3_C1499::ClassMethod2386.MI.16480()#G3_C1499::ClassMethod2387.MI.16481()#G3_C1499::ClassMethod2388.MI.16482<System.Object>()#G3_C1499::ClassMethod2389.MI.16483<System.Object>()#G2_C514::Method4.9706()#G3_C1499::Method5.16473()#G2_C514::Method6.9708<System.Object>()#G2_C514::Method7.9710<System.Object>()#"
		call void Framework::M.G2_C514.B.T<class BaseClass0,class G3_C1499`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C514::ClassMethod1336.MI.9715()#G2_C514::ClassMethod1337.MI.9716()#G2_C514::ClassMethod1338.MI.9717<System.Object>()#G2_C514::ClassMethod1339.MI.9718<System.Object>()#G3_C1499::ClassMethod2386.MI.16480()#G3_C1499::ClassMethod2387.MI.16481()#G3_C1499::ClassMethod2388.MI.16482<System.Object>()#G3_C1499::ClassMethod2389.MI.16483<System.Object>()#G2_C514::Method4.9706()#G3_C1499::Method5.16473()#G2_C514::Method6.9708<System.Object>()#G2_C514::Method7.9710<System.Object>()#"
		call void Framework::M.G2_C514.B.A<class G3_C1499`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C514::Method4.9706()#G3_C1499::Method5.16473()#G2_C514::Method6.MI.9709<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass1,class G3_C1499`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C514::Method4.9706()#G3_C1499::Method5.16473()#G2_C514::Method6.MI.9709<System.Object>()#"
		call void Framework::M.IBase1.B<class G3_C1499`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C514::ClassMethod1336.MI.9715()#G2_C514::ClassMethod1337.MI.9716()#G2_C514::ClassMethod1338.MI.9717<System.Object>()#G2_C514::ClassMethod1339.MI.9718<System.Object>()#G3_C1499::ClassMethod2386.MI.16480()#G3_C1499::ClassMethod2387.MI.16481()#G3_C1499::ClassMethod2388.MI.16482<System.Object>()#G3_C1499::ClassMethod2389.MI.16483<System.Object>()#G3_C1499::ClassMethod4387.16476()#G3_C1499::ClassMethod4388.16477()#G3_C1499::ClassMethod4389.16478<System.Object>()#G3_C1499::ClassMethod4390.16479<System.Object>()#G3_C1499::Method4.16471()#G3_C1499::Method5.16473()#G3_C1499::Method6.16474<System.Object>()#G2_C514::Method7.9710<System.Object>()#"
		call void Framework::M.G3_C1499.T<class BaseClass1,class G3_C1499`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C514::ClassMethod1336.MI.9715()#G2_C514::ClassMethod1337.MI.9716()#G2_C514::ClassMethod1338.MI.9717<System.Object>()#G2_C514::ClassMethod1339.MI.9718<System.Object>()#G3_C1499::ClassMethod2386.MI.16480()#G3_C1499::ClassMethod2387.MI.16481()#G3_C1499::ClassMethod2388.MI.16482<System.Object>()#G3_C1499::ClassMethod2389.MI.16483<System.Object>()#G3_C1499::ClassMethod4387.16476()#G3_C1499::ClassMethod4388.16477()#G3_C1499::ClassMethod4389.16478<System.Object>()#G3_C1499::ClassMethod4390.16479<System.Object>()#G3_C1499::Method4.16471()#G3_C1499::Method5.16473()#G3_C1499::Method6.16474<System.Object>()#G2_C514::Method7.9710<System.Object>()#"
		call void Framework::M.G3_C1499.B<class G3_C1499`1<class BaseClass1>>(!!0,string)
		newobj instance void class G2_C514`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C514::ClassMethod1336.MI.9715()#G2_C514::ClassMethod1337.MI.9716()#G2_C514::ClassMethod1338.MI.9717<System.Object>()#G2_C514::ClassMethod1339.MI.9718<System.Object>()#G1_C9::Method4.4834()#G2_C514::Method5.9707()#G1_C9::Method6.4838<System.Object>()#G1_C9::Method7.4833<System.Object>()#"
		call void Framework::M.G1_C9.T.T<class BaseClass0,class BaseClass0,class G2_C514`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C514::ClassMethod1336.MI.9715()#G2_C514::ClassMethod1337.MI.9716()#G2_C514::ClassMethod1338.MI.9717<System.Object>()#G2_C514::ClassMethod1339.MI.9718<System.Object>()#G1_C9::Method4.4834()#G2_C514::Method5.9707()#G1_C9::Method6.4838<System.Object>()#G1_C9::Method7.4833<System.Object>()#"
		call void Framework::M.G1_C9.A.T<class BaseClass0,class G2_C514`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C514::ClassMethod1336.MI.9715()#G2_C514::ClassMethod1337.MI.9716()#G2_C514::ClassMethod1338.MI.9717<System.Object>()#G2_C514::ClassMethod1339.MI.9718<System.Object>()#G1_C9::Method4.4834()#G2_C514::Method5.9707()#G1_C9::Method6.4838<System.Object>()#G1_C9::Method7.4833<System.Object>()#"
		call void Framework::M.G1_C9.A.A<class G2_C514`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C514::Method7.9710<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G2_C514`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C514::Method7.9710<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass0,class G2_C514`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C514::Method7.9710<System.Object>()#"
		call void Framework::M.IBase2.A.A<class G2_C514`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C514::Method4.9706()#G2_C514::Method5.9707()#G2_C514::Method6.MI.9709<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G2_C514`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C514::Method4.9706()#G2_C514::Method5.9707()#G2_C514::Method6.MI.9709<System.Object>()#"
		call void Framework::M.IBase1.A<class G2_C514`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C514::Method7.9710<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C514`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C514::Method7.9710<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G2_C514`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C514::Method7.9710<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G2_C514`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C514::ClassMethod1336.MI.9715()#G2_C514::ClassMethod1337.MI.9716()#G2_C514::ClassMethod1338.MI.9717<System.Object>()#G2_C514::ClassMethod1339.MI.9718<System.Object>()#G2_C514::ClassMethod2386.9711()#G2_C514::ClassMethod2387.9712()#G2_C514::ClassMethod2388.9713<System.Object>()#G2_C514::ClassMethod2389.9714<System.Object>()#G2_C514::Method4.9706()#G2_C514::Method5.9707()#G2_C514::Method6.9708<System.Object>()#G2_C514::Method7.9710<System.Object>()#"
		call void Framework::M.G2_C514.T.T<class BaseClass0,class BaseClass0,class G2_C514`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C514::ClassMethod1336.MI.9715()#G2_C514::ClassMethod1337.MI.9716()#G2_C514::ClassMethod1338.MI.9717<System.Object>()#G2_C514::ClassMethod1339.MI.9718<System.Object>()#G2_C514::ClassMethod2386.9711()#G2_C514::ClassMethod2387.9712()#G2_C514::ClassMethod2388.9713<System.Object>()#G2_C514::ClassMethod2389.9714<System.Object>()#G2_C514::Method4.9706()#G2_C514::Method5.9707()#G2_C514::Method6.9708<System.Object>()#G2_C514::Method7.9710<System.Object>()#"
		call void Framework::M.G2_C514.A.T<class BaseClass0,class G2_C514`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C514::ClassMethod1336.MI.9715()#G2_C514::ClassMethod1337.MI.9716()#G2_C514::ClassMethod1338.MI.9717<System.Object>()#G2_C514::ClassMethod1339.MI.9718<System.Object>()#G2_C514::ClassMethod2386.9711()#G2_C514::ClassMethod2387.9712()#G2_C514::ClassMethod2388.9713<System.Object>()#G2_C514::ClassMethod2389.9714<System.Object>()#G2_C514::Method4.9706()#G2_C514::Method5.9707()#G2_C514::Method6.9708<System.Object>()#G2_C514::Method7.9710<System.Object>()#"
		call void Framework::M.G2_C514.A.A<class G2_C514`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C514::Method4.9706()#G2_C514::Method5.9707()#G2_C514::Method6.MI.9709<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass1,class G2_C514`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C514::Method4.9706()#G2_C514::Method5.9707()#G2_C514::Method6.MI.9709<System.Object>()#"
		call void Framework::M.IBase1.B<class G2_C514`2<class BaseClass0,class BaseClass0>>(!!0,string)
		newobj instance void class G2_C514`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C514::ClassMethod1336.MI.9715()#G2_C514::ClassMethod1337.MI.9716()#G2_C514::ClassMethod1338.MI.9717<System.Object>()#G2_C514::ClassMethod1339.MI.9718<System.Object>()#G1_C9::Method4.4834()#G2_C514::Method5.9707()#G1_C9::Method6.4838<System.Object>()#G1_C9::Method7.4833<System.Object>()#"
		call void Framework::M.G1_C9.T.T<class BaseClass1,class BaseClass1,class G2_C514`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C514::ClassMethod1336.MI.9715()#G2_C514::ClassMethod1337.MI.9716()#G2_C514::ClassMethod1338.MI.9717<System.Object>()#G2_C514::ClassMethod1339.MI.9718<System.Object>()#G1_C9::Method4.4834()#G2_C514::Method5.9707()#G1_C9::Method6.4838<System.Object>()#G1_C9::Method7.4833<System.Object>()#"
		call void Framework::M.G1_C9.B.T<class BaseClass1,class G2_C514`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C514::ClassMethod1336.MI.9715()#G2_C514::ClassMethod1337.MI.9716()#G2_C514::ClassMethod1338.MI.9717<System.Object>()#G2_C514::ClassMethod1339.MI.9718<System.Object>()#G1_C9::Method4.4834()#G2_C514::Method5.9707()#G1_C9::Method6.4838<System.Object>()#G1_C9::Method7.4833<System.Object>()#"
		call void Framework::M.G1_C9.B.B<class G2_C514`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C514::Method7.9710<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C514`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C514::Method7.9710<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G2_C514`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C514::Method7.9710<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G2_C514`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C514::Method4.9706()#G2_C514::Method5.9707()#G2_C514::Method6.MI.9709<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G2_C514`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C514::Method4.9706()#G2_C514::Method5.9707()#G2_C514::Method6.MI.9709<System.Object>()#"
		call void Framework::M.IBase1.A<class G2_C514`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C514::ClassMethod1336.MI.9715()#G2_C514::ClassMethod1337.MI.9716()#G2_C514::ClassMethod1338.MI.9717<System.Object>()#G2_C514::ClassMethod1339.MI.9718<System.Object>()#G2_C514::ClassMethod2386.9711()#G2_C514::ClassMethod2387.9712()#G2_C514::ClassMethod2388.9713<System.Object>()#G2_C514::ClassMethod2389.9714<System.Object>()#G2_C514::Method4.9706()#G2_C514::Method5.9707()#G2_C514::Method6.9708<System.Object>()#G2_C514::Method7.9710<System.Object>()#"
		call void Framework::M.G2_C514.T.T<class BaseClass0,class BaseClass1,class G2_C514`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C514::ClassMethod1336.MI.9715()#G2_C514::ClassMethod1337.MI.9716()#G2_C514::ClassMethod1338.MI.9717<System.Object>()#G2_C514::ClassMethod1339.MI.9718<System.Object>()#G2_C514::ClassMethod2386.9711()#G2_C514::ClassMethod2387.9712()#G2_C514::ClassMethod2388.9713<System.Object>()#G2_C514::ClassMethod2389.9714<System.Object>()#G2_C514::Method4.9706()#G2_C514::Method5.9707()#G2_C514::Method6.9708<System.Object>()#G2_C514::Method7.9710<System.Object>()#"
		call void Framework::M.G2_C514.A.T<class BaseClass1,class G2_C514`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C514::ClassMethod1336.MI.9715()#G2_C514::ClassMethod1337.MI.9716()#G2_C514::ClassMethod1338.MI.9717<System.Object>()#G2_C514::ClassMethod1339.MI.9718<System.Object>()#G2_C514::ClassMethod2386.9711()#G2_C514::ClassMethod2387.9712()#G2_C514::ClassMethod2388.9713<System.Object>()#G2_C514::ClassMethod2389.9714<System.Object>()#G2_C514::Method4.9706()#G2_C514::Method5.9707()#G2_C514::Method6.9708<System.Object>()#G2_C514::Method7.9710<System.Object>()#"
		call void Framework::M.G2_C514.A.B<class G2_C514`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C514::Method4.9706()#G2_C514::Method5.9707()#G2_C514::Method6.MI.9709<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass1,class G2_C514`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C514::Method4.9706()#G2_C514::Method5.9707()#G2_C514::Method6.MI.9709<System.Object>()#"
		call void Framework::M.IBase1.B<class G2_C514`2<class BaseClass0,class BaseClass1>>(!!0,string)
		newobj instance void class G2_C514`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C514::ClassMethod1336.MI.9715()#G2_C514::ClassMethod1337.MI.9716()#G2_C514::ClassMethod1338.MI.9717<System.Object>()#G2_C514::ClassMethod1339.MI.9718<System.Object>()#G1_C9::Method4.4834()#G2_C514::Method5.9707()#G1_C9::Method6.4838<System.Object>()#G1_C9::Method7.4833<System.Object>()#"
		call void Framework::M.G1_C9.T.T<class BaseClass0,class BaseClass0,class G2_C514`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C514::ClassMethod1336.MI.9715()#G2_C514::ClassMethod1337.MI.9716()#G2_C514::ClassMethod1338.MI.9717<System.Object>()#G2_C514::ClassMethod1339.MI.9718<System.Object>()#G1_C9::Method4.4834()#G2_C514::Method5.9707()#G1_C9::Method6.4838<System.Object>()#G1_C9::Method7.4833<System.Object>()#"
		call void Framework::M.G1_C9.A.T<class BaseClass0,class G2_C514`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C514::ClassMethod1336.MI.9715()#G2_C514::ClassMethod1337.MI.9716()#G2_C514::ClassMethod1338.MI.9717<System.Object>()#G2_C514::ClassMethod1339.MI.9718<System.Object>()#G1_C9::Method4.4834()#G2_C514::Method5.9707()#G1_C9::Method6.4838<System.Object>()#G1_C9::Method7.4833<System.Object>()#"
		call void Framework::M.G1_C9.A.A<class G2_C514`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C514::Method7.9710<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G2_C514`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C514::Method7.9710<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass0,class G2_C514`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C514::Method7.9710<System.Object>()#"
		call void Framework::M.IBase2.A.A<class G2_C514`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C514::Method4.9706()#G2_C514::Method5.9707()#G2_C514::Method6.MI.9709<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G2_C514`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C514::Method4.9706()#G2_C514::Method5.9707()#G2_C514::Method6.MI.9709<System.Object>()#"
		call void Framework::M.IBase1.A<class G2_C514`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C514::Method7.9710<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C514`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C514::Method7.9710<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G2_C514`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C514::Method7.9710<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G2_C514`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C514::ClassMethod1336.MI.9715()#G2_C514::ClassMethod1337.MI.9716()#G2_C514::ClassMethod1338.MI.9717<System.Object>()#G2_C514::ClassMethod1339.MI.9718<System.Object>()#G2_C514::ClassMethod2386.9711()#G2_C514::ClassMethod2387.9712()#G2_C514::ClassMethod2388.9713<System.Object>()#G2_C514::ClassMethod2389.9714<System.Object>()#G2_C514::Method4.9706()#G2_C514::Method5.9707()#G2_C514::Method6.9708<System.Object>()#G2_C514::Method7.9710<System.Object>()#"
		call void Framework::M.G2_C514.T.T<class BaseClass1,class BaseClass0,class G2_C514`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C514::ClassMethod1336.MI.9715()#G2_C514::ClassMethod1337.MI.9716()#G2_C514::ClassMethod1338.MI.9717<System.Object>()#G2_C514::ClassMethod1339.MI.9718<System.Object>()#G2_C514::ClassMethod2386.9711()#G2_C514::ClassMethod2387.9712()#G2_C514::ClassMethod2388.9713<System.Object>()#G2_C514::ClassMethod2389.9714<System.Object>()#G2_C514::Method4.9706()#G2_C514::Method5.9707()#G2_C514::Method6.9708<System.Object>()#G2_C514::Method7.9710<System.Object>()#"
		call void Framework::M.G2_C514.B.T<class BaseClass0,class G2_C514`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C514::ClassMethod1336.MI.9715()#G2_C514::ClassMethod1337.MI.9716()#G2_C514::ClassMethod1338.MI.9717<System.Object>()#G2_C514::ClassMethod1339.MI.9718<System.Object>()#G2_C514::ClassMethod2386.9711()#G2_C514::ClassMethod2387.9712()#G2_C514::ClassMethod2388.9713<System.Object>()#G2_C514::ClassMethod2389.9714<System.Object>()#G2_C514::Method4.9706()#G2_C514::Method5.9707()#G2_C514::Method6.9708<System.Object>()#G2_C514::Method7.9710<System.Object>()#"
		call void Framework::M.G2_C514.B.A<class G2_C514`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C514::Method4.9706()#G2_C514::Method5.9707()#G2_C514::Method6.MI.9709<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass1,class G2_C514`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C514::Method4.9706()#G2_C514::Method5.9707()#G2_C514::Method6.MI.9709<System.Object>()#"
		call void Framework::M.IBase1.B<class G2_C514`2<class BaseClass1,class BaseClass0>>(!!0,string)
		newobj instance void class G2_C514`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C514::ClassMethod1336.MI.9715()#G2_C514::ClassMethod1337.MI.9716()#G2_C514::ClassMethod1338.MI.9717<System.Object>()#G2_C514::ClassMethod1339.MI.9718<System.Object>()#G1_C9::Method4.4834()#G2_C514::Method5.9707()#G1_C9::Method6.4838<System.Object>()#G1_C9::Method7.4833<System.Object>()#"
		call void Framework::M.G1_C9.T.T<class BaseClass1,class BaseClass1,class G2_C514`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C514::ClassMethod1336.MI.9715()#G2_C514::ClassMethod1337.MI.9716()#G2_C514::ClassMethod1338.MI.9717<System.Object>()#G2_C514::ClassMethod1339.MI.9718<System.Object>()#G1_C9::Method4.4834()#G2_C514::Method5.9707()#G1_C9::Method6.4838<System.Object>()#G1_C9::Method7.4833<System.Object>()#"
		call void Framework::M.G1_C9.B.T<class BaseClass1,class G2_C514`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C514::ClassMethod1336.MI.9715()#G2_C514::ClassMethod1337.MI.9716()#G2_C514::ClassMethod1338.MI.9717<System.Object>()#G2_C514::ClassMethod1339.MI.9718<System.Object>()#G1_C9::Method4.4834()#G2_C514::Method5.9707()#G1_C9::Method6.4838<System.Object>()#G1_C9::Method7.4833<System.Object>()#"
		call void Framework::M.G1_C9.B.B<class G2_C514`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C514::Method7.9710<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C514`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C514::Method7.9710<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G2_C514`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C514::Method7.9710<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G2_C514`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C514::Method4.9706()#G2_C514::Method5.9707()#G2_C514::Method6.MI.9709<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G2_C514`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C514::Method4.9706()#G2_C514::Method5.9707()#G2_C514::Method6.MI.9709<System.Object>()#"
		call void Framework::M.IBase1.A<class G2_C514`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C514::ClassMethod1336.MI.9715()#G2_C514::ClassMethod1337.MI.9716()#G2_C514::ClassMethod1338.MI.9717<System.Object>()#G2_C514::ClassMethod1339.MI.9718<System.Object>()#G2_C514::ClassMethod2386.9711()#G2_C514::ClassMethod2387.9712()#G2_C514::ClassMethod2388.9713<System.Object>()#G2_C514::ClassMethod2389.9714<System.Object>()#G2_C514::Method4.9706()#G2_C514::Method5.9707()#G2_C514::Method6.9708<System.Object>()#G2_C514::Method7.9710<System.Object>()#"
		call void Framework::M.G2_C514.T.T<class BaseClass1,class BaseClass1,class G2_C514`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C514::ClassMethod1336.MI.9715()#G2_C514::ClassMethod1337.MI.9716()#G2_C514::ClassMethod1338.MI.9717<System.Object>()#G2_C514::ClassMethod1339.MI.9718<System.Object>()#G2_C514::ClassMethod2386.9711()#G2_C514::ClassMethod2387.9712()#G2_C514::ClassMethod2388.9713<System.Object>()#G2_C514::ClassMethod2389.9714<System.Object>()#G2_C514::Method4.9706()#G2_C514::Method5.9707()#G2_C514::Method6.9708<System.Object>()#G2_C514::Method7.9710<System.Object>()#"
		call void Framework::M.G2_C514.B.T<class BaseClass1,class G2_C514`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C514::ClassMethod1336.MI.9715()#G2_C514::ClassMethod1337.MI.9716()#G2_C514::ClassMethod1338.MI.9717<System.Object>()#G2_C514::ClassMethod1339.MI.9718<System.Object>()#G2_C514::ClassMethod2386.9711()#G2_C514::ClassMethod2387.9712()#G2_C514::ClassMethod2388.9713<System.Object>()#G2_C514::ClassMethod2389.9714<System.Object>()#G2_C514::Method4.9706()#G2_C514::Method5.9707()#G2_C514::Method6.9708<System.Object>()#G2_C514::Method7.9710<System.Object>()#"
		call void Framework::M.G2_C514.B.B<class G2_C514`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C514::Method4.9706()#G2_C514::Method5.9707()#G2_C514::Method6.MI.9709<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass1,class G2_C514`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C514::Method4.9706()#G2_C514::Method5.9707()#G2_C514::Method6.MI.9709<System.Object>()#"
		call void Framework::M.IBase1.B<class G2_C514`2<class BaseClass1,class BaseClass1>>(!!0,string)
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
		newobj instance void class G3_C1499`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass0,class BaseClass0>::ClassMethod1339<object>()
		calli default string(class G3_C1499`1<class BaseClass0>)
		ldstr "G2_C514::ClassMethod1339.MI.9718<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G3_C1499`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass0,class BaseClass0>::ClassMethod1338<object>()
		calli default string(class G3_C1499`1<class BaseClass0>)
		ldstr "G2_C514::ClassMethod1338.MI.9717<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G3_C1499`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass0,class BaseClass0>::ClassMethod1337()
		calli default string(class G3_C1499`1<class BaseClass0>)
		ldstr "G2_C514::ClassMethod1337.MI.9716()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G3_C1499`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass0,class BaseClass0>::ClassMethod1336()
		calli default string(class G3_C1499`1<class BaseClass0>)
		ldstr "G2_C514::ClassMethod1336.MI.9715()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G3_C1499`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass0,class BaseClass0>::Method6<object>()
		calli default string(class G3_C1499`1<class BaseClass0>)
		ldstr "G1_C9::Method6.4838<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G3_C1499`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass0,class BaseClass0>::Method5()
		calli default string(class G3_C1499`1<class BaseClass0>)
		ldstr "G3_C1499::Method5.16473()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G3_C1499`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass0,class BaseClass0>::Method4()
		calli default string(class G3_C1499`1<class BaseClass0>)
		ldstr "G1_C9::Method4.4834()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G3_C1499`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1499`1<class BaseClass0>)
		ldstr "G1_C9::Method7.4833<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G3_C1499`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1499`1<class BaseClass0>)
		ldstr "G2_C514::Method7.9710<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1499`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G3_C1499`1<class BaseClass0>)
		ldstr "G3_C1499::Method4.MI.16472()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1499`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G3_C1499`1<class BaseClass0>)
		ldstr "G3_C1499::Method5.16473()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1499`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1499`1<class BaseClass0>)
		ldstr "G3_C1499::Method6.MI.16475<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1499`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1499`1<class BaseClass0>)
		ldstr "G2_C514::Method7.9710<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1499`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C514`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C514`2<class BaseClass0,class BaseClass0>::ClassMethod2389<object>()
		calli default string(class G3_C1499`1<class BaseClass0>)
		ldstr "G3_C1499::ClassMethod2389.MI.16483<System.Object>()"
		ldstr "class G2_C514`2<class BaseClass0,class BaseClass0> on type class G3_C1499`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C514`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C514`2<class BaseClass0,class BaseClass0>::ClassMethod2388<object>()
		calli default string(class G3_C1499`1<class BaseClass0>)
		ldstr "G3_C1499::ClassMethod2388.MI.16482<System.Object>()"
		ldstr "class G2_C514`2<class BaseClass0,class BaseClass0> on type class G3_C1499`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C514`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C514`2<class BaseClass0,class BaseClass0>::ClassMethod2387()
		calli default string(class G3_C1499`1<class BaseClass0>)
		ldstr "G3_C1499::ClassMethod2387.MI.16481()"
		ldstr "class G2_C514`2<class BaseClass0,class BaseClass0> on type class G3_C1499`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C514`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C514`2<class BaseClass0,class BaseClass0>::ClassMethod2386()
		calli default string(class G3_C1499`1<class BaseClass0>)
		ldstr "G3_C1499::ClassMethod2386.MI.16480()"
		ldstr "class G2_C514`2<class BaseClass0,class BaseClass0> on type class G3_C1499`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C514`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C514`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1499`1<class BaseClass0>)
		ldstr "G2_C514::Method7.9710<System.Object>()"
		ldstr "class G2_C514`2<class BaseClass0,class BaseClass0> on type class G3_C1499`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C514`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C514`2<class BaseClass0,class BaseClass0>::Method6<object>()
		calli default string(class G3_C1499`1<class BaseClass0>)
		ldstr "G2_C514::Method6.9708<System.Object>()"
		ldstr "class G2_C514`2<class BaseClass0,class BaseClass0> on type class G3_C1499`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C514`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C514`2<class BaseClass0,class BaseClass0>::Method5()
		calli default string(class G3_C1499`1<class BaseClass0>)
		ldstr "G3_C1499::Method5.16473()"
		ldstr "class G2_C514`2<class BaseClass0,class BaseClass0> on type class G3_C1499`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C514`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C514`2<class BaseClass0,class BaseClass0>::Method4()
		calli default string(class G3_C1499`1<class BaseClass0>)
		ldstr "G2_C514::Method4.9706()"
		ldstr "class G2_C514`2<class BaseClass0,class BaseClass0> on type class G3_C1499`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C514`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C514`2<class BaseClass0,class BaseClass0>::ClassMethod1339<object>()
		calli default string(class G3_C1499`1<class BaseClass0>)
		ldstr "G2_C514::ClassMethod1339.MI.9718<System.Object>()"
		ldstr "class G2_C514`2<class BaseClass0,class BaseClass0> on type class G3_C1499`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C514`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C514`2<class BaseClass0,class BaseClass0>::ClassMethod1338<object>()
		calli default string(class G3_C1499`1<class BaseClass0>)
		ldstr "G2_C514::ClassMethod1338.MI.9717<System.Object>()"
		ldstr "class G2_C514`2<class BaseClass0,class BaseClass0> on type class G3_C1499`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C514`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C514`2<class BaseClass0,class BaseClass0>::ClassMethod1337()
		calli default string(class G3_C1499`1<class BaseClass0>)
		ldstr "G2_C514::ClassMethod1337.MI.9716()"
		ldstr "class G2_C514`2<class BaseClass0,class BaseClass0> on type class G3_C1499`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C514`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C514`2<class BaseClass0,class BaseClass0>::ClassMethod1336()
		calli default string(class G3_C1499`1<class BaseClass0>)
		ldstr "G2_C514::ClassMethod1336.MI.9715()"
		ldstr "class G2_C514`2<class BaseClass0,class BaseClass0> on type class G3_C1499`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G3_C1499`1<class BaseClass0>)
		ldstr "G2_C514::Method4.9706()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1499`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G3_C1499`1<class BaseClass0>)
		ldstr "G3_C1499::Method5.16473()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1499`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G3_C1499`1<class BaseClass0>)
		ldstr "G2_C514::Method6.MI.9709<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1499`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1499`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1499`1<class BaseClass0>::ClassMethod4390<object>()
		calli default string(class G3_C1499`1<class BaseClass0>)
		ldstr "G3_C1499::ClassMethod4390.16479<System.Object>()"
		ldstr "class G3_C1499`1<class BaseClass0> on type class G3_C1499`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1499`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1499`1<class BaseClass0>::ClassMethod4389<object>()
		calli default string(class G3_C1499`1<class BaseClass0>)
		ldstr "G3_C1499::ClassMethod4389.16478<System.Object>()"
		ldstr "class G3_C1499`1<class BaseClass0> on type class G3_C1499`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1499`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1499`1<class BaseClass0>::ClassMethod4388()
		calli default string(class G3_C1499`1<class BaseClass0>)
		ldstr "G3_C1499::ClassMethod4388.16477()"
		ldstr "class G3_C1499`1<class BaseClass0> on type class G3_C1499`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1499`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1499`1<class BaseClass0>::ClassMethod4387()
		calli default string(class G3_C1499`1<class BaseClass0>)
		ldstr "G3_C1499::ClassMethod4387.16476()"
		ldstr "class G3_C1499`1<class BaseClass0> on type class G3_C1499`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1499`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1499`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1499`1<class BaseClass0>)
		ldstr "G3_C1499::Method6.16474<System.Object>()"
		ldstr "class G3_C1499`1<class BaseClass0> on type class G3_C1499`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1499`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1499`1<class BaseClass0>::Method5()
		calli default string(class G3_C1499`1<class BaseClass0>)
		ldstr "G3_C1499::Method5.16473()"
		ldstr "class G3_C1499`1<class BaseClass0> on type class G3_C1499`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1499`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1499`1<class BaseClass0>::Method4()
		calli default string(class G3_C1499`1<class BaseClass0>)
		ldstr "G3_C1499::Method4.16471()"
		ldstr "class G3_C1499`1<class BaseClass0> on type class G3_C1499`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1499`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1499`1<class BaseClass0>::ClassMethod2389<object>()
		calli default string(class G3_C1499`1<class BaseClass0>)
		ldstr "G3_C1499::ClassMethod2389.MI.16483<System.Object>()"
		ldstr "class G3_C1499`1<class BaseClass0> on type class G3_C1499`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1499`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1499`1<class BaseClass0>::ClassMethod2388<object>()
		calli default string(class G3_C1499`1<class BaseClass0>)
		ldstr "G3_C1499::ClassMethod2388.MI.16482<System.Object>()"
		ldstr "class G3_C1499`1<class BaseClass0> on type class G3_C1499`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1499`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1499`1<class BaseClass0>::ClassMethod2387()
		calli default string(class G3_C1499`1<class BaseClass0>)
		ldstr "G3_C1499::ClassMethod2387.MI.16481()"
		ldstr "class G3_C1499`1<class BaseClass0> on type class G3_C1499`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1499`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1499`1<class BaseClass0>::ClassMethod2386()
		calli default string(class G3_C1499`1<class BaseClass0>)
		ldstr "G3_C1499::ClassMethod2386.MI.16480()"
		ldstr "class G3_C1499`1<class BaseClass0> on type class G3_C1499`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1499`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1499`1<class BaseClass0>::Method7<object>()
		calli default string(class G3_C1499`1<class BaseClass0>)
		ldstr "G2_C514::Method7.9710<System.Object>()"
		ldstr "class G3_C1499`1<class BaseClass0> on type class G3_C1499`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1499`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1499`1<class BaseClass0>::ClassMethod1339<object>()
		calli default string(class G3_C1499`1<class BaseClass0>)
		ldstr "G2_C514::ClassMethod1339.MI.9718<System.Object>()"
		ldstr "class G3_C1499`1<class BaseClass0> on type class G3_C1499`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1499`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1499`1<class BaseClass0>::ClassMethod1338<object>()
		calli default string(class G3_C1499`1<class BaseClass0>)
		ldstr "G2_C514::ClassMethod1338.MI.9717<System.Object>()"
		ldstr "class G3_C1499`1<class BaseClass0> on type class G3_C1499`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1499`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1499`1<class BaseClass0>::ClassMethod1337()
		calli default string(class G3_C1499`1<class BaseClass0>)
		ldstr "G2_C514::ClassMethod1337.MI.9716()"
		ldstr "class G3_C1499`1<class BaseClass0> on type class G3_C1499`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1499`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1499`1<class BaseClass0>::ClassMethod1336()
		calli default string(class G3_C1499`1<class BaseClass0>)
		ldstr "G2_C514::ClassMethod1336.MI.9715()"
		ldstr "class G3_C1499`1<class BaseClass0> on type class G3_C1499`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G3_C1499`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass0,class BaseClass0>::ClassMethod1339<object>()
		calli default string(class G3_C1499`1<class BaseClass1>)
		ldstr "G2_C514::ClassMethod1339.MI.9718<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G3_C1499`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass0,class BaseClass0>::ClassMethod1338<object>()
		calli default string(class G3_C1499`1<class BaseClass1>)
		ldstr "G2_C514::ClassMethod1338.MI.9717<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G3_C1499`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass0,class BaseClass0>::ClassMethod1337()
		calli default string(class G3_C1499`1<class BaseClass1>)
		ldstr "G2_C514::ClassMethod1337.MI.9716()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G3_C1499`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass0,class BaseClass0>::ClassMethod1336()
		calli default string(class G3_C1499`1<class BaseClass1>)
		ldstr "G2_C514::ClassMethod1336.MI.9715()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G3_C1499`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass0,class BaseClass0>::Method6<object>()
		calli default string(class G3_C1499`1<class BaseClass1>)
		ldstr "G1_C9::Method6.4838<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G3_C1499`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass0,class BaseClass0>::Method5()
		calli default string(class G3_C1499`1<class BaseClass1>)
		ldstr "G3_C1499::Method5.16473()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G3_C1499`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass0,class BaseClass0>::Method4()
		calli default string(class G3_C1499`1<class BaseClass1>)
		ldstr "G1_C9::Method4.4834()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G3_C1499`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1499`1<class BaseClass1>)
		ldstr "G1_C9::Method7.4833<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G3_C1499`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1499`1<class BaseClass1>)
		ldstr "G2_C514::Method7.9710<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1499`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G3_C1499`1<class BaseClass1>)
		ldstr "G3_C1499::Method4.MI.16472()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1499`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G3_C1499`1<class BaseClass1>)
		ldstr "G3_C1499::Method5.16473()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1499`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1499`1<class BaseClass1>)
		ldstr "G3_C1499::Method6.MI.16475<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1499`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1499`1<class BaseClass1>)
		ldstr "G2_C514::Method7.9710<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1499`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C514`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C514`2<class BaseClass1,class BaseClass0>::ClassMethod2389<object>()
		calli default string(class G3_C1499`1<class BaseClass1>)
		ldstr "G3_C1499::ClassMethod2389.MI.16483<System.Object>()"
		ldstr "class G2_C514`2<class BaseClass1,class BaseClass0> on type class G3_C1499`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C514`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C514`2<class BaseClass1,class BaseClass0>::ClassMethod2388<object>()
		calli default string(class G3_C1499`1<class BaseClass1>)
		ldstr "G3_C1499::ClassMethod2388.MI.16482<System.Object>()"
		ldstr "class G2_C514`2<class BaseClass1,class BaseClass0> on type class G3_C1499`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C514`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C514`2<class BaseClass1,class BaseClass0>::ClassMethod2387()
		calli default string(class G3_C1499`1<class BaseClass1>)
		ldstr "G3_C1499::ClassMethod2387.MI.16481()"
		ldstr "class G2_C514`2<class BaseClass1,class BaseClass0> on type class G3_C1499`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C514`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C514`2<class BaseClass1,class BaseClass0>::ClassMethod2386()
		calli default string(class G3_C1499`1<class BaseClass1>)
		ldstr "G3_C1499::ClassMethod2386.MI.16480()"
		ldstr "class G2_C514`2<class BaseClass1,class BaseClass0> on type class G3_C1499`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C514`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C514`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1499`1<class BaseClass1>)
		ldstr "G2_C514::Method7.9710<System.Object>()"
		ldstr "class G2_C514`2<class BaseClass1,class BaseClass0> on type class G3_C1499`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C514`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C514`2<class BaseClass1,class BaseClass0>::Method6<object>()
		calli default string(class G3_C1499`1<class BaseClass1>)
		ldstr "G2_C514::Method6.9708<System.Object>()"
		ldstr "class G2_C514`2<class BaseClass1,class BaseClass0> on type class G3_C1499`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C514`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C514`2<class BaseClass1,class BaseClass0>::Method5()
		calli default string(class G3_C1499`1<class BaseClass1>)
		ldstr "G3_C1499::Method5.16473()"
		ldstr "class G2_C514`2<class BaseClass1,class BaseClass0> on type class G3_C1499`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C514`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C514`2<class BaseClass1,class BaseClass0>::Method4()
		calli default string(class G3_C1499`1<class BaseClass1>)
		ldstr "G2_C514::Method4.9706()"
		ldstr "class G2_C514`2<class BaseClass1,class BaseClass0> on type class G3_C1499`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C514`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C514`2<class BaseClass1,class BaseClass0>::ClassMethod1339<object>()
		calli default string(class G3_C1499`1<class BaseClass1>)
		ldstr "G2_C514::ClassMethod1339.MI.9718<System.Object>()"
		ldstr "class G2_C514`2<class BaseClass1,class BaseClass0> on type class G3_C1499`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C514`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C514`2<class BaseClass1,class BaseClass0>::ClassMethod1338<object>()
		calli default string(class G3_C1499`1<class BaseClass1>)
		ldstr "G2_C514::ClassMethod1338.MI.9717<System.Object>()"
		ldstr "class G2_C514`2<class BaseClass1,class BaseClass0> on type class G3_C1499`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C514`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C514`2<class BaseClass1,class BaseClass0>::ClassMethod1337()
		calli default string(class G3_C1499`1<class BaseClass1>)
		ldstr "G2_C514::ClassMethod1337.MI.9716()"
		ldstr "class G2_C514`2<class BaseClass1,class BaseClass0> on type class G3_C1499`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C514`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C514`2<class BaseClass1,class BaseClass0>::ClassMethod1336()
		calli default string(class G3_C1499`1<class BaseClass1>)
		ldstr "G2_C514::ClassMethod1336.MI.9715()"
		ldstr "class G2_C514`2<class BaseClass1,class BaseClass0> on type class G3_C1499`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G3_C1499`1<class BaseClass1>)
		ldstr "G2_C514::Method4.9706()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1499`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G3_C1499`1<class BaseClass1>)
		ldstr "G3_C1499::Method5.16473()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1499`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G3_C1499`1<class BaseClass1>)
		ldstr "G2_C514::Method6.MI.9709<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1499`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1499`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1499`1<class BaseClass1>::ClassMethod4390<object>()
		calli default string(class G3_C1499`1<class BaseClass1>)
		ldstr "G3_C1499::ClassMethod4390.16479<System.Object>()"
		ldstr "class G3_C1499`1<class BaseClass1> on type class G3_C1499`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1499`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1499`1<class BaseClass1>::ClassMethod4389<object>()
		calli default string(class G3_C1499`1<class BaseClass1>)
		ldstr "G3_C1499::ClassMethod4389.16478<System.Object>()"
		ldstr "class G3_C1499`1<class BaseClass1> on type class G3_C1499`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1499`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1499`1<class BaseClass1>::ClassMethod4388()
		calli default string(class G3_C1499`1<class BaseClass1>)
		ldstr "G3_C1499::ClassMethod4388.16477()"
		ldstr "class G3_C1499`1<class BaseClass1> on type class G3_C1499`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1499`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1499`1<class BaseClass1>::ClassMethod4387()
		calli default string(class G3_C1499`1<class BaseClass1>)
		ldstr "G3_C1499::ClassMethod4387.16476()"
		ldstr "class G3_C1499`1<class BaseClass1> on type class G3_C1499`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1499`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1499`1<class BaseClass1>::Method6<object>()
		calli default string(class G3_C1499`1<class BaseClass1>)
		ldstr "G3_C1499::Method6.16474<System.Object>()"
		ldstr "class G3_C1499`1<class BaseClass1> on type class G3_C1499`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1499`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1499`1<class BaseClass1>::Method5()
		calli default string(class G3_C1499`1<class BaseClass1>)
		ldstr "G3_C1499::Method5.16473()"
		ldstr "class G3_C1499`1<class BaseClass1> on type class G3_C1499`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1499`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1499`1<class BaseClass1>::Method4()
		calli default string(class G3_C1499`1<class BaseClass1>)
		ldstr "G3_C1499::Method4.16471()"
		ldstr "class G3_C1499`1<class BaseClass1> on type class G3_C1499`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1499`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1499`1<class BaseClass1>::ClassMethod2389<object>()
		calli default string(class G3_C1499`1<class BaseClass1>)
		ldstr "G3_C1499::ClassMethod2389.MI.16483<System.Object>()"
		ldstr "class G3_C1499`1<class BaseClass1> on type class G3_C1499`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1499`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1499`1<class BaseClass1>::ClassMethod2388<object>()
		calli default string(class G3_C1499`1<class BaseClass1>)
		ldstr "G3_C1499::ClassMethod2388.MI.16482<System.Object>()"
		ldstr "class G3_C1499`1<class BaseClass1> on type class G3_C1499`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1499`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1499`1<class BaseClass1>::ClassMethod2387()
		calli default string(class G3_C1499`1<class BaseClass1>)
		ldstr "G3_C1499::ClassMethod2387.MI.16481()"
		ldstr "class G3_C1499`1<class BaseClass1> on type class G3_C1499`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1499`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1499`1<class BaseClass1>::ClassMethod2386()
		calli default string(class G3_C1499`1<class BaseClass1>)
		ldstr "G3_C1499::ClassMethod2386.MI.16480()"
		ldstr "class G3_C1499`1<class BaseClass1> on type class G3_C1499`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1499`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1499`1<class BaseClass1>::Method7<object>()
		calli default string(class G3_C1499`1<class BaseClass1>)
		ldstr "G2_C514::Method7.9710<System.Object>()"
		ldstr "class G3_C1499`1<class BaseClass1> on type class G3_C1499`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1499`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1499`1<class BaseClass1>::ClassMethod1339<object>()
		calli default string(class G3_C1499`1<class BaseClass1>)
		ldstr "G2_C514::ClassMethod1339.MI.9718<System.Object>()"
		ldstr "class G3_C1499`1<class BaseClass1> on type class G3_C1499`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1499`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1499`1<class BaseClass1>::ClassMethod1338<object>()
		calli default string(class G3_C1499`1<class BaseClass1>)
		ldstr "G2_C514::ClassMethod1338.MI.9717<System.Object>()"
		ldstr "class G3_C1499`1<class BaseClass1> on type class G3_C1499`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1499`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1499`1<class BaseClass1>::ClassMethod1337()
		calli default string(class G3_C1499`1<class BaseClass1>)
		ldstr "G2_C514::ClassMethod1337.MI.9716()"
		ldstr "class G3_C1499`1<class BaseClass1> on type class G3_C1499`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1499`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1499`1<class BaseClass1>::ClassMethod1336()
		calli default string(class G3_C1499`1<class BaseClass1>)
		ldstr "G2_C514::ClassMethod1336.MI.9715()"
		ldstr "class G3_C1499`1<class BaseClass1> on type class G3_C1499`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C514`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass0,class BaseClass0>::ClassMethod1339<object>()
		calli default string(class G2_C514`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C514::ClassMethod1339.MI.9718<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C514`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass0,class BaseClass0>::ClassMethod1338<object>()
		calli default string(class G2_C514`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C514::ClassMethod1338.MI.9717<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C514`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass0,class BaseClass0>::ClassMethod1337()
		calli default string(class G2_C514`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C514::ClassMethod1337.MI.9716()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C514`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass0,class BaseClass0>::ClassMethod1336()
		calli default string(class G2_C514`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C514::ClassMethod1336.MI.9715()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C514`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass0,class BaseClass0>::Method6<object>()
		calli default string(class G2_C514`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C9::Method6.4838<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C514`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass0,class BaseClass0>::Method5()
		calli default string(class G2_C514`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C514::Method5.9707()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C514`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass0,class BaseClass0>::Method4()
		calli default string(class G2_C514`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C9::Method4.4834()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C514`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C514`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C9::Method7.4833<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C514`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C514`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C514::Method7.9710<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C514`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G2_C514`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C514::Method4.9706()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C514`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G2_C514`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C514::Method5.9707()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C514`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G2_C514`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C514::Method6.MI.9709<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C514`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C514`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C514::Method7.9710<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C514`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C514`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C514`2<class BaseClass0,class BaseClass0>::ClassMethod2389<object>()
		calli default string(class G2_C514`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C514::ClassMethod2389.9714<System.Object>()"
		ldstr "class G2_C514`2<class BaseClass0,class BaseClass0> on type class G2_C514`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C514`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C514`2<class BaseClass0,class BaseClass0>::ClassMethod2388<object>()
		calli default string(class G2_C514`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C514::ClassMethod2388.9713<System.Object>()"
		ldstr "class G2_C514`2<class BaseClass0,class BaseClass0> on type class G2_C514`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C514`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C514`2<class BaseClass0,class BaseClass0>::ClassMethod2387()
		calli default string(class G2_C514`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C514::ClassMethod2387.9712()"
		ldstr "class G2_C514`2<class BaseClass0,class BaseClass0> on type class G2_C514`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C514`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C514`2<class BaseClass0,class BaseClass0>::ClassMethod2386()
		calli default string(class G2_C514`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C514::ClassMethod2386.9711()"
		ldstr "class G2_C514`2<class BaseClass0,class BaseClass0> on type class G2_C514`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C514`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C514`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C514`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C514::Method7.9710<System.Object>()"
		ldstr "class G2_C514`2<class BaseClass0,class BaseClass0> on type class G2_C514`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C514`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C514`2<class BaseClass0,class BaseClass0>::Method6<object>()
		calli default string(class G2_C514`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C514::Method6.9708<System.Object>()"
		ldstr "class G2_C514`2<class BaseClass0,class BaseClass0> on type class G2_C514`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C514`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C514`2<class BaseClass0,class BaseClass0>::Method5()
		calli default string(class G2_C514`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C514::Method5.9707()"
		ldstr "class G2_C514`2<class BaseClass0,class BaseClass0> on type class G2_C514`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C514`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C514`2<class BaseClass0,class BaseClass0>::Method4()
		calli default string(class G2_C514`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C514::Method4.9706()"
		ldstr "class G2_C514`2<class BaseClass0,class BaseClass0> on type class G2_C514`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C514`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C514`2<class BaseClass0,class BaseClass0>::ClassMethod1339<object>()
		calli default string(class G2_C514`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C514::ClassMethod1339.MI.9718<System.Object>()"
		ldstr "class G2_C514`2<class BaseClass0,class BaseClass0> on type class G2_C514`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C514`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C514`2<class BaseClass0,class BaseClass0>::ClassMethod1338<object>()
		calli default string(class G2_C514`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C514::ClassMethod1338.MI.9717<System.Object>()"
		ldstr "class G2_C514`2<class BaseClass0,class BaseClass0> on type class G2_C514`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C514`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C514`2<class BaseClass0,class BaseClass0>::ClassMethod1337()
		calli default string(class G2_C514`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C514::ClassMethod1337.MI.9716()"
		ldstr "class G2_C514`2<class BaseClass0,class BaseClass0> on type class G2_C514`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C514`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C514`2<class BaseClass0,class BaseClass0>::ClassMethod1336()
		calli default string(class G2_C514`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C514::ClassMethod1336.MI.9715()"
		ldstr "class G2_C514`2<class BaseClass0,class BaseClass0> on type class G2_C514`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G2_C514`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C514::Method4.9706()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C514`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G2_C514`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C514::Method5.9707()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C514`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G2_C514`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C514::Method6.MI.9709<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C514`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C514`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C9`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass1,class BaseClass1>::ClassMethod1339<object>()
		calli default string(class G2_C514`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C514::ClassMethod1339.MI.9718<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass1,class BaseClass1> on type class G2_C514`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass1,class BaseClass1>::ClassMethod1338<object>()
		calli default string(class G2_C514`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C514::ClassMethod1338.MI.9717<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass1,class BaseClass1> on type class G2_C514`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass1,class BaseClass1>::ClassMethod1337()
		calli default string(class G2_C514`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C514::ClassMethod1337.MI.9716()"
		ldstr "class G1_C9`2<class BaseClass1,class BaseClass1> on type class G2_C514`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass1,class BaseClass1>::ClassMethod1336()
		calli default string(class G2_C514`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C514::ClassMethod1336.MI.9715()"
		ldstr "class G1_C9`2<class BaseClass1,class BaseClass1> on type class G2_C514`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass1,class BaseClass1>::Method6<object>()
		calli default string(class G2_C514`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C9::Method6.4838<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass1,class BaseClass1> on type class G2_C514`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass1,class BaseClass1>::Method5()
		calli default string(class G2_C514`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C514::Method5.9707()"
		ldstr "class G1_C9`2<class BaseClass1,class BaseClass1> on type class G2_C514`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass1,class BaseClass1>::Method4()
		calli default string(class G2_C514`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C9::Method4.4834()"
		ldstr "class G1_C9`2<class BaseClass1,class BaseClass1> on type class G2_C514`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C514`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C9::Method7.4833<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass1,class BaseClass1> on type class G2_C514`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C514`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C514::Method7.9710<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C514`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G2_C514`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C514::Method4.9706()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C514`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G2_C514`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C514::Method5.9707()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C514`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G2_C514`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C514::Method6.MI.9709<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C514`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C514`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C514`2<class BaseClass0,class BaseClass1>::ClassMethod2389<object>()
		calli default string(class G2_C514`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C514::ClassMethod2389.9714<System.Object>()"
		ldstr "class G2_C514`2<class BaseClass0,class BaseClass1> on type class G2_C514`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C514`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C514`2<class BaseClass0,class BaseClass1>::ClassMethod2388<object>()
		calli default string(class G2_C514`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C514::ClassMethod2388.9713<System.Object>()"
		ldstr "class G2_C514`2<class BaseClass0,class BaseClass1> on type class G2_C514`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C514`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C514`2<class BaseClass0,class BaseClass1>::ClassMethod2387()
		calli default string(class G2_C514`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C514::ClassMethod2387.9712()"
		ldstr "class G2_C514`2<class BaseClass0,class BaseClass1> on type class G2_C514`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C514`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C514`2<class BaseClass0,class BaseClass1>::ClassMethod2386()
		calli default string(class G2_C514`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C514::ClassMethod2386.9711()"
		ldstr "class G2_C514`2<class BaseClass0,class BaseClass1> on type class G2_C514`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C514`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C514`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C514`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C514::Method7.9710<System.Object>()"
		ldstr "class G2_C514`2<class BaseClass0,class BaseClass1> on type class G2_C514`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C514`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C514`2<class BaseClass0,class BaseClass1>::Method6<object>()
		calli default string(class G2_C514`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C514::Method6.9708<System.Object>()"
		ldstr "class G2_C514`2<class BaseClass0,class BaseClass1> on type class G2_C514`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C514`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C514`2<class BaseClass0,class BaseClass1>::Method5()
		calli default string(class G2_C514`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C514::Method5.9707()"
		ldstr "class G2_C514`2<class BaseClass0,class BaseClass1> on type class G2_C514`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C514`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C514`2<class BaseClass0,class BaseClass1>::Method4()
		calli default string(class G2_C514`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C514::Method4.9706()"
		ldstr "class G2_C514`2<class BaseClass0,class BaseClass1> on type class G2_C514`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C514`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C514`2<class BaseClass0,class BaseClass1>::ClassMethod1339<object>()
		calli default string(class G2_C514`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C514::ClassMethod1339.MI.9718<System.Object>()"
		ldstr "class G2_C514`2<class BaseClass0,class BaseClass1> on type class G2_C514`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C514`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C514`2<class BaseClass0,class BaseClass1>::ClassMethod1338<object>()
		calli default string(class G2_C514`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C514::ClassMethod1338.MI.9717<System.Object>()"
		ldstr "class G2_C514`2<class BaseClass0,class BaseClass1> on type class G2_C514`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C514`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C514`2<class BaseClass0,class BaseClass1>::ClassMethod1337()
		calli default string(class G2_C514`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C514::ClassMethod1337.MI.9716()"
		ldstr "class G2_C514`2<class BaseClass0,class BaseClass1> on type class G2_C514`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C514`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C514`2<class BaseClass0,class BaseClass1>::ClassMethod1336()
		calli default string(class G2_C514`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C514::ClassMethod1336.MI.9715()"
		ldstr "class G2_C514`2<class BaseClass0,class BaseClass1> on type class G2_C514`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G2_C514`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C514::Method4.9706()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C514`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G2_C514`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C514::Method5.9707()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C514`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G2_C514`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C514::Method6.MI.9709<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C514`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C514`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass0,class BaseClass0>::ClassMethod1339<object>()
		calli default string(class G2_C514`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C514::ClassMethod1339.MI.9718<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C514`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass0,class BaseClass0>::ClassMethod1338<object>()
		calli default string(class G2_C514`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C514::ClassMethod1338.MI.9717<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C514`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass0,class BaseClass0>::ClassMethod1337()
		calli default string(class G2_C514`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C514::ClassMethod1337.MI.9716()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C514`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass0,class BaseClass0>::ClassMethod1336()
		calli default string(class G2_C514`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C514::ClassMethod1336.MI.9715()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C514`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass0,class BaseClass0>::Method6<object>()
		calli default string(class G2_C514`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C9::Method6.4838<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C514`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass0,class BaseClass0>::Method5()
		calli default string(class G2_C514`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C514::Method5.9707()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C514`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass0,class BaseClass0>::Method4()
		calli default string(class G2_C514`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C9::Method4.4834()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C514`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C514`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C9::Method7.4833<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C514`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C514`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C514::Method7.9710<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C514`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G2_C514`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C514::Method4.9706()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C514`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G2_C514`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C514::Method5.9707()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C514`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G2_C514`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C514::Method6.MI.9709<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C514`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C514`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C514::Method7.9710<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C514`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C514`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C514`2<class BaseClass1,class BaseClass0>::ClassMethod2389<object>()
		calli default string(class G2_C514`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C514::ClassMethod2389.9714<System.Object>()"
		ldstr "class G2_C514`2<class BaseClass1,class BaseClass0> on type class G2_C514`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C514`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C514`2<class BaseClass1,class BaseClass0>::ClassMethod2388<object>()
		calli default string(class G2_C514`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C514::ClassMethod2388.9713<System.Object>()"
		ldstr "class G2_C514`2<class BaseClass1,class BaseClass0> on type class G2_C514`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C514`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C514`2<class BaseClass1,class BaseClass0>::ClassMethod2387()
		calli default string(class G2_C514`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C514::ClassMethod2387.9712()"
		ldstr "class G2_C514`2<class BaseClass1,class BaseClass0> on type class G2_C514`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C514`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C514`2<class BaseClass1,class BaseClass0>::ClassMethod2386()
		calli default string(class G2_C514`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C514::ClassMethod2386.9711()"
		ldstr "class G2_C514`2<class BaseClass1,class BaseClass0> on type class G2_C514`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C514`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C514`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G2_C514`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C514::Method7.9710<System.Object>()"
		ldstr "class G2_C514`2<class BaseClass1,class BaseClass0> on type class G2_C514`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C514`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C514`2<class BaseClass1,class BaseClass0>::Method6<object>()
		calli default string(class G2_C514`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C514::Method6.9708<System.Object>()"
		ldstr "class G2_C514`2<class BaseClass1,class BaseClass0> on type class G2_C514`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C514`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C514`2<class BaseClass1,class BaseClass0>::Method5()
		calli default string(class G2_C514`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C514::Method5.9707()"
		ldstr "class G2_C514`2<class BaseClass1,class BaseClass0> on type class G2_C514`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C514`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C514`2<class BaseClass1,class BaseClass0>::Method4()
		calli default string(class G2_C514`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C514::Method4.9706()"
		ldstr "class G2_C514`2<class BaseClass1,class BaseClass0> on type class G2_C514`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C514`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C514`2<class BaseClass1,class BaseClass0>::ClassMethod1339<object>()
		calli default string(class G2_C514`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C514::ClassMethod1339.MI.9718<System.Object>()"
		ldstr "class G2_C514`2<class BaseClass1,class BaseClass0> on type class G2_C514`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C514`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C514`2<class BaseClass1,class BaseClass0>::ClassMethod1338<object>()
		calli default string(class G2_C514`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C514::ClassMethod1338.MI.9717<System.Object>()"
		ldstr "class G2_C514`2<class BaseClass1,class BaseClass0> on type class G2_C514`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C514`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C514`2<class BaseClass1,class BaseClass0>::ClassMethod1337()
		calli default string(class G2_C514`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C514::ClassMethod1337.MI.9716()"
		ldstr "class G2_C514`2<class BaseClass1,class BaseClass0> on type class G2_C514`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C514`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C514`2<class BaseClass1,class BaseClass0>::ClassMethod1336()
		calli default string(class G2_C514`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C514::ClassMethod1336.MI.9715()"
		ldstr "class G2_C514`2<class BaseClass1,class BaseClass0> on type class G2_C514`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G2_C514`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C514::Method4.9706()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C514`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G2_C514`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C514::Method5.9707()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C514`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G2_C514`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C514::Method6.MI.9709<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C514`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C514`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C9`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass1,class BaseClass1>::ClassMethod1339<object>()
		calli default string(class G2_C514`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C514::ClassMethod1339.MI.9718<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass1,class BaseClass1> on type class G2_C514`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass1,class BaseClass1>::ClassMethod1338<object>()
		calli default string(class G2_C514`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C514::ClassMethod1338.MI.9717<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass1,class BaseClass1> on type class G2_C514`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass1,class BaseClass1>::ClassMethod1337()
		calli default string(class G2_C514`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C514::ClassMethod1337.MI.9716()"
		ldstr "class G1_C9`2<class BaseClass1,class BaseClass1> on type class G2_C514`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass1,class BaseClass1>::ClassMethod1336()
		calli default string(class G2_C514`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C514::ClassMethod1336.MI.9715()"
		ldstr "class G1_C9`2<class BaseClass1,class BaseClass1> on type class G2_C514`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass1,class BaseClass1>::Method6<object>()
		calli default string(class G2_C514`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C9::Method6.4838<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass1,class BaseClass1> on type class G2_C514`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass1,class BaseClass1>::Method5()
		calli default string(class G2_C514`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C514::Method5.9707()"
		ldstr "class G1_C9`2<class BaseClass1,class BaseClass1> on type class G2_C514`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass1,class BaseClass1>::Method4()
		calli default string(class G2_C514`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C9::Method4.4834()"
		ldstr "class G1_C9`2<class BaseClass1,class BaseClass1> on type class G2_C514`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C514`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C9::Method7.4833<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass1,class BaseClass1> on type class G2_C514`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C514`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C514::Method7.9710<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C514`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G2_C514`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C514::Method4.9706()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C514`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G2_C514`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C514::Method5.9707()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C514`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G2_C514`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C514::Method6.MI.9709<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C514`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C514`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C514`2<class BaseClass1,class BaseClass1>::ClassMethod2389<object>()
		calli default string(class G2_C514`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C514::ClassMethod2389.9714<System.Object>()"
		ldstr "class G2_C514`2<class BaseClass1,class BaseClass1> on type class G2_C514`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C514`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C514`2<class BaseClass1,class BaseClass1>::ClassMethod2388<object>()
		calli default string(class G2_C514`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C514::ClassMethod2388.9713<System.Object>()"
		ldstr "class G2_C514`2<class BaseClass1,class BaseClass1> on type class G2_C514`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C514`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C514`2<class BaseClass1,class BaseClass1>::ClassMethod2387()
		calli default string(class G2_C514`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C514::ClassMethod2387.9712()"
		ldstr "class G2_C514`2<class BaseClass1,class BaseClass1> on type class G2_C514`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C514`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C514`2<class BaseClass1,class BaseClass1>::ClassMethod2386()
		calli default string(class G2_C514`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C514::ClassMethod2386.9711()"
		ldstr "class G2_C514`2<class BaseClass1,class BaseClass1> on type class G2_C514`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C514`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C514`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C514`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C514::Method7.9710<System.Object>()"
		ldstr "class G2_C514`2<class BaseClass1,class BaseClass1> on type class G2_C514`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C514`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C514`2<class BaseClass1,class BaseClass1>::Method6<object>()
		calli default string(class G2_C514`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C514::Method6.9708<System.Object>()"
		ldstr "class G2_C514`2<class BaseClass1,class BaseClass1> on type class G2_C514`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C514`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C514`2<class BaseClass1,class BaseClass1>::Method5()
		calli default string(class G2_C514`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C514::Method5.9707()"
		ldstr "class G2_C514`2<class BaseClass1,class BaseClass1> on type class G2_C514`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C514`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C514`2<class BaseClass1,class BaseClass1>::Method4()
		calli default string(class G2_C514`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C514::Method4.9706()"
		ldstr "class G2_C514`2<class BaseClass1,class BaseClass1> on type class G2_C514`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C514`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C514`2<class BaseClass1,class BaseClass1>::ClassMethod1339<object>()
		calli default string(class G2_C514`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C514::ClassMethod1339.MI.9718<System.Object>()"
		ldstr "class G2_C514`2<class BaseClass1,class BaseClass1> on type class G2_C514`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C514`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C514`2<class BaseClass1,class BaseClass1>::ClassMethod1338<object>()
		calli default string(class G2_C514`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C514::ClassMethod1338.MI.9717<System.Object>()"
		ldstr "class G2_C514`2<class BaseClass1,class BaseClass1> on type class G2_C514`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C514`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C514`2<class BaseClass1,class BaseClass1>::ClassMethod1337()
		calli default string(class G2_C514`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C514::ClassMethod1337.MI.9716()"
		ldstr "class G2_C514`2<class BaseClass1,class BaseClass1> on type class G2_C514`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C514`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C514`2<class BaseClass1,class BaseClass1>::ClassMethod1336()
		calli default string(class G2_C514`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C514::ClassMethod1336.MI.9715()"
		ldstr "class G2_C514`2<class BaseClass1,class BaseClass1> on type class G2_C514`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G2_C514`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C514::Method4.9706()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C514`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G2_C514`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C514::Method5.9707()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C514`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G2_C514`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C514::Method6.MI.9709<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C514`2<class BaseClass1,class BaseClass1>"
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
