// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) .ver 4:0:0:0 }
.assembly extern TestFramework { .publickeytoken = ( B0 3F 5F 7F 11 D5 0A 3A )  }

//TYPES IN FORWARDER ASSEMBLIES:

//TEST ASSEMBLY:
.assembly Generated796 { .hash algorithm 0x00008004 }
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
.class public G3_C1268`1<T0> 
		extends class G2_C319`2<!T0,!T0>
		implements IBase0 
{
	.method public hidebysig virtual instance string Method0() cil managed noinlining { 
		ldstr "G3_C1268::Method0.15224()"
		ret
	}
	.method public hidebysig virtual instance string Method1() cil managed noinlining { 
		ldstr "G3_C1268::Method1.15225()"
		ret
	}
	.method public hidebysig virtual instance string Method2<M0>() cil managed noinlining { 
		ldstr "G3_C1268::Method2.15226<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase0.Method2'<M0>() cil managed noinlining { 
		.override method instance string IBase0::Method2<[1]>()
		ldstr "G3_C1268::Method2.MI.15227<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig virtual instance string Method3<M0>() cil managed noinlining { 
		ldstr "G3_C1268::Method3.15228<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase0.Method3'<M0>() cil managed noinlining { 
		.override method instance string IBase0::Method3<[1]>()
		ldstr "G3_C1268::Method3.MI.15229<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod3908() cil managed noinlining { 
		ldstr "G3_C1268::ClassMethod3908.15230()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod3909() cil managed noinlining { 
		ldstr "G3_C1268::ClassMethod3909.15231()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod3910<M0>() cil managed noinlining { 
		ldstr "G3_C1268::ClassMethod3910.15232<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod3911<M0>() cil managed noinlining { 
		ldstr "G3_C1268::ClassMethod3911.15233<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C319<T0,T0>.ClassMethod1977'() cil managed noinlining { 
		.override method instance string class G2_C319`2<!T0,!T0>::ClassMethod1977()
		ldstr "G3_C1268::ClassMethod1977.MI.15234()"
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class G2_C319`2<!T0,!T0>::.ctor()
		ret
	}
}
.class public abstract G2_C319`2<T0, T1> 
		extends class G1_C6`2<!T1,class BaseClass1>
		implements class IBase2`2<class BaseClass0,!T1>, class IBase1`1<class BaseClass1> 
{
	.method public hidebysig newslot virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G2_C319::Method7.7730<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string Method4() cil managed noinlining { 
		ldstr "G2_C319::Method4.7731()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class BaseClass1>.Method4'() cil managed noinlining { 
		.override method instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G2_C319::Method4.MI.7732()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method5() cil managed noinlining { 
		ldstr "G2_C319::Method5.7733()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class BaseClass1>.Method5'() cil managed noinlining { 
		.override method instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G2_C319::Method5.MI.7734()"
		ret
	}
	.method public hidebysig virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G2_C319::Method6.7735<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class BaseClass1>.Method6'<M0>() cil managed noinlining { 
		.override method instance string class IBase1`1<class BaseClass1>::Method6<[1]>()
		ldstr "G2_C319::Method6.MI.7736<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1977() cil managed noinlining { 
		ldstr "G2_C319::ClassMethod1977.7737()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1978<M0>() cil managed noinlining { 
		ldstr "G2_C319::ClassMethod1978.7738<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C6<T1,class BaseClass1>.ClassMethod1327'<M0>() cil managed noinlining { 
		.override method instance string class G1_C6`2<!T1,class BaseClass1>::ClassMethod1327<[1]>()
		ldstr "G2_C319::ClassMethod1327.MI.7739<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class G1_C6`2<!T1,class BaseClass1>::.ctor()
		ret
	}
}
.class interface public abstract IBase0 
{
	.method public hidebysig newslot abstract virtual instance string Method0() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method1() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method2<M0>() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method3<M0>() cil managed { }
}
.class public G1_C6`2<T0, T1> 
		implements class IBase2`2<class BaseClass1,!T0> 
{
	.method public hidebysig newslot virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G1_C6::Method7.4808<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1326() cil managed noinlining { 
		ldstr "G1_C6::ClassMethod1326.4809()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1327<M0>() cil managed noinlining { 
		ldstr "G1_C6::ClassMethod1327.4810<"
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
.class interface public abstract IBase1`1<+T0> 
{
	.method public hidebysig newslot abstract virtual instance string Method4() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method5() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method6<M0>() cil managed { }
}
.class public auto ansi beforefieldinit Generated796 {
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
	.method static void M.G3_C1268.T<T0,(class G3_C1268`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 21
		.locals init (string[] actualResults)
		ldc.i4.s    16
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1268.T<T0,(class G3_C1268`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 16
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1268`1<!!T0>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1268`1<!!T0>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1268`1<!!T0>::ClassMethod1977()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1268`1<!!T0>::ClassMethod1978<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1268`1<!!T0>::ClassMethod3908()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1268`1<!!T0>::ClassMethod3909()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1268`1<!!T0>::ClassMethod3910<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1268`1<!!T0>::ClassMethod3911<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1268`1<!!T0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1268`1<!!T0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1268`1<!!T0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1268`1<!!T0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1268`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1268`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     14
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1268`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     15
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1268`1<!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1268.A<(class G3_C1268`1<class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 21
		.locals init (string[] actualResults)
		ldc.i4.s    16
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1268.A<(class G3_C1268`1<class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 16
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1268`1<class BaseClass0>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1268`1<class BaseClass0>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1268`1<class BaseClass0>::ClassMethod1977()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1268`1<class BaseClass0>::ClassMethod1978<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1268`1<class BaseClass0>::ClassMethod3908()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1268`1<class BaseClass0>::ClassMethod3909()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1268`1<class BaseClass0>::ClassMethod3910<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1268`1<class BaseClass0>::ClassMethod3911<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1268`1<class BaseClass0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1268`1<class BaseClass0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1268`1<class BaseClass0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1268`1<class BaseClass0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1268`1<class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1268`1<class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     14
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1268`1<class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     15
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1268`1<class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1268.B<(class G3_C1268`1<class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 21
		.locals init (string[] actualResults)
		ldc.i4.s    16
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1268.B<(class G3_C1268`1<class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 16
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1268`1<class BaseClass1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1268`1<class BaseClass1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1268`1<class BaseClass1>::ClassMethod1977()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1268`1<class BaseClass1>::ClassMethod1978<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1268`1<class BaseClass1>::ClassMethod3908()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1268`1<class BaseClass1>::ClassMethod3909()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1268`1<class BaseClass1>::ClassMethod3910<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1268`1<class BaseClass1>::ClassMethod3911<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1268`1<class BaseClass1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1268`1<class BaseClass1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1268`1<class BaseClass1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1268`1<class BaseClass1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1268`1<class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1268`1<class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     14
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1268`1<class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     15
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1268`1<class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C319.T.T<T0,T1,(class G2_C319`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C319.T.T<T0,T1,(class G2_C319`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C319`2<!!T0,!!T1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C319`2<!!T0,!!T1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C319`2<!!T0,!!T1>::ClassMethod1977()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C319`2<!!T0,!!T1>::ClassMethod1978<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C319`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C319`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C319`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C319`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C319.A.T<T1,(class G2_C319`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C319.A.T<T1,(class G2_C319`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C319`2<class BaseClass0,!!T1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C319`2<class BaseClass0,!!T1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C319`2<class BaseClass0,!!T1>::ClassMethod1977()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C319`2<class BaseClass0,!!T1>::ClassMethod1978<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C319`2<class BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C319`2<class BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C319`2<class BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C319`2<class BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C319.A.A<(class G2_C319`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C319.A.A<(class G2_C319`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C319`2<class BaseClass0,class BaseClass0>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C319`2<class BaseClass0,class BaseClass0>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C319`2<class BaseClass0,class BaseClass0>::ClassMethod1977()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C319`2<class BaseClass0,class BaseClass0>::ClassMethod1978<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C319`2<class BaseClass0,class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C319`2<class BaseClass0,class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C319`2<class BaseClass0,class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C319`2<class BaseClass0,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C319.A.B<(class G2_C319`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C319.A.B<(class G2_C319`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C319`2<class BaseClass0,class BaseClass1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C319`2<class BaseClass0,class BaseClass1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C319`2<class BaseClass0,class BaseClass1>::ClassMethod1977()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C319`2<class BaseClass0,class BaseClass1>::ClassMethod1978<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C319`2<class BaseClass0,class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C319`2<class BaseClass0,class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C319`2<class BaseClass0,class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C319`2<class BaseClass0,class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C319.B.T<T1,(class G2_C319`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C319.B.T<T1,(class G2_C319`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C319`2<class BaseClass1,!!T1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C319`2<class BaseClass1,!!T1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C319`2<class BaseClass1,!!T1>::ClassMethod1977()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C319`2<class BaseClass1,!!T1>::ClassMethod1978<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C319`2<class BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C319`2<class BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C319`2<class BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C319`2<class BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C319.B.A<(class G2_C319`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C319.B.A<(class G2_C319`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C319`2<class BaseClass1,class BaseClass0>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C319`2<class BaseClass1,class BaseClass0>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C319`2<class BaseClass1,class BaseClass0>::ClassMethod1977()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C319`2<class BaseClass1,class BaseClass0>::ClassMethod1978<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C319`2<class BaseClass1,class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C319`2<class BaseClass1,class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C319`2<class BaseClass1,class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C319`2<class BaseClass1,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C319.B.B<(class G2_C319`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C319.B.B<(class G2_C319`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C319`2<class BaseClass1,class BaseClass1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C319`2<class BaseClass1,class BaseClass1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C319`2<class BaseClass1,class BaseClass1>::ClassMethod1977()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C319`2<class BaseClass1,class BaseClass1>::ClassMethod1978<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C319`2<class BaseClass1,class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C319`2<class BaseClass1,class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C319`2<class BaseClass1,class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C319`2<class BaseClass1,class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase0<(IBase0)W>(!!W inst, string exp) cil managed {
		.maxstack 9
		.locals init (string[] actualResults)
		ldc.i4.s    4
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase0<(IBase0)W>(!!W inst, string exp)"
		ldc.i4.s 4
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string IBase0::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string IBase0::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string IBase0::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string IBase0::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C6.T.T<T0,T1,(class G1_C6`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C6.T.T<T0,T1,(class G1_C6`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C6`2<!!T0,!!T1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C6`2<!!T0,!!T1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C6`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C6.A.T<T1,(class G1_C6`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C6.A.T<T1,(class G1_C6`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C6`2<class BaseClass0,!!T1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C6`2<class BaseClass0,!!T1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C6`2<class BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C6.A.A<(class G1_C6`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C6.A.A<(class G1_C6`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C6`2<class BaseClass0,class BaseClass0>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C6`2<class BaseClass0,class BaseClass0>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C6`2<class BaseClass0,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C6.A.B<(class G1_C6`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C6.A.B<(class G1_C6`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C6`2<class BaseClass0,class BaseClass1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C6`2<class BaseClass0,class BaseClass1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C6`2<class BaseClass0,class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C6.B.T<T1,(class G1_C6`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C6.B.T<T1,(class G1_C6`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C6`2<class BaseClass1,!!T1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C6`2<class BaseClass1,!!T1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C6`2<class BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C6.B.A<(class G1_C6`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C6.B.A<(class G1_C6`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C6`2<class BaseClass1,class BaseClass0>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C6`2<class BaseClass1,class BaseClass0>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C6`2<class BaseClass1,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C6.B.B<(class G1_C6`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C6.B.B<(class G1_C6`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C6`2<class BaseClass1,class BaseClass1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C6`2<class BaseClass1,class BaseClass1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C6`2<class BaseClass1,class BaseClass1>::Method7<object>()
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
		newobj instance void class G3_C1268`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C6`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C6`2<class BaseClass0,class BaseClass1>::ClassMethod1327<object>()
		ldstr "G2_C319::ClassMethod1327.MI.7739<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass0,class BaseClass1> on type class G3_C1268`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C6`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C6`2<class BaseClass0,class BaseClass1>::ClassMethod1326()
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class BaseClass0,class BaseClass1> on type class G3_C1268`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C6`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C6`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass0,class BaseClass1> on type class G3_C1268`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G3_C1268`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C319::Method7.7730<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1268`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C319::Method7.7730<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1268`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1268`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C319`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C319`2<class BaseClass0,class BaseClass0>::ClassMethod1978<object>()
		ldstr "G2_C319::ClassMethod1978.7738<System.Object>()"
		ldstr "class G2_C319`2<class BaseClass0,class BaseClass0> on type class G3_C1268`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C319`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C319`2<class BaseClass0,class BaseClass0>::ClassMethod1977()
		ldstr "G3_C1268::ClassMethod1977.MI.15234()"
		ldstr "class G2_C319`2<class BaseClass0,class BaseClass0> on type class G3_C1268`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C319`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C319`2<class BaseClass0,class BaseClass0>::Method6<object>()
		ldstr "G2_C319::Method6.7735<System.Object>()"
		ldstr "class G2_C319`2<class BaseClass0,class BaseClass0> on type class G3_C1268`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C319`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C319`2<class BaseClass0,class BaseClass0>::Method5()
		ldstr "G2_C319::Method5.7733()"
		ldstr "class G2_C319`2<class BaseClass0,class BaseClass0> on type class G3_C1268`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C319`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C319`2<class BaseClass0,class BaseClass0>::Method4()
		ldstr "G2_C319::Method4.7731()"
		ldstr "class G2_C319`2<class BaseClass0,class BaseClass0> on type class G3_C1268`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C319`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C319`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C319::Method7.7730<System.Object>()"
		ldstr "class G2_C319`2<class BaseClass0,class BaseClass0> on type class G3_C1268`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C319`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C319`2<class BaseClass0,class BaseClass0>::ClassMethod1327<object>()
		ldstr "G2_C319::ClassMethod1327.MI.7739<System.Object>()"
		ldstr "class G2_C319`2<class BaseClass0,class BaseClass0> on type class G3_C1268`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C319`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C319`2<class BaseClass0,class BaseClass0>::ClassMethod1326()
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G2_C319`2<class BaseClass0,class BaseClass0> on type class G3_C1268`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G2_C319::Method4.MI.7732()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1268`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G2_C319::Method5.MI.7734()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1268`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C319::Method6.MI.7736<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1268`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C319::Method4.MI.7732()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1268`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C319::Method5.MI.7734()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1268`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C319::Method6.MI.7736<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1268`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G3_C1268`1<class BaseClass0>
		callvirt instance string class G3_C1268`1<class BaseClass0>::ClassMethod3911<object>()
		ldstr "G3_C1268::ClassMethod3911.15233<System.Object>()"
		ldstr "class G3_C1268`1<class BaseClass0> on type class G3_C1268`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1268`1<class BaseClass0>
		callvirt instance string class G3_C1268`1<class BaseClass0>::ClassMethod3910<object>()
		ldstr "G3_C1268::ClassMethod3910.15232<System.Object>()"
		ldstr "class G3_C1268`1<class BaseClass0> on type class G3_C1268`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1268`1<class BaseClass0>
		callvirt instance string class G3_C1268`1<class BaseClass0>::ClassMethod3909()
		ldstr "G3_C1268::ClassMethod3909.15231()"
		ldstr "class G3_C1268`1<class BaseClass0> on type class G3_C1268`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1268`1<class BaseClass0>
		callvirt instance string class G3_C1268`1<class BaseClass0>::ClassMethod3908()
		ldstr "G3_C1268::ClassMethod3908.15230()"
		ldstr "class G3_C1268`1<class BaseClass0> on type class G3_C1268`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1268`1<class BaseClass0>
		callvirt instance string class G3_C1268`1<class BaseClass0>::Method3<object>()
		ldstr "G3_C1268::Method3.15228<System.Object>()"
		ldstr "class G3_C1268`1<class BaseClass0> on type class G3_C1268`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1268`1<class BaseClass0>
		callvirt instance string class G3_C1268`1<class BaseClass0>::Method2<object>()
		ldstr "G3_C1268::Method2.15226<System.Object>()"
		ldstr "class G3_C1268`1<class BaseClass0> on type class G3_C1268`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1268`1<class BaseClass0>
		callvirt instance string class G3_C1268`1<class BaseClass0>::Method1()
		ldstr "G3_C1268::Method1.15225()"
		ldstr "class G3_C1268`1<class BaseClass0> on type class G3_C1268`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1268`1<class BaseClass0>
		callvirt instance string class G3_C1268`1<class BaseClass0>::Method0()
		ldstr "G3_C1268::Method0.15224()"
		ldstr "class G3_C1268`1<class BaseClass0> on type class G3_C1268`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1268`1<class BaseClass0>
		callvirt instance string class G3_C1268`1<class BaseClass0>::ClassMethod1978<object>()
		ldstr "G2_C319::ClassMethod1978.7738<System.Object>()"
		ldstr "class G3_C1268`1<class BaseClass0> on type class G3_C1268`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1268`1<class BaseClass0>
		callvirt instance string class G3_C1268`1<class BaseClass0>::ClassMethod1977()
		ldstr "G3_C1268::ClassMethod1977.MI.15234()"
		ldstr "class G3_C1268`1<class BaseClass0> on type class G3_C1268`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1268`1<class BaseClass0>
		callvirt instance string class G3_C1268`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C319::Method6.7735<System.Object>()"
		ldstr "class G3_C1268`1<class BaseClass0> on type class G3_C1268`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1268`1<class BaseClass0>
		callvirt instance string class G3_C1268`1<class BaseClass0>::Method5()
		ldstr "G2_C319::Method5.7733()"
		ldstr "class G3_C1268`1<class BaseClass0> on type class G3_C1268`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1268`1<class BaseClass0>
		callvirt instance string class G3_C1268`1<class BaseClass0>::Method4()
		ldstr "G2_C319::Method4.7731()"
		ldstr "class G3_C1268`1<class BaseClass0> on type class G3_C1268`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1268`1<class BaseClass0>
		callvirt instance string class G3_C1268`1<class BaseClass0>::Method7<object>()
		ldstr "G2_C319::Method7.7730<System.Object>()"
		ldstr "class G3_C1268`1<class BaseClass0> on type class G3_C1268`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1268`1<class BaseClass0>
		callvirt instance string class G3_C1268`1<class BaseClass0>::ClassMethod1327<object>()
		ldstr "G2_C319::ClassMethod1327.MI.7739<System.Object>()"
		ldstr "class G3_C1268`1<class BaseClass0> on type class G3_C1268`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1268`1<class BaseClass0>
		callvirt instance string class G3_C1268`1<class BaseClass0>::ClassMethod1326()
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G3_C1268`1<class BaseClass0> on type class G3_C1268`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string IBase0::Method0()
		ldstr "G3_C1268::Method0.15224()"
		ldstr "IBase0 on type class G3_C1268`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method1()
		ldstr "G3_C1268::Method1.15225()"
		ldstr "IBase0 on type class G3_C1268`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method2<object>()
		ldstr "G3_C1268::Method2.MI.15227<System.Object>()"
		ldstr "IBase0 on type class G3_C1268`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method3<object>()
		ldstr "G3_C1268::Method3.MI.15229<System.Object>()"
		ldstr "IBase0 on type class G3_C1268`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G3_C1268`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C6`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C6`2<class BaseClass1,class BaseClass1>::ClassMethod1327<object>()
		ldstr "G2_C319::ClassMethod1327.MI.7739<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass1> on type class G3_C1268`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C6`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C6`2<class BaseClass1,class BaseClass1>::ClassMethod1326()
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass1> on type class G3_C1268`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C6`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C6`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass1> on type class G3_C1268`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1268`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C319::Method7.7730<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1268`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C319`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C319`2<class BaseClass1,class BaseClass1>::ClassMethod1978<object>()
		ldstr "G2_C319::ClassMethod1978.7738<System.Object>()"
		ldstr "class G2_C319`2<class BaseClass1,class BaseClass1> on type class G3_C1268`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C319`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C319`2<class BaseClass1,class BaseClass1>::ClassMethod1977()
		ldstr "G3_C1268::ClassMethod1977.MI.15234()"
		ldstr "class G2_C319`2<class BaseClass1,class BaseClass1> on type class G3_C1268`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C319`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C319`2<class BaseClass1,class BaseClass1>::Method6<object>()
		ldstr "G2_C319::Method6.7735<System.Object>()"
		ldstr "class G2_C319`2<class BaseClass1,class BaseClass1> on type class G3_C1268`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C319`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C319`2<class BaseClass1,class BaseClass1>::Method5()
		ldstr "G2_C319::Method5.7733()"
		ldstr "class G2_C319`2<class BaseClass1,class BaseClass1> on type class G3_C1268`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C319`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C319`2<class BaseClass1,class BaseClass1>::Method4()
		ldstr "G2_C319::Method4.7731()"
		ldstr "class G2_C319`2<class BaseClass1,class BaseClass1> on type class G3_C1268`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C319`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C319`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C319::Method7.7730<System.Object>()"
		ldstr "class G2_C319`2<class BaseClass1,class BaseClass1> on type class G3_C1268`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C319`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C319`2<class BaseClass1,class BaseClass1>::ClassMethod1327<object>()
		ldstr "G2_C319::ClassMethod1327.MI.7739<System.Object>()"
		ldstr "class G2_C319`2<class BaseClass1,class BaseClass1> on type class G3_C1268`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C319`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C319`2<class BaseClass1,class BaseClass1>::ClassMethod1326()
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G2_C319`2<class BaseClass1,class BaseClass1> on type class G3_C1268`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G2_C319::Method4.MI.7732()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1268`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G2_C319::Method5.MI.7734()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1268`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C319::Method6.MI.7736<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1268`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C319::Method4.MI.7732()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1268`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C319::Method5.MI.7734()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1268`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C319::Method6.MI.7736<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1268`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G3_C1268`1<class BaseClass1>
		callvirt instance string class G3_C1268`1<class BaseClass1>::ClassMethod3911<object>()
		ldstr "G3_C1268::ClassMethod3911.15233<System.Object>()"
		ldstr "class G3_C1268`1<class BaseClass1> on type class G3_C1268`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1268`1<class BaseClass1>
		callvirt instance string class G3_C1268`1<class BaseClass1>::ClassMethod3910<object>()
		ldstr "G3_C1268::ClassMethod3910.15232<System.Object>()"
		ldstr "class G3_C1268`1<class BaseClass1> on type class G3_C1268`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1268`1<class BaseClass1>
		callvirt instance string class G3_C1268`1<class BaseClass1>::ClassMethod3909()
		ldstr "G3_C1268::ClassMethod3909.15231()"
		ldstr "class G3_C1268`1<class BaseClass1> on type class G3_C1268`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1268`1<class BaseClass1>
		callvirt instance string class G3_C1268`1<class BaseClass1>::ClassMethod3908()
		ldstr "G3_C1268::ClassMethod3908.15230()"
		ldstr "class G3_C1268`1<class BaseClass1> on type class G3_C1268`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1268`1<class BaseClass1>
		callvirt instance string class G3_C1268`1<class BaseClass1>::Method3<object>()
		ldstr "G3_C1268::Method3.15228<System.Object>()"
		ldstr "class G3_C1268`1<class BaseClass1> on type class G3_C1268`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1268`1<class BaseClass1>
		callvirt instance string class G3_C1268`1<class BaseClass1>::Method2<object>()
		ldstr "G3_C1268::Method2.15226<System.Object>()"
		ldstr "class G3_C1268`1<class BaseClass1> on type class G3_C1268`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1268`1<class BaseClass1>
		callvirt instance string class G3_C1268`1<class BaseClass1>::Method1()
		ldstr "G3_C1268::Method1.15225()"
		ldstr "class G3_C1268`1<class BaseClass1> on type class G3_C1268`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1268`1<class BaseClass1>
		callvirt instance string class G3_C1268`1<class BaseClass1>::Method0()
		ldstr "G3_C1268::Method0.15224()"
		ldstr "class G3_C1268`1<class BaseClass1> on type class G3_C1268`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1268`1<class BaseClass1>
		callvirt instance string class G3_C1268`1<class BaseClass1>::ClassMethod1978<object>()
		ldstr "G2_C319::ClassMethod1978.7738<System.Object>()"
		ldstr "class G3_C1268`1<class BaseClass1> on type class G3_C1268`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1268`1<class BaseClass1>
		callvirt instance string class G3_C1268`1<class BaseClass1>::ClassMethod1977()
		ldstr "G3_C1268::ClassMethod1977.MI.15234()"
		ldstr "class G3_C1268`1<class BaseClass1> on type class G3_C1268`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1268`1<class BaseClass1>
		callvirt instance string class G3_C1268`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C319::Method6.7735<System.Object>()"
		ldstr "class G3_C1268`1<class BaseClass1> on type class G3_C1268`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1268`1<class BaseClass1>
		callvirt instance string class G3_C1268`1<class BaseClass1>::Method5()
		ldstr "G2_C319::Method5.7733()"
		ldstr "class G3_C1268`1<class BaseClass1> on type class G3_C1268`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1268`1<class BaseClass1>
		callvirt instance string class G3_C1268`1<class BaseClass1>::Method4()
		ldstr "G2_C319::Method4.7731()"
		ldstr "class G3_C1268`1<class BaseClass1> on type class G3_C1268`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1268`1<class BaseClass1>
		callvirt instance string class G3_C1268`1<class BaseClass1>::Method7<object>()
		ldstr "G2_C319::Method7.7730<System.Object>()"
		ldstr "class G3_C1268`1<class BaseClass1> on type class G3_C1268`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1268`1<class BaseClass1>
		callvirt instance string class G3_C1268`1<class BaseClass1>::ClassMethod1327<object>()
		ldstr "G2_C319::ClassMethod1327.MI.7739<System.Object>()"
		ldstr "class G3_C1268`1<class BaseClass1> on type class G3_C1268`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1268`1<class BaseClass1>
		callvirt instance string class G3_C1268`1<class BaseClass1>::ClassMethod1326()
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G3_C1268`1<class BaseClass1> on type class G3_C1268`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string IBase0::Method0()
		ldstr "G3_C1268::Method0.15224()"
		ldstr "IBase0 on type class G3_C1268`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method1()
		ldstr "G3_C1268::Method1.15225()"
		ldstr "IBase0 on type class G3_C1268`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method2<object>()
		ldstr "G3_C1268::Method2.MI.15227<System.Object>()"
		ldstr "IBase0 on type class G3_C1268`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method3<object>()
		ldstr "G3_C1268::Method3.MI.15229<System.Object>()"
		ldstr "IBase0 on type class G3_C1268`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G1_C6`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C6`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C6`2<class BaseClass0,class BaseClass0>::ClassMethod1327<object>()
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass0,class BaseClass0> on type class G1_C6`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C6`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C6`2<class BaseClass0,class BaseClass0>::ClassMethod1326()
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class BaseClass0,class BaseClass0> on type class G1_C6`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C6`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C6`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass0,class BaseClass0> on type class G1_C6`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G1_C6`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G1_C6`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G1_C6`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G1_C6`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G1_C6`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C6`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C6`2<class BaseClass0,class BaseClass1>::ClassMethod1327<object>()
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass0,class BaseClass1> on type class G1_C6`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C6`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C6`2<class BaseClass0,class BaseClass1>::ClassMethod1326()
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class BaseClass0,class BaseClass1> on type class G1_C6`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C6`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C6`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass0,class BaseClass1> on type class G1_C6`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G1_C6`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G1_C6`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G1_C6`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G1_C6`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G1_C6`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C6`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C6`2<class BaseClass1,class BaseClass0>::ClassMethod1327<object>()
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass0> on type class G1_C6`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C6`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C6`2<class BaseClass1,class BaseClass0>::ClassMethod1326()
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass0> on type class G1_C6`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C6`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C6`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass0> on type class G1_C6`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G1_C6`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G1_C6`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G1_C6`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C6`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C6`2<class BaseClass1,class BaseClass1>::ClassMethod1327<object>()
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass1> on type class G1_C6`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C6`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C6`2<class BaseClass1,class BaseClass1>::ClassMethod1326()
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass1> on type class G1_C6`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C6`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C6`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass1> on type class G1_C6`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G1_C6`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G1_C6`2<class BaseClass1,class BaseClass1>"
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
		newobj instance void class G3_C1268`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G2_C319::ClassMethod1327.MI.7739<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated796::M.G1_C6.T.T<class BaseClass0,class BaseClass1,class G3_C1268`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G2_C319::ClassMethod1327.MI.7739<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated796::M.G1_C6.A.T<class BaseClass1,class G3_C1268`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G2_C319::ClassMethod1327.MI.7739<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated796::M.G1_C6.A.B<class G3_C1268`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated796::M.IBase2.T.T<class BaseClass1,class BaseClass0,class G3_C1268`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated796::M.IBase2.B.T<class BaseClass0,class G3_C1268`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated796::M.IBase2.B.A<class G3_C1268`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C319::Method7.7730<System.Object>()#"
		call void Generated796::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G3_C1268`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C319::Method7.7730<System.Object>()#"
		call void Generated796::M.IBase2.A.T<class BaseClass0,class G3_C1268`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C319::Method7.7730<System.Object>()#"
		call void Generated796::M.IBase2.A.A<class G3_C1268`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C319::Method7.7730<System.Object>()#"
		call void Generated796::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G3_C1268`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C319::Method7.7730<System.Object>()#"
		call void Generated796::M.IBase2.A.T<class BaseClass1,class G3_C1268`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C319::Method7.7730<System.Object>()#"
		call void Generated796::M.IBase2.A.B<class G3_C1268`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated796::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G3_C1268`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated796::M.IBase2.B.T<class BaseClass1,class G3_C1268`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated796::M.IBase2.B.B<class G3_C1268`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G2_C319::ClassMethod1327.MI.7739<System.Object>()#G3_C1268::ClassMethod1977.MI.15234()#G2_C319::ClassMethod1978.7738<System.Object>()#G2_C319::Method4.7731()#G2_C319::Method5.7733()#G2_C319::Method6.7735<System.Object>()#G2_C319::Method7.7730<System.Object>()#"
		call void Generated796::M.G2_C319.T.T<class BaseClass0,class BaseClass0,class G3_C1268`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G2_C319::ClassMethod1327.MI.7739<System.Object>()#G3_C1268::ClassMethod1977.MI.15234()#G2_C319::ClassMethod1978.7738<System.Object>()#G2_C319::Method4.7731()#G2_C319::Method5.7733()#G2_C319::Method6.7735<System.Object>()#G2_C319::Method7.7730<System.Object>()#"
		call void Generated796::M.G2_C319.A.T<class BaseClass0,class G3_C1268`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G2_C319::ClassMethod1327.MI.7739<System.Object>()#G3_C1268::ClassMethod1977.MI.15234()#G2_C319::ClassMethod1978.7738<System.Object>()#G2_C319::Method4.7731()#G2_C319::Method5.7733()#G2_C319::Method6.7735<System.Object>()#G2_C319::Method7.7730<System.Object>()#"
		call void Generated796::M.G2_C319.A.A<class G3_C1268`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C319::Method4.MI.7732()#G2_C319::Method5.MI.7734()#G2_C319::Method6.MI.7736<System.Object>()#"
		call void Generated796::M.IBase1.T<class BaseClass1,class G3_C1268`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C319::Method4.MI.7732()#G2_C319::Method5.MI.7734()#G2_C319::Method6.MI.7736<System.Object>()#"
		call void Generated796::M.IBase1.B<class G3_C1268`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C319::Method4.MI.7732()#G2_C319::Method5.MI.7734()#G2_C319::Method6.MI.7736<System.Object>()#"
		call void Generated796::M.IBase1.T<class BaseClass0,class G3_C1268`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C319::Method4.MI.7732()#G2_C319::Method5.MI.7734()#G2_C319::Method6.MI.7736<System.Object>()#"
		call void Generated796::M.IBase1.A<class G3_C1268`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G2_C319::ClassMethod1327.MI.7739<System.Object>()#G3_C1268::ClassMethod1977.MI.15234()#G2_C319::ClassMethod1978.7738<System.Object>()#G3_C1268::ClassMethod3908.15230()#G3_C1268::ClassMethod3909.15231()#G3_C1268::ClassMethod3910.15232<System.Object>()#G3_C1268::ClassMethod3911.15233<System.Object>()#G3_C1268::Method0.15224()#G3_C1268::Method1.15225()#G3_C1268::Method2.15226<System.Object>()#G3_C1268::Method3.15228<System.Object>()#G2_C319::Method4.7731()#G2_C319::Method5.7733()#G2_C319::Method6.7735<System.Object>()#G2_C319::Method7.7730<System.Object>()#"
		call void Generated796::M.G3_C1268.T<class BaseClass0,class G3_C1268`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G2_C319::ClassMethod1327.MI.7739<System.Object>()#G3_C1268::ClassMethod1977.MI.15234()#G2_C319::ClassMethod1978.7738<System.Object>()#G3_C1268::ClassMethod3908.15230()#G3_C1268::ClassMethod3909.15231()#G3_C1268::ClassMethod3910.15232<System.Object>()#G3_C1268::ClassMethod3911.15233<System.Object>()#G3_C1268::Method0.15224()#G3_C1268::Method1.15225()#G3_C1268::Method2.15226<System.Object>()#G3_C1268::Method3.15228<System.Object>()#G2_C319::Method4.7731()#G2_C319::Method5.7733()#G2_C319::Method6.7735<System.Object>()#G2_C319::Method7.7730<System.Object>()#"
		call void Generated796::M.G3_C1268.A<class G3_C1268`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1268::Method0.15224()#G3_C1268::Method1.15225()#G3_C1268::Method2.MI.15227<System.Object>()#G3_C1268::Method3.MI.15229<System.Object>()#"
		call void Generated796::M.IBase0<class G3_C1268`1<class BaseClass0>>(!!0,string)
		newobj instance void class G3_C1268`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G2_C319::ClassMethod1327.MI.7739<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated796::M.G1_C6.T.T<class BaseClass1,class BaseClass1,class G3_C1268`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G2_C319::ClassMethod1327.MI.7739<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated796::M.G1_C6.B.T<class BaseClass1,class G3_C1268`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G2_C319::ClassMethod1327.MI.7739<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated796::M.G1_C6.B.B<class G3_C1268`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated796::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G3_C1268`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated796::M.IBase2.B.T<class BaseClass1,class G3_C1268`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated796::M.IBase2.B.B<class G3_C1268`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C319::Method7.7730<System.Object>()#"
		call void Generated796::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G3_C1268`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C319::Method7.7730<System.Object>()#"
		call void Generated796::M.IBase2.A.T<class BaseClass1,class G3_C1268`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C319::Method7.7730<System.Object>()#"
		call void Generated796::M.IBase2.A.B<class G3_C1268`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G2_C319::ClassMethod1327.MI.7739<System.Object>()#G3_C1268::ClassMethod1977.MI.15234()#G2_C319::ClassMethod1978.7738<System.Object>()#G2_C319::Method4.7731()#G2_C319::Method5.7733()#G2_C319::Method6.7735<System.Object>()#G2_C319::Method7.7730<System.Object>()#"
		call void Generated796::M.G2_C319.T.T<class BaseClass1,class BaseClass1,class G3_C1268`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G2_C319::ClassMethod1327.MI.7739<System.Object>()#G3_C1268::ClassMethod1977.MI.15234()#G2_C319::ClassMethod1978.7738<System.Object>()#G2_C319::Method4.7731()#G2_C319::Method5.7733()#G2_C319::Method6.7735<System.Object>()#G2_C319::Method7.7730<System.Object>()#"
		call void Generated796::M.G2_C319.B.T<class BaseClass1,class G3_C1268`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G2_C319::ClassMethod1327.MI.7739<System.Object>()#G3_C1268::ClassMethod1977.MI.15234()#G2_C319::ClassMethod1978.7738<System.Object>()#G2_C319::Method4.7731()#G2_C319::Method5.7733()#G2_C319::Method6.7735<System.Object>()#G2_C319::Method7.7730<System.Object>()#"
		call void Generated796::M.G2_C319.B.B<class G3_C1268`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C319::Method4.MI.7732()#G2_C319::Method5.MI.7734()#G2_C319::Method6.MI.7736<System.Object>()#"
		call void Generated796::M.IBase1.T<class BaseClass1,class G3_C1268`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C319::Method4.MI.7732()#G2_C319::Method5.MI.7734()#G2_C319::Method6.MI.7736<System.Object>()#"
		call void Generated796::M.IBase1.B<class G3_C1268`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C319::Method4.MI.7732()#G2_C319::Method5.MI.7734()#G2_C319::Method6.MI.7736<System.Object>()#"
		call void Generated796::M.IBase1.T<class BaseClass0,class G3_C1268`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C319::Method4.MI.7732()#G2_C319::Method5.MI.7734()#G2_C319::Method6.MI.7736<System.Object>()#"
		call void Generated796::M.IBase1.A<class G3_C1268`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G2_C319::ClassMethod1327.MI.7739<System.Object>()#G3_C1268::ClassMethod1977.MI.15234()#G2_C319::ClassMethod1978.7738<System.Object>()#G3_C1268::ClassMethod3908.15230()#G3_C1268::ClassMethod3909.15231()#G3_C1268::ClassMethod3910.15232<System.Object>()#G3_C1268::ClassMethod3911.15233<System.Object>()#G3_C1268::Method0.15224()#G3_C1268::Method1.15225()#G3_C1268::Method2.15226<System.Object>()#G3_C1268::Method3.15228<System.Object>()#G2_C319::Method4.7731()#G2_C319::Method5.7733()#G2_C319::Method6.7735<System.Object>()#G2_C319::Method7.7730<System.Object>()#"
		call void Generated796::M.G3_C1268.T<class BaseClass1,class G3_C1268`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G2_C319::ClassMethod1327.MI.7739<System.Object>()#G3_C1268::ClassMethod1977.MI.15234()#G2_C319::ClassMethod1978.7738<System.Object>()#G3_C1268::ClassMethod3908.15230()#G3_C1268::ClassMethod3909.15231()#G3_C1268::ClassMethod3910.15232<System.Object>()#G3_C1268::ClassMethod3911.15233<System.Object>()#G3_C1268::Method0.15224()#G3_C1268::Method1.15225()#G3_C1268::Method2.15226<System.Object>()#G3_C1268::Method3.15228<System.Object>()#G2_C319::Method4.7731()#G2_C319::Method5.7733()#G2_C319::Method6.7735<System.Object>()#G2_C319::Method7.7730<System.Object>()#"
		call void Generated796::M.G3_C1268.B<class G3_C1268`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1268::Method0.15224()#G3_C1268::Method1.15225()#G3_C1268::Method2.MI.15227<System.Object>()#G3_C1268::Method3.MI.15229<System.Object>()#"
		call void Generated796::M.IBase0<class G3_C1268`1<class BaseClass1>>(!!0,string)
		newobj instance void class G1_C6`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated796::M.G1_C6.T.T<class BaseClass0,class BaseClass0,class G1_C6`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated796::M.G1_C6.A.T<class BaseClass0,class G1_C6`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated796::M.G1_C6.A.A<class G1_C6`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated796::M.IBase2.T.T<class BaseClass1,class BaseClass0,class G1_C6`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated796::M.IBase2.B.T<class BaseClass0,class G1_C6`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated796::M.IBase2.B.A<class G1_C6`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated796::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G1_C6`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated796::M.IBase2.A.T<class BaseClass0,class G1_C6`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated796::M.IBase2.A.A<class G1_C6`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated796::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G1_C6`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated796::M.IBase2.A.T<class BaseClass1,class G1_C6`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated796::M.IBase2.A.B<class G1_C6`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated796::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G1_C6`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated796::M.IBase2.B.T<class BaseClass1,class G1_C6`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated796::M.IBase2.B.B<class G1_C6`2<class BaseClass0,class BaseClass0>>(!!0,string)
		newobj instance void class G1_C6`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated796::M.G1_C6.T.T<class BaseClass0,class BaseClass1,class G1_C6`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated796::M.G1_C6.A.T<class BaseClass1,class G1_C6`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated796::M.G1_C6.A.B<class G1_C6`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated796::M.IBase2.T.T<class BaseClass1,class BaseClass0,class G1_C6`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated796::M.IBase2.B.T<class BaseClass0,class G1_C6`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated796::M.IBase2.B.A<class G1_C6`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated796::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G1_C6`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated796::M.IBase2.A.T<class BaseClass0,class G1_C6`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated796::M.IBase2.A.A<class G1_C6`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated796::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G1_C6`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated796::M.IBase2.A.T<class BaseClass1,class G1_C6`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated796::M.IBase2.A.B<class G1_C6`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated796::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G1_C6`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated796::M.IBase2.B.T<class BaseClass1,class G1_C6`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated796::M.IBase2.B.B<class G1_C6`2<class BaseClass0,class BaseClass1>>(!!0,string)
		newobj instance void class G1_C6`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated796::M.G1_C6.T.T<class BaseClass1,class BaseClass0,class G1_C6`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated796::M.G1_C6.B.T<class BaseClass0,class G1_C6`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated796::M.G1_C6.B.A<class G1_C6`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated796::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G1_C6`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated796::M.IBase2.B.T<class BaseClass1,class G1_C6`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated796::M.IBase2.B.B<class G1_C6`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated796::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G1_C6`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated796::M.IBase2.A.T<class BaseClass1,class G1_C6`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated796::M.IBase2.A.B<class G1_C6`2<class BaseClass1,class BaseClass0>>(!!0,string)
		newobj instance void class G1_C6`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated796::M.G1_C6.T.T<class BaseClass1,class BaseClass1,class G1_C6`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated796::M.G1_C6.B.T<class BaseClass1,class G1_C6`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated796::M.G1_C6.B.B<class G1_C6`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated796::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G1_C6`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated796::M.IBase2.B.T<class BaseClass1,class G1_C6`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated796::M.IBase2.B.B<class G1_C6`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated796::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G1_C6`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated796::M.IBase2.A.T<class BaseClass1,class G1_C6`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated796::M.IBase2.A.B<class G1_C6`2<class BaseClass1,class BaseClass1>>(!!0,string)
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
		newobj instance void class G3_C1268`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C6`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass0,class BaseClass1>::ClassMethod1327<object>()
		calli default string(class G3_C1268`1<class BaseClass0>)
		ldstr "G2_C319::ClassMethod1327.MI.7739<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass0,class BaseClass1> on type class G3_C1268`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C6`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass0,class BaseClass1>::ClassMethod1326()
		calli default string(class G3_C1268`1<class BaseClass0>)
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class BaseClass0,class BaseClass1> on type class G3_C1268`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C6`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1268`1<class BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass0,class BaseClass1> on type class G3_C1268`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1268`1<class BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G3_C1268`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1268`1<class BaseClass0>)
		ldstr "G2_C319::Method7.7730<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1268`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1268`1<class BaseClass0>)
		ldstr "G2_C319::Method7.7730<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1268`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1268`1<class BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1268`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C319`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C319`2<class BaseClass0,class BaseClass0>::ClassMethod1978<object>()
		calli default string(class G3_C1268`1<class BaseClass0>)
		ldstr "G2_C319::ClassMethod1978.7738<System.Object>()"
		ldstr "class G2_C319`2<class BaseClass0,class BaseClass0> on type class G3_C1268`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C319`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C319`2<class BaseClass0,class BaseClass0>::ClassMethod1977()
		calli default string(class G3_C1268`1<class BaseClass0>)
		ldstr "G3_C1268::ClassMethod1977.MI.15234()"
		ldstr "class G2_C319`2<class BaseClass0,class BaseClass0> on type class G3_C1268`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C319`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C319`2<class BaseClass0,class BaseClass0>::Method6<object>()
		calli default string(class G3_C1268`1<class BaseClass0>)
		ldstr "G2_C319::Method6.7735<System.Object>()"
		ldstr "class G2_C319`2<class BaseClass0,class BaseClass0> on type class G3_C1268`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C319`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C319`2<class BaseClass0,class BaseClass0>::Method5()
		calli default string(class G3_C1268`1<class BaseClass0>)
		ldstr "G2_C319::Method5.7733()"
		ldstr "class G2_C319`2<class BaseClass0,class BaseClass0> on type class G3_C1268`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C319`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C319`2<class BaseClass0,class BaseClass0>::Method4()
		calli default string(class G3_C1268`1<class BaseClass0>)
		ldstr "G2_C319::Method4.7731()"
		ldstr "class G2_C319`2<class BaseClass0,class BaseClass0> on type class G3_C1268`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C319`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C319`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1268`1<class BaseClass0>)
		ldstr "G2_C319::Method7.7730<System.Object>()"
		ldstr "class G2_C319`2<class BaseClass0,class BaseClass0> on type class G3_C1268`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C319`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C319`2<class BaseClass0,class BaseClass0>::ClassMethod1327<object>()
		calli default string(class G3_C1268`1<class BaseClass0>)
		ldstr "G2_C319::ClassMethod1327.MI.7739<System.Object>()"
		ldstr "class G2_C319`2<class BaseClass0,class BaseClass0> on type class G3_C1268`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C319`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C319`2<class BaseClass0,class BaseClass0>::ClassMethod1326()
		calli default string(class G3_C1268`1<class BaseClass0>)
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G2_C319`2<class BaseClass0,class BaseClass0> on type class G3_C1268`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G3_C1268`1<class BaseClass0>)
		ldstr "G2_C319::Method4.MI.7732()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1268`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G3_C1268`1<class BaseClass0>)
		ldstr "G2_C319::Method5.MI.7734()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1268`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G3_C1268`1<class BaseClass0>)
		ldstr "G2_C319::Method6.MI.7736<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1268`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G3_C1268`1<class BaseClass0>)
		ldstr "G2_C319::Method4.MI.7732()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1268`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G3_C1268`1<class BaseClass0>)
		ldstr "G2_C319::Method5.MI.7734()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1268`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1268`1<class BaseClass0>)
		ldstr "G2_C319::Method6.MI.7736<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1268`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1268`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1268`1<class BaseClass0>::ClassMethod3911<object>()
		calli default string(class G3_C1268`1<class BaseClass0>)
		ldstr "G3_C1268::ClassMethod3911.15233<System.Object>()"
		ldstr "class G3_C1268`1<class BaseClass0> on type class G3_C1268`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1268`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1268`1<class BaseClass0>::ClassMethod3910<object>()
		calli default string(class G3_C1268`1<class BaseClass0>)
		ldstr "G3_C1268::ClassMethod3910.15232<System.Object>()"
		ldstr "class G3_C1268`1<class BaseClass0> on type class G3_C1268`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1268`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1268`1<class BaseClass0>::ClassMethod3909()
		calli default string(class G3_C1268`1<class BaseClass0>)
		ldstr "G3_C1268::ClassMethod3909.15231()"
		ldstr "class G3_C1268`1<class BaseClass0> on type class G3_C1268`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1268`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1268`1<class BaseClass0>::ClassMethod3908()
		calli default string(class G3_C1268`1<class BaseClass0>)
		ldstr "G3_C1268::ClassMethod3908.15230()"
		ldstr "class G3_C1268`1<class BaseClass0> on type class G3_C1268`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1268`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1268`1<class BaseClass0>::Method3<object>()
		calli default string(class G3_C1268`1<class BaseClass0>)
		ldstr "G3_C1268::Method3.15228<System.Object>()"
		ldstr "class G3_C1268`1<class BaseClass0> on type class G3_C1268`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1268`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1268`1<class BaseClass0>::Method2<object>()
		calli default string(class G3_C1268`1<class BaseClass0>)
		ldstr "G3_C1268::Method2.15226<System.Object>()"
		ldstr "class G3_C1268`1<class BaseClass0> on type class G3_C1268`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1268`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1268`1<class BaseClass0>::Method1()
		calli default string(class G3_C1268`1<class BaseClass0>)
		ldstr "G3_C1268::Method1.15225()"
		ldstr "class G3_C1268`1<class BaseClass0> on type class G3_C1268`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1268`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1268`1<class BaseClass0>::Method0()
		calli default string(class G3_C1268`1<class BaseClass0>)
		ldstr "G3_C1268::Method0.15224()"
		ldstr "class G3_C1268`1<class BaseClass0> on type class G3_C1268`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1268`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1268`1<class BaseClass0>::ClassMethod1978<object>()
		calli default string(class G3_C1268`1<class BaseClass0>)
		ldstr "G2_C319::ClassMethod1978.7738<System.Object>()"
		ldstr "class G3_C1268`1<class BaseClass0> on type class G3_C1268`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1268`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1268`1<class BaseClass0>::ClassMethod1977()
		calli default string(class G3_C1268`1<class BaseClass0>)
		ldstr "G3_C1268::ClassMethod1977.MI.15234()"
		ldstr "class G3_C1268`1<class BaseClass0> on type class G3_C1268`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1268`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1268`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1268`1<class BaseClass0>)
		ldstr "G2_C319::Method6.7735<System.Object>()"
		ldstr "class G3_C1268`1<class BaseClass0> on type class G3_C1268`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1268`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1268`1<class BaseClass0>::Method5()
		calli default string(class G3_C1268`1<class BaseClass0>)
		ldstr "G2_C319::Method5.7733()"
		ldstr "class G3_C1268`1<class BaseClass0> on type class G3_C1268`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1268`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1268`1<class BaseClass0>::Method4()
		calli default string(class G3_C1268`1<class BaseClass0>)
		ldstr "G2_C319::Method4.7731()"
		ldstr "class G3_C1268`1<class BaseClass0> on type class G3_C1268`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1268`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1268`1<class BaseClass0>::Method7<object>()
		calli default string(class G3_C1268`1<class BaseClass0>)
		ldstr "G2_C319::Method7.7730<System.Object>()"
		ldstr "class G3_C1268`1<class BaseClass0> on type class G3_C1268`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1268`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1268`1<class BaseClass0>::ClassMethod1327<object>()
		calli default string(class G3_C1268`1<class BaseClass0>)
		ldstr "G2_C319::ClassMethod1327.MI.7739<System.Object>()"
		ldstr "class G3_C1268`1<class BaseClass0> on type class G3_C1268`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1268`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1268`1<class BaseClass0>::ClassMethod1326()
		calli default string(class G3_C1268`1<class BaseClass0>)
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G3_C1268`1<class BaseClass0> on type class G3_C1268`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method0()
		calli default string(class G3_C1268`1<class BaseClass0>)
		ldstr "G3_C1268::Method0.15224()"
		ldstr "IBase0 on type class G3_C1268`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method1()
		calli default string(class G3_C1268`1<class BaseClass0>)
		ldstr "G3_C1268::Method1.15225()"
		ldstr "IBase0 on type class G3_C1268`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method2<object>()
		calli default string(class G3_C1268`1<class BaseClass0>)
		ldstr "G3_C1268::Method2.MI.15227<System.Object>()"
		ldstr "IBase0 on type class G3_C1268`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method3<object>()
		calli default string(class G3_C1268`1<class BaseClass0>)
		ldstr "G3_C1268::Method3.MI.15229<System.Object>()"
		ldstr "IBase0 on type class G3_C1268`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G3_C1268`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C6`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass1,class BaseClass1>::ClassMethod1327<object>()
		calli default string(class G3_C1268`1<class BaseClass1>)
		ldstr "G2_C319::ClassMethod1327.MI.7739<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass1> on type class G3_C1268`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C6`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass1,class BaseClass1>::ClassMethod1326()
		calli default string(class G3_C1268`1<class BaseClass1>)
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass1> on type class G3_C1268`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C6`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1268`1<class BaseClass1>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass1> on type class G3_C1268`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1268`1<class BaseClass1>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1268`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1268`1<class BaseClass1>)
		ldstr "G2_C319::Method7.7730<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1268`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C319`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C319`2<class BaseClass1,class BaseClass1>::ClassMethod1978<object>()
		calli default string(class G3_C1268`1<class BaseClass1>)
		ldstr "G2_C319::ClassMethod1978.7738<System.Object>()"
		ldstr "class G2_C319`2<class BaseClass1,class BaseClass1> on type class G3_C1268`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C319`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C319`2<class BaseClass1,class BaseClass1>::ClassMethod1977()
		calli default string(class G3_C1268`1<class BaseClass1>)
		ldstr "G3_C1268::ClassMethod1977.MI.15234()"
		ldstr "class G2_C319`2<class BaseClass1,class BaseClass1> on type class G3_C1268`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C319`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C319`2<class BaseClass1,class BaseClass1>::Method6<object>()
		calli default string(class G3_C1268`1<class BaseClass1>)
		ldstr "G2_C319::Method6.7735<System.Object>()"
		ldstr "class G2_C319`2<class BaseClass1,class BaseClass1> on type class G3_C1268`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C319`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C319`2<class BaseClass1,class BaseClass1>::Method5()
		calli default string(class G3_C1268`1<class BaseClass1>)
		ldstr "G2_C319::Method5.7733()"
		ldstr "class G2_C319`2<class BaseClass1,class BaseClass1> on type class G3_C1268`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C319`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C319`2<class BaseClass1,class BaseClass1>::Method4()
		calli default string(class G3_C1268`1<class BaseClass1>)
		ldstr "G2_C319::Method4.7731()"
		ldstr "class G2_C319`2<class BaseClass1,class BaseClass1> on type class G3_C1268`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C319`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C319`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1268`1<class BaseClass1>)
		ldstr "G2_C319::Method7.7730<System.Object>()"
		ldstr "class G2_C319`2<class BaseClass1,class BaseClass1> on type class G3_C1268`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C319`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C319`2<class BaseClass1,class BaseClass1>::ClassMethod1327<object>()
		calli default string(class G3_C1268`1<class BaseClass1>)
		ldstr "G2_C319::ClassMethod1327.MI.7739<System.Object>()"
		ldstr "class G2_C319`2<class BaseClass1,class BaseClass1> on type class G3_C1268`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C319`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C319`2<class BaseClass1,class BaseClass1>::ClassMethod1326()
		calli default string(class G3_C1268`1<class BaseClass1>)
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G2_C319`2<class BaseClass1,class BaseClass1> on type class G3_C1268`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G3_C1268`1<class BaseClass1>)
		ldstr "G2_C319::Method4.MI.7732()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1268`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G3_C1268`1<class BaseClass1>)
		ldstr "G2_C319::Method5.MI.7734()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1268`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G3_C1268`1<class BaseClass1>)
		ldstr "G2_C319::Method6.MI.7736<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1268`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G3_C1268`1<class BaseClass1>)
		ldstr "G2_C319::Method4.MI.7732()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1268`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G3_C1268`1<class BaseClass1>)
		ldstr "G2_C319::Method5.MI.7734()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1268`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1268`1<class BaseClass1>)
		ldstr "G2_C319::Method6.MI.7736<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1268`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1268`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1268`1<class BaseClass1>::ClassMethod3911<object>()
		calli default string(class G3_C1268`1<class BaseClass1>)
		ldstr "G3_C1268::ClassMethod3911.15233<System.Object>()"
		ldstr "class G3_C1268`1<class BaseClass1> on type class G3_C1268`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1268`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1268`1<class BaseClass1>::ClassMethod3910<object>()
		calli default string(class G3_C1268`1<class BaseClass1>)
		ldstr "G3_C1268::ClassMethod3910.15232<System.Object>()"
		ldstr "class G3_C1268`1<class BaseClass1> on type class G3_C1268`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1268`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1268`1<class BaseClass1>::ClassMethod3909()
		calli default string(class G3_C1268`1<class BaseClass1>)
		ldstr "G3_C1268::ClassMethod3909.15231()"
		ldstr "class G3_C1268`1<class BaseClass1> on type class G3_C1268`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1268`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1268`1<class BaseClass1>::ClassMethod3908()
		calli default string(class G3_C1268`1<class BaseClass1>)
		ldstr "G3_C1268::ClassMethod3908.15230()"
		ldstr "class G3_C1268`1<class BaseClass1> on type class G3_C1268`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1268`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1268`1<class BaseClass1>::Method3<object>()
		calli default string(class G3_C1268`1<class BaseClass1>)
		ldstr "G3_C1268::Method3.15228<System.Object>()"
		ldstr "class G3_C1268`1<class BaseClass1> on type class G3_C1268`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1268`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1268`1<class BaseClass1>::Method2<object>()
		calli default string(class G3_C1268`1<class BaseClass1>)
		ldstr "G3_C1268::Method2.15226<System.Object>()"
		ldstr "class G3_C1268`1<class BaseClass1> on type class G3_C1268`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1268`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1268`1<class BaseClass1>::Method1()
		calli default string(class G3_C1268`1<class BaseClass1>)
		ldstr "G3_C1268::Method1.15225()"
		ldstr "class G3_C1268`1<class BaseClass1> on type class G3_C1268`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1268`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1268`1<class BaseClass1>::Method0()
		calli default string(class G3_C1268`1<class BaseClass1>)
		ldstr "G3_C1268::Method0.15224()"
		ldstr "class G3_C1268`1<class BaseClass1> on type class G3_C1268`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1268`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1268`1<class BaseClass1>::ClassMethod1978<object>()
		calli default string(class G3_C1268`1<class BaseClass1>)
		ldstr "G2_C319::ClassMethod1978.7738<System.Object>()"
		ldstr "class G3_C1268`1<class BaseClass1> on type class G3_C1268`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1268`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1268`1<class BaseClass1>::ClassMethod1977()
		calli default string(class G3_C1268`1<class BaseClass1>)
		ldstr "G3_C1268::ClassMethod1977.MI.15234()"
		ldstr "class G3_C1268`1<class BaseClass1> on type class G3_C1268`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1268`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1268`1<class BaseClass1>::Method6<object>()
		calli default string(class G3_C1268`1<class BaseClass1>)
		ldstr "G2_C319::Method6.7735<System.Object>()"
		ldstr "class G3_C1268`1<class BaseClass1> on type class G3_C1268`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1268`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1268`1<class BaseClass1>::Method5()
		calli default string(class G3_C1268`1<class BaseClass1>)
		ldstr "G2_C319::Method5.7733()"
		ldstr "class G3_C1268`1<class BaseClass1> on type class G3_C1268`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1268`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1268`1<class BaseClass1>::Method4()
		calli default string(class G3_C1268`1<class BaseClass1>)
		ldstr "G2_C319::Method4.7731()"
		ldstr "class G3_C1268`1<class BaseClass1> on type class G3_C1268`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1268`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1268`1<class BaseClass1>::Method7<object>()
		calli default string(class G3_C1268`1<class BaseClass1>)
		ldstr "G2_C319::Method7.7730<System.Object>()"
		ldstr "class G3_C1268`1<class BaseClass1> on type class G3_C1268`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1268`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1268`1<class BaseClass1>::ClassMethod1327<object>()
		calli default string(class G3_C1268`1<class BaseClass1>)
		ldstr "G2_C319::ClassMethod1327.MI.7739<System.Object>()"
		ldstr "class G3_C1268`1<class BaseClass1> on type class G3_C1268`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1268`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1268`1<class BaseClass1>::ClassMethod1326()
		calli default string(class G3_C1268`1<class BaseClass1>)
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G3_C1268`1<class BaseClass1> on type class G3_C1268`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method0()
		calli default string(class G3_C1268`1<class BaseClass1>)
		ldstr "G3_C1268::Method0.15224()"
		ldstr "IBase0 on type class G3_C1268`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method1()
		calli default string(class G3_C1268`1<class BaseClass1>)
		ldstr "G3_C1268::Method1.15225()"
		ldstr "IBase0 on type class G3_C1268`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method2<object>()
		calli default string(class G3_C1268`1<class BaseClass1>)
		ldstr "G3_C1268::Method2.MI.15227<System.Object>()"
		ldstr "IBase0 on type class G3_C1268`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method3<object>()
		calli default string(class G3_C1268`1<class BaseClass1>)
		ldstr "G3_C1268::Method3.MI.15229<System.Object>()"
		ldstr "IBase0 on type class G3_C1268`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G1_C6`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C6`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass0,class BaseClass0>::ClassMethod1327<object>()
		calli default string(class G1_C6`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass0,class BaseClass0> on type class G1_C6`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C6`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass0,class BaseClass0>::ClassMethod1326()
		calli default string(class G1_C6`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class BaseClass0,class BaseClass0> on type class G1_C6`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C6`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G1_C6`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass0,class BaseClass0> on type class G1_C6`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G1_C6`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G1_C6`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G1_C6`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G1_C6`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G1_C6`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G1_C6`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G1_C6`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G1_C6`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G1_C6`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C6`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass0,class BaseClass1>::ClassMethod1327<object>()
		calli default string(class G1_C6`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass0,class BaseClass1> on type class G1_C6`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C6`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass0,class BaseClass1>::ClassMethod1326()
		calli default string(class G1_C6`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class BaseClass0,class BaseClass1> on type class G1_C6`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C6`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G1_C6`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass0,class BaseClass1> on type class G1_C6`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G1_C6`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G1_C6`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G1_C6`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G1_C6`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G1_C6`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G1_C6`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G1_C6`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G1_C6`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G1_C6`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C6`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass1,class BaseClass0>::ClassMethod1327<object>()
		calli default string(class G1_C6`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass0> on type class G1_C6`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C6`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass1,class BaseClass0>::ClassMethod1326()
		calli default string(class G1_C6`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass0> on type class G1_C6`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C6`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G1_C6`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass0> on type class G1_C6`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G1_C6`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G1_C6`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G1_C6`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G1_C6`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G1_C6`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C6`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass1,class BaseClass1>::ClassMethod1327<object>()
		calli default string(class G1_C6`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass1> on type class G1_C6`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C6`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass1,class BaseClass1>::ClassMethod1326()
		calli default string(class G1_C6`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass1> on type class G1_C6`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C6`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G1_C6`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass1> on type class G1_C6`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G1_C6`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G1_C6`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G1_C6`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G1_C6`2<class BaseClass1,class BaseClass1>"
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
		call void Generated796::MethodCallingTest()
		call void Generated796::ConstrainedCallsTest()
		call void Generated796::StructConstrainedInterfaceCallsTest()
		call void Generated796::CalliTest()
		ldc.i4 100
		ret
	}
}
