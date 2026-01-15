// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.class public Generated1411.BaseClass0
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed {
		ldarg.0
		call instance void [mscorlib]System.Object::.ctor()
		ret
	}
}
.class public Generated1411.BaseClass1
		extends Generated1411.BaseClass0
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed {
		ldarg.0
		call instance void Generated1411.BaseClass0::.ctor()
		ret
	}
}
.class public Generated1411.G3_C1883`1<T0>
		extends class Generated1411.G2_C812`2<!T0,class Generated1411.BaseClass1>
{
	.method public hidebysig newslot virtual instance string ClassMethod5160() cil managed noinlining {
		ldstr "G3_C1883::ClassMethod5160.18527()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod5161<M0>() cil managed noinlining {
		ldstr "G3_C1883::ClassMethod5161.18528<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod5162<M0>() cil managed noinlining {
		ldstr "G3_C1883::ClassMethod5162.18529<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C812<T0,class Generated1411.BaseClass1>.ClassMethod3006'() cil managed noinlining {
		.override method instance string class Generated1411.G2_C812`2<!T0,class Generated1411.BaseClass1>::ClassMethod3006()
		ldstr "G3_C1883::ClassMethod3006.MI.18530()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C812<T0,class Generated1411.BaseClass1>.ClassMethod3008'<M0>() cil managed noinlining {
		.override method instance string class Generated1411.G2_C812`2<!T0,class Generated1411.BaseClass1>::ClassMethod3008<[1]>()
		ldstr "G3_C1883::ClassMethod3008.MI.18531<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed {
		ldarg.0
		call instance void class Generated1411.G2_C812`2<!T0,class Generated1411.BaseClass1>::.ctor()
		ret
	}
}
.class public Generated1411.G2_C812`2<T0, T1>
		extends class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>
		implements class Generated1411.IBase1`1<!T0>, class Generated1411.IBase2`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>
{
	.method public hidebysig newslot virtual instance string Method4() cil managed noinlining {
		ldstr "G2_C812::Method4.12493()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<T0>.Method4'() cil managed noinlining {
		.override method instance string class Generated1411.IBase1`1<!T0>::Method4()
		ldstr "G2_C812::Method4.MI.12494()"
		ret
	}
	.method public hidebysig virtual instance string Method5() cil managed noinlining {
		ldstr "G2_C812::Method5.12495()"
		ret
	}
	.method public hidebysig virtual instance string Method6<M0>() cil managed noinlining {
		ldstr "G2_C812::Method6.12496<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig virtual instance string Method7<M0>() cil managed noinlining {
		ldstr "G2_C812::Method7.12497<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod3006() cil managed noinlining {
		ldstr "G2_C812::ClassMethod3006.12498()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod3007() cil managed noinlining {
		ldstr "G2_C812::ClassMethod3007.12499()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod3008<M0>() cil managed noinlining {
		ldstr "G2_C812::ClassMethod3008.12500<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed {
		ldarg.0
		call instance void class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::.ctor()
		ret
	}
}
.class public abstract Generated1411.G1_C15`2<T0, T1>
		implements class Generated1411.IBase2`2<!T1,!T1>, class Generated1411.IBase1`1<class Generated1411.BaseClass0>
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
		.override method instance string class Generated1411.IBase2`2<!T1,!T1>::Method7<[1]>()
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
	.method public hidebysig newslot virtual instance string 'IBase1<class Generated1411.BaseClass0>.Method4'() cil managed noinlining {
		.override method instance string class Generated1411.IBase1`1<class Generated1411.BaseClass0>::Method4()
		ldstr "G1_C15::Method4.MI.4888()"
		ret
	}
	.method public hidebysig virtual instance string Method5() cil managed noinlining {
		ldstr "G1_C15::Method5.4889()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class Generated1411.BaseClass0>.Method5'() cil managed noinlining {
		.override method instance string class Generated1411.IBase1`1<class Generated1411.BaseClass0>::Method5()
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
	.method public hidebysig newslot virtual instance string 'IBase1<class Generated1411.BaseClass0>.Method6'<M0>() cil managed noinlining {
		.override method instance string class Generated1411.IBase1`1<class Generated1411.BaseClass0>::Method6<[1]>()
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
.class interface public abstract Generated1411.IBase1`1<+T0>
{
	.method public hidebysig newslot abstract virtual instance string Method4() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method5() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method6<M0>() cil managed { }
}
.class interface public abstract Generated1411.IBase2`2<+T0, -T1>
{
	.method public hidebysig newslot abstract virtual instance string Method7<M0>() cil managed { }
}
.class public auto ansi beforefieldinit Generated1411.Generated1411 {
	.method static void M.BaseClass0<(Generated1411.BaseClass0)W>(!!W inst, string exp) cil managed {
		.maxstack 5
		.locals init (string[] actualResults)
		ldc.i4.s    0
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.BaseClass0<(Generated1411.BaseClass0)W>(!!W inst, string exp)"
		ldc.i4.s 0
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.BaseClass1<(Generated1411.BaseClass1)W>(!!W inst, string exp) cil managed {
		.maxstack 5
		.locals init (string[] actualResults)
		ldc.i4.s    0
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.BaseClass1<(Generated1411.BaseClass1)W>(!!W inst, string exp)"
		ldc.i4.s 0
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1883.T<T0,(class Generated1411.G3_C1883`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 15
		.locals init (string[] actualResults)
		ldc.i4.s    10
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1883.T<T0,(class Generated1411.G3_C1883`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 10
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G3_C1883`1<!!T0>::ClassMethod3006()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G3_C1883`1<!!T0>::ClassMethod3007()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G3_C1883`1<!!T0>::ClassMethod3008<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G3_C1883`1<!!T0>::ClassMethod5160()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G3_C1883`1<!!T0>::ClassMethod5161<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G3_C1883`1<!!T0>::ClassMethod5162<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G3_C1883`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G3_C1883`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G3_C1883`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G3_C1883`1<!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1883.A<(class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 15
		.locals init (string[] actualResults)
		ldc.i4.s    10
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1883.A<(class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 10
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>::ClassMethod3006()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>::ClassMethod3007()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>::ClassMethod3008<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>::ClassMethod5160()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>::ClassMethod5161<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>::ClassMethod5162<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1883.B<(class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 15
		.locals init (string[] actualResults)
		ldc.i4.s    10
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1883.B<(class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 10
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>::ClassMethod3006()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>::ClassMethod3007()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>::ClassMethod3008<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>::ClassMethod5160()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>::ClassMethod5161<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>::ClassMethod5162<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C812.T.T<T0,T1,(class Generated1411.G2_C812`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C812.T.T<T0,T1,(class Generated1411.G2_C812`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G2_C812`2<!!T0,!!T1>::ClassMethod3006()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G2_C812`2<!!T0,!!T1>::ClassMethod3007()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G2_C812`2<!!T0,!!T1>::ClassMethod3008<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G2_C812`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G2_C812`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G2_C812`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G2_C812`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C812.A.T<T1,(class Generated1411.G2_C812`2<class Generated1411.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C812.A.T<T1,(class Generated1411.G2_C812`2<class Generated1411.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass0,!!T1>::ClassMethod3006()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass0,!!T1>::ClassMethod3007()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass0,!!T1>::ClassMethod3008<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C812.A.A<(class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C812.A.A<(class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>::ClassMethod3006()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>::ClassMethod3007()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>::ClassMethod3008<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C812.A.B<(class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C812.A.B<(class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::ClassMethod3006()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::ClassMethod3007()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::ClassMethod3008<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C812.B.T<T1,(class Generated1411.G2_C812`2<class Generated1411.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C812.B.T<T1,(class Generated1411.G2_C812`2<class Generated1411.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass1,!!T1>::ClassMethod3006()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass1,!!T1>::ClassMethod3007()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass1,!!T1>::ClassMethod3008<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C812.B.A<(class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C812.B.A<(class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>::ClassMethod3006()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>::ClassMethod3007()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>::ClassMethod3008<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C812.B.B<(class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C812.B.B<(class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>::ClassMethod3006()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>::ClassMethod3007()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>::ClassMethod3008<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C15.T.T<T0,T1,(class Generated1411.G1_C15`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 9
		.locals init (string[] actualResults)
		ldc.i4.s    4
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C15.T.T<T0,T1,(class Generated1411.G1_C15`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 4
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G1_C15`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G1_C15`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G1_C15`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G1_C15`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C15.A.T<T1,(class Generated1411.G1_C15`2<class Generated1411.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 9
		.locals init (string[] actualResults)
		ldc.i4.s    4
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C15.A.T<T1,(class Generated1411.G1_C15`2<class Generated1411.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 4
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G1_C15`2<class Generated1411.BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G1_C15`2<class Generated1411.BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G1_C15`2<class Generated1411.BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G1_C15`2<class Generated1411.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C15.A.A<(class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 9
		.locals init (string[] actualResults)
		ldc.i4.s    4
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C15.A.A<(class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 4
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C15.A.B<(class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 9
		.locals init (string[] actualResults)
		ldc.i4.s    4
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C15.A.B<(class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 4
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C15.B.T<T1,(class Generated1411.G1_C15`2<class Generated1411.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 9
		.locals init (string[] actualResults)
		ldc.i4.s    4
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C15.B.T<T1,(class Generated1411.G1_C15`2<class Generated1411.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 4
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G1_C15`2<class Generated1411.BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G1_C15`2<class Generated1411.BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G1_C15`2<class Generated1411.BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G1_C15`2<class Generated1411.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C15.B.A<(class Generated1411.G1_C15`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 9
		.locals init (string[] actualResults)
		ldc.i4.s    4
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C15.B.A<(class Generated1411.G1_C15`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 4
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G1_C15`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G1_C15`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G1_C15`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G1_C15`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C15.B.B<(class Generated1411.G1_C15`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 9
		.locals init (string[] actualResults)
		ldc.i4.s    4
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C15.B.B<(class Generated1411.G1_C15`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 4
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G1_C15`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G1_C15`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G1_C15`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.G1_C15`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.T<T0,(class Generated1411.IBase1`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.T<T0,(class Generated1411.IBase1`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.IBase1`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.IBase1`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.IBase1`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.A<(class Generated1411.IBase1`1<class Generated1411.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.A<(class Generated1411.IBase1`1<class Generated1411.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.IBase1`1<class Generated1411.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.IBase1`1<class Generated1411.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.IBase1`1<class Generated1411.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.B<(class Generated1411.IBase1`1<class Generated1411.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.B<(class Generated1411.IBase1`1<class Generated1411.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.IBase1`1<class Generated1411.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.IBase1`1<class Generated1411.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.IBase1`1<class Generated1411.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.T.T<T0,T1,(class Generated1411.IBase2`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.T.T<T0,T1,(class Generated1411.IBase2`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.IBase2`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.T<T1,(class Generated1411.IBase2`2<class Generated1411.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.T<T1,(class Generated1411.IBase2`2<class Generated1411.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.IBase2`2<class Generated1411.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.A<(class Generated1411.IBase2`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.A<(class Generated1411.IBase2`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.IBase2`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.B<(class Generated1411.IBase2`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.B<(class Generated1411.IBase2`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.IBase2`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.T<T1,(class Generated1411.IBase2`2<class Generated1411.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.T<T1,(class Generated1411.IBase2`2<class Generated1411.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.IBase2`2<class Generated1411.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.A<(class Generated1411.IBase2`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.A<(class Generated1411.IBase2`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.IBase2`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.B<(class Generated1411.IBase2`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.B<(class Generated1411.IBase2`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1411.IBase2`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>::Method7<object>()
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
		newobj instance void class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>
		callvirt instance string class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::Method6<object>()
		ldstr "G2_C812::Method6.12496<System.Object>()"
		ldstr "class G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>
		callvirt instance string class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::Method5()
		ldstr "G2_C812::Method5.12495()"
		ldstr "class G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>
		callvirt instance string class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::Method4()
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>
		callvirt instance string class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::Method7<object>()
		ldstr "G2_C812::Method7.12497<System.Object>()"
		ldstr "class G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1411.IBase2`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>::Method7<object>()
		ldstr "G2_C812::Method7.12497<System.Object>()"
		ldstr "class IBase2`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1411.IBase1`1<class Generated1411.BaseClass0>::Method4()
		ldstr "G2_C812::Method4.MI.12494()"
		ldstr "class IBase1`1<class Generated1411.BaseClass0> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1411.IBase1`1<class Generated1411.BaseClass0>::Method5()
		ldstr "G2_C812::Method5.12495()"
		ldstr "class IBase1`1<class Generated1411.BaseClass0> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1411.IBase1`1<class Generated1411.BaseClass0>::Method6<object>()
		ldstr "G2_C812::Method6.12496<System.Object>()"
		ldstr "class IBase1`1<class Generated1411.BaseClass0> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1411.IBase2`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::Method7<object>()
		ldstr "G2_C812::Method7.12497<System.Object>()"
		ldstr "class IBase2`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>
		callvirt instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::ClassMethod3008<object>()
		ldstr "G3_C1883::ClassMethod3008.MI.18531<System.Object>()"
		ldstr "class G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>
		callvirt instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::ClassMethod3007()
		ldstr "G2_C812::ClassMethod3007.12499()"
		ldstr "class G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>
		callvirt instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::ClassMethod3006()
		ldstr "G3_C1883::ClassMethod3006.MI.18530()"
		ldstr "class G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>
		callvirt instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::Method7<object>()
		ldstr "G2_C812::Method7.12497<System.Object>()"
		ldstr "class G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>
		callvirt instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::Method6<object>()
		ldstr "G2_C812::Method6.12496<System.Object>()"
		ldstr "class G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>
		callvirt instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::Method5()
		ldstr "G2_C812::Method5.12495()"
		ldstr "class G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>
		callvirt instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::Method4()
		ldstr "G2_C812::Method4.12493()"
		ldstr "class G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1411.IBase2`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>::Method7<object>()
		ldstr "G2_C812::Method7.12497<System.Object>()"
		ldstr "class IBase2`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1411.IBase2`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>::Method7<object>()
		ldstr "G2_C812::Method7.12497<System.Object>()"
		ldstr "class IBase2`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>
		callvirt instance string class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>::ClassMethod5162<object>()
		ldstr "G3_C1883::ClassMethod5162.18529<System.Object>()"
		ldstr "class G3_C1883`1<class Generated1411.BaseClass0> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>
		callvirt instance string class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>::ClassMethod5161<object>()
		ldstr "G3_C1883::ClassMethod5161.18528<System.Object>()"
		ldstr "class G3_C1883`1<class Generated1411.BaseClass0> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>
		callvirt instance string class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>::ClassMethod5160()
		ldstr "G3_C1883::ClassMethod5160.18527()"
		ldstr "class G3_C1883`1<class Generated1411.BaseClass0> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>
		callvirt instance string class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>::ClassMethod3008<object>()
		ldstr "G3_C1883::ClassMethod3008.MI.18531<System.Object>()"
		ldstr "class G3_C1883`1<class Generated1411.BaseClass0> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>
		callvirt instance string class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>::ClassMethod3007()
		ldstr "G2_C812::ClassMethod3007.12499()"
		ldstr "class G3_C1883`1<class Generated1411.BaseClass0> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>
		callvirt instance string class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>::ClassMethod3006()
		ldstr "G3_C1883::ClassMethod3006.MI.18530()"
		ldstr "class G3_C1883`1<class Generated1411.BaseClass0> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>
		callvirt instance string class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>::Method7<object>()
		ldstr "G2_C812::Method7.12497<System.Object>()"
		ldstr "class G3_C1883`1<class Generated1411.BaseClass0> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>
		callvirt instance string class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>::Method6<object>()
		ldstr "G2_C812::Method6.12496<System.Object>()"
		ldstr "class G3_C1883`1<class Generated1411.BaseClass0> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>
		callvirt instance string class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>::Method5()
		ldstr "G2_C812::Method5.12495()"
		ldstr "class G3_C1883`1<class Generated1411.BaseClass0> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>
		callvirt instance string class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>::Method4()
		ldstr "G2_C812::Method4.12493()"
		ldstr "class G3_C1883`1<class Generated1411.BaseClass0> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>
		callvirt instance string class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::Method6<object>()
		ldstr "G2_C812::Method6.12496<System.Object>()"
		ldstr "class G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>
		callvirt instance string class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::Method5()
		ldstr "G2_C812::Method5.12495()"
		ldstr "class G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>
		callvirt instance string class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::Method4()
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>
		callvirt instance string class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::Method7<object>()
		ldstr "G2_C812::Method7.12497<System.Object>()"
		ldstr "class G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1411.IBase2`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>::Method7<object>()
		ldstr "G2_C812::Method7.12497<System.Object>()"
		ldstr "class IBase2`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1411.IBase1`1<class Generated1411.BaseClass0>::Method4()
		ldstr "G2_C812::Method4.MI.12494()"
		ldstr "class IBase1`1<class Generated1411.BaseClass0> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1411.IBase1`1<class Generated1411.BaseClass0>::Method5()
		ldstr "G2_C812::Method5.12495()"
		ldstr "class IBase1`1<class Generated1411.BaseClass0> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1411.IBase1`1<class Generated1411.BaseClass0>::Method6<object>()
		ldstr "G2_C812::Method6.12496<System.Object>()"
		ldstr "class IBase1`1<class Generated1411.BaseClass0> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1411.IBase2`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::Method7<object>()
		ldstr "G2_C812::Method7.12497<System.Object>()"
		ldstr "class IBase2`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>
		callvirt instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>::ClassMethod3008<object>()
		ldstr "G3_C1883::ClassMethod3008.MI.18531<System.Object>()"
		ldstr "class G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>
		callvirt instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>::ClassMethod3007()
		ldstr "G2_C812::ClassMethod3007.12499()"
		ldstr "class G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>
		callvirt instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>::ClassMethod3006()
		ldstr "G3_C1883::ClassMethod3006.MI.18530()"
		ldstr "class G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>
		callvirt instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>::Method7<object>()
		ldstr "G2_C812::Method7.12497<System.Object>()"
		ldstr "class G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>
		callvirt instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>::Method6<object>()
		ldstr "G2_C812::Method6.12496<System.Object>()"
		ldstr "class G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>
		callvirt instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>::Method5()
		ldstr "G2_C812::Method5.12495()"
		ldstr "class G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>
		callvirt instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>::Method4()
		ldstr "G2_C812::Method4.12493()"
		ldstr "class G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1411.IBase1`1<class Generated1411.BaseClass1>::Method4()
		ldstr "G2_C812::Method4.MI.12494()"
		ldstr "class IBase1`1<class Generated1411.BaseClass1> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1411.IBase1`1<class Generated1411.BaseClass1>::Method5()
		ldstr "G2_C812::Method5.12495()"
		ldstr "class IBase1`1<class Generated1411.BaseClass1> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1411.IBase1`1<class Generated1411.BaseClass1>::Method6<object>()
		ldstr "G2_C812::Method6.12496<System.Object>()"
		ldstr "class IBase1`1<class Generated1411.BaseClass1> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1411.IBase2`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>::Method7<object>()
		ldstr "G2_C812::Method7.12497<System.Object>()"
		ldstr "class IBase2`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1411.IBase2`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>::Method7<object>()
		ldstr "G2_C812::Method7.12497<System.Object>()"
		ldstr "class IBase2`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>
		callvirt instance string class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>::ClassMethod5162<object>()
		ldstr "G3_C1883::ClassMethod5162.18529<System.Object>()"
		ldstr "class G3_C1883`1<class Generated1411.BaseClass1> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>
		callvirt instance string class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>::ClassMethod5161<object>()
		ldstr "G3_C1883::ClassMethod5161.18528<System.Object>()"
		ldstr "class G3_C1883`1<class Generated1411.BaseClass1> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>
		callvirt instance string class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>::ClassMethod5160()
		ldstr "G3_C1883::ClassMethod5160.18527()"
		ldstr "class G3_C1883`1<class Generated1411.BaseClass1> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>
		callvirt instance string class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>::ClassMethod3008<object>()
		ldstr "G3_C1883::ClassMethod3008.MI.18531<System.Object>()"
		ldstr "class G3_C1883`1<class Generated1411.BaseClass1> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>
		callvirt instance string class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>::ClassMethod3007()
		ldstr "G2_C812::ClassMethod3007.12499()"
		ldstr "class G3_C1883`1<class Generated1411.BaseClass1> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>
		callvirt instance string class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>::ClassMethod3006()
		ldstr "G3_C1883::ClassMethod3006.MI.18530()"
		ldstr "class G3_C1883`1<class Generated1411.BaseClass1> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>
		callvirt instance string class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>::Method7<object>()
		ldstr "G2_C812::Method7.12497<System.Object>()"
		ldstr "class G3_C1883`1<class Generated1411.BaseClass1> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>
		callvirt instance string class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>::Method6<object>()
		ldstr "G2_C812::Method6.12496<System.Object>()"
		ldstr "class G3_C1883`1<class Generated1411.BaseClass1> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>
		callvirt instance string class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>::Method5()
		ldstr "G2_C812::Method5.12495()"
		ldstr "class G3_C1883`1<class Generated1411.BaseClass1> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>
		callvirt instance string class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>::Method4()
		ldstr "G2_C812::Method4.12493()"
		ldstr "class G3_C1883`1<class Generated1411.BaseClass1> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>
		callvirt instance string class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::Method6<object>()
		ldstr "G2_C812::Method6.12496<System.Object>()"
		ldstr "class G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>
		callvirt instance string class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::Method5()
		ldstr "G2_C812::Method5.12495()"
		ldstr "class G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>
		callvirt instance string class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::Method4()
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>
		callvirt instance string class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::Method7<object>()
		ldstr "G2_C812::Method7.12497<System.Object>()"
		ldstr "class G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1411.IBase2`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>::Method7<object>()
		ldstr "G2_C812::Method7.12497<System.Object>()"
		ldstr "class IBase2`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1411.IBase1`1<class Generated1411.BaseClass0>::Method4()
		ldstr "G2_C812::Method4.MI.12494()"
		ldstr "class IBase1`1<class Generated1411.BaseClass0> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1411.IBase1`1<class Generated1411.BaseClass0>::Method5()
		ldstr "G2_C812::Method5.12495()"
		ldstr "class IBase1`1<class Generated1411.BaseClass0> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1411.IBase1`1<class Generated1411.BaseClass0>::Method6<object>()
		ldstr "G2_C812::Method6.12496<System.Object>()"
		ldstr "class IBase1`1<class Generated1411.BaseClass0> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1411.IBase2`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::Method7<object>()
		ldstr "G2_C812::Method7.12497<System.Object>()"
		ldstr "class IBase2`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>
		callvirt instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>::ClassMethod3008<object>()
		ldstr "G2_C812::ClassMethod3008.12500<System.Object>()"
		ldstr "class G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>
		callvirt instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>::ClassMethod3007()
		ldstr "G2_C812::ClassMethod3007.12499()"
		ldstr "class G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>
		callvirt instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>::ClassMethod3006()
		ldstr "G2_C812::ClassMethod3006.12498()"
		ldstr "class G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>
		callvirt instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>::Method7<object>()
		ldstr "G2_C812::Method7.12497<System.Object>()"
		ldstr "class G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>
		callvirt instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>::Method6<object>()
		ldstr "G2_C812::Method6.12496<System.Object>()"
		ldstr "class G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>
		callvirt instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>::Method5()
		ldstr "G2_C812::Method5.12495()"
		ldstr "class G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>
		callvirt instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>::Method4()
		ldstr "G2_C812::Method4.12493()"
		ldstr "class G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1411.IBase2`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>::Method7<object>()
		ldstr "G2_C812::Method7.12497<System.Object>()"
		ldstr "class IBase2`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1411.IBase2`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>::Method7<object>()
		ldstr "G2_C812::Method7.12497<System.Object>()"
		ldstr "class IBase2`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>
		callvirt instance string class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::Method6<object>()
		ldstr "G2_C812::Method6.12496<System.Object>()"
		ldstr "class G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>
		callvirt instance string class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::Method5()
		ldstr "G2_C812::Method5.12495()"
		ldstr "class G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>
		callvirt instance string class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::Method4()
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>
		callvirt instance string class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::Method7<object>()
		ldstr "G2_C812::Method7.12497<System.Object>()"
		ldstr "class G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1411.IBase2`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>::Method7<object>()
		ldstr "G2_C812::Method7.12497<System.Object>()"
		ldstr "class IBase2`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1411.IBase1`1<class Generated1411.BaseClass0>::Method4()
		ldstr "G2_C812::Method4.MI.12494()"
		ldstr "class IBase1`1<class Generated1411.BaseClass0> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1411.IBase1`1<class Generated1411.BaseClass0>::Method5()
		ldstr "G2_C812::Method5.12495()"
		ldstr "class IBase1`1<class Generated1411.BaseClass0> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1411.IBase1`1<class Generated1411.BaseClass0>::Method6<object>()
		ldstr "G2_C812::Method6.12496<System.Object>()"
		ldstr "class IBase1`1<class Generated1411.BaseClass0> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1411.IBase2`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::Method7<object>()
		ldstr "G2_C812::Method7.12497<System.Object>()"
		ldstr "class IBase2`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>
		callvirt instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::ClassMethod3008<object>()
		ldstr "G2_C812::ClassMethod3008.12500<System.Object>()"
		ldstr "class G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>
		callvirt instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::ClassMethod3007()
		ldstr "G2_C812::ClassMethod3007.12499()"
		ldstr "class G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>
		callvirt instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::ClassMethod3006()
		ldstr "G2_C812::ClassMethod3006.12498()"
		ldstr "class G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>
		callvirt instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::Method7<object>()
		ldstr "G2_C812::Method7.12497<System.Object>()"
		ldstr "class G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>
		callvirt instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::Method6<object>()
		ldstr "G2_C812::Method6.12496<System.Object>()"
		ldstr "class G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>
		callvirt instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::Method5()
		ldstr "G2_C812::Method5.12495()"
		ldstr "class G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>
		callvirt instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::Method4()
		ldstr "G2_C812::Method4.12493()"
		ldstr "class G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1411.IBase2`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>::Method7<object>()
		ldstr "G2_C812::Method7.12497<System.Object>()"
		ldstr "class IBase2`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1411.IBase2`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>::Method7<object>()
		ldstr "G2_C812::Method7.12497<System.Object>()"
		ldstr "class IBase2`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>
		callvirt instance string class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::Method6<object>()
		ldstr "G2_C812::Method6.12496<System.Object>()"
		ldstr "class G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>
		callvirt instance string class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::Method5()
		ldstr "G2_C812::Method5.12495()"
		ldstr "class G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>
		callvirt instance string class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::Method4()
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>
		callvirt instance string class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::Method7<object>()
		ldstr "G2_C812::Method7.12497<System.Object>()"
		ldstr "class G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1411.IBase2`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>::Method7<object>()
		ldstr "G2_C812::Method7.12497<System.Object>()"
		ldstr "class IBase2`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1411.IBase1`1<class Generated1411.BaseClass0>::Method4()
		ldstr "G2_C812::Method4.MI.12494()"
		ldstr "class IBase1`1<class Generated1411.BaseClass0> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1411.IBase1`1<class Generated1411.BaseClass0>::Method5()
		ldstr "G2_C812::Method5.12495()"
		ldstr "class IBase1`1<class Generated1411.BaseClass0> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1411.IBase1`1<class Generated1411.BaseClass0>::Method6<object>()
		ldstr "G2_C812::Method6.12496<System.Object>()"
		ldstr "class IBase1`1<class Generated1411.BaseClass0> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1411.IBase2`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::Method7<object>()
		ldstr "G2_C812::Method7.12497<System.Object>()"
		ldstr "class IBase2`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>
		callvirt instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>::ClassMethod3008<object>()
		ldstr "G2_C812::ClassMethod3008.12500<System.Object>()"
		ldstr "class G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>
		callvirt instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>::ClassMethod3007()
		ldstr "G2_C812::ClassMethod3007.12499()"
		ldstr "class G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>
		callvirt instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>::ClassMethod3006()
		ldstr "G2_C812::ClassMethod3006.12498()"
		ldstr "class G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>
		callvirt instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>::Method7<object>()
		ldstr "G2_C812::Method7.12497<System.Object>()"
		ldstr "class G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>
		callvirt instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>::Method6<object>()
		ldstr "G2_C812::Method6.12496<System.Object>()"
		ldstr "class G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>
		callvirt instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>::Method5()
		ldstr "G2_C812::Method5.12495()"
		ldstr "class G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>
		callvirt instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>::Method4()
		ldstr "G2_C812::Method4.12493()"
		ldstr "class G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1411.IBase1`1<class Generated1411.BaseClass1>::Method4()
		ldstr "G2_C812::Method4.MI.12494()"
		ldstr "class IBase1`1<class Generated1411.BaseClass1> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1411.IBase1`1<class Generated1411.BaseClass1>::Method5()
		ldstr "G2_C812::Method5.12495()"
		ldstr "class IBase1`1<class Generated1411.BaseClass1> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1411.IBase1`1<class Generated1411.BaseClass1>::Method6<object>()
		ldstr "G2_C812::Method6.12496<System.Object>()"
		ldstr "class IBase1`1<class Generated1411.BaseClass1> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1411.IBase2`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>::Method7<object>()
		ldstr "G2_C812::Method7.12497<System.Object>()"
		ldstr "class IBase2`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1411.IBase2`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>::Method7<object>()
		ldstr "G2_C812::Method7.12497<System.Object>()"
		ldstr "class IBase2`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>
		callvirt instance string class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::Method6<object>()
		ldstr "G2_C812::Method6.12496<System.Object>()"
		ldstr "class G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>
		callvirt instance string class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::Method5()
		ldstr "G2_C812::Method5.12495()"
		ldstr "class G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>
		callvirt instance string class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::Method4()
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>
		callvirt instance string class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::Method7<object>()
		ldstr "G2_C812::Method7.12497<System.Object>()"
		ldstr "class G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1411.IBase2`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>::Method7<object>()
		ldstr "G2_C812::Method7.12497<System.Object>()"
		ldstr "class IBase2`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1411.IBase1`1<class Generated1411.BaseClass0>::Method4()
		ldstr "G2_C812::Method4.MI.12494()"
		ldstr "class IBase1`1<class Generated1411.BaseClass0> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1411.IBase1`1<class Generated1411.BaseClass0>::Method5()
		ldstr "G2_C812::Method5.12495()"
		ldstr "class IBase1`1<class Generated1411.BaseClass0> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1411.IBase1`1<class Generated1411.BaseClass0>::Method6<object>()
		ldstr "G2_C812::Method6.12496<System.Object>()"
		ldstr "class IBase1`1<class Generated1411.BaseClass0> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1411.IBase2`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::Method7<object>()
		ldstr "G2_C812::Method7.12497<System.Object>()"
		ldstr "class IBase2`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>
		callvirt instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>::ClassMethod3008<object>()
		ldstr "G2_C812::ClassMethod3008.12500<System.Object>()"
		ldstr "class G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>
		callvirt instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>::ClassMethod3007()
		ldstr "G2_C812::ClassMethod3007.12499()"
		ldstr "class G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>
		callvirt instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>::ClassMethod3006()
		ldstr "G2_C812::ClassMethod3006.12498()"
		ldstr "class G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>
		callvirt instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>::Method7<object>()
		ldstr "G2_C812::Method7.12497<System.Object>()"
		ldstr "class G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>
		callvirt instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>::Method6<object>()
		ldstr "G2_C812::Method6.12496<System.Object>()"
		ldstr "class G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>
		callvirt instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>::Method5()
		ldstr "G2_C812::Method5.12495()"
		ldstr "class G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>
		callvirt instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>::Method4()
		ldstr "G2_C812::Method4.12493()"
		ldstr "class G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1411.IBase1`1<class Generated1411.BaseClass1>::Method4()
		ldstr "G2_C812::Method4.MI.12494()"
		ldstr "class IBase1`1<class Generated1411.BaseClass1> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1411.IBase1`1<class Generated1411.BaseClass1>::Method5()
		ldstr "G2_C812::Method5.12495()"
		ldstr "class IBase1`1<class Generated1411.BaseClass1> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1411.IBase1`1<class Generated1411.BaseClass1>::Method6<object>()
		ldstr "G2_C812::Method6.12496<System.Object>()"
		ldstr "class IBase1`1<class Generated1411.BaseClass1> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1411.IBase2`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>::Method7<object>()
		ldstr "G2_C812::Method7.12497<System.Object>()"
		ldstr "class IBase2`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1411.IBase2`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>::Method7<object>()
		ldstr "G2_C812::Method7.12497<System.Object>()"
		ldstr "class IBase2`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>"
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
		newobj instance void class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G2_C812::Method5.12495()#G2_C812::Method6.12496<System.Object>()#G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.G1_C15.T.T<class Generated1411.BaseClass0,class Generated1411.BaseClass1,class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G2_C812::Method5.12495()#G2_C812::Method6.12496<System.Object>()#G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.G1_C15.A.T<class Generated1411.BaseClass1,class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G2_C812::Method5.12495()#G2_C812::Method6.12496<System.Object>()#G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.G1_C15.A.B<class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.IBase2.T.T<class Generated1411.BaseClass1,class Generated1411.BaseClass1,class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.IBase2.B.T<class Generated1411.BaseClass1,class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.IBase2.B.B<class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C812::Method4.MI.12494()#G2_C812::Method5.12495()#G2_C812::Method6.12496<System.Object>()#"
		call void Generated1411.Generated1411::M.IBase1.T<class Generated1411.BaseClass0,class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C812::Method4.MI.12494()#G2_C812::Method5.12495()#G2_C812::Method6.12496<System.Object>()#"
		call void Generated1411.Generated1411::M.IBase1.A<class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.IBase2.T.T<class Generated1411.BaseClass0,class Generated1411.BaseClass1,class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.IBase2.A.T<class Generated1411.BaseClass1,class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.IBase2.A.B<class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1883::ClassMethod3006.MI.18530()#G2_C812::ClassMethod3007.12499()#G3_C1883::ClassMethod3008.MI.18531<System.Object>()#G2_C812::Method4.12493()#G2_C812::Method5.12495()#G2_C812::Method6.12496<System.Object>()#G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.G2_C812.T.T<class Generated1411.BaseClass0,class Generated1411.BaseClass1,class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1883::ClassMethod3006.MI.18530()#G2_C812::ClassMethod3007.12499()#G3_C1883::ClassMethod3008.MI.18531<System.Object>()#G2_C812::Method4.12493()#G2_C812::Method5.12495()#G2_C812::Method6.12496<System.Object>()#G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.G2_C812.A.T<class Generated1411.BaseClass1,class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1883::ClassMethod3006.MI.18530()#G2_C812::ClassMethod3007.12499()#G3_C1883::ClassMethod3008.MI.18531<System.Object>()#G2_C812::Method4.12493()#G2_C812::Method5.12495()#G2_C812::Method6.12496<System.Object>()#G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.G2_C812.A.B<class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.IBase2.T.T<class Generated1411.BaseClass1,class Generated1411.BaseClass0,class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.IBase2.B.T<class Generated1411.BaseClass0,class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.IBase2.B.A<class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.IBase2.T.T<class Generated1411.BaseClass0,class Generated1411.BaseClass0,class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.IBase2.A.T<class Generated1411.BaseClass0,class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.IBase2.A.A<class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1883::ClassMethod3006.MI.18530()#G2_C812::ClassMethod3007.12499()#G3_C1883::ClassMethod3008.MI.18531<System.Object>()#G3_C1883::ClassMethod5160.18527()#G3_C1883::ClassMethod5161.18528<System.Object>()#G3_C1883::ClassMethod5162.18529<System.Object>()#G2_C812::Method4.12493()#G2_C812::Method5.12495()#G2_C812::Method6.12496<System.Object>()#G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.G3_C1883.T<class Generated1411.BaseClass0,class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1883::ClassMethod3006.MI.18530()#G2_C812::ClassMethod3007.12499()#G3_C1883::ClassMethod3008.MI.18531<System.Object>()#G3_C1883::ClassMethod5160.18527()#G3_C1883::ClassMethod5161.18528<System.Object>()#G3_C1883::ClassMethod5162.18529<System.Object>()#G2_C812::Method4.12493()#G2_C812::Method5.12495()#G2_C812::Method6.12496<System.Object>()#G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.G3_C1883.A<class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>>(!!0,string)
		newobj instance void class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G2_C812::Method5.12495()#G2_C812::Method6.12496<System.Object>()#G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.G1_C15.T.T<class Generated1411.BaseClass0,class Generated1411.BaseClass1,class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G2_C812::Method5.12495()#G2_C812::Method6.12496<System.Object>()#G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.G1_C15.A.T<class Generated1411.BaseClass1,class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G2_C812::Method5.12495()#G2_C812::Method6.12496<System.Object>()#G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.G1_C15.A.B<class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.IBase2.T.T<class Generated1411.BaseClass1,class Generated1411.BaseClass1,class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.IBase2.B.T<class Generated1411.BaseClass1,class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.IBase2.B.B<class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C812::Method4.MI.12494()#G2_C812::Method5.12495()#G2_C812::Method6.12496<System.Object>()#"
		call void Generated1411.Generated1411::M.IBase1.T<class Generated1411.BaseClass0,class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C812::Method4.MI.12494()#G2_C812::Method5.12495()#G2_C812::Method6.12496<System.Object>()#"
		call void Generated1411.Generated1411::M.IBase1.A<class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.IBase2.T.T<class Generated1411.BaseClass0,class Generated1411.BaseClass1,class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.IBase2.A.T<class Generated1411.BaseClass1,class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.IBase2.A.B<class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1883::ClassMethod3006.MI.18530()#G2_C812::ClassMethod3007.12499()#G3_C1883::ClassMethod3008.MI.18531<System.Object>()#G2_C812::Method4.12493()#G2_C812::Method5.12495()#G2_C812::Method6.12496<System.Object>()#G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.G2_C812.T.T<class Generated1411.BaseClass1,class Generated1411.BaseClass1,class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1883::ClassMethod3006.MI.18530()#G2_C812::ClassMethod3007.12499()#G3_C1883::ClassMethod3008.MI.18531<System.Object>()#G2_C812::Method4.12493()#G2_C812::Method5.12495()#G2_C812::Method6.12496<System.Object>()#G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.G2_C812.B.T<class Generated1411.BaseClass1,class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1883::ClassMethod3006.MI.18530()#G2_C812::ClassMethod3007.12499()#G3_C1883::ClassMethod3008.MI.18531<System.Object>()#G2_C812::Method4.12493()#G2_C812::Method5.12495()#G2_C812::Method6.12496<System.Object>()#G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.G2_C812.B.B<class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C812::Method4.MI.12494()#G2_C812::Method5.12495()#G2_C812::Method6.12496<System.Object>()#"
		call void Generated1411.Generated1411::M.IBase1.T<class Generated1411.BaseClass1,class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C812::Method4.MI.12494()#G2_C812::Method5.12495()#G2_C812::Method6.12496<System.Object>()#"
		call void Generated1411.Generated1411::M.IBase1.B<class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.IBase2.T.T<class Generated1411.BaseClass1,class Generated1411.BaseClass0,class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.IBase2.B.T<class Generated1411.BaseClass0,class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.IBase2.B.A<class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.IBase2.T.T<class Generated1411.BaseClass0,class Generated1411.BaseClass0,class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.IBase2.A.T<class Generated1411.BaseClass0,class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.IBase2.A.A<class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1883::ClassMethod3006.MI.18530()#G2_C812::ClassMethod3007.12499()#G3_C1883::ClassMethod3008.MI.18531<System.Object>()#G3_C1883::ClassMethod5160.18527()#G3_C1883::ClassMethod5161.18528<System.Object>()#G3_C1883::ClassMethod5162.18529<System.Object>()#G2_C812::Method4.12493()#G2_C812::Method5.12495()#G2_C812::Method6.12496<System.Object>()#G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.G3_C1883.T<class Generated1411.BaseClass1,class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1883::ClassMethod3006.MI.18530()#G2_C812::ClassMethod3007.12499()#G3_C1883::ClassMethod3008.MI.18531<System.Object>()#G3_C1883::ClassMethod5160.18527()#G3_C1883::ClassMethod5161.18528<System.Object>()#G3_C1883::ClassMethod5162.18529<System.Object>()#G2_C812::Method4.12493()#G2_C812::Method5.12495()#G2_C812::Method6.12496<System.Object>()#G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.G3_C1883.B<class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>>(!!0,string)
		newobj instance void class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G2_C812::Method5.12495()#G2_C812::Method6.12496<System.Object>()#G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.G1_C15.T.T<class Generated1411.BaseClass0,class Generated1411.BaseClass1,class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G2_C812::Method5.12495()#G2_C812::Method6.12496<System.Object>()#G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.G1_C15.A.T<class Generated1411.BaseClass1,class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G2_C812::Method5.12495()#G2_C812::Method6.12496<System.Object>()#G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.G1_C15.A.B<class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.IBase2.T.T<class Generated1411.BaseClass1,class Generated1411.BaseClass1,class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.IBase2.B.T<class Generated1411.BaseClass1,class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.IBase2.B.B<class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C812::Method4.MI.12494()#G2_C812::Method5.12495()#G2_C812::Method6.12496<System.Object>()#"
		call void Generated1411.Generated1411::M.IBase1.T<class Generated1411.BaseClass0,class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C812::Method4.MI.12494()#G2_C812::Method5.12495()#G2_C812::Method6.12496<System.Object>()#"
		call void Generated1411.Generated1411::M.IBase1.A<class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.IBase2.T.T<class Generated1411.BaseClass0,class Generated1411.BaseClass1,class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.IBase2.A.T<class Generated1411.BaseClass1,class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.IBase2.A.B<class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C812::ClassMethod3006.12498()#G2_C812::ClassMethod3007.12499()#G2_C812::ClassMethod3008.12500<System.Object>()#G2_C812::Method4.12493()#G2_C812::Method5.12495()#G2_C812::Method6.12496<System.Object>()#G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.G2_C812.T.T<class Generated1411.BaseClass0,class Generated1411.BaseClass0,class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C812::ClassMethod3006.12498()#G2_C812::ClassMethod3007.12499()#G2_C812::ClassMethod3008.12500<System.Object>()#G2_C812::Method4.12493()#G2_C812::Method5.12495()#G2_C812::Method6.12496<System.Object>()#G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.G2_C812.A.T<class Generated1411.BaseClass0,class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C812::ClassMethod3006.12498()#G2_C812::ClassMethod3007.12499()#G2_C812::ClassMethod3008.12500<System.Object>()#G2_C812::Method4.12493()#G2_C812::Method5.12495()#G2_C812::Method6.12496<System.Object>()#G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.G2_C812.A.A<class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.IBase2.T.T<class Generated1411.BaseClass1,class Generated1411.BaseClass0,class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.IBase2.B.T<class Generated1411.BaseClass0,class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.IBase2.B.A<class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.IBase2.T.T<class Generated1411.BaseClass0,class Generated1411.BaseClass0,class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.IBase2.A.T<class Generated1411.BaseClass0,class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.IBase2.A.A<class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>>(!!0,string)
		newobj instance void class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G2_C812::Method5.12495()#G2_C812::Method6.12496<System.Object>()#G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.G1_C15.T.T<class Generated1411.BaseClass0,class Generated1411.BaseClass1,class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G2_C812::Method5.12495()#G2_C812::Method6.12496<System.Object>()#G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.G1_C15.A.T<class Generated1411.BaseClass1,class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G2_C812::Method5.12495()#G2_C812::Method6.12496<System.Object>()#G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.G1_C15.A.B<class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.IBase2.T.T<class Generated1411.BaseClass1,class Generated1411.BaseClass1,class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.IBase2.B.T<class Generated1411.BaseClass1,class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.IBase2.B.B<class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C812::Method4.MI.12494()#G2_C812::Method5.12495()#G2_C812::Method6.12496<System.Object>()#"
		call void Generated1411.Generated1411::M.IBase1.T<class Generated1411.BaseClass0,class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C812::Method4.MI.12494()#G2_C812::Method5.12495()#G2_C812::Method6.12496<System.Object>()#"
		call void Generated1411.Generated1411::M.IBase1.A<class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.IBase2.T.T<class Generated1411.BaseClass0,class Generated1411.BaseClass1,class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.IBase2.A.T<class Generated1411.BaseClass1,class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.IBase2.A.B<class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C812::ClassMethod3006.12498()#G2_C812::ClassMethod3007.12499()#G2_C812::ClassMethod3008.12500<System.Object>()#G2_C812::Method4.12493()#G2_C812::Method5.12495()#G2_C812::Method6.12496<System.Object>()#G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.G2_C812.T.T<class Generated1411.BaseClass0,class Generated1411.BaseClass1,class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C812::ClassMethod3006.12498()#G2_C812::ClassMethod3007.12499()#G2_C812::ClassMethod3008.12500<System.Object>()#G2_C812::Method4.12493()#G2_C812::Method5.12495()#G2_C812::Method6.12496<System.Object>()#G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.G2_C812.A.T<class Generated1411.BaseClass1,class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C812::ClassMethod3006.12498()#G2_C812::ClassMethod3007.12499()#G2_C812::ClassMethod3008.12500<System.Object>()#G2_C812::Method4.12493()#G2_C812::Method5.12495()#G2_C812::Method6.12496<System.Object>()#G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.G2_C812.A.B<class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.IBase2.T.T<class Generated1411.BaseClass1,class Generated1411.BaseClass0,class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.IBase2.B.T<class Generated1411.BaseClass0,class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.IBase2.B.A<class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.IBase2.T.T<class Generated1411.BaseClass0,class Generated1411.BaseClass0,class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.IBase2.A.T<class Generated1411.BaseClass0,class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.IBase2.A.A<class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>>(!!0,string)
		newobj instance void class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G2_C812::Method5.12495()#G2_C812::Method6.12496<System.Object>()#G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.G1_C15.T.T<class Generated1411.BaseClass0,class Generated1411.BaseClass1,class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G2_C812::Method5.12495()#G2_C812::Method6.12496<System.Object>()#G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.G1_C15.A.T<class Generated1411.BaseClass1,class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G2_C812::Method5.12495()#G2_C812::Method6.12496<System.Object>()#G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.G1_C15.A.B<class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.IBase2.T.T<class Generated1411.BaseClass1,class Generated1411.BaseClass1,class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.IBase2.B.T<class Generated1411.BaseClass1,class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.IBase2.B.B<class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C812::Method4.MI.12494()#G2_C812::Method5.12495()#G2_C812::Method6.12496<System.Object>()#"
		call void Generated1411.Generated1411::M.IBase1.T<class Generated1411.BaseClass0,class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C812::Method4.MI.12494()#G2_C812::Method5.12495()#G2_C812::Method6.12496<System.Object>()#"
		call void Generated1411.Generated1411::M.IBase1.A<class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.IBase2.T.T<class Generated1411.BaseClass0,class Generated1411.BaseClass1,class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.IBase2.A.T<class Generated1411.BaseClass1,class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.IBase2.A.B<class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C812::ClassMethod3006.12498()#G2_C812::ClassMethod3007.12499()#G2_C812::ClassMethod3008.12500<System.Object>()#G2_C812::Method4.12493()#G2_C812::Method5.12495()#G2_C812::Method6.12496<System.Object>()#G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.G2_C812.T.T<class Generated1411.BaseClass1,class Generated1411.BaseClass0,class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C812::ClassMethod3006.12498()#G2_C812::ClassMethod3007.12499()#G2_C812::ClassMethod3008.12500<System.Object>()#G2_C812::Method4.12493()#G2_C812::Method5.12495()#G2_C812::Method6.12496<System.Object>()#G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.G2_C812.B.T<class Generated1411.BaseClass0,class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C812::ClassMethod3006.12498()#G2_C812::ClassMethod3007.12499()#G2_C812::ClassMethod3008.12500<System.Object>()#G2_C812::Method4.12493()#G2_C812::Method5.12495()#G2_C812::Method6.12496<System.Object>()#G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.G2_C812.B.A<class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C812::Method4.MI.12494()#G2_C812::Method5.12495()#G2_C812::Method6.12496<System.Object>()#"
		call void Generated1411.Generated1411::M.IBase1.T<class Generated1411.BaseClass1,class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C812::Method4.MI.12494()#G2_C812::Method5.12495()#G2_C812::Method6.12496<System.Object>()#"
		call void Generated1411.Generated1411::M.IBase1.B<class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.IBase2.T.T<class Generated1411.BaseClass1,class Generated1411.BaseClass0,class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.IBase2.B.T<class Generated1411.BaseClass0,class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.IBase2.B.A<class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.IBase2.T.T<class Generated1411.BaseClass0,class Generated1411.BaseClass0,class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.IBase2.A.T<class Generated1411.BaseClass0,class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.IBase2.A.A<class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>>(!!0,string)
		newobj instance void class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G2_C812::Method5.12495()#G2_C812::Method6.12496<System.Object>()#G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.G1_C15.T.T<class Generated1411.BaseClass0,class Generated1411.BaseClass1,class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G2_C812::Method5.12495()#G2_C812::Method6.12496<System.Object>()#G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.G1_C15.A.T<class Generated1411.BaseClass1,class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G2_C812::Method5.12495()#G2_C812::Method6.12496<System.Object>()#G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.G1_C15.A.B<class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.IBase2.T.T<class Generated1411.BaseClass1,class Generated1411.BaseClass1,class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.IBase2.B.T<class Generated1411.BaseClass1,class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.IBase2.B.B<class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C812::Method4.MI.12494()#G2_C812::Method5.12495()#G2_C812::Method6.12496<System.Object>()#"
		call void Generated1411.Generated1411::M.IBase1.T<class Generated1411.BaseClass0,class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C812::Method4.MI.12494()#G2_C812::Method5.12495()#G2_C812::Method6.12496<System.Object>()#"
		call void Generated1411.Generated1411::M.IBase1.A<class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.IBase2.T.T<class Generated1411.BaseClass0,class Generated1411.BaseClass1,class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.IBase2.A.T<class Generated1411.BaseClass1,class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.IBase2.A.B<class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C812::ClassMethod3006.12498()#G2_C812::ClassMethod3007.12499()#G2_C812::ClassMethod3008.12500<System.Object>()#G2_C812::Method4.12493()#G2_C812::Method5.12495()#G2_C812::Method6.12496<System.Object>()#G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.G2_C812.T.T<class Generated1411.BaseClass1,class Generated1411.BaseClass1,class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C812::ClassMethod3006.12498()#G2_C812::ClassMethod3007.12499()#G2_C812::ClassMethod3008.12500<System.Object>()#G2_C812::Method4.12493()#G2_C812::Method5.12495()#G2_C812::Method6.12496<System.Object>()#G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.G2_C812.B.T<class Generated1411.BaseClass1,class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C812::ClassMethod3006.12498()#G2_C812::ClassMethod3007.12499()#G2_C812::ClassMethod3008.12500<System.Object>()#G2_C812::Method4.12493()#G2_C812::Method5.12495()#G2_C812::Method6.12496<System.Object>()#G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.G2_C812.B.B<class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C812::Method4.MI.12494()#G2_C812::Method5.12495()#G2_C812::Method6.12496<System.Object>()#"
		call void Generated1411.Generated1411::M.IBase1.T<class Generated1411.BaseClass1,class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C812::Method4.MI.12494()#G2_C812::Method5.12495()#G2_C812::Method6.12496<System.Object>()#"
		call void Generated1411.Generated1411::M.IBase1.B<class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.IBase2.T.T<class Generated1411.BaseClass1,class Generated1411.BaseClass0,class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.IBase2.B.T<class Generated1411.BaseClass0,class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.IBase2.B.A<class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.IBase2.T.T<class Generated1411.BaseClass0,class Generated1411.BaseClass0,class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.IBase2.A.T<class Generated1411.BaseClass0,class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C812::Method7.12497<System.Object>()#"
		call void Generated1411.Generated1411::M.IBase2.A.A<class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>>(!!0,string)
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
		newobj instance void class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::Method6<object>()
		calli default string(class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>)
		ldstr "G2_C812::Method6.12496<System.Object>()"
		ldstr "class G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::Method5()
		calli default string(class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>)
		ldstr "G2_C812::Method5.12495()"
		ldstr "class G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::Method4()
		calli default string(class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>)
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::Method7<object>()
		calli default string(class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>)
		ldstr "G2_C812::Method7.12497<System.Object>()"
		ldstr "class G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1411.IBase2`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>::Method7<object>()
		calli default string(class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>)
		ldstr "G2_C812::Method7.12497<System.Object>()"
		ldstr "class IBase2`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1411.IBase1`1<class Generated1411.BaseClass0>::Method4()
		calli default string(class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>)
		ldstr "G2_C812::Method4.MI.12494()"
		ldstr "class IBase1`1<class Generated1411.BaseClass0> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1411.IBase1`1<class Generated1411.BaseClass0>::Method5()
		calli default string(class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>)
		ldstr "G2_C812::Method5.12495()"
		ldstr "class IBase1`1<class Generated1411.BaseClass0> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1411.IBase1`1<class Generated1411.BaseClass0>::Method6<object>()
		calli default string(class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>)
		ldstr "G2_C812::Method6.12496<System.Object>()"
		ldstr "class IBase1`1<class Generated1411.BaseClass0> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1411.IBase2`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::Method7<object>()
		calli default string(class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>)
		ldstr "G2_C812::Method7.12497<System.Object>()"
		ldstr "class IBase2`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::ClassMethod3008<object>()
		calli default string(class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>)
		ldstr "G3_C1883::ClassMethod3008.MI.18531<System.Object>()"
		ldstr "class G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::ClassMethod3007()
		calli default string(class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>)
		ldstr "G2_C812::ClassMethod3007.12499()"
		ldstr "class G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::ClassMethod3006()
		calli default string(class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>)
		ldstr "G3_C1883::ClassMethod3006.MI.18530()"
		ldstr "class G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::Method7<object>()
		calli default string(class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>)
		ldstr "G2_C812::Method7.12497<System.Object>()"
		ldstr "class G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::Method6<object>()
		calli default string(class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>)
		ldstr "G2_C812::Method6.12496<System.Object>()"
		ldstr "class G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::Method5()
		calli default string(class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>)
		ldstr "G2_C812::Method5.12495()"
		ldstr "class G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::Method4()
		calli default string(class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>)
		ldstr "G2_C812::Method4.12493()"
		ldstr "class G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1411.IBase2`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>::Method7<object>()
		calli default string(class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>)
		ldstr "G2_C812::Method7.12497<System.Object>()"
		ldstr "class IBase2`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1411.IBase2`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>::Method7<object>()
		calli default string(class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>)
		ldstr "G2_C812::Method7.12497<System.Object>()"
		ldstr "class IBase2`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>::ClassMethod5162<object>()
		calli default string(class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>)
		ldstr "G3_C1883::ClassMethod5162.18529<System.Object>()"
		ldstr "class G3_C1883`1<class Generated1411.BaseClass0> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>::ClassMethod5161<object>()
		calli default string(class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>)
		ldstr "G3_C1883::ClassMethod5161.18528<System.Object>()"
		ldstr "class G3_C1883`1<class Generated1411.BaseClass0> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>::ClassMethod5160()
		calli default string(class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>)
		ldstr "G3_C1883::ClassMethod5160.18527()"
		ldstr "class G3_C1883`1<class Generated1411.BaseClass0> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>::ClassMethod3008<object>()
		calli default string(class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>)
		ldstr "G3_C1883::ClassMethod3008.MI.18531<System.Object>()"
		ldstr "class G3_C1883`1<class Generated1411.BaseClass0> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>::ClassMethod3007()
		calli default string(class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>)
		ldstr "G2_C812::ClassMethod3007.12499()"
		ldstr "class G3_C1883`1<class Generated1411.BaseClass0> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>::ClassMethod3006()
		calli default string(class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>)
		ldstr "G3_C1883::ClassMethod3006.MI.18530()"
		ldstr "class G3_C1883`1<class Generated1411.BaseClass0> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>::Method7<object>()
		calli default string(class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>)
		ldstr "G2_C812::Method7.12497<System.Object>()"
		ldstr "class G3_C1883`1<class Generated1411.BaseClass0> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>::Method6<object>()
		calli default string(class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>)
		ldstr "G2_C812::Method6.12496<System.Object>()"
		ldstr "class G3_C1883`1<class Generated1411.BaseClass0> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>::Method5()
		calli default string(class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>)
		ldstr "G2_C812::Method5.12495()"
		ldstr "class G3_C1883`1<class Generated1411.BaseClass0> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>::Method4()
		calli default string(class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>)
		ldstr "G2_C812::Method4.12493()"
		ldstr "class G3_C1883`1<class Generated1411.BaseClass0> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::Method6<object>()
		calli default string(class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>)
		ldstr "G2_C812::Method6.12496<System.Object>()"
		ldstr "class G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::Method5()
		calli default string(class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>)
		ldstr "G2_C812::Method5.12495()"
		ldstr "class G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::Method4()
		calli default string(class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>)
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::Method7<object>()
		calli default string(class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>)
		ldstr "G2_C812::Method7.12497<System.Object>()"
		ldstr "class G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1411.IBase2`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>::Method7<object>()
		calli default string(class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>)
		ldstr "G2_C812::Method7.12497<System.Object>()"
		ldstr "class IBase2`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1411.IBase1`1<class Generated1411.BaseClass0>::Method4()
		calli default string(class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>)
		ldstr "G2_C812::Method4.MI.12494()"
		ldstr "class IBase1`1<class Generated1411.BaseClass0> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1411.IBase1`1<class Generated1411.BaseClass0>::Method5()
		calli default string(class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>)
		ldstr "G2_C812::Method5.12495()"
		ldstr "class IBase1`1<class Generated1411.BaseClass0> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1411.IBase1`1<class Generated1411.BaseClass0>::Method6<object>()
		calli default string(class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>)
		ldstr "G2_C812::Method6.12496<System.Object>()"
		ldstr "class IBase1`1<class Generated1411.BaseClass0> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1411.IBase2`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::Method7<object>()
		calli default string(class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>)
		ldstr "G2_C812::Method7.12497<System.Object>()"
		ldstr "class IBase2`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>::ClassMethod3008<object>()
		calli default string(class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>)
		ldstr "G3_C1883::ClassMethod3008.MI.18531<System.Object>()"
		ldstr "class G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>::ClassMethod3007()
		calli default string(class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>)
		ldstr "G2_C812::ClassMethod3007.12499()"
		ldstr "class G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>::ClassMethod3006()
		calli default string(class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>)
		ldstr "G3_C1883::ClassMethod3006.MI.18530()"
		ldstr "class G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>::Method7<object>()
		calli default string(class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>)
		ldstr "G2_C812::Method7.12497<System.Object>()"
		ldstr "class G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>::Method6<object>()
		calli default string(class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>)
		ldstr "G2_C812::Method6.12496<System.Object>()"
		ldstr "class G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>::Method5()
		calli default string(class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>)
		ldstr "G2_C812::Method5.12495()"
		ldstr "class G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>::Method4()
		calli default string(class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>)
		ldstr "G2_C812::Method4.12493()"
		ldstr "class G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1411.IBase1`1<class Generated1411.BaseClass1>::Method4()
		calli default string(class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>)
		ldstr "G2_C812::Method4.MI.12494()"
		ldstr "class IBase1`1<class Generated1411.BaseClass1> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1411.IBase1`1<class Generated1411.BaseClass1>::Method5()
		calli default string(class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>)
		ldstr "G2_C812::Method5.12495()"
		ldstr "class IBase1`1<class Generated1411.BaseClass1> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1411.IBase1`1<class Generated1411.BaseClass1>::Method6<object>()
		calli default string(class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>)
		ldstr "G2_C812::Method6.12496<System.Object>()"
		ldstr "class IBase1`1<class Generated1411.BaseClass1> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1411.IBase2`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>::Method7<object>()
		calli default string(class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>)
		ldstr "G2_C812::Method7.12497<System.Object>()"
		ldstr "class IBase2`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1411.IBase2`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>::Method7<object>()
		calli default string(class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>)
		ldstr "G2_C812::Method7.12497<System.Object>()"
		ldstr "class IBase2`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>::ClassMethod5162<object>()
		calli default string(class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>)
		ldstr "G3_C1883::ClassMethod5162.18529<System.Object>()"
		ldstr "class G3_C1883`1<class Generated1411.BaseClass1> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>::ClassMethod5161<object>()
		calli default string(class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>)
		ldstr "G3_C1883::ClassMethod5161.18528<System.Object>()"
		ldstr "class G3_C1883`1<class Generated1411.BaseClass1> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>::ClassMethod5160()
		calli default string(class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>)
		ldstr "G3_C1883::ClassMethod5160.18527()"
		ldstr "class G3_C1883`1<class Generated1411.BaseClass1> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>::ClassMethod3008<object>()
		calli default string(class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>)
		ldstr "G3_C1883::ClassMethod3008.MI.18531<System.Object>()"
		ldstr "class G3_C1883`1<class Generated1411.BaseClass1> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>::ClassMethod3007()
		calli default string(class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>)
		ldstr "G2_C812::ClassMethod3007.12499()"
		ldstr "class G3_C1883`1<class Generated1411.BaseClass1> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>::ClassMethod3006()
		calli default string(class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>)
		ldstr "G3_C1883::ClassMethod3006.MI.18530()"
		ldstr "class G3_C1883`1<class Generated1411.BaseClass1> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>::Method7<object>()
		calli default string(class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>)
		ldstr "G2_C812::Method7.12497<System.Object>()"
		ldstr "class G3_C1883`1<class Generated1411.BaseClass1> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>::Method6<object>()
		calli default string(class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>)
		ldstr "G2_C812::Method6.12496<System.Object>()"
		ldstr "class G3_C1883`1<class Generated1411.BaseClass1> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>::Method5()
		calli default string(class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>)
		ldstr "G2_C812::Method5.12495()"
		ldstr "class G3_C1883`1<class Generated1411.BaseClass1> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>::Method4()
		calli default string(class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>)
		ldstr "G2_C812::Method4.12493()"
		ldstr "class G3_C1883`1<class Generated1411.BaseClass1> on type class Generated1411.G3_C1883`1<class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::Method6<object>()
		calli default string(class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>)
		ldstr "G2_C812::Method6.12496<System.Object>()"
		ldstr "class G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::Method5()
		calli default string(class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>)
		ldstr "G2_C812::Method5.12495()"
		ldstr "class G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::Method4()
		calli default string(class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>)
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::Method7<object>()
		calli default string(class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>)
		ldstr "G2_C812::Method7.12497<System.Object>()"
		ldstr "class G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1411.IBase2`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>::Method7<object>()
		calli default string(class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>)
		ldstr "G2_C812::Method7.12497<System.Object>()"
		ldstr "class IBase2`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1411.IBase1`1<class Generated1411.BaseClass0>::Method4()
		calli default string(class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>)
		ldstr "G2_C812::Method4.MI.12494()"
		ldstr "class IBase1`1<class Generated1411.BaseClass0> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1411.IBase1`1<class Generated1411.BaseClass0>::Method5()
		calli default string(class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>)
		ldstr "G2_C812::Method5.12495()"
		ldstr "class IBase1`1<class Generated1411.BaseClass0> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1411.IBase1`1<class Generated1411.BaseClass0>::Method6<object>()
		calli default string(class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>)
		ldstr "G2_C812::Method6.12496<System.Object>()"
		ldstr "class IBase1`1<class Generated1411.BaseClass0> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1411.IBase2`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::Method7<object>()
		calli default string(class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>)
		ldstr "G2_C812::Method7.12497<System.Object>()"
		ldstr "class IBase2`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>::ClassMethod3008<object>()
		calli default string(class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>)
		ldstr "G2_C812::ClassMethod3008.12500<System.Object>()"
		ldstr "class G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>::ClassMethod3007()
		calli default string(class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>)
		ldstr "G2_C812::ClassMethod3007.12499()"
		ldstr "class G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>::ClassMethod3006()
		calli default string(class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>)
		ldstr "G2_C812::ClassMethod3006.12498()"
		ldstr "class G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>::Method7<object>()
		calli default string(class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>)
		ldstr "G2_C812::Method7.12497<System.Object>()"
		ldstr "class G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>::Method6<object>()
		calli default string(class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>)
		ldstr "G2_C812::Method6.12496<System.Object>()"
		ldstr "class G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>::Method5()
		calli default string(class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>)
		ldstr "G2_C812::Method5.12495()"
		ldstr "class G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>::Method4()
		calli default string(class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>)
		ldstr "G2_C812::Method4.12493()"
		ldstr "class G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1411.IBase2`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>::Method7<object>()
		calli default string(class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>)
		ldstr "G2_C812::Method7.12497<System.Object>()"
		ldstr "class IBase2`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1411.IBase2`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>::Method7<object>()
		calli default string(class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>)
		ldstr "G2_C812::Method7.12497<System.Object>()"
		ldstr "class IBase2`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::Method6<object>()
		calli default string(class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>)
		ldstr "G2_C812::Method6.12496<System.Object>()"
		ldstr "class G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::Method5()
		calli default string(class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>)
		ldstr "G2_C812::Method5.12495()"
		ldstr "class G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::Method4()
		calli default string(class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>)
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::Method7<object>()
		calli default string(class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>)
		ldstr "G2_C812::Method7.12497<System.Object>()"
		ldstr "class G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1411.IBase2`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>::Method7<object>()
		calli default string(class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>)
		ldstr "G2_C812::Method7.12497<System.Object>()"
		ldstr "class IBase2`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1411.IBase1`1<class Generated1411.BaseClass0>::Method4()
		calli default string(class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>)
		ldstr "G2_C812::Method4.MI.12494()"
		ldstr "class IBase1`1<class Generated1411.BaseClass0> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1411.IBase1`1<class Generated1411.BaseClass0>::Method5()
		calli default string(class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>)
		ldstr "G2_C812::Method5.12495()"
		ldstr "class IBase1`1<class Generated1411.BaseClass0> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1411.IBase1`1<class Generated1411.BaseClass0>::Method6<object>()
		calli default string(class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>)
		ldstr "G2_C812::Method6.12496<System.Object>()"
		ldstr "class IBase1`1<class Generated1411.BaseClass0> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1411.IBase2`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::Method7<object>()
		calli default string(class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>)
		ldstr "G2_C812::Method7.12497<System.Object>()"
		ldstr "class IBase2`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::ClassMethod3008<object>()
		calli default string(class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>)
		ldstr "G2_C812::ClassMethod3008.12500<System.Object>()"
		ldstr "class G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::ClassMethod3007()
		calli default string(class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>)
		ldstr "G2_C812::ClassMethod3007.12499()"
		ldstr "class G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::ClassMethod3006()
		calli default string(class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>)
		ldstr "G2_C812::ClassMethod3006.12498()"
		ldstr "class G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::Method7<object>()
		calli default string(class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>)
		ldstr "G2_C812::Method7.12497<System.Object>()"
		ldstr "class G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::Method6<object>()
		calli default string(class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>)
		ldstr "G2_C812::Method6.12496<System.Object>()"
		ldstr "class G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::Method5()
		calli default string(class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>)
		ldstr "G2_C812::Method5.12495()"
		ldstr "class G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::Method4()
		calli default string(class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>)
		ldstr "G2_C812::Method4.12493()"
		ldstr "class G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1411.IBase2`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>::Method7<object>()
		calli default string(class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>)
		ldstr "G2_C812::Method7.12497<System.Object>()"
		ldstr "class IBase2`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1411.IBase2`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>::Method7<object>()
		calli default string(class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>)
		ldstr "G2_C812::Method7.12497<System.Object>()"
		ldstr "class IBase2`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::Method6<object>()
		calli default string(class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>)
		ldstr "G2_C812::Method6.12496<System.Object>()"
		ldstr "class G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::Method5()
		calli default string(class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>)
		ldstr "G2_C812::Method5.12495()"
		ldstr "class G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::Method4()
		calli default string(class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>)
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::Method7<object>()
		calli default string(class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>)
		ldstr "G2_C812::Method7.12497<System.Object>()"
		ldstr "class G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1411.IBase2`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>::Method7<object>()
		calli default string(class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>)
		ldstr "G2_C812::Method7.12497<System.Object>()"
		ldstr "class IBase2`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1411.IBase1`1<class Generated1411.BaseClass0>::Method4()
		calli default string(class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>)
		ldstr "G2_C812::Method4.MI.12494()"
		ldstr "class IBase1`1<class Generated1411.BaseClass0> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1411.IBase1`1<class Generated1411.BaseClass0>::Method5()
		calli default string(class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>)
		ldstr "G2_C812::Method5.12495()"
		ldstr "class IBase1`1<class Generated1411.BaseClass0> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1411.IBase1`1<class Generated1411.BaseClass0>::Method6<object>()
		calli default string(class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>)
		ldstr "G2_C812::Method6.12496<System.Object>()"
		ldstr "class IBase1`1<class Generated1411.BaseClass0> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1411.IBase2`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::Method7<object>()
		calli default string(class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>)
		ldstr "G2_C812::Method7.12497<System.Object>()"
		ldstr "class IBase2`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>::ClassMethod3008<object>()
		calli default string(class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>)
		ldstr "G2_C812::ClassMethod3008.12500<System.Object>()"
		ldstr "class G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>::ClassMethod3007()
		calli default string(class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>)
		ldstr "G2_C812::ClassMethod3007.12499()"
		ldstr "class G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>::ClassMethod3006()
		calli default string(class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>)
		ldstr "G2_C812::ClassMethod3006.12498()"
		ldstr "class G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>::Method7<object>()
		calli default string(class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>)
		ldstr "G2_C812::Method7.12497<System.Object>()"
		ldstr "class G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>::Method6<object>()
		calli default string(class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>)
		ldstr "G2_C812::Method6.12496<System.Object>()"
		ldstr "class G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>::Method5()
		calli default string(class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>)
		ldstr "G2_C812::Method5.12495()"
		ldstr "class G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>::Method4()
		calli default string(class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>)
		ldstr "G2_C812::Method4.12493()"
		ldstr "class G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1411.IBase1`1<class Generated1411.BaseClass1>::Method4()
		calli default string(class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>)
		ldstr "G2_C812::Method4.MI.12494()"
		ldstr "class IBase1`1<class Generated1411.BaseClass1> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1411.IBase1`1<class Generated1411.BaseClass1>::Method5()
		calli default string(class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>)
		ldstr "G2_C812::Method5.12495()"
		ldstr "class IBase1`1<class Generated1411.BaseClass1> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1411.IBase1`1<class Generated1411.BaseClass1>::Method6<object>()
		calli default string(class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>)
		ldstr "G2_C812::Method6.12496<System.Object>()"
		ldstr "class IBase1`1<class Generated1411.BaseClass1> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1411.IBase2`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>::Method7<object>()
		calli default string(class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>)
		ldstr "G2_C812::Method7.12497<System.Object>()"
		ldstr "class IBase2`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1411.IBase2`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>::Method7<object>()
		calli default string(class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>)
		ldstr "G2_C812::Method7.12497<System.Object>()"
		ldstr "class IBase2`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::Method6<object>()
		calli default string(class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>)
		ldstr "G2_C812::Method6.12496<System.Object>()"
		ldstr "class G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::Method5()
		calli default string(class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>)
		ldstr "G2_C812::Method5.12495()"
		ldstr "class G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::Method4()
		calli default string(class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>)
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1411.G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::Method7<object>()
		calli default string(class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>)
		ldstr "G2_C812::Method7.12497<System.Object>()"
		ldstr "class G1_C15`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1411.IBase2`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>::Method7<object>()
		calli default string(class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>)
		ldstr "G2_C812::Method7.12497<System.Object>()"
		ldstr "class IBase2`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1411.IBase1`1<class Generated1411.BaseClass0>::Method4()
		calli default string(class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>)
		ldstr "G2_C812::Method4.MI.12494()"
		ldstr "class IBase1`1<class Generated1411.BaseClass0> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1411.IBase1`1<class Generated1411.BaseClass0>::Method5()
		calli default string(class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>)
		ldstr "G2_C812::Method5.12495()"
		ldstr "class IBase1`1<class Generated1411.BaseClass0> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1411.IBase1`1<class Generated1411.BaseClass0>::Method6<object>()
		calli default string(class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>)
		ldstr "G2_C812::Method6.12496<System.Object>()"
		ldstr "class IBase1`1<class Generated1411.BaseClass0> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1411.IBase2`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1>::Method7<object>()
		calli default string(class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>)
		ldstr "G2_C812::Method7.12497<System.Object>()"
		ldstr "class IBase2`2<class Generated1411.BaseClass0,class Generated1411.BaseClass1> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>::ClassMethod3008<object>()
		calli default string(class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>)
		ldstr "G2_C812::ClassMethod3008.12500<System.Object>()"
		ldstr "class G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>::ClassMethod3007()
		calli default string(class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>)
		ldstr "G2_C812::ClassMethod3007.12499()"
		ldstr "class G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>::ClassMethod3006()
		calli default string(class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>)
		ldstr "G2_C812::ClassMethod3006.12498()"
		ldstr "class G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>::Method7<object>()
		calli default string(class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>)
		ldstr "G2_C812::Method7.12497<System.Object>()"
		ldstr "class G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>::Method6<object>()
		calli default string(class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>)
		ldstr "G2_C812::Method6.12496<System.Object>()"
		ldstr "class G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>::Method5()
		calli default string(class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>)
		ldstr "G2_C812::Method5.12495()"
		ldstr "class G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>::Method4()
		calli default string(class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>)
		ldstr "G2_C812::Method4.12493()"
		ldstr "class G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1411.IBase1`1<class Generated1411.BaseClass1>::Method4()
		calli default string(class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>)
		ldstr "G2_C812::Method4.MI.12494()"
		ldstr "class IBase1`1<class Generated1411.BaseClass1> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1411.IBase1`1<class Generated1411.BaseClass1>::Method5()
		calli default string(class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>)
		ldstr "G2_C812::Method5.12495()"
		ldstr "class IBase1`1<class Generated1411.BaseClass1> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1411.IBase1`1<class Generated1411.BaseClass1>::Method6<object>()
		calli default string(class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>)
		ldstr "G2_C812::Method6.12496<System.Object>()"
		ldstr "class IBase1`1<class Generated1411.BaseClass1> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1411.IBase2`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0>::Method7<object>()
		calli default string(class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>)
		ldstr "G2_C812::Method7.12497<System.Object>()"
		ldstr "class IBase2`2<class Generated1411.BaseClass1,class Generated1411.BaseClass0> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1411.IBase2`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0>::Method7<object>()
		calli default string(class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>)
		ldstr "G2_C812::Method7.12497<System.Object>()"
		ldstr "class IBase2`2<class Generated1411.BaseClass0,class Generated1411.BaseClass0> on type class Generated1411.G2_C812`2<class Generated1411.BaseClass1,class Generated1411.BaseClass1>"
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
				.maxstack  10
		call void Generated1411.Generated1411::MethodCallingTest()
		call void Generated1411.Generated1411::ConstrainedCallsTest()
		call void Generated1411.Generated1411::StructConstrainedInterfaceCallsTest()
		call void Generated1411.Generated1411::CalliTest()
		ldc.i4 100
		ret
	}
}
