// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) .ver 4:0:0:0 }
.assembly extern TestFramework { .publickeytoken = ( B0 3F 5F 7F 11 D5 0A 3A )  }

//TYPES IN FORWARDER ASSEMBLIES:

//TEST ASSEMBLY:
.assembly Generated806 { .hash algorithm 0x00008004 }
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
.class public G3_C1278`1<T0> 
		extends class G2_C323`2<class BaseClass0,!T0>
		implements IBase0 
{
	.method public hidebysig newslot virtual instance string Method0() cil managed noinlining { 
		ldstr "G3_C1278::Method0.15286()"
		ret
	}
	.method public hidebysig virtual instance string Method1() cil managed noinlining { 
		ldstr "G3_C1278::Method1.15287()"
		ret
	}
	.method public hidebysig virtual instance string Method2<M0>() cil managed noinlining { 
		ldstr "G3_C1278::Method2.15288<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase0.Method2'<M0>() cil managed noinlining { 
		.override method instance string IBase0::Method2<[1]>()
		ldstr "G3_C1278::Method2.MI.15289<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string Method3<M0>() cil managed noinlining { 
		ldstr "G3_C1278::Method3.15290<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase0.Method3'<M0>() cil managed noinlining { 
		.override method instance string IBase0::Method3<[1]>()
		ldstr "G3_C1278::Method3.MI.15291<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod3930() cil managed noinlining { 
		ldstr "G3_C1278::ClassMethod3930.15292()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C323<class BaseClass0,T0>.ClassMethod1985'() cil managed noinlining { 
		.override method instance string class G2_C323`2<class BaseClass0,!T0>::ClassMethod1985()
		ldstr "G3_C1278::ClassMethod1985.MI.15293()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C323<class BaseClass0,T0>.ClassMethod1987'<M0>() cil managed noinlining { 
		.override method instance string class G2_C323`2<class BaseClass0,!T0>::ClassMethod1987<[1]>()
		ldstr "G3_C1278::ClassMethod1987.MI.15294<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C323<class BaseClass0,T0>.ClassMethod1988'<M0>() cil managed noinlining { 
		.override method instance string class G2_C323`2<class BaseClass0,!T0>::ClassMethod1988<[1]>()
		ldstr "G3_C1278::ClassMethod1988.MI.15295<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class G2_C323`2<class BaseClass0,!T0>::.ctor()
		ret
	}
}
.class public G2_C323`2<T0, T1> 
		extends class G1_C6`2<!T1,!T1>
		implements IBase0, class IBase2`2<class BaseClass0,class BaseClass1> 
{
	.method public hidebysig newslot virtual instance string Method0() cil managed noinlining { 
		ldstr "G2_C323::Method0.7761()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase0.Method0'() cil managed noinlining { 
		.override method instance string IBase0::Method0()
		ldstr "G2_C323::Method0.MI.7762()"
		ret
	}
	.method public hidebysig virtual instance string Method1() cil managed noinlining { 
		ldstr "G2_C323::Method1.7763()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase0.Method1'() cil managed noinlining { 
		.override method instance string IBase0::Method1()
		ldstr "G2_C323::Method1.MI.7764()"
		ret
	}
	.method public hidebysig virtual instance string Method2<M0>() cil managed noinlining { 
		ldstr "G2_C323::Method2.7765<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase0.Method2'<M0>() cil managed noinlining { 
		.override method instance string IBase0::Method2<[1]>()
		ldstr "G2_C323::Method2.MI.7766<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string Method3<M0>() cil managed noinlining { 
		ldstr "G2_C323::Method3.7767<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase0.Method3'<M0>() cil managed noinlining { 
		.override method instance string IBase0::Method3<[1]>()
		ldstr "G2_C323::Method3.MI.7768<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G2_C323::Method7.7769<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1985() cil managed noinlining { 
		ldstr "G2_C323::ClassMethod1985.7770()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1986() cil managed noinlining { 
		ldstr "G2_C323::ClassMethod1986.7771()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1987<M0>() cil managed noinlining { 
		ldstr "G2_C323::ClassMethod1987.7772<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1988<M0>() cil managed noinlining { 
		ldstr "G2_C323::ClassMethod1988.7773<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C6<T1,T1>.ClassMethod1326'() cil managed noinlining { 
		.override method instance string class G1_C6`2<!T1,!T1>::ClassMethod1326()
		ldstr "G2_C323::ClassMethod1326.MI.7774()"
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class G1_C6`2<!T1,!T1>::.ctor()
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
.class public auto ansi beforefieldinit Generated806 {
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
	.method static void M.G3_C1278.T<T0,(class G3_C1278`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 17
		.locals init (string[] actualResults)
		ldc.i4.s    12
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1278.T<T0,(class G3_C1278`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 12
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1278`1<!!T0>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1278`1<!!T0>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1278`1<!!T0>::ClassMethod1985()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1278`1<!!T0>::ClassMethod1986()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1278`1<!!T0>::ClassMethod1987<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1278`1<!!T0>::ClassMethod1988<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1278`1<!!T0>::ClassMethod3930()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1278`1<!!T0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1278`1<!!T0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1278`1<!!T0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1278`1<!!T0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1278`1<!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1278.A<(class G3_C1278`1<class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 17
		.locals init (string[] actualResults)
		ldc.i4.s    12
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1278.A<(class G3_C1278`1<class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 12
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1278`1<class BaseClass0>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1278`1<class BaseClass0>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1278`1<class BaseClass0>::ClassMethod1985()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1278`1<class BaseClass0>::ClassMethod1986()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1278`1<class BaseClass0>::ClassMethod1987<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1278`1<class BaseClass0>::ClassMethod1988<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1278`1<class BaseClass0>::ClassMethod3930()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1278`1<class BaseClass0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1278`1<class BaseClass0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1278`1<class BaseClass0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1278`1<class BaseClass0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1278`1<class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1278.B<(class G3_C1278`1<class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 17
		.locals init (string[] actualResults)
		ldc.i4.s    12
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1278.B<(class G3_C1278`1<class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 12
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1278`1<class BaseClass1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1278`1<class BaseClass1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1278`1<class BaseClass1>::ClassMethod1985()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1278`1<class BaseClass1>::ClassMethod1986()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1278`1<class BaseClass1>::ClassMethod1987<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1278`1<class BaseClass1>::ClassMethod1988<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1278`1<class BaseClass1>::ClassMethod3930()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1278`1<class BaseClass1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1278`1<class BaseClass1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1278`1<class BaseClass1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1278`1<class BaseClass1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1278`1<class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C323.T.T<T0,T1,(class G2_C323`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C323.T.T<T0,T1,(class G2_C323`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C323`2<!!T0,!!T1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C323`2<!!T0,!!T1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C323`2<!!T0,!!T1>::ClassMethod1985()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C323`2<!!T0,!!T1>::ClassMethod1986()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C323`2<!!T0,!!T1>::ClassMethod1987<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C323`2<!!T0,!!T1>::ClassMethod1988<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C323`2<!!T0,!!T1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C323`2<!!T0,!!T1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C323`2<!!T0,!!T1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C323`2<!!T0,!!T1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C323`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C323.A.T<T1,(class G2_C323`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C323.A.T<T1,(class G2_C323`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C323`2<class BaseClass0,!!T1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C323`2<class BaseClass0,!!T1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C323`2<class BaseClass0,!!T1>::ClassMethod1985()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C323`2<class BaseClass0,!!T1>::ClassMethod1986()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C323`2<class BaseClass0,!!T1>::ClassMethod1987<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C323`2<class BaseClass0,!!T1>::ClassMethod1988<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C323`2<class BaseClass0,!!T1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C323`2<class BaseClass0,!!T1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C323`2<class BaseClass0,!!T1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C323`2<class BaseClass0,!!T1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C323`2<class BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C323.A.A<(class G2_C323`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C323.A.A<(class G2_C323`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C323`2<class BaseClass0,class BaseClass0>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C323`2<class BaseClass0,class BaseClass0>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C323`2<class BaseClass0,class BaseClass0>::ClassMethod1985()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C323`2<class BaseClass0,class BaseClass0>::ClassMethod1986()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C323`2<class BaseClass0,class BaseClass0>::ClassMethod1987<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C323`2<class BaseClass0,class BaseClass0>::ClassMethod1988<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C323`2<class BaseClass0,class BaseClass0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C323`2<class BaseClass0,class BaseClass0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C323`2<class BaseClass0,class BaseClass0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C323`2<class BaseClass0,class BaseClass0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C323`2<class BaseClass0,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C323.A.B<(class G2_C323`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C323.A.B<(class G2_C323`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C323`2<class BaseClass0,class BaseClass1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C323`2<class BaseClass0,class BaseClass1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C323`2<class BaseClass0,class BaseClass1>::ClassMethod1985()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C323`2<class BaseClass0,class BaseClass1>::ClassMethod1986()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C323`2<class BaseClass0,class BaseClass1>::ClassMethod1987<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C323`2<class BaseClass0,class BaseClass1>::ClassMethod1988<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C323`2<class BaseClass0,class BaseClass1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C323`2<class BaseClass0,class BaseClass1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C323`2<class BaseClass0,class BaseClass1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C323`2<class BaseClass0,class BaseClass1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C323`2<class BaseClass0,class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C323.B.T<T1,(class G2_C323`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C323.B.T<T1,(class G2_C323`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C323`2<class BaseClass1,!!T1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C323`2<class BaseClass1,!!T1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C323`2<class BaseClass1,!!T1>::ClassMethod1985()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C323`2<class BaseClass1,!!T1>::ClassMethod1986()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C323`2<class BaseClass1,!!T1>::ClassMethod1987<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C323`2<class BaseClass1,!!T1>::ClassMethod1988<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C323`2<class BaseClass1,!!T1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C323`2<class BaseClass1,!!T1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C323`2<class BaseClass1,!!T1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C323`2<class BaseClass1,!!T1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C323`2<class BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C323.B.A<(class G2_C323`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C323.B.A<(class G2_C323`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C323`2<class BaseClass1,class BaseClass0>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C323`2<class BaseClass1,class BaseClass0>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C323`2<class BaseClass1,class BaseClass0>::ClassMethod1985()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C323`2<class BaseClass1,class BaseClass0>::ClassMethod1986()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C323`2<class BaseClass1,class BaseClass0>::ClassMethod1987<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C323`2<class BaseClass1,class BaseClass0>::ClassMethod1988<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C323`2<class BaseClass1,class BaseClass0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C323`2<class BaseClass1,class BaseClass0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C323`2<class BaseClass1,class BaseClass0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C323`2<class BaseClass1,class BaseClass0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C323`2<class BaseClass1,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C323.B.B<(class G2_C323`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C323.B.B<(class G2_C323`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C323`2<class BaseClass1,class BaseClass1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C323`2<class BaseClass1,class BaseClass1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C323`2<class BaseClass1,class BaseClass1>::ClassMethod1985()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C323`2<class BaseClass1,class BaseClass1>::ClassMethod1986()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C323`2<class BaseClass1,class BaseClass1>::ClassMethod1987<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C323`2<class BaseClass1,class BaseClass1>::ClassMethod1988<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C323`2<class BaseClass1,class BaseClass1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C323`2<class BaseClass1,class BaseClass1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C323`2<class BaseClass1,class BaseClass1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C323`2<class BaseClass1,class BaseClass1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C323`2<class BaseClass1,class BaseClass1>::Method7<object>()
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
	.method public hidebysig static void MethodCallingTest() cil managed
	{
		.maxstack  10
		.locals init (object V_0)
		ldstr "========================== Method Calling Test =========================="
		call void [mscorlib]System.Console::WriteLine(string)
		newobj instance void class G3_C1278`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C6`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C6`2<class BaseClass0,class BaseClass0>::ClassMethod1327<object>()
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass0,class BaseClass0> on type class G3_C1278`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C6`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C6`2<class BaseClass0,class BaseClass0>::ClassMethod1326()
		ldstr "G2_C323::ClassMethod1326.MI.7774()"
		ldstr "class G1_C6`2<class BaseClass0,class BaseClass0> on type class G3_C1278`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C6`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C6`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass0,class BaseClass0> on type class G3_C1278`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G3_C1278`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1278`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C323::Method7.7769<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1278`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1278`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C323`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C323`2<class BaseClass0,class BaseClass0>::ClassMethod1988<object>()
		ldstr "G3_C1278::ClassMethod1988.MI.15295<System.Object>()"
		ldstr "class G2_C323`2<class BaseClass0,class BaseClass0> on type class G3_C1278`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C323`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C323`2<class BaseClass0,class BaseClass0>::ClassMethod1987<object>()
		ldstr "G3_C1278::ClassMethod1987.MI.15294<System.Object>()"
		ldstr "class G2_C323`2<class BaseClass0,class BaseClass0> on type class G3_C1278`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C323`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C323`2<class BaseClass0,class BaseClass0>::ClassMethod1986()
		ldstr "G2_C323::ClassMethod1986.7771()"
		ldstr "class G2_C323`2<class BaseClass0,class BaseClass0> on type class G3_C1278`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C323`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C323`2<class BaseClass0,class BaseClass0>::ClassMethod1985()
		ldstr "G3_C1278::ClassMethod1985.MI.15293()"
		ldstr "class G2_C323`2<class BaseClass0,class BaseClass0> on type class G3_C1278`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C323`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C323`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C323::Method7.7769<System.Object>()"
		ldstr "class G2_C323`2<class BaseClass0,class BaseClass0> on type class G3_C1278`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C323`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C323`2<class BaseClass0,class BaseClass0>::Method3<object>()
		ldstr "G2_C323::Method3.7767<System.Object>()"
		ldstr "class G2_C323`2<class BaseClass0,class BaseClass0> on type class G3_C1278`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C323`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C323`2<class BaseClass0,class BaseClass0>::Method2<object>()
		ldstr "G3_C1278::Method2.15288<System.Object>()"
		ldstr "class G2_C323`2<class BaseClass0,class BaseClass0> on type class G3_C1278`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C323`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C323`2<class BaseClass0,class BaseClass0>::Method1()
		ldstr "G3_C1278::Method1.15287()"
		ldstr "class G2_C323`2<class BaseClass0,class BaseClass0> on type class G3_C1278`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C323`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C323`2<class BaseClass0,class BaseClass0>::Method0()
		ldstr "G2_C323::Method0.7761()"
		ldstr "class G2_C323`2<class BaseClass0,class BaseClass0> on type class G3_C1278`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C323`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C323`2<class BaseClass0,class BaseClass0>::ClassMethod1327<object>()
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G2_C323`2<class BaseClass0,class BaseClass0> on type class G3_C1278`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C323`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C323`2<class BaseClass0,class BaseClass0>::ClassMethod1326()
		ldstr "G2_C323::ClassMethod1326.MI.7774()"
		ldstr "class G2_C323`2<class BaseClass0,class BaseClass0> on type class G3_C1278`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string IBase0::Method0()
		ldstr "G3_C1278::Method0.15286()"
		ldstr "IBase0 on type class G3_C1278`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method1()
		ldstr "G3_C1278::Method1.15287()"
		ldstr "IBase0 on type class G3_C1278`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method2<object>()
		ldstr "G3_C1278::Method2.MI.15289<System.Object>()"
		ldstr "IBase0 on type class G3_C1278`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method3<object>()
		ldstr "G3_C1278::Method3.MI.15291<System.Object>()"
		ldstr "IBase0 on type class G3_C1278`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G3_C1278`1<class BaseClass0>
		callvirt instance string class G3_C1278`1<class BaseClass0>::ClassMethod3930()
		ldstr "G3_C1278::ClassMethod3930.15292()"
		ldstr "class G3_C1278`1<class BaseClass0> on type class G3_C1278`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1278`1<class BaseClass0>
		callvirt instance string class G3_C1278`1<class BaseClass0>::Method3<object>()
		ldstr "G3_C1278::Method3.15290<System.Object>()"
		ldstr "class G3_C1278`1<class BaseClass0> on type class G3_C1278`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1278`1<class BaseClass0>
		callvirt instance string class G3_C1278`1<class BaseClass0>::Method2<object>()
		ldstr "G3_C1278::Method2.15288<System.Object>()"
		ldstr "class G3_C1278`1<class BaseClass0> on type class G3_C1278`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1278`1<class BaseClass0>
		callvirt instance string class G3_C1278`1<class BaseClass0>::Method1()
		ldstr "G3_C1278::Method1.15287()"
		ldstr "class G3_C1278`1<class BaseClass0> on type class G3_C1278`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1278`1<class BaseClass0>
		callvirt instance string class G3_C1278`1<class BaseClass0>::Method0()
		ldstr "G3_C1278::Method0.15286()"
		ldstr "class G3_C1278`1<class BaseClass0> on type class G3_C1278`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1278`1<class BaseClass0>
		callvirt instance string class G3_C1278`1<class BaseClass0>::ClassMethod1988<object>()
		ldstr "G3_C1278::ClassMethod1988.MI.15295<System.Object>()"
		ldstr "class G3_C1278`1<class BaseClass0> on type class G3_C1278`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1278`1<class BaseClass0>
		callvirt instance string class G3_C1278`1<class BaseClass0>::ClassMethod1987<object>()
		ldstr "G3_C1278::ClassMethod1987.MI.15294<System.Object>()"
		ldstr "class G3_C1278`1<class BaseClass0> on type class G3_C1278`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1278`1<class BaseClass0>
		callvirt instance string class G3_C1278`1<class BaseClass0>::ClassMethod1986()
		ldstr "G2_C323::ClassMethod1986.7771()"
		ldstr "class G3_C1278`1<class BaseClass0> on type class G3_C1278`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1278`1<class BaseClass0>
		callvirt instance string class G3_C1278`1<class BaseClass0>::ClassMethod1985()
		ldstr "G3_C1278::ClassMethod1985.MI.15293()"
		ldstr "class G3_C1278`1<class BaseClass0> on type class G3_C1278`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1278`1<class BaseClass0>
		callvirt instance string class G3_C1278`1<class BaseClass0>::Method7<object>()
		ldstr "G2_C323::Method7.7769<System.Object>()"
		ldstr "class G3_C1278`1<class BaseClass0> on type class G3_C1278`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1278`1<class BaseClass0>
		callvirt instance string class G3_C1278`1<class BaseClass0>::ClassMethod1327<object>()
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G3_C1278`1<class BaseClass0> on type class G3_C1278`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1278`1<class BaseClass0>
		callvirt instance string class G3_C1278`1<class BaseClass0>::ClassMethod1326()
		ldstr "G2_C323::ClassMethod1326.MI.7774()"
		ldstr "class G3_C1278`1<class BaseClass0> on type class G3_C1278`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G3_C1278`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C6`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C6`2<class BaseClass1,class BaseClass1>::ClassMethod1327<object>()
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass1> on type class G3_C1278`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C6`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C6`2<class BaseClass1,class BaseClass1>::ClassMethod1326()
		ldstr "G2_C323::ClassMethod1326.MI.7774()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass1> on type class G3_C1278`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C6`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C6`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass1> on type class G3_C1278`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1278`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C323::Method7.7769<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1278`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C323`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C323`2<class BaseClass0,class BaseClass1>::ClassMethod1988<object>()
		ldstr "G3_C1278::ClassMethod1988.MI.15295<System.Object>()"
		ldstr "class G2_C323`2<class BaseClass0,class BaseClass1> on type class G3_C1278`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C323`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C323`2<class BaseClass0,class BaseClass1>::ClassMethod1987<object>()
		ldstr "G3_C1278::ClassMethod1987.MI.15294<System.Object>()"
		ldstr "class G2_C323`2<class BaseClass0,class BaseClass1> on type class G3_C1278`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C323`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C323`2<class BaseClass0,class BaseClass1>::ClassMethod1986()
		ldstr "G2_C323::ClassMethod1986.7771()"
		ldstr "class G2_C323`2<class BaseClass0,class BaseClass1> on type class G3_C1278`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C323`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C323`2<class BaseClass0,class BaseClass1>::ClassMethod1985()
		ldstr "G3_C1278::ClassMethod1985.MI.15293()"
		ldstr "class G2_C323`2<class BaseClass0,class BaseClass1> on type class G3_C1278`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C323`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C323`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C323::Method7.7769<System.Object>()"
		ldstr "class G2_C323`2<class BaseClass0,class BaseClass1> on type class G3_C1278`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C323`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C323`2<class BaseClass0,class BaseClass1>::Method3<object>()
		ldstr "G2_C323::Method3.7767<System.Object>()"
		ldstr "class G2_C323`2<class BaseClass0,class BaseClass1> on type class G3_C1278`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C323`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C323`2<class BaseClass0,class BaseClass1>::Method2<object>()
		ldstr "G3_C1278::Method2.15288<System.Object>()"
		ldstr "class G2_C323`2<class BaseClass0,class BaseClass1> on type class G3_C1278`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C323`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C323`2<class BaseClass0,class BaseClass1>::Method1()
		ldstr "G3_C1278::Method1.15287()"
		ldstr "class G2_C323`2<class BaseClass0,class BaseClass1> on type class G3_C1278`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C323`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C323`2<class BaseClass0,class BaseClass1>::Method0()
		ldstr "G2_C323::Method0.7761()"
		ldstr "class G2_C323`2<class BaseClass0,class BaseClass1> on type class G3_C1278`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C323`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C323`2<class BaseClass0,class BaseClass1>::ClassMethod1327<object>()
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G2_C323`2<class BaseClass0,class BaseClass1> on type class G3_C1278`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C323`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C323`2<class BaseClass0,class BaseClass1>::ClassMethod1326()
		ldstr "G2_C323::ClassMethod1326.MI.7774()"
		ldstr "class G2_C323`2<class BaseClass0,class BaseClass1> on type class G3_C1278`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string IBase0::Method0()
		ldstr "G3_C1278::Method0.15286()"
		ldstr "IBase0 on type class G3_C1278`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method1()
		ldstr "G3_C1278::Method1.15287()"
		ldstr "IBase0 on type class G3_C1278`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method2<object>()
		ldstr "G3_C1278::Method2.MI.15289<System.Object>()"
		ldstr "IBase0 on type class G3_C1278`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method3<object>()
		ldstr "G3_C1278::Method3.MI.15291<System.Object>()"
		ldstr "IBase0 on type class G3_C1278`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G3_C1278`1<class BaseClass1>
		callvirt instance string class G3_C1278`1<class BaseClass1>::ClassMethod3930()
		ldstr "G3_C1278::ClassMethod3930.15292()"
		ldstr "class G3_C1278`1<class BaseClass1> on type class G3_C1278`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1278`1<class BaseClass1>
		callvirt instance string class G3_C1278`1<class BaseClass1>::Method3<object>()
		ldstr "G3_C1278::Method3.15290<System.Object>()"
		ldstr "class G3_C1278`1<class BaseClass1> on type class G3_C1278`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1278`1<class BaseClass1>
		callvirt instance string class G3_C1278`1<class BaseClass1>::Method2<object>()
		ldstr "G3_C1278::Method2.15288<System.Object>()"
		ldstr "class G3_C1278`1<class BaseClass1> on type class G3_C1278`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1278`1<class BaseClass1>
		callvirt instance string class G3_C1278`1<class BaseClass1>::Method1()
		ldstr "G3_C1278::Method1.15287()"
		ldstr "class G3_C1278`1<class BaseClass1> on type class G3_C1278`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1278`1<class BaseClass1>
		callvirt instance string class G3_C1278`1<class BaseClass1>::Method0()
		ldstr "G3_C1278::Method0.15286()"
		ldstr "class G3_C1278`1<class BaseClass1> on type class G3_C1278`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1278`1<class BaseClass1>
		callvirt instance string class G3_C1278`1<class BaseClass1>::ClassMethod1988<object>()
		ldstr "G3_C1278::ClassMethod1988.MI.15295<System.Object>()"
		ldstr "class G3_C1278`1<class BaseClass1> on type class G3_C1278`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1278`1<class BaseClass1>
		callvirt instance string class G3_C1278`1<class BaseClass1>::ClassMethod1987<object>()
		ldstr "G3_C1278::ClassMethod1987.MI.15294<System.Object>()"
		ldstr "class G3_C1278`1<class BaseClass1> on type class G3_C1278`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1278`1<class BaseClass1>
		callvirt instance string class G3_C1278`1<class BaseClass1>::ClassMethod1986()
		ldstr "G2_C323::ClassMethod1986.7771()"
		ldstr "class G3_C1278`1<class BaseClass1> on type class G3_C1278`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1278`1<class BaseClass1>
		callvirt instance string class G3_C1278`1<class BaseClass1>::ClassMethod1985()
		ldstr "G3_C1278::ClassMethod1985.MI.15293()"
		ldstr "class G3_C1278`1<class BaseClass1> on type class G3_C1278`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1278`1<class BaseClass1>
		callvirt instance string class G3_C1278`1<class BaseClass1>::Method7<object>()
		ldstr "G2_C323::Method7.7769<System.Object>()"
		ldstr "class G3_C1278`1<class BaseClass1> on type class G3_C1278`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1278`1<class BaseClass1>
		callvirt instance string class G3_C1278`1<class BaseClass1>::ClassMethod1327<object>()
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G3_C1278`1<class BaseClass1> on type class G3_C1278`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1278`1<class BaseClass1>
		callvirt instance string class G3_C1278`1<class BaseClass1>::ClassMethod1326()
		ldstr "G2_C323::ClassMethod1326.MI.7774()"
		ldstr "class G3_C1278`1<class BaseClass1> on type class G3_C1278`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C323`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C6`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C6`2<class BaseClass0,class BaseClass0>::ClassMethod1327<object>()
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass0,class BaseClass0> on type class G2_C323`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C6`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C6`2<class BaseClass0,class BaseClass0>::ClassMethod1326()
		ldstr "G2_C323::ClassMethod1326.MI.7774()"
		ldstr "class G1_C6`2<class BaseClass0,class BaseClass0> on type class G2_C323`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C6`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C6`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass0,class BaseClass0> on type class G2_C323`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G2_C323`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C323`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C323::Method7.7769<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C323`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C323`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C323`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C323`2<class BaseClass0,class BaseClass0>::ClassMethod1988<object>()
		ldstr "G2_C323::ClassMethod1988.7773<System.Object>()"
		ldstr "class G2_C323`2<class BaseClass0,class BaseClass0> on type class G2_C323`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C323`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C323`2<class BaseClass0,class BaseClass0>::ClassMethod1987<object>()
		ldstr "G2_C323::ClassMethod1987.7772<System.Object>()"
		ldstr "class G2_C323`2<class BaseClass0,class BaseClass0> on type class G2_C323`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C323`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C323`2<class BaseClass0,class BaseClass0>::ClassMethod1986()
		ldstr "G2_C323::ClassMethod1986.7771()"
		ldstr "class G2_C323`2<class BaseClass0,class BaseClass0> on type class G2_C323`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C323`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C323`2<class BaseClass0,class BaseClass0>::ClassMethod1985()
		ldstr "G2_C323::ClassMethod1985.7770()"
		ldstr "class G2_C323`2<class BaseClass0,class BaseClass0> on type class G2_C323`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C323`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C323`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C323::Method7.7769<System.Object>()"
		ldstr "class G2_C323`2<class BaseClass0,class BaseClass0> on type class G2_C323`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C323`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C323`2<class BaseClass0,class BaseClass0>::Method3<object>()
		ldstr "G2_C323::Method3.7767<System.Object>()"
		ldstr "class G2_C323`2<class BaseClass0,class BaseClass0> on type class G2_C323`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C323`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C323`2<class BaseClass0,class BaseClass0>::Method2<object>()
		ldstr "G2_C323::Method2.7765<System.Object>()"
		ldstr "class G2_C323`2<class BaseClass0,class BaseClass0> on type class G2_C323`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C323`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C323`2<class BaseClass0,class BaseClass0>::Method1()
		ldstr "G2_C323::Method1.7763()"
		ldstr "class G2_C323`2<class BaseClass0,class BaseClass0> on type class G2_C323`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C323`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C323`2<class BaseClass0,class BaseClass0>::Method0()
		ldstr "G2_C323::Method0.7761()"
		ldstr "class G2_C323`2<class BaseClass0,class BaseClass0> on type class G2_C323`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C323`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C323`2<class BaseClass0,class BaseClass0>::ClassMethod1327<object>()
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G2_C323`2<class BaseClass0,class BaseClass0> on type class G2_C323`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C323`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C323`2<class BaseClass0,class BaseClass0>::ClassMethod1326()
		ldstr "G2_C323::ClassMethod1326.MI.7774()"
		ldstr "class G2_C323`2<class BaseClass0,class BaseClass0> on type class G2_C323`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string IBase0::Method0()
		ldstr "G2_C323::Method0.MI.7762()"
		ldstr "IBase0 on type class G2_C323`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method1()
		ldstr "G2_C323::Method1.MI.7764()"
		ldstr "IBase0 on type class G2_C323`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method2<object>()
		ldstr "G2_C323::Method2.MI.7766<System.Object>()"
		ldstr "IBase0 on type class G2_C323`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method3<object>()
		ldstr "G2_C323::Method3.MI.7768<System.Object>()"
		ldstr "IBase0 on type class G2_C323`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C323`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C6`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C6`2<class BaseClass1,class BaseClass1>::ClassMethod1327<object>()
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass1> on type class G2_C323`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C6`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C6`2<class BaseClass1,class BaseClass1>::ClassMethod1326()
		ldstr "G2_C323::ClassMethod1326.MI.7774()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass1> on type class G2_C323`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C6`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C6`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass1> on type class G2_C323`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C323`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C323::Method7.7769<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C323`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C323`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C323`2<class BaseClass0,class BaseClass1>::ClassMethod1988<object>()
		ldstr "G2_C323::ClassMethod1988.7773<System.Object>()"
		ldstr "class G2_C323`2<class BaseClass0,class BaseClass1> on type class G2_C323`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C323`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C323`2<class BaseClass0,class BaseClass1>::ClassMethod1987<object>()
		ldstr "G2_C323::ClassMethod1987.7772<System.Object>()"
		ldstr "class G2_C323`2<class BaseClass0,class BaseClass1> on type class G2_C323`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C323`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C323`2<class BaseClass0,class BaseClass1>::ClassMethod1986()
		ldstr "G2_C323::ClassMethod1986.7771()"
		ldstr "class G2_C323`2<class BaseClass0,class BaseClass1> on type class G2_C323`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C323`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C323`2<class BaseClass0,class BaseClass1>::ClassMethod1985()
		ldstr "G2_C323::ClassMethod1985.7770()"
		ldstr "class G2_C323`2<class BaseClass0,class BaseClass1> on type class G2_C323`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C323`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C323`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C323::Method7.7769<System.Object>()"
		ldstr "class G2_C323`2<class BaseClass0,class BaseClass1> on type class G2_C323`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C323`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C323`2<class BaseClass0,class BaseClass1>::Method3<object>()
		ldstr "G2_C323::Method3.7767<System.Object>()"
		ldstr "class G2_C323`2<class BaseClass0,class BaseClass1> on type class G2_C323`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C323`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C323`2<class BaseClass0,class BaseClass1>::Method2<object>()
		ldstr "G2_C323::Method2.7765<System.Object>()"
		ldstr "class G2_C323`2<class BaseClass0,class BaseClass1> on type class G2_C323`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C323`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C323`2<class BaseClass0,class BaseClass1>::Method1()
		ldstr "G2_C323::Method1.7763()"
		ldstr "class G2_C323`2<class BaseClass0,class BaseClass1> on type class G2_C323`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C323`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C323`2<class BaseClass0,class BaseClass1>::Method0()
		ldstr "G2_C323::Method0.7761()"
		ldstr "class G2_C323`2<class BaseClass0,class BaseClass1> on type class G2_C323`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C323`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C323`2<class BaseClass0,class BaseClass1>::ClassMethod1327<object>()
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G2_C323`2<class BaseClass0,class BaseClass1> on type class G2_C323`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C323`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C323`2<class BaseClass0,class BaseClass1>::ClassMethod1326()
		ldstr "G2_C323::ClassMethod1326.MI.7774()"
		ldstr "class G2_C323`2<class BaseClass0,class BaseClass1> on type class G2_C323`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string IBase0::Method0()
		ldstr "G2_C323::Method0.MI.7762()"
		ldstr "IBase0 on type class G2_C323`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method1()
		ldstr "G2_C323::Method1.MI.7764()"
		ldstr "IBase0 on type class G2_C323`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method2<object>()
		ldstr "G2_C323::Method2.MI.7766<System.Object>()"
		ldstr "IBase0 on type class G2_C323`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method3<object>()
		ldstr "G2_C323::Method3.MI.7768<System.Object>()"
		ldstr "IBase0 on type class G2_C323`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C323`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C6`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C6`2<class BaseClass0,class BaseClass0>::ClassMethod1327<object>()
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass0,class BaseClass0> on type class G2_C323`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C6`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C6`2<class BaseClass0,class BaseClass0>::ClassMethod1326()
		ldstr "G2_C323::ClassMethod1326.MI.7774()"
		ldstr "class G1_C6`2<class BaseClass0,class BaseClass0> on type class G2_C323`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C6`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C6`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass0,class BaseClass0> on type class G2_C323`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G2_C323`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C323`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C323::Method7.7769<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C323`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C323`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C323`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C323`2<class BaseClass1,class BaseClass0>::ClassMethod1988<object>()
		ldstr "G2_C323::ClassMethod1988.7773<System.Object>()"
		ldstr "class G2_C323`2<class BaseClass1,class BaseClass0> on type class G2_C323`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C323`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C323`2<class BaseClass1,class BaseClass0>::ClassMethod1987<object>()
		ldstr "G2_C323::ClassMethod1987.7772<System.Object>()"
		ldstr "class G2_C323`2<class BaseClass1,class BaseClass0> on type class G2_C323`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C323`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C323`2<class BaseClass1,class BaseClass0>::ClassMethod1986()
		ldstr "G2_C323::ClassMethod1986.7771()"
		ldstr "class G2_C323`2<class BaseClass1,class BaseClass0> on type class G2_C323`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C323`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C323`2<class BaseClass1,class BaseClass0>::ClassMethod1985()
		ldstr "G2_C323::ClassMethod1985.7770()"
		ldstr "class G2_C323`2<class BaseClass1,class BaseClass0> on type class G2_C323`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C323`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C323`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G2_C323::Method7.7769<System.Object>()"
		ldstr "class G2_C323`2<class BaseClass1,class BaseClass0> on type class G2_C323`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C323`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C323`2<class BaseClass1,class BaseClass0>::Method3<object>()
		ldstr "G2_C323::Method3.7767<System.Object>()"
		ldstr "class G2_C323`2<class BaseClass1,class BaseClass0> on type class G2_C323`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C323`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C323`2<class BaseClass1,class BaseClass0>::Method2<object>()
		ldstr "G2_C323::Method2.7765<System.Object>()"
		ldstr "class G2_C323`2<class BaseClass1,class BaseClass0> on type class G2_C323`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C323`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C323`2<class BaseClass1,class BaseClass0>::Method1()
		ldstr "G2_C323::Method1.7763()"
		ldstr "class G2_C323`2<class BaseClass1,class BaseClass0> on type class G2_C323`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C323`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C323`2<class BaseClass1,class BaseClass0>::Method0()
		ldstr "G2_C323::Method0.7761()"
		ldstr "class G2_C323`2<class BaseClass1,class BaseClass0> on type class G2_C323`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C323`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C323`2<class BaseClass1,class BaseClass0>::ClassMethod1327<object>()
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G2_C323`2<class BaseClass1,class BaseClass0> on type class G2_C323`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C323`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C323`2<class BaseClass1,class BaseClass0>::ClassMethod1326()
		ldstr "G2_C323::ClassMethod1326.MI.7774()"
		ldstr "class G2_C323`2<class BaseClass1,class BaseClass0> on type class G2_C323`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string IBase0::Method0()
		ldstr "G2_C323::Method0.MI.7762()"
		ldstr "IBase0 on type class G2_C323`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method1()
		ldstr "G2_C323::Method1.MI.7764()"
		ldstr "IBase0 on type class G2_C323`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method2<object>()
		ldstr "G2_C323::Method2.MI.7766<System.Object>()"
		ldstr "IBase0 on type class G2_C323`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method3<object>()
		ldstr "G2_C323::Method3.MI.7768<System.Object>()"
		ldstr "IBase0 on type class G2_C323`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C323`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C6`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C6`2<class BaseClass1,class BaseClass1>::ClassMethod1327<object>()
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass1> on type class G2_C323`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C6`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C6`2<class BaseClass1,class BaseClass1>::ClassMethod1326()
		ldstr "G2_C323::ClassMethod1326.MI.7774()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass1> on type class G2_C323`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C6`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C6`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass1> on type class G2_C323`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C323`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C323::Method7.7769<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C323`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C323`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C323`2<class BaseClass1,class BaseClass1>::ClassMethod1988<object>()
		ldstr "G2_C323::ClassMethod1988.7773<System.Object>()"
		ldstr "class G2_C323`2<class BaseClass1,class BaseClass1> on type class G2_C323`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C323`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C323`2<class BaseClass1,class BaseClass1>::ClassMethod1987<object>()
		ldstr "G2_C323::ClassMethod1987.7772<System.Object>()"
		ldstr "class G2_C323`2<class BaseClass1,class BaseClass1> on type class G2_C323`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C323`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C323`2<class BaseClass1,class BaseClass1>::ClassMethod1986()
		ldstr "G2_C323::ClassMethod1986.7771()"
		ldstr "class G2_C323`2<class BaseClass1,class BaseClass1> on type class G2_C323`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C323`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C323`2<class BaseClass1,class BaseClass1>::ClassMethod1985()
		ldstr "G2_C323::ClassMethod1985.7770()"
		ldstr "class G2_C323`2<class BaseClass1,class BaseClass1> on type class G2_C323`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C323`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C323`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C323::Method7.7769<System.Object>()"
		ldstr "class G2_C323`2<class BaseClass1,class BaseClass1> on type class G2_C323`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C323`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C323`2<class BaseClass1,class BaseClass1>::Method3<object>()
		ldstr "G2_C323::Method3.7767<System.Object>()"
		ldstr "class G2_C323`2<class BaseClass1,class BaseClass1> on type class G2_C323`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C323`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C323`2<class BaseClass1,class BaseClass1>::Method2<object>()
		ldstr "G2_C323::Method2.7765<System.Object>()"
		ldstr "class G2_C323`2<class BaseClass1,class BaseClass1> on type class G2_C323`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C323`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C323`2<class BaseClass1,class BaseClass1>::Method1()
		ldstr "G2_C323::Method1.7763()"
		ldstr "class G2_C323`2<class BaseClass1,class BaseClass1> on type class G2_C323`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C323`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C323`2<class BaseClass1,class BaseClass1>::Method0()
		ldstr "G2_C323::Method0.7761()"
		ldstr "class G2_C323`2<class BaseClass1,class BaseClass1> on type class G2_C323`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C323`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C323`2<class BaseClass1,class BaseClass1>::ClassMethod1327<object>()
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G2_C323`2<class BaseClass1,class BaseClass1> on type class G2_C323`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C323`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C323`2<class BaseClass1,class BaseClass1>::ClassMethod1326()
		ldstr "G2_C323::ClassMethod1326.MI.7774()"
		ldstr "class G2_C323`2<class BaseClass1,class BaseClass1> on type class G2_C323`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string IBase0::Method0()
		ldstr "G2_C323::Method0.MI.7762()"
		ldstr "IBase0 on type class G2_C323`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method1()
		ldstr "G2_C323::Method1.MI.7764()"
		ldstr "IBase0 on type class G2_C323`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method2<object>()
		ldstr "G2_C323::Method2.MI.7766<System.Object>()"
		ldstr "IBase0 on type class G2_C323`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method3<object>()
		ldstr "G2_C323::Method3.MI.7768<System.Object>()"
		ldstr "IBase0 on type class G2_C323`2<class BaseClass1,class BaseClass1>"
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
		newobj instance void class G3_C1278`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C323::ClassMethod1326.MI.7774()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.G1_C6.T.T<class BaseClass0,class BaseClass0,class G3_C1278`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C323::ClassMethod1326.MI.7774()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.G1_C6.A.T<class BaseClass0,class G3_C1278`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C323::ClassMethod1326.MI.7774()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.G1_C6.A.A<class G3_C1278`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.IBase2.T.T<class BaseClass1,class BaseClass0,class G3_C1278`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.IBase2.B.T<class BaseClass0,class G3_C1278`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.IBase2.B.A<class G3_C1278`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G3_C1278`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.IBase2.A.T<class BaseClass0,class G3_C1278`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.IBase2.A.A<class G3_C1278`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C323::Method7.7769<System.Object>()#"
		call void Generated806::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G3_C1278`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C323::Method7.7769<System.Object>()#"
		call void Generated806::M.IBase2.A.T<class BaseClass1,class G3_C1278`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C323::Method7.7769<System.Object>()#"
		call void Generated806::M.IBase2.A.B<class G3_C1278`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G3_C1278`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.IBase2.B.T<class BaseClass1,class G3_C1278`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.IBase2.B.B<class G3_C1278`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C323::ClassMethod1326.MI.7774()#G1_C6::ClassMethod1327.4810<System.Object>()#G3_C1278::ClassMethod1985.MI.15293()#G2_C323::ClassMethod1986.7771()#G3_C1278::ClassMethod1987.MI.15294<System.Object>()#G3_C1278::ClassMethod1988.MI.15295<System.Object>()#G2_C323::Method0.7761()#G3_C1278::Method1.15287()#G3_C1278::Method2.15288<System.Object>()#G2_C323::Method3.7767<System.Object>()#G2_C323::Method7.7769<System.Object>()#"
		call void Generated806::M.G2_C323.T.T<class BaseClass0,class BaseClass0,class G3_C1278`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C323::ClassMethod1326.MI.7774()#G1_C6::ClassMethod1327.4810<System.Object>()#G3_C1278::ClassMethod1985.MI.15293()#G2_C323::ClassMethod1986.7771()#G3_C1278::ClassMethod1987.MI.15294<System.Object>()#G3_C1278::ClassMethod1988.MI.15295<System.Object>()#G2_C323::Method0.7761()#G3_C1278::Method1.15287()#G3_C1278::Method2.15288<System.Object>()#G2_C323::Method3.7767<System.Object>()#G2_C323::Method7.7769<System.Object>()#"
		call void Generated806::M.G2_C323.A.T<class BaseClass0,class G3_C1278`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C323::ClassMethod1326.MI.7774()#G1_C6::ClassMethod1327.4810<System.Object>()#G3_C1278::ClassMethod1985.MI.15293()#G2_C323::ClassMethod1986.7771()#G3_C1278::ClassMethod1987.MI.15294<System.Object>()#G3_C1278::ClassMethod1988.MI.15295<System.Object>()#G2_C323::Method0.7761()#G3_C1278::Method1.15287()#G3_C1278::Method2.15288<System.Object>()#G2_C323::Method3.7767<System.Object>()#G2_C323::Method7.7769<System.Object>()#"
		call void Generated806::M.G2_C323.A.A<class G3_C1278`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1278::Method0.15286()#G3_C1278::Method1.15287()#G3_C1278::Method2.MI.15289<System.Object>()#G3_C1278::Method3.MI.15291<System.Object>()#"
		call void Generated806::M.IBase0<class G3_C1278`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C323::ClassMethod1326.MI.7774()#G1_C6::ClassMethod1327.4810<System.Object>()#G3_C1278::ClassMethod1985.MI.15293()#G2_C323::ClassMethod1986.7771()#G3_C1278::ClassMethod1987.MI.15294<System.Object>()#G3_C1278::ClassMethod1988.MI.15295<System.Object>()#G3_C1278::ClassMethod3930.15292()#G3_C1278::Method0.15286()#G3_C1278::Method1.15287()#G3_C1278::Method2.15288<System.Object>()#G3_C1278::Method3.15290<System.Object>()#G2_C323::Method7.7769<System.Object>()#"
		call void Generated806::M.G3_C1278.T<class BaseClass0,class G3_C1278`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C323::ClassMethod1326.MI.7774()#G1_C6::ClassMethod1327.4810<System.Object>()#G3_C1278::ClassMethod1985.MI.15293()#G2_C323::ClassMethod1986.7771()#G3_C1278::ClassMethod1987.MI.15294<System.Object>()#G3_C1278::ClassMethod1988.MI.15295<System.Object>()#G3_C1278::ClassMethod3930.15292()#G3_C1278::Method0.15286()#G3_C1278::Method1.15287()#G3_C1278::Method2.15288<System.Object>()#G3_C1278::Method3.15290<System.Object>()#G2_C323::Method7.7769<System.Object>()#"
		call void Generated806::M.G3_C1278.A<class G3_C1278`1<class BaseClass0>>(!!0,string)
		newobj instance void class G3_C1278`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C323::ClassMethod1326.MI.7774()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.G1_C6.T.T<class BaseClass1,class BaseClass1,class G3_C1278`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C323::ClassMethod1326.MI.7774()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.G1_C6.B.T<class BaseClass1,class G3_C1278`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C323::ClassMethod1326.MI.7774()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.G1_C6.B.B<class G3_C1278`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G3_C1278`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.IBase2.B.T<class BaseClass1,class G3_C1278`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.IBase2.B.B<class G3_C1278`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C323::Method7.7769<System.Object>()#"
		call void Generated806::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G3_C1278`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C323::Method7.7769<System.Object>()#"
		call void Generated806::M.IBase2.A.T<class BaseClass1,class G3_C1278`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C323::Method7.7769<System.Object>()#"
		call void Generated806::M.IBase2.A.B<class G3_C1278`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C323::ClassMethod1326.MI.7774()#G1_C6::ClassMethod1327.4810<System.Object>()#G3_C1278::ClassMethod1985.MI.15293()#G2_C323::ClassMethod1986.7771()#G3_C1278::ClassMethod1987.MI.15294<System.Object>()#G3_C1278::ClassMethod1988.MI.15295<System.Object>()#G2_C323::Method0.7761()#G3_C1278::Method1.15287()#G3_C1278::Method2.15288<System.Object>()#G2_C323::Method3.7767<System.Object>()#G2_C323::Method7.7769<System.Object>()#"
		call void Generated806::M.G2_C323.T.T<class BaseClass0,class BaseClass1,class G3_C1278`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C323::ClassMethod1326.MI.7774()#G1_C6::ClassMethod1327.4810<System.Object>()#G3_C1278::ClassMethod1985.MI.15293()#G2_C323::ClassMethod1986.7771()#G3_C1278::ClassMethod1987.MI.15294<System.Object>()#G3_C1278::ClassMethod1988.MI.15295<System.Object>()#G2_C323::Method0.7761()#G3_C1278::Method1.15287()#G3_C1278::Method2.15288<System.Object>()#G2_C323::Method3.7767<System.Object>()#G2_C323::Method7.7769<System.Object>()#"
		call void Generated806::M.G2_C323.A.T<class BaseClass1,class G3_C1278`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C323::ClassMethod1326.MI.7774()#G1_C6::ClassMethod1327.4810<System.Object>()#G3_C1278::ClassMethod1985.MI.15293()#G2_C323::ClassMethod1986.7771()#G3_C1278::ClassMethod1987.MI.15294<System.Object>()#G3_C1278::ClassMethod1988.MI.15295<System.Object>()#G2_C323::Method0.7761()#G3_C1278::Method1.15287()#G3_C1278::Method2.15288<System.Object>()#G2_C323::Method3.7767<System.Object>()#G2_C323::Method7.7769<System.Object>()#"
		call void Generated806::M.G2_C323.A.B<class G3_C1278`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1278::Method0.15286()#G3_C1278::Method1.15287()#G3_C1278::Method2.MI.15289<System.Object>()#G3_C1278::Method3.MI.15291<System.Object>()#"
		call void Generated806::M.IBase0<class G3_C1278`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C323::ClassMethod1326.MI.7774()#G1_C6::ClassMethod1327.4810<System.Object>()#G3_C1278::ClassMethod1985.MI.15293()#G2_C323::ClassMethod1986.7771()#G3_C1278::ClassMethod1987.MI.15294<System.Object>()#G3_C1278::ClassMethod1988.MI.15295<System.Object>()#G3_C1278::ClassMethod3930.15292()#G3_C1278::Method0.15286()#G3_C1278::Method1.15287()#G3_C1278::Method2.15288<System.Object>()#G3_C1278::Method3.15290<System.Object>()#G2_C323::Method7.7769<System.Object>()#"
		call void Generated806::M.G3_C1278.T<class BaseClass1,class G3_C1278`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C323::ClassMethod1326.MI.7774()#G1_C6::ClassMethod1327.4810<System.Object>()#G3_C1278::ClassMethod1985.MI.15293()#G2_C323::ClassMethod1986.7771()#G3_C1278::ClassMethod1987.MI.15294<System.Object>()#G3_C1278::ClassMethod1988.MI.15295<System.Object>()#G3_C1278::ClassMethod3930.15292()#G3_C1278::Method0.15286()#G3_C1278::Method1.15287()#G3_C1278::Method2.15288<System.Object>()#G3_C1278::Method3.15290<System.Object>()#G2_C323::Method7.7769<System.Object>()#"
		call void Generated806::M.G3_C1278.B<class G3_C1278`1<class BaseClass1>>(!!0,string)
		newobj instance void class G2_C323`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C323::ClassMethod1326.MI.7774()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.G1_C6.T.T<class BaseClass0,class BaseClass0,class G2_C323`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C323::ClassMethod1326.MI.7774()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.G1_C6.A.T<class BaseClass0,class G2_C323`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C323::ClassMethod1326.MI.7774()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.G1_C6.A.A<class G2_C323`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.IBase2.T.T<class BaseClass1,class BaseClass0,class G2_C323`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.IBase2.B.T<class BaseClass0,class G2_C323`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.IBase2.B.A<class G2_C323`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G2_C323`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.IBase2.A.T<class BaseClass0,class G2_C323`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.IBase2.A.A<class G2_C323`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C323::Method7.7769<System.Object>()#"
		call void Generated806::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C323`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C323::Method7.7769<System.Object>()#"
		call void Generated806::M.IBase2.A.T<class BaseClass1,class G2_C323`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C323::Method7.7769<System.Object>()#"
		call void Generated806::M.IBase2.A.B<class G2_C323`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G2_C323`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.IBase2.B.T<class BaseClass1,class G2_C323`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.IBase2.B.B<class G2_C323`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C323::ClassMethod1326.MI.7774()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C323::ClassMethod1985.7770()#G2_C323::ClassMethod1986.7771()#G2_C323::ClassMethod1987.7772<System.Object>()#G2_C323::ClassMethod1988.7773<System.Object>()#G2_C323::Method0.7761()#G2_C323::Method1.7763()#G2_C323::Method2.7765<System.Object>()#G2_C323::Method3.7767<System.Object>()#G2_C323::Method7.7769<System.Object>()#"
		call void Generated806::M.G2_C323.T.T<class BaseClass0,class BaseClass0,class G2_C323`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C323::ClassMethod1326.MI.7774()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C323::ClassMethod1985.7770()#G2_C323::ClassMethod1986.7771()#G2_C323::ClassMethod1987.7772<System.Object>()#G2_C323::ClassMethod1988.7773<System.Object>()#G2_C323::Method0.7761()#G2_C323::Method1.7763()#G2_C323::Method2.7765<System.Object>()#G2_C323::Method3.7767<System.Object>()#G2_C323::Method7.7769<System.Object>()#"
		call void Generated806::M.G2_C323.A.T<class BaseClass0,class G2_C323`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C323::ClassMethod1326.MI.7774()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C323::ClassMethod1985.7770()#G2_C323::ClassMethod1986.7771()#G2_C323::ClassMethod1987.7772<System.Object>()#G2_C323::ClassMethod1988.7773<System.Object>()#G2_C323::Method0.7761()#G2_C323::Method1.7763()#G2_C323::Method2.7765<System.Object>()#G2_C323::Method3.7767<System.Object>()#G2_C323::Method7.7769<System.Object>()#"
		call void Generated806::M.G2_C323.A.A<class G2_C323`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C323::Method0.MI.7762()#G2_C323::Method1.MI.7764()#G2_C323::Method2.MI.7766<System.Object>()#G2_C323::Method3.MI.7768<System.Object>()#"
		call void Generated806::M.IBase0<class G2_C323`2<class BaseClass0,class BaseClass0>>(!!0,string)
		newobj instance void class G2_C323`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C323::ClassMethod1326.MI.7774()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.G1_C6.T.T<class BaseClass1,class BaseClass1,class G2_C323`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C323::ClassMethod1326.MI.7774()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.G1_C6.B.T<class BaseClass1,class G2_C323`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C323::ClassMethod1326.MI.7774()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.G1_C6.B.B<class G2_C323`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G2_C323`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.IBase2.B.T<class BaseClass1,class G2_C323`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.IBase2.B.B<class G2_C323`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C323::Method7.7769<System.Object>()#"
		call void Generated806::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C323`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C323::Method7.7769<System.Object>()#"
		call void Generated806::M.IBase2.A.T<class BaseClass1,class G2_C323`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C323::Method7.7769<System.Object>()#"
		call void Generated806::M.IBase2.A.B<class G2_C323`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C323::ClassMethod1326.MI.7774()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C323::ClassMethod1985.7770()#G2_C323::ClassMethod1986.7771()#G2_C323::ClassMethod1987.7772<System.Object>()#G2_C323::ClassMethod1988.7773<System.Object>()#G2_C323::Method0.7761()#G2_C323::Method1.7763()#G2_C323::Method2.7765<System.Object>()#G2_C323::Method3.7767<System.Object>()#G2_C323::Method7.7769<System.Object>()#"
		call void Generated806::M.G2_C323.T.T<class BaseClass0,class BaseClass1,class G2_C323`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C323::ClassMethod1326.MI.7774()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C323::ClassMethod1985.7770()#G2_C323::ClassMethod1986.7771()#G2_C323::ClassMethod1987.7772<System.Object>()#G2_C323::ClassMethod1988.7773<System.Object>()#G2_C323::Method0.7761()#G2_C323::Method1.7763()#G2_C323::Method2.7765<System.Object>()#G2_C323::Method3.7767<System.Object>()#G2_C323::Method7.7769<System.Object>()#"
		call void Generated806::M.G2_C323.A.T<class BaseClass1,class G2_C323`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C323::ClassMethod1326.MI.7774()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C323::ClassMethod1985.7770()#G2_C323::ClassMethod1986.7771()#G2_C323::ClassMethod1987.7772<System.Object>()#G2_C323::ClassMethod1988.7773<System.Object>()#G2_C323::Method0.7761()#G2_C323::Method1.7763()#G2_C323::Method2.7765<System.Object>()#G2_C323::Method3.7767<System.Object>()#G2_C323::Method7.7769<System.Object>()#"
		call void Generated806::M.G2_C323.A.B<class G2_C323`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C323::Method0.MI.7762()#G2_C323::Method1.MI.7764()#G2_C323::Method2.MI.7766<System.Object>()#G2_C323::Method3.MI.7768<System.Object>()#"
		call void Generated806::M.IBase0<class G2_C323`2<class BaseClass0,class BaseClass1>>(!!0,string)
		newobj instance void class G2_C323`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C323::ClassMethod1326.MI.7774()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.G1_C6.T.T<class BaseClass0,class BaseClass0,class G2_C323`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C323::ClassMethod1326.MI.7774()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.G1_C6.A.T<class BaseClass0,class G2_C323`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C323::ClassMethod1326.MI.7774()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.G1_C6.A.A<class G2_C323`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.IBase2.T.T<class BaseClass1,class BaseClass0,class G2_C323`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.IBase2.B.T<class BaseClass0,class G2_C323`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.IBase2.B.A<class G2_C323`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G2_C323`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.IBase2.A.T<class BaseClass0,class G2_C323`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.IBase2.A.A<class G2_C323`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C323::Method7.7769<System.Object>()#"
		call void Generated806::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C323`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C323::Method7.7769<System.Object>()#"
		call void Generated806::M.IBase2.A.T<class BaseClass1,class G2_C323`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C323::Method7.7769<System.Object>()#"
		call void Generated806::M.IBase2.A.B<class G2_C323`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G2_C323`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.IBase2.B.T<class BaseClass1,class G2_C323`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.IBase2.B.B<class G2_C323`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C323::ClassMethod1326.MI.7774()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C323::ClassMethod1985.7770()#G2_C323::ClassMethod1986.7771()#G2_C323::ClassMethod1987.7772<System.Object>()#G2_C323::ClassMethod1988.7773<System.Object>()#G2_C323::Method0.7761()#G2_C323::Method1.7763()#G2_C323::Method2.7765<System.Object>()#G2_C323::Method3.7767<System.Object>()#G2_C323::Method7.7769<System.Object>()#"
		call void Generated806::M.G2_C323.T.T<class BaseClass1,class BaseClass0,class G2_C323`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C323::ClassMethod1326.MI.7774()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C323::ClassMethod1985.7770()#G2_C323::ClassMethod1986.7771()#G2_C323::ClassMethod1987.7772<System.Object>()#G2_C323::ClassMethod1988.7773<System.Object>()#G2_C323::Method0.7761()#G2_C323::Method1.7763()#G2_C323::Method2.7765<System.Object>()#G2_C323::Method3.7767<System.Object>()#G2_C323::Method7.7769<System.Object>()#"
		call void Generated806::M.G2_C323.B.T<class BaseClass0,class G2_C323`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C323::ClassMethod1326.MI.7774()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C323::ClassMethod1985.7770()#G2_C323::ClassMethod1986.7771()#G2_C323::ClassMethod1987.7772<System.Object>()#G2_C323::ClassMethod1988.7773<System.Object>()#G2_C323::Method0.7761()#G2_C323::Method1.7763()#G2_C323::Method2.7765<System.Object>()#G2_C323::Method3.7767<System.Object>()#G2_C323::Method7.7769<System.Object>()#"
		call void Generated806::M.G2_C323.B.A<class G2_C323`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C323::Method0.MI.7762()#G2_C323::Method1.MI.7764()#G2_C323::Method2.MI.7766<System.Object>()#G2_C323::Method3.MI.7768<System.Object>()#"
		call void Generated806::M.IBase0<class G2_C323`2<class BaseClass1,class BaseClass0>>(!!0,string)
		newobj instance void class G2_C323`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C323::ClassMethod1326.MI.7774()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.G1_C6.T.T<class BaseClass1,class BaseClass1,class G2_C323`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C323::ClassMethod1326.MI.7774()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.G1_C6.B.T<class BaseClass1,class G2_C323`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C323::ClassMethod1326.MI.7774()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.G1_C6.B.B<class G2_C323`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G2_C323`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.IBase2.B.T<class BaseClass1,class G2_C323`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.IBase2.B.B<class G2_C323`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C323::Method7.7769<System.Object>()#"
		call void Generated806::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C323`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C323::Method7.7769<System.Object>()#"
		call void Generated806::M.IBase2.A.T<class BaseClass1,class G2_C323`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C323::Method7.7769<System.Object>()#"
		call void Generated806::M.IBase2.A.B<class G2_C323`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C323::ClassMethod1326.MI.7774()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C323::ClassMethod1985.7770()#G2_C323::ClassMethod1986.7771()#G2_C323::ClassMethod1987.7772<System.Object>()#G2_C323::ClassMethod1988.7773<System.Object>()#G2_C323::Method0.7761()#G2_C323::Method1.7763()#G2_C323::Method2.7765<System.Object>()#G2_C323::Method3.7767<System.Object>()#G2_C323::Method7.7769<System.Object>()#"
		call void Generated806::M.G2_C323.T.T<class BaseClass1,class BaseClass1,class G2_C323`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C323::ClassMethod1326.MI.7774()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C323::ClassMethod1985.7770()#G2_C323::ClassMethod1986.7771()#G2_C323::ClassMethod1987.7772<System.Object>()#G2_C323::ClassMethod1988.7773<System.Object>()#G2_C323::Method0.7761()#G2_C323::Method1.7763()#G2_C323::Method2.7765<System.Object>()#G2_C323::Method3.7767<System.Object>()#G2_C323::Method7.7769<System.Object>()#"
		call void Generated806::M.G2_C323.B.T<class BaseClass1,class G2_C323`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C323::ClassMethod1326.MI.7774()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C323::ClassMethod1985.7770()#G2_C323::ClassMethod1986.7771()#G2_C323::ClassMethod1987.7772<System.Object>()#G2_C323::ClassMethod1988.7773<System.Object>()#G2_C323::Method0.7761()#G2_C323::Method1.7763()#G2_C323::Method2.7765<System.Object>()#G2_C323::Method3.7767<System.Object>()#G2_C323::Method7.7769<System.Object>()#"
		call void Generated806::M.G2_C323.B.B<class G2_C323`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C323::Method0.MI.7762()#G2_C323::Method1.MI.7764()#G2_C323::Method2.MI.7766<System.Object>()#G2_C323::Method3.MI.7768<System.Object>()#"
		call void Generated806::M.IBase0<class G2_C323`2<class BaseClass1,class BaseClass1>>(!!0,string)
		newobj instance void class G1_C6`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.G1_C6.T.T<class BaseClass0,class BaseClass0,class G1_C6`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.G1_C6.A.T<class BaseClass0,class G1_C6`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.G1_C6.A.A<class G1_C6`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.IBase2.T.T<class BaseClass1,class BaseClass0,class G1_C6`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.IBase2.B.T<class BaseClass0,class G1_C6`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.IBase2.B.A<class G1_C6`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G1_C6`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.IBase2.A.T<class BaseClass0,class G1_C6`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.IBase2.A.A<class G1_C6`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G1_C6`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.IBase2.A.T<class BaseClass1,class G1_C6`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.IBase2.A.B<class G1_C6`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G1_C6`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.IBase2.B.T<class BaseClass1,class G1_C6`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.IBase2.B.B<class G1_C6`2<class BaseClass0,class BaseClass0>>(!!0,string)
		newobj instance void class G1_C6`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.G1_C6.T.T<class BaseClass0,class BaseClass1,class G1_C6`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.G1_C6.A.T<class BaseClass1,class G1_C6`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.G1_C6.A.B<class G1_C6`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.IBase2.T.T<class BaseClass1,class BaseClass0,class G1_C6`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.IBase2.B.T<class BaseClass0,class G1_C6`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.IBase2.B.A<class G1_C6`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G1_C6`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.IBase2.A.T<class BaseClass0,class G1_C6`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.IBase2.A.A<class G1_C6`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G1_C6`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.IBase2.A.T<class BaseClass1,class G1_C6`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.IBase2.A.B<class G1_C6`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G1_C6`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.IBase2.B.T<class BaseClass1,class G1_C6`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.IBase2.B.B<class G1_C6`2<class BaseClass0,class BaseClass1>>(!!0,string)
		newobj instance void class G1_C6`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.G1_C6.T.T<class BaseClass1,class BaseClass0,class G1_C6`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.G1_C6.B.T<class BaseClass0,class G1_C6`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.G1_C6.B.A<class G1_C6`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G1_C6`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.IBase2.B.T<class BaseClass1,class G1_C6`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.IBase2.B.B<class G1_C6`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G1_C6`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.IBase2.A.T<class BaseClass1,class G1_C6`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.IBase2.A.B<class G1_C6`2<class BaseClass1,class BaseClass0>>(!!0,string)
		newobj instance void class G1_C6`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.G1_C6.T.T<class BaseClass1,class BaseClass1,class G1_C6`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.G1_C6.B.T<class BaseClass1,class G1_C6`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.G1_C6.B.B<class G1_C6`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G1_C6`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.IBase2.B.T<class BaseClass1,class G1_C6`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.IBase2.B.B<class G1_C6`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G1_C6`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.IBase2.A.T<class BaseClass1,class G1_C6`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated806::M.IBase2.A.B<class G1_C6`2<class BaseClass1,class BaseClass1>>(!!0,string)
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
		newobj instance void class G3_C1278`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C6`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass0,class BaseClass0>::ClassMethod1327<object>()
		calli default string(class G3_C1278`1<class BaseClass0>)
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass0,class BaseClass0> on type class G3_C1278`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C6`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass0,class BaseClass0>::ClassMethod1326()
		calli default string(class G3_C1278`1<class BaseClass0>)
		ldstr "G2_C323::ClassMethod1326.MI.7774()"
		ldstr "class G1_C6`2<class BaseClass0,class BaseClass0> on type class G3_C1278`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C6`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1278`1<class BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass0,class BaseClass0> on type class G3_C1278`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1278`1<class BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G3_C1278`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1278`1<class BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1278`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1278`1<class BaseClass0>)
		ldstr "G2_C323::Method7.7769<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1278`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1278`1<class BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1278`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C323`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C323`2<class BaseClass0,class BaseClass0>::ClassMethod1988<object>()
		calli default string(class G3_C1278`1<class BaseClass0>)
		ldstr "G3_C1278::ClassMethod1988.MI.15295<System.Object>()"
		ldstr "class G2_C323`2<class BaseClass0,class BaseClass0> on type class G3_C1278`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C323`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C323`2<class BaseClass0,class BaseClass0>::ClassMethod1987<object>()
		calli default string(class G3_C1278`1<class BaseClass0>)
		ldstr "G3_C1278::ClassMethod1987.MI.15294<System.Object>()"
		ldstr "class G2_C323`2<class BaseClass0,class BaseClass0> on type class G3_C1278`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C323`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C323`2<class BaseClass0,class BaseClass0>::ClassMethod1986()
		calli default string(class G3_C1278`1<class BaseClass0>)
		ldstr "G2_C323::ClassMethod1986.7771()"
		ldstr "class G2_C323`2<class BaseClass0,class BaseClass0> on type class G3_C1278`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C323`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C323`2<class BaseClass0,class BaseClass0>::ClassMethod1985()
		calli default string(class G3_C1278`1<class BaseClass0>)
		ldstr "G3_C1278::ClassMethod1985.MI.15293()"
		ldstr "class G2_C323`2<class BaseClass0,class BaseClass0> on type class G3_C1278`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C323`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C323`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1278`1<class BaseClass0>)
		ldstr "G2_C323::Method7.7769<System.Object>()"
		ldstr "class G2_C323`2<class BaseClass0,class BaseClass0> on type class G3_C1278`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C323`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C323`2<class BaseClass0,class BaseClass0>::Method3<object>()
		calli default string(class G3_C1278`1<class BaseClass0>)
		ldstr "G2_C323::Method3.7767<System.Object>()"
		ldstr "class G2_C323`2<class BaseClass0,class BaseClass0> on type class G3_C1278`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C323`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C323`2<class BaseClass0,class BaseClass0>::Method2<object>()
		calli default string(class G3_C1278`1<class BaseClass0>)
		ldstr "G3_C1278::Method2.15288<System.Object>()"
		ldstr "class G2_C323`2<class BaseClass0,class BaseClass0> on type class G3_C1278`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C323`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C323`2<class BaseClass0,class BaseClass0>::Method1()
		calli default string(class G3_C1278`1<class BaseClass0>)
		ldstr "G3_C1278::Method1.15287()"
		ldstr "class G2_C323`2<class BaseClass0,class BaseClass0> on type class G3_C1278`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C323`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C323`2<class BaseClass0,class BaseClass0>::Method0()
		calli default string(class G3_C1278`1<class BaseClass0>)
		ldstr "G2_C323::Method0.7761()"
		ldstr "class G2_C323`2<class BaseClass0,class BaseClass0> on type class G3_C1278`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C323`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C323`2<class BaseClass0,class BaseClass0>::ClassMethod1327<object>()
		calli default string(class G3_C1278`1<class BaseClass0>)
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G2_C323`2<class BaseClass0,class BaseClass0> on type class G3_C1278`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C323`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C323`2<class BaseClass0,class BaseClass0>::ClassMethod1326()
		calli default string(class G3_C1278`1<class BaseClass0>)
		ldstr "G2_C323::ClassMethod1326.MI.7774()"
		ldstr "class G2_C323`2<class BaseClass0,class BaseClass0> on type class G3_C1278`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method0()
		calli default string(class G3_C1278`1<class BaseClass0>)
		ldstr "G3_C1278::Method0.15286()"
		ldstr "IBase0 on type class G3_C1278`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method1()
		calli default string(class G3_C1278`1<class BaseClass0>)
		ldstr "G3_C1278::Method1.15287()"
		ldstr "IBase0 on type class G3_C1278`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method2<object>()
		calli default string(class G3_C1278`1<class BaseClass0>)
		ldstr "G3_C1278::Method2.MI.15289<System.Object>()"
		ldstr "IBase0 on type class G3_C1278`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method3<object>()
		calli default string(class G3_C1278`1<class BaseClass0>)
		ldstr "G3_C1278::Method3.MI.15291<System.Object>()"
		ldstr "IBase0 on type class G3_C1278`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1278`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1278`1<class BaseClass0>::ClassMethod3930()
		calli default string(class G3_C1278`1<class BaseClass0>)
		ldstr "G3_C1278::ClassMethod3930.15292()"
		ldstr "class G3_C1278`1<class BaseClass0> on type class G3_C1278`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1278`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1278`1<class BaseClass0>::Method3<object>()
		calli default string(class G3_C1278`1<class BaseClass0>)
		ldstr "G3_C1278::Method3.15290<System.Object>()"
		ldstr "class G3_C1278`1<class BaseClass0> on type class G3_C1278`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1278`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1278`1<class BaseClass0>::Method2<object>()
		calli default string(class G3_C1278`1<class BaseClass0>)
		ldstr "G3_C1278::Method2.15288<System.Object>()"
		ldstr "class G3_C1278`1<class BaseClass0> on type class G3_C1278`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1278`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1278`1<class BaseClass0>::Method1()
		calli default string(class G3_C1278`1<class BaseClass0>)
		ldstr "G3_C1278::Method1.15287()"
		ldstr "class G3_C1278`1<class BaseClass0> on type class G3_C1278`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1278`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1278`1<class BaseClass0>::Method0()
		calli default string(class G3_C1278`1<class BaseClass0>)
		ldstr "G3_C1278::Method0.15286()"
		ldstr "class G3_C1278`1<class BaseClass0> on type class G3_C1278`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1278`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1278`1<class BaseClass0>::ClassMethod1988<object>()
		calli default string(class G3_C1278`1<class BaseClass0>)
		ldstr "G3_C1278::ClassMethod1988.MI.15295<System.Object>()"
		ldstr "class G3_C1278`1<class BaseClass0> on type class G3_C1278`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1278`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1278`1<class BaseClass0>::ClassMethod1987<object>()
		calli default string(class G3_C1278`1<class BaseClass0>)
		ldstr "G3_C1278::ClassMethod1987.MI.15294<System.Object>()"
		ldstr "class G3_C1278`1<class BaseClass0> on type class G3_C1278`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1278`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1278`1<class BaseClass0>::ClassMethod1986()
		calli default string(class G3_C1278`1<class BaseClass0>)
		ldstr "G2_C323::ClassMethod1986.7771()"
		ldstr "class G3_C1278`1<class BaseClass0> on type class G3_C1278`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1278`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1278`1<class BaseClass0>::ClassMethod1985()
		calli default string(class G3_C1278`1<class BaseClass0>)
		ldstr "G3_C1278::ClassMethod1985.MI.15293()"
		ldstr "class G3_C1278`1<class BaseClass0> on type class G3_C1278`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1278`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1278`1<class BaseClass0>::Method7<object>()
		calli default string(class G3_C1278`1<class BaseClass0>)
		ldstr "G2_C323::Method7.7769<System.Object>()"
		ldstr "class G3_C1278`1<class BaseClass0> on type class G3_C1278`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1278`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1278`1<class BaseClass0>::ClassMethod1327<object>()
		calli default string(class G3_C1278`1<class BaseClass0>)
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G3_C1278`1<class BaseClass0> on type class G3_C1278`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1278`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1278`1<class BaseClass0>::ClassMethod1326()
		calli default string(class G3_C1278`1<class BaseClass0>)
		ldstr "G2_C323::ClassMethod1326.MI.7774()"
		ldstr "class G3_C1278`1<class BaseClass0> on type class G3_C1278`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G3_C1278`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C6`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass1,class BaseClass1>::ClassMethod1327<object>()
		calli default string(class G3_C1278`1<class BaseClass1>)
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass1> on type class G3_C1278`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C6`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass1,class BaseClass1>::ClassMethod1326()
		calli default string(class G3_C1278`1<class BaseClass1>)
		ldstr "G2_C323::ClassMethod1326.MI.7774()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass1> on type class G3_C1278`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C6`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1278`1<class BaseClass1>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass1> on type class G3_C1278`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1278`1<class BaseClass1>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1278`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1278`1<class BaseClass1>)
		ldstr "G2_C323::Method7.7769<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1278`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C323`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C323`2<class BaseClass0,class BaseClass1>::ClassMethod1988<object>()
		calli default string(class G3_C1278`1<class BaseClass1>)
		ldstr "G3_C1278::ClassMethod1988.MI.15295<System.Object>()"
		ldstr "class G2_C323`2<class BaseClass0,class BaseClass1> on type class G3_C1278`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C323`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C323`2<class BaseClass0,class BaseClass1>::ClassMethod1987<object>()
		calli default string(class G3_C1278`1<class BaseClass1>)
		ldstr "G3_C1278::ClassMethod1987.MI.15294<System.Object>()"
		ldstr "class G2_C323`2<class BaseClass0,class BaseClass1> on type class G3_C1278`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C323`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C323`2<class BaseClass0,class BaseClass1>::ClassMethod1986()
		calli default string(class G3_C1278`1<class BaseClass1>)
		ldstr "G2_C323::ClassMethod1986.7771()"
		ldstr "class G2_C323`2<class BaseClass0,class BaseClass1> on type class G3_C1278`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C323`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C323`2<class BaseClass0,class BaseClass1>::ClassMethod1985()
		calli default string(class G3_C1278`1<class BaseClass1>)
		ldstr "G3_C1278::ClassMethod1985.MI.15293()"
		ldstr "class G2_C323`2<class BaseClass0,class BaseClass1> on type class G3_C1278`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C323`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C323`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1278`1<class BaseClass1>)
		ldstr "G2_C323::Method7.7769<System.Object>()"
		ldstr "class G2_C323`2<class BaseClass0,class BaseClass1> on type class G3_C1278`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C323`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C323`2<class BaseClass0,class BaseClass1>::Method3<object>()
		calli default string(class G3_C1278`1<class BaseClass1>)
		ldstr "G2_C323::Method3.7767<System.Object>()"
		ldstr "class G2_C323`2<class BaseClass0,class BaseClass1> on type class G3_C1278`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C323`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C323`2<class BaseClass0,class BaseClass1>::Method2<object>()
		calli default string(class G3_C1278`1<class BaseClass1>)
		ldstr "G3_C1278::Method2.15288<System.Object>()"
		ldstr "class G2_C323`2<class BaseClass0,class BaseClass1> on type class G3_C1278`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C323`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C323`2<class BaseClass0,class BaseClass1>::Method1()
		calli default string(class G3_C1278`1<class BaseClass1>)
		ldstr "G3_C1278::Method1.15287()"
		ldstr "class G2_C323`2<class BaseClass0,class BaseClass1> on type class G3_C1278`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C323`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C323`2<class BaseClass0,class BaseClass1>::Method0()
		calli default string(class G3_C1278`1<class BaseClass1>)
		ldstr "G2_C323::Method0.7761()"
		ldstr "class G2_C323`2<class BaseClass0,class BaseClass1> on type class G3_C1278`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C323`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C323`2<class BaseClass0,class BaseClass1>::ClassMethod1327<object>()
		calli default string(class G3_C1278`1<class BaseClass1>)
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G2_C323`2<class BaseClass0,class BaseClass1> on type class G3_C1278`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C323`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C323`2<class BaseClass0,class BaseClass1>::ClassMethod1326()
		calli default string(class G3_C1278`1<class BaseClass1>)
		ldstr "G2_C323::ClassMethod1326.MI.7774()"
		ldstr "class G2_C323`2<class BaseClass0,class BaseClass1> on type class G3_C1278`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method0()
		calli default string(class G3_C1278`1<class BaseClass1>)
		ldstr "G3_C1278::Method0.15286()"
		ldstr "IBase0 on type class G3_C1278`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method1()
		calli default string(class G3_C1278`1<class BaseClass1>)
		ldstr "G3_C1278::Method1.15287()"
		ldstr "IBase0 on type class G3_C1278`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method2<object>()
		calli default string(class G3_C1278`1<class BaseClass1>)
		ldstr "G3_C1278::Method2.MI.15289<System.Object>()"
		ldstr "IBase0 on type class G3_C1278`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method3<object>()
		calli default string(class G3_C1278`1<class BaseClass1>)
		ldstr "G3_C1278::Method3.MI.15291<System.Object>()"
		ldstr "IBase0 on type class G3_C1278`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1278`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1278`1<class BaseClass1>::ClassMethod3930()
		calli default string(class G3_C1278`1<class BaseClass1>)
		ldstr "G3_C1278::ClassMethod3930.15292()"
		ldstr "class G3_C1278`1<class BaseClass1> on type class G3_C1278`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1278`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1278`1<class BaseClass1>::Method3<object>()
		calli default string(class G3_C1278`1<class BaseClass1>)
		ldstr "G3_C1278::Method3.15290<System.Object>()"
		ldstr "class G3_C1278`1<class BaseClass1> on type class G3_C1278`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1278`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1278`1<class BaseClass1>::Method2<object>()
		calli default string(class G3_C1278`1<class BaseClass1>)
		ldstr "G3_C1278::Method2.15288<System.Object>()"
		ldstr "class G3_C1278`1<class BaseClass1> on type class G3_C1278`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1278`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1278`1<class BaseClass1>::Method1()
		calli default string(class G3_C1278`1<class BaseClass1>)
		ldstr "G3_C1278::Method1.15287()"
		ldstr "class G3_C1278`1<class BaseClass1> on type class G3_C1278`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1278`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1278`1<class BaseClass1>::Method0()
		calli default string(class G3_C1278`1<class BaseClass1>)
		ldstr "G3_C1278::Method0.15286()"
		ldstr "class G3_C1278`1<class BaseClass1> on type class G3_C1278`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1278`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1278`1<class BaseClass1>::ClassMethod1988<object>()
		calli default string(class G3_C1278`1<class BaseClass1>)
		ldstr "G3_C1278::ClassMethod1988.MI.15295<System.Object>()"
		ldstr "class G3_C1278`1<class BaseClass1> on type class G3_C1278`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1278`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1278`1<class BaseClass1>::ClassMethod1987<object>()
		calli default string(class G3_C1278`1<class BaseClass1>)
		ldstr "G3_C1278::ClassMethod1987.MI.15294<System.Object>()"
		ldstr "class G3_C1278`1<class BaseClass1> on type class G3_C1278`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1278`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1278`1<class BaseClass1>::ClassMethod1986()
		calli default string(class G3_C1278`1<class BaseClass1>)
		ldstr "G2_C323::ClassMethod1986.7771()"
		ldstr "class G3_C1278`1<class BaseClass1> on type class G3_C1278`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1278`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1278`1<class BaseClass1>::ClassMethod1985()
		calli default string(class G3_C1278`1<class BaseClass1>)
		ldstr "G3_C1278::ClassMethod1985.MI.15293()"
		ldstr "class G3_C1278`1<class BaseClass1> on type class G3_C1278`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1278`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1278`1<class BaseClass1>::Method7<object>()
		calli default string(class G3_C1278`1<class BaseClass1>)
		ldstr "G2_C323::Method7.7769<System.Object>()"
		ldstr "class G3_C1278`1<class BaseClass1> on type class G3_C1278`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1278`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1278`1<class BaseClass1>::ClassMethod1327<object>()
		calli default string(class G3_C1278`1<class BaseClass1>)
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G3_C1278`1<class BaseClass1> on type class G3_C1278`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1278`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1278`1<class BaseClass1>::ClassMethod1326()
		calli default string(class G3_C1278`1<class BaseClass1>)
		ldstr "G2_C323::ClassMethod1326.MI.7774()"
		ldstr "class G3_C1278`1<class BaseClass1> on type class G3_C1278`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C323`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C6`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass0,class BaseClass0>::ClassMethod1327<object>()
		calli default string(class G2_C323`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass0,class BaseClass0> on type class G2_C323`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C6`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass0,class BaseClass0>::ClassMethod1326()
		calli default string(class G2_C323`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C323::ClassMethod1326.MI.7774()"
		ldstr "class G1_C6`2<class BaseClass0,class BaseClass0> on type class G2_C323`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C6`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C323`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass0,class BaseClass0> on type class G2_C323`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G2_C323`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G2_C323`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C323`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C323`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C323`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C323::Method7.7769<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C323`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C323`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C323`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C323`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C323`2<class BaseClass0,class BaseClass0>::ClassMethod1988<object>()
		calli default string(class G2_C323`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C323::ClassMethod1988.7773<System.Object>()"
		ldstr "class G2_C323`2<class BaseClass0,class BaseClass0> on type class G2_C323`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C323`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C323`2<class BaseClass0,class BaseClass0>::ClassMethod1987<object>()
		calli default string(class G2_C323`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C323::ClassMethod1987.7772<System.Object>()"
		ldstr "class G2_C323`2<class BaseClass0,class BaseClass0> on type class G2_C323`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C323`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C323`2<class BaseClass0,class BaseClass0>::ClassMethod1986()
		calli default string(class G2_C323`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C323::ClassMethod1986.7771()"
		ldstr "class G2_C323`2<class BaseClass0,class BaseClass0> on type class G2_C323`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C323`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C323`2<class BaseClass0,class BaseClass0>::ClassMethod1985()
		calli default string(class G2_C323`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C323::ClassMethod1985.7770()"
		ldstr "class G2_C323`2<class BaseClass0,class BaseClass0> on type class G2_C323`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C323`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C323`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C323`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C323::Method7.7769<System.Object>()"
		ldstr "class G2_C323`2<class BaseClass0,class BaseClass0> on type class G2_C323`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C323`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C323`2<class BaseClass0,class BaseClass0>::Method3<object>()
		calli default string(class G2_C323`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C323::Method3.7767<System.Object>()"
		ldstr "class G2_C323`2<class BaseClass0,class BaseClass0> on type class G2_C323`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C323`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C323`2<class BaseClass0,class BaseClass0>::Method2<object>()
		calli default string(class G2_C323`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C323::Method2.7765<System.Object>()"
		ldstr "class G2_C323`2<class BaseClass0,class BaseClass0> on type class G2_C323`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C323`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C323`2<class BaseClass0,class BaseClass0>::Method1()
		calli default string(class G2_C323`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C323::Method1.7763()"
		ldstr "class G2_C323`2<class BaseClass0,class BaseClass0> on type class G2_C323`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C323`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C323`2<class BaseClass0,class BaseClass0>::Method0()
		calli default string(class G2_C323`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C323::Method0.7761()"
		ldstr "class G2_C323`2<class BaseClass0,class BaseClass0> on type class G2_C323`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C323`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C323`2<class BaseClass0,class BaseClass0>::ClassMethod1327<object>()
		calli default string(class G2_C323`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G2_C323`2<class BaseClass0,class BaseClass0> on type class G2_C323`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C323`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C323`2<class BaseClass0,class BaseClass0>::ClassMethod1326()
		calli default string(class G2_C323`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C323::ClassMethod1326.MI.7774()"
		ldstr "class G2_C323`2<class BaseClass0,class BaseClass0> on type class G2_C323`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method0()
		calli default string(class G2_C323`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C323::Method0.MI.7762()"
		ldstr "IBase0 on type class G2_C323`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method1()
		calli default string(class G2_C323`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C323::Method1.MI.7764()"
		ldstr "IBase0 on type class G2_C323`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method2<object>()
		calli default string(class G2_C323`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C323::Method2.MI.7766<System.Object>()"
		ldstr "IBase0 on type class G2_C323`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method3<object>()
		calli default string(class G2_C323`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C323::Method3.MI.7768<System.Object>()"
		ldstr "IBase0 on type class G2_C323`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C323`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C6`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass1,class BaseClass1>::ClassMethod1327<object>()
		calli default string(class G2_C323`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass1> on type class G2_C323`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C6`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass1,class BaseClass1>::ClassMethod1326()
		calli default string(class G2_C323`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C323::ClassMethod1326.MI.7774()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass1> on type class G2_C323`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C6`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C323`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass1> on type class G2_C323`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C323`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C323`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C323`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C323::Method7.7769<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C323`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C323`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C323`2<class BaseClass0,class BaseClass1>::ClassMethod1988<object>()
		calli default string(class G2_C323`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C323::ClassMethod1988.7773<System.Object>()"
		ldstr "class G2_C323`2<class BaseClass0,class BaseClass1> on type class G2_C323`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C323`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C323`2<class BaseClass0,class BaseClass1>::ClassMethod1987<object>()
		calli default string(class G2_C323`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C323::ClassMethod1987.7772<System.Object>()"
		ldstr "class G2_C323`2<class BaseClass0,class BaseClass1> on type class G2_C323`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C323`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C323`2<class BaseClass0,class BaseClass1>::ClassMethod1986()
		calli default string(class G2_C323`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C323::ClassMethod1986.7771()"
		ldstr "class G2_C323`2<class BaseClass0,class BaseClass1> on type class G2_C323`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C323`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C323`2<class BaseClass0,class BaseClass1>::ClassMethod1985()
		calli default string(class G2_C323`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C323::ClassMethod1985.7770()"
		ldstr "class G2_C323`2<class BaseClass0,class BaseClass1> on type class G2_C323`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C323`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C323`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C323`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C323::Method7.7769<System.Object>()"
		ldstr "class G2_C323`2<class BaseClass0,class BaseClass1> on type class G2_C323`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C323`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C323`2<class BaseClass0,class BaseClass1>::Method3<object>()
		calli default string(class G2_C323`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C323::Method3.7767<System.Object>()"
		ldstr "class G2_C323`2<class BaseClass0,class BaseClass1> on type class G2_C323`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C323`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C323`2<class BaseClass0,class BaseClass1>::Method2<object>()
		calli default string(class G2_C323`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C323::Method2.7765<System.Object>()"
		ldstr "class G2_C323`2<class BaseClass0,class BaseClass1> on type class G2_C323`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C323`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C323`2<class BaseClass0,class BaseClass1>::Method1()
		calli default string(class G2_C323`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C323::Method1.7763()"
		ldstr "class G2_C323`2<class BaseClass0,class BaseClass1> on type class G2_C323`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C323`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C323`2<class BaseClass0,class BaseClass1>::Method0()
		calli default string(class G2_C323`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C323::Method0.7761()"
		ldstr "class G2_C323`2<class BaseClass0,class BaseClass1> on type class G2_C323`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C323`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C323`2<class BaseClass0,class BaseClass1>::ClassMethod1327<object>()
		calli default string(class G2_C323`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G2_C323`2<class BaseClass0,class BaseClass1> on type class G2_C323`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C323`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C323`2<class BaseClass0,class BaseClass1>::ClassMethod1326()
		calli default string(class G2_C323`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C323::ClassMethod1326.MI.7774()"
		ldstr "class G2_C323`2<class BaseClass0,class BaseClass1> on type class G2_C323`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method0()
		calli default string(class G2_C323`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C323::Method0.MI.7762()"
		ldstr "IBase0 on type class G2_C323`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method1()
		calli default string(class G2_C323`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C323::Method1.MI.7764()"
		ldstr "IBase0 on type class G2_C323`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method2<object>()
		calli default string(class G2_C323`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C323::Method2.MI.7766<System.Object>()"
		ldstr "IBase0 on type class G2_C323`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method3<object>()
		calli default string(class G2_C323`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C323::Method3.MI.7768<System.Object>()"
		ldstr "IBase0 on type class G2_C323`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C323`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C6`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass0,class BaseClass0>::ClassMethod1327<object>()
		calli default string(class G2_C323`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass0,class BaseClass0> on type class G2_C323`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C6`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass0,class BaseClass0>::ClassMethod1326()
		calli default string(class G2_C323`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C323::ClassMethod1326.MI.7774()"
		ldstr "class G1_C6`2<class BaseClass0,class BaseClass0> on type class G2_C323`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C6`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C323`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass0,class BaseClass0> on type class G2_C323`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G2_C323`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G2_C323`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C323`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C323`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C323`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C323::Method7.7769<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C323`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C323`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C323`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C323`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C323`2<class BaseClass1,class BaseClass0>::ClassMethod1988<object>()
		calli default string(class G2_C323`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C323::ClassMethod1988.7773<System.Object>()"
		ldstr "class G2_C323`2<class BaseClass1,class BaseClass0> on type class G2_C323`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C323`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C323`2<class BaseClass1,class BaseClass0>::ClassMethod1987<object>()
		calli default string(class G2_C323`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C323::ClassMethod1987.7772<System.Object>()"
		ldstr "class G2_C323`2<class BaseClass1,class BaseClass0> on type class G2_C323`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C323`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C323`2<class BaseClass1,class BaseClass0>::ClassMethod1986()
		calli default string(class G2_C323`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C323::ClassMethod1986.7771()"
		ldstr "class G2_C323`2<class BaseClass1,class BaseClass0> on type class G2_C323`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C323`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C323`2<class BaseClass1,class BaseClass0>::ClassMethod1985()
		calli default string(class G2_C323`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C323::ClassMethod1985.7770()"
		ldstr "class G2_C323`2<class BaseClass1,class BaseClass0> on type class G2_C323`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C323`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C323`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G2_C323`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C323::Method7.7769<System.Object>()"
		ldstr "class G2_C323`2<class BaseClass1,class BaseClass0> on type class G2_C323`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C323`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C323`2<class BaseClass1,class BaseClass0>::Method3<object>()
		calli default string(class G2_C323`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C323::Method3.7767<System.Object>()"
		ldstr "class G2_C323`2<class BaseClass1,class BaseClass0> on type class G2_C323`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C323`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C323`2<class BaseClass1,class BaseClass0>::Method2<object>()
		calli default string(class G2_C323`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C323::Method2.7765<System.Object>()"
		ldstr "class G2_C323`2<class BaseClass1,class BaseClass0> on type class G2_C323`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C323`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C323`2<class BaseClass1,class BaseClass0>::Method1()
		calli default string(class G2_C323`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C323::Method1.7763()"
		ldstr "class G2_C323`2<class BaseClass1,class BaseClass0> on type class G2_C323`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C323`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C323`2<class BaseClass1,class BaseClass0>::Method0()
		calli default string(class G2_C323`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C323::Method0.7761()"
		ldstr "class G2_C323`2<class BaseClass1,class BaseClass0> on type class G2_C323`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C323`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C323`2<class BaseClass1,class BaseClass0>::ClassMethod1327<object>()
		calli default string(class G2_C323`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G2_C323`2<class BaseClass1,class BaseClass0> on type class G2_C323`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C323`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C323`2<class BaseClass1,class BaseClass0>::ClassMethod1326()
		calli default string(class G2_C323`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C323::ClassMethod1326.MI.7774()"
		ldstr "class G2_C323`2<class BaseClass1,class BaseClass0> on type class G2_C323`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method0()
		calli default string(class G2_C323`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C323::Method0.MI.7762()"
		ldstr "IBase0 on type class G2_C323`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method1()
		calli default string(class G2_C323`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C323::Method1.MI.7764()"
		ldstr "IBase0 on type class G2_C323`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method2<object>()
		calli default string(class G2_C323`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C323::Method2.MI.7766<System.Object>()"
		ldstr "IBase0 on type class G2_C323`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method3<object>()
		calli default string(class G2_C323`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C323::Method3.MI.7768<System.Object>()"
		ldstr "IBase0 on type class G2_C323`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C323`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C6`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass1,class BaseClass1>::ClassMethod1327<object>()
		calli default string(class G2_C323`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass1> on type class G2_C323`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C6`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass1,class BaseClass1>::ClassMethod1326()
		calli default string(class G2_C323`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C323::ClassMethod1326.MI.7774()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass1> on type class G2_C323`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C6`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C323`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass1> on type class G2_C323`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C323`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C323`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C323`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C323::Method7.7769<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C323`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C323`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C323`2<class BaseClass1,class BaseClass1>::ClassMethod1988<object>()
		calli default string(class G2_C323`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C323::ClassMethod1988.7773<System.Object>()"
		ldstr "class G2_C323`2<class BaseClass1,class BaseClass1> on type class G2_C323`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C323`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C323`2<class BaseClass1,class BaseClass1>::ClassMethod1987<object>()
		calli default string(class G2_C323`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C323::ClassMethod1987.7772<System.Object>()"
		ldstr "class G2_C323`2<class BaseClass1,class BaseClass1> on type class G2_C323`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C323`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C323`2<class BaseClass1,class BaseClass1>::ClassMethod1986()
		calli default string(class G2_C323`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C323::ClassMethod1986.7771()"
		ldstr "class G2_C323`2<class BaseClass1,class BaseClass1> on type class G2_C323`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C323`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C323`2<class BaseClass1,class BaseClass1>::ClassMethod1985()
		calli default string(class G2_C323`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C323::ClassMethod1985.7770()"
		ldstr "class G2_C323`2<class BaseClass1,class BaseClass1> on type class G2_C323`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C323`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C323`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C323`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C323::Method7.7769<System.Object>()"
		ldstr "class G2_C323`2<class BaseClass1,class BaseClass1> on type class G2_C323`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C323`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C323`2<class BaseClass1,class BaseClass1>::Method3<object>()
		calli default string(class G2_C323`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C323::Method3.7767<System.Object>()"
		ldstr "class G2_C323`2<class BaseClass1,class BaseClass1> on type class G2_C323`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C323`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C323`2<class BaseClass1,class BaseClass1>::Method2<object>()
		calli default string(class G2_C323`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C323::Method2.7765<System.Object>()"
		ldstr "class G2_C323`2<class BaseClass1,class BaseClass1> on type class G2_C323`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C323`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C323`2<class BaseClass1,class BaseClass1>::Method1()
		calli default string(class G2_C323`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C323::Method1.7763()"
		ldstr "class G2_C323`2<class BaseClass1,class BaseClass1> on type class G2_C323`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C323`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C323`2<class BaseClass1,class BaseClass1>::Method0()
		calli default string(class G2_C323`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C323::Method0.7761()"
		ldstr "class G2_C323`2<class BaseClass1,class BaseClass1> on type class G2_C323`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C323`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C323`2<class BaseClass1,class BaseClass1>::ClassMethod1327<object>()
		calli default string(class G2_C323`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G2_C323`2<class BaseClass1,class BaseClass1> on type class G2_C323`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C323`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C323`2<class BaseClass1,class BaseClass1>::ClassMethod1326()
		calli default string(class G2_C323`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C323::ClassMethod1326.MI.7774()"
		ldstr "class G2_C323`2<class BaseClass1,class BaseClass1> on type class G2_C323`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method0()
		calli default string(class G2_C323`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C323::Method0.MI.7762()"
		ldstr "IBase0 on type class G2_C323`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method1()
		calli default string(class G2_C323`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C323::Method1.MI.7764()"
		ldstr "IBase0 on type class G2_C323`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method2<object>()
		calli default string(class G2_C323`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C323::Method2.MI.7766<System.Object>()"
		ldstr "IBase0 on type class G2_C323`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method3<object>()
		calli default string(class G2_C323`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C323::Method3.MI.7768<System.Object>()"
		ldstr "IBase0 on type class G2_C323`2<class BaseClass1,class BaseClass1>"
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
		call void Generated806::MethodCallingTest()
		call void Generated806::ConstrainedCallsTest()
		call void Generated806::StructConstrainedInterfaceCallsTest()
		call void Generated806::CalliTest()
		ldc.i4 100
		ret
	}
}
