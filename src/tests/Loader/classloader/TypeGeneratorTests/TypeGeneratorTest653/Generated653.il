// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) .ver 4:0:0:0 }
.assembly extern TestFramework { .publickeytoken = ( B0 3F 5F 7F 11 D5 0A 3A )  }

//TYPES IN FORWARDER ASSEMBLIES:

//TEST ASSEMBLY:
.assembly Generated653 { .hash algorithm 0x00008004 }
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
.class public G3_C1125`1<T0> 
		extends class G2_C157`1<class BaseClass0>
		implements class IBase2`2<class BaseClass0,class BaseClass0> 
{
	.method public hidebysig virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G3_C1125::Method7.14465<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod3647() cil managed noinlining { 
		ldstr "G3_C1125::ClassMethod3647.14466()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod3648() cil managed noinlining { 
		ldstr "G3_C1125::ClassMethod3648.14467()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod3649<M0>() cil managed noinlining { 
		ldstr "G3_C1125::ClassMethod3649.14468<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C157<class BaseClass0>.ClassMethod1637'() cil managed noinlining { 
		.override method instance string class G2_C157`1<class BaseClass0>::ClassMethod1637()
		ldstr "G3_C1125::ClassMethod1637.MI.14469()"
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class G2_C157`1<class BaseClass0>::.ctor()
		ret
	}
}
.class public G2_C157`1<T0> 
		extends class G1_C11`2<!T0,!T0>
		implements class IBase1`1<class BaseClass1>, class IBase2`2<!T0,!T0> 
{
	.method public hidebysig newslot virtual instance string Method4() cil managed noinlining { 
		ldstr "G2_C157::Method4.6236()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class BaseClass1>.Method4'() cil managed noinlining { 
		.override method instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G2_C157::Method4.MI.6237()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method5() cil managed noinlining { 
		ldstr "G2_C157::Method5.6238()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class BaseClass1>.Method5'() cil managed noinlining { 
		.override method instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G2_C157::Method5.MI.6239()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G2_C157::Method6.6240<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G2_C157::Method7.6241<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1637() cil managed noinlining { 
		ldstr "G2_C157::ClassMethod1637.6242()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1638() cil managed noinlining { 
		ldstr "G2_C157::ClassMethod1638.6243()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C11<T0,T0>.ClassMethod1344'<M0>() cil managed noinlining { 
		.override method instance string class G1_C11`2<!T0,!T0>::ClassMethod1344<[1]>()
		ldstr "G2_C157::ClassMethod1344.MI.6244<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C11<T0,T0>.ClassMethod1345'<M0>() cil managed noinlining { 
		.override method instance string class G1_C11`2<!T0,!T0>::ClassMethod1345<[1]>()
		ldstr "G2_C157::ClassMethod1345.MI.6245<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class G1_C11`2<!T0,!T0>::.ctor()
		ret
	}
}
.class interface public abstract IBase2`2<+T0, -T1> 
{
	.method public hidebysig newslot abstract virtual instance string Method7<M0>() cil managed { }
}
.class public G1_C11`2<T0, T1> 
		implements class IBase2`2<class BaseClass1,!T1>, class IBase1`1<class BaseClass0> 
{
	.method public hidebysig newslot virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G1_C11::Method7.4854<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase2<class BaseClass1,T1>.Method7'<M0>() cil managed noinlining { 
		.override method instance string class IBase2`2<class BaseClass1,!T1>::Method7<[1]>()
		ldstr "G1_C11::Method7.MI.4855<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig virtual instance string Method4() cil managed noinlining { 
		ldstr "G1_C11::Method4.4856()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class BaseClass0>.Method4'() cil managed noinlining { 
		.override method instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G1_C11::Method4.MI.4857()"
		ret
	}
	.method public hidebysig virtual instance string Method5() cil managed noinlining { 
		ldstr "G1_C11::Method5.4858()"
		ret
	}
	.method public hidebysig virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G1_C11::Method6.4859<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class BaseClass0>.Method6'<M0>() cil managed noinlining { 
		.override method instance string class IBase1`1<class BaseClass0>::Method6<[1]>()
		ldstr "G1_C11::Method6.MI.4860<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1344<M0>() cil managed noinlining { 
		ldstr "G1_C11::ClassMethod1344.4861<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1345<M0>() cil managed noinlining { 
		ldstr "G1_C11::ClassMethod1345.4862<"
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
.class public auto ansi beforefieldinit Generated653 {
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
	.method static void M.G3_C1125.T<T0,(class G3_C1125`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1125.T<T0,(class G3_C1125`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1125`1<!!T0>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1125`1<!!T0>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1125`1<!!T0>::ClassMethod1637()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1125`1<!!T0>::ClassMethod1638()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1125`1<!!T0>::ClassMethod3647()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1125`1<!!T0>::ClassMethod3648()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1125`1<!!T0>::ClassMethod3649<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1125`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1125`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1125`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1125`1<!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1125.A<(class G3_C1125`1<class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1125.A<(class G3_C1125`1<class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1125`1<class BaseClass0>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1125`1<class BaseClass0>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1125`1<class BaseClass0>::ClassMethod1637()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1125`1<class BaseClass0>::ClassMethod1638()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1125`1<class BaseClass0>::ClassMethod3647()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1125`1<class BaseClass0>::ClassMethod3648()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1125`1<class BaseClass0>::ClassMethod3649<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1125`1<class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1125`1<class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1125`1<class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1125`1<class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1125.B<(class G3_C1125`1<class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1125.B<(class G3_C1125`1<class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1125`1<class BaseClass1>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1125`1<class BaseClass1>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1125`1<class BaseClass1>::ClassMethod1637()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1125`1<class BaseClass1>::ClassMethod1638()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1125`1<class BaseClass1>::ClassMethod3647()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1125`1<class BaseClass1>::ClassMethod3648()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1125`1<class BaseClass1>::ClassMethod3649<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1125`1<class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1125`1<class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1125`1<class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1125`1<class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C157.T<T0,(class G2_C157`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C157.T<T0,(class G2_C157`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C157`1<!!T0>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C157`1<!!T0>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C157`1<!!T0>::ClassMethod1637()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C157`1<!!T0>::ClassMethod1638()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C157`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C157`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C157`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C157`1<!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C157.A<(class G2_C157`1<class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C157.A<(class G2_C157`1<class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C157`1<class BaseClass0>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C157`1<class BaseClass0>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C157`1<class BaseClass0>::ClassMethod1637()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C157`1<class BaseClass0>::ClassMethod1638()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C157`1<class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C157`1<class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C157`1<class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C157`1<class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C157.B<(class G2_C157`1<class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C157.B<(class G2_C157`1<class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C157`1<class BaseClass1>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C157`1<class BaseClass1>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C157`1<class BaseClass1>::ClassMethod1637()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C157`1<class BaseClass1>::ClassMethod1638()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C157`1<class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C157`1<class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C157`1<class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C157`1<class BaseClass1>::Method7<object>()
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
	.method static void M.G1_C11.T.T<T0,T1,(class G1_C11`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C11.T.T<T0,T1,(class G1_C11`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C11`2<!!T0,!!T1>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C11`2<!!T0,!!T1>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C11`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C11`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C11`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C11`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C11.A.T<T1,(class G1_C11`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C11.A.T<T1,(class G1_C11`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C11`2<class BaseClass0,!!T1>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C11`2<class BaseClass0,!!T1>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C11`2<class BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C11`2<class BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C11`2<class BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C11`2<class BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C11.A.A<(class G1_C11`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C11.A.A<(class G1_C11`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C11`2<class BaseClass0,class BaseClass0>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C11`2<class BaseClass0,class BaseClass0>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C11`2<class BaseClass0,class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C11`2<class BaseClass0,class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C11`2<class BaseClass0,class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C11`2<class BaseClass0,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C11.A.B<(class G1_C11`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C11.A.B<(class G1_C11`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C11`2<class BaseClass0,class BaseClass1>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C11`2<class BaseClass0,class BaseClass1>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C11`2<class BaseClass0,class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C11`2<class BaseClass0,class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C11`2<class BaseClass0,class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C11`2<class BaseClass0,class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C11.B.T<T1,(class G1_C11`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C11.B.T<T1,(class G1_C11`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C11`2<class BaseClass1,!!T1>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C11`2<class BaseClass1,!!T1>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C11`2<class BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C11`2<class BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C11`2<class BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C11`2<class BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C11.B.A<(class G1_C11`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C11.B.A<(class G1_C11`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C11`2<class BaseClass1,class BaseClass0>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C11`2<class BaseClass1,class BaseClass0>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C11`2<class BaseClass1,class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C11`2<class BaseClass1,class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C11`2<class BaseClass1,class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C11`2<class BaseClass1,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C11.B.B<(class G1_C11`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C11.B.B<(class G1_C11`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C11`2<class BaseClass1,class BaseClass1>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C11`2<class BaseClass1,class BaseClass1>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C11`2<class BaseClass1,class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C11`2<class BaseClass1,class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C11`2<class BaseClass1,class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C11`2<class BaseClass1,class BaseClass1>::Method7<object>()
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
		newobj instance void class G3_C1125`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C11`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C11`2<class BaseClass0,class BaseClass0>::ClassMethod1345<object>()
		ldstr "G2_C157::ClassMethod1345.MI.6245<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass0> on type class G3_C1125`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C11`2<class BaseClass0,class BaseClass0>::ClassMethod1344<object>()
		ldstr "G2_C157::ClassMethod1344.MI.6244<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass0> on type class G3_C1125`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C11`2<class BaseClass0,class BaseClass0>::Method6<object>()
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass0> on type class G3_C1125`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C11`2<class BaseClass0,class BaseClass0>::Method5()
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass0> on type class G3_C1125`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C11`2<class BaseClass0,class BaseClass0>::Method4()
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass0> on type class G3_C1125`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C11`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G1_C11::Method7.4854<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass0> on type class G3_C1125`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G3_C1125`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C157::Method4.MI.6237()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1125`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C157::Method5.MI.6239()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1125`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C157::Method6.6240<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1125`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G3_C1125::Method7.14465<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1125`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G3_C1125::Method7.14465<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1125`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1125`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C157`1<class BaseClass0>
		callvirt instance string class G2_C157`1<class BaseClass0>::ClassMethod1638()
		ldstr "G2_C157::ClassMethod1638.6243()"
		ldstr "class G2_C157`1<class BaseClass0> on type class G3_C1125`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C157`1<class BaseClass0>
		callvirt instance string class G2_C157`1<class BaseClass0>::ClassMethod1637()
		ldstr "G3_C1125::ClassMethod1637.MI.14469()"
		ldstr "class G2_C157`1<class BaseClass0> on type class G3_C1125`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C157`1<class BaseClass0>
		callvirt instance string class G2_C157`1<class BaseClass0>::Method7<object>()
		ldstr "G3_C1125::Method7.14465<System.Object>()"
		ldstr "class G2_C157`1<class BaseClass0> on type class G3_C1125`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C157`1<class BaseClass0>
		callvirt instance string class G2_C157`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C157::Method6.6240<System.Object>()"
		ldstr "class G2_C157`1<class BaseClass0> on type class G3_C1125`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C157`1<class BaseClass0>
		callvirt instance string class G2_C157`1<class BaseClass0>::Method5()
		ldstr "G2_C157::Method5.6238()"
		ldstr "class G2_C157`1<class BaseClass0> on type class G3_C1125`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C157`1<class BaseClass0>
		callvirt instance string class G2_C157`1<class BaseClass0>::Method4()
		ldstr "G2_C157::Method4.6236()"
		ldstr "class G2_C157`1<class BaseClass0> on type class G3_C1125`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C157`1<class BaseClass0>
		callvirt instance string class G2_C157`1<class BaseClass0>::ClassMethod1345<object>()
		ldstr "G2_C157::ClassMethod1345.MI.6245<System.Object>()"
		ldstr "class G2_C157`1<class BaseClass0> on type class G3_C1125`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C157`1<class BaseClass0>
		callvirt instance string class G2_C157`1<class BaseClass0>::ClassMethod1344<object>()
		ldstr "G2_C157::ClassMethod1344.MI.6244<System.Object>()"
		ldstr "class G2_C157`1<class BaseClass0> on type class G3_C1125`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G2_C157::Method4.MI.6237()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1125`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G2_C157::Method5.MI.6239()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1125`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C157::Method6.6240<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1125`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G3_C1125`1<class BaseClass0>
		callvirt instance string class G3_C1125`1<class BaseClass0>::ClassMethod3649<object>()
		ldstr "G3_C1125::ClassMethod3649.14468<System.Object>()"
		ldstr "class G3_C1125`1<class BaseClass0> on type class G3_C1125`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1125`1<class BaseClass0>
		callvirt instance string class G3_C1125`1<class BaseClass0>::ClassMethod3648()
		ldstr "G3_C1125::ClassMethod3648.14467()"
		ldstr "class G3_C1125`1<class BaseClass0> on type class G3_C1125`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1125`1<class BaseClass0>
		callvirt instance string class G3_C1125`1<class BaseClass0>::ClassMethod3647()
		ldstr "G3_C1125::ClassMethod3647.14466()"
		ldstr "class G3_C1125`1<class BaseClass0> on type class G3_C1125`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1125`1<class BaseClass0>
		callvirt instance string class G3_C1125`1<class BaseClass0>::Method7<object>()
		ldstr "G3_C1125::Method7.14465<System.Object>()"
		ldstr "class G3_C1125`1<class BaseClass0> on type class G3_C1125`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1125`1<class BaseClass0>
		callvirt instance string class G3_C1125`1<class BaseClass0>::ClassMethod1638()
		ldstr "G2_C157::ClassMethod1638.6243()"
		ldstr "class G3_C1125`1<class BaseClass0> on type class G3_C1125`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1125`1<class BaseClass0>
		callvirt instance string class G3_C1125`1<class BaseClass0>::ClassMethod1637()
		ldstr "G3_C1125::ClassMethod1637.MI.14469()"
		ldstr "class G3_C1125`1<class BaseClass0> on type class G3_C1125`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1125`1<class BaseClass0>
		callvirt instance string class G3_C1125`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C157::Method6.6240<System.Object>()"
		ldstr "class G3_C1125`1<class BaseClass0> on type class G3_C1125`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1125`1<class BaseClass0>
		callvirt instance string class G3_C1125`1<class BaseClass0>::Method5()
		ldstr "G2_C157::Method5.6238()"
		ldstr "class G3_C1125`1<class BaseClass0> on type class G3_C1125`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1125`1<class BaseClass0>
		callvirt instance string class G3_C1125`1<class BaseClass0>::Method4()
		ldstr "G2_C157::Method4.6236()"
		ldstr "class G3_C1125`1<class BaseClass0> on type class G3_C1125`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1125`1<class BaseClass0>
		callvirt instance string class G3_C1125`1<class BaseClass0>::ClassMethod1345<object>()
		ldstr "G2_C157::ClassMethod1345.MI.6245<System.Object>()"
		ldstr "class G3_C1125`1<class BaseClass0> on type class G3_C1125`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1125`1<class BaseClass0>
		callvirt instance string class G3_C1125`1<class BaseClass0>::ClassMethod1344<object>()
		ldstr "G2_C157::ClassMethod1344.MI.6244<System.Object>()"
		ldstr "class G3_C1125`1<class BaseClass0> on type class G3_C1125`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G3_C1125`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C11`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C11`2<class BaseClass0,class BaseClass0>::ClassMethod1345<object>()
		ldstr "G2_C157::ClassMethod1345.MI.6245<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass0> on type class G3_C1125`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C11`2<class BaseClass0,class BaseClass0>::ClassMethod1344<object>()
		ldstr "G2_C157::ClassMethod1344.MI.6244<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass0> on type class G3_C1125`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C11`2<class BaseClass0,class BaseClass0>::Method6<object>()
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass0> on type class G3_C1125`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C11`2<class BaseClass0,class BaseClass0>::Method5()
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass0> on type class G3_C1125`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C11`2<class BaseClass0,class BaseClass0>::Method4()
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass0> on type class G3_C1125`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C11`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G1_C11::Method7.4854<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass0> on type class G3_C1125`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G3_C1125`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C157::Method4.MI.6237()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1125`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C157::Method5.MI.6239()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1125`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C157::Method6.6240<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1125`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G3_C1125::Method7.14465<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1125`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G3_C1125::Method7.14465<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1125`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1125`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C157`1<class BaseClass0>
		callvirt instance string class G2_C157`1<class BaseClass0>::ClassMethod1638()
		ldstr "G2_C157::ClassMethod1638.6243()"
		ldstr "class G2_C157`1<class BaseClass0> on type class G3_C1125`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C157`1<class BaseClass0>
		callvirt instance string class G2_C157`1<class BaseClass0>::ClassMethod1637()
		ldstr "G3_C1125::ClassMethod1637.MI.14469()"
		ldstr "class G2_C157`1<class BaseClass0> on type class G3_C1125`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C157`1<class BaseClass0>
		callvirt instance string class G2_C157`1<class BaseClass0>::Method7<object>()
		ldstr "G3_C1125::Method7.14465<System.Object>()"
		ldstr "class G2_C157`1<class BaseClass0> on type class G3_C1125`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C157`1<class BaseClass0>
		callvirt instance string class G2_C157`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C157::Method6.6240<System.Object>()"
		ldstr "class G2_C157`1<class BaseClass0> on type class G3_C1125`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C157`1<class BaseClass0>
		callvirt instance string class G2_C157`1<class BaseClass0>::Method5()
		ldstr "G2_C157::Method5.6238()"
		ldstr "class G2_C157`1<class BaseClass0> on type class G3_C1125`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C157`1<class BaseClass0>
		callvirt instance string class G2_C157`1<class BaseClass0>::Method4()
		ldstr "G2_C157::Method4.6236()"
		ldstr "class G2_C157`1<class BaseClass0> on type class G3_C1125`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C157`1<class BaseClass0>
		callvirt instance string class G2_C157`1<class BaseClass0>::ClassMethod1345<object>()
		ldstr "G2_C157::ClassMethod1345.MI.6245<System.Object>()"
		ldstr "class G2_C157`1<class BaseClass0> on type class G3_C1125`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C157`1<class BaseClass0>
		callvirt instance string class G2_C157`1<class BaseClass0>::ClassMethod1344<object>()
		ldstr "G2_C157::ClassMethod1344.MI.6244<System.Object>()"
		ldstr "class G2_C157`1<class BaseClass0> on type class G3_C1125`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G2_C157::Method4.MI.6237()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1125`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G2_C157::Method5.MI.6239()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1125`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C157::Method6.6240<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1125`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G3_C1125`1<class BaseClass1>
		callvirt instance string class G3_C1125`1<class BaseClass1>::ClassMethod3649<object>()
		ldstr "G3_C1125::ClassMethod3649.14468<System.Object>()"
		ldstr "class G3_C1125`1<class BaseClass1> on type class G3_C1125`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1125`1<class BaseClass1>
		callvirt instance string class G3_C1125`1<class BaseClass1>::ClassMethod3648()
		ldstr "G3_C1125::ClassMethod3648.14467()"
		ldstr "class G3_C1125`1<class BaseClass1> on type class G3_C1125`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1125`1<class BaseClass1>
		callvirt instance string class G3_C1125`1<class BaseClass1>::ClassMethod3647()
		ldstr "G3_C1125::ClassMethod3647.14466()"
		ldstr "class G3_C1125`1<class BaseClass1> on type class G3_C1125`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1125`1<class BaseClass1>
		callvirt instance string class G3_C1125`1<class BaseClass1>::Method7<object>()
		ldstr "G3_C1125::Method7.14465<System.Object>()"
		ldstr "class G3_C1125`1<class BaseClass1> on type class G3_C1125`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1125`1<class BaseClass1>
		callvirt instance string class G3_C1125`1<class BaseClass1>::ClassMethod1638()
		ldstr "G2_C157::ClassMethod1638.6243()"
		ldstr "class G3_C1125`1<class BaseClass1> on type class G3_C1125`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1125`1<class BaseClass1>
		callvirt instance string class G3_C1125`1<class BaseClass1>::ClassMethod1637()
		ldstr "G3_C1125::ClassMethod1637.MI.14469()"
		ldstr "class G3_C1125`1<class BaseClass1> on type class G3_C1125`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1125`1<class BaseClass1>
		callvirt instance string class G3_C1125`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C157::Method6.6240<System.Object>()"
		ldstr "class G3_C1125`1<class BaseClass1> on type class G3_C1125`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1125`1<class BaseClass1>
		callvirt instance string class G3_C1125`1<class BaseClass1>::Method5()
		ldstr "G2_C157::Method5.6238()"
		ldstr "class G3_C1125`1<class BaseClass1> on type class G3_C1125`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1125`1<class BaseClass1>
		callvirt instance string class G3_C1125`1<class BaseClass1>::Method4()
		ldstr "G2_C157::Method4.6236()"
		ldstr "class G3_C1125`1<class BaseClass1> on type class G3_C1125`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1125`1<class BaseClass1>
		callvirt instance string class G3_C1125`1<class BaseClass1>::ClassMethod1345<object>()
		ldstr "G2_C157::ClassMethod1345.MI.6245<System.Object>()"
		ldstr "class G3_C1125`1<class BaseClass1> on type class G3_C1125`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1125`1<class BaseClass1>
		callvirt instance string class G3_C1125`1<class BaseClass1>::ClassMethod1344<object>()
		ldstr "G2_C157::ClassMethod1344.MI.6244<System.Object>()"
		ldstr "class G3_C1125`1<class BaseClass1> on type class G3_C1125`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C157`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C11`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C11`2<class BaseClass0,class BaseClass0>::ClassMethod1345<object>()
		ldstr "G2_C157::ClassMethod1345.MI.6245<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass0> on type class G2_C157`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C11`2<class BaseClass0,class BaseClass0>::ClassMethod1344<object>()
		ldstr "G2_C157::ClassMethod1344.MI.6244<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass0> on type class G2_C157`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C11`2<class BaseClass0,class BaseClass0>::Method6<object>()
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass0> on type class G2_C157`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C11`2<class BaseClass0,class BaseClass0>::Method5()
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass0> on type class G2_C157`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C11`2<class BaseClass0,class BaseClass0>::Method4()
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass0> on type class G2_C157`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C11`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G1_C11::Method7.4854<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass0> on type class G2_C157`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G2_C157`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C157::Method4.MI.6237()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C157`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C157::Method5.MI.6239()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C157`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C157::Method6.6240<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C157`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C157::Method7.6241<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C157`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C157::Method7.6241<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C157`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C157`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C157`1<class BaseClass0>
		callvirt instance string class G2_C157`1<class BaseClass0>::ClassMethod1638()
		ldstr "G2_C157::ClassMethod1638.6243()"
		ldstr "class G2_C157`1<class BaseClass0> on type class G2_C157`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C157`1<class BaseClass0>
		callvirt instance string class G2_C157`1<class BaseClass0>::ClassMethod1637()
		ldstr "G2_C157::ClassMethod1637.6242()"
		ldstr "class G2_C157`1<class BaseClass0> on type class G2_C157`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C157`1<class BaseClass0>
		callvirt instance string class G2_C157`1<class BaseClass0>::Method7<object>()
		ldstr "G2_C157::Method7.6241<System.Object>()"
		ldstr "class G2_C157`1<class BaseClass0> on type class G2_C157`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C157`1<class BaseClass0>
		callvirt instance string class G2_C157`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C157::Method6.6240<System.Object>()"
		ldstr "class G2_C157`1<class BaseClass0> on type class G2_C157`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C157`1<class BaseClass0>
		callvirt instance string class G2_C157`1<class BaseClass0>::Method5()
		ldstr "G2_C157::Method5.6238()"
		ldstr "class G2_C157`1<class BaseClass0> on type class G2_C157`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C157`1<class BaseClass0>
		callvirt instance string class G2_C157`1<class BaseClass0>::Method4()
		ldstr "G2_C157::Method4.6236()"
		ldstr "class G2_C157`1<class BaseClass0> on type class G2_C157`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C157`1<class BaseClass0>
		callvirt instance string class G2_C157`1<class BaseClass0>::ClassMethod1345<object>()
		ldstr "G2_C157::ClassMethod1345.MI.6245<System.Object>()"
		ldstr "class G2_C157`1<class BaseClass0> on type class G2_C157`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C157`1<class BaseClass0>
		callvirt instance string class G2_C157`1<class BaseClass0>::ClassMethod1344<object>()
		ldstr "G2_C157::ClassMethod1344.MI.6244<System.Object>()"
		ldstr "class G2_C157`1<class BaseClass0> on type class G2_C157`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G2_C157::Method4.MI.6237()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C157`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G2_C157::Method5.MI.6239()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C157`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C157::Method6.6240<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C157`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C157`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C11`2<class BaseClass1,class BaseClass1>::ClassMethod1345<object>()
		ldstr "G2_C157::ClassMethod1345.MI.6245<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G2_C157`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C11`2<class BaseClass1,class BaseClass1>::ClassMethod1344<object>()
		ldstr "G2_C157::ClassMethod1344.MI.6244<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G2_C157`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C11`2<class BaseClass1,class BaseClass1>::Method6<object>()
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G2_C157`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C11`2<class BaseClass1,class BaseClass1>::Method5()
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G2_C157`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C11`2<class BaseClass1,class BaseClass1>::Method4()
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G2_C157`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C11`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.4854<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G2_C157`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C157::Method7.6241<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C157`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C157::Method4.MI.6237()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C157`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C157::Method5.MI.6239()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C157`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C157::Method6.6240<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C157`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C157::Method7.6241<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C157`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C157`1<class BaseClass1>
		callvirt instance string class G2_C157`1<class BaseClass1>::ClassMethod1638()
		ldstr "G2_C157::ClassMethod1638.6243()"
		ldstr "class G2_C157`1<class BaseClass1> on type class G2_C157`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C157`1<class BaseClass1>
		callvirt instance string class G2_C157`1<class BaseClass1>::ClassMethod1637()
		ldstr "G2_C157::ClassMethod1637.6242()"
		ldstr "class G2_C157`1<class BaseClass1> on type class G2_C157`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C157`1<class BaseClass1>
		callvirt instance string class G2_C157`1<class BaseClass1>::Method7<object>()
		ldstr "G2_C157::Method7.6241<System.Object>()"
		ldstr "class G2_C157`1<class BaseClass1> on type class G2_C157`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C157`1<class BaseClass1>
		callvirt instance string class G2_C157`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C157::Method6.6240<System.Object>()"
		ldstr "class G2_C157`1<class BaseClass1> on type class G2_C157`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C157`1<class BaseClass1>
		callvirt instance string class G2_C157`1<class BaseClass1>::Method5()
		ldstr "G2_C157::Method5.6238()"
		ldstr "class G2_C157`1<class BaseClass1> on type class G2_C157`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C157`1<class BaseClass1>
		callvirt instance string class G2_C157`1<class BaseClass1>::Method4()
		ldstr "G2_C157::Method4.6236()"
		ldstr "class G2_C157`1<class BaseClass1> on type class G2_C157`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C157`1<class BaseClass1>
		callvirt instance string class G2_C157`1<class BaseClass1>::ClassMethod1345<object>()
		ldstr "G2_C157::ClassMethod1345.MI.6245<System.Object>()"
		ldstr "class G2_C157`1<class BaseClass1> on type class G2_C157`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C157`1<class BaseClass1>
		callvirt instance string class G2_C157`1<class BaseClass1>::ClassMethod1344<object>()
		ldstr "G2_C157::ClassMethod1344.MI.6244<System.Object>()"
		ldstr "class G2_C157`1<class BaseClass1> on type class G2_C157`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G2_C157::Method4.MI.6237()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C157`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G2_C157::Method5.MI.6239()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C157`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C157::Method6.6240<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C157`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G1_C11`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C11`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C11`2<class BaseClass0,class BaseClass0>::ClassMethod1345<object>()
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass0> on type class G1_C11`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C11`2<class BaseClass0,class BaseClass0>::ClassMethod1344<object>()
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass0> on type class G1_C11`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C11`2<class BaseClass0,class BaseClass0>::Method6<object>()
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass0> on type class G1_C11`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C11`2<class BaseClass0,class BaseClass0>::Method5()
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass0> on type class G1_C11`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C11`2<class BaseClass0,class BaseClass0>::Method4()
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass0> on type class G1_C11`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C11`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G1_C11::Method7.4854<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass0> on type class G1_C11`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G1_C11`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G1_C11::Method4.MI.4857()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C11`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G1_C11::Method5.4858()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C11`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G1_C11::Method6.MI.4860<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C11`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G1_C11`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G1_C11`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G1_C11`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G1_C11`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C11`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C11`2<class BaseClass0,class BaseClass1>::ClassMethod1345<object>()
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass1> on type class G1_C11`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C11`2<class BaseClass0,class BaseClass1>::ClassMethod1344<object>()
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass1> on type class G1_C11`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C11`2<class BaseClass0,class BaseClass1>::Method6<object>()
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass1> on type class G1_C11`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C11`2<class BaseClass0,class BaseClass1>::Method5()
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass1> on type class G1_C11`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C11`2<class BaseClass0,class BaseClass1>::Method4()
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass1> on type class G1_C11`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C11`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.4854<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass1> on type class G1_C11`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G1_C11`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G1_C11::Method4.MI.4857()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C11`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G1_C11::Method5.4858()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C11`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G1_C11::Method6.MI.4860<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C11`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G1_C11`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G1_C11`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C11`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C11`2<class BaseClass1,class BaseClass0>::ClassMethod1345<object>()
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass0> on type class G1_C11`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C11`2<class BaseClass1,class BaseClass0>::ClassMethod1344<object>()
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass0> on type class G1_C11`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C11`2<class BaseClass1,class BaseClass0>::Method6<object>()
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass0> on type class G1_C11`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C11`2<class BaseClass1,class BaseClass0>::Method5()
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass0> on type class G1_C11`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C11`2<class BaseClass1,class BaseClass0>::Method4()
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass0> on type class G1_C11`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C11`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G1_C11::Method7.4854<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass0> on type class G1_C11`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G1_C11`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G1_C11::Method4.MI.4857()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C11`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G1_C11::Method5.4858()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C11`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G1_C11::Method6.MI.4860<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C11`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G1_C11`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G1_C11`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G1_C11`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G1_C11`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C11`2<class BaseClass1,class BaseClass1>::ClassMethod1345<object>()
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G1_C11`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C11`2<class BaseClass1,class BaseClass1>::ClassMethod1344<object>()
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G1_C11`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C11`2<class BaseClass1,class BaseClass1>::Method6<object>()
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G1_C11`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C11`2<class BaseClass1,class BaseClass1>::Method5()
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G1_C11`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C11`2<class BaseClass1,class BaseClass1>::Method4()
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G1_C11`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C11`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.4854<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G1_C11`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G1_C11`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G1_C11::Method4.MI.4857()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C11`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G1_C11::Method5.4858()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C11`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G1_C11::Method6.MI.4860<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C11`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G1_C11`2<class BaseClass1,class BaseClass1>"
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
		newobj instance void class G3_C1125`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C157::ClassMethod1344.MI.6244<System.Object>()#G2_C157::ClassMethod1345.MI.6245<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated653::M.G1_C11.T.T<class BaseClass0,class BaseClass0,class G3_C1125`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C157::ClassMethod1344.MI.6244<System.Object>()#G2_C157::ClassMethod1345.MI.6245<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated653::M.G1_C11.A.T<class BaseClass0,class G3_C1125`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C157::ClassMethod1344.MI.6244<System.Object>()#G2_C157::ClassMethod1345.MI.6245<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated653::M.G1_C11.A.A<class G3_C1125`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated653::M.IBase2.T.T<class BaseClass1,class BaseClass0,class G3_C1125`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated653::M.IBase2.B.T<class BaseClass0,class G3_C1125`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated653::M.IBase2.B.A<class G3_C1125`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C157::Method4.MI.6237()#G2_C157::Method5.MI.6239()#G2_C157::Method6.6240<System.Object>()#"
		call void Generated653::M.IBase1.T<class BaseClass0,class G3_C1125`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C157::Method4.MI.6237()#G2_C157::Method5.MI.6239()#G2_C157::Method6.6240<System.Object>()#"
		call void Generated653::M.IBase1.A<class G3_C1125`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1125::Method7.14465<System.Object>()#"
		call void Generated653::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G3_C1125`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1125::Method7.14465<System.Object>()#"
		call void Generated653::M.IBase2.A.T<class BaseClass0,class G3_C1125`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1125::Method7.14465<System.Object>()#"
		call void Generated653::M.IBase2.A.A<class G3_C1125`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1125::Method7.14465<System.Object>()#"
		call void Generated653::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G3_C1125`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1125::Method7.14465<System.Object>()#"
		call void Generated653::M.IBase2.A.T<class BaseClass1,class G3_C1125`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1125::Method7.14465<System.Object>()#"
		call void Generated653::M.IBase2.A.B<class G3_C1125`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated653::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G3_C1125`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated653::M.IBase2.B.T<class BaseClass1,class G3_C1125`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated653::M.IBase2.B.B<class G3_C1125`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C157::ClassMethod1344.MI.6244<System.Object>()#G2_C157::ClassMethod1345.MI.6245<System.Object>()#G3_C1125::ClassMethod1637.MI.14469()#G2_C157::ClassMethod1638.6243()#G2_C157::Method4.6236()#G2_C157::Method5.6238()#G2_C157::Method6.6240<System.Object>()#G3_C1125::Method7.14465<System.Object>()#"
		call void Generated653::M.G2_C157.T<class BaseClass0,class G3_C1125`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C157::ClassMethod1344.MI.6244<System.Object>()#G2_C157::ClassMethod1345.MI.6245<System.Object>()#G3_C1125::ClassMethod1637.MI.14469()#G2_C157::ClassMethod1638.6243()#G2_C157::Method4.6236()#G2_C157::Method5.6238()#G2_C157::Method6.6240<System.Object>()#G3_C1125::Method7.14465<System.Object>()#"
		call void Generated653::M.G2_C157.A<class G3_C1125`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C157::Method4.MI.6237()#G2_C157::Method5.MI.6239()#G2_C157::Method6.6240<System.Object>()#"
		call void Generated653::M.IBase1.T<class BaseClass1,class G3_C1125`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C157::Method4.MI.6237()#G2_C157::Method5.MI.6239()#G2_C157::Method6.6240<System.Object>()#"
		call void Generated653::M.IBase1.B<class G3_C1125`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C157::ClassMethod1344.MI.6244<System.Object>()#G2_C157::ClassMethod1345.MI.6245<System.Object>()#G3_C1125::ClassMethod1637.MI.14469()#G2_C157::ClassMethod1638.6243()#G3_C1125::ClassMethod3647.14466()#G3_C1125::ClassMethod3648.14467()#G3_C1125::ClassMethod3649.14468<System.Object>()#G2_C157::Method4.6236()#G2_C157::Method5.6238()#G2_C157::Method6.6240<System.Object>()#G3_C1125::Method7.14465<System.Object>()#"
		call void Generated653::M.G3_C1125.T<class BaseClass0,class G3_C1125`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C157::ClassMethod1344.MI.6244<System.Object>()#G2_C157::ClassMethod1345.MI.6245<System.Object>()#G3_C1125::ClassMethod1637.MI.14469()#G2_C157::ClassMethod1638.6243()#G3_C1125::ClassMethod3647.14466()#G3_C1125::ClassMethod3648.14467()#G3_C1125::ClassMethod3649.14468<System.Object>()#G2_C157::Method4.6236()#G2_C157::Method5.6238()#G2_C157::Method6.6240<System.Object>()#G3_C1125::Method7.14465<System.Object>()#"
		call void Generated653::M.G3_C1125.A<class G3_C1125`1<class BaseClass0>>(!!0,string)
		newobj instance void class G3_C1125`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C157::ClassMethod1344.MI.6244<System.Object>()#G2_C157::ClassMethod1345.MI.6245<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated653::M.G1_C11.T.T<class BaseClass0,class BaseClass0,class G3_C1125`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C157::ClassMethod1344.MI.6244<System.Object>()#G2_C157::ClassMethod1345.MI.6245<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated653::M.G1_C11.A.T<class BaseClass0,class G3_C1125`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C157::ClassMethod1344.MI.6244<System.Object>()#G2_C157::ClassMethod1345.MI.6245<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated653::M.G1_C11.A.A<class G3_C1125`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated653::M.IBase2.T.T<class BaseClass1,class BaseClass0,class G3_C1125`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated653::M.IBase2.B.T<class BaseClass0,class G3_C1125`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated653::M.IBase2.B.A<class G3_C1125`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C157::Method4.MI.6237()#G2_C157::Method5.MI.6239()#G2_C157::Method6.6240<System.Object>()#"
		call void Generated653::M.IBase1.T<class BaseClass0,class G3_C1125`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C157::Method4.MI.6237()#G2_C157::Method5.MI.6239()#G2_C157::Method6.6240<System.Object>()#"
		call void Generated653::M.IBase1.A<class G3_C1125`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1125::Method7.14465<System.Object>()#"
		call void Generated653::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G3_C1125`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1125::Method7.14465<System.Object>()#"
		call void Generated653::M.IBase2.A.T<class BaseClass0,class G3_C1125`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1125::Method7.14465<System.Object>()#"
		call void Generated653::M.IBase2.A.A<class G3_C1125`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1125::Method7.14465<System.Object>()#"
		call void Generated653::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G3_C1125`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1125::Method7.14465<System.Object>()#"
		call void Generated653::M.IBase2.A.T<class BaseClass1,class G3_C1125`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1125::Method7.14465<System.Object>()#"
		call void Generated653::M.IBase2.A.B<class G3_C1125`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated653::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G3_C1125`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated653::M.IBase2.B.T<class BaseClass1,class G3_C1125`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated653::M.IBase2.B.B<class G3_C1125`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C157::ClassMethod1344.MI.6244<System.Object>()#G2_C157::ClassMethod1345.MI.6245<System.Object>()#G3_C1125::ClassMethod1637.MI.14469()#G2_C157::ClassMethod1638.6243()#G2_C157::Method4.6236()#G2_C157::Method5.6238()#G2_C157::Method6.6240<System.Object>()#G3_C1125::Method7.14465<System.Object>()#"
		call void Generated653::M.G2_C157.T<class BaseClass0,class G3_C1125`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C157::ClassMethod1344.MI.6244<System.Object>()#G2_C157::ClassMethod1345.MI.6245<System.Object>()#G3_C1125::ClassMethod1637.MI.14469()#G2_C157::ClassMethod1638.6243()#G2_C157::Method4.6236()#G2_C157::Method5.6238()#G2_C157::Method6.6240<System.Object>()#G3_C1125::Method7.14465<System.Object>()#"
		call void Generated653::M.G2_C157.A<class G3_C1125`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C157::Method4.MI.6237()#G2_C157::Method5.MI.6239()#G2_C157::Method6.6240<System.Object>()#"
		call void Generated653::M.IBase1.T<class BaseClass1,class G3_C1125`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C157::Method4.MI.6237()#G2_C157::Method5.MI.6239()#G2_C157::Method6.6240<System.Object>()#"
		call void Generated653::M.IBase1.B<class G3_C1125`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C157::ClassMethod1344.MI.6244<System.Object>()#G2_C157::ClassMethod1345.MI.6245<System.Object>()#G3_C1125::ClassMethod1637.MI.14469()#G2_C157::ClassMethod1638.6243()#G3_C1125::ClassMethod3647.14466()#G3_C1125::ClassMethod3648.14467()#G3_C1125::ClassMethod3649.14468<System.Object>()#G2_C157::Method4.6236()#G2_C157::Method5.6238()#G2_C157::Method6.6240<System.Object>()#G3_C1125::Method7.14465<System.Object>()#"
		call void Generated653::M.G3_C1125.T<class BaseClass1,class G3_C1125`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C157::ClassMethod1344.MI.6244<System.Object>()#G2_C157::ClassMethod1345.MI.6245<System.Object>()#G3_C1125::ClassMethod1637.MI.14469()#G2_C157::ClassMethod1638.6243()#G3_C1125::ClassMethod3647.14466()#G3_C1125::ClassMethod3648.14467()#G3_C1125::ClassMethod3649.14468<System.Object>()#G2_C157::Method4.6236()#G2_C157::Method5.6238()#G2_C157::Method6.6240<System.Object>()#G3_C1125::Method7.14465<System.Object>()#"
		call void Generated653::M.G3_C1125.B<class G3_C1125`1<class BaseClass1>>(!!0,string)
		newobj instance void class G2_C157`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C157::ClassMethod1344.MI.6244<System.Object>()#G2_C157::ClassMethod1345.MI.6245<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated653::M.G1_C11.T.T<class BaseClass0,class BaseClass0,class G2_C157`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C157::ClassMethod1344.MI.6244<System.Object>()#G2_C157::ClassMethod1345.MI.6245<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated653::M.G1_C11.A.T<class BaseClass0,class G2_C157`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C157::ClassMethod1344.MI.6244<System.Object>()#G2_C157::ClassMethod1345.MI.6245<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated653::M.G1_C11.A.A<class G2_C157`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated653::M.IBase2.T.T<class BaseClass1,class BaseClass0,class G2_C157`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated653::M.IBase2.B.T<class BaseClass0,class G2_C157`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated653::M.IBase2.B.A<class G2_C157`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C157::Method4.MI.6237()#G2_C157::Method5.MI.6239()#G2_C157::Method6.6240<System.Object>()#"
		call void Generated653::M.IBase1.T<class BaseClass0,class G2_C157`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C157::Method4.MI.6237()#G2_C157::Method5.MI.6239()#G2_C157::Method6.6240<System.Object>()#"
		call void Generated653::M.IBase1.A<class G2_C157`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C157::Method7.6241<System.Object>()#"
		call void Generated653::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G2_C157`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C157::Method7.6241<System.Object>()#"
		call void Generated653::M.IBase2.A.T<class BaseClass0,class G2_C157`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C157::Method7.6241<System.Object>()#"
		call void Generated653::M.IBase2.A.A<class G2_C157`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C157::Method7.6241<System.Object>()#"
		call void Generated653::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C157`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C157::Method7.6241<System.Object>()#"
		call void Generated653::M.IBase2.A.T<class BaseClass1,class G2_C157`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C157::Method7.6241<System.Object>()#"
		call void Generated653::M.IBase2.A.B<class G2_C157`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated653::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G2_C157`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated653::M.IBase2.B.T<class BaseClass1,class G2_C157`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated653::M.IBase2.B.B<class G2_C157`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C157::ClassMethod1344.MI.6244<System.Object>()#G2_C157::ClassMethod1345.MI.6245<System.Object>()#G2_C157::ClassMethod1637.6242()#G2_C157::ClassMethod1638.6243()#G2_C157::Method4.6236()#G2_C157::Method5.6238()#G2_C157::Method6.6240<System.Object>()#G2_C157::Method7.6241<System.Object>()#"
		call void Generated653::M.G2_C157.T<class BaseClass0,class G2_C157`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C157::ClassMethod1344.MI.6244<System.Object>()#G2_C157::ClassMethod1345.MI.6245<System.Object>()#G2_C157::ClassMethod1637.6242()#G2_C157::ClassMethod1638.6243()#G2_C157::Method4.6236()#G2_C157::Method5.6238()#G2_C157::Method6.6240<System.Object>()#G2_C157::Method7.6241<System.Object>()#"
		call void Generated653::M.G2_C157.A<class G2_C157`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C157::Method4.MI.6237()#G2_C157::Method5.MI.6239()#G2_C157::Method6.6240<System.Object>()#"
		call void Generated653::M.IBase1.T<class BaseClass1,class G2_C157`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C157::Method4.MI.6237()#G2_C157::Method5.MI.6239()#G2_C157::Method6.6240<System.Object>()#"
		call void Generated653::M.IBase1.B<class G2_C157`1<class BaseClass0>>(!!0,string)
		newobj instance void class G2_C157`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C157::ClassMethod1344.MI.6244<System.Object>()#G2_C157::ClassMethod1345.MI.6245<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated653::M.G1_C11.T.T<class BaseClass1,class BaseClass1,class G2_C157`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C157::ClassMethod1344.MI.6244<System.Object>()#G2_C157::ClassMethod1345.MI.6245<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated653::M.G1_C11.B.T<class BaseClass1,class G2_C157`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C157::ClassMethod1344.MI.6244<System.Object>()#G2_C157::ClassMethod1345.MI.6245<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated653::M.G1_C11.B.B<class G2_C157`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C157::Method7.6241<System.Object>()#"
		call void Generated653::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G2_C157`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C157::Method7.6241<System.Object>()#"
		call void Generated653::M.IBase2.B.T<class BaseClass1,class G2_C157`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C157::Method7.6241<System.Object>()#"
		call void Generated653::M.IBase2.B.B<class G2_C157`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C157::Method4.MI.6237()#G2_C157::Method5.MI.6239()#G2_C157::Method6.6240<System.Object>()#"
		call void Generated653::M.IBase1.T<class BaseClass0,class G2_C157`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C157::Method4.MI.6237()#G2_C157::Method5.MI.6239()#G2_C157::Method6.6240<System.Object>()#"
		call void Generated653::M.IBase1.A<class G2_C157`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C157::Method7.6241<System.Object>()#"
		call void Generated653::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C157`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C157::Method7.6241<System.Object>()#"
		call void Generated653::M.IBase2.A.T<class BaseClass1,class G2_C157`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C157::Method7.6241<System.Object>()#"
		call void Generated653::M.IBase2.A.B<class G2_C157`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C157::ClassMethod1344.MI.6244<System.Object>()#G2_C157::ClassMethod1345.MI.6245<System.Object>()#G2_C157::ClassMethod1637.6242()#G2_C157::ClassMethod1638.6243()#G2_C157::Method4.6236()#G2_C157::Method5.6238()#G2_C157::Method6.6240<System.Object>()#G2_C157::Method7.6241<System.Object>()#"
		call void Generated653::M.G2_C157.T<class BaseClass1,class G2_C157`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C157::ClassMethod1344.MI.6244<System.Object>()#G2_C157::ClassMethod1345.MI.6245<System.Object>()#G2_C157::ClassMethod1637.6242()#G2_C157::ClassMethod1638.6243()#G2_C157::Method4.6236()#G2_C157::Method5.6238()#G2_C157::Method6.6240<System.Object>()#G2_C157::Method7.6241<System.Object>()#"
		call void Generated653::M.G2_C157.B<class G2_C157`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C157::Method4.MI.6237()#G2_C157::Method5.MI.6239()#G2_C157::Method6.6240<System.Object>()#"
		call void Generated653::M.IBase1.T<class BaseClass1,class G2_C157`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C157::Method4.MI.6237()#G2_C157::Method5.MI.6239()#G2_C157::Method6.6240<System.Object>()#"
		call void Generated653::M.IBase1.B<class G2_C157`1<class BaseClass1>>(!!0,string)
		newobj instance void class G1_C11`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated653::M.G1_C11.T.T<class BaseClass0,class BaseClass0,class G1_C11`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated653::M.G1_C11.A.T<class BaseClass0,class G1_C11`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated653::M.G1_C11.A.A<class G1_C11`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated653::M.IBase2.T.T<class BaseClass1,class BaseClass0,class G1_C11`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated653::M.IBase2.B.T<class BaseClass0,class G1_C11`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated653::M.IBase2.B.A<class G1_C11`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method4.MI.4857()#G1_C11::Method5.4858()#G1_C11::Method6.MI.4860<System.Object>()#"
		call void Generated653::M.IBase1.T<class BaseClass0,class G1_C11`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method4.MI.4857()#G1_C11::Method5.4858()#G1_C11::Method6.MI.4860<System.Object>()#"
		call void Generated653::M.IBase1.A<class G1_C11`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated653::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G1_C11`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated653::M.IBase2.A.T<class BaseClass0,class G1_C11`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated653::M.IBase2.A.A<class G1_C11`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated653::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G1_C11`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated653::M.IBase2.A.T<class BaseClass1,class G1_C11`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated653::M.IBase2.A.B<class G1_C11`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated653::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G1_C11`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated653::M.IBase2.B.T<class BaseClass1,class G1_C11`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated653::M.IBase2.B.B<class G1_C11`2<class BaseClass0,class BaseClass0>>(!!0,string)
		newobj instance void class G1_C11`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated653::M.G1_C11.T.T<class BaseClass0,class BaseClass1,class G1_C11`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated653::M.G1_C11.A.T<class BaseClass1,class G1_C11`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated653::M.G1_C11.A.B<class G1_C11`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated653::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G1_C11`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated653::M.IBase2.B.T<class BaseClass1,class G1_C11`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated653::M.IBase2.B.B<class G1_C11`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method4.MI.4857()#G1_C11::Method5.4858()#G1_C11::Method6.MI.4860<System.Object>()#"
		call void Generated653::M.IBase1.T<class BaseClass0,class G1_C11`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method4.MI.4857()#G1_C11::Method5.4858()#G1_C11::Method6.MI.4860<System.Object>()#"
		call void Generated653::M.IBase1.A<class G1_C11`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated653::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G1_C11`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated653::M.IBase2.A.T<class BaseClass1,class G1_C11`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated653::M.IBase2.A.B<class G1_C11`2<class BaseClass0,class BaseClass1>>(!!0,string)
		newobj instance void class G1_C11`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated653::M.G1_C11.T.T<class BaseClass1,class BaseClass0,class G1_C11`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated653::M.G1_C11.B.T<class BaseClass0,class G1_C11`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated653::M.G1_C11.B.A<class G1_C11`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated653::M.IBase2.T.T<class BaseClass1,class BaseClass0,class G1_C11`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated653::M.IBase2.B.T<class BaseClass0,class G1_C11`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated653::M.IBase2.B.A<class G1_C11`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method4.MI.4857()#G1_C11::Method5.4858()#G1_C11::Method6.MI.4860<System.Object>()#"
		call void Generated653::M.IBase1.T<class BaseClass0,class G1_C11`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method4.MI.4857()#G1_C11::Method5.4858()#G1_C11::Method6.MI.4860<System.Object>()#"
		call void Generated653::M.IBase1.A<class G1_C11`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated653::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G1_C11`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated653::M.IBase2.A.T<class BaseClass0,class G1_C11`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated653::M.IBase2.A.A<class G1_C11`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated653::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G1_C11`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated653::M.IBase2.A.T<class BaseClass1,class G1_C11`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated653::M.IBase2.A.B<class G1_C11`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated653::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G1_C11`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated653::M.IBase2.B.T<class BaseClass1,class G1_C11`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated653::M.IBase2.B.B<class G1_C11`2<class BaseClass1,class BaseClass0>>(!!0,string)
		newobj instance void class G1_C11`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated653::M.G1_C11.T.T<class BaseClass1,class BaseClass1,class G1_C11`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated653::M.G1_C11.B.T<class BaseClass1,class G1_C11`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated653::M.G1_C11.B.B<class G1_C11`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated653::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G1_C11`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated653::M.IBase2.B.T<class BaseClass1,class G1_C11`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated653::M.IBase2.B.B<class G1_C11`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method4.MI.4857()#G1_C11::Method5.4858()#G1_C11::Method6.MI.4860<System.Object>()#"
		call void Generated653::M.IBase1.T<class BaseClass0,class G1_C11`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method4.MI.4857()#G1_C11::Method5.4858()#G1_C11::Method6.MI.4860<System.Object>()#"
		call void Generated653::M.IBase1.A<class G1_C11`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated653::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G1_C11`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated653::M.IBase2.A.T<class BaseClass1,class G1_C11`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated653::M.IBase2.A.B<class G1_C11`2<class BaseClass1,class BaseClass1>>(!!0,string)
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
		newobj instance void class G3_C1125`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C11`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass0,class BaseClass0>::ClassMethod1345<object>()
		calli default string(class G3_C1125`1<class BaseClass0>)
		ldstr "G2_C157::ClassMethod1345.MI.6245<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass0> on type class G3_C1125`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass0,class BaseClass0>::ClassMethod1344<object>()
		calli default string(class G3_C1125`1<class BaseClass0>)
		ldstr "G2_C157::ClassMethod1344.MI.6244<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass0> on type class G3_C1125`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass0,class BaseClass0>::Method6<object>()
		calli default string(class G3_C1125`1<class BaseClass0>)
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass0> on type class G3_C1125`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass0,class BaseClass0>::Method5()
		calli default string(class G3_C1125`1<class BaseClass0>)
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass0> on type class G3_C1125`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass0,class BaseClass0>::Method4()
		calli default string(class G3_C1125`1<class BaseClass0>)
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass0> on type class G3_C1125`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1125`1<class BaseClass0>)
		ldstr "G1_C11::Method7.4854<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass0> on type class G3_C1125`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1125`1<class BaseClass0>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G3_C1125`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G3_C1125`1<class BaseClass0>)
		ldstr "G2_C157::Method4.MI.6237()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1125`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G3_C1125`1<class BaseClass0>)
		ldstr "G2_C157::Method5.MI.6239()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1125`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1125`1<class BaseClass0>)
		ldstr "G2_C157::Method6.6240<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1125`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1125`1<class BaseClass0>)
		ldstr "G3_C1125::Method7.14465<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1125`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1125`1<class BaseClass0>)
		ldstr "G3_C1125::Method7.14465<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1125`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1125`1<class BaseClass0>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1125`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C157`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C157`1<class BaseClass0>::ClassMethod1638()
		calli default string(class G3_C1125`1<class BaseClass0>)
		ldstr "G2_C157::ClassMethod1638.6243()"
		ldstr "class G2_C157`1<class BaseClass0> on type class G3_C1125`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C157`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C157`1<class BaseClass0>::ClassMethod1637()
		calli default string(class G3_C1125`1<class BaseClass0>)
		ldstr "G3_C1125::ClassMethod1637.MI.14469()"
		ldstr "class G2_C157`1<class BaseClass0> on type class G3_C1125`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C157`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C157`1<class BaseClass0>::Method7<object>()
		calli default string(class G3_C1125`1<class BaseClass0>)
		ldstr "G3_C1125::Method7.14465<System.Object>()"
		ldstr "class G2_C157`1<class BaseClass0> on type class G3_C1125`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C157`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C157`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1125`1<class BaseClass0>)
		ldstr "G2_C157::Method6.6240<System.Object>()"
		ldstr "class G2_C157`1<class BaseClass0> on type class G3_C1125`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C157`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C157`1<class BaseClass0>::Method5()
		calli default string(class G3_C1125`1<class BaseClass0>)
		ldstr "G2_C157::Method5.6238()"
		ldstr "class G2_C157`1<class BaseClass0> on type class G3_C1125`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C157`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C157`1<class BaseClass0>::Method4()
		calli default string(class G3_C1125`1<class BaseClass0>)
		ldstr "G2_C157::Method4.6236()"
		ldstr "class G2_C157`1<class BaseClass0> on type class G3_C1125`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C157`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C157`1<class BaseClass0>::ClassMethod1345<object>()
		calli default string(class G3_C1125`1<class BaseClass0>)
		ldstr "G2_C157::ClassMethod1345.MI.6245<System.Object>()"
		ldstr "class G2_C157`1<class BaseClass0> on type class G3_C1125`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C157`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C157`1<class BaseClass0>::ClassMethod1344<object>()
		calli default string(class G3_C1125`1<class BaseClass0>)
		ldstr "G2_C157::ClassMethod1344.MI.6244<System.Object>()"
		ldstr "class G2_C157`1<class BaseClass0> on type class G3_C1125`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G3_C1125`1<class BaseClass0>)
		ldstr "G2_C157::Method4.MI.6237()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1125`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G3_C1125`1<class BaseClass0>)
		ldstr "G2_C157::Method5.MI.6239()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1125`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G3_C1125`1<class BaseClass0>)
		ldstr "G2_C157::Method6.6240<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1125`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1125`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1125`1<class BaseClass0>::ClassMethod3649<object>()
		calli default string(class G3_C1125`1<class BaseClass0>)
		ldstr "G3_C1125::ClassMethod3649.14468<System.Object>()"
		ldstr "class G3_C1125`1<class BaseClass0> on type class G3_C1125`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1125`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1125`1<class BaseClass0>::ClassMethod3648()
		calli default string(class G3_C1125`1<class BaseClass0>)
		ldstr "G3_C1125::ClassMethod3648.14467()"
		ldstr "class G3_C1125`1<class BaseClass0> on type class G3_C1125`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1125`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1125`1<class BaseClass0>::ClassMethod3647()
		calli default string(class G3_C1125`1<class BaseClass0>)
		ldstr "G3_C1125::ClassMethod3647.14466()"
		ldstr "class G3_C1125`1<class BaseClass0> on type class G3_C1125`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1125`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1125`1<class BaseClass0>::Method7<object>()
		calli default string(class G3_C1125`1<class BaseClass0>)
		ldstr "G3_C1125::Method7.14465<System.Object>()"
		ldstr "class G3_C1125`1<class BaseClass0> on type class G3_C1125`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1125`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1125`1<class BaseClass0>::ClassMethod1638()
		calli default string(class G3_C1125`1<class BaseClass0>)
		ldstr "G2_C157::ClassMethod1638.6243()"
		ldstr "class G3_C1125`1<class BaseClass0> on type class G3_C1125`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1125`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1125`1<class BaseClass0>::ClassMethod1637()
		calli default string(class G3_C1125`1<class BaseClass0>)
		ldstr "G3_C1125::ClassMethod1637.MI.14469()"
		ldstr "class G3_C1125`1<class BaseClass0> on type class G3_C1125`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1125`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1125`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1125`1<class BaseClass0>)
		ldstr "G2_C157::Method6.6240<System.Object>()"
		ldstr "class G3_C1125`1<class BaseClass0> on type class G3_C1125`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1125`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1125`1<class BaseClass0>::Method5()
		calli default string(class G3_C1125`1<class BaseClass0>)
		ldstr "G2_C157::Method5.6238()"
		ldstr "class G3_C1125`1<class BaseClass0> on type class G3_C1125`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1125`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1125`1<class BaseClass0>::Method4()
		calli default string(class G3_C1125`1<class BaseClass0>)
		ldstr "G2_C157::Method4.6236()"
		ldstr "class G3_C1125`1<class BaseClass0> on type class G3_C1125`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1125`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1125`1<class BaseClass0>::ClassMethod1345<object>()
		calli default string(class G3_C1125`1<class BaseClass0>)
		ldstr "G2_C157::ClassMethod1345.MI.6245<System.Object>()"
		ldstr "class G3_C1125`1<class BaseClass0> on type class G3_C1125`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1125`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1125`1<class BaseClass0>::ClassMethod1344<object>()
		calli default string(class G3_C1125`1<class BaseClass0>)
		ldstr "G2_C157::ClassMethod1344.MI.6244<System.Object>()"
		ldstr "class G3_C1125`1<class BaseClass0> on type class G3_C1125`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G3_C1125`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C11`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass0,class BaseClass0>::ClassMethod1345<object>()
		calli default string(class G3_C1125`1<class BaseClass1>)
		ldstr "G2_C157::ClassMethod1345.MI.6245<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass0> on type class G3_C1125`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass0,class BaseClass0>::ClassMethod1344<object>()
		calli default string(class G3_C1125`1<class BaseClass1>)
		ldstr "G2_C157::ClassMethod1344.MI.6244<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass0> on type class G3_C1125`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass0,class BaseClass0>::Method6<object>()
		calli default string(class G3_C1125`1<class BaseClass1>)
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass0> on type class G3_C1125`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass0,class BaseClass0>::Method5()
		calli default string(class G3_C1125`1<class BaseClass1>)
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass0> on type class G3_C1125`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass0,class BaseClass0>::Method4()
		calli default string(class G3_C1125`1<class BaseClass1>)
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass0> on type class G3_C1125`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1125`1<class BaseClass1>)
		ldstr "G1_C11::Method7.4854<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass0> on type class G3_C1125`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1125`1<class BaseClass1>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G3_C1125`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G3_C1125`1<class BaseClass1>)
		ldstr "G2_C157::Method4.MI.6237()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1125`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G3_C1125`1<class BaseClass1>)
		ldstr "G2_C157::Method5.MI.6239()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1125`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1125`1<class BaseClass1>)
		ldstr "G2_C157::Method6.6240<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1125`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1125`1<class BaseClass1>)
		ldstr "G3_C1125::Method7.14465<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1125`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1125`1<class BaseClass1>)
		ldstr "G3_C1125::Method7.14465<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1125`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1125`1<class BaseClass1>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1125`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C157`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C157`1<class BaseClass0>::ClassMethod1638()
		calli default string(class G3_C1125`1<class BaseClass1>)
		ldstr "G2_C157::ClassMethod1638.6243()"
		ldstr "class G2_C157`1<class BaseClass0> on type class G3_C1125`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C157`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C157`1<class BaseClass0>::ClassMethod1637()
		calli default string(class G3_C1125`1<class BaseClass1>)
		ldstr "G3_C1125::ClassMethod1637.MI.14469()"
		ldstr "class G2_C157`1<class BaseClass0> on type class G3_C1125`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C157`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C157`1<class BaseClass0>::Method7<object>()
		calli default string(class G3_C1125`1<class BaseClass1>)
		ldstr "G3_C1125::Method7.14465<System.Object>()"
		ldstr "class G2_C157`1<class BaseClass0> on type class G3_C1125`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C157`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C157`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1125`1<class BaseClass1>)
		ldstr "G2_C157::Method6.6240<System.Object>()"
		ldstr "class G2_C157`1<class BaseClass0> on type class G3_C1125`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C157`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C157`1<class BaseClass0>::Method5()
		calli default string(class G3_C1125`1<class BaseClass1>)
		ldstr "G2_C157::Method5.6238()"
		ldstr "class G2_C157`1<class BaseClass0> on type class G3_C1125`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C157`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C157`1<class BaseClass0>::Method4()
		calli default string(class G3_C1125`1<class BaseClass1>)
		ldstr "G2_C157::Method4.6236()"
		ldstr "class G2_C157`1<class BaseClass0> on type class G3_C1125`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C157`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C157`1<class BaseClass0>::ClassMethod1345<object>()
		calli default string(class G3_C1125`1<class BaseClass1>)
		ldstr "G2_C157::ClassMethod1345.MI.6245<System.Object>()"
		ldstr "class G2_C157`1<class BaseClass0> on type class G3_C1125`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C157`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C157`1<class BaseClass0>::ClassMethod1344<object>()
		calli default string(class G3_C1125`1<class BaseClass1>)
		ldstr "G2_C157::ClassMethod1344.MI.6244<System.Object>()"
		ldstr "class G2_C157`1<class BaseClass0> on type class G3_C1125`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G3_C1125`1<class BaseClass1>)
		ldstr "G2_C157::Method4.MI.6237()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1125`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G3_C1125`1<class BaseClass1>)
		ldstr "G2_C157::Method5.MI.6239()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1125`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G3_C1125`1<class BaseClass1>)
		ldstr "G2_C157::Method6.6240<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1125`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1125`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1125`1<class BaseClass1>::ClassMethod3649<object>()
		calli default string(class G3_C1125`1<class BaseClass1>)
		ldstr "G3_C1125::ClassMethod3649.14468<System.Object>()"
		ldstr "class G3_C1125`1<class BaseClass1> on type class G3_C1125`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1125`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1125`1<class BaseClass1>::ClassMethod3648()
		calli default string(class G3_C1125`1<class BaseClass1>)
		ldstr "G3_C1125::ClassMethod3648.14467()"
		ldstr "class G3_C1125`1<class BaseClass1> on type class G3_C1125`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1125`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1125`1<class BaseClass1>::ClassMethod3647()
		calli default string(class G3_C1125`1<class BaseClass1>)
		ldstr "G3_C1125::ClassMethod3647.14466()"
		ldstr "class G3_C1125`1<class BaseClass1> on type class G3_C1125`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1125`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1125`1<class BaseClass1>::Method7<object>()
		calli default string(class G3_C1125`1<class BaseClass1>)
		ldstr "G3_C1125::Method7.14465<System.Object>()"
		ldstr "class G3_C1125`1<class BaseClass1> on type class G3_C1125`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1125`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1125`1<class BaseClass1>::ClassMethod1638()
		calli default string(class G3_C1125`1<class BaseClass1>)
		ldstr "G2_C157::ClassMethod1638.6243()"
		ldstr "class G3_C1125`1<class BaseClass1> on type class G3_C1125`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1125`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1125`1<class BaseClass1>::ClassMethod1637()
		calli default string(class G3_C1125`1<class BaseClass1>)
		ldstr "G3_C1125::ClassMethod1637.MI.14469()"
		ldstr "class G3_C1125`1<class BaseClass1> on type class G3_C1125`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1125`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1125`1<class BaseClass1>::Method6<object>()
		calli default string(class G3_C1125`1<class BaseClass1>)
		ldstr "G2_C157::Method6.6240<System.Object>()"
		ldstr "class G3_C1125`1<class BaseClass1> on type class G3_C1125`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1125`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1125`1<class BaseClass1>::Method5()
		calli default string(class G3_C1125`1<class BaseClass1>)
		ldstr "G2_C157::Method5.6238()"
		ldstr "class G3_C1125`1<class BaseClass1> on type class G3_C1125`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1125`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1125`1<class BaseClass1>::Method4()
		calli default string(class G3_C1125`1<class BaseClass1>)
		ldstr "G2_C157::Method4.6236()"
		ldstr "class G3_C1125`1<class BaseClass1> on type class G3_C1125`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1125`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1125`1<class BaseClass1>::ClassMethod1345<object>()
		calli default string(class G3_C1125`1<class BaseClass1>)
		ldstr "G2_C157::ClassMethod1345.MI.6245<System.Object>()"
		ldstr "class G3_C1125`1<class BaseClass1> on type class G3_C1125`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1125`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1125`1<class BaseClass1>::ClassMethod1344<object>()
		calli default string(class G3_C1125`1<class BaseClass1>)
		ldstr "G2_C157::ClassMethod1344.MI.6244<System.Object>()"
		ldstr "class G3_C1125`1<class BaseClass1> on type class G3_C1125`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C157`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C11`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass0,class BaseClass0>::ClassMethod1345<object>()
		calli default string(class G2_C157`1<class BaseClass0>)
		ldstr "G2_C157::ClassMethod1345.MI.6245<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass0> on type class G2_C157`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass0,class BaseClass0>::ClassMethod1344<object>()
		calli default string(class G2_C157`1<class BaseClass0>)
		ldstr "G2_C157::ClassMethod1344.MI.6244<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass0> on type class G2_C157`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass0,class BaseClass0>::Method6<object>()
		calli default string(class G2_C157`1<class BaseClass0>)
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass0> on type class G2_C157`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass0,class BaseClass0>::Method5()
		calli default string(class G2_C157`1<class BaseClass0>)
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass0> on type class G2_C157`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass0,class BaseClass0>::Method4()
		calli default string(class G2_C157`1<class BaseClass0>)
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass0> on type class G2_C157`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C157`1<class BaseClass0>)
		ldstr "G1_C11::Method7.4854<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass0> on type class G2_C157`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G2_C157`1<class BaseClass0>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G2_C157`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G2_C157`1<class BaseClass0>)
		ldstr "G2_C157::Method4.MI.6237()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C157`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G2_C157`1<class BaseClass0>)
		ldstr "G2_C157::Method5.MI.6239()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C157`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G2_C157`1<class BaseClass0>)
		ldstr "G2_C157::Method6.6240<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C157`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C157`1<class BaseClass0>)
		ldstr "G2_C157::Method7.6241<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C157`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C157`1<class BaseClass0>)
		ldstr "G2_C157::Method7.6241<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C157`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C157`1<class BaseClass0>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C157`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C157`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C157`1<class BaseClass0>::ClassMethod1638()
		calli default string(class G2_C157`1<class BaseClass0>)
		ldstr "G2_C157::ClassMethod1638.6243()"
		ldstr "class G2_C157`1<class BaseClass0> on type class G2_C157`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C157`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C157`1<class BaseClass0>::ClassMethod1637()
		calli default string(class G2_C157`1<class BaseClass0>)
		ldstr "G2_C157::ClassMethod1637.6242()"
		ldstr "class G2_C157`1<class BaseClass0> on type class G2_C157`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C157`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C157`1<class BaseClass0>::Method7<object>()
		calli default string(class G2_C157`1<class BaseClass0>)
		ldstr "G2_C157::Method7.6241<System.Object>()"
		ldstr "class G2_C157`1<class BaseClass0> on type class G2_C157`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C157`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C157`1<class BaseClass0>::Method6<object>()
		calli default string(class G2_C157`1<class BaseClass0>)
		ldstr "G2_C157::Method6.6240<System.Object>()"
		ldstr "class G2_C157`1<class BaseClass0> on type class G2_C157`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C157`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C157`1<class BaseClass0>::Method5()
		calli default string(class G2_C157`1<class BaseClass0>)
		ldstr "G2_C157::Method5.6238()"
		ldstr "class G2_C157`1<class BaseClass0> on type class G2_C157`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C157`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C157`1<class BaseClass0>::Method4()
		calli default string(class G2_C157`1<class BaseClass0>)
		ldstr "G2_C157::Method4.6236()"
		ldstr "class G2_C157`1<class BaseClass0> on type class G2_C157`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C157`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C157`1<class BaseClass0>::ClassMethod1345<object>()
		calli default string(class G2_C157`1<class BaseClass0>)
		ldstr "G2_C157::ClassMethod1345.MI.6245<System.Object>()"
		ldstr "class G2_C157`1<class BaseClass0> on type class G2_C157`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C157`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C157`1<class BaseClass0>::ClassMethod1344<object>()
		calli default string(class G2_C157`1<class BaseClass0>)
		ldstr "G2_C157::ClassMethod1344.MI.6244<System.Object>()"
		ldstr "class G2_C157`1<class BaseClass0> on type class G2_C157`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G2_C157`1<class BaseClass0>)
		ldstr "G2_C157::Method4.MI.6237()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C157`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G2_C157`1<class BaseClass0>)
		ldstr "G2_C157::Method5.MI.6239()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C157`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G2_C157`1<class BaseClass0>)
		ldstr "G2_C157::Method6.6240<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C157`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C157`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass1,class BaseClass1>::ClassMethod1345<object>()
		calli default string(class G2_C157`1<class BaseClass1>)
		ldstr "G2_C157::ClassMethod1345.MI.6245<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G2_C157`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass1,class BaseClass1>::ClassMethod1344<object>()
		calli default string(class G2_C157`1<class BaseClass1>)
		ldstr "G2_C157::ClassMethod1344.MI.6244<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G2_C157`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass1,class BaseClass1>::Method6<object>()
		calli default string(class G2_C157`1<class BaseClass1>)
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G2_C157`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass1,class BaseClass1>::Method5()
		calli default string(class G2_C157`1<class BaseClass1>)
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G2_C157`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass1,class BaseClass1>::Method4()
		calli default string(class G2_C157`1<class BaseClass1>)
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G2_C157`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C157`1<class BaseClass1>)
		ldstr "G1_C11::Method7.4854<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G2_C157`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C157`1<class BaseClass1>)
		ldstr "G2_C157::Method7.6241<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C157`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G2_C157`1<class BaseClass1>)
		ldstr "G2_C157::Method4.MI.6237()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C157`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G2_C157`1<class BaseClass1>)
		ldstr "G2_C157::Method5.MI.6239()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C157`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G2_C157`1<class BaseClass1>)
		ldstr "G2_C157::Method6.6240<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C157`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C157`1<class BaseClass1>)
		ldstr "G2_C157::Method7.6241<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C157`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C157`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C157`1<class BaseClass1>::ClassMethod1638()
		calli default string(class G2_C157`1<class BaseClass1>)
		ldstr "G2_C157::ClassMethod1638.6243()"
		ldstr "class G2_C157`1<class BaseClass1> on type class G2_C157`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C157`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C157`1<class BaseClass1>::ClassMethod1637()
		calli default string(class G2_C157`1<class BaseClass1>)
		ldstr "G2_C157::ClassMethod1637.6242()"
		ldstr "class G2_C157`1<class BaseClass1> on type class G2_C157`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C157`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C157`1<class BaseClass1>::Method7<object>()
		calli default string(class G2_C157`1<class BaseClass1>)
		ldstr "G2_C157::Method7.6241<System.Object>()"
		ldstr "class G2_C157`1<class BaseClass1> on type class G2_C157`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C157`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C157`1<class BaseClass1>::Method6<object>()
		calli default string(class G2_C157`1<class BaseClass1>)
		ldstr "G2_C157::Method6.6240<System.Object>()"
		ldstr "class G2_C157`1<class BaseClass1> on type class G2_C157`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C157`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C157`1<class BaseClass1>::Method5()
		calli default string(class G2_C157`1<class BaseClass1>)
		ldstr "G2_C157::Method5.6238()"
		ldstr "class G2_C157`1<class BaseClass1> on type class G2_C157`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C157`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C157`1<class BaseClass1>::Method4()
		calli default string(class G2_C157`1<class BaseClass1>)
		ldstr "G2_C157::Method4.6236()"
		ldstr "class G2_C157`1<class BaseClass1> on type class G2_C157`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C157`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C157`1<class BaseClass1>::ClassMethod1345<object>()
		calli default string(class G2_C157`1<class BaseClass1>)
		ldstr "G2_C157::ClassMethod1345.MI.6245<System.Object>()"
		ldstr "class G2_C157`1<class BaseClass1> on type class G2_C157`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C157`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C157`1<class BaseClass1>::ClassMethod1344<object>()
		calli default string(class G2_C157`1<class BaseClass1>)
		ldstr "G2_C157::ClassMethod1344.MI.6244<System.Object>()"
		ldstr "class G2_C157`1<class BaseClass1> on type class G2_C157`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G2_C157`1<class BaseClass1>)
		ldstr "G2_C157::Method4.MI.6237()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C157`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G2_C157`1<class BaseClass1>)
		ldstr "G2_C157::Method5.MI.6239()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C157`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G2_C157`1<class BaseClass1>)
		ldstr "G2_C157::Method6.6240<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C157`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G1_C11`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C11`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass0,class BaseClass0>::ClassMethod1345<object>()
		calli default string(class G1_C11`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass0> on type class G1_C11`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass0,class BaseClass0>::ClassMethod1344<object>()
		calli default string(class G1_C11`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass0> on type class G1_C11`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass0,class BaseClass0>::Method6<object>()
		calli default string(class G1_C11`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass0> on type class G1_C11`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass0,class BaseClass0>::Method5()
		calli default string(class G1_C11`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass0> on type class G1_C11`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass0,class BaseClass0>::Method4()
		calli default string(class G1_C11`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass0> on type class G1_C11`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G1_C11`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C11::Method7.4854<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass0> on type class G1_C11`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G1_C11`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G1_C11`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G1_C11`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C11::Method4.MI.4857()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C11`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G1_C11`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C11::Method5.4858()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C11`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G1_C11`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C11::Method6.MI.4860<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C11`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G1_C11`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G1_C11`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G1_C11`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G1_C11`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G1_C11`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G1_C11`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G1_C11`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C11`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass0,class BaseClass1>::ClassMethod1345<object>()
		calli default string(class G1_C11`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass1> on type class G1_C11`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass0,class BaseClass1>::ClassMethod1344<object>()
		calli default string(class G1_C11`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass1> on type class G1_C11`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass0,class BaseClass1>::Method6<object>()
		calli default string(class G1_C11`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass1> on type class G1_C11`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass0,class BaseClass1>::Method5()
		calli default string(class G1_C11`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass1> on type class G1_C11`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass0,class BaseClass1>::Method4()
		calli default string(class G1_C11`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass1> on type class G1_C11`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G1_C11`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C11::Method7.4854<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass0,class BaseClass1> on type class G1_C11`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G1_C11`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G1_C11`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G1_C11`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C11::Method4.MI.4857()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C11`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G1_C11`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C11::Method5.4858()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C11`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G1_C11`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C11::Method6.MI.4860<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C11`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G1_C11`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G1_C11`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G1_C11`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C11`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass1,class BaseClass0>::ClassMethod1345<object>()
		calli default string(class G1_C11`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass0> on type class G1_C11`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass1,class BaseClass0>::ClassMethod1344<object>()
		calli default string(class G1_C11`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass0> on type class G1_C11`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass1,class BaseClass0>::Method6<object>()
		calli default string(class G1_C11`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass0> on type class G1_C11`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass1,class BaseClass0>::Method5()
		calli default string(class G1_C11`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass0> on type class G1_C11`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass1,class BaseClass0>::Method4()
		calli default string(class G1_C11`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass0> on type class G1_C11`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G1_C11`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C11::Method7.4854<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass0> on type class G1_C11`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G1_C11`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G1_C11`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G1_C11`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C11::Method4.MI.4857()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C11`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G1_C11`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C11::Method5.4858()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C11`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G1_C11`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C11::Method6.MI.4860<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C11`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G1_C11`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G1_C11`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G1_C11`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G1_C11`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G1_C11`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G1_C11`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G1_C11`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass1,class BaseClass1>::ClassMethod1345<object>()
		calli default string(class G1_C11`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G1_C11`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass1,class BaseClass1>::ClassMethod1344<object>()
		calli default string(class G1_C11`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G1_C11`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass1,class BaseClass1>::Method6<object>()
		calli default string(class G1_C11`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G1_C11`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass1,class BaseClass1>::Method5()
		calli default string(class G1_C11`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G1_C11`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass1,class BaseClass1>::Method4()
		calli default string(class G1_C11`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G1_C11`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C11`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C11`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G1_C11`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C11::Method7.4854<System.Object>()"
		ldstr "class G1_C11`2<class BaseClass1,class BaseClass1> on type class G1_C11`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G1_C11`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G1_C11`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G1_C11`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C11::Method4.MI.4857()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C11`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G1_C11`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C11::Method5.4858()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C11`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G1_C11`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C11::Method6.MI.4860<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C11`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G1_C11`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G1_C11`2<class BaseClass1,class BaseClass1>"
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
		call void Generated653::MethodCallingTest()
		call void Generated653::ConstrainedCallsTest()
		call void Generated653::StructConstrainedInterfaceCallsTest()
		call void Generated653::CalliTest()
		ldc.i4 100
		ret
	}
}
