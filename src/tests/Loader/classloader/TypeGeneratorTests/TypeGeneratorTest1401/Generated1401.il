// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) .ver 4:0:0:0 }
.assembly extern TestFramework { .publickeytoken = ( B0 3F 5F 7F 11 D5 0A 3A )  }

//TYPES IN FORWARDER ASSEMBLIES:

//TEST ASSEMBLY:
.assembly Generated1401 { .hash algorithm 0x00008004 }
.module Generated1401.exe

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
.class public G3_C1873`1<T0> 
		extends class G2_C809`2<class BaseClass1,class BaseClass0>
		implements IBase0 
{
	.method public hidebysig newslot virtual instance string Method0() cil managed noinlining { 
		ldstr "G3_C1873::Method0.18462()"
		ret
	}
	.method public hidebysig virtual instance string Method1() cil managed noinlining { 
		ldstr "G3_C1873::Method1.18463()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method2<M0>() cil managed noinlining { 
		ldstr "G3_C1873::Method2.18464<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string Method3<M0>() cil managed noinlining { 
		ldstr "G3_C1873::Method3.18465<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod5137() cil managed noinlining { 
		ldstr "G3_C1873::ClassMethod5137.18466()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod5138() cil managed noinlining { 
		ldstr "G3_C1873::ClassMethod5138.18467()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod5139<M0>() cil managed noinlining { 
		ldstr "G3_C1873::ClassMethod5139.18468<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod5140<M0>() cil managed noinlining { 
		ldstr "G3_C1873::ClassMethod5140.18469<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class G2_C809`2<class BaseClass1,class BaseClass0>::.ctor()
		ret
	}
}
.class public G2_C809`2<T0, T1> 
		extends class G1_C15`2<class BaseClass0,class BaseClass1>
		implements class IBase2`2<class BaseClass0,class BaseClass1> 
{
	.method public hidebysig virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G2_C809::Method7.12470<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod3000<M0>() cil managed noinlining { 
		ldstr "G2_C809::ClassMethod3000.12471<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class G1_C15`2<class BaseClass0,class BaseClass1>::.ctor()
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
.class public abstract G1_C15`2<T0, T1> 
		implements class IBase2`2<!T1,!T1>, class IBase1`1<class BaseClass0> 
{
	.method public hidebysig newslot virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G1_C15::Method7.4885<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase2<T1,T1>.Method7'<M0>() cil managed noinlining { 
		.override method instance string class IBase2`2<!T1,!T1>::Method7<[1]>()
		ldstr "G1_C15::Method7.MI.4886<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig virtual instance string Method4() cil managed noinlining { 
		ldstr "G1_C15::Method4.4887()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class BaseClass0>.Method4'() cil managed noinlining { 
		.override method instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G1_C15::Method4.MI.4888()"
		ret
	}
	.method public hidebysig virtual instance string Method5() cil managed noinlining { 
		ldstr "G1_C15::Method5.4889()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class BaseClass0>.Method5'() cil managed noinlining { 
		.override method instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G1_C15::Method5.MI.4890()"
		ret
	}
	.method public hidebysig virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G1_C15::Method6.4891<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class BaseClass0>.Method6'<M0>() cil managed noinlining { 
		.override method instance string class IBase1`1<class BaseClass0>::Method6<[1]>()
		ldstr "G1_C15::Method6.MI.4892<"
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
	.method static void M.G3_C1873.T<T0,(class G3_C1873`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 18
		.locals init (string[] actualResults)
		ldc.i4.s    13
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1873.T<T0,(class G3_C1873`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 13
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1873`1<!!T0>::ClassMethod3000<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1873`1<!!T0>::ClassMethod5137()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1873`1<!!T0>::ClassMethod5138()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1873`1<!!T0>::ClassMethod5139<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1873`1<!!T0>::ClassMethod5140<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1873`1<!!T0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1873`1<!!T0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1873`1<!!T0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1873`1<!!T0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1873`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1873`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1873`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1873`1<!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1873.A<(class G3_C1873`1<class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 18
		.locals init (string[] actualResults)
		ldc.i4.s    13
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1873.A<(class G3_C1873`1<class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 13
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1873`1<class BaseClass0>::ClassMethod3000<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1873`1<class BaseClass0>::ClassMethod5137()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1873`1<class BaseClass0>::ClassMethod5138()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1873`1<class BaseClass0>::ClassMethod5139<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1873`1<class BaseClass0>::ClassMethod5140<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1873`1<class BaseClass0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1873`1<class BaseClass0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1873`1<class BaseClass0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1873`1<class BaseClass0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1873`1<class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1873`1<class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1873`1<class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1873`1<class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1873.B<(class G3_C1873`1<class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 18
		.locals init (string[] actualResults)
		ldc.i4.s    13
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1873.B<(class G3_C1873`1<class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 13
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1873`1<class BaseClass1>::ClassMethod3000<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1873`1<class BaseClass1>::ClassMethod5137()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1873`1<class BaseClass1>::ClassMethod5138()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1873`1<class BaseClass1>::ClassMethod5139<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1873`1<class BaseClass1>::ClassMethod5140<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1873`1<class BaseClass1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1873`1<class BaseClass1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1873`1<class BaseClass1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1873`1<class BaseClass1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1873`1<class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1873`1<class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1873`1<class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1873`1<class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C809.T.T<T0,T1,(class G2_C809`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 10
		.locals init (string[] actualResults)
		ldc.i4.s    5
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C809.T.T<T0,T1,(class G2_C809`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 5
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C809`2<!!T0,!!T1>::ClassMethod3000<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C809`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C809`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C809`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C809`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C809.A.T<T1,(class G2_C809`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 10
		.locals init (string[] actualResults)
		ldc.i4.s    5
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C809.A.T<T1,(class G2_C809`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 5
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C809`2<class BaseClass0,!!T1>::ClassMethod3000<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C809`2<class BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C809`2<class BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C809`2<class BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C809`2<class BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C809.A.A<(class G2_C809`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 10
		.locals init (string[] actualResults)
		ldc.i4.s    5
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C809.A.A<(class G2_C809`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 5
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C809`2<class BaseClass0,class BaseClass0>::ClassMethod3000<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C809`2<class BaseClass0,class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C809`2<class BaseClass0,class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C809`2<class BaseClass0,class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C809`2<class BaseClass0,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C809.A.B<(class G2_C809`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 10
		.locals init (string[] actualResults)
		ldc.i4.s    5
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C809.A.B<(class G2_C809`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 5
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C809`2<class BaseClass0,class BaseClass1>::ClassMethod3000<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C809`2<class BaseClass0,class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C809`2<class BaseClass0,class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C809`2<class BaseClass0,class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C809`2<class BaseClass0,class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C809.B.T<T1,(class G2_C809`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 10
		.locals init (string[] actualResults)
		ldc.i4.s    5
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C809.B.T<T1,(class G2_C809`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 5
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C809`2<class BaseClass1,!!T1>::ClassMethod3000<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C809`2<class BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C809`2<class BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C809`2<class BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C809`2<class BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C809.B.A<(class G2_C809`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 10
		.locals init (string[] actualResults)
		ldc.i4.s    5
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C809.B.A<(class G2_C809`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 5
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C809`2<class BaseClass1,class BaseClass0>::ClassMethod3000<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C809`2<class BaseClass1,class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C809`2<class BaseClass1,class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C809`2<class BaseClass1,class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C809`2<class BaseClass1,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C809.B.B<(class G2_C809`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 10
		.locals init (string[] actualResults)
		ldc.i4.s    5
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C809.B.B<(class G2_C809`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 5
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C809`2<class BaseClass1,class BaseClass1>::ClassMethod3000<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C809`2<class BaseClass1,class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C809`2<class BaseClass1,class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C809`2<class BaseClass1,class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C809`2<class BaseClass1,class BaseClass1>::Method7<object>()
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
	.method static void M.G1_C15.T.T<T0,T1,(class G1_C15`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 9
		.locals init (string[] actualResults)
		ldc.i4.s    4
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C15.T.T<T0,T1,(class G1_C15`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 4
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C15`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C15`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C15`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C15`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C15.A.T<T1,(class G1_C15`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 9
		.locals init (string[] actualResults)
		ldc.i4.s    4
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C15.A.T<T1,(class G1_C15`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 4
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C15`2<class BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C15`2<class BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C15`2<class BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C15`2<class BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C15.A.A<(class G1_C15`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 9
		.locals init (string[] actualResults)
		ldc.i4.s    4
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C15.A.A<(class G1_C15`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 4
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C15`2<class BaseClass0,class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C15`2<class BaseClass0,class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C15`2<class BaseClass0,class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C15`2<class BaseClass0,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C15.A.B<(class G1_C15`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 9
		.locals init (string[] actualResults)
		ldc.i4.s    4
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C15.A.B<(class G1_C15`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 4
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C15`2<class BaseClass0,class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C15`2<class BaseClass0,class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C15`2<class BaseClass0,class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C15`2<class BaseClass0,class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C15.B.T<T1,(class G1_C15`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 9
		.locals init (string[] actualResults)
		ldc.i4.s    4
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C15.B.T<T1,(class G1_C15`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 4
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C15`2<class BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C15`2<class BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C15`2<class BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C15`2<class BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C15.B.A<(class G1_C15`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 9
		.locals init (string[] actualResults)
		ldc.i4.s    4
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C15.B.A<(class G1_C15`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 4
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C15`2<class BaseClass1,class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C15`2<class BaseClass1,class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C15`2<class BaseClass1,class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C15`2<class BaseClass1,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C15.B.B<(class G1_C15`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 9
		.locals init (string[] actualResults)
		ldc.i4.s    4
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C15.B.B<(class G1_C15`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 4
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method7<object>()
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
		newobj instance void class G3_C1873`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C15`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C15`2<class BaseClass0,class BaseClass1>::Method6<object>()
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G1_C15`2<class BaseClass0,class BaseClass1> on type class G3_C1873`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C15`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C15`2<class BaseClass0,class BaseClass1>::Method5()
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G1_C15`2<class BaseClass0,class BaseClass1> on type class G3_C1873`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C15`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C15`2<class BaseClass0,class BaseClass1>::Method4()
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G1_C15`2<class BaseClass0,class BaseClass1> on type class G3_C1873`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C15`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C15`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C809::Method7.12470<System.Object>()"
		ldstr "class G1_C15`2<class BaseClass0,class BaseClass1> on type class G3_C1873`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C15::Method7.MI.4886<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1873`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G1_C15::Method4.MI.4888()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1873`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G1_C15::Method5.MI.4890()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1873`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G1_C15::Method6.MI.4892<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1873`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C809::Method7.12470<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1873`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C809`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C809`2<class BaseClass1,class BaseClass0>::ClassMethod3000<object>()
		ldstr "G2_C809::ClassMethod3000.12471<System.Object>()"
		ldstr "class G2_C809`2<class BaseClass1,class BaseClass0> on type class G3_C1873`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C809`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C809`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G2_C809::Method7.12470<System.Object>()"
		ldstr "class G2_C809`2<class BaseClass1,class BaseClass0> on type class G3_C1873`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C809`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C809`2<class BaseClass1,class BaseClass0>::Method6<object>()
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G2_C809`2<class BaseClass1,class BaseClass0> on type class G3_C1873`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C809`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C809`2<class BaseClass1,class BaseClass0>::Method5()
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G2_C809`2<class BaseClass1,class BaseClass0> on type class G3_C1873`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C809`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C809`2<class BaseClass1,class BaseClass0>::Method4()
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G2_C809`2<class BaseClass1,class BaseClass0> on type class G3_C1873`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G3_C1873`1<class BaseClass0>
		callvirt instance string class G3_C1873`1<class BaseClass0>::ClassMethod5140<object>()
		ldstr "G3_C1873::ClassMethod5140.18469<System.Object>()"
		ldstr "class G3_C1873`1<class BaseClass0> on type class G3_C1873`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1873`1<class BaseClass0>
		callvirt instance string class G3_C1873`1<class BaseClass0>::ClassMethod5139<object>()
		ldstr "G3_C1873::ClassMethod5139.18468<System.Object>()"
		ldstr "class G3_C1873`1<class BaseClass0> on type class G3_C1873`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1873`1<class BaseClass0>
		callvirt instance string class G3_C1873`1<class BaseClass0>::ClassMethod5138()
		ldstr "G3_C1873::ClassMethod5138.18467()"
		ldstr "class G3_C1873`1<class BaseClass0> on type class G3_C1873`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1873`1<class BaseClass0>
		callvirt instance string class G3_C1873`1<class BaseClass0>::ClassMethod5137()
		ldstr "G3_C1873::ClassMethod5137.18466()"
		ldstr "class G3_C1873`1<class BaseClass0> on type class G3_C1873`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1873`1<class BaseClass0>
		callvirt instance string class G3_C1873`1<class BaseClass0>::Method3<object>()
		ldstr "G3_C1873::Method3.18465<System.Object>()"
		ldstr "class G3_C1873`1<class BaseClass0> on type class G3_C1873`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1873`1<class BaseClass0>
		callvirt instance string class G3_C1873`1<class BaseClass0>::Method2<object>()
		ldstr "G3_C1873::Method2.18464<System.Object>()"
		ldstr "class G3_C1873`1<class BaseClass0> on type class G3_C1873`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1873`1<class BaseClass0>
		callvirt instance string class G3_C1873`1<class BaseClass0>::Method1()
		ldstr "G3_C1873::Method1.18463()"
		ldstr "class G3_C1873`1<class BaseClass0> on type class G3_C1873`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1873`1<class BaseClass0>
		callvirt instance string class G3_C1873`1<class BaseClass0>::Method0()
		ldstr "G3_C1873::Method0.18462()"
		ldstr "class G3_C1873`1<class BaseClass0> on type class G3_C1873`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1873`1<class BaseClass0>
		callvirt instance string class G3_C1873`1<class BaseClass0>::ClassMethod3000<object>()
		ldstr "G2_C809::ClassMethod3000.12471<System.Object>()"
		ldstr "class G3_C1873`1<class BaseClass0> on type class G3_C1873`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1873`1<class BaseClass0>
		callvirt instance string class G3_C1873`1<class BaseClass0>::Method7<object>()
		ldstr "G2_C809::Method7.12470<System.Object>()"
		ldstr "class G3_C1873`1<class BaseClass0> on type class G3_C1873`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1873`1<class BaseClass0>
		callvirt instance string class G3_C1873`1<class BaseClass0>::Method6<object>()
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G3_C1873`1<class BaseClass0> on type class G3_C1873`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1873`1<class BaseClass0>
		callvirt instance string class G3_C1873`1<class BaseClass0>::Method5()
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G3_C1873`1<class BaseClass0> on type class G3_C1873`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1873`1<class BaseClass0>
		callvirt instance string class G3_C1873`1<class BaseClass0>::Method4()
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G3_C1873`1<class BaseClass0> on type class G3_C1873`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string IBase0::Method0()
		ldstr "G3_C1873::Method0.18462()"
		ldstr "IBase0 on type class G3_C1873`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method1()
		ldstr "G3_C1873::Method1.18463()"
		ldstr "IBase0 on type class G3_C1873`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method2<object>()
		ldstr "G3_C1873::Method2.18464<System.Object>()"
		ldstr "IBase0 on type class G3_C1873`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method3<object>()
		ldstr "G3_C1873::Method3.18465<System.Object>()"
		ldstr "IBase0 on type class G3_C1873`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G3_C1873`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C15`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C15`2<class BaseClass0,class BaseClass1>::Method6<object>()
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G1_C15`2<class BaseClass0,class BaseClass1> on type class G3_C1873`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C15`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C15`2<class BaseClass0,class BaseClass1>::Method5()
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G1_C15`2<class BaseClass0,class BaseClass1> on type class G3_C1873`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C15`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C15`2<class BaseClass0,class BaseClass1>::Method4()
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G1_C15`2<class BaseClass0,class BaseClass1> on type class G3_C1873`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C15`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C15`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C809::Method7.12470<System.Object>()"
		ldstr "class G1_C15`2<class BaseClass0,class BaseClass1> on type class G3_C1873`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C15::Method7.MI.4886<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1873`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G1_C15::Method4.MI.4888()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1873`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G1_C15::Method5.MI.4890()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1873`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G1_C15::Method6.MI.4892<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1873`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C809::Method7.12470<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1873`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C809`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C809`2<class BaseClass1,class BaseClass0>::ClassMethod3000<object>()
		ldstr "G2_C809::ClassMethod3000.12471<System.Object>()"
		ldstr "class G2_C809`2<class BaseClass1,class BaseClass0> on type class G3_C1873`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C809`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C809`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G2_C809::Method7.12470<System.Object>()"
		ldstr "class G2_C809`2<class BaseClass1,class BaseClass0> on type class G3_C1873`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C809`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C809`2<class BaseClass1,class BaseClass0>::Method6<object>()
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G2_C809`2<class BaseClass1,class BaseClass0> on type class G3_C1873`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C809`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C809`2<class BaseClass1,class BaseClass0>::Method5()
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G2_C809`2<class BaseClass1,class BaseClass0> on type class G3_C1873`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C809`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C809`2<class BaseClass1,class BaseClass0>::Method4()
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G2_C809`2<class BaseClass1,class BaseClass0> on type class G3_C1873`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G3_C1873`1<class BaseClass1>
		callvirt instance string class G3_C1873`1<class BaseClass1>::ClassMethod5140<object>()
		ldstr "G3_C1873::ClassMethod5140.18469<System.Object>()"
		ldstr "class G3_C1873`1<class BaseClass1> on type class G3_C1873`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1873`1<class BaseClass1>
		callvirt instance string class G3_C1873`1<class BaseClass1>::ClassMethod5139<object>()
		ldstr "G3_C1873::ClassMethod5139.18468<System.Object>()"
		ldstr "class G3_C1873`1<class BaseClass1> on type class G3_C1873`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1873`1<class BaseClass1>
		callvirt instance string class G3_C1873`1<class BaseClass1>::ClassMethod5138()
		ldstr "G3_C1873::ClassMethod5138.18467()"
		ldstr "class G3_C1873`1<class BaseClass1> on type class G3_C1873`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1873`1<class BaseClass1>
		callvirt instance string class G3_C1873`1<class BaseClass1>::ClassMethod5137()
		ldstr "G3_C1873::ClassMethod5137.18466()"
		ldstr "class G3_C1873`1<class BaseClass1> on type class G3_C1873`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1873`1<class BaseClass1>
		callvirt instance string class G3_C1873`1<class BaseClass1>::Method3<object>()
		ldstr "G3_C1873::Method3.18465<System.Object>()"
		ldstr "class G3_C1873`1<class BaseClass1> on type class G3_C1873`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1873`1<class BaseClass1>
		callvirt instance string class G3_C1873`1<class BaseClass1>::Method2<object>()
		ldstr "G3_C1873::Method2.18464<System.Object>()"
		ldstr "class G3_C1873`1<class BaseClass1> on type class G3_C1873`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1873`1<class BaseClass1>
		callvirt instance string class G3_C1873`1<class BaseClass1>::Method1()
		ldstr "G3_C1873::Method1.18463()"
		ldstr "class G3_C1873`1<class BaseClass1> on type class G3_C1873`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1873`1<class BaseClass1>
		callvirt instance string class G3_C1873`1<class BaseClass1>::Method0()
		ldstr "G3_C1873::Method0.18462()"
		ldstr "class G3_C1873`1<class BaseClass1> on type class G3_C1873`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1873`1<class BaseClass1>
		callvirt instance string class G3_C1873`1<class BaseClass1>::ClassMethod3000<object>()
		ldstr "G2_C809::ClassMethod3000.12471<System.Object>()"
		ldstr "class G3_C1873`1<class BaseClass1> on type class G3_C1873`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1873`1<class BaseClass1>
		callvirt instance string class G3_C1873`1<class BaseClass1>::Method7<object>()
		ldstr "G2_C809::Method7.12470<System.Object>()"
		ldstr "class G3_C1873`1<class BaseClass1> on type class G3_C1873`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1873`1<class BaseClass1>
		callvirt instance string class G3_C1873`1<class BaseClass1>::Method6<object>()
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G3_C1873`1<class BaseClass1> on type class G3_C1873`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1873`1<class BaseClass1>
		callvirt instance string class G3_C1873`1<class BaseClass1>::Method5()
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G3_C1873`1<class BaseClass1> on type class G3_C1873`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1873`1<class BaseClass1>
		callvirt instance string class G3_C1873`1<class BaseClass1>::Method4()
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G3_C1873`1<class BaseClass1> on type class G3_C1873`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string IBase0::Method0()
		ldstr "G3_C1873::Method0.18462()"
		ldstr "IBase0 on type class G3_C1873`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method1()
		ldstr "G3_C1873::Method1.18463()"
		ldstr "IBase0 on type class G3_C1873`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method2<object>()
		ldstr "G3_C1873::Method2.18464<System.Object>()"
		ldstr "IBase0 on type class G3_C1873`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method3<object>()
		ldstr "G3_C1873::Method3.18465<System.Object>()"
		ldstr "IBase0 on type class G3_C1873`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C809`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C15`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C15`2<class BaseClass0,class BaseClass1>::Method6<object>()
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G1_C15`2<class BaseClass0,class BaseClass1> on type class G2_C809`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C15`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C15`2<class BaseClass0,class BaseClass1>::Method5()
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G1_C15`2<class BaseClass0,class BaseClass1> on type class G2_C809`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C15`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C15`2<class BaseClass0,class BaseClass1>::Method4()
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G1_C15`2<class BaseClass0,class BaseClass1> on type class G2_C809`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C15`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C15`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C809::Method7.12470<System.Object>()"
		ldstr "class G1_C15`2<class BaseClass0,class BaseClass1> on type class G2_C809`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C15::Method7.MI.4886<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C809`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G1_C15::Method4.MI.4888()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C809`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G1_C15::Method5.MI.4890()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C809`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G1_C15::Method6.MI.4892<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C809`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C809::Method7.12470<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C809`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C809`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C809`2<class BaseClass0,class BaseClass0>::ClassMethod3000<object>()
		ldstr "G2_C809::ClassMethod3000.12471<System.Object>()"
		ldstr "class G2_C809`2<class BaseClass0,class BaseClass0> on type class G2_C809`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C809`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C809`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C809::Method7.12470<System.Object>()"
		ldstr "class G2_C809`2<class BaseClass0,class BaseClass0> on type class G2_C809`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C809`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C809`2<class BaseClass0,class BaseClass0>::Method6<object>()
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G2_C809`2<class BaseClass0,class BaseClass0> on type class G2_C809`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C809`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C809`2<class BaseClass0,class BaseClass0>::Method5()
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G2_C809`2<class BaseClass0,class BaseClass0> on type class G2_C809`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C809`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C809`2<class BaseClass0,class BaseClass0>::Method4()
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G2_C809`2<class BaseClass0,class BaseClass0> on type class G2_C809`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C809`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C15`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C15`2<class BaseClass0,class BaseClass1>::Method6<object>()
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G1_C15`2<class BaseClass0,class BaseClass1> on type class G2_C809`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C15`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C15`2<class BaseClass0,class BaseClass1>::Method5()
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G1_C15`2<class BaseClass0,class BaseClass1> on type class G2_C809`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C15`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C15`2<class BaseClass0,class BaseClass1>::Method4()
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G1_C15`2<class BaseClass0,class BaseClass1> on type class G2_C809`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C15`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C15`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C809::Method7.12470<System.Object>()"
		ldstr "class G1_C15`2<class BaseClass0,class BaseClass1> on type class G2_C809`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C15::Method7.MI.4886<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C809`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G1_C15::Method4.MI.4888()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C809`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G1_C15::Method5.MI.4890()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C809`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G1_C15::Method6.MI.4892<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C809`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C809::Method7.12470<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C809`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C809`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C809`2<class BaseClass0,class BaseClass1>::ClassMethod3000<object>()
		ldstr "G2_C809::ClassMethod3000.12471<System.Object>()"
		ldstr "class G2_C809`2<class BaseClass0,class BaseClass1> on type class G2_C809`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C809`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C809`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C809::Method7.12470<System.Object>()"
		ldstr "class G2_C809`2<class BaseClass0,class BaseClass1> on type class G2_C809`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C809`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C809`2<class BaseClass0,class BaseClass1>::Method6<object>()
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G2_C809`2<class BaseClass0,class BaseClass1> on type class G2_C809`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C809`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C809`2<class BaseClass0,class BaseClass1>::Method5()
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G2_C809`2<class BaseClass0,class BaseClass1> on type class G2_C809`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C809`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C809`2<class BaseClass0,class BaseClass1>::Method4()
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G2_C809`2<class BaseClass0,class BaseClass1> on type class G2_C809`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C809`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C15`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C15`2<class BaseClass0,class BaseClass1>::Method6<object>()
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G1_C15`2<class BaseClass0,class BaseClass1> on type class G2_C809`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C15`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C15`2<class BaseClass0,class BaseClass1>::Method5()
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G1_C15`2<class BaseClass0,class BaseClass1> on type class G2_C809`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C15`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C15`2<class BaseClass0,class BaseClass1>::Method4()
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G1_C15`2<class BaseClass0,class BaseClass1> on type class G2_C809`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C15`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C15`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C809::Method7.12470<System.Object>()"
		ldstr "class G1_C15`2<class BaseClass0,class BaseClass1> on type class G2_C809`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C15::Method7.MI.4886<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C809`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G1_C15::Method4.MI.4888()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C809`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G1_C15::Method5.MI.4890()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C809`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G1_C15::Method6.MI.4892<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C809`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C809::Method7.12470<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C809`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C809`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C809`2<class BaseClass1,class BaseClass0>::ClassMethod3000<object>()
		ldstr "G2_C809::ClassMethod3000.12471<System.Object>()"
		ldstr "class G2_C809`2<class BaseClass1,class BaseClass0> on type class G2_C809`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C809`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C809`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G2_C809::Method7.12470<System.Object>()"
		ldstr "class G2_C809`2<class BaseClass1,class BaseClass0> on type class G2_C809`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C809`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C809`2<class BaseClass1,class BaseClass0>::Method6<object>()
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G2_C809`2<class BaseClass1,class BaseClass0> on type class G2_C809`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C809`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C809`2<class BaseClass1,class BaseClass0>::Method5()
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G2_C809`2<class BaseClass1,class BaseClass0> on type class G2_C809`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C809`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C809`2<class BaseClass1,class BaseClass0>::Method4()
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G2_C809`2<class BaseClass1,class BaseClass0> on type class G2_C809`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C809`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C15`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C15`2<class BaseClass0,class BaseClass1>::Method6<object>()
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G1_C15`2<class BaseClass0,class BaseClass1> on type class G2_C809`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C15`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C15`2<class BaseClass0,class BaseClass1>::Method5()
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G1_C15`2<class BaseClass0,class BaseClass1> on type class G2_C809`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C15`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C15`2<class BaseClass0,class BaseClass1>::Method4()
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G1_C15`2<class BaseClass0,class BaseClass1> on type class G2_C809`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C15`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C15`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C809::Method7.12470<System.Object>()"
		ldstr "class G1_C15`2<class BaseClass0,class BaseClass1> on type class G2_C809`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C15::Method7.MI.4886<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C809`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G1_C15::Method4.MI.4888()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C809`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G1_C15::Method5.MI.4890()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C809`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G1_C15::Method6.MI.4892<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C809`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C809::Method7.12470<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C809`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C809`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C809`2<class BaseClass1,class BaseClass1>::ClassMethod3000<object>()
		ldstr "G2_C809::ClassMethod3000.12471<System.Object>()"
		ldstr "class G2_C809`2<class BaseClass1,class BaseClass1> on type class G2_C809`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C809`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C809`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C809::Method7.12470<System.Object>()"
		ldstr "class G2_C809`2<class BaseClass1,class BaseClass1> on type class G2_C809`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C809`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C809`2<class BaseClass1,class BaseClass1>::Method6<object>()
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G2_C809`2<class BaseClass1,class BaseClass1> on type class G2_C809`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C809`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C809`2<class BaseClass1,class BaseClass1>::Method5()
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G2_C809`2<class BaseClass1,class BaseClass1> on type class G2_C809`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C809`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C809`2<class BaseClass1,class BaseClass1>::Method4()
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G2_C809`2<class BaseClass1,class BaseClass1> on type class G2_C809`2<class BaseClass1,class BaseClass1>"
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
		newobj instance void class G3_C1873`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C809::Method7.12470<System.Object>()#"
		call void Framework::M.G1_C15.T.T<class BaseClass0,class BaseClass1,class G3_C1873`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C809::Method7.12470<System.Object>()#"
		call void Framework::M.G1_C15.A.T<class BaseClass1,class G3_C1873`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C809::Method7.12470<System.Object>()#"
		call void Framework::M.G1_C15.A.B<class G3_C1873`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G3_C1873`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G3_C1873`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G3_C1873`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C15::Method4.MI.4888()#G1_C15::Method5.MI.4890()#G1_C15::Method6.MI.4892<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G3_C1873`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method4.MI.4888()#G1_C15::Method5.MI.4890()#G1_C15::Method6.MI.4892<System.Object>()#"
		call void Framework::M.IBase1.A<class G3_C1873`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C809::Method7.12470<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G3_C1873`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C809::Method7.12470<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G3_C1873`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C809::Method7.12470<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G3_C1873`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C809::ClassMethod3000.12471<System.Object>()#G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C809::Method7.12470<System.Object>()#"
		call void Framework::M.G2_C809.T.T<class BaseClass1,class BaseClass0,class G3_C1873`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C809::ClassMethod3000.12471<System.Object>()#G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C809::Method7.12470<System.Object>()#"
		call void Framework::M.G2_C809.B.T<class BaseClass0,class G3_C1873`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C809::ClassMethod3000.12471<System.Object>()#G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C809::Method7.12470<System.Object>()#"
		call void Framework::M.G2_C809.B.A<class G3_C1873`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C809::ClassMethod3000.12471<System.Object>()#G3_C1873::ClassMethod5137.18466()#G3_C1873::ClassMethod5138.18467()#G3_C1873::ClassMethod5139.18468<System.Object>()#G3_C1873::ClassMethod5140.18469<System.Object>()#G3_C1873::Method0.18462()#G3_C1873::Method1.18463()#G3_C1873::Method2.18464<System.Object>()#G3_C1873::Method3.18465<System.Object>()#G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C809::Method7.12470<System.Object>()#"
		call void Framework::M.G3_C1873.T<class BaseClass0,class G3_C1873`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C809::ClassMethod3000.12471<System.Object>()#G3_C1873::ClassMethod5137.18466()#G3_C1873::ClassMethod5138.18467()#G3_C1873::ClassMethod5139.18468<System.Object>()#G3_C1873::ClassMethod5140.18469<System.Object>()#G3_C1873::Method0.18462()#G3_C1873::Method1.18463()#G3_C1873::Method2.18464<System.Object>()#G3_C1873::Method3.18465<System.Object>()#G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C809::Method7.12470<System.Object>()#"
		call void Framework::M.G3_C1873.A<class G3_C1873`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1873::Method0.18462()#G3_C1873::Method1.18463()#G3_C1873::Method2.18464<System.Object>()#G3_C1873::Method3.18465<System.Object>()#"
		call void Framework::M.IBase0<class G3_C1873`1<class BaseClass0>>(!!0,string)
		newobj instance void class G3_C1873`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C809::Method7.12470<System.Object>()#"
		call void Framework::M.G1_C15.T.T<class BaseClass0,class BaseClass1,class G3_C1873`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C809::Method7.12470<System.Object>()#"
		call void Framework::M.G1_C15.A.T<class BaseClass1,class G3_C1873`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C809::Method7.12470<System.Object>()#"
		call void Framework::M.G1_C15.A.B<class G3_C1873`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G3_C1873`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G3_C1873`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G3_C1873`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C15::Method4.MI.4888()#G1_C15::Method5.MI.4890()#G1_C15::Method6.MI.4892<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G3_C1873`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method4.MI.4888()#G1_C15::Method5.MI.4890()#G1_C15::Method6.MI.4892<System.Object>()#"
		call void Framework::M.IBase1.A<class G3_C1873`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C809::Method7.12470<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G3_C1873`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C809::Method7.12470<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G3_C1873`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C809::Method7.12470<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G3_C1873`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C809::ClassMethod3000.12471<System.Object>()#G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C809::Method7.12470<System.Object>()#"
		call void Framework::M.G2_C809.T.T<class BaseClass1,class BaseClass0,class G3_C1873`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C809::ClassMethod3000.12471<System.Object>()#G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C809::Method7.12470<System.Object>()#"
		call void Framework::M.G2_C809.B.T<class BaseClass0,class G3_C1873`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C809::ClassMethod3000.12471<System.Object>()#G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C809::Method7.12470<System.Object>()#"
		call void Framework::M.G2_C809.B.A<class G3_C1873`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C809::ClassMethod3000.12471<System.Object>()#G3_C1873::ClassMethod5137.18466()#G3_C1873::ClassMethod5138.18467()#G3_C1873::ClassMethod5139.18468<System.Object>()#G3_C1873::ClassMethod5140.18469<System.Object>()#G3_C1873::Method0.18462()#G3_C1873::Method1.18463()#G3_C1873::Method2.18464<System.Object>()#G3_C1873::Method3.18465<System.Object>()#G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C809::Method7.12470<System.Object>()#"
		call void Framework::M.G3_C1873.T<class BaseClass1,class G3_C1873`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C809::ClassMethod3000.12471<System.Object>()#G3_C1873::ClassMethod5137.18466()#G3_C1873::ClassMethod5138.18467()#G3_C1873::ClassMethod5139.18468<System.Object>()#G3_C1873::ClassMethod5140.18469<System.Object>()#G3_C1873::Method0.18462()#G3_C1873::Method1.18463()#G3_C1873::Method2.18464<System.Object>()#G3_C1873::Method3.18465<System.Object>()#G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C809::Method7.12470<System.Object>()#"
		call void Framework::M.G3_C1873.B<class G3_C1873`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1873::Method0.18462()#G3_C1873::Method1.18463()#G3_C1873::Method2.18464<System.Object>()#G3_C1873::Method3.18465<System.Object>()#"
		call void Framework::M.IBase0<class G3_C1873`1<class BaseClass1>>(!!0,string)
		newobj instance void class G2_C809`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C809::Method7.12470<System.Object>()#"
		call void Framework::M.G1_C15.T.T<class BaseClass0,class BaseClass1,class G2_C809`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C809::Method7.12470<System.Object>()#"
		call void Framework::M.G1_C15.A.T<class BaseClass1,class G2_C809`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C809::Method7.12470<System.Object>()#"
		call void Framework::M.G1_C15.A.B<class G2_C809`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G2_C809`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G2_C809`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G2_C809`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C15::Method4.MI.4888()#G1_C15::Method5.MI.4890()#G1_C15::Method6.MI.4892<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G2_C809`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method4.MI.4888()#G1_C15::Method5.MI.4890()#G1_C15::Method6.MI.4892<System.Object>()#"
		call void Framework::M.IBase1.A<class G2_C809`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C809::Method7.12470<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C809`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C809::Method7.12470<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G2_C809`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C809::Method7.12470<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G2_C809`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C809::ClassMethod3000.12471<System.Object>()#G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C809::Method7.12470<System.Object>()#"
		call void Framework::M.G2_C809.T.T<class BaseClass0,class BaseClass0,class G2_C809`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C809::ClassMethod3000.12471<System.Object>()#G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C809::Method7.12470<System.Object>()#"
		call void Framework::M.G2_C809.A.T<class BaseClass0,class G2_C809`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C809::ClassMethod3000.12471<System.Object>()#G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C809::Method7.12470<System.Object>()#"
		call void Framework::M.G2_C809.A.A<class G2_C809`2<class BaseClass0,class BaseClass0>>(!!0,string)
		newobj instance void class G2_C809`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C809::Method7.12470<System.Object>()#"
		call void Framework::M.G1_C15.T.T<class BaseClass0,class BaseClass1,class G2_C809`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C809::Method7.12470<System.Object>()#"
		call void Framework::M.G1_C15.A.T<class BaseClass1,class G2_C809`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C809::Method7.12470<System.Object>()#"
		call void Framework::M.G1_C15.A.B<class G2_C809`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G2_C809`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G2_C809`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G2_C809`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C15::Method4.MI.4888()#G1_C15::Method5.MI.4890()#G1_C15::Method6.MI.4892<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G2_C809`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method4.MI.4888()#G1_C15::Method5.MI.4890()#G1_C15::Method6.MI.4892<System.Object>()#"
		call void Framework::M.IBase1.A<class G2_C809`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C809::Method7.12470<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C809`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C809::Method7.12470<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G2_C809`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C809::Method7.12470<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G2_C809`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C809::ClassMethod3000.12471<System.Object>()#G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C809::Method7.12470<System.Object>()#"
		call void Framework::M.G2_C809.T.T<class BaseClass0,class BaseClass1,class G2_C809`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C809::ClassMethod3000.12471<System.Object>()#G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C809::Method7.12470<System.Object>()#"
		call void Framework::M.G2_C809.A.T<class BaseClass1,class G2_C809`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C809::ClassMethod3000.12471<System.Object>()#G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C809::Method7.12470<System.Object>()#"
		call void Framework::M.G2_C809.A.B<class G2_C809`2<class BaseClass0,class BaseClass1>>(!!0,string)
		newobj instance void class G2_C809`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C809::Method7.12470<System.Object>()#"
		call void Framework::M.G1_C15.T.T<class BaseClass0,class BaseClass1,class G2_C809`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C809::Method7.12470<System.Object>()#"
		call void Framework::M.G1_C15.A.T<class BaseClass1,class G2_C809`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C809::Method7.12470<System.Object>()#"
		call void Framework::M.G1_C15.A.B<class G2_C809`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G2_C809`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G2_C809`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G2_C809`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C15::Method4.MI.4888()#G1_C15::Method5.MI.4890()#G1_C15::Method6.MI.4892<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G2_C809`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method4.MI.4888()#G1_C15::Method5.MI.4890()#G1_C15::Method6.MI.4892<System.Object>()#"
		call void Framework::M.IBase1.A<class G2_C809`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C809::Method7.12470<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C809`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C809::Method7.12470<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G2_C809`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C809::Method7.12470<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G2_C809`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C809::ClassMethod3000.12471<System.Object>()#G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C809::Method7.12470<System.Object>()#"
		call void Framework::M.G2_C809.T.T<class BaseClass1,class BaseClass0,class G2_C809`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C809::ClassMethod3000.12471<System.Object>()#G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C809::Method7.12470<System.Object>()#"
		call void Framework::M.G2_C809.B.T<class BaseClass0,class G2_C809`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C809::ClassMethod3000.12471<System.Object>()#G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C809::Method7.12470<System.Object>()#"
		call void Framework::M.G2_C809.B.A<class G2_C809`2<class BaseClass1,class BaseClass0>>(!!0,string)
		newobj instance void class G2_C809`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C809::Method7.12470<System.Object>()#"
		call void Framework::M.G1_C15.T.T<class BaseClass0,class BaseClass1,class G2_C809`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C809::Method7.12470<System.Object>()#"
		call void Framework::M.G1_C15.A.T<class BaseClass1,class G2_C809`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C809::Method7.12470<System.Object>()#"
		call void Framework::M.G1_C15.A.B<class G2_C809`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G2_C809`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G2_C809`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G2_C809`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C15::Method4.MI.4888()#G1_C15::Method5.MI.4890()#G1_C15::Method6.MI.4892<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G2_C809`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method4.MI.4888()#G1_C15::Method5.MI.4890()#G1_C15::Method6.MI.4892<System.Object>()#"
		call void Framework::M.IBase1.A<class G2_C809`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C809::Method7.12470<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C809`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C809::Method7.12470<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G2_C809`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C809::Method7.12470<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G2_C809`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C809::ClassMethod3000.12471<System.Object>()#G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C809::Method7.12470<System.Object>()#"
		call void Framework::M.G2_C809.T.T<class BaseClass1,class BaseClass1,class G2_C809`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C809::ClassMethod3000.12471<System.Object>()#G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C809::Method7.12470<System.Object>()#"
		call void Framework::M.G2_C809.B.T<class BaseClass1,class G2_C809`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C809::ClassMethod3000.12471<System.Object>()#G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C809::Method7.12470<System.Object>()#"
		call void Framework::M.G2_C809.B.B<class G2_C809`2<class BaseClass1,class BaseClass1>>(!!0,string)
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
		newobj instance void class G3_C1873`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C15`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C15`2<class BaseClass0,class BaseClass1>::Method6<object>()
		calli default string(class G3_C1873`1<class BaseClass0>)
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G1_C15`2<class BaseClass0,class BaseClass1> on type class G3_C1873`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C15`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C15`2<class BaseClass0,class BaseClass1>::Method5()
		calli default string(class G3_C1873`1<class BaseClass0>)
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G1_C15`2<class BaseClass0,class BaseClass1> on type class G3_C1873`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C15`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C15`2<class BaseClass0,class BaseClass1>::Method4()
		calli default string(class G3_C1873`1<class BaseClass0>)
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G1_C15`2<class BaseClass0,class BaseClass1> on type class G3_C1873`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C15`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C15`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1873`1<class BaseClass0>)
		ldstr "G2_C809::Method7.12470<System.Object>()"
		ldstr "class G1_C15`2<class BaseClass0,class BaseClass1> on type class G3_C1873`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1873`1<class BaseClass0>)
		ldstr "G1_C15::Method7.MI.4886<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1873`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G3_C1873`1<class BaseClass0>)
		ldstr "G1_C15::Method4.MI.4888()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1873`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G3_C1873`1<class BaseClass0>)
		ldstr "G1_C15::Method5.MI.4890()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1873`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1873`1<class BaseClass0>)
		ldstr "G1_C15::Method6.MI.4892<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1873`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1873`1<class BaseClass0>)
		ldstr "G2_C809::Method7.12470<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1873`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C809`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C809`2<class BaseClass1,class BaseClass0>::ClassMethod3000<object>()
		calli default string(class G3_C1873`1<class BaseClass0>)
		ldstr "G2_C809::ClassMethod3000.12471<System.Object>()"
		ldstr "class G2_C809`2<class BaseClass1,class BaseClass0> on type class G3_C1873`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C809`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C809`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1873`1<class BaseClass0>)
		ldstr "G2_C809::Method7.12470<System.Object>()"
		ldstr "class G2_C809`2<class BaseClass1,class BaseClass0> on type class G3_C1873`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C809`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C809`2<class BaseClass1,class BaseClass0>::Method6<object>()
		calli default string(class G3_C1873`1<class BaseClass0>)
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G2_C809`2<class BaseClass1,class BaseClass0> on type class G3_C1873`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C809`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C809`2<class BaseClass1,class BaseClass0>::Method5()
		calli default string(class G3_C1873`1<class BaseClass0>)
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G2_C809`2<class BaseClass1,class BaseClass0> on type class G3_C1873`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C809`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C809`2<class BaseClass1,class BaseClass0>::Method4()
		calli default string(class G3_C1873`1<class BaseClass0>)
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G2_C809`2<class BaseClass1,class BaseClass0> on type class G3_C1873`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1873`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1873`1<class BaseClass0>::ClassMethod5140<object>()
		calli default string(class G3_C1873`1<class BaseClass0>)
		ldstr "G3_C1873::ClassMethod5140.18469<System.Object>()"
		ldstr "class G3_C1873`1<class BaseClass0> on type class G3_C1873`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1873`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1873`1<class BaseClass0>::ClassMethod5139<object>()
		calli default string(class G3_C1873`1<class BaseClass0>)
		ldstr "G3_C1873::ClassMethod5139.18468<System.Object>()"
		ldstr "class G3_C1873`1<class BaseClass0> on type class G3_C1873`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1873`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1873`1<class BaseClass0>::ClassMethod5138()
		calli default string(class G3_C1873`1<class BaseClass0>)
		ldstr "G3_C1873::ClassMethod5138.18467()"
		ldstr "class G3_C1873`1<class BaseClass0> on type class G3_C1873`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1873`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1873`1<class BaseClass0>::ClassMethod5137()
		calli default string(class G3_C1873`1<class BaseClass0>)
		ldstr "G3_C1873::ClassMethod5137.18466()"
		ldstr "class G3_C1873`1<class BaseClass0> on type class G3_C1873`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1873`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1873`1<class BaseClass0>::Method3<object>()
		calli default string(class G3_C1873`1<class BaseClass0>)
		ldstr "G3_C1873::Method3.18465<System.Object>()"
		ldstr "class G3_C1873`1<class BaseClass0> on type class G3_C1873`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1873`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1873`1<class BaseClass0>::Method2<object>()
		calli default string(class G3_C1873`1<class BaseClass0>)
		ldstr "G3_C1873::Method2.18464<System.Object>()"
		ldstr "class G3_C1873`1<class BaseClass0> on type class G3_C1873`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1873`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1873`1<class BaseClass0>::Method1()
		calli default string(class G3_C1873`1<class BaseClass0>)
		ldstr "G3_C1873::Method1.18463()"
		ldstr "class G3_C1873`1<class BaseClass0> on type class G3_C1873`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1873`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1873`1<class BaseClass0>::Method0()
		calli default string(class G3_C1873`1<class BaseClass0>)
		ldstr "G3_C1873::Method0.18462()"
		ldstr "class G3_C1873`1<class BaseClass0> on type class G3_C1873`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1873`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1873`1<class BaseClass0>::ClassMethod3000<object>()
		calli default string(class G3_C1873`1<class BaseClass0>)
		ldstr "G2_C809::ClassMethod3000.12471<System.Object>()"
		ldstr "class G3_C1873`1<class BaseClass0> on type class G3_C1873`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1873`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1873`1<class BaseClass0>::Method7<object>()
		calli default string(class G3_C1873`1<class BaseClass0>)
		ldstr "G2_C809::Method7.12470<System.Object>()"
		ldstr "class G3_C1873`1<class BaseClass0> on type class G3_C1873`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1873`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1873`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1873`1<class BaseClass0>)
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G3_C1873`1<class BaseClass0> on type class G3_C1873`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1873`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1873`1<class BaseClass0>::Method5()
		calli default string(class G3_C1873`1<class BaseClass0>)
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G3_C1873`1<class BaseClass0> on type class G3_C1873`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1873`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1873`1<class BaseClass0>::Method4()
		calli default string(class G3_C1873`1<class BaseClass0>)
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G3_C1873`1<class BaseClass0> on type class G3_C1873`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method0()
		calli default string(class G3_C1873`1<class BaseClass0>)
		ldstr "G3_C1873::Method0.18462()"
		ldstr "IBase0 on type class G3_C1873`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method1()
		calli default string(class G3_C1873`1<class BaseClass0>)
		ldstr "G3_C1873::Method1.18463()"
		ldstr "IBase0 on type class G3_C1873`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method2<object>()
		calli default string(class G3_C1873`1<class BaseClass0>)
		ldstr "G3_C1873::Method2.18464<System.Object>()"
		ldstr "IBase0 on type class G3_C1873`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method3<object>()
		calli default string(class G3_C1873`1<class BaseClass0>)
		ldstr "G3_C1873::Method3.18465<System.Object>()"
		ldstr "IBase0 on type class G3_C1873`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G3_C1873`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C15`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C15`2<class BaseClass0,class BaseClass1>::Method6<object>()
		calli default string(class G3_C1873`1<class BaseClass1>)
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G1_C15`2<class BaseClass0,class BaseClass1> on type class G3_C1873`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C15`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C15`2<class BaseClass0,class BaseClass1>::Method5()
		calli default string(class G3_C1873`1<class BaseClass1>)
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G1_C15`2<class BaseClass0,class BaseClass1> on type class G3_C1873`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C15`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C15`2<class BaseClass0,class BaseClass1>::Method4()
		calli default string(class G3_C1873`1<class BaseClass1>)
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G1_C15`2<class BaseClass0,class BaseClass1> on type class G3_C1873`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C15`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C15`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1873`1<class BaseClass1>)
		ldstr "G2_C809::Method7.12470<System.Object>()"
		ldstr "class G1_C15`2<class BaseClass0,class BaseClass1> on type class G3_C1873`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1873`1<class BaseClass1>)
		ldstr "G1_C15::Method7.MI.4886<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1873`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G3_C1873`1<class BaseClass1>)
		ldstr "G1_C15::Method4.MI.4888()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1873`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G3_C1873`1<class BaseClass1>)
		ldstr "G1_C15::Method5.MI.4890()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1873`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1873`1<class BaseClass1>)
		ldstr "G1_C15::Method6.MI.4892<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1873`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1873`1<class BaseClass1>)
		ldstr "G2_C809::Method7.12470<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1873`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C809`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C809`2<class BaseClass1,class BaseClass0>::ClassMethod3000<object>()
		calli default string(class G3_C1873`1<class BaseClass1>)
		ldstr "G2_C809::ClassMethod3000.12471<System.Object>()"
		ldstr "class G2_C809`2<class BaseClass1,class BaseClass0> on type class G3_C1873`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C809`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C809`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1873`1<class BaseClass1>)
		ldstr "G2_C809::Method7.12470<System.Object>()"
		ldstr "class G2_C809`2<class BaseClass1,class BaseClass0> on type class G3_C1873`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C809`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C809`2<class BaseClass1,class BaseClass0>::Method6<object>()
		calli default string(class G3_C1873`1<class BaseClass1>)
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G2_C809`2<class BaseClass1,class BaseClass0> on type class G3_C1873`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C809`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C809`2<class BaseClass1,class BaseClass0>::Method5()
		calli default string(class G3_C1873`1<class BaseClass1>)
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G2_C809`2<class BaseClass1,class BaseClass0> on type class G3_C1873`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C809`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C809`2<class BaseClass1,class BaseClass0>::Method4()
		calli default string(class G3_C1873`1<class BaseClass1>)
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G2_C809`2<class BaseClass1,class BaseClass0> on type class G3_C1873`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1873`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1873`1<class BaseClass1>::ClassMethod5140<object>()
		calli default string(class G3_C1873`1<class BaseClass1>)
		ldstr "G3_C1873::ClassMethod5140.18469<System.Object>()"
		ldstr "class G3_C1873`1<class BaseClass1> on type class G3_C1873`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1873`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1873`1<class BaseClass1>::ClassMethod5139<object>()
		calli default string(class G3_C1873`1<class BaseClass1>)
		ldstr "G3_C1873::ClassMethod5139.18468<System.Object>()"
		ldstr "class G3_C1873`1<class BaseClass1> on type class G3_C1873`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1873`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1873`1<class BaseClass1>::ClassMethod5138()
		calli default string(class G3_C1873`1<class BaseClass1>)
		ldstr "G3_C1873::ClassMethod5138.18467()"
		ldstr "class G3_C1873`1<class BaseClass1> on type class G3_C1873`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1873`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1873`1<class BaseClass1>::ClassMethod5137()
		calli default string(class G3_C1873`1<class BaseClass1>)
		ldstr "G3_C1873::ClassMethod5137.18466()"
		ldstr "class G3_C1873`1<class BaseClass1> on type class G3_C1873`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1873`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1873`1<class BaseClass1>::Method3<object>()
		calli default string(class G3_C1873`1<class BaseClass1>)
		ldstr "G3_C1873::Method3.18465<System.Object>()"
		ldstr "class G3_C1873`1<class BaseClass1> on type class G3_C1873`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1873`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1873`1<class BaseClass1>::Method2<object>()
		calli default string(class G3_C1873`1<class BaseClass1>)
		ldstr "G3_C1873::Method2.18464<System.Object>()"
		ldstr "class G3_C1873`1<class BaseClass1> on type class G3_C1873`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1873`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1873`1<class BaseClass1>::Method1()
		calli default string(class G3_C1873`1<class BaseClass1>)
		ldstr "G3_C1873::Method1.18463()"
		ldstr "class G3_C1873`1<class BaseClass1> on type class G3_C1873`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1873`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1873`1<class BaseClass1>::Method0()
		calli default string(class G3_C1873`1<class BaseClass1>)
		ldstr "G3_C1873::Method0.18462()"
		ldstr "class G3_C1873`1<class BaseClass1> on type class G3_C1873`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1873`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1873`1<class BaseClass1>::ClassMethod3000<object>()
		calli default string(class G3_C1873`1<class BaseClass1>)
		ldstr "G2_C809::ClassMethod3000.12471<System.Object>()"
		ldstr "class G3_C1873`1<class BaseClass1> on type class G3_C1873`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1873`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1873`1<class BaseClass1>::Method7<object>()
		calli default string(class G3_C1873`1<class BaseClass1>)
		ldstr "G2_C809::Method7.12470<System.Object>()"
		ldstr "class G3_C1873`1<class BaseClass1> on type class G3_C1873`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1873`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1873`1<class BaseClass1>::Method6<object>()
		calli default string(class G3_C1873`1<class BaseClass1>)
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G3_C1873`1<class BaseClass1> on type class G3_C1873`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1873`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1873`1<class BaseClass1>::Method5()
		calli default string(class G3_C1873`1<class BaseClass1>)
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G3_C1873`1<class BaseClass1> on type class G3_C1873`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1873`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1873`1<class BaseClass1>::Method4()
		calli default string(class G3_C1873`1<class BaseClass1>)
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G3_C1873`1<class BaseClass1> on type class G3_C1873`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method0()
		calli default string(class G3_C1873`1<class BaseClass1>)
		ldstr "G3_C1873::Method0.18462()"
		ldstr "IBase0 on type class G3_C1873`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method1()
		calli default string(class G3_C1873`1<class BaseClass1>)
		ldstr "G3_C1873::Method1.18463()"
		ldstr "IBase0 on type class G3_C1873`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method2<object>()
		calli default string(class G3_C1873`1<class BaseClass1>)
		ldstr "G3_C1873::Method2.18464<System.Object>()"
		ldstr "IBase0 on type class G3_C1873`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method3<object>()
		calli default string(class G3_C1873`1<class BaseClass1>)
		ldstr "G3_C1873::Method3.18465<System.Object>()"
		ldstr "IBase0 on type class G3_C1873`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C809`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C15`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C15`2<class BaseClass0,class BaseClass1>::Method6<object>()
		calli default string(class G2_C809`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G1_C15`2<class BaseClass0,class BaseClass1> on type class G2_C809`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C15`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C15`2<class BaseClass0,class BaseClass1>::Method5()
		calli default string(class G2_C809`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G1_C15`2<class BaseClass0,class BaseClass1> on type class G2_C809`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C15`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C15`2<class BaseClass0,class BaseClass1>::Method4()
		calli default string(class G2_C809`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G1_C15`2<class BaseClass0,class BaseClass1> on type class G2_C809`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C15`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C15`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C809`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C809::Method7.12470<System.Object>()"
		ldstr "class G1_C15`2<class BaseClass0,class BaseClass1> on type class G2_C809`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C809`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C15::Method7.MI.4886<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C809`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G2_C809`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C15::Method4.MI.4888()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C809`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G2_C809`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C15::Method5.MI.4890()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C809`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G2_C809`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C15::Method6.MI.4892<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C809`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C809`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C809::Method7.12470<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C809`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C809`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C809`2<class BaseClass0,class BaseClass0>::ClassMethod3000<object>()
		calli default string(class G2_C809`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C809::ClassMethod3000.12471<System.Object>()"
		ldstr "class G2_C809`2<class BaseClass0,class BaseClass0> on type class G2_C809`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C809`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C809`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C809`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C809::Method7.12470<System.Object>()"
		ldstr "class G2_C809`2<class BaseClass0,class BaseClass0> on type class G2_C809`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C809`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C809`2<class BaseClass0,class BaseClass0>::Method6<object>()
		calli default string(class G2_C809`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G2_C809`2<class BaseClass0,class BaseClass0> on type class G2_C809`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C809`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C809`2<class BaseClass0,class BaseClass0>::Method5()
		calli default string(class G2_C809`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G2_C809`2<class BaseClass0,class BaseClass0> on type class G2_C809`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C809`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C809`2<class BaseClass0,class BaseClass0>::Method4()
		calli default string(class G2_C809`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G2_C809`2<class BaseClass0,class BaseClass0> on type class G2_C809`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C809`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C15`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C15`2<class BaseClass0,class BaseClass1>::Method6<object>()
		calli default string(class G2_C809`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G1_C15`2<class BaseClass0,class BaseClass1> on type class G2_C809`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C15`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C15`2<class BaseClass0,class BaseClass1>::Method5()
		calli default string(class G2_C809`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G1_C15`2<class BaseClass0,class BaseClass1> on type class G2_C809`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C15`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C15`2<class BaseClass0,class BaseClass1>::Method4()
		calli default string(class G2_C809`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G1_C15`2<class BaseClass0,class BaseClass1> on type class G2_C809`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C15`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C15`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C809`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C809::Method7.12470<System.Object>()"
		ldstr "class G1_C15`2<class BaseClass0,class BaseClass1> on type class G2_C809`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C809`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C15::Method7.MI.4886<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C809`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G2_C809`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C15::Method4.MI.4888()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C809`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G2_C809`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C15::Method5.MI.4890()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C809`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G2_C809`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C15::Method6.MI.4892<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C809`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C809`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C809::Method7.12470<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C809`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C809`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C809`2<class BaseClass0,class BaseClass1>::ClassMethod3000<object>()
		calli default string(class G2_C809`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C809::ClassMethod3000.12471<System.Object>()"
		ldstr "class G2_C809`2<class BaseClass0,class BaseClass1> on type class G2_C809`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C809`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C809`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C809`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C809::Method7.12470<System.Object>()"
		ldstr "class G2_C809`2<class BaseClass0,class BaseClass1> on type class G2_C809`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C809`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C809`2<class BaseClass0,class BaseClass1>::Method6<object>()
		calli default string(class G2_C809`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G2_C809`2<class BaseClass0,class BaseClass1> on type class G2_C809`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C809`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C809`2<class BaseClass0,class BaseClass1>::Method5()
		calli default string(class G2_C809`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G2_C809`2<class BaseClass0,class BaseClass1> on type class G2_C809`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C809`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C809`2<class BaseClass0,class BaseClass1>::Method4()
		calli default string(class G2_C809`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G2_C809`2<class BaseClass0,class BaseClass1> on type class G2_C809`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C809`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C15`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C15`2<class BaseClass0,class BaseClass1>::Method6<object>()
		calli default string(class G2_C809`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G1_C15`2<class BaseClass0,class BaseClass1> on type class G2_C809`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C15`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C15`2<class BaseClass0,class BaseClass1>::Method5()
		calli default string(class G2_C809`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G1_C15`2<class BaseClass0,class BaseClass1> on type class G2_C809`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C15`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C15`2<class BaseClass0,class BaseClass1>::Method4()
		calli default string(class G2_C809`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G1_C15`2<class BaseClass0,class BaseClass1> on type class G2_C809`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C15`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C15`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C809`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C809::Method7.12470<System.Object>()"
		ldstr "class G1_C15`2<class BaseClass0,class BaseClass1> on type class G2_C809`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C809`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C15::Method7.MI.4886<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C809`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G2_C809`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C15::Method4.MI.4888()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C809`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G2_C809`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C15::Method5.MI.4890()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C809`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G2_C809`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C15::Method6.MI.4892<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C809`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C809`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C809::Method7.12470<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C809`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C809`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C809`2<class BaseClass1,class BaseClass0>::ClassMethod3000<object>()
		calli default string(class G2_C809`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C809::ClassMethod3000.12471<System.Object>()"
		ldstr "class G2_C809`2<class BaseClass1,class BaseClass0> on type class G2_C809`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C809`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C809`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G2_C809`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C809::Method7.12470<System.Object>()"
		ldstr "class G2_C809`2<class BaseClass1,class BaseClass0> on type class G2_C809`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C809`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C809`2<class BaseClass1,class BaseClass0>::Method6<object>()
		calli default string(class G2_C809`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G2_C809`2<class BaseClass1,class BaseClass0> on type class G2_C809`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C809`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C809`2<class BaseClass1,class BaseClass0>::Method5()
		calli default string(class G2_C809`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G2_C809`2<class BaseClass1,class BaseClass0> on type class G2_C809`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C809`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C809`2<class BaseClass1,class BaseClass0>::Method4()
		calli default string(class G2_C809`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G2_C809`2<class BaseClass1,class BaseClass0> on type class G2_C809`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C809`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C15`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C15`2<class BaseClass0,class BaseClass1>::Method6<object>()
		calli default string(class G2_C809`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G1_C15`2<class BaseClass0,class BaseClass1> on type class G2_C809`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C15`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C15`2<class BaseClass0,class BaseClass1>::Method5()
		calli default string(class G2_C809`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G1_C15`2<class BaseClass0,class BaseClass1> on type class G2_C809`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C15`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C15`2<class BaseClass0,class BaseClass1>::Method4()
		calli default string(class G2_C809`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G1_C15`2<class BaseClass0,class BaseClass1> on type class G2_C809`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C15`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C15`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C809`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C809::Method7.12470<System.Object>()"
		ldstr "class G1_C15`2<class BaseClass0,class BaseClass1> on type class G2_C809`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C809`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C15::Method7.MI.4886<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C809`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G2_C809`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C15::Method4.MI.4888()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C809`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G2_C809`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C15::Method5.MI.4890()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C809`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G2_C809`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C15::Method6.MI.4892<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C809`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C809`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C809::Method7.12470<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C809`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C809`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C809`2<class BaseClass1,class BaseClass1>::ClassMethod3000<object>()
		calli default string(class G2_C809`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C809::ClassMethod3000.12471<System.Object>()"
		ldstr "class G2_C809`2<class BaseClass1,class BaseClass1> on type class G2_C809`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C809`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C809`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C809`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C809::Method7.12470<System.Object>()"
		ldstr "class G2_C809`2<class BaseClass1,class BaseClass1> on type class G2_C809`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C809`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C809`2<class BaseClass1,class BaseClass1>::Method6<object>()
		calli default string(class G2_C809`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G2_C809`2<class BaseClass1,class BaseClass1> on type class G2_C809`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C809`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C809`2<class BaseClass1,class BaseClass1>::Method5()
		calli default string(class G2_C809`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G2_C809`2<class BaseClass1,class BaseClass1> on type class G2_C809`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C809`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C809`2<class BaseClass1,class BaseClass1>::Method4()
		calli default string(class G2_C809`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G2_C809`2<class BaseClass1,class BaseClass1> on type class G2_C809`2<class BaseClass1,class BaseClass1>"
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
