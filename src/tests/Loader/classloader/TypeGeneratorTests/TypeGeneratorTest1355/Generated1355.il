// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern mscorlib { .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) .ver 4:0:0:0 }
.assembly extern TestFramework { .publickeytoken = ( B0 3F 5F 7F 11 D5 0A 3A )  }

//TYPES IN FORWARDER ASSEMBLIES:

//TEST ASSEMBLY:
.assembly Generated1355 { .hash algorithm 0x00008004 }
.module Generated1355.exe

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
.class public G3_C1827`1<T0> 
		extends class G2_C772`2<!T0,class BaseClass1>
		implements IBase0 
{
	.method public hidebysig newslot virtual instance string Method0() cil managed noinlining { 
		ldstr "G3_C1827::Method0.18221()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase0.Method0'() cil managed noinlining { 
		.override method instance string IBase0::Method0()
		ldstr "G3_C1827::Method0.MI.18222()"
		ret
	}
	.method public hidebysig virtual instance string Method1() cil managed noinlining { 
		ldstr "G3_C1827::Method1.18223()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase0.Method1'() cil managed noinlining { 
		.override method instance string IBase0::Method1()
		ldstr "G3_C1827::Method1.MI.18224()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method2<M0>() cil managed noinlining { 
		ldstr "G3_C1827::Method2.18225<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase0.Method2'<M0>() cil managed noinlining { 
		.override method instance string IBase0::Method2<[1]>()
		ldstr "G3_C1827::Method2.MI.18226<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string Method3<M0>() cil managed noinlining { 
		ldstr "G3_C1827::Method3.18227<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase0.Method3'<M0>() cil managed noinlining { 
		.override method instance string IBase0::Method3<[1]>()
		ldstr "G3_C1827::Method3.MI.18228<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod5048<M0>() cil managed noinlining { 
		ldstr "G3_C1827::ClassMethod5048.18229<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C772<T0,class BaseClass1>.ClassMethod2926'() cil managed noinlining { 
		.override method instance string class G2_C772`2<!T0,class BaseClass1>::ClassMethod2926()
		ldstr "G3_C1827::ClassMethod2926.MI.18230()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C772<T0,class BaseClass1>.ClassMethod2928'<M0>() cil managed noinlining { 
		.override method instance string class G2_C772`2<!T0,class BaseClass1>::ClassMethod2928<[1]>()
		ldstr "G3_C1827::ClassMethod2928.MI.18231<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class G2_C772`2<!T0,class BaseClass1>::.ctor()
		ret
	}
}
.class public G2_C772`2<T0, T1> 
		extends class G1_C14`2<class BaseClass1,!T1>
		implements class IBase2`2<class BaseClass1,class BaseClass1>, class IBase1`1<!T1> 
{
	.method public hidebysig virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G2_C772::Method7.12137<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase2<class BaseClass1,class BaseClass1>.Method7'<M0>() cil managed noinlining { 
		.override method instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<[1]>()
		ldstr "G2_C772::Method7.MI.12138<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig virtual instance string Method4() cil managed noinlining { 
		ldstr "G2_C772::Method4.12139()"
		ret
	}
	.method public hidebysig virtual instance string Method5() cil managed noinlining { 
		ldstr "G2_C772::Method5.12140()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<T1>.Method5'() cil managed noinlining { 
		.override method instance string class IBase1`1<!T1>::Method5()
		ldstr "G2_C772::Method5.MI.12141()"
		ret
	}
	.method public hidebysig virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G2_C772::Method6.12142<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod2925() cil managed noinlining { 
		ldstr "G2_C772::ClassMethod2925.12143()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod2926() cil managed noinlining { 
		ldstr "G2_C772::ClassMethod2926.12144()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod2927<M0>() cil managed noinlining { 
		ldstr "G2_C772::ClassMethod2927.12145<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod2928<M0>() cil managed noinlining { 
		ldstr "G2_C772::ClassMethod2928.12146<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C14<class BaseClass1,T1>.ClassMethod1351'<M0>() cil managed noinlining { 
		.override method instance string class G1_C14`2<class BaseClass1,!T1>::ClassMethod1351<[1]>()
		ldstr "G2_C772::ClassMethod1351.MI.12147<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class G1_C14`2<class BaseClass1,!T1>::.ctor()
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
.class public G1_C14`2<T0, T1> 
		implements class IBase2`2<!T1,class BaseClass1>, class IBase1`1<class BaseClass1> 
{
	.method public hidebysig virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G1_C14::Method7.4878<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string Method4() cil managed noinlining { 
		ldstr "G1_C14::Method4.4879()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method5() cil managed noinlining { 
		ldstr "G1_C14::Method5.4880()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class BaseClass1>.Method5'() cil managed noinlining { 
		.override method instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G1_C14::Method5.MI.4881()"
		ret
	}
	.method public hidebysig virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G1_C14::Method6.4882<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1350<M0>() cil managed noinlining { 
		ldstr "G1_C14::ClassMethod1350.4883<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1351<M0>() cil managed noinlining { 
		ldstr "G1_C14::ClassMethod1351.4884<"
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
	.method static void M.G3_C1827.T<T0,(class G3_C1827`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 20
		.locals init (string[] actualResults)
		ldc.i4.s    15
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1827.T<T0,(class G3_C1827`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 15
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1827`1<!!T0>::ClassMethod1350<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1827`1<!!T0>::ClassMethod1351<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1827`1<!!T0>::ClassMethod2925()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1827`1<!!T0>::ClassMethod2926()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1827`1<!!T0>::ClassMethod2927<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1827`1<!!T0>::ClassMethod2928<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1827`1<!!T0>::ClassMethod5048<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1827`1<!!T0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1827`1<!!T0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1827`1<!!T0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1827`1<!!T0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1827`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1827`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1827`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     14
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1827`1<!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1827.A<(class G3_C1827`1<class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 20
		.locals init (string[] actualResults)
		ldc.i4.s    15
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1827.A<(class G3_C1827`1<class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 15
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1827`1<class BaseClass0>::ClassMethod1350<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1827`1<class BaseClass0>::ClassMethod1351<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1827`1<class BaseClass0>::ClassMethod2925()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1827`1<class BaseClass0>::ClassMethod2926()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1827`1<class BaseClass0>::ClassMethod2927<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1827`1<class BaseClass0>::ClassMethod2928<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1827`1<class BaseClass0>::ClassMethod5048<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1827`1<class BaseClass0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1827`1<class BaseClass0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1827`1<class BaseClass0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1827`1<class BaseClass0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1827`1<class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1827`1<class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1827`1<class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     14
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1827`1<class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1827.B<(class G3_C1827`1<class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 20
		.locals init (string[] actualResults)
		ldc.i4.s    15
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1827.B<(class G3_C1827`1<class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 15
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1827`1<class BaseClass1>::ClassMethod1350<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1827`1<class BaseClass1>::ClassMethod1351<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1827`1<class BaseClass1>::ClassMethod2925()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1827`1<class BaseClass1>::ClassMethod2926()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1827`1<class BaseClass1>::ClassMethod2927<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1827`1<class BaseClass1>::ClassMethod2928<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1827`1<class BaseClass1>::ClassMethod5048<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1827`1<class BaseClass1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1827`1<class BaseClass1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1827`1<class BaseClass1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1827`1<class BaseClass1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1827`1<class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1827`1<class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1827`1<class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     14
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1827`1<class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C772.T.T<T0,T1,(class G2_C772`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 15
		.locals init (string[] actualResults)
		ldc.i4.s    10
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C772.T.T<T0,T1,(class G2_C772`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 10
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C772`2<!!T0,!!T1>::ClassMethod1350<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C772`2<!!T0,!!T1>::ClassMethod1351<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C772`2<!!T0,!!T1>::ClassMethod2925()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C772`2<!!T0,!!T1>::ClassMethod2926()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C772`2<!!T0,!!T1>::ClassMethod2927<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C772`2<!!T0,!!T1>::ClassMethod2928<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C772`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C772`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C772`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C772`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C772.A.T<T1,(class G2_C772`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 15
		.locals init (string[] actualResults)
		ldc.i4.s    10
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C772.A.T<T1,(class G2_C772`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 10
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C772`2<class BaseClass0,!!T1>::ClassMethod1350<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C772`2<class BaseClass0,!!T1>::ClassMethod1351<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C772`2<class BaseClass0,!!T1>::ClassMethod2925()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C772`2<class BaseClass0,!!T1>::ClassMethod2926()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C772`2<class BaseClass0,!!T1>::ClassMethod2927<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C772`2<class BaseClass0,!!T1>::ClassMethod2928<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C772`2<class BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C772`2<class BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C772`2<class BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C772`2<class BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C772.A.A<(class G2_C772`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 15
		.locals init (string[] actualResults)
		ldc.i4.s    10
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C772.A.A<(class G2_C772`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 10
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C772`2<class BaseClass0,class BaseClass0>::ClassMethod1350<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C772`2<class BaseClass0,class BaseClass0>::ClassMethod1351<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C772`2<class BaseClass0,class BaseClass0>::ClassMethod2925()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C772`2<class BaseClass0,class BaseClass0>::ClassMethod2926()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C772`2<class BaseClass0,class BaseClass0>::ClassMethod2927<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C772`2<class BaseClass0,class BaseClass0>::ClassMethod2928<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C772`2<class BaseClass0,class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C772`2<class BaseClass0,class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C772`2<class BaseClass0,class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C772`2<class BaseClass0,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C772.A.B<(class G2_C772`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 15
		.locals init (string[] actualResults)
		ldc.i4.s    10
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C772.A.B<(class G2_C772`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 10
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C772`2<class BaseClass0,class BaseClass1>::ClassMethod1350<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C772`2<class BaseClass0,class BaseClass1>::ClassMethod1351<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C772`2<class BaseClass0,class BaseClass1>::ClassMethod2925()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C772`2<class BaseClass0,class BaseClass1>::ClassMethod2926()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C772`2<class BaseClass0,class BaseClass1>::ClassMethod2927<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C772`2<class BaseClass0,class BaseClass1>::ClassMethod2928<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C772`2<class BaseClass0,class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C772`2<class BaseClass0,class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C772`2<class BaseClass0,class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C772`2<class BaseClass0,class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C772.B.T<T1,(class G2_C772`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 15
		.locals init (string[] actualResults)
		ldc.i4.s    10
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C772.B.T<T1,(class G2_C772`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 10
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C772`2<class BaseClass1,!!T1>::ClassMethod1350<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C772`2<class BaseClass1,!!T1>::ClassMethod1351<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C772`2<class BaseClass1,!!T1>::ClassMethod2925()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C772`2<class BaseClass1,!!T1>::ClassMethod2926()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C772`2<class BaseClass1,!!T1>::ClassMethod2927<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C772`2<class BaseClass1,!!T1>::ClassMethod2928<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C772`2<class BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C772`2<class BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C772`2<class BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C772`2<class BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C772.B.A<(class G2_C772`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 15
		.locals init (string[] actualResults)
		ldc.i4.s    10
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C772.B.A<(class G2_C772`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 10
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C772`2<class BaseClass1,class BaseClass0>::ClassMethod1350<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C772`2<class BaseClass1,class BaseClass0>::ClassMethod1351<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C772`2<class BaseClass1,class BaseClass0>::ClassMethod2925()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C772`2<class BaseClass1,class BaseClass0>::ClassMethod2926()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C772`2<class BaseClass1,class BaseClass0>::ClassMethod2927<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C772`2<class BaseClass1,class BaseClass0>::ClassMethod2928<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C772`2<class BaseClass1,class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C772`2<class BaseClass1,class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C772`2<class BaseClass1,class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C772`2<class BaseClass1,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C772.B.B<(class G2_C772`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 15
		.locals init (string[] actualResults)
		ldc.i4.s    10
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C772.B.B<(class G2_C772`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 10
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C772`2<class BaseClass1,class BaseClass1>::ClassMethod1350<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C772`2<class BaseClass1,class BaseClass1>::ClassMethod1351<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C772`2<class BaseClass1,class BaseClass1>::ClassMethod2925()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C772`2<class BaseClass1,class BaseClass1>::ClassMethod2926()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C772`2<class BaseClass1,class BaseClass1>::ClassMethod2927<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C772`2<class BaseClass1,class BaseClass1>::ClassMethod2928<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C772`2<class BaseClass1,class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C772`2<class BaseClass1,class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C772`2<class BaseClass1,class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C772`2<class BaseClass1,class BaseClass1>::Method7<object>()
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
	.method static void M.G1_C14.T.T<T0,T1,(class G1_C14`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C14.T.T<T0,T1,(class G1_C14`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C14`2<!!T0,!!T1>::ClassMethod1350<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C14`2<!!T0,!!T1>::ClassMethod1351<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C14`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C14`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C14`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C14`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C14.A.T<T1,(class G1_C14`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C14.A.T<T1,(class G1_C14`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C14`2<class BaseClass0,!!T1>::ClassMethod1350<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C14`2<class BaseClass0,!!T1>::ClassMethod1351<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C14`2<class BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C14`2<class BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C14`2<class BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C14`2<class BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C14.A.A<(class G1_C14`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C14.A.A<(class G1_C14`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C14`2<class BaseClass0,class BaseClass0>::ClassMethod1350<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C14`2<class BaseClass0,class BaseClass0>::ClassMethod1351<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C14`2<class BaseClass0,class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C14`2<class BaseClass0,class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C14`2<class BaseClass0,class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C14`2<class BaseClass0,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C14.A.B<(class G1_C14`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C14.A.B<(class G1_C14`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C14`2<class BaseClass0,class BaseClass1>::ClassMethod1350<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C14`2<class BaseClass0,class BaseClass1>::ClassMethod1351<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C14`2<class BaseClass0,class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C14`2<class BaseClass0,class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C14`2<class BaseClass0,class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C14`2<class BaseClass0,class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C14.B.T<T1,(class G1_C14`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C14.B.T<T1,(class G1_C14`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C14`2<class BaseClass1,!!T1>::ClassMethod1350<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C14`2<class BaseClass1,!!T1>::ClassMethod1351<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C14`2<class BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C14`2<class BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C14`2<class BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C14`2<class BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C14.B.A<(class G1_C14`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C14.B.A<(class G1_C14`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C14`2<class BaseClass1,class BaseClass0>::ClassMethod1350<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C14`2<class BaseClass1,class BaseClass0>::ClassMethod1351<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C14`2<class BaseClass1,class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C14`2<class BaseClass1,class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C14`2<class BaseClass1,class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C14`2<class BaseClass1,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C14.B.B<(class G1_C14`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C14.B.B<(class G1_C14`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C14`2<class BaseClass1,class BaseClass1>::ClassMethod1350<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C14`2<class BaseClass1,class BaseClass1>::ClassMethod1351<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C14`2<class BaseClass1,class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C14`2<class BaseClass1,class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C14`2<class BaseClass1,class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C14`2<class BaseClass1,class BaseClass1>::Method7<object>()
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
		newobj instance void class G3_C1827`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C14`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C14`2<class BaseClass1,class BaseClass1>::ClassMethod1351<object>()
		ldstr "G2_C772::ClassMethod1351.MI.12147<System.Object>()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass1> on type class G3_C1827`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C14`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C14`2<class BaseClass1,class BaseClass1>::ClassMethod1350<object>()
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass1> on type class G3_C1827`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C14`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C14`2<class BaseClass1,class BaseClass1>::Method6<object>()
		ldstr "G2_C772::Method6.12142<System.Object>()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass1> on type class G3_C1827`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C14`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C14`2<class BaseClass1,class BaseClass1>::Method5()
		ldstr "G2_C772::Method5.12140()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass1> on type class G3_C1827`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C14`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C14`2<class BaseClass1,class BaseClass1>::Method4()
		ldstr "G2_C772::Method4.12139()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass1> on type class G3_C1827`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C14`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C14`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C772::Method7.12137<System.Object>()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass1> on type class G3_C1827`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C772::Method7.MI.12138<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1827`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G2_C772::Method4.12139()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1827`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G2_C772::Method5.MI.12141()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1827`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C772::Method6.12142<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1827`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C772::Method7.MI.12138<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1827`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C772::Method4.12139()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1827`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C772::Method5.MI.12141()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1827`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C772::Method6.12142<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1827`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C772`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C772`2<class BaseClass0,class BaseClass1>::ClassMethod2928<object>()
		ldstr "G3_C1827::ClassMethod2928.MI.18231<System.Object>()"
		ldstr "class G2_C772`2<class BaseClass0,class BaseClass1> on type class G3_C1827`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C772`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C772`2<class BaseClass0,class BaseClass1>::ClassMethod2927<object>()
		ldstr "G2_C772::ClassMethod2927.12145<System.Object>()"
		ldstr "class G2_C772`2<class BaseClass0,class BaseClass1> on type class G3_C1827`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C772`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C772`2<class BaseClass0,class BaseClass1>::ClassMethod2926()
		ldstr "G3_C1827::ClassMethod2926.MI.18230()"
		ldstr "class G2_C772`2<class BaseClass0,class BaseClass1> on type class G3_C1827`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C772`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C772`2<class BaseClass0,class BaseClass1>::ClassMethod2925()
		ldstr "G2_C772::ClassMethod2925.12143()"
		ldstr "class G2_C772`2<class BaseClass0,class BaseClass1> on type class G3_C1827`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C772`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C772`2<class BaseClass0,class BaseClass1>::Method6<object>()
		ldstr "G2_C772::Method6.12142<System.Object>()"
		ldstr "class G2_C772`2<class BaseClass0,class BaseClass1> on type class G3_C1827`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C772`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C772`2<class BaseClass0,class BaseClass1>::Method5()
		ldstr "G2_C772::Method5.12140()"
		ldstr "class G2_C772`2<class BaseClass0,class BaseClass1> on type class G3_C1827`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C772`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C772`2<class BaseClass0,class BaseClass1>::Method4()
		ldstr "G2_C772::Method4.12139()"
		ldstr "class G2_C772`2<class BaseClass0,class BaseClass1> on type class G3_C1827`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C772`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C772`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C772::Method7.12137<System.Object>()"
		ldstr "class G2_C772`2<class BaseClass0,class BaseClass1> on type class G3_C1827`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C772`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C772`2<class BaseClass0,class BaseClass1>::ClassMethod1351<object>()
		ldstr "G2_C772::ClassMethod1351.MI.12147<System.Object>()"
		ldstr "class G2_C772`2<class BaseClass0,class BaseClass1> on type class G3_C1827`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C772`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C772`2<class BaseClass0,class BaseClass1>::ClassMethod1350<object>()
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()"
		ldstr "class G2_C772`2<class BaseClass0,class BaseClass1> on type class G3_C1827`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G3_C1827`1<class BaseClass0>
		callvirt instance string class G3_C1827`1<class BaseClass0>::ClassMethod5048<object>()
		ldstr "G3_C1827::ClassMethod5048.18229<System.Object>()"
		ldstr "class G3_C1827`1<class BaseClass0> on type class G3_C1827`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1827`1<class BaseClass0>
		callvirt instance string class G3_C1827`1<class BaseClass0>::Method3<object>()
		ldstr "G3_C1827::Method3.18227<System.Object>()"
		ldstr "class G3_C1827`1<class BaseClass0> on type class G3_C1827`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1827`1<class BaseClass0>
		callvirt instance string class G3_C1827`1<class BaseClass0>::Method2<object>()
		ldstr "G3_C1827::Method2.18225<System.Object>()"
		ldstr "class G3_C1827`1<class BaseClass0> on type class G3_C1827`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1827`1<class BaseClass0>
		callvirt instance string class G3_C1827`1<class BaseClass0>::Method1()
		ldstr "G3_C1827::Method1.18223()"
		ldstr "class G3_C1827`1<class BaseClass0> on type class G3_C1827`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1827`1<class BaseClass0>
		callvirt instance string class G3_C1827`1<class BaseClass0>::Method0()
		ldstr "G3_C1827::Method0.18221()"
		ldstr "class G3_C1827`1<class BaseClass0> on type class G3_C1827`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1827`1<class BaseClass0>
		callvirt instance string class G3_C1827`1<class BaseClass0>::ClassMethod2928<object>()
		ldstr "G3_C1827::ClassMethod2928.MI.18231<System.Object>()"
		ldstr "class G3_C1827`1<class BaseClass0> on type class G3_C1827`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1827`1<class BaseClass0>
		callvirt instance string class G3_C1827`1<class BaseClass0>::ClassMethod2927<object>()
		ldstr "G2_C772::ClassMethod2927.12145<System.Object>()"
		ldstr "class G3_C1827`1<class BaseClass0> on type class G3_C1827`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1827`1<class BaseClass0>
		callvirt instance string class G3_C1827`1<class BaseClass0>::ClassMethod2926()
		ldstr "G3_C1827::ClassMethod2926.MI.18230()"
		ldstr "class G3_C1827`1<class BaseClass0> on type class G3_C1827`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1827`1<class BaseClass0>
		callvirt instance string class G3_C1827`1<class BaseClass0>::ClassMethod2925()
		ldstr "G2_C772::ClassMethod2925.12143()"
		ldstr "class G3_C1827`1<class BaseClass0> on type class G3_C1827`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1827`1<class BaseClass0>
		callvirt instance string class G3_C1827`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C772::Method6.12142<System.Object>()"
		ldstr "class G3_C1827`1<class BaseClass0> on type class G3_C1827`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1827`1<class BaseClass0>
		callvirt instance string class G3_C1827`1<class BaseClass0>::Method5()
		ldstr "G2_C772::Method5.12140()"
		ldstr "class G3_C1827`1<class BaseClass0> on type class G3_C1827`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1827`1<class BaseClass0>
		callvirt instance string class G3_C1827`1<class BaseClass0>::Method4()
		ldstr "G2_C772::Method4.12139()"
		ldstr "class G3_C1827`1<class BaseClass0> on type class G3_C1827`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1827`1<class BaseClass0>
		callvirt instance string class G3_C1827`1<class BaseClass0>::Method7<object>()
		ldstr "G2_C772::Method7.12137<System.Object>()"
		ldstr "class G3_C1827`1<class BaseClass0> on type class G3_C1827`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1827`1<class BaseClass0>
		callvirt instance string class G3_C1827`1<class BaseClass0>::ClassMethod1351<object>()
		ldstr "G2_C772::ClassMethod1351.MI.12147<System.Object>()"
		ldstr "class G3_C1827`1<class BaseClass0> on type class G3_C1827`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1827`1<class BaseClass0>
		callvirt instance string class G3_C1827`1<class BaseClass0>::ClassMethod1350<object>()
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()"
		ldstr "class G3_C1827`1<class BaseClass0> on type class G3_C1827`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string IBase0::Method0()
		ldstr "G3_C1827::Method0.MI.18222()"
		ldstr "IBase0 on type class G3_C1827`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method1()
		ldstr "G3_C1827::Method1.MI.18224()"
		ldstr "IBase0 on type class G3_C1827`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method2<object>()
		ldstr "G3_C1827::Method2.MI.18226<System.Object>()"
		ldstr "IBase0 on type class G3_C1827`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method3<object>()
		ldstr "G3_C1827::Method3.MI.18228<System.Object>()"
		ldstr "IBase0 on type class G3_C1827`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G3_C1827`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C14`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C14`2<class BaseClass1,class BaseClass1>::ClassMethod1351<object>()
		ldstr "G2_C772::ClassMethod1351.MI.12147<System.Object>()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass1> on type class G3_C1827`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C14`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C14`2<class BaseClass1,class BaseClass1>::ClassMethod1350<object>()
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass1> on type class G3_C1827`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C14`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C14`2<class BaseClass1,class BaseClass1>::Method6<object>()
		ldstr "G2_C772::Method6.12142<System.Object>()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass1> on type class G3_C1827`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C14`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C14`2<class BaseClass1,class BaseClass1>::Method5()
		ldstr "G2_C772::Method5.12140()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass1> on type class G3_C1827`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C14`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C14`2<class BaseClass1,class BaseClass1>::Method4()
		ldstr "G2_C772::Method4.12139()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass1> on type class G3_C1827`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C14`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C14`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C772::Method7.12137<System.Object>()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass1> on type class G3_C1827`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C772::Method7.MI.12138<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1827`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G2_C772::Method4.12139()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1827`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G2_C772::Method5.MI.12141()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1827`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C772::Method6.12142<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1827`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C772::Method7.MI.12138<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1827`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C772::Method4.12139()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1827`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C772::Method5.MI.12141()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1827`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C772::Method6.12142<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1827`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C772`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C772`2<class BaseClass1,class BaseClass1>::ClassMethod2928<object>()
		ldstr "G3_C1827::ClassMethod2928.MI.18231<System.Object>()"
		ldstr "class G2_C772`2<class BaseClass1,class BaseClass1> on type class G3_C1827`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C772`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C772`2<class BaseClass1,class BaseClass1>::ClassMethod2927<object>()
		ldstr "G2_C772::ClassMethod2927.12145<System.Object>()"
		ldstr "class G2_C772`2<class BaseClass1,class BaseClass1> on type class G3_C1827`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C772`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C772`2<class BaseClass1,class BaseClass1>::ClassMethod2926()
		ldstr "G3_C1827::ClassMethod2926.MI.18230()"
		ldstr "class G2_C772`2<class BaseClass1,class BaseClass1> on type class G3_C1827`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C772`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C772`2<class BaseClass1,class BaseClass1>::ClassMethod2925()
		ldstr "G2_C772::ClassMethod2925.12143()"
		ldstr "class G2_C772`2<class BaseClass1,class BaseClass1> on type class G3_C1827`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C772`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C772`2<class BaseClass1,class BaseClass1>::Method6<object>()
		ldstr "G2_C772::Method6.12142<System.Object>()"
		ldstr "class G2_C772`2<class BaseClass1,class BaseClass1> on type class G3_C1827`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C772`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C772`2<class BaseClass1,class BaseClass1>::Method5()
		ldstr "G2_C772::Method5.12140()"
		ldstr "class G2_C772`2<class BaseClass1,class BaseClass1> on type class G3_C1827`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C772`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C772`2<class BaseClass1,class BaseClass1>::Method4()
		ldstr "G2_C772::Method4.12139()"
		ldstr "class G2_C772`2<class BaseClass1,class BaseClass1> on type class G3_C1827`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C772`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C772`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C772::Method7.12137<System.Object>()"
		ldstr "class G2_C772`2<class BaseClass1,class BaseClass1> on type class G3_C1827`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C772`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C772`2<class BaseClass1,class BaseClass1>::ClassMethod1351<object>()
		ldstr "G2_C772::ClassMethod1351.MI.12147<System.Object>()"
		ldstr "class G2_C772`2<class BaseClass1,class BaseClass1> on type class G3_C1827`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C772`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C772`2<class BaseClass1,class BaseClass1>::ClassMethod1350<object>()
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()"
		ldstr "class G2_C772`2<class BaseClass1,class BaseClass1> on type class G3_C1827`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G3_C1827`1<class BaseClass1>
		callvirt instance string class G3_C1827`1<class BaseClass1>::ClassMethod5048<object>()
		ldstr "G3_C1827::ClassMethod5048.18229<System.Object>()"
		ldstr "class G3_C1827`1<class BaseClass1> on type class G3_C1827`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1827`1<class BaseClass1>
		callvirt instance string class G3_C1827`1<class BaseClass1>::Method3<object>()
		ldstr "G3_C1827::Method3.18227<System.Object>()"
		ldstr "class G3_C1827`1<class BaseClass1> on type class G3_C1827`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1827`1<class BaseClass1>
		callvirt instance string class G3_C1827`1<class BaseClass1>::Method2<object>()
		ldstr "G3_C1827::Method2.18225<System.Object>()"
		ldstr "class G3_C1827`1<class BaseClass1> on type class G3_C1827`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1827`1<class BaseClass1>
		callvirt instance string class G3_C1827`1<class BaseClass1>::Method1()
		ldstr "G3_C1827::Method1.18223()"
		ldstr "class G3_C1827`1<class BaseClass1> on type class G3_C1827`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1827`1<class BaseClass1>
		callvirt instance string class G3_C1827`1<class BaseClass1>::Method0()
		ldstr "G3_C1827::Method0.18221()"
		ldstr "class G3_C1827`1<class BaseClass1> on type class G3_C1827`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1827`1<class BaseClass1>
		callvirt instance string class G3_C1827`1<class BaseClass1>::ClassMethod2928<object>()
		ldstr "G3_C1827::ClassMethod2928.MI.18231<System.Object>()"
		ldstr "class G3_C1827`1<class BaseClass1> on type class G3_C1827`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1827`1<class BaseClass1>
		callvirt instance string class G3_C1827`1<class BaseClass1>::ClassMethod2927<object>()
		ldstr "G2_C772::ClassMethod2927.12145<System.Object>()"
		ldstr "class G3_C1827`1<class BaseClass1> on type class G3_C1827`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1827`1<class BaseClass1>
		callvirt instance string class G3_C1827`1<class BaseClass1>::ClassMethod2926()
		ldstr "G3_C1827::ClassMethod2926.MI.18230()"
		ldstr "class G3_C1827`1<class BaseClass1> on type class G3_C1827`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1827`1<class BaseClass1>
		callvirt instance string class G3_C1827`1<class BaseClass1>::ClassMethod2925()
		ldstr "G2_C772::ClassMethod2925.12143()"
		ldstr "class G3_C1827`1<class BaseClass1> on type class G3_C1827`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1827`1<class BaseClass1>
		callvirt instance string class G3_C1827`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C772::Method6.12142<System.Object>()"
		ldstr "class G3_C1827`1<class BaseClass1> on type class G3_C1827`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1827`1<class BaseClass1>
		callvirt instance string class G3_C1827`1<class BaseClass1>::Method5()
		ldstr "G2_C772::Method5.12140()"
		ldstr "class G3_C1827`1<class BaseClass1> on type class G3_C1827`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1827`1<class BaseClass1>
		callvirt instance string class G3_C1827`1<class BaseClass1>::Method4()
		ldstr "G2_C772::Method4.12139()"
		ldstr "class G3_C1827`1<class BaseClass1> on type class G3_C1827`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1827`1<class BaseClass1>
		callvirt instance string class G3_C1827`1<class BaseClass1>::Method7<object>()
		ldstr "G2_C772::Method7.12137<System.Object>()"
		ldstr "class G3_C1827`1<class BaseClass1> on type class G3_C1827`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1827`1<class BaseClass1>
		callvirt instance string class G3_C1827`1<class BaseClass1>::ClassMethod1351<object>()
		ldstr "G2_C772::ClassMethod1351.MI.12147<System.Object>()"
		ldstr "class G3_C1827`1<class BaseClass1> on type class G3_C1827`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1827`1<class BaseClass1>
		callvirt instance string class G3_C1827`1<class BaseClass1>::ClassMethod1350<object>()
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()"
		ldstr "class G3_C1827`1<class BaseClass1> on type class G3_C1827`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string IBase0::Method0()
		ldstr "G3_C1827::Method0.MI.18222()"
		ldstr "IBase0 on type class G3_C1827`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method1()
		ldstr "G3_C1827::Method1.MI.18224()"
		ldstr "IBase0 on type class G3_C1827`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method2<object>()
		ldstr "G3_C1827::Method2.MI.18226<System.Object>()"
		ldstr "IBase0 on type class G3_C1827`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method3<object>()
		ldstr "G3_C1827::Method3.MI.18228<System.Object>()"
		ldstr "IBase0 on type class G3_C1827`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C772`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C14`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C14`2<class BaseClass1,class BaseClass0>::ClassMethod1351<object>()
		ldstr "G2_C772::ClassMethod1351.MI.12147<System.Object>()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass0> on type class G2_C772`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C14`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C14`2<class BaseClass1,class BaseClass0>::ClassMethod1350<object>()
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass0> on type class G2_C772`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C14`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C14`2<class BaseClass1,class BaseClass0>::Method6<object>()
		ldstr "G2_C772::Method6.12142<System.Object>()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass0> on type class G2_C772`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C14`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C14`2<class BaseClass1,class BaseClass0>::Method5()
		ldstr "G2_C772::Method5.12140()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass0> on type class G2_C772`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C14`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C14`2<class BaseClass1,class BaseClass0>::Method4()
		ldstr "G2_C772::Method4.12139()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass0> on type class G2_C772`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C14`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C14`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G2_C772::Method7.12137<System.Object>()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass0> on type class G2_C772`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C772::Method7.MI.12138<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C772`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G2_C772::Method4.12139()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C772`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G1_C14::Method5.MI.4881()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C772`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C772::Method6.12142<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C772`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C772::Method4.12139()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C772`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C772::Method5.MI.12141()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C772`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C772::Method6.12142<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C772`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C772`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C772`2<class BaseClass0,class BaseClass0>::ClassMethod2928<object>()
		ldstr "G2_C772::ClassMethod2928.12146<System.Object>()"
		ldstr "class G2_C772`2<class BaseClass0,class BaseClass0> on type class G2_C772`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C772`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C772`2<class BaseClass0,class BaseClass0>::ClassMethod2927<object>()
		ldstr "G2_C772::ClassMethod2927.12145<System.Object>()"
		ldstr "class G2_C772`2<class BaseClass0,class BaseClass0> on type class G2_C772`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C772`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C772`2<class BaseClass0,class BaseClass0>::ClassMethod2926()
		ldstr "G2_C772::ClassMethod2926.12144()"
		ldstr "class G2_C772`2<class BaseClass0,class BaseClass0> on type class G2_C772`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C772`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C772`2<class BaseClass0,class BaseClass0>::ClassMethod2925()
		ldstr "G2_C772::ClassMethod2925.12143()"
		ldstr "class G2_C772`2<class BaseClass0,class BaseClass0> on type class G2_C772`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C772`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C772`2<class BaseClass0,class BaseClass0>::Method6<object>()
		ldstr "G2_C772::Method6.12142<System.Object>()"
		ldstr "class G2_C772`2<class BaseClass0,class BaseClass0> on type class G2_C772`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C772`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C772`2<class BaseClass0,class BaseClass0>::Method5()
		ldstr "G2_C772::Method5.12140()"
		ldstr "class G2_C772`2<class BaseClass0,class BaseClass0> on type class G2_C772`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C772`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C772`2<class BaseClass0,class BaseClass0>::Method4()
		ldstr "G2_C772::Method4.12139()"
		ldstr "class G2_C772`2<class BaseClass0,class BaseClass0> on type class G2_C772`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C772`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C772`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C772::Method7.12137<System.Object>()"
		ldstr "class G2_C772`2<class BaseClass0,class BaseClass0> on type class G2_C772`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C772`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C772`2<class BaseClass0,class BaseClass0>::ClassMethod1351<object>()
		ldstr "G2_C772::ClassMethod1351.MI.12147<System.Object>()"
		ldstr "class G2_C772`2<class BaseClass0,class BaseClass0> on type class G2_C772`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C772`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C772`2<class BaseClass0,class BaseClass0>::ClassMethod1350<object>()
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()"
		ldstr "class G2_C772`2<class BaseClass0,class BaseClass0> on type class G2_C772`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C772::Method7.MI.12138<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C772`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C772`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C14`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C14`2<class BaseClass1,class BaseClass1>::ClassMethod1351<object>()
		ldstr "G2_C772::ClassMethod1351.MI.12147<System.Object>()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass1> on type class G2_C772`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C14`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C14`2<class BaseClass1,class BaseClass1>::ClassMethod1350<object>()
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass1> on type class G2_C772`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C14`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C14`2<class BaseClass1,class BaseClass1>::Method6<object>()
		ldstr "G2_C772::Method6.12142<System.Object>()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass1> on type class G2_C772`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C14`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C14`2<class BaseClass1,class BaseClass1>::Method5()
		ldstr "G2_C772::Method5.12140()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass1> on type class G2_C772`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C14`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C14`2<class BaseClass1,class BaseClass1>::Method4()
		ldstr "G2_C772::Method4.12139()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass1> on type class G2_C772`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C14`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C14`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C772::Method7.12137<System.Object>()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass1> on type class G2_C772`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C772::Method7.MI.12138<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C772`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G2_C772::Method4.12139()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C772`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G2_C772::Method5.MI.12141()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C772`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C772::Method6.12142<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C772`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C772::Method7.MI.12138<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C772`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C772::Method4.12139()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C772`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C772::Method5.MI.12141()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C772`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C772::Method6.12142<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C772`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C772`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C772`2<class BaseClass0,class BaseClass1>::ClassMethod2928<object>()
		ldstr "G2_C772::ClassMethod2928.12146<System.Object>()"
		ldstr "class G2_C772`2<class BaseClass0,class BaseClass1> on type class G2_C772`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C772`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C772`2<class BaseClass0,class BaseClass1>::ClassMethod2927<object>()
		ldstr "G2_C772::ClassMethod2927.12145<System.Object>()"
		ldstr "class G2_C772`2<class BaseClass0,class BaseClass1> on type class G2_C772`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C772`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C772`2<class BaseClass0,class BaseClass1>::ClassMethod2926()
		ldstr "G2_C772::ClassMethod2926.12144()"
		ldstr "class G2_C772`2<class BaseClass0,class BaseClass1> on type class G2_C772`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C772`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C772`2<class BaseClass0,class BaseClass1>::ClassMethod2925()
		ldstr "G2_C772::ClassMethod2925.12143()"
		ldstr "class G2_C772`2<class BaseClass0,class BaseClass1> on type class G2_C772`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C772`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C772`2<class BaseClass0,class BaseClass1>::Method6<object>()
		ldstr "G2_C772::Method6.12142<System.Object>()"
		ldstr "class G2_C772`2<class BaseClass0,class BaseClass1> on type class G2_C772`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C772`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C772`2<class BaseClass0,class BaseClass1>::Method5()
		ldstr "G2_C772::Method5.12140()"
		ldstr "class G2_C772`2<class BaseClass0,class BaseClass1> on type class G2_C772`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C772`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C772`2<class BaseClass0,class BaseClass1>::Method4()
		ldstr "G2_C772::Method4.12139()"
		ldstr "class G2_C772`2<class BaseClass0,class BaseClass1> on type class G2_C772`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C772`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C772`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C772::Method7.12137<System.Object>()"
		ldstr "class G2_C772`2<class BaseClass0,class BaseClass1> on type class G2_C772`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C772`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C772`2<class BaseClass0,class BaseClass1>::ClassMethod1351<object>()
		ldstr "G2_C772::ClassMethod1351.MI.12147<System.Object>()"
		ldstr "class G2_C772`2<class BaseClass0,class BaseClass1> on type class G2_C772`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C772`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C772`2<class BaseClass0,class BaseClass1>::ClassMethod1350<object>()
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()"
		ldstr "class G2_C772`2<class BaseClass0,class BaseClass1> on type class G2_C772`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C772`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C14`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C14`2<class BaseClass1,class BaseClass0>::ClassMethod1351<object>()
		ldstr "G2_C772::ClassMethod1351.MI.12147<System.Object>()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass0> on type class G2_C772`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C14`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C14`2<class BaseClass1,class BaseClass0>::ClassMethod1350<object>()
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass0> on type class G2_C772`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C14`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C14`2<class BaseClass1,class BaseClass0>::Method6<object>()
		ldstr "G2_C772::Method6.12142<System.Object>()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass0> on type class G2_C772`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C14`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C14`2<class BaseClass1,class BaseClass0>::Method5()
		ldstr "G2_C772::Method5.12140()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass0> on type class G2_C772`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C14`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C14`2<class BaseClass1,class BaseClass0>::Method4()
		ldstr "G2_C772::Method4.12139()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass0> on type class G2_C772`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C14`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C14`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G2_C772::Method7.12137<System.Object>()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass0> on type class G2_C772`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C772::Method7.MI.12138<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C772`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G2_C772::Method4.12139()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C772`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G1_C14::Method5.MI.4881()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C772`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C772::Method6.12142<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C772`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C772::Method4.12139()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C772`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C772::Method5.MI.12141()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C772`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C772::Method6.12142<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C772`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C772`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C772`2<class BaseClass1,class BaseClass0>::ClassMethod2928<object>()
		ldstr "G2_C772::ClassMethod2928.12146<System.Object>()"
		ldstr "class G2_C772`2<class BaseClass1,class BaseClass0> on type class G2_C772`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C772`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C772`2<class BaseClass1,class BaseClass0>::ClassMethod2927<object>()
		ldstr "G2_C772::ClassMethod2927.12145<System.Object>()"
		ldstr "class G2_C772`2<class BaseClass1,class BaseClass0> on type class G2_C772`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C772`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C772`2<class BaseClass1,class BaseClass0>::ClassMethod2926()
		ldstr "G2_C772::ClassMethod2926.12144()"
		ldstr "class G2_C772`2<class BaseClass1,class BaseClass0> on type class G2_C772`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C772`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C772`2<class BaseClass1,class BaseClass0>::ClassMethod2925()
		ldstr "G2_C772::ClassMethod2925.12143()"
		ldstr "class G2_C772`2<class BaseClass1,class BaseClass0> on type class G2_C772`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C772`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C772`2<class BaseClass1,class BaseClass0>::Method6<object>()
		ldstr "G2_C772::Method6.12142<System.Object>()"
		ldstr "class G2_C772`2<class BaseClass1,class BaseClass0> on type class G2_C772`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C772`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C772`2<class BaseClass1,class BaseClass0>::Method5()
		ldstr "G2_C772::Method5.12140()"
		ldstr "class G2_C772`2<class BaseClass1,class BaseClass0> on type class G2_C772`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C772`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C772`2<class BaseClass1,class BaseClass0>::Method4()
		ldstr "G2_C772::Method4.12139()"
		ldstr "class G2_C772`2<class BaseClass1,class BaseClass0> on type class G2_C772`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C772`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C772`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G2_C772::Method7.12137<System.Object>()"
		ldstr "class G2_C772`2<class BaseClass1,class BaseClass0> on type class G2_C772`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C772`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C772`2<class BaseClass1,class BaseClass0>::ClassMethod1351<object>()
		ldstr "G2_C772::ClassMethod1351.MI.12147<System.Object>()"
		ldstr "class G2_C772`2<class BaseClass1,class BaseClass0> on type class G2_C772`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C772`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C772`2<class BaseClass1,class BaseClass0>::ClassMethod1350<object>()
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()"
		ldstr "class G2_C772`2<class BaseClass1,class BaseClass0> on type class G2_C772`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C772::Method7.MI.12138<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C772`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C772`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C14`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C14`2<class BaseClass1,class BaseClass1>::ClassMethod1351<object>()
		ldstr "G2_C772::ClassMethod1351.MI.12147<System.Object>()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass1> on type class G2_C772`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C14`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C14`2<class BaseClass1,class BaseClass1>::ClassMethod1350<object>()
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass1> on type class G2_C772`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C14`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C14`2<class BaseClass1,class BaseClass1>::Method6<object>()
		ldstr "G2_C772::Method6.12142<System.Object>()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass1> on type class G2_C772`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C14`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C14`2<class BaseClass1,class BaseClass1>::Method5()
		ldstr "G2_C772::Method5.12140()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass1> on type class G2_C772`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C14`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C14`2<class BaseClass1,class BaseClass1>::Method4()
		ldstr "G2_C772::Method4.12139()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass1> on type class G2_C772`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C14`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C14`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C772::Method7.12137<System.Object>()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass1> on type class G2_C772`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C772::Method7.MI.12138<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C772`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G2_C772::Method4.12139()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C772`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G2_C772::Method5.MI.12141()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C772`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C772::Method6.12142<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C772`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C772::Method7.MI.12138<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C772`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C772::Method4.12139()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C772`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C772::Method5.MI.12141()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C772`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C772::Method6.12142<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C772`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C772`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C772`2<class BaseClass1,class BaseClass1>::ClassMethod2928<object>()
		ldstr "G2_C772::ClassMethod2928.12146<System.Object>()"
		ldstr "class G2_C772`2<class BaseClass1,class BaseClass1> on type class G2_C772`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C772`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C772`2<class BaseClass1,class BaseClass1>::ClassMethod2927<object>()
		ldstr "G2_C772::ClassMethod2927.12145<System.Object>()"
		ldstr "class G2_C772`2<class BaseClass1,class BaseClass1> on type class G2_C772`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C772`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C772`2<class BaseClass1,class BaseClass1>::ClassMethod2926()
		ldstr "G2_C772::ClassMethod2926.12144()"
		ldstr "class G2_C772`2<class BaseClass1,class BaseClass1> on type class G2_C772`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C772`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C772`2<class BaseClass1,class BaseClass1>::ClassMethod2925()
		ldstr "G2_C772::ClassMethod2925.12143()"
		ldstr "class G2_C772`2<class BaseClass1,class BaseClass1> on type class G2_C772`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C772`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C772`2<class BaseClass1,class BaseClass1>::Method6<object>()
		ldstr "G2_C772::Method6.12142<System.Object>()"
		ldstr "class G2_C772`2<class BaseClass1,class BaseClass1> on type class G2_C772`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C772`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C772`2<class BaseClass1,class BaseClass1>::Method5()
		ldstr "G2_C772::Method5.12140()"
		ldstr "class G2_C772`2<class BaseClass1,class BaseClass1> on type class G2_C772`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C772`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C772`2<class BaseClass1,class BaseClass1>::Method4()
		ldstr "G2_C772::Method4.12139()"
		ldstr "class G2_C772`2<class BaseClass1,class BaseClass1> on type class G2_C772`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C772`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C772`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C772::Method7.12137<System.Object>()"
		ldstr "class G2_C772`2<class BaseClass1,class BaseClass1> on type class G2_C772`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C772`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C772`2<class BaseClass1,class BaseClass1>::ClassMethod1351<object>()
		ldstr "G2_C772::ClassMethod1351.MI.12147<System.Object>()"
		ldstr "class G2_C772`2<class BaseClass1,class BaseClass1> on type class G2_C772`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C772`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C772`2<class BaseClass1,class BaseClass1>::ClassMethod1350<object>()
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()"
		ldstr "class G2_C772`2<class BaseClass1,class BaseClass1> on type class G2_C772`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G1_C14`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C14`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C14`2<class BaseClass0,class BaseClass0>::ClassMethod1351<object>()
		ldstr "G1_C14::ClassMethod1351.4884<System.Object>()"
		ldstr "class G1_C14`2<class BaseClass0,class BaseClass0> on type class G1_C14`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C14`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C14`2<class BaseClass0,class BaseClass0>::ClassMethod1350<object>()
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()"
		ldstr "class G1_C14`2<class BaseClass0,class BaseClass0> on type class G1_C14`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C14`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C14`2<class BaseClass0,class BaseClass0>::Method6<object>()
		ldstr "G1_C14::Method6.4882<System.Object>()"
		ldstr "class G1_C14`2<class BaseClass0,class BaseClass0> on type class G1_C14`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C14`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C14`2<class BaseClass0,class BaseClass0>::Method5()
		ldstr "G1_C14::Method5.4880()"
		ldstr "class G1_C14`2<class BaseClass0,class BaseClass0> on type class G1_C14`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C14`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C14`2<class BaseClass0,class BaseClass0>::Method4()
		ldstr "G1_C14::Method4.4879()"
		ldstr "class G1_C14`2<class BaseClass0,class BaseClass0> on type class G1_C14`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C14`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C14`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G1_C14::Method7.4878<System.Object>()"
		ldstr "class G1_C14`2<class BaseClass0,class BaseClass0> on type class G1_C14`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G1_C14::Method7.4878<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G1_C14`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G1_C14::Method4.4879()"
		ldstr "class IBase1`1<class BaseClass1> on type class G1_C14`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G1_C14::Method5.MI.4881()"
		ldstr "class IBase1`1<class BaseClass1> on type class G1_C14`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G1_C14::Method6.4882<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G1_C14`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G1_C14::Method4.4879()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C14`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G1_C14::Method5.MI.4881()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C14`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G1_C14::Method6.4882<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C14`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G1_C14`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C14`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C14`2<class BaseClass0,class BaseClass1>::ClassMethod1351<object>()
		ldstr "G1_C14::ClassMethod1351.4884<System.Object>()"
		ldstr "class G1_C14`2<class BaseClass0,class BaseClass1> on type class G1_C14`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C14`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C14`2<class BaseClass0,class BaseClass1>::ClassMethod1350<object>()
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()"
		ldstr "class G1_C14`2<class BaseClass0,class BaseClass1> on type class G1_C14`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C14`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C14`2<class BaseClass0,class BaseClass1>::Method6<object>()
		ldstr "G1_C14::Method6.4882<System.Object>()"
		ldstr "class G1_C14`2<class BaseClass0,class BaseClass1> on type class G1_C14`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C14`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C14`2<class BaseClass0,class BaseClass1>::Method5()
		ldstr "G1_C14::Method5.4880()"
		ldstr "class G1_C14`2<class BaseClass0,class BaseClass1> on type class G1_C14`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C14`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C14`2<class BaseClass0,class BaseClass1>::Method4()
		ldstr "G1_C14::Method4.4879()"
		ldstr "class G1_C14`2<class BaseClass0,class BaseClass1> on type class G1_C14`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C14`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C14`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G1_C14::Method7.4878<System.Object>()"
		ldstr "class G1_C14`2<class BaseClass0,class BaseClass1> on type class G1_C14`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C14::Method7.4878<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G1_C14`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G1_C14::Method4.4879()"
		ldstr "class IBase1`1<class BaseClass1> on type class G1_C14`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G1_C14::Method5.MI.4881()"
		ldstr "class IBase1`1<class BaseClass1> on type class G1_C14`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G1_C14::Method6.4882<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G1_C14`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G1_C14::Method7.4878<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G1_C14`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G1_C14::Method4.4879()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C14`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G1_C14::Method5.MI.4881()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C14`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G1_C14::Method6.4882<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C14`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G1_C14`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C14`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C14`2<class BaseClass1,class BaseClass0>::ClassMethod1351<object>()
		ldstr "G1_C14::ClassMethod1351.4884<System.Object>()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass0> on type class G1_C14`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C14`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C14`2<class BaseClass1,class BaseClass0>::ClassMethod1350<object>()
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass0> on type class G1_C14`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C14`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C14`2<class BaseClass1,class BaseClass0>::Method6<object>()
		ldstr "G1_C14::Method6.4882<System.Object>()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass0> on type class G1_C14`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C14`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C14`2<class BaseClass1,class BaseClass0>::Method5()
		ldstr "G1_C14::Method5.4880()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass0> on type class G1_C14`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C14`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C14`2<class BaseClass1,class BaseClass0>::Method4()
		ldstr "G1_C14::Method4.4879()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass0> on type class G1_C14`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C14`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C14`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G1_C14::Method7.4878<System.Object>()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass0> on type class G1_C14`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G1_C14::Method7.4878<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G1_C14`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G1_C14::Method4.4879()"
		ldstr "class IBase1`1<class BaseClass1> on type class G1_C14`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G1_C14::Method5.MI.4881()"
		ldstr "class IBase1`1<class BaseClass1> on type class G1_C14`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G1_C14::Method6.4882<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G1_C14`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G1_C14::Method4.4879()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C14`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G1_C14::Method5.MI.4881()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C14`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G1_C14::Method6.4882<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C14`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G1_C14`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C14`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C14`2<class BaseClass1,class BaseClass1>::ClassMethod1351<object>()
		ldstr "G1_C14::ClassMethod1351.4884<System.Object>()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass1> on type class G1_C14`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C14`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C14`2<class BaseClass1,class BaseClass1>::ClassMethod1350<object>()
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass1> on type class G1_C14`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C14`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C14`2<class BaseClass1,class BaseClass1>::Method6<object>()
		ldstr "G1_C14::Method6.4882<System.Object>()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass1> on type class G1_C14`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C14`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C14`2<class BaseClass1,class BaseClass1>::Method5()
		ldstr "G1_C14::Method5.4880()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass1> on type class G1_C14`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C14`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C14`2<class BaseClass1,class BaseClass1>::Method4()
		ldstr "G1_C14::Method4.4879()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass1> on type class G1_C14`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C14`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C14`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C14::Method7.4878<System.Object>()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass1> on type class G1_C14`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C14::Method7.4878<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G1_C14`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G1_C14::Method4.4879()"
		ldstr "class IBase1`1<class BaseClass1> on type class G1_C14`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G1_C14::Method5.MI.4881()"
		ldstr "class IBase1`1<class BaseClass1> on type class G1_C14`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G1_C14::Method6.4882<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G1_C14`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G1_C14::Method7.4878<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G1_C14`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G1_C14::Method4.4879()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C14`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G1_C14::Method5.MI.4881()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C14`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G1_C14::Method6.4882<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C14`2<class BaseClass1,class BaseClass1>"
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
		newobj instance void class G3_C1827`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()#G2_C772::ClassMethod1351.MI.12147<System.Object>()#G2_C772::Method4.12139()#G2_C772::Method5.12140()#G2_C772::Method6.12142<System.Object>()#G2_C772::Method7.12137<System.Object>()#"
		call void Framework::M.G1_C14.T.T<class BaseClass1,class BaseClass1,class G3_C1827`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()#G2_C772::ClassMethod1351.MI.12147<System.Object>()#G2_C772::Method4.12139()#G2_C772::Method5.12140()#G2_C772::Method6.12142<System.Object>()#G2_C772::Method7.12137<System.Object>()#"
		call void Framework::M.G1_C14.B.T<class BaseClass1,class G3_C1827`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()#G2_C772::ClassMethod1351.MI.12147<System.Object>()#G2_C772::Method4.12139()#G2_C772::Method5.12140()#G2_C772::Method6.12142<System.Object>()#G2_C772::Method7.12137<System.Object>()#"
		call void Framework::M.G1_C14.B.B<class G3_C1827`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C772::Method7.MI.12138<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G3_C1827`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C772::Method7.MI.12138<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G3_C1827`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C772::Method7.MI.12138<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G3_C1827`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C772::Method4.12139()#G2_C772::Method5.MI.12141()#G2_C772::Method6.12142<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass1,class G3_C1827`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C772::Method4.12139()#G2_C772::Method5.MI.12141()#G2_C772::Method6.12142<System.Object>()#"
		call void Framework::M.IBase1.B<class G3_C1827`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C772::Method7.MI.12138<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G3_C1827`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C772::Method7.MI.12138<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G3_C1827`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C772::Method7.MI.12138<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G3_C1827`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C772::Method4.12139()#G2_C772::Method5.MI.12141()#G2_C772::Method6.12142<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G3_C1827`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C772::Method4.12139()#G2_C772::Method5.MI.12141()#G2_C772::Method6.12142<System.Object>()#"
		call void Framework::M.IBase1.A<class G3_C1827`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()#G2_C772::ClassMethod1351.MI.12147<System.Object>()#G2_C772::ClassMethod2925.12143()#G3_C1827::ClassMethod2926.MI.18230()#G2_C772::ClassMethod2927.12145<System.Object>()#G3_C1827::ClassMethod2928.MI.18231<System.Object>()#G2_C772::Method4.12139()#G2_C772::Method5.12140()#G2_C772::Method6.12142<System.Object>()#G2_C772::Method7.12137<System.Object>()#"
		call void Framework::M.G2_C772.T.T<class BaseClass0,class BaseClass1,class G3_C1827`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()#G2_C772::ClassMethod1351.MI.12147<System.Object>()#G2_C772::ClassMethod2925.12143()#G3_C1827::ClassMethod2926.MI.18230()#G2_C772::ClassMethod2927.12145<System.Object>()#G3_C1827::ClassMethod2928.MI.18231<System.Object>()#G2_C772::Method4.12139()#G2_C772::Method5.12140()#G2_C772::Method6.12142<System.Object>()#G2_C772::Method7.12137<System.Object>()#"
		call void Framework::M.G2_C772.A.T<class BaseClass1,class G3_C1827`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()#G2_C772::ClassMethod1351.MI.12147<System.Object>()#G2_C772::ClassMethod2925.12143()#G3_C1827::ClassMethod2926.MI.18230()#G2_C772::ClassMethod2927.12145<System.Object>()#G3_C1827::ClassMethod2928.MI.18231<System.Object>()#G2_C772::Method4.12139()#G2_C772::Method5.12140()#G2_C772::Method6.12142<System.Object>()#G2_C772::Method7.12137<System.Object>()#"
		call void Framework::M.G2_C772.A.B<class G3_C1827`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()#G2_C772::ClassMethod1351.MI.12147<System.Object>()#G2_C772::ClassMethod2925.12143()#G3_C1827::ClassMethod2926.MI.18230()#G2_C772::ClassMethod2927.12145<System.Object>()#G3_C1827::ClassMethod2928.MI.18231<System.Object>()#G3_C1827::ClassMethod5048.18229<System.Object>()#G3_C1827::Method0.18221()#G3_C1827::Method1.18223()#G3_C1827::Method2.18225<System.Object>()#G3_C1827::Method3.18227<System.Object>()#G2_C772::Method4.12139()#G2_C772::Method5.12140()#G2_C772::Method6.12142<System.Object>()#G2_C772::Method7.12137<System.Object>()#"
		call void Framework::M.G3_C1827.T<class BaseClass0,class G3_C1827`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()#G2_C772::ClassMethod1351.MI.12147<System.Object>()#G2_C772::ClassMethod2925.12143()#G3_C1827::ClassMethod2926.MI.18230()#G2_C772::ClassMethod2927.12145<System.Object>()#G3_C1827::ClassMethod2928.MI.18231<System.Object>()#G3_C1827::ClassMethod5048.18229<System.Object>()#G3_C1827::Method0.18221()#G3_C1827::Method1.18223()#G3_C1827::Method2.18225<System.Object>()#G3_C1827::Method3.18227<System.Object>()#G2_C772::Method4.12139()#G2_C772::Method5.12140()#G2_C772::Method6.12142<System.Object>()#G2_C772::Method7.12137<System.Object>()#"
		call void Framework::M.G3_C1827.A<class G3_C1827`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1827::Method0.MI.18222()#G3_C1827::Method1.MI.18224()#G3_C1827::Method2.MI.18226<System.Object>()#G3_C1827::Method3.MI.18228<System.Object>()#"
		call void Framework::M.IBase0<class G3_C1827`1<class BaseClass0>>(!!0,string)
		newobj instance void class G3_C1827`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()#G2_C772::ClassMethod1351.MI.12147<System.Object>()#G2_C772::Method4.12139()#G2_C772::Method5.12140()#G2_C772::Method6.12142<System.Object>()#G2_C772::Method7.12137<System.Object>()#"
		call void Framework::M.G1_C14.T.T<class BaseClass1,class BaseClass1,class G3_C1827`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()#G2_C772::ClassMethod1351.MI.12147<System.Object>()#G2_C772::Method4.12139()#G2_C772::Method5.12140()#G2_C772::Method6.12142<System.Object>()#G2_C772::Method7.12137<System.Object>()#"
		call void Framework::M.G1_C14.B.T<class BaseClass1,class G3_C1827`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()#G2_C772::ClassMethod1351.MI.12147<System.Object>()#G2_C772::Method4.12139()#G2_C772::Method5.12140()#G2_C772::Method6.12142<System.Object>()#G2_C772::Method7.12137<System.Object>()#"
		call void Framework::M.G1_C14.B.B<class G3_C1827`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C772::Method7.MI.12138<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G3_C1827`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C772::Method7.MI.12138<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G3_C1827`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C772::Method7.MI.12138<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G3_C1827`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C772::Method4.12139()#G2_C772::Method5.MI.12141()#G2_C772::Method6.12142<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass1,class G3_C1827`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C772::Method4.12139()#G2_C772::Method5.MI.12141()#G2_C772::Method6.12142<System.Object>()#"
		call void Framework::M.IBase1.B<class G3_C1827`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C772::Method7.MI.12138<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G3_C1827`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C772::Method7.MI.12138<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G3_C1827`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C772::Method7.MI.12138<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G3_C1827`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C772::Method4.12139()#G2_C772::Method5.MI.12141()#G2_C772::Method6.12142<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G3_C1827`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C772::Method4.12139()#G2_C772::Method5.MI.12141()#G2_C772::Method6.12142<System.Object>()#"
		call void Framework::M.IBase1.A<class G3_C1827`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()#G2_C772::ClassMethod1351.MI.12147<System.Object>()#G2_C772::ClassMethod2925.12143()#G3_C1827::ClassMethod2926.MI.18230()#G2_C772::ClassMethod2927.12145<System.Object>()#G3_C1827::ClassMethod2928.MI.18231<System.Object>()#G2_C772::Method4.12139()#G2_C772::Method5.12140()#G2_C772::Method6.12142<System.Object>()#G2_C772::Method7.12137<System.Object>()#"
		call void Framework::M.G2_C772.T.T<class BaseClass1,class BaseClass1,class G3_C1827`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()#G2_C772::ClassMethod1351.MI.12147<System.Object>()#G2_C772::ClassMethod2925.12143()#G3_C1827::ClassMethod2926.MI.18230()#G2_C772::ClassMethod2927.12145<System.Object>()#G3_C1827::ClassMethod2928.MI.18231<System.Object>()#G2_C772::Method4.12139()#G2_C772::Method5.12140()#G2_C772::Method6.12142<System.Object>()#G2_C772::Method7.12137<System.Object>()#"
		call void Framework::M.G2_C772.B.T<class BaseClass1,class G3_C1827`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()#G2_C772::ClassMethod1351.MI.12147<System.Object>()#G2_C772::ClassMethod2925.12143()#G3_C1827::ClassMethod2926.MI.18230()#G2_C772::ClassMethod2927.12145<System.Object>()#G3_C1827::ClassMethod2928.MI.18231<System.Object>()#G2_C772::Method4.12139()#G2_C772::Method5.12140()#G2_C772::Method6.12142<System.Object>()#G2_C772::Method7.12137<System.Object>()#"
		call void Framework::M.G2_C772.B.B<class G3_C1827`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()#G2_C772::ClassMethod1351.MI.12147<System.Object>()#G2_C772::ClassMethod2925.12143()#G3_C1827::ClassMethod2926.MI.18230()#G2_C772::ClassMethod2927.12145<System.Object>()#G3_C1827::ClassMethod2928.MI.18231<System.Object>()#G3_C1827::ClassMethod5048.18229<System.Object>()#G3_C1827::Method0.18221()#G3_C1827::Method1.18223()#G3_C1827::Method2.18225<System.Object>()#G3_C1827::Method3.18227<System.Object>()#G2_C772::Method4.12139()#G2_C772::Method5.12140()#G2_C772::Method6.12142<System.Object>()#G2_C772::Method7.12137<System.Object>()#"
		call void Framework::M.G3_C1827.T<class BaseClass1,class G3_C1827`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()#G2_C772::ClassMethod1351.MI.12147<System.Object>()#G2_C772::ClassMethod2925.12143()#G3_C1827::ClassMethod2926.MI.18230()#G2_C772::ClassMethod2927.12145<System.Object>()#G3_C1827::ClassMethod2928.MI.18231<System.Object>()#G3_C1827::ClassMethod5048.18229<System.Object>()#G3_C1827::Method0.18221()#G3_C1827::Method1.18223()#G3_C1827::Method2.18225<System.Object>()#G3_C1827::Method3.18227<System.Object>()#G2_C772::Method4.12139()#G2_C772::Method5.12140()#G2_C772::Method6.12142<System.Object>()#G2_C772::Method7.12137<System.Object>()#"
		call void Framework::M.G3_C1827.B<class G3_C1827`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1827::Method0.MI.18222()#G3_C1827::Method1.MI.18224()#G3_C1827::Method2.MI.18226<System.Object>()#G3_C1827::Method3.MI.18228<System.Object>()#"
		call void Framework::M.IBase0<class G3_C1827`1<class BaseClass1>>(!!0,string)
		newobj instance void class G2_C772`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()#G2_C772::ClassMethod1351.MI.12147<System.Object>()#G2_C772::Method4.12139()#G2_C772::Method5.12140()#G2_C772::Method6.12142<System.Object>()#G2_C772::Method7.12137<System.Object>()#"
		call void Framework::M.G1_C14.T.T<class BaseClass1,class BaseClass0,class G2_C772`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()#G2_C772::ClassMethod1351.MI.12147<System.Object>()#G2_C772::Method4.12139()#G2_C772::Method5.12140()#G2_C772::Method6.12142<System.Object>()#G2_C772::Method7.12137<System.Object>()#"
		call void Framework::M.G1_C14.B.T<class BaseClass0,class G2_C772`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()#G2_C772::ClassMethod1351.MI.12147<System.Object>()#G2_C772::Method4.12139()#G2_C772::Method5.12140()#G2_C772::Method6.12142<System.Object>()#G2_C772::Method7.12137<System.Object>()#"
		call void Framework::M.G1_C14.B.A<class G2_C772`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C772::Method7.MI.12138<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C772`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C772::Method7.MI.12138<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G2_C772`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C772::Method7.MI.12138<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G2_C772`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C772::Method4.12139()#G1_C14::Method5.MI.4881()#G2_C772::Method6.12142<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass1,class G2_C772`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C772::Method4.12139()#G1_C14::Method5.MI.4881()#G2_C772::Method6.12142<System.Object>()#"
		call void Framework::M.IBase1.B<class G2_C772`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C772::Method4.12139()#G2_C772::Method5.MI.12141()#G2_C772::Method6.12142<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G2_C772`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C772::Method4.12139()#G2_C772::Method5.MI.12141()#G2_C772::Method6.12142<System.Object>()#"
		call void Framework::M.IBase1.A<class G2_C772`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()#G2_C772::ClassMethod1351.MI.12147<System.Object>()#G2_C772::ClassMethod2925.12143()#G2_C772::ClassMethod2926.12144()#G2_C772::ClassMethod2927.12145<System.Object>()#G2_C772::ClassMethod2928.12146<System.Object>()#G2_C772::Method4.12139()#G2_C772::Method5.12140()#G2_C772::Method6.12142<System.Object>()#G2_C772::Method7.12137<System.Object>()#"
		call void Framework::M.G2_C772.T.T<class BaseClass0,class BaseClass0,class G2_C772`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()#G2_C772::ClassMethod1351.MI.12147<System.Object>()#G2_C772::ClassMethod2925.12143()#G2_C772::ClassMethod2926.12144()#G2_C772::ClassMethod2927.12145<System.Object>()#G2_C772::ClassMethod2928.12146<System.Object>()#G2_C772::Method4.12139()#G2_C772::Method5.12140()#G2_C772::Method6.12142<System.Object>()#G2_C772::Method7.12137<System.Object>()#"
		call void Framework::M.G2_C772.A.T<class BaseClass0,class G2_C772`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()#G2_C772::ClassMethod1351.MI.12147<System.Object>()#G2_C772::ClassMethod2925.12143()#G2_C772::ClassMethod2926.12144()#G2_C772::ClassMethod2927.12145<System.Object>()#G2_C772::ClassMethod2928.12146<System.Object>()#G2_C772::Method4.12139()#G2_C772::Method5.12140()#G2_C772::Method6.12142<System.Object>()#G2_C772::Method7.12137<System.Object>()#"
		call void Framework::M.G2_C772.A.A<class G2_C772`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C772::Method7.MI.12138<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G2_C772`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C772::Method7.MI.12138<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G2_C772`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C772::Method7.MI.12138<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G2_C772`2<class BaseClass0,class BaseClass0>>(!!0,string)
		newobj instance void class G2_C772`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()#G2_C772::ClassMethod1351.MI.12147<System.Object>()#G2_C772::Method4.12139()#G2_C772::Method5.12140()#G2_C772::Method6.12142<System.Object>()#G2_C772::Method7.12137<System.Object>()#"
		call void Framework::M.G1_C14.T.T<class BaseClass1,class BaseClass1,class G2_C772`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()#G2_C772::ClassMethod1351.MI.12147<System.Object>()#G2_C772::Method4.12139()#G2_C772::Method5.12140()#G2_C772::Method6.12142<System.Object>()#G2_C772::Method7.12137<System.Object>()#"
		call void Framework::M.G1_C14.B.T<class BaseClass1,class G2_C772`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()#G2_C772::ClassMethod1351.MI.12147<System.Object>()#G2_C772::Method4.12139()#G2_C772::Method5.12140()#G2_C772::Method6.12142<System.Object>()#G2_C772::Method7.12137<System.Object>()#"
		call void Framework::M.G1_C14.B.B<class G2_C772`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C772::Method7.MI.12138<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G2_C772`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C772::Method7.MI.12138<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G2_C772`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C772::Method7.MI.12138<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G2_C772`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C772::Method4.12139()#G2_C772::Method5.MI.12141()#G2_C772::Method6.12142<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass1,class G2_C772`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C772::Method4.12139()#G2_C772::Method5.MI.12141()#G2_C772::Method6.12142<System.Object>()#"
		call void Framework::M.IBase1.B<class G2_C772`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C772::Method7.MI.12138<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C772`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C772::Method7.MI.12138<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G2_C772`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C772::Method7.MI.12138<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G2_C772`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C772::Method4.12139()#G2_C772::Method5.MI.12141()#G2_C772::Method6.12142<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G2_C772`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C772::Method4.12139()#G2_C772::Method5.MI.12141()#G2_C772::Method6.12142<System.Object>()#"
		call void Framework::M.IBase1.A<class G2_C772`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()#G2_C772::ClassMethod1351.MI.12147<System.Object>()#G2_C772::ClassMethod2925.12143()#G2_C772::ClassMethod2926.12144()#G2_C772::ClassMethod2927.12145<System.Object>()#G2_C772::ClassMethod2928.12146<System.Object>()#G2_C772::Method4.12139()#G2_C772::Method5.12140()#G2_C772::Method6.12142<System.Object>()#G2_C772::Method7.12137<System.Object>()#"
		call void Framework::M.G2_C772.T.T<class BaseClass0,class BaseClass1,class G2_C772`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()#G2_C772::ClassMethod1351.MI.12147<System.Object>()#G2_C772::ClassMethod2925.12143()#G2_C772::ClassMethod2926.12144()#G2_C772::ClassMethod2927.12145<System.Object>()#G2_C772::ClassMethod2928.12146<System.Object>()#G2_C772::Method4.12139()#G2_C772::Method5.12140()#G2_C772::Method6.12142<System.Object>()#G2_C772::Method7.12137<System.Object>()#"
		call void Framework::M.G2_C772.A.T<class BaseClass1,class G2_C772`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()#G2_C772::ClassMethod1351.MI.12147<System.Object>()#G2_C772::ClassMethod2925.12143()#G2_C772::ClassMethod2926.12144()#G2_C772::ClassMethod2927.12145<System.Object>()#G2_C772::ClassMethod2928.12146<System.Object>()#G2_C772::Method4.12139()#G2_C772::Method5.12140()#G2_C772::Method6.12142<System.Object>()#G2_C772::Method7.12137<System.Object>()#"
		call void Framework::M.G2_C772.A.B<class G2_C772`2<class BaseClass0,class BaseClass1>>(!!0,string)
		newobj instance void class G2_C772`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()#G2_C772::ClassMethod1351.MI.12147<System.Object>()#G2_C772::Method4.12139()#G2_C772::Method5.12140()#G2_C772::Method6.12142<System.Object>()#G2_C772::Method7.12137<System.Object>()#"
		call void Framework::M.G1_C14.T.T<class BaseClass1,class BaseClass0,class G2_C772`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()#G2_C772::ClassMethod1351.MI.12147<System.Object>()#G2_C772::Method4.12139()#G2_C772::Method5.12140()#G2_C772::Method6.12142<System.Object>()#G2_C772::Method7.12137<System.Object>()#"
		call void Framework::M.G1_C14.B.T<class BaseClass0,class G2_C772`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()#G2_C772::ClassMethod1351.MI.12147<System.Object>()#G2_C772::Method4.12139()#G2_C772::Method5.12140()#G2_C772::Method6.12142<System.Object>()#G2_C772::Method7.12137<System.Object>()#"
		call void Framework::M.G1_C14.B.A<class G2_C772`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C772::Method7.MI.12138<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C772`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C772::Method7.MI.12138<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G2_C772`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C772::Method7.MI.12138<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G2_C772`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C772::Method4.12139()#G1_C14::Method5.MI.4881()#G2_C772::Method6.12142<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass1,class G2_C772`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C772::Method4.12139()#G1_C14::Method5.MI.4881()#G2_C772::Method6.12142<System.Object>()#"
		call void Framework::M.IBase1.B<class G2_C772`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C772::Method4.12139()#G2_C772::Method5.MI.12141()#G2_C772::Method6.12142<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G2_C772`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C772::Method4.12139()#G2_C772::Method5.MI.12141()#G2_C772::Method6.12142<System.Object>()#"
		call void Framework::M.IBase1.A<class G2_C772`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()#G2_C772::ClassMethod1351.MI.12147<System.Object>()#G2_C772::ClassMethod2925.12143()#G2_C772::ClassMethod2926.12144()#G2_C772::ClassMethod2927.12145<System.Object>()#G2_C772::ClassMethod2928.12146<System.Object>()#G2_C772::Method4.12139()#G2_C772::Method5.12140()#G2_C772::Method6.12142<System.Object>()#G2_C772::Method7.12137<System.Object>()#"
		call void Framework::M.G2_C772.T.T<class BaseClass1,class BaseClass0,class G2_C772`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()#G2_C772::ClassMethod1351.MI.12147<System.Object>()#G2_C772::ClassMethod2925.12143()#G2_C772::ClassMethod2926.12144()#G2_C772::ClassMethod2927.12145<System.Object>()#G2_C772::ClassMethod2928.12146<System.Object>()#G2_C772::Method4.12139()#G2_C772::Method5.12140()#G2_C772::Method6.12142<System.Object>()#G2_C772::Method7.12137<System.Object>()#"
		call void Framework::M.G2_C772.B.T<class BaseClass0,class G2_C772`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()#G2_C772::ClassMethod1351.MI.12147<System.Object>()#G2_C772::ClassMethod2925.12143()#G2_C772::ClassMethod2926.12144()#G2_C772::ClassMethod2927.12145<System.Object>()#G2_C772::ClassMethod2928.12146<System.Object>()#G2_C772::Method4.12139()#G2_C772::Method5.12140()#G2_C772::Method6.12142<System.Object>()#G2_C772::Method7.12137<System.Object>()#"
		call void Framework::M.G2_C772.B.A<class G2_C772`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C772::Method7.MI.12138<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G2_C772`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C772::Method7.MI.12138<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G2_C772`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C772::Method7.MI.12138<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G2_C772`2<class BaseClass1,class BaseClass0>>(!!0,string)
		newobj instance void class G2_C772`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()#G2_C772::ClassMethod1351.MI.12147<System.Object>()#G2_C772::Method4.12139()#G2_C772::Method5.12140()#G2_C772::Method6.12142<System.Object>()#G2_C772::Method7.12137<System.Object>()#"
		call void Framework::M.G1_C14.T.T<class BaseClass1,class BaseClass1,class G2_C772`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()#G2_C772::ClassMethod1351.MI.12147<System.Object>()#G2_C772::Method4.12139()#G2_C772::Method5.12140()#G2_C772::Method6.12142<System.Object>()#G2_C772::Method7.12137<System.Object>()#"
		call void Framework::M.G1_C14.B.T<class BaseClass1,class G2_C772`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()#G2_C772::ClassMethod1351.MI.12147<System.Object>()#G2_C772::Method4.12139()#G2_C772::Method5.12140()#G2_C772::Method6.12142<System.Object>()#G2_C772::Method7.12137<System.Object>()#"
		call void Framework::M.G1_C14.B.B<class G2_C772`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C772::Method7.MI.12138<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G2_C772`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C772::Method7.MI.12138<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G2_C772`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C772::Method7.MI.12138<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G2_C772`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C772::Method4.12139()#G2_C772::Method5.MI.12141()#G2_C772::Method6.12142<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass1,class G2_C772`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C772::Method4.12139()#G2_C772::Method5.MI.12141()#G2_C772::Method6.12142<System.Object>()#"
		call void Framework::M.IBase1.B<class G2_C772`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C772::Method7.MI.12138<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C772`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C772::Method7.MI.12138<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G2_C772`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C772::Method7.MI.12138<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G2_C772`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C772::Method4.12139()#G2_C772::Method5.MI.12141()#G2_C772::Method6.12142<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G2_C772`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C772::Method4.12139()#G2_C772::Method5.MI.12141()#G2_C772::Method6.12142<System.Object>()#"
		call void Framework::M.IBase1.A<class G2_C772`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()#G2_C772::ClassMethod1351.MI.12147<System.Object>()#G2_C772::ClassMethod2925.12143()#G2_C772::ClassMethod2926.12144()#G2_C772::ClassMethod2927.12145<System.Object>()#G2_C772::ClassMethod2928.12146<System.Object>()#G2_C772::Method4.12139()#G2_C772::Method5.12140()#G2_C772::Method6.12142<System.Object>()#G2_C772::Method7.12137<System.Object>()#"
		call void Framework::M.G2_C772.T.T<class BaseClass1,class BaseClass1,class G2_C772`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()#G2_C772::ClassMethod1351.MI.12147<System.Object>()#G2_C772::ClassMethod2925.12143()#G2_C772::ClassMethod2926.12144()#G2_C772::ClassMethod2927.12145<System.Object>()#G2_C772::ClassMethod2928.12146<System.Object>()#G2_C772::Method4.12139()#G2_C772::Method5.12140()#G2_C772::Method6.12142<System.Object>()#G2_C772::Method7.12137<System.Object>()#"
		call void Framework::M.G2_C772.B.T<class BaseClass1,class G2_C772`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()#G2_C772::ClassMethod1351.MI.12147<System.Object>()#G2_C772::ClassMethod2925.12143()#G2_C772::ClassMethod2926.12144()#G2_C772::ClassMethod2927.12145<System.Object>()#G2_C772::ClassMethod2928.12146<System.Object>()#G2_C772::Method4.12139()#G2_C772::Method5.12140()#G2_C772::Method6.12142<System.Object>()#G2_C772::Method7.12137<System.Object>()#"
		call void Framework::M.G2_C772.B.B<class G2_C772`2<class BaseClass1,class BaseClass1>>(!!0,string)
		newobj instance void class G1_C14`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()#G1_C14::ClassMethod1351.4884<System.Object>()#G1_C14::Method4.4879()#G1_C14::Method5.4880()#G1_C14::Method6.4882<System.Object>()#G1_C14::Method7.4878<System.Object>()#"
		call void Framework::M.G1_C14.T.T<class BaseClass0,class BaseClass0,class G1_C14`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()#G1_C14::ClassMethod1351.4884<System.Object>()#G1_C14::Method4.4879()#G1_C14::Method5.4880()#G1_C14::Method6.4882<System.Object>()#G1_C14::Method7.4878<System.Object>()#"
		call void Framework::M.G1_C14.A.T<class BaseClass0,class G1_C14`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()#G1_C14::ClassMethod1351.4884<System.Object>()#G1_C14::Method4.4879()#G1_C14::Method5.4880()#G1_C14::Method6.4882<System.Object>()#G1_C14::Method7.4878<System.Object>()#"
		call void Framework::M.G1_C14.A.A<class G1_C14`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C14::Method7.4878<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G1_C14`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C14::Method7.4878<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G1_C14`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C14::Method7.4878<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G1_C14`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C14::Method4.4879()#G1_C14::Method5.MI.4881()#G1_C14::Method6.4882<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass1,class G1_C14`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C14::Method4.4879()#G1_C14::Method5.MI.4881()#G1_C14::Method6.4882<System.Object>()#"
		call void Framework::M.IBase1.B<class G1_C14`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C14::Method4.4879()#G1_C14::Method5.MI.4881()#G1_C14::Method6.4882<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G1_C14`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C14::Method4.4879()#G1_C14::Method5.MI.4881()#G1_C14::Method6.4882<System.Object>()#"
		call void Framework::M.IBase1.A<class G1_C14`2<class BaseClass0,class BaseClass0>>(!!0,string)
		newobj instance void class G1_C14`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()#G1_C14::ClassMethod1351.4884<System.Object>()#G1_C14::Method4.4879()#G1_C14::Method5.4880()#G1_C14::Method6.4882<System.Object>()#G1_C14::Method7.4878<System.Object>()#"
		call void Framework::M.G1_C14.T.T<class BaseClass0,class BaseClass1,class G1_C14`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()#G1_C14::ClassMethod1351.4884<System.Object>()#G1_C14::Method4.4879()#G1_C14::Method5.4880()#G1_C14::Method6.4882<System.Object>()#G1_C14::Method7.4878<System.Object>()#"
		call void Framework::M.G1_C14.A.T<class BaseClass1,class G1_C14`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()#G1_C14::ClassMethod1351.4884<System.Object>()#G1_C14::Method4.4879()#G1_C14::Method5.4880()#G1_C14::Method6.4882<System.Object>()#G1_C14::Method7.4878<System.Object>()#"
		call void Framework::M.G1_C14.A.B<class G1_C14`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C14::Method7.4878<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G1_C14`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C14::Method7.4878<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G1_C14`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C14::Method7.4878<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G1_C14`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C14::Method4.4879()#G1_C14::Method5.MI.4881()#G1_C14::Method6.4882<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass1,class G1_C14`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C14::Method4.4879()#G1_C14::Method5.MI.4881()#G1_C14::Method6.4882<System.Object>()#"
		call void Framework::M.IBase1.B<class G1_C14`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C14::Method7.4878<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G1_C14`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C14::Method7.4878<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G1_C14`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C14::Method7.4878<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G1_C14`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C14::Method4.4879()#G1_C14::Method5.MI.4881()#G1_C14::Method6.4882<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G1_C14`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C14::Method4.4879()#G1_C14::Method5.MI.4881()#G1_C14::Method6.4882<System.Object>()#"
		call void Framework::M.IBase1.A<class G1_C14`2<class BaseClass0,class BaseClass1>>(!!0,string)
		newobj instance void class G1_C14`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()#G1_C14::ClassMethod1351.4884<System.Object>()#G1_C14::Method4.4879()#G1_C14::Method5.4880()#G1_C14::Method6.4882<System.Object>()#G1_C14::Method7.4878<System.Object>()#"
		call void Framework::M.G1_C14.T.T<class BaseClass1,class BaseClass0,class G1_C14`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()#G1_C14::ClassMethod1351.4884<System.Object>()#G1_C14::Method4.4879()#G1_C14::Method5.4880()#G1_C14::Method6.4882<System.Object>()#G1_C14::Method7.4878<System.Object>()#"
		call void Framework::M.G1_C14.B.T<class BaseClass0,class G1_C14`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()#G1_C14::ClassMethod1351.4884<System.Object>()#G1_C14::Method4.4879()#G1_C14::Method5.4880()#G1_C14::Method6.4882<System.Object>()#G1_C14::Method7.4878<System.Object>()#"
		call void Framework::M.G1_C14.B.A<class G1_C14`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C14::Method7.4878<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G1_C14`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C14::Method7.4878<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G1_C14`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C14::Method7.4878<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G1_C14`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C14::Method4.4879()#G1_C14::Method5.MI.4881()#G1_C14::Method6.4882<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass1,class G1_C14`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C14::Method4.4879()#G1_C14::Method5.MI.4881()#G1_C14::Method6.4882<System.Object>()#"
		call void Framework::M.IBase1.B<class G1_C14`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C14::Method4.4879()#G1_C14::Method5.MI.4881()#G1_C14::Method6.4882<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G1_C14`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C14::Method4.4879()#G1_C14::Method5.MI.4881()#G1_C14::Method6.4882<System.Object>()#"
		call void Framework::M.IBase1.A<class G1_C14`2<class BaseClass1,class BaseClass0>>(!!0,string)
		newobj instance void class G1_C14`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()#G1_C14::ClassMethod1351.4884<System.Object>()#G1_C14::Method4.4879()#G1_C14::Method5.4880()#G1_C14::Method6.4882<System.Object>()#G1_C14::Method7.4878<System.Object>()#"
		call void Framework::M.G1_C14.T.T<class BaseClass1,class BaseClass1,class G1_C14`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()#G1_C14::ClassMethod1351.4884<System.Object>()#G1_C14::Method4.4879()#G1_C14::Method5.4880()#G1_C14::Method6.4882<System.Object>()#G1_C14::Method7.4878<System.Object>()#"
		call void Framework::M.G1_C14.B.T<class BaseClass1,class G1_C14`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()#G1_C14::ClassMethod1351.4884<System.Object>()#G1_C14::Method4.4879()#G1_C14::Method5.4880()#G1_C14::Method6.4882<System.Object>()#G1_C14::Method7.4878<System.Object>()#"
		call void Framework::M.G1_C14.B.B<class G1_C14`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C14::Method7.4878<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G1_C14`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C14::Method7.4878<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G1_C14`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C14::Method7.4878<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G1_C14`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C14::Method4.4879()#G1_C14::Method5.MI.4881()#G1_C14::Method6.4882<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass1,class G1_C14`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C14::Method4.4879()#G1_C14::Method5.MI.4881()#G1_C14::Method6.4882<System.Object>()#"
		call void Framework::M.IBase1.B<class G1_C14`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C14::Method7.4878<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G1_C14`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C14::Method7.4878<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G1_C14`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C14::Method7.4878<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G1_C14`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C14::Method4.4879()#G1_C14::Method5.MI.4881()#G1_C14::Method6.4882<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G1_C14`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C14::Method4.4879()#G1_C14::Method5.MI.4881()#G1_C14::Method6.4882<System.Object>()#"
		call void Framework::M.IBase1.A<class G1_C14`2<class BaseClass1,class BaseClass1>>(!!0,string)
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
		newobj instance void class G3_C1827`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C14`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C14`2<class BaseClass1,class BaseClass1>::ClassMethod1351<object>()
		calli default string(class G3_C1827`1<class BaseClass0>)
		ldstr "G2_C772::ClassMethod1351.MI.12147<System.Object>()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass1> on type class G3_C1827`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C14`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C14`2<class BaseClass1,class BaseClass1>::ClassMethod1350<object>()
		calli default string(class G3_C1827`1<class BaseClass0>)
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass1> on type class G3_C1827`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C14`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C14`2<class BaseClass1,class BaseClass1>::Method6<object>()
		calli default string(class G3_C1827`1<class BaseClass0>)
		ldstr "G2_C772::Method6.12142<System.Object>()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass1> on type class G3_C1827`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C14`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C14`2<class BaseClass1,class BaseClass1>::Method5()
		calli default string(class G3_C1827`1<class BaseClass0>)
		ldstr "G2_C772::Method5.12140()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass1> on type class G3_C1827`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C14`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C14`2<class BaseClass1,class BaseClass1>::Method4()
		calli default string(class G3_C1827`1<class BaseClass0>)
		ldstr "G2_C772::Method4.12139()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass1> on type class G3_C1827`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C14`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C14`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1827`1<class BaseClass0>)
		ldstr "G2_C772::Method7.12137<System.Object>()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass1> on type class G3_C1827`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1827`1<class BaseClass0>)
		ldstr "G2_C772::Method7.MI.12138<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1827`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G3_C1827`1<class BaseClass0>)
		ldstr "G2_C772::Method4.12139()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1827`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G3_C1827`1<class BaseClass0>)
		ldstr "G2_C772::Method5.MI.12141()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1827`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G3_C1827`1<class BaseClass0>)
		ldstr "G2_C772::Method6.12142<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1827`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1827`1<class BaseClass0>)
		ldstr "G2_C772::Method7.MI.12138<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1827`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G3_C1827`1<class BaseClass0>)
		ldstr "G2_C772::Method4.12139()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1827`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G3_C1827`1<class BaseClass0>)
		ldstr "G2_C772::Method5.MI.12141()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1827`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1827`1<class BaseClass0>)
		ldstr "G2_C772::Method6.12142<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1827`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C772`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C772`2<class BaseClass0,class BaseClass1>::ClassMethod2928<object>()
		calli default string(class G3_C1827`1<class BaseClass0>)
		ldstr "G3_C1827::ClassMethod2928.MI.18231<System.Object>()"
		ldstr "class G2_C772`2<class BaseClass0,class BaseClass1> on type class G3_C1827`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C772`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C772`2<class BaseClass0,class BaseClass1>::ClassMethod2927<object>()
		calli default string(class G3_C1827`1<class BaseClass0>)
		ldstr "G2_C772::ClassMethod2927.12145<System.Object>()"
		ldstr "class G2_C772`2<class BaseClass0,class BaseClass1> on type class G3_C1827`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C772`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C772`2<class BaseClass0,class BaseClass1>::ClassMethod2926()
		calli default string(class G3_C1827`1<class BaseClass0>)
		ldstr "G3_C1827::ClassMethod2926.MI.18230()"
		ldstr "class G2_C772`2<class BaseClass0,class BaseClass1> on type class G3_C1827`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C772`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C772`2<class BaseClass0,class BaseClass1>::ClassMethod2925()
		calli default string(class G3_C1827`1<class BaseClass0>)
		ldstr "G2_C772::ClassMethod2925.12143()"
		ldstr "class G2_C772`2<class BaseClass0,class BaseClass1> on type class G3_C1827`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C772`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C772`2<class BaseClass0,class BaseClass1>::Method6<object>()
		calli default string(class G3_C1827`1<class BaseClass0>)
		ldstr "G2_C772::Method6.12142<System.Object>()"
		ldstr "class G2_C772`2<class BaseClass0,class BaseClass1> on type class G3_C1827`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C772`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C772`2<class BaseClass0,class BaseClass1>::Method5()
		calli default string(class G3_C1827`1<class BaseClass0>)
		ldstr "G2_C772::Method5.12140()"
		ldstr "class G2_C772`2<class BaseClass0,class BaseClass1> on type class G3_C1827`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C772`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C772`2<class BaseClass0,class BaseClass1>::Method4()
		calli default string(class G3_C1827`1<class BaseClass0>)
		ldstr "G2_C772::Method4.12139()"
		ldstr "class G2_C772`2<class BaseClass0,class BaseClass1> on type class G3_C1827`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C772`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C772`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1827`1<class BaseClass0>)
		ldstr "G2_C772::Method7.12137<System.Object>()"
		ldstr "class G2_C772`2<class BaseClass0,class BaseClass1> on type class G3_C1827`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C772`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C772`2<class BaseClass0,class BaseClass1>::ClassMethod1351<object>()
		calli default string(class G3_C1827`1<class BaseClass0>)
		ldstr "G2_C772::ClassMethod1351.MI.12147<System.Object>()"
		ldstr "class G2_C772`2<class BaseClass0,class BaseClass1> on type class G3_C1827`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C772`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C772`2<class BaseClass0,class BaseClass1>::ClassMethod1350<object>()
		calli default string(class G3_C1827`1<class BaseClass0>)
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()"
		ldstr "class G2_C772`2<class BaseClass0,class BaseClass1> on type class G3_C1827`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1827`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1827`1<class BaseClass0>::ClassMethod5048<object>()
		calli default string(class G3_C1827`1<class BaseClass0>)
		ldstr "G3_C1827::ClassMethod5048.18229<System.Object>()"
		ldstr "class G3_C1827`1<class BaseClass0> on type class G3_C1827`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1827`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1827`1<class BaseClass0>::Method3<object>()
		calli default string(class G3_C1827`1<class BaseClass0>)
		ldstr "G3_C1827::Method3.18227<System.Object>()"
		ldstr "class G3_C1827`1<class BaseClass0> on type class G3_C1827`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1827`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1827`1<class BaseClass0>::Method2<object>()
		calli default string(class G3_C1827`1<class BaseClass0>)
		ldstr "G3_C1827::Method2.18225<System.Object>()"
		ldstr "class G3_C1827`1<class BaseClass0> on type class G3_C1827`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1827`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1827`1<class BaseClass0>::Method1()
		calli default string(class G3_C1827`1<class BaseClass0>)
		ldstr "G3_C1827::Method1.18223()"
		ldstr "class G3_C1827`1<class BaseClass0> on type class G3_C1827`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1827`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1827`1<class BaseClass0>::Method0()
		calli default string(class G3_C1827`1<class BaseClass0>)
		ldstr "G3_C1827::Method0.18221()"
		ldstr "class G3_C1827`1<class BaseClass0> on type class G3_C1827`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1827`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1827`1<class BaseClass0>::ClassMethod2928<object>()
		calli default string(class G3_C1827`1<class BaseClass0>)
		ldstr "G3_C1827::ClassMethod2928.MI.18231<System.Object>()"
		ldstr "class G3_C1827`1<class BaseClass0> on type class G3_C1827`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1827`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1827`1<class BaseClass0>::ClassMethod2927<object>()
		calli default string(class G3_C1827`1<class BaseClass0>)
		ldstr "G2_C772::ClassMethod2927.12145<System.Object>()"
		ldstr "class G3_C1827`1<class BaseClass0> on type class G3_C1827`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1827`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1827`1<class BaseClass0>::ClassMethod2926()
		calli default string(class G3_C1827`1<class BaseClass0>)
		ldstr "G3_C1827::ClassMethod2926.MI.18230()"
		ldstr "class G3_C1827`1<class BaseClass0> on type class G3_C1827`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1827`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1827`1<class BaseClass0>::ClassMethod2925()
		calli default string(class G3_C1827`1<class BaseClass0>)
		ldstr "G2_C772::ClassMethod2925.12143()"
		ldstr "class G3_C1827`1<class BaseClass0> on type class G3_C1827`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1827`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1827`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1827`1<class BaseClass0>)
		ldstr "G2_C772::Method6.12142<System.Object>()"
		ldstr "class G3_C1827`1<class BaseClass0> on type class G3_C1827`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1827`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1827`1<class BaseClass0>::Method5()
		calli default string(class G3_C1827`1<class BaseClass0>)
		ldstr "G2_C772::Method5.12140()"
		ldstr "class G3_C1827`1<class BaseClass0> on type class G3_C1827`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1827`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1827`1<class BaseClass0>::Method4()
		calli default string(class G3_C1827`1<class BaseClass0>)
		ldstr "G2_C772::Method4.12139()"
		ldstr "class G3_C1827`1<class BaseClass0> on type class G3_C1827`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1827`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1827`1<class BaseClass0>::Method7<object>()
		calli default string(class G3_C1827`1<class BaseClass0>)
		ldstr "G2_C772::Method7.12137<System.Object>()"
		ldstr "class G3_C1827`1<class BaseClass0> on type class G3_C1827`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1827`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1827`1<class BaseClass0>::ClassMethod1351<object>()
		calli default string(class G3_C1827`1<class BaseClass0>)
		ldstr "G2_C772::ClassMethod1351.MI.12147<System.Object>()"
		ldstr "class G3_C1827`1<class BaseClass0> on type class G3_C1827`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1827`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1827`1<class BaseClass0>::ClassMethod1350<object>()
		calli default string(class G3_C1827`1<class BaseClass0>)
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()"
		ldstr "class G3_C1827`1<class BaseClass0> on type class G3_C1827`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method0()
		calli default string(class G3_C1827`1<class BaseClass0>)
		ldstr "G3_C1827::Method0.MI.18222()"
		ldstr "IBase0 on type class G3_C1827`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method1()
		calli default string(class G3_C1827`1<class BaseClass0>)
		ldstr "G3_C1827::Method1.MI.18224()"
		ldstr "IBase0 on type class G3_C1827`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method2<object>()
		calli default string(class G3_C1827`1<class BaseClass0>)
		ldstr "G3_C1827::Method2.MI.18226<System.Object>()"
		ldstr "IBase0 on type class G3_C1827`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method3<object>()
		calli default string(class G3_C1827`1<class BaseClass0>)
		ldstr "G3_C1827::Method3.MI.18228<System.Object>()"
		ldstr "IBase0 on type class G3_C1827`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G3_C1827`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C14`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C14`2<class BaseClass1,class BaseClass1>::ClassMethod1351<object>()
		calli default string(class G3_C1827`1<class BaseClass1>)
		ldstr "G2_C772::ClassMethod1351.MI.12147<System.Object>()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass1> on type class G3_C1827`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C14`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C14`2<class BaseClass1,class BaseClass1>::ClassMethod1350<object>()
		calli default string(class G3_C1827`1<class BaseClass1>)
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass1> on type class G3_C1827`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C14`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C14`2<class BaseClass1,class BaseClass1>::Method6<object>()
		calli default string(class G3_C1827`1<class BaseClass1>)
		ldstr "G2_C772::Method6.12142<System.Object>()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass1> on type class G3_C1827`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C14`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C14`2<class BaseClass1,class BaseClass1>::Method5()
		calli default string(class G3_C1827`1<class BaseClass1>)
		ldstr "G2_C772::Method5.12140()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass1> on type class G3_C1827`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C14`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C14`2<class BaseClass1,class BaseClass1>::Method4()
		calli default string(class G3_C1827`1<class BaseClass1>)
		ldstr "G2_C772::Method4.12139()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass1> on type class G3_C1827`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C14`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C14`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1827`1<class BaseClass1>)
		ldstr "G2_C772::Method7.12137<System.Object>()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass1> on type class G3_C1827`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1827`1<class BaseClass1>)
		ldstr "G2_C772::Method7.MI.12138<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1827`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G3_C1827`1<class BaseClass1>)
		ldstr "G2_C772::Method4.12139()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1827`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G3_C1827`1<class BaseClass1>)
		ldstr "G2_C772::Method5.MI.12141()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1827`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G3_C1827`1<class BaseClass1>)
		ldstr "G2_C772::Method6.12142<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1827`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1827`1<class BaseClass1>)
		ldstr "G2_C772::Method7.MI.12138<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1827`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G3_C1827`1<class BaseClass1>)
		ldstr "G2_C772::Method4.12139()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1827`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G3_C1827`1<class BaseClass1>)
		ldstr "G2_C772::Method5.MI.12141()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1827`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1827`1<class BaseClass1>)
		ldstr "G2_C772::Method6.12142<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1827`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C772`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C772`2<class BaseClass1,class BaseClass1>::ClassMethod2928<object>()
		calli default string(class G3_C1827`1<class BaseClass1>)
		ldstr "G3_C1827::ClassMethod2928.MI.18231<System.Object>()"
		ldstr "class G2_C772`2<class BaseClass1,class BaseClass1> on type class G3_C1827`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C772`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C772`2<class BaseClass1,class BaseClass1>::ClassMethod2927<object>()
		calli default string(class G3_C1827`1<class BaseClass1>)
		ldstr "G2_C772::ClassMethod2927.12145<System.Object>()"
		ldstr "class G2_C772`2<class BaseClass1,class BaseClass1> on type class G3_C1827`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C772`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C772`2<class BaseClass1,class BaseClass1>::ClassMethod2926()
		calli default string(class G3_C1827`1<class BaseClass1>)
		ldstr "G3_C1827::ClassMethod2926.MI.18230()"
		ldstr "class G2_C772`2<class BaseClass1,class BaseClass1> on type class G3_C1827`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C772`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C772`2<class BaseClass1,class BaseClass1>::ClassMethod2925()
		calli default string(class G3_C1827`1<class BaseClass1>)
		ldstr "G2_C772::ClassMethod2925.12143()"
		ldstr "class G2_C772`2<class BaseClass1,class BaseClass1> on type class G3_C1827`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C772`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C772`2<class BaseClass1,class BaseClass1>::Method6<object>()
		calli default string(class G3_C1827`1<class BaseClass1>)
		ldstr "G2_C772::Method6.12142<System.Object>()"
		ldstr "class G2_C772`2<class BaseClass1,class BaseClass1> on type class G3_C1827`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C772`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C772`2<class BaseClass1,class BaseClass1>::Method5()
		calli default string(class G3_C1827`1<class BaseClass1>)
		ldstr "G2_C772::Method5.12140()"
		ldstr "class G2_C772`2<class BaseClass1,class BaseClass1> on type class G3_C1827`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C772`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C772`2<class BaseClass1,class BaseClass1>::Method4()
		calli default string(class G3_C1827`1<class BaseClass1>)
		ldstr "G2_C772::Method4.12139()"
		ldstr "class G2_C772`2<class BaseClass1,class BaseClass1> on type class G3_C1827`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C772`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C772`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1827`1<class BaseClass1>)
		ldstr "G2_C772::Method7.12137<System.Object>()"
		ldstr "class G2_C772`2<class BaseClass1,class BaseClass1> on type class G3_C1827`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C772`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C772`2<class BaseClass1,class BaseClass1>::ClassMethod1351<object>()
		calli default string(class G3_C1827`1<class BaseClass1>)
		ldstr "G2_C772::ClassMethod1351.MI.12147<System.Object>()"
		ldstr "class G2_C772`2<class BaseClass1,class BaseClass1> on type class G3_C1827`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C772`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C772`2<class BaseClass1,class BaseClass1>::ClassMethod1350<object>()
		calli default string(class G3_C1827`1<class BaseClass1>)
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()"
		ldstr "class G2_C772`2<class BaseClass1,class BaseClass1> on type class G3_C1827`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1827`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1827`1<class BaseClass1>::ClassMethod5048<object>()
		calli default string(class G3_C1827`1<class BaseClass1>)
		ldstr "G3_C1827::ClassMethod5048.18229<System.Object>()"
		ldstr "class G3_C1827`1<class BaseClass1> on type class G3_C1827`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1827`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1827`1<class BaseClass1>::Method3<object>()
		calli default string(class G3_C1827`1<class BaseClass1>)
		ldstr "G3_C1827::Method3.18227<System.Object>()"
		ldstr "class G3_C1827`1<class BaseClass1> on type class G3_C1827`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1827`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1827`1<class BaseClass1>::Method2<object>()
		calli default string(class G3_C1827`1<class BaseClass1>)
		ldstr "G3_C1827::Method2.18225<System.Object>()"
		ldstr "class G3_C1827`1<class BaseClass1> on type class G3_C1827`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1827`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1827`1<class BaseClass1>::Method1()
		calli default string(class G3_C1827`1<class BaseClass1>)
		ldstr "G3_C1827::Method1.18223()"
		ldstr "class G3_C1827`1<class BaseClass1> on type class G3_C1827`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1827`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1827`1<class BaseClass1>::Method0()
		calli default string(class G3_C1827`1<class BaseClass1>)
		ldstr "G3_C1827::Method0.18221()"
		ldstr "class G3_C1827`1<class BaseClass1> on type class G3_C1827`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1827`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1827`1<class BaseClass1>::ClassMethod2928<object>()
		calli default string(class G3_C1827`1<class BaseClass1>)
		ldstr "G3_C1827::ClassMethod2928.MI.18231<System.Object>()"
		ldstr "class G3_C1827`1<class BaseClass1> on type class G3_C1827`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1827`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1827`1<class BaseClass1>::ClassMethod2927<object>()
		calli default string(class G3_C1827`1<class BaseClass1>)
		ldstr "G2_C772::ClassMethod2927.12145<System.Object>()"
		ldstr "class G3_C1827`1<class BaseClass1> on type class G3_C1827`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1827`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1827`1<class BaseClass1>::ClassMethod2926()
		calli default string(class G3_C1827`1<class BaseClass1>)
		ldstr "G3_C1827::ClassMethod2926.MI.18230()"
		ldstr "class G3_C1827`1<class BaseClass1> on type class G3_C1827`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1827`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1827`1<class BaseClass1>::ClassMethod2925()
		calli default string(class G3_C1827`1<class BaseClass1>)
		ldstr "G2_C772::ClassMethod2925.12143()"
		ldstr "class G3_C1827`1<class BaseClass1> on type class G3_C1827`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1827`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1827`1<class BaseClass1>::Method6<object>()
		calli default string(class G3_C1827`1<class BaseClass1>)
		ldstr "G2_C772::Method6.12142<System.Object>()"
		ldstr "class G3_C1827`1<class BaseClass1> on type class G3_C1827`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1827`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1827`1<class BaseClass1>::Method5()
		calli default string(class G3_C1827`1<class BaseClass1>)
		ldstr "G2_C772::Method5.12140()"
		ldstr "class G3_C1827`1<class BaseClass1> on type class G3_C1827`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1827`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1827`1<class BaseClass1>::Method4()
		calli default string(class G3_C1827`1<class BaseClass1>)
		ldstr "G2_C772::Method4.12139()"
		ldstr "class G3_C1827`1<class BaseClass1> on type class G3_C1827`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1827`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1827`1<class BaseClass1>::Method7<object>()
		calli default string(class G3_C1827`1<class BaseClass1>)
		ldstr "G2_C772::Method7.12137<System.Object>()"
		ldstr "class G3_C1827`1<class BaseClass1> on type class G3_C1827`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1827`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1827`1<class BaseClass1>::ClassMethod1351<object>()
		calli default string(class G3_C1827`1<class BaseClass1>)
		ldstr "G2_C772::ClassMethod1351.MI.12147<System.Object>()"
		ldstr "class G3_C1827`1<class BaseClass1> on type class G3_C1827`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1827`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1827`1<class BaseClass1>::ClassMethod1350<object>()
		calli default string(class G3_C1827`1<class BaseClass1>)
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()"
		ldstr "class G3_C1827`1<class BaseClass1> on type class G3_C1827`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method0()
		calli default string(class G3_C1827`1<class BaseClass1>)
		ldstr "G3_C1827::Method0.MI.18222()"
		ldstr "IBase0 on type class G3_C1827`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method1()
		calli default string(class G3_C1827`1<class BaseClass1>)
		ldstr "G3_C1827::Method1.MI.18224()"
		ldstr "IBase0 on type class G3_C1827`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method2<object>()
		calli default string(class G3_C1827`1<class BaseClass1>)
		ldstr "G3_C1827::Method2.MI.18226<System.Object>()"
		ldstr "IBase0 on type class G3_C1827`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method3<object>()
		calli default string(class G3_C1827`1<class BaseClass1>)
		ldstr "G3_C1827::Method3.MI.18228<System.Object>()"
		ldstr "IBase0 on type class G3_C1827`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C772`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C14`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C14`2<class BaseClass1,class BaseClass0>::ClassMethod1351<object>()
		calli default string(class G2_C772`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C772::ClassMethod1351.MI.12147<System.Object>()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass0> on type class G2_C772`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C14`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C14`2<class BaseClass1,class BaseClass0>::ClassMethod1350<object>()
		calli default string(class G2_C772`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass0> on type class G2_C772`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C14`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C14`2<class BaseClass1,class BaseClass0>::Method6<object>()
		calli default string(class G2_C772`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C772::Method6.12142<System.Object>()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass0> on type class G2_C772`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C14`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C14`2<class BaseClass1,class BaseClass0>::Method5()
		calli default string(class G2_C772`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C772::Method5.12140()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass0> on type class G2_C772`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C14`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C14`2<class BaseClass1,class BaseClass0>::Method4()
		calli default string(class G2_C772`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C772::Method4.12139()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass0> on type class G2_C772`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C14`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C14`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G2_C772`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C772::Method7.12137<System.Object>()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass0> on type class G2_C772`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C772`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C772::Method7.MI.12138<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C772`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G2_C772`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C772::Method4.12139()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C772`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G2_C772`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C14::Method5.MI.4881()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C772`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G2_C772`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C772::Method6.12142<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C772`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G2_C772`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C772::Method4.12139()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C772`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G2_C772`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C772::Method5.MI.12141()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C772`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G2_C772`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C772::Method6.12142<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C772`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C772`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C772`2<class BaseClass0,class BaseClass0>::ClassMethod2928<object>()
		calli default string(class G2_C772`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C772::ClassMethod2928.12146<System.Object>()"
		ldstr "class G2_C772`2<class BaseClass0,class BaseClass0> on type class G2_C772`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C772`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C772`2<class BaseClass0,class BaseClass0>::ClassMethod2927<object>()
		calli default string(class G2_C772`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C772::ClassMethod2927.12145<System.Object>()"
		ldstr "class G2_C772`2<class BaseClass0,class BaseClass0> on type class G2_C772`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C772`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C772`2<class BaseClass0,class BaseClass0>::ClassMethod2926()
		calli default string(class G2_C772`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C772::ClassMethod2926.12144()"
		ldstr "class G2_C772`2<class BaseClass0,class BaseClass0> on type class G2_C772`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C772`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C772`2<class BaseClass0,class BaseClass0>::ClassMethod2925()
		calli default string(class G2_C772`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C772::ClassMethod2925.12143()"
		ldstr "class G2_C772`2<class BaseClass0,class BaseClass0> on type class G2_C772`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C772`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C772`2<class BaseClass0,class BaseClass0>::Method6<object>()
		calli default string(class G2_C772`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C772::Method6.12142<System.Object>()"
		ldstr "class G2_C772`2<class BaseClass0,class BaseClass0> on type class G2_C772`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C772`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C772`2<class BaseClass0,class BaseClass0>::Method5()
		calli default string(class G2_C772`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C772::Method5.12140()"
		ldstr "class G2_C772`2<class BaseClass0,class BaseClass0> on type class G2_C772`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C772`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C772`2<class BaseClass0,class BaseClass0>::Method4()
		calli default string(class G2_C772`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C772::Method4.12139()"
		ldstr "class G2_C772`2<class BaseClass0,class BaseClass0> on type class G2_C772`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C772`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C772`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C772`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C772::Method7.12137<System.Object>()"
		ldstr "class G2_C772`2<class BaseClass0,class BaseClass0> on type class G2_C772`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C772`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C772`2<class BaseClass0,class BaseClass0>::ClassMethod1351<object>()
		calli default string(class G2_C772`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C772::ClassMethod1351.MI.12147<System.Object>()"
		ldstr "class G2_C772`2<class BaseClass0,class BaseClass0> on type class G2_C772`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C772`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C772`2<class BaseClass0,class BaseClass0>::ClassMethod1350<object>()
		calli default string(class G2_C772`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()"
		ldstr "class G2_C772`2<class BaseClass0,class BaseClass0> on type class G2_C772`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C772`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C772::Method7.MI.12138<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C772`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C772`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C14`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C14`2<class BaseClass1,class BaseClass1>::ClassMethod1351<object>()
		calli default string(class G2_C772`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C772::ClassMethod1351.MI.12147<System.Object>()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass1> on type class G2_C772`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C14`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C14`2<class BaseClass1,class BaseClass1>::ClassMethod1350<object>()
		calli default string(class G2_C772`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass1> on type class G2_C772`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C14`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C14`2<class BaseClass1,class BaseClass1>::Method6<object>()
		calli default string(class G2_C772`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C772::Method6.12142<System.Object>()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass1> on type class G2_C772`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C14`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C14`2<class BaseClass1,class BaseClass1>::Method5()
		calli default string(class G2_C772`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C772::Method5.12140()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass1> on type class G2_C772`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C14`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C14`2<class BaseClass1,class BaseClass1>::Method4()
		calli default string(class G2_C772`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C772::Method4.12139()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass1> on type class G2_C772`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C14`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C14`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C772`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C772::Method7.12137<System.Object>()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass1> on type class G2_C772`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C772`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C772::Method7.MI.12138<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C772`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G2_C772`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C772::Method4.12139()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C772`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G2_C772`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C772::Method5.MI.12141()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C772`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G2_C772`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C772::Method6.12142<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C772`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C772`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C772::Method7.MI.12138<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C772`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G2_C772`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C772::Method4.12139()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C772`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G2_C772`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C772::Method5.MI.12141()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C772`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G2_C772`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C772::Method6.12142<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C772`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C772`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C772`2<class BaseClass0,class BaseClass1>::ClassMethod2928<object>()
		calli default string(class G2_C772`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C772::ClassMethod2928.12146<System.Object>()"
		ldstr "class G2_C772`2<class BaseClass0,class BaseClass1> on type class G2_C772`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C772`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C772`2<class BaseClass0,class BaseClass1>::ClassMethod2927<object>()
		calli default string(class G2_C772`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C772::ClassMethod2927.12145<System.Object>()"
		ldstr "class G2_C772`2<class BaseClass0,class BaseClass1> on type class G2_C772`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C772`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C772`2<class BaseClass0,class BaseClass1>::ClassMethod2926()
		calli default string(class G2_C772`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C772::ClassMethod2926.12144()"
		ldstr "class G2_C772`2<class BaseClass0,class BaseClass1> on type class G2_C772`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C772`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C772`2<class BaseClass0,class BaseClass1>::ClassMethod2925()
		calli default string(class G2_C772`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C772::ClassMethod2925.12143()"
		ldstr "class G2_C772`2<class BaseClass0,class BaseClass1> on type class G2_C772`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C772`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C772`2<class BaseClass0,class BaseClass1>::Method6<object>()
		calli default string(class G2_C772`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C772::Method6.12142<System.Object>()"
		ldstr "class G2_C772`2<class BaseClass0,class BaseClass1> on type class G2_C772`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C772`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C772`2<class BaseClass0,class BaseClass1>::Method5()
		calli default string(class G2_C772`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C772::Method5.12140()"
		ldstr "class G2_C772`2<class BaseClass0,class BaseClass1> on type class G2_C772`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C772`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C772`2<class BaseClass0,class BaseClass1>::Method4()
		calli default string(class G2_C772`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C772::Method4.12139()"
		ldstr "class G2_C772`2<class BaseClass0,class BaseClass1> on type class G2_C772`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C772`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C772`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C772`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C772::Method7.12137<System.Object>()"
		ldstr "class G2_C772`2<class BaseClass0,class BaseClass1> on type class G2_C772`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C772`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C772`2<class BaseClass0,class BaseClass1>::ClassMethod1351<object>()
		calli default string(class G2_C772`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C772::ClassMethod1351.MI.12147<System.Object>()"
		ldstr "class G2_C772`2<class BaseClass0,class BaseClass1> on type class G2_C772`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C772`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C772`2<class BaseClass0,class BaseClass1>::ClassMethod1350<object>()
		calli default string(class G2_C772`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()"
		ldstr "class G2_C772`2<class BaseClass0,class BaseClass1> on type class G2_C772`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C772`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C14`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C14`2<class BaseClass1,class BaseClass0>::ClassMethod1351<object>()
		calli default string(class G2_C772`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C772::ClassMethod1351.MI.12147<System.Object>()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass0> on type class G2_C772`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C14`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C14`2<class BaseClass1,class BaseClass0>::ClassMethod1350<object>()
		calli default string(class G2_C772`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass0> on type class G2_C772`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C14`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C14`2<class BaseClass1,class BaseClass0>::Method6<object>()
		calli default string(class G2_C772`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C772::Method6.12142<System.Object>()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass0> on type class G2_C772`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C14`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C14`2<class BaseClass1,class BaseClass0>::Method5()
		calli default string(class G2_C772`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C772::Method5.12140()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass0> on type class G2_C772`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C14`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C14`2<class BaseClass1,class BaseClass0>::Method4()
		calli default string(class G2_C772`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C772::Method4.12139()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass0> on type class G2_C772`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C14`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C14`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G2_C772`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C772::Method7.12137<System.Object>()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass0> on type class G2_C772`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C772`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C772::Method7.MI.12138<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C772`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G2_C772`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C772::Method4.12139()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C772`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G2_C772`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C14::Method5.MI.4881()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C772`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G2_C772`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C772::Method6.12142<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C772`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G2_C772`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C772::Method4.12139()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C772`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G2_C772`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C772::Method5.MI.12141()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C772`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G2_C772`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C772::Method6.12142<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C772`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C772`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C772`2<class BaseClass1,class BaseClass0>::ClassMethod2928<object>()
		calli default string(class G2_C772`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C772::ClassMethod2928.12146<System.Object>()"
		ldstr "class G2_C772`2<class BaseClass1,class BaseClass0> on type class G2_C772`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C772`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C772`2<class BaseClass1,class BaseClass0>::ClassMethod2927<object>()
		calli default string(class G2_C772`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C772::ClassMethod2927.12145<System.Object>()"
		ldstr "class G2_C772`2<class BaseClass1,class BaseClass0> on type class G2_C772`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C772`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C772`2<class BaseClass1,class BaseClass0>::ClassMethod2926()
		calli default string(class G2_C772`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C772::ClassMethod2926.12144()"
		ldstr "class G2_C772`2<class BaseClass1,class BaseClass0> on type class G2_C772`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C772`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C772`2<class BaseClass1,class BaseClass0>::ClassMethod2925()
		calli default string(class G2_C772`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C772::ClassMethod2925.12143()"
		ldstr "class G2_C772`2<class BaseClass1,class BaseClass0> on type class G2_C772`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C772`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C772`2<class BaseClass1,class BaseClass0>::Method6<object>()
		calli default string(class G2_C772`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C772::Method6.12142<System.Object>()"
		ldstr "class G2_C772`2<class BaseClass1,class BaseClass0> on type class G2_C772`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C772`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C772`2<class BaseClass1,class BaseClass0>::Method5()
		calli default string(class G2_C772`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C772::Method5.12140()"
		ldstr "class G2_C772`2<class BaseClass1,class BaseClass0> on type class G2_C772`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C772`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C772`2<class BaseClass1,class BaseClass0>::Method4()
		calli default string(class G2_C772`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C772::Method4.12139()"
		ldstr "class G2_C772`2<class BaseClass1,class BaseClass0> on type class G2_C772`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C772`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C772`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G2_C772`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C772::Method7.12137<System.Object>()"
		ldstr "class G2_C772`2<class BaseClass1,class BaseClass0> on type class G2_C772`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C772`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C772`2<class BaseClass1,class BaseClass0>::ClassMethod1351<object>()
		calli default string(class G2_C772`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C772::ClassMethod1351.MI.12147<System.Object>()"
		ldstr "class G2_C772`2<class BaseClass1,class BaseClass0> on type class G2_C772`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C772`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C772`2<class BaseClass1,class BaseClass0>::ClassMethod1350<object>()
		calli default string(class G2_C772`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()"
		ldstr "class G2_C772`2<class BaseClass1,class BaseClass0> on type class G2_C772`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C772`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C772::Method7.MI.12138<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C772`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C772`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C14`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C14`2<class BaseClass1,class BaseClass1>::ClassMethod1351<object>()
		calli default string(class G2_C772`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C772::ClassMethod1351.MI.12147<System.Object>()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass1> on type class G2_C772`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C14`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C14`2<class BaseClass1,class BaseClass1>::ClassMethod1350<object>()
		calli default string(class G2_C772`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass1> on type class G2_C772`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C14`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C14`2<class BaseClass1,class BaseClass1>::Method6<object>()
		calli default string(class G2_C772`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C772::Method6.12142<System.Object>()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass1> on type class G2_C772`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C14`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C14`2<class BaseClass1,class BaseClass1>::Method5()
		calli default string(class G2_C772`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C772::Method5.12140()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass1> on type class G2_C772`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C14`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C14`2<class BaseClass1,class BaseClass1>::Method4()
		calli default string(class G2_C772`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C772::Method4.12139()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass1> on type class G2_C772`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C14`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C14`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C772`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C772::Method7.12137<System.Object>()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass1> on type class G2_C772`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C772`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C772::Method7.MI.12138<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C772`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G2_C772`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C772::Method4.12139()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C772`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G2_C772`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C772::Method5.MI.12141()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C772`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G2_C772`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C772::Method6.12142<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C772`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C772`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C772::Method7.MI.12138<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C772`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G2_C772`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C772::Method4.12139()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C772`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G2_C772`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C772::Method5.MI.12141()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C772`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G2_C772`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C772::Method6.12142<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C772`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C772`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C772`2<class BaseClass1,class BaseClass1>::ClassMethod2928<object>()
		calli default string(class G2_C772`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C772::ClassMethod2928.12146<System.Object>()"
		ldstr "class G2_C772`2<class BaseClass1,class BaseClass1> on type class G2_C772`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C772`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C772`2<class BaseClass1,class BaseClass1>::ClassMethod2927<object>()
		calli default string(class G2_C772`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C772::ClassMethod2927.12145<System.Object>()"
		ldstr "class G2_C772`2<class BaseClass1,class BaseClass1> on type class G2_C772`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C772`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C772`2<class BaseClass1,class BaseClass1>::ClassMethod2926()
		calli default string(class G2_C772`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C772::ClassMethod2926.12144()"
		ldstr "class G2_C772`2<class BaseClass1,class BaseClass1> on type class G2_C772`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C772`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C772`2<class BaseClass1,class BaseClass1>::ClassMethod2925()
		calli default string(class G2_C772`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C772::ClassMethod2925.12143()"
		ldstr "class G2_C772`2<class BaseClass1,class BaseClass1> on type class G2_C772`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C772`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C772`2<class BaseClass1,class BaseClass1>::Method6<object>()
		calli default string(class G2_C772`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C772::Method6.12142<System.Object>()"
		ldstr "class G2_C772`2<class BaseClass1,class BaseClass1> on type class G2_C772`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C772`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C772`2<class BaseClass1,class BaseClass1>::Method5()
		calli default string(class G2_C772`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C772::Method5.12140()"
		ldstr "class G2_C772`2<class BaseClass1,class BaseClass1> on type class G2_C772`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C772`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C772`2<class BaseClass1,class BaseClass1>::Method4()
		calli default string(class G2_C772`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C772::Method4.12139()"
		ldstr "class G2_C772`2<class BaseClass1,class BaseClass1> on type class G2_C772`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C772`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C772`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C772`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C772::Method7.12137<System.Object>()"
		ldstr "class G2_C772`2<class BaseClass1,class BaseClass1> on type class G2_C772`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C772`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C772`2<class BaseClass1,class BaseClass1>::ClassMethod1351<object>()
		calli default string(class G2_C772`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C772::ClassMethod1351.MI.12147<System.Object>()"
		ldstr "class G2_C772`2<class BaseClass1,class BaseClass1> on type class G2_C772`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C772`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C772`2<class BaseClass1,class BaseClass1>::ClassMethod1350<object>()
		calli default string(class G2_C772`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()"
		ldstr "class G2_C772`2<class BaseClass1,class BaseClass1> on type class G2_C772`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G1_C14`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C14`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C14`2<class BaseClass0,class BaseClass0>::ClassMethod1351<object>()
		calli default string(class G1_C14`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C14::ClassMethod1351.4884<System.Object>()"
		ldstr "class G1_C14`2<class BaseClass0,class BaseClass0> on type class G1_C14`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C14`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C14`2<class BaseClass0,class BaseClass0>::ClassMethod1350<object>()
		calli default string(class G1_C14`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()"
		ldstr "class G1_C14`2<class BaseClass0,class BaseClass0> on type class G1_C14`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C14`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C14`2<class BaseClass0,class BaseClass0>::Method6<object>()
		calli default string(class G1_C14`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C14::Method6.4882<System.Object>()"
		ldstr "class G1_C14`2<class BaseClass0,class BaseClass0> on type class G1_C14`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C14`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C14`2<class BaseClass0,class BaseClass0>::Method5()
		calli default string(class G1_C14`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C14::Method5.4880()"
		ldstr "class G1_C14`2<class BaseClass0,class BaseClass0> on type class G1_C14`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C14`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C14`2<class BaseClass0,class BaseClass0>::Method4()
		calli default string(class G1_C14`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C14::Method4.4879()"
		ldstr "class G1_C14`2<class BaseClass0,class BaseClass0> on type class G1_C14`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C14`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C14`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G1_C14`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C14::Method7.4878<System.Object>()"
		ldstr "class G1_C14`2<class BaseClass0,class BaseClass0> on type class G1_C14`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G1_C14`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C14::Method7.4878<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G1_C14`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G1_C14`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C14::Method4.4879()"
		ldstr "class IBase1`1<class BaseClass1> on type class G1_C14`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G1_C14`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C14::Method5.MI.4881()"
		ldstr "class IBase1`1<class BaseClass1> on type class G1_C14`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G1_C14`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C14::Method6.4882<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G1_C14`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G1_C14`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C14::Method4.4879()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C14`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G1_C14`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C14::Method5.MI.4881()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C14`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G1_C14`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C14::Method6.4882<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C14`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G1_C14`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C14`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C14`2<class BaseClass0,class BaseClass1>::ClassMethod1351<object>()
		calli default string(class G1_C14`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C14::ClassMethod1351.4884<System.Object>()"
		ldstr "class G1_C14`2<class BaseClass0,class BaseClass1> on type class G1_C14`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C14`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C14`2<class BaseClass0,class BaseClass1>::ClassMethod1350<object>()
		calli default string(class G1_C14`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()"
		ldstr "class G1_C14`2<class BaseClass0,class BaseClass1> on type class G1_C14`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C14`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C14`2<class BaseClass0,class BaseClass1>::Method6<object>()
		calli default string(class G1_C14`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C14::Method6.4882<System.Object>()"
		ldstr "class G1_C14`2<class BaseClass0,class BaseClass1> on type class G1_C14`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C14`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C14`2<class BaseClass0,class BaseClass1>::Method5()
		calli default string(class G1_C14`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C14::Method5.4880()"
		ldstr "class G1_C14`2<class BaseClass0,class BaseClass1> on type class G1_C14`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C14`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C14`2<class BaseClass0,class BaseClass1>::Method4()
		calli default string(class G1_C14`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C14::Method4.4879()"
		ldstr "class G1_C14`2<class BaseClass0,class BaseClass1> on type class G1_C14`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C14`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C14`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G1_C14`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C14::Method7.4878<System.Object>()"
		ldstr "class G1_C14`2<class BaseClass0,class BaseClass1> on type class G1_C14`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G1_C14`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C14::Method7.4878<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G1_C14`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G1_C14`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C14::Method4.4879()"
		ldstr "class IBase1`1<class BaseClass1> on type class G1_C14`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G1_C14`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C14::Method5.MI.4881()"
		ldstr "class IBase1`1<class BaseClass1> on type class G1_C14`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G1_C14`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C14::Method6.4882<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G1_C14`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G1_C14`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C14::Method7.4878<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G1_C14`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G1_C14`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C14::Method4.4879()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C14`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G1_C14`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C14::Method5.MI.4881()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C14`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G1_C14`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C14::Method6.4882<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C14`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G1_C14`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C14`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C14`2<class BaseClass1,class BaseClass0>::ClassMethod1351<object>()
		calli default string(class G1_C14`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C14::ClassMethod1351.4884<System.Object>()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass0> on type class G1_C14`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C14`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C14`2<class BaseClass1,class BaseClass0>::ClassMethod1350<object>()
		calli default string(class G1_C14`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass0> on type class G1_C14`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C14`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C14`2<class BaseClass1,class BaseClass0>::Method6<object>()
		calli default string(class G1_C14`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C14::Method6.4882<System.Object>()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass0> on type class G1_C14`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C14`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C14`2<class BaseClass1,class BaseClass0>::Method5()
		calli default string(class G1_C14`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C14::Method5.4880()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass0> on type class G1_C14`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C14`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C14`2<class BaseClass1,class BaseClass0>::Method4()
		calli default string(class G1_C14`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C14::Method4.4879()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass0> on type class G1_C14`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C14`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C14`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G1_C14`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C14::Method7.4878<System.Object>()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass0> on type class G1_C14`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G1_C14`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C14::Method7.4878<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G1_C14`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G1_C14`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C14::Method4.4879()"
		ldstr "class IBase1`1<class BaseClass1> on type class G1_C14`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G1_C14`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C14::Method5.MI.4881()"
		ldstr "class IBase1`1<class BaseClass1> on type class G1_C14`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G1_C14`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C14::Method6.4882<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G1_C14`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G1_C14`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C14::Method4.4879()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C14`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G1_C14`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C14::Method5.MI.4881()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C14`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G1_C14`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C14::Method6.4882<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C14`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G1_C14`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C14`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C14`2<class BaseClass1,class BaseClass1>::ClassMethod1351<object>()
		calli default string(class G1_C14`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C14::ClassMethod1351.4884<System.Object>()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass1> on type class G1_C14`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C14`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C14`2<class BaseClass1,class BaseClass1>::ClassMethod1350<object>()
		calli default string(class G1_C14`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass1> on type class G1_C14`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C14`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C14`2<class BaseClass1,class BaseClass1>::Method6<object>()
		calli default string(class G1_C14`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C14::Method6.4882<System.Object>()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass1> on type class G1_C14`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C14`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C14`2<class BaseClass1,class BaseClass1>::Method5()
		calli default string(class G1_C14`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C14::Method5.4880()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass1> on type class G1_C14`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C14`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C14`2<class BaseClass1,class BaseClass1>::Method4()
		calli default string(class G1_C14`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C14::Method4.4879()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass1> on type class G1_C14`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C14`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C14`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G1_C14`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C14::Method7.4878<System.Object>()"
		ldstr "class G1_C14`2<class BaseClass1,class BaseClass1> on type class G1_C14`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G1_C14`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C14::Method7.4878<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G1_C14`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G1_C14`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C14::Method4.4879()"
		ldstr "class IBase1`1<class BaseClass1> on type class G1_C14`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G1_C14`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C14::Method5.MI.4881()"
		ldstr "class IBase1`1<class BaseClass1> on type class G1_C14`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G1_C14`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C14::Method6.4882<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G1_C14`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G1_C14`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C14::Method7.4878<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G1_C14`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G1_C14`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C14::Method4.4879()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C14`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G1_C14`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C14::Method5.MI.4881()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C14`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G1_C14`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C14::Method6.4882<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C14`2<class BaseClass1,class BaseClass1>"
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
