// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) .ver 4:0:0:0 }
.assembly extern TestFramework { .publickeytoken = ( B0 3F 5F 7F 11 D5 0A 3A )  }

//TYPES IN FORWARDER ASSEMBLIES:

//TEST ASSEMBLY:
.assembly Generated654 { .hash algorithm 0x00008004 }
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
.class public G3_C1126`1<T0> 
		extends class G2_C161`1<!T0>
		implements class IBase2`2<!T0,!T0> 
{
	.method public hidebysig virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G3_C1126::Method7.14470<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase2<T0,T0>.Method7'<M0>() cil managed noinlining { 
		.override method instance string class IBase2`2<!T0,!T0>::Method7<[1]>()
		ldstr "G3_C1126::Method7.MI.14471<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod3650<M0>() cil managed noinlining { 
		ldstr "G3_C1126::ClassMethod3650.14472<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod3651<M0>() cil managed noinlining { 
		ldstr "G3_C1126::ClassMethod3651.14473<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class G2_C161`1<!T0>::.ctor()
		ret
	}
}
.class public G2_C161`1<T0> 
		extends class G1_C12`2<class BaseClass0,class BaseClass1>
		implements class IBase1`1<class BaseClass0>, class IBase2`2<!T0,class BaseClass1> 
{
	.method public hidebysig virtual instance string Method4() cil managed noinlining { 
		ldstr "G2_C161::Method4.6274()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class BaseClass0>.Method4'() cil managed noinlining { 
		.override method instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C161::Method4.MI.6275()"
		ret
	}
	.method public hidebysig virtual instance string Method5() cil managed noinlining { 
		ldstr "G2_C161::Method5.6276()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class BaseClass0>.Method5'() cil managed noinlining { 
		.override method instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C161::Method5.MI.6277()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G2_C161::Method6.6278<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class BaseClass0>.Method6'<M0>() cil managed noinlining { 
		.override method instance string class IBase1`1<class BaseClass0>::Method6<[1]>()
		ldstr "G2_C161::Method6.MI.6279<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G2_C161::Method7.6280<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1645() cil managed noinlining { 
		ldstr "G2_C161::ClassMethod1645.6281()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1646() cil managed noinlining { 
		ldstr "G2_C161::ClassMethod1646.6282()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C12<class BaseClass0,class BaseClass1>.ClassMethod1346'<M0>() cil managed noinlining { 
		.override method instance string class G1_C12`2<class BaseClass0,class BaseClass1>::ClassMethod1346<[1]>()
		ldstr "G2_C161::ClassMethod1346.MI.6283<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C12<class BaseClass0,class BaseClass1>.ClassMethod1347'<M0>() cil managed noinlining { 
		.override method instance string class G1_C12`2<class BaseClass0,class BaseClass1>::ClassMethod1347<[1]>()
		ldstr "G2_C161::ClassMethod1347.MI.6284<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class G1_C12`2<class BaseClass0,class BaseClass1>::.ctor()
		ret
	}
}
.class interface public abstract IBase2`2<+T0, -T1> 
{
	.method public hidebysig newslot abstract virtual instance string Method7<M0>() cil managed { }
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
.class interface public abstract IBase1`1<+T0> 
{
	.method public hidebysig newslot abstract virtual instance string Method4() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method5() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method6<M0>() cil managed { }
}
.class public auto ansi beforefieldinit Generated654 {
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
	.method static void M.G3_C1126.T<T0,(class G3_C1126`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 15
		.locals init (string[] actualResults)
		ldc.i4.s    10
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1126.T<T0,(class G3_C1126`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 10
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1126`1<!!T0>::ClassMethod1346<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1126`1<!!T0>::ClassMethod1347<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1126`1<!!T0>::ClassMethod1645()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1126`1<!!T0>::ClassMethod1646()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1126`1<!!T0>::ClassMethod3650<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1126`1<!!T0>::ClassMethod3651<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1126`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1126`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1126`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1126`1<!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1126.A<(class G3_C1126`1<class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 15
		.locals init (string[] actualResults)
		ldc.i4.s    10
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1126.A<(class G3_C1126`1<class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 10
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1126`1<class BaseClass0>::ClassMethod1346<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1126`1<class BaseClass0>::ClassMethod1347<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1126`1<class BaseClass0>::ClassMethod1645()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1126`1<class BaseClass0>::ClassMethod1646()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1126`1<class BaseClass0>::ClassMethod3650<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1126`1<class BaseClass0>::ClassMethod3651<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1126`1<class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1126`1<class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1126`1<class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1126`1<class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1126.B<(class G3_C1126`1<class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 15
		.locals init (string[] actualResults)
		ldc.i4.s    10
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1126.B<(class G3_C1126`1<class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 10
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1126`1<class BaseClass1>::ClassMethod1346<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1126`1<class BaseClass1>::ClassMethod1347<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1126`1<class BaseClass1>::ClassMethod1645()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1126`1<class BaseClass1>::ClassMethod1646()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1126`1<class BaseClass1>::ClassMethod3650<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1126`1<class BaseClass1>::ClassMethod3651<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1126`1<class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1126`1<class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1126`1<class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1126`1<class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C161.T<T0,(class G2_C161`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C161.T<T0,(class G2_C161`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C161`1<!!T0>::ClassMethod1346<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C161`1<!!T0>::ClassMethod1347<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C161`1<!!T0>::ClassMethod1645()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C161`1<!!T0>::ClassMethod1646()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C161`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C161`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C161`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C161`1<!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C161.A<(class G2_C161`1<class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C161.A<(class G2_C161`1<class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C161`1<class BaseClass0>::ClassMethod1346<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C161`1<class BaseClass0>::ClassMethod1347<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C161`1<class BaseClass0>::ClassMethod1645()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C161`1<class BaseClass0>::ClassMethod1646()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C161`1<class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C161`1<class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C161`1<class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C161`1<class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C161.B<(class G2_C161`1<class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C161.B<(class G2_C161`1<class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C161`1<class BaseClass1>::ClassMethod1346<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C161`1<class BaseClass1>::ClassMethod1347<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C161`1<class BaseClass1>::ClassMethod1645()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C161`1<class BaseClass1>::ClassMethod1646()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C161`1<class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C161`1<class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C161`1<class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C161`1<class BaseClass1>::Method7<object>()
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
		newobj instance void class G3_C1126`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C12`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass0,class BaseClass1>::ClassMethod1347<object>()
		ldstr "G2_C161::ClassMethod1347.MI.6284<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass1> on type class G3_C1126`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass0,class BaseClass1>::ClassMethod1346<object>()
		ldstr "G2_C161::ClassMethod1346.MI.6283<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass1> on type class G3_C1126`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass0,class BaseClass1>::Method6<object>()
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass1> on type class G3_C1126`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass0,class BaseClass1>::Method5()
		ldstr "G2_C161::Method5.6276()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass1> on type class G3_C1126`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass0,class BaseClass1>::Method4()
		ldstr "G2_C161::Method4.6274()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass1> on type class G3_C1126`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass1> on type class G3_C1126`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G3_C1126::Method7.MI.14471<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1126`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C161::Method4.MI.6275()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1126`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C161::Method5.MI.6277()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1126`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C161::Method6.MI.6279<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1126`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C161`1<class BaseClass0>
		callvirt instance string class G2_C161`1<class BaseClass0>::ClassMethod1646()
		ldstr "G2_C161::ClassMethod1646.6282()"
		ldstr "class G2_C161`1<class BaseClass0> on type class G3_C1126`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C161`1<class BaseClass0>
		callvirt instance string class G2_C161`1<class BaseClass0>::ClassMethod1645()
		ldstr "G2_C161::ClassMethod1645.6281()"
		ldstr "class G2_C161`1<class BaseClass0> on type class G3_C1126`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C161`1<class BaseClass0>
		callvirt instance string class G2_C161`1<class BaseClass0>::Method7<object>()
		ldstr "G3_C1126::Method7.14470<System.Object>()"
		ldstr "class G2_C161`1<class BaseClass0> on type class G3_C1126`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C161`1<class BaseClass0>
		callvirt instance string class G2_C161`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C161::Method6.6278<System.Object>()"
		ldstr "class G2_C161`1<class BaseClass0> on type class G3_C1126`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C161`1<class BaseClass0>
		callvirt instance string class G2_C161`1<class BaseClass0>::Method5()
		ldstr "G2_C161::Method5.6276()"
		ldstr "class G2_C161`1<class BaseClass0> on type class G3_C1126`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C161`1<class BaseClass0>
		callvirt instance string class G2_C161`1<class BaseClass0>::Method4()
		ldstr "G2_C161::Method4.6274()"
		ldstr "class G2_C161`1<class BaseClass0> on type class G3_C1126`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C161`1<class BaseClass0>
		callvirt instance string class G2_C161`1<class BaseClass0>::ClassMethod1347<object>()
		ldstr "G2_C161::ClassMethod1347.MI.6284<System.Object>()"
		ldstr "class G2_C161`1<class BaseClass0> on type class G3_C1126`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C161`1<class BaseClass0>
		callvirt instance string class G2_C161`1<class BaseClass0>::ClassMethod1346<object>()
		ldstr "G2_C161::ClassMethod1346.MI.6283<System.Object>()"
		ldstr "class G2_C161`1<class BaseClass0> on type class G3_C1126`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G3_C1126`1<class BaseClass0>
		callvirt instance string class G3_C1126`1<class BaseClass0>::ClassMethod3651<object>()
		ldstr "G3_C1126::ClassMethod3651.14473<System.Object>()"
		ldstr "class G3_C1126`1<class BaseClass0> on type class G3_C1126`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1126`1<class BaseClass0>
		callvirt instance string class G3_C1126`1<class BaseClass0>::ClassMethod3650<object>()
		ldstr "G3_C1126::ClassMethod3650.14472<System.Object>()"
		ldstr "class G3_C1126`1<class BaseClass0> on type class G3_C1126`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1126`1<class BaseClass0>
		callvirt instance string class G3_C1126`1<class BaseClass0>::Method7<object>()
		ldstr "G3_C1126::Method7.14470<System.Object>()"
		ldstr "class G3_C1126`1<class BaseClass0> on type class G3_C1126`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1126`1<class BaseClass0>
		callvirt instance string class G3_C1126`1<class BaseClass0>::ClassMethod1646()
		ldstr "G2_C161::ClassMethod1646.6282()"
		ldstr "class G3_C1126`1<class BaseClass0> on type class G3_C1126`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1126`1<class BaseClass0>
		callvirt instance string class G3_C1126`1<class BaseClass0>::ClassMethod1645()
		ldstr "G2_C161::ClassMethod1645.6281()"
		ldstr "class G3_C1126`1<class BaseClass0> on type class G3_C1126`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1126`1<class BaseClass0>
		callvirt instance string class G3_C1126`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C161::Method6.6278<System.Object>()"
		ldstr "class G3_C1126`1<class BaseClass0> on type class G3_C1126`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1126`1<class BaseClass0>
		callvirt instance string class G3_C1126`1<class BaseClass0>::Method5()
		ldstr "G2_C161::Method5.6276()"
		ldstr "class G3_C1126`1<class BaseClass0> on type class G3_C1126`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1126`1<class BaseClass0>
		callvirt instance string class G3_C1126`1<class BaseClass0>::Method4()
		ldstr "G2_C161::Method4.6274()"
		ldstr "class G3_C1126`1<class BaseClass0> on type class G3_C1126`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1126`1<class BaseClass0>
		callvirt instance string class G3_C1126`1<class BaseClass0>::ClassMethod1347<object>()
		ldstr "G2_C161::ClassMethod1347.MI.6284<System.Object>()"
		ldstr "class G3_C1126`1<class BaseClass0> on type class G3_C1126`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1126`1<class BaseClass0>
		callvirt instance string class G3_C1126`1<class BaseClass0>::ClassMethod1346<object>()
		ldstr "G2_C161::ClassMethod1346.MI.6283<System.Object>()"
		ldstr "class G3_C1126`1<class BaseClass0> on type class G3_C1126`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G3_C1126::Method7.MI.14471<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1126`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G3_C1126`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C12`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass0,class BaseClass1>::ClassMethod1347<object>()
		ldstr "G2_C161::ClassMethod1347.MI.6284<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass1> on type class G3_C1126`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass0,class BaseClass1>::ClassMethod1346<object>()
		ldstr "G2_C161::ClassMethod1346.MI.6283<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass1> on type class G3_C1126`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass0,class BaseClass1>::Method6<object>()
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass1> on type class G3_C1126`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass0,class BaseClass1>::Method5()
		ldstr "G2_C161::Method5.6276()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass1> on type class G3_C1126`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass0,class BaseClass1>::Method4()
		ldstr "G2_C161::Method4.6274()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass1> on type class G3_C1126`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass1> on type class G3_C1126`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G3_C1126::Method7.MI.14471<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1126`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C161::Method4.MI.6275()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1126`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C161::Method5.MI.6277()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1126`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C161::Method6.MI.6279<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1126`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C161`1<class BaseClass1>
		callvirt instance string class G2_C161`1<class BaseClass1>::ClassMethod1646()
		ldstr "G2_C161::ClassMethod1646.6282()"
		ldstr "class G2_C161`1<class BaseClass1> on type class G3_C1126`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C161`1<class BaseClass1>
		callvirt instance string class G2_C161`1<class BaseClass1>::ClassMethod1645()
		ldstr "G2_C161::ClassMethod1645.6281()"
		ldstr "class G2_C161`1<class BaseClass1> on type class G3_C1126`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C161`1<class BaseClass1>
		callvirt instance string class G2_C161`1<class BaseClass1>::Method7<object>()
		ldstr "G3_C1126::Method7.14470<System.Object>()"
		ldstr "class G2_C161`1<class BaseClass1> on type class G3_C1126`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C161`1<class BaseClass1>
		callvirt instance string class G2_C161`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C161::Method6.6278<System.Object>()"
		ldstr "class G2_C161`1<class BaseClass1> on type class G3_C1126`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C161`1<class BaseClass1>
		callvirt instance string class G2_C161`1<class BaseClass1>::Method5()
		ldstr "G2_C161::Method5.6276()"
		ldstr "class G2_C161`1<class BaseClass1> on type class G3_C1126`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C161`1<class BaseClass1>
		callvirt instance string class G2_C161`1<class BaseClass1>::Method4()
		ldstr "G2_C161::Method4.6274()"
		ldstr "class G2_C161`1<class BaseClass1> on type class G3_C1126`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C161`1<class BaseClass1>
		callvirt instance string class G2_C161`1<class BaseClass1>::ClassMethod1347<object>()
		ldstr "G2_C161::ClassMethod1347.MI.6284<System.Object>()"
		ldstr "class G2_C161`1<class BaseClass1> on type class G3_C1126`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C161`1<class BaseClass1>
		callvirt instance string class G2_C161`1<class BaseClass1>::ClassMethod1346<object>()
		ldstr "G2_C161::ClassMethod1346.MI.6283<System.Object>()"
		ldstr "class G2_C161`1<class BaseClass1> on type class G3_C1126`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G3_C1126::Method7.MI.14471<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1126`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G3_C1126`1<class BaseClass1>
		callvirt instance string class G3_C1126`1<class BaseClass1>::ClassMethod3651<object>()
		ldstr "G3_C1126::ClassMethod3651.14473<System.Object>()"
		ldstr "class G3_C1126`1<class BaseClass1> on type class G3_C1126`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1126`1<class BaseClass1>
		callvirt instance string class G3_C1126`1<class BaseClass1>::ClassMethod3650<object>()
		ldstr "G3_C1126::ClassMethod3650.14472<System.Object>()"
		ldstr "class G3_C1126`1<class BaseClass1> on type class G3_C1126`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1126`1<class BaseClass1>
		callvirt instance string class G3_C1126`1<class BaseClass1>::Method7<object>()
		ldstr "G3_C1126::Method7.14470<System.Object>()"
		ldstr "class G3_C1126`1<class BaseClass1> on type class G3_C1126`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1126`1<class BaseClass1>
		callvirt instance string class G3_C1126`1<class BaseClass1>::ClassMethod1646()
		ldstr "G2_C161::ClassMethod1646.6282()"
		ldstr "class G3_C1126`1<class BaseClass1> on type class G3_C1126`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1126`1<class BaseClass1>
		callvirt instance string class G3_C1126`1<class BaseClass1>::ClassMethod1645()
		ldstr "G2_C161::ClassMethod1645.6281()"
		ldstr "class G3_C1126`1<class BaseClass1> on type class G3_C1126`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1126`1<class BaseClass1>
		callvirt instance string class G3_C1126`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C161::Method6.6278<System.Object>()"
		ldstr "class G3_C1126`1<class BaseClass1> on type class G3_C1126`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1126`1<class BaseClass1>
		callvirt instance string class G3_C1126`1<class BaseClass1>::Method5()
		ldstr "G2_C161::Method5.6276()"
		ldstr "class G3_C1126`1<class BaseClass1> on type class G3_C1126`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1126`1<class BaseClass1>
		callvirt instance string class G3_C1126`1<class BaseClass1>::Method4()
		ldstr "G2_C161::Method4.6274()"
		ldstr "class G3_C1126`1<class BaseClass1> on type class G3_C1126`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1126`1<class BaseClass1>
		callvirt instance string class G3_C1126`1<class BaseClass1>::ClassMethod1347<object>()
		ldstr "G2_C161::ClassMethod1347.MI.6284<System.Object>()"
		ldstr "class G3_C1126`1<class BaseClass1> on type class G3_C1126`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1126`1<class BaseClass1>
		callvirt instance string class G3_C1126`1<class BaseClass1>::ClassMethod1346<object>()
		ldstr "G2_C161::ClassMethod1346.MI.6283<System.Object>()"
		ldstr "class G3_C1126`1<class BaseClass1> on type class G3_C1126`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C161`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C12`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass0,class BaseClass1>::ClassMethod1347<object>()
		ldstr "G2_C161::ClassMethod1347.MI.6284<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass1> on type class G2_C161`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass0,class BaseClass1>::ClassMethod1346<object>()
		ldstr "G2_C161::ClassMethod1346.MI.6283<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass1> on type class G2_C161`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass0,class BaseClass1>::Method6<object>()
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass1> on type class G2_C161`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass0,class BaseClass1>::Method5()
		ldstr "G2_C161::Method5.6276()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass1> on type class G2_C161`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass0,class BaseClass1>::Method4()
		ldstr "G2_C161::Method4.6274()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass1> on type class G2_C161`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass1> on type class G2_C161`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C161::Method7.6280<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C161`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C161::Method4.MI.6275()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C161`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C161::Method5.MI.6277()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C161`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C161::Method6.MI.6279<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C161`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C161`1<class BaseClass0>
		callvirt instance string class G2_C161`1<class BaseClass0>::ClassMethod1646()
		ldstr "G2_C161::ClassMethod1646.6282()"
		ldstr "class G2_C161`1<class BaseClass0> on type class G2_C161`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C161`1<class BaseClass0>
		callvirt instance string class G2_C161`1<class BaseClass0>::ClassMethod1645()
		ldstr "G2_C161::ClassMethod1645.6281()"
		ldstr "class G2_C161`1<class BaseClass0> on type class G2_C161`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C161`1<class BaseClass0>
		callvirt instance string class G2_C161`1<class BaseClass0>::Method7<object>()
		ldstr "G2_C161::Method7.6280<System.Object>()"
		ldstr "class G2_C161`1<class BaseClass0> on type class G2_C161`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C161`1<class BaseClass0>
		callvirt instance string class G2_C161`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C161::Method6.6278<System.Object>()"
		ldstr "class G2_C161`1<class BaseClass0> on type class G2_C161`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C161`1<class BaseClass0>
		callvirt instance string class G2_C161`1<class BaseClass0>::Method5()
		ldstr "G2_C161::Method5.6276()"
		ldstr "class G2_C161`1<class BaseClass0> on type class G2_C161`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C161`1<class BaseClass0>
		callvirt instance string class G2_C161`1<class BaseClass0>::Method4()
		ldstr "G2_C161::Method4.6274()"
		ldstr "class G2_C161`1<class BaseClass0> on type class G2_C161`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C161`1<class BaseClass0>
		callvirt instance string class G2_C161`1<class BaseClass0>::ClassMethod1347<object>()
		ldstr "G2_C161::ClassMethod1347.MI.6284<System.Object>()"
		ldstr "class G2_C161`1<class BaseClass0> on type class G2_C161`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C161`1<class BaseClass0>
		callvirt instance string class G2_C161`1<class BaseClass0>::ClassMethod1346<object>()
		ldstr "G2_C161::ClassMethod1346.MI.6283<System.Object>()"
		ldstr "class G2_C161`1<class BaseClass0> on type class G2_C161`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C161`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C12`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass0,class BaseClass1>::ClassMethod1347<object>()
		ldstr "G2_C161::ClassMethod1347.MI.6284<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass1> on type class G2_C161`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass0,class BaseClass1>::ClassMethod1346<object>()
		ldstr "G2_C161::ClassMethod1346.MI.6283<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass1> on type class G2_C161`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass0,class BaseClass1>::Method6<object>()
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass1> on type class G2_C161`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass0,class BaseClass1>::Method5()
		ldstr "G2_C161::Method5.6276()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass1> on type class G2_C161`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass0,class BaseClass1>::Method4()
		ldstr "G2_C161::Method4.6274()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass1> on type class G2_C161`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C12`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C12`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass1> on type class G2_C161`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C161::Method7.6280<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C161`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C161::Method4.MI.6275()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C161`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C161::Method5.MI.6277()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C161`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C161::Method6.MI.6279<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C161`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C161`1<class BaseClass1>
		callvirt instance string class G2_C161`1<class BaseClass1>::ClassMethod1646()
		ldstr "G2_C161::ClassMethod1646.6282()"
		ldstr "class G2_C161`1<class BaseClass1> on type class G2_C161`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C161`1<class BaseClass1>
		callvirt instance string class G2_C161`1<class BaseClass1>::ClassMethod1645()
		ldstr "G2_C161::ClassMethod1645.6281()"
		ldstr "class G2_C161`1<class BaseClass1> on type class G2_C161`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C161`1<class BaseClass1>
		callvirt instance string class G2_C161`1<class BaseClass1>::Method7<object>()
		ldstr "G2_C161::Method7.6280<System.Object>()"
		ldstr "class G2_C161`1<class BaseClass1> on type class G2_C161`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C161`1<class BaseClass1>
		callvirt instance string class G2_C161`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C161::Method6.6278<System.Object>()"
		ldstr "class G2_C161`1<class BaseClass1> on type class G2_C161`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C161`1<class BaseClass1>
		callvirt instance string class G2_C161`1<class BaseClass1>::Method5()
		ldstr "G2_C161::Method5.6276()"
		ldstr "class G2_C161`1<class BaseClass1> on type class G2_C161`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C161`1<class BaseClass1>
		callvirt instance string class G2_C161`1<class BaseClass1>::Method4()
		ldstr "G2_C161::Method4.6274()"
		ldstr "class G2_C161`1<class BaseClass1> on type class G2_C161`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C161`1<class BaseClass1>
		callvirt instance string class G2_C161`1<class BaseClass1>::ClassMethod1347<object>()
		ldstr "G2_C161::ClassMethod1347.MI.6284<System.Object>()"
		ldstr "class G2_C161`1<class BaseClass1> on type class G2_C161`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C161`1<class BaseClass1>
		callvirt instance string class G2_C161`1<class BaseClass1>::ClassMethod1346<object>()
		ldstr "G2_C161::ClassMethod1346.MI.6283<System.Object>()"
		ldstr "class G2_C161`1<class BaseClass1> on type class G2_C161`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C161::Method7.6280<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C161`1<class BaseClass1>"
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
		newobj instance void class G3_C1126`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C161::ClassMethod1346.MI.6283<System.Object>()#G2_C161::ClassMethod1347.MI.6284<System.Object>()#G2_C161::Method4.6274()#G2_C161::Method5.6276()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated654::M.G1_C12.T.T<class BaseClass0,class BaseClass1,class G3_C1126`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C161::ClassMethod1346.MI.6283<System.Object>()#G2_C161::ClassMethod1347.MI.6284<System.Object>()#G2_C161::Method4.6274()#G2_C161::Method5.6276()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated654::M.G1_C12.A.T<class BaseClass1,class G3_C1126`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C161::ClassMethod1346.MI.6283<System.Object>()#G2_C161::ClassMethod1347.MI.6284<System.Object>()#G2_C161::Method4.6274()#G2_C161::Method5.6276()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated654::M.G1_C12.A.B<class G3_C1126`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1126::Method7.MI.14471<System.Object>()#"
		call void Generated654::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G3_C1126`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1126::Method7.MI.14471<System.Object>()#"
		call void Generated654::M.IBase2.A.T<class BaseClass1,class G3_C1126`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1126::Method7.MI.14471<System.Object>()#"
		call void Generated654::M.IBase2.A.B<class G3_C1126`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C161::Method4.MI.6275()#G2_C161::Method5.MI.6277()#G2_C161::Method6.MI.6279<System.Object>()#"
		call void Generated654::M.IBase1.T<class BaseClass0,class G3_C1126`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C161::Method4.MI.6275()#G2_C161::Method5.MI.6277()#G2_C161::Method6.MI.6279<System.Object>()#"
		call void Generated654::M.IBase1.A<class G3_C1126`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C161::ClassMethod1346.MI.6283<System.Object>()#G2_C161::ClassMethod1347.MI.6284<System.Object>()#G2_C161::ClassMethod1645.6281()#G2_C161::ClassMethod1646.6282()#G2_C161::Method4.6274()#G2_C161::Method5.6276()#G2_C161::Method6.6278<System.Object>()#G3_C1126::Method7.14470<System.Object>()#"
		call void Generated654::M.G2_C161.T<class BaseClass0,class G3_C1126`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C161::ClassMethod1346.MI.6283<System.Object>()#G2_C161::ClassMethod1347.MI.6284<System.Object>()#G2_C161::ClassMethod1645.6281()#G2_C161::ClassMethod1646.6282()#G2_C161::Method4.6274()#G2_C161::Method5.6276()#G2_C161::Method6.6278<System.Object>()#G3_C1126::Method7.14470<System.Object>()#"
		call void Generated654::M.G2_C161.A<class G3_C1126`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C161::ClassMethod1346.MI.6283<System.Object>()#G2_C161::ClassMethod1347.MI.6284<System.Object>()#G2_C161::ClassMethod1645.6281()#G2_C161::ClassMethod1646.6282()#G3_C1126::ClassMethod3650.14472<System.Object>()#G3_C1126::ClassMethod3651.14473<System.Object>()#G2_C161::Method4.6274()#G2_C161::Method5.6276()#G2_C161::Method6.6278<System.Object>()#G3_C1126::Method7.14470<System.Object>()#"
		call void Generated654::M.G3_C1126.T<class BaseClass0,class G3_C1126`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C161::ClassMethod1346.MI.6283<System.Object>()#G2_C161::ClassMethod1347.MI.6284<System.Object>()#G2_C161::ClassMethod1645.6281()#G2_C161::ClassMethod1646.6282()#G3_C1126::ClassMethod3650.14472<System.Object>()#G3_C1126::ClassMethod3651.14473<System.Object>()#G2_C161::Method4.6274()#G2_C161::Method5.6276()#G2_C161::Method6.6278<System.Object>()#G3_C1126::Method7.14470<System.Object>()#"
		call void Generated654::M.G3_C1126.A<class G3_C1126`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1126::Method7.MI.14471<System.Object>()#"
		call void Generated654::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G3_C1126`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1126::Method7.MI.14471<System.Object>()#"
		call void Generated654::M.IBase2.A.T<class BaseClass0,class G3_C1126`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1126::Method7.MI.14471<System.Object>()#"
		call void Generated654::M.IBase2.A.A<class G3_C1126`1<class BaseClass0>>(!!0,string)
		newobj instance void class G3_C1126`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C161::ClassMethod1346.MI.6283<System.Object>()#G2_C161::ClassMethod1347.MI.6284<System.Object>()#G2_C161::Method4.6274()#G2_C161::Method5.6276()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated654::M.G1_C12.T.T<class BaseClass0,class BaseClass1,class G3_C1126`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C161::ClassMethod1346.MI.6283<System.Object>()#G2_C161::ClassMethod1347.MI.6284<System.Object>()#G2_C161::Method4.6274()#G2_C161::Method5.6276()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated654::M.G1_C12.A.T<class BaseClass1,class G3_C1126`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C161::ClassMethod1346.MI.6283<System.Object>()#G2_C161::ClassMethod1347.MI.6284<System.Object>()#G2_C161::Method4.6274()#G2_C161::Method5.6276()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated654::M.G1_C12.A.B<class G3_C1126`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1126::Method7.MI.14471<System.Object>()#"
		call void Generated654::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G3_C1126`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1126::Method7.MI.14471<System.Object>()#"
		call void Generated654::M.IBase2.A.T<class BaseClass1,class G3_C1126`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1126::Method7.MI.14471<System.Object>()#"
		call void Generated654::M.IBase2.A.B<class G3_C1126`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C161::Method4.MI.6275()#G2_C161::Method5.MI.6277()#G2_C161::Method6.MI.6279<System.Object>()#"
		call void Generated654::M.IBase1.T<class BaseClass0,class G3_C1126`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C161::Method4.MI.6275()#G2_C161::Method5.MI.6277()#G2_C161::Method6.MI.6279<System.Object>()#"
		call void Generated654::M.IBase1.A<class G3_C1126`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C161::ClassMethod1346.MI.6283<System.Object>()#G2_C161::ClassMethod1347.MI.6284<System.Object>()#G2_C161::ClassMethod1645.6281()#G2_C161::ClassMethod1646.6282()#G2_C161::Method4.6274()#G2_C161::Method5.6276()#G2_C161::Method6.6278<System.Object>()#G3_C1126::Method7.14470<System.Object>()#"
		call void Generated654::M.G2_C161.T<class BaseClass1,class G3_C1126`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C161::ClassMethod1346.MI.6283<System.Object>()#G2_C161::ClassMethod1347.MI.6284<System.Object>()#G2_C161::ClassMethod1645.6281()#G2_C161::ClassMethod1646.6282()#G2_C161::Method4.6274()#G2_C161::Method5.6276()#G2_C161::Method6.6278<System.Object>()#G3_C1126::Method7.14470<System.Object>()#"
		call void Generated654::M.G2_C161.B<class G3_C1126`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1126::Method7.MI.14471<System.Object>()#"
		call void Generated654::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G3_C1126`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1126::Method7.MI.14471<System.Object>()#"
		call void Generated654::M.IBase2.B.T<class BaseClass1,class G3_C1126`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1126::Method7.MI.14471<System.Object>()#"
		call void Generated654::M.IBase2.B.B<class G3_C1126`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C161::ClassMethod1346.MI.6283<System.Object>()#G2_C161::ClassMethod1347.MI.6284<System.Object>()#G2_C161::ClassMethod1645.6281()#G2_C161::ClassMethod1646.6282()#G3_C1126::ClassMethod3650.14472<System.Object>()#G3_C1126::ClassMethod3651.14473<System.Object>()#G2_C161::Method4.6274()#G2_C161::Method5.6276()#G2_C161::Method6.6278<System.Object>()#G3_C1126::Method7.14470<System.Object>()#"
		call void Generated654::M.G3_C1126.T<class BaseClass1,class G3_C1126`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C161::ClassMethod1346.MI.6283<System.Object>()#G2_C161::ClassMethod1347.MI.6284<System.Object>()#G2_C161::ClassMethod1645.6281()#G2_C161::ClassMethod1646.6282()#G3_C1126::ClassMethod3650.14472<System.Object>()#G3_C1126::ClassMethod3651.14473<System.Object>()#G2_C161::Method4.6274()#G2_C161::Method5.6276()#G2_C161::Method6.6278<System.Object>()#G3_C1126::Method7.14470<System.Object>()#"
		call void Generated654::M.G3_C1126.B<class G3_C1126`1<class BaseClass1>>(!!0,string)
		newobj instance void class G2_C161`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C161::ClassMethod1346.MI.6283<System.Object>()#G2_C161::ClassMethod1347.MI.6284<System.Object>()#G2_C161::Method4.6274()#G2_C161::Method5.6276()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated654::M.G1_C12.T.T<class BaseClass0,class BaseClass1,class G2_C161`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C161::ClassMethod1346.MI.6283<System.Object>()#G2_C161::ClassMethod1347.MI.6284<System.Object>()#G2_C161::Method4.6274()#G2_C161::Method5.6276()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated654::M.G1_C12.A.T<class BaseClass1,class G2_C161`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C161::ClassMethod1346.MI.6283<System.Object>()#G2_C161::ClassMethod1347.MI.6284<System.Object>()#G2_C161::Method4.6274()#G2_C161::Method5.6276()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated654::M.G1_C12.A.B<class G2_C161`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C161::Method7.6280<System.Object>()#"
		call void Generated654::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C161`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C161::Method7.6280<System.Object>()#"
		call void Generated654::M.IBase2.A.T<class BaseClass1,class G2_C161`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C161::Method7.6280<System.Object>()#"
		call void Generated654::M.IBase2.A.B<class G2_C161`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C161::Method4.MI.6275()#G2_C161::Method5.MI.6277()#G2_C161::Method6.MI.6279<System.Object>()#"
		call void Generated654::M.IBase1.T<class BaseClass0,class G2_C161`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C161::Method4.MI.6275()#G2_C161::Method5.MI.6277()#G2_C161::Method6.MI.6279<System.Object>()#"
		call void Generated654::M.IBase1.A<class G2_C161`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C161::ClassMethod1346.MI.6283<System.Object>()#G2_C161::ClassMethod1347.MI.6284<System.Object>()#G2_C161::ClassMethod1645.6281()#G2_C161::ClassMethod1646.6282()#G2_C161::Method4.6274()#G2_C161::Method5.6276()#G2_C161::Method6.6278<System.Object>()#G2_C161::Method7.6280<System.Object>()#"
		call void Generated654::M.G2_C161.T<class BaseClass0,class G2_C161`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C161::ClassMethod1346.MI.6283<System.Object>()#G2_C161::ClassMethod1347.MI.6284<System.Object>()#G2_C161::ClassMethod1645.6281()#G2_C161::ClassMethod1646.6282()#G2_C161::Method4.6274()#G2_C161::Method5.6276()#G2_C161::Method6.6278<System.Object>()#G2_C161::Method7.6280<System.Object>()#"
		call void Generated654::M.G2_C161.A<class G2_C161`1<class BaseClass0>>(!!0,string)
		newobj instance void class G2_C161`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C161::ClassMethod1346.MI.6283<System.Object>()#G2_C161::ClassMethod1347.MI.6284<System.Object>()#G2_C161::Method4.6274()#G2_C161::Method5.6276()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated654::M.G1_C12.T.T<class BaseClass0,class BaseClass1,class G2_C161`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C161::ClassMethod1346.MI.6283<System.Object>()#G2_C161::ClassMethod1347.MI.6284<System.Object>()#G2_C161::Method4.6274()#G2_C161::Method5.6276()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated654::M.G1_C12.A.T<class BaseClass1,class G2_C161`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C161::ClassMethod1346.MI.6283<System.Object>()#G2_C161::ClassMethod1347.MI.6284<System.Object>()#G2_C161::Method4.6274()#G2_C161::Method5.6276()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated654::M.G1_C12.A.B<class G2_C161`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C161::Method7.6280<System.Object>()#"
		call void Generated654::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C161`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C161::Method7.6280<System.Object>()#"
		call void Generated654::M.IBase2.A.T<class BaseClass1,class G2_C161`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C161::Method7.6280<System.Object>()#"
		call void Generated654::M.IBase2.A.B<class G2_C161`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C161::Method4.MI.6275()#G2_C161::Method5.MI.6277()#G2_C161::Method6.MI.6279<System.Object>()#"
		call void Generated654::M.IBase1.T<class BaseClass0,class G2_C161`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C161::Method4.MI.6275()#G2_C161::Method5.MI.6277()#G2_C161::Method6.MI.6279<System.Object>()#"
		call void Generated654::M.IBase1.A<class G2_C161`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C161::ClassMethod1346.MI.6283<System.Object>()#G2_C161::ClassMethod1347.MI.6284<System.Object>()#G2_C161::ClassMethod1645.6281()#G2_C161::ClassMethod1646.6282()#G2_C161::Method4.6274()#G2_C161::Method5.6276()#G2_C161::Method6.6278<System.Object>()#G2_C161::Method7.6280<System.Object>()#"
		call void Generated654::M.G2_C161.T<class BaseClass1,class G2_C161`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C161::ClassMethod1346.MI.6283<System.Object>()#G2_C161::ClassMethod1347.MI.6284<System.Object>()#G2_C161::ClassMethod1645.6281()#G2_C161::ClassMethod1646.6282()#G2_C161::Method4.6274()#G2_C161::Method5.6276()#G2_C161::Method6.6278<System.Object>()#G2_C161::Method7.6280<System.Object>()#"
		call void Generated654::M.G2_C161.B<class G2_C161`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C161::Method7.6280<System.Object>()#"
		call void Generated654::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G2_C161`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C161::Method7.6280<System.Object>()#"
		call void Generated654::M.IBase2.B.T<class BaseClass1,class G2_C161`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C161::Method7.6280<System.Object>()#"
		call void Generated654::M.IBase2.B.B<class G2_C161`1<class BaseClass1>>(!!0,string)
		newobj instance void class G1_C12`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G1_C12::ClassMethod1347.4870<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated654::M.G1_C12.T.T<class BaseClass0,class BaseClass0,class G1_C12`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G1_C12::ClassMethod1347.4870<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated654::M.G1_C12.A.T<class BaseClass0,class G1_C12`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G1_C12::ClassMethod1347.4870<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated654::M.G1_C12.A.A<class G1_C12`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated654::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G1_C12`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated654::M.IBase2.A.T<class BaseClass0,class G1_C12`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated654::M.IBase2.A.A<class G1_C12`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.MI.4868<System.Object>()#"
		call void Generated654::M.IBase1.T<class BaseClass0,class G1_C12`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.MI.4868<System.Object>()#"
		call void Generated654::M.IBase1.A<class G1_C12`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated654::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G1_C12`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated654::M.IBase2.A.T<class BaseClass1,class G1_C12`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated654::M.IBase2.A.B<class G1_C12`2<class BaseClass0,class BaseClass0>>(!!0,string)
		newobj instance void class G1_C12`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G1_C12::ClassMethod1347.4870<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated654::M.G1_C12.T.T<class BaseClass0,class BaseClass1,class G1_C12`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G1_C12::ClassMethod1347.4870<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated654::M.G1_C12.A.T<class BaseClass1,class G1_C12`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G1_C12::ClassMethod1347.4870<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated654::M.G1_C12.A.B<class G1_C12`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated654::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G1_C12`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated654::M.IBase2.A.T<class BaseClass1,class G1_C12`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated654::M.IBase2.A.B<class G1_C12`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.MI.4868<System.Object>()#"
		call void Generated654::M.IBase1.T<class BaseClass0,class G1_C12`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.MI.4868<System.Object>()#"
		call void Generated654::M.IBase1.A<class G1_C12`2<class BaseClass0,class BaseClass1>>(!!0,string)
		newobj instance void class G1_C12`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G1_C12::ClassMethod1347.4870<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated654::M.G1_C12.T.T<class BaseClass1,class BaseClass0,class G1_C12`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G1_C12::ClassMethod1347.4870<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated654::M.G1_C12.B.T<class BaseClass0,class G1_C12`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G1_C12::ClassMethod1347.4870<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated654::M.G1_C12.B.A<class G1_C12`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated654::M.IBase2.T.T<class BaseClass1,class BaseClass0,class G1_C12`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated654::M.IBase2.B.T<class BaseClass0,class G1_C12`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated654::M.IBase2.B.A<class G1_C12`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.MI.4868<System.Object>()#"
		call void Generated654::M.IBase1.T<class BaseClass0,class G1_C12`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.MI.4868<System.Object>()#"
		call void Generated654::M.IBase1.A<class G1_C12`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated654::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G1_C12`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated654::M.IBase2.A.T<class BaseClass0,class G1_C12`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated654::M.IBase2.A.A<class G1_C12`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated654::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G1_C12`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated654::M.IBase2.A.T<class BaseClass1,class G1_C12`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated654::M.IBase2.A.B<class G1_C12`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated654::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G1_C12`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated654::M.IBase2.B.T<class BaseClass1,class G1_C12`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated654::M.IBase2.B.B<class G1_C12`2<class BaseClass1,class BaseClass0>>(!!0,string)
		newobj instance void class G1_C12`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G1_C12::ClassMethod1347.4870<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated654::M.G1_C12.T.T<class BaseClass1,class BaseClass1,class G1_C12`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G1_C12::ClassMethod1347.4870<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated654::M.G1_C12.B.T<class BaseClass1,class G1_C12`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::ClassMethod1346.4869<System.Object>()#G1_C12::ClassMethod1347.4870<System.Object>()#G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.4867<System.Object>()#G1_C12::Method7.4863<System.Object>()#"
		call void Generated654::M.G1_C12.B.B<class G1_C12`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated654::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G1_C12`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated654::M.IBase2.B.T<class BaseClass1,class G1_C12`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated654::M.IBase2.B.B<class G1_C12`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.MI.4868<System.Object>()#"
		call void Generated654::M.IBase1.T<class BaseClass0,class G1_C12`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method4.4865()#G1_C12::Method5.4866()#G1_C12::Method6.MI.4868<System.Object>()#"
		call void Generated654::M.IBase1.A<class G1_C12`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated654::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G1_C12`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated654::M.IBase2.A.T<class BaseClass1,class G1_C12`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C12::Method7.MI.4864<System.Object>()#"
		call void Generated654::M.IBase2.A.B<class G1_C12`2<class BaseClass1,class BaseClass1>>(!!0,string)
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
		newobj instance void class G3_C1126`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C12`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass0,class BaseClass1>::ClassMethod1347<object>()
		calli default string(class G3_C1126`1<class BaseClass0>)
		ldstr "G2_C161::ClassMethod1347.MI.6284<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass1> on type class G3_C1126`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass0,class BaseClass1>::ClassMethod1346<object>()
		calli default string(class G3_C1126`1<class BaseClass0>)
		ldstr "G2_C161::ClassMethod1346.MI.6283<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass1> on type class G3_C1126`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass0,class BaseClass1>::Method6<object>()
		calli default string(class G3_C1126`1<class BaseClass0>)
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass1> on type class G3_C1126`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass0,class BaseClass1>::Method5()
		calli default string(class G3_C1126`1<class BaseClass0>)
		ldstr "G2_C161::Method5.6276()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass1> on type class G3_C1126`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass0,class BaseClass1>::Method4()
		calli default string(class G3_C1126`1<class BaseClass0>)
		ldstr "G2_C161::Method4.6274()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass1> on type class G3_C1126`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1126`1<class BaseClass0>)
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass1> on type class G3_C1126`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1126`1<class BaseClass0>)
		ldstr "G3_C1126::Method7.MI.14471<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1126`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G3_C1126`1<class BaseClass0>)
		ldstr "G2_C161::Method4.MI.6275()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1126`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G3_C1126`1<class BaseClass0>)
		ldstr "G2_C161::Method5.MI.6277()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1126`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1126`1<class BaseClass0>)
		ldstr "G2_C161::Method6.MI.6279<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1126`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C161`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C161`1<class BaseClass0>::ClassMethod1646()
		calli default string(class G3_C1126`1<class BaseClass0>)
		ldstr "G2_C161::ClassMethod1646.6282()"
		ldstr "class G2_C161`1<class BaseClass0> on type class G3_C1126`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C161`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C161`1<class BaseClass0>::ClassMethod1645()
		calli default string(class G3_C1126`1<class BaseClass0>)
		ldstr "G2_C161::ClassMethod1645.6281()"
		ldstr "class G2_C161`1<class BaseClass0> on type class G3_C1126`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C161`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C161`1<class BaseClass0>::Method7<object>()
		calli default string(class G3_C1126`1<class BaseClass0>)
		ldstr "G3_C1126::Method7.14470<System.Object>()"
		ldstr "class G2_C161`1<class BaseClass0> on type class G3_C1126`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C161`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C161`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1126`1<class BaseClass0>)
		ldstr "G2_C161::Method6.6278<System.Object>()"
		ldstr "class G2_C161`1<class BaseClass0> on type class G3_C1126`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C161`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C161`1<class BaseClass0>::Method5()
		calli default string(class G3_C1126`1<class BaseClass0>)
		ldstr "G2_C161::Method5.6276()"
		ldstr "class G2_C161`1<class BaseClass0> on type class G3_C1126`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C161`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C161`1<class BaseClass0>::Method4()
		calli default string(class G3_C1126`1<class BaseClass0>)
		ldstr "G2_C161::Method4.6274()"
		ldstr "class G2_C161`1<class BaseClass0> on type class G3_C1126`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C161`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C161`1<class BaseClass0>::ClassMethod1347<object>()
		calli default string(class G3_C1126`1<class BaseClass0>)
		ldstr "G2_C161::ClassMethod1347.MI.6284<System.Object>()"
		ldstr "class G2_C161`1<class BaseClass0> on type class G3_C1126`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C161`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C161`1<class BaseClass0>::ClassMethod1346<object>()
		calli default string(class G3_C1126`1<class BaseClass0>)
		ldstr "G2_C161::ClassMethod1346.MI.6283<System.Object>()"
		ldstr "class G2_C161`1<class BaseClass0> on type class G3_C1126`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1126`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1126`1<class BaseClass0>::ClassMethod3651<object>()
		calli default string(class G3_C1126`1<class BaseClass0>)
		ldstr "G3_C1126::ClassMethod3651.14473<System.Object>()"
		ldstr "class G3_C1126`1<class BaseClass0> on type class G3_C1126`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1126`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1126`1<class BaseClass0>::ClassMethod3650<object>()
		calli default string(class G3_C1126`1<class BaseClass0>)
		ldstr "G3_C1126::ClassMethod3650.14472<System.Object>()"
		ldstr "class G3_C1126`1<class BaseClass0> on type class G3_C1126`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1126`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1126`1<class BaseClass0>::Method7<object>()
		calli default string(class G3_C1126`1<class BaseClass0>)
		ldstr "G3_C1126::Method7.14470<System.Object>()"
		ldstr "class G3_C1126`1<class BaseClass0> on type class G3_C1126`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1126`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1126`1<class BaseClass0>::ClassMethod1646()
		calli default string(class G3_C1126`1<class BaseClass0>)
		ldstr "G2_C161::ClassMethod1646.6282()"
		ldstr "class G3_C1126`1<class BaseClass0> on type class G3_C1126`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1126`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1126`1<class BaseClass0>::ClassMethod1645()
		calli default string(class G3_C1126`1<class BaseClass0>)
		ldstr "G2_C161::ClassMethod1645.6281()"
		ldstr "class G3_C1126`1<class BaseClass0> on type class G3_C1126`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1126`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1126`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1126`1<class BaseClass0>)
		ldstr "G2_C161::Method6.6278<System.Object>()"
		ldstr "class G3_C1126`1<class BaseClass0> on type class G3_C1126`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1126`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1126`1<class BaseClass0>::Method5()
		calli default string(class G3_C1126`1<class BaseClass0>)
		ldstr "G2_C161::Method5.6276()"
		ldstr "class G3_C1126`1<class BaseClass0> on type class G3_C1126`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1126`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1126`1<class BaseClass0>::Method4()
		calli default string(class G3_C1126`1<class BaseClass0>)
		ldstr "G2_C161::Method4.6274()"
		ldstr "class G3_C1126`1<class BaseClass0> on type class G3_C1126`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1126`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1126`1<class BaseClass0>::ClassMethod1347<object>()
		calli default string(class G3_C1126`1<class BaseClass0>)
		ldstr "G2_C161::ClassMethod1347.MI.6284<System.Object>()"
		ldstr "class G3_C1126`1<class BaseClass0> on type class G3_C1126`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1126`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1126`1<class BaseClass0>::ClassMethod1346<object>()
		calli default string(class G3_C1126`1<class BaseClass0>)
		ldstr "G2_C161::ClassMethod1346.MI.6283<System.Object>()"
		ldstr "class G3_C1126`1<class BaseClass0> on type class G3_C1126`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1126`1<class BaseClass0>)
		ldstr "G3_C1126::Method7.MI.14471<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1126`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G3_C1126`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C12`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass0,class BaseClass1>::ClassMethod1347<object>()
		calli default string(class G3_C1126`1<class BaseClass1>)
		ldstr "G2_C161::ClassMethod1347.MI.6284<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass1> on type class G3_C1126`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass0,class BaseClass1>::ClassMethod1346<object>()
		calli default string(class G3_C1126`1<class BaseClass1>)
		ldstr "G2_C161::ClassMethod1346.MI.6283<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass1> on type class G3_C1126`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass0,class BaseClass1>::Method6<object>()
		calli default string(class G3_C1126`1<class BaseClass1>)
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass1> on type class G3_C1126`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass0,class BaseClass1>::Method5()
		calli default string(class G3_C1126`1<class BaseClass1>)
		ldstr "G2_C161::Method5.6276()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass1> on type class G3_C1126`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass0,class BaseClass1>::Method4()
		calli default string(class G3_C1126`1<class BaseClass1>)
		ldstr "G2_C161::Method4.6274()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass1> on type class G3_C1126`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1126`1<class BaseClass1>)
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass1> on type class G3_C1126`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1126`1<class BaseClass1>)
		ldstr "G3_C1126::Method7.MI.14471<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1126`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G3_C1126`1<class BaseClass1>)
		ldstr "G2_C161::Method4.MI.6275()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1126`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G3_C1126`1<class BaseClass1>)
		ldstr "G2_C161::Method5.MI.6277()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1126`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1126`1<class BaseClass1>)
		ldstr "G2_C161::Method6.MI.6279<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1126`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C161`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C161`1<class BaseClass1>::ClassMethod1646()
		calli default string(class G3_C1126`1<class BaseClass1>)
		ldstr "G2_C161::ClassMethod1646.6282()"
		ldstr "class G2_C161`1<class BaseClass1> on type class G3_C1126`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C161`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C161`1<class BaseClass1>::ClassMethod1645()
		calli default string(class G3_C1126`1<class BaseClass1>)
		ldstr "G2_C161::ClassMethod1645.6281()"
		ldstr "class G2_C161`1<class BaseClass1> on type class G3_C1126`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C161`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C161`1<class BaseClass1>::Method7<object>()
		calli default string(class G3_C1126`1<class BaseClass1>)
		ldstr "G3_C1126::Method7.14470<System.Object>()"
		ldstr "class G2_C161`1<class BaseClass1> on type class G3_C1126`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C161`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C161`1<class BaseClass1>::Method6<object>()
		calli default string(class G3_C1126`1<class BaseClass1>)
		ldstr "G2_C161::Method6.6278<System.Object>()"
		ldstr "class G2_C161`1<class BaseClass1> on type class G3_C1126`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C161`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C161`1<class BaseClass1>::Method5()
		calli default string(class G3_C1126`1<class BaseClass1>)
		ldstr "G2_C161::Method5.6276()"
		ldstr "class G2_C161`1<class BaseClass1> on type class G3_C1126`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C161`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C161`1<class BaseClass1>::Method4()
		calli default string(class G3_C1126`1<class BaseClass1>)
		ldstr "G2_C161::Method4.6274()"
		ldstr "class G2_C161`1<class BaseClass1> on type class G3_C1126`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C161`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C161`1<class BaseClass1>::ClassMethod1347<object>()
		calli default string(class G3_C1126`1<class BaseClass1>)
		ldstr "G2_C161::ClassMethod1347.MI.6284<System.Object>()"
		ldstr "class G2_C161`1<class BaseClass1> on type class G3_C1126`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C161`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C161`1<class BaseClass1>::ClassMethod1346<object>()
		calli default string(class G3_C1126`1<class BaseClass1>)
		ldstr "G2_C161::ClassMethod1346.MI.6283<System.Object>()"
		ldstr "class G2_C161`1<class BaseClass1> on type class G3_C1126`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1126`1<class BaseClass1>)
		ldstr "G3_C1126::Method7.MI.14471<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1126`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1126`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1126`1<class BaseClass1>::ClassMethod3651<object>()
		calli default string(class G3_C1126`1<class BaseClass1>)
		ldstr "G3_C1126::ClassMethod3651.14473<System.Object>()"
		ldstr "class G3_C1126`1<class BaseClass1> on type class G3_C1126`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1126`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1126`1<class BaseClass1>::ClassMethod3650<object>()
		calli default string(class G3_C1126`1<class BaseClass1>)
		ldstr "G3_C1126::ClassMethod3650.14472<System.Object>()"
		ldstr "class G3_C1126`1<class BaseClass1> on type class G3_C1126`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1126`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1126`1<class BaseClass1>::Method7<object>()
		calli default string(class G3_C1126`1<class BaseClass1>)
		ldstr "G3_C1126::Method7.14470<System.Object>()"
		ldstr "class G3_C1126`1<class BaseClass1> on type class G3_C1126`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1126`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1126`1<class BaseClass1>::ClassMethod1646()
		calli default string(class G3_C1126`1<class BaseClass1>)
		ldstr "G2_C161::ClassMethod1646.6282()"
		ldstr "class G3_C1126`1<class BaseClass1> on type class G3_C1126`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1126`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1126`1<class BaseClass1>::ClassMethod1645()
		calli default string(class G3_C1126`1<class BaseClass1>)
		ldstr "G2_C161::ClassMethod1645.6281()"
		ldstr "class G3_C1126`1<class BaseClass1> on type class G3_C1126`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1126`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1126`1<class BaseClass1>::Method6<object>()
		calli default string(class G3_C1126`1<class BaseClass1>)
		ldstr "G2_C161::Method6.6278<System.Object>()"
		ldstr "class G3_C1126`1<class BaseClass1> on type class G3_C1126`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1126`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1126`1<class BaseClass1>::Method5()
		calli default string(class G3_C1126`1<class BaseClass1>)
		ldstr "G2_C161::Method5.6276()"
		ldstr "class G3_C1126`1<class BaseClass1> on type class G3_C1126`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1126`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1126`1<class BaseClass1>::Method4()
		calli default string(class G3_C1126`1<class BaseClass1>)
		ldstr "G2_C161::Method4.6274()"
		ldstr "class G3_C1126`1<class BaseClass1> on type class G3_C1126`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1126`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1126`1<class BaseClass1>::ClassMethod1347<object>()
		calli default string(class G3_C1126`1<class BaseClass1>)
		ldstr "G2_C161::ClassMethod1347.MI.6284<System.Object>()"
		ldstr "class G3_C1126`1<class BaseClass1> on type class G3_C1126`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1126`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1126`1<class BaseClass1>::ClassMethod1346<object>()
		calli default string(class G3_C1126`1<class BaseClass1>)
		ldstr "G2_C161::ClassMethod1346.MI.6283<System.Object>()"
		ldstr "class G3_C1126`1<class BaseClass1> on type class G3_C1126`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C161`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C12`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass0,class BaseClass1>::ClassMethod1347<object>()
		calli default string(class G2_C161`1<class BaseClass0>)
		ldstr "G2_C161::ClassMethod1347.MI.6284<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass1> on type class G2_C161`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass0,class BaseClass1>::ClassMethod1346<object>()
		calli default string(class G2_C161`1<class BaseClass0>)
		ldstr "G2_C161::ClassMethod1346.MI.6283<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass1> on type class G2_C161`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass0,class BaseClass1>::Method6<object>()
		calli default string(class G2_C161`1<class BaseClass0>)
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass1> on type class G2_C161`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass0,class BaseClass1>::Method5()
		calli default string(class G2_C161`1<class BaseClass0>)
		ldstr "G2_C161::Method5.6276()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass1> on type class G2_C161`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass0,class BaseClass1>::Method4()
		calli default string(class G2_C161`1<class BaseClass0>)
		ldstr "G2_C161::Method4.6274()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass1> on type class G2_C161`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C161`1<class BaseClass0>)
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass1> on type class G2_C161`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C161`1<class BaseClass0>)
		ldstr "G2_C161::Method7.6280<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C161`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G2_C161`1<class BaseClass0>)
		ldstr "G2_C161::Method4.MI.6275()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C161`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G2_C161`1<class BaseClass0>)
		ldstr "G2_C161::Method5.MI.6277()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C161`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G2_C161`1<class BaseClass0>)
		ldstr "G2_C161::Method6.MI.6279<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C161`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C161`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C161`1<class BaseClass0>::ClassMethod1646()
		calli default string(class G2_C161`1<class BaseClass0>)
		ldstr "G2_C161::ClassMethod1646.6282()"
		ldstr "class G2_C161`1<class BaseClass0> on type class G2_C161`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C161`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C161`1<class BaseClass0>::ClassMethod1645()
		calli default string(class G2_C161`1<class BaseClass0>)
		ldstr "G2_C161::ClassMethod1645.6281()"
		ldstr "class G2_C161`1<class BaseClass0> on type class G2_C161`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C161`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C161`1<class BaseClass0>::Method7<object>()
		calli default string(class G2_C161`1<class BaseClass0>)
		ldstr "G2_C161::Method7.6280<System.Object>()"
		ldstr "class G2_C161`1<class BaseClass0> on type class G2_C161`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C161`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C161`1<class BaseClass0>::Method6<object>()
		calli default string(class G2_C161`1<class BaseClass0>)
		ldstr "G2_C161::Method6.6278<System.Object>()"
		ldstr "class G2_C161`1<class BaseClass0> on type class G2_C161`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C161`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C161`1<class BaseClass0>::Method5()
		calli default string(class G2_C161`1<class BaseClass0>)
		ldstr "G2_C161::Method5.6276()"
		ldstr "class G2_C161`1<class BaseClass0> on type class G2_C161`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C161`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C161`1<class BaseClass0>::Method4()
		calli default string(class G2_C161`1<class BaseClass0>)
		ldstr "G2_C161::Method4.6274()"
		ldstr "class G2_C161`1<class BaseClass0> on type class G2_C161`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C161`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C161`1<class BaseClass0>::ClassMethod1347<object>()
		calli default string(class G2_C161`1<class BaseClass0>)
		ldstr "G2_C161::ClassMethod1347.MI.6284<System.Object>()"
		ldstr "class G2_C161`1<class BaseClass0> on type class G2_C161`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C161`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C161`1<class BaseClass0>::ClassMethod1346<object>()
		calli default string(class G2_C161`1<class BaseClass0>)
		ldstr "G2_C161::ClassMethod1346.MI.6283<System.Object>()"
		ldstr "class G2_C161`1<class BaseClass0> on type class G2_C161`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C161`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C12`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass0,class BaseClass1>::ClassMethod1347<object>()
		calli default string(class G2_C161`1<class BaseClass1>)
		ldstr "G2_C161::ClassMethod1347.MI.6284<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass1> on type class G2_C161`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass0,class BaseClass1>::ClassMethod1346<object>()
		calli default string(class G2_C161`1<class BaseClass1>)
		ldstr "G2_C161::ClassMethod1346.MI.6283<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass1> on type class G2_C161`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass0,class BaseClass1>::Method6<object>()
		calli default string(class G2_C161`1<class BaseClass1>)
		ldstr "G1_C12::Method6.4867<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass1> on type class G2_C161`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass0,class BaseClass1>::Method5()
		calli default string(class G2_C161`1<class BaseClass1>)
		ldstr "G2_C161::Method5.6276()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass1> on type class G2_C161`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass0,class BaseClass1>::Method4()
		calli default string(class G2_C161`1<class BaseClass1>)
		ldstr "G2_C161::Method4.6274()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass1> on type class G2_C161`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C12`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C12`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C161`1<class BaseClass1>)
		ldstr "G1_C12::Method7.4863<System.Object>()"
		ldstr "class G1_C12`2<class BaseClass0,class BaseClass1> on type class G2_C161`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C161`1<class BaseClass1>)
		ldstr "G2_C161::Method7.6280<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C161`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G2_C161`1<class BaseClass1>)
		ldstr "G2_C161::Method4.MI.6275()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C161`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G2_C161`1<class BaseClass1>)
		ldstr "G2_C161::Method5.MI.6277()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C161`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G2_C161`1<class BaseClass1>)
		ldstr "G2_C161::Method6.MI.6279<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C161`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C161`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C161`1<class BaseClass1>::ClassMethod1646()
		calli default string(class G2_C161`1<class BaseClass1>)
		ldstr "G2_C161::ClassMethod1646.6282()"
		ldstr "class G2_C161`1<class BaseClass1> on type class G2_C161`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C161`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C161`1<class BaseClass1>::ClassMethod1645()
		calli default string(class G2_C161`1<class BaseClass1>)
		ldstr "G2_C161::ClassMethod1645.6281()"
		ldstr "class G2_C161`1<class BaseClass1> on type class G2_C161`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C161`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C161`1<class BaseClass1>::Method7<object>()
		calli default string(class G2_C161`1<class BaseClass1>)
		ldstr "G2_C161::Method7.6280<System.Object>()"
		ldstr "class G2_C161`1<class BaseClass1> on type class G2_C161`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C161`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C161`1<class BaseClass1>::Method6<object>()
		calli default string(class G2_C161`1<class BaseClass1>)
		ldstr "G2_C161::Method6.6278<System.Object>()"
		ldstr "class G2_C161`1<class BaseClass1> on type class G2_C161`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C161`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C161`1<class BaseClass1>::Method5()
		calli default string(class G2_C161`1<class BaseClass1>)
		ldstr "G2_C161::Method5.6276()"
		ldstr "class G2_C161`1<class BaseClass1> on type class G2_C161`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C161`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C161`1<class BaseClass1>::Method4()
		calli default string(class G2_C161`1<class BaseClass1>)
		ldstr "G2_C161::Method4.6274()"
		ldstr "class G2_C161`1<class BaseClass1> on type class G2_C161`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C161`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C161`1<class BaseClass1>::ClassMethod1347<object>()
		calli default string(class G2_C161`1<class BaseClass1>)
		ldstr "G2_C161::ClassMethod1347.MI.6284<System.Object>()"
		ldstr "class G2_C161`1<class BaseClass1> on type class G2_C161`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C161`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C161`1<class BaseClass1>::ClassMethod1346<object>()
		calli default string(class G2_C161`1<class BaseClass1>)
		ldstr "G2_C161::ClassMethod1346.MI.6283<System.Object>()"
		ldstr "class G2_C161`1<class BaseClass1> on type class G2_C161`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C161`1<class BaseClass1>)
		ldstr "G2_C161::Method7.6280<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C161`1<class BaseClass1>"
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
		.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
		    01 00 00 00
		)
		.entrypoint
		.maxstack  10
		call void Generated654::MethodCallingTest()
		call void Generated654::ConstrainedCallsTest()
		call void Generated654::StructConstrainedInterfaceCallsTest()
		call void Generated654::CalliTest()
		ldc.i4 100
		ret
	}
}
