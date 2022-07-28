// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) .ver 4:0:0:0 }
.assembly extern TestFramework { .publickeytoken = ( B0 3F 5F 7F 11 D5 0A 3A )  }

//TYPES IN FORWARDER ASSEMBLIES:

//TEST ASSEMBLY:
.assembly Generated1441 { .hash algorithm 0x00008004 }
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
.class public G3_C1913`1<T0> 
		extends class G2_C832`2<class BaseClass0,!T0>
		implements class IBase2`2<class BaseClass1,class BaseClass0> 
{
	.method public hidebysig newslot virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G3_C1913::Method7.18682<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod5218() cil managed noinlining { 
		ldstr "G3_C1913::ClassMethod5218.18683()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod5219<M0>() cil managed noinlining { 
		ldstr "G3_C1913::ClassMethod5219.18684<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C832<class BaseClass0,T0>.ClassMethod3043'() cil managed noinlining { 
		.override method instance string class G2_C832`2<class BaseClass0,!T0>::ClassMethod3043()
		ldstr "G3_C1913::ClassMethod3043.MI.18685()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C832<class BaseClass0,T0>.ClassMethod3045'<M0>() cil managed noinlining { 
		.override method instance string class G2_C832`2<class BaseClass0,!T0>::ClassMethod3045<[1]>()
		ldstr "G3_C1913::ClassMethod3045.MI.18686<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class G2_C832`2<class BaseClass0,!T0>::.ctor()
		ret
	}
}
.class public G2_C832`2<T0, T1> 
		extends class G1_C15`2<class BaseClass1,class BaseClass1>
		implements class IBase2`2<class BaseClass1,!T0>, class IBase1`1<!T0> 
{
	.method public hidebysig newslot virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G2_C832::Method7.12650<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string Method4() cil managed noinlining { 
		ldstr "G2_C832::Method4.12651()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<T0>.Method4'() cil managed noinlining { 
		.override method instance string class IBase1`1<!T0>::Method4()
		ldstr "G2_C832::Method4.MI.12652()"
		ret
	}
	.method public hidebysig virtual instance string Method5() cil managed noinlining { 
		ldstr "G2_C832::Method5.12653()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G2_C832::Method6.12654<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<T0>.Method6'<M0>() cil managed noinlining { 
		.override method instance string class IBase1`1<!T0>::Method6<[1]>()
		ldstr "G2_C832::Method6.MI.12655<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod3043() cil managed noinlining { 
		ldstr "G2_C832::ClassMethod3043.12656()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod3044<M0>() cil managed noinlining { 
		ldstr "G2_C832::ClassMethod3044.12657<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod3045<M0>() cil managed noinlining { 
		ldstr "G2_C832::ClassMethod3045.12658<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class G1_C15`2<class BaseClass1,class BaseClass1>::.ctor()
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
.class public auto ansi beforefieldinit Generated1441 {
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
	.method static void M.G3_C1913.T<T0,(class G3_C1913`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1913.T<T0,(class G3_C1913`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1913`1<!!T0>::ClassMethod3043()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1913`1<!!T0>::ClassMethod3044<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1913`1<!!T0>::ClassMethod3045<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1913`1<!!T0>::ClassMethod5218()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1913`1<!!T0>::ClassMethod5219<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1913`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1913`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1913`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1913`1<!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1913.A<(class G3_C1913`1<class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1913.A<(class G3_C1913`1<class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1913`1<class BaseClass0>::ClassMethod3043()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1913`1<class BaseClass0>::ClassMethod3044<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1913`1<class BaseClass0>::ClassMethod3045<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1913`1<class BaseClass0>::ClassMethod5218()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1913`1<class BaseClass0>::ClassMethod5219<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1913`1<class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1913`1<class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1913`1<class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1913`1<class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1913.B<(class G3_C1913`1<class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1913.B<(class G3_C1913`1<class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1913`1<class BaseClass1>::ClassMethod3043()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1913`1<class BaseClass1>::ClassMethod3044<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1913`1<class BaseClass1>::ClassMethod3045<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1913`1<class BaseClass1>::ClassMethod5218()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1913`1<class BaseClass1>::ClassMethod5219<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1913`1<class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1913`1<class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1913`1<class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1913`1<class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C832.T.T<T0,T1,(class G2_C832`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C832.T.T<T0,T1,(class G2_C832`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C832`2<!!T0,!!T1>::ClassMethod3043()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C832`2<!!T0,!!T1>::ClassMethod3044<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C832`2<!!T0,!!T1>::ClassMethod3045<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C832`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C832`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C832`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C832`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C832.A.T<T1,(class G2_C832`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C832.A.T<T1,(class G2_C832`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C832`2<class BaseClass0,!!T1>::ClassMethod3043()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C832`2<class BaseClass0,!!T1>::ClassMethod3044<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C832`2<class BaseClass0,!!T1>::ClassMethod3045<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C832`2<class BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C832`2<class BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C832`2<class BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C832`2<class BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C832.A.A<(class G2_C832`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C832.A.A<(class G2_C832`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C832`2<class BaseClass0,class BaseClass0>::ClassMethod3043()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C832`2<class BaseClass0,class BaseClass0>::ClassMethod3044<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C832`2<class BaseClass0,class BaseClass0>::ClassMethod3045<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C832`2<class BaseClass0,class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C832`2<class BaseClass0,class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C832`2<class BaseClass0,class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C832`2<class BaseClass0,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C832.A.B<(class G2_C832`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C832.A.B<(class G2_C832`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C832`2<class BaseClass0,class BaseClass1>::ClassMethod3043()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C832`2<class BaseClass0,class BaseClass1>::ClassMethod3044<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C832`2<class BaseClass0,class BaseClass1>::ClassMethod3045<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C832`2<class BaseClass0,class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C832`2<class BaseClass0,class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C832`2<class BaseClass0,class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C832`2<class BaseClass0,class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C832.B.T<T1,(class G2_C832`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C832.B.T<T1,(class G2_C832`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C832`2<class BaseClass1,!!T1>::ClassMethod3043()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C832`2<class BaseClass1,!!T1>::ClassMethod3044<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C832`2<class BaseClass1,!!T1>::ClassMethod3045<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C832`2<class BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C832`2<class BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C832`2<class BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C832`2<class BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C832.B.A<(class G2_C832`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C832.B.A<(class G2_C832`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C832`2<class BaseClass1,class BaseClass0>::ClassMethod3043()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C832`2<class BaseClass1,class BaseClass0>::ClassMethod3044<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C832`2<class BaseClass1,class BaseClass0>::ClassMethod3045<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C832`2<class BaseClass1,class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C832`2<class BaseClass1,class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C832`2<class BaseClass1,class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C832`2<class BaseClass1,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C832.B.B<(class G2_C832`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C832.B.B<(class G2_C832`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C832`2<class BaseClass1,class BaseClass1>::ClassMethod3043()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C832`2<class BaseClass1,class BaseClass1>::ClassMethod3044<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C832`2<class BaseClass1,class BaseClass1>::ClassMethod3045<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C832`2<class BaseClass1,class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C832`2<class BaseClass1,class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C832`2<class BaseClass1,class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C832`2<class BaseClass1,class BaseClass1>::Method7<object>()
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
		newobj instance void class G3_C1913`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method6<object>()
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G3_C1913`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method5()
		ldstr "G2_C832::Method5.12653()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G3_C1913`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method4()
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G3_C1913`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C15::Method7.4885<System.Object>()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G3_C1913`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G3_C1913::Method7.18682<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1913`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C832::Method4.MI.12652()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1913`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C832::Method5.12653()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1913`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C832::Method6.MI.12655<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1913`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G3_C1913::Method7.18682<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1913`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C832`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C832`2<class BaseClass0,class BaseClass0>::ClassMethod3045<object>()
		ldstr "G3_C1913::ClassMethod3045.MI.18686<System.Object>()"
		ldstr "class G2_C832`2<class BaseClass0,class BaseClass0> on type class G3_C1913`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C832`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C832`2<class BaseClass0,class BaseClass0>::ClassMethod3044<object>()
		ldstr "G2_C832::ClassMethod3044.12657<System.Object>()"
		ldstr "class G2_C832`2<class BaseClass0,class BaseClass0> on type class G3_C1913`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C832`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C832`2<class BaseClass0,class BaseClass0>::ClassMethod3043()
		ldstr "G3_C1913::ClassMethod3043.MI.18685()"
		ldstr "class G2_C832`2<class BaseClass0,class BaseClass0> on type class G3_C1913`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C832`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C832`2<class BaseClass0,class BaseClass0>::Method6<object>()
		ldstr "G2_C832::Method6.12654<System.Object>()"
		ldstr "class G2_C832`2<class BaseClass0,class BaseClass0> on type class G3_C1913`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C832`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C832`2<class BaseClass0,class BaseClass0>::Method5()
		ldstr "G2_C832::Method5.12653()"
		ldstr "class G2_C832`2<class BaseClass0,class BaseClass0> on type class G3_C1913`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C832`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C832`2<class BaseClass0,class BaseClass0>::Method4()
		ldstr "G2_C832::Method4.12651()"
		ldstr "class G2_C832`2<class BaseClass0,class BaseClass0> on type class G3_C1913`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C832`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C832`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C832::Method7.12650<System.Object>()"
		ldstr "class G2_C832`2<class BaseClass0,class BaseClass0> on type class G3_C1913`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G3_C1913::Method7.18682<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G3_C1913`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G3_C1913::Method7.18682<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1913`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G3_C1913`1<class BaseClass0>
		callvirt instance string class G3_C1913`1<class BaseClass0>::ClassMethod5219<object>()
		ldstr "G3_C1913::ClassMethod5219.18684<System.Object>()"
		ldstr "class G3_C1913`1<class BaseClass0> on type class G3_C1913`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1913`1<class BaseClass0>
		callvirt instance string class G3_C1913`1<class BaseClass0>::ClassMethod5218()
		ldstr "G3_C1913::ClassMethod5218.18683()"
		ldstr "class G3_C1913`1<class BaseClass0> on type class G3_C1913`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1913`1<class BaseClass0>
		callvirt instance string class G3_C1913`1<class BaseClass0>::Method7<object>()
		ldstr "G3_C1913::Method7.18682<System.Object>()"
		ldstr "class G3_C1913`1<class BaseClass0> on type class G3_C1913`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1913`1<class BaseClass0>
		callvirt instance string class G3_C1913`1<class BaseClass0>::ClassMethod3045<object>()
		ldstr "G3_C1913::ClassMethod3045.MI.18686<System.Object>()"
		ldstr "class G3_C1913`1<class BaseClass0> on type class G3_C1913`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1913`1<class BaseClass0>
		callvirt instance string class G3_C1913`1<class BaseClass0>::ClassMethod3044<object>()
		ldstr "G2_C832::ClassMethod3044.12657<System.Object>()"
		ldstr "class G3_C1913`1<class BaseClass0> on type class G3_C1913`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1913`1<class BaseClass0>
		callvirt instance string class G3_C1913`1<class BaseClass0>::ClassMethod3043()
		ldstr "G3_C1913::ClassMethod3043.MI.18685()"
		ldstr "class G3_C1913`1<class BaseClass0> on type class G3_C1913`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1913`1<class BaseClass0>
		callvirt instance string class G3_C1913`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C832::Method6.12654<System.Object>()"
		ldstr "class G3_C1913`1<class BaseClass0> on type class G3_C1913`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1913`1<class BaseClass0>
		callvirt instance string class G3_C1913`1<class BaseClass0>::Method5()
		ldstr "G2_C832::Method5.12653()"
		ldstr "class G3_C1913`1<class BaseClass0> on type class G3_C1913`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1913`1<class BaseClass0>
		callvirt instance string class G3_C1913`1<class BaseClass0>::Method4()
		ldstr "G2_C832::Method4.12651()"
		ldstr "class G3_C1913`1<class BaseClass0> on type class G3_C1913`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G3_C1913`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method6<object>()
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G3_C1913`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method5()
		ldstr "G2_C832::Method5.12653()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G3_C1913`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method4()
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G3_C1913`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C15::Method7.4885<System.Object>()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G3_C1913`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G3_C1913::Method7.18682<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1913`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C832::Method4.MI.12652()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1913`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C832::Method5.12653()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1913`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C832::Method6.MI.12655<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1913`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G3_C1913::Method7.18682<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1913`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C832`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C832`2<class BaseClass0,class BaseClass1>::ClassMethod3045<object>()
		ldstr "G3_C1913::ClassMethod3045.MI.18686<System.Object>()"
		ldstr "class G2_C832`2<class BaseClass0,class BaseClass1> on type class G3_C1913`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C832`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C832`2<class BaseClass0,class BaseClass1>::ClassMethod3044<object>()
		ldstr "G2_C832::ClassMethod3044.12657<System.Object>()"
		ldstr "class G2_C832`2<class BaseClass0,class BaseClass1> on type class G3_C1913`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C832`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C832`2<class BaseClass0,class BaseClass1>::ClassMethod3043()
		ldstr "G3_C1913::ClassMethod3043.MI.18685()"
		ldstr "class G2_C832`2<class BaseClass0,class BaseClass1> on type class G3_C1913`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C832`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C832`2<class BaseClass0,class BaseClass1>::Method6<object>()
		ldstr "G2_C832::Method6.12654<System.Object>()"
		ldstr "class G2_C832`2<class BaseClass0,class BaseClass1> on type class G3_C1913`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C832`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C832`2<class BaseClass0,class BaseClass1>::Method5()
		ldstr "G2_C832::Method5.12653()"
		ldstr "class G2_C832`2<class BaseClass0,class BaseClass1> on type class G3_C1913`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C832`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C832`2<class BaseClass0,class BaseClass1>::Method4()
		ldstr "G2_C832::Method4.12651()"
		ldstr "class G2_C832`2<class BaseClass0,class BaseClass1> on type class G3_C1913`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C832`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C832`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C832::Method7.12650<System.Object>()"
		ldstr "class G2_C832`2<class BaseClass0,class BaseClass1> on type class G3_C1913`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G3_C1913::Method7.18682<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G3_C1913`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G3_C1913::Method7.18682<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1913`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G3_C1913`1<class BaseClass1>
		callvirt instance string class G3_C1913`1<class BaseClass1>::ClassMethod5219<object>()
		ldstr "G3_C1913::ClassMethod5219.18684<System.Object>()"
		ldstr "class G3_C1913`1<class BaseClass1> on type class G3_C1913`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1913`1<class BaseClass1>
		callvirt instance string class G3_C1913`1<class BaseClass1>::ClassMethod5218()
		ldstr "G3_C1913::ClassMethod5218.18683()"
		ldstr "class G3_C1913`1<class BaseClass1> on type class G3_C1913`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1913`1<class BaseClass1>
		callvirt instance string class G3_C1913`1<class BaseClass1>::Method7<object>()
		ldstr "G3_C1913::Method7.18682<System.Object>()"
		ldstr "class G3_C1913`1<class BaseClass1> on type class G3_C1913`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1913`1<class BaseClass1>
		callvirt instance string class G3_C1913`1<class BaseClass1>::ClassMethod3045<object>()
		ldstr "G3_C1913::ClassMethod3045.MI.18686<System.Object>()"
		ldstr "class G3_C1913`1<class BaseClass1> on type class G3_C1913`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1913`1<class BaseClass1>
		callvirt instance string class G3_C1913`1<class BaseClass1>::ClassMethod3044<object>()
		ldstr "G2_C832::ClassMethod3044.12657<System.Object>()"
		ldstr "class G3_C1913`1<class BaseClass1> on type class G3_C1913`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1913`1<class BaseClass1>
		callvirt instance string class G3_C1913`1<class BaseClass1>::ClassMethod3043()
		ldstr "G3_C1913::ClassMethod3043.MI.18685()"
		ldstr "class G3_C1913`1<class BaseClass1> on type class G3_C1913`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1913`1<class BaseClass1>
		callvirt instance string class G3_C1913`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C832::Method6.12654<System.Object>()"
		ldstr "class G3_C1913`1<class BaseClass1> on type class G3_C1913`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1913`1<class BaseClass1>
		callvirt instance string class G3_C1913`1<class BaseClass1>::Method5()
		ldstr "G2_C832::Method5.12653()"
		ldstr "class G3_C1913`1<class BaseClass1> on type class G3_C1913`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1913`1<class BaseClass1>
		callvirt instance string class G3_C1913`1<class BaseClass1>::Method4()
		ldstr "G2_C832::Method4.12651()"
		ldstr "class G3_C1913`1<class BaseClass1> on type class G3_C1913`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C832`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method6<object>()
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G2_C832`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method5()
		ldstr "G2_C832::Method5.12653()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G2_C832`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method4()
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G2_C832`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C15::Method7.4885<System.Object>()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G2_C832`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C832::Method7.12650<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C832`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C832::Method4.MI.12652()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C832`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C832::Method5.12653()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C832`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C832::Method6.MI.12655<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C832`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C832::Method7.12650<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C832`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C832`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C832`2<class BaseClass0,class BaseClass0>::ClassMethod3045<object>()
		ldstr "G2_C832::ClassMethod3045.12658<System.Object>()"
		ldstr "class G2_C832`2<class BaseClass0,class BaseClass0> on type class G2_C832`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C832`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C832`2<class BaseClass0,class BaseClass0>::ClassMethod3044<object>()
		ldstr "G2_C832::ClassMethod3044.12657<System.Object>()"
		ldstr "class G2_C832`2<class BaseClass0,class BaseClass0> on type class G2_C832`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C832`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C832`2<class BaseClass0,class BaseClass0>::ClassMethod3043()
		ldstr "G2_C832::ClassMethod3043.12656()"
		ldstr "class G2_C832`2<class BaseClass0,class BaseClass0> on type class G2_C832`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C832`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C832`2<class BaseClass0,class BaseClass0>::Method6<object>()
		ldstr "G2_C832::Method6.12654<System.Object>()"
		ldstr "class G2_C832`2<class BaseClass0,class BaseClass0> on type class G2_C832`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C832`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C832`2<class BaseClass0,class BaseClass0>::Method5()
		ldstr "G2_C832::Method5.12653()"
		ldstr "class G2_C832`2<class BaseClass0,class BaseClass0> on type class G2_C832`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C832`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C832`2<class BaseClass0,class BaseClass0>::Method4()
		ldstr "G2_C832::Method4.12651()"
		ldstr "class G2_C832`2<class BaseClass0,class BaseClass0> on type class G2_C832`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C832`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C832`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C832::Method7.12650<System.Object>()"
		ldstr "class G2_C832`2<class BaseClass0,class BaseClass0> on type class G2_C832`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G2_C832::Method7.12650<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G2_C832`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C832::Method7.12650<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C832`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C832`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method6<object>()
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G2_C832`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method5()
		ldstr "G2_C832::Method5.12653()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G2_C832`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method4()
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G2_C832`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C15::Method7.4885<System.Object>()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G2_C832`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C832::Method7.12650<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C832`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C832::Method4.MI.12652()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C832`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C832::Method5.12653()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C832`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C832::Method6.MI.12655<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C832`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C832::Method7.12650<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C832`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C832`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C832`2<class BaseClass0,class BaseClass1>::ClassMethod3045<object>()
		ldstr "G2_C832::ClassMethod3045.12658<System.Object>()"
		ldstr "class G2_C832`2<class BaseClass0,class BaseClass1> on type class G2_C832`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C832`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C832`2<class BaseClass0,class BaseClass1>::ClassMethod3044<object>()
		ldstr "G2_C832::ClassMethod3044.12657<System.Object>()"
		ldstr "class G2_C832`2<class BaseClass0,class BaseClass1> on type class G2_C832`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C832`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C832`2<class BaseClass0,class BaseClass1>::ClassMethod3043()
		ldstr "G2_C832::ClassMethod3043.12656()"
		ldstr "class G2_C832`2<class BaseClass0,class BaseClass1> on type class G2_C832`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C832`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C832`2<class BaseClass0,class BaseClass1>::Method6<object>()
		ldstr "G2_C832::Method6.12654<System.Object>()"
		ldstr "class G2_C832`2<class BaseClass0,class BaseClass1> on type class G2_C832`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C832`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C832`2<class BaseClass0,class BaseClass1>::Method5()
		ldstr "G2_C832::Method5.12653()"
		ldstr "class G2_C832`2<class BaseClass0,class BaseClass1> on type class G2_C832`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C832`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C832`2<class BaseClass0,class BaseClass1>::Method4()
		ldstr "G2_C832::Method4.12651()"
		ldstr "class G2_C832`2<class BaseClass0,class BaseClass1> on type class G2_C832`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C832`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C832`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C832::Method7.12650<System.Object>()"
		ldstr "class G2_C832`2<class BaseClass0,class BaseClass1> on type class G2_C832`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G2_C832::Method7.12650<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G2_C832`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C832::Method7.12650<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C832`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C832`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method6<object>()
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G2_C832`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method5()
		ldstr "G2_C832::Method5.12653()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G2_C832`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method4()
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G2_C832`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C15::Method7.4885<System.Object>()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G2_C832`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C832::Method7.12650<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C832`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C832::Method4.MI.12652()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C832`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C832::Method5.12653()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C832`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C832::Method6.MI.12655<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C832`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C832::Method7.12650<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C832`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C832`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C832`2<class BaseClass1,class BaseClass0>::ClassMethod3045<object>()
		ldstr "G2_C832::ClassMethod3045.12658<System.Object>()"
		ldstr "class G2_C832`2<class BaseClass1,class BaseClass0> on type class G2_C832`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C832`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C832`2<class BaseClass1,class BaseClass0>::ClassMethod3044<object>()
		ldstr "G2_C832::ClassMethod3044.12657<System.Object>()"
		ldstr "class G2_C832`2<class BaseClass1,class BaseClass0> on type class G2_C832`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C832`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C832`2<class BaseClass1,class BaseClass0>::ClassMethod3043()
		ldstr "G2_C832::ClassMethod3043.12656()"
		ldstr "class G2_C832`2<class BaseClass1,class BaseClass0> on type class G2_C832`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C832`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C832`2<class BaseClass1,class BaseClass0>::Method6<object>()
		ldstr "G2_C832::Method6.12654<System.Object>()"
		ldstr "class G2_C832`2<class BaseClass1,class BaseClass0> on type class G2_C832`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C832`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C832`2<class BaseClass1,class BaseClass0>::Method5()
		ldstr "G2_C832::Method5.12653()"
		ldstr "class G2_C832`2<class BaseClass1,class BaseClass0> on type class G2_C832`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C832`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C832`2<class BaseClass1,class BaseClass0>::Method4()
		ldstr "G2_C832::Method4.12651()"
		ldstr "class G2_C832`2<class BaseClass1,class BaseClass0> on type class G2_C832`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C832`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C832`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G2_C832::Method7.12650<System.Object>()"
		ldstr "class G2_C832`2<class BaseClass1,class BaseClass0> on type class G2_C832`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G2_C832::Method4.MI.12652()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C832`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G2_C832::Method5.12653()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C832`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C832::Method6.MI.12655<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C832`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C832`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method6<object>()
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G2_C832`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method5()
		ldstr "G2_C832::Method5.12653()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G2_C832`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method4()
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G2_C832`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C15::Method7.4885<System.Object>()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G2_C832`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C832::Method7.12650<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C832`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C832::Method4.MI.12652()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C832`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C832::Method5.12653()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C832`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C832::Method6.MI.12655<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C832`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C832::Method7.12650<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C832`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C832`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C832`2<class BaseClass1,class BaseClass1>::ClassMethod3045<object>()
		ldstr "G2_C832::ClassMethod3045.12658<System.Object>()"
		ldstr "class G2_C832`2<class BaseClass1,class BaseClass1> on type class G2_C832`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C832`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C832`2<class BaseClass1,class BaseClass1>::ClassMethod3044<object>()
		ldstr "G2_C832::ClassMethod3044.12657<System.Object>()"
		ldstr "class G2_C832`2<class BaseClass1,class BaseClass1> on type class G2_C832`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C832`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C832`2<class BaseClass1,class BaseClass1>::ClassMethod3043()
		ldstr "G2_C832::ClassMethod3043.12656()"
		ldstr "class G2_C832`2<class BaseClass1,class BaseClass1> on type class G2_C832`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C832`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C832`2<class BaseClass1,class BaseClass1>::Method6<object>()
		ldstr "G2_C832::Method6.12654<System.Object>()"
		ldstr "class G2_C832`2<class BaseClass1,class BaseClass1> on type class G2_C832`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C832`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C832`2<class BaseClass1,class BaseClass1>::Method5()
		ldstr "G2_C832::Method5.12653()"
		ldstr "class G2_C832`2<class BaseClass1,class BaseClass1> on type class G2_C832`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C832`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C832`2<class BaseClass1,class BaseClass1>::Method4()
		ldstr "G2_C832::Method4.12651()"
		ldstr "class G2_C832`2<class BaseClass1,class BaseClass1> on type class G2_C832`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C832`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C832`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C832::Method7.12650<System.Object>()"
		ldstr "class G2_C832`2<class BaseClass1,class BaseClass1> on type class G2_C832`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G2_C832::Method4.MI.12652()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C832`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G2_C832::Method5.12653()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C832`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C832::Method6.MI.12655<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C832`2<class BaseClass1,class BaseClass1>"
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
		newobj instance void class G3_C1913`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G2_C832::Method5.12653()#G1_C15::Method6.4891<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Generated1441::M.G1_C15.T.T<class BaseClass1,class BaseClass1,class G3_C1913`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G2_C832::Method5.12653()#G1_C15::Method6.4891<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Generated1441::M.G1_C15.B.T<class BaseClass1,class G3_C1913`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G2_C832::Method5.12653()#G1_C15::Method6.4891<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Generated1441::M.G1_C15.B.B<class G3_C1913`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1913::Method7.18682<System.Object>()#"
		call void Generated1441::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G3_C1913`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1913::Method7.18682<System.Object>()#"
		call void Generated1441::M.IBase2.B.T<class BaseClass1,class G3_C1913`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1913::Method7.18682<System.Object>()#"
		call void Generated1441::M.IBase2.B.B<class G3_C1913`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C832::Method4.MI.12652()#G2_C832::Method5.12653()#G2_C832::Method6.MI.12655<System.Object>()#"
		call void Generated1441::M.IBase1.T<class BaseClass0,class G3_C1913`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C832::Method4.MI.12652()#G2_C832::Method5.12653()#G2_C832::Method6.MI.12655<System.Object>()#"
		call void Generated1441::M.IBase1.A<class G3_C1913`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1913::Method7.18682<System.Object>()#"
		call void Generated1441::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G3_C1913`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1913::Method7.18682<System.Object>()#"
		call void Generated1441::M.IBase2.A.T<class BaseClass1,class G3_C1913`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1913::Method7.18682<System.Object>()#"
		call void Generated1441::M.IBase2.A.B<class G3_C1913`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1913::ClassMethod3043.MI.18685()#G2_C832::ClassMethod3044.12657<System.Object>()#G3_C1913::ClassMethod3045.MI.18686<System.Object>()#G2_C832::Method4.12651()#G2_C832::Method5.12653()#G2_C832::Method6.12654<System.Object>()#G2_C832::Method7.12650<System.Object>()#"
		call void Generated1441::M.G2_C832.T.T<class BaseClass0,class BaseClass0,class G3_C1913`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1913::ClassMethod3043.MI.18685()#G2_C832::ClassMethod3044.12657<System.Object>()#G3_C1913::ClassMethod3045.MI.18686<System.Object>()#G2_C832::Method4.12651()#G2_C832::Method5.12653()#G2_C832::Method6.12654<System.Object>()#G2_C832::Method7.12650<System.Object>()#"
		call void Generated1441::M.G2_C832.A.T<class BaseClass0,class G3_C1913`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1913::ClassMethod3043.MI.18685()#G2_C832::ClassMethod3044.12657<System.Object>()#G3_C1913::ClassMethod3045.MI.18686<System.Object>()#G2_C832::Method4.12651()#G2_C832::Method5.12653()#G2_C832::Method6.12654<System.Object>()#G2_C832::Method7.12650<System.Object>()#"
		call void Generated1441::M.G2_C832.A.A<class G3_C1913`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1913::Method7.18682<System.Object>()#"
		call void Generated1441::M.IBase2.T.T<class BaseClass1,class BaseClass0,class G3_C1913`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1913::Method7.18682<System.Object>()#"
		call void Generated1441::M.IBase2.B.T<class BaseClass0,class G3_C1913`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1913::Method7.18682<System.Object>()#"
		call void Generated1441::M.IBase2.B.A<class G3_C1913`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1913::Method7.18682<System.Object>()#"
		call void Generated1441::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G3_C1913`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1913::Method7.18682<System.Object>()#"
		call void Generated1441::M.IBase2.A.T<class BaseClass0,class G3_C1913`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1913::Method7.18682<System.Object>()#"
		call void Generated1441::M.IBase2.A.A<class G3_C1913`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1913::ClassMethod3043.MI.18685()#G2_C832::ClassMethod3044.12657<System.Object>()#G3_C1913::ClassMethod3045.MI.18686<System.Object>()#G3_C1913::ClassMethod5218.18683()#G3_C1913::ClassMethod5219.18684<System.Object>()#G2_C832::Method4.12651()#G2_C832::Method5.12653()#G2_C832::Method6.12654<System.Object>()#G3_C1913::Method7.18682<System.Object>()#"
		call void Generated1441::M.G3_C1913.T<class BaseClass0,class G3_C1913`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1913::ClassMethod3043.MI.18685()#G2_C832::ClassMethod3044.12657<System.Object>()#G3_C1913::ClassMethod3045.MI.18686<System.Object>()#G3_C1913::ClassMethod5218.18683()#G3_C1913::ClassMethod5219.18684<System.Object>()#G2_C832::Method4.12651()#G2_C832::Method5.12653()#G2_C832::Method6.12654<System.Object>()#G3_C1913::Method7.18682<System.Object>()#"
		call void Generated1441::M.G3_C1913.A<class G3_C1913`1<class BaseClass0>>(!!0,string)
		newobj instance void class G3_C1913`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G2_C832::Method5.12653()#G1_C15::Method6.4891<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Generated1441::M.G1_C15.T.T<class BaseClass1,class BaseClass1,class G3_C1913`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G2_C832::Method5.12653()#G1_C15::Method6.4891<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Generated1441::M.G1_C15.B.T<class BaseClass1,class G3_C1913`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G2_C832::Method5.12653()#G1_C15::Method6.4891<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Generated1441::M.G1_C15.B.B<class G3_C1913`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1913::Method7.18682<System.Object>()#"
		call void Generated1441::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G3_C1913`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1913::Method7.18682<System.Object>()#"
		call void Generated1441::M.IBase2.B.T<class BaseClass1,class G3_C1913`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1913::Method7.18682<System.Object>()#"
		call void Generated1441::M.IBase2.B.B<class G3_C1913`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C832::Method4.MI.12652()#G2_C832::Method5.12653()#G2_C832::Method6.MI.12655<System.Object>()#"
		call void Generated1441::M.IBase1.T<class BaseClass0,class G3_C1913`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C832::Method4.MI.12652()#G2_C832::Method5.12653()#G2_C832::Method6.MI.12655<System.Object>()#"
		call void Generated1441::M.IBase1.A<class G3_C1913`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1913::Method7.18682<System.Object>()#"
		call void Generated1441::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G3_C1913`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1913::Method7.18682<System.Object>()#"
		call void Generated1441::M.IBase2.A.T<class BaseClass1,class G3_C1913`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1913::Method7.18682<System.Object>()#"
		call void Generated1441::M.IBase2.A.B<class G3_C1913`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1913::ClassMethod3043.MI.18685()#G2_C832::ClassMethod3044.12657<System.Object>()#G3_C1913::ClassMethod3045.MI.18686<System.Object>()#G2_C832::Method4.12651()#G2_C832::Method5.12653()#G2_C832::Method6.12654<System.Object>()#G2_C832::Method7.12650<System.Object>()#"
		call void Generated1441::M.G2_C832.T.T<class BaseClass0,class BaseClass1,class G3_C1913`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1913::ClassMethod3043.MI.18685()#G2_C832::ClassMethod3044.12657<System.Object>()#G3_C1913::ClassMethod3045.MI.18686<System.Object>()#G2_C832::Method4.12651()#G2_C832::Method5.12653()#G2_C832::Method6.12654<System.Object>()#G2_C832::Method7.12650<System.Object>()#"
		call void Generated1441::M.G2_C832.A.T<class BaseClass1,class G3_C1913`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1913::ClassMethod3043.MI.18685()#G2_C832::ClassMethod3044.12657<System.Object>()#G3_C1913::ClassMethod3045.MI.18686<System.Object>()#G2_C832::Method4.12651()#G2_C832::Method5.12653()#G2_C832::Method6.12654<System.Object>()#G2_C832::Method7.12650<System.Object>()#"
		call void Generated1441::M.G2_C832.A.B<class G3_C1913`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1913::Method7.18682<System.Object>()#"
		call void Generated1441::M.IBase2.T.T<class BaseClass1,class BaseClass0,class G3_C1913`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1913::Method7.18682<System.Object>()#"
		call void Generated1441::M.IBase2.B.T<class BaseClass0,class G3_C1913`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1913::Method7.18682<System.Object>()#"
		call void Generated1441::M.IBase2.B.A<class G3_C1913`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1913::Method7.18682<System.Object>()#"
		call void Generated1441::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G3_C1913`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1913::Method7.18682<System.Object>()#"
		call void Generated1441::M.IBase2.A.T<class BaseClass0,class G3_C1913`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1913::Method7.18682<System.Object>()#"
		call void Generated1441::M.IBase2.A.A<class G3_C1913`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1913::ClassMethod3043.MI.18685()#G2_C832::ClassMethod3044.12657<System.Object>()#G3_C1913::ClassMethod3045.MI.18686<System.Object>()#G3_C1913::ClassMethod5218.18683()#G3_C1913::ClassMethod5219.18684<System.Object>()#G2_C832::Method4.12651()#G2_C832::Method5.12653()#G2_C832::Method6.12654<System.Object>()#G3_C1913::Method7.18682<System.Object>()#"
		call void Generated1441::M.G3_C1913.T<class BaseClass1,class G3_C1913`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1913::ClassMethod3043.MI.18685()#G2_C832::ClassMethod3044.12657<System.Object>()#G3_C1913::ClassMethod3045.MI.18686<System.Object>()#G3_C1913::ClassMethod5218.18683()#G3_C1913::ClassMethod5219.18684<System.Object>()#G2_C832::Method4.12651()#G2_C832::Method5.12653()#G2_C832::Method6.12654<System.Object>()#G3_C1913::Method7.18682<System.Object>()#"
		call void Generated1441::M.G3_C1913.B<class G3_C1913`1<class BaseClass1>>(!!0,string)
		newobj instance void class G2_C832`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G2_C832::Method5.12653()#G1_C15::Method6.4891<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Generated1441::M.G1_C15.T.T<class BaseClass1,class BaseClass1,class G2_C832`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G2_C832::Method5.12653()#G1_C15::Method6.4891<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Generated1441::M.G1_C15.B.T<class BaseClass1,class G2_C832`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G2_C832::Method5.12653()#G1_C15::Method6.4891<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Generated1441::M.G1_C15.B.B<class G2_C832`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C832::Method7.12650<System.Object>()#"
		call void Generated1441::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G2_C832`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C832::Method7.12650<System.Object>()#"
		call void Generated1441::M.IBase2.B.T<class BaseClass1,class G2_C832`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C832::Method7.12650<System.Object>()#"
		call void Generated1441::M.IBase2.B.B<class G2_C832`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C832::Method4.MI.12652()#G2_C832::Method5.12653()#G2_C832::Method6.MI.12655<System.Object>()#"
		call void Generated1441::M.IBase1.T<class BaseClass0,class G2_C832`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C832::Method4.MI.12652()#G2_C832::Method5.12653()#G2_C832::Method6.MI.12655<System.Object>()#"
		call void Generated1441::M.IBase1.A<class G2_C832`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C832::Method7.12650<System.Object>()#"
		call void Generated1441::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C832`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C832::Method7.12650<System.Object>()#"
		call void Generated1441::M.IBase2.A.T<class BaseClass1,class G2_C832`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C832::Method7.12650<System.Object>()#"
		call void Generated1441::M.IBase2.A.B<class G2_C832`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C832::ClassMethod3043.12656()#G2_C832::ClassMethod3044.12657<System.Object>()#G2_C832::ClassMethod3045.12658<System.Object>()#G2_C832::Method4.12651()#G2_C832::Method5.12653()#G2_C832::Method6.12654<System.Object>()#G2_C832::Method7.12650<System.Object>()#"
		call void Generated1441::M.G2_C832.T.T<class BaseClass0,class BaseClass0,class G2_C832`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C832::ClassMethod3043.12656()#G2_C832::ClassMethod3044.12657<System.Object>()#G2_C832::ClassMethod3045.12658<System.Object>()#G2_C832::Method4.12651()#G2_C832::Method5.12653()#G2_C832::Method6.12654<System.Object>()#G2_C832::Method7.12650<System.Object>()#"
		call void Generated1441::M.G2_C832.A.T<class BaseClass0,class G2_C832`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C832::ClassMethod3043.12656()#G2_C832::ClassMethod3044.12657<System.Object>()#G2_C832::ClassMethod3045.12658<System.Object>()#G2_C832::Method4.12651()#G2_C832::Method5.12653()#G2_C832::Method6.12654<System.Object>()#G2_C832::Method7.12650<System.Object>()#"
		call void Generated1441::M.G2_C832.A.A<class G2_C832`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C832::Method7.12650<System.Object>()#"
		call void Generated1441::M.IBase2.T.T<class BaseClass1,class BaseClass0,class G2_C832`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C832::Method7.12650<System.Object>()#"
		call void Generated1441::M.IBase2.B.T<class BaseClass0,class G2_C832`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C832::Method7.12650<System.Object>()#"
		call void Generated1441::M.IBase2.B.A<class G2_C832`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C832::Method7.12650<System.Object>()#"
		call void Generated1441::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G2_C832`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C832::Method7.12650<System.Object>()#"
		call void Generated1441::M.IBase2.A.T<class BaseClass0,class G2_C832`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C832::Method7.12650<System.Object>()#"
		call void Generated1441::M.IBase2.A.A<class G2_C832`2<class BaseClass0,class BaseClass0>>(!!0,string)
		newobj instance void class G2_C832`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G2_C832::Method5.12653()#G1_C15::Method6.4891<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Generated1441::M.G1_C15.T.T<class BaseClass1,class BaseClass1,class G2_C832`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G2_C832::Method5.12653()#G1_C15::Method6.4891<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Generated1441::M.G1_C15.B.T<class BaseClass1,class G2_C832`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G2_C832::Method5.12653()#G1_C15::Method6.4891<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Generated1441::M.G1_C15.B.B<class G2_C832`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C832::Method7.12650<System.Object>()#"
		call void Generated1441::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G2_C832`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C832::Method7.12650<System.Object>()#"
		call void Generated1441::M.IBase2.B.T<class BaseClass1,class G2_C832`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C832::Method7.12650<System.Object>()#"
		call void Generated1441::M.IBase2.B.B<class G2_C832`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C832::Method4.MI.12652()#G2_C832::Method5.12653()#G2_C832::Method6.MI.12655<System.Object>()#"
		call void Generated1441::M.IBase1.T<class BaseClass0,class G2_C832`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C832::Method4.MI.12652()#G2_C832::Method5.12653()#G2_C832::Method6.MI.12655<System.Object>()#"
		call void Generated1441::M.IBase1.A<class G2_C832`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C832::Method7.12650<System.Object>()#"
		call void Generated1441::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C832`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C832::Method7.12650<System.Object>()#"
		call void Generated1441::M.IBase2.A.T<class BaseClass1,class G2_C832`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C832::Method7.12650<System.Object>()#"
		call void Generated1441::M.IBase2.A.B<class G2_C832`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C832::ClassMethod3043.12656()#G2_C832::ClassMethod3044.12657<System.Object>()#G2_C832::ClassMethod3045.12658<System.Object>()#G2_C832::Method4.12651()#G2_C832::Method5.12653()#G2_C832::Method6.12654<System.Object>()#G2_C832::Method7.12650<System.Object>()#"
		call void Generated1441::M.G2_C832.T.T<class BaseClass0,class BaseClass1,class G2_C832`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C832::ClassMethod3043.12656()#G2_C832::ClassMethod3044.12657<System.Object>()#G2_C832::ClassMethod3045.12658<System.Object>()#G2_C832::Method4.12651()#G2_C832::Method5.12653()#G2_C832::Method6.12654<System.Object>()#G2_C832::Method7.12650<System.Object>()#"
		call void Generated1441::M.G2_C832.A.T<class BaseClass1,class G2_C832`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C832::ClassMethod3043.12656()#G2_C832::ClassMethod3044.12657<System.Object>()#G2_C832::ClassMethod3045.12658<System.Object>()#G2_C832::Method4.12651()#G2_C832::Method5.12653()#G2_C832::Method6.12654<System.Object>()#G2_C832::Method7.12650<System.Object>()#"
		call void Generated1441::M.G2_C832.A.B<class G2_C832`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C832::Method7.12650<System.Object>()#"
		call void Generated1441::M.IBase2.T.T<class BaseClass1,class BaseClass0,class G2_C832`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C832::Method7.12650<System.Object>()#"
		call void Generated1441::M.IBase2.B.T<class BaseClass0,class G2_C832`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C832::Method7.12650<System.Object>()#"
		call void Generated1441::M.IBase2.B.A<class G2_C832`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C832::Method7.12650<System.Object>()#"
		call void Generated1441::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G2_C832`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C832::Method7.12650<System.Object>()#"
		call void Generated1441::M.IBase2.A.T<class BaseClass0,class G2_C832`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C832::Method7.12650<System.Object>()#"
		call void Generated1441::M.IBase2.A.A<class G2_C832`2<class BaseClass0,class BaseClass1>>(!!0,string)
		newobj instance void class G2_C832`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G2_C832::Method5.12653()#G1_C15::Method6.4891<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Generated1441::M.G1_C15.T.T<class BaseClass1,class BaseClass1,class G2_C832`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G2_C832::Method5.12653()#G1_C15::Method6.4891<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Generated1441::M.G1_C15.B.T<class BaseClass1,class G2_C832`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G2_C832::Method5.12653()#G1_C15::Method6.4891<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Generated1441::M.G1_C15.B.B<class G2_C832`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C832::Method7.12650<System.Object>()#"
		call void Generated1441::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G2_C832`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C832::Method7.12650<System.Object>()#"
		call void Generated1441::M.IBase2.B.T<class BaseClass1,class G2_C832`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C832::Method7.12650<System.Object>()#"
		call void Generated1441::M.IBase2.B.B<class G2_C832`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C832::Method4.MI.12652()#G2_C832::Method5.12653()#G2_C832::Method6.MI.12655<System.Object>()#"
		call void Generated1441::M.IBase1.T<class BaseClass0,class G2_C832`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C832::Method4.MI.12652()#G2_C832::Method5.12653()#G2_C832::Method6.MI.12655<System.Object>()#"
		call void Generated1441::M.IBase1.A<class G2_C832`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C832::Method7.12650<System.Object>()#"
		call void Generated1441::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C832`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C832::Method7.12650<System.Object>()#"
		call void Generated1441::M.IBase2.A.T<class BaseClass1,class G2_C832`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C832::Method7.12650<System.Object>()#"
		call void Generated1441::M.IBase2.A.B<class G2_C832`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C832::ClassMethod3043.12656()#G2_C832::ClassMethod3044.12657<System.Object>()#G2_C832::ClassMethod3045.12658<System.Object>()#G2_C832::Method4.12651()#G2_C832::Method5.12653()#G2_C832::Method6.12654<System.Object>()#G2_C832::Method7.12650<System.Object>()#"
		call void Generated1441::M.G2_C832.T.T<class BaseClass1,class BaseClass0,class G2_C832`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C832::ClassMethod3043.12656()#G2_C832::ClassMethod3044.12657<System.Object>()#G2_C832::ClassMethod3045.12658<System.Object>()#G2_C832::Method4.12651()#G2_C832::Method5.12653()#G2_C832::Method6.12654<System.Object>()#G2_C832::Method7.12650<System.Object>()#"
		call void Generated1441::M.G2_C832.B.T<class BaseClass0,class G2_C832`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C832::ClassMethod3043.12656()#G2_C832::ClassMethod3044.12657<System.Object>()#G2_C832::ClassMethod3045.12658<System.Object>()#G2_C832::Method4.12651()#G2_C832::Method5.12653()#G2_C832::Method6.12654<System.Object>()#G2_C832::Method7.12650<System.Object>()#"
		call void Generated1441::M.G2_C832.B.A<class G2_C832`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C832::Method4.MI.12652()#G2_C832::Method5.12653()#G2_C832::Method6.MI.12655<System.Object>()#"
		call void Generated1441::M.IBase1.T<class BaseClass1,class G2_C832`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C832::Method4.MI.12652()#G2_C832::Method5.12653()#G2_C832::Method6.MI.12655<System.Object>()#"
		call void Generated1441::M.IBase1.B<class G2_C832`2<class BaseClass1,class BaseClass0>>(!!0,string)
		newobj instance void class G2_C832`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G2_C832::Method5.12653()#G1_C15::Method6.4891<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Generated1441::M.G1_C15.T.T<class BaseClass1,class BaseClass1,class G2_C832`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G2_C832::Method5.12653()#G1_C15::Method6.4891<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Generated1441::M.G1_C15.B.T<class BaseClass1,class G2_C832`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G2_C832::Method5.12653()#G1_C15::Method6.4891<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Generated1441::M.G1_C15.B.B<class G2_C832`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C832::Method7.12650<System.Object>()#"
		call void Generated1441::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G2_C832`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C832::Method7.12650<System.Object>()#"
		call void Generated1441::M.IBase2.B.T<class BaseClass1,class G2_C832`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C832::Method7.12650<System.Object>()#"
		call void Generated1441::M.IBase2.B.B<class G2_C832`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C832::Method4.MI.12652()#G2_C832::Method5.12653()#G2_C832::Method6.MI.12655<System.Object>()#"
		call void Generated1441::M.IBase1.T<class BaseClass0,class G2_C832`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C832::Method4.MI.12652()#G2_C832::Method5.12653()#G2_C832::Method6.MI.12655<System.Object>()#"
		call void Generated1441::M.IBase1.A<class G2_C832`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C832::Method7.12650<System.Object>()#"
		call void Generated1441::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C832`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C832::Method7.12650<System.Object>()#"
		call void Generated1441::M.IBase2.A.T<class BaseClass1,class G2_C832`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C832::Method7.12650<System.Object>()#"
		call void Generated1441::M.IBase2.A.B<class G2_C832`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C832::ClassMethod3043.12656()#G2_C832::ClassMethod3044.12657<System.Object>()#G2_C832::ClassMethod3045.12658<System.Object>()#G2_C832::Method4.12651()#G2_C832::Method5.12653()#G2_C832::Method6.12654<System.Object>()#G2_C832::Method7.12650<System.Object>()#"
		call void Generated1441::M.G2_C832.T.T<class BaseClass1,class BaseClass1,class G2_C832`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C832::ClassMethod3043.12656()#G2_C832::ClassMethod3044.12657<System.Object>()#G2_C832::ClassMethod3045.12658<System.Object>()#G2_C832::Method4.12651()#G2_C832::Method5.12653()#G2_C832::Method6.12654<System.Object>()#G2_C832::Method7.12650<System.Object>()#"
		call void Generated1441::M.G2_C832.B.T<class BaseClass1,class G2_C832`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C832::ClassMethod3043.12656()#G2_C832::ClassMethod3044.12657<System.Object>()#G2_C832::ClassMethod3045.12658<System.Object>()#G2_C832::Method4.12651()#G2_C832::Method5.12653()#G2_C832::Method6.12654<System.Object>()#G2_C832::Method7.12650<System.Object>()#"
		call void Generated1441::M.G2_C832.B.B<class G2_C832`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C832::Method4.MI.12652()#G2_C832::Method5.12653()#G2_C832::Method6.MI.12655<System.Object>()#"
		call void Generated1441::M.IBase1.T<class BaseClass1,class G2_C832`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C832::Method4.MI.12652()#G2_C832::Method5.12653()#G2_C832::Method6.MI.12655<System.Object>()#"
		call void Generated1441::M.IBase1.B<class G2_C832`2<class BaseClass1,class BaseClass1>>(!!0,string)
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
		newobj instance void class G3_C1913`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method6<object>()
		calli default string(class G3_C1913`1<class BaseClass0>)
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G3_C1913`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method5()
		calli default string(class G3_C1913`1<class BaseClass0>)
		ldstr "G2_C832::Method5.12653()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G3_C1913`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method4()
		calli default string(class G3_C1913`1<class BaseClass0>)
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G3_C1913`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1913`1<class BaseClass0>)
		ldstr "G1_C15::Method7.4885<System.Object>()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G3_C1913`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1913`1<class BaseClass0>)
		ldstr "G3_C1913::Method7.18682<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1913`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G3_C1913`1<class BaseClass0>)
		ldstr "G2_C832::Method4.MI.12652()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1913`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G3_C1913`1<class BaseClass0>)
		ldstr "G2_C832::Method5.12653()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1913`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1913`1<class BaseClass0>)
		ldstr "G2_C832::Method6.MI.12655<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1913`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1913`1<class BaseClass0>)
		ldstr "G3_C1913::Method7.18682<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1913`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C832`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C832`2<class BaseClass0,class BaseClass0>::ClassMethod3045<object>()
		calli default string(class G3_C1913`1<class BaseClass0>)
		ldstr "G3_C1913::ClassMethod3045.MI.18686<System.Object>()"
		ldstr "class G2_C832`2<class BaseClass0,class BaseClass0> on type class G3_C1913`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C832`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C832`2<class BaseClass0,class BaseClass0>::ClassMethod3044<object>()
		calli default string(class G3_C1913`1<class BaseClass0>)
		ldstr "G2_C832::ClassMethod3044.12657<System.Object>()"
		ldstr "class G2_C832`2<class BaseClass0,class BaseClass0> on type class G3_C1913`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C832`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C832`2<class BaseClass0,class BaseClass0>::ClassMethod3043()
		calli default string(class G3_C1913`1<class BaseClass0>)
		ldstr "G3_C1913::ClassMethod3043.MI.18685()"
		ldstr "class G2_C832`2<class BaseClass0,class BaseClass0> on type class G3_C1913`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C832`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C832`2<class BaseClass0,class BaseClass0>::Method6<object>()
		calli default string(class G3_C1913`1<class BaseClass0>)
		ldstr "G2_C832::Method6.12654<System.Object>()"
		ldstr "class G2_C832`2<class BaseClass0,class BaseClass0> on type class G3_C1913`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C832`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C832`2<class BaseClass0,class BaseClass0>::Method5()
		calli default string(class G3_C1913`1<class BaseClass0>)
		ldstr "G2_C832::Method5.12653()"
		ldstr "class G2_C832`2<class BaseClass0,class BaseClass0> on type class G3_C1913`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C832`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C832`2<class BaseClass0,class BaseClass0>::Method4()
		calli default string(class G3_C1913`1<class BaseClass0>)
		ldstr "G2_C832::Method4.12651()"
		ldstr "class G2_C832`2<class BaseClass0,class BaseClass0> on type class G3_C1913`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C832`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C832`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1913`1<class BaseClass0>)
		ldstr "G2_C832::Method7.12650<System.Object>()"
		ldstr "class G2_C832`2<class BaseClass0,class BaseClass0> on type class G3_C1913`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1913`1<class BaseClass0>)
		ldstr "G3_C1913::Method7.18682<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G3_C1913`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1913`1<class BaseClass0>)
		ldstr "G3_C1913::Method7.18682<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1913`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1913`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1913`1<class BaseClass0>::ClassMethod5219<object>()
		calli default string(class G3_C1913`1<class BaseClass0>)
		ldstr "G3_C1913::ClassMethod5219.18684<System.Object>()"
		ldstr "class G3_C1913`1<class BaseClass0> on type class G3_C1913`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1913`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1913`1<class BaseClass0>::ClassMethod5218()
		calli default string(class G3_C1913`1<class BaseClass0>)
		ldstr "G3_C1913::ClassMethod5218.18683()"
		ldstr "class G3_C1913`1<class BaseClass0> on type class G3_C1913`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1913`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1913`1<class BaseClass0>::Method7<object>()
		calli default string(class G3_C1913`1<class BaseClass0>)
		ldstr "G3_C1913::Method7.18682<System.Object>()"
		ldstr "class G3_C1913`1<class BaseClass0> on type class G3_C1913`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1913`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1913`1<class BaseClass0>::ClassMethod3045<object>()
		calli default string(class G3_C1913`1<class BaseClass0>)
		ldstr "G3_C1913::ClassMethod3045.MI.18686<System.Object>()"
		ldstr "class G3_C1913`1<class BaseClass0> on type class G3_C1913`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1913`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1913`1<class BaseClass0>::ClassMethod3044<object>()
		calli default string(class G3_C1913`1<class BaseClass0>)
		ldstr "G2_C832::ClassMethod3044.12657<System.Object>()"
		ldstr "class G3_C1913`1<class BaseClass0> on type class G3_C1913`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1913`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1913`1<class BaseClass0>::ClassMethod3043()
		calli default string(class G3_C1913`1<class BaseClass0>)
		ldstr "G3_C1913::ClassMethod3043.MI.18685()"
		ldstr "class G3_C1913`1<class BaseClass0> on type class G3_C1913`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1913`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1913`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1913`1<class BaseClass0>)
		ldstr "G2_C832::Method6.12654<System.Object>()"
		ldstr "class G3_C1913`1<class BaseClass0> on type class G3_C1913`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1913`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1913`1<class BaseClass0>::Method5()
		calli default string(class G3_C1913`1<class BaseClass0>)
		ldstr "G2_C832::Method5.12653()"
		ldstr "class G3_C1913`1<class BaseClass0> on type class G3_C1913`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1913`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1913`1<class BaseClass0>::Method4()
		calli default string(class G3_C1913`1<class BaseClass0>)
		ldstr "G2_C832::Method4.12651()"
		ldstr "class G3_C1913`1<class BaseClass0> on type class G3_C1913`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G3_C1913`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method6<object>()
		calli default string(class G3_C1913`1<class BaseClass1>)
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G3_C1913`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method5()
		calli default string(class G3_C1913`1<class BaseClass1>)
		ldstr "G2_C832::Method5.12653()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G3_C1913`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method4()
		calli default string(class G3_C1913`1<class BaseClass1>)
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G3_C1913`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1913`1<class BaseClass1>)
		ldstr "G1_C15::Method7.4885<System.Object>()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G3_C1913`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1913`1<class BaseClass1>)
		ldstr "G3_C1913::Method7.18682<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1913`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G3_C1913`1<class BaseClass1>)
		ldstr "G2_C832::Method4.MI.12652()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1913`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G3_C1913`1<class BaseClass1>)
		ldstr "G2_C832::Method5.12653()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1913`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1913`1<class BaseClass1>)
		ldstr "G2_C832::Method6.MI.12655<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1913`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1913`1<class BaseClass1>)
		ldstr "G3_C1913::Method7.18682<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1913`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C832`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C832`2<class BaseClass0,class BaseClass1>::ClassMethod3045<object>()
		calli default string(class G3_C1913`1<class BaseClass1>)
		ldstr "G3_C1913::ClassMethod3045.MI.18686<System.Object>()"
		ldstr "class G2_C832`2<class BaseClass0,class BaseClass1> on type class G3_C1913`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C832`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C832`2<class BaseClass0,class BaseClass1>::ClassMethod3044<object>()
		calli default string(class G3_C1913`1<class BaseClass1>)
		ldstr "G2_C832::ClassMethod3044.12657<System.Object>()"
		ldstr "class G2_C832`2<class BaseClass0,class BaseClass1> on type class G3_C1913`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C832`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C832`2<class BaseClass0,class BaseClass1>::ClassMethod3043()
		calli default string(class G3_C1913`1<class BaseClass1>)
		ldstr "G3_C1913::ClassMethod3043.MI.18685()"
		ldstr "class G2_C832`2<class BaseClass0,class BaseClass1> on type class G3_C1913`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C832`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C832`2<class BaseClass0,class BaseClass1>::Method6<object>()
		calli default string(class G3_C1913`1<class BaseClass1>)
		ldstr "G2_C832::Method6.12654<System.Object>()"
		ldstr "class G2_C832`2<class BaseClass0,class BaseClass1> on type class G3_C1913`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C832`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C832`2<class BaseClass0,class BaseClass1>::Method5()
		calli default string(class G3_C1913`1<class BaseClass1>)
		ldstr "G2_C832::Method5.12653()"
		ldstr "class G2_C832`2<class BaseClass0,class BaseClass1> on type class G3_C1913`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C832`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C832`2<class BaseClass0,class BaseClass1>::Method4()
		calli default string(class G3_C1913`1<class BaseClass1>)
		ldstr "G2_C832::Method4.12651()"
		ldstr "class G2_C832`2<class BaseClass0,class BaseClass1> on type class G3_C1913`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C832`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C832`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1913`1<class BaseClass1>)
		ldstr "G2_C832::Method7.12650<System.Object>()"
		ldstr "class G2_C832`2<class BaseClass0,class BaseClass1> on type class G3_C1913`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1913`1<class BaseClass1>)
		ldstr "G3_C1913::Method7.18682<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G3_C1913`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1913`1<class BaseClass1>)
		ldstr "G3_C1913::Method7.18682<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1913`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1913`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1913`1<class BaseClass1>::ClassMethod5219<object>()
		calli default string(class G3_C1913`1<class BaseClass1>)
		ldstr "G3_C1913::ClassMethod5219.18684<System.Object>()"
		ldstr "class G3_C1913`1<class BaseClass1> on type class G3_C1913`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1913`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1913`1<class BaseClass1>::ClassMethod5218()
		calli default string(class G3_C1913`1<class BaseClass1>)
		ldstr "G3_C1913::ClassMethod5218.18683()"
		ldstr "class G3_C1913`1<class BaseClass1> on type class G3_C1913`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1913`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1913`1<class BaseClass1>::Method7<object>()
		calli default string(class G3_C1913`1<class BaseClass1>)
		ldstr "G3_C1913::Method7.18682<System.Object>()"
		ldstr "class G3_C1913`1<class BaseClass1> on type class G3_C1913`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1913`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1913`1<class BaseClass1>::ClassMethod3045<object>()
		calli default string(class G3_C1913`1<class BaseClass1>)
		ldstr "G3_C1913::ClassMethod3045.MI.18686<System.Object>()"
		ldstr "class G3_C1913`1<class BaseClass1> on type class G3_C1913`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1913`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1913`1<class BaseClass1>::ClassMethod3044<object>()
		calli default string(class G3_C1913`1<class BaseClass1>)
		ldstr "G2_C832::ClassMethod3044.12657<System.Object>()"
		ldstr "class G3_C1913`1<class BaseClass1> on type class G3_C1913`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1913`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1913`1<class BaseClass1>::ClassMethod3043()
		calli default string(class G3_C1913`1<class BaseClass1>)
		ldstr "G3_C1913::ClassMethod3043.MI.18685()"
		ldstr "class G3_C1913`1<class BaseClass1> on type class G3_C1913`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1913`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1913`1<class BaseClass1>::Method6<object>()
		calli default string(class G3_C1913`1<class BaseClass1>)
		ldstr "G2_C832::Method6.12654<System.Object>()"
		ldstr "class G3_C1913`1<class BaseClass1> on type class G3_C1913`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1913`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1913`1<class BaseClass1>::Method5()
		calli default string(class G3_C1913`1<class BaseClass1>)
		ldstr "G2_C832::Method5.12653()"
		ldstr "class G3_C1913`1<class BaseClass1> on type class G3_C1913`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1913`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1913`1<class BaseClass1>::Method4()
		calli default string(class G3_C1913`1<class BaseClass1>)
		ldstr "G2_C832::Method4.12651()"
		ldstr "class G3_C1913`1<class BaseClass1> on type class G3_C1913`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C832`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method6<object>()
		calli default string(class G2_C832`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G2_C832`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method5()
		calli default string(class G2_C832`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C832::Method5.12653()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G2_C832`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method4()
		calli default string(class G2_C832`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G2_C832`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C832`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C15::Method7.4885<System.Object>()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G2_C832`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C832`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C832::Method7.12650<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C832`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G2_C832`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C832::Method4.MI.12652()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C832`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G2_C832`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C832::Method5.12653()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C832`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G2_C832`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C832::Method6.MI.12655<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C832`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C832`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C832::Method7.12650<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C832`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C832`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C832`2<class BaseClass0,class BaseClass0>::ClassMethod3045<object>()
		calli default string(class G2_C832`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C832::ClassMethod3045.12658<System.Object>()"
		ldstr "class G2_C832`2<class BaseClass0,class BaseClass0> on type class G2_C832`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C832`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C832`2<class BaseClass0,class BaseClass0>::ClassMethod3044<object>()
		calli default string(class G2_C832`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C832::ClassMethod3044.12657<System.Object>()"
		ldstr "class G2_C832`2<class BaseClass0,class BaseClass0> on type class G2_C832`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C832`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C832`2<class BaseClass0,class BaseClass0>::ClassMethod3043()
		calli default string(class G2_C832`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C832::ClassMethod3043.12656()"
		ldstr "class G2_C832`2<class BaseClass0,class BaseClass0> on type class G2_C832`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C832`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C832`2<class BaseClass0,class BaseClass0>::Method6<object>()
		calli default string(class G2_C832`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C832::Method6.12654<System.Object>()"
		ldstr "class G2_C832`2<class BaseClass0,class BaseClass0> on type class G2_C832`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C832`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C832`2<class BaseClass0,class BaseClass0>::Method5()
		calli default string(class G2_C832`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C832::Method5.12653()"
		ldstr "class G2_C832`2<class BaseClass0,class BaseClass0> on type class G2_C832`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C832`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C832`2<class BaseClass0,class BaseClass0>::Method4()
		calli default string(class G2_C832`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C832::Method4.12651()"
		ldstr "class G2_C832`2<class BaseClass0,class BaseClass0> on type class G2_C832`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C832`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C832`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C832`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C832::Method7.12650<System.Object>()"
		ldstr "class G2_C832`2<class BaseClass0,class BaseClass0> on type class G2_C832`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G2_C832`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C832::Method7.12650<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G2_C832`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C832`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C832::Method7.12650<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C832`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C832`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method6<object>()
		calli default string(class G2_C832`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G2_C832`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method5()
		calli default string(class G2_C832`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C832::Method5.12653()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G2_C832`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method4()
		calli default string(class G2_C832`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G2_C832`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C832`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C15::Method7.4885<System.Object>()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G2_C832`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C832`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C832::Method7.12650<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C832`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G2_C832`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C832::Method4.MI.12652()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C832`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G2_C832`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C832::Method5.12653()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C832`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G2_C832`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C832::Method6.MI.12655<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C832`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C832`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C832::Method7.12650<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C832`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C832`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C832`2<class BaseClass0,class BaseClass1>::ClassMethod3045<object>()
		calli default string(class G2_C832`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C832::ClassMethod3045.12658<System.Object>()"
		ldstr "class G2_C832`2<class BaseClass0,class BaseClass1> on type class G2_C832`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C832`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C832`2<class BaseClass0,class BaseClass1>::ClassMethod3044<object>()
		calli default string(class G2_C832`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C832::ClassMethod3044.12657<System.Object>()"
		ldstr "class G2_C832`2<class BaseClass0,class BaseClass1> on type class G2_C832`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C832`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C832`2<class BaseClass0,class BaseClass1>::ClassMethod3043()
		calli default string(class G2_C832`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C832::ClassMethod3043.12656()"
		ldstr "class G2_C832`2<class BaseClass0,class BaseClass1> on type class G2_C832`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C832`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C832`2<class BaseClass0,class BaseClass1>::Method6<object>()
		calli default string(class G2_C832`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C832::Method6.12654<System.Object>()"
		ldstr "class G2_C832`2<class BaseClass0,class BaseClass1> on type class G2_C832`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C832`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C832`2<class BaseClass0,class BaseClass1>::Method5()
		calli default string(class G2_C832`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C832::Method5.12653()"
		ldstr "class G2_C832`2<class BaseClass0,class BaseClass1> on type class G2_C832`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C832`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C832`2<class BaseClass0,class BaseClass1>::Method4()
		calli default string(class G2_C832`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C832::Method4.12651()"
		ldstr "class G2_C832`2<class BaseClass0,class BaseClass1> on type class G2_C832`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C832`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C832`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C832`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C832::Method7.12650<System.Object>()"
		ldstr "class G2_C832`2<class BaseClass0,class BaseClass1> on type class G2_C832`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G2_C832`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C832::Method7.12650<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G2_C832`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C832`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C832::Method7.12650<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C832`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C832`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method6<object>()
		calli default string(class G2_C832`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G2_C832`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method5()
		calli default string(class G2_C832`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C832::Method5.12653()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G2_C832`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method4()
		calli default string(class G2_C832`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G2_C832`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C832`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C15::Method7.4885<System.Object>()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G2_C832`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C832`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C832::Method7.12650<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C832`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G2_C832`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C832::Method4.MI.12652()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C832`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G2_C832`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C832::Method5.12653()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C832`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G2_C832`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C832::Method6.MI.12655<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C832`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C832`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C832::Method7.12650<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C832`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C832`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C832`2<class BaseClass1,class BaseClass0>::ClassMethod3045<object>()
		calli default string(class G2_C832`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C832::ClassMethod3045.12658<System.Object>()"
		ldstr "class G2_C832`2<class BaseClass1,class BaseClass0> on type class G2_C832`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C832`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C832`2<class BaseClass1,class BaseClass0>::ClassMethod3044<object>()
		calli default string(class G2_C832`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C832::ClassMethod3044.12657<System.Object>()"
		ldstr "class G2_C832`2<class BaseClass1,class BaseClass0> on type class G2_C832`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C832`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C832`2<class BaseClass1,class BaseClass0>::ClassMethod3043()
		calli default string(class G2_C832`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C832::ClassMethod3043.12656()"
		ldstr "class G2_C832`2<class BaseClass1,class BaseClass0> on type class G2_C832`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C832`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C832`2<class BaseClass1,class BaseClass0>::Method6<object>()
		calli default string(class G2_C832`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C832::Method6.12654<System.Object>()"
		ldstr "class G2_C832`2<class BaseClass1,class BaseClass0> on type class G2_C832`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C832`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C832`2<class BaseClass1,class BaseClass0>::Method5()
		calli default string(class G2_C832`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C832::Method5.12653()"
		ldstr "class G2_C832`2<class BaseClass1,class BaseClass0> on type class G2_C832`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C832`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C832`2<class BaseClass1,class BaseClass0>::Method4()
		calli default string(class G2_C832`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C832::Method4.12651()"
		ldstr "class G2_C832`2<class BaseClass1,class BaseClass0> on type class G2_C832`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C832`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C832`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G2_C832`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C832::Method7.12650<System.Object>()"
		ldstr "class G2_C832`2<class BaseClass1,class BaseClass0> on type class G2_C832`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G2_C832`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C832::Method4.MI.12652()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C832`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G2_C832`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C832::Method5.12653()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C832`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G2_C832`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C832::Method6.MI.12655<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C832`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C832`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method6<object>()
		calli default string(class G2_C832`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G2_C832`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method5()
		calli default string(class G2_C832`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C832::Method5.12653()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G2_C832`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method4()
		calli default string(class G2_C832`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G2_C832`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C15`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C15`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C832`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C15::Method7.4885<System.Object>()"
		ldstr "class G1_C15`2<class BaseClass1,class BaseClass1> on type class G2_C832`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C832`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C832::Method7.12650<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C832`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G2_C832`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C832::Method4.MI.12652()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C832`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G2_C832`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C832::Method5.12653()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C832`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G2_C832`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C832::Method6.MI.12655<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C832`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C832`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C832::Method7.12650<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C832`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C832`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C832`2<class BaseClass1,class BaseClass1>::ClassMethod3045<object>()
		calli default string(class G2_C832`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C832::ClassMethod3045.12658<System.Object>()"
		ldstr "class G2_C832`2<class BaseClass1,class BaseClass1> on type class G2_C832`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C832`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C832`2<class BaseClass1,class BaseClass1>::ClassMethod3044<object>()
		calli default string(class G2_C832`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C832::ClassMethod3044.12657<System.Object>()"
		ldstr "class G2_C832`2<class BaseClass1,class BaseClass1> on type class G2_C832`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C832`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C832`2<class BaseClass1,class BaseClass1>::ClassMethod3043()
		calli default string(class G2_C832`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C832::ClassMethod3043.12656()"
		ldstr "class G2_C832`2<class BaseClass1,class BaseClass1> on type class G2_C832`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C832`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C832`2<class BaseClass1,class BaseClass1>::Method6<object>()
		calli default string(class G2_C832`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C832::Method6.12654<System.Object>()"
		ldstr "class G2_C832`2<class BaseClass1,class BaseClass1> on type class G2_C832`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C832`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C832`2<class BaseClass1,class BaseClass1>::Method5()
		calli default string(class G2_C832`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C832::Method5.12653()"
		ldstr "class G2_C832`2<class BaseClass1,class BaseClass1> on type class G2_C832`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C832`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C832`2<class BaseClass1,class BaseClass1>::Method4()
		calli default string(class G2_C832`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C832::Method4.12651()"
		ldstr "class G2_C832`2<class BaseClass1,class BaseClass1> on type class G2_C832`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C832`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C832`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C832`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C832::Method7.12650<System.Object>()"
		ldstr "class G2_C832`2<class BaseClass1,class BaseClass1> on type class G2_C832`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G2_C832`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C832::Method4.MI.12652()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C832`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G2_C832`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C832::Method5.12653()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C832`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G2_C832`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C832::Method6.MI.12655<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C832`2<class BaseClass1,class BaseClass1>"
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
		call void Generated1441::MethodCallingTest()
		call void Generated1441::ConstrainedCallsTest()
		call void Generated1441::StructConstrainedInterfaceCallsTest()
		call void Generated1441::CalliTest()
		ldc.i4 100
		ret
	}
}
