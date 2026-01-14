// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) .ver 4:0:0:0 }
.assembly extern TestFramework { .publickeytoken = ( B0 3F 5F 7F 11 D5 0A 3A )  }

//TYPES IN FORWARDER ASSEMBLIES:

//TEST ASSEMBLY:
.assembly extern xunit.core {}

.class public Generated703.BaseClass0 
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void [mscorlib]System.Object::.ctor()
		ret
	}
}
.class public Generated703.BaseClass1 
		extends Generated703.BaseClass0
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void Generated703.BaseClass0::.ctor()
		ret
	}
}
.class public Generated703.G3_C1175`1<T0> 
		extends class Generated703.G2_C239`2<!T0,class Generated703.BaseClass0>
		implements Generated703.IBase0 
{
	.method public hidebysig newslot virtual instance string Method0() cil managed noinlining { 
		ldstr "G3_C1175::Method0.14727()"
		ret
	}
	.method public hidebysig virtual instance string Method1() cil managed noinlining { 
		ldstr "G3_C1175::Method1.14728()"
		ret
	}
	.method public hidebysig virtual instance string Method2<M0>() cil managed noinlining { 
		ldstr "G3_C1175::Method2.14729<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string Method3<M0>() cil managed noinlining { 
		ldstr "G3_C1175::Method3.14730<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod3745() cil managed noinlining { 
		ldstr "G3_C1175::ClassMethod3745.14731()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod3746() cil managed noinlining { 
		ldstr "G3_C1175::ClassMethod3746.14732()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod3747<M0>() cil managed noinlining { 
		ldstr "G3_C1175::ClassMethod3747.14733<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod3748<M0>() cil managed noinlining { 
		ldstr "G3_C1175::ClassMethod3748.14734<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class Generated703.G2_C239`2<!T0,class Generated703.BaseClass0>::.ctor()
		ret
	}
}
.class public Generated703.G2_C239`2<T0, T1> 
		extends class Generated703.G1_C4`1<class Generated703.BaseClass0>
		implements class Generated703.IBase1`1<class Generated703.BaseClass1>, class Generated703.IBase2`2<class Generated703.BaseClass0,!T1> 
{
	.method public hidebysig virtual instance string Method4() cil managed noinlining { 
		ldstr "G2_C239::Method4.6980()"
		ret
	}
	.method public hidebysig virtual instance string Method5() cil managed noinlining { 
		ldstr "G2_C239::Method5.6981()"
		ret
	}
	.method public hidebysig virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G2_C239::Method6.6982<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class Generated703.BaseClass1>.Method6'<M0>() cil managed noinlining { 
		.override method instance string class Generated703.IBase1`1<class Generated703.BaseClass1>::Method6<[1]>()
		ldstr "G2_C239::Method6.MI.6983<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G2_C239::Method7.6984<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C4<class Generated703.BaseClass0>.ClassMethod1321'() cil managed noinlining { 
		.override method instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::ClassMethod1321()
		ldstr "G2_C239::ClassMethod1321.MI.6985()"
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class Generated703.G1_C4`1<class Generated703.BaseClass0>::.ctor()
		ret
	}
}
.class interface public abstract Generated703.IBase0 
{
	.method public hidebysig newslot abstract virtual instance string Method0() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method1() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method2<M0>() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method3<M0>() cil managed { }
}
.class public Generated703.G1_C4`1<T0> 
		implements class Generated703.IBase2`2<class Generated703.BaseClass0,class Generated703.BaseClass1>, class Generated703.IBase1`1<class Generated703.BaseClass1> 
{
	.method public hidebysig newslot virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G1_C4::Method7.4790<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase2<class Generated703.BaseClass0,class Generated703.BaseClass1>.Method7'<M0>() cil managed noinlining { 
		.override method instance string class Generated703.IBase2`2<class Generated703.BaseClass0,class Generated703.BaseClass1>::Method7<[1]>()
		ldstr "G1_C4::Method7.MI.4791<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig virtual instance string Method4() cil managed noinlining { 
		ldstr "G1_C4::Method4.4792()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class Generated703.BaseClass1>.Method4'() cil managed noinlining { 
		.override method instance string class Generated703.IBase1`1<class Generated703.BaseClass1>::Method4()
		ldstr "G1_C4::Method4.MI.4793()"
		ret
	}
	.method public hidebysig virtual instance string Method5() cil managed noinlining { 
		ldstr "G1_C4::Method5.4794()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class Generated703.BaseClass1>.Method5'() cil managed noinlining { 
		.override method instance string class Generated703.IBase1`1<class Generated703.BaseClass1>::Method5()
		ldstr "G1_C4::Method5.MI.4795()"
		ret
	}
	.method public hidebysig virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G1_C4::Method6.4796<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class Generated703.BaseClass1>.Method6'<M0>() cil managed noinlining { 
		.override method instance string class Generated703.IBase1`1<class Generated703.BaseClass1>::Method6<[1]>()
		ldstr "G1_C4::Method6.MI.4797<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1321() cil managed noinlining { 
		ldstr "G1_C4::ClassMethod1321.4798()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1322() cil managed noinlining { 
		ldstr "G1_C4::ClassMethod1322.4799()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1323<M0>() cil managed noinlining { 
		ldstr "G1_C4::ClassMethod1323.4800<"
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
.class interface public abstract Generated703.IBase1`1<+T0> 
{
	.method public hidebysig newslot abstract virtual instance string Method4() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method5() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method6<M0>() cil managed { }
}
.class interface public abstract Generated703.IBase2`2<+T0, -T1> 
{
	.method public hidebysig newslot abstract virtual instance string Method7<M0>() cil managed { }
}
.class public auto ansi beforefieldinit Generated703.Generated703 {
	.method static void M.BaseClass0<(Generated703.BaseClass0)W>(!!W inst, string exp) cil managed {
		.maxstack 5
		.locals init (string[] actualResults)
		ldc.i4.s    0
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.BaseClass0<(Generated703.BaseClass0)W>(!!W inst, string exp)"
		ldc.i4.s 0
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.BaseClass1<(Generated703.BaseClass1)W>(!!W inst, string exp) cil managed {
		.maxstack 5
		.locals init (string[] actualResults)
		ldc.i4.s    0
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.BaseClass1<(Generated703.BaseClass1)W>(!!W inst, string exp)"
		ldc.i4.s 0
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1175.T<T0,(class Generated703.G3_C1175`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 20
		.locals init (string[] actualResults)
		ldc.i4.s    15
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1175.T<T0,(class Generated703.G3_C1175`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 15
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G3_C1175`1<!!T0>::ClassMethod1321()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G3_C1175`1<!!T0>::ClassMethod1322()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G3_C1175`1<!!T0>::ClassMethod1323<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G3_C1175`1<!!T0>::ClassMethod3745()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G3_C1175`1<!!T0>::ClassMethod3746()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G3_C1175`1<!!T0>::ClassMethod3747<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G3_C1175`1<!!T0>::ClassMethod3748<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G3_C1175`1<!!T0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G3_C1175`1<!!T0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G3_C1175`1<!!T0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G3_C1175`1<!!T0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G3_C1175`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G3_C1175`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G3_C1175`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     14
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G3_C1175`1<!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1175.A<(class Generated703.G3_C1175`1<class Generated703.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 20
		.locals init (string[] actualResults)
		ldc.i4.s    15
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1175.A<(class Generated703.G3_C1175`1<class Generated703.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 15
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G3_C1175`1<class Generated703.BaseClass0>::ClassMethod1321()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G3_C1175`1<class Generated703.BaseClass0>::ClassMethod1322()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G3_C1175`1<class Generated703.BaseClass0>::ClassMethod1323<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G3_C1175`1<class Generated703.BaseClass0>::ClassMethod3745()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G3_C1175`1<class Generated703.BaseClass0>::ClassMethod3746()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G3_C1175`1<class Generated703.BaseClass0>::ClassMethod3747<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G3_C1175`1<class Generated703.BaseClass0>::ClassMethod3748<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G3_C1175`1<class Generated703.BaseClass0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G3_C1175`1<class Generated703.BaseClass0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G3_C1175`1<class Generated703.BaseClass0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G3_C1175`1<class Generated703.BaseClass0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G3_C1175`1<class Generated703.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G3_C1175`1<class Generated703.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G3_C1175`1<class Generated703.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     14
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G3_C1175`1<class Generated703.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1175.B<(class Generated703.G3_C1175`1<class Generated703.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 20
		.locals init (string[] actualResults)
		ldc.i4.s    15
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1175.B<(class Generated703.G3_C1175`1<class Generated703.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 15
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G3_C1175`1<class Generated703.BaseClass1>::ClassMethod1321()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G3_C1175`1<class Generated703.BaseClass1>::ClassMethod1322()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G3_C1175`1<class Generated703.BaseClass1>::ClassMethod1323<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G3_C1175`1<class Generated703.BaseClass1>::ClassMethod3745()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G3_C1175`1<class Generated703.BaseClass1>::ClassMethod3746()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G3_C1175`1<class Generated703.BaseClass1>::ClassMethod3747<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G3_C1175`1<class Generated703.BaseClass1>::ClassMethod3748<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G3_C1175`1<class Generated703.BaseClass1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G3_C1175`1<class Generated703.BaseClass1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G3_C1175`1<class Generated703.BaseClass1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G3_C1175`1<class Generated703.BaseClass1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G3_C1175`1<class Generated703.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G3_C1175`1<class Generated703.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G3_C1175`1<class Generated703.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     14
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G3_C1175`1<class Generated703.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C239.T.T<T0,T1,(class Generated703.G2_C239`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C239.T.T<T0,T1,(class Generated703.G2_C239`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G2_C239`2<!!T0,!!T1>::ClassMethod1321()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G2_C239`2<!!T0,!!T1>::ClassMethod1322()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G2_C239`2<!!T0,!!T1>::ClassMethod1323<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G2_C239`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G2_C239`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G2_C239`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G2_C239`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C239.A.T<T1,(class Generated703.G2_C239`2<class Generated703.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C239.A.T<T1,(class Generated703.G2_C239`2<class Generated703.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G2_C239`2<class Generated703.BaseClass0,!!T1>::ClassMethod1321()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G2_C239`2<class Generated703.BaseClass0,!!T1>::ClassMethod1322()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G2_C239`2<class Generated703.BaseClass0,!!T1>::ClassMethod1323<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G2_C239`2<class Generated703.BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G2_C239`2<class Generated703.BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G2_C239`2<class Generated703.BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G2_C239`2<class Generated703.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C239.A.A<(class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C239.A.A<(class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>::ClassMethod1321()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>::ClassMethod1322()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>::ClassMethod1323<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C239.A.B<(class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C239.A.B<(class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>::ClassMethod1321()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>::ClassMethod1322()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>::ClassMethod1323<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C239.B.T<T1,(class Generated703.G2_C239`2<class Generated703.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C239.B.T<T1,(class Generated703.G2_C239`2<class Generated703.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G2_C239`2<class Generated703.BaseClass1,!!T1>::ClassMethod1321()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G2_C239`2<class Generated703.BaseClass1,!!T1>::ClassMethod1322()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G2_C239`2<class Generated703.BaseClass1,!!T1>::ClassMethod1323<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G2_C239`2<class Generated703.BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G2_C239`2<class Generated703.BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G2_C239`2<class Generated703.BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G2_C239`2<class Generated703.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C239.B.A<(class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C239.B.A<(class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>::ClassMethod1321()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>::ClassMethod1322()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>::ClassMethod1323<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C239.B.B<(class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C239.B.B<(class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>::ClassMethod1321()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>::ClassMethod1322()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>::ClassMethod1323<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase0<(Generated703.IBase0)W>(!!W inst, string exp) cil managed {
		.maxstack 9
		.locals init (string[] actualResults)
		ldc.i4.s    4
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase0<(Generated703.IBase0)W>(!!W inst, string exp)"
		ldc.i4.s 4
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string Generated703.IBase0::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string Generated703.IBase0::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string Generated703.IBase0::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string Generated703.IBase0::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C4.T<T0,(class Generated703.G1_C4`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C4.T<T0,(class Generated703.G1_C4`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G1_C4`1<!!T0>::ClassMethod1321()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G1_C4`1<!!T0>::ClassMethod1322()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G1_C4`1<!!T0>::ClassMethod1323<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G1_C4`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G1_C4`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G1_C4`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G1_C4`1<!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C4.A<(class Generated703.G1_C4`1<class Generated703.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C4.A<(class Generated703.G1_C4`1<class Generated703.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::ClassMethod1321()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::ClassMethod1322()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::ClassMethod1323<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C4.B<(class Generated703.G1_C4`1<class Generated703.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C4.B<(class Generated703.G1_C4`1<class Generated703.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G1_C4`1<class Generated703.BaseClass1>::ClassMethod1321()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G1_C4`1<class Generated703.BaseClass1>::ClassMethod1322()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G1_C4`1<class Generated703.BaseClass1>::ClassMethod1323<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G1_C4`1<class Generated703.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G1_C4`1<class Generated703.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G1_C4`1<class Generated703.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.G1_C4`1<class Generated703.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.T<T0,(class Generated703.IBase1`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.T<T0,(class Generated703.IBase1`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.IBase1`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.IBase1`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.IBase1`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.A<(class Generated703.IBase1`1<class Generated703.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.A<(class Generated703.IBase1`1<class Generated703.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.IBase1`1<class Generated703.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.IBase1`1<class Generated703.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.IBase1`1<class Generated703.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.B<(class Generated703.IBase1`1<class Generated703.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.B<(class Generated703.IBase1`1<class Generated703.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.IBase1`1<class Generated703.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.IBase1`1<class Generated703.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.IBase1`1<class Generated703.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.T.T<T0,T1,(class Generated703.IBase2`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.T.T<T0,T1,(class Generated703.IBase2`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.IBase2`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.T<T1,(class Generated703.IBase2`2<class Generated703.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.T<T1,(class Generated703.IBase2`2<class Generated703.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.IBase2`2<class Generated703.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.A<(class Generated703.IBase2`2<class Generated703.BaseClass0,class Generated703.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.A<(class Generated703.IBase2`2<class Generated703.BaseClass0,class Generated703.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.IBase2`2<class Generated703.BaseClass0,class Generated703.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.B<(class Generated703.IBase2`2<class Generated703.BaseClass0,class Generated703.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.B<(class Generated703.IBase2`2<class Generated703.BaseClass0,class Generated703.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.IBase2`2<class Generated703.BaseClass0,class Generated703.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.T<T1,(class Generated703.IBase2`2<class Generated703.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.T<T1,(class Generated703.IBase2`2<class Generated703.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.IBase2`2<class Generated703.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.A<(class Generated703.IBase2`2<class Generated703.BaseClass1,class Generated703.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.A<(class Generated703.IBase2`2<class Generated703.BaseClass1,class Generated703.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.IBase2`2<class Generated703.BaseClass1,class Generated703.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.B<(class Generated703.IBase2`2<class Generated703.BaseClass1,class Generated703.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.B<(class Generated703.IBase2`2<class Generated703.BaseClass1,class Generated703.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated703.IBase2`2<class Generated703.BaseClass1,class Generated703.BaseClass1>::Method7<object>()
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
		newobj instance void class Generated703.G3_C1175`1<class Generated703.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		callvirt instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::ClassMethod1323<object>()
		ldstr "G1_C4::ClassMethod1323.4800<System.Object>()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		callvirt instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::ClassMethod1322()
		ldstr "G1_C4::ClassMethod1322.4799()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		callvirt instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::ClassMethod1321()
		ldstr "G2_C239::ClassMethod1321.MI.6985()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		callvirt instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::Method6<object>()
		ldstr "G2_C239::Method6.6982<System.Object>()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		callvirt instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::Method5()
		ldstr "G2_C239::Method5.6981()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		callvirt instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::Method4()
		ldstr "G2_C239::Method4.6980()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		callvirt instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::Method7<object>()
		ldstr "G1_C4::Method7.4790<System.Object>()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated703.IBase2`2<class Generated703.BaseClass0,class Generated703.BaseClass1>::Method7<object>()
		ldstr "G2_C239::Method7.6984<System.Object>()"
		ldstr "class IBase2`2<class Generated703.BaseClass0,class Generated703.BaseClass1> on type class Generated703.G3_C1175`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated703.IBase1`1<class Generated703.BaseClass1>::Method4()
		ldstr "G2_C239::Method4.6980()"
		ldstr "class IBase1`1<class Generated703.BaseClass1> on type class Generated703.G3_C1175`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated703.IBase1`1<class Generated703.BaseClass1>::Method5()
		ldstr "G2_C239::Method5.6981()"
		ldstr "class IBase1`1<class Generated703.BaseClass1> on type class Generated703.G3_C1175`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated703.IBase1`1<class Generated703.BaseClass1>::Method6<object>()
		ldstr "G2_C239::Method6.MI.6983<System.Object>()"
		ldstr "class IBase1`1<class Generated703.BaseClass1> on type class Generated703.G3_C1175`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated703.IBase1`1<class Generated703.BaseClass0>::Method4()
		ldstr "G2_C239::Method4.6980()"
		ldstr "class IBase1`1<class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated703.IBase1`1<class Generated703.BaseClass0>::Method5()
		ldstr "G2_C239::Method5.6981()"
		ldstr "class IBase1`1<class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated703.IBase1`1<class Generated703.BaseClass0>::Method6<object>()
		ldstr "G2_C239::Method6.MI.6983<System.Object>()"
		ldstr "class IBase1`1<class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>
		callvirt instance string class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>::Method7<object>()
		ldstr "G2_C239::Method7.6984<System.Object>()"
		ldstr "class G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>
		callvirt instance string class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>::Method6<object>()
		ldstr "G2_C239::Method6.6982<System.Object>()"
		ldstr "class G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>
		callvirt instance string class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>::Method5()
		ldstr "G2_C239::Method5.6981()"
		ldstr "class G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>
		callvirt instance string class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>::Method4()
		ldstr "G2_C239::Method4.6980()"
		ldstr "class G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>
		callvirt instance string class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>::ClassMethod1323<object>()
		ldstr "G1_C4::ClassMethod1323.4800<System.Object>()"
		ldstr "class G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>
		callvirt instance string class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>::ClassMethod1322()
		ldstr "G1_C4::ClassMethod1322.4799()"
		ldstr "class G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>
		callvirt instance string class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>::ClassMethod1321()
		ldstr "G2_C239::ClassMethod1321.MI.6985()"
		ldstr "class G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated703.IBase2`2<class Generated703.BaseClass0,class Generated703.BaseClass0>::Method7<object>()
		ldstr "G2_C239::Method7.6984<System.Object>()"
		ldstr "class IBase2`2<class Generated703.BaseClass0,class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated703.G3_C1175`1<class Generated703.BaseClass0>
		callvirt instance string class Generated703.G3_C1175`1<class Generated703.BaseClass0>::ClassMethod3748<object>()
		ldstr "G3_C1175::ClassMethod3748.14734<System.Object>()"
		ldstr "class G3_C1175`1<class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G3_C1175`1<class Generated703.BaseClass0>
		callvirt instance string class Generated703.G3_C1175`1<class Generated703.BaseClass0>::ClassMethod3747<object>()
		ldstr "G3_C1175::ClassMethod3747.14733<System.Object>()"
		ldstr "class G3_C1175`1<class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G3_C1175`1<class Generated703.BaseClass0>
		callvirt instance string class Generated703.G3_C1175`1<class Generated703.BaseClass0>::ClassMethod3746()
		ldstr "G3_C1175::ClassMethod3746.14732()"
		ldstr "class G3_C1175`1<class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G3_C1175`1<class Generated703.BaseClass0>
		callvirt instance string class Generated703.G3_C1175`1<class Generated703.BaseClass0>::ClassMethod3745()
		ldstr "G3_C1175::ClassMethod3745.14731()"
		ldstr "class G3_C1175`1<class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G3_C1175`1<class Generated703.BaseClass0>
		callvirt instance string class Generated703.G3_C1175`1<class Generated703.BaseClass0>::Method3<object>()
		ldstr "G3_C1175::Method3.14730<System.Object>()"
		ldstr "class G3_C1175`1<class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G3_C1175`1<class Generated703.BaseClass0>
		callvirt instance string class Generated703.G3_C1175`1<class Generated703.BaseClass0>::Method2<object>()
		ldstr "G3_C1175::Method2.14729<System.Object>()"
		ldstr "class G3_C1175`1<class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G3_C1175`1<class Generated703.BaseClass0>
		callvirt instance string class Generated703.G3_C1175`1<class Generated703.BaseClass0>::Method1()
		ldstr "G3_C1175::Method1.14728()"
		ldstr "class G3_C1175`1<class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G3_C1175`1<class Generated703.BaseClass0>
		callvirt instance string class Generated703.G3_C1175`1<class Generated703.BaseClass0>::Method0()
		ldstr "G3_C1175::Method0.14727()"
		ldstr "class G3_C1175`1<class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G3_C1175`1<class Generated703.BaseClass0>
		callvirt instance string class Generated703.G3_C1175`1<class Generated703.BaseClass0>::Method7<object>()
		ldstr "G2_C239::Method7.6984<System.Object>()"
		ldstr "class G3_C1175`1<class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G3_C1175`1<class Generated703.BaseClass0>
		callvirt instance string class Generated703.G3_C1175`1<class Generated703.BaseClass0>::Method6<object>()
		ldstr "G2_C239::Method6.6982<System.Object>()"
		ldstr "class G3_C1175`1<class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G3_C1175`1<class Generated703.BaseClass0>
		callvirt instance string class Generated703.G3_C1175`1<class Generated703.BaseClass0>::Method5()
		ldstr "G2_C239::Method5.6981()"
		ldstr "class G3_C1175`1<class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G3_C1175`1<class Generated703.BaseClass0>
		callvirt instance string class Generated703.G3_C1175`1<class Generated703.BaseClass0>::Method4()
		ldstr "G2_C239::Method4.6980()"
		ldstr "class G3_C1175`1<class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G3_C1175`1<class Generated703.BaseClass0>
		callvirt instance string class Generated703.G3_C1175`1<class Generated703.BaseClass0>::ClassMethod1323<object>()
		ldstr "G1_C4::ClassMethod1323.4800<System.Object>()"
		ldstr "class G3_C1175`1<class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G3_C1175`1<class Generated703.BaseClass0>
		callvirt instance string class Generated703.G3_C1175`1<class Generated703.BaseClass0>::ClassMethod1322()
		ldstr "G1_C4::ClassMethod1322.4799()"
		ldstr "class G3_C1175`1<class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G3_C1175`1<class Generated703.BaseClass0>
		callvirt instance string class Generated703.G3_C1175`1<class Generated703.BaseClass0>::ClassMethod1321()
		ldstr "G2_C239::ClassMethod1321.MI.6985()"
		ldstr "class G3_C1175`1<class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string Generated703.IBase0::Method0()
		ldstr "G3_C1175::Method0.14727()"
		ldstr "IBase0 on type class Generated703.G3_C1175`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated703.IBase0::Method1()
		ldstr "G3_C1175::Method1.14728()"
		ldstr "IBase0 on type class Generated703.G3_C1175`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated703.IBase0::Method2<object>()
		ldstr "G3_C1175::Method2.14729<System.Object>()"
		ldstr "IBase0 on type class Generated703.G3_C1175`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated703.IBase0::Method3<object>()
		ldstr "G3_C1175::Method3.14730<System.Object>()"
		ldstr "IBase0 on type class Generated703.G3_C1175`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated703.G3_C1175`1<class Generated703.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		callvirt instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::ClassMethod1323<object>()
		ldstr "G1_C4::ClassMethod1323.4800<System.Object>()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		callvirt instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::ClassMethod1322()
		ldstr "G1_C4::ClassMethod1322.4799()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		callvirt instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::ClassMethod1321()
		ldstr "G2_C239::ClassMethod1321.MI.6985()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		callvirt instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::Method6<object>()
		ldstr "G2_C239::Method6.6982<System.Object>()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		callvirt instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::Method5()
		ldstr "G2_C239::Method5.6981()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		callvirt instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::Method4()
		ldstr "G2_C239::Method4.6980()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		callvirt instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::Method7<object>()
		ldstr "G1_C4::Method7.4790<System.Object>()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated703.IBase2`2<class Generated703.BaseClass0,class Generated703.BaseClass1>::Method7<object>()
		ldstr "G2_C239::Method7.6984<System.Object>()"
		ldstr "class IBase2`2<class Generated703.BaseClass0,class Generated703.BaseClass1> on type class Generated703.G3_C1175`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated703.IBase1`1<class Generated703.BaseClass1>::Method4()
		ldstr "G2_C239::Method4.6980()"
		ldstr "class IBase1`1<class Generated703.BaseClass1> on type class Generated703.G3_C1175`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated703.IBase1`1<class Generated703.BaseClass1>::Method5()
		ldstr "G2_C239::Method5.6981()"
		ldstr "class IBase1`1<class Generated703.BaseClass1> on type class Generated703.G3_C1175`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated703.IBase1`1<class Generated703.BaseClass1>::Method6<object>()
		ldstr "G2_C239::Method6.MI.6983<System.Object>()"
		ldstr "class IBase1`1<class Generated703.BaseClass1> on type class Generated703.G3_C1175`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated703.IBase1`1<class Generated703.BaseClass0>::Method4()
		ldstr "G2_C239::Method4.6980()"
		ldstr "class IBase1`1<class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated703.IBase1`1<class Generated703.BaseClass0>::Method5()
		ldstr "G2_C239::Method5.6981()"
		ldstr "class IBase1`1<class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated703.IBase1`1<class Generated703.BaseClass0>::Method6<object>()
		ldstr "G2_C239::Method6.MI.6983<System.Object>()"
		ldstr "class IBase1`1<class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>
		callvirt instance string class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>::Method7<object>()
		ldstr "G2_C239::Method7.6984<System.Object>()"
		ldstr "class G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>
		callvirt instance string class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>::Method6<object>()
		ldstr "G2_C239::Method6.6982<System.Object>()"
		ldstr "class G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>
		callvirt instance string class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>::Method5()
		ldstr "G2_C239::Method5.6981()"
		ldstr "class G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>
		callvirt instance string class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>::Method4()
		ldstr "G2_C239::Method4.6980()"
		ldstr "class G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>
		callvirt instance string class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>::ClassMethod1323<object>()
		ldstr "G1_C4::ClassMethod1323.4800<System.Object>()"
		ldstr "class G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>
		callvirt instance string class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>::ClassMethod1322()
		ldstr "G1_C4::ClassMethod1322.4799()"
		ldstr "class G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>
		callvirt instance string class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>::ClassMethod1321()
		ldstr "G2_C239::ClassMethod1321.MI.6985()"
		ldstr "class G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated703.IBase2`2<class Generated703.BaseClass0,class Generated703.BaseClass0>::Method7<object>()
		ldstr "G2_C239::Method7.6984<System.Object>()"
		ldstr "class IBase2`2<class Generated703.BaseClass0,class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated703.G3_C1175`1<class Generated703.BaseClass1>
		callvirt instance string class Generated703.G3_C1175`1<class Generated703.BaseClass1>::ClassMethod3748<object>()
		ldstr "G3_C1175::ClassMethod3748.14734<System.Object>()"
		ldstr "class G3_C1175`1<class Generated703.BaseClass1> on type class Generated703.G3_C1175`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G3_C1175`1<class Generated703.BaseClass1>
		callvirt instance string class Generated703.G3_C1175`1<class Generated703.BaseClass1>::ClassMethod3747<object>()
		ldstr "G3_C1175::ClassMethod3747.14733<System.Object>()"
		ldstr "class G3_C1175`1<class Generated703.BaseClass1> on type class Generated703.G3_C1175`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G3_C1175`1<class Generated703.BaseClass1>
		callvirt instance string class Generated703.G3_C1175`1<class Generated703.BaseClass1>::ClassMethod3746()
		ldstr "G3_C1175::ClassMethod3746.14732()"
		ldstr "class G3_C1175`1<class Generated703.BaseClass1> on type class Generated703.G3_C1175`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G3_C1175`1<class Generated703.BaseClass1>
		callvirt instance string class Generated703.G3_C1175`1<class Generated703.BaseClass1>::ClassMethod3745()
		ldstr "G3_C1175::ClassMethod3745.14731()"
		ldstr "class G3_C1175`1<class Generated703.BaseClass1> on type class Generated703.G3_C1175`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G3_C1175`1<class Generated703.BaseClass1>
		callvirt instance string class Generated703.G3_C1175`1<class Generated703.BaseClass1>::Method3<object>()
		ldstr "G3_C1175::Method3.14730<System.Object>()"
		ldstr "class G3_C1175`1<class Generated703.BaseClass1> on type class Generated703.G3_C1175`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G3_C1175`1<class Generated703.BaseClass1>
		callvirt instance string class Generated703.G3_C1175`1<class Generated703.BaseClass1>::Method2<object>()
		ldstr "G3_C1175::Method2.14729<System.Object>()"
		ldstr "class G3_C1175`1<class Generated703.BaseClass1> on type class Generated703.G3_C1175`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G3_C1175`1<class Generated703.BaseClass1>
		callvirt instance string class Generated703.G3_C1175`1<class Generated703.BaseClass1>::Method1()
		ldstr "G3_C1175::Method1.14728()"
		ldstr "class G3_C1175`1<class Generated703.BaseClass1> on type class Generated703.G3_C1175`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G3_C1175`1<class Generated703.BaseClass1>
		callvirt instance string class Generated703.G3_C1175`1<class Generated703.BaseClass1>::Method0()
		ldstr "G3_C1175::Method0.14727()"
		ldstr "class G3_C1175`1<class Generated703.BaseClass1> on type class Generated703.G3_C1175`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G3_C1175`1<class Generated703.BaseClass1>
		callvirt instance string class Generated703.G3_C1175`1<class Generated703.BaseClass1>::Method7<object>()
		ldstr "G2_C239::Method7.6984<System.Object>()"
		ldstr "class G3_C1175`1<class Generated703.BaseClass1> on type class Generated703.G3_C1175`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G3_C1175`1<class Generated703.BaseClass1>
		callvirt instance string class Generated703.G3_C1175`1<class Generated703.BaseClass1>::Method6<object>()
		ldstr "G2_C239::Method6.6982<System.Object>()"
		ldstr "class G3_C1175`1<class Generated703.BaseClass1> on type class Generated703.G3_C1175`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G3_C1175`1<class Generated703.BaseClass1>
		callvirt instance string class Generated703.G3_C1175`1<class Generated703.BaseClass1>::Method5()
		ldstr "G2_C239::Method5.6981()"
		ldstr "class G3_C1175`1<class Generated703.BaseClass1> on type class Generated703.G3_C1175`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G3_C1175`1<class Generated703.BaseClass1>
		callvirt instance string class Generated703.G3_C1175`1<class Generated703.BaseClass1>::Method4()
		ldstr "G2_C239::Method4.6980()"
		ldstr "class G3_C1175`1<class Generated703.BaseClass1> on type class Generated703.G3_C1175`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G3_C1175`1<class Generated703.BaseClass1>
		callvirt instance string class Generated703.G3_C1175`1<class Generated703.BaseClass1>::ClassMethod1323<object>()
		ldstr "G1_C4::ClassMethod1323.4800<System.Object>()"
		ldstr "class G3_C1175`1<class Generated703.BaseClass1> on type class Generated703.G3_C1175`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G3_C1175`1<class Generated703.BaseClass1>
		callvirt instance string class Generated703.G3_C1175`1<class Generated703.BaseClass1>::ClassMethod1322()
		ldstr "G1_C4::ClassMethod1322.4799()"
		ldstr "class G3_C1175`1<class Generated703.BaseClass1> on type class Generated703.G3_C1175`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G3_C1175`1<class Generated703.BaseClass1>
		callvirt instance string class Generated703.G3_C1175`1<class Generated703.BaseClass1>::ClassMethod1321()
		ldstr "G2_C239::ClassMethod1321.MI.6985()"
		ldstr "class G3_C1175`1<class Generated703.BaseClass1> on type class Generated703.G3_C1175`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string Generated703.IBase0::Method0()
		ldstr "G3_C1175::Method0.14727()"
		ldstr "IBase0 on type class Generated703.G3_C1175`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated703.IBase0::Method1()
		ldstr "G3_C1175::Method1.14728()"
		ldstr "IBase0 on type class Generated703.G3_C1175`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated703.IBase0::Method2<object>()
		ldstr "G3_C1175::Method2.14729<System.Object>()"
		ldstr "IBase0 on type class Generated703.G3_C1175`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated703.IBase0::Method3<object>()
		ldstr "G3_C1175::Method3.14730<System.Object>()"
		ldstr "IBase0 on type class Generated703.G3_C1175`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		callvirt instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::ClassMethod1323<object>()
		ldstr "G1_C4::ClassMethod1323.4800<System.Object>()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		callvirt instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::ClassMethod1322()
		ldstr "G1_C4::ClassMethod1322.4799()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		callvirt instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::ClassMethod1321()
		ldstr "G2_C239::ClassMethod1321.MI.6985()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		callvirt instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::Method6<object>()
		ldstr "G2_C239::Method6.6982<System.Object>()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		callvirt instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::Method5()
		ldstr "G2_C239::Method5.6981()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		callvirt instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::Method4()
		ldstr "G2_C239::Method4.6980()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		callvirt instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::Method7<object>()
		ldstr "G1_C4::Method7.4790<System.Object>()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated703.IBase2`2<class Generated703.BaseClass0,class Generated703.BaseClass1>::Method7<object>()
		ldstr "G2_C239::Method7.6984<System.Object>()"
		ldstr "class IBase2`2<class Generated703.BaseClass0,class Generated703.BaseClass1> on type class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated703.IBase1`1<class Generated703.BaseClass1>::Method4()
		ldstr "G2_C239::Method4.6980()"
		ldstr "class IBase1`1<class Generated703.BaseClass1> on type class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated703.IBase1`1<class Generated703.BaseClass1>::Method5()
		ldstr "G2_C239::Method5.6981()"
		ldstr "class IBase1`1<class Generated703.BaseClass1> on type class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated703.IBase1`1<class Generated703.BaseClass1>::Method6<object>()
		ldstr "G2_C239::Method6.MI.6983<System.Object>()"
		ldstr "class IBase1`1<class Generated703.BaseClass1> on type class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated703.IBase1`1<class Generated703.BaseClass0>::Method4()
		ldstr "G2_C239::Method4.6980()"
		ldstr "class IBase1`1<class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated703.IBase1`1<class Generated703.BaseClass0>::Method5()
		ldstr "G2_C239::Method5.6981()"
		ldstr "class IBase1`1<class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated703.IBase1`1<class Generated703.BaseClass0>::Method6<object>()
		ldstr "G2_C239::Method6.MI.6983<System.Object>()"
		ldstr "class IBase1`1<class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>
		callvirt instance string class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>::Method7<object>()
		ldstr "G2_C239::Method7.6984<System.Object>()"
		ldstr "class G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>
		callvirt instance string class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>::Method6<object>()
		ldstr "G2_C239::Method6.6982<System.Object>()"
		ldstr "class G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>
		callvirt instance string class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>::Method5()
		ldstr "G2_C239::Method5.6981()"
		ldstr "class G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>
		callvirt instance string class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>::Method4()
		ldstr "G2_C239::Method4.6980()"
		ldstr "class G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>
		callvirt instance string class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>::ClassMethod1323<object>()
		ldstr "G1_C4::ClassMethod1323.4800<System.Object>()"
		ldstr "class G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>
		callvirt instance string class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>::ClassMethod1322()
		ldstr "G1_C4::ClassMethod1322.4799()"
		ldstr "class G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>
		callvirt instance string class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>::ClassMethod1321()
		ldstr "G2_C239::ClassMethod1321.MI.6985()"
		ldstr "class G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated703.IBase2`2<class Generated703.BaseClass0,class Generated703.BaseClass0>::Method7<object>()
		ldstr "G2_C239::Method7.6984<System.Object>()"
		ldstr "class IBase2`2<class Generated703.BaseClass0,class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		callvirt instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::ClassMethod1323<object>()
		ldstr "G1_C4::ClassMethod1323.4800<System.Object>()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		callvirt instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::ClassMethod1322()
		ldstr "G1_C4::ClassMethod1322.4799()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		callvirt instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::ClassMethod1321()
		ldstr "G2_C239::ClassMethod1321.MI.6985()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		callvirt instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::Method6<object>()
		ldstr "G2_C239::Method6.6982<System.Object>()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		callvirt instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::Method5()
		ldstr "G2_C239::Method5.6981()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		callvirt instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::Method4()
		ldstr "G2_C239::Method4.6980()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		callvirt instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::Method7<object>()
		ldstr "G1_C4::Method7.4790<System.Object>()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated703.IBase2`2<class Generated703.BaseClass0,class Generated703.BaseClass1>::Method7<object>()
		ldstr "G2_C239::Method7.6984<System.Object>()"
		ldstr "class IBase2`2<class Generated703.BaseClass0,class Generated703.BaseClass1> on type class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated703.IBase1`1<class Generated703.BaseClass1>::Method4()
		ldstr "G2_C239::Method4.6980()"
		ldstr "class IBase1`1<class Generated703.BaseClass1> on type class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated703.IBase1`1<class Generated703.BaseClass1>::Method5()
		ldstr "G2_C239::Method5.6981()"
		ldstr "class IBase1`1<class Generated703.BaseClass1> on type class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated703.IBase1`1<class Generated703.BaseClass1>::Method6<object>()
		ldstr "G2_C239::Method6.MI.6983<System.Object>()"
		ldstr "class IBase1`1<class Generated703.BaseClass1> on type class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated703.IBase1`1<class Generated703.BaseClass0>::Method4()
		ldstr "G2_C239::Method4.6980()"
		ldstr "class IBase1`1<class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated703.IBase1`1<class Generated703.BaseClass0>::Method5()
		ldstr "G2_C239::Method5.6981()"
		ldstr "class IBase1`1<class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated703.IBase1`1<class Generated703.BaseClass0>::Method6<object>()
		ldstr "G2_C239::Method6.MI.6983<System.Object>()"
		ldstr "class IBase1`1<class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>
		callvirt instance string class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>::Method7<object>()
		ldstr "G2_C239::Method7.6984<System.Object>()"
		ldstr "class G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1> on type class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>
		callvirt instance string class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>::Method6<object>()
		ldstr "G2_C239::Method6.6982<System.Object>()"
		ldstr "class G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1> on type class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>
		callvirt instance string class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>::Method5()
		ldstr "G2_C239::Method5.6981()"
		ldstr "class G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1> on type class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>
		callvirt instance string class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>::Method4()
		ldstr "G2_C239::Method4.6980()"
		ldstr "class G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1> on type class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>
		callvirt instance string class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>::ClassMethod1323<object>()
		ldstr "G1_C4::ClassMethod1323.4800<System.Object>()"
		ldstr "class G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1> on type class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>
		callvirt instance string class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>::ClassMethod1322()
		ldstr "G1_C4::ClassMethod1322.4799()"
		ldstr "class G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1> on type class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>
		callvirt instance string class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>::ClassMethod1321()
		ldstr "G2_C239::ClassMethod1321.MI.6985()"
		ldstr "class G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1> on type class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		callvirt instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::ClassMethod1323<object>()
		ldstr "G1_C4::ClassMethod1323.4800<System.Object>()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		callvirt instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::ClassMethod1322()
		ldstr "G1_C4::ClassMethod1322.4799()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		callvirt instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::ClassMethod1321()
		ldstr "G2_C239::ClassMethod1321.MI.6985()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		callvirt instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::Method6<object>()
		ldstr "G2_C239::Method6.6982<System.Object>()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		callvirt instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::Method5()
		ldstr "G2_C239::Method5.6981()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		callvirt instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::Method4()
		ldstr "G2_C239::Method4.6980()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		callvirt instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::Method7<object>()
		ldstr "G1_C4::Method7.4790<System.Object>()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated703.IBase2`2<class Generated703.BaseClass0,class Generated703.BaseClass1>::Method7<object>()
		ldstr "G2_C239::Method7.6984<System.Object>()"
		ldstr "class IBase2`2<class Generated703.BaseClass0,class Generated703.BaseClass1> on type class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated703.IBase1`1<class Generated703.BaseClass1>::Method4()
		ldstr "G2_C239::Method4.6980()"
		ldstr "class IBase1`1<class Generated703.BaseClass1> on type class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated703.IBase1`1<class Generated703.BaseClass1>::Method5()
		ldstr "G2_C239::Method5.6981()"
		ldstr "class IBase1`1<class Generated703.BaseClass1> on type class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated703.IBase1`1<class Generated703.BaseClass1>::Method6<object>()
		ldstr "G2_C239::Method6.MI.6983<System.Object>()"
		ldstr "class IBase1`1<class Generated703.BaseClass1> on type class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated703.IBase1`1<class Generated703.BaseClass0>::Method4()
		ldstr "G2_C239::Method4.6980()"
		ldstr "class IBase1`1<class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated703.IBase1`1<class Generated703.BaseClass0>::Method5()
		ldstr "G2_C239::Method5.6981()"
		ldstr "class IBase1`1<class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated703.IBase1`1<class Generated703.BaseClass0>::Method6<object>()
		ldstr "G2_C239::Method6.MI.6983<System.Object>()"
		ldstr "class IBase1`1<class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>
		callvirt instance string class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>::Method7<object>()
		ldstr "G2_C239::Method7.6984<System.Object>()"
		ldstr "class G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>
		callvirt instance string class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>::Method6<object>()
		ldstr "G2_C239::Method6.6982<System.Object>()"
		ldstr "class G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>
		callvirt instance string class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>::Method5()
		ldstr "G2_C239::Method5.6981()"
		ldstr "class G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>
		callvirt instance string class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>::Method4()
		ldstr "G2_C239::Method4.6980()"
		ldstr "class G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>
		callvirt instance string class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>::ClassMethod1323<object>()
		ldstr "G1_C4::ClassMethod1323.4800<System.Object>()"
		ldstr "class G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>
		callvirt instance string class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>::ClassMethod1322()
		ldstr "G1_C4::ClassMethod1322.4799()"
		ldstr "class G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>
		callvirt instance string class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>::ClassMethod1321()
		ldstr "G2_C239::ClassMethod1321.MI.6985()"
		ldstr "class G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated703.IBase2`2<class Generated703.BaseClass0,class Generated703.BaseClass0>::Method7<object>()
		ldstr "G2_C239::Method7.6984<System.Object>()"
		ldstr "class IBase2`2<class Generated703.BaseClass0,class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		callvirt instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::ClassMethod1323<object>()
		ldstr "G1_C4::ClassMethod1323.4800<System.Object>()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		callvirt instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::ClassMethod1322()
		ldstr "G1_C4::ClassMethod1322.4799()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		callvirt instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::ClassMethod1321()
		ldstr "G2_C239::ClassMethod1321.MI.6985()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		callvirt instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::Method6<object>()
		ldstr "G2_C239::Method6.6982<System.Object>()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		callvirt instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::Method5()
		ldstr "G2_C239::Method5.6981()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		callvirt instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::Method4()
		ldstr "G2_C239::Method4.6980()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		callvirt instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::Method7<object>()
		ldstr "G1_C4::Method7.4790<System.Object>()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated703.IBase2`2<class Generated703.BaseClass0,class Generated703.BaseClass1>::Method7<object>()
		ldstr "G2_C239::Method7.6984<System.Object>()"
		ldstr "class IBase2`2<class Generated703.BaseClass0,class Generated703.BaseClass1> on type class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated703.IBase1`1<class Generated703.BaseClass1>::Method4()
		ldstr "G2_C239::Method4.6980()"
		ldstr "class IBase1`1<class Generated703.BaseClass1> on type class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated703.IBase1`1<class Generated703.BaseClass1>::Method5()
		ldstr "G2_C239::Method5.6981()"
		ldstr "class IBase1`1<class Generated703.BaseClass1> on type class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated703.IBase1`1<class Generated703.BaseClass1>::Method6<object>()
		ldstr "G2_C239::Method6.MI.6983<System.Object>()"
		ldstr "class IBase1`1<class Generated703.BaseClass1> on type class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated703.IBase1`1<class Generated703.BaseClass0>::Method4()
		ldstr "G2_C239::Method4.6980()"
		ldstr "class IBase1`1<class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated703.IBase1`1<class Generated703.BaseClass0>::Method5()
		ldstr "G2_C239::Method5.6981()"
		ldstr "class IBase1`1<class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated703.IBase1`1<class Generated703.BaseClass0>::Method6<object>()
		ldstr "G2_C239::Method6.MI.6983<System.Object>()"
		ldstr "class IBase1`1<class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>
		callvirt instance string class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>::Method7<object>()
		ldstr "G2_C239::Method7.6984<System.Object>()"
		ldstr "class G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1> on type class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>
		callvirt instance string class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>::Method6<object>()
		ldstr "G2_C239::Method6.6982<System.Object>()"
		ldstr "class G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1> on type class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>
		callvirt instance string class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>::Method5()
		ldstr "G2_C239::Method5.6981()"
		ldstr "class G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1> on type class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>
		callvirt instance string class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>::Method4()
		ldstr "G2_C239::Method4.6980()"
		ldstr "class G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1> on type class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>
		callvirt instance string class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>::ClassMethod1323<object>()
		ldstr "G1_C4::ClassMethod1323.4800<System.Object>()"
		ldstr "class G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1> on type class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>
		callvirt instance string class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>::ClassMethod1322()
		ldstr "G1_C4::ClassMethod1322.4799()"
		ldstr "class G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1> on type class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>
		callvirt instance string class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>::ClassMethod1321()
		ldstr "G2_C239::ClassMethod1321.MI.6985()"
		ldstr "class G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1> on type class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated703.G1_C4`1<class Generated703.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		callvirt instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::ClassMethod1323<object>()
		ldstr "G1_C4::ClassMethod1323.4800<System.Object>()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G1_C4`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		callvirt instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::ClassMethod1322()
		ldstr "G1_C4::ClassMethod1322.4799()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G1_C4`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		callvirt instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::ClassMethod1321()
		ldstr "G1_C4::ClassMethod1321.4798()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G1_C4`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		callvirt instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::Method6<object>()
		ldstr "G1_C4::Method6.4796<System.Object>()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G1_C4`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		callvirt instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::Method5()
		ldstr "G1_C4::Method5.4794()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G1_C4`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		callvirt instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::Method4()
		ldstr "G1_C4::Method4.4792()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G1_C4`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		callvirt instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::Method7<object>()
		ldstr "G1_C4::Method7.4790<System.Object>()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G1_C4`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated703.IBase2`2<class Generated703.BaseClass0,class Generated703.BaseClass1>::Method7<object>()
		ldstr "G1_C4::Method7.MI.4791<System.Object>()"
		ldstr "class IBase2`2<class Generated703.BaseClass0,class Generated703.BaseClass1> on type class Generated703.G1_C4`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated703.IBase1`1<class Generated703.BaseClass1>::Method4()
		ldstr "G1_C4::Method4.MI.4793()"
		ldstr "class IBase1`1<class Generated703.BaseClass1> on type class Generated703.G1_C4`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated703.IBase1`1<class Generated703.BaseClass1>::Method5()
		ldstr "G1_C4::Method5.MI.4795()"
		ldstr "class IBase1`1<class Generated703.BaseClass1> on type class Generated703.G1_C4`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated703.IBase1`1<class Generated703.BaseClass1>::Method6<object>()
		ldstr "G1_C4::Method6.MI.4797<System.Object>()"
		ldstr "class IBase1`1<class Generated703.BaseClass1> on type class Generated703.G1_C4`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated703.IBase1`1<class Generated703.BaseClass0>::Method4()
		ldstr "G1_C4::Method4.MI.4793()"
		ldstr "class IBase1`1<class Generated703.BaseClass0> on type class Generated703.G1_C4`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated703.IBase1`1<class Generated703.BaseClass0>::Method5()
		ldstr "G1_C4::Method5.MI.4795()"
		ldstr "class IBase1`1<class Generated703.BaseClass0> on type class Generated703.G1_C4`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated703.IBase1`1<class Generated703.BaseClass0>::Method6<object>()
		ldstr "G1_C4::Method6.MI.4797<System.Object>()"
		ldstr "class IBase1`1<class Generated703.BaseClass0> on type class Generated703.G1_C4`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated703.G1_C4`1<class Generated703.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass1>
		callvirt instance string class Generated703.G1_C4`1<class Generated703.BaseClass1>::ClassMethod1323<object>()
		ldstr "G1_C4::ClassMethod1323.4800<System.Object>()"
		ldstr "class G1_C4`1<class Generated703.BaseClass1> on type class Generated703.G1_C4`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass1>
		callvirt instance string class Generated703.G1_C4`1<class Generated703.BaseClass1>::ClassMethod1322()
		ldstr "G1_C4::ClassMethod1322.4799()"
		ldstr "class G1_C4`1<class Generated703.BaseClass1> on type class Generated703.G1_C4`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass1>
		callvirt instance string class Generated703.G1_C4`1<class Generated703.BaseClass1>::ClassMethod1321()
		ldstr "G1_C4::ClassMethod1321.4798()"
		ldstr "class G1_C4`1<class Generated703.BaseClass1> on type class Generated703.G1_C4`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass1>
		callvirt instance string class Generated703.G1_C4`1<class Generated703.BaseClass1>::Method6<object>()
		ldstr "G1_C4::Method6.4796<System.Object>()"
		ldstr "class G1_C4`1<class Generated703.BaseClass1> on type class Generated703.G1_C4`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass1>
		callvirt instance string class Generated703.G1_C4`1<class Generated703.BaseClass1>::Method5()
		ldstr "G1_C4::Method5.4794()"
		ldstr "class G1_C4`1<class Generated703.BaseClass1> on type class Generated703.G1_C4`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass1>
		callvirt instance string class Generated703.G1_C4`1<class Generated703.BaseClass1>::Method4()
		ldstr "G1_C4::Method4.4792()"
		ldstr "class G1_C4`1<class Generated703.BaseClass1> on type class Generated703.G1_C4`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass1>
		callvirt instance string class Generated703.G1_C4`1<class Generated703.BaseClass1>::Method7<object>()
		ldstr "G1_C4::Method7.4790<System.Object>()"
		ldstr "class G1_C4`1<class Generated703.BaseClass1> on type class Generated703.G1_C4`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated703.IBase2`2<class Generated703.BaseClass0,class Generated703.BaseClass1>::Method7<object>()
		ldstr "G1_C4::Method7.MI.4791<System.Object>()"
		ldstr "class IBase2`2<class Generated703.BaseClass0,class Generated703.BaseClass1> on type class Generated703.G1_C4`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated703.IBase1`1<class Generated703.BaseClass1>::Method4()
		ldstr "G1_C4::Method4.MI.4793()"
		ldstr "class IBase1`1<class Generated703.BaseClass1> on type class Generated703.G1_C4`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated703.IBase1`1<class Generated703.BaseClass1>::Method5()
		ldstr "G1_C4::Method5.MI.4795()"
		ldstr "class IBase1`1<class Generated703.BaseClass1> on type class Generated703.G1_C4`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated703.IBase1`1<class Generated703.BaseClass1>::Method6<object>()
		ldstr "G1_C4::Method6.MI.4797<System.Object>()"
		ldstr "class IBase1`1<class Generated703.BaseClass1> on type class Generated703.G1_C4`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated703.IBase1`1<class Generated703.BaseClass0>::Method4()
		ldstr "G1_C4::Method4.MI.4793()"
		ldstr "class IBase1`1<class Generated703.BaseClass0> on type class Generated703.G1_C4`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated703.IBase1`1<class Generated703.BaseClass0>::Method5()
		ldstr "G1_C4::Method5.MI.4795()"
		ldstr "class IBase1`1<class Generated703.BaseClass0> on type class Generated703.G1_C4`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated703.IBase1`1<class Generated703.BaseClass0>::Method6<object>()
		ldstr "G1_C4::Method6.MI.4797<System.Object>()"
		ldstr "class IBase1`1<class Generated703.BaseClass0> on type class Generated703.G1_C4`1<class Generated703.BaseClass1>"
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
		newobj instance void class Generated703.G3_C1175`1<class Generated703.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C239::ClassMethod1321.MI.6985()#G1_C4::ClassMethod1322.4799()#G1_C4::ClassMethod1323.4800<System.Object>()#G2_C239::Method4.6980()#G2_C239::Method5.6981()#G2_C239::Method6.6982<System.Object>()#G1_C4::Method7.4790<System.Object>()#"
		call void Generated703.Generated703::M.G1_C4.T<class Generated703.BaseClass0,class Generated703.G3_C1175`1<class Generated703.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C239::ClassMethod1321.MI.6985()#G1_C4::ClassMethod1322.4799()#G1_C4::ClassMethod1323.4800<System.Object>()#G2_C239::Method4.6980()#G2_C239::Method5.6981()#G2_C239::Method6.6982<System.Object>()#G1_C4::Method7.4790<System.Object>()#"
		call void Generated703.Generated703::M.G1_C4.A<class Generated703.G3_C1175`1<class Generated703.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C239::Method7.6984<System.Object>()#"
		call void Generated703.Generated703::M.IBase2.T.T<class Generated703.BaseClass0,class Generated703.BaseClass1,class Generated703.G3_C1175`1<class Generated703.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C239::Method7.6984<System.Object>()#"
		call void Generated703.Generated703::M.IBase2.A.T<class Generated703.BaseClass1,class Generated703.G3_C1175`1<class Generated703.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C239::Method7.6984<System.Object>()#"
		call void Generated703.Generated703::M.IBase2.A.B<class Generated703.G3_C1175`1<class Generated703.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C239::Method4.6980()#G2_C239::Method5.6981()#G2_C239::Method6.MI.6983<System.Object>()#"
		call void Generated703.Generated703::M.IBase1.T<class Generated703.BaseClass1,class Generated703.G3_C1175`1<class Generated703.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C239::Method4.6980()#G2_C239::Method5.6981()#G2_C239::Method6.MI.6983<System.Object>()#"
		call void Generated703.Generated703::M.IBase1.B<class Generated703.G3_C1175`1<class Generated703.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C239::Method4.6980()#G2_C239::Method5.6981()#G2_C239::Method6.MI.6983<System.Object>()#"
		call void Generated703.Generated703::M.IBase1.T<class Generated703.BaseClass0,class Generated703.G3_C1175`1<class Generated703.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C239::Method4.6980()#G2_C239::Method5.6981()#G2_C239::Method6.MI.6983<System.Object>()#"
		call void Generated703.Generated703::M.IBase1.A<class Generated703.G3_C1175`1<class Generated703.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C239::ClassMethod1321.MI.6985()#G1_C4::ClassMethod1322.4799()#G1_C4::ClassMethod1323.4800<System.Object>()#G2_C239::Method4.6980()#G2_C239::Method5.6981()#G2_C239::Method6.6982<System.Object>()#G2_C239::Method7.6984<System.Object>()#"
		call void Generated703.Generated703::M.G2_C239.T.T<class Generated703.BaseClass0,class Generated703.BaseClass0,class Generated703.G3_C1175`1<class Generated703.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C239::ClassMethod1321.MI.6985()#G1_C4::ClassMethod1322.4799()#G1_C4::ClassMethod1323.4800<System.Object>()#G2_C239::Method4.6980()#G2_C239::Method5.6981()#G2_C239::Method6.6982<System.Object>()#G2_C239::Method7.6984<System.Object>()#"
		call void Generated703.Generated703::M.G2_C239.A.T<class Generated703.BaseClass0,class Generated703.G3_C1175`1<class Generated703.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C239::ClassMethod1321.MI.6985()#G1_C4::ClassMethod1322.4799()#G1_C4::ClassMethod1323.4800<System.Object>()#G2_C239::Method4.6980()#G2_C239::Method5.6981()#G2_C239::Method6.6982<System.Object>()#G2_C239::Method7.6984<System.Object>()#"
		call void Generated703.Generated703::M.G2_C239.A.A<class Generated703.G3_C1175`1<class Generated703.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C239::Method7.6984<System.Object>()#"
		call void Generated703.Generated703::M.IBase2.T.T<class Generated703.BaseClass0,class Generated703.BaseClass0,class Generated703.G3_C1175`1<class Generated703.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C239::Method7.6984<System.Object>()#"
		call void Generated703.Generated703::M.IBase2.A.T<class Generated703.BaseClass0,class Generated703.G3_C1175`1<class Generated703.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C239::Method7.6984<System.Object>()#"
		call void Generated703.Generated703::M.IBase2.A.A<class Generated703.G3_C1175`1<class Generated703.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C239::ClassMethod1321.MI.6985()#G1_C4::ClassMethod1322.4799()#G1_C4::ClassMethod1323.4800<System.Object>()#G3_C1175::ClassMethod3745.14731()#G3_C1175::ClassMethod3746.14732()#G3_C1175::ClassMethod3747.14733<System.Object>()#G3_C1175::ClassMethod3748.14734<System.Object>()#G3_C1175::Method0.14727()#G3_C1175::Method1.14728()#G3_C1175::Method2.14729<System.Object>()#G3_C1175::Method3.14730<System.Object>()#G2_C239::Method4.6980()#G2_C239::Method5.6981()#G2_C239::Method6.6982<System.Object>()#G2_C239::Method7.6984<System.Object>()#"
		call void Generated703.Generated703::M.G3_C1175.T<class Generated703.BaseClass0,class Generated703.G3_C1175`1<class Generated703.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C239::ClassMethod1321.MI.6985()#G1_C4::ClassMethod1322.4799()#G1_C4::ClassMethod1323.4800<System.Object>()#G3_C1175::ClassMethod3745.14731()#G3_C1175::ClassMethod3746.14732()#G3_C1175::ClassMethod3747.14733<System.Object>()#G3_C1175::ClassMethod3748.14734<System.Object>()#G3_C1175::Method0.14727()#G3_C1175::Method1.14728()#G3_C1175::Method2.14729<System.Object>()#G3_C1175::Method3.14730<System.Object>()#G2_C239::Method4.6980()#G2_C239::Method5.6981()#G2_C239::Method6.6982<System.Object>()#G2_C239::Method7.6984<System.Object>()#"
		call void Generated703.Generated703::M.G3_C1175.A<class Generated703.G3_C1175`1<class Generated703.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1175::Method0.14727()#G3_C1175::Method1.14728()#G3_C1175::Method2.14729<System.Object>()#G3_C1175::Method3.14730<System.Object>()#"
		call void Generated703.Generated703::M.IBase0<class Generated703.G3_C1175`1<class Generated703.BaseClass0>>(!!0,string)
		newobj instance void class Generated703.G3_C1175`1<class Generated703.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C239::ClassMethod1321.MI.6985()#G1_C4::ClassMethod1322.4799()#G1_C4::ClassMethod1323.4800<System.Object>()#G2_C239::Method4.6980()#G2_C239::Method5.6981()#G2_C239::Method6.6982<System.Object>()#G1_C4::Method7.4790<System.Object>()#"
		call void Generated703.Generated703::M.G1_C4.T<class Generated703.BaseClass0,class Generated703.G3_C1175`1<class Generated703.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C239::ClassMethod1321.MI.6985()#G1_C4::ClassMethod1322.4799()#G1_C4::ClassMethod1323.4800<System.Object>()#G2_C239::Method4.6980()#G2_C239::Method5.6981()#G2_C239::Method6.6982<System.Object>()#G1_C4::Method7.4790<System.Object>()#"
		call void Generated703.Generated703::M.G1_C4.A<class Generated703.G3_C1175`1<class Generated703.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C239::Method7.6984<System.Object>()#"
		call void Generated703.Generated703::M.IBase2.T.T<class Generated703.BaseClass0,class Generated703.BaseClass1,class Generated703.G3_C1175`1<class Generated703.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C239::Method7.6984<System.Object>()#"
		call void Generated703.Generated703::M.IBase2.A.T<class Generated703.BaseClass1,class Generated703.G3_C1175`1<class Generated703.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C239::Method7.6984<System.Object>()#"
		call void Generated703.Generated703::M.IBase2.A.B<class Generated703.G3_C1175`1<class Generated703.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C239::Method4.6980()#G2_C239::Method5.6981()#G2_C239::Method6.MI.6983<System.Object>()#"
		call void Generated703.Generated703::M.IBase1.T<class Generated703.BaseClass1,class Generated703.G3_C1175`1<class Generated703.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C239::Method4.6980()#G2_C239::Method5.6981()#G2_C239::Method6.MI.6983<System.Object>()#"
		call void Generated703.Generated703::M.IBase1.B<class Generated703.G3_C1175`1<class Generated703.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C239::Method4.6980()#G2_C239::Method5.6981()#G2_C239::Method6.MI.6983<System.Object>()#"
		call void Generated703.Generated703::M.IBase1.T<class Generated703.BaseClass0,class Generated703.G3_C1175`1<class Generated703.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C239::Method4.6980()#G2_C239::Method5.6981()#G2_C239::Method6.MI.6983<System.Object>()#"
		call void Generated703.Generated703::M.IBase1.A<class Generated703.G3_C1175`1<class Generated703.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C239::ClassMethod1321.MI.6985()#G1_C4::ClassMethod1322.4799()#G1_C4::ClassMethod1323.4800<System.Object>()#G2_C239::Method4.6980()#G2_C239::Method5.6981()#G2_C239::Method6.6982<System.Object>()#G2_C239::Method7.6984<System.Object>()#"
		call void Generated703.Generated703::M.G2_C239.T.T<class Generated703.BaseClass1,class Generated703.BaseClass0,class Generated703.G3_C1175`1<class Generated703.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C239::ClassMethod1321.MI.6985()#G1_C4::ClassMethod1322.4799()#G1_C4::ClassMethod1323.4800<System.Object>()#G2_C239::Method4.6980()#G2_C239::Method5.6981()#G2_C239::Method6.6982<System.Object>()#G2_C239::Method7.6984<System.Object>()#"
		call void Generated703.Generated703::M.G2_C239.B.T<class Generated703.BaseClass0,class Generated703.G3_C1175`1<class Generated703.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C239::ClassMethod1321.MI.6985()#G1_C4::ClassMethod1322.4799()#G1_C4::ClassMethod1323.4800<System.Object>()#G2_C239::Method4.6980()#G2_C239::Method5.6981()#G2_C239::Method6.6982<System.Object>()#G2_C239::Method7.6984<System.Object>()#"
		call void Generated703.Generated703::M.G2_C239.B.A<class Generated703.G3_C1175`1<class Generated703.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C239::Method7.6984<System.Object>()#"
		call void Generated703.Generated703::M.IBase2.T.T<class Generated703.BaseClass0,class Generated703.BaseClass0,class Generated703.G3_C1175`1<class Generated703.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C239::Method7.6984<System.Object>()#"
		call void Generated703.Generated703::M.IBase2.A.T<class Generated703.BaseClass0,class Generated703.G3_C1175`1<class Generated703.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C239::Method7.6984<System.Object>()#"
		call void Generated703.Generated703::M.IBase2.A.A<class Generated703.G3_C1175`1<class Generated703.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C239::ClassMethod1321.MI.6985()#G1_C4::ClassMethod1322.4799()#G1_C4::ClassMethod1323.4800<System.Object>()#G3_C1175::ClassMethod3745.14731()#G3_C1175::ClassMethod3746.14732()#G3_C1175::ClassMethod3747.14733<System.Object>()#G3_C1175::ClassMethod3748.14734<System.Object>()#G3_C1175::Method0.14727()#G3_C1175::Method1.14728()#G3_C1175::Method2.14729<System.Object>()#G3_C1175::Method3.14730<System.Object>()#G2_C239::Method4.6980()#G2_C239::Method5.6981()#G2_C239::Method6.6982<System.Object>()#G2_C239::Method7.6984<System.Object>()#"
		call void Generated703.Generated703::M.G3_C1175.T<class Generated703.BaseClass1,class Generated703.G3_C1175`1<class Generated703.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C239::ClassMethod1321.MI.6985()#G1_C4::ClassMethod1322.4799()#G1_C4::ClassMethod1323.4800<System.Object>()#G3_C1175::ClassMethod3745.14731()#G3_C1175::ClassMethod3746.14732()#G3_C1175::ClassMethod3747.14733<System.Object>()#G3_C1175::ClassMethod3748.14734<System.Object>()#G3_C1175::Method0.14727()#G3_C1175::Method1.14728()#G3_C1175::Method2.14729<System.Object>()#G3_C1175::Method3.14730<System.Object>()#G2_C239::Method4.6980()#G2_C239::Method5.6981()#G2_C239::Method6.6982<System.Object>()#G2_C239::Method7.6984<System.Object>()#"
		call void Generated703.Generated703::M.G3_C1175.B<class Generated703.G3_C1175`1<class Generated703.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1175::Method0.14727()#G3_C1175::Method1.14728()#G3_C1175::Method2.14729<System.Object>()#G3_C1175::Method3.14730<System.Object>()#"
		call void Generated703.Generated703::M.IBase0<class Generated703.G3_C1175`1<class Generated703.BaseClass1>>(!!0,string)
		newobj instance void class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C239::ClassMethod1321.MI.6985()#G1_C4::ClassMethod1322.4799()#G1_C4::ClassMethod1323.4800<System.Object>()#G2_C239::Method4.6980()#G2_C239::Method5.6981()#G2_C239::Method6.6982<System.Object>()#G1_C4::Method7.4790<System.Object>()#"
		call void Generated703.Generated703::M.G1_C4.T<class Generated703.BaseClass0,class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C239::ClassMethod1321.MI.6985()#G1_C4::ClassMethod1322.4799()#G1_C4::ClassMethod1323.4800<System.Object>()#G2_C239::Method4.6980()#G2_C239::Method5.6981()#G2_C239::Method6.6982<System.Object>()#G1_C4::Method7.4790<System.Object>()#"
		call void Generated703.Generated703::M.G1_C4.A<class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C239::Method7.6984<System.Object>()#"
		call void Generated703.Generated703::M.IBase2.T.T<class Generated703.BaseClass0,class Generated703.BaseClass1,class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C239::Method7.6984<System.Object>()#"
		call void Generated703.Generated703::M.IBase2.A.T<class Generated703.BaseClass1,class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C239::Method7.6984<System.Object>()#"
		call void Generated703.Generated703::M.IBase2.A.B<class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C239::Method4.6980()#G2_C239::Method5.6981()#G2_C239::Method6.MI.6983<System.Object>()#"
		call void Generated703.Generated703::M.IBase1.T<class Generated703.BaseClass1,class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C239::Method4.6980()#G2_C239::Method5.6981()#G2_C239::Method6.MI.6983<System.Object>()#"
		call void Generated703.Generated703::M.IBase1.B<class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C239::Method4.6980()#G2_C239::Method5.6981()#G2_C239::Method6.MI.6983<System.Object>()#"
		call void Generated703.Generated703::M.IBase1.T<class Generated703.BaseClass0,class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C239::Method4.6980()#G2_C239::Method5.6981()#G2_C239::Method6.MI.6983<System.Object>()#"
		call void Generated703.Generated703::M.IBase1.A<class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C239::ClassMethod1321.MI.6985()#G1_C4::ClassMethod1322.4799()#G1_C4::ClassMethod1323.4800<System.Object>()#G2_C239::Method4.6980()#G2_C239::Method5.6981()#G2_C239::Method6.6982<System.Object>()#G2_C239::Method7.6984<System.Object>()#"
		call void Generated703.Generated703::M.G2_C239.T.T<class Generated703.BaseClass0,class Generated703.BaseClass0,class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C239::ClassMethod1321.MI.6985()#G1_C4::ClassMethod1322.4799()#G1_C4::ClassMethod1323.4800<System.Object>()#G2_C239::Method4.6980()#G2_C239::Method5.6981()#G2_C239::Method6.6982<System.Object>()#G2_C239::Method7.6984<System.Object>()#"
		call void Generated703.Generated703::M.G2_C239.A.T<class Generated703.BaseClass0,class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C239::ClassMethod1321.MI.6985()#G1_C4::ClassMethod1322.4799()#G1_C4::ClassMethod1323.4800<System.Object>()#G2_C239::Method4.6980()#G2_C239::Method5.6981()#G2_C239::Method6.6982<System.Object>()#G2_C239::Method7.6984<System.Object>()#"
		call void Generated703.Generated703::M.G2_C239.A.A<class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C239::Method7.6984<System.Object>()#"
		call void Generated703.Generated703::M.IBase2.T.T<class Generated703.BaseClass0,class Generated703.BaseClass0,class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C239::Method7.6984<System.Object>()#"
		call void Generated703.Generated703::M.IBase2.A.T<class Generated703.BaseClass0,class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C239::Method7.6984<System.Object>()#"
		call void Generated703.Generated703::M.IBase2.A.A<class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>>(!!0,string)
		newobj instance void class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C239::ClassMethod1321.MI.6985()#G1_C4::ClassMethod1322.4799()#G1_C4::ClassMethod1323.4800<System.Object>()#G2_C239::Method4.6980()#G2_C239::Method5.6981()#G2_C239::Method6.6982<System.Object>()#G1_C4::Method7.4790<System.Object>()#"
		call void Generated703.Generated703::M.G1_C4.T<class Generated703.BaseClass0,class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C239::ClassMethod1321.MI.6985()#G1_C4::ClassMethod1322.4799()#G1_C4::ClassMethod1323.4800<System.Object>()#G2_C239::Method4.6980()#G2_C239::Method5.6981()#G2_C239::Method6.6982<System.Object>()#G1_C4::Method7.4790<System.Object>()#"
		call void Generated703.Generated703::M.G1_C4.A<class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C239::Method7.6984<System.Object>()#"
		call void Generated703.Generated703::M.IBase2.T.T<class Generated703.BaseClass0,class Generated703.BaseClass1,class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C239::Method7.6984<System.Object>()#"
		call void Generated703.Generated703::M.IBase2.A.T<class Generated703.BaseClass1,class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C239::Method7.6984<System.Object>()#"
		call void Generated703.Generated703::M.IBase2.A.B<class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C239::Method4.6980()#G2_C239::Method5.6981()#G2_C239::Method6.MI.6983<System.Object>()#"
		call void Generated703.Generated703::M.IBase1.T<class Generated703.BaseClass1,class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C239::Method4.6980()#G2_C239::Method5.6981()#G2_C239::Method6.MI.6983<System.Object>()#"
		call void Generated703.Generated703::M.IBase1.B<class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C239::Method4.6980()#G2_C239::Method5.6981()#G2_C239::Method6.MI.6983<System.Object>()#"
		call void Generated703.Generated703::M.IBase1.T<class Generated703.BaseClass0,class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C239::Method4.6980()#G2_C239::Method5.6981()#G2_C239::Method6.MI.6983<System.Object>()#"
		call void Generated703.Generated703::M.IBase1.A<class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C239::ClassMethod1321.MI.6985()#G1_C4::ClassMethod1322.4799()#G1_C4::ClassMethod1323.4800<System.Object>()#G2_C239::Method4.6980()#G2_C239::Method5.6981()#G2_C239::Method6.6982<System.Object>()#G2_C239::Method7.6984<System.Object>()#"
		call void Generated703.Generated703::M.G2_C239.T.T<class Generated703.BaseClass0,class Generated703.BaseClass1,class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C239::ClassMethod1321.MI.6985()#G1_C4::ClassMethod1322.4799()#G1_C4::ClassMethod1323.4800<System.Object>()#G2_C239::Method4.6980()#G2_C239::Method5.6981()#G2_C239::Method6.6982<System.Object>()#G2_C239::Method7.6984<System.Object>()#"
		call void Generated703.Generated703::M.G2_C239.A.T<class Generated703.BaseClass1,class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C239::ClassMethod1321.MI.6985()#G1_C4::ClassMethod1322.4799()#G1_C4::ClassMethod1323.4800<System.Object>()#G2_C239::Method4.6980()#G2_C239::Method5.6981()#G2_C239::Method6.6982<System.Object>()#G2_C239::Method7.6984<System.Object>()#"
		call void Generated703.Generated703::M.G2_C239.A.B<class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>>(!!0,string)
		newobj instance void class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C239::ClassMethod1321.MI.6985()#G1_C4::ClassMethod1322.4799()#G1_C4::ClassMethod1323.4800<System.Object>()#G2_C239::Method4.6980()#G2_C239::Method5.6981()#G2_C239::Method6.6982<System.Object>()#G1_C4::Method7.4790<System.Object>()#"
		call void Generated703.Generated703::M.G1_C4.T<class Generated703.BaseClass0,class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C239::ClassMethod1321.MI.6985()#G1_C4::ClassMethod1322.4799()#G1_C4::ClassMethod1323.4800<System.Object>()#G2_C239::Method4.6980()#G2_C239::Method5.6981()#G2_C239::Method6.6982<System.Object>()#G1_C4::Method7.4790<System.Object>()#"
		call void Generated703.Generated703::M.G1_C4.A<class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C239::Method7.6984<System.Object>()#"
		call void Generated703.Generated703::M.IBase2.T.T<class Generated703.BaseClass0,class Generated703.BaseClass1,class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C239::Method7.6984<System.Object>()#"
		call void Generated703.Generated703::M.IBase2.A.T<class Generated703.BaseClass1,class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C239::Method7.6984<System.Object>()#"
		call void Generated703.Generated703::M.IBase2.A.B<class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C239::Method4.6980()#G2_C239::Method5.6981()#G2_C239::Method6.MI.6983<System.Object>()#"
		call void Generated703.Generated703::M.IBase1.T<class Generated703.BaseClass1,class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C239::Method4.6980()#G2_C239::Method5.6981()#G2_C239::Method6.MI.6983<System.Object>()#"
		call void Generated703.Generated703::M.IBase1.B<class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C239::Method4.6980()#G2_C239::Method5.6981()#G2_C239::Method6.MI.6983<System.Object>()#"
		call void Generated703.Generated703::M.IBase1.T<class Generated703.BaseClass0,class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C239::Method4.6980()#G2_C239::Method5.6981()#G2_C239::Method6.MI.6983<System.Object>()#"
		call void Generated703.Generated703::M.IBase1.A<class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C239::ClassMethod1321.MI.6985()#G1_C4::ClassMethod1322.4799()#G1_C4::ClassMethod1323.4800<System.Object>()#G2_C239::Method4.6980()#G2_C239::Method5.6981()#G2_C239::Method6.6982<System.Object>()#G2_C239::Method7.6984<System.Object>()#"
		call void Generated703.Generated703::M.G2_C239.T.T<class Generated703.BaseClass1,class Generated703.BaseClass0,class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C239::ClassMethod1321.MI.6985()#G1_C4::ClassMethod1322.4799()#G1_C4::ClassMethod1323.4800<System.Object>()#G2_C239::Method4.6980()#G2_C239::Method5.6981()#G2_C239::Method6.6982<System.Object>()#G2_C239::Method7.6984<System.Object>()#"
		call void Generated703.Generated703::M.G2_C239.B.T<class Generated703.BaseClass0,class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C239::ClassMethod1321.MI.6985()#G1_C4::ClassMethod1322.4799()#G1_C4::ClassMethod1323.4800<System.Object>()#G2_C239::Method4.6980()#G2_C239::Method5.6981()#G2_C239::Method6.6982<System.Object>()#G2_C239::Method7.6984<System.Object>()#"
		call void Generated703.Generated703::M.G2_C239.B.A<class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C239::Method7.6984<System.Object>()#"
		call void Generated703.Generated703::M.IBase2.T.T<class Generated703.BaseClass0,class Generated703.BaseClass0,class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C239::Method7.6984<System.Object>()#"
		call void Generated703.Generated703::M.IBase2.A.T<class Generated703.BaseClass0,class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C239::Method7.6984<System.Object>()#"
		call void Generated703.Generated703::M.IBase2.A.A<class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>>(!!0,string)
		newobj instance void class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C239::ClassMethod1321.MI.6985()#G1_C4::ClassMethod1322.4799()#G1_C4::ClassMethod1323.4800<System.Object>()#G2_C239::Method4.6980()#G2_C239::Method5.6981()#G2_C239::Method6.6982<System.Object>()#G1_C4::Method7.4790<System.Object>()#"
		call void Generated703.Generated703::M.G1_C4.T<class Generated703.BaseClass0,class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C239::ClassMethod1321.MI.6985()#G1_C4::ClassMethod1322.4799()#G1_C4::ClassMethod1323.4800<System.Object>()#G2_C239::Method4.6980()#G2_C239::Method5.6981()#G2_C239::Method6.6982<System.Object>()#G1_C4::Method7.4790<System.Object>()#"
		call void Generated703.Generated703::M.G1_C4.A<class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C239::Method7.6984<System.Object>()#"
		call void Generated703.Generated703::M.IBase2.T.T<class Generated703.BaseClass0,class Generated703.BaseClass1,class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C239::Method7.6984<System.Object>()#"
		call void Generated703.Generated703::M.IBase2.A.T<class Generated703.BaseClass1,class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C239::Method7.6984<System.Object>()#"
		call void Generated703.Generated703::M.IBase2.A.B<class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C239::Method4.6980()#G2_C239::Method5.6981()#G2_C239::Method6.MI.6983<System.Object>()#"
		call void Generated703.Generated703::M.IBase1.T<class Generated703.BaseClass1,class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C239::Method4.6980()#G2_C239::Method5.6981()#G2_C239::Method6.MI.6983<System.Object>()#"
		call void Generated703.Generated703::M.IBase1.B<class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C239::Method4.6980()#G2_C239::Method5.6981()#G2_C239::Method6.MI.6983<System.Object>()#"
		call void Generated703.Generated703::M.IBase1.T<class Generated703.BaseClass0,class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C239::Method4.6980()#G2_C239::Method5.6981()#G2_C239::Method6.MI.6983<System.Object>()#"
		call void Generated703.Generated703::M.IBase1.A<class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C239::ClassMethod1321.MI.6985()#G1_C4::ClassMethod1322.4799()#G1_C4::ClassMethod1323.4800<System.Object>()#G2_C239::Method4.6980()#G2_C239::Method5.6981()#G2_C239::Method6.6982<System.Object>()#G2_C239::Method7.6984<System.Object>()#"
		call void Generated703.Generated703::M.G2_C239.T.T<class Generated703.BaseClass1,class Generated703.BaseClass1,class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C239::ClassMethod1321.MI.6985()#G1_C4::ClassMethod1322.4799()#G1_C4::ClassMethod1323.4800<System.Object>()#G2_C239::Method4.6980()#G2_C239::Method5.6981()#G2_C239::Method6.6982<System.Object>()#G2_C239::Method7.6984<System.Object>()#"
		call void Generated703.Generated703::M.G2_C239.B.T<class Generated703.BaseClass1,class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C239::ClassMethod1321.MI.6985()#G1_C4::ClassMethod1322.4799()#G1_C4::ClassMethod1323.4800<System.Object>()#G2_C239::Method4.6980()#G2_C239::Method5.6981()#G2_C239::Method6.6982<System.Object>()#G2_C239::Method7.6984<System.Object>()#"
		call void Generated703.Generated703::M.G2_C239.B.B<class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>>(!!0,string)
		newobj instance void class Generated703.G1_C4`1<class Generated703.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C4::ClassMethod1321.4798()#G1_C4::ClassMethod1322.4799()#G1_C4::ClassMethod1323.4800<System.Object>()#G1_C4::Method4.4792()#G1_C4::Method5.4794()#G1_C4::Method6.4796<System.Object>()#G1_C4::Method7.4790<System.Object>()#"
		call void Generated703.Generated703::M.G1_C4.T<class Generated703.BaseClass0,class Generated703.G1_C4`1<class Generated703.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C4::ClassMethod1321.4798()#G1_C4::ClassMethod1322.4799()#G1_C4::ClassMethod1323.4800<System.Object>()#G1_C4::Method4.4792()#G1_C4::Method5.4794()#G1_C4::Method6.4796<System.Object>()#G1_C4::Method7.4790<System.Object>()#"
		call void Generated703.Generated703::M.G1_C4.A<class Generated703.G1_C4`1<class Generated703.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C4::Method7.MI.4791<System.Object>()#"
		call void Generated703.Generated703::M.IBase2.T.T<class Generated703.BaseClass0,class Generated703.BaseClass1,class Generated703.G1_C4`1<class Generated703.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C4::Method7.MI.4791<System.Object>()#"
		call void Generated703.Generated703::M.IBase2.A.T<class Generated703.BaseClass1,class Generated703.G1_C4`1<class Generated703.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C4::Method7.MI.4791<System.Object>()#"
		call void Generated703.Generated703::M.IBase2.A.B<class Generated703.G1_C4`1<class Generated703.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C4::Method4.MI.4793()#G1_C4::Method5.MI.4795()#G1_C4::Method6.MI.4797<System.Object>()#"
		call void Generated703.Generated703::M.IBase1.T<class Generated703.BaseClass1,class Generated703.G1_C4`1<class Generated703.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C4::Method4.MI.4793()#G1_C4::Method5.MI.4795()#G1_C4::Method6.MI.4797<System.Object>()#"
		call void Generated703.Generated703::M.IBase1.B<class Generated703.G1_C4`1<class Generated703.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C4::Method4.MI.4793()#G1_C4::Method5.MI.4795()#G1_C4::Method6.MI.4797<System.Object>()#"
		call void Generated703.Generated703::M.IBase1.T<class Generated703.BaseClass0,class Generated703.G1_C4`1<class Generated703.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C4::Method4.MI.4793()#G1_C4::Method5.MI.4795()#G1_C4::Method6.MI.4797<System.Object>()#"
		call void Generated703.Generated703::M.IBase1.A<class Generated703.G1_C4`1<class Generated703.BaseClass0>>(!!0,string)
		newobj instance void class Generated703.G1_C4`1<class Generated703.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C4::ClassMethod1321.4798()#G1_C4::ClassMethod1322.4799()#G1_C4::ClassMethod1323.4800<System.Object>()#G1_C4::Method4.4792()#G1_C4::Method5.4794()#G1_C4::Method6.4796<System.Object>()#G1_C4::Method7.4790<System.Object>()#"
		call void Generated703.Generated703::M.G1_C4.T<class Generated703.BaseClass1,class Generated703.G1_C4`1<class Generated703.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C4::ClassMethod1321.4798()#G1_C4::ClassMethod1322.4799()#G1_C4::ClassMethod1323.4800<System.Object>()#G1_C4::Method4.4792()#G1_C4::Method5.4794()#G1_C4::Method6.4796<System.Object>()#G1_C4::Method7.4790<System.Object>()#"
		call void Generated703.Generated703::M.G1_C4.B<class Generated703.G1_C4`1<class Generated703.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C4::Method7.MI.4791<System.Object>()#"
		call void Generated703.Generated703::M.IBase2.T.T<class Generated703.BaseClass0,class Generated703.BaseClass1,class Generated703.G1_C4`1<class Generated703.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C4::Method7.MI.4791<System.Object>()#"
		call void Generated703.Generated703::M.IBase2.A.T<class Generated703.BaseClass1,class Generated703.G1_C4`1<class Generated703.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C4::Method7.MI.4791<System.Object>()#"
		call void Generated703.Generated703::M.IBase2.A.B<class Generated703.G1_C4`1<class Generated703.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C4::Method4.MI.4793()#G1_C4::Method5.MI.4795()#G1_C4::Method6.MI.4797<System.Object>()#"
		call void Generated703.Generated703::M.IBase1.T<class Generated703.BaseClass1,class Generated703.G1_C4`1<class Generated703.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C4::Method4.MI.4793()#G1_C4::Method5.MI.4795()#G1_C4::Method6.MI.4797<System.Object>()#"
		call void Generated703.Generated703::M.IBase1.B<class Generated703.G1_C4`1<class Generated703.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C4::Method4.MI.4793()#G1_C4::Method5.MI.4795()#G1_C4::Method6.MI.4797<System.Object>()#"
		call void Generated703.Generated703::M.IBase1.T<class Generated703.BaseClass0,class Generated703.G1_C4`1<class Generated703.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C4::Method4.MI.4793()#G1_C4::Method5.MI.4795()#G1_C4::Method6.MI.4797<System.Object>()#"
		call void Generated703.Generated703::M.IBase1.A<class Generated703.G1_C4`1<class Generated703.BaseClass1>>(!!0,string)
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
		newobj instance void class Generated703.G3_C1175`1<class Generated703.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::ClassMethod1323<object>()
		calli default string(class Generated703.G3_C1175`1<class Generated703.BaseClass0>)
		ldstr "G1_C4::ClassMethod1323.4800<System.Object>()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::ClassMethod1322()
		calli default string(class Generated703.G3_C1175`1<class Generated703.BaseClass0>)
		ldstr "G1_C4::ClassMethod1322.4799()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::ClassMethod1321()
		calli default string(class Generated703.G3_C1175`1<class Generated703.BaseClass0>)
		ldstr "G2_C239::ClassMethod1321.MI.6985()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::Method6<object>()
		calli default string(class Generated703.G3_C1175`1<class Generated703.BaseClass0>)
		ldstr "G2_C239::Method6.6982<System.Object>()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::Method5()
		calli default string(class Generated703.G3_C1175`1<class Generated703.BaseClass0>)
		ldstr "G2_C239::Method5.6981()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::Method4()
		calli default string(class Generated703.G3_C1175`1<class Generated703.BaseClass0>)
		ldstr "G2_C239::Method4.6980()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::Method7<object>()
		calli default string(class Generated703.G3_C1175`1<class Generated703.BaseClass0>)
		ldstr "G1_C4::Method7.4790<System.Object>()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated703.IBase2`2<class Generated703.BaseClass0,class Generated703.BaseClass1>::Method7<object>()
		calli default string(class Generated703.G3_C1175`1<class Generated703.BaseClass0>)
		ldstr "G2_C239::Method7.6984<System.Object>()"
		ldstr "class IBase2`2<class Generated703.BaseClass0,class Generated703.BaseClass1> on type class Generated703.G3_C1175`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated703.IBase1`1<class Generated703.BaseClass1>::Method4()
		calli default string(class Generated703.G3_C1175`1<class Generated703.BaseClass0>)
		ldstr "G2_C239::Method4.6980()"
		ldstr "class IBase1`1<class Generated703.BaseClass1> on type class Generated703.G3_C1175`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated703.IBase1`1<class Generated703.BaseClass1>::Method5()
		calli default string(class Generated703.G3_C1175`1<class Generated703.BaseClass0>)
		ldstr "G2_C239::Method5.6981()"
		ldstr "class IBase1`1<class Generated703.BaseClass1> on type class Generated703.G3_C1175`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated703.IBase1`1<class Generated703.BaseClass1>::Method6<object>()
		calli default string(class Generated703.G3_C1175`1<class Generated703.BaseClass0>)
		ldstr "G2_C239::Method6.MI.6983<System.Object>()"
		ldstr "class IBase1`1<class Generated703.BaseClass1> on type class Generated703.G3_C1175`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated703.IBase1`1<class Generated703.BaseClass0>::Method4()
		calli default string(class Generated703.G3_C1175`1<class Generated703.BaseClass0>)
		ldstr "G2_C239::Method4.6980()"
		ldstr "class IBase1`1<class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated703.IBase1`1<class Generated703.BaseClass0>::Method5()
		calli default string(class Generated703.G3_C1175`1<class Generated703.BaseClass0>)
		ldstr "G2_C239::Method5.6981()"
		ldstr "class IBase1`1<class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated703.IBase1`1<class Generated703.BaseClass0>::Method6<object>()
		calli default string(class Generated703.G3_C1175`1<class Generated703.BaseClass0>)
		ldstr "G2_C239::Method6.MI.6983<System.Object>()"
		ldstr "class IBase1`1<class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>::Method7<object>()
		calli default string(class Generated703.G3_C1175`1<class Generated703.BaseClass0>)
		ldstr "G2_C239::Method7.6984<System.Object>()"
		ldstr "class G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>::Method6<object>()
		calli default string(class Generated703.G3_C1175`1<class Generated703.BaseClass0>)
		ldstr "G2_C239::Method6.6982<System.Object>()"
		ldstr "class G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>::Method5()
		calli default string(class Generated703.G3_C1175`1<class Generated703.BaseClass0>)
		ldstr "G2_C239::Method5.6981()"
		ldstr "class G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>::Method4()
		calli default string(class Generated703.G3_C1175`1<class Generated703.BaseClass0>)
		ldstr "G2_C239::Method4.6980()"
		ldstr "class G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>::ClassMethod1323<object>()
		calli default string(class Generated703.G3_C1175`1<class Generated703.BaseClass0>)
		ldstr "G1_C4::ClassMethod1323.4800<System.Object>()"
		ldstr "class G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>::ClassMethod1322()
		calli default string(class Generated703.G3_C1175`1<class Generated703.BaseClass0>)
		ldstr "G1_C4::ClassMethod1322.4799()"
		ldstr "class G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>::ClassMethod1321()
		calli default string(class Generated703.G3_C1175`1<class Generated703.BaseClass0>)
		ldstr "G2_C239::ClassMethod1321.MI.6985()"
		ldstr "class G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated703.IBase2`2<class Generated703.BaseClass0,class Generated703.BaseClass0>::Method7<object>()
		calli default string(class Generated703.G3_C1175`1<class Generated703.BaseClass0>)
		ldstr "G2_C239::Method7.6984<System.Object>()"
		ldstr "class IBase2`2<class Generated703.BaseClass0,class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G3_C1175`1<class Generated703.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated703.G3_C1175`1<class Generated703.BaseClass0>::ClassMethod3748<object>()
		calli default string(class Generated703.G3_C1175`1<class Generated703.BaseClass0>)
		ldstr "G3_C1175::ClassMethod3748.14734<System.Object>()"
		ldstr "class G3_C1175`1<class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G3_C1175`1<class Generated703.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated703.G3_C1175`1<class Generated703.BaseClass0>::ClassMethod3747<object>()
		calli default string(class Generated703.G3_C1175`1<class Generated703.BaseClass0>)
		ldstr "G3_C1175::ClassMethod3747.14733<System.Object>()"
		ldstr "class G3_C1175`1<class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G3_C1175`1<class Generated703.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated703.G3_C1175`1<class Generated703.BaseClass0>::ClassMethod3746()
		calli default string(class Generated703.G3_C1175`1<class Generated703.BaseClass0>)
		ldstr "G3_C1175::ClassMethod3746.14732()"
		ldstr "class G3_C1175`1<class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G3_C1175`1<class Generated703.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated703.G3_C1175`1<class Generated703.BaseClass0>::ClassMethod3745()
		calli default string(class Generated703.G3_C1175`1<class Generated703.BaseClass0>)
		ldstr "G3_C1175::ClassMethod3745.14731()"
		ldstr "class G3_C1175`1<class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G3_C1175`1<class Generated703.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated703.G3_C1175`1<class Generated703.BaseClass0>::Method3<object>()
		calli default string(class Generated703.G3_C1175`1<class Generated703.BaseClass0>)
		ldstr "G3_C1175::Method3.14730<System.Object>()"
		ldstr "class G3_C1175`1<class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G3_C1175`1<class Generated703.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated703.G3_C1175`1<class Generated703.BaseClass0>::Method2<object>()
		calli default string(class Generated703.G3_C1175`1<class Generated703.BaseClass0>)
		ldstr "G3_C1175::Method2.14729<System.Object>()"
		ldstr "class G3_C1175`1<class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G3_C1175`1<class Generated703.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated703.G3_C1175`1<class Generated703.BaseClass0>::Method1()
		calli default string(class Generated703.G3_C1175`1<class Generated703.BaseClass0>)
		ldstr "G3_C1175::Method1.14728()"
		ldstr "class G3_C1175`1<class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G3_C1175`1<class Generated703.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated703.G3_C1175`1<class Generated703.BaseClass0>::Method0()
		calli default string(class Generated703.G3_C1175`1<class Generated703.BaseClass0>)
		ldstr "G3_C1175::Method0.14727()"
		ldstr "class G3_C1175`1<class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G3_C1175`1<class Generated703.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated703.G3_C1175`1<class Generated703.BaseClass0>::Method7<object>()
		calli default string(class Generated703.G3_C1175`1<class Generated703.BaseClass0>)
		ldstr "G2_C239::Method7.6984<System.Object>()"
		ldstr "class G3_C1175`1<class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G3_C1175`1<class Generated703.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated703.G3_C1175`1<class Generated703.BaseClass0>::Method6<object>()
		calli default string(class Generated703.G3_C1175`1<class Generated703.BaseClass0>)
		ldstr "G2_C239::Method6.6982<System.Object>()"
		ldstr "class G3_C1175`1<class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G3_C1175`1<class Generated703.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated703.G3_C1175`1<class Generated703.BaseClass0>::Method5()
		calli default string(class Generated703.G3_C1175`1<class Generated703.BaseClass0>)
		ldstr "G2_C239::Method5.6981()"
		ldstr "class G3_C1175`1<class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G3_C1175`1<class Generated703.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated703.G3_C1175`1<class Generated703.BaseClass0>::Method4()
		calli default string(class Generated703.G3_C1175`1<class Generated703.BaseClass0>)
		ldstr "G2_C239::Method4.6980()"
		ldstr "class G3_C1175`1<class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G3_C1175`1<class Generated703.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated703.G3_C1175`1<class Generated703.BaseClass0>::ClassMethod1323<object>()
		calli default string(class Generated703.G3_C1175`1<class Generated703.BaseClass0>)
		ldstr "G1_C4::ClassMethod1323.4800<System.Object>()"
		ldstr "class G3_C1175`1<class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G3_C1175`1<class Generated703.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated703.G3_C1175`1<class Generated703.BaseClass0>::ClassMethod1322()
		calli default string(class Generated703.G3_C1175`1<class Generated703.BaseClass0>)
		ldstr "G1_C4::ClassMethod1322.4799()"
		ldstr "class G3_C1175`1<class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G3_C1175`1<class Generated703.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated703.G3_C1175`1<class Generated703.BaseClass0>::ClassMethod1321()
		calli default string(class Generated703.G3_C1175`1<class Generated703.BaseClass0>)
		ldstr "G2_C239::ClassMethod1321.MI.6985()"
		ldstr "class G3_C1175`1<class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated703.IBase0::Method0()
		calli default string(class Generated703.G3_C1175`1<class Generated703.BaseClass0>)
		ldstr "G3_C1175::Method0.14727()"
		ldstr "IBase0 on type class Generated703.G3_C1175`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated703.IBase0::Method1()
		calli default string(class Generated703.G3_C1175`1<class Generated703.BaseClass0>)
		ldstr "G3_C1175::Method1.14728()"
		ldstr "IBase0 on type class Generated703.G3_C1175`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated703.IBase0::Method2<object>()
		calli default string(class Generated703.G3_C1175`1<class Generated703.BaseClass0>)
		ldstr "G3_C1175::Method2.14729<System.Object>()"
		ldstr "IBase0 on type class Generated703.G3_C1175`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated703.IBase0::Method3<object>()
		calli default string(class Generated703.G3_C1175`1<class Generated703.BaseClass0>)
		ldstr "G3_C1175::Method3.14730<System.Object>()"
		ldstr "IBase0 on type class Generated703.G3_C1175`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated703.G3_C1175`1<class Generated703.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::ClassMethod1323<object>()
		calli default string(class Generated703.G3_C1175`1<class Generated703.BaseClass1>)
		ldstr "G1_C4::ClassMethod1323.4800<System.Object>()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::ClassMethod1322()
		calli default string(class Generated703.G3_C1175`1<class Generated703.BaseClass1>)
		ldstr "G1_C4::ClassMethod1322.4799()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::ClassMethod1321()
		calli default string(class Generated703.G3_C1175`1<class Generated703.BaseClass1>)
		ldstr "G2_C239::ClassMethod1321.MI.6985()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::Method6<object>()
		calli default string(class Generated703.G3_C1175`1<class Generated703.BaseClass1>)
		ldstr "G2_C239::Method6.6982<System.Object>()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::Method5()
		calli default string(class Generated703.G3_C1175`1<class Generated703.BaseClass1>)
		ldstr "G2_C239::Method5.6981()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::Method4()
		calli default string(class Generated703.G3_C1175`1<class Generated703.BaseClass1>)
		ldstr "G2_C239::Method4.6980()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::Method7<object>()
		calli default string(class Generated703.G3_C1175`1<class Generated703.BaseClass1>)
		ldstr "G1_C4::Method7.4790<System.Object>()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated703.IBase2`2<class Generated703.BaseClass0,class Generated703.BaseClass1>::Method7<object>()
		calli default string(class Generated703.G3_C1175`1<class Generated703.BaseClass1>)
		ldstr "G2_C239::Method7.6984<System.Object>()"
		ldstr "class IBase2`2<class Generated703.BaseClass0,class Generated703.BaseClass1> on type class Generated703.G3_C1175`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated703.IBase1`1<class Generated703.BaseClass1>::Method4()
		calli default string(class Generated703.G3_C1175`1<class Generated703.BaseClass1>)
		ldstr "G2_C239::Method4.6980()"
		ldstr "class IBase1`1<class Generated703.BaseClass1> on type class Generated703.G3_C1175`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated703.IBase1`1<class Generated703.BaseClass1>::Method5()
		calli default string(class Generated703.G3_C1175`1<class Generated703.BaseClass1>)
		ldstr "G2_C239::Method5.6981()"
		ldstr "class IBase1`1<class Generated703.BaseClass1> on type class Generated703.G3_C1175`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated703.IBase1`1<class Generated703.BaseClass1>::Method6<object>()
		calli default string(class Generated703.G3_C1175`1<class Generated703.BaseClass1>)
		ldstr "G2_C239::Method6.MI.6983<System.Object>()"
		ldstr "class IBase1`1<class Generated703.BaseClass1> on type class Generated703.G3_C1175`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated703.IBase1`1<class Generated703.BaseClass0>::Method4()
		calli default string(class Generated703.G3_C1175`1<class Generated703.BaseClass1>)
		ldstr "G2_C239::Method4.6980()"
		ldstr "class IBase1`1<class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated703.IBase1`1<class Generated703.BaseClass0>::Method5()
		calli default string(class Generated703.G3_C1175`1<class Generated703.BaseClass1>)
		ldstr "G2_C239::Method5.6981()"
		ldstr "class IBase1`1<class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated703.IBase1`1<class Generated703.BaseClass0>::Method6<object>()
		calli default string(class Generated703.G3_C1175`1<class Generated703.BaseClass1>)
		ldstr "G2_C239::Method6.MI.6983<System.Object>()"
		ldstr "class IBase1`1<class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>::Method7<object>()
		calli default string(class Generated703.G3_C1175`1<class Generated703.BaseClass1>)
		ldstr "G2_C239::Method7.6984<System.Object>()"
		ldstr "class G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>::Method6<object>()
		calli default string(class Generated703.G3_C1175`1<class Generated703.BaseClass1>)
		ldstr "G2_C239::Method6.6982<System.Object>()"
		ldstr "class G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>::Method5()
		calli default string(class Generated703.G3_C1175`1<class Generated703.BaseClass1>)
		ldstr "G2_C239::Method5.6981()"
		ldstr "class G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>::Method4()
		calli default string(class Generated703.G3_C1175`1<class Generated703.BaseClass1>)
		ldstr "G2_C239::Method4.6980()"
		ldstr "class G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>::ClassMethod1323<object>()
		calli default string(class Generated703.G3_C1175`1<class Generated703.BaseClass1>)
		ldstr "G1_C4::ClassMethod1323.4800<System.Object>()"
		ldstr "class G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>::ClassMethod1322()
		calli default string(class Generated703.G3_C1175`1<class Generated703.BaseClass1>)
		ldstr "G1_C4::ClassMethod1322.4799()"
		ldstr "class G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>::ClassMethod1321()
		calli default string(class Generated703.G3_C1175`1<class Generated703.BaseClass1>)
		ldstr "G2_C239::ClassMethod1321.MI.6985()"
		ldstr "class G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated703.IBase2`2<class Generated703.BaseClass0,class Generated703.BaseClass0>::Method7<object>()
		calli default string(class Generated703.G3_C1175`1<class Generated703.BaseClass1>)
		ldstr "G2_C239::Method7.6984<System.Object>()"
		ldstr "class IBase2`2<class Generated703.BaseClass0,class Generated703.BaseClass0> on type class Generated703.G3_C1175`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G3_C1175`1<class Generated703.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated703.G3_C1175`1<class Generated703.BaseClass1>::ClassMethod3748<object>()
		calli default string(class Generated703.G3_C1175`1<class Generated703.BaseClass1>)
		ldstr "G3_C1175::ClassMethod3748.14734<System.Object>()"
		ldstr "class G3_C1175`1<class Generated703.BaseClass1> on type class Generated703.G3_C1175`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G3_C1175`1<class Generated703.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated703.G3_C1175`1<class Generated703.BaseClass1>::ClassMethod3747<object>()
		calli default string(class Generated703.G3_C1175`1<class Generated703.BaseClass1>)
		ldstr "G3_C1175::ClassMethod3747.14733<System.Object>()"
		ldstr "class G3_C1175`1<class Generated703.BaseClass1> on type class Generated703.G3_C1175`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G3_C1175`1<class Generated703.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated703.G3_C1175`1<class Generated703.BaseClass1>::ClassMethod3746()
		calli default string(class Generated703.G3_C1175`1<class Generated703.BaseClass1>)
		ldstr "G3_C1175::ClassMethod3746.14732()"
		ldstr "class G3_C1175`1<class Generated703.BaseClass1> on type class Generated703.G3_C1175`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G3_C1175`1<class Generated703.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated703.G3_C1175`1<class Generated703.BaseClass1>::ClassMethod3745()
		calli default string(class Generated703.G3_C1175`1<class Generated703.BaseClass1>)
		ldstr "G3_C1175::ClassMethod3745.14731()"
		ldstr "class G3_C1175`1<class Generated703.BaseClass1> on type class Generated703.G3_C1175`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G3_C1175`1<class Generated703.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated703.G3_C1175`1<class Generated703.BaseClass1>::Method3<object>()
		calli default string(class Generated703.G3_C1175`1<class Generated703.BaseClass1>)
		ldstr "G3_C1175::Method3.14730<System.Object>()"
		ldstr "class G3_C1175`1<class Generated703.BaseClass1> on type class Generated703.G3_C1175`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G3_C1175`1<class Generated703.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated703.G3_C1175`1<class Generated703.BaseClass1>::Method2<object>()
		calli default string(class Generated703.G3_C1175`1<class Generated703.BaseClass1>)
		ldstr "G3_C1175::Method2.14729<System.Object>()"
		ldstr "class G3_C1175`1<class Generated703.BaseClass1> on type class Generated703.G3_C1175`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G3_C1175`1<class Generated703.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated703.G3_C1175`1<class Generated703.BaseClass1>::Method1()
		calli default string(class Generated703.G3_C1175`1<class Generated703.BaseClass1>)
		ldstr "G3_C1175::Method1.14728()"
		ldstr "class G3_C1175`1<class Generated703.BaseClass1> on type class Generated703.G3_C1175`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G3_C1175`1<class Generated703.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated703.G3_C1175`1<class Generated703.BaseClass1>::Method0()
		calli default string(class Generated703.G3_C1175`1<class Generated703.BaseClass1>)
		ldstr "G3_C1175::Method0.14727()"
		ldstr "class G3_C1175`1<class Generated703.BaseClass1> on type class Generated703.G3_C1175`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G3_C1175`1<class Generated703.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated703.G3_C1175`1<class Generated703.BaseClass1>::Method7<object>()
		calli default string(class Generated703.G3_C1175`1<class Generated703.BaseClass1>)
		ldstr "G2_C239::Method7.6984<System.Object>()"
		ldstr "class G3_C1175`1<class Generated703.BaseClass1> on type class Generated703.G3_C1175`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G3_C1175`1<class Generated703.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated703.G3_C1175`1<class Generated703.BaseClass1>::Method6<object>()
		calli default string(class Generated703.G3_C1175`1<class Generated703.BaseClass1>)
		ldstr "G2_C239::Method6.6982<System.Object>()"
		ldstr "class G3_C1175`1<class Generated703.BaseClass1> on type class Generated703.G3_C1175`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G3_C1175`1<class Generated703.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated703.G3_C1175`1<class Generated703.BaseClass1>::Method5()
		calli default string(class Generated703.G3_C1175`1<class Generated703.BaseClass1>)
		ldstr "G2_C239::Method5.6981()"
		ldstr "class G3_C1175`1<class Generated703.BaseClass1> on type class Generated703.G3_C1175`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G3_C1175`1<class Generated703.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated703.G3_C1175`1<class Generated703.BaseClass1>::Method4()
		calli default string(class Generated703.G3_C1175`1<class Generated703.BaseClass1>)
		ldstr "G2_C239::Method4.6980()"
		ldstr "class G3_C1175`1<class Generated703.BaseClass1> on type class Generated703.G3_C1175`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G3_C1175`1<class Generated703.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated703.G3_C1175`1<class Generated703.BaseClass1>::ClassMethod1323<object>()
		calli default string(class Generated703.G3_C1175`1<class Generated703.BaseClass1>)
		ldstr "G1_C4::ClassMethod1323.4800<System.Object>()"
		ldstr "class G3_C1175`1<class Generated703.BaseClass1> on type class Generated703.G3_C1175`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G3_C1175`1<class Generated703.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated703.G3_C1175`1<class Generated703.BaseClass1>::ClassMethod1322()
		calli default string(class Generated703.G3_C1175`1<class Generated703.BaseClass1>)
		ldstr "G1_C4::ClassMethod1322.4799()"
		ldstr "class G3_C1175`1<class Generated703.BaseClass1> on type class Generated703.G3_C1175`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G3_C1175`1<class Generated703.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated703.G3_C1175`1<class Generated703.BaseClass1>::ClassMethod1321()
		calli default string(class Generated703.G3_C1175`1<class Generated703.BaseClass1>)
		ldstr "G2_C239::ClassMethod1321.MI.6985()"
		ldstr "class G3_C1175`1<class Generated703.BaseClass1> on type class Generated703.G3_C1175`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated703.IBase0::Method0()
		calli default string(class Generated703.G3_C1175`1<class Generated703.BaseClass1>)
		ldstr "G3_C1175::Method0.14727()"
		ldstr "IBase0 on type class Generated703.G3_C1175`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated703.IBase0::Method1()
		calli default string(class Generated703.G3_C1175`1<class Generated703.BaseClass1>)
		ldstr "G3_C1175::Method1.14728()"
		ldstr "IBase0 on type class Generated703.G3_C1175`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated703.IBase0::Method2<object>()
		calli default string(class Generated703.G3_C1175`1<class Generated703.BaseClass1>)
		ldstr "G3_C1175::Method2.14729<System.Object>()"
		ldstr "IBase0 on type class Generated703.G3_C1175`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated703.IBase0::Method3<object>()
		calli default string(class Generated703.G3_C1175`1<class Generated703.BaseClass1>)
		ldstr "G3_C1175::Method3.14730<System.Object>()"
		ldstr "IBase0 on type class Generated703.G3_C1175`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::ClassMethod1323<object>()
		calli default string(class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>)
		ldstr "G1_C4::ClassMethod1323.4800<System.Object>()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::ClassMethod1322()
		calli default string(class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>)
		ldstr "G1_C4::ClassMethod1322.4799()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::ClassMethod1321()
		calli default string(class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>)
		ldstr "G2_C239::ClassMethod1321.MI.6985()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::Method6<object>()
		calli default string(class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>)
		ldstr "G2_C239::Method6.6982<System.Object>()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::Method5()
		calli default string(class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>)
		ldstr "G2_C239::Method5.6981()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::Method4()
		calli default string(class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>)
		ldstr "G2_C239::Method4.6980()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::Method7<object>()
		calli default string(class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>)
		ldstr "G1_C4::Method7.4790<System.Object>()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated703.IBase2`2<class Generated703.BaseClass0,class Generated703.BaseClass1>::Method7<object>()
		calli default string(class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>)
		ldstr "G2_C239::Method7.6984<System.Object>()"
		ldstr "class IBase2`2<class Generated703.BaseClass0,class Generated703.BaseClass1> on type class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated703.IBase1`1<class Generated703.BaseClass1>::Method4()
		calli default string(class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>)
		ldstr "G2_C239::Method4.6980()"
		ldstr "class IBase1`1<class Generated703.BaseClass1> on type class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated703.IBase1`1<class Generated703.BaseClass1>::Method5()
		calli default string(class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>)
		ldstr "G2_C239::Method5.6981()"
		ldstr "class IBase1`1<class Generated703.BaseClass1> on type class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated703.IBase1`1<class Generated703.BaseClass1>::Method6<object>()
		calli default string(class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>)
		ldstr "G2_C239::Method6.MI.6983<System.Object>()"
		ldstr "class IBase1`1<class Generated703.BaseClass1> on type class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated703.IBase1`1<class Generated703.BaseClass0>::Method4()
		calli default string(class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>)
		ldstr "G2_C239::Method4.6980()"
		ldstr "class IBase1`1<class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated703.IBase1`1<class Generated703.BaseClass0>::Method5()
		calli default string(class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>)
		ldstr "G2_C239::Method5.6981()"
		ldstr "class IBase1`1<class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated703.IBase1`1<class Generated703.BaseClass0>::Method6<object>()
		calli default string(class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>)
		ldstr "G2_C239::Method6.MI.6983<System.Object>()"
		ldstr "class IBase1`1<class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>::Method7<object>()
		calli default string(class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>)
		ldstr "G2_C239::Method7.6984<System.Object>()"
		ldstr "class G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>::Method6<object>()
		calli default string(class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>)
		ldstr "G2_C239::Method6.6982<System.Object>()"
		ldstr "class G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>::Method5()
		calli default string(class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>)
		ldstr "G2_C239::Method5.6981()"
		ldstr "class G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>::Method4()
		calli default string(class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>)
		ldstr "G2_C239::Method4.6980()"
		ldstr "class G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>::ClassMethod1323<object>()
		calli default string(class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>)
		ldstr "G1_C4::ClassMethod1323.4800<System.Object>()"
		ldstr "class G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>::ClassMethod1322()
		calli default string(class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>)
		ldstr "G1_C4::ClassMethod1322.4799()"
		ldstr "class G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>::ClassMethod1321()
		calli default string(class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>)
		ldstr "G2_C239::ClassMethod1321.MI.6985()"
		ldstr "class G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated703.IBase2`2<class Generated703.BaseClass0,class Generated703.BaseClass0>::Method7<object>()
		calli default string(class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>)
		ldstr "G2_C239::Method7.6984<System.Object>()"
		ldstr "class IBase2`2<class Generated703.BaseClass0,class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::ClassMethod1323<object>()
		calli default string(class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>)
		ldstr "G1_C4::ClassMethod1323.4800<System.Object>()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::ClassMethod1322()
		calli default string(class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>)
		ldstr "G1_C4::ClassMethod1322.4799()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::ClassMethod1321()
		calli default string(class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>)
		ldstr "G2_C239::ClassMethod1321.MI.6985()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::Method6<object>()
		calli default string(class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>)
		ldstr "G2_C239::Method6.6982<System.Object>()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::Method5()
		calli default string(class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>)
		ldstr "G2_C239::Method5.6981()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::Method4()
		calli default string(class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>)
		ldstr "G2_C239::Method4.6980()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::Method7<object>()
		calli default string(class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>)
		ldstr "G1_C4::Method7.4790<System.Object>()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated703.IBase2`2<class Generated703.BaseClass0,class Generated703.BaseClass1>::Method7<object>()
		calli default string(class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>)
		ldstr "G2_C239::Method7.6984<System.Object>()"
		ldstr "class IBase2`2<class Generated703.BaseClass0,class Generated703.BaseClass1> on type class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated703.IBase1`1<class Generated703.BaseClass1>::Method4()
		calli default string(class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>)
		ldstr "G2_C239::Method4.6980()"
		ldstr "class IBase1`1<class Generated703.BaseClass1> on type class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated703.IBase1`1<class Generated703.BaseClass1>::Method5()
		calli default string(class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>)
		ldstr "G2_C239::Method5.6981()"
		ldstr "class IBase1`1<class Generated703.BaseClass1> on type class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated703.IBase1`1<class Generated703.BaseClass1>::Method6<object>()
		calli default string(class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>)
		ldstr "G2_C239::Method6.MI.6983<System.Object>()"
		ldstr "class IBase1`1<class Generated703.BaseClass1> on type class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated703.IBase1`1<class Generated703.BaseClass0>::Method4()
		calli default string(class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>)
		ldstr "G2_C239::Method4.6980()"
		ldstr "class IBase1`1<class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated703.IBase1`1<class Generated703.BaseClass0>::Method5()
		calli default string(class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>)
		ldstr "G2_C239::Method5.6981()"
		ldstr "class IBase1`1<class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated703.IBase1`1<class Generated703.BaseClass0>::Method6<object>()
		calli default string(class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>)
		ldstr "G2_C239::Method6.MI.6983<System.Object>()"
		ldstr "class IBase1`1<class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>::Method7<object>()
		calli default string(class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>)
		ldstr "G2_C239::Method7.6984<System.Object>()"
		ldstr "class G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1> on type class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>::Method6<object>()
		calli default string(class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>)
		ldstr "G2_C239::Method6.6982<System.Object>()"
		ldstr "class G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1> on type class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>::Method5()
		calli default string(class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>)
		ldstr "G2_C239::Method5.6981()"
		ldstr "class G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1> on type class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>::Method4()
		calli default string(class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>)
		ldstr "G2_C239::Method4.6980()"
		ldstr "class G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1> on type class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>::ClassMethod1323<object>()
		calli default string(class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>)
		ldstr "G1_C4::ClassMethod1323.4800<System.Object>()"
		ldstr "class G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1> on type class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>::ClassMethod1322()
		calli default string(class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>)
		ldstr "G1_C4::ClassMethod1322.4799()"
		ldstr "class G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1> on type class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>::ClassMethod1321()
		calli default string(class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>)
		ldstr "G2_C239::ClassMethod1321.MI.6985()"
		ldstr "class G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1> on type class Generated703.G2_C239`2<class Generated703.BaseClass0,class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::ClassMethod1323<object>()
		calli default string(class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>)
		ldstr "G1_C4::ClassMethod1323.4800<System.Object>()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::ClassMethod1322()
		calli default string(class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>)
		ldstr "G1_C4::ClassMethod1322.4799()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::ClassMethod1321()
		calli default string(class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>)
		ldstr "G2_C239::ClassMethod1321.MI.6985()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::Method6<object>()
		calli default string(class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>)
		ldstr "G2_C239::Method6.6982<System.Object>()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::Method5()
		calli default string(class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>)
		ldstr "G2_C239::Method5.6981()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::Method4()
		calli default string(class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>)
		ldstr "G2_C239::Method4.6980()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::Method7<object>()
		calli default string(class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>)
		ldstr "G1_C4::Method7.4790<System.Object>()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated703.IBase2`2<class Generated703.BaseClass0,class Generated703.BaseClass1>::Method7<object>()
		calli default string(class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>)
		ldstr "G2_C239::Method7.6984<System.Object>()"
		ldstr "class IBase2`2<class Generated703.BaseClass0,class Generated703.BaseClass1> on type class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated703.IBase1`1<class Generated703.BaseClass1>::Method4()
		calli default string(class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>)
		ldstr "G2_C239::Method4.6980()"
		ldstr "class IBase1`1<class Generated703.BaseClass1> on type class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated703.IBase1`1<class Generated703.BaseClass1>::Method5()
		calli default string(class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>)
		ldstr "G2_C239::Method5.6981()"
		ldstr "class IBase1`1<class Generated703.BaseClass1> on type class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated703.IBase1`1<class Generated703.BaseClass1>::Method6<object>()
		calli default string(class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>)
		ldstr "G2_C239::Method6.MI.6983<System.Object>()"
		ldstr "class IBase1`1<class Generated703.BaseClass1> on type class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated703.IBase1`1<class Generated703.BaseClass0>::Method4()
		calli default string(class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>)
		ldstr "G2_C239::Method4.6980()"
		ldstr "class IBase1`1<class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated703.IBase1`1<class Generated703.BaseClass0>::Method5()
		calli default string(class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>)
		ldstr "G2_C239::Method5.6981()"
		ldstr "class IBase1`1<class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated703.IBase1`1<class Generated703.BaseClass0>::Method6<object>()
		calli default string(class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>)
		ldstr "G2_C239::Method6.MI.6983<System.Object>()"
		ldstr "class IBase1`1<class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>::Method7<object>()
		calli default string(class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>)
		ldstr "G2_C239::Method7.6984<System.Object>()"
		ldstr "class G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>::Method6<object>()
		calli default string(class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>)
		ldstr "G2_C239::Method6.6982<System.Object>()"
		ldstr "class G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>::Method5()
		calli default string(class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>)
		ldstr "G2_C239::Method5.6981()"
		ldstr "class G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>::Method4()
		calli default string(class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>)
		ldstr "G2_C239::Method4.6980()"
		ldstr "class G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>::ClassMethod1323<object>()
		calli default string(class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>)
		ldstr "G1_C4::ClassMethod1323.4800<System.Object>()"
		ldstr "class G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>::ClassMethod1322()
		calli default string(class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>)
		ldstr "G1_C4::ClassMethod1322.4799()"
		ldstr "class G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>::ClassMethod1321()
		calli default string(class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>)
		ldstr "G2_C239::ClassMethod1321.MI.6985()"
		ldstr "class G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated703.IBase2`2<class Generated703.BaseClass0,class Generated703.BaseClass0>::Method7<object>()
		calli default string(class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>)
		ldstr "G2_C239::Method7.6984<System.Object>()"
		ldstr "class IBase2`2<class Generated703.BaseClass0,class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::ClassMethod1323<object>()
		calli default string(class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>)
		ldstr "G1_C4::ClassMethod1323.4800<System.Object>()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::ClassMethod1322()
		calli default string(class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>)
		ldstr "G1_C4::ClassMethod1322.4799()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::ClassMethod1321()
		calli default string(class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>)
		ldstr "G2_C239::ClassMethod1321.MI.6985()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::Method6<object>()
		calli default string(class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>)
		ldstr "G2_C239::Method6.6982<System.Object>()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::Method5()
		calli default string(class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>)
		ldstr "G2_C239::Method5.6981()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::Method4()
		calli default string(class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>)
		ldstr "G2_C239::Method4.6980()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::Method7<object>()
		calli default string(class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>)
		ldstr "G1_C4::Method7.4790<System.Object>()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated703.IBase2`2<class Generated703.BaseClass0,class Generated703.BaseClass1>::Method7<object>()
		calli default string(class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>)
		ldstr "G2_C239::Method7.6984<System.Object>()"
		ldstr "class IBase2`2<class Generated703.BaseClass0,class Generated703.BaseClass1> on type class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated703.IBase1`1<class Generated703.BaseClass1>::Method4()
		calli default string(class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>)
		ldstr "G2_C239::Method4.6980()"
		ldstr "class IBase1`1<class Generated703.BaseClass1> on type class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated703.IBase1`1<class Generated703.BaseClass1>::Method5()
		calli default string(class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>)
		ldstr "G2_C239::Method5.6981()"
		ldstr "class IBase1`1<class Generated703.BaseClass1> on type class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated703.IBase1`1<class Generated703.BaseClass1>::Method6<object>()
		calli default string(class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>)
		ldstr "G2_C239::Method6.MI.6983<System.Object>()"
		ldstr "class IBase1`1<class Generated703.BaseClass1> on type class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated703.IBase1`1<class Generated703.BaseClass0>::Method4()
		calli default string(class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>)
		ldstr "G2_C239::Method4.6980()"
		ldstr "class IBase1`1<class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated703.IBase1`1<class Generated703.BaseClass0>::Method5()
		calli default string(class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>)
		ldstr "G2_C239::Method5.6981()"
		ldstr "class IBase1`1<class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated703.IBase1`1<class Generated703.BaseClass0>::Method6<object>()
		calli default string(class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>)
		ldstr "G2_C239::Method6.MI.6983<System.Object>()"
		ldstr "class IBase1`1<class Generated703.BaseClass0> on type class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>::Method7<object>()
		calli default string(class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>)
		ldstr "G2_C239::Method7.6984<System.Object>()"
		ldstr "class G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1> on type class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>::Method6<object>()
		calli default string(class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>)
		ldstr "G2_C239::Method6.6982<System.Object>()"
		ldstr "class G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1> on type class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>::Method5()
		calli default string(class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>)
		ldstr "G2_C239::Method5.6981()"
		ldstr "class G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1> on type class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>::Method4()
		calli default string(class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>)
		ldstr "G2_C239::Method4.6980()"
		ldstr "class G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1> on type class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>::ClassMethod1323<object>()
		calli default string(class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>)
		ldstr "G1_C4::ClassMethod1323.4800<System.Object>()"
		ldstr "class G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1> on type class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>::ClassMethod1322()
		calli default string(class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>)
		ldstr "G1_C4::ClassMethod1322.4799()"
		ldstr "class G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1> on type class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>::ClassMethod1321()
		calli default string(class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>)
		ldstr "G2_C239::ClassMethod1321.MI.6985()"
		ldstr "class G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1> on type class Generated703.G2_C239`2<class Generated703.BaseClass1,class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated703.G1_C4`1<class Generated703.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::ClassMethod1323<object>()
		calli default string(class Generated703.G1_C4`1<class Generated703.BaseClass0>)
		ldstr "G1_C4::ClassMethod1323.4800<System.Object>()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G1_C4`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::ClassMethod1322()
		calli default string(class Generated703.G1_C4`1<class Generated703.BaseClass0>)
		ldstr "G1_C4::ClassMethod1322.4799()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G1_C4`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::ClassMethod1321()
		calli default string(class Generated703.G1_C4`1<class Generated703.BaseClass0>)
		ldstr "G1_C4::ClassMethod1321.4798()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G1_C4`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::Method6<object>()
		calli default string(class Generated703.G1_C4`1<class Generated703.BaseClass0>)
		ldstr "G1_C4::Method6.4796<System.Object>()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G1_C4`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::Method5()
		calli default string(class Generated703.G1_C4`1<class Generated703.BaseClass0>)
		ldstr "G1_C4::Method5.4794()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G1_C4`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::Method4()
		calli default string(class Generated703.G1_C4`1<class Generated703.BaseClass0>)
		ldstr "G1_C4::Method4.4792()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G1_C4`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated703.G1_C4`1<class Generated703.BaseClass0>::Method7<object>()
		calli default string(class Generated703.G1_C4`1<class Generated703.BaseClass0>)
		ldstr "G1_C4::Method7.4790<System.Object>()"
		ldstr "class G1_C4`1<class Generated703.BaseClass0> on type class Generated703.G1_C4`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated703.IBase2`2<class Generated703.BaseClass0,class Generated703.BaseClass1>::Method7<object>()
		calli default string(class Generated703.G1_C4`1<class Generated703.BaseClass0>)
		ldstr "G1_C4::Method7.MI.4791<System.Object>()"
		ldstr "class IBase2`2<class Generated703.BaseClass0,class Generated703.BaseClass1> on type class Generated703.G1_C4`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated703.IBase1`1<class Generated703.BaseClass1>::Method4()
		calli default string(class Generated703.G1_C4`1<class Generated703.BaseClass0>)
		ldstr "G1_C4::Method4.MI.4793()"
		ldstr "class IBase1`1<class Generated703.BaseClass1> on type class Generated703.G1_C4`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated703.IBase1`1<class Generated703.BaseClass1>::Method5()
		calli default string(class Generated703.G1_C4`1<class Generated703.BaseClass0>)
		ldstr "G1_C4::Method5.MI.4795()"
		ldstr "class IBase1`1<class Generated703.BaseClass1> on type class Generated703.G1_C4`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated703.IBase1`1<class Generated703.BaseClass1>::Method6<object>()
		calli default string(class Generated703.G1_C4`1<class Generated703.BaseClass0>)
		ldstr "G1_C4::Method6.MI.4797<System.Object>()"
		ldstr "class IBase1`1<class Generated703.BaseClass1> on type class Generated703.G1_C4`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated703.IBase1`1<class Generated703.BaseClass0>::Method4()
		calli default string(class Generated703.G1_C4`1<class Generated703.BaseClass0>)
		ldstr "G1_C4::Method4.MI.4793()"
		ldstr "class IBase1`1<class Generated703.BaseClass0> on type class Generated703.G1_C4`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated703.IBase1`1<class Generated703.BaseClass0>::Method5()
		calli default string(class Generated703.G1_C4`1<class Generated703.BaseClass0>)
		ldstr "G1_C4::Method5.MI.4795()"
		ldstr "class IBase1`1<class Generated703.BaseClass0> on type class Generated703.G1_C4`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated703.IBase1`1<class Generated703.BaseClass0>::Method6<object>()
		calli default string(class Generated703.G1_C4`1<class Generated703.BaseClass0>)
		ldstr "G1_C4::Method6.MI.4797<System.Object>()"
		ldstr "class IBase1`1<class Generated703.BaseClass0> on type class Generated703.G1_C4`1<class Generated703.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated703.G1_C4`1<class Generated703.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated703.G1_C4`1<class Generated703.BaseClass1>::ClassMethod1323<object>()
		calli default string(class Generated703.G1_C4`1<class Generated703.BaseClass1>)
		ldstr "G1_C4::ClassMethod1323.4800<System.Object>()"
		ldstr "class G1_C4`1<class Generated703.BaseClass1> on type class Generated703.G1_C4`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated703.G1_C4`1<class Generated703.BaseClass1>::ClassMethod1322()
		calli default string(class Generated703.G1_C4`1<class Generated703.BaseClass1>)
		ldstr "G1_C4::ClassMethod1322.4799()"
		ldstr "class G1_C4`1<class Generated703.BaseClass1> on type class Generated703.G1_C4`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated703.G1_C4`1<class Generated703.BaseClass1>::ClassMethod1321()
		calli default string(class Generated703.G1_C4`1<class Generated703.BaseClass1>)
		ldstr "G1_C4::ClassMethod1321.4798()"
		ldstr "class G1_C4`1<class Generated703.BaseClass1> on type class Generated703.G1_C4`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated703.G1_C4`1<class Generated703.BaseClass1>::Method6<object>()
		calli default string(class Generated703.G1_C4`1<class Generated703.BaseClass1>)
		ldstr "G1_C4::Method6.4796<System.Object>()"
		ldstr "class G1_C4`1<class Generated703.BaseClass1> on type class Generated703.G1_C4`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated703.G1_C4`1<class Generated703.BaseClass1>::Method5()
		calli default string(class Generated703.G1_C4`1<class Generated703.BaseClass1>)
		ldstr "G1_C4::Method5.4794()"
		ldstr "class G1_C4`1<class Generated703.BaseClass1> on type class Generated703.G1_C4`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated703.G1_C4`1<class Generated703.BaseClass1>::Method4()
		calli default string(class Generated703.G1_C4`1<class Generated703.BaseClass1>)
		ldstr "G1_C4::Method4.4792()"
		ldstr "class G1_C4`1<class Generated703.BaseClass1> on type class Generated703.G1_C4`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated703.G1_C4`1<class Generated703.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated703.G1_C4`1<class Generated703.BaseClass1>::Method7<object>()
		calli default string(class Generated703.G1_C4`1<class Generated703.BaseClass1>)
		ldstr "G1_C4::Method7.4790<System.Object>()"
		ldstr "class G1_C4`1<class Generated703.BaseClass1> on type class Generated703.G1_C4`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated703.IBase2`2<class Generated703.BaseClass0,class Generated703.BaseClass1>::Method7<object>()
		calli default string(class Generated703.G1_C4`1<class Generated703.BaseClass1>)
		ldstr "G1_C4::Method7.MI.4791<System.Object>()"
		ldstr "class IBase2`2<class Generated703.BaseClass0,class Generated703.BaseClass1> on type class Generated703.G1_C4`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated703.IBase1`1<class Generated703.BaseClass1>::Method4()
		calli default string(class Generated703.G1_C4`1<class Generated703.BaseClass1>)
		ldstr "G1_C4::Method4.MI.4793()"
		ldstr "class IBase1`1<class Generated703.BaseClass1> on type class Generated703.G1_C4`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated703.IBase1`1<class Generated703.BaseClass1>::Method5()
		calli default string(class Generated703.G1_C4`1<class Generated703.BaseClass1>)
		ldstr "G1_C4::Method5.MI.4795()"
		ldstr "class IBase1`1<class Generated703.BaseClass1> on type class Generated703.G1_C4`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated703.IBase1`1<class Generated703.BaseClass1>::Method6<object>()
		calli default string(class Generated703.G1_C4`1<class Generated703.BaseClass1>)
		ldstr "G1_C4::Method6.MI.4797<System.Object>()"
		ldstr "class IBase1`1<class Generated703.BaseClass1> on type class Generated703.G1_C4`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated703.IBase1`1<class Generated703.BaseClass0>::Method4()
		calli default string(class Generated703.G1_C4`1<class Generated703.BaseClass1>)
		ldstr "G1_C4::Method4.MI.4793()"
		ldstr "class IBase1`1<class Generated703.BaseClass0> on type class Generated703.G1_C4`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated703.IBase1`1<class Generated703.BaseClass0>::Method5()
		calli default string(class Generated703.G1_C4`1<class Generated703.BaseClass1>)
		ldstr "G1_C4::Method5.MI.4795()"
		ldstr "class IBase1`1<class Generated703.BaseClass0> on type class Generated703.G1_C4`1<class Generated703.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated703.IBase1`1<class Generated703.BaseClass0>::Method6<object>()
		calli default string(class Generated703.G1_C4`1<class Generated703.BaseClass1>)
		ldstr "G1_C4::Method6.MI.4797<System.Object>()"
		ldstr "class IBase1`1<class Generated703.BaseClass0> on type class Generated703.G1_C4`1<class Generated703.BaseClass1>"
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
		call void Generated703.Generated703::MethodCallingTest()
		call void Generated703.Generated703::ConstrainedCallsTest()
		call void Generated703.Generated703::StructConstrainedInterfaceCallsTest()
		call void Generated703.Generated703::CalliTest()
		ldc.i4 100
		ret
	}
}
