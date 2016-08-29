// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern mscorlib { .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) .ver 4:0:0:0 }
.assembly extern TestFramework { .publickeytoken = ( B0 3F 5F 7F 11 D5 0A 3A )  }

//TYPES IN FORWARDER ASSEMBLIES:

//TEST ASSEMBLY:
.assembly Generated1454 { .hash algorithm 0x00008004 }
.module Generated1454.exe

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
.class public G3_C1926`1<T0> 
		extends class G2_C840`2<class BaseClass0,class BaseClass1>
		implements class IBase2`2<!T0,!T0> 
{
	.method public hidebysig virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G3_C1926::Method7.18741<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase2<T0,T0>.Method7'<M0>() cil managed noinlining { 
		.override method instance string class IBase2`2<!T0,!T0>::Method7<[1]>()
		ldstr "G3_C1926::Method7.MI.18742<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod5246() cil managed noinlining { 
		ldstr "G3_C1926::ClassMethod5246.18743()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod5247() cil managed noinlining { 
		ldstr "G3_C1926::ClassMethod5247.18744()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod5248<M0>() cil managed noinlining { 
		ldstr "G3_C1926::ClassMethod5248.18745<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C840<class BaseClass0,class BaseClass1>.ClassMethod3057'() cil managed noinlining { 
		.override method instance string class G2_C840`2<class BaseClass0,class BaseClass1>::ClassMethod3057()
		ldstr "G3_C1926::ClassMethod3057.MI.18746()"
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class G2_C840`2<class BaseClass0,class BaseClass1>::.ctor()
		ret
	}
}
.class public G2_C840`2<T0, T1> 
		extends class G1_C15`2<class BaseClass1,!T1>
		implements class IBase1`1<class BaseClass1>, class IBase2`2<class BaseClass1,class BaseClass1> 
{
	.method public hidebysig newslot virtual instance string Method4() cil managed noinlining { 
		ldstr "G2_C840::Method4.12706()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class BaseClass1>.Method4'() cil managed noinlining { 
		.override method instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G2_C840::Method4.MI.12707()"
		ret
	}
	.method public hidebysig virtual instance string Method5() cil managed noinlining { 
		ldstr "G2_C840::Method5.12708()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G2_C840::Method6.12709<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G2_C840::Method7.12710<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod3056() cil managed noinlining { 
		ldstr "G2_C840::ClassMethod3056.12711()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod3057() cil managed noinlining { 
		ldstr "G2_C840::ClassMethod3057.12712()"
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class G1_C15`2<class BaseClass1,!T1>::.ctor()
		ret
	}
}
.class interface public abstract IBase2`2<+T0, -T1> 
{
	.method public hidebysig newslot abstract virtual instance string Method7<M0>() cil managed { }
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
	.method static void M.G3_C1926.T<T0,(class G3_C1926`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1926.T<T0,(class G3_C1926`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1926`1<!!T0>::ClassMethod3056()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1926`1<!!T0>::ClassMethod3057()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1926`1<!!T0>::ClassMethod5246()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1926`1<!!T0>::ClassMethod5247()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1926`1<!!T0>::ClassMethod5248<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1926`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1926`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1926`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1926`1<!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1926.A<(class G3_C1926`1<class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1926.A<(class G3_C1926`1<class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1926`1<class BaseClass0>::ClassMethod3056()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1926`1<class BaseClass0>::ClassMethod3057()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1926`1<class BaseClass0>::ClassMethod5246()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1926`1<class BaseClass0>::ClassMethod5247()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1926`1<class BaseClass0>::ClassMethod5248<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1926`1<class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1926`1<class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1926`1<class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1926`1<class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1926.B<(class G3_C1926`1<class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1926.B<(class G3_C1926`1<class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1926`1<class BaseClass1>::ClassMethod3056()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1926`1<class BaseClass1>::ClassMethod3057()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1926`1<class BaseClass1>::ClassMethod5246()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1926`1<class BaseClass1>::ClassMethod5247()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1926`1<class BaseClass1>::ClassMethod5248<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1926`1<class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1926`1<class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1926`1<class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1926`1<class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C840.T.T<T0,T1,(class G2_C840`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C840.T.T<T0,T1,(class G2_C840`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C840`2<!!T0,!!T1>::ClassMethod3056()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C840`2<!!T0,!!T1>::ClassMethod3057()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C840`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C840`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C840`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C840`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C840.A.T<T1,(class G2_C840`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C840.A.T<T1,(class G2_C840`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C840`2<class BaseClass0,!!T1>::ClassMethod3056()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C840`2<class BaseClass0,!!T1>::ClassMethod3057()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C840`2<class BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C840`2<class BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C840`2<class BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C840`2<class BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C840.A.A<(class G2_C840`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C840.A.A<(class G2_C840`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C840`2<class BaseClass0,class BaseClass0>::ClassMethod3056()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C840`2<class BaseClass0,class BaseClass0>::ClassMethod3057()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C840`2<class BaseClass0,class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C840`2<class BaseClass0,class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C840`2<class BaseClass0,class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C840`2<class BaseClass0,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C840.A.B<(class G2_C840`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C840.A.B<(class G2_C840`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C840`2<class BaseClass0,class BaseClass1>::ClassMethod3056()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C840`2<class BaseClass0,class BaseClass1>::ClassMethod3057()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C840`2<class BaseClass0,class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C840`2<class BaseClass0,class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C840`2<class BaseClass0,class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C840`2<class BaseClass0,class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C840.B.T<T1,(class G2_C840`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C840.B.T<T1,(class G2_C840`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C840`2<class BaseClass1,!!T1>::ClassMethod3056()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C840`2<class BaseClass1,!!T1>::ClassMethod3057()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C840`2<class BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C840`2<class BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C840`2<class BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C840`2<class BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C840.B.A<(class G2_C840`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C840.B.A<(class G2_C840`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C840`2<class BaseClass1,class BaseClass0>::ClassMethod3056()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C840`2<class BaseClass1,class BaseClass0>::ClassMethod3057()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C840`2<class BaseClass1,class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C840`2<class BaseClass1,class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C840`2<class BaseClass1,class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C840`2<class BaseClass1,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C840.B.B<(class G2_C840`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C840.B.B<(class G2_C840`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C840`2<class BaseClass1,class BaseClass1>::ClassMethod3056()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C840`2<class BaseClass1,class BaseClass1>::ClassMethod3057()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C840`2<class BaseClass1,class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C840`2<class BaseClass1,class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C840`2<class BaseClass1,class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C840`2<class BaseClass1,class BaseClass1>::Method7<object>()
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
		newobj instance void class G3_C1926`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method6<object>()
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G3_C1926`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method5()
		ldstr "G2_C840::Method5.12708()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G3_C1926`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method4()
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G3_C1926`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C15::Method7.4885<System.Object>()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G3_C1926`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G3_C1926::Method7.18741<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1926`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C840::Method4.MI.12707()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1926`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C840::Method5.12708()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1926`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C840::Method6.12709<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1926`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G3_C1926::Method7.MI.18742<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1926`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C840`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C840`2<class BaseClass0,class BaseClass1>::ClassMethod3057()
		ldstr "G3_C1926::ClassMethod3057.MI.18746()"
		ldstr "class G2_C840`2<class BaseClass0,class BaseClass1> on type class G3_C1926`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C840`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C840`2<class BaseClass0,class BaseClass1>::ClassMethod3056()
		ldstr "G2_C840::ClassMethod3056.12711()"
		ldstr "class G2_C840`2<class BaseClass0,class BaseClass1> on type class G3_C1926`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C840`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C840`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G3_C1926::Method7.18741<System.Object>()"
		ldstr "class G2_C840`2<class BaseClass0,class BaseClass1> on type class G3_C1926`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C840`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C840`2<class BaseClass0,class BaseClass1>::Method6<object>()
		ldstr "G2_C840::Method6.12709<System.Object>()"
		ldstr "class G2_C840`2<class BaseClass0,class BaseClass1> on type class G3_C1926`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C840`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C840`2<class BaseClass0,class BaseClass1>::Method5()
		ldstr "G2_C840::Method5.12708()"
		ldstr "class G2_C840`2<class BaseClass0,class BaseClass1> on type class G3_C1926`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C840`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C840`2<class BaseClass0,class BaseClass1>::Method4()
		ldstr "G2_C840::Method4.12706()"
		ldstr "class G2_C840`2<class BaseClass0,class BaseClass1> on type class G3_C1926`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G2_C840::Method4.MI.12707()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1926`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G2_C840::Method5.12708()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1926`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C840::Method6.12709<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1926`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G3_C1926`1<class BaseClass0>
		callvirt instance string class G3_C1926`1<class BaseClass0>::ClassMethod5248<object>()
		ldstr "G3_C1926::ClassMethod5248.18745<System.Object>()"
		ldstr "class G3_C1926`1<class BaseClass0> on type class G3_C1926`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1926`1<class BaseClass0>
		callvirt instance string class G3_C1926`1<class BaseClass0>::ClassMethod5247()
		ldstr "G3_C1926::ClassMethod5247.18744()"
		ldstr "class G3_C1926`1<class BaseClass0> on type class G3_C1926`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1926`1<class BaseClass0>
		callvirt instance string class G3_C1926`1<class BaseClass0>::ClassMethod5246()
		ldstr "G3_C1926::ClassMethod5246.18743()"
		ldstr "class G3_C1926`1<class BaseClass0> on type class G3_C1926`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1926`1<class BaseClass0>
		callvirt instance string class G3_C1926`1<class BaseClass0>::Method7<object>()
		ldstr "G3_C1926::Method7.18741<System.Object>()"
		ldstr "class G3_C1926`1<class BaseClass0> on type class G3_C1926`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1926`1<class BaseClass0>
		callvirt instance string class G3_C1926`1<class BaseClass0>::ClassMethod3057()
		ldstr "G3_C1926::ClassMethod3057.MI.18746()"
		ldstr "class G3_C1926`1<class BaseClass0> on type class G3_C1926`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1926`1<class BaseClass0>
		callvirt instance string class G3_C1926`1<class BaseClass0>::ClassMethod3056()
		ldstr "G2_C840::ClassMethod3056.12711()"
		ldstr "class G3_C1926`1<class BaseClass0> on type class G3_C1926`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1926`1<class BaseClass0>
		callvirt instance string class G3_C1926`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C840::Method6.12709<System.Object>()"
		ldstr "class G3_C1926`1<class BaseClass0> on type class G3_C1926`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1926`1<class BaseClass0>
		callvirt instance string class G3_C1926`1<class BaseClass0>::Method5()
		ldstr "G2_C840::Method5.12708()"
		ldstr "class G3_C1926`1<class BaseClass0> on type class G3_C1926`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1926`1<class BaseClass0>
		callvirt instance string class G3_C1926`1<class BaseClass0>::Method4()
		ldstr "G2_C840::Method4.12706()"
		ldstr "class G3_C1926`1<class BaseClass0> on type class G3_C1926`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G3_C1926::Method7.MI.18742<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1926`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G3_C1926`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method6<object>()
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G3_C1926`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method5()
		ldstr "G2_C840::Method5.12708()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G3_C1926`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method4()
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G3_C1926`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C15::Method7.4885<System.Object>()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G3_C1926`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G3_C1926::Method7.MI.18742<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1926`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C840::Method4.MI.12707()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1926`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C840::Method5.12708()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1926`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C840::Method6.12709<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1926`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G3_C1926::Method7.MI.18742<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1926`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C840`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C840`2<class BaseClass0,class BaseClass1>::ClassMethod3057()
		ldstr "G3_C1926::ClassMethod3057.MI.18746()"
		ldstr "class G2_C840`2<class BaseClass0,class BaseClass1> on type class G3_C1926`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C840`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C840`2<class BaseClass0,class BaseClass1>::ClassMethod3056()
		ldstr "G2_C840::ClassMethod3056.12711()"
		ldstr "class G2_C840`2<class BaseClass0,class BaseClass1> on type class G3_C1926`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C840`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C840`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G3_C1926::Method7.18741<System.Object>()"
		ldstr "class G2_C840`2<class BaseClass0,class BaseClass1> on type class G3_C1926`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C840`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C840`2<class BaseClass0,class BaseClass1>::Method6<object>()
		ldstr "G2_C840::Method6.12709<System.Object>()"
		ldstr "class G2_C840`2<class BaseClass0,class BaseClass1> on type class G3_C1926`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C840`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C840`2<class BaseClass0,class BaseClass1>::Method5()
		ldstr "G2_C840::Method5.12708()"
		ldstr "class G2_C840`2<class BaseClass0,class BaseClass1> on type class G3_C1926`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C840`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C840`2<class BaseClass0,class BaseClass1>::Method4()
		ldstr "G2_C840::Method4.12706()"
		ldstr "class G2_C840`2<class BaseClass0,class BaseClass1> on type class G3_C1926`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G2_C840::Method4.MI.12707()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1926`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G2_C840::Method5.12708()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1926`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C840::Method6.12709<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1926`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G3_C1926`1<class BaseClass1>
		callvirt instance string class G3_C1926`1<class BaseClass1>::ClassMethod5248<object>()
		ldstr "G3_C1926::ClassMethod5248.18745<System.Object>()"
		ldstr "class G3_C1926`1<class BaseClass1> on type class G3_C1926`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1926`1<class BaseClass1>
		callvirt instance string class G3_C1926`1<class BaseClass1>::ClassMethod5247()
		ldstr "G3_C1926::ClassMethod5247.18744()"
		ldstr "class G3_C1926`1<class BaseClass1> on type class G3_C1926`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1926`1<class BaseClass1>
		callvirt instance string class G3_C1926`1<class BaseClass1>::ClassMethod5246()
		ldstr "G3_C1926::ClassMethod5246.18743()"
		ldstr "class G3_C1926`1<class BaseClass1> on type class G3_C1926`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1926`1<class BaseClass1>
		callvirt instance string class G3_C1926`1<class BaseClass1>::Method7<object>()
		ldstr "G3_C1926::Method7.18741<System.Object>()"
		ldstr "class G3_C1926`1<class BaseClass1> on type class G3_C1926`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1926`1<class BaseClass1>
		callvirt instance string class G3_C1926`1<class BaseClass1>::ClassMethod3057()
		ldstr "G3_C1926::ClassMethod3057.MI.18746()"
		ldstr "class G3_C1926`1<class BaseClass1> on type class G3_C1926`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1926`1<class BaseClass1>
		callvirt instance string class G3_C1926`1<class BaseClass1>::ClassMethod3056()
		ldstr "G2_C840::ClassMethod3056.12711()"
		ldstr "class G3_C1926`1<class BaseClass1> on type class G3_C1926`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1926`1<class BaseClass1>
		callvirt instance string class G3_C1926`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C840::Method6.12709<System.Object>()"
		ldstr "class G3_C1926`1<class BaseClass1> on type class G3_C1926`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1926`1<class BaseClass1>
		callvirt instance string class G3_C1926`1<class BaseClass1>::Method5()
		ldstr "G2_C840::Method5.12708()"
		ldstr "class G3_C1926`1<class BaseClass1> on type class G3_C1926`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1926`1<class BaseClass1>
		callvirt instance string class G3_C1926`1<class BaseClass1>::Method4()
		ldstr "G2_C840::Method4.12706()"
		ldstr "class G3_C1926`1<class BaseClass1> on type class G3_C1926`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C840`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C15`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C15`2<class BaseClass1,class BaseClass0>::Method6<object>()
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass0> on type class G2_C840`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C15`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C15`2<class BaseClass1,class BaseClass0>::Method5()
		ldstr "G2_C840::Method5.12708()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass0> on type class G2_C840`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C15`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C15`2<class BaseClass1,class BaseClass0>::Method4()
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass0> on type class G2_C840`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C15`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C15`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G1_C15::Method7.4885<System.Object>()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass0> on type class G2_C840`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G1_C15::Method7.MI.4886<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C840`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C840::Method4.MI.12707()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C840`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C840::Method5.12708()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C840`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C840::Method6.12709<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C840`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C840::Method7.12710<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C840`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C840`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C840`2<class BaseClass0,class BaseClass0>::ClassMethod3057()
		ldstr "G2_C840::ClassMethod3057.12712()"
		ldstr "class G2_C840`2<class BaseClass0,class BaseClass0> on type class G2_C840`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C840`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C840`2<class BaseClass0,class BaseClass0>::ClassMethod3056()
		ldstr "G2_C840::ClassMethod3056.12711()"
		ldstr "class G2_C840`2<class BaseClass0,class BaseClass0> on type class G2_C840`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C840`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C840`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C840::Method7.12710<System.Object>()"
		ldstr "class G2_C840`2<class BaseClass0,class BaseClass0> on type class G2_C840`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C840`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C840`2<class BaseClass0,class BaseClass0>::Method6<object>()
		ldstr "G2_C840::Method6.12709<System.Object>()"
		ldstr "class G2_C840`2<class BaseClass0,class BaseClass0> on type class G2_C840`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C840`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C840`2<class BaseClass0,class BaseClass0>::Method5()
		ldstr "G2_C840::Method5.12708()"
		ldstr "class G2_C840`2<class BaseClass0,class BaseClass0> on type class G2_C840`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C840`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C840`2<class BaseClass0,class BaseClass0>::Method4()
		ldstr "G2_C840::Method4.12706()"
		ldstr "class G2_C840`2<class BaseClass0,class BaseClass0> on type class G2_C840`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G2_C840::Method4.MI.12707()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C840`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G2_C840::Method5.12708()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C840`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C840::Method6.12709<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C840`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C840::Method7.12710<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C840`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C840`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method6<object>()
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G2_C840`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method5()
		ldstr "G2_C840::Method5.12708()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G2_C840`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method4()
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G2_C840`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C15::Method7.4885<System.Object>()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G2_C840`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C840::Method7.12710<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C840`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C840::Method4.MI.12707()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C840`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C840::Method5.12708()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C840`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C840::Method6.12709<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C840`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C840::Method7.12710<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C840`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C840`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C840`2<class BaseClass0,class BaseClass1>::ClassMethod3057()
		ldstr "G2_C840::ClassMethod3057.12712()"
		ldstr "class G2_C840`2<class BaseClass0,class BaseClass1> on type class G2_C840`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C840`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C840`2<class BaseClass0,class BaseClass1>::ClassMethod3056()
		ldstr "G2_C840::ClassMethod3056.12711()"
		ldstr "class G2_C840`2<class BaseClass0,class BaseClass1> on type class G2_C840`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C840`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C840`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C840::Method7.12710<System.Object>()"
		ldstr "class G2_C840`2<class BaseClass0,class BaseClass1> on type class G2_C840`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C840`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C840`2<class BaseClass0,class BaseClass1>::Method6<object>()
		ldstr "G2_C840::Method6.12709<System.Object>()"
		ldstr "class G2_C840`2<class BaseClass0,class BaseClass1> on type class G2_C840`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C840`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C840`2<class BaseClass0,class BaseClass1>::Method5()
		ldstr "G2_C840::Method5.12708()"
		ldstr "class G2_C840`2<class BaseClass0,class BaseClass1> on type class G2_C840`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C840`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C840`2<class BaseClass0,class BaseClass1>::Method4()
		ldstr "G2_C840::Method4.12706()"
		ldstr "class G2_C840`2<class BaseClass0,class BaseClass1> on type class G2_C840`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G2_C840::Method4.MI.12707()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C840`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G2_C840::Method5.12708()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C840`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C840::Method6.12709<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C840`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C840`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C15`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C15`2<class BaseClass1,class BaseClass0>::Method6<object>()
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass0> on type class G2_C840`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C15`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C15`2<class BaseClass1,class BaseClass0>::Method5()
		ldstr "G2_C840::Method5.12708()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass0> on type class G2_C840`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C15`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C15`2<class BaseClass1,class BaseClass0>::Method4()
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass0> on type class G2_C840`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C15`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C15`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G1_C15::Method7.4885<System.Object>()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass0> on type class G2_C840`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G1_C15::Method7.MI.4886<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C840`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C840::Method4.MI.12707()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C840`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C840::Method5.12708()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C840`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C840::Method6.12709<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C840`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C840::Method7.12710<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C840`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C840`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C840`2<class BaseClass1,class BaseClass0>::ClassMethod3057()
		ldstr "G2_C840::ClassMethod3057.12712()"
		ldstr "class G2_C840`2<class BaseClass1,class BaseClass0> on type class G2_C840`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C840`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C840`2<class BaseClass1,class BaseClass0>::ClassMethod3056()
		ldstr "G2_C840::ClassMethod3056.12711()"
		ldstr "class G2_C840`2<class BaseClass1,class BaseClass0> on type class G2_C840`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C840`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C840`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G2_C840::Method7.12710<System.Object>()"
		ldstr "class G2_C840`2<class BaseClass1,class BaseClass0> on type class G2_C840`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C840`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C840`2<class BaseClass1,class BaseClass0>::Method6<object>()
		ldstr "G2_C840::Method6.12709<System.Object>()"
		ldstr "class G2_C840`2<class BaseClass1,class BaseClass0> on type class G2_C840`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C840`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C840`2<class BaseClass1,class BaseClass0>::Method5()
		ldstr "G2_C840::Method5.12708()"
		ldstr "class G2_C840`2<class BaseClass1,class BaseClass0> on type class G2_C840`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C840`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C840`2<class BaseClass1,class BaseClass0>::Method4()
		ldstr "G2_C840::Method4.12706()"
		ldstr "class G2_C840`2<class BaseClass1,class BaseClass0> on type class G2_C840`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G2_C840::Method4.MI.12707()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C840`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G2_C840::Method5.12708()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C840`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C840::Method6.12709<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C840`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C840::Method7.12710<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C840`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C840`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method6<object>()
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G2_C840`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method5()
		ldstr "G2_C840::Method5.12708()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G2_C840`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method4()
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G2_C840`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C15::Method7.4885<System.Object>()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G2_C840`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C840::Method7.12710<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C840`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C840::Method4.MI.12707()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C840`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C840::Method5.12708()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C840`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C840::Method6.12709<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C840`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C840::Method7.12710<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C840`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C840`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C840`2<class BaseClass1,class BaseClass1>::ClassMethod3057()
		ldstr "G2_C840::ClassMethod3057.12712()"
		ldstr "class G2_C840`2<class BaseClass1,class BaseClass1> on type class G2_C840`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C840`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C840`2<class BaseClass1,class BaseClass1>::ClassMethod3056()
		ldstr "G2_C840::ClassMethod3056.12711()"
		ldstr "class G2_C840`2<class BaseClass1,class BaseClass1> on type class G2_C840`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C840`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C840`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C840::Method7.12710<System.Object>()"
		ldstr "class G2_C840`2<class BaseClass1,class BaseClass1> on type class G2_C840`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C840`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C840`2<class BaseClass1,class BaseClass1>::Method6<object>()
		ldstr "G2_C840::Method6.12709<System.Object>()"
		ldstr "class G2_C840`2<class BaseClass1,class BaseClass1> on type class G2_C840`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C840`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C840`2<class BaseClass1,class BaseClass1>::Method5()
		ldstr "G2_C840::Method5.12708()"
		ldstr "class G2_C840`2<class BaseClass1,class BaseClass1> on type class G2_C840`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C840`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C840`2<class BaseClass1,class BaseClass1>::Method4()
		ldstr "G2_C840::Method4.12706()"
		ldstr "class G2_C840`2<class BaseClass1,class BaseClass1> on type class G2_C840`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G2_C840::Method4.MI.12707()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C840`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G2_C840::Method5.12708()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C840`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C840::Method6.12709<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C840`2<class BaseClass1,class BaseClass1>"
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
		newobj instance void class G3_C1926`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G2_C840::Method5.12708()#G1_C15::Method6.4891<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Framework::M.G1_C15.T.T<class BaseClass1,class BaseClass1,class G3_C1926`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G2_C840::Method5.12708()#G1_C15::Method6.4891<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Framework::M.G1_C15.B.T<class BaseClass1,class G3_C1926`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G2_C840::Method5.12708()#G1_C15::Method6.4891<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Framework::M.G1_C15.B.B<class G3_C1926`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1926::Method7.18741<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G3_C1926`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1926::Method7.18741<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G3_C1926`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1926::Method7.18741<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G3_C1926`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C840::Method4.MI.12707()#G2_C840::Method5.12708()#G2_C840::Method6.12709<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G3_C1926`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C840::Method4.MI.12707()#G2_C840::Method5.12708()#G2_C840::Method6.12709<System.Object>()#"
		call void Framework::M.IBase1.A<class G3_C1926`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1926::Method7.MI.18742<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G3_C1926`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1926::Method7.MI.18742<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G3_C1926`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1926::Method7.MI.18742<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G3_C1926`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C840::ClassMethod3056.12711()#G3_C1926::ClassMethod3057.MI.18746()#G2_C840::Method4.12706()#G2_C840::Method5.12708()#G2_C840::Method6.12709<System.Object>()#G3_C1926::Method7.18741<System.Object>()#"
		call void Framework::M.G2_C840.T.T<class BaseClass0,class BaseClass1,class G3_C1926`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C840::ClassMethod3056.12711()#G3_C1926::ClassMethod3057.MI.18746()#G2_C840::Method4.12706()#G2_C840::Method5.12708()#G2_C840::Method6.12709<System.Object>()#G3_C1926::Method7.18741<System.Object>()#"
		call void Framework::M.G2_C840.A.T<class BaseClass1,class G3_C1926`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C840::ClassMethod3056.12711()#G3_C1926::ClassMethod3057.MI.18746()#G2_C840::Method4.12706()#G2_C840::Method5.12708()#G2_C840::Method6.12709<System.Object>()#G3_C1926::Method7.18741<System.Object>()#"
		call void Framework::M.G2_C840.A.B<class G3_C1926`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C840::Method4.MI.12707()#G2_C840::Method5.12708()#G2_C840::Method6.12709<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass1,class G3_C1926`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C840::Method4.MI.12707()#G2_C840::Method5.12708()#G2_C840::Method6.12709<System.Object>()#"
		call void Framework::M.IBase1.B<class G3_C1926`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C840::ClassMethod3056.12711()#G3_C1926::ClassMethod3057.MI.18746()#G3_C1926::ClassMethod5246.18743()#G3_C1926::ClassMethod5247.18744()#G3_C1926::ClassMethod5248.18745<System.Object>()#G2_C840::Method4.12706()#G2_C840::Method5.12708()#G2_C840::Method6.12709<System.Object>()#G3_C1926::Method7.18741<System.Object>()#"
		call void Framework::M.G3_C1926.T<class BaseClass0,class G3_C1926`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C840::ClassMethod3056.12711()#G3_C1926::ClassMethod3057.MI.18746()#G3_C1926::ClassMethod5246.18743()#G3_C1926::ClassMethod5247.18744()#G3_C1926::ClassMethod5248.18745<System.Object>()#G2_C840::Method4.12706()#G2_C840::Method5.12708()#G2_C840::Method6.12709<System.Object>()#G3_C1926::Method7.18741<System.Object>()#"
		call void Framework::M.G3_C1926.A<class G3_C1926`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1926::Method7.MI.18742<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G3_C1926`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1926::Method7.MI.18742<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass0,class G3_C1926`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1926::Method7.MI.18742<System.Object>()#"
		call void Framework::M.IBase2.A.A<class G3_C1926`1<class BaseClass0>>(!!0,string)
		newobj instance void class G3_C1926`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G2_C840::Method5.12708()#G1_C15::Method6.4891<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Framework::M.G1_C15.T.T<class BaseClass1,class BaseClass1,class G3_C1926`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G2_C840::Method5.12708()#G1_C15::Method6.4891<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Framework::M.G1_C15.B.T<class BaseClass1,class G3_C1926`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G2_C840::Method5.12708()#G1_C15::Method6.4891<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Framework::M.G1_C15.B.B<class G3_C1926`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1926::Method7.MI.18742<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G3_C1926`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1926::Method7.MI.18742<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G3_C1926`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1926::Method7.MI.18742<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G3_C1926`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C840::Method4.MI.12707()#G2_C840::Method5.12708()#G2_C840::Method6.12709<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G3_C1926`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C840::Method4.MI.12707()#G2_C840::Method5.12708()#G2_C840::Method6.12709<System.Object>()#"
		call void Framework::M.IBase1.A<class G3_C1926`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1926::Method7.MI.18742<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G3_C1926`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1926::Method7.MI.18742<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G3_C1926`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1926::Method7.MI.18742<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G3_C1926`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C840::ClassMethod3056.12711()#G3_C1926::ClassMethod3057.MI.18746()#G2_C840::Method4.12706()#G2_C840::Method5.12708()#G2_C840::Method6.12709<System.Object>()#G3_C1926::Method7.18741<System.Object>()#"
		call void Framework::M.G2_C840.T.T<class BaseClass0,class BaseClass1,class G3_C1926`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C840::ClassMethod3056.12711()#G3_C1926::ClassMethod3057.MI.18746()#G2_C840::Method4.12706()#G2_C840::Method5.12708()#G2_C840::Method6.12709<System.Object>()#G3_C1926::Method7.18741<System.Object>()#"
		call void Framework::M.G2_C840.A.T<class BaseClass1,class G3_C1926`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C840::ClassMethod3056.12711()#G3_C1926::ClassMethod3057.MI.18746()#G2_C840::Method4.12706()#G2_C840::Method5.12708()#G2_C840::Method6.12709<System.Object>()#G3_C1926::Method7.18741<System.Object>()#"
		call void Framework::M.G2_C840.A.B<class G3_C1926`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C840::Method4.MI.12707()#G2_C840::Method5.12708()#G2_C840::Method6.12709<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass1,class G3_C1926`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C840::Method4.MI.12707()#G2_C840::Method5.12708()#G2_C840::Method6.12709<System.Object>()#"
		call void Framework::M.IBase1.B<class G3_C1926`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C840::ClassMethod3056.12711()#G3_C1926::ClassMethod3057.MI.18746()#G3_C1926::ClassMethod5246.18743()#G3_C1926::ClassMethod5247.18744()#G3_C1926::ClassMethod5248.18745<System.Object>()#G2_C840::Method4.12706()#G2_C840::Method5.12708()#G2_C840::Method6.12709<System.Object>()#G3_C1926::Method7.18741<System.Object>()#"
		call void Framework::M.G3_C1926.T<class BaseClass1,class G3_C1926`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C840::ClassMethod3056.12711()#G3_C1926::ClassMethod3057.MI.18746()#G3_C1926::ClassMethod5246.18743()#G3_C1926::ClassMethod5247.18744()#G3_C1926::ClassMethod5248.18745<System.Object>()#G2_C840::Method4.12706()#G2_C840::Method5.12708()#G2_C840::Method6.12709<System.Object>()#G3_C1926::Method7.18741<System.Object>()#"
		call void Framework::M.G3_C1926.B<class G3_C1926`1<class BaseClass1>>(!!0,string)
		newobj instance void class G2_C840`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G2_C840::Method5.12708()#G1_C15::Method6.4891<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Framework::M.G1_C15.T.T<class BaseClass1,class BaseClass0,class G2_C840`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G2_C840::Method5.12708()#G1_C15::Method6.4891<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Framework::M.G1_C15.B.T<class BaseClass0,class G2_C840`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G2_C840::Method5.12708()#G1_C15::Method6.4891<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Framework::M.G1_C15.B.A<class G2_C840`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G2_C840`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass0,class G2_C840`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Framework::M.IBase2.A.A<class G2_C840`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C840::Method4.MI.12707()#G2_C840::Method5.12708()#G2_C840::Method6.12709<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G2_C840`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C840::Method4.MI.12707()#G2_C840::Method5.12708()#G2_C840::Method6.12709<System.Object>()#"
		call void Framework::M.IBase1.A<class G2_C840`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C840::Method7.12710<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C840`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C840::Method7.12710<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G2_C840`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C840::Method7.12710<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G2_C840`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C840::ClassMethod3056.12711()#G2_C840::ClassMethod3057.12712()#G2_C840::Method4.12706()#G2_C840::Method5.12708()#G2_C840::Method6.12709<System.Object>()#G2_C840::Method7.12710<System.Object>()#"
		call void Framework::M.G2_C840.T.T<class BaseClass0,class BaseClass0,class G2_C840`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C840::ClassMethod3056.12711()#G2_C840::ClassMethod3057.12712()#G2_C840::Method4.12706()#G2_C840::Method5.12708()#G2_C840::Method6.12709<System.Object>()#G2_C840::Method7.12710<System.Object>()#"
		call void Framework::M.G2_C840.A.T<class BaseClass0,class G2_C840`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C840::ClassMethod3056.12711()#G2_C840::ClassMethod3057.12712()#G2_C840::Method4.12706()#G2_C840::Method5.12708()#G2_C840::Method6.12709<System.Object>()#G2_C840::Method7.12710<System.Object>()#"
		call void Framework::M.G2_C840.A.A<class G2_C840`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C840::Method4.MI.12707()#G2_C840::Method5.12708()#G2_C840::Method6.12709<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass1,class G2_C840`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C840::Method4.MI.12707()#G2_C840::Method5.12708()#G2_C840::Method6.12709<System.Object>()#"
		call void Framework::M.IBase1.B<class G2_C840`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C840::Method7.12710<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G2_C840`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C840::Method7.12710<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G2_C840`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C840::Method7.12710<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G2_C840`2<class BaseClass0,class BaseClass0>>(!!0,string)
		newobj instance void class G2_C840`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G2_C840::Method5.12708()#G1_C15::Method6.4891<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Framework::M.G1_C15.T.T<class BaseClass1,class BaseClass1,class G2_C840`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G2_C840::Method5.12708()#G1_C15::Method6.4891<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Framework::M.G1_C15.B.T<class BaseClass1,class G2_C840`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G2_C840::Method5.12708()#G1_C15::Method6.4891<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Framework::M.G1_C15.B.B<class G2_C840`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C840::Method7.12710<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G2_C840`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C840::Method7.12710<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G2_C840`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C840::Method7.12710<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G2_C840`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C840::Method4.MI.12707()#G2_C840::Method5.12708()#G2_C840::Method6.12709<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G2_C840`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C840::Method4.MI.12707()#G2_C840::Method5.12708()#G2_C840::Method6.12709<System.Object>()#"
		call void Framework::M.IBase1.A<class G2_C840`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C840::Method7.12710<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C840`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C840::Method7.12710<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G2_C840`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C840::Method7.12710<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G2_C840`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C840::ClassMethod3056.12711()#G2_C840::ClassMethod3057.12712()#G2_C840::Method4.12706()#G2_C840::Method5.12708()#G2_C840::Method6.12709<System.Object>()#G2_C840::Method7.12710<System.Object>()#"
		call void Framework::M.G2_C840.T.T<class BaseClass0,class BaseClass1,class G2_C840`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C840::ClassMethod3056.12711()#G2_C840::ClassMethod3057.12712()#G2_C840::Method4.12706()#G2_C840::Method5.12708()#G2_C840::Method6.12709<System.Object>()#G2_C840::Method7.12710<System.Object>()#"
		call void Framework::M.G2_C840.A.T<class BaseClass1,class G2_C840`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C840::ClassMethod3056.12711()#G2_C840::ClassMethod3057.12712()#G2_C840::Method4.12706()#G2_C840::Method5.12708()#G2_C840::Method6.12709<System.Object>()#G2_C840::Method7.12710<System.Object>()#"
		call void Framework::M.G2_C840.A.B<class G2_C840`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C840::Method4.MI.12707()#G2_C840::Method5.12708()#G2_C840::Method6.12709<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass1,class G2_C840`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C840::Method4.MI.12707()#G2_C840::Method5.12708()#G2_C840::Method6.12709<System.Object>()#"
		call void Framework::M.IBase1.B<class G2_C840`2<class BaseClass0,class BaseClass1>>(!!0,string)
		newobj instance void class G2_C840`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G2_C840::Method5.12708()#G1_C15::Method6.4891<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Framework::M.G1_C15.T.T<class BaseClass1,class BaseClass0,class G2_C840`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G2_C840::Method5.12708()#G1_C15::Method6.4891<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Framework::M.G1_C15.B.T<class BaseClass0,class G2_C840`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G2_C840::Method5.12708()#G1_C15::Method6.4891<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Framework::M.G1_C15.B.A<class G2_C840`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G2_C840`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass0,class G2_C840`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Framework::M.IBase2.A.A<class G2_C840`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C840::Method4.MI.12707()#G2_C840::Method5.12708()#G2_C840::Method6.12709<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G2_C840`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C840::Method4.MI.12707()#G2_C840::Method5.12708()#G2_C840::Method6.12709<System.Object>()#"
		call void Framework::M.IBase1.A<class G2_C840`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C840::Method7.12710<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C840`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C840::Method7.12710<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G2_C840`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C840::Method7.12710<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G2_C840`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C840::ClassMethod3056.12711()#G2_C840::ClassMethod3057.12712()#G2_C840::Method4.12706()#G2_C840::Method5.12708()#G2_C840::Method6.12709<System.Object>()#G2_C840::Method7.12710<System.Object>()#"
		call void Framework::M.G2_C840.T.T<class BaseClass1,class BaseClass0,class G2_C840`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C840::ClassMethod3056.12711()#G2_C840::ClassMethod3057.12712()#G2_C840::Method4.12706()#G2_C840::Method5.12708()#G2_C840::Method6.12709<System.Object>()#G2_C840::Method7.12710<System.Object>()#"
		call void Framework::M.G2_C840.B.T<class BaseClass0,class G2_C840`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C840::ClassMethod3056.12711()#G2_C840::ClassMethod3057.12712()#G2_C840::Method4.12706()#G2_C840::Method5.12708()#G2_C840::Method6.12709<System.Object>()#G2_C840::Method7.12710<System.Object>()#"
		call void Framework::M.G2_C840.B.A<class G2_C840`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C840::Method4.MI.12707()#G2_C840::Method5.12708()#G2_C840::Method6.12709<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass1,class G2_C840`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C840::Method4.MI.12707()#G2_C840::Method5.12708()#G2_C840::Method6.12709<System.Object>()#"
		call void Framework::M.IBase1.B<class G2_C840`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C840::Method7.12710<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G2_C840`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C840::Method7.12710<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G2_C840`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C840::Method7.12710<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G2_C840`2<class BaseClass1,class BaseClass0>>(!!0,string)
		newobj instance void class G2_C840`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G2_C840::Method5.12708()#G1_C15::Method6.4891<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Framework::M.G1_C15.T.T<class BaseClass1,class BaseClass1,class G2_C840`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G2_C840::Method5.12708()#G1_C15::Method6.4891<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Framework::M.G1_C15.B.T<class BaseClass1,class G2_C840`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G2_C840::Method5.12708()#G1_C15::Method6.4891<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Framework::M.G1_C15.B.B<class G2_C840`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C840::Method7.12710<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G2_C840`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C840::Method7.12710<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G2_C840`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C840::Method7.12710<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G2_C840`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C840::Method4.MI.12707()#G2_C840::Method5.12708()#G2_C840::Method6.12709<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G2_C840`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C840::Method4.MI.12707()#G2_C840::Method5.12708()#G2_C840::Method6.12709<System.Object>()#"
		call void Framework::M.IBase1.A<class G2_C840`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C840::Method7.12710<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C840`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C840::Method7.12710<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G2_C840`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C840::Method7.12710<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G2_C840`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C840::ClassMethod3056.12711()#G2_C840::ClassMethod3057.12712()#G2_C840::Method4.12706()#G2_C840::Method5.12708()#G2_C840::Method6.12709<System.Object>()#G2_C840::Method7.12710<System.Object>()#"
		call void Framework::M.G2_C840.T.T<class BaseClass1,class BaseClass1,class G2_C840`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C840::ClassMethod3056.12711()#G2_C840::ClassMethod3057.12712()#G2_C840::Method4.12706()#G2_C840::Method5.12708()#G2_C840::Method6.12709<System.Object>()#G2_C840::Method7.12710<System.Object>()#"
		call void Framework::M.G2_C840.B.T<class BaseClass1,class G2_C840`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C840::ClassMethod3056.12711()#G2_C840::ClassMethod3057.12712()#G2_C840::Method4.12706()#G2_C840::Method5.12708()#G2_C840::Method6.12709<System.Object>()#G2_C840::Method7.12710<System.Object>()#"
		call void Framework::M.G2_C840.B.B<class G2_C840`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C840::Method4.MI.12707()#G2_C840::Method5.12708()#G2_C840::Method6.12709<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass1,class G2_C840`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C840::Method4.MI.12707()#G2_C840::Method5.12708()#G2_C840::Method6.12709<System.Object>()#"
		call void Framework::M.IBase1.B<class G2_C840`2<class BaseClass1,class BaseClass1>>(!!0,string)
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
		newobj instance void class G3_C1926`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method6<object>()
		calli default string(class G3_C1926`1<class BaseClass0>)
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G3_C1926`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method5()
		calli default string(class G3_C1926`1<class BaseClass0>)
		ldstr "G2_C840::Method5.12708()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G3_C1926`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method4()
		calli default string(class G3_C1926`1<class BaseClass0>)
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G3_C1926`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1926`1<class BaseClass0>)
		ldstr "G1_C15::Method7.4885<System.Object>()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G3_C1926`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1926`1<class BaseClass0>)
		ldstr "G3_C1926::Method7.18741<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1926`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G3_C1926`1<class BaseClass0>)
		ldstr "G2_C840::Method4.MI.12707()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1926`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G3_C1926`1<class BaseClass0>)
		ldstr "G2_C840::Method5.12708()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1926`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1926`1<class BaseClass0>)
		ldstr "G2_C840::Method6.12709<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1926`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1926`1<class BaseClass0>)
		ldstr "G3_C1926::Method7.MI.18742<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1926`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C840`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C840`2<class BaseClass0,class BaseClass1>::ClassMethod3057()
		calli default string(class G3_C1926`1<class BaseClass0>)
		ldstr "G3_C1926::ClassMethod3057.MI.18746()"
		ldstr "class G2_C840`2<class BaseClass0,class BaseClass1> on type class G3_C1926`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C840`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C840`2<class BaseClass0,class BaseClass1>::ClassMethod3056()
		calli default string(class G3_C1926`1<class BaseClass0>)
		ldstr "G2_C840::ClassMethod3056.12711()"
		ldstr "class G2_C840`2<class BaseClass0,class BaseClass1> on type class G3_C1926`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C840`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C840`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1926`1<class BaseClass0>)
		ldstr "G3_C1926::Method7.18741<System.Object>()"
		ldstr "class G2_C840`2<class BaseClass0,class BaseClass1> on type class G3_C1926`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C840`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C840`2<class BaseClass0,class BaseClass1>::Method6<object>()
		calli default string(class G3_C1926`1<class BaseClass0>)
		ldstr "G2_C840::Method6.12709<System.Object>()"
		ldstr "class G2_C840`2<class BaseClass0,class BaseClass1> on type class G3_C1926`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C840`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C840`2<class BaseClass0,class BaseClass1>::Method5()
		calli default string(class G3_C1926`1<class BaseClass0>)
		ldstr "G2_C840::Method5.12708()"
		ldstr "class G2_C840`2<class BaseClass0,class BaseClass1> on type class G3_C1926`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C840`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C840`2<class BaseClass0,class BaseClass1>::Method4()
		calli default string(class G3_C1926`1<class BaseClass0>)
		ldstr "G2_C840::Method4.12706()"
		ldstr "class G2_C840`2<class BaseClass0,class BaseClass1> on type class G3_C1926`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G3_C1926`1<class BaseClass0>)
		ldstr "G2_C840::Method4.MI.12707()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1926`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G3_C1926`1<class BaseClass0>)
		ldstr "G2_C840::Method5.12708()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1926`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G3_C1926`1<class BaseClass0>)
		ldstr "G2_C840::Method6.12709<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1926`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1926`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1926`1<class BaseClass0>::ClassMethod5248<object>()
		calli default string(class G3_C1926`1<class BaseClass0>)
		ldstr "G3_C1926::ClassMethod5248.18745<System.Object>()"
		ldstr "class G3_C1926`1<class BaseClass0> on type class G3_C1926`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1926`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1926`1<class BaseClass0>::ClassMethod5247()
		calli default string(class G3_C1926`1<class BaseClass0>)
		ldstr "G3_C1926::ClassMethod5247.18744()"
		ldstr "class G3_C1926`1<class BaseClass0> on type class G3_C1926`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1926`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1926`1<class BaseClass0>::ClassMethod5246()
		calli default string(class G3_C1926`1<class BaseClass0>)
		ldstr "G3_C1926::ClassMethod5246.18743()"
		ldstr "class G3_C1926`1<class BaseClass0> on type class G3_C1926`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1926`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1926`1<class BaseClass0>::Method7<object>()
		calli default string(class G3_C1926`1<class BaseClass0>)
		ldstr "G3_C1926::Method7.18741<System.Object>()"
		ldstr "class G3_C1926`1<class BaseClass0> on type class G3_C1926`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1926`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1926`1<class BaseClass0>::ClassMethod3057()
		calli default string(class G3_C1926`1<class BaseClass0>)
		ldstr "G3_C1926::ClassMethod3057.MI.18746()"
		ldstr "class G3_C1926`1<class BaseClass0> on type class G3_C1926`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1926`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1926`1<class BaseClass0>::ClassMethod3056()
		calli default string(class G3_C1926`1<class BaseClass0>)
		ldstr "G2_C840::ClassMethod3056.12711()"
		ldstr "class G3_C1926`1<class BaseClass0> on type class G3_C1926`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1926`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1926`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1926`1<class BaseClass0>)
		ldstr "G2_C840::Method6.12709<System.Object>()"
		ldstr "class G3_C1926`1<class BaseClass0> on type class G3_C1926`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1926`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1926`1<class BaseClass0>::Method5()
		calli default string(class G3_C1926`1<class BaseClass0>)
		ldstr "G2_C840::Method5.12708()"
		ldstr "class G3_C1926`1<class BaseClass0> on type class G3_C1926`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1926`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1926`1<class BaseClass0>::Method4()
		calli default string(class G3_C1926`1<class BaseClass0>)
		ldstr "G2_C840::Method4.12706()"
		ldstr "class G3_C1926`1<class BaseClass0> on type class G3_C1926`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1926`1<class BaseClass0>)
		ldstr "G3_C1926::Method7.MI.18742<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1926`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G3_C1926`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method6<object>()
		calli default string(class G3_C1926`1<class BaseClass1>)
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G3_C1926`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method5()
		calli default string(class G3_C1926`1<class BaseClass1>)
		ldstr "G2_C840::Method5.12708()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G3_C1926`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method4()
		calli default string(class G3_C1926`1<class BaseClass1>)
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G3_C1926`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1926`1<class BaseClass1>)
		ldstr "G1_C15::Method7.4885<System.Object>()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G3_C1926`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1926`1<class BaseClass1>)
		ldstr "G3_C1926::Method7.MI.18742<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1926`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G3_C1926`1<class BaseClass1>)
		ldstr "G2_C840::Method4.MI.12707()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1926`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G3_C1926`1<class BaseClass1>)
		ldstr "G2_C840::Method5.12708()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1926`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1926`1<class BaseClass1>)
		ldstr "G2_C840::Method6.12709<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1926`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1926`1<class BaseClass1>)
		ldstr "G3_C1926::Method7.MI.18742<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1926`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C840`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C840`2<class BaseClass0,class BaseClass1>::ClassMethod3057()
		calli default string(class G3_C1926`1<class BaseClass1>)
		ldstr "G3_C1926::ClassMethod3057.MI.18746()"
		ldstr "class G2_C840`2<class BaseClass0,class BaseClass1> on type class G3_C1926`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C840`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C840`2<class BaseClass0,class BaseClass1>::ClassMethod3056()
		calli default string(class G3_C1926`1<class BaseClass1>)
		ldstr "G2_C840::ClassMethod3056.12711()"
		ldstr "class G2_C840`2<class BaseClass0,class BaseClass1> on type class G3_C1926`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C840`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C840`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1926`1<class BaseClass1>)
		ldstr "G3_C1926::Method7.18741<System.Object>()"
		ldstr "class G2_C840`2<class BaseClass0,class BaseClass1> on type class G3_C1926`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C840`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C840`2<class BaseClass0,class BaseClass1>::Method6<object>()
		calli default string(class G3_C1926`1<class BaseClass1>)
		ldstr "G2_C840::Method6.12709<System.Object>()"
		ldstr "class G2_C840`2<class BaseClass0,class BaseClass1> on type class G3_C1926`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C840`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C840`2<class BaseClass0,class BaseClass1>::Method5()
		calli default string(class G3_C1926`1<class BaseClass1>)
		ldstr "G2_C840::Method5.12708()"
		ldstr "class G2_C840`2<class BaseClass0,class BaseClass1> on type class G3_C1926`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C840`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C840`2<class BaseClass0,class BaseClass1>::Method4()
		calli default string(class G3_C1926`1<class BaseClass1>)
		ldstr "G2_C840::Method4.12706()"
		ldstr "class G2_C840`2<class BaseClass0,class BaseClass1> on type class G3_C1926`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G3_C1926`1<class BaseClass1>)
		ldstr "G2_C840::Method4.MI.12707()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1926`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G3_C1926`1<class BaseClass1>)
		ldstr "G2_C840::Method5.12708()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1926`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G3_C1926`1<class BaseClass1>)
		ldstr "G2_C840::Method6.12709<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1926`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1926`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1926`1<class BaseClass1>::ClassMethod5248<object>()
		calli default string(class G3_C1926`1<class BaseClass1>)
		ldstr "G3_C1926::ClassMethod5248.18745<System.Object>()"
		ldstr "class G3_C1926`1<class BaseClass1> on type class G3_C1926`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1926`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1926`1<class BaseClass1>::ClassMethod5247()
		calli default string(class G3_C1926`1<class BaseClass1>)
		ldstr "G3_C1926::ClassMethod5247.18744()"
		ldstr "class G3_C1926`1<class BaseClass1> on type class G3_C1926`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1926`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1926`1<class BaseClass1>::ClassMethod5246()
		calli default string(class G3_C1926`1<class BaseClass1>)
		ldstr "G3_C1926::ClassMethod5246.18743()"
		ldstr "class G3_C1926`1<class BaseClass1> on type class G3_C1926`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1926`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1926`1<class BaseClass1>::Method7<object>()
		calli default string(class G3_C1926`1<class BaseClass1>)
		ldstr "G3_C1926::Method7.18741<System.Object>()"
		ldstr "class G3_C1926`1<class BaseClass1> on type class G3_C1926`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1926`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1926`1<class BaseClass1>::ClassMethod3057()
		calli default string(class G3_C1926`1<class BaseClass1>)
		ldstr "G3_C1926::ClassMethod3057.MI.18746()"
		ldstr "class G3_C1926`1<class BaseClass1> on type class G3_C1926`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1926`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1926`1<class BaseClass1>::ClassMethod3056()
		calli default string(class G3_C1926`1<class BaseClass1>)
		ldstr "G2_C840::ClassMethod3056.12711()"
		ldstr "class G3_C1926`1<class BaseClass1> on type class G3_C1926`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1926`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1926`1<class BaseClass1>::Method6<object>()
		calli default string(class G3_C1926`1<class BaseClass1>)
		ldstr "G2_C840::Method6.12709<System.Object>()"
		ldstr "class G3_C1926`1<class BaseClass1> on type class G3_C1926`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1926`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1926`1<class BaseClass1>::Method5()
		calli default string(class G3_C1926`1<class BaseClass1>)
		ldstr "G2_C840::Method5.12708()"
		ldstr "class G3_C1926`1<class BaseClass1> on type class G3_C1926`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1926`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1926`1<class BaseClass1>::Method4()
		calli default string(class G3_C1926`1<class BaseClass1>)
		ldstr "G2_C840::Method4.12706()"
		ldstr "class G3_C1926`1<class BaseClass1> on type class G3_C1926`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C840`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C15`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C15`2<class BaseClass1,class BaseClass0>::Method6<object>()
		calli default string(class G2_C840`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass0> on type class G2_C840`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C15`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C15`2<class BaseClass1,class BaseClass0>::Method5()
		calli default string(class G2_C840`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C840::Method5.12708()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass0> on type class G2_C840`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C15`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C15`2<class BaseClass1,class BaseClass0>::Method4()
		calli default string(class G2_C840`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass0> on type class G2_C840`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C15`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C15`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G2_C840`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C15::Method7.4885<System.Object>()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass0> on type class G2_C840`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C840`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C15::Method7.MI.4886<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C840`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G2_C840`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C840::Method4.MI.12707()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C840`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G2_C840`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C840::Method5.12708()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C840`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G2_C840`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C840::Method6.12709<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C840`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C840`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C840::Method7.12710<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C840`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C840`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C840`2<class BaseClass0,class BaseClass0>::ClassMethod3057()
		calli default string(class G2_C840`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C840::ClassMethod3057.12712()"
		ldstr "class G2_C840`2<class BaseClass0,class BaseClass0> on type class G2_C840`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C840`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C840`2<class BaseClass0,class BaseClass0>::ClassMethod3056()
		calli default string(class G2_C840`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C840::ClassMethod3056.12711()"
		ldstr "class G2_C840`2<class BaseClass0,class BaseClass0> on type class G2_C840`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C840`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C840`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C840`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C840::Method7.12710<System.Object>()"
		ldstr "class G2_C840`2<class BaseClass0,class BaseClass0> on type class G2_C840`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C840`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C840`2<class BaseClass0,class BaseClass0>::Method6<object>()
		calli default string(class G2_C840`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C840::Method6.12709<System.Object>()"
		ldstr "class G2_C840`2<class BaseClass0,class BaseClass0> on type class G2_C840`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C840`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C840`2<class BaseClass0,class BaseClass0>::Method5()
		calli default string(class G2_C840`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C840::Method5.12708()"
		ldstr "class G2_C840`2<class BaseClass0,class BaseClass0> on type class G2_C840`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C840`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C840`2<class BaseClass0,class BaseClass0>::Method4()
		calli default string(class G2_C840`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C840::Method4.12706()"
		ldstr "class G2_C840`2<class BaseClass0,class BaseClass0> on type class G2_C840`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G2_C840`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C840::Method4.MI.12707()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C840`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G2_C840`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C840::Method5.12708()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C840`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G2_C840`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C840::Method6.12709<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C840`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C840`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C840::Method7.12710<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C840`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C840`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method6<object>()
		calli default string(class G2_C840`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G2_C840`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method5()
		calli default string(class G2_C840`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C840::Method5.12708()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G2_C840`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method4()
		calli default string(class G2_C840`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G2_C840`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C840`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C15::Method7.4885<System.Object>()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G2_C840`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C840`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C840::Method7.12710<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C840`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G2_C840`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C840::Method4.MI.12707()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C840`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G2_C840`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C840::Method5.12708()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C840`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G2_C840`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C840::Method6.12709<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C840`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C840`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C840::Method7.12710<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C840`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C840`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C840`2<class BaseClass0,class BaseClass1>::ClassMethod3057()
		calli default string(class G2_C840`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C840::ClassMethod3057.12712()"
		ldstr "class G2_C840`2<class BaseClass0,class BaseClass1> on type class G2_C840`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C840`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C840`2<class BaseClass0,class BaseClass1>::ClassMethod3056()
		calli default string(class G2_C840`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C840::ClassMethod3056.12711()"
		ldstr "class G2_C840`2<class BaseClass0,class BaseClass1> on type class G2_C840`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C840`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C840`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C840`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C840::Method7.12710<System.Object>()"
		ldstr "class G2_C840`2<class BaseClass0,class BaseClass1> on type class G2_C840`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C840`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C840`2<class BaseClass0,class BaseClass1>::Method6<object>()
		calli default string(class G2_C840`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C840::Method6.12709<System.Object>()"
		ldstr "class G2_C840`2<class BaseClass0,class BaseClass1> on type class G2_C840`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C840`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C840`2<class BaseClass0,class BaseClass1>::Method5()
		calli default string(class G2_C840`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C840::Method5.12708()"
		ldstr "class G2_C840`2<class BaseClass0,class BaseClass1> on type class G2_C840`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C840`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C840`2<class BaseClass0,class BaseClass1>::Method4()
		calli default string(class G2_C840`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C840::Method4.12706()"
		ldstr "class G2_C840`2<class BaseClass0,class BaseClass1> on type class G2_C840`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G2_C840`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C840::Method4.MI.12707()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C840`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G2_C840`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C840::Method5.12708()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C840`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G2_C840`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C840::Method6.12709<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C840`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C840`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C15`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C15`2<class BaseClass1,class BaseClass0>::Method6<object>()
		calli default string(class G2_C840`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass0> on type class G2_C840`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C15`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C15`2<class BaseClass1,class BaseClass0>::Method5()
		calli default string(class G2_C840`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C840::Method5.12708()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass0> on type class G2_C840`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C15`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C15`2<class BaseClass1,class BaseClass0>::Method4()
		calli default string(class G2_C840`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass0> on type class G2_C840`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C15`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C15`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G2_C840`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C15::Method7.4885<System.Object>()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass0> on type class G2_C840`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C840`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C15::Method7.MI.4886<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C840`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G2_C840`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C840::Method4.MI.12707()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C840`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G2_C840`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C840::Method5.12708()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C840`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G2_C840`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C840::Method6.12709<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C840`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C840`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C840::Method7.12710<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C840`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C840`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C840`2<class BaseClass1,class BaseClass0>::ClassMethod3057()
		calli default string(class G2_C840`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C840::ClassMethod3057.12712()"
		ldstr "class G2_C840`2<class BaseClass1,class BaseClass0> on type class G2_C840`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C840`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C840`2<class BaseClass1,class BaseClass0>::ClassMethod3056()
		calli default string(class G2_C840`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C840::ClassMethod3056.12711()"
		ldstr "class G2_C840`2<class BaseClass1,class BaseClass0> on type class G2_C840`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C840`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C840`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G2_C840`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C840::Method7.12710<System.Object>()"
		ldstr "class G2_C840`2<class BaseClass1,class BaseClass0> on type class G2_C840`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C840`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C840`2<class BaseClass1,class BaseClass0>::Method6<object>()
		calli default string(class G2_C840`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C840::Method6.12709<System.Object>()"
		ldstr "class G2_C840`2<class BaseClass1,class BaseClass0> on type class G2_C840`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C840`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C840`2<class BaseClass1,class BaseClass0>::Method5()
		calli default string(class G2_C840`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C840::Method5.12708()"
		ldstr "class G2_C840`2<class BaseClass1,class BaseClass0> on type class G2_C840`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C840`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C840`2<class BaseClass1,class BaseClass0>::Method4()
		calli default string(class G2_C840`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C840::Method4.12706()"
		ldstr "class G2_C840`2<class BaseClass1,class BaseClass0> on type class G2_C840`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G2_C840`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C840::Method4.MI.12707()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C840`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G2_C840`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C840::Method5.12708()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C840`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G2_C840`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C840::Method6.12709<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C840`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C840`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C840::Method7.12710<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C840`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C840`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method6<object>()
		calli default string(class G2_C840`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G2_C840`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method5()
		calli default string(class G2_C840`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C840::Method5.12708()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G2_C840`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method4()
		calli default string(class G2_C840`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G2_C840`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C840`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C15::Method7.4885<System.Object>()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G2_C840`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C840`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C840::Method7.12710<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C840`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G2_C840`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C840::Method4.MI.12707()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C840`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G2_C840`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C840::Method5.12708()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C840`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G2_C840`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C840::Method6.12709<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C840`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C840`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C840::Method7.12710<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C840`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C840`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C840`2<class BaseClass1,class BaseClass1>::ClassMethod3057()
		calli default string(class G2_C840`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C840::ClassMethod3057.12712()"
		ldstr "class G2_C840`2<class BaseClass1,class BaseClass1> on type class G2_C840`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C840`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C840`2<class BaseClass1,class BaseClass1>::ClassMethod3056()
		calli default string(class G2_C840`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C840::ClassMethod3056.12711()"
		ldstr "class G2_C840`2<class BaseClass1,class BaseClass1> on type class G2_C840`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C840`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C840`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C840`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C840::Method7.12710<System.Object>()"
		ldstr "class G2_C840`2<class BaseClass1,class BaseClass1> on type class G2_C840`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C840`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C840`2<class BaseClass1,class BaseClass1>::Method6<object>()
		calli default string(class G2_C840`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C840::Method6.12709<System.Object>()"
		ldstr "class G2_C840`2<class BaseClass1,class BaseClass1> on type class G2_C840`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C840`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C840`2<class BaseClass1,class BaseClass1>::Method5()
		calli default string(class G2_C840`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C840::Method5.12708()"
		ldstr "class G2_C840`2<class BaseClass1,class BaseClass1> on type class G2_C840`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C840`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C840`2<class BaseClass1,class BaseClass1>::Method4()
		calli default string(class G2_C840`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C840::Method4.12706()"
		ldstr "class G2_C840`2<class BaseClass1,class BaseClass1> on type class G2_C840`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G2_C840`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C840::Method4.MI.12707()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C840`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G2_C840`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C840::Method5.12708()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C840`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G2_C840`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C840::Method6.12709<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C840`2<class BaseClass1,class BaseClass1>"
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
