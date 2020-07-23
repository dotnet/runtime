// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) .ver 4:0:0:0 }
.assembly extern TestFramework { .publickeytoken = ( B0 3F 5F 7F 11 D5 0A 3A )  }

//TYPES IN FORWARDER ASSEMBLIES:

//TEST ASSEMBLY:
.assembly Generated776 { .hash algorithm 0x00008004 }
.module Generated776.exe

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
.class public G3_C1248`1<T0> 
		extends class G2_C295`2<!T0,class BaseClass1>
		implements class IBase1`1<class BaseClass0> 
{
	.method public hidebysig virtual instance string Method4() cil managed noinlining { 
		ldstr "G3_C1248::Method4.15103()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class BaseClass0>.Method4'() cil managed noinlining { 
		.override method instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G3_C1248::Method4.MI.15104()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method5() cil managed noinlining { 
		ldstr "G3_C1248::Method5.15105()"
		ret
	}
	.method public hidebysig virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G3_C1248::Method6.15106<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod3876<M0>() cil managed noinlining { 
		ldstr "G3_C1248::ClassMethod3876.15107<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class G2_C295`2<!T0,class BaseClass1>::.ctor()
		ret
	}
}
.class public abstract G2_C295`2<T0, T1> 
		extends class G1_C6`2<class BaseClass1,!T1>
		implements class IBase1`1<class BaseClass0>, class IBase2`2<class BaseClass0,class BaseClass1> 
{
	.method public hidebysig newslot virtual instance string Method4() cil managed noinlining { 
		ldstr "G2_C295::Method4.7481()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class BaseClass0>.Method4'() cil managed noinlining { 
		.override method instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C295::Method4.MI.7482()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method5() cil managed noinlining { 
		ldstr "G2_C295::Method5.7483()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class BaseClass0>.Method5'() cil managed noinlining { 
		.override method instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C295::Method5.MI.7484()"
		ret
	}
	.method public hidebysig virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G2_C295::Method6.7485<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G2_C295::Method7.7486<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase2<class BaseClass0,class BaseClass1>.Method7'<M0>() cil managed noinlining { 
		.override method instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<[1]>()
		ldstr "G2_C295::Method7.MI.7487<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1919() cil managed noinlining { 
		ldstr "G2_C295::ClassMethod1919.7488()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1920() cil managed noinlining { 
		ldstr "G2_C295::ClassMethod1920.7489()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C6<class BaseClass1,T1>.ClassMethod1326'() cil managed noinlining { 
		.override method instance string class G1_C6`2<class BaseClass1,!T1>::ClassMethod1326()
		ldstr "G2_C295::ClassMethod1326.MI.7490()"
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class G1_C6`2<class BaseClass1,!T1>::.ctor()
		ret
	}
}
.class interface public abstract IBase1`1<+T0> 
{
	.method public hidebysig newslot abstract virtual instance string Method4() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method5() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method6<M0>() cil managed { }
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
	.method static void M.G3_C1248.T<T0,(class G3_C1248`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1248.T<T0,(class G3_C1248`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1248`1<!!T0>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1248`1<!!T0>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1248`1<!!T0>::ClassMethod1919()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1248`1<!!T0>::ClassMethod1920()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1248`1<!!T0>::ClassMethod3876<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1248`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1248`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1248`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1248`1<!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1248.A<(class G3_C1248`1<class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1248.A<(class G3_C1248`1<class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1248`1<class BaseClass0>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1248`1<class BaseClass0>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1248`1<class BaseClass0>::ClassMethod1919()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1248`1<class BaseClass0>::ClassMethod1920()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1248`1<class BaseClass0>::ClassMethod3876<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1248`1<class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1248`1<class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1248`1<class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1248`1<class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1248.B<(class G3_C1248`1<class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1248.B<(class G3_C1248`1<class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1248`1<class BaseClass1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1248`1<class BaseClass1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1248`1<class BaseClass1>::ClassMethod1919()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1248`1<class BaseClass1>::ClassMethod1920()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1248`1<class BaseClass1>::ClassMethod3876<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1248`1<class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1248`1<class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1248`1<class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1248`1<class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C295.T.T<T0,T1,(class G2_C295`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C295.T.T<T0,T1,(class G2_C295`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C295`2<!!T0,!!T1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C295`2<!!T0,!!T1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C295`2<!!T0,!!T1>::ClassMethod1919()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C295`2<!!T0,!!T1>::ClassMethod1920()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C295`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C295`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C295`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C295`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C295.A.T<T1,(class G2_C295`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C295.A.T<T1,(class G2_C295`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C295`2<class BaseClass0,!!T1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C295`2<class BaseClass0,!!T1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C295`2<class BaseClass0,!!T1>::ClassMethod1919()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C295`2<class BaseClass0,!!T1>::ClassMethod1920()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C295`2<class BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C295`2<class BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C295`2<class BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C295`2<class BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C295.A.A<(class G2_C295`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C295.A.A<(class G2_C295`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C295`2<class BaseClass0,class BaseClass0>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C295`2<class BaseClass0,class BaseClass0>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C295`2<class BaseClass0,class BaseClass0>::ClassMethod1919()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C295`2<class BaseClass0,class BaseClass0>::ClassMethod1920()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C295`2<class BaseClass0,class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C295`2<class BaseClass0,class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C295`2<class BaseClass0,class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C295`2<class BaseClass0,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C295.A.B<(class G2_C295`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C295.A.B<(class G2_C295`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C295`2<class BaseClass0,class BaseClass1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C295`2<class BaseClass0,class BaseClass1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C295`2<class BaseClass0,class BaseClass1>::ClassMethod1919()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C295`2<class BaseClass0,class BaseClass1>::ClassMethod1920()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C295`2<class BaseClass0,class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C295`2<class BaseClass0,class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C295`2<class BaseClass0,class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C295`2<class BaseClass0,class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C295.B.T<T1,(class G2_C295`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C295.B.T<T1,(class G2_C295`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C295`2<class BaseClass1,!!T1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C295`2<class BaseClass1,!!T1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C295`2<class BaseClass1,!!T1>::ClassMethod1919()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C295`2<class BaseClass1,!!T1>::ClassMethod1920()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C295`2<class BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C295`2<class BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C295`2<class BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C295`2<class BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C295.B.A<(class G2_C295`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C295.B.A<(class G2_C295`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C295`2<class BaseClass1,class BaseClass0>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C295`2<class BaseClass1,class BaseClass0>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C295`2<class BaseClass1,class BaseClass0>::ClassMethod1919()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C295`2<class BaseClass1,class BaseClass0>::ClassMethod1920()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C295`2<class BaseClass1,class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C295`2<class BaseClass1,class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C295`2<class BaseClass1,class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C295`2<class BaseClass1,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C295.B.B<(class G2_C295`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C295.B.B<(class G2_C295`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C295`2<class BaseClass1,class BaseClass1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C295`2<class BaseClass1,class BaseClass1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C295`2<class BaseClass1,class BaseClass1>::ClassMethod1919()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C295`2<class BaseClass1,class BaseClass1>::ClassMethod1920()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C295`2<class BaseClass1,class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C295`2<class BaseClass1,class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C295`2<class BaseClass1,class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C295`2<class BaseClass1,class BaseClass1>::Method7<object>()
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
		newobj instance void class G3_C1248`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C6`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C6`2<class BaseClass1,class BaseClass1>::ClassMethod1327<object>()
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass1> on type class G3_C1248`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C6`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C6`2<class BaseClass1,class BaseClass1>::ClassMethod1326()
		ldstr "G2_C295::ClassMethod1326.MI.7490()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass1> on type class G3_C1248`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C6`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C6`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C295::Method7.7486<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass1> on type class G3_C1248`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C295::Method7.7486<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1248`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C295::Method7.MI.7487<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1248`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C295`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C295`2<class BaseClass0,class BaseClass1>::ClassMethod1920()
		ldstr "G2_C295::ClassMethod1920.7489()"
		ldstr "class G2_C295`2<class BaseClass0,class BaseClass1> on type class G3_C1248`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C295`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C295`2<class BaseClass0,class BaseClass1>::ClassMethod1919()
		ldstr "G2_C295::ClassMethod1919.7488()"
		ldstr "class G2_C295`2<class BaseClass0,class BaseClass1> on type class G3_C1248`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C295`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C295`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C295::Method7.7486<System.Object>()"
		ldstr "class G2_C295`2<class BaseClass0,class BaseClass1> on type class G3_C1248`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C295`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C295`2<class BaseClass0,class BaseClass1>::Method6<object>()
		ldstr "G3_C1248::Method6.15106<System.Object>()"
		ldstr "class G2_C295`2<class BaseClass0,class BaseClass1> on type class G3_C1248`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C295`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C295`2<class BaseClass0,class BaseClass1>::Method5()
		ldstr "G2_C295::Method5.7483()"
		ldstr "class G2_C295`2<class BaseClass0,class BaseClass1> on type class G3_C1248`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C295`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C295`2<class BaseClass0,class BaseClass1>::Method4()
		ldstr "G3_C1248::Method4.15103()"
		ldstr "class G2_C295`2<class BaseClass0,class BaseClass1> on type class G3_C1248`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C295`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C295`2<class BaseClass0,class BaseClass1>::ClassMethod1327<object>()
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G2_C295`2<class BaseClass0,class BaseClass1> on type class G3_C1248`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C295`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C295`2<class BaseClass0,class BaseClass1>::ClassMethod1326()
		ldstr "G2_C295::ClassMethod1326.MI.7490()"
		ldstr "class G2_C295`2<class BaseClass0,class BaseClass1> on type class G3_C1248`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G3_C1248::Method4.MI.15104()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1248`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G3_C1248::Method5.15105()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1248`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G3_C1248::Method6.15106<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1248`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G3_C1248`1<class BaseClass0>
		callvirt instance string class G3_C1248`1<class BaseClass0>::ClassMethod3876<object>()
		ldstr "G3_C1248::ClassMethod3876.15107<System.Object>()"
		ldstr "class G3_C1248`1<class BaseClass0> on type class G3_C1248`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1248`1<class BaseClass0>
		callvirt instance string class G3_C1248`1<class BaseClass0>::Method6<object>()
		ldstr "G3_C1248::Method6.15106<System.Object>()"
		ldstr "class G3_C1248`1<class BaseClass0> on type class G3_C1248`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1248`1<class BaseClass0>
		callvirt instance string class G3_C1248`1<class BaseClass0>::Method5()
		ldstr "G3_C1248::Method5.15105()"
		ldstr "class G3_C1248`1<class BaseClass0> on type class G3_C1248`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1248`1<class BaseClass0>
		callvirt instance string class G3_C1248`1<class BaseClass0>::Method4()
		ldstr "G3_C1248::Method4.15103()"
		ldstr "class G3_C1248`1<class BaseClass0> on type class G3_C1248`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1248`1<class BaseClass0>
		callvirt instance string class G3_C1248`1<class BaseClass0>::ClassMethod1920()
		ldstr "G2_C295::ClassMethod1920.7489()"
		ldstr "class G3_C1248`1<class BaseClass0> on type class G3_C1248`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1248`1<class BaseClass0>
		callvirt instance string class G3_C1248`1<class BaseClass0>::ClassMethod1919()
		ldstr "G2_C295::ClassMethod1919.7488()"
		ldstr "class G3_C1248`1<class BaseClass0> on type class G3_C1248`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1248`1<class BaseClass0>
		callvirt instance string class G3_C1248`1<class BaseClass0>::Method7<object>()
		ldstr "G2_C295::Method7.7486<System.Object>()"
		ldstr "class G3_C1248`1<class BaseClass0> on type class G3_C1248`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1248`1<class BaseClass0>
		callvirt instance string class G3_C1248`1<class BaseClass0>::ClassMethod1327<object>()
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G3_C1248`1<class BaseClass0> on type class G3_C1248`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1248`1<class BaseClass0>
		callvirt instance string class G3_C1248`1<class BaseClass0>::ClassMethod1326()
		ldstr "G2_C295::ClassMethod1326.MI.7490()"
		ldstr "class G3_C1248`1<class BaseClass0> on type class G3_C1248`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G3_C1248`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C6`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C6`2<class BaseClass1,class BaseClass1>::ClassMethod1327<object>()
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass1> on type class G3_C1248`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C6`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C6`2<class BaseClass1,class BaseClass1>::ClassMethod1326()
		ldstr "G2_C295::ClassMethod1326.MI.7490()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass1> on type class G3_C1248`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C6`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C6`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C295::Method7.7486<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass1> on type class G3_C1248`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C295::Method7.7486<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1248`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C295::Method7.MI.7487<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1248`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C295`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C295`2<class BaseClass1,class BaseClass1>::ClassMethod1920()
		ldstr "G2_C295::ClassMethod1920.7489()"
		ldstr "class G2_C295`2<class BaseClass1,class BaseClass1> on type class G3_C1248`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C295`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C295`2<class BaseClass1,class BaseClass1>::ClassMethod1919()
		ldstr "G2_C295::ClassMethod1919.7488()"
		ldstr "class G2_C295`2<class BaseClass1,class BaseClass1> on type class G3_C1248`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C295`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C295`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C295::Method7.7486<System.Object>()"
		ldstr "class G2_C295`2<class BaseClass1,class BaseClass1> on type class G3_C1248`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C295`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C295`2<class BaseClass1,class BaseClass1>::Method6<object>()
		ldstr "G3_C1248::Method6.15106<System.Object>()"
		ldstr "class G2_C295`2<class BaseClass1,class BaseClass1> on type class G3_C1248`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C295`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C295`2<class BaseClass1,class BaseClass1>::Method5()
		ldstr "G2_C295::Method5.7483()"
		ldstr "class G2_C295`2<class BaseClass1,class BaseClass1> on type class G3_C1248`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C295`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C295`2<class BaseClass1,class BaseClass1>::Method4()
		ldstr "G3_C1248::Method4.15103()"
		ldstr "class G2_C295`2<class BaseClass1,class BaseClass1> on type class G3_C1248`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C295`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C295`2<class BaseClass1,class BaseClass1>::ClassMethod1327<object>()
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G2_C295`2<class BaseClass1,class BaseClass1> on type class G3_C1248`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C295`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C295`2<class BaseClass1,class BaseClass1>::ClassMethod1326()
		ldstr "G2_C295::ClassMethod1326.MI.7490()"
		ldstr "class G2_C295`2<class BaseClass1,class BaseClass1> on type class G3_C1248`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G3_C1248::Method4.MI.15104()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1248`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G3_C1248::Method5.15105()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1248`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G3_C1248::Method6.15106<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1248`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G3_C1248`1<class BaseClass1>
		callvirt instance string class G3_C1248`1<class BaseClass1>::ClassMethod3876<object>()
		ldstr "G3_C1248::ClassMethod3876.15107<System.Object>()"
		ldstr "class G3_C1248`1<class BaseClass1> on type class G3_C1248`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1248`1<class BaseClass1>
		callvirt instance string class G3_C1248`1<class BaseClass1>::Method6<object>()
		ldstr "G3_C1248::Method6.15106<System.Object>()"
		ldstr "class G3_C1248`1<class BaseClass1> on type class G3_C1248`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1248`1<class BaseClass1>
		callvirt instance string class G3_C1248`1<class BaseClass1>::Method5()
		ldstr "G3_C1248::Method5.15105()"
		ldstr "class G3_C1248`1<class BaseClass1> on type class G3_C1248`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1248`1<class BaseClass1>
		callvirt instance string class G3_C1248`1<class BaseClass1>::Method4()
		ldstr "G3_C1248::Method4.15103()"
		ldstr "class G3_C1248`1<class BaseClass1> on type class G3_C1248`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1248`1<class BaseClass1>
		callvirt instance string class G3_C1248`1<class BaseClass1>::ClassMethod1920()
		ldstr "G2_C295::ClassMethod1920.7489()"
		ldstr "class G3_C1248`1<class BaseClass1> on type class G3_C1248`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1248`1<class BaseClass1>
		callvirt instance string class G3_C1248`1<class BaseClass1>::ClassMethod1919()
		ldstr "G2_C295::ClassMethod1919.7488()"
		ldstr "class G3_C1248`1<class BaseClass1> on type class G3_C1248`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1248`1<class BaseClass1>
		callvirt instance string class G3_C1248`1<class BaseClass1>::Method7<object>()
		ldstr "G2_C295::Method7.7486<System.Object>()"
		ldstr "class G3_C1248`1<class BaseClass1> on type class G3_C1248`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1248`1<class BaseClass1>
		callvirt instance string class G3_C1248`1<class BaseClass1>::ClassMethod1327<object>()
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G3_C1248`1<class BaseClass1> on type class G3_C1248`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1248`1<class BaseClass1>
		callvirt instance string class G3_C1248`1<class BaseClass1>::ClassMethod1326()
		ldstr "G2_C295::ClassMethod1326.MI.7490()"
		ldstr "class G3_C1248`1<class BaseClass1> on type class G3_C1248`1<class BaseClass1>"
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
		newobj instance void class G3_C1248`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C295::ClassMethod1326.MI.7490()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C295::Method7.7486<System.Object>()#"
		call void Framework::M.G1_C6.T.T<class BaseClass1,class BaseClass1,class G3_C1248`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C295::ClassMethod1326.MI.7490()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C295::Method7.7486<System.Object>()#"
		call void Framework::M.G1_C6.B.T<class BaseClass1,class G3_C1248`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C295::ClassMethod1326.MI.7490()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C295::Method7.7486<System.Object>()#"
		call void Framework::M.G1_C6.B.B<class G3_C1248`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C295::Method7.7486<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G3_C1248`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C295::Method7.7486<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G3_C1248`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C295::Method7.7486<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G3_C1248`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C295::Method7.MI.7487<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G3_C1248`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C295::Method7.MI.7487<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G3_C1248`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C295::Method7.MI.7487<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G3_C1248`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C295::ClassMethod1326.MI.7490()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C295::ClassMethod1919.7488()#G2_C295::ClassMethod1920.7489()#G3_C1248::Method4.15103()#G2_C295::Method5.7483()#G3_C1248::Method6.15106<System.Object>()#G2_C295::Method7.7486<System.Object>()#"
		call void Framework::M.G2_C295.T.T<class BaseClass0,class BaseClass1,class G3_C1248`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C295::ClassMethod1326.MI.7490()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C295::ClassMethod1919.7488()#G2_C295::ClassMethod1920.7489()#G3_C1248::Method4.15103()#G2_C295::Method5.7483()#G3_C1248::Method6.15106<System.Object>()#G2_C295::Method7.7486<System.Object>()#"
		call void Framework::M.G2_C295.A.T<class BaseClass1,class G3_C1248`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C295::ClassMethod1326.MI.7490()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C295::ClassMethod1919.7488()#G2_C295::ClassMethod1920.7489()#G3_C1248::Method4.15103()#G2_C295::Method5.7483()#G3_C1248::Method6.15106<System.Object>()#G2_C295::Method7.7486<System.Object>()#"
		call void Framework::M.G2_C295.A.B<class G3_C1248`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1248::Method4.MI.15104()#G3_C1248::Method5.15105()#G3_C1248::Method6.15106<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G3_C1248`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1248::Method4.MI.15104()#G3_C1248::Method5.15105()#G3_C1248::Method6.15106<System.Object>()#"
		call void Framework::M.IBase1.A<class G3_C1248`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C295::ClassMethod1326.MI.7490()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C295::ClassMethod1919.7488()#G2_C295::ClassMethod1920.7489()#G3_C1248::ClassMethod3876.15107<System.Object>()#G3_C1248::Method4.15103()#G3_C1248::Method5.15105()#G3_C1248::Method6.15106<System.Object>()#G2_C295::Method7.7486<System.Object>()#"
		call void Framework::M.G3_C1248.T<class BaseClass0,class G3_C1248`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C295::ClassMethod1326.MI.7490()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C295::ClassMethod1919.7488()#G2_C295::ClassMethod1920.7489()#G3_C1248::ClassMethod3876.15107<System.Object>()#G3_C1248::Method4.15103()#G3_C1248::Method5.15105()#G3_C1248::Method6.15106<System.Object>()#G2_C295::Method7.7486<System.Object>()#"
		call void Framework::M.G3_C1248.A<class G3_C1248`1<class BaseClass0>>(!!0,string)
		newobj instance void class G3_C1248`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C295::ClassMethod1326.MI.7490()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C295::Method7.7486<System.Object>()#"
		call void Framework::M.G1_C6.T.T<class BaseClass1,class BaseClass1,class G3_C1248`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C295::ClassMethod1326.MI.7490()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C295::Method7.7486<System.Object>()#"
		call void Framework::M.G1_C6.B.T<class BaseClass1,class G3_C1248`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C295::ClassMethod1326.MI.7490()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C295::Method7.7486<System.Object>()#"
		call void Framework::M.G1_C6.B.B<class G3_C1248`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C295::Method7.7486<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G3_C1248`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C295::Method7.7486<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G3_C1248`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C295::Method7.7486<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G3_C1248`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C295::Method7.MI.7487<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G3_C1248`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C295::Method7.MI.7487<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G3_C1248`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C295::Method7.MI.7487<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G3_C1248`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C295::ClassMethod1326.MI.7490()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C295::ClassMethod1919.7488()#G2_C295::ClassMethod1920.7489()#G3_C1248::Method4.15103()#G2_C295::Method5.7483()#G3_C1248::Method6.15106<System.Object>()#G2_C295::Method7.7486<System.Object>()#"
		call void Framework::M.G2_C295.T.T<class BaseClass1,class BaseClass1,class G3_C1248`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C295::ClassMethod1326.MI.7490()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C295::ClassMethod1919.7488()#G2_C295::ClassMethod1920.7489()#G3_C1248::Method4.15103()#G2_C295::Method5.7483()#G3_C1248::Method6.15106<System.Object>()#G2_C295::Method7.7486<System.Object>()#"
		call void Framework::M.G2_C295.B.T<class BaseClass1,class G3_C1248`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C295::ClassMethod1326.MI.7490()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C295::ClassMethod1919.7488()#G2_C295::ClassMethod1920.7489()#G3_C1248::Method4.15103()#G2_C295::Method5.7483()#G3_C1248::Method6.15106<System.Object>()#G2_C295::Method7.7486<System.Object>()#"
		call void Framework::M.G2_C295.B.B<class G3_C1248`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1248::Method4.MI.15104()#G3_C1248::Method5.15105()#G3_C1248::Method6.15106<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G3_C1248`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1248::Method4.MI.15104()#G3_C1248::Method5.15105()#G3_C1248::Method6.15106<System.Object>()#"
		call void Framework::M.IBase1.A<class G3_C1248`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C295::ClassMethod1326.MI.7490()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C295::ClassMethod1919.7488()#G2_C295::ClassMethod1920.7489()#G3_C1248::ClassMethod3876.15107<System.Object>()#G3_C1248::Method4.15103()#G3_C1248::Method5.15105()#G3_C1248::Method6.15106<System.Object>()#G2_C295::Method7.7486<System.Object>()#"
		call void Framework::M.G3_C1248.T<class BaseClass1,class G3_C1248`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C295::ClassMethod1326.MI.7490()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C295::ClassMethod1919.7488()#G2_C295::ClassMethod1920.7489()#G3_C1248::ClassMethod3876.15107<System.Object>()#G3_C1248::Method4.15103()#G3_C1248::Method5.15105()#G3_C1248::Method6.15106<System.Object>()#G2_C295::Method7.7486<System.Object>()#"
		call void Framework::M.G3_C1248.B<class G3_C1248`1<class BaseClass1>>(!!0,string)
		newobj instance void class G1_C6`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.G1_C6.T.T<class BaseClass0,class BaseClass0,class G1_C6`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.G1_C6.A.T<class BaseClass0,class G1_C6`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.G1_C6.A.A<class G1_C6`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass0,class G1_C6`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass0,class G1_C6`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.IBase2.B.A<class G1_C6`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G1_C6`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass0,class G1_C6`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.IBase2.A.A<class G1_C6`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G1_C6`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G1_C6`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G1_C6`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G1_C6`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G1_C6`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G1_C6`2<class BaseClass0,class BaseClass0>>(!!0,string)
		newobj instance void class G1_C6`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.G1_C6.T.T<class BaseClass0,class BaseClass1,class G1_C6`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.G1_C6.A.T<class BaseClass1,class G1_C6`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.G1_C6.A.B<class G1_C6`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass0,class G1_C6`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass0,class G1_C6`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.IBase2.B.A<class G1_C6`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G1_C6`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass0,class G1_C6`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.IBase2.A.A<class G1_C6`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G1_C6`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G1_C6`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G1_C6`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G1_C6`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G1_C6`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G1_C6`2<class BaseClass0,class BaseClass1>>(!!0,string)
		newobj instance void class G1_C6`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.G1_C6.T.T<class BaseClass1,class BaseClass0,class G1_C6`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.G1_C6.B.T<class BaseClass0,class G1_C6`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.G1_C6.B.A<class G1_C6`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G1_C6`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G1_C6`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G1_C6`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G1_C6`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G1_C6`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G1_C6`2<class BaseClass1,class BaseClass0>>(!!0,string)
		newobj instance void class G1_C6`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.G1_C6.T.T<class BaseClass1,class BaseClass1,class G1_C6`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.G1_C6.B.T<class BaseClass1,class G1_C6`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.G1_C6.B.B<class G1_C6`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G1_C6`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G1_C6`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G1_C6`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G1_C6`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G1_C6`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G1_C6`2<class BaseClass1,class BaseClass1>>(!!0,string)
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
		newobj instance void class G3_C1248`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C6`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass1,class BaseClass1>::ClassMethod1327<object>()
		calli default string(class G3_C1248`1<class BaseClass0>)
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass1> on type class G3_C1248`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C6`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass1,class BaseClass1>::ClassMethod1326()
		calli default string(class G3_C1248`1<class BaseClass0>)
		ldstr "G2_C295::ClassMethod1326.MI.7490()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass1> on type class G3_C1248`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C6`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1248`1<class BaseClass0>)
		ldstr "G2_C295::Method7.7486<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass1> on type class G3_C1248`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1248`1<class BaseClass0>)
		ldstr "G2_C295::Method7.7486<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1248`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1248`1<class BaseClass0>)
		ldstr "G2_C295::Method7.MI.7487<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1248`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C295`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C295`2<class BaseClass0,class BaseClass1>::ClassMethod1920()
		calli default string(class G3_C1248`1<class BaseClass0>)
		ldstr "G2_C295::ClassMethod1920.7489()"
		ldstr "class G2_C295`2<class BaseClass0,class BaseClass1> on type class G3_C1248`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C295`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C295`2<class BaseClass0,class BaseClass1>::ClassMethod1919()
		calli default string(class G3_C1248`1<class BaseClass0>)
		ldstr "G2_C295::ClassMethod1919.7488()"
		ldstr "class G2_C295`2<class BaseClass0,class BaseClass1> on type class G3_C1248`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C295`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C295`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1248`1<class BaseClass0>)
		ldstr "G2_C295::Method7.7486<System.Object>()"
		ldstr "class G2_C295`2<class BaseClass0,class BaseClass1> on type class G3_C1248`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C295`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C295`2<class BaseClass0,class BaseClass1>::Method6<object>()
		calli default string(class G3_C1248`1<class BaseClass0>)
		ldstr "G3_C1248::Method6.15106<System.Object>()"
		ldstr "class G2_C295`2<class BaseClass0,class BaseClass1> on type class G3_C1248`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C295`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C295`2<class BaseClass0,class BaseClass1>::Method5()
		calli default string(class G3_C1248`1<class BaseClass0>)
		ldstr "G2_C295::Method5.7483()"
		ldstr "class G2_C295`2<class BaseClass0,class BaseClass1> on type class G3_C1248`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C295`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C295`2<class BaseClass0,class BaseClass1>::Method4()
		calli default string(class G3_C1248`1<class BaseClass0>)
		ldstr "G3_C1248::Method4.15103()"
		ldstr "class G2_C295`2<class BaseClass0,class BaseClass1> on type class G3_C1248`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C295`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C295`2<class BaseClass0,class BaseClass1>::ClassMethod1327<object>()
		calli default string(class G3_C1248`1<class BaseClass0>)
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G2_C295`2<class BaseClass0,class BaseClass1> on type class G3_C1248`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C295`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C295`2<class BaseClass0,class BaseClass1>::ClassMethod1326()
		calli default string(class G3_C1248`1<class BaseClass0>)
		ldstr "G2_C295::ClassMethod1326.MI.7490()"
		ldstr "class G2_C295`2<class BaseClass0,class BaseClass1> on type class G3_C1248`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G3_C1248`1<class BaseClass0>)
		ldstr "G3_C1248::Method4.MI.15104()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1248`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G3_C1248`1<class BaseClass0>)
		ldstr "G3_C1248::Method5.15105()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1248`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1248`1<class BaseClass0>)
		ldstr "G3_C1248::Method6.15106<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1248`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1248`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1248`1<class BaseClass0>::ClassMethod3876<object>()
		calli default string(class G3_C1248`1<class BaseClass0>)
		ldstr "G3_C1248::ClassMethod3876.15107<System.Object>()"
		ldstr "class G3_C1248`1<class BaseClass0> on type class G3_C1248`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1248`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1248`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1248`1<class BaseClass0>)
		ldstr "G3_C1248::Method6.15106<System.Object>()"
		ldstr "class G3_C1248`1<class BaseClass0> on type class G3_C1248`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1248`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1248`1<class BaseClass0>::Method5()
		calli default string(class G3_C1248`1<class BaseClass0>)
		ldstr "G3_C1248::Method5.15105()"
		ldstr "class G3_C1248`1<class BaseClass0> on type class G3_C1248`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1248`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1248`1<class BaseClass0>::Method4()
		calli default string(class G3_C1248`1<class BaseClass0>)
		ldstr "G3_C1248::Method4.15103()"
		ldstr "class G3_C1248`1<class BaseClass0> on type class G3_C1248`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1248`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1248`1<class BaseClass0>::ClassMethod1920()
		calli default string(class G3_C1248`1<class BaseClass0>)
		ldstr "G2_C295::ClassMethod1920.7489()"
		ldstr "class G3_C1248`1<class BaseClass0> on type class G3_C1248`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1248`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1248`1<class BaseClass0>::ClassMethod1919()
		calli default string(class G3_C1248`1<class BaseClass0>)
		ldstr "G2_C295::ClassMethod1919.7488()"
		ldstr "class G3_C1248`1<class BaseClass0> on type class G3_C1248`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1248`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1248`1<class BaseClass0>::Method7<object>()
		calli default string(class G3_C1248`1<class BaseClass0>)
		ldstr "G2_C295::Method7.7486<System.Object>()"
		ldstr "class G3_C1248`1<class BaseClass0> on type class G3_C1248`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1248`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1248`1<class BaseClass0>::ClassMethod1327<object>()
		calli default string(class G3_C1248`1<class BaseClass0>)
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G3_C1248`1<class BaseClass0> on type class G3_C1248`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1248`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1248`1<class BaseClass0>::ClassMethod1326()
		calli default string(class G3_C1248`1<class BaseClass0>)
		ldstr "G2_C295::ClassMethod1326.MI.7490()"
		ldstr "class G3_C1248`1<class BaseClass0> on type class G3_C1248`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G3_C1248`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C6`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass1,class BaseClass1>::ClassMethod1327<object>()
		calli default string(class G3_C1248`1<class BaseClass1>)
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass1> on type class G3_C1248`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C6`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass1,class BaseClass1>::ClassMethod1326()
		calli default string(class G3_C1248`1<class BaseClass1>)
		ldstr "G2_C295::ClassMethod1326.MI.7490()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass1> on type class G3_C1248`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C6`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1248`1<class BaseClass1>)
		ldstr "G2_C295::Method7.7486<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass1> on type class G3_C1248`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1248`1<class BaseClass1>)
		ldstr "G2_C295::Method7.7486<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1248`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1248`1<class BaseClass1>)
		ldstr "G2_C295::Method7.MI.7487<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1248`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C295`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C295`2<class BaseClass1,class BaseClass1>::ClassMethod1920()
		calli default string(class G3_C1248`1<class BaseClass1>)
		ldstr "G2_C295::ClassMethod1920.7489()"
		ldstr "class G2_C295`2<class BaseClass1,class BaseClass1> on type class G3_C1248`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C295`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C295`2<class BaseClass1,class BaseClass1>::ClassMethod1919()
		calli default string(class G3_C1248`1<class BaseClass1>)
		ldstr "G2_C295::ClassMethod1919.7488()"
		ldstr "class G2_C295`2<class BaseClass1,class BaseClass1> on type class G3_C1248`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C295`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C295`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1248`1<class BaseClass1>)
		ldstr "G2_C295::Method7.7486<System.Object>()"
		ldstr "class G2_C295`2<class BaseClass1,class BaseClass1> on type class G3_C1248`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C295`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C295`2<class BaseClass1,class BaseClass1>::Method6<object>()
		calli default string(class G3_C1248`1<class BaseClass1>)
		ldstr "G3_C1248::Method6.15106<System.Object>()"
		ldstr "class G2_C295`2<class BaseClass1,class BaseClass1> on type class G3_C1248`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C295`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C295`2<class BaseClass1,class BaseClass1>::Method5()
		calli default string(class G3_C1248`1<class BaseClass1>)
		ldstr "G2_C295::Method5.7483()"
		ldstr "class G2_C295`2<class BaseClass1,class BaseClass1> on type class G3_C1248`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C295`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C295`2<class BaseClass1,class BaseClass1>::Method4()
		calli default string(class G3_C1248`1<class BaseClass1>)
		ldstr "G3_C1248::Method4.15103()"
		ldstr "class G2_C295`2<class BaseClass1,class BaseClass1> on type class G3_C1248`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C295`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C295`2<class BaseClass1,class BaseClass1>::ClassMethod1327<object>()
		calli default string(class G3_C1248`1<class BaseClass1>)
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G2_C295`2<class BaseClass1,class BaseClass1> on type class G3_C1248`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C295`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C295`2<class BaseClass1,class BaseClass1>::ClassMethod1326()
		calli default string(class G3_C1248`1<class BaseClass1>)
		ldstr "G2_C295::ClassMethod1326.MI.7490()"
		ldstr "class G2_C295`2<class BaseClass1,class BaseClass1> on type class G3_C1248`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G3_C1248`1<class BaseClass1>)
		ldstr "G3_C1248::Method4.MI.15104()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1248`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G3_C1248`1<class BaseClass1>)
		ldstr "G3_C1248::Method5.15105()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1248`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1248`1<class BaseClass1>)
		ldstr "G3_C1248::Method6.15106<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1248`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1248`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1248`1<class BaseClass1>::ClassMethod3876<object>()
		calli default string(class G3_C1248`1<class BaseClass1>)
		ldstr "G3_C1248::ClassMethod3876.15107<System.Object>()"
		ldstr "class G3_C1248`1<class BaseClass1> on type class G3_C1248`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1248`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1248`1<class BaseClass1>::Method6<object>()
		calli default string(class G3_C1248`1<class BaseClass1>)
		ldstr "G3_C1248::Method6.15106<System.Object>()"
		ldstr "class G3_C1248`1<class BaseClass1> on type class G3_C1248`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1248`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1248`1<class BaseClass1>::Method5()
		calli default string(class G3_C1248`1<class BaseClass1>)
		ldstr "G3_C1248::Method5.15105()"
		ldstr "class G3_C1248`1<class BaseClass1> on type class G3_C1248`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1248`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1248`1<class BaseClass1>::Method4()
		calli default string(class G3_C1248`1<class BaseClass1>)
		ldstr "G3_C1248::Method4.15103()"
		ldstr "class G3_C1248`1<class BaseClass1> on type class G3_C1248`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1248`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1248`1<class BaseClass1>::ClassMethod1920()
		calli default string(class G3_C1248`1<class BaseClass1>)
		ldstr "G2_C295::ClassMethod1920.7489()"
		ldstr "class G3_C1248`1<class BaseClass1> on type class G3_C1248`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1248`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1248`1<class BaseClass1>::ClassMethod1919()
		calli default string(class G3_C1248`1<class BaseClass1>)
		ldstr "G2_C295::ClassMethod1919.7488()"
		ldstr "class G3_C1248`1<class BaseClass1> on type class G3_C1248`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1248`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1248`1<class BaseClass1>::Method7<object>()
		calli default string(class G3_C1248`1<class BaseClass1>)
		ldstr "G2_C295::Method7.7486<System.Object>()"
		ldstr "class G3_C1248`1<class BaseClass1> on type class G3_C1248`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1248`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1248`1<class BaseClass1>::ClassMethod1327<object>()
		calli default string(class G3_C1248`1<class BaseClass1>)
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G3_C1248`1<class BaseClass1> on type class G3_C1248`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1248`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1248`1<class BaseClass1>::ClassMethod1326()
		calli default string(class G3_C1248`1<class BaseClass1>)
		ldstr "G2_C295::ClassMethod1326.MI.7490()"
		ldstr "class G3_C1248`1<class BaseClass1> on type class G3_C1248`1<class BaseClass1>"
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
