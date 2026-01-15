// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.class public Generated879.BaseClass0 
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void [mscorlib]System.Object::.ctor()
		ret
	}
}
.class public Generated879.BaseClass1 
		extends Generated879.BaseClass0
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void Generated879.BaseClass0::.ctor()
		ret
	}
}
.class public Generated879.G3_C1351`1<T0> 
		extends class Generated879.G2_C384`2<!T0,class Generated879.BaseClass1>
		implements class Generated879.IBase2`2<class Generated879.BaseClass1,class Generated879.BaseClass1> 
{
	.method public hidebysig newslot virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G3_C1351::Method7.15724<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase2<class Generated879.BaseClass1,class Generated879.BaseClass1>.Method7'<M0>() cil managed noinlining { 
		.override method instance string class Generated879.IBase2`2<class Generated879.BaseClass1,class Generated879.BaseClass1>::Method7<[1]>()
		ldstr "G3_C1351::Method7.MI.15725<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod4096() cil managed noinlining { 
		ldstr "G3_C1351::ClassMethod4096.15726()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod4097() cil managed noinlining { 
		ldstr "G3_C1351::ClassMethod4097.15727()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod4098<M0>() cil managed noinlining { 
		ldstr "G3_C1351::ClassMethod4098.15728<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod4099<M0>() cil managed noinlining { 
		ldstr "G3_C1351::ClassMethod4099.15729<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class Generated879.G2_C384`2<!T0,class Generated879.BaseClass1>::.ctor()
		ret
	}
}
.class public Generated879.G2_C384`2<T0, T1> 
		extends class Generated879.G1_C7`2<!T1,class Generated879.BaseClass0>
		implements class Generated879.IBase1`1<!T0>, class Generated879.IBase2`2<!T1,class Generated879.BaseClass1> 
{
	.method public hidebysig newslot virtual instance string Method4() cil managed noinlining { 
		ldstr "G2_C384::Method4.8389()"
		ret
	}
	.method public hidebysig virtual instance string Method5() cil managed noinlining { 
		ldstr "G2_C384::Method5.8390()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<T0>.Method5'() cil managed noinlining { 
		.override method instance string class Generated879.IBase1`1<!T0>::Method5()
		ldstr "G2_C384::Method5.MI.8391()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G2_C384::Method6.8392<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<T0>.Method6'<M0>() cil managed noinlining { 
		.override method instance string class Generated879.IBase1`1<!T0>::Method6<[1]>()
		ldstr "G2_C384::Method6.MI.8393<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G2_C384::Method7.8394<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase2<T1,class Generated879.BaseClass1>.Method7'<M0>() cil managed noinlining { 
		.override method instance string class Generated879.IBase2`2<!T1,class Generated879.BaseClass1>::Method7<[1]>()
		ldstr "G2_C384::Method7.MI.8395<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod2114<M0>() cil managed noinlining { 
		ldstr "G2_C384::ClassMethod2114.8396<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod2115<M0>() cil managed noinlining { 
		ldstr "G2_C384::ClassMethod2115.8397<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class Generated879.G1_C7`2<!T1,class Generated879.BaseClass0>::.ctor()
		ret
	}
}
.class interface public abstract Generated879.IBase2`2<+T0, -T1> 
{
	.method public hidebysig newslot abstract virtual instance string Method7<M0>() cil managed { }
}
.class public abstract Generated879.G1_C7`2<T0, T1> 
		implements class Generated879.IBase1`1<!T0>, class Generated879.IBase2`2<class Generated879.BaseClass1,!T0> 
{
	.method public hidebysig virtual instance string Method4() cil managed noinlining { 
		ldstr "G1_C7::Method4.4811()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method5() cil managed noinlining { 
		ldstr "G1_C7::Method5.4812()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G1_C7::Method6.4813<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<T0>.Method6'<M0>() cil managed noinlining { 
		.override method instance string class Generated879.IBase1`1<!T0>::Method6<[1]>()
		ldstr "G1_C7::Method6.MI.4814<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G1_C7::Method7.4815<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase2<class Generated879.BaseClass1,T0>.Method7'<M0>() cil managed noinlining { 
		.override method instance string class Generated879.IBase2`2<class Generated879.BaseClass1,!T0>::Method7<[1]>()
		ldstr "G1_C7::Method7.MI.4816<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1328() cil managed noinlining { 
		ldstr "G1_C7::ClassMethod1328.4817()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1329() cil managed noinlining { 
		ldstr "G1_C7::ClassMethod1329.4818()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1330<M0>() cil managed noinlining { 
		ldstr "G1_C7::ClassMethod1330.4819<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1331<M0>() cil managed noinlining { 
		ldstr "G1_C7::ClassMethod1331.4820<"
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
.class interface public abstract Generated879.IBase1`1<+T0> 
{
	.method public hidebysig newslot abstract virtual instance string Method4() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method5() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method6<M0>() cil managed { }
}
.class public auto ansi beforefieldinit Generated879.Generated879 {
	.method static void M.BaseClass0<(Generated879.BaseClass0)W>(!!W inst, string exp) cil managed {
		.maxstack 5
		.locals init (string[] actualResults)
		ldc.i4.s    0
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.BaseClass0<(Generated879.BaseClass0)W>(!!W inst, string exp)"
		ldc.i4.s 0
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.BaseClass1<(Generated879.BaseClass1)W>(!!W inst, string exp) cil managed {
		.maxstack 5
		.locals init (string[] actualResults)
		ldc.i4.s    0
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.BaseClass1<(Generated879.BaseClass1)W>(!!W inst, string exp)"
		ldc.i4.s 0
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1351.T<T0,(class Generated879.G3_C1351`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 19
		.locals init (string[] actualResults)
		ldc.i4.s    14
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1351.T<T0,(class Generated879.G3_C1351`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 14
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G3_C1351`1<!!T0>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G3_C1351`1<!!T0>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G3_C1351`1<!!T0>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G3_C1351`1<!!T0>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G3_C1351`1<!!T0>::ClassMethod2114<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G3_C1351`1<!!T0>::ClassMethod2115<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G3_C1351`1<!!T0>::ClassMethod4096()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G3_C1351`1<!!T0>::ClassMethod4097()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G3_C1351`1<!!T0>::ClassMethod4098<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G3_C1351`1<!!T0>::ClassMethod4099<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G3_C1351`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G3_C1351`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G3_C1351`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G3_C1351`1<!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1351.A<(class Generated879.G3_C1351`1<class Generated879.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 19
		.locals init (string[] actualResults)
		ldc.i4.s    14
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1351.A<(class Generated879.G3_C1351`1<class Generated879.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 14
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G3_C1351`1<class Generated879.BaseClass0>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G3_C1351`1<class Generated879.BaseClass0>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G3_C1351`1<class Generated879.BaseClass0>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G3_C1351`1<class Generated879.BaseClass0>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G3_C1351`1<class Generated879.BaseClass0>::ClassMethod2114<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G3_C1351`1<class Generated879.BaseClass0>::ClassMethod2115<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G3_C1351`1<class Generated879.BaseClass0>::ClassMethod4096()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G3_C1351`1<class Generated879.BaseClass0>::ClassMethod4097()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G3_C1351`1<class Generated879.BaseClass0>::ClassMethod4098<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G3_C1351`1<class Generated879.BaseClass0>::ClassMethod4099<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G3_C1351`1<class Generated879.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G3_C1351`1<class Generated879.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G3_C1351`1<class Generated879.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G3_C1351`1<class Generated879.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1351.B<(class Generated879.G3_C1351`1<class Generated879.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 19
		.locals init (string[] actualResults)
		ldc.i4.s    14
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1351.B<(class Generated879.G3_C1351`1<class Generated879.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 14
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G3_C1351`1<class Generated879.BaseClass1>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G3_C1351`1<class Generated879.BaseClass1>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G3_C1351`1<class Generated879.BaseClass1>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G3_C1351`1<class Generated879.BaseClass1>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G3_C1351`1<class Generated879.BaseClass1>::ClassMethod2114<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G3_C1351`1<class Generated879.BaseClass1>::ClassMethod2115<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G3_C1351`1<class Generated879.BaseClass1>::ClassMethod4096()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G3_C1351`1<class Generated879.BaseClass1>::ClassMethod4097()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G3_C1351`1<class Generated879.BaseClass1>::ClassMethod4098<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G3_C1351`1<class Generated879.BaseClass1>::ClassMethod4099<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G3_C1351`1<class Generated879.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G3_C1351`1<class Generated879.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G3_C1351`1<class Generated879.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G3_C1351`1<class Generated879.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C384.T.T<T0,T1,(class Generated879.G2_C384`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 15
		.locals init (string[] actualResults)
		ldc.i4.s    10
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C384.T.T<T0,T1,(class Generated879.G2_C384`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 10
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G2_C384`2<!!T0,!!T1>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G2_C384`2<!!T0,!!T1>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G2_C384`2<!!T0,!!T1>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G2_C384`2<!!T0,!!T1>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G2_C384`2<!!T0,!!T1>::ClassMethod2114<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G2_C384`2<!!T0,!!T1>::ClassMethod2115<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G2_C384`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G2_C384`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G2_C384`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G2_C384`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C384.A.T<T1,(class Generated879.G2_C384`2<class Generated879.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 15
		.locals init (string[] actualResults)
		ldc.i4.s    10
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C384.A.T<T1,(class Generated879.G2_C384`2<class Generated879.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 10
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G2_C384`2<class Generated879.BaseClass0,!!T1>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G2_C384`2<class Generated879.BaseClass0,!!T1>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G2_C384`2<class Generated879.BaseClass0,!!T1>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G2_C384`2<class Generated879.BaseClass0,!!T1>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G2_C384`2<class Generated879.BaseClass0,!!T1>::ClassMethod2114<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G2_C384`2<class Generated879.BaseClass0,!!T1>::ClassMethod2115<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G2_C384`2<class Generated879.BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G2_C384`2<class Generated879.BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G2_C384`2<class Generated879.BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G2_C384`2<class Generated879.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C384.A.A<(class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 15
		.locals init (string[] actualResults)
		ldc.i4.s    10
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C384.A.A<(class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 10
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>::ClassMethod2114<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>::ClassMethod2115<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C384.A.B<(class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 15
		.locals init (string[] actualResults)
		ldc.i4.s    10
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C384.A.B<(class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 10
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>::ClassMethod2114<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>::ClassMethod2115<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C384.B.T<T1,(class Generated879.G2_C384`2<class Generated879.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 15
		.locals init (string[] actualResults)
		ldc.i4.s    10
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C384.B.T<T1,(class Generated879.G2_C384`2<class Generated879.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 10
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G2_C384`2<class Generated879.BaseClass1,!!T1>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G2_C384`2<class Generated879.BaseClass1,!!T1>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G2_C384`2<class Generated879.BaseClass1,!!T1>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G2_C384`2<class Generated879.BaseClass1,!!T1>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G2_C384`2<class Generated879.BaseClass1,!!T1>::ClassMethod2114<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G2_C384`2<class Generated879.BaseClass1,!!T1>::ClassMethod2115<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G2_C384`2<class Generated879.BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G2_C384`2<class Generated879.BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G2_C384`2<class Generated879.BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G2_C384`2<class Generated879.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C384.B.A<(class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 15
		.locals init (string[] actualResults)
		ldc.i4.s    10
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C384.B.A<(class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 10
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::ClassMethod2114<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::ClassMethod2115<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C384.B.B<(class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 15
		.locals init (string[] actualResults)
		ldc.i4.s    10
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C384.B.B<(class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 10
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>::ClassMethod2114<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>::ClassMethod2115<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.T.T<T0,T1,(class Generated879.IBase2`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.T.T<T0,T1,(class Generated879.IBase2`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.IBase2`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.T<T1,(class Generated879.IBase2`2<class Generated879.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.T<T1,(class Generated879.IBase2`2<class Generated879.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.IBase2`2<class Generated879.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.A<(class Generated879.IBase2`2<class Generated879.BaseClass0,class Generated879.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.A<(class Generated879.IBase2`2<class Generated879.BaseClass0,class Generated879.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.IBase2`2<class Generated879.BaseClass0,class Generated879.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.B<(class Generated879.IBase2`2<class Generated879.BaseClass0,class Generated879.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.B<(class Generated879.IBase2`2<class Generated879.BaseClass0,class Generated879.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.IBase2`2<class Generated879.BaseClass0,class Generated879.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.T<T1,(class Generated879.IBase2`2<class Generated879.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.T<T1,(class Generated879.IBase2`2<class Generated879.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.IBase2`2<class Generated879.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.A<(class Generated879.IBase2`2<class Generated879.BaseClass1,class Generated879.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.A<(class Generated879.IBase2`2<class Generated879.BaseClass1,class Generated879.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.IBase2`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.B<(class Generated879.IBase2`2<class Generated879.BaseClass1,class Generated879.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.B<(class Generated879.IBase2`2<class Generated879.BaseClass1,class Generated879.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.IBase2`2<class Generated879.BaseClass1,class Generated879.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C7.T.T<T0,T1,(class Generated879.G1_C7`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C7.T.T<T0,T1,(class Generated879.G1_C7`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G1_C7`2<!!T0,!!T1>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G1_C7`2<!!T0,!!T1>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G1_C7`2<!!T0,!!T1>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G1_C7`2<!!T0,!!T1>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G1_C7`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G1_C7`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G1_C7`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G1_C7`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C7.A.T<T1,(class Generated879.G1_C7`2<class Generated879.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C7.A.T<T1,(class Generated879.G1_C7`2<class Generated879.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G1_C7`2<class Generated879.BaseClass0,!!T1>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G1_C7`2<class Generated879.BaseClass0,!!T1>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G1_C7`2<class Generated879.BaseClass0,!!T1>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G1_C7`2<class Generated879.BaseClass0,!!T1>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G1_C7`2<class Generated879.BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G1_C7`2<class Generated879.BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G1_C7`2<class Generated879.BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G1_C7`2<class Generated879.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C7.A.A<(class Generated879.G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C7.A.A<(class Generated879.G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C7.A.B<(class Generated879.G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C7.A.B<(class Generated879.G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass1>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass1>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass1>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass1>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C7.B.T<T1,(class Generated879.G1_C7`2<class Generated879.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C7.B.T<T1,(class Generated879.G1_C7`2<class Generated879.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G1_C7`2<class Generated879.BaseClass1,!!T1>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G1_C7`2<class Generated879.BaseClass1,!!T1>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G1_C7`2<class Generated879.BaseClass1,!!T1>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G1_C7`2<class Generated879.BaseClass1,!!T1>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G1_C7`2<class Generated879.BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G1_C7`2<class Generated879.BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G1_C7`2<class Generated879.BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G1_C7`2<class Generated879.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C7.B.A<(class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C7.B.A<(class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C7.B.B<(class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C7.B.B<(class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass1>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass1>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass1>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass1>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.T<T0,(class Generated879.IBase1`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.T<T0,(class Generated879.IBase1`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.IBase1`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.IBase1`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.IBase1`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.A<(class Generated879.IBase1`1<class Generated879.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.A<(class Generated879.IBase1`1<class Generated879.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.IBase1`1<class Generated879.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.IBase1`1<class Generated879.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.IBase1`1<class Generated879.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.B<(class Generated879.IBase1`1<class Generated879.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.B<(class Generated879.IBase1`1<class Generated879.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.IBase1`1<class Generated879.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.IBase1`1<class Generated879.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated879.IBase1`1<class Generated879.BaseClass1>::Method6<object>()
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
		newobj instance void class Generated879.G3_C1351`1<class Generated879.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>
		callvirt instance string class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::ClassMethod1331<object>()
		ldstr "G1_C7::ClassMethod1331.4820<System.Object>()"
		ldstr "class G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0> on type class Generated879.G3_C1351`1<class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>
		callvirt instance string class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::ClassMethod1330<object>()
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0> on type class Generated879.G3_C1351`1<class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>
		callvirt instance string class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::ClassMethod1329()
		ldstr "G1_C7::ClassMethod1329.4818()"
		ldstr "class G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0> on type class Generated879.G3_C1351`1<class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>
		callvirt instance string class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::ClassMethod1328()
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0> on type class Generated879.G3_C1351`1<class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>
		callvirt instance string class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::Method7<object>()
		ldstr "G2_C384::Method7.8394<System.Object>()"
		ldstr "class G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0> on type class Generated879.G3_C1351`1<class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>
		callvirt instance string class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::Method6<object>()
		ldstr "G1_C7::Method6.4813<System.Object>()"
		ldstr "class G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0> on type class Generated879.G3_C1351`1<class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>
		callvirt instance string class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::Method5()
		ldstr "G2_C384::Method5.8390()"
		ldstr "class G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0> on type class Generated879.G3_C1351`1<class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>
		callvirt instance string class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::Method4()
		ldstr "G1_C7::Method4.4811()"
		ldstr "class G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0> on type class Generated879.G3_C1351`1<class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated879.IBase1`1<class Generated879.BaseClass1>::Method4()
		ldstr "G1_C7::Method4.4811()"
		ldstr "class IBase1`1<class Generated879.BaseClass1> on type class Generated879.G3_C1351`1<class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated879.IBase1`1<class Generated879.BaseClass1>::Method5()
		ldstr "G2_C384::Method5.8390()"
		ldstr "class IBase1`1<class Generated879.BaseClass1> on type class Generated879.G3_C1351`1<class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated879.IBase1`1<class Generated879.BaseClass1>::Method6<object>()
		ldstr "G1_C7::Method6.MI.4814<System.Object>()"
		ldstr "class IBase1`1<class Generated879.BaseClass1> on type class Generated879.G3_C1351`1<class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated879.IBase2`2<class Generated879.BaseClass1,class Generated879.BaseClass1>::Method7<object>()
		ldstr "G3_C1351::Method7.MI.15725<System.Object>()"
		ldstr "class IBase2`2<class Generated879.BaseClass1,class Generated879.BaseClass1> on type class Generated879.G3_C1351`1<class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated879.IBase1`1<class Generated879.BaseClass0>::Method4()
		ldstr "G2_C384::Method4.8389()"
		ldstr "class IBase1`1<class Generated879.BaseClass0> on type class Generated879.G3_C1351`1<class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated879.IBase1`1<class Generated879.BaseClass0>::Method5()
		ldstr "G2_C384::Method5.MI.8391()"
		ldstr "class IBase1`1<class Generated879.BaseClass0> on type class Generated879.G3_C1351`1<class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated879.IBase1`1<class Generated879.BaseClass0>::Method6<object>()
		ldstr "G2_C384::Method6.MI.8393<System.Object>()"
		ldstr "class IBase1`1<class Generated879.BaseClass0> on type class Generated879.G3_C1351`1<class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated879.IBase2`2<class Generated879.BaseClass0,class Generated879.BaseClass1>::Method7<object>()
		ldstr "G3_C1351::Method7.MI.15725<System.Object>()"
		ldstr "class IBase2`2<class Generated879.BaseClass0,class Generated879.BaseClass1> on type class Generated879.G3_C1351`1<class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>
		callvirt instance string class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>::ClassMethod2115<object>()
		ldstr "G2_C384::ClassMethod2115.8397<System.Object>()"
		ldstr "class G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1> on type class Generated879.G3_C1351`1<class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>
		callvirt instance string class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>::ClassMethod2114<object>()
		ldstr "G2_C384::ClassMethod2114.8396<System.Object>()"
		ldstr "class G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1> on type class Generated879.G3_C1351`1<class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>
		callvirt instance string class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>::Method7<object>()
		ldstr "G2_C384::Method7.8394<System.Object>()"
		ldstr "class G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1> on type class Generated879.G3_C1351`1<class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>
		callvirt instance string class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>::Method6<object>()
		ldstr "G2_C384::Method6.8392<System.Object>()"
		ldstr "class G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1> on type class Generated879.G3_C1351`1<class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>
		callvirt instance string class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>::Method5()
		ldstr "G2_C384::Method5.8390()"
		ldstr "class G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1> on type class Generated879.G3_C1351`1<class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>
		callvirt instance string class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>::Method4()
		ldstr "G2_C384::Method4.8389()"
		ldstr "class G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1> on type class Generated879.G3_C1351`1<class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>
		callvirt instance string class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>::ClassMethod1331<object>()
		ldstr "G1_C7::ClassMethod1331.4820<System.Object>()"
		ldstr "class G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1> on type class Generated879.G3_C1351`1<class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>
		callvirt instance string class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>::ClassMethod1330<object>()
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1> on type class Generated879.G3_C1351`1<class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>
		callvirt instance string class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>::ClassMethod1329()
		ldstr "G1_C7::ClassMethod1329.4818()"
		ldstr "class G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1> on type class Generated879.G3_C1351`1<class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>
		callvirt instance string class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>::ClassMethod1328()
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1> on type class Generated879.G3_C1351`1<class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated879.G3_C1351`1<class Generated879.BaseClass0>
		callvirt instance string class Generated879.G3_C1351`1<class Generated879.BaseClass0>::ClassMethod4099<object>()
		ldstr "G3_C1351::ClassMethod4099.15729<System.Object>()"
		ldstr "class G3_C1351`1<class Generated879.BaseClass0> on type class Generated879.G3_C1351`1<class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G3_C1351`1<class Generated879.BaseClass0>
		callvirt instance string class Generated879.G3_C1351`1<class Generated879.BaseClass0>::ClassMethod4098<object>()
		ldstr "G3_C1351::ClassMethod4098.15728<System.Object>()"
		ldstr "class G3_C1351`1<class Generated879.BaseClass0> on type class Generated879.G3_C1351`1<class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G3_C1351`1<class Generated879.BaseClass0>
		callvirt instance string class Generated879.G3_C1351`1<class Generated879.BaseClass0>::ClassMethod4097()
		ldstr "G3_C1351::ClassMethod4097.15727()"
		ldstr "class G3_C1351`1<class Generated879.BaseClass0> on type class Generated879.G3_C1351`1<class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G3_C1351`1<class Generated879.BaseClass0>
		callvirt instance string class Generated879.G3_C1351`1<class Generated879.BaseClass0>::ClassMethod4096()
		ldstr "G3_C1351::ClassMethod4096.15726()"
		ldstr "class G3_C1351`1<class Generated879.BaseClass0> on type class Generated879.G3_C1351`1<class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G3_C1351`1<class Generated879.BaseClass0>
		callvirt instance string class Generated879.G3_C1351`1<class Generated879.BaseClass0>::Method7<object>()
		ldstr "G3_C1351::Method7.15724<System.Object>()"
		ldstr "class G3_C1351`1<class Generated879.BaseClass0> on type class Generated879.G3_C1351`1<class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G3_C1351`1<class Generated879.BaseClass0>
		callvirt instance string class Generated879.G3_C1351`1<class Generated879.BaseClass0>::ClassMethod2115<object>()
		ldstr "G2_C384::ClassMethod2115.8397<System.Object>()"
		ldstr "class G3_C1351`1<class Generated879.BaseClass0> on type class Generated879.G3_C1351`1<class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G3_C1351`1<class Generated879.BaseClass0>
		callvirt instance string class Generated879.G3_C1351`1<class Generated879.BaseClass0>::ClassMethod2114<object>()
		ldstr "G2_C384::ClassMethod2114.8396<System.Object>()"
		ldstr "class G3_C1351`1<class Generated879.BaseClass0> on type class Generated879.G3_C1351`1<class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G3_C1351`1<class Generated879.BaseClass0>
		callvirt instance string class Generated879.G3_C1351`1<class Generated879.BaseClass0>::Method6<object>()
		ldstr "G2_C384::Method6.8392<System.Object>()"
		ldstr "class G3_C1351`1<class Generated879.BaseClass0> on type class Generated879.G3_C1351`1<class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G3_C1351`1<class Generated879.BaseClass0>
		callvirt instance string class Generated879.G3_C1351`1<class Generated879.BaseClass0>::Method5()
		ldstr "G2_C384::Method5.8390()"
		ldstr "class G3_C1351`1<class Generated879.BaseClass0> on type class Generated879.G3_C1351`1<class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G3_C1351`1<class Generated879.BaseClass0>
		callvirt instance string class Generated879.G3_C1351`1<class Generated879.BaseClass0>::Method4()
		ldstr "G2_C384::Method4.8389()"
		ldstr "class G3_C1351`1<class Generated879.BaseClass0> on type class Generated879.G3_C1351`1<class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G3_C1351`1<class Generated879.BaseClass0>
		callvirt instance string class Generated879.G3_C1351`1<class Generated879.BaseClass0>::ClassMethod1331<object>()
		ldstr "G1_C7::ClassMethod1331.4820<System.Object>()"
		ldstr "class G3_C1351`1<class Generated879.BaseClass0> on type class Generated879.G3_C1351`1<class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G3_C1351`1<class Generated879.BaseClass0>
		callvirt instance string class Generated879.G3_C1351`1<class Generated879.BaseClass0>::ClassMethod1330<object>()
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G3_C1351`1<class Generated879.BaseClass0> on type class Generated879.G3_C1351`1<class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G3_C1351`1<class Generated879.BaseClass0>
		callvirt instance string class Generated879.G3_C1351`1<class Generated879.BaseClass0>::ClassMethod1329()
		ldstr "G1_C7::ClassMethod1329.4818()"
		ldstr "class G3_C1351`1<class Generated879.BaseClass0> on type class Generated879.G3_C1351`1<class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G3_C1351`1<class Generated879.BaseClass0>
		callvirt instance string class Generated879.G3_C1351`1<class Generated879.BaseClass0>::ClassMethod1328()
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G3_C1351`1<class Generated879.BaseClass0> on type class Generated879.G3_C1351`1<class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated879.G3_C1351`1<class Generated879.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>
		callvirt instance string class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::ClassMethod1331<object>()
		ldstr "G1_C7::ClassMethod1331.4820<System.Object>()"
		ldstr "class G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0> on type class Generated879.G3_C1351`1<class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>
		callvirt instance string class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::ClassMethod1330<object>()
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0> on type class Generated879.G3_C1351`1<class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>
		callvirt instance string class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::ClassMethod1329()
		ldstr "G1_C7::ClassMethod1329.4818()"
		ldstr "class G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0> on type class Generated879.G3_C1351`1<class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>
		callvirt instance string class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::ClassMethod1328()
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0> on type class Generated879.G3_C1351`1<class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>
		callvirt instance string class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::Method7<object>()
		ldstr "G2_C384::Method7.8394<System.Object>()"
		ldstr "class G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0> on type class Generated879.G3_C1351`1<class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>
		callvirt instance string class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::Method6<object>()
		ldstr "G1_C7::Method6.4813<System.Object>()"
		ldstr "class G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0> on type class Generated879.G3_C1351`1<class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>
		callvirt instance string class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::Method5()
		ldstr "G2_C384::Method5.8390()"
		ldstr "class G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0> on type class Generated879.G3_C1351`1<class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>
		callvirt instance string class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::Method4()
		ldstr "G1_C7::Method4.4811()"
		ldstr "class G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0> on type class Generated879.G3_C1351`1<class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated879.IBase1`1<class Generated879.BaseClass1>::Method4()
		ldstr "G2_C384::Method4.8389()"
		ldstr "class IBase1`1<class Generated879.BaseClass1> on type class Generated879.G3_C1351`1<class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated879.IBase1`1<class Generated879.BaseClass1>::Method5()
		ldstr "G2_C384::Method5.MI.8391()"
		ldstr "class IBase1`1<class Generated879.BaseClass1> on type class Generated879.G3_C1351`1<class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated879.IBase1`1<class Generated879.BaseClass1>::Method6<object>()
		ldstr "G2_C384::Method6.MI.8393<System.Object>()"
		ldstr "class IBase1`1<class Generated879.BaseClass1> on type class Generated879.G3_C1351`1<class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated879.IBase2`2<class Generated879.BaseClass1,class Generated879.BaseClass1>::Method7<object>()
		ldstr "G3_C1351::Method7.MI.15725<System.Object>()"
		ldstr "class IBase2`2<class Generated879.BaseClass1,class Generated879.BaseClass1> on type class Generated879.G3_C1351`1<class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated879.IBase1`1<class Generated879.BaseClass0>::Method4()
		ldstr "G2_C384::Method4.8389()"
		ldstr "class IBase1`1<class Generated879.BaseClass0> on type class Generated879.G3_C1351`1<class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated879.IBase1`1<class Generated879.BaseClass0>::Method5()
		ldstr "G2_C384::Method5.MI.8391()"
		ldstr "class IBase1`1<class Generated879.BaseClass0> on type class Generated879.G3_C1351`1<class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated879.IBase1`1<class Generated879.BaseClass0>::Method6<object>()
		ldstr "G2_C384::Method6.MI.8393<System.Object>()"
		ldstr "class IBase1`1<class Generated879.BaseClass0> on type class Generated879.G3_C1351`1<class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated879.IBase2`2<class Generated879.BaseClass0,class Generated879.BaseClass1>::Method7<object>()
		ldstr "G3_C1351::Method7.MI.15725<System.Object>()"
		ldstr "class IBase2`2<class Generated879.BaseClass0,class Generated879.BaseClass1> on type class Generated879.G3_C1351`1<class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>
		callvirt instance string class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>::ClassMethod2115<object>()
		ldstr "G2_C384::ClassMethod2115.8397<System.Object>()"
		ldstr "class G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1> on type class Generated879.G3_C1351`1<class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>
		callvirt instance string class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>::ClassMethod2114<object>()
		ldstr "G2_C384::ClassMethod2114.8396<System.Object>()"
		ldstr "class G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1> on type class Generated879.G3_C1351`1<class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>
		callvirt instance string class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>::Method7<object>()
		ldstr "G2_C384::Method7.8394<System.Object>()"
		ldstr "class G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1> on type class Generated879.G3_C1351`1<class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>
		callvirt instance string class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>::Method6<object>()
		ldstr "G2_C384::Method6.8392<System.Object>()"
		ldstr "class G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1> on type class Generated879.G3_C1351`1<class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>
		callvirt instance string class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>::Method5()
		ldstr "G2_C384::Method5.8390()"
		ldstr "class G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1> on type class Generated879.G3_C1351`1<class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>
		callvirt instance string class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>::Method4()
		ldstr "G2_C384::Method4.8389()"
		ldstr "class G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1> on type class Generated879.G3_C1351`1<class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>
		callvirt instance string class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>::ClassMethod1331<object>()
		ldstr "G1_C7::ClassMethod1331.4820<System.Object>()"
		ldstr "class G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1> on type class Generated879.G3_C1351`1<class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>
		callvirt instance string class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>::ClassMethod1330<object>()
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1> on type class Generated879.G3_C1351`1<class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>
		callvirt instance string class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>::ClassMethod1329()
		ldstr "G1_C7::ClassMethod1329.4818()"
		ldstr "class G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1> on type class Generated879.G3_C1351`1<class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>
		callvirt instance string class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>::ClassMethod1328()
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1> on type class Generated879.G3_C1351`1<class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated879.G3_C1351`1<class Generated879.BaseClass1>
		callvirt instance string class Generated879.G3_C1351`1<class Generated879.BaseClass1>::ClassMethod4099<object>()
		ldstr "G3_C1351::ClassMethod4099.15729<System.Object>()"
		ldstr "class G3_C1351`1<class Generated879.BaseClass1> on type class Generated879.G3_C1351`1<class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G3_C1351`1<class Generated879.BaseClass1>
		callvirt instance string class Generated879.G3_C1351`1<class Generated879.BaseClass1>::ClassMethod4098<object>()
		ldstr "G3_C1351::ClassMethod4098.15728<System.Object>()"
		ldstr "class G3_C1351`1<class Generated879.BaseClass1> on type class Generated879.G3_C1351`1<class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G3_C1351`1<class Generated879.BaseClass1>
		callvirt instance string class Generated879.G3_C1351`1<class Generated879.BaseClass1>::ClassMethod4097()
		ldstr "G3_C1351::ClassMethod4097.15727()"
		ldstr "class G3_C1351`1<class Generated879.BaseClass1> on type class Generated879.G3_C1351`1<class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G3_C1351`1<class Generated879.BaseClass1>
		callvirt instance string class Generated879.G3_C1351`1<class Generated879.BaseClass1>::ClassMethod4096()
		ldstr "G3_C1351::ClassMethod4096.15726()"
		ldstr "class G3_C1351`1<class Generated879.BaseClass1> on type class Generated879.G3_C1351`1<class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G3_C1351`1<class Generated879.BaseClass1>
		callvirt instance string class Generated879.G3_C1351`1<class Generated879.BaseClass1>::Method7<object>()
		ldstr "G3_C1351::Method7.15724<System.Object>()"
		ldstr "class G3_C1351`1<class Generated879.BaseClass1> on type class Generated879.G3_C1351`1<class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G3_C1351`1<class Generated879.BaseClass1>
		callvirt instance string class Generated879.G3_C1351`1<class Generated879.BaseClass1>::ClassMethod2115<object>()
		ldstr "G2_C384::ClassMethod2115.8397<System.Object>()"
		ldstr "class G3_C1351`1<class Generated879.BaseClass1> on type class Generated879.G3_C1351`1<class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G3_C1351`1<class Generated879.BaseClass1>
		callvirt instance string class Generated879.G3_C1351`1<class Generated879.BaseClass1>::ClassMethod2114<object>()
		ldstr "G2_C384::ClassMethod2114.8396<System.Object>()"
		ldstr "class G3_C1351`1<class Generated879.BaseClass1> on type class Generated879.G3_C1351`1<class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G3_C1351`1<class Generated879.BaseClass1>
		callvirt instance string class Generated879.G3_C1351`1<class Generated879.BaseClass1>::Method6<object>()
		ldstr "G2_C384::Method6.8392<System.Object>()"
		ldstr "class G3_C1351`1<class Generated879.BaseClass1> on type class Generated879.G3_C1351`1<class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G3_C1351`1<class Generated879.BaseClass1>
		callvirt instance string class Generated879.G3_C1351`1<class Generated879.BaseClass1>::Method5()
		ldstr "G2_C384::Method5.8390()"
		ldstr "class G3_C1351`1<class Generated879.BaseClass1> on type class Generated879.G3_C1351`1<class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G3_C1351`1<class Generated879.BaseClass1>
		callvirt instance string class Generated879.G3_C1351`1<class Generated879.BaseClass1>::Method4()
		ldstr "G2_C384::Method4.8389()"
		ldstr "class G3_C1351`1<class Generated879.BaseClass1> on type class Generated879.G3_C1351`1<class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G3_C1351`1<class Generated879.BaseClass1>
		callvirt instance string class Generated879.G3_C1351`1<class Generated879.BaseClass1>::ClassMethod1331<object>()
		ldstr "G1_C7::ClassMethod1331.4820<System.Object>()"
		ldstr "class G3_C1351`1<class Generated879.BaseClass1> on type class Generated879.G3_C1351`1<class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G3_C1351`1<class Generated879.BaseClass1>
		callvirt instance string class Generated879.G3_C1351`1<class Generated879.BaseClass1>::ClassMethod1330<object>()
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G3_C1351`1<class Generated879.BaseClass1> on type class Generated879.G3_C1351`1<class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G3_C1351`1<class Generated879.BaseClass1>
		callvirt instance string class Generated879.G3_C1351`1<class Generated879.BaseClass1>::ClassMethod1329()
		ldstr "G1_C7::ClassMethod1329.4818()"
		ldstr "class G3_C1351`1<class Generated879.BaseClass1> on type class Generated879.G3_C1351`1<class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G3_C1351`1<class Generated879.BaseClass1>
		callvirt instance string class Generated879.G3_C1351`1<class Generated879.BaseClass1>::ClassMethod1328()
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G3_C1351`1<class Generated879.BaseClass1> on type class Generated879.G3_C1351`1<class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0>
		callvirt instance string class Generated879.G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0>::ClassMethod1331<object>()
		ldstr "G1_C7::ClassMethod1331.4820<System.Object>()"
		ldstr "class G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0>
		callvirt instance string class Generated879.G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0>::ClassMethod1330<object>()
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0>
		callvirt instance string class Generated879.G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0>::ClassMethod1329()
		ldstr "G1_C7::ClassMethod1329.4818()"
		ldstr "class G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0>
		callvirt instance string class Generated879.G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0>::ClassMethod1328()
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0>
		callvirt instance string class Generated879.G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0>::Method7<object>()
		ldstr "G2_C384::Method7.8394<System.Object>()"
		ldstr "class G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0>
		callvirt instance string class Generated879.G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0>::Method6<object>()
		ldstr "G1_C7::Method6.4813<System.Object>()"
		ldstr "class G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0>
		callvirt instance string class Generated879.G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0>::Method5()
		ldstr "G2_C384::Method5.8390()"
		ldstr "class G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0>
		callvirt instance string class Generated879.G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0>::Method4()
		ldstr "G1_C7::Method4.4811()"
		ldstr "class G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated879.IBase1`1<class Generated879.BaseClass0>::Method4()
		ldstr "G2_C384::Method4.8389()"
		ldstr "class IBase1`1<class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated879.IBase1`1<class Generated879.BaseClass0>::Method5()
		ldstr "G2_C384::Method5.MI.8391()"
		ldstr "class IBase1`1<class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated879.IBase1`1<class Generated879.BaseClass0>::Method6<object>()
		ldstr "G2_C384::Method6.MI.8393<System.Object>()"
		ldstr "class IBase1`1<class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated879.IBase2`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::Method7<object>()
		ldstr "G1_C7::Method7.MI.4816<System.Object>()"
		ldstr "class IBase2`2<class Generated879.BaseClass1,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated879.IBase2`2<class Generated879.BaseClass0,class Generated879.BaseClass0>::Method7<object>()
		ldstr "G1_C7::Method7.MI.4816<System.Object>()"
		ldstr "class IBase2`2<class Generated879.BaseClass0,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated879.IBase2`2<class Generated879.BaseClass0,class Generated879.BaseClass1>::Method7<object>()
		ldstr "G2_C384::Method7.MI.8395<System.Object>()"
		ldstr "class IBase2`2<class Generated879.BaseClass0,class Generated879.BaseClass1> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated879.IBase2`2<class Generated879.BaseClass1,class Generated879.BaseClass1>::Method7<object>()
		ldstr "G1_C7::Method7.MI.4816<System.Object>()"
		ldstr "class IBase2`2<class Generated879.BaseClass1,class Generated879.BaseClass1> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>
		callvirt instance string class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>::ClassMethod2115<object>()
		ldstr "G2_C384::ClassMethod2115.8397<System.Object>()"
		ldstr "class G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>
		callvirt instance string class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>::ClassMethod2114<object>()
		ldstr "G2_C384::ClassMethod2114.8396<System.Object>()"
		ldstr "class G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>
		callvirt instance string class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>::Method7<object>()
		ldstr "G2_C384::Method7.8394<System.Object>()"
		ldstr "class G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>
		callvirt instance string class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>::Method6<object>()
		ldstr "G2_C384::Method6.8392<System.Object>()"
		ldstr "class G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>
		callvirt instance string class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>::Method5()
		ldstr "G2_C384::Method5.8390()"
		ldstr "class G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>
		callvirt instance string class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>::Method4()
		ldstr "G2_C384::Method4.8389()"
		ldstr "class G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>
		callvirt instance string class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>::ClassMethod1331<object>()
		ldstr "G1_C7::ClassMethod1331.4820<System.Object>()"
		ldstr "class G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>
		callvirt instance string class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>::ClassMethod1330<object>()
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>
		callvirt instance string class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>::ClassMethod1329()
		ldstr "G1_C7::ClassMethod1329.4818()"
		ldstr "class G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>
		callvirt instance string class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>::ClassMethod1328()
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>
		callvirt instance string class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::ClassMethod1331<object>()
		ldstr "G1_C7::ClassMethod1331.4820<System.Object>()"
		ldstr "class G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>
		callvirt instance string class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::ClassMethod1330<object>()
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>
		callvirt instance string class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::ClassMethod1329()
		ldstr "G1_C7::ClassMethod1329.4818()"
		ldstr "class G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>
		callvirt instance string class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::ClassMethod1328()
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>
		callvirt instance string class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::Method7<object>()
		ldstr "G2_C384::Method7.8394<System.Object>()"
		ldstr "class G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>
		callvirt instance string class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::Method6<object>()
		ldstr "G1_C7::Method6.4813<System.Object>()"
		ldstr "class G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>
		callvirt instance string class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::Method5()
		ldstr "G2_C384::Method5.8390()"
		ldstr "class G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>
		callvirt instance string class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::Method4()
		ldstr "G1_C7::Method4.4811()"
		ldstr "class G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated879.IBase1`1<class Generated879.BaseClass1>::Method4()
		ldstr "G1_C7::Method4.4811()"
		ldstr "class IBase1`1<class Generated879.BaseClass1> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated879.IBase1`1<class Generated879.BaseClass1>::Method5()
		ldstr "G2_C384::Method5.8390()"
		ldstr "class IBase1`1<class Generated879.BaseClass1> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated879.IBase1`1<class Generated879.BaseClass1>::Method6<object>()
		ldstr "G1_C7::Method6.MI.4814<System.Object>()"
		ldstr "class IBase1`1<class Generated879.BaseClass1> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated879.IBase2`2<class Generated879.BaseClass1,class Generated879.BaseClass1>::Method7<object>()
		ldstr "G2_C384::Method7.MI.8395<System.Object>()"
		ldstr "class IBase2`2<class Generated879.BaseClass1,class Generated879.BaseClass1> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated879.IBase1`1<class Generated879.BaseClass0>::Method4()
		ldstr "G2_C384::Method4.8389()"
		ldstr "class IBase1`1<class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated879.IBase1`1<class Generated879.BaseClass0>::Method5()
		ldstr "G2_C384::Method5.MI.8391()"
		ldstr "class IBase1`1<class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated879.IBase1`1<class Generated879.BaseClass0>::Method6<object>()
		ldstr "G2_C384::Method6.MI.8393<System.Object>()"
		ldstr "class IBase1`1<class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated879.IBase2`2<class Generated879.BaseClass0,class Generated879.BaseClass1>::Method7<object>()
		ldstr "G2_C384::Method7.MI.8395<System.Object>()"
		ldstr "class IBase2`2<class Generated879.BaseClass0,class Generated879.BaseClass1> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>
		callvirt instance string class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>::ClassMethod2115<object>()
		ldstr "G2_C384::ClassMethod2115.8397<System.Object>()"
		ldstr "class G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>
		callvirt instance string class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>::ClassMethod2114<object>()
		ldstr "G2_C384::ClassMethod2114.8396<System.Object>()"
		ldstr "class G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>
		callvirt instance string class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>::Method7<object>()
		ldstr "G2_C384::Method7.8394<System.Object>()"
		ldstr "class G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>
		callvirt instance string class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>::Method6<object>()
		ldstr "G2_C384::Method6.8392<System.Object>()"
		ldstr "class G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>
		callvirt instance string class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>::Method5()
		ldstr "G2_C384::Method5.8390()"
		ldstr "class G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>
		callvirt instance string class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>::Method4()
		ldstr "G2_C384::Method4.8389()"
		ldstr "class G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>
		callvirt instance string class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>::ClassMethod1331<object>()
		ldstr "G1_C7::ClassMethod1331.4820<System.Object>()"
		ldstr "class G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>
		callvirt instance string class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>::ClassMethod1330<object>()
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>
		callvirt instance string class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>::ClassMethod1329()
		ldstr "G1_C7::ClassMethod1329.4818()"
		ldstr "class G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>
		callvirt instance string class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>::ClassMethod1328()
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0>
		callvirt instance string class Generated879.G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0>::ClassMethod1331<object>()
		ldstr "G1_C7::ClassMethod1331.4820<System.Object>()"
		ldstr "class G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0>
		callvirt instance string class Generated879.G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0>::ClassMethod1330<object>()
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0>
		callvirt instance string class Generated879.G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0>::ClassMethod1329()
		ldstr "G1_C7::ClassMethod1329.4818()"
		ldstr "class G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0>
		callvirt instance string class Generated879.G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0>::ClassMethod1328()
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0>
		callvirt instance string class Generated879.G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0>::Method7<object>()
		ldstr "G2_C384::Method7.8394<System.Object>()"
		ldstr "class G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0>
		callvirt instance string class Generated879.G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0>::Method6<object>()
		ldstr "G1_C7::Method6.4813<System.Object>()"
		ldstr "class G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0>
		callvirt instance string class Generated879.G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0>::Method5()
		ldstr "G2_C384::Method5.8390()"
		ldstr "class G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0>
		callvirt instance string class Generated879.G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0>::Method4()
		ldstr "G1_C7::Method4.4811()"
		ldstr "class G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated879.IBase1`1<class Generated879.BaseClass0>::Method4()
		ldstr "G2_C384::Method4.8389()"
		ldstr "class IBase1`1<class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated879.IBase1`1<class Generated879.BaseClass0>::Method5()
		ldstr "G2_C384::Method5.MI.8391()"
		ldstr "class IBase1`1<class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated879.IBase1`1<class Generated879.BaseClass0>::Method6<object>()
		ldstr "G2_C384::Method6.MI.8393<System.Object>()"
		ldstr "class IBase1`1<class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated879.IBase2`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::Method7<object>()
		ldstr "G1_C7::Method7.MI.4816<System.Object>()"
		ldstr "class IBase2`2<class Generated879.BaseClass1,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated879.IBase2`2<class Generated879.BaseClass0,class Generated879.BaseClass0>::Method7<object>()
		ldstr "G1_C7::Method7.MI.4816<System.Object>()"
		ldstr "class IBase2`2<class Generated879.BaseClass0,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated879.IBase2`2<class Generated879.BaseClass0,class Generated879.BaseClass1>::Method7<object>()
		ldstr "G2_C384::Method7.MI.8395<System.Object>()"
		ldstr "class IBase2`2<class Generated879.BaseClass0,class Generated879.BaseClass1> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated879.IBase2`2<class Generated879.BaseClass1,class Generated879.BaseClass1>::Method7<object>()
		ldstr "G1_C7::Method7.MI.4816<System.Object>()"
		ldstr "class IBase2`2<class Generated879.BaseClass1,class Generated879.BaseClass1> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>
		callvirt instance string class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::ClassMethod2115<object>()
		ldstr "G2_C384::ClassMethod2115.8397<System.Object>()"
		ldstr "class G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>
		callvirt instance string class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::ClassMethod2114<object>()
		ldstr "G2_C384::ClassMethod2114.8396<System.Object>()"
		ldstr "class G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>
		callvirt instance string class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::Method7<object>()
		ldstr "G2_C384::Method7.8394<System.Object>()"
		ldstr "class G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>
		callvirt instance string class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::Method6<object>()
		ldstr "G2_C384::Method6.8392<System.Object>()"
		ldstr "class G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>
		callvirt instance string class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::Method5()
		ldstr "G2_C384::Method5.8390()"
		ldstr "class G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>
		callvirt instance string class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::Method4()
		ldstr "G2_C384::Method4.8389()"
		ldstr "class G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>
		callvirt instance string class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::ClassMethod1331<object>()
		ldstr "G1_C7::ClassMethod1331.4820<System.Object>()"
		ldstr "class G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>
		callvirt instance string class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::ClassMethod1330<object>()
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>
		callvirt instance string class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::ClassMethod1329()
		ldstr "G1_C7::ClassMethod1329.4818()"
		ldstr "class G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>
		callvirt instance string class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::ClassMethod1328()
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated879.IBase1`1<class Generated879.BaseClass1>::Method4()
		ldstr "G2_C384::Method4.8389()"
		ldstr "class IBase1`1<class Generated879.BaseClass1> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated879.IBase1`1<class Generated879.BaseClass1>::Method5()
		ldstr "G2_C384::Method5.MI.8391()"
		ldstr "class IBase1`1<class Generated879.BaseClass1> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated879.IBase1`1<class Generated879.BaseClass1>::Method6<object>()
		ldstr "G2_C384::Method6.MI.8393<System.Object>()"
		ldstr "class IBase1`1<class Generated879.BaseClass1> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>
		callvirt instance string class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::ClassMethod1331<object>()
		ldstr "G1_C7::ClassMethod1331.4820<System.Object>()"
		ldstr "class G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>
		callvirt instance string class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::ClassMethod1330<object>()
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>
		callvirt instance string class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::ClassMethod1329()
		ldstr "G1_C7::ClassMethod1329.4818()"
		ldstr "class G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>
		callvirt instance string class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::ClassMethod1328()
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>
		callvirt instance string class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::Method7<object>()
		ldstr "G2_C384::Method7.8394<System.Object>()"
		ldstr "class G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>
		callvirt instance string class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::Method6<object>()
		ldstr "G1_C7::Method6.4813<System.Object>()"
		ldstr "class G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>
		callvirt instance string class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::Method5()
		ldstr "G2_C384::Method5.8390()"
		ldstr "class G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>
		callvirt instance string class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::Method4()
		ldstr "G1_C7::Method4.4811()"
		ldstr "class G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated879.IBase1`1<class Generated879.BaseClass1>::Method4()
		ldstr "G2_C384::Method4.8389()"
		ldstr "class IBase1`1<class Generated879.BaseClass1> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated879.IBase1`1<class Generated879.BaseClass1>::Method5()
		ldstr "G2_C384::Method5.MI.8391()"
		ldstr "class IBase1`1<class Generated879.BaseClass1> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated879.IBase1`1<class Generated879.BaseClass1>::Method6<object>()
		ldstr "G2_C384::Method6.MI.8393<System.Object>()"
		ldstr "class IBase1`1<class Generated879.BaseClass1> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated879.IBase2`2<class Generated879.BaseClass1,class Generated879.BaseClass1>::Method7<object>()
		ldstr "G2_C384::Method7.MI.8395<System.Object>()"
		ldstr "class IBase2`2<class Generated879.BaseClass1,class Generated879.BaseClass1> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated879.IBase1`1<class Generated879.BaseClass0>::Method4()
		ldstr "G2_C384::Method4.8389()"
		ldstr "class IBase1`1<class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated879.IBase1`1<class Generated879.BaseClass0>::Method5()
		ldstr "G2_C384::Method5.MI.8391()"
		ldstr "class IBase1`1<class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated879.IBase1`1<class Generated879.BaseClass0>::Method6<object>()
		ldstr "G2_C384::Method6.MI.8393<System.Object>()"
		ldstr "class IBase1`1<class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated879.IBase2`2<class Generated879.BaseClass0,class Generated879.BaseClass1>::Method7<object>()
		ldstr "G2_C384::Method7.MI.8395<System.Object>()"
		ldstr "class IBase2`2<class Generated879.BaseClass0,class Generated879.BaseClass1> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>
		callvirt instance string class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>::ClassMethod2115<object>()
		ldstr "G2_C384::ClassMethod2115.8397<System.Object>()"
		ldstr "class G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>
		callvirt instance string class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>::ClassMethod2114<object>()
		ldstr "G2_C384::ClassMethod2114.8396<System.Object>()"
		ldstr "class G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>
		callvirt instance string class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>::Method7<object>()
		ldstr "G2_C384::Method7.8394<System.Object>()"
		ldstr "class G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>
		callvirt instance string class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>::Method6<object>()
		ldstr "G2_C384::Method6.8392<System.Object>()"
		ldstr "class G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>
		callvirt instance string class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>::Method5()
		ldstr "G2_C384::Method5.8390()"
		ldstr "class G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>
		callvirt instance string class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>::Method4()
		ldstr "G2_C384::Method4.8389()"
		ldstr "class G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>
		callvirt instance string class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>::ClassMethod1331<object>()
		ldstr "G1_C7::ClassMethod1331.4820<System.Object>()"
		ldstr "class G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>
		callvirt instance string class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>::ClassMethod1330<object>()
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>
		callvirt instance string class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>::ClassMethod1329()
		ldstr "G1_C7::ClassMethod1329.4818()"
		ldstr "class G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>
		callvirt instance string class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>::ClassMethod1328()
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>"
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
		newobj instance void class Generated879.G3_C1351`1<class Generated879.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G1_C7::ClassMethod1329.4818()#G1_C7::ClassMethod1330.4819<System.Object>()#G1_C7::ClassMethod1331.4820<System.Object>()#G1_C7::Method4.4811()#G2_C384::Method5.8390()#G1_C7::Method6.4813<System.Object>()#G2_C384::Method7.8394<System.Object>()#"
		call void Generated879.Generated879::M.G1_C7.T.T<class Generated879.BaseClass1,class Generated879.BaseClass0,class Generated879.G3_C1351`1<class Generated879.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G1_C7::ClassMethod1329.4818()#G1_C7::ClassMethod1330.4819<System.Object>()#G1_C7::ClassMethod1331.4820<System.Object>()#G1_C7::Method4.4811()#G2_C384::Method5.8390()#G1_C7::Method6.4813<System.Object>()#G2_C384::Method7.8394<System.Object>()#"
		call void Generated879.Generated879::M.G1_C7.B.T<class Generated879.BaseClass0,class Generated879.G3_C1351`1<class Generated879.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G1_C7::ClassMethod1329.4818()#G1_C7::ClassMethod1330.4819<System.Object>()#G1_C7::ClassMethod1331.4820<System.Object>()#G1_C7::Method4.4811()#G2_C384::Method5.8390()#G1_C7::Method6.4813<System.Object>()#G2_C384::Method7.8394<System.Object>()#"
		call void Generated879.Generated879::M.G1_C7.B.A<class Generated879.G3_C1351`1<class Generated879.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::Method4.4811()#G2_C384::Method5.8390()#G1_C7::Method6.MI.4814<System.Object>()#"
		call void Generated879.Generated879::M.IBase1.T<class Generated879.BaseClass1,class Generated879.G3_C1351`1<class Generated879.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::Method4.4811()#G2_C384::Method5.8390()#G1_C7::Method6.MI.4814<System.Object>()#"
		call void Generated879.Generated879::M.IBase1.B<class Generated879.G3_C1351`1<class Generated879.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1351::Method7.MI.15725<System.Object>()#"
		call void Generated879.Generated879::M.IBase2.T.T<class Generated879.BaseClass1,class Generated879.BaseClass1,class Generated879.G3_C1351`1<class Generated879.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1351::Method7.MI.15725<System.Object>()#"
		call void Generated879.Generated879::M.IBase2.B.T<class Generated879.BaseClass1,class Generated879.G3_C1351`1<class Generated879.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1351::Method7.MI.15725<System.Object>()#"
		call void Generated879.Generated879::M.IBase2.B.B<class Generated879.G3_C1351`1<class Generated879.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C384::Method4.8389()#G2_C384::Method5.MI.8391()#G2_C384::Method6.MI.8393<System.Object>()#"
		call void Generated879.Generated879::M.IBase1.T<class Generated879.BaseClass0,class Generated879.G3_C1351`1<class Generated879.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C384::Method4.8389()#G2_C384::Method5.MI.8391()#G2_C384::Method6.MI.8393<System.Object>()#"
		call void Generated879.Generated879::M.IBase1.A<class Generated879.G3_C1351`1<class Generated879.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1351::Method7.MI.15725<System.Object>()#"
		call void Generated879.Generated879::M.IBase2.T.T<class Generated879.BaseClass0,class Generated879.BaseClass1,class Generated879.G3_C1351`1<class Generated879.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1351::Method7.MI.15725<System.Object>()#"
		call void Generated879.Generated879::M.IBase2.A.T<class Generated879.BaseClass1,class Generated879.G3_C1351`1<class Generated879.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1351::Method7.MI.15725<System.Object>()#"
		call void Generated879.Generated879::M.IBase2.A.B<class Generated879.G3_C1351`1<class Generated879.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G1_C7::ClassMethod1329.4818()#G1_C7::ClassMethod1330.4819<System.Object>()#G1_C7::ClassMethod1331.4820<System.Object>()#G2_C384::ClassMethod2114.8396<System.Object>()#G2_C384::ClassMethod2115.8397<System.Object>()#G2_C384::Method4.8389()#G2_C384::Method5.8390()#G2_C384::Method6.8392<System.Object>()#G2_C384::Method7.8394<System.Object>()#"
		call void Generated879.Generated879::M.G2_C384.T.T<class Generated879.BaseClass0,class Generated879.BaseClass1,class Generated879.G3_C1351`1<class Generated879.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G1_C7::ClassMethod1329.4818()#G1_C7::ClassMethod1330.4819<System.Object>()#G1_C7::ClassMethod1331.4820<System.Object>()#G2_C384::ClassMethod2114.8396<System.Object>()#G2_C384::ClassMethod2115.8397<System.Object>()#G2_C384::Method4.8389()#G2_C384::Method5.8390()#G2_C384::Method6.8392<System.Object>()#G2_C384::Method7.8394<System.Object>()#"
		call void Generated879.Generated879::M.G2_C384.A.T<class Generated879.BaseClass1,class Generated879.G3_C1351`1<class Generated879.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G1_C7::ClassMethod1329.4818()#G1_C7::ClassMethod1330.4819<System.Object>()#G1_C7::ClassMethod1331.4820<System.Object>()#G2_C384::ClassMethod2114.8396<System.Object>()#G2_C384::ClassMethod2115.8397<System.Object>()#G2_C384::Method4.8389()#G2_C384::Method5.8390()#G2_C384::Method6.8392<System.Object>()#G2_C384::Method7.8394<System.Object>()#"
		call void Generated879.Generated879::M.G2_C384.A.B<class Generated879.G3_C1351`1<class Generated879.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G1_C7::ClassMethod1329.4818()#G1_C7::ClassMethod1330.4819<System.Object>()#G1_C7::ClassMethod1331.4820<System.Object>()#G2_C384::ClassMethod2114.8396<System.Object>()#G2_C384::ClassMethod2115.8397<System.Object>()#G3_C1351::ClassMethod4096.15726()#G3_C1351::ClassMethod4097.15727()#G3_C1351::ClassMethod4098.15728<System.Object>()#G3_C1351::ClassMethod4099.15729<System.Object>()#G2_C384::Method4.8389()#G2_C384::Method5.8390()#G2_C384::Method6.8392<System.Object>()#G3_C1351::Method7.15724<System.Object>()#"
		call void Generated879.Generated879::M.G3_C1351.T<class Generated879.BaseClass0,class Generated879.G3_C1351`1<class Generated879.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G1_C7::ClassMethod1329.4818()#G1_C7::ClassMethod1330.4819<System.Object>()#G1_C7::ClassMethod1331.4820<System.Object>()#G2_C384::ClassMethod2114.8396<System.Object>()#G2_C384::ClassMethod2115.8397<System.Object>()#G3_C1351::ClassMethod4096.15726()#G3_C1351::ClassMethod4097.15727()#G3_C1351::ClassMethod4098.15728<System.Object>()#G3_C1351::ClassMethod4099.15729<System.Object>()#G2_C384::Method4.8389()#G2_C384::Method5.8390()#G2_C384::Method6.8392<System.Object>()#G3_C1351::Method7.15724<System.Object>()#"
		call void Generated879.Generated879::M.G3_C1351.A<class Generated879.G3_C1351`1<class Generated879.BaseClass0>>(!!0,string)
		newobj instance void class Generated879.G3_C1351`1<class Generated879.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G1_C7::ClassMethod1329.4818()#G1_C7::ClassMethod1330.4819<System.Object>()#G1_C7::ClassMethod1331.4820<System.Object>()#G1_C7::Method4.4811()#G2_C384::Method5.8390()#G1_C7::Method6.4813<System.Object>()#G2_C384::Method7.8394<System.Object>()#"
		call void Generated879.Generated879::M.G1_C7.T.T<class Generated879.BaseClass1,class Generated879.BaseClass0,class Generated879.G3_C1351`1<class Generated879.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G1_C7::ClassMethod1329.4818()#G1_C7::ClassMethod1330.4819<System.Object>()#G1_C7::ClassMethod1331.4820<System.Object>()#G1_C7::Method4.4811()#G2_C384::Method5.8390()#G1_C7::Method6.4813<System.Object>()#G2_C384::Method7.8394<System.Object>()#"
		call void Generated879.Generated879::M.G1_C7.B.T<class Generated879.BaseClass0,class Generated879.G3_C1351`1<class Generated879.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G1_C7::ClassMethod1329.4818()#G1_C7::ClassMethod1330.4819<System.Object>()#G1_C7::ClassMethod1331.4820<System.Object>()#G1_C7::Method4.4811()#G2_C384::Method5.8390()#G1_C7::Method6.4813<System.Object>()#G2_C384::Method7.8394<System.Object>()#"
		call void Generated879.Generated879::M.G1_C7.B.A<class Generated879.G3_C1351`1<class Generated879.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C384::Method4.8389()#G2_C384::Method5.MI.8391()#G2_C384::Method6.MI.8393<System.Object>()#"
		call void Generated879.Generated879::M.IBase1.T<class Generated879.BaseClass1,class Generated879.G3_C1351`1<class Generated879.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C384::Method4.8389()#G2_C384::Method5.MI.8391()#G2_C384::Method6.MI.8393<System.Object>()#"
		call void Generated879.Generated879::M.IBase1.B<class Generated879.G3_C1351`1<class Generated879.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1351::Method7.MI.15725<System.Object>()#"
		call void Generated879.Generated879::M.IBase2.T.T<class Generated879.BaseClass1,class Generated879.BaseClass1,class Generated879.G3_C1351`1<class Generated879.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1351::Method7.MI.15725<System.Object>()#"
		call void Generated879.Generated879::M.IBase2.B.T<class Generated879.BaseClass1,class Generated879.G3_C1351`1<class Generated879.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1351::Method7.MI.15725<System.Object>()#"
		call void Generated879.Generated879::M.IBase2.B.B<class Generated879.G3_C1351`1<class Generated879.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C384::Method4.8389()#G2_C384::Method5.MI.8391()#G2_C384::Method6.MI.8393<System.Object>()#"
		call void Generated879.Generated879::M.IBase1.T<class Generated879.BaseClass0,class Generated879.G3_C1351`1<class Generated879.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C384::Method4.8389()#G2_C384::Method5.MI.8391()#G2_C384::Method6.MI.8393<System.Object>()#"
		call void Generated879.Generated879::M.IBase1.A<class Generated879.G3_C1351`1<class Generated879.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1351::Method7.MI.15725<System.Object>()#"
		call void Generated879.Generated879::M.IBase2.T.T<class Generated879.BaseClass0,class Generated879.BaseClass1,class Generated879.G3_C1351`1<class Generated879.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1351::Method7.MI.15725<System.Object>()#"
		call void Generated879.Generated879::M.IBase2.A.T<class Generated879.BaseClass1,class Generated879.G3_C1351`1<class Generated879.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1351::Method7.MI.15725<System.Object>()#"
		call void Generated879.Generated879::M.IBase2.A.B<class Generated879.G3_C1351`1<class Generated879.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G1_C7::ClassMethod1329.4818()#G1_C7::ClassMethod1330.4819<System.Object>()#G1_C7::ClassMethod1331.4820<System.Object>()#G2_C384::ClassMethod2114.8396<System.Object>()#G2_C384::ClassMethod2115.8397<System.Object>()#G2_C384::Method4.8389()#G2_C384::Method5.8390()#G2_C384::Method6.8392<System.Object>()#G2_C384::Method7.8394<System.Object>()#"
		call void Generated879.Generated879::M.G2_C384.T.T<class Generated879.BaseClass1,class Generated879.BaseClass1,class Generated879.G3_C1351`1<class Generated879.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G1_C7::ClassMethod1329.4818()#G1_C7::ClassMethod1330.4819<System.Object>()#G1_C7::ClassMethod1331.4820<System.Object>()#G2_C384::ClassMethod2114.8396<System.Object>()#G2_C384::ClassMethod2115.8397<System.Object>()#G2_C384::Method4.8389()#G2_C384::Method5.8390()#G2_C384::Method6.8392<System.Object>()#G2_C384::Method7.8394<System.Object>()#"
		call void Generated879.Generated879::M.G2_C384.B.T<class Generated879.BaseClass1,class Generated879.G3_C1351`1<class Generated879.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G1_C7::ClassMethod1329.4818()#G1_C7::ClassMethod1330.4819<System.Object>()#G1_C7::ClassMethod1331.4820<System.Object>()#G2_C384::ClassMethod2114.8396<System.Object>()#G2_C384::ClassMethod2115.8397<System.Object>()#G2_C384::Method4.8389()#G2_C384::Method5.8390()#G2_C384::Method6.8392<System.Object>()#G2_C384::Method7.8394<System.Object>()#"
		call void Generated879.Generated879::M.G2_C384.B.B<class Generated879.G3_C1351`1<class Generated879.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G1_C7::ClassMethod1329.4818()#G1_C7::ClassMethod1330.4819<System.Object>()#G1_C7::ClassMethod1331.4820<System.Object>()#G2_C384::ClassMethod2114.8396<System.Object>()#G2_C384::ClassMethod2115.8397<System.Object>()#G3_C1351::ClassMethod4096.15726()#G3_C1351::ClassMethod4097.15727()#G3_C1351::ClassMethod4098.15728<System.Object>()#G3_C1351::ClassMethod4099.15729<System.Object>()#G2_C384::Method4.8389()#G2_C384::Method5.8390()#G2_C384::Method6.8392<System.Object>()#G3_C1351::Method7.15724<System.Object>()#"
		call void Generated879.Generated879::M.G3_C1351.T<class Generated879.BaseClass1,class Generated879.G3_C1351`1<class Generated879.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G1_C7::ClassMethod1329.4818()#G1_C7::ClassMethod1330.4819<System.Object>()#G1_C7::ClassMethod1331.4820<System.Object>()#G2_C384::ClassMethod2114.8396<System.Object>()#G2_C384::ClassMethod2115.8397<System.Object>()#G3_C1351::ClassMethod4096.15726()#G3_C1351::ClassMethod4097.15727()#G3_C1351::ClassMethod4098.15728<System.Object>()#G3_C1351::ClassMethod4099.15729<System.Object>()#G2_C384::Method4.8389()#G2_C384::Method5.8390()#G2_C384::Method6.8392<System.Object>()#G3_C1351::Method7.15724<System.Object>()#"
		call void Generated879.Generated879::M.G3_C1351.B<class Generated879.G3_C1351`1<class Generated879.BaseClass1>>(!!0,string)
		newobj instance void class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G1_C7::ClassMethod1329.4818()#G1_C7::ClassMethod1330.4819<System.Object>()#G1_C7::ClassMethod1331.4820<System.Object>()#G1_C7::Method4.4811()#G2_C384::Method5.8390()#G1_C7::Method6.4813<System.Object>()#G2_C384::Method7.8394<System.Object>()#"
		call void Generated879.Generated879::M.G1_C7.T.T<class Generated879.BaseClass0,class Generated879.BaseClass0,class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G1_C7::ClassMethod1329.4818()#G1_C7::ClassMethod1330.4819<System.Object>()#G1_C7::ClassMethod1331.4820<System.Object>()#G1_C7::Method4.4811()#G2_C384::Method5.8390()#G1_C7::Method6.4813<System.Object>()#G2_C384::Method7.8394<System.Object>()#"
		call void Generated879.Generated879::M.G1_C7.A.T<class Generated879.BaseClass0,class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G1_C7::ClassMethod1329.4818()#G1_C7::ClassMethod1330.4819<System.Object>()#G1_C7::ClassMethod1331.4820<System.Object>()#G1_C7::Method4.4811()#G2_C384::Method5.8390()#G1_C7::Method6.4813<System.Object>()#G2_C384::Method7.8394<System.Object>()#"
		call void Generated879.Generated879::M.G1_C7.A.A<class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C384::Method4.8389()#G2_C384::Method5.MI.8391()#G2_C384::Method6.MI.8393<System.Object>()#"
		call void Generated879.Generated879::M.IBase1.T<class Generated879.BaseClass0,class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C384::Method4.8389()#G2_C384::Method5.MI.8391()#G2_C384::Method6.MI.8393<System.Object>()#"
		call void Generated879.Generated879::M.IBase1.A<class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated879.Generated879::M.IBase2.T.T<class Generated879.BaseClass1,class Generated879.BaseClass0,class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated879.Generated879::M.IBase2.B.T<class Generated879.BaseClass0,class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated879.Generated879::M.IBase2.B.A<class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated879.Generated879::M.IBase2.T.T<class Generated879.BaseClass0,class Generated879.BaseClass0,class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated879.Generated879::M.IBase2.A.T<class Generated879.BaseClass0,class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated879.Generated879::M.IBase2.A.A<class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C384::Method7.MI.8395<System.Object>()#"
		call void Generated879.Generated879::M.IBase2.T.T<class Generated879.BaseClass0,class Generated879.BaseClass1,class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C384::Method7.MI.8395<System.Object>()#"
		call void Generated879.Generated879::M.IBase2.A.T<class Generated879.BaseClass1,class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C384::Method7.MI.8395<System.Object>()#"
		call void Generated879.Generated879::M.IBase2.A.B<class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated879.Generated879::M.IBase2.T.T<class Generated879.BaseClass1,class Generated879.BaseClass1,class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated879.Generated879::M.IBase2.B.T<class Generated879.BaseClass1,class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated879.Generated879::M.IBase2.B.B<class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G1_C7::ClassMethod1329.4818()#G1_C7::ClassMethod1330.4819<System.Object>()#G1_C7::ClassMethod1331.4820<System.Object>()#G2_C384::ClassMethod2114.8396<System.Object>()#G2_C384::ClassMethod2115.8397<System.Object>()#G2_C384::Method4.8389()#G2_C384::Method5.8390()#G2_C384::Method6.8392<System.Object>()#G2_C384::Method7.8394<System.Object>()#"
		call void Generated879.Generated879::M.G2_C384.T.T<class Generated879.BaseClass0,class Generated879.BaseClass0,class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G1_C7::ClassMethod1329.4818()#G1_C7::ClassMethod1330.4819<System.Object>()#G1_C7::ClassMethod1331.4820<System.Object>()#G2_C384::ClassMethod2114.8396<System.Object>()#G2_C384::ClassMethod2115.8397<System.Object>()#G2_C384::Method4.8389()#G2_C384::Method5.8390()#G2_C384::Method6.8392<System.Object>()#G2_C384::Method7.8394<System.Object>()#"
		call void Generated879.Generated879::M.G2_C384.A.T<class Generated879.BaseClass0,class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G1_C7::ClassMethod1329.4818()#G1_C7::ClassMethod1330.4819<System.Object>()#G1_C7::ClassMethod1331.4820<System.Object>()#G2_C384::ClassMethod2114.8396<System.Object>()#G2_C384::ClassMethod2115.8397<System.Object>()#G2_C384::Method4.8389()#G2_C384::Method5.8390()#G2_C384::Method6.8392<System.Object>()#G2_C384::Method7.8394<System.Object>()#"
		call void Generated879.Generated879::M.G2_C384.A.A<class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>>(!!0,string)
		newobj instance void class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G1_C7::ClassMethod1329.4818()#G1_C7::ClassMethod1330.4819<System.Object>()#G1_C7::ClassMethod1331.4820<System.Object>()#G1_C7::Method4.4811()#G2_C384::Method5.8390()#G1_C7::Method6.4813<System.Object>()#G2_C384::Method7.8394<System.Object>()#"
		call void Generated879.Generated879::M.G1_C7.T.T<class Generated879.BaseClass1,class Generated879.BaseClass0,class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G1_C7::ClassMethod1329.4818()#G1_C7::ClassMethod1330.4819<System.Object>()#G1_C7::ClassMethod1331.4820<System.Object>()#G1_C7::Method4.4811()#G2_C384::Method5.8390()#G1_C7::Method6.4813<System.Object>()#G2_C384::Method7.8394<System.Object>()#"
		call void Generated879.Generated879::M.G1_C7.B.T<class Generated879.BaseClass0,class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G1_C7::ClassMethod1329.4818()#G1_C7::ClassMethod1330.4819<System.Object>()#G1_C7::ClassMethod1331.4820<System.Object>()#G1_C7::Method4.4811()#G2_C384::Method5.8390()#G1_C7::Method6.4813<System.Object>()#G2_C384::Method7.8394<System.Object>()#"
		call void Generated879.Generated879::M.G1_C7.B.A<class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::Method4.4811()#G2_C384::Method5.8390()#G1_C7::Method6.MI.4814<System.Object>()#"
		call void Generated879.Generated879::M.IBase1.T<class Generated879.BaseClass1,class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::Method4.4811()#G2_C384::Method5.8390()#G1_C7::Method6.MI.4814<System.Object>()#"
		call void Generated879.Generated879::M.IBase1.B<class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C384::Method7.MI.8395<System.Object>()#"
		call void Generated879.Generated879::M.IBase2.T.T<class Generated879.BaseClass1,class Generated879.BaseClass1,class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C384::Method7.MI.8395<System.Object>()#"
		call void Generated879.Generated879::M.IBase2.B.T<class Generated879.BaseClass1,class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C384::Method7.MI.8395<System.Object>()#"
		call void Generated879.Generated879::M.IBase2.B.B<class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C384::Method4.8389()#G2_C384::Method5.MI.8391()#G2_C384::Method6.MI.8393<System.Object>()#"
		call void Generated879.Generated879::M.IBase1.T<class Generated879.BaseClass0,class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C384::Method4.8389()#G2_C384::Method5.MI.8391()#G2_C384::Method6.MI.8393<System.Object>()#"
		call void Generated879.Generated879::M.IBase1.A<class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C384::Method7.MI.8395<System.Object>()#"
		call void Generated879.Generated879::M.IBase2.T.T<class Generated879.BaseClass0,class Generated879.BaseClass1,class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C384::Method7.MI.8395<System.Object>()#"
		call void Generated879.Generated879::M.IBase2.A.T<class Generated879.BaseClass1,class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C384::Method7.MI.8395<System.Object>()#"
		call void Generated879.Generated879::M.IBase2.A.B<class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G1_C7::ClassMethod1329.4818()#G1_C7::ClassMethod1330.4819<System.Object>()#G1_C7::ClassMethod1331.4820<System.Object>()#G2_C384::ClassMethod2114.8396<System.Object>()#G2_C384::ClassMethod2115.8397<System.Object>()#G2_C384::Method4.8389()#G2_C384::Method5.8390()#G2_C384::Method6.8392<System.Object>()#G2_C384::Method7.8394<System.Object>()#"
		call void Generated879.Generated879::M.G2_C384.T.T<class Generated879.BaseClass0,class Generated879.BaseClass1,class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G1_C7::ClassMethod1329.4818()#G1_C7::ClassMethod1330.4819<System.Object>()#G1_C7::ClassMethod1331.4820<System.Object>()#G2_C384::ClassMethod2114.8396<System.Object>()#G2_C384::ClassMethod2115.8397<System.Object>()#G2_C384::Method4.8389()#G2_C384::Method5.8390()#G2_C384::Method6.8392<System.Object>()#G2_C384::Method7.8394<System.Object>()#"
		call void Generated879.Generated879::M.G2_C384.A.T<class Generated879.BaseClass1,class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G1_C7::ClassMethod1329.4818()#G1_C7::ClassMethod1330.4819<System.Object>()#G1_C7::ClassMethod1331.4820<System.Object>()#G2_C384::ClassMethod2114.8396<System.Object>()#G2_C384::ClassMethod2115.8397<System.Object>()#G2_C384::Method4.8389()#G2_C384::Method5.8390()#G2_C384::Method6.8392<System.Object>()#G2_C384::Method7.8394<System.Object>()#"
		call void Generated879.Generated879::M.G2_C384.A.B<class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>>(!!0,string)
		newobj instance void class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G1_C7::ClassMethod1329.4818()#G1_C7::ClassMethod1330.4819<System.Object>()#G1_C7::ClassMethod1331.4820<System.Object>()#G1_C7::Method4.4811()#G2_C384::Method5.8390()#G1_C7::Method6.4813<System.Object>()#G2_C384::Method7.8394<System.Object>()#"
		call void Generated879.Generated879::M.G1_C7.T.T<class Generated879.BaseClass0,class Generated879.BaseClass0,class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G1_C7::ClassMethod1329.4818()#G1_C7::ClassMethod1330.4819<System.Object>()#G1_C7::ClassMethod1331.4820<System.Object>()#G1_C7::Method4.4811()#G2_C384::Method5.8390()#G1_C7::Method6.4813<System.Object>()#G2_C384::Method7.8394<System.Object>()#"
		call void Generated879.Generated879::M.G1_C7.A.T<class Generated879.BaseClass0,class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G1_C7::ClassMethod1329.4818()#G1_C7::ClassMethod1330.4819<System.Object>()#G1_C7::ClassMethod1331.4820<System.Object>()#G1_C7::Method4.4811()#G2_C384::Method5.8390()#G1_C7::Method6.4813<System.Object>()#G2_C384::Method7.8394<System.Object>()#"
		call void Generated879.Generated879::M.G1_C7.A.A<class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C384::Method4.8389()#G2_C384::Method5.MI.8391()#G2_C384::Method6.MI.8393<System.Object>()#"
		call void Generated879.Generated879::M.IBase1.T<class Generated879.BaseClass0,class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C384::Method4.8389()#G2_C384::Method5.MI.8391()#G2_C384::Method6.MI.8393<System.Object>()#"
		call void Generated879.Generated879::M.IBase1.A<class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated879.Generated879::M.IBase2.T.T<class Generated879.BaseClass1,class Generated879.BaseClass0,class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated879.Generated879::M.IBase2.B.T<class Generated879.BaseClass0,class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated879.Generated879::M.IBase2.B.A<class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated879.Generated879::M.IBase2.T.T<class Generated879.BaseClass0,class Generated879.BaseClass0,class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated879.Generated879::M.IBase2.A.T<class Generated879.BaseClass0,class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated879.Generated879::M.IBase2.A.A<class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C384::Method7.MI.8395<System.Object>()#"
		call void Generated879.Generated879::M.IBase2.T.T<class Generated879.BaseClass0,class Generated879.BaseClass1,class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C384::Method7.MI.8395<System.Object>()#"
		call void Generated879.Generated879::M.IBase2.A.T<class Generated879.BaseClass1,class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C384::Method7.MI.8395<System.Object>()#"
		call void Generated879.Generated879::M.IBase2.A.B<class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated879.Generated879::M.IBase2.T.T<class Generated879.BaseClass1,class Generated879.BaseClass1,class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated879.Generated879::M.IBase2.B.T<class Generated879.BaseClass1,class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated879.Generated879::M.IBase2.B.B<class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G1_C7::ClassMethod1329.4818()#G1_C7::ClassMethod1330.4819<System.Object>()#G1_C7::ClassMethod1331.4820<System.Object>()#G2_C384::ClassMethod2114.8396<System.Object>()#G2_C384::ClassMethod2115.8397<System.Object>()#G2_C384::Method4.8389()#G2_C384::Method5.8390()#G2_C384::Method6.8392<System.Object>()#G2_C384::Method7.8394<System.Object>()#"
		call void Generated879.Generated879::M.G2_C384.T.T<class Generated879.BaseClass1,class Generated879.BaseClass0,class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G1_C7::ClassMethod1329.4818()#G1_C7::ClassMethod1330.4819<System.Object>()#G1_C7::ClassMethod1331.4820<System.Object>()#G2_C384::ClassMethod2114.8396<System.Object>()#G2_C384::ClassMethod2115.8397<System.Object>()#G2_C384::Method4.8389()#G2_C384::Method5.8390()#G2_C384::Method6.8392<System.Object>()#G2_C384::Method7.8394<System.Object>()#"
		call void Generated879.Generated879::M.G2_C384.B.T<class Generated879.BaseClass0,class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G1_C7::ClassMethod1329.4818()#G1_C7::ClassMethod1330.4819<System.Object>()#G1_C7::ClassMethod1331.4820<System.Object>()#G2_C384::ClassMethod2114.8396<System.Object>()#G2_C384::ClassMethod2115.8397<System.Object>()#G2_C384::Method4.8389()#G2_C384::Method5.8390()#G2_C384::Method6.8392<System.Object>()#G2_C384::Method7.8394<System.Object>()#"
		call void Generated879.Generated879::M.G2_C384.B.A<class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C384::Method4.8389()#G2_C384::Method5.MI.8391()#G2_C384::Method6.MI.8393<System.Object>()#"
		call void Generated879.Generated879::M.IBase1.T<class Generated879.BaseClass1,class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C384::Method4.8389()#G2_C384::Method5.MI.8391()#G2_C384::Method6.MI.8393<System.Object>()#"
		call void Generated879.Generated879::M.IBase1.B<class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>>(!!0,string)
		newobj instance void class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G1_C7::ClassMethod1329.4818()#G1_C7::ClassMethod1330.4819<System.Object>()#G1_C7::ClassMethod1331.4820<System.Object>()#G1_C7::Method4.4811()#G2_C384::Method5.8390()#G1_C7::Method6.4813<System.Object>()#G2_C384::Method7.8394<System.Object>()#"
		call void Generated879.Generated879::M.G1_C7.T.T<class Generated879.BaseClass1,class Generated879.BaseClass0,class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G1_C7::ClassMethod1329.4818()#G1_C7::ClassMethod1330.4819<System.Object>()#G1_C7::ClassMethod1331.4820<System.Object>()#G1_C7::Method4.4811()#G2_C384::Method5.8390()#G1_C7::Method6.4813<System.Object>()#G2_C384::Method7.8394<System.Object>()#"
		call void Generated879.Generated879::M.G1_C7.B.T<class Generated879.BaseClass0,class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G1_C7::ClassMethod1329.4818()#G1_C7::ClassMethod1330.4819<System.Object>()#G1_C7::ClassMethod1331.4820<System.Object>()#G1_C7::Method4.4811()#G2_C384::Method5.8390()#G1_C7::Method6.4813<System.Object>()#G2_C384::Method7.8394<System.Object>()#"
		call void Generated879.Generated879::M.G1_C7.B.A<class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C384::Method4.8389()#G2_C384::Method5.MI.8391()#G2_C384::Method6.MI.8393<System.Object>()#"
		call void Generated879.Generated879::M.IBase1.T<class Generated879.BaseClass1,class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C384::Method4.8389()#G2_C384::Method5.MI.8391()#G2_C384::Method6.MI.8393<System.Object>()#"
		call void Generated879.Generated879::M.IBase1.B<class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C384::Method7.MI.8395<System.Object>()#"
		call void Generated879.Generated879::M.IBase2.T.T<class Generated879.BaseClass1,class Generated879.BaseClass1,class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C384::Method7.MI.8395<System.Object>()#"
		call void Generated879.Generated879::M.IBase2.B.T<class Generated879.BaseClass1,class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C384::Method7.MI.8395<System.Object>()#"
		call void Generated879.Generated879::M.IBase2.B.B<class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C384::Method4.8389()#G2_C384::Method5.MI.8391()#G2_C384::Method6.MI.8393<System.Object>()#"
		call void Generated879.Generated879::M.IBase1.T<class Generated879.BaseClass0,class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C384::Method4.8389()#G2_C384::Method5.MI.8391()#G2_C384::Method6.MI.8393<System.Object>()#"
		call void Generated879.Generated879::M.IBase1.A<class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C384::Method7.MI.8395<System.Object>()#"
		call void Generated879.Generated879::M.IBase2.T.T<class Generated879.BaseClass0,class Generated879.BaseClass1,class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C384::Method7.MI.8395<System.Object>()#"
		call void Generated879.Generated879::M.IBase2.A.T<class Generated879.BaseClass1,class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C384::Method7.MI.8395<System.Object>()#"
		call void Generated879.Generated879::M.IBase2.A.B<class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G1_C7::ClassMethod1329.4818()#G1_C7::ClassMethod1330.4819<System.Object>()#G1_C7::ClassMethod1331.4820<System.Object>()#G2_C384::ClassMethod2114.8396<System.Object>()#G2_C384::ClassMethod2115.8397<System.Object>()#G2_C384::Method4.8389()#G2_C384::Method5.8390()#G2_C384::Method6.8392<System.Object>()#G2_C384::Method7.8394<System.Object>()#"
		call void Generated879.Generated879::M.G2_C384.T.T<class Generated879.BaseClass1,class Generated879.BaseClass1,class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G1_C7::ClassMethod1329.4818()#G1_C7::ClassMethod1330.4819<System.Object>()#G1_C7::ClassMethod1331.4820<System.Object>()#G2_C384::ClassMethod2114.8396<System.Object>()#G2_C384::ClassMethod2115.8397<System.Object>()#G2_C384::Method4.8389()#G2_C384::Method5.8390()#G2_C384::Method6.8392<System.Object>()#G2_C384::Method7.8394<System.Object>()#"
		call void Generated879.Generated879::M.G2_C384.B.T<class Generated879.BaseClass1,class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G1_C7::ClassMethod1329.4818()#G1_C7::ClassMethod1330.4819<System.Object>()#G1_C7::ClassMethod1331.4820<System.Object>()#G2_C384::ClassMethod2114.8396<System.Object>()#G2_C384::ClassMethod2115.8397<System.Object>()#G2_C384::Method4.8389()#G2_C384::Method5.8390()#G2_C384::Method6.8392<System.Object>()#G2_C384::Method7.8394<System.Object>()#"
		call void Generated879.Generated879::M.G2_C384.B.B<class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>>(!!0,string)
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
		newobj instance void class Generated879.G3_C1351`1<class Generated879.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::ClassMethod1331<object>()
		calli default string(class Generated879.G3_C1351`1<class Generated879.BaseClass0>)
		ldstr "G1_C7::ClassMethod1331.4820<System.Object>()"
		ldstr "class G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0> on type class Generated879.G3_C1351`1<class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::ClassMethod1330<object>()
		calli default string(class Generated879.G3_C1351`1<class Generated879.BaseClass0>)
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0> on type class Generated879.G3_C1351`1<class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::ClassMethod1329()
		calli default string(class Generated879.G3_C1351`1<class Generated879.BaseClass0>)
		ldstr "G1_C7::ClassMethod1329.4818()"
		ldstr "class G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0> on type class Generated879.G3_C1351`1<class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::ClassMethod1328()
		calli default string(class Generated879.G3_C1351`1<class Generated879.BaseClass0>)
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0> on type class Generated879.G3_C1351`1<class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::Method7<object>()
		calli default string(class Generated879.G3_C1351`1<class Generated879.BaseClass0>)
		ldstr "G2_C384::Method7.8394<System.Object>()"
		ldstr "class G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0> on type class Generated879.G3_C1351`1<class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::Method6<object>()
		calli default string(class Generated879.G3_C1351`1<class Generated879.BaseClass0>)
		ldstr "G1_C7::Method6.4813<System.Object>()"
		ldstr "class G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0> on type class Generated879.G3_C1351`1<class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::Method5()
		calli default string(class Generated879.G3_C1351`1<class Generated879.BaseClass0>)
		ldstr "G2_C384::Method5.8390()"
		ldstr "class G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0> on type class Generated879.G3_C1351`1<class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::Method4()
		calli default string(class Generated879.G3_C1351`1<class Generated879.BaseClass0>)
		ldstr "G1_C7::Method4.4811()"
		ldstr "class G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0> on type class Generated879.G3_C1351`1<class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated879.IBase1`1<class Generated879.BaseClass1>::Method4()
		calli default string(class Generated879.G3_C1351`1<class Generated879.BaseClass0>)
		ldstr "G1_C7::Method4.4811()"
		ldstr "class IBase1`1<class Generated879.BaseClass1> on type class Generated879.G3_C1351`1<class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated879.IBase1`1<class Generated879.BaseClass1>::Method5()
		calli default string(class Generated879.G3_C1351`1<class Generated879.BaseClass0>)
		ldstr "G2_C384::Method5.8390()"
		ldstr "class IBase1`1<class Generated879.BaseClass1> on type class Generated879.G3_C1351`1<class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated879.IBase1`1<class Generated879.BaseClass1>::Method6<object>()
		calli default string(class Generated879.G3_C1351`1<class Generated879.BaseClass0>)
		ldstr "G1_C7::Method6.MI.4814<System.Object>()"
		ldstr "class IBase1`1<class Generated879.BaseClass1> on type class Generated879.G3_C1351`1<class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated879.IBase2`2<class Generated879.BaseClass1,class Generated879.BaseClass1>::Method7<object>()
		calli default string(class Generated879.G3_C1351`1<class Generated879.BaseClass0>)
		ldstr "G3_C1351::Method7.MI.15725<System.Object>()"
		ldstr "class IBase2`2<class Generated879.BaseClass1,class Generated879.BaseClass1> on type class Generated879.G3_C1351`1<class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated879.IBase1`1<class Generated879.BaseClass0>::Method4()
		calli default string(class Generated879.G3_C1351`1<class Generated879.BaseClass0>)
		ldstr "G2_C384::Method4.8389()"
		ldstr "class IBase1`1<class Generated879.BaseClass0> on type class Generated879.G3_C1351`1<class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated879.IBase1`1<class Generated879.BaseClass0>::Method5()
		calli default string(class Generated879.G3_C1351`1<class Generated879.BaseClass0>)
		ldstr "G2_C384::Method5.MI.8391()"
		ldstr "class IBase1`1<class Generated879.BaseClass0> on type class Generated879.G3_C1351`1<class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated879.IBase1`1<class Generated879.BaseClass0>::Method6<object>()
		calli default string(class Generated879.G3_C1351`1<class Generated879.BaseClass0>)
		ldstr "G2_C384::Method6.MI.8393<System.Object>()"
		ldstr "class IBase1`1<class Generated879.BaseClass0> on type class Generated879.G3_C1351`1<class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated879.IBase2`2<class Generated879.BaseClass0,class Generated879.BaseClass1>::Method7<object>()
		calli default string(class Generated879.G3_C1351`1<class Generated879.BaseClass0>)
		ldstr "G3_C1351::Method7.MI.15725<System.Object>()"
		ldstr "class IBase2`2<class Generated879.BaseClass0,class Generated879.BaseClass1> on type class Generated879.G3_C1351`1<class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>::ClassMethod2115<object>()
		calli default string(class Generated879.G3_C1351`1<class Generated879.BaseClass0>)
		ldstr "G2_C384::ClassMethod2115.8397<System.Object>()"
		ldstr "class G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1> on type class Generated879.G3_C1351`1<class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>::ClassMethod2114<object>()
		calli default string(class Generated879.G3_C1351`1<class Generated879.BaseClass0>)
		ldstr "G2_C384::ClassMethod2114.8396<System.Object>()"
		ldstr "class G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1> on type class Generated879.G3_C1351`1<class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>::Method7<object>()
		calli default string(class Generated879.G3_C1351`1<class Generated879.BaseClass0>)
		ldstr "G2_C384::Method7.8394<System.Object>()"
		ldstr "class G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1> on type class Generated879.G3_C1351`1<class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>::Method6<object>()
		calli default string(class Generated879.G3_C1351`1<class Generated879.BaseClass0>)
		ldstr "G2_C384::Method6.8392<System.Object>()"
		ldstr "class G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1> on type class Generated879.G3_C1351`1<class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>::Method5()
		calli default string(class Generated879.G3_C1351`1<class Generated879.BaseClass0>)
		ldstr "G2_C384::Method5.8390()"
		ldstr "class G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1> on type class Generated879.G3_C1351`1<class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>::Method4()
		calli default string(class Generated879.G3_C1351`1<class Generated879.BaseClass0>)
		ldstr "G2_C384::Method4.8389()"
		ldstr "class G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1> on type class Generated879.G3_C1351`1<class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>::ClassMethod1331<object>()
		calli default string(class Generated879.G3_C1351`1<class Generated879.BaseClass0>)
		ldstr "G1_C7::ClassMethod1331.4820<System.Object>()"
		ldstr "class G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1> on type class Generated879.G3_C1351`1<class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>::ClassMethod1330<object>()
		calli default string(class Generated879.G3_C1351`1<class Generated879.BaseClass0>)
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1> on type class Generated879.G3_C1351`1<class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>::ClassMethod1329()
		calli default string(class Generated879.G3_C1351`1<class Generated879.BaseClass0>)
		ldstr "G1_C7::ClassMethod1329.4818()"
		ldstr "class G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1> on type class Generated879.G3_C1351`1<class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>::ClassMethod1328()
		calli default string(class Generated879.G3_C1351`1<class Generated879.BaseClass0>)
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1> on type class Generated879.G3_C1351`1<class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G3_C1351`1<class Generated879.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated879.G3_C1351`1<class Generated879.BaseClass0>::ClassMethod4099<object>()
		calli default string(class Generated879.G3_C1351`1<class Generated879.BaseClass0>)
		ldstr "G3_C1351::ClassMethod4099.15729<System.Object>()"
		ldstr "class G3_C1351`1<class Generated879.BaseClass0> on type class Generated879.G3_C1351`1<class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G3_C1351`1<class Generated879.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated879.G3_C1351`1<class Generated879.BaseClass0>::ClassMethod4098<object>()
		calli default string(class Generated879.G3_C1351`1<class Generated879.BaseClass0>)
		ldstr "G3_C1351::ClassMethod4098.15728<System.Object>()"
		ldstr "class G3_C1351`1<class Generated879.BaseClass0> on type class Generated879.G3_C1351`1<class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G3_C1351`1<class Generated879.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated879.G3_C1351`1<class Generated879.BaseClass0>::ClassMethod4097()
		calli default string(class Generated879.G3_C1351`1<class Generated879.BaseClass0>)
		ldstr "G3_C1351::ClassMethod4097.15727()"
		ldstr "class G3_C1351`1<class Generated879.BaseClass0> on type class Generated879.G3_C1351`1<class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G3_C1351`1<class Generated879.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated879.G3_C1351`1<class Generated879.BaseClass0>::ClassMethod4096()
		calli default string(class Generated879.G3_C1351`1<class Generated879.BaseClass0>)
		ldstr "G3_C1351::ClassMethod4096.15726()"
		ldstr "class G3_C1351`1<class Generated879.BaseClass0> on type class Generated879.G3_C1351`1<class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G3_C1351`1<class Generated879.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated879.G3_C1351`1<class Generated879.BaseClass0>::Method7<object>()
		calli default string(class Generated879.G3_C1351`1<class Generated879.BaseClass0>)
		ldstr "G3_C1351::Method7.15724<System.Object>()"
		ldstr "class G3_C1351`1<class Generated879.BaseClass0> on type class Generated879.G3_C1351`1<class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G3_C1351`1<class Generated879.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated879.G3_C1351`1<class Generated879.BaseClass0>::ClassMethod2115<object>()
		calli default string(class Generated879.G3_C1351`1<class Generated879.BaseClass0>)
		ldstr "G2_C384::ClassMethod2115.8397<System.Object>()"
		ldstr "class G3_C1351`1<class Generated879.BaseClass0> on type class Generated879.G3_C1351`1<class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G3_C1351`1<class Generated879.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated879.G3_C1351`1<class Generated879.BaseClass0>::ClassMethod2114<object>()
		calli default string(class Generated879.G3_C1351`1<class Generated879.BaseClass0>)
		ldstr "G2_C384::ClassMethod2114.8396<System.Object>()"
		ldstr "class G3_C1351`1<class Generated879.BaseClass0> on type class Generated879.G3_C1351`1<class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G3_C1351`1<class Generated879.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated879.G3_C1351`1<class Generated879.BaseClass0>::Method6<object>()
		calli default string(class Generated879.G3_C1351`1<class Generated879.BaseClass0>)
		ldstr "G2_C384::Method6.8392<System.Object>()"
		ldstr "class G3_C1351`1<class Generated879.BaseClass0> on type class Generated879.G3_C1351`1<class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G3_C1351`1<class Generated879.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated879.G3_C1351`1<class Generated879.BaseClass0>::Method5()
		calli default string(class Generated879.G3_C1351`1<class Generated879.BaseClass0>)
		ldstr "G2_C384::Method5.8390()"
		ldstr "class G3_C1351`1<class Generated879.BaseClass0> on type class Generated879.G3_C1351`1<class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G3_C1351`1<class Generated879.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated879.G3_C1351`1<class Generated879.BaseClass0>::Method4()
		calli default string(class Generated879.G3_C1351`1<class Generated879.BaseClass0>)
		ldstr "G2_C384::Method4.8389()"
		ldstr "class G3_C1351`1<class Generated879.BaseClass0> on type class Generated879.G3_C1351`1<class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G3_C1351`1<class Generated879.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated879.G3_C1351`1<class Generated879.BaseClass0>::ClassMethod1331<object>()
		calli default string(class Generated879.G3_C1351`1<class Generated879.BaseClass0>)
		ldstr "G1_C7::ClassMethod1331.4820<System.Object>()"
		ldstr "class G3_C1351`1<class Generated879.BaseClass0> on type class Generated879.G3_C1351`1<class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G3_C1351`1<class Generated879.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated879.G3_C1351`1<class Generated879.BaseClass0>::ClassMethod1330<object>()
		calli default string(class Generated879.G3_C1351`1<class Generated879.BaseClass0>)
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G3_C1351`1<class Generated879.BaseClass0> on type class Generated879.G3_C1351`1<class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G3_C1351`1<class Generated879.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated879.G3_C1351`1<class Generated879.BaseClass0>::ClassMethod1329()
		calli default string(class Generated879.G3_C1351`1<class Generated879.BaseClass0>)
		ldstr "G1_C7::ClassMethod1329.4818()"
		ldstr "class G3_C1351`1<class Generated879.BaseClass0> on type class Generated879.G3_C1351`1<class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G3_C1351`1<class Generated879.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated879.G3_C1351`1<class Generated879.BaseClass0>::ClassMethod1328()
		calli default string(class Generated879.G3_C1351`1<class Generated879.BaseClass0>)
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G3_C1351`1<class Generated879.BaseClass0> on type class Generated879.G3_C1351`1<class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated879.G3_C1351`1<class Generated879.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::ClassMethod1331<object>()
		calli default string(class Generated879.G3_C1351`1<class Generated879.BaseClass1>)
		ldstr "G1_C7::ClassMethod1331.4820<System.Object>()"
		ldstr "class G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0> on type class Generated879.G3_C1351`1<class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::ClassMethod1330<object>()
		calli default string(class Generated879.G3_C1351`1<class Generated879.BaseClass1>)
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0> on type class Generated879.G3_C1351`1<class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::ClassMethod1329()
		calli default string(class Generated879.G3_C1351`1<class Generated879.BaseClass1>)
		ldstr "G1_C7::ClassMethod1329.4818()"
		ldstr "class G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0> on type class Generated879.G3_C1351`1<class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::ClassMethod1328()
		calli default string(class Generated879.G3_C1351`1<class Generated879.BaseClass1>)
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0> on type class Generated879.G3_C1351`1<class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::Method7<object>()
		calli default string(class Generated879.G3_C1351`1<class Generated879.BaseClass1>)
		ldstr "G2_C384::Method7.8394<System.Object>()"
		ldstr "class G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0> on type class Generated879.G3_C1351`1<class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::Method6<object>()
		calli default string(class Generated879.G3_C1351`1<class Generated879.BaseClass1>)
		ldstr "G1_C7::Method6.4813<System.Object>()"
		ldstr "class G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0> on type class Generated879.G3_C1351`1<class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::Method5()
		calli default string(class Generated879.G3_C1351`1<class Generated879.BaseClass1>)
		ldstr "G2_C384::Method5.8390()"
		ldstr "class G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0> on type class Generated879.G3_C1351`1<class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::Method4()
		calli default string(class Generated879.G3_C1351`1<class Generated879.BaseClass1>)
		ldstr "G1_C7::Method4.4811()"
		ldstr "class G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0> on type class Generated879.G3_C1351`1<class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated879.IBase1`1<class Generated879.BaseClass1>::Method4()
		calli default string(class Generated879.G3_C1351`1<class Generated879.BaseClass1>)
		ldstr "G2_C384::Method4.8389()"
		ldstr "class IBase1`1<class Generated879.BaseClass1> on type class Generated879.G3_C1351`1<class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated879.IBase1`1<class Generated879.BaseClass1>::Method5()
		calli default string(class Generated879.G3_C1351`1<class Generated879.BaseClass1>)
		ldstr "G2_C384::Method5.MI.8391()"
		ldstr "class IBase1`1<class Generated879.BaseClass1> on type class Generated879.G3_C1351`1<class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated879.IBase1`1<class Generated879.BaseClass1>::Method6<object>()
		calli default string(class Generated879.G3_C1351`1<class Generated879.BaseClass1>)
		ldstr "G2_C384::Method6.MI.8393<System.Object>()"
		ldstr "class IBase1`1<class Generated879.BaseClass1> on type class Generated879.G3_C1351`1<class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated879.IBase2`2<class Generated879.BaseClass1,class Generated879.BaseClass1>::Method7<object>()
		calli default string(class Generated879.G3_C1351`1<class Generated879.BaseClass1>)
		ldstr "G3_C1351::Method7.MI.15725<System.Object>()"
		ldstr "class IBase2`2<class Generated879.BaseClass1,class Generated879.BaseClass1> on type class Generated879.G3_C1351`1<class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated879.IBase1`1<class Generated879.BaseClass0>::Method4()
		calli default string(class Generated879.G3_C1351`1<class Generated879.BaseClass1>)
		ldstr "G2_C384::Method4.8389()"
		ldstr "class IBase1`1<class Generated879.BaseClass0> on type class Generated879.G3_C1351`1<class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated879.IBase1`1<class Generated879.BaseClass0>::Method5()
		calli default string(class Generated879.G3_C1351`1<class Generated879.BaseClass1>)
		ldstr "G2_C384::Method5.MI.8391()"
		ldstr "class IBase1`1<class Generated879.BaseClass0> on type class Generated879.G3_C1351`1<class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated879.IBase1`1<class Generated879.BaseClass0>::Method6<object>()
		calli default string(class Generated879.G3_C1351`1<class Generated879.BaseClass1>)
		ldstr "G2_C384::Method6.MI.8393<System.Object>()"
		ldstr "class IBase1`1<class Generated879.BaseClass0> on type class Generated879.G3_C1351`1<class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated879.IBase2`2<class Generated879.BaseClass0,class Generated879.BaseClass1>::Method7<object>()
		calli default string(class Generated879.G3_C1351`1<class Generated879.BaseClass1>)
		ldstr "G3_C1351::Method7.MI.15725<System.Object>()"
		ldstr "class IBase2`2<class Generated879.BaseClass0,class Generated879.BaseClass1> on type class Generated879.G3_C1351`1<class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>::ClassMethod2115<object>()
		calli default string(class Generated879.G3_C1351`1<class Generated879.BaseClass1>)
		ldstr "G2_C384::ClassMethod2115.8397<System.Object>()"
		ldstr "class G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1> on type class Generated879.G3_C1351`1<class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>::ClassMethod2114<object>()
		calli default string(class Generated879.G3_C1351`1<class Generated879.BaseClass1>)
		ldstr "G2_C384::ClassMethod2114.8396<System.Object>()"
		ldstr "class G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1> on type class Generated879.G3_C1351`1<class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>::Method7<object>()
		calli default string(class Generated879.G3_C1351`1<class Generated879.BaseClass1>)
		ldstr "G2_C384::Method7.8394<System.Object>()"
		ldstr "class G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1> on type class Generated879.G3_C1351`1<class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>::Method6<object>()
		calli default string(class Generated879.G3_C1351`1<class Generated879.BaseClass1>)
		ldstr "G2_C384::Method6.8392<System.Object>()"
		ldstr "class G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1> on type class Generated879.G3_C1351`1<class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>::Method5()
		calli default string(class Generated879.G3_C1351`1<class Generated879.BaseClass1>)
		ldstr "G2_C384::Method5.8390()"
		ldstr "class G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1> on type class Generated879.G3_C1351`1<class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>::Method4()
		calli default string(class Generated879.G3_C1351`1<class Generated879.BaseClass1>)
		ldstr "G2_C384::Method4.8389()"
		ldstr "class G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1> on type class Generated879.G3_C1351`1<class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>::ClassMethod1331<object>()
		calli default string(class Generated879.G3_C1351`1<class Generated879.BaseClass1>)
		ldstr "G1_C7::ClassMethod1331.4820<System.Object>()"
		ldstr "class G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1> on type class Generated879.G3_C1351`1<class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>::ClassMethod1330<object>()
		calli default string(class Generated879.G3_C1351`1<class Generated879.BaseClass1>)
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1> on type class Generated879.G3_C1351`1<class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>::ClassMethod1329()
		calli default string(class Generated879.G3_C1351`1<class Generated879.BaseClass1>)
		ldstr "G1_C7::ClassMethod1329.4818()"
		ldstr "class G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1> on type class Generated879.G3_C1351`1<class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>::ClassMethod1328()
		calli default string(class Generated879.G3_C1351`1<class Generated879.BaseClass1>)
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1> on type class Generated879.G3_C1351`1<class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G3_C1351`1<class Generated879.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated879.G3_C1351`1<class Generated879.BaseClass1>::ClassMethod4099<object>()
		calli default string(class Generated879.G3_C1351`1<class Generated879.BaseClass1>)
		ldstr "G3_C1351::ClassMethod4099.15729<System.Object>()"
		ldstr "class G3_C1351`1<class Generated879.BaseClass1> on type class Generated879.G3_C1351`1<class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G3_C1351`1<class Generated879.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated879.G3_C1351`1<class Generated879.BaseClass1>::ClassMethod4098<object>()
		calli default string(class Generated879.G3_C1351`1<class Generated879.BaseClass1>)
		ldstr "G3_C1351::ClassMethod4098.15728<System.Object>()"
		ldstr "class G3_C1351`1<class Generated879.BaseClass1> on type class Generated879.G3_C1351`1<class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G3_C1351`1<class Generated879.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated879.G3_C1351`1<class Generated879.BaseClass1>::ClassMethod4097()
		calli default string(class Generated879.G3_C1351`1<class Generated879.BaseClass1>)
		ldstr "G3_C1351::ClassMethod4097.15727()"
		ldstr "class G3_C1351`1<class Generated879.BaseClass1> on type class Generated879.G3_C1351`1<class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G3_C1351`1<class Generated879.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated879.G3_C1351`1<class Generated879.BaseClass1>::ClassMethod4096()
		calli default string(class Generated879.G3_C1351`1<class Generated879.BaseClass1>)
		ldstr "G3_C1351::ClassMethod4096.15726()"
		ldstr "class G3_C1351`1<class Generated879.BaseClass1> on type class Generated879.G3_C1351`1<class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G3_C1351`1<class Generated879.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated879.G3_C1351`1<class Generated879.BaseClass1>::Method7<object>()
		calli default string(class Generated879.G3_C1351`1<class Generated879.BaseClass1>)
		ldstr "G3_C1351::Method7.15724<System.Object>()"
		ldstr "class G3_C1351`1<class Generated879.BaseClass1> on type class Generated879.G3_C1351`1<class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G3_C1351`1<class Generated879.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated879.G3_C1351`1<class Generated879.BaseClass1>::ClassMethod2115<object>()
		calli default string(class Generated879.G3_C1351`1<class Generated879.BaseClass1>)
		ldstr "G2_C384::ClassMethod2115.8397<System.Object>()"
		ldstr "class G3_C1351`1<class Generated879.BaseClass1> on type class Generated879.G3_C1351`1<class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G3_C1351`1<class Generated879.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated879.G3_C1351`1<class Generated879.BaseClass1>::ClassMethod2114<object>()
		calli default string(class Generated879.G3_C1351`1<class Generated879.BaseClass1>)
		ldstr "G2_C384::ClassMethod2114.8396<System.Object>()"
		ldstr "class G3_C1351`1<class Generated879.BaseClass1> on type class Generated879.G3_C1351`1<class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G3_C1351`1<class Generated879.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated879.G3_C1351`1<class Generated879.BaseClass1>::Method6<object>()
		calli default string(class Generated879.G3_C1351`1<class Generated879.BaseClass1>)
		ldstr "G2_C384::Method6.8392<System.Object>()"
		ldstr "class G3_C1351`1<class Generated879.BaseClass1> on type class Generated879.G3_C1351`1<class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G3_C1351`1<class Generated879.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated879.G3_C1351`1<class Generated879.BaseClass1>::Method5()
		calli default string(class Generated879.G3_C1351`1<class Generated879.BaseClass1>)
		ldstr "G2_C384::Method5.8390()"
		ldstr "class G3_C1351`1<class Generated879.BaseClass1> on type class Generated879.G3_C1351`1<class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G3_C1351`1<class Generated879.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated879.G3_C1351`1<class Generated879.BaseClass1>::Method4()
		calli default string(class Generated879.G3_C1351`1<class Generated879.BaseClass1>)
		ldstr "G2_C384::Method4.8389()"
		ldstr "class G3_C1351`1<class Generated879.BaseClass1> on type class Generated879.G3_C1351`1<class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G3_C1351`1<class Generated879.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated879.G3_C1351`1<class Generated879.BaseClass1>::ClassMethod1331<object>()
		calli default string(class Generated879.G3_C1351`1<class Generated879.BaseClass1>)
		ldstr "G1_C7::ClassMethod1331.4820<System.Object>()"
		ldstr "class G3_C1351`1<class Generated879.BaseClass1> on type class Generated879.G3_C1351`1<class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G3_C1351`1<class Generated879.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated879.G3_C1351`1<class Generated879.BaseClass1>::ClassMethod1330<object>()
		calli default string(class Generated879.G3_C1351`1<class Generated879.BaseClass1>)
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G3_C1351`1<class Generated879.BaseClass1> on type class Generated879.G3_C1351`1<class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G3_C1351`1<class Generated879.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated879.G3_C1351`1<class Generated879.BaseClass1>::ClassMethod1329()
		calli default string(class Generated879.G3_C1351`1<class Generated879.BaseClass1>)
		ldstr "G1_C7::ClassMethod1329.4818()"
		ldstr "class G3_C1351`1<class Generated879.BaseClass1> on type class Generated879.G3_C1351`1<class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G3_C1351`1<class Generated879.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated879.G3_C1351`1<class Generated879.BaseClass1>::ClassMethod1328()
		calli default string(class Generated879.G3_C1351`1<class Generated879.BaseClass1>)
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G3_C1351`1<class Generated879.BaseClass1> on type class Generated879.G3_C1351`1<class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated879.G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0>::ClassMethod1331<object>()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>)
		ldstr "G1_C7::ClassMethod1331.4820<System.Object>()"
		ldstr "class G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated879.G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0>::ClassMethod1330<object>()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>)
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated879.G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0>::ClassMethod1329()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>)
		ldstr "G1_C7::ClassMethod1329.4818()"
		ldstr "class G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated879.G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0>::ClassMethod1328()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>)
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated879.G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0>::Method7<object>()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>)
		ldstr "G2_C384::Method7.8394<System.Object>()"
		ldstr "class G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated879.G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0>::Method6<object>()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>)
		ldstr "G1_C7::Method6.4813<System.Object>()"
		ldstr "class G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated879.G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0>::Method5()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>)
		ldstr "G2_C384::Method5.8390()"
		ldstr "class G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated879.G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0>::Method4()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>)
		ldstr "G1_C7::Method4.4811()"
		ldstr "class G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated879.IBase1`1<class Generated879.BaseClass0>::Method4()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>)
		ldstr "G2_C384::Method4.8389()"
		ldstr "class IBase1`1<class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated879.IBase1`1<class Generated879.BaseClass0>::Method5()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>)
		ldstr "G2_C384::Method5.MI.8391()"
		ldstr "class IBase1`1<class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated879.IBase1`1<class Generated879.BaseClass0>::Method6<object>()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>)
		ldstr "G2_C384::Method6.MI.8393<System.Object>()"
		ldstr "class IBase1`1<class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated879.IBase2`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::Method7<object>()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>)
		ldstr "G1_C7::Method7.MI.4816<System.Object>()"
		ldstr "class IBase2`2<class Generated879.BaseClass1,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated879.IBase2`2<class Generated879.BaseClass0,class Generated879.BaseClass0>::Method7<object>()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>)
		ldstr "G1_C7::Method7.MI.4816<System.Object>()"
		ldstr "class IBase2`2<class Generated879.BaseClass0,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated879.IBase2`2<class Generated879.BaseClass0,class Generated879.BaseClass1>::Method7<object>()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>)
		ldstr "G2_C384::Method7.MI.8395<System.Object>()"
		ldstr "class IBase2`2<class Generated879.BaseClass0,class Generated879.BaseClass1> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated879.IBase2`2<class Generated879.BaseClass1,class Generated879.BaseClass1>::Method7<object>()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>)
		ldstr "G1_C7::Method7.MI.4816<System.Object>()"
		ldstr "class IBase2`2<class Generated879.BaseClass1,class Generated879.BaseClass1> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>::ClassMethod2115<object>()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>)
		ldstr "G2_C384::ClassMethod2115.8397<System.Object>()"
		ldstr "class G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>::ClassMethod2114<object>()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>)
		ldstr "G2_C384::ClassMethod2114.8396<System.Object>()"
		ldstr "class G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>::Method7<object>()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>)
		ldstr "G2_C384::Method7.8394<System.Object>()"
		ldstr "class G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>::Method6<object>()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>)
		ldstr "G2_C384::Method6.8392<System.Object>()"
		ldstr "class G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>::Method5()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>)
		ldstr "G2_C384::Method5.8390()"
		ldstr "class G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>::Method4()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>)
		ldstr "G2_C384::Method4.8389()"
		ldstr "class G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>::ClassMethod1331<object>()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>)
		ldstr "G1_C7::ClassMethod1331.4820<System.Object>()"
		ldstr "class G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>::ClassMethod1330<object>()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>)
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>::ClassMethod1329()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>)
		ldstr "G1_C7::ClassMethod1329.4818()"
		ldstr "class G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>::ClassMethod1328()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>)
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::ClassMethod1331<object>()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>)
		ldstr "G1_C7::ClassMethod1331.4820<System.Object>()"
		ldstr "class G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::ClassMethod1330<object>()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>)
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::ClassMethod1329()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>)
		ldstr "G1_C7::ClassMethod1329.4818()"
		ldstr "class G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::ClassMethod1328()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>)
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::Method7<object>()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>)
		ldstr "G2_C384::Method7.8394<System.Object>()"
		ldstr "class G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::Method6<object>()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>)
		ldstr "G1_C7::Method6.4813<System.Object>()"
		ldstr "class G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::Method5()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>)
		ldstr "G2_C384::Method5.8390()"
		ldstr "class G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::Method4()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>)
		ldstr "G1_C7::Method4.4811()"
		ldstr "class G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated879.IBase1`1<class Generated879.BaseClass1>::Method4()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>)
		ldstr "G1_C7::Method4.4811()"
		ldstr "class IBase1`1<class Generated879.BaseClass1> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated879.IBase1`1<class Generated879.BaseClass1>::Method5()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>)
		ldstr "G2_C384::Method5.8390()"
		ldstr "class IBase1`1<class Generated879.BaseClass1> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated879.IBase1`1<class Generated879.BaseClass1>::Method6<object>()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>)
		ldstr "G1_C7::Method6.MI.4814<System.Object>()"
		ldstr "class IBase1`1<class Generated879.BaseClass1> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated879.IBase2`2<class Generated879.BaseClass1,class Generated879.BaseClass1>::Method7<object>()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>)
		ldstr "G2_C384::Method7.MI.8395<System.Object>()"
		ldstr "class IBase2`2<class Generated879.BaseClass1,class Generated879.BaseClass1> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated879.IBase1`1<class Generated879.BaseClass0>::Method4()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>)
		ldstr "G2_C384::Method4.8389()"
		ldstr "class IBase1`1<class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated879.IBase1`1<class Generated879.BaseClass0>::Method5()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>)
		ldstr "G2_C384::Method5.MI.8391()"
		ldstr "class IBase1`1<class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated879.IBase1`1<class Generated879.BaseClass0>::Method6<object>()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>)
		ldstr "G2_C384::Method6.MI.8393<System.Object>()"
		ldstr "class IBase1`1<class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated879.IBase2`2<class Generated879.BaseClass0,class Generated879.BaseClass1>::Method7<object>()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>)
		ldstr "G2_C384::Method7.MI.8395<System.Object>()"
		ldstr "class IBase2`2<class Generated879.BaseClass0,class Generated879.BaseClass1> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>::ClassMethod2115<object>()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>)
		ldstr "G2_C384::ClassMethod2115.8397<System.Object>()"
		ldstr "class G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>::ClassMethod2114<object>()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>)
		ldstr "G2_C384::ClassMethod2114.8396<System.Object>()"
		ldstr "class G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>::Method7<object>()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>)
		ldstr "G2_C384::Method7.8394<System.Object>()"
		ldstr "class G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>::Method6<object>()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>)
		ldstr "G2_C384::Method6.8392<System.Object>()"
		ldstr "class G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>::Method5()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>)
		ldstr "G2_C384::Method5.8390()"
		ldstr "class G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>::Method4()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>)
		ldstr "G2_C384::Method4.8389()"
		ldstr "class G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>::ClassMethod1331<object>()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>)
		ldstr "G1_C7::ClassMethod1331.4820<System.Object>()"
		ldstr "class G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>::ClassMethod1330<object>()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>)
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>::ClassMethod1329()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>)
		ldstr "G1_C7::ClassMethod1329.4818()"
		ldstr "class G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>::ClassMethod1328()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>)
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1> on type class Generated879.G2_C384`2<class Generated879.BaseClass0,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated879.G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0>::ClassMethod1331<object>()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>)
		ldstr "G1_C7::ClassMethod1331.4820<System.Object>()"
		ldstr "class G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated879.G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0>::ClassMethod1330<object>()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>)
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated879.G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0>::ClassMethod1329()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>)
		ldstr "G1_C7::ClassMethod1329.4818()"
		ldstr "class G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated879.G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0>::ClassMethod1328()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>)
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated879.G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0>::Method7<object>()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>)
		ldstr "G2_C384::Method7.8394<System.Object>()"
		ldstr "class G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated879.G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0>::Method6<object>()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>)
		ldstr "G1_C7::Method6.4813<System.Object>()"
		ldstr "class G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated879.G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0>::Method5()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>)
		ldstr "G2_C384::Method5.8390()"
		ldstr "class G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated879.G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0>::Method4()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>)
		ldstr "G1_C7::Method4.4811()"
		ldstr "class G1_C7`2<class Generated879.BaseClass0,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated879.IBase1`1<class Generated879.BaseClass0>::Method4()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>)
		ldstr "G2_C384::Method4.8389()"
		ldstr "class IBase1`1<class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated879.IBase1`1<class Generated879.BaseClass0>::Method5()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>)
		ldstr "G2_C384::Method5.MI.8391()"
		ldstr "class IBase1`1<class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated879.IBase1`1<class Generated879.BaseClass0>::Method6<object>()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>)
		ldstr "G2_C384::Method6.MI.8393<System.Object>()"
		ldstr "class IBase1`1<class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated879.IBase2`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::Method7<object>()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>)
		ldstr "G1_C7::Method7.MI.4816<System.Object>()"
		ldstr "class IBase2`2<class Generated879.BaseClass1,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated879.IBase2`2<class Generated879.BaseClass0,class Generated879.BaseClass0>::Method7<object>()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>)
		ldstr "G1_C7::Method7.MI.4816<System.Object>()"
		ldstr "class IBase2`2<class Generated879.BaseClass0,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated879.IBase2`2<class Generated879.BaseClass0,class Generated879.BaseClass1>::Method7<object>()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>)
		ldstr "G2_C384::Method7.MI.8395<System.Object>()"
		ldstr "class IBase2`2<class Generated879.BaseClass0,class Generated879.BaseClass1> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated879.IBase2`2<class Generated879.BaseClass1,class Generated879.BaseClass1>::Method7<object>()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>)
		ldstr "G1_C7::Method7.MI.4816<System.Object>()"
		ldstr "class IBase2`2<class Generated879.BaseClass1,class Generated879.BaseClass1> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::ClassMethod2115<object>()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>)
		ldstr "G2_C384::ClassMethod2115.8397<System.Object>()"
		ldstr "class G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::ClassMethod2114<object>()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>)
		ldstr "G2_C384::ClassMethod2114.8396<System.Object>()"
		ldstr "class G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::Method7<object>()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>)
		ldstr "G2_C384::Method7.8394<System.Object>()"
		ldstr "class G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::Method6<object>()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>)
		ldstr "G2_C384::Method6.8392<System.Object>()"
		ldstr "class G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::Method5()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>)
		ldstr "G2_C384::Method5.8390()"
		ldstr "class G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::Method4()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>)
		ldstr "G2_C384::Method4.8389()"
		ldstr "class G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::ClassMethod1331<object>()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>)
		ldstr "G1_C7::ClassMethod1331.4820<System.Object>()"
		ldstr "class G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::ClassMethod1330<object>()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>)
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::ClassMethod1329()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>)
		ldstr "G1_C7::ClassMethod1329.4818()"
		ldstr "class G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::ClassMethod1328()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>)
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated879.IBase1`1<class Generated879.BaseClass1>::Method4()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>)
		ldstr "G2_C384::Method4.8389()"
		ldstr "class IBase1`1<class Generated879.BaseClass1> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated879.IBase1`1<class Generated879.BaseClass1>::Method5()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>)
		ldstr "G2_C384::Method5.MI.8391()"
		ldstr "class IBase1`1<class Generated879.BaseClass1> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated879.IBase1`1<class Generated879.BaseClass1>::Method6<object>()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>)
		ldstr "G2_C384::Method6.MI.8393<System.Object>()"
		ldstr "class IBase1`1<class Generated879.BaseClass1> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::ClassMethod1331<object>()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>)
		ldstr "G1_C7::ClassMethod1331.4820<System.Object>()"
		ldstr "class G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::ClassMethod1330<object>()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>)
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::ClassMethod1329()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>)
		ldstr "G1_C7::ClassMethod1329.4818()"
		ldstr "class G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::ClassMethod1328()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>)
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::Method7<object>()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>)
		ldstr "G2_C384::Method7.8394<System.Object>()"
		ldstr "class G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::Method6<object>()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>)
		ldstr "G1_C7::Method6.4813<System.Object>()"
		ldstr "class G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::Method5()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>)
		ldstr "G2_C384::Method5.8390()"
		ldstr "class G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated879.G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0>::Method4()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>)
		ldstr "G1_C7::Method4.4811()"
		ldstr "class G1_C7`2<class Generated879.BaseClass1,class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated879.IBase1`1<class Generated879.BaseClass1>::Method4()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>)
		ldstr "G2_C384::Method4.8389()"
		ldstr "class IBase1`1<class Generated879.BaseClass1> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated879.IBase1`1<class Generated879.BaseClass1>::Method5()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>)
		ldstr "G2_C384::Method5.MI.8391()"
		ldstr "class IBase1`1<class Generated879.BaseClass1> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated879.IBase1`1<class Generated879.BaseClass1>::Method6<object>()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>)
		ldstr "G2_C384::Method6.MI.8393<System.Object>()"
		ldstr "class IBase1`1<class Generated879.BaseClass1> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated879.IBase2`2<class Generated879.BaseClass1,class Generated879.BaseClass1>::Method7<object>()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>)
		ldstr "G2_C384::Method7.MI.8395<System.Object>()"
		ldstr "class IBase2`2<class Generated879.BaseClass1,class Generated879.BaseClass1> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated879.IBase1`1<class Generated879.BaseClass0>::Method4()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>)
		ldstr "G2_C384::Method4.8389()"
		ldstr "class IBase1`1<class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated879.IBase1`1<class Generated879.BaseClass0>::Method5()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>)
		ldstr "G2_C384::Method5.MI.8391()"
		ldstr "class IBase1`1<class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated879.IBase1`1<class Generated879.BaseClass0>::Method6<object>()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>)
		ldstr "G2_C384::Method6.MI.8393<System.Object>()"
		ldstr "class IBase1`1<class Generated879.BaseClass0> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated879.IBase2`2<class Generated879.BaseClass0,class Generated879.BaseClass1>::Method7<object>()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>)
		ldstr "G2_C384::Method7.MI.8395<System.Object>()"
		ldstr "class IBase2`2<class Generated879.BaseClass0,class Generated879.BaseClass1> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>::ClassMethod2115<object>()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>)
		ldstr "G2_C384::ClassMethod2115.8397<System.Object>()"
		ldstr "class G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>::ClassMethod2114<object>()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>)
		ldstr "G2_C384::ClassMethod2114.8396<System.Object>()"
		ldstr "class G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>::Method7<object>()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>)
		ldstr "G2_C384::Method7.8394<System.Object>()"
		ldstr "class G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>::Method6<object>()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>)
		ldstr "G2_C384::Method6.8392<System.Object>()"
		ldstr "class G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>::Method5()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>)
		ldstr "G2_C384::Method5.8390()"
		ldstr "class G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>::Method4()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>)
		ldstr "G2_C384::Method4.8389()"
		ldstr "class G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>::ClassMethod1331<object>()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>)
		ldstr "G1_C7::ClassMethod1331.4820<System.Object>()"
		ldstr "class G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>::ClassMethod1330<object>()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>)
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>::ClassMethod1329()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>)
		ldstr "G1_C7::ClassMethod1329.4818()"
		ldstr "class G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>::ClassMethod1328()
		calli default string(class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>)
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1> on type class Generated879.G2_C384`2<class Generated879.BaseClass1,class Generated879.BaseClass1>"
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
		call void Generated879.Generated879::MethodCallingTest()
		call void Generated879.Generated879::ConstrainedCallsTest()
		call void Generated879.Generated879::StructConstrainedInterfaceCallsTest()
		call void Generated879.Generated879::CalliTest()
		ldc.i4 100
		ret
	}
}
