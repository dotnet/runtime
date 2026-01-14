// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) .ver 4:0:0:0 }
.assembly extern TestFramework { .publickeytoken = ( B0 3F 5F 7F 11 D5 0A 3A )  }

//TYPES IN FORWARDER ASSEMBLIES:

//TEST ASSEMBLY:
.assembly extern xunit.core {}

.class public Generated738.BaseClass0 
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void [mscorlib]System.Object::.ctor()
		ret
	}
}
.class public Generated738.BaseClass1 
		extends Generated738.BaseClass0
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void Generated738.BaseClass0::.ctor()
		ret
	}
}
.class public Generated738.G3_C1210`1<T0> 
		extends class Generated738.G2_C262`2<!T0,class Generated738.BaseClass1>
		implements class Generated738.IBase2`2<class Generated738.BaseClass0,!T0> 
{
	.method public hidebysig virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G3_C1210::Method7.14905<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod3806<M0>() cil managed noinlining { 
		ldstr "G3_C1210::ClassMethod3806.14906<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C262<T0,class Generated738.BaseClass1>.ClassMethod1859'() cil managed noinlining { 
		.override method instance string class Generated738.G2_C262`2<!T0,class Generated738.BaseClass1>::ClassMethod1859()
		ldstr "G3_C1210::ClassMethod1859.MI.14907()"
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class Generated738.G2_C262`2<!T0,class Generated738.BaseClass1>::.ctor()
		ret
	}
}
.class public Generated738.G2_C262`2<T0, T1> 
		extends class Generated738.G1_C5`1<!T0>
		implements class Generated738.IBase1`1<class Generated738.BaseClass1>, class Generated738.IBase2`2<class Generated738.BaseClass1,!T1> 
{
	.method public hidebysig newslot virtual instance string Method4() cil managed noinlining { 
		ldstr "G2_C262::Method4.7211()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class Generated738.BaseClass1>.Method4'() cil managed noinlining { 
		.override method instance string class Generated738.IBase1`1<class Generated738.BaseClass1>::Method4()
		ldstr "G2_C262::Method4.MI.7212()"
		ret
	}
	.method public hidebysig virtual instance string Method5() cil managed noinlining { 
		ldstr "G2_C262::Method5.7213()"
		ret
	}
	.method public hidebysig virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G2_C262::Method6.7214<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class Generated738.BaseClass1>.Method6'<M0>() cil managed noinlining { 
		.override method instance string class Generated738.IBase1`1<class Generated738.BaseClass1>::Method6<[1]>()
		ldstr "G2_C262::Method6.MI.7215<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G2_C262::Method7.7216<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase2<class Generated738.BaseClass1,T1>.Method7'<M0>() cil managed noinlining { 
		.override method instance string class Generated738.IBase2`2<class Generated738.BaseClass1,!T1>::Method7<[1]>()
		ldstr "G2_C262::Method7.MI.7217<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1859() cil managed noinlining { 
		ldstr "G2_C262::ClassMethod1859.7218()"
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class Generated738.G1_C5`1<!T0>::.ctor()
		ret
	}
}
.class interface public abstract Generated738.IBase2`2<+T0, -T1> 
{
	.method public hidebysig newslot abstract virtual instance string Method7<M0>() cil managed { }
}
.class public Generated738.G1_C5`1<T0> 
		implements class Generated738.IBase2`2<class Generated738.BaseClass1,class Generated738.BaseClass1>, class Generated738.IBase1`1<!T0> 
{
	.method public hidebysig newslot virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G1_C5::Method7.4801<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig virtual instance string Method4() cil managed noinlining { 
		ldstr "G1_C5::Method4.4802()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method5() cil managed noinlining { 
		ldstr "G1_C5::Method5.4803()"
		ret
	}
	.method public hidebysig virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G1_C5::Method6.4804<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<T0>.Method6'<M0>() cil managed noinlining { 
		.override method instance string class Generated738.IBase1`1<!T0>::Method6<[1]>()
		ldstr "G1_C5::Method6.MI.4805<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1324() cil managed noinlining { 
		ldstr "G1_C5::ClassMethod1324.4806()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1325<M0>() cil managed noinlining { 
		ldstr "G1_C5::ClassMethod1325.4807<"
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
.class interface public abstract Generated738.IBase1`1<+T0> 
{
	.method public hidebysig newslot abstract virtual instance string Method4() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method5() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method6<M0>() cil managed { }
}
.class public auto ansi beforefieldinit Generated738.Generated738 {
	.method static void M.BaseClass0<(Generated738.BaseClass0)W>(!!W inst, string exp) cil managed {
		.maxstack 5
		.locals init (string[] actualResults)
		ldc.i4.s    0
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.BaseClass0<(Generated738.BaseClass0)W>(!!W inst, string exp)"
		ldc.i4.s 0
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.BaseClass1<(Generated738.BaseClass1)W>(!!W inst, string exp) cil managed {
		.maxstack 5
		.locals init (string[] actualResults)
		ldc.i4.s    0
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.BaseClass1<(Generated738.BaseClass1)W>(!!W inst, string exp)"
		ldc.i4.s 0
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1210.T<T0,(class Generated738.G3_C1210`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1210.T<T0,(class Generated738.G3_C1210`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.G3_C1210`1<!!T0>::ClassMethod1324()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.G3_C1210`1<!!T0>::ClassMethod1325<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.G3_C1210`1<!!T0>::ClassMethod1859()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.G3_C1210`1<!!T0>::ClassMethod3806<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.G3_C1210`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.G3_C1210`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.G3_C1210`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.G3_C1210`1<!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1210.A<(class Generated738.G3_C1210`1<class Generated738.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1210.A<(class Generated738.G3_C1210`1<class Generated738.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.G3_C1210`1<class Generated738.BaseClass0>::ClassMethod1324()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.G3_C1210`1<class Generated738.BaseClass0>::ClassMethod1325<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.G3_C1210`1<class Generated738.BaseClass0>::ClassMethod1859()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.G3_C1210`1<class Generated738.BaseClass0>::ClassMethod3806<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.G3_C1210`1<class Generated738.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.G3_C1210`1<class Generated738.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.G3_C1210`1<class Generated738.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.G3_C1210`1<class Generated738.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1210.B<(class Generated738.G3_C1210`1<class Generated738.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1210.B<(class Generated738.G3_C1210`1<class Generated738.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.G3_C1210`1<class Generated738.BaseClass1>::ClassMethod1324()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.G3_C1210`1<class Generated738.BaseClass1>::ClassMethod1325<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.G3_C1210`1<class Generated738.BaseClass1>::ClassMethod1859()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.G3_C1210`1<class Generated738.BaseClass1>::ClassMethod3806<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.G3_C1210`1<class Generated738.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.G3_C1210`1<class Generated738.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.G3_C1210`1<class Generated738.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.G3_C1210`1<class Generated738.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C262.T.T<T0,T1,(class Generated738.G2_C262`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C262.T.T<T0,T1,(class Generated738.G2_C262`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.G2_C262`2<!!T0,!!T1>::ClassMethod1324()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.G2_C262`2<!!T0,!!T1>::ClassMethod1325<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.G2_C262`2<!!T0,!!T1>::ClassMethod1859()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.G2_C262`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.G2_C262`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.G2_C262`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.G2_C262`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C262.A.T<T1,(class Generated738.G2_C262`2<class Generated738.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C262.A.T<T1,(class Generated738.G2_C262`2<class Generated738.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.G2_C262`2<class Generated738.BaseClass0,!!T1>::ClassMethod1324()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.G2_C262`2<class Generated738.BaseClass0,!!T1>::ClassMethod1325<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.G2_C262`2<class Generated738.BaseClass0,!!T1>::ClassMethod1859()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.G2_C262`2<class Generated738.BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.G2_C262`2<class Generated738.BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.G2_C262`2<class Generated738.BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.G2_C262`2<class Generated738.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C262.A.A<(class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C262.A.A<(class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>::ClassMethod1324()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>::ClassMethod1325<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>::ClassMethod1859()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C262.A.B<(class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C262.A.B<(class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>::ClassMethod1324()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>::ClassMethod1325<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>::ClassMethod1859()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C262.B.T<T1,(class Generated738.G2_C262`2<class Generated738.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C262.B.T<T1,(class Generated738.G2_C262`2<class Generated738.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.G2_C262`2<class Generated738.BaseClass1,!!T1>::ClassMethod1324()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.G2_C262`2<class Generated738.BaseClass1,!!T1>::ClassMethod1325<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.G2_C262`2<class Generated738.BaseClass1,!!T1>::ClassMethod1859()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.G2_C262`2<class Generated738.BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.G2_C262`2<class Generated738.BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.G2_C262`2<class Generated738.BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.G2_C262`2<class Generated738.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C262.B.A<(class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C262.B.A<(class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>::ClassMethod1324()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>::ClassMethod1325<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>::ClassMethod1859()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C262.B.B<(class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C262.B.B<(class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>::ClassMethod1324()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>::ClassMethod1325<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>::ClassMethod1859()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.T.T<T0,T1,(class Generated738.IBase2`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.T.T<T0,T1,(class Generated738.IBase2`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.IBase2`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.T<T1,(class Generated738.IBase2`2<class Generated738.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.T<T1,(class Generated738.IBase2`2<class Generated738.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.IBase2`2<class Generated738.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.A<(class Generated738.IBase2`2<class Generated738.BaseClass0,class Generated738.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.A<(class Generated738.IBase2`2<class Generated738.BaseClass0,class Generated738.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.IBase2`2<class Generated738.BaseClass0,class Generated738.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.B<(class Generated738.IBase2`2<class Generated738.BaseClass0,class Generated738.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.B<(class Generated738.IBase2`2<class Generated738.BaseClass0,class Generated738.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.IBase2`2<class Generated738.BaseClass0,class Generated738.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.T<T1,(class Generated738.IBase2`2<class Generated738.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.T<T1,(class Generated738.IBase2`2<class Generated738.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.IBase2`2<class Generated738.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.A<(class Generated738.IBase2`2<class Generated738.BaseClass1,class Generated738.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.A<(class Generated738.IBase2`2<class Generated738.BaseClass1,class Generated738.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.IBase2`2<class Generated738.BaseClass1,class Generated738.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.B<(class Generated738.IBase2`2<class Generated738.BaseClass1,class Generated738.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.B<(class Generated738.IBase2`2<class Generated738.BaseClass1,class Generated738.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.IBase2`2<class Generated738.BaseClass1,class Generated738.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C5.T<T0,(class Generated738.G1_C5`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C5.T<T0,(class Generated738.G1_C5`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.G1_C5`1<!!T0>::ClassMethod1324()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.G1_C5`1<!!T0>::ClassMethod1325<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.G1_C5`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.G1_C5`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.G1_C5`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.G1_C5`1<!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C5.A<(class Generated738.G1_C5`1<class Generated738.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C5.A<(class Generated738.G1_C5`1<class Generated738.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.G1_C5`1<class Generated738.BaseClass0>::ClassMethod1324()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.G1_C5`1<class Generated738.BaseClass0>::ClassMethod1325<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.G1_C5`1<class Generated738.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.G1_C5`1<class Generated738.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.G1_C5`1<class Generated738.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.G1_C5`1<class Generated738.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C5.B<(class Generated738.G1_C5`1<class Generated738.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C5.B<(class Generated738.G1_C5`1<class Generated738.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.G1_C5`1<class Generated738.BaseClass1>::ClassMethod1324()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.G1_C5`1<class Generated738.BaseClass1>::ClassMethod1325<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.G1_C5`1<class Generated738.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.G1_C5`1<class Generated738.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.G1_C5`1<class Generated738.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.G1_C5`1<class Generated738.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.T<T0,(class Generated738.IBase1`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.T<T0,(class Generated738.IBase1`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.IBase1`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.IBase1`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.IBase1`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.A<(class Generated738.IBase1`1<class Generated738.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.A<(class Generated738.IBase1`1<class Generated738.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.IBase1`1<class Generated738.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.IBase1`1<class Generated738.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.IBase1`1<class Generated738.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.B<(class Generated738.IBase1`1<class Generated738.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.B<(class Generated738.IBase1`1<class Generated738.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.IBase1`1<class Generated738.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.IBase1`1<class Generated738.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated738.IBase1`1<class Generated738.BaseClass1>::Method6<object>()
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
		newobj instance void class Generated738.G3_C1210`1<class Generated738.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass0>
		callvirt instance string class Generated738.G1_C5`1<class Generated738.BaseClass0>::ClassMethod1325<object>()
		ldstr "G1_C5::ClassMethod1325.4807<System.Object>()"
		ldstr "class G1_C5`1<class Generated738.BaseClass0> on type class Generated738.G3_C1210`1<class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass0>
		callvirt instance string class Generated738.G1_C5`1<class Generated738.BaseClass0>::ClassMethod1324()
		ldstr "G1_C5::ClassMethod1324.4806()"
		ldstr "class G1_C5`1<class Generated738.BaseClass0> on type class Generated738.G3_C1210`1<class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass0>
		callvirt instance string class Generated738.G1_C5`1<class Generated738.BaseClass0>::Method6<object>()
		ldstr "G2_C262::Method6.7214<System.Object>()"
		ldstr "class G1_C5`1<class Generated738.BaseClass0> on type class Generated738.G3_C1210`1<class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass0>
		callvirt instance string class Generated738.G1_C5`1<class Generated738.BaseClass0>::Method5()
		ldstr "G2_C262::Method5.7213()"
		ldstr "class G1_C5`1<class Generated738.BaseClass0> on type class Generated738.G3_C1210`1<class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass0>
		callvirt instance string class Generated738.G1_C5`1<class Generated738.BaseClass0>::Method4()
		ldstr "G1_C5::Method4.4802()"
		ldstr "class G1_C5`1<class Generated738.BaseClass0> on type class Generated738.G3_C1210`1<class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass0>
		callvirt instance string class Generated738.G1_C5`1<class Generated738.BaseClass0>::Method7<object>()
		ldstr "G3_C1210::Method7.14905<System.Object>()"
		ldstr "class G1_C5`1<class Generated738.BaseClass0> on type class Generated738.G3_C1210`1<class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated738.IBase2`2<class Generated738.BaseClass1,class Generated738.BaseClass1>::Method7<object>()
		ldstr "G2_C262::Method7.MI.7217<System.Object>()"
		ldstr "class IBase2`2<class Generated738.BaseClass1,class Generated738.BaseClass1> on type class Generated738.G3_C1210`1<class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated738.IBase1`1<class Generated738.BaseClass0>::Method4()
		ldstr "G2_C262::Method4.MI.7212()"
		ldstr "class IBase1`1<class Generated738.BaseClass0> on type class Generated738.G3_C1210`1<class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated738.IBase1`1<class Generated738.BaseClass0>::Method5()
		ldstr "G2_C262::Method5.7213()"
		ldstr "class IBase1`1<class Generated738.BaseClass0> on type class Generated738.G3_C1210`1<class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated738.IBase1`1<class Generated738.BaseClass0>::Method6<object>()
		ldstr "G2_C262::Method6.MI.7215<System.Object>()"
		ldstr "class IBase1`1<class Generated738.BaseClass0> on type class Generated738.G3_C1210`1<class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated738.IBase2`2<class Generated738.BaseClass0,class Generated738.BaseClass1>::Method7<object>()
		ldstr "G3_C1210::Method7.14905<System.Object>()"
		ldstr "class IBase2`2<class Generated738.BaseClass0,class Generated738.BaseClass1> on type class Generated738.G3_C1210`1<class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>
		callvirt instance string class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>::ClassMethod1859()
		ldstr "G3_C1210::ClassMethod1859.MI.14907()"
		ldstr "class G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1> on type class Generated738.G3_C1210`1<class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>
		callvirt instance string class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>::Method7<object>()
		ldstr "G3_C1210::Method7.14905<System.Object>()"
		ldstr "class G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1> on type class Generated738.G3_C1210`1<class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>
		callvirt instance string class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>::Method6<object>()
		ldstr "G2_C262::Method6.7214<System.Object>()"
		ldstr "class G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1> on type class Generated738.G3_C1210`1<class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>
		callvirt instance string class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>::Method5()
		ldstr "G2_C262::Method5.7213()"
		ldstr "class G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1> on type class Generated738.G3_C1210`1<class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>
		callvirt instance string class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>::Method4()
		ldstr "G2_C262::Method4.7211()"
		ldstr "class G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1> on type class Generated738.G3_C1210`1<class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>
		callvirt instance string class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>::ClassMethod1325<object>()
		ldstr "G1_C5::ClassMethod1325.4807<System.Object>()"
		ldstr "class G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1> on type class Generated738.G3_C1210`1<class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>
		callvirt instance string class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>::ClassMethod1324()
		ldstr "G1_C5::ClassMethod1324.4806()"
		ldstr "class G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1> on type class Generated738.G3_C1210`1<class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated738.IBase1`1<class Generated738.BaseClass1>::Method4()
		ldstr "G2_C262::Method4.MI.7212()"
		ldstr "class IBase1`1<class Generated738.BaseClass1> on type class Generated738.G3_C1210`1<class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated738.IBase1`1<class Generated738.BaseClass1>::Method5()
		ldstr "G2_C262::Method5.7213()"
		ldstr "class IBase1`1<class Generated738.BaseClass1> on type class Generated738.G3_C1210`1<class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated738.IBase1`1<class Generated738.BaseClass1>::Method6<object>()
		ldstr "G2_C262::Method6.MI.7215<System.Object>()"
		ldstr "class IBase1`1<class Generated738.BaseClass1> on type class Generated738.G3_C1210`1<class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated738.G3_C1210`1<class Generated738.BaseClass0>
		callvirt instance string class Generated738.G3_C1210`1<class Generated738.BaseClass0>::ClassMethod3806<object>()
		ldstr "G3_C1210::ClassMethod3806.14906<System.Object>()"
		ldstr "class G3_C1210`1<class Generated738.BaseClass0> on type class Generated738.G3_C1210`1<class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated738.G3_C1210`1<class Generated738.BaseClass0>
		callvirt instance string class Generated738.G3_C1210`1<class Generated738.BaseClass0>::Method7<object>()
		ldstr "G3_C1210::Method7.14905<System.Object>()"
		ldstr "class G3_C1210`1<class Generated738.BaseClass0> on type class Generated738.G3_C1210`1<class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated738.G3_C1210`1<class Generated738.BaseClass0>
		callvirt instance string class Generated738.G3_C1210`1<class Generated738.BaseClass0>::ClassMethod1859()
		ldstr "G3_C1210::ClassMethod1859.MI.14907()"
		ldstr "class G3_C1210`1<class Generated738.BaseClass0> on type class Generated738.G3_C1210`1<class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated738.G3_C1210`1<class Generated738.BaseClass0>
		callvirt instance string class Generated738.G3_C1210`1<class Generated738.BaseClass0>::Method6<object>()
		ldstr "G2_C262::Method6.7214<System.Object>()"
		ldstr "class G3_C1210`1<class Generated738.BaseClass0> on type class Generated738.G3_C1210`1<class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated738.G3_C1210`1<class Generated738.BaseClass0>
		callvirt instance string class Generated738.G3_C1210`1<class Generated738.BaseClass0>::Method5()
		ldstr "G2_C262::Method5.7213()"
		ldstr "class G3_C1210`1<class Generated738.BaseClass0> on type class Generated738.G3_C1210`1<class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated738.G3_C1210`1<class Generated738.BaseClass0>
		callvirt instance string class Generated738.G3_C1210`1<class Generated738.BaseClass0>::Method4()
		ldstr "G2_C262::Method4.7211()"
		ldstr "class G3_C1210`1<class Generated738.BaseClass0> on type class Generated738.G3_C1210`1<class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated738.G3_C1210`1<class Generated738.BaseClass0>
		callvirt instance string class Generated738.G3_C1210`1<class Generated738.BaseClass0>::ClassMethod1325<object>()
		ldstr "G1_C5::ClassMethod1325.4807<System.Object>()"
		ldstr "class G3_C1210`1<class Generated738.BaseClass0> on type class Generated738.G3_C1210`1<class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated738.G3_C1210`1<class Generated738.BaseClass0>
		callvirt instance string class Generated738.G3_C1210`1<class Generated738.BaseClass0>::ClassMethod1324()
		ldstr "G1_C5::ClassMethod1324.4806()"
		ldstr "class G3_C1210`1<class Generated738.BaseClass0> on type class Generated738.G3_C1210`1<class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated738.IBase2`2<class Generated738.BaseClass0,class Generated738.BaseClass0>::Method7<object>()
		ldstr "G3_C1210::Method7.14905<System.Object>()"
		ldstr "class IBase2`2<class Generated738.BaseClass0,class Generated738.BaseClass0> on type class Generated738.G3_C1210`1<class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated738.G3_C1210`1<class Generated738.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass1>
		callvirt instance string class Generated738.G1_C5`1<class Generated738.BaseClass1>::ClassMethod1325<object>()
		ldstr "G1_C5::ClassMethod1325.4807<System.Object>()"
		ldstr "class G1_C5`1<class Generated738.BaseClass1> on type class Generated738.G3_C1210`1<class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass1>
		callvirt instance string class Generated738.G1_C5`1<class Generated738.BaseClass1>::ClassMethod1324()
		ldstr "G1_C5::ClassMethod1324.4806()"
		ldstr "class G1_C5`1<class Generated738.BaseClass1> on type class Generated738.G3_C1210`1<class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass1>
		callvirt instance string class Generated738.G1_C5`1<class Generated738.BaseClass1>::Method6<object>()
		ldstr "G2_C262::Method6.7214<System.Object>()"
		ldstr "class G1_C5`1<class Generated738.BaseClass1> on type class Generated738.G3_C1210`1<class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass1>
		callvirt instance string class Generated738.G1_C5`1<class Generated738.BaseClass1>::Method5()
		ldstr "G2_C262::Method5.7213()"
		ldstr "class G1_C5`1<class Generated738.BaseClass1> on type class Generated738.G3_C1210`1<class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass1>
		callvirt instance string class Generated738.G1_C5`1<class Generated738.BaseClass1>::Method4()
		ldstr "G1_C5::Method4.4802()"
		ldstr "class G1_C5`1<class Generated738.BaseClass1> on type class Generated738.G3_C1210`1<class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass1>
		callvirt instance string class Generated738.G1_C5`1<class Generated738.BaseClass1>::Method7<object>()
		ldstr "G3_C1210::Method7.14905<System.Object>()"
		ldstr "class G1_C5`1<class Generated738.BaseClass1> on type class Generated738.G3_C1210`1<class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated738.IBase2`2<class Generated738.BaseClass1,class Generated738.BaseClass1>::Method7<object>()
		ldstr "G2_C262::Method7.MI.7217<System.Object>()"
		ldstr "class IBase2`2<class Generated738.BaseClass1,class Generated738.BaseClass1> on type class Generated738.G3_C1210`1<class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated738.IBase1`1<class Generated738.BaseClass1>::Method4()
		ldstr "G2_C262::Method4.MI.7212()"
		ldstr "class IBase1`1<class Generated738.BaseClass1> on type class Generated738.G3_C1210`1<class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated738.IBase1`1<class Generated738.BaseClass1>::Method5()
		ldstr "G2_C262::Method5.7213()"
		ldstr "class IBase1`1<class Generated738.BaseClass1> on type class Generated738.G3_C1210`1<class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated738.IBase1`1<class Generated738.BaseClass1>::Method6<object>()
		ldstr "G2_C262::Method6.MI.7215<System.Object>()"
		ldstr "class IBase1`1<class Generated738.BaseClass1> on type class Generated738.G3_C1210`1<class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated738.IBase2`2<class Generated738.BaseClass0,class Generated738.BaseClass1>::Method7<object>()
		ldstr "G3_C1210::Method7.14905<System.Object>()"
		ldstr "class IBase2`2<class Generated738.BaseClass0,class Generated738.BaseClass1> on type class Generated738.G3_C1210`1<class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated738.IBase1`1<class Generated738.BaseClass0>::Method4()
		ldstr "G2_C262::Method4.MI.7212()"
		ldstr "class IBase1`1<class Generated738.BaseClass0> on type class Generated738.G3_C1210`1<class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated738.IBase1`1<class Generated738.BaseClass0>::Method5()
		ldstr "G2_C262::Method5.7213()"
		ldstr "class IBase1`1<class Generated738.BaseClass0> on type class Generated738.G3_C1210`1<class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated738.IBase1`1<class Generated738.BaseClass0>::Method6<object>()
		ldstr "G2_C262::Method6.MI.7215<System.Object>()"
		ldstr "class IBase1`1<class Generated738.BaseClass0> on type class Generated738.G3_C1210`1<class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>
		callvirt instance string class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>::ClassMethod1859()
		ldstr "G3_C1210::ClassMethod1859.MI.14907()"
		ldstr "class G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1> on type class Generated738.G3_C1210`1<class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>
		callvirt instance string class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>::Method7<object>()
		ldstr "G3_C1210::Method7.14905<System.Object>()"
		ldstr "class G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1> on type class Generated738.G3_C1210`1<class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>
		callvirt instance string class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>::Method6<object>()
		ldstr "G2_C262::Method6.7214<System.Object>()"
		ldstr "class G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1> on type class Generated738.G3_C1210`1<class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>
		callvirt instance string class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>::Method5()
		ldstr "G2_C262::Method5.7213()"
		ldstr "class G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1> on type class Generated738.G3_C1210`1<class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>
		callvirt instance string class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>::Method4()
		ldstr "G2_C262::Method4.7211()"
		ldstr "class G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1> on type class Generated738.G3_C1210`1<class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>
		callvirt instance string class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>::ClassMethod1325<object>()
		ldstr "G1_C5::ClassMethod1325.4807<System.Object>()"
		ldstr "class G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1> on type class Generated738.G3_C1210`1<class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>
		callvirt instance string class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>::ClassMethod1324()
		ldstr "G1_C5::ClassMethod1324.4806()"
		ldstr "class G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1> on type class Generated738.G3_C1210`1<class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated738.G3_C1210`1<class Generated738.BaseClass1>
		callvirt instance string class Generated738.G3_C1210`1<class Generated738.BaseClass1>::ClassMethod3806<object>()
		ldstr "G3_C1210::ClassMethod3806.14906<System.Object>()"
		ldstr "class G3_C1210`1<class Generated738.BaseClass1> on type class Generated738.G3_C1210`1<class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated738.G3_C1210`1<class Generated738.BaseClass1>
		callvirt instance string class Generated738.G3_C1210`1<class Generated738.BaseClass1>::Method7<object>()
		ldstr "G3_C1210::Method7.14905<System.Object>()"
		ldstr "class G3_C1210`1<class Generated738.BaseClass1> on type class Generated738.G3_C1210`1<class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated738.G3_C1210`1<class Generated738.BaseClass1>
		callvirt instance string class Generated738.G3_C1210`1<class Generated738.BaseClass1>::ClassMethod1859()
		ldstr "G3_C1210::ClassMethod1859.MI.14907()"
		ldstr "class G3_C1210`1<class Generated738.BaseClass1> on type class Generated738.G3_C1210`1<class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated738.G3_C1210`1<class Generated738.BaseClass1>
		callvirt instance string class Generated738.G3_C1210`1<class Generated738.BaseClass1>::Method6<object>()
		ldstr "G2_C262::Method6.7214<System.Object>()"
		ldstr "class G3_C1210`1<class Generated738.BaseClass1> on type class Generated738.G3_C1210`1<class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated738.G3_C1210`1<class Generated738.BaseClass1>
		callvirt instance string class Generated738.G3_C1210`1<class Generated738.BaseClass1>::Method5()
		ldstr "G2_C262::Method5.7213()"
		ldstr "class G3_C1210`1<class Generated738.BaseClass1> on type class Generated738.G3_C1210`1<class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated738.G3_C1210`1<class Generated738.BaseClass1>
		callvirt instance string class Generated738.G3_C1210`1<class Generated738.BaseClass1>::Method4()
		ldstr "G2_C262::Method4.7211()"
		ldstr "class G3_C1210`1<class Generated738.BaseClass1> on type class Generated738.G3_C1210`1<class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated738.G3_C1210`1<class Generated738.BaseClass1>
		callvirt instance string class Generated738.G3_C1210`1<class Generated738.BaseClass1>::ClassMethod1325<object>()
		ldstr "G1_C5::ClassMethod1325.4807<System.Object>()"
		ldstr "class G3_C1210`1<class Generated738.BaseClass1> on type class Generated738.G3_C1210`1<class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated738.G3_C1210`1<class Generated738.BaseClass1>
		callvirt instance string class Generated738.G3_C1210`1<class Generated738.BaseClass1>::ClassMethod1324()
		ldstr "G1_C5::ClassMethod1324.4806()"
		ldstr "class G3_C1210`1<class Generated738.BaseClass1> on type class Generated738.G3_C1210`1<class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass0>
		callvirt instance string class Generated738.G1_C5`1<class Generated738.BaseClass0>::ClassMethod1325<object>()
		ldstr "G1_C5::ClassMethod1325.4807<System.Object>()"
		ldstr "class G1_C5`1<class Generated738.BaseClass0> on type class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass0>
		callvirt instance string class Generated738.G1_C5`1<class Generated738.BaseClass0>::ClassMethod1324()
		ldstr "G1_C5::ClassMethod1324.4806()"
		ldstr "class G1_C5`1<class Generated738.BaseClass0> on type class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass0>
		callvirt instance string class Generated738.G1_C5`1<class Generated738.BaseClass0>::Method6<object>()
		ldstr "G2_C262::Method6.7214<System.Object>()"
		ldstr "class G1_C5`1<class Generated738.BaseClass0> on type class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass0>
		callvirt instance string class Generated738.G1_C5`1<class Generated738.BaseClass0>::Method5()
		ldstr "G2_C262::Method5.7213()"
		ldstr "class G1_C5`1<class Generated738.BaseClass0> on type class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass0>
		callvirt instance string class Generated738.G1_C5`1<class Generated738.BaseClass0>::Method4()
		ldstr "G1_C5::Method4.4802()"
		ldstr "class G1_C5`1<class Generated738.BaseClass0> on type class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass0>
		callvirt instance string class Generated738.G1_C5`1<class Generated738.BaseClass0>::Method7<object>()
		ldstr "G2_C262::Method7.7216<System.Object>()"
		ldstr "class G1_C5`1<class Generated738.BaseClass0> on type class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated738.IBase2`2<class Generated738.BaseClass1,class Generated738.BaseClass1>::Method7<object>()
		ldstr "G2_C262::Method7.MI.7217<System.Object>()"
		ldstr "class IBase2`2<class Generated738.BaseClass1,class Generated738.BaseClass1> on type class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated738.IBase1`1<class Generated738.BaseClass0>::Method4()
		ldstr "G2_C262::Method4.MI.7212()"
		ldstr "class IBase1`1<class Generated738.BaseClass0> on type class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated738.IBase1`1<class Generated738.BaseClass0>::Method5()
		ldstr "G2_C262::Method5.7213()"
		ldstr "class IBase1`1<class Generated738.BaseClass0> on type class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated738.IBase1`1<class Generated738.BaseClass0>::Method6<object>()
		ldstr "G2_C262::Method6.MI.7215<System.Object>()"
		ldstr "class IBase1`1<class Generated738.BaseClass0> on type class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated738.IBase2`2<class Generated738.BaseClass0,class Generated738.BaseClass1>::Method7<object>()
		ldstr "G2_C262::Method7.MI.7217<System.Object>()"
		ldstr "class IBase2`2<class Generated738.BaseClass0,class Generated738.BaseClass1> on type class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>
		callvirt instance string class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>::ClassMethod1859()
		ldstr "G2_C262::ClassMethod1859.7218()"
		ldstr "class G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0> on type class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>
		callvirt instance string class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>::Method7<object>()
		ldstr "G2_C262::Method7.7216<System.Object>()"
		ldstr "class G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0> on type class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>
		callvirt instance string class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>::Method6<object>()
		ldstr "G2_C262::Method6.7214<System.Object>()"
		ldstr "class G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0> on type class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>
		callvirt instance string class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>::Method5()
		ldstr "G2_C262::Method5.7213()"
		ldstr "class G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0> on type class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>
		callvirt instance string class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>::Method4()
		ldstr "G2_C262::Method4.7211()"
		ldstr "class G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0> on type class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>
		callvirt instance string class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>::ClassMethod1325<object>()
		ldstr "G1_C5::ClassMethod1325.4807<System.Object>()"
		ldstr "class G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0> on type class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>
		callvirt instance string class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>::ClassMethod1324()
		ldstr "G1_C5::ClassMethod1324.4806()"
		ldstr "class G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0> on type class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated738.IBase1`1<class Generated738.BaseClass1>::Method4()
		ldstr "G2_C262::Method4.MI.7212()"
		ldstr "class IBase1`1<class Generated738.BaseClass1> on type class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated738.IBase1`1<class Generated738.BaseClass1>::Method5()
		ldstr "G2_C262::Method5.7213()"
		ldstr "class IBase1`1<class Generated738.BaseClass1> on type class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated738.IBase1`1<class Generated738.BaseClass1>::Method6<object>()
		ldstr "G2_C262::Method6.MI.7215<System.Object>()"
		ldstr "class IBase1`1<class Generated738.BaseClass1> on type class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated738.IBase2`2<class Generated738.BaseClass1,class Generated738.BaseClass0>::Method7<object>()
		ldstr "G2_C262::Method7.MI.7217<System.Object>()"
		ldstr "class IBase2`2<class Generated738.BaseClass1,class Generated738.BaseClass0> on type class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated738.IBase2`2<class Generated738.BaseClass0,class Generated738.BaseClass0>::Method7<object>()
		ldstr "G2_C262::Method7.MI.7217<System.Object>()"
		ldstr "class IBase2`2<class Generated738.BaseClass0,class Generated738.BaseClass0> on type class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass0>
		callvirt instance string class Generated738.G1_C5`1<class Generated738.BaseClass0>::ClassMethod1325<object>()
		ldstr "G1_C5::ClassMethod1325.4807<System.Object>()"
		ldstr "class G1_C5`1<class Generated738.BaseClass0> on type class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass0>
		callvirt instance string class Generated738.G1_C5`1<class Generated738.BaseClass0>::ClassMethod1324()
		ldstr "G1_C5::ClassMethod1324.4806()"
		ldstr "class G1_C5`1<class Generated738.BaseClass0> on type class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass0>
		callvirt instance string class Generated738.G1_C5`1<class Generated738.BaseClass0>::Method6<object>()
		ldstr "G2_C262::Method6.7214<System.Object>()"
		ldstr "class G1_C5`1<class Generated738.BaseClass0> on type class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass0>
		callvirt instance string class Generated738.G1_C5`1<class Generated738.BaseClass0>::Method5()
		ldstr "G2_C262::Method5.7213()"
		ldstr "class G1_C5`1<class Generated738.BaseClass0> on type class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass0>
		callvirt instance string class Generated738.G1_C5`1<class Generated738.BaseClass0>::Method4()
		ldstr "G1_C5::Method4.4802()"
		ldstr "class G1_C5`1<class Generated738.BaseClass0> on type class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass0>
		callvirt instance string class Generated738.G1_C5`1<class Generated738.BaseClass0>::Method7<object>()
		ldstr "G2_C262::Method7.7216<System.Object>()"
		ldstr "class G1_C5`1<class Generated738.BaseClass0> on type class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated738.IBase2`2<class Generated738.BaseClass1,class Generated738.BaseClass1>::Method7<object>()
		ldstr "G2_C262::Method7.MI.7217<System.Object>()"
		ldstr "class IBase2`2<class Generated738.BaseClass1,class Generated738.BaseClass1> on type class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated738.IBase1`1<class Generated738.BaseClass0>::Method4()
		ldstr "G2_C262::Method4.MI.7212()"
		ldstr "class IBase1`1<class Generated738.BaseClass0> on type class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated738.IBase1`1<class Generated738.BaseClass0>::Method5()
		ldstr "G2_C262::Method5.7213()"
		ldstr "class IBase1`1<class Generated738.BaseClass0> on type class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated738.IBase1`1<class Generated738.BaseClass0>::Method6<object>()
		ldstr "G2_C262::Method6.MI.7215<System.Object>()"
		ldstr "class IBase1`1<class Generated738.BaseClass0> on type class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated738.IBase2`2<class Generated738.BaseClass0,class Generated738.BaseClass1>::Method7<object>()
		ldstr "G2_C262::Method7.MI.7217<System.Object>()"
		ldstr "class IBase2`2<class Generated738.BaseClass0,class Generated738.BaseClass1> on type class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>
		callvirt instance string class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>::ClassMethod1859()
		ldstr "G2_C262::ClassMethod1859.7218()"
		ldstr "class G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1> on type class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>
		callvirt instance string class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>::Method7<object>()
		ldstr "G2_C262::Method7.7216<System.Object>()"
		ldstr "class G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1> on type class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>
		callvirt instance string class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>::Method6<object>()
		ldstr "G2_C262::Method6.7214<System.Object>()"
		ldstr "class G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1> on type class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>
		callvirt instance string class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>::Method5()
		ldstr "G2_C262::Method5.7213()"
		ldstr "class G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1> on type class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>
		callvirt instance string class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>::Method4()
		ldstr "G2_C262::Method4.7211()"
		ldstr "class G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1> on type class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>
		callvirt instance string class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>::ClassMethod1325<object>()
		ldstr "G1_C5::ClassMethod1325.4807<System.Object>()"
		ldstr "class G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1> on type class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>
		callvirt instance string class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>::ClassMethod1324()
		ldstr "G1_C5::ClassMethod1324.4806()"
		ldstr "class G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1> on type class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated738.IBase1`1<class Generated738.BaseClass1>::Method4()
		ldstr "G2_C262::Method4.MI.7212()"
		ldstr "class IBase1`1<class Generated738.BaseClass1> on type class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated738.IBase1`1<class Generated738.BaseClass1>::Method5()
		ldstr "G2_C262::Method5.7213()"
		ldstr "class IBase1`1<class Generated738.BaseClass1> on type class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated738.IBase1`1<class Generated738.BaseClass1>::Method6<object>()
		ldstr "G2_C262::Method6.MI.7215<System.Object>()"
		ldstr "class IBase1`1<class Generated738.BaseClass1> on type class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass1>
		callvirt instance string class Generated738.G1_C5`1<class Generated738.BaseClass1>::ClassMethod1325<object>()
		ldstr "G1_C5::ClassMethod1325.4807<System.Object>()"
		ldstr "class G1_C5`1<class Generated738.BaseClass1> on type class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass1>
		callvirt instance string class Generated738.G1_C5`1<class Generated738.BaseClass1>::ClassMethod1324()
		ldstr "G1_C5::ClassMethod1324.4806()"
		ldstr "class G1_C5`1<class Generated738.BaseClass1> on type class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass1>
		callvirt instance string class Generated738.G1_C5`1<class Generated738.BaseClass1>::Method6<object>()
		ldstr "G2_C262::Method6.7214<System.Object>()"
		ldstr "class G1_C5`1<class Generated738.BaseClass1> on type class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass1>
		callvirt instance string class Generated738.G1_C5`1<class Generated738.BaseClass1>::Method5()
		ldstr "G2_C262::Method5.7213()"
		ldstr "class G1_C5`1<class Generated738.BaseClass1> on type class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass1>
		callvirt instance string class Generated738.G1_C5`1<class Generated738.BaseClass1>::Method4()
		ldstr "G1_C5::Method4.4802()"
		ldstr "class G1_C5`1<class Generated738.BaseClass1> on type class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass1>
		callvirt instance string class Generated738.G1_C5`1<class Generated738.BaseClass1>::Method7<object>()
		ldstr "G2_C262::Method7.7216<System.Object>()"
		ldstr "class G1_C5`1<class Generated738.BaseClass1> on type class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated738.IBase2`2<class Generated738.BaseClass1,class Generated738.BaseClass1>::Method7<object>()
		ldstr "G2_C262::Method7.MI.7217<System.Object>()"
		ldstr "class IBase2`2<class Generated738.BaseClass1,class Generated738.BaseClass1> on type class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated738.IBase1`1<class Generated738.BaseClass1>::Method4()
		ldstr "G2_C262::Method4.MI.7212()"
		ldstr "class IBase1`1<class Generated738.BaseClass1> on type class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated738.IBase1`1<class Generated738.BaseClass1>::Method5()
		ldstr "G2_C262::Method5.7213()"
		ldstr "class IBase1`1<class Generated738.BaseClass1> on type class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated738.IBase1`1<class Generated738.BaseClass1>::Method6<object>()
		ldstr "G2_C262::Method6.MI.7215<System.Object>()"
		ldstr "class IBase1`1<class Generated738.BaseClass1> on type class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated738.IBase2`2<class Generated738.BaseClass0,class Generated738.BaseClass1>::Method7<object>()
		ldstr "G2_C262::Method7.MI.7217<System.Object>()"
		ldstr "class IBase2`2<class Generated738.BaseClass0,class Generated738.BaseClass1> on type class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated738.IBase1`1<class Generated738.BaseClass0>::Method4()
		ldstr "G2_C262::Method4.MI.7212()"
		ldstr "class IBase1`1<class Generated738.BaseClass0> on type class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated738.IBase1`1<class Generated738.BaseClass0>::Method5()
		ldstr "G2_C262::Method5.7213()"
		ldstr "class IBase1`1<class Generated738.BaseClass0> on type class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated738.IBase1`1<class Generated738.BaseClass0>::Method6<object>()
		ldstr "G2_C262::Method6.MI.7215<System.Object>()"
		ldstr "class IBase1`1<class Generated738.BaseClass0> on type class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>
		callvirt instance string class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>::ClassMethod1859()
		ldstr "G2_C262::ClassMethod1859.7218()"
		ldstr "class G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0> on type class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>
		callvirt instance string class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>::Method7<object>()
		ldstr "G2_C262::Method7.7216<System.Object>()"
		ldstr "class G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0> on type class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>
		callvirt instance string class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>::Method6<object>()
		ldstr "G2_C262::Method6.7214<System.Object>()"
		ldstr "class G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0> on type class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>
		callvirt instance string class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>::Method5()
		ldstr "G2_C262::Method5.7213()"
		ldstr "class G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0> on type class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>
		callvirt instance string class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>::Method4()
		ldstr "G2_C262::Method4.7211()"
		ldstr "class G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0> on type class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>
		callvirt instance string class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>::ClassMethod1325<object>()
		ldstr "G1_C5::ClassMethod1325.4807<System.Object>()"
		ldstr "class G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0> on type class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>
		callvirt instance string class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>::ClassMethod1324()
		ldstr "G1_C5::ClassMethod1324.4806()"
		ldstr "class G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0> on type class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated738.IBase2`2<class Generated738.BaseClass1,class Generated738.BaseClass0>::Method7<object>()
		ldstr "G2_C262::Method7.MI.7217<System.Object>()"
		ldstr "class IBase2`2<class Generated738.BaseClass1,class Generated738.BaseClass0> on type class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated738.IBase2`2<class Generated738.BaseClass0,class Generated738.BaseClass0>::Method7<object>()
		ldstr "G2_C262::Method7.MI.7217<System.Object>()"
		ldstr "class IBase2`2<class Generated738.BaseClass0,class Generated738.BaseClass0> on type class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass1>
		callvirt instance string class Generated738.G1_C5`1<class Generated738.BaseClass1>::ClassMethod1325<object>()
		ldstr "G1_C5::ClassMethod1325.4807<System.Object>()"
		ldstr "class G1_C5`1<class Generated738.BaseClass1> on type class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass1>
		callvirt instance string class Generated738.G1_C5`1<class Generated738.BaseClass1>::ClassMethod1324()
		ldstr "G1_C5::ClassMethod1324.4806()"
		ldstr "class G1_C5`1<class Generated738.BaseClass1> on type class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass1>
		callvirt instance string class Generated738.G1_C5`1<class Generated738.BaseClass1>::Method6<object>()
		ldstr "G2_C262::Method6.7214<System.Object>()"
		ldstr "class G1_C5`1<class Generated738.BaseClass1> on type class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass1>
		callvirt instance string class Generated738.G1_C5`1<class Generated738.BaseClass1>::Method5()
		ldstr "G2_C262::Method5.7213()"
		ldstr "class G1_C5`1<class Generated738.BaseClass1> on type class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass1>
		callvirt instance string class Generated738.G1_C5`1<class Generated738.BaseClass1>::Method4()
		ldstr "G1_C5::Method4.4802()"
		ldstr "class G1_C5`1<class Generated738.BaseClass1> on type class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass1>
		callvirt instance string class Generated738.G1_C5`1<class Generated738.BaseClass1>::Method7<object>()
		ldstr "G2_C262::Method7.7216<System.Object>()"
		ldstr "class G1_C5`1<class Generated738.BaseClass1> on type class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated738.IBase2`2<class Generated738.BaseClass1,class Generated738.BaseClass1>::Method7<object>()
		ldstr "G2_C262::Method7.MI.7217<System.Object>()"
		ldstr "class IBase2`2<class Generated738.BaseClass1,class Generated738.BaseClass1> on type class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated738.IBase1`1<class Generated738.BaseClass1>::Method4()
		ldstr "G2_C262::Method4.MI.7212()"
		ldstr "class IBase1`1<class Generated738.BaseClass1> on type class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated738.IBase1`1<class Generated738.BaseClass1>::Method5()
		ldstr "G2_C262::Method5.7213()"
		ldstr "class IBase1`1<class Generated738.BaseClass1> on type class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated738.IBase1`1<class Generated738.BaseClass1>::Method6<object>()
		ldstr "G2_C262::Method6.MI.7215<System.Object>()"
		ldstr "class IBase1`1<class Generated738.BaseClass1> on type class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated738.IBase2`2<class Generated738.BaseClass0,class Generated738.BaseClass1>::Method7<object>()
		ldstr "G2_C262::Method7.MI.7217<System.Object>()"
		ldstr "class IBase2`2<class Generated738.BaseClass0,class Generated738.BaseClass1> on type class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated738.IBase1`1<class Generated738.BaseClass0>::Method4()
		ldstr "G2_C262::Method4.MI.7212()"
		ldstr "class IBase1`1<class Generated738.BaseClass0> on type class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated738.IBase1`1<class Generated738.BaseClass0>::Method5()
		ldstr "G2_C262::Method5.7213()"
		ldstr "class IBase1`1<class Generated738.BaseClass0> on type class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated738.IBase1`1<class Generated738.BaseClass0>::Method6<object>()
		ldstr "G2_C262::Method6.MI.7215<System.Object>()"
		ldstr "class IBase1`1<class Generated738.BaseClass0> on type class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>
		callvirt instance string class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>::ClassMethod1859()
		ldstr "G2_C262::ClassMethod1859.7218()"
		ldstr "class G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1> on type class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>
		callvirt instance string class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>::Method7<object>()
		ldstr "G2_C262::Method7.7216<System.Object>()"
		ldstr "class G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1> on type class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>
		callvirt instance string class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>::Method6<object>()
		ldstr "G2_C262::Method6.7214<System.Object>()"
		ldstr "class G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1> on type class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>
		callvirt instance string class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>::Method5()
		ldstr "G2_C262::Method5.7213()"
		ldstr "class G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1> on type class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>
		callvirt instance string class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>::Method4()
		ldstr "G2_C262::Method4.7211()"
		ldstr "class G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1> on type class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>
		callvirt instance string class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>::ClassMethod1325<object>()
		ldstr "G1_C5::ClassMethod1325.4807<System.Object>()"
		ldstr "class G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1> on type class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>
		callvirt instance string class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>::ClassMethod1324()
		ldstr "G1_C5::ClassMethod1324.4806()"
		ldstr "class G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1> on type class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated738.G1_C5`1<class Generated738.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass0>
		callvirt instance string class Generated738.G1_C5`1<class Generated738.BaseClass0>::ClassMethod1325<object>()
		ldstr "G1_C5::ClassMethod1325.4807<System.Object>()"
		ldstr "class G1_C5`1<class Generated738.BaseClass0> on type class Generated738.G1_C5`1<class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass0>
		callvirt instance string class Generated738.G1_C5`1<class Generated738.BaseClass0>::ClassMethod1324()
		ldstr "G1_C5::ClassMethod1324.4806()"
		ldstr "class G1_C5`1<class Generated738.BaseClass0> on type class Generated738.G1_C5`1<class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass0>
		callvirt instance string class Generated738.G1_C5`1<class Generated738.BaseClass0>::Method6<object>()
		ldstr "G1_C5::Method6.4804<System.Object>()"
		ldstr "class G1_C5`1<class Generated738.BaseClass0> on type class Generated738.G1_C5`1<class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass0>
		callvirt instance string class Generated738.G1_C5`1<class Generated738.BaseClass0>::Method5()
		ldstr "G1_C5::Method5.4803()"
		ldstr "class G1_C5`1<class Generated738.BaseClass0> on type class Generated738.G1_C5`1<class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass0>
		callvirt instance string class Generated738.G1_C5`1<class Generated738.BaseClass0>::Method4()
		ldstr "G1_C5::Method4.4802()"
		ldstr "class G1_C5`1<class Generated738.BaseClass0> on type class Generated738.G1_C5`1<class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass0>
		callvirt instance string class Generated738.G1_C5`1<class Generated738.BaseClass0>::Method7<object>()
		ldstr "G1_C5::Method7.4801<System.Object>()"
		ldstr "class G1_C5`1<class Generated738.BaseClass0> on type class Generated738.G1_C5`1<class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated738.IBase2`2<class Generated738.BaseClass1,class Generated738.BaseClass1>::Method7<object>()
		ldstr "G1_C5::Method7.4801<System.Object>()"
		ldstr "class IBase2`2<class Generated738.BaseClass1,class Generated738.BaseClass1> on type class Generated738.G1_C5`1<class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated738.IBase1`1<class Generated738.BaseClass0>::Method4()
		ldstr "G1_C5::Method4.4802()"
		ldstr "class IBase1`1<class Generated738.BaseClass0> on type class Generated738.G1_C5`1<class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated738.IBase1`1<class Generated738.BaseClass0>::Method5()
		ldstr "G1_C5::Method5.4803()"
		ldstr "class IBase1`1<class Generated738.BaseClass0> on type class Generated738.G1_C5`1<class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated738.IBase1`1<class Generated738.BaseClass0>::Method6<object>()
		ldstr "G1_C5::Method6.MI.4805<System.Object>()"
		ldstr "class IBase1`1<class Generated738.BaseClass0> on type class Generated738.G1_C5`1<class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated738.IBase2`2<class Generated738.BaseClass0,class Generated738.BaseClass1>::Method7<object>()
		ldstr "G1_C5::Method7.4801<System.Object>()"
		ldstr "class IBase2`2<class Generated738.BaseClass0,class Generated738.BaseClass1> on type class Generated738.G1_C5`1<class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated738.G1_C5`1<class Generated738.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass1>
		callvirt instance string class Generated738.G1_C5`1<class Generated738.BaseClass1>::ClassMethod1325<object>()
		ldstr "G1_C5::ClassMethod1325.4807<System.Object>()"
		ldstr "class G1_C5`1<class Generated738.BaseClass1> on type class Generated738.G1_C5`1<class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass1>
		callvirt instance string class Generated738.G1_C5`1<class Generated738.BaseClass1>::ClassMethod1324()
		ldstr "G1_C5::ClassMethod1324.4806()"
		ldstr "class G1_C5`1<class Generated738.BaseClass1> on type class Generated738.G1_C5`1<class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass1>
		callvirt instance string class Generated738.G1_C5`1<class Generated738.BaseClass1>::Method6<object>()
		ldstr "G1_C5::Method6.4804<System.Object>()"
		ldstr "class G1_C5`1<class Generated738.BaseClass1> on type class Generated738.G1_C5`1<class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass1>
		callvirt instance string class Generated738.G1_C5`1<class Generated738.BaseClass1>::Method5()
		ldstr "G1_C5::Method5.4803()"
		ldstr "class G1_C5`1<class Generated738.BaseClass1> on type class Generated738.G1_C5`1<class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass1>
		callvirt instance string class Generated738.G1_C5`1<class Generated738.BaseClass1>::Method4()
		ldstr "G1_C5::Method4.4802()"
		ldstr "class G1_C5`1<class Generated738.BaseClass1> on type class Generated738.G1_C5`1<class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass1>
		callvirt instance string class Generated738.G1_C5`1<class Generated738.BaseClass1>::Method7<object>()
		ldstr "G1_C5::Method7.4801<System.Object>()"
		ldstr "class G1_C5`1<class Generated738.BaseClass1> on type class Generated738.G1_C5`1<class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated738.IBase2`2<class Generated738.BaseClass1,class Generated738.BaseClass1>::Method7<object>()
		ldstr "G1_C5::Method7.4801<System.Object>()"
		ldstr "class IBase2`2<class Generated738.BaseClass1,class Generated738.BaseClass1> on type class Generated738.G1_C5`1<class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated738.IBase1`1<class Generated738.BaseClass1>::Method4()
		ldstr "G1_C5::Method4.4802()"
		ldstr "class IBase1`1<class Generated738.BaseClass1> on type class Generated738.G1_C5`1<class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated738.IBase1`1<class Generated738.BaseClass1>::Method5()
		ldstr "G1_C5::Method5.4803()"
		ldstr "class IBase1`1<class Generated738.BaseClass1> on type class Generated738.G1_C5`1<class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated738.IBase1`1<class Generated738.BaseClass1>::Method6<object>()
		ldstr "G1_C5::Method6.MI.4805<System.Object>()"
		ldstr "class IBase1`1<class Generated738.BaseClass1> on type class Generated738.G1_C5`1<class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated738.IBase2`2<class Generated738.BaseClass0,class Generated738.BaseClass1>::Method7<object>()
		ldstr "G1_C5::Method7.4801<System.Object>()"
		ldstr "class IBase2`2<class Generated738.BaseClass0,class Generated738.BaseClass1> on type class Generated738.G1_C5`1<class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated738.IBase1`1<class Generated738.BaseClass0>::Method4()
		ldstr "G1_C5::Method4.4802()"
		ldstr "class IBase1`1<class Generated738.BaseClass0> on type class Generated738.G1_C5`1<class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated738.IBase1`1<class Generated738.BaseClass0>::Method5()
		ldstr "G1_C5::Method5.4803()"
		ldstr "class IBase1`1<class Generated738.BaseClass0> on type class Generated738.G1_C5`1<class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated738.IBase1`1<class Generated738.BaseClass0>::Method6<object>()
		ldstr "G1_C5::Method6.MI.4805<System.Object>()"
		ldstr "class IBase1`1<class Generated738.BaseClass0> on type class Generated738.G1_C5`1<class Generated738.BaseClass1>"
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
		newobj instance void class Generated738.G3_C1210`1<class Generated738.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C5::ClassMethod1324.4806()#G1_C5::ClassMethod1325.4807<System.Object>()#G1_C5::Method4.4802()#G2_C262::Method5.7213()#G2_C262::Method6.7214<System.Object>()#G3_C1210::Method7.14905<System.Object>()#"
		call void Generated738.Generated738::M.G1_C5.T<class Generated738.BaseClass0,class Generated738.G3_C1210`1<class Generated738.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C5::ClassMethod1324.4806()#G1_C5::ClassMethod1325.4807<System.Object>()#G1_C5::Method4.4802()#G2_C262::Method5.7213()#G2_C262::Method6.7214<System.Object>()#G3_C1210::Method7.14905<System.Object>()#"
		call void Generated738.Generated738::M.G1_C5.A<class Generated738.G3_C1210`1<class Generated738.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C262::Method7.MI.7217<System.Object>()#"
		call void Generated738.Generated738::M.IBase2.T.T<class Generated738.BaseClass1,class Generated738.BaseClass1,class Generated738.G3_C1210`1<class Generated738.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C262::Method7.MI.7217<System.Object>()#"
		call void Generated738.Generated738::M.IBase2.B.T<class Generated738.BaseClass1,class Generated738.G3_C1210`1<class Generated738.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C262::Method7.MI.7217<System.Object>()#"
		call void Generated738.Generated738::M.IBase2.B.B<class Generated738.G3_C1210`1<class Generated738.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C262::Method4.MI.7212()#G2_C262::Method5.7213()#G2_C262::Method6.MI.7215<System.Object>()#"
		call void Generated738.Generated738::M.IBase1.T<class Generated738.BaseClass0,class Generated738.G3_C1210`1<class Generated738.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C262::Method4.MI.7212()#G2_C262::Method5.7213()#G2_C262::Method6.MI.7215<System.Object>()#"
		call void Generated738.Generated738::M.IBase1.A<class Generated738.G3_C1210`1<class Generated738.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1210::Method7.14905<System.Object>()#"
		call void Generated738.Generated738::M.IBase2.T.T<class Generated738.BaseClass0,class Generated738.BaseClass1,class Generated738.G3_C1210`1<class Generated738.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1210::Method7.14905<System.Object>()#"
		call void Generated738.Generated738::M.IBase2.A.T<class Generated738.BaseClass1,class Generated738.G3_C1210`1<class Generated738.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1210::Method7.14905<System.Object>()#"
		call void Generated738.Generated738::M.IBase2.A.B<class Generated738.G3_C1210`1<class Generated738.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C5::ClassMethod1324.4806()#G1_C5::ClassMethod1325.4807<System.Object>()#G3_C1210::ClassMethod1859.MI.14907()#G2_C262::Method4.7211()#G2_C262::Method5.7213()#G2_C262::Method6.7214<System.Object>()#G3_C1210::Method7.14905<System.Object>()#"
		call void Generated738.Generated738::M.G2_C262.T.T<class Generated738.BaseClass0,class Generated738.BaseClass1,class Generated738.G3_C1210`1<class Generated738.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C5::ClassMethod1324.4806()#G1_C5::ClassMethod1325.4807<System.Object>()#G3_C1210::ClassMethod1859.MI.14907()#G2_C262::Method4.7211()#G2_C262::Method5.7213()#G2_C262::Method6.7214<System.Object>()#G3_C1210::Method7.14905<System.Object>()#"
		call void Generated738.Generated738::M.G2_C262.A.T<class Generated738.BaseClass1,class Generated738.G3_C1210`1<class Generated738.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C5::ClassMethod1324.4806()#G1_C5::ClassMethod1325.4807<System.Object>()#G3_C1210::ClassMethod1859.MI.14907()#G2_C262::Method4.7211()#G2_C262::Method5.7213()#G2_C262::Method6.7214<System.Object>()#G3_C1210::Method7.14905<System.Object>()#"
		call void Generated738.Generated738::M.G2_C262.A.B<class Generated738.G3_C1210`1<class Generated738.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C262::Method4.MI.7212()#G2_C262::Method5.7213()#G2_C262::Method6.MI.7215<System.Object>()#"
		call void Generated738.Generated738::M.IBase1.T<class Generated738.BaseClass1,class Generated738.G3_C1210`1<class Generated738.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C262::Method4.MI.7212()#G2_C262::Method5.7213()#G2_C262::Method6.MI.7215<System.Object>()#"
		call void Generated738.Generated738::M.IBase1.B<class Generated738.G3_C1210`1<class Generated738.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C5::ClassMethod1324.4806()#G1_C5::ClassMethod1325.4807<System.Object>()#G3_C1210::ClassMethod1859.MI.14907()#G3_C1210::ClassMethod3806.14906<System.Object>()#G2_C262::Method4.7211()#G2_C262::Method5.7213()#G2_C262::Method6.7214<System.Object>()#G3_C1210::Method7.14905<System.Object>()#"
		call void Generated738.Generated738::M.G3_C1210.T<class Generated738.BaseClass0,class Generated738.G3_C1210`1<class Generated738.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C5::ClassMethod1324.4806()#G1_C5::ClassMethod1325.4807<System.Object>()#G3_C1210::ClassMethod1859.MI.14907()#G3_C1210::ClassMethod3806.14906<System.Object>()#G2_C262::Method4.7211()#G2_C262::Method5.7213()#G2_C262::Method6.7214<System.Object>()#G3_C1210::Method7.14905<System.Object>()#"
		call void Generated738.Generated738::M.G3_C1210.A<class Generated738.G3_C1210`1<class Generated738.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1210::Method7.14905<System.Object>()#"
		call void Generated738.Generated738::M.IBase2.T.T<class Generated738.BaseClass0,class Generated738.BaseClass0,class Generated738.G3_C1210`1<class Generated738.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1210::Method7.14905<System.Object>()#"
		call void Generated738.Generated738::M.IBase2.A.T<class Generated738.BaseClass0,class Generated738.G3_C1210`1<class Generated738.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1210::Method7.14905<System.Object>()#"
		call void Generated738.Generated738::M.IBase2.A.A<class Generated738.G3_C1210`1<class Generated738.BaseClass0>>(!!0,string)
		newobj instance void class Generated738.G3_C1210`1<class Generated738.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C5::ClassMethod1324.4806()#G1_C5::ClassMethod1325.4807<System.Object>()#G1_C5::Method4.4802()#G2_C262::Method5.7213()#G2_C262::Method6.7214<System.Object>()#G3_C1210::Method7.14905<System.Object>()#"
		call void Generated738.Generated738::M.G1_C5.T<class Generated738.BaseClass1,class Generated738.G3_C1210`1<class Generated738.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C5::ClassMethod1324.4806()#G1_C5::ClassMethod1325.4807<System.Object>()#G1_C5::Method4.4802()#G2_C262::Method5.7213()#G2_C262::Method6.7214<System.Object>()#G3_C1210::Method7.14905<System.Object>()#"
		call void Generated738.Generated738::M.G1_C5.B<class Generated738.G3_C1210`1<class Generated738.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C262::Method7.MI.7217<System.Object>()#"
		call void Generated738.Generated738::M.IBase2.T.T<class Generated738.BaseClass1,class Generated738.BaseClass1,class Generated738.G3_C1210`1<class Generated738.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C262::Method7.MI.7217<System.Object>()#"
		call void Generated738.Generated738::M.IBase2.B.T<class Generated738.BaseClass1,class Generated738.G3_C1210`1<class Generated738.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C262::Method7.MI.7217<System.Object>()#"
		call void Generated738.Generated738::M.IBase2.B.B<class Generated738.G3_C1210`1<class Generated738.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C262::Method4.MI.7212()#G2_C262::Method5.7213()#G2_C262::Method6.MI.7215<System.Object>()#"
		call void Generated738.Generated738::M.IBase1.T<class Generated738.BaseClass1,class Generated738.G3_C1210`1<class Generated738.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C262::Method4.MI.7212()#G2_C262::Method5.7213()#G2_C262::Method6.MI.7215<System.Object>()#"
		call void Generated738.Generated738::M.IBase1.B<class Generated738.G3_C1210`1<class Generated738.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1210::Method7.14905<System.Object>()#"
		call void Generated738.Generated738::M.IBase2.T.T<class Generated738.BaseClass0,class Generated738.BaseClass1,class Generated738.G3_C1210`1<class Generated738.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1210::Method7.14905<System.Object>()#"
		call void Generated738.Generated738::M.IBase2.A.T<class Generated738.BaseClass1,class Generated738.G3_C1210`1<class Generated738.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1210::Method7.14905<System.Object>()#"
		call void Generated738.Generated738::M.IBase2.A.B<class Generated738.G3_C1210`1<class Generated738.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C262::Method4.MI.7212()#G2_C262::Method5.7213()#G2_C262::Method6.MI.7215<System.Object>()#"
		call void Generated738.Generated738::M.IBase1.T<class Generated738.BaseClass0,class Generated738.G3_C1210`1<class Generated738.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C262::Method4.MI.7212()#G2_C262::Method5.7213()#G2_C262::Method6.MI.7215<System.Object>()#"
		call void Generated738.Generated738::M.IBase1.A<class Generated738.G3_C1210`1<class Generated738.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C5::ClassMethod1324.4806()#G1_C5::ClassMethod1325.4807<System.Object>()#G3_C1210::ClassMethod1859.MI.14907()#G2_C262::Method4.7211()#G2_C262::Method5.7213()#G2_C262::Method6.7214<System.Object>()#G3_C1210::Method7.14905<System.Object>()#"
		call void Generated738.Generated738::M.G2_C262.T.T<class Generated738.BaseClass1,class Generated738.BaseClass1,class Generated738.G3_C1210`1<class Generated738.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C5::ClassMethod1324.4806()#G1_C5::ClassMethod1325.4807<System.Object>()#G3_C1210::ClassMethod1859.MI.14907()#G2_C262::Method4.7211()#G2_C262::Method5.7213()#G2_C262::Method6.7214<System.Object>()#G3_C1210::Method7.14905<System.Object>()#"
		call void Generated738.Generated738::M.G2_C262.B.T<class Generated738.BaseClass1,class Generated738.G3_C1210`1<class Generated738.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C5::ClassMethod1324.4806()#G1_C5::ClassMethod1325.4807<System.Object>()#G3_C1210::ClassMethod1859.MI.14907()#G2_C262::Method4.7211()#G2_C262::Method5.7213()#G2_C262::Method6.7214<System.Object>()#G3_C1210::Method7.14905<System.Object>()#"
		call void Generated738.Generated738::M.G2_C262.B.B<class Generated738.G3_C1210`1<class Generated738.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C5::ClassMethod1324.4806()#G1_C5::ClassMethod1325.4807<System.Object>()#G3_C1210::ClassMethod1859.MI.14907()#G3_C1210::ClassMethod3806.14906<System.Object>()#G2_C262::Method4.7211()#G2_C262::Method5.7213()#G2_C262::Method6.7214<System.Object>()#G3_C1210::Method7.14905<System.Object>()#"
		call void Generated738.Generated738::M.G3_C1210.T<class Generated738.BaseClass1,class Generated738.G3_C1210`1<class Generated738.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C5::ClassMethod1324.4806()#G1_C5::ClassMethod1325.4807<System.Object>()#G3_C1210::ClassMethod1859.MI.14907()#G3_C1210::ClassMethod3806.14906<System.Object>()#G2_C262::Method4.7211()#G2_C262::Method5.7213()#G2_C262::Method6.7214<System.Object>()#G3_C1210::Method7.14905<System.Object>()#"
		call void Generated738.Generated738::M.G3_C1210.B<class Generated738.G3_C1210`1<class Generated738.BaseClass1>>(!!0,string)
		newobj instance void class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C5::ClassMethod1324.4806()#G1_C5::ClassMethod1325.4807<System.Object>()#G1_C5::Method4.4802()#G2_C262::Method5.7213()#G2_C262::Method6.7214<System.Object>()#G2_C262::Method7.7216<System.Object>()#"
		call void Generated738.Generated738::M.G1_C5.T<class Generated738.BaseClass0,class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C5::ClassMethod1324.4806()#G1_C5::ClassMethod1325.4807<System.Object>()#G1_C5::Method4.4802()#G2_C262::Method5.7213()#G2_C262::Method6.7214<System.Object>()#G2_C262::Method7.7216<System.Object>()#"
		call void Generated738.Generated738::M.G1_C5.A<class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C262::Method7.MI.7217<System.Object>()#"
		call void Generated738.Generated738::M.IBase2.T.T<class Generated738.BaseClass1,class Generated738.BaseClass1,class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C262::Method7.MI.7217<System.Object>()#"
		call void Generated738.Generated738::M.IBase2.B.T<class Generated738.BaseClass1,class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C262::Method7.MI.7217<System.Object>()#"
		call void Generated738.Generated738::M.IBase2.B.B<class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C262::Method4.MI.7212()#G2_C262::Method5.7213()#G2_C262::Method6.MI.7215<System.Object>()#"
		call void Generated738.Generated738::M.IBase1.T<class Generated738.BaseClass0,class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C262::Method4.MI.7212()#G2_C262::Method5.7213()#G2_C262::Method6.MI.7215<System.Object>()#"
		call void Generated738.Generated738::M.IBase1.A<class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C262::Method7.MI.7217<System.Object>()#"
		call void Generated738.Generated738::M.IBase2.T.T<class Generated738.BaseClass0,class Generated738.BaseClass1,class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C262::Method7.MI.7217<System.Object>()#"
		call void Generated738.Generated738::M.IBase2.A.T<class Generated738.BaseClass1,class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C262::Method7.MI.7217<System.Object>()#"
		call void Generated738.Generated738::M.IBase2.A.B<class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C5::ClassMethod1324.4806()#G1_C5::ClassMethod1325.4807<System.Object>()#G2_C262::ClassMethod1859.7218()#G2_C262::Method4.7211()#G2_C262::Method5.7213()#G2_C262::Method6.7214<System.Object>()#G2_C262::Method7.7216<System.Object>()#"
		call void Generated738.Generated738::M.G2_C262.T.T<class Generated738.BaseClass0,class Generated738.BaseClass0,class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C5::ClassMethod1324.4806()#G1_C5::ClassMethod1325.4807<System.Object>()#G2_C262::ClassMethod1859.7218()#G2_C262::Method4.7211()#G2_C262::Method5.7213()#G2_C262::Method6.7214<System.Object>()#G2_C262::Method7.7216<System.Object>()#"
		call void Generated738.Generated738::M.G2_C262.A.T<class Generated738.BaseClass0,class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C5::ClassMethod1324.4806()#G1_C5::ClassMethod1325.4807<System.Object>()#G2_C262::ClassMethod1859.7218()#G2_C262::Method4.7211()#G2_C262::Method5.7213()#G2_C262::Method6.7214<System.Object>()#G2_C262::Method7.7216<System.Object>()#"
		call void Generated738.Generated738::M.G2_C262.A.A<class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C262::Method4.MI.7212()#G2_C262::Method5.7213()#G2_C262::Method6.MI.7215<System.Object>()#"
		call void Generated738.Generated738::M.IBase1.T<class Generated738.BaseClass1,class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C262::Method4.MI.7212()#G2_C262::Method5.7213()#G2_C262::Method6.MI.7215<System.Object>()#"
		call void Generated738.Generated738::M.IBase1.B<class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C262::Method7.MI.7217<System.Object>()#"
		call void Generated738.Generated738::M.IBase2.T.T<class Generated738.BaseClass1,class Generated738.BaseClass0,class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C262::Method7.MI.7217<System.Object>()#"
		call void Generated738.Generated738::M.IBase2.B.T<class Generated738.BaseClass0,class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C262::Method7.MI.7217<System.Object>()#"
		call void Generated738.Generated738::M.IBase2.B.A<class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C262::Method7.MI.7217<System.Object>()#"
		call void Generated738.Generated738::M.IBase2.T.T<class Generated738.BaseClass0,class Generated738.BaseClass0,class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C262::Method7.MI.7217<System.Object>()#"
		call void Generated738.Generated738::M.IBase2.A.T<class Generated738.BaseClass0,class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C262::Method7.MI.7217<System.Object>()#"
		call void Generated738.Generated738::M.IBase2.A.A<class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>>(!!0,string)
		newobj instance void class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C5::ClassMethod1324.4806()#G1_C5::ClassMethod1325.4807<System.Object>()#G1_C5::Method4.4802()#G2_C262::Method5.7213()#G2_C262::Method6.7214<System.Object>()#G2_C262::Method7.7216<System.Object>()#"
		call void Generated738.Generated738::M.G1_C5.T<class Generated738.BaseClass0,class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C5::ClassMethod1324.4806()#G1_C5::ClassMethod1325.4807<System.Object>()#G1_C5::Method4.4802()#G2_C262::Method5.7213()#G2_C262::Method6.7214<System.Object>()#G2_C262::Method7.7216<System.Object>()#"
		call void Generated738.Generated738::M.G1_C5.A<class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C262::Method7.MI.7217<System.Object>()#"
		call void Generated738.Generated738::M.IBase2.T.T<class Generated738.BaseClass1,class Generated738.BaseClass1,class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C262::Method7.MI.7217<System.Object>()#"
		call void Generated738.Generated738::M.IBase2.B.T<class Generated738.BaseClass1,class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C262::Method7.MI.7217<System.Object>()#"
		call void Generated738.Generated738::M.IBase2.B.B<class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C262::Method4.MI.7212()#G2_C262::Method5.7213()#G2_C262::Method6.MI.7215<System.Object>()#"
		call void Generated738.Generated738::M.IBase1.T<class Generated738.BaseClass0,class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C262::Method4.MI.7212()#G2_C262::Method5.7213()#G2_C262::Method6.MI.7215<System.Object>()#"
		call void Generated738.Generated738::M.IBase1.A<class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C262::Method7.MI.7217<System.Object>()#"
		call void Generated738.Generated738::M.IBase2.T.T<class Generated738.BaseClass0,class Generated738.BaseClass1,class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C262::Method7.MI.7217<System.Object>()#"
		call void Generated738.Generated738::M.IBase2.A.T<class Generated738.BaseClass1,class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C262::Method7.MI.7217<System.Object>()#"
		call void Generated738.Generated738::M.IBase2.A.B<class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C5::ClassMethod1324.4806()#G1_C5::ClassMethod1325.4807<System.Object>()#G2_C262::ClassMethod1859.7218()#G2_C262::Method4.7211()#G2_C262::Method5.7213()#G2_C262::Method6.7214<System.Object>()#G2_C262::Method7.7216<System.Object>()#"
		call void Generated738.Generated738::M.G2_C262.T.T<class Generated738.BaseClass0,class Generated738.BaseClass1,class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C5::ClassMethod1324.4806()#G1_C5::ClassMethod1325.4807<System.Object>()#G2_C262::ClassMethod1859.7218()#G2_C262::Method4.7211()#G2_C262::Method5.7213()#G2_C262::Method6.7214<System.Object>()#G2_C262::Method7.7216<System.Object>()#"
		call void Generated738.Generated738::M.G2_C262.A.T<class Generated738.BaseClass1,class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C5::ClassMethod1324.4806()#G1_C5::ClassMethod1325.4807<System.Object>()#G2_C262::ClassMethod1859.7218()#G2_C262::Method4.7211()#G2_C262::Method5.7213()#G2_C262::Method6.7214<System.Object>()#G2_C262::Method7.7216<System.Object>()#"
		call void Generated738.Generated738::M.G2_C262.A.B<class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C262::Method4.MI.7212()#G2_C262::Method5.7213()#G2_C262::Method6.MI.7215<System.Object>()#"
		call void Generated738.Generated738::M.IBase1.T<class Generated738.BaseClass1,class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C262::Method4.MI.7212()#G2_C262::Method5.7213()#G2_C262::Method6.MI.7215<System.Object>()#"
		call void Generated738.Generated738::M.IBase1.B<class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>>(!!0,string)
		newobj instance void class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C5::ClassMethod1324.4806()#G1_C5::ClassMethod1325.4807<System.Object>()#G1_C5::Method4.4802()#G2_C262::Method5.7213()#G2_C262::Method6.7214<System.Object>()#G2_C262::Method7.7216<System.Object>()#"
		call void Generated738.Generated738::M.G1_C5.T<class Generated738.BaseClass1,class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C5::ClassMethod1324.4806()#G1_C5::ClassMethod1325.4807<System.Object>()#G1_C5::Method4.4802()#G2_C262::Method5.7213()#G2_C262::Method6.7214<System.Object>()#G2_C262::Method7.7216<System.Object>()#"
		call void Generated738.Generated738::M.G1_C5.B<class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C262::Method7.MI.7217<System.Object>()#"
		call void Generated738.Generated738::M.IBase2.T.T<class Generated738.BaseClass1,class Generated738.BaseClass1,class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C262::Method7.MI.7217<System.Object>()#"
		call void Generated738.Generated738::M.IBase2.B.T<class Generated738.BaseClass1,class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C262::Method7.MI.7217<System.Object>()#"
		call void Generated738.Generated738::M.IBase2.B.B<class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C262::Method4.MI.7212()#G2_C262::Method5.7213()#G2_C262::Method6.MI.7215<System.Object>()#"
		call void Generated738.Generated738::M.IBase1.T<class Generated738.BaseClass1,class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C262::Method4.MI.7212()#G2_C262::Method5.7213()#G2_C262::Method6.MI.7215<System.Object>()#"
		call void Generated738.Generated738::M.IBase1.B<class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C262::Method7.MI.7217<System.Object>()#"
		call void Generated738.Generated738::M.IBase2.T.T<class Generated738.BaseClass0,class Generated738.BaseClass1,class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C262::Method7.MI.7217<System.Object>()#"
		call void Generated738.Generated738::M.IBase2.A.T<class Generated738.BaseClass1,class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C262::Method7.MI.7217<System.Object>()#"
		call void Generated738.Generated738::M.IBase2.A.B<class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C262::Method4.MI.7212()#G2_C262::Method5.7213()#G2_C262::Method6.MI.7215<System.Object>()#"
		call void Generated738.Generated738::M.IBase1.T<class Generated738.BaseClass0,class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C262::Method4.MI.7212()#G2_C262::Method5.7213()#G2_C262::Method6.MI.7215<System.Object>()#"
		call void Generated738.Generated738::M.IBase1.A<class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C5::ClassMethod1324.4806()#G1_C5::ClassMethod1325.4807<System.Object>()#G2_C262::ClassMethod1859.7218()#G2_C262::Method4.7211()#G2_C262::Method5.7213()#G2_C262::Method6.7214<System.Object>()#G2_C262::Method7.7216<System.Object>()#"
		call void Generated738.Generated738::M.G2_C262.T.T<class Generated738.BaseClass1,class Generated738.BaseClass0,class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C5::ClassMethod1324.4806()#G1_C5::ClassMethod1325.4807<System.Object>()#G2_C262::ClassMethod1859.7218()#G2_C262::Method4.7211()#G2_C262::Method5.7213()#G2_C262::Method6.7214<System.Object>()#G2_C262::Method7.7216<System.Object>()#"
		call void Generated738.Generated738::M.G2_C262.B.T<class Generated738.BaseClass0,class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C5::ClassMethod1324.4806()#G1_C5::ClassMethod1325.4807<System.Object>()#G2_C262::ClassMethod1859.7218()#G2_C262::Method4.7211()#G2_C262::Method5.7213()#G2_C262::Method6.7214<System.Object>()#G2_C262::Method7.7216<System.Object>()#"
		call void Generated738.Generated738::M.G2_C262.B.A<class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C262::Method7.MI.7217<System.Object>()#"
		call void Generated738.Generated738::M.IBase2.T.T<class Generated738.BaseClass1,class Generated738.BaseClass0,class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C262::Method7.MI.7217<System.Object>()#"
		call void Generated738.Generated738::M.IBase2.B.T<class Generated738.BaseClass0,class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C262::Method7.MI.7217<System.Object>()#"
		call void Generated738.Generated738::M.IBase2.B.A<class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C262::Method7.MI.7217<System.Object>()#"
		call void Generated738.Generated738::M.IBase2.T.T<class Generated738.BaseClass0,class Generated738.BaseClass0,class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C262::Method7.MI.7217<System.Object>()#"
		call void Generated738.Generated738::M.IBase2.A.T<class Generated738.BaseClass0,class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C262::Method7.MI.7217<System.Object>()#"
		call void Generated738.Generated738::M.IBase2.A.A<class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>>(!!0,string)
		newobj instance void class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C5::ClassMethod1324.4806()#G1_C5::ClassMethod1325.4807<System.Object>()#G1_C5::Method4.4802()#G2_C262::Method5.7213()#G2_C262::Method6.7214<System.Object>()#G2_C262::Method7.7216<System.Object>()#"
		call void Generated738.Generated738::M.G1_C5.T<class Generated738.BaseClass1,class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C5::ClassMethod1324.4806()#G1_C5::ClassMethod1325.4807<System.Object>()#G1_C5::Method4.4802()#G2_C262::Method5.7213()#G2_C262::Method6.7214<System.Object>()#G2_C262::Method7.7216<System.Object>()#"
		call void Generated738.Generated738::M.G1_C5.B<class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C262::Method7.MI.7217<System.Object>()#"
		call void Generated738.Generated738::M.IBase2.T.T<class Generated738.BaseClass1,class Generated738.BaseClass1,class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C262::Method7.MI.7217<System.Object>()#"
		call void Generated738.Generated738::M.IBase2.B.T<class Generated738.BaseClass1,class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C262::Method7.MI.7217<System.Object>()#"
		call void Generated738.Generated738::M.IBase2.B.B<class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C262::Method4.MI.7212()#G2_C262::Method5.7213()#G2_C262::Method6.MI.7215<System.Object>()#"
		call void Generated738.Generated738::M.IBase1.T<class Generated738.BaseClass1,class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C262::Method4.MI.7212()#G2_C262::Method5.7213()#G2_C262::Method6.MI.7215<System.Object>()#"
		call void Generated738.Generated738::M.IBase1.B<class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C262::Method7.MI.7217<System.Object>()#"
		call void Generated738.Generated738::M.IBase2.T.T<class Generated738.BaseClass0,class Generated738.BaseClass1,class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C262::Method7.MI.7217<System.Object>()#"
		call void Generated738.Generated738::M.IBase2.A.T<class Generated738.BaseClass1,class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C262::Method7.MI.7217<System.Object>()#"
		call void Generated738.Generated738::M.IBase2.A.B<class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C262::Method4.MI.7212()#G2_C262::Method5.7213()#G2_C262::Method6.MI.7215<System.Object>()#"
		call void Generated738.Generated738::M.IBase1.T<class Generated738.BaseClass0,class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C262::Method4.MI.7212()#G2_C262::Method5.7213()#G2_C262::Method6.MI.7215<System.Object>()#"
		call void Generated738.Generated738::M.IBase1.A<class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C5::ClassMethod1324.4806()#G1_C5::ClassMethod1325.4807<System.Object>()#G2_C262::ClassMethod1859.7218()#G2_C262::Method4.7211()#G2_C262::Method5.7213()#G2_C262::Method6.7214<System.Object>()#G2_C262::Method7.7216<System.Object>()#"
		call void Generated738.Generated738::M.G2_C262.T.T<class Generated738.BaseClass1,class Generated738.BaseClass1,class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C5::ClassMethod1324.4806()#G1_C5::ClassMethod1325.4807<System.Object>()#G2_C262::ClassMethod1859.7218()#G2_C262::Method4.7211()#G2_C262::Method5.7213()#G2_C262::Method6.7214<System.Object>()#G2_C262::Method7.7216<System.Object>()#"
		call void Generated738.Generated738::M.G2_C262.B.T<class Generated738.BaseClass1,class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C5::ClassMethod1324.4806()#G1_C5::ClassMethod1325.4807<System.Object>()#G2_C262::ClassMethod1859.7218()#G2_C262::Method4.7211()#G2_C262::Method5.7213()#G2_C262::Method6.7214<System.Object>()#G2_C262::Method7.7216<System.Object>()#"
		call void Generated738.Generated738::M.G2_C262.B.B<class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>>(!!0,string)
		newobj instance void class Generated738.G1_C5`1<class Generated738.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C5::ClassMethod1324.4806()#G1_C5::ClassMethod1325.4807<System.Object>()#G1_C5::Method4.4802()#G1_C5::Method5.4803()#G1_C5::Method6.4804<System.Object>()#G1_C5::Method7.4801<System.Object>()#"
		call void Generated738.Generated738::M.G1_C5.T<class Generated738.BaseClass0,class Generated738.G1_C5`1<class Generated738.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C5::ClassMethod1324.4806()#G1_C5::ClassMethod1325.4807<System.Object>()#G1_C5::Method4.4802()#G1_C5::Method5.4803()#G1_C5::Method6.4804<System.Object>()#G1_C5::Method7.4801<System.Object>()#"
		call void Generated738.Generated738::M.G1_C5.A<class Generated738.G1_C5`1<class Generated738.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C5::Method7.4801<System.Object>()#"
		call void Generated738.Generated738::M.IBase2.T.T<class Generated738.BaseClass1,class Generated738.BaseClass1,class Generated738.G1_C5`1<class Generated738.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C5::Method7.4801<System.Object>()#"
		call void Generated738.Generated738::M.IBase2.B.T<class Generated738.BaseClass1,class Generated738.G1_C5`1<class Generated738.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C5::Method7.4801<System.Object>()#"
		call void Generated738.Generated738::M.IBase2.B.B<class Generated738.G1_C5`1<class Generated738.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C5::Method4.4802()#G1_C5::Method5.4803()#G1_C5::Method6.MI.4805<System.Object>()#"
		call void Generated738.Generated738::M.IBase1.T<class Generated738.BaseClass0,class Generated738.G1_C5`1<class Generated738.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C5::Method4.4802()#G1_C5::Method5.4803()#G1_C5::Method6.MI.4805<System.Object>()#"
		call void Generated738.Generated738::M.IBase1.A<class Generated738.G1_C5`1<class Generated738.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C5::Method7.4801<System.Object>()#"
		call void Generated738.Generated738::M.IBase2.T.T<class Generated738.BaseClass0,class Generated738.BaseClass1,class Generated738.G1_C5`1<class Generated738.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C5::Method7.4801<System.Object>()#"
		call void Generated738.Generated738::M.IBase2.A.T<class Generated738.BaseClass1,class Generated738.G1_C5`1<class Generated738.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C5::Method7.4801<System.Object>()#"
		call void Generated738.Generated738::M.IBase2.A.B<class Generated738.G1_C5`1<class Generated738.BaseClass0>>(!!0,string)
		newobj instance void class Generated738.G1_C5`1<class Generated738.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C5::ClassMethod1324.4806()#G1_C5::ClassMethod1325.4807<System.Object>()#G1_C5::Method4.4802()#G1_C5::Method5.4803()#G1_C5::Method6.4804<System.Object>()#G1_C5::Method7.4801<System.Object>()#"
		call void Generated738.Generated738::M.G1_C5.T<class Generated738.BaseClass1,class Generated738.G1_C5`1<class Generated738.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C5::ClassMethod1324.4806()#G1_C5::ClassMethod1325.4807<System.Object>()#G1_C5::Method4.4802()#G1_C5::Method5.4803()#G1_C5::Method6.4804<System.Object>()#G1_C5::Method7.4801<System.Object>()#"
		call void Generated738.Generated738::M.G1_C5.B<class Generated738.G1_C5`1<class Generated738.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C5::Method7.4801<System.Object>()#"
		call void Generated738.Generated738::M.IBase2.T.T<class Generated738.BaseClass1,class Generated738.BaseClass1,class Generated738.G1_C5`1<class Generated738.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C5::Method7.4801<System.Object>()#"
		call void Generated738.Generated738::M.IBase2.B.T<class Generated738.BaseClass1,class Generated738.G1_C5`1<class Generated738.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C5::Method7.4801<System.Object>()#"
		call void Generated738.Generated738::M.IBase2.B.B<class Generated738.G1_C5`1<class Generated738.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C5::Method4.4802()#G1_C5::Method5.4803()#G1_C5::Method6.MI.4805<System.Object>()#"
		call void Generated738.Generated738::M.IBase1.T<class Generated738.BaseClass1,class Generated738.G1_C5`1<class Generated738.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C5::Method4.4802()#G1_C5::Method5.4803()#G1_C5::Method6.MI.4805<System.Object>()#"
		call void Generated738.Generated738::M.IBase1.B<class Generated738.G1_C5`1<class Generated738.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C5::Method7.4801<System.Object>()#"
		call void Generated738.Generated738::M.IBase2.T.T<class Generated738.BaseClass0,class Generated738.BaseClass1,class Generated738.G1_C5`1<class Generated738.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C5::Method7.4801<System.Object>()#"
		call void Generated738.Generated738::M.IBase2.A.T<class Generated738.BaseClass1,class Generated738.G1_C5`1<class Generated738.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C5::Method7.4801<System.Object>()#"
		call void Generated738.Generated738::M.IBase2.A.B<class Generated738.G1_C5`1<class Generated738.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C5::Method4.4802()#G1_C5::Method5.4803()#G1_C5::Method6.MI.4805<System.Object>()#"
		call void Generated738.Generated738::M.IBase1.T<class Generated738.BaseClass0,class Generated738.G1_C5`1<class Generated738.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C5::Method4.4802()#G1_C5::Method5.4803()#G1_C5::Method6.MI.4805<System.Object>()#"
		call void Generated738.Generated738::M.IBase1.A<class Generated738.G1_C5`1<class Generated738.BaseClass1>>(!!0,string)
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
		newobj instance void class Generated738.G3_C1210`1<class Generated738.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated738.G1_C5`1<class Generated738.BaseClass0>::ClassMethod1325<object>()
		calli default string(class Generated738.G3_C1210`1<class Generated738.BaseClass0>)
		ldstr "G1_C5::ClassMethod1325.4807<System.Object>()"
		ldstr "class G1_C5`1<class Generated738.BaseClass0> on type class Generated738.G3_C1210`1<class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated738.G1_C5`1<class Generated738.BaseClass0>::ClassMethod1324()
		calli default string(class Generated738.G3_C1210`1<class Generated738.BaseClass0>)
		ldstr "G1_C5::ClassMethod1324.4806()"
		ldstr "class G1_C5`1<class Generated738.BaseClass0> on type class Generated738.G3_C1210`1<class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated738.G1_C5`1<class Generated738.BaseClass0>::Method6<object>()
		calli default string(class Generated738.G3_C1210`1<class Generated738.BaseClass0>)
		ldstr "G2_C262::Method6.7214<System.Object>()"
		ldstr "class G1_C5`1<class Generated738.BaseClass0> on type class Generated738.G3_C1210`1<class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated738.G1_C5`1<class Generated738.BaseClass0>::Method5()
		calli default string(class Generated738.G3_C1210`1<class Generated738.BaseClass0>)
		ldstr "G2_C262::Method5.7213()"
		ldstr "class G1_C5`1<class Generated738.BaseClass0> on type class Generated738.G3_C1210`1<class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated738.G1_C5`1<class Generated738.BaseClass0>::Method4()
		calli default string(class Generated738.G3_C1210`1<class Generated738.BaseClass0>)
		ldstr "G1_C5::Method4.4802()"
		ldstr "class G1_C5`1<class Generated738.BaseClass0> on type class Generated738.G3_C1210`1<class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated738.G1_C5`1<class Generated738.BaseClass0>::Method7<object>()
		calli default string(class Generated738.G3_C1210`1<class Generated738.BaseClass0>)
		ldstr "G3_C1210::Method7.14905<System.Object>()"
		ldstr "class G1_C5`1<class Generated738.BaseClass0> on type class Generated738.G3_C1210`1<class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated738.IBase2`2<class Generated738.BaseClass1,class Generated738.BaseClass1>::Method7<object>()
		calli default string(class Generated738.G3_C1210`1<class Generated738.BaseClass0>)
		ldstr "G2_C262::Method7.MI.7217<System.Object>()"
		ldstr "class IBase2`2<class Generated738.BaseClass1,class Generated738.BaseClass1> on type class Generated738.G3_C1210`1<class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated738.IBase1`1<class Generated738.BaseClass0>::Method4()
		calli default string(class Generated738.G3_C1210`1<class Generated738.BaseClass0>)
		ldstr "G2_C262::Method4.MI.7212()"
		ldstr "class IBase1`1<class Generated738.BaseClass0> on type class Generated738.G3_C1210`1<class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated738.IBase1`1<class Generated738.BaseClass0>::Method5()
		calli default string(class Generated738.G3_C1210`1<class Generated738.BaseClass0>)
		ldstr "G2_C262::Method5.7213()"
		ldstr "class IBase1`1<class Generated738.BaseClass0> on type class Generated738.G3_C1210`1<class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated738.IBase1`1<class Generated738.BaseClass0>::Method6<object>()
		calli default string(class Generated738.G3_C1210`1<class Generated738.BaseClass0>)
		ldstr "G2_C262::Method6.MI.7215<System.Object>()"
		ldstr "class IBase1`1<class Generated738.BaseClass0> on type class Generated738.G3_C1210`1<class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated738.IBase2`2<class Generated738.BaseClass0,class Generated738.BaseClass1>::Method7<object>()
		calli default string(class Generated738.G3_C1210`1<class Generated738.BaseClass0>)
		ldstr "G3_C1210::Method7.14905<System.Object>()"
		ldstr "class IBase2`2<class Generated738.BaseClass0,class Generated738.BaseClass1> on type class Generated738.G3_C1210`1<class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>::ClassMethod1859()
		calli default string(class Generated738.G3_C1210`1<class Generated738.BaseClass0>)
		ldstr "G3_C1210::ClassMethod1859.MI.14907()"
		ldstr "class G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1> on type class Generated738.G3_C1210`1<class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>::Method7<object>()
		calli default string(class Generated738.G3_C1210`1<class Generated738.BaseClass0>)
		ldstr "G3_C1210::Method7.14905<System.Object>()"
		ldstr "class G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1> on type class Generated738.G3_C1210`1<class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>::Method6<object>()
		calli default string(class Generated738.G3_C1210`1<class Generated738.BaseClass0>)
		ldstr "G2_C262::Method6.7214<System.Object>()"
		ldstr "class G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1> on type class Generated738.G3_C1210`1<class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>::Method5()
		calli default string(class Generated738.G3_C1210`1<class Generated738.BaseClass0>)
		ldstr "G2_C262::Method5.7213()"
		ldstr "class G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1> on type class Generated738.G3_C1210`1<class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>::Method4()
		calli default string(class Generated738.G3_C1210`1<class Generated738.BaseClass0>)
		ldstr "G2_C262::Method4.7211()"
		ldstr "class G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1> on type class Generated738.G3_C1210`1<class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>::ClassMethod1325<object>()
		calli default string(class Generated738.G3_C1210`1<class Generated738.BaseClass0>)
		ldstr "G1_C5::ClassMethod1325.4807<System.Object>()"
		ldstr "class G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1> on type class Generated738.G3_C1210`1<class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>::ClassMethod1324()
		calli default string(class Generated738.G3_C1210`1<class Generated738.BaseClass0>)
		ldstr "G1_C5::ClassMethod1324.4806()"
		ldstr "class G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1> on type class Generated738.G3_C1210`1<class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated738.IBase1`1<class Generated738.BaseClass1>::Method4()
		calli default string(class Generated738.G3_C1210`1<class Generated738.BaseClass0>)
		ldstr "G2_C262::Method4.MI.7212()"
		ldstr "class IBase1`1<class Generated738.BaseClass1> on type class Generated738.G3_C1210`1<class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated738.IBase1`1<class Generated738.BaseClass1>::Method5()
		calli default string(class Generated738.G3_C1210`1<class Generated738.BaseClass0>)
		ldstr "G2_C262::Method5.7213()"
		ldstr "class IBase1`1<class Generated738.BaseClass1> on type class Generated738.G3_C1210`1<class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated738.IBase1`1<class Generated738.BaseClass1>::Method6<object>()
		calli default string(class Generated738.G3_C1210`1<class Generated738.BaseClass0>)
		ldstr "G2_C262::Method6.MI.7215<System.Object>()"
		ldstr "class IBase1`1<class Generated738.BaseClass1> on type class Generated738.G3_C1210`1<class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G3_C1210`1<class Generated738.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated738.G3_C1210`1<class Generated738.BaseClass0>::ClassMethod3806<object>()
		calli default string(class Generated738.G3_C1210`1<class Generated738.BaseClass0>)
		ldstr "G3_C1210::ClassMethod3806.14906<System.Object>()"
		ldstr "class G3_C1210`1<class Generated738.BaseClass0> on type class Generated738.G3_C1210`1<class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G3_C1210`1<class Generated738.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated738.G3_C1210`1<class Generated738.BaseClass0>::Method7<object>()
		calli default string(class Generated738.G3_C1210`1<class Generated738.BaseClass0>)
		ldstr "G3_C1210::Method7.14905<System.Object>()"
		ldstr "class G3_C1210`1<class Generated738.BaseClass0> on type class Generated738.G3_C1210`1<class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G3_C1210`1<class Generated738.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated738.G3_C1210`1<class Generated738.BaseClass0>::ClassMethod1859()
		calli default string(class Generated738.G3_C1210`1<class Generated738.BaseClass0>)
		ldstr "G3_C1210::ClassMethod1859.MI.14907()"
		ldstr "class G3_C1210`1<class Generated738.BaseClass0> on type class Generated738.G3_C1210`1<class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G3_C1210`1<class Generated738.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated738.G3_C1210`1<class Generated738.BaseClass0>::Method6<object>()
		calli default string(class Generated738.G3_C1210`1<class Generated738.BaseClass0>)
		ldstr "G2_C262::Method6.7214<System.Object>()"
		ldstr "class G3_C1210`1<class Generated738.BaseClass0> on type class Generated738.G3_C1210`1<class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G3_C1210`1<class Generated738.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated738.G3_C1210`1<class Generated738.BaseClass0>::Method5()
		calli default string(class Generated738.G3_C1210`1<class Generated738.BaseClass0>)
		ldstr "G2_C262::Method5.7213()"
		ldstr "class G3_C1210`1<class Generated738.BaseClass0> on type class Generated738.G3_C1210`1<class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G3_C1210`1<class Generated738.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated738.G3_C1210`1<class Generated738.BaseClass0>::Method4()
		calli default string(class Generated738.G3_C1210`1<class Generated738.BaseClass0>)
		ldstr "G2_C262::Method4.7211()"
		ldstr "class G3_C1210`1<class Generated738.BaseClass0> on type class Generated738.G3_C1210`1<class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G3_C1210`1<class Generated738.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated738.G3_C1210`1<class Generated738.BaseClass0>::ClassMethod1325<object>()
		calli default string(class Generated738.G3_C1210`1<class Generated738.BaseClass0>)
		ldstr "G1_C5::ClassMethod1325.4807<System.Object>()"
		ldstr "class G3_C1210`1<class Generated738.BaseClass0> on type class Generated738.G3_C1210`1<class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G3_C1210`1<class Generated738.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated738.G3_C1210`1<class Generated738.BaseClass0>::ClassMethod1324()
		calli default string(class Generated738.G3_C1210`1<class Generated738.BaseClass0>)
		ldstr "G1_C5::ClassMethod1324.4806()"
		ldstr "class G3_C1210`1<class Generated738.BaseClass0> on type class Generated738.G3_C1210`1<class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated738.IBase2`2<class Generated738.BaseClass0,class Generated738.BaseClass0>::Method7<object>()
		calli default string(class Generated738.G3_C1210`1<class Generated738.BaseClass0>)
		ldstr "G3_C1210::Method7.14905<System.Object>()"
		ldstr "class IBase2`2<class Generated738.BaseClass0,class Generated738.BaseClass0> on type class Generated738.G3_C1210`1<class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated738.G3_C1210`1<class Generated738.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated738.G1_C5`1<class Generated738.BaseClass1>::ClassMethod1325<object>()
		calli default string(class Generated738.G3_C1210`1<class Generated738.BaseClass1>)
		ldstr "G1_C5::ClassMethod1325.4807<System.Object>()"
		ldstr "class G1_C5`1<class Generated738.BaseClass1> on type class Generated738.G3_C1210`1<class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated738.G1_C5`1<class Generated738.BaseClass1>::ClassMethod1324()
		calli default string(class Generated738.G3_C1210`1<class Generated738.BaseClass1>)
		ldstr "G1_C5::ClassMethod1324.4806()"
		ldstr "class G1_C5`1<class Generated738.BaseClass1> on type class Generated738.G3_C1210`1<class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated738.G1_C5`1<class Generated738.BaseClass1>::Method6<object>()
		calli default string(class Generated738.G3_C1210`1<class Generated738.BaseClass1>)
		ldstr "G2_C262::Method6.7214<System.Object>()"
		ldstr "class G1_C5`1<class Generated738.BaseClass1> on type class Generated738.G3_C1210`1<class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated738.G1_C5`1<class Generated738.BaseClass1>::Method5()
		calli default string(class Generated738.G3_C1210`1<class Generated738.BaseClass1>)
		ldstr "G2_C262::Method5.7213()"
		ldstr "class G1_C5`1<class Generated738.BaseClass1> on type class Generated738.G3_C1210`1<class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated738.G1_C5`1<class Generated738.BaseClass1>::Method4()
		calli default string(class Generated738.G3_C1210`1<class Generated738.BaseClass1>)
		ldstr "G1_C5::Method4.4802()"
		ldstr "class G1_C5`1<class Generated738.BaseClass1> on type class Generated738.G3_C1210`1<class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated738.G1_C5`1<class Generated738.BaseClass1>::Method7<object>()
		calli default string(class Generated738.G3_C1210`1<class Generated738.BaseClass1>)
		ldstr "G3_C1210::Method7.14905<System.Object>()"
		ldstr "class G1_C5`1<class Generated738.BaseClass1> on type class Generated738.G3_C1210`1<class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated738.IBase2`2<class Generated738.BaseClass1,class Generated738.BaseClass1>::Method7<object>()
		calli default string(class Generated738.G3_C1210`1<class Generated738.BaseClass1>)
		ldstr "G2_C262::Method7.MI.7217<System.Object>()"
		ldstr "class IBase2`2<class Generated738.BaseClass1,class Generated738.BaseClass1> on type class Generated738.G3_C1210`1<class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated738.IBase1`1<class Generated738.BaseClass1>::Method4()
		calli default string(class Generated738.G3_C1210`1<class Generated738.BaseClass1>)
		ldstr "G2_C262::Method4.MI.7212()"
		ldstr "class IBase1`1<class Generated738.BaseClass1> on type class Generated738.G3_C1210`1<class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated738.IBase1`1<class Generated738.BaseClass1>::Method5()
		calli default string(class Generated738.G3_C1210`1<class Generated738.BaseClass1>)
		ldstr "G2_C262::Method5.7213()"
		ldstr "class IBase1`1<class Generated738.BaseClass1> on type class Generated738.G3_C1210`1<class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated738.IBase1`1<class Generated738.BaseClass1>::Method6<object>()
		calli default string(class Generated738.G3_C1210`1<class Generated738.BaseClass1>)
		ldstr "G2_C262::Method6.MI.7215<System.Object>()"
		ldstr "class IBase1`1<class Generated738.BaseClass1> on type class Generated738.G3_C1210`1<class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated738.IBase2`2<class Generated738.BaseClass0,class Generated738.BaseClass1>::Method7<object>()
		calli default string(class Generated738.G3_C1210`1<class Generated738.BaseClass1>)
		ldstr "G3_C1210::Method7.14905<System.Object>()"
		ldstr "class IBase2`2<class Generated738.BaseClass0,class Generated738.BaseClass1> on type class Generated738.G3_C1210`1<class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated738.IBase1`1<class Generated738.BaseClass0>::Method4()
		calli default string(class Generated738.G3_C1210`1<class Generated738.BaseClass1>)
		ldstr "G2_C262::Method4.MI.7212()"
		ldstr "class IBase1`1<class Generated738.BaseClass0> on type class Generated738.G3_C1210`1<class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated738.IBase1`1<class Generated738.BaseClass0>::Method5()
		calli default string(class Generated738.G3_C1210`1<class Generated738.BaseClass1>)
		ldstr "G2_C262::Method5.7213()"
		ldstr "class IBase1`1<class Generated738.BaseClass0> on type class Generated738.G3_C1210`1<class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated738.IBase1`1<class Generated738.BaseClass0>::Method6<object>()
		calli default string(class Generated738.G3_C1210`1<class Generated738.BaseClass1>)
		ldstr "G2_C262::Method6.MI.7215<System.Object>()"
		ldstr "class IBase1`1<class Generated738.BaseClass0> on type class Generated738.G3_C1210`1<class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>::ClassMethod1859()
		calli default string(class Generated738.G3_C1210`1<class Generated738.BaseClass1>)
		ldstr "G3_C1210::ClassMethod1859.MI.14907()"
		ldstr "class G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1> on type class Generated738.G3_C1210`1<class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>::Method7<object>()
		calli default string(class Generated738.G3_C1210`1<class Generated738.BaseClass1>)
		ldstr "G3_C1210::Method7.14905<System.Object>()"
		ldstr "class G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1> on type class Generated738.G3_C1210`1<class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>::Method6<object>()
		calli default string(class Generated738.G3_C1210`1<class Generated738.BaseClass1>)
		ldstr "G2_C262::Method6.7214<System.Object>()"
		ldstr "class G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1> on type class Generated738.G3_C1210`1<class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>::Method5()
		calli default string(class Generated738.G3_C1210`1<class Generated738.BaseClass1>)
		ldstr "G2_C262::Method5.7213()"
		ldstr "class G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1> on type class Generated738.G3_C1210`1<class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>::Method4()
		calli default string(class Generated738.G3_C1210`1<class Generated738.BaseClass1>)
		ldstr "G2_C262::Method4.7211()"
		ldstr "class G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1> on type class Generated738.G3_C1210`1<class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>::ClassMethod1325<object>()
		calli default string(class Generated738.G3_C1210`1<class Generated738.BaseClass1>)
		ldstr "G1_C5::ClassMethod1325.4807<System.Object>()"
		ldstr "class G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1> on type class Generated738.G3_C1210`1<class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>::ClassMethod1324()
		calli default string(class Generated738.G3_C1210`1<class Generated738.BaseClass1>)
		ldstr "G1_C5::ClassMethod1324.4806()"
		ldstr "class G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1> on type class Generated738.G3_C1210`1<class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G3_C1210`1<class Generated738.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated738.G3_C1210`1<class Generated738.BaseClass1>::ClassMethod3806<object>()
		calli default string(class Generated738.G3_C1210`1<class Generated738.BaseClass1>)
		ldstr "G3_C1210::ClassMethod3806.14906<System.Object>()"
		ldstr "class G3_C1210`1<class Generated738.BaseClass1> on type class Generated738.G3_C1210`1<class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G3_C1210`1<class Generated738.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated738.G3_C1210`1<class Generated738.BaseClass1>::Method7<object>()
		calli default string(class Generated738.G3_C1210`1<class Generated738.BaseClass1>)
		ldstr "G3_C1210::Method7.14905<System.Object>()"
		ldstr "class G3_C1210`1<class Generated738.BaseClass1> on type class Generated738.G3_C1210`1<class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G3_C1210`1<class Generated738.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated738.G3_C1210`1<class Generated738.BaseClass1>::ClassMethod1859()
		calli default string(class Generated738.G3_C1210`1<class Generated738.BaseClass1>)
		ldstr "G3_C1210::ClassMethod1859.MI.14907()"
		ldstr "class G3_C1210`1<class Generated738.BaseClass1> on type class Generated738.G3_C1210`1<class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G3_C1210`1<class Generated738.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated738.G3_C1210`1<class Generated738.BaseClass1>::Method6<object>()
		calli default string(class Generated738.G3_C1210`1<class Generated738.BaseClass1>)
		ldstr "G2_C262::Method6.7214<System.Object>()"
		ldstr "class G3_C1210`1<class Generated738.BaseClass1> on type class Generated738.G3_C1210`1<class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G3_C1210`1<class Generated738.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated738.G3_C1210`1<class Generated738.BaseClass1>::Method5()
		calli default string(class Generated738.G3_C1210`1<class Generated738.BaseClass1>)
		ldstr "G2_C262::Method5.7213()"
		ldstr "class G3_C1210`1<class Generated738.BaseClass1> on type class Generated738.G3_C1210`1<class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G3_C1210`1<class Generated738.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated738.G3_C1210`1<class Generated738.BaseClass1>::Method4()
		calli default string(class Generated738.G3_C1210`1<class Generated738.BaseClass1>)
		ldstr "G2_C262::Method4.7211()"
		ldstr "class G3_C1210`1<class Generated738.BaseClass1> on type class Generated738.G3_C1210`1<class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G3_C1210`1<class Generated738.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated738.G3_C1210`1<class Generated738.BaseClass1>::ClassMethod1325<object>()
		calli default string(class Generated738.G3_C1210`1<class Generated738.BaseClass1>)
		ldstr "G1_C5::ClassMethod1325.4807<System.Object>()"
		ldstr "class G3_C1210`1<class Generated738.BaseClass1> on type class Generated738.G3_C1210`1<class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G3_C1210`1<class Generated738.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated738.G3_C1210`1<class Generated738.BaseClass1>::ClassMethod1324()
		calli default string(class Generated738.G3_C1210`1<class Generated738.BaseClass1>)
		ldstr "G1_C5::ClassMethod1324.4806()"
		ldstr "class G3_C1210`1<class Generated738.BaseClass1> on type class Generated738.G3_C1210`1<class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated738.G1_C5`1<class Generated738.BaseClass0>::ClassMethod1325<object>()
		calli default string(class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>)
		ldstr "G1_C5::ClassMethod1325.4807<System.Object>()"
		ldstr "class G1_C5`1<class Generated738.BaseClass0> on type class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated738.G1_C5`1<class Generated738.BaseClass0>::ClassMethod1324()
		calli default string(class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>)
		ldstr "G1_C5::ClassMethod1324.4806()"
		ldstr "class G1_C5`1<class Generated738.BaseClass0> on type class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated738.G1_C5`1<class Generated738.BaseClass0>::Method6<object>()
		calli default string(class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>)
		ldstr "G2_C262::Method6.7214<System.Object>()"
		ldstr "class G1_C5`1<class Generated738.BaseClass0> on type class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated738.G1_C5`1<class Generated738.BaseClass0>::Method5()
		calli default string(class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>)
		ldstr "G2_C262::Method5.7213()"
		ldstr "class G1_C5`1<class Generated738.BaseClass0> on type class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated738.G1_C5`1<class Generated738.BaseClass0>::Method4()
		calli default string(class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>)
		ldstr "G1_C5::Method4.4802()"
		ldstr "class G1_C5`1<class Generated738.BaseClass0> on type class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated738.G1_C5`1<class Generated738.BaseClass0>::Method7<object>()
		calli default string(class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>)
		ldstr "G2_C262::Method7.7216<System.Object>()"
		ldstr "class G1_C5`1<class Generated738.BaseClass0> on type class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated738.IBase2`2<class Generated738.BaseClass1,class Generated738.BaseClass1>::Method7<object>()
		calli default string(class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>)
		ldstr "G2_C262::Method7.MI.7217<System.Object>()"
		ldstr "class IBase2`2<class Generated738.BaseClass1,class Generated738.BaseClass1> on type class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated738.IBase1`1<class Generated738.BaseClass0>::Method4()
		calli default string(class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>)
		ldstr "G2_C262::Method4.MI.7212()"
		ldstr "class IBase1`1<class Generated738.BaseClass0> on type class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated738.IBase1`1<class Generated738.BaseClass0>::Method5()
		calli default string(class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>)
		ldstr "G2_C262::Method5.7213()"
		ldstr "class IBase1`1<class Generated738.BaseClass0> on type class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated738.IBase1`1<class Generated738.BaseClass0>::Method6<object>()
		calli default string(class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>)
		ldstr "G2_C262::Method6.MI.7215<System.Object>()"
		ldstr "class IBase1`1<class Generated738.BaseClass0> on type class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated738.IBase2`2<class Generated738.BaseClass0,class Generated738.BaseClass1>::Method7<object>()
		calli default string(class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>)
		ldstr "G2_C262::Method7.MI.7217<System.Object>()"
		ldstr "class IBase2`2<class Generated738.BaseClass0,class Generated738.BaseClass1> on type class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>::ClassMethod1859()
		calli default string(class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>)
		ldstr "G2_C262::ClassMethod1859.7218()"
		ldstr "class G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0> on type class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>::Method7<object>()
		calli default string(class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>)
		ldstr "G2_C262::Method7.7216<System.Object>()"
		ldstr "class G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0> on type class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>::Method6<object>()
		calli default string(class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>)
		ldstr "G2_C262::Method6.7214<System.Object>()"
		ldstr "class G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0> on type class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>::Method5()
		calli default string(class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>)
		ldstr "G2_C262::Method5.7213()"
		ldstr "class G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0> on type class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>::Method4()
		calli default string(class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>)
		ldstr "G2_C262::Method4.7211()"
		ldstr "class G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0> on type class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>::ClassMethod1325<object>()
		calli default string(class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>)
		ldstr "G1_C5::ClassMethod1325.4807<System.Object>()"
		ldstr "class G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0> on type class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>::ClassMethod1324()
		calli default string(class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>)
		ldstr "G1_C5::ClassMethod1324.4806()"
		ldstr "class G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0> on type class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated738.IBase1`1<class Generated738.BaseClass1>::Method4()
		calli default string(class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>)
		ldstr "G2_C262::Method4.MI.7212()"
		ldstr "class IBase1`1<class Generated738.BaseClass1> on type class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated738.IBase1`1<class Generated738.BaseClass1>::Method5()
		calli default string(class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>)
		ldstr "G2_C262::Method5.7213()"
		ldstr "class IBase1`1<class Generated738.BaseClass1> on type class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated738.IBase1`1<class Generated738.BaseClass1>::Method6<object>()
		calli default string(class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>)
		ldstr "G2_C262::Method6.MI.7215<System.Object>()"
		ldstr "class IBase1`1<class Generated738.BaseClass1> on type class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated738.IBase2`2<class Generated738.BaseClass1,class Generated738.BaseClass0>::Method7<object>()
		calli default string(class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>)
		ldstr "G2_C262::Method7.MI.7217<System.Object>()"
		ldstr "class IBase2`2<class Generated738.BaseClass1,class Generated738.BaseClass0> on type class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated738.IBase2`2<class Generated738.BaseClass0,class Generated738.BaseClass0>::Method7<object>()
		calli default string(class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>)
		ldstr "G2_C262::Method7.MI.7217<System.Object>()"
		ldstr "class IBase2`2<class Generated738.BaseClass0,class Generated738.BaseClass0> on type class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated738.G1_C5`1<class Generated738.BaseClass0>::ClassMethod1325<object>()
		calli default string(class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>)
		ldstr "G1_C5::ClassMethod1325.4807<System.Object>()"
		ldstr "class G1_C5`1<class Generated738.BaseClass0> on type class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated738.G1_C5`1<class Generated738.BaseClass0>::ClassMethod1324()
		calli default string(class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>)
		ldstr "G1_C5::ClassMethod1324.4806()"
		ldstr "class G1_C5`1<class Generated738.BaseClass0> on type class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated738.G1_C5`1<class Generated738.BaseClass0>::Method6<object>()
		calli default string(class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>)
		ldstr "G2_C262::Method6.7214<System.Object>()"
		ldstr "class G1_C5`1<class Generated738.BaseClass0> on type class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated738.G1_C5`1<class Generated738.BaseClass0>::Method5()
		calli default string(class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>)
		ldstr "G2_C262::Method5.7213()"
		ldstr "class G1_C5`1<class Generated738.BaseClass0> on type class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated738.G1_C5`1<class Generated738.BaseClass0>::Method4()
		calli default string(class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>)
		ldstr "G1_C5::Method4.4802()"
		ldstr "class G1_C5`1<class Generated738.BaseClass0> on type class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated738.G1_C5`1<class Generated738.BaseClass0>::Method7<object>()
		calli default string(class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>)
		ldstr "G2_C262::Method7.7216<System.Object>()"
		ldstr "class G1_C5`1<class Generated738.BaseClass0> on type class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated738.IBase2`2<class Generated738.BaseClass1,class Generated738.BaseClass1>::Method7<object>()
		calli default string(class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>)
		ldstr "G2_C262::Method7.MI.7217<System.Object>()"
		ldstr "class IBase2`2<class Generated738.BaseClass1,class Generated738.BaseClass1> on type class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated738.IBase1`1<class Generated738.BaseClass0>::Method4()
		calli default string(class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>)
		ldstr "G2_C262::Method4.MI.7212()"
		ldstr "class IBase1`1<class Generated738.BaseClass0> on type class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated738.IBase1`1<class Generated738.BaseClass0>::Method5()
		calli default string(class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>)
		ldstr "G2_C262::Method5.7213()"
		ldstr "class IBase1`1<class Generated738.BaseClass0> on type class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated738.IBase1`1<class Generated738.BaseClass0>::Method6<object>()
		calli default string(class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>)
		ldstr "G2_C262::Method6.MI.7215<System.Object>()"
		ldstr "class IBase1`1<class Generated738.BaseClass0> on type class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated738.IBase2`2<class Generated738.BaseClass0,class Generated738.BaseClass1>::Method7<object>()
		calli default string(class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>)
		ldstr "G2_C262::Method7.MI.7217<System.Object>()"
		ldstr "class IBase2`2<class Generated738.BaseClass0,class Generated738.BaseClass1> on type class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>::ClassMethod1859()
		calli default string(class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>)
		ldstr "G2_C262::ClassMethod1859.7218()"
		ldstr "class G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1> on type class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>::Method7<object>()
		calli default string(class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>)
		ldstr "G2_C262::Method7.7216<System.Object>()"
		ldstr "class G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1> on type class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>::Method6<object>()
		calli default string(class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>)
		ldstr "G2_C262::Method6.7214<System.Object>()"
		ldstr "class G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1> on type class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>::Method5()
		calli default string(class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>)
		ldstr "G2_C262::Method5.7213()"
		ldstr "class G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1> on type class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>::Method4()
		calli default string(class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>)
		ldstr "G2_C262::Method4.7211()"
		ldstr "class G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1> on type class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>::ClassMethod1325<object>()
		calli default string(class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>)
		ldstr "G1_C5::ClassMethod1325.4807<System.Object>()"
		ldstr "class G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1> on type class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>::ClassMethod1324()
		calli default string(class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>)
		ldstr "G1_C5::ClassMethod1324.4806()"
		ldstr "class G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1> on type class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated738.IBase1`1<class Generated738.BaseClass1>::Method4()
		calli default string(class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>)
		ldstr "G2_C262::Method4.MI.7212()"
		ldstr "class IBase1`1<class Generated738.BaseClass1> on type class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated738.IBase1`1<class Generated738.BaseClass1>::Method5()
		calli default string(class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>)
		ldstr "G2_C262::Method5.7213()"
		ldstr "class IBase1`1<class Generated738.BaseClass1> on type class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated738.IBase1`1<class Generated738.BaseClass1>::Method6<object>()
		calli default string(class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>)
		ldstr "G2_C262::Method6.MI.7215<System.Object>()"
		ldstr "class IBase1`1<class Generated738.BaseClass1> on type class Generated738.G2_C262`2<class Generated738.BaseClass0,class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated738.G1_C5`1<class Generated738.BaseClass1>::ClassMethod1325<object>()
		calli default string(class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>)
		ldstr "G1_C5::ClassMethod1325.4807<System.Object>()"
		ldstr "class G1_C5`1<class Generated738.BaseClass1> on type class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated738.G1_C5`1<class Generated738.BaseClass1>::ClassMethod1324()
		calli default string(class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>)
		ldstr "G1_C5::ClassMethod1324.4806()"
		ldstr "class G1_C5`1<class Generated738.BaseClass1> on type class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated738.G1_C5`1<class Generated738.BaseClass1>::Method6<object>()
		calli default string(class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>)
		ldstr "G2_C262::Method6.7214<System.Object>()"
		ldstr "class G1_C5`1<class Generated738.BaseClass1> on type class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated738.G1_C5`1<class Generated738.BaseClass1>::Method5()
		calli default string(class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>)
		ldstr "G2_C262::Method5.7213()"
		ldstr "class G1_C5`1<class Generated738.BaseClass1> on type class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated738.G1_C5`1<class Generated738.BaseClass1>::Method4()
		calli default string(class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>)
		ldstr "G1_C5::Method4.4802()"
		ldstr "class G1_C5`1<class Generated738.BaseClass1> on type class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated738.G1_C5`1<class Generated738.BaseClass1>::Method7<object>()
		calli default string(class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>)
		ldstr "G2_C262::Method7.7216<System.Object>()"
		ldstr "class G1_C5`1<class Generated738.BaseClass1> on type class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated738.IBase2`2<class Generated738.BaseClass1,class Generated738.BaseClass1>::Method7<object>()
		calli default string(class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>)
		ldstr "G2_C262::Method7.MI.7217<System.Object>()"
		ldstr "class IBase2`2<class Generated738.BaseClass1,class Generated738.BaseClass1> on type class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated738.IBase1`1<class Generated738.BaseClass1>::Method4()
		calli default string(class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>)
		ldstr "G2_C262::Method4.MI.7212()"
		ldstr "class IBase1`1<class Generated738.BaseClass1> on type class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated738.IBase1`1<class Generated738.BaseClass1>::Method5()
		calli default string(class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>)
		ldstr "G2_C262::Method5.7213()"
		ldstr "class IBase1`1<class Generated738.BaseClass1> on type class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated738.IBase1`1<class Generated738.BaseClass1>::Method6<object>()
		calli default string(class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>)
		ldstr "G2_C262::Method6.MI.7215<System.Object>()"
		ldstr "class IBase1`1<class Generated738.BaseClass1> on type class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated738.IBase2`2<class Generated738.BaseClass0,class Generated738.BaseClass1>::Method7<object>()
		calli default string(class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>)
		ldstr "G2_C262::Method7.MI.7217<System.Object>()"
		ldstr "class IBase2`2<class Generated738.BaseClass0,class Generated738.BaseClass1> on type class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated738.IBase1`1<class Generated738.BaseClass0>::Method4()
		calli default string(class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>)
		ldstr "G2_C262::Method4.MI.7212()"
		ldstr "class IBase1`1<class Generated738.BaseClass0> on type class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated738.IBase1`1<class Generated738.BaseClass0>::Method5()
		calli default string(class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>)
		ldstr "G2_C262::Method5.7213()"
		ldstr "class IBase1`1<class Generated738.BaseClass0> on type class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated738.IBase1`1<class Generated738.BaseClass0>::Method6<object>()
		calli default string(class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>)
		ldstr "G2_C262::Method6.MI.7215<System.Object>()"
		ldstr "class IBase1`1<class Generated738.BaseClass0> on type class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>::ClassMethod1859()
		calli default string(class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>)
		ldstr "G2_C262::ClassMethod1859.7218()"
		ldstr "class G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0> on type class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>::Method7<object>()
		calli default string(class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>)
		ldstr "G2_C262::Method7.7216<System.Object>()"
		ldstr "class G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0> on type class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>::Method6<object>()
		calli default string(class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>)
		ldstr "G2_C262::Method6.7214<System.Object>()"
		ldstr "class G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0> on type class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>::Method5()
		calli default string(class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>)
		ldstr "G2_C262::Method5.7213()"
		ldstr "class G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0> on type class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>::Method4()
		calli default string(class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>)
		ldstr "G2_C262::Method4.7211()"
		ldstr "class G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0> on type class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>::ClassMethod1325<object>()
		calli default string(class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>)
		ldstr "G1_C5::ClassMethod1325.4807<System.Object>()"
		ldstr "class G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0> on type class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>::ClassMethod1324()
		calli default string(class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>)
		ldstr "G1_C5::ClassMethod1324.4806()"
		ldstr "class G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0> on type class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated738.IBase2`2<class Generated738.BaseClass1,class Generated738.BaseClass0>::Method7<object>()
		calli default string(class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>)
		ldstr "G2_C262::Method7.MI.7217<System.Object>()"
		ldstr "class IBase2`2<class Generated738.BaseClass1,class Generated738.BaseClass0> on type class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated738.IBase2`2<class Generated738.BaseClass0,class Generated738.BaseClass0>::Method7<object>()
		calli default string(class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>)
		ldstr "G2_C262::Method7.MI.7217<System.Object>()"
		ldstr "class IBase2`2<class Generated738.BaseClass0,class Generated738.BaseClass0> on type class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated738.G1_C5`1<class Generated738.BaseClass1>::ClassMethod1325<object>()
		calli default string(class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>)
		ldstr "G1_C5::ClassMethod1325.4807<System.Object>()"
		ldstr "class G1_C5`1<class Generated738.BaseClass1> on type class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated738.G1_C5`1<class Generated738.BaseClass1>::ClassMethod1324()
		calli default string(class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>)
		ldstr "G1_C5::ClassMethod1324.4806()"
		ldstr "class G1_C5`1<class Generated738.BaseClass1> on type class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated738.G1_C5`1<class Generated738.BaseClass1>::Method6<object>()
		calli default string(class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>)
		ldstr "G2_C262::Method6.7214<System.Object>()"
		ldstr "class G1_C5`1<class Generated738.BaseClass1> on type class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated738.G1_C5`1<class Generated738.BaseClass1>::Method5()
		calli default string(class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>)
		ldstr "G2_C262::Method5.7213()"
		ldstr "class G1_C5`1<class Generated738.BaseClass1> on type class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated738.G1_C5`1<class Generated738.BaseClass1>::Method4()
		calli default string(class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>)
		ldstr "G1_C5::Method4.4802()"
		ldstr "class G1_C5`1<class Generated738.BaseClass1> on type class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated738.G1_C5`1<class Generated738.BaseClass1>::Method7<object>()
		calli default string(class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>)
		ldstr "G2_C262::Method7.7216<System.Object>()"
		ldstr "class G1_C5`1<class Generated738.BaseClass1> on type class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated738.IBase2`2<class Generated738.BaseClass1,class Generated738.BaseClass1>::Method7<object>()
		calli default string(class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>)
		ldstr "G2_C262::Method7.MI.7217<System.Object>()"
		ldstr "class IBase2`2<class Generated738.BaseClass1,class Generated738.BaseClass1> on type class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated738.IBase1`1<class Generated738.BaseClass1>::Method4()
		calli default string(class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>)
		ldstr "G2_C262::Method4.MI.7212()"
		ldstr "class IBase1`1<class Generated738.BaseClass1> on type class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated738.IBase1`1<class Generated738.BaseClass1>::Method5()
		calli default string(class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>)
		ldstr "G2_C262::Method5.7213()"
		ldstr "class IBase1`1<class Generated738.BaseClass1> on type class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated738.IBase1`1<class Generated738.BaseClass1>::Method6<object>()
		calli default string(class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>)
		ldstr "G2_C262::Method6.MI.7215<System.Object>()"
		ldstr "class IBase1`1<class Generated738.BaseClass1> on type class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated738.IBase2`2<class Generated738.BaseClass0,class Generated738.BaseClass1>::Method7<object>()
		calli default string(class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>)
		ldstr "G2_C262::Method7.MI.7217<System.Object>()"
		ldstr "class IBase2`2<class Generated738.BaseClass0,class Generated738.BaseClass1> on type class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated738.IBase1`1<class Generated738.BaseClass0>::Method4()
		calli default string(class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>)
		ldstr "G2_C262::Method4.MI.7212()"
		ldstr "class IBase1`1<class Generated738.BaseClass0> on type class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated738.IBase1`1<class Generated738.BaseClass0>::Method5()
		calli default string(class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>)
		ldstr "G2_C262::Method5.7213()"
		ldstr "class IBase1`1<class Generated738.BaseClass0> on type class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated738.IBase1`1<class Generated738.BaseClass0>::Method6<object>()
		calli default string(class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>)
		ldstr "G2_C262::Method6.MI.7215<System.Object>()"
		ldstr "class IBase1`1<class Generated738.BaseClass0> on type class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>::ClassMethod1859()
		calli default string(class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>)
		ldstr "G2_C262::ClassMethod1859.7218()"
		ldstr "class G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1> on type class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>::Method7<object>()
		calli default string(class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>)
		ldstr "G2_C262::Method7.7216<System.Object>()"
		ldstr "class G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1> on type class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>::Method6<object>()
		calli default string(class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>)
		ldstr "G2_C262::Method6.7214<System.Object>()"
		ldstr "class G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1> on type class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>::Method5()
		calli default string(class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>)
		ldstr "G2_C262::Method5.7213()"
		ldstr "class G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1> on type class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>::Method4()
		calli default string(class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>)
		ldstr "G2_C262::Method4.7211()"
		ldstr "class G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1> on type class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>::ClassMethod1325<object>()
		calli default string(class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>)
		ldstr "G1_C5::ClassMethod1325.4807<System.Object>()"
		ldstr "class G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1> on type class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>::ClassMethod1324()
		calli default string(class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>)
		ldstr "G1_C5::ClassMethod1324.4806()"
		ldstr "class G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1> on type class Generated738.G2_C262`2<class Generated738.BaseClass1,class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated738.G1_C5`1<class Generated738.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated738.G1_C5`1<class Generated738.BaseClass0>::ClassMethod1325<object>()
		calli default string(class Generated738.G1_C5`1<class Generated738.BaseClass0>)
		ldstr "G1_C5::ClassMethod1325.4807<System.Object>()"
		ldstr "class G1_C5`1<class Generated738.BaseClass0> on type class Generated738.G1_C5`1<class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated738.G1_C5`1<class Generated738.BaseClass0>::ClassMethod1324()
		calli default string(class Generated738.G1_C5`1<class Generated738.BaseClass0>)
		ldstr "G1_C5::ClassMethod1324.4806()"
		ldstr "class G1_C5`1<class Generated738.BaseClass0> on type class Generated738.G1_C5`1<class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated738.G1_C5`1<class Generated738.BaseClass0>::Method6<object>()
		calli default string(class Generated738.G1_C5`1<class Generated738.BaseClass0>)
		ldstr "G1_C5::Method6.4804<System.Object>()"
		ldstr "class G1_C5`1<class Generated738.BaseClass0> on type class Generated738.G1_C5`1<class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated738.G1_C5`1<class Generated738.BaseClass0>::Method5()
		calli default string(class Generated738.G1_C5`1<class Generated738.BaseClass0>)
		ldstr "G1_C5::Method5.4803()"
		ldstr "class G1_C5`1<class Generated738.BaseClass0> on type class Generated738.G1_C5`1<class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated738.G1_C5`1<class Generated738.BaseClass0>::Method4()
		calli default string(class Generated738.G1_C5`1<class Generated738.BaseClass0>)
		ldstr "G1_C5::Method4.4802()"
		ldstr "class G1_C5`1<class Generated738.BaseClass0> on type class Generated738.G1_C5`1<class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated738.G1_C5`1<class Generated738.BaseClass0>::Method7<object>()
		calli default string(class Generated738.G1_C5`1<class Generated738.BaseClass0>)
		ldstr "G1_C5::Method7.4801<System.Object>()"
		ldstr "class G1_C5`1<class Generated738.BaseClass0> on type class Generated738.G1_C5`1<class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated738.IBase2`2<class Generated738.BaseClass1,class Generated738.BaseClass1>::Method7<object>()
		calli default string(class Generated738.G1_C5`1<class Generated738.BaseClass0>)
		ldstr "G1_C5::Method7.4801<System.Object>()"
		ldstr "class IBase2`2<class Generated738.BaseClass1,class Generated738.BaseClass1> on type class Generated738.G1_C5`1<class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated738.IBase1`1<class Generated738.BaseClass0>::Method4()
		calli default string(class Generated738.G1_C5`1<class Generated738.BaseClass0>)
		ldstr "G1_C5::Method4.4802()"
		ldstr "class IBase1`1<class Generated738.BaseClass0> on type class Generated738.G1_C5`1<class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated738.IBase1`1<class Generated738.BaseClass0>::Method5()
		calli default string(class Generated738.G1_C5`1<class Generated738.BaseClass0>)
		ldstr "G1_C5::Method5.4803()"
		ldstr "class IBase1`1<class Generated738.BaseClass0> on type class Generated738.G1_C5`1<class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated738.IBase1`1<class Generated738.BaseClass0>::Method6<object>()
		calli default string(class Generated738.G1_C5`1<class Generated738.BaseClass0>)
		ldstr "G1_C5::Method6.MI.4805<System.Object>()"
		ldstr "class IBase1`1<class Generated738.BaseClass0> on type class Generated738.G1_C5`1<class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated738.IBase2`2<class Generated738.BaseClass0,class Generated738.BaseClass1>::Method7<object>()
		calli default string(class Generated738.G1_C5`1<class Generated738.BaseClass0>)
		ldstr "G1_C5::Method7.4801<System.Object>()"
		ldstr "class IBase2`2<class Generated738.BaseClass0,class Generated738.BaseClass1> on type class Generated738.G1_C5`1<class Generated738.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated738.G1_C5`1<class Generated738.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated738.G1_C5`1<class Generated738.BaseClass1>::ClassMethod1325<object>()
		calli default string(class Generated738.G1_C5`1<class Generated738.BaseClass1>)
		ldstr "G1_C5::ClassMethod1325.4807<System.Object>()"
		ldstr "class G1_C5`1<class Generated738.BaseClass1> on type class Generated738.G1_C5`1<class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated738.G1_C5`1<class Generated738.BaseClass1>::ClassMethod1324()
		calli default string(class Generated738.G1_C5`1<class Generated738.BaseClass1>)
		ldstr "G1_C5::ClassMethod1324.4806()"
		ldstr "class G1_C5`1<class Generated738.BaseClass1> on type class Generated738.G1_C5`1<class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated738.G1_C5`1<class Generated738.BaseClass1>::Method6<object>()
		calli default string(class Generated738.G1_C5`1<class Generated738.BaseClass1>)
		ldstr "G1_C5::Method6.4804<System.Object>()"
		ldstr "class G1_C5`1<class Generated738.BaseClass1> on type class Generated738.G1_C5`1<class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated738.G1_C5`1<class Generated738.BaseClass1>::Method5()
		calli default string(class Generated738.G1_C5`1<class Generated738.BaseClass1>)
		ldstr "G1_C5::Method5.4803()"
		ldstr "class G1_C5`1<class Generated738.BaseClass1> on type class Generated738.G1_C5`1<class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated738.G1_C5`1<class Generated738.BaseClass1>::Method4()
		calli default string(class Generated738.G1_C5`1<class Generated738.BaseClass1>)
		ldstr "G1_C5::Method4.4802()"
		ldstr "class G1_C5`1<class Generated738.BaseClass1> on type class Generated738.G1_C5`1<class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated738.G1_C5`1<class Generated738.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated738.G1_C5`1<class Generated738.BaseClass1>::Method7<object>()
		calli default string(class Generated738.G1_C5`1<class Generated738.BaseClass1>)
		ldstr "G1_C5::Method7.4801<System.Object>()"
		ldstr "class G1_C5`1<class Generated738.BaseClass1> on type class Generated738.G1_C5`1<class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated738.IBase2`2<class Generated738.BaseClass1,class Generated738.BaseClass1>::Method7<object>()
		calli default string(class Generated738.G1_C5`1<class Generated738.BaseClass1>)
		ldstr "G1_C5::Method7.4801<System.Object>()"
		ldstr "class IBase2`2<class Generated738.BaseClass1,class Generated738.BaseClass1> on type class Generated738.G1_C5`1<class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated738.IBase1`1<class Generated738.BaseClass1>::Method4()
		calli default string(class Generated738.G1_C5`1<class Generated738.BaseClass1>)
		ldstr "G1_C5::Method4.4802()"
		ldstr "class IBase1`1<class Generated738.BaseClass1> on type class Generated738.G1_C5`1<class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated738.IBase1`1<class Generated738.BaseClass1>::Method5()
		calli default string(class Generated738.G1_C5`1<class Generated738.BaseClass1>)
		ldstr "G1_C5::Method5.4803()"
		ldstr "class IBase1`1<class Generated738.BaseClass1> on type class Generated738.G1_C5`1<class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated738.IBase1`1<class Generated738.BaseClass1>::Method6<object>()
		calli default string(class Generated738.G1_C5`1<class Generated738.BaseClass1>)
		ldstr "G1_C5::Method6.MI.4805<System.Object>()"
		ldstr "class IBase1`1<class Generated738.BaseClass1> on type class Generated738.G1_C5`1<class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated738.IBase2`2<class Generated738.BaseClass0,class Generated738.BaseClass1>::Method7<object>()
		calli default string(class Generated738.G1_C5`1<class Generated738.BaseClass1>)
		ldstr "G1_C5::Method7.4801<System.Object>()"
		ldstr "class IBase2`2<class Generated738.BaseClass0,class Generated738.BaseClass1> on type class Generated738.G1_C5`1<class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated738.IBase1`1<class Generated738.BaseClass0>::Method4()
		calli default string(class Generated738.G1_C5`1<class Generated738.BaseClass1>)
		ldstr "G1_C5::Method4.4802()"
		ldstr "class IBase1`1<class Generated738.BaseClass0> on type class Generated738.G1_C5`1<class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated738.IBase1`1<class Generated738.BaseClass0>::Method5()
		calli default string(class Generated738.G1_C5`1<class Generated738.BaseClass1>)
		ldstr "G1_C5::Method5.4803()"
		ldstr "class IBase1`1<class Generated738.BaseClass0> on type class Generated738.G1_C5`1<class Generated738.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated738.IBase1`1<class Generated738.BaseClass0>::Method6<object>()
		calli default string(class Generated738.G1_C5`1<class Generated738.BaseClass1>)
		ldstr "G1_C5::Method6.MI.4805<System.Object>()"
		ldstr "class IBase1`1<class Generated738.BaseClass0> on type class Generated738.G1_C5`1<class Generated738.BaseClass1>"
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
		call void Generated738.Generated738::MethodCallingTest()
		call void Generated738.Generated738::ConstrainedCallsTest()
		call void Generated738.Generated738::StructConstrainedInterfaceCallsTest()
		call void Generated738.Generated738::CalliTest()
		ldc.i4 100
		ret
	}
}
