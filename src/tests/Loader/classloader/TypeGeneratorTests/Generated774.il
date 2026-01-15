// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.class public Generated774.BaseClass0 
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void [mscorlib]System.Object::.ctor()
		ret
	}
}
.class public Generated774.BaseClass1 
		extends Generated774.BaseClass0
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void Generated774.BaseClass0::.ctor()
		ret
	}
}
.class public Generated774.G3_C1246`1<T0> 
		extends class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>
		implements class Generated774.IBase1`1<!T0> 
{
	.method public hidebysig virtual instance string Method4() cil managed noinlining { 
		ldstr "G3_C1246::Method4.15087()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<T0>.Method4'() cil managed noinlining { 
		.override method instance string class Generated774.IBase1`1<!T0>::Method4()
		ldstr "G3_C1246::Method4.MI.15088()"
		ret
	}
	.method public hidebysig virtual instance string Method5() cil managed noinlining { 
		ldstr "G3_C1246::Method5.15089()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<T0>.Method5'() cil managed noinlining { 
		.override method instance string class Generated774.IBase1`1<!T0>::Method5()
		ldstr "G3_C1246::Method5.MI.15090()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G3_C1246::Method6.15091<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<T0>.Method6'<M0>() cil managed noinlining { 
		.override method instance string class Generated774.IBase1`1<!T0>::Method6<[1]>()
		ldstr "G3_C1246::Method6.MI.15092<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod3870() cil managed noinlining { 
		ldstr "G3_C1246::ClassMethod3870.15093()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod3871<M0>() cil managed noinlining { 
		ldstr "G3_C1246::ClassMethod3871.15094<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod3872<M0>() cil managed noinlining { 
		ldstr "G3_C1246::ClassMethod3872.15095<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C293<class Generated774.BaseClass1,class Generated774.BaseClass0>.ClassMethod1913'() cil managed noinlining { 
		.override method instance string class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::ClassMethod1913()
		ldstr "G3_C1246::ClassMethod1913.MI.15096()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C293<class Generated774.BaseClass1,class Generated774.BaseClass0>.ClassMethod1915'<M0>() cil managed noinlining { 
		.override method instance string class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::ClassMethod1915<[1]>()
		ldstr "G3_C1246::ClassMethod1915.MI.15097<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::.ctor()
		ret
	}
}
.class public Generated774.G2_C293`2<T0, T1> 
		extends class Generated774.G1_C6`2<class Generated774.BaseClass1,!T1>
		implements class Generated774.IBase2`2<class Generated774.BaseClass0,class Generated774.BaseClass0> 
{
	.method public hidebysig virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G2_C293::Method7.7469<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase2<class Generated774.BaseClass0,class Generated774.BaseClass0>.Method7'<M0>() cil managed noinlining { 
		.override method instance string class Generated774.IBase2`2<class Generated774.BaseClass0,class Generated774.BaseClass0>::Method7<[1]>()
		ldstr "G2_C293::Method7.MI.7470<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1913() cil managed noinlining { 
		ldstr "G2_C293::ClassMethod1913.7471()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1914() cil managed noinlining { 
		ldstr "G2_C293::ClassMethod1914.7472()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1915<M0>() cil managed noinlining { 
		ldstr "G2_C293::ClassMethod1915.7473<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1916<M0>() cil managed noinlining { 
		ldstr "G2_C293::ClassMethod1916.7474<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C6<class Generated774.BaseClass1,T1>.ClassMethod1327'<M0>() cil managed noinlining { 
		.override method instance string class Generated774.G1_C6`2<class Generated774.BaseClass1,!T1>::ClassMethod1327<[1]>()
		ldstr "G2_C293::ClassMethod1327.MI.7475<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class Generated774.G1_C6`2<class Generated774.BaseClass1,!T1>::.ctor()
		ret
	}
}
.class interface public abstract Generated774.IBase1`1<+T0> 
{
	.method public hidebysig newslot abstract virtual instance string Method4() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method5() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method6<M0>() cil managed { }
}
.class public Generated774.G1_C6`2<T0, T1> 
		implements class Generated774.IBase2`2<class Generated774.BaseClass1,!T0> 
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
.class interface public abstract Generated774.IBase2`2<+T0, -T1> 
{
	.method public hidebysig newslot abstract virtual instance string Method7<M0>() cil managed { }
}
.class public auto ansi beforefieldinit Generated774.Generated774 {
	.method static void M.BaseClass0<(Generated774.BaseClass0)W>(!!W inst, string exp) cil managed {
		.maxstack 5
		.locals init (string[] actualResults)
		ldc.i4.s    0
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.BaseClass0<(Generated774.BaseClass0)W>(!!W inst, string exp)"
		ldc.i4.s 0
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.BaseClass1<(Generated774.BaseClass1)W>(!!W inst, string exp) cil managed {
		.maxstack 5
		.locals init (string[] actualResults)
		ldc.i4.s    0
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.BaseClass1<(Generated774.BaseClass1)W>(!!W inst, string exp)"
		ldc.i4.s 0
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1246.T<T0,(class Generated774.G3_C1246`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 18
		.locals init (string[] actualResults)
		ldc.i4.s    13
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1246.T<T0,(class Generated774.G3_C1246`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 13
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G3_C1246`1<!!T0>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G3_C1246`1<!!T0>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G3_C1246`1<!!T0>::ClassMethod1913()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G3_C1246`1<!!T0>::ClassMethod1914()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G3_C1246`1<!!T0>::ClassMethod1915<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G3_C1246`1<!!T0>::ClassMethod1916<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G3_C1246`1<!!T0>::ClassMethod3870()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G3_C1246`1<!!T0>::ClassMethod3871<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G3_C1246`1<!!T0>::ClassMethod3872<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G3_C1246`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G3_C1246`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G3_C1246`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G3_C1246`1<!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1246.A<(class Generated774.G3_C1246`1<class Generated774.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 18
		.locals init (string[] actualResults)
		ldc.i4.s    13
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1246.A<(class Generated774.G3_C1246`1<class Generated774.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 13
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G3_C1246`1<class Generated774.BaseClass0>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G3_C1246`1<class Generated774.BaseClass0>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G3_C1246`1<class Generated774.BaseClass0>::ClassMethod1913()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G3_C1246`1<class Generated774.BaseClass0>::ClassMethod1914()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G3_C1246`1<class Generated774.BaseClass0>::ClassMethod1915<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G3_C1246`1<class Generated774.BaseClass0>::ClassMethod1916<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G3_C1246`1<class Generated774.BaseClass0>::ClassMethod3870()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G3_C1246`1<class Generated774.BaseClass0>::ClassMethod3871<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G3_C1246`1<class Generated774.BaseClass0>::ClassMethod3872<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G3_C1246`1<class Generated774.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G3_C1246`1<class Generated774.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G3_C1246`1<class Generated774.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G3_C1246`1<class Generated774.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1246.B<(class Generated774.G3_C1246`1<class Generated774.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 18
		.locals init (string[] actualResults)
		ldc.i4.s    13
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1246.B<(class Generated774.G3_C1246`1<class Generated774.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 13
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G3_C1246`1<class Generated774.BaseClass1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G3_C1246`1<class Generated774.BaseClass1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G3_C1246`1<class Generated774.BaseClass1>::ClassMethod1913()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G3_C1246`1<class Generated774.BaseClass1>::ClassMethod1914()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G3_C1246`1<class Generated774.BaseClass1>::ClassMethod1915<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G3_C1246`1<class Generated774.BaseClass1>::ClassMethod1916<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G3_C1246`1<class Generated774.BaseClass1>::ClassMethod3870()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G3_C1246`1<class Generated774.BaseClass1>::ClassMethod3871<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G3_C1246`1<class Generated774.BaseClass1>::ClassMethod3872<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G3_C1246`1<class Generated774.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G3_C1246`1<class Generated774.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G3_C1246`1<class Generated774.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G3_C1246`1<class Generated774.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C293.T.T<T0,T1,(class Generated774.G2_C293`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C293.T.T<T0,T1,(class Generated774.G2_C293`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G2_C293`2<!!T0,!!T1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G2_C293`2<!!T0,!!T1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G2_C293`2<!!T0,!!T1>::ClassMethod1913()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G2_C293`2<!!T0,!!T1>::ClassMethod1914()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G2_C293`2<!!T0,!!T1>::ClassMethod1915<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G2_C293`2<!!T0,!!T1>::ClassMethod1916<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G2_C293`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C293.A.T<T1,(class Generated774.G2_C293`2<class Generated774.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C293.A.T<T1,(class Generated774.G2_C293`2<class Generated774.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G2_C293`2<class Generated774.BaseClass0,!!T1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G2_C293`2<class Generated774.BaseClass0,!!T1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G2_C293`2<class Generated774.BaseClass0,!!T1>::ClassMethod1913()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G2_C293`2<class Generated774.BaseClass0,!!T1>::ClassMethod1914()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G2_C293`2<class Generated774.BaseClass0,!!T1>::ClassMethod1915<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G2_C293`2<class Generated774.BaseClass0,!!T1>::ClassMethod1916<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G2_C293`2<class Generated774.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C293.A.A<(class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C293.A.A<(class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>::ClassMethod1913()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>::ClassMethod1914()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>::ClassMethod1915<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>::ClassMethod1916<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C293.A.B<(class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C293.A.B<(class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>::ClassMethod1913()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>::ClassMethod1914()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>::ClassMethod1915<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>::ClassMethod1916<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C293.B.T<T1,(class Generated774.G2_C293`2<class Generated774.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C293.B.T<T1,(class Generated774.G2_C293`2<class Generated774.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G2_C293`2<class Generated774.BaseClass1,!!T1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G2_C293`2<class Generated774.BaseClass1,!!T1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G2_C293`2<class Generated774.BaseClass1,!!T1>::ClassMethod1913()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G2_C293`2<class Generated774.BaseClass1,!!T1>::ClassMethod1914()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G2_C293`2<class Generated774.BaseClass1,!!T1>::ClassMethod1915<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G2_C293`2<class Generated774.BaseClass1,!!T1>::ClassMethod1916<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G2_C293`2<class Generated774.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C293.B.A<(class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C293.B.A<(class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::ClassMethod1913()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::ClassMethod1914()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::ClassMethod1915<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::ClassMethod1916<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C293.B.B<(class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C293.B.B<(class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>::ClassMethod1913()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>::ClassMethod1914()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>::ClassMethod1915<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>::ClassMethod1916<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.T<T0,(class Generated774.IBase1`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.T<T0,(class Generated774.IBase1`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.IBase1`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.IBase1`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.IBase1`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.A<(class Generated774.IBase1`1<class Generated774.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.A<(class Generated774.IBase1`1<class Generated774.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.IBase1`1<class Generated774.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.IBase1`1<class Generated774.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.IBase1`1<class Generated774.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.B<(class Generated774.IBase1`1<class Generated774.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.B<(class Generated774.IBase1`1<class Generated774.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.IBase1`1<class Generated774.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.IBase1`1<class Generated774.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.IBase1`1<class Generated774.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C6.T.T<T0,T1,(class Generated774.G1_C6`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C6.T.T<T0,T1,(class Generated774.G1_C6`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G1_C6`2<!!T0,!!T1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G1_C6`2<!!T0,!!T1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G1_C6`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C6.A.T<T1,(class Generated774.G1_C6`2<class Generated774.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C6.A.T<T1,(class Generated774.G1_C6`2<class Generated774.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G1_C6`2<class Generated774.BaseClass0,!!T1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G1_C6`2<class Generated774.BaseClass0,!!T1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G1_C6`2<class Generated774.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C6.A.A<(class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C6.A.A<(class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass0>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass0>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C6.A.B<(class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C6.A.B<(class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C6.B.T<T1,(class Generated774.G1_C6`2<class Generated774.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C6.B.T<T1,(class Generated774.G1_C6`2<class Generated774.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G1_C6`2<class Generated774.BaseClass1,!!T1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G1_C6`2<class Generated774.BaseClass1,!!T1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G1_C6`2<class Generated774.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C6.B.A<(class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C6.B.A<(class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C6.B.B<(class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C6.B.B<(class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.T.T<T0,T1,(class Generated774.IBase2`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.T.T<T0,T1,(class Generated774.IBase2`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.IBase2`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.T<T1,(class Generated774.IBase2`2<class Generated774.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.T<T1,(class Generated774.IBase2`2<class Generated774.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.IBase2`2<class Generated774.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.A<(class Generated774.IBase2`2<class Generated774.BaseClass0,class Generated774.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.A<(class Generated774.IBase2`2<class Generated774.BaseClass0,class Generated774.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.IBase2`2<class Generated774.BaseClass0,class Generated774.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.B<(class Generated774.IBase2`2<class Generated774.BaseClass0,class Generated774.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.B<(class Generated774.IBase2`2<class Generated774.BaseClass0,class Generated774.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.IBase2`2<class Generated774.BaseClass0,class Generated774.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.T<T1,(class Generated774.IBase2`2<class Generated774.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.T<T1,(class Generated774.IBase2`2<class Generated774.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.IBase2`2<class Generated774.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.A<(class Generated774.IBase2`2<class Generated774.BaseClass1,class Generated774.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.A<(class Generated774.IBase2`2<class Generated774.BaseClass1,class Generated774.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.IBase2`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.B<(class Generated774.IBase2`2<class Generated774.BaseClass1,class Generated774.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.B<(class Generated774.IBase2`2<class Generated774.BaseClass1,class Generated774.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated774.IBase2`2<class Generated774.BaseClass1,class Generated774.BaseClass1>::Method7<object>()
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
		newobj instance void class Generated774.G3_C1246`1<class Generated774.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0>
		callvirt instance string class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::ClassMethod1327<object>()
		ldstr "G2_C293::ClassMethod1327.MI.7475<System.Object>()"
		ldstr "class G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0> on type class Generated774.G3_C1246`1<class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0>
		callvirt instance string class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::ClassMethod1326()
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0> on type class Generated774.G3_C1246`1<class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0>
		callvirt instance string class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::Method7<object>()
		ldstr "G2_C293::Method7.7469<System.Object>()"
		ldstr "class G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0> on type class Generated774.G3_C1246`1<class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated774.IBase2`2<class Generated774.BaseClass1,class Generated774.BaseClass1>::Method7<object>()
		ldstr "G2_C293::Method7.7469<System.Object>()"
		ldstr "class IBase2`2<class Generated774.BaseClass1,class Generated774.BaseClass1> on type class Generated774.G3_C1246`1<class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated774.IBase2`2<class Generated774.BaseClass0,class Generated774.BaseClass1>::Method7<object>()
		ldstr "G2_C293::Method7.MI.7470<System.Object>()"
		ldstr "class IBase2`2<class Generated774.BaseClass0,class Generated774.BaseClass1> on type class Generated774.G3_C1246`1<class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>
		callvirt instance string class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::ClassMethod1916<object>()
		ldstr "G2_C293::ClassMethod1916.7474<System.Object>()"
		ldstr "class G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0> on type class Generated774.G3_C1246`1<class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>
		callvirt instance string class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::ClassMethod1915<object>()
		ldstr "G3_C1246::ClassMethod1915.MI.15097<System.Object>()"
		ldstr "class G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0> on type class Generated774.G3_C1246`1<class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>
		callvirt instance string class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::ClassMethod1914()
		ldstr "G2_C293::ClassMethod1914.7472()"
		ldstr "class G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0> on type class Generated774.G3_C1246`1<class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>
		callvirt instance string class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::ClassMethod1913()
		ldstr "G3_C1246::ClassMethod1913.MI.15096()"
		ldstr "class G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0> on type class Generated774.G3_C1246`1<class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>
		callvirt instance string class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::Method7<object>()
		ldstr "G2_C293::Method7.7469<System.Object>()"
		ldstr "class G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0> on type class Generated774.G3_C1246`1<class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>
		callvirt instance string class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::ClassMethod1327<object>()
		ldstr "G2_C293::ClassMethod1327.MI.7475<System.Object>()"
		ldstr "class G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0> on type class Generated774.G3_C1246`1<class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>
		callvirt instance string class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::ClassMethod1326()
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0> on type class Generated774.G3_C1246`1<class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated774.IBase2`2<class Generated774.BaseClass0,class Generated774.BaseClass0>::Method7<object>()
		ldstr "G2_C293::Method7.MI.7470<System.Object>()"
		ldstr "class IBase2`2<class Generated774.BaseClass0,class Generated774.BaseClass0> on type class Generated774.G3_C1246`1<class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated774.G3_C1246`1<class Generated774.BaseClass0>
		callvirt instance string class Generated774.G3_C1246`1<class Generated774.BaseClass0>::ClassMethod3872<object>()
		ldstr "G3_C1246::ClassMethod3872.15095<System.Object>()"
		ldstr "class G3_C1246`1<class Generated774.BaseClass0> on type class Generated774.G3_C1246`1<class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated774.G3_C1246`1<class Generated774.BaseClass0>
		callvirt instance string class Generated774.G3_C1246`1<class Generated774.BaseClass0>::ClassMethod3871<object>()
		ldstr "G3_C1246::ClassMethod3871.15094<System.Object>()"
		ldstr "class G3_C1246`1<class Generated774.BaseClass0> on type class Generated774.G3_C1246`1<class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated774.G3_C1246`1<class Generated774.BaseClass0>
		callvirt instance string class Generated774.G3_C1246`1<class Generated774.BaseClass0>::ClassMethod3870()
		ldstr "G3_C1246::ClassMethod3870.15093()"
		ldstr "class G3_C1246`1<class Generated774.BaseClass0> on type class Generated774.G3_C1246`1<class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated774.G3_C1246`1<class Generated774.BaseClass0>
		callvirt instance string class Generated774.G3_C1246`1<class Generated774.BaseClass0>::Method6<object>()
		ldstr "G3_C1246::Method6.15091<System.Object>()"
		ldstr "class G3_C1246`1<class Generated774.BaseClass0> on type class Generated774.G3_C1246`1<class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated774.G3_C1246`1<class Generated774.BaseClass0>
		callvirt instance string class Generated774.G3_C1246`1<class Generated774.BaseClass0>::Method5()
		ldstr "G3_C1246::Method5.15089()"
		ldstr "class G3_C1246`1<class Generated774.BaseClass0> on type class Generated774.G3_C1246`1<class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated774.G3_C1246`1<class Generated774.BaseClass0>
		callvirt instance string class Generated774.G3_C1246`1<class Generated774.BaseClass0>::Method4()
		ldstr "G3_C1246::Method4.15087()"
		ldstr "class G3_C1246`1<class Generated774.BaseClass0> on type class Generated774.G3_C1246`1<class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated774.G3_C1246`1<class Generated774.BaseClass0>
		callvirt instance string class Generated774.G3_C1246`1<class Generated774.BaseClass0>::ClassMethod1916<object>()
		ldstr "G2_C293::ClassMethod1916.7474<System.Object>()"
		ldstr "class G3_C1246`1<class Generated774.BaseClass0> on type class Generated774.G3_C1246`1<class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated774.G3_C1246`1<class Generated774.BaseClass0>
		callvirt instance string class Generated774.G3_C1246`1<class Generated774.BaseClass0>::ClassMethod1915<object>()
		ldstr "G3_C1246::ClassMethod1915.MI.15097<System.Object>()"
		ldstr "class G3_C1246`1<class Generated774.BaseClass0> on type class Generated774.G3_C1246`1<class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated774.G3_C1246`1<class Generated774.BaseClass0>
		callvirt instance string class Generated774.G3_C1246`1<class Generated774.BaseClass0>::ClassMethod1914()
		ldstr "G2_C293::ClassMethod1914.7472()"
		ldstr "class G3_C1246`1<class Generated774.BaseClass0> on type class Generated774.G3_C1246`1<class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated774.G3_C1246`1<class Generated774.BaseClass0>
		callvirt instance string class Generated774.G3_C1246`1<class Generated774.BaseClass0>::ClassMethod1913()
		ldstr "G3_C1246::ClassMethod1913.MI.15096()"
		ldstr "class G3_C1246`1<class Generated774.BaseClass0> on type class Generated774.G3_C1246`1<class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated774.G3_C1246`1<class Generated774.BaseClass0>
		callvirt instance string class Generated774.G3_C1246`1<class Generated774.BaseClass0>::Method7<object>()
		ldstr "G2_C293::Method7.7469<System.Object>()"
		ldstr "class G3_C1246`1<class Generated774.BaseClass0> on type class Generated774.G3_C1246`1<class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated774.G3_C1246`1<class Generated774.BaseClass0>
		callvirt instance string class Generated774.G3_C1246`1<class Generated774.BaseClass0>::ClassMethod1327<object>()
		ldstr "G2_C293::ClassMethod1327.MI.7475<System.Object>()"
		ldstr "class G3_C1246`1<class Generated774.BaseClass0> on type class Generated774.G3_C1246`1<class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated774.G3_C1246`1<class Generated774.BaseClass0>
		callvirt instance string class Generated774.G3_C1246`1<class Generated774.BaseClass0>::ClassMethod1326()
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G3_C1246`1<class Generated774.BaseClass0> on type class Generated774.G3_C1246`1<class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated774.IBase1`1<class Generated774.BaseClass0>::Method4()
		ldstr "G3_C1246::Method4.MI.15088()"
		ldstr "class IBase1`1<class Generated774.BaseClass0> on type class Generated774.G3_C1246`1<class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated774.IBase1`1<class Generated774.BaseClass0>::Method5()
		ldstr "G3_C1246::Method5.MI.15090()"
		ldstr "class IBase1`1<class Generated774.BaseClass0> on type class Generated774.G3_C1246`1<class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated774.IBase1`1<class Generated774.BaseClass0>::Method6<object>()
		ldstr "G3_C1246::Method6.MI.15092<System.Object>()"
		ldstr "class IBase1`1<class Generated774.BaseClass0> on type class Generated774.G3_C1246`1<class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated774.G3_C1246`1<class Generated774.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0>
		callvirt instance string class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::ClassMethod1327<object>()
		ldstr "G2_C293::ClassMethod1327.MI.7475<System.Object>()"
		ldstr "class G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0> on type class Generated774.G3_C1246`1<class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0>
		callvirt instance string class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::ClassMethod1326()
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0> on type class Generated774.G3_C1246`1<class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0>
		callvirt instance string class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::Method7<object>()
		ldstr "G2_C293::Method7.7469<System.Object>()"
		ldstr "class G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0> on type class Generated774.G3_C1246`1<class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated774.IBase2`2<class Generated774.BaseClass1,class Generated774.BaseClass1>::Method7<object>()
		ldstr "G2_C293::Method7.7469<System.Object>()"
		ldstr "class IBase2`2<class Generated774.BaseClass1,class Generated774.BaseClass1> on type class Generated774.G3_C1246`1<class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated774.IBase2`2<class Generated774.BaseClass0,class Generated774.BaseClass1>::Method7<object>()
		ldstr "G2_C293::Method7.MI.7470<System.Object>()"
		ldstr "class IBase2`2<class Generated774.BaseClass0,class Generated774.BaseClass1> on type class Generated774.G3_C1246`1<class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>
		callvirt instance string class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::ClassMethod1916<object>()
		ldstr "G2_C293::ClassMethod1916.7474<System.Object>()"
		ldstr "class G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0> on type class Generated774.G3_C1246`1<class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>
		callvirt instance string class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::ClassMethod1915<object>()
		ldstr "G3_C1246::ClassMethod1915.MI.15097<System.Object>()"
		ldstr "class G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0> on type class Generated774.G3_C1246`1<class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>
		callvirt instance string class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::ClassMethod1914()
		ldstr "G2_C293::ClassMethod1914.7472()"
		ldstr "class G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0> on type class Generated774.G3_C1246`1<class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>
		callvirt instance string class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::ClassMethod1913()
		ldstr "G3_C1246::ClassMethod1913.MI.15096()"
		ldstr "class G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0> on type class Generated774.G3_C1246`1<class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>
		callvirt instance string class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::Method7<object>()
		ldstr "G2_C293::Method7.7469<System.Object>()"
		ldstr "class G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0> on type class Generated774.G3_C1246`1<class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>
		callvirt instance string class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::ClassMethod1327<object>()
		ldstr "G2_C293::ClassMethod1327.MI.7475<System.Object>()"
		ldstr "class G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0> on type class Generated774.G3_C1246`1<class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>
		callvirt instance string class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::ClassMethod1326()
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0> on type class Generated774.G3_C1246`1<class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated774.IBase2`2<class Generated774.BaseClass0,class Generated774.BaseClass0>::Method7<object>()
		ldstr "G2_C293::Method7.MI.7470<System.Object>()"
		ldstr "class IBase2`2<class Generated774.BaseClass0,class Generated774.BaseClass0> on type class Generated774.G3_C1246`1<class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated774.G3_C1246`1<class Generated774.BaseClass1>
		callvirt instance string class Generated774.G3_C1246`1<class Generated774.BaseClass1>::ClassMethod3872<object>()
		ldstr "G3_C1246::ClassMethod3872.15095<System.Object>()"
		ldstr "class G3_C1246`1<class Generated774.BaseClass1> on type class Generated774.G3_C1246`1<class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated774.G3_C1246`1<class Generated774.BaseClass1>
		callvirt instance string class Generated774.G3_C1246`1<class Generated774.BaseClass1>::ClassMethod3871<object>()
		ldstr "G3_C1246::ClassMethod3871.15094<System.Object>()"
		ldstr "class G3_C1246`1<class Generated774.BaseClass1> on type class Generated774.G3_C1246`1<class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated774.G3_C1246`1<class Generated774.BaseClass1>
		callvirt instance string class Generated774.G3_C1246`1<class Generated774.BaseClass1>::ClassMethod3870()
		ldstr "G3_C1246::ClassMethod3870.15093()"
		ldstr "class G3_C1246`1<class Generated774.BaseClass1> on type class Generated774.G3_C1246`1<class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated774.G3_C1246`1<class Generated774.BaseClass1>
		callvirt instance string class Generated774.G3_C1246`1<class Generated774.BaseClass1>::Method6<object>()
		ldstr "G3_C1246::Method6.15091<System.Object>()"
		ldstr "class G3_C1246`1<class Generated774.BaseClass1> on type class Generated774.G3_C1246`1<class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated774.G3_C1246`1<class Generated774.BaseClass1>
		callvirt instance string class Generated774.G3_C1246`1<class Generated774.BaseClass1>::Method5()
		ldstr "G3_C1246::Method5.15089()"
		ldstr "class G3_C1246`1<class Generated774.BaseClass1> on type class Generated774.G3_C1246`1<class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated774.G3_C1246`1<class Generated774.BaseClass1>
		callvirt instance string class Generated774.G3_C1246`1<class Generated774.BaseClass1>::Method4()
		ldstr "G3_C1246::Method4.15087()"
		ldstr "class G3_C1246`1<class Generated774.BaseClass1> on type class Generated774.G3_C1246`1<class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated774.G3_C1246`1<class Generated774.BaseClass1>
		callvirt instance string class Generated774.G3_C1246`1<class Generated774.BaseClass1>::ClassMethod1916<object>()
		ldstr "G2_C293::ClassMethod1916.7474<System.Object>()"
		ldstr "class G3_C1246`1<class Generated774.BaseClass1> on type class Generated774.G3_C1246`1<class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated774.G3_C1246`1<class Generated774.BaseClass1>
		callvirt instance string class Generated774.G3_C1246`1<class Generated774.BaseClass1>::ClassMethod1915<object>()
		ldstr "G3_C1246::ClassMethod1915.MI.15097<System.Object>()"
		ldstr "class G3_C1246`1<class Generated774.BaseClass1> on type class Generated774.G3_C1246`1<class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated774.G3_C1246`1<class Generated774.BaseClass1>
		callvirt instance string class Generated774.G3_C1246`1<class Generated774.BaseClass1>::ClassMethod1914()
		ldstr "G2_C293::ClassMethod1914.7472()"
		ldstr "class G3_C1246`1<class Generated774.BaseClass1> on type class Generated774.G3_C1246`1<class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated774.G3_C1246`1<class Generated774.BaseClass1>
		callvirt instance string class Generated774.G3_C1246`1<class Generated774.BaseClass1>::ClassMethod1913()
		ldstr "G3_C1246::ClassMethod1913.MI.15096()"
		ldstr "class G3_C1246`1<class Generated774.BaseClass1> on type class Generated774.G3_C1246`1<class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated774.G3_C1246`1<class Generated774.BaseClass1>
		callvirt instance string class Generated774.G3_C1246`1<class Generated774.BaseClass1>::Method7<object>()
		ldstr "G2_C293::Method7.7469<System.Object>()"
		ldstr "class G3_C1246`1<class Generated774.BaseClass1> on type class Generated774.G3_C1246`1<class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated774.G3_C1246`1<class Generated774.BaseClass1>
		callvirt instance string class Generated774.G3_C1246`1<class Generated774.BaseClass1>::ClassMethod1327<object>()
		ldstr "G2_C293::ClassMethod1327.MI.7475<System.Object>()"
		ldstr "class G3_C1246`1<class Generated774.BaseClass1> on type class Generated774.G3_C1246`1<class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated774.G3_C1246`1<class Generated774.BaseClass1>
		callvirt instance string class Generated774.G3_C1246`1<class Generated774.BaseClass1>::ClassMethod1326()
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G3_C1246`1<class Generated774.BaseClass1> on type class Generated774.G3_C1246`1<class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated774.IBase1`1<class Generated774.BaseClass1>::Method4()
		ldstr "G3_C1246::Method4.MI.15088()"
		ldstr "class IBase1`1<class Generated774.BaseClass1> on type class Generated774.G3_C1246`1<class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated774.IBase1`1<class Generated774.BaseClass1>::Method5()
		ldstr "G3_C1246::Method5.MI.15090()"
		ldstr "class IBase1`1<class Generated774.BaseClass1> on type class Generated774.G3_C1246`1<class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated774.IBase1`1<class Generated774.BaseClass1>::Method6<object>()
		ldstr "G3_C1246::Method6.MI.15092<System.Object>()"
		ldstr "class IBase1`1<class Generated774.BaseClass1> on type class Generated774.G3_C1246`1<class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated774.IBase1`1<class Generated774.BaseClass0>::Method4()
		ldstr "G3_C1246::Method4.MI.15088()"
		ldstr "class IBase1`1<class Generated774.BaseClass0> on type class Generated774.G3_C1246`1<class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated774.IBase1`1<class Generated774.BaseClass0>::Method5()
		ldstr "G3_C1246::Method5.MI.15090()"
		ldstr "class IBase1`1<class Generated774.BaseClass0> on type class Generated774.G3_C1246`1<class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated774.IBase1`1<class Generated774.BaseClass0>::Method6<object>()
		ldstr "G3_C1246::Method6.MI.15092<System.Object>()"
		ldstr "class IBase1`1<class Generated774.BaseClass0> on type class Generated774.G3_C1246`1<class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0>
		callvirt instance string class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::ClassMethod1327<object>()
		ldstr "G2_C293::ClassMethod1327.MI.7475<System.Object>()"
		ldstr "class G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0> on type class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0>
		callvirt instance string class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::ClassMethod1326()
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0> on type class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0>
		callvirt instance string class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::Method7<object>()
		ldstr "G2_C293::Method7.7469<System.Object>()"
		ldstr "class G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0> on type class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated774.IBase2`2<class Generated774.BaseClass1,class Generated774.BaseClass1>::Method7<object>()
		ldstr "G2_C293::Method7.7469<System.Object>()"
		ldstr "class IBase2`2<class Generated774.BaseClass1,class Generated774.BaseClass1> on type class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated774.IBase2`2<class Generated774.BaseClass0,class Generated774.BaseClass1>::Method7<object>()
		ldstr "G2_C293::Method7.MI.7470<System.Object>()"
		ldstr "class IBase2`2<class Generated774.BaseClass0,class Generated774.BaseClass1> on type class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>
		callvirt instance string class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>::ClassMethod1916<object>()
		ldstr "G2_C293::ClassMethod1916.7474<System.Object>()"
		ldstr "class G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0> on type class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>
		callvirt instance string class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>::ClassMethod1915<object>()
		ldstr "G2_C293::ClassMethod1915.7473<System.Object>()"
		ldstr "class G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0> on type class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>
		callvirt instance string class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>::ClassMethod1914()
		ldstr "G2_C293::ClassMethod1914.7472()"
		ldstr "class G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0> on type class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>
		callvirt instance string class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>::ClassMethod1913()
		ldstr "G2_C293::ClassMethod1913.7471()"
		ldstr "class G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0> on type class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>
		callvirt instance string class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>::Method7<object>()
		ldstr "G2_C293::Method7.7469<System.Object>()"
		ldstr "class G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0> on type class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>
		callvirt instance string class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>::ClassMethod1327<object>()
		ldstr "G2_C293::ClassMethod1327.MI.7475<System.Object>()"
		ldstr "class G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0> on type class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>
		callvirt instance string class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>::ClassMethod1326()
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0> on type class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated774.IBase2`2<class Generated774.BaseClass0,class Generated774.BaseClass0>::Method7<object>()
		ldstr "G2_C293::Method7.MI.7470<System.Object>()"
		ldstr "class IBase2`2<class Generated774.BaseClass0,class Generated774.BaseClass0> on type class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass1>
		callvirt instance string class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass1>::ClassMethod1327<object>()
		ldstr "G2_C293::ClassMethod1327.MI.7475<System.Object>()"
		ldstr "class G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass1> on type class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass1>
		callvirt instance string class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass1>::ClassMethod1326()
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass1> on type class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass1>
		callvirt instance string class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass1>::Method7<object>()
		ldstr "G2_C293::Method7.7469<System.Object>()"
		ldstr "class G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass1> on type class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated774.IBase2`2<class Generated774.BaseClass1,class Generated774.BaseClass1>::Method7<object>()
		ldstr "G2_C293::Method7.7469<System.Object>()"
		ldstr "class IBase2`2<class Generated774.BaseClass1,class Generated774.BaseClass1> on type class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated774.IBase2`2<class Generated774.BaseClass0,class Generated774.BaseClass1>::Method7<object>()
		ldstr "G2_C293::Method7.MI.7470<System.Object>()"
		ldstr "class IBase2`2<class Generated774.BaseClass0,class Generated774.BaseClass1> on type class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>
		callvirt instance string class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>::ClassMethod1916<object>()
		ldstr "G2_C293::ClassMethod1916.7474<System.Object>()"
		ldstr "class G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1> on type class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>
		callvirt instance string class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>::ClassMethod1915<object>()
		ldstr "G2_C293::ClassMethod1915.7473<System.Object>()"
		ldstr "class G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1> on type class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>
		callvirt instance string class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>::ClassMethod1914()
		ldstr "G2_C293::ClassMethod1914.7472()"
		ldstr "class G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1> on type class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>
		callvirt instance string class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>::ClassMethod1913()
		ldstr "G2_C293::ClassMethod1913.7471()"
		ldstr "class G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1> on type class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>
		callvirt instance string class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>::Method7<object>()
		ldstr "G2_C293::Method7.7469<System.Object>()"
		ldstr "class G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1> on type class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>
		callvirt instance string class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>::ClassMethod1327<object>()
		ldstr "G2_C293::ClassMethod1327.MI.7475<System.Object>()"
		ldstr "class G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1> on type class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>
		callvirt instance string class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>::ClassMethod1326()
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1> on type class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated774.IBase2`2<class Generated774.BaseClass0,class Generated774.BaseClass0>::Method7<object>()
		ldstr "G2_C293::Method7.MI.7470<System.Object>()"
		ldstr "class IBase2`2<class Generated774.BaseClass0,class Generated774.BaseClass0> on type class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0>
		callvirt instance string class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::ClassMethod1327<object>()
		ldstr "G2_C293::ClassMethod1327.MI.7475<System.Object>()"
		ldstr "class G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0> on type class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0>
		callvirt instance string class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::ClassMethod1326()
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0> on type class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0>
		callvirt instance string class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::Method7<object>()
		ldstr "G2_C293::Method7.7469<System.Object>()"
		ldstr "class G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0> on type class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated774.IBase2`2<class Generated774.BaseClass1,class Generated774.BaseClass1>::Method7<object>()
		ldstr "G2_C293::Method7.7469<System.Object>()"
		ldstr "class IBase2`2<class Generated774.BaseClass1,class Generated774.BaseClass1> on type class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated774.IBase2`2<class Generated774.BaseClass0,class Generated774.BaseClass1>::Method7<object>()
		ldstr "G2_C293::Method7.MI.7470<System.Object>()"
		ldstr "class IBase2`2<class Generated774.BaseClass0,class Generated774.BaseClass1> on type class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>
		callvirt instance string class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::ClassMethod1916<object>()
		ldstr "G2_C293::ClassMethod1916.7474<System.Object>()"
		ldstr "class G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0> on type class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>
		callvirt instance string class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::ClassMethod1915<object>()
		ldstr "G2_C293::ClassMethod1915.7473<System.Object>()"
		ldstr "class G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0> on type class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>
		callvirt instance string class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::ClassMethod1914()
		ldstr "G2_C293::ClassMethod1914.7472()"
		ldstr "class G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0> on type class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>
		callvirt instance string class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::ClassMethod1913()
		ldstr "G2_C293::ClassMethod1913.7471()"
		ldstr "class G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0> on type class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>
		callvirt instance string class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::Method7<object>()
		ldstr "G2_C293::Method7.7469<System.Object>()"
		ldstr "class G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0> on type class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>
		callvirt instance string class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::ClassMethod1327<object>()
		ldstr "G2_C293::ClassMethod1327.MI.7475<System.Object>()"
		ldstr "class G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0> on type class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>
		callvirt instance string class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::ClassMethod1326()
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0> on type class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated774.IBase2`2<class Generated774.BaseClass0,class Generated774.BaseClass0>::Method7<object>()
		ldstr "G2_C293::Method7.MI.7470<System.Object>()"
		ldstr "class IBase2`2<class Generated774.BaseClass0,class Generated774.BaseClass0> on type class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass1>
		callvirt instance string class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass1>::ClassMethod1327<object>()
		ldstr "G2_C293::ClassMethod1327.MI.7475<System.Object>()"
		ldstr "class G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass1> on type class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass1>
		callvirt instance string class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass1>::ClassMethod1326()
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass1> on type class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass1>
		callvirt instance string class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass1>::Method7<object>()
		ldstr "G2_C293::Method7.7469<System.Object>()"
		ldstr "class G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass1> on type class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated774.IBase2`2<class Generated774.BaseClass1,class Generated774.BaseClass1>::Method7<object>()
		ldstr "G2_C293::Method7.7469<System.Object>()"
		ldstr "class IBase2`2<class Generated774.BaseClass1,class Generated774.BaseClass1> on type class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated774.IBase2`2<class Generated774.BaseClass0,class Generated774.BaseClass1>::Method7<object>()
		ldstr "G2_C293::Method7.MI.7470<System.Object>()"
		ldstr "class IBase2`2<class Generated774.BaseClass0,class Generated774.BaseClass1> on type class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>
		callvirt instance string class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>::ClassMethod1916<object>()
		ldstr "G2_C293::ClassMethod1916.7474<System.Object>()"
		ldstr "class G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1> on type class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>
		callvirt instance string class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>::ClassMethod1915<object>()
		ldstr "G2_C293::ClassMethod1915.7473<System.Object>()"
		ldstr "class G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1> on type class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>
		callvirt instance string class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>::ClassMethod1914()
		ldstr "G2_C293::ClassMethod1914.7472()"
		ldstr "class G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1> on type class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>
		callvirt instance string class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>::ClassMethod1913()
		ldstr "G2_C293::ClassMethod1913.7471()"
		ldstr "class G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1> on type class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>
		callvirt instance string class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>::Method7<object>()
		ldstr "G2_C293::Method7.7469<System.Object>()"
		ldstr "class G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1> on type class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>
		callvirt instance string class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>::ClassMethod1327<object>()
		ldstr "G2_C293::ClassMethod1327.MI.7475<System.Object>()"
		ldstr "class G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1> on type class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>
		callvirt instance string class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>::ClassMethod1326()
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1> on type class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated774.IBase2`2<class Generated774.BaseClass0,class Generated774.BaseClass0>::Method7<object>()
		ldstr "G2_C293::Method7.MI.7470<System.Object>()"
		ldstr "class IBase2`2<class Generated774.BaseClass0,class Generated774.BaseClass0> on type class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass0>
		callvirt instance string class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass0>::ClassMethod1327<object>()
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass0> on type class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass0>
		callvirt instance string class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass0>::ClassMethod1326()
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass0> on type class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass0>
		callvirt instance string class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass0>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass0> on type class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated774.IBase2`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated774.BaseClass1,class Generated774.BaseClass0> on type class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated774.IBase2`2<class Generated774.BaseClass0,class Generated774.BaseClass0>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated774.BaseClass0,class Generated774.BaseClass0> on type class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated774.IBase2`2<class Generated774.BaseClass0,class Generated774.BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated774.BaseClass0,class Generated774.BaseClass1> on type class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated774.IBase2`2<class Generated774.BaseClass1,class Generated774.BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated774.BaseClass1,class Generated774.BaseClass1> on type class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass1>
		callvirt instance string class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass1>::ClassMethod1327<object>()
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass1> on type class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass1>
		callvirt instance string class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass1>::ClassMethod1326()
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass1> on type class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass1>
		callvirt instance string class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass1> on type class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated774.IBase2`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated774.BaseClass1,class Generated774.BaseClass0> on type class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated774.IBase2`2<class Generated774.BaseClass0,class Generated774.BaseClass0>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated774.BaseClass0,class Generated774.BaseClass0> on type class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated774.IBase2`2<class Generated774.BaseClass0,class Generated774.BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated774.BaseClass0,class Generated774.BaseClass1> on type class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated774.IBase2`2<class Generated774.BaseClass1,class Generated774.BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated774.BaseClass1,class Generated774.BaseClass1> on type class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0>
		callvirt instance string class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::ClassMethod1327<object>()
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0> on type class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0>
		callvirt instance string class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::ClassMethod1326()
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0> on type class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0>
		callvirt instance string class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0> on type class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated774.IBase2`2<class Generated774.BaseClass1,class Generated774.BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated774.BaseClass1,class Generated774.BaseClass1> on type class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated774.IBase2`2<class Generated774.BaseClass0,class Generated774.BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated774.BaseClass0,class Generated774.BaseClass1> on type class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass1>
		callvirt instance string class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass1>::ClassMethod1327<object>()
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass1> on type class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass1>
		callvirt instance string class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass1>::ClassMethod1326()
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass1> on type class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass1>
		callvirt instance string class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass1> on type class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated774.IBase2`2<class Generated774.BaseClass1,class Generated774.BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated774.BaseClass1,class Generated774.BaseClass1> on type class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated774.IBase2`2<class Generated774.BaseClass0,class Generated774.BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated774.BaseClass0,class Generated774.BaseClass1> on type class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass1>"
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
		newobj instance void class Generated774.G3_C1246`1<class Generated774.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G2_C293::ClassMethod1327.MI.7475<System.Object>()#G2_C293::Method7.7469<System.Object>()#"
		call void Generated774.Generated774::M.G1_C6.T.T<class Generated774.BaseClass1,class Generated774.BaseClass0,class Generated774.G3_C1246`1<class Generated774.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G2_C293::ClassMethod1327.MI.7475<System.Object>()#G2_C293::Method7.7469<System.Object>()#"
		call void Generated774.Generated774::M.G1_C6.B.T<class Generated774.BaseClass0,class Generated774.G3_C1246`1<class Generated774.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G2_C293::ClassMethod1327.MI.7475<System.Object>()#G2_C293::Method7.7469<System.Object>()#"
		call void Generated774.Generated774::M.G1_C6.B.A<class Generated774.G3_C1246`1<class Generated774.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C293::Method7.7469<System.Object>()#"
		call void Generated774.Generated774::M.IBase2.T.T<class Generated774.BaseClass1,class Generated774.BaseClass1,class Generated774.G3_C1246`1<class Generated774.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C293::Method7.7469<System.Object>()#"
		call void Generated774.Generated774::M.IBase2.B.T<class Generated774.BaseClass1,class Generated774.G3_C1246`1<class Generated774.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C293::Method7.7469<System.Object>()#"
		call void Generated774.Generated774::M.IBase2.B.B<class Generated774.G3_C1246`1<class Generated774.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C293::Method7.MI.7470<System.Object>()#"
		call void Generated774.Generated774::M.IBase2.T.T<class Generated774.BaseClass0,class Generated774.BaseClass1,class Generated774.G3_C1246`1<class Generated774.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C293::Method7.MI.7470<System.Object>()#"
		call void Generated774.Generated774::M.IBase2.A.T<class Generated774.BaseClass1,class Generated774.G3_C1246`1<class Generated774.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C293::Method7.MI.7470<System.Object>()#"
		call void Generated774.Generated774::M.IBase2.A.B<class Generated774.G3_C1246`1<class Generated774.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G2_C293::ClassMethod1327.MI.7475<System.Object>()#G3_C1246::ClassMethod1913.MI.15096()#G2_C293::ClassMethod1914.7472()#G3_C1246::ClassMethod1915.MI.15097<System.Object>()#G2_C293::ClassMethod1916.7474<System.Object>()#G2_C293::Method7.7469<System.Object>()#"
		call void Generated774.Generated774::M.G2_C293.T.T<class Generated774.BaseClass1,class Generated774.BaseClass0,class Generated774.G3_C1246`1<class Generated774.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G2_C293::ClassMethod1327.MI.7475<System.Object>()#G3_C1246::ClassMethod1913.MI.15096()#G2_C293::ClassMethod1914.7472()#G3_C1246::ClassMethod1915.MI.15097<System.Object>()#G2_C293::ClassMethod1916.7474<System.Object>()#G2_C293::Method7.7469<System.Object>()#"
		call void Generated774.Generated774::M.G2_C293.B.T<class Generated774.BaseClass0,class Generated774.G3_C1246`1<class Generated774.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G2_C293::ClassMethod1327.MI.7475<System.Object>()#G3_C1246::ClassMethod1913.MI.15096()#G2_C293::ClassMethod1914.7472()#G3_C1246::ClassMethod1915.MI.15097<System.Object>()#G2_C293::ClassMethod1916.7474<System.Object>()#G2_C293::Method7.7469<System.Object>()#"
		call void Generated774.Generated774::M.G2_C293.B.A<class Generated774.G3_C1246`1<class Generated774.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C293::Method7.MI.7470<System.Object>()#"
		call void Generated774.Generated774::M.IBase2.T.T<class Generated774.BaseClass0,class Generated774.BaseClass0,class Generated774.G3_C1246`1<class Generated774.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C293::Method7.MI.7470<System.Object>()#"
		call void Generated774.Generated774::M.IBase2.A.T<class Generated774.BaseClass0,class Generated774.G3_C1246`1<class Generated774.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C293::Method7.MI.7470<System.Object>()#"
		call void Generated774.Generated774::M.IBase2.A.A<class Generated774.G3_C1246`1<class Generated774.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G2_C293::ClassMethod1327.MI.7475<System.Object>()#G3_C1246::ClassMethod1913.MI.15096()#G2_C293::ClassMethod1914.7472()#G3_C1246::ClassMethod1915.MI.15097<System.Object>()#G2_C293::ClassMethod1916.7474<System.Object>()#G3_C1246::ClassMethod3870.15093()#G3_C1246::ClassMethod3871.15094<System.Object>()#G3_C1246::ClassMethod3872.15095<System.Object>()#G3_C1246::Method4.15087()#G3_C1246::Method5.15089()#G3_C1246::Method6.15091<System.Object>()#G2_C293::Method7.7469<System.Object>()#"
		call void Generated774.Generated774::M.G3_C1246.T<class Generated774.BaseClass0,class Generated774.G3_C1246`1<class Generated774.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G2_C293::ClassMethod1327.MI.7475<System.Object>()#G3_C1246::ClassMethod1913.MI.15096()#G2_C293::ClassMethod1914.7472()#G3_C1246::ClassMethod1915.MI.15097<System.Object>()#G2_C293::ClassMethod1916.7474<System.Object>()#G3_C1246::ClassMethod3870.15093()#G3_C1246::ClassMethod3871.15094<System.Object>()#G3_C1246::ClassMethod3872.15095<System.Object>()#G3_C1246::Method4.15087()#G3_C1246::Method5.15089()#G3_C1246::Method6.15091<System.Object>()#G2_C293::Method7.7469<System.Object>()#"
		call void Generated774.Generated774::M.G3_C1246.A<class Generated774.G3_C1246`1<class Generated774.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1246::Method4.MI.15088()#G3_C1246::Method5.MI.15090()#G3_C1246::Method6.MI.15092<System.Object>()#"
		call void Generated774.Generated774::M.IBase1.T<class Generated774.BaseClass0,class Generated774.G3_C1246`1<class Generated774.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1246::Method4.MI.15088()#G3_C1246::Method5.MI.15090()#G3_C1246::Method6.MI.15092<System.Object>()#"
		call void Generated774.Generated774::M.IBase1.A<class Generated774.G3_C1246`1<class Generated774.BaseClass0>>(!!0,string)
		newobj instance void class Generated774.G3_C1246`1<class Generated774.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G2_C293::ClassMethod1327.MI.7475<System.Object>()#G2_C293::Method7.7469<System.Object>()#"
		call void Generated774.Generated774::M.G1_C6.T.T<class Generated774.BaseClass1,class Generated774.BaseClass0,class Generated774.G3_C1246`1<class Generated774.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G2_C293::ClassMethod1327.MI.7475<System.Object>()#G2_C293::Method7.7469<System.Object>()#"
		call void Generated774.Generated774::M.G1_C6.B.T<class Generated774.BaseClass0,class Generated774.G3_C1246`1<class Generated774.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G2_C293::ClassMethod1327.MI.7475<System.Object>()#G2_C293::Method7.7469<System.Object>()#"
		call void Generated774.Generated774::M.G1_C6.B.A<class Generated774.G3_C1246`1<class Generated774.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C293::Method7.7469<System.Object>()#"
		call void Generated774.Generated774::M.IBase2.T.T<class Generated774.BaseClass1,class Generated774.BaseClass1,class Generated774.G3_C1246`1<class Generated774.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C293::Method7.7469<System.Object>()#"
		call void Generated774.Generated774::M.IBase2.B.T<class Generated774.BaseClass1,class Generated774.G3_C1246`1<class Generated774.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C293::Method7.7469<System.Object>()#"
		call void Generated774.Generated774::M.IBase2.B.B<class Generated774.G3_C1246`1<class Generated774.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C293::Method7.MI.7470<System.Object>()#"
		call void Generated774.Generated774::M.IBase2.T.T<class Generated774.BaseClass0,class Generated774.BaseClass1,class Generated774.G3_C1246`1<class Generated774.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C293::Method7.MI.7470<System.Object>()#"
		call void Generated774.Generated774::M.IBase2.A.T<class Generated774.BaseClass1,class Generated774.G3_C1246`1<class Generated774.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C293::Method7.MI.7470<System.Object>()#"
		call void Generated774.Generated774::M.IBase2.A.B<class Generated774.G3_C1246`1<class Generated774.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G2_C293::ClassMethod1327.MI.7475<System.Object>()#G3_C1246::ClassMethod1913.MI.15096()#G2_C293::ClassMethod1914.7472()#G3_C1246::ClassMethod1915.MI.15097<System.Object>()#G2_C293::ClassMethod1916.7474<System.Object>()#G2_C293::Method7.7469<System.Object>()#"
		call void Generated774.Generated774::M.G2_C293.T.T<class Generated774.BaseClass1,class Generated774.BaseClass0,class Generated774.G3_C1246`1<class Generated774.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G2_C293::ClassMethod1327.MI.7475<System.Object>()#G3_C1246::ClassMethod1913.MI.15096()#G2_C293::ClassMethod1914.7472()#G3_C1246::ClassMethod1915.MI.15097<System.Object>()#G2_C293::ClassMethod1916.7474<System.Object>()#G2_C293::Method7.7469<System.Object>()#"
		call void Generated774.Generated774::M.G2_C293.B.T<class Generated774.BaseClass0,class Generated774.G3_C1246`1<class Generated774.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G2_C293::ClassMethod1327.MI.7475<System.Object>()#G3_C1246::ClassMethod1913.MI.15096()#G2_C293::ClassMethod1914.7472()#G3_C1246::ClassMethod1915.MI.15097<System.Object>()#G2_C293::ClassMethod1916.7474<System.Object>()#G2_C293::Method7.7469<System.Object>()#"
		call void Generated774.Generated774::M.G2_C293.B.A<class Generated774.G3_C1246`1<class Generated774.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C293::Method7.MI.7470<System.Object>()#"
		call void Generated774.Generated774::M.IBase2.T.T<class Generated774.BaseClass0,class Generated774.BaseClass0,class Generated774.G3_C1246`1<class Generated774.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C293::Method7.MI.7470<System.Object>()#"
		call void Generated774.Generated774::M.IBase2.A.T<class Generated774.BaseClass0,class Generated774.G3_C1246`1<class Generated774.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C293::Method7.MI.7470<System.Object>()#"
		call void Generated774.Generated774::M.IBase2.A.A<class Generated774.G3_C1246`1<class Generated774.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G2_C293::ClassMethod1327.MI.7475<System.Object>()#G3_C1246::ClassMethod1913.MI.15096()#G2_C293::ClassMethod1914.7472()#G3_C1246::ClassMethod1915.MI.15097<System.Object>()#G2_C293::ClassMethod1916.7474<System.Object>()#G3_C1246::ClassMethod3870.15093()#G3_C1246::ClassMethod3871.15094<System.Object>()#G3_C1246::ClassMethod3872.15095<System.Object>()#G3_C1246::Method4.15087()#G3_C1246::Method5.15089()#G3_C1246::Method6.15091<System.Object>()#G2_C293::Method7.7469<System.Object>()#"
		call void Generated774.Generated774::M.G3_C1246.T<class Generated774.BaseClass1,class Generated774.G3_C1246`1<class Generated774.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G2_C293::ClassMethod1327.MI.7475<System.Object>()#G3_C1246::ClassMethod1913.MI.15096()#G2_C293::ClassMethod1914.7472()#G3_C1246::ClassMethod1915.MI.15097<System.Object>()#G2_C293::ClassMethod1916.7474<System.Object>()#G3_C1246::ClassMethod3870.15093()#G3_C1246::ClassMethod3871.15094<System.Object>()#G3_C1246::ClassMethod3872.15095<System.Object>()#G3_C1246::Method4.15087()#G3_C1246::Method5.15089()#G3_C1246::Method6.15091<System.Object>()#G2_C293::Method7.7469<System.Object>()#"
		call void Generated774.Generated774::M.G3_C1246.B<class Generated774.G3_C1246`1<class Generated774.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1246::Method4.MI.15088()#G3_C1246::Method5.MI.15090()#G3_C1246::Method6.MI.15092<System.Object>()#"
		call void Generated774.Generated774::M.IBase1.T<class Generated774.BaseClass1,class Generated774.G3_C1246`1<class Generated774.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1246::Method4.MI.15088()#G3_C1246::Method5.MI.15090()#G3_C1246::Method6.MI.15092<System.Object>()#"
		call void Generated774.Generated774::M.IBase1.B<class Generated774.G3_C1246`1<class Generated774.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1246::Method4.MI.15088()#G3_C1246::Method5.MI.15090()#G3_C1246::Method6.MI.15092<System.Object>()#"
		call void Generated774.Generated774::M.IBase1.T<class Generated774.BaseClass0,class Generated774.G3_C1246`1<class Generated774.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1246::Method4.MI.15088()#G3_C1246::Method5.MI.15090()#G3_C1246::Method6.MI.15092<System.Object>()#"
		call void Generated774.Generated774::M.IBase1.A<class Generated774.G3_C1246`1<class Generated774.BaseClass1>>(!!0,string)
		newobj instance void class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G2_C293::ClassMethod1327.MI.7475<System.Object>()#G2_C293::Method7.7469<System.Object>()#"
		call void Generated774.Generated774::M.G1_C6.T.T<class Generated774.BaseClass1,class Generated774.BaseClass0,class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G2_C293::ClassMethod1327.MI.7475<System.Object>()#G2_C293::Method7.7469<System.Object>()#"
		call void Generated774.Generated774::M.G1_C6.B.T<class Generated774.BaseClass0,class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G2_C293::ClassMethod1327.MI.7475<System.Object>()#G2_C293::Method7.7469<System.Object>()#"
		call void Generated774.Generated774::M.G1_C6.B.A<class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C293::Method7.7469<System.Object>()#"
		call void Generated774.Generated774::M.IBase2.T.T<class Generated774.BaseClass1,class Generated774.BaseClass1,class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C293::Method7.7469<System.Object>()#"
		call void Generated774.Generated774::M.IBase2.B.T<class Generated774.BaseClass1,class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C293::Method7.7469<System.Object>()#"
		call void Generated774.Generated774::M.IBase2.B.B<class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C293::Method7.MI.7470<System.Object>()#"
		call void Generated774.Generated774::M.IBase2.T.T<class Generated774.BaseClass0,class Generated774.BaseClass1,class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C293::Method7.MI.7470<System.Object>()#"
		call void Generated774.Generated774::M.IBase2.A.T<class Generated774.BaseClass1,class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C293::Method7.MI.7470<System.Object>()#"
		call void Generated774.Generated774::M.IBase2.A.B<class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G2_C293::ClassMethod1327.MI.7475<System.Object>()#G2_C293::ClassMethod1913.7471()#G2_C293::ClassMethod1914.7472()#G2_C293::ClassMethod1915.7473<System.Object>()#G2_C293::ClassMethod1916.7474<System.Object>()#G2_C293::Method7.7469<System.Object>()#"
		call void Generated774.Generated774::M.G2_C293.T.T<class Generated774.BaseClass0,class Generated774.BaseClass0,class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G2_C293::ClassMethod1327.MI.7475<System.Object>()#G2_C293::ClassMethod1913.7471()#G2_C293::ClassMethod1914.7472()#G2_C293::ClassMethod1915.7473<System.Object>()#G2_C293::ClassMethod1916.7474<System.Object>()#G2_C293::Method7.7469<System.Object>()#"
		call void Generated774.Generated774::M.G2_C293.A.T<class Generated774.BaseClass0,class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G2_C293::ClassMethod1327.MI.7475<System.Object>()#G2_C293::ClassMethod1913.7471()#G2_C293::ClassMethod1914.7472()#G2_C293::ClassMethod1915.7473<System.Object>()#G2_C293::ClassMethod1916.7474<System.Object>()#G2_C293::Method7.7469<System.Object>()#"
		call void Generated774.Generated774::M.G2_C293.A.A<class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C293::Method7.MI.7470<System.Object>()#"
		call void Generated774.Generated774::M.IBase2.T.T<class Generated774.BaseClass0,class Generated774.BaseClass0,class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C293::Method7.MI.7470<System.Object>()#"
		call void Generated774.Generated774::M.IBase2.A.T<class Generated774.BaseClass0,class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C293::Method7.MI.7470<System.Object>()#"
		call void Generated774.Generated774::M.IBase2.A.A<class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>>(!!0,string)
		newobj instance void class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G2_C293::ClassMethod1327.MI.7475<System.Object>()#G2_C293::Method7.7469<System.Object>()#"
		call void Generated774.Generated774::M.G1_C6.T.T<class Generated774.BaseClass1,class Generated774.BaseClass1,class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G2_C293::ClassMethod1327.MI.7475<System.Object>()#G2_C293::Method7.7469<System.Object>()#"
		call void Generated774.Generated774::M.G1_C6.B.T<class Generated774.BaseClass1,class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G2_C293::ClassMethod1327.MI.7475<System.Object>()#G2_C293::Method7.7469<System.Object>()#"
		call void Generated774.Generated774::M.G1_C6.B.B<class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C293::Method7.7469<System.Object>()#"
		call void Generated774.Generated774::M.IBase2.T.T<class Generated774.BaseClass1,class Generated774.BaseClass1,class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C293::Method7.7469<System.Object>()#"
		call void Generated774.Generated774::M.IBase2.B.T<class Generated774.BaseClass1,class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C293::Method7.7469<System.Object>()#"
		call void Generated774.Generated774::M.IBase2.B.B<class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C293::Method7.MI.7470<System.Object>()#"
		call void Generated774.Generated774::M.IBase2.T.T<class Generated774.BaseClass0,class Generated774.BaseClass1,class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C293::Method7.MI.7470<System.Object>()#"
		call void Generated774.Generated774::M.IBase2.A.T<class Generated774.BaseClass1,class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C293::Method7.MI.7470<System.Object>()#"
		call void Generated774.Generated774::M.IBase2.A.B<class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G2_C293::ClassMethod1327.MI.7475<System.Object>()#G2_C293::ClassMethod1913.7471()#G2_C293::ClassMethod1914.7472()#G2_C293::ClassMethod1915.7473<System.Object>()#G2_C293::ClassMethod1916.7474<System.Object>()#G2_C293::Method7.7469<System.Object>()#"
		call void Generated774.Generated774::M.G2_C293.T.T<class Generated774.BaseClass0,class Generated774.BaseClass1,class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G2_C293::ClassMethod1327.MI.7475<System.Object>()#G2_C293::ClassMethod1913.7471()#G2_C293::ClassMethod1914.7472()#G2_C293::ClassMethod1915.7473<System.Object>()#G2_C293::ClassMethod1916.7474<System.Object>()#G2_C293::Method7.7469<System.Object>()#"
		call void Generated774.Generated774::M.G2_C293.A.T<class Generated774.BaseClass1,class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G2_C293::ClassMethod1327.MI.7475<System.Object>()#G2_C293::ClassMethod1913.7471()#G2_C293::ClassMethod1914.7472()#G2_C293::ClassMethod1915.7473<System.Object>()#G2_C293::ClassMethod1916.7474<System.Object>()#G2_C293::Method7.7469<System.Object>()#"
		call void Generated774.Generated774::M.G2_C293.A.B<class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C293::Method7.MI.7470<System.Object>()#"
		call void Generated774.Generated774::M.IBase2.T.T<class Generated774.BaseClass0,class Generated774.BaseClass0,class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C293::Method7.MI.7470<System.Object>()#"
		call void Generated774.Generated774::M.IBase2.A.T<class Generated774.BaseClass0,class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C293::Method7.MI.7470<System.Object>()#"
		call void Generated774.Generated774::M.IBase2.A.A<class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>>(!!0,string)
		newobj instance void class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G2_C293::ClassMethod1327.MI.7475<System.Object>()#G2_C293::Method7.7469<System.Object>()#"
		call void Generated774.Generated774::M.G1_C6.T.T<class Generated774.BaseClass1,class Generated774.BaseClass0,class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G2_C293::ClassMethod1327.MI.7475<System.Object>()#G2_C293::Method7.7469<System.Object>()#"
		call void Generated774.Generated774::M.G1_C6.B.T<class Generated774.BaseClass0,class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G2_C293::ClassMethod1327.MI.7475<System.Object>()#G2_C293::Method7.7469<System.Object>()#"
		call void Generated774.Generated774::M.G1_C6.B.A<class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C293::Method7.7469<System.Object>()#"
		call void Generated774.Generated774::M.IBase2.T.T<class Generated774.BaseClass1,class Generated774.BaseClass1,class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C293::Method7.7469<System.Object>()#"
		call void Generated774.Generated774::M.IBase2.B.T<class Generated774.BaseClass1,class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C293::Method7.7469<System.Object>()#"
		call void Generated774.Generated774::M.IBase2.B.B<class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C293::Method7.MI.7470<System.Object>()#"
		call void Generated774.Generated774::M.IBase2.T.T<class Generated774.BaseClass0,class Generated774.BaseClass1,class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C293::Method7.MI.7470<System.Object>()#"
		call void Generated774.Generated774::M.IBase2.A.T<class Generated774.BaseClass1,class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C293::Method7.MI.7470<System.Object>()#"
		call void Generated774.Generated774::M.IBase2.A.B<class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G2_C293::ClassMethod1327.MI.7475<System.Object>()#G2_C293::ClassMethod1913.7471()#G2_C293::ClassMethod1914.7472()#G2_C293::ClassMethod1915.7473<System.Object>()#G2_C293::ClassMethod1916.7474<System.Object>()#G2_C293::Method7.7469<System.Object>()#"
		call void Generated774.Generated774::M.G2_C293.T.T<class Generated774.BaseClass1,class Generated774.BaseClass0,class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G2_C293::ClassMethod1327.MI.7475<System.Object>()#G2_C293::ClassMethod1913.7471()#G2_C293::ClassMethod1914.7472()#G2_C293::ClassMethod1915.7473<System.Object>()#G2_C293::ClassMethod1916.7474<System.Object>()#G2_C293::Method7.7469<System.Object>()#"
		call void Generated774.Generated774::M.G2_C293.B.T<class Generated774.BaseClass0,class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G2_C293::ClassMethod1327.MI.7475<System.Object>()#G2_C293::ClassMethod1913.7471()#G2_C293::ClassMethod1914.7472()#G2_C293::ClassMethod1915.7473<System.Object>()#G2_C293::ClassMethod1916.7474<System.Object>()#G2_C293::Method7.7469<System.Object>()#"
		call void Generated774.Generated774::M.G2_C293.B.A<class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C293::Method7.MI.7470<System.Object>()#"
		call void Generated774.Generated774::M.IBase2.T.T<class Generated774.BaseClass0,class Generated774.BaseClass0,class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C293::Method7.MI.7470<System.Object>()#"
		call void Generated774.Generated774::M.IBase2.A.T<class Generated774.BaseClass0,class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C293::Method7.MI.7470<System.Object>()#"
		call void Generated774.Generated774::M.IBase2.A.A<class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>>(!!0,string)
		newobj instance void class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G2_C293::ClassMethod1327.MI.7475<System.Object>()#G2_C293::Method7.7469<System.Object>()#"
		call void Generated774.Generated774::M.G1_C6.T.T<class Generated774.BaseClass1,class Generated774.BaseClass1,class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G2_C293::ClassMethod1327.MI.7475<System.Object>()#G2_C293::Method7.7469<System.Object>()#"
		call void Generated774.Generated774::M.G1_C6.B.T<class Generated774.BaseClass1,class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G2_C293::ClassMethod1327.MI.7475<System.Object>()#G2_C293::Method7.7469<System.Object>()#"
		call void Generated774.Generated774::M.G1_C6.B.B<class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C293::Method7.7469<System.Object>()#"
		call void Generated774.Generated774::M.IBase2.T.T<class Generated774.BaseClass1,class Generated774.BaseClass1,class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C293::Method7.7469<System.Object>()#"
		call void Generated774.Generated774::M.IBase2.B.T<class Generated774.BaseClass1,class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C293::Method7.7469<System.Object>()#"
		call void Generated774.Generated774::M.IBase2.B.B<class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C293::Method7.MI.7470<System.Object>()#"
		call void Generated774.Generated774::M.IBase2.T.T<class Generated774.BaseClass0,class Generated774.BaseClass1,class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C293::Method7.MI.7470<System.Object>()#"
		call void Generated774.Generated774::M.IBase2.A.T<class Generated774.BaseClass1,class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C293::Method7.MI.7470<System.Object>()#"
		call void Generated774.Generated774::M.IBase2.A.B<class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G2_C293::ClassMethod1327.MI.7475<System.Object>()#G2_C293::ClassMethod1913.7471()#G2_C293::ClassMethod1914.7472()#G2_C293::ClassMethod1915.7473<System.Object>()#G2_C293::ClassMethod1916.7474<System.Object>()#G2_C293::Method7.7469<System.Object>()#"
		call void Generated774.Generated774::M.G2_C293.T.T<class Generated774.BaseClass1,class Generated774.BaseClass1,class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G2_C293::ClassMethod1327.MI.7475<System.Object>()#G2_C293::ClassMethod1913.7471()#G2_C293::ClassMethod1914.7472()#G2_C293::ClassMethod1915.7473<System.Object>()#G2_C293::ClassMethod1916.7474<System.Object>()#G2_C293::Method7.7469<System.Object>()#"
		call void Generated774.Generated774::M.G2_C293.B.T<class Generated774.BaseClass1,class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G2_C293::ClassMethod1327.MI.7475<System.Object>()#G2_C293::ClassMethod1913.7471()#G2_C293::ClassMethod1914.7472()#G2_C293::ClassMethod1915.7473<System.Object>()#G2_C293::ClassMethod1916.7474<System.Object>()#G2_C293::Method7.7469<System.Object>()#"
		call void Generated774.Generated774::M.G2_C293.B.B<class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C293::Method7.MI.7470<System.Object>()#"
		call void Generated774.Generated774::M.IBase2.T.T<class Generated774.BaseClass0,class Generated774.BaseClass0,class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C293::Method7.MI.7470<System.Object>()#"
		call void Generated774.Generated774::M.IBase2.A.T<class Generated774.BaseClass0,class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C293::Method7.MI.7470<System.Object>()#"
		call void Generated774.Generated774::M.IBase2.A.A<class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>>(!!0,string)
		newobj instance void class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated774.Generated774::M.G1_C6.T.T<class Generated774.BaseClass0,class Generated774.BaseClass0,class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated774.Generated774::M.G1_C6.A.T<class Generated774.BaseClass0,class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated774.Generated774::M.G1_C6.A.A<class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated774.Generated774::M.IBase2.T.T<class Generated774.BaseClass1,class Generated774.BaseClass0,class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated774.Generated774::M.IBase2.B.T<class Generated774.BaseClass0,class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated774.Generated774::M.IBase2.B.A<class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated774.Generated774::M.IBase2.T.T<class Generated774.BaseClass0,class Generated774.BaseClass0,class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated774.Generated774::M.IBase2.A.T<class Generated774.BaseClass0,class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated774.Generated774::M.IBase2.A.A<class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated774.Generated774::M.IBase2.T.T<class Generated774.BaseClass0,class Generated774.BaseClass1,class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated774.Generated774::M.IBase2.A.T<class Generated774.BaseClass1,class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated774.Generated774::M.IBase2.A.B<class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated774.Generated774::M.IBase2.T.T<class Generated774.BaseClass1,class Generated774.BaseClass1,class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated774.Generated774::M.IBase2.B.T<class Generated774.BaseClass1,class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated774.Generated774::M.IBase2.B.B<class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass0>>(!!0,string)
		newobj instance void class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated774.Generated774::M.G1_C6.T.T<class Generated774.BaseClass0,class Generated774.BaseClass1,class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated774.Generated774::M.G1_C6.A.T<class Generated774.BaseClass1,class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated774.Generated774::M.G1_C6.A.B<class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated774.Generated774::M.IBase2.T.T<class Generated774.BaseClass1,class Generated774.BaseClass0,class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated774.Generated774::M.IBase2.B.T<class Generated774.BaseClass0,class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated774.Generated774::M.IBase2.B.A<class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated774.Generated774::M.IBase2.T.T<class Generated774.BaseClass0,class Generated774.BaseClass0,class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated774.Generated774::M.IBase2.A.T<class Generated774.BaseClass0,class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated774.Generated774::M.IBase2.A.A<class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated774.Generated774::M.IBase2.T.T<class Generated774.BaseClass0,class Generated774.BaseClass1,class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated774.Generated774::M.IBase2.A.T<class Generated774.BaseClass1,class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated774.Generated774::M.IBase2.A.B<class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated774.Generated774::M.IBase2.T.T<class Generated774.BaseClass1,class Generated774.BaseClass1,class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated774.Generated774::M.IBase2.B.T<class Generated774.BaseClass1,class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated774.Generated774::M.IBase2.B.B<class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass1>>(!!0,string)
		newobj instance void class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated774.Generated774::M.G1_C6.T.T<class Generated774.BaseClass1,class Generated774.BaseClass0,class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated774.Generated774::M.G1_C6.B.T<class Generated774.BaseClass0,class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated774.Generated774::M.G1_C6.B.A<class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated774.Generated774::M.IBase2.T.T<class Generated774.BaseClass1,class Generated774.BaseClass1,class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated774.Generated774::M.IBase2.B.T<class Generated774.BaseClass1,class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated774.Generated774::M.IBase2.B.B<class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated774.Generated774::M.IBase2.T.T<class Generated774.BaseClass0,class Generated774.BaseClass1,class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated774.Generated774::M.IBase2.A.T<class Generated774.BaseClass1,class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated774.Generated774::M.IBase2.A.B<class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0>>(!!0,string)
		newobj instance void class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated774.Generated774::M.G1_C6.T.T<class Generated774.BaseClass1,class Generated774.BaseClass1,class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated774.Generated774::M.G1_C6.B.T<class Generated774.BaseClass1,class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated774.Generated774::M.G1_C6.B.B<class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated774.Generated774::M.IBase2.T.T<class Generated774.BaseClass1,class Generated774.BaseClass1,class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated774.Generated774::M.IBase2.B.T<class Generated774.BaseClass1,class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated774.Generated774::M.IBase2.B.B<class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated774.Generated774::M.IBase2.T.T<class Generated774.BaseClass0,class Generated774.BaseClass1,class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated774.Generated774::M.IBase2.A.T<class Generated774.BaseClass1,class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated774.Generated774::M.IBase2.A.B<class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass1>>(!!0,string)
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
		newobj instance void class Generated774.G3_C1246`1<class Generated774.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::ClassMethod1327<object>()
		calli default string(class Generated774.G3_C1246`1<class Generated774.BaseClass0>)
		ldstr "G2_C293::ClassMethod1327.MI.7475<System.Object>()"
		ldstr "class G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0> on type class Generated774.G3_C1246`1<class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::ClassMethod1326()
		calli default string(class Generated774.G3_C1246`1<class Generated774.BaseClass0>)
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0> on type class Generated774.G3_C1246`1<class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::Method7<object>()
		calli default string(class Generated774.G3_C1246`1<class Generated774.BaseClass0>)
		ldstr "G2_C293::Method7.7469<System.Object>()"
		ldstr "class G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0> on type class Generated774.G3_C1246`1<class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated774.IBase2`2<class Generated774.BaseClass1,class Generated774.BaseClass1>::Method7<object>()
		calli default string(class Generated774.G3_C1246`1<class Generated774.BaseClass0>)
		ldstr "G2_C293::Method7.7469<System.Object>()"
		ldstr "class IBase2`2<class Generated774.BaseClass1,class Generated774.BaseClass1> on type class Generated774.G3_C1246`1<class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated774.IBase2`2<class Generated774.BaseClass0,class Generated774.BaseClass1>::Method7<object>()
		calli default string(class Generated774.G3_C1246`1<class Generated774.BaseClass0>)
		ldstr "G2_C293::Method7.MI.7470<System.Object>()"
		ldstr "class IBase2`2<class Generated774.BaseClass0,class Generated774.BaseClass1> on type class Generated774.G3_C1246`1<class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::ClassMethod1916<object>()
		calli default string(class Generated774.G3_C1246`1<class Generated774.BaseClass0>)
		ldstr "G2_C293::ClassMethod1916.7474<System.Object>()"
		ldstr "class G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0> on type class Generated774.G3_C1246`1<class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::ClassMethod1915<object>()
		calli default string(class Generated774.G3_C1246`1<class Generated774.BaseClass0>)
		ldstr "G3_C1246::ClassMethod1915.MI.15097<System.Object>()"
		ldstr "class G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0> on type class Generated774.G3_C1246`1<class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::ClassMethod1914()
		calli default string(class Generated774.G3_C1246`1<class Generated774.BaseClass0>)
		ldstr "G2_C293::ClassMethod1914.7472()"
		ldstr "class G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0> on type class Generated774.G3_C1246`1<class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::ClassMethod1913()
		calli default string(class Generated774.G3_C1246`1<class Generated774.BaseClass0>)
		ldstr "G3_C1246::ClassMethod1913.MI.15096()"
		ldstr "class G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0> on type class Generated774.G3_C1246`1<class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::Method7<object>()
		calli default string(class Generated774.G3_C1246`1<class Generated774.BaseClass0>)
		ldstr "G2_C293::Method7.7469<System.Object>()"
		ldstr "class G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0> on type class Generated774.G3_C1246`1<class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::ClassMethod1327<object>()
		calli default string(class Generated774.G3_C1246`1<class Generated774.BaseClass0>)
		ldstr "G2_C293::ClassMethod1327.MI.7475<System.Object>()"
		ldstr "class G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0> on type class Generated774.G3_C1246`1<class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::ClassMethod1326()
		calli default string(class Generated774.G3_C1246`1<class Generated774.BaseClass0>)
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0> on type class Generated774.G3_C1246`1<class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated774.IBase2`2<class Generated774.BaseClass0,class Generated774.BaseClass0>::Method7<object>()
		calli default string(class Generated774.G3_C1246`1<class Generated774.BaseClass0>)
		ldstr "G2_C293::Method7.MI.7470<System.Object>()"
		ldstr "class IBase2`2<class Generated774.BaseClass0,class Generated774.BaseClass0> on type class Generated774.G3_C1246`1<class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated774.G3_C1246`1<class Generated774.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated774.G3_C1246`1<class Generated774.BaseClass0>::ClassMethod3872<object>()
		calli default string(class Generated774.G3_C1246`1<class Generated774.BaseClass0>)
		ldstr "G3_C1246::ClassMethod3872.15095<System.Object>()"
		ldstr "class G3_C1246`1<class Generated774.BaseClass0> on type class Generated774.G3_C1246`1<class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated774.G3_C1246`1<class Generated774.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated774.G3_C1246`1<class Generated774.BaseClass0>::ClassMethod3871<object>()
		calli default string(class Generated774.G3_C1246`1<class Generated774.BaseClass0>)
		ldstr "G3_C1246::ClassMethod3871.15094<System.Object>()"
		ldstr "class G3_C1246`1<class Generated774.BaseClass0> on type class Generated774.G3_C1246`1<class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated774.G3_C1246`1<class Generated774.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated774.G3_C1246`1<class Generated774.BaseClass0>::ClassMethod3870()
		calli default string(class Generated774.G3_C1246`1<class Generated774.BaseClass0>)
		ldstr "G3_C1246::ClassMethod3870.15093()"
		ldstr "class G3_C1246`1<class Generated774.BaseClass0> on type class Generated774.G3_C1246`1<class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated774.G3_C1246`1<class Generated774.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated774.G3_C1246`1<class Generated774.BaseClass0>::Method6<object>()
		calli default string(class Generated774.G3_C1246`1<class Generated774.BaseClass0>)
		ldstr "G3_C1246::Method6.15091<System.Object>()"
		ldstr "class G3_C1246`1<class Generated774.BaseClass0> on type class Generated774.G3_C1246`1<class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated774.G3_C1246`1<class Generated774.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated774.G3_C1246`1<class Generated774.BaseClass0>::Method5()
		calli default string(class Generated774.G3_C1246`1<class Generated774.BaseClass0>)
		ldstr "G3_C1246::Method5.15089()"
		ldstr "class G3_C1246`1<class Generated774.BaseClass0> on type class Generated774.G3_C1246`1<class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated774.G3_C1246`1<class Generated774.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated774.G3_C1246`1<class Generated774.BaseClass0>::Method4()
		calli default string(class Generated774.G3_C1246`1<class Generated774.BaseClass0>)
		ldstr "G3_C1246::Method4.15087()"
		ldstr "class G3_C1246`1<class Generated774.BaseClass0> on type class Generated774.G3_C1246`1<class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated774.G3_C1246`1<class Generated774.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated774.G3_C1246`1<class Generated774.BaseClass0>::ClassMethod1916<object>()
		calli default string(class Generated774.G3_C1246`1<class Generated774.BaseClass0>)
		ldstr "G2_C293::ClassMethod1916.7474<System.Object>()"
		ldstr "class G3_C1246`1<class Generated774.BaseClass0> on type class Generated774.G3_C1246`1<class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated774.G3_C1246`1<class Generated774.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated774.G3_C1246`1<class Generated774.BaseClass0>::ClassMethod1915<object>()
		calli default string(class Generated774.G3_C1246`1<class Generated774.BaseClass0>)
		ldstr "G3_C1246::ClassMethod1915.MI.15097<System.Object>()"
		ldstr "class G3_C1246`1<class Generated774.BaseClass0> on type class Generated774.G3_C1246`1<class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated774.G3_C1246`1<class Generated774.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated774.G3_C1246`1<class Generated774.BaseClass0>::ClassMethod1914()
		calli default string(class Generated774.G3_C1246`1<class Generated774.BaseClass0>)
		ldstr "G2_C293::ClassMethod1914.7472()"
		ldstr "class G3_C1246`1<class Generated774.BaseClass0> on type class Generated774.G3_C1246`1<class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated774.G3_C1246`1<class Generated774.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated774.G3_C1246`1<class Generated774.BaseClass0>::ClassMethod1913()
		calli default string(class Generated774.G3_C1246`1<class Generated774.BaseClass0>)
		ldstr "G3_C1246::ClassMethod1913.MI.15096()"
		ldstr "class G3_C1246`1<class Generated774.BaseClass0> on type class Generated774.G3_C1246`1<class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated774.G3_C1246`1<class Generated774.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated774.G3_C1246`1<class Generated774.BaseClass0>::Method7<object>()
		calli default string(class Generated774.G3_C1246`1<class Generated774.BaseClass0>)
		ldstr "G2_C293::Method7.7469<System.Object>()"
		ldstr "class G3_C1246`1<class Generated774.BaseClass0> on type class Generated774.G3_C1246`1<class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated774.G3_C1246`1<class Generated774.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated774.G3_C1246`1<class Generated774.BaseClass0>::ClassMethod1327<object>()
		calli default string(class Generated774.G3_C1246`1<class Generated774.BaseClass0>)
		ldstr "G2_C293::ClassMethod1327.MI.7475<System.Object>()"
		ldstr "class G3_C1246`1<class Generated774.BaseClass0> on type class Generated774.G3_C1246`1<class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated774.G3_C1246`1<class Generated774.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated774.G3_C1246`1<class Generated774.BaseClass0>::ClassMethod1326()
		calli default string(class Generated774.G3_C1246`1<class Generated774.BaseClass0>)
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G3_C1246`1<class Generated774.BaseClass0> on type class Generated774.G3_C1246`1<class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated774.IBase1`1<class Generated774.BaseClass0>::Method4()
		calli default string(class Generated774.G3_C1246`1<class Generated774.BaseClass0>)
		ldstr "G3_C1246::Method4.MI.15088()"
		ldstr "class IBase1`1<class Generated774.BaseClass0> on type class Generated774.G3_C1246`1<class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated774.IBase1`1<class Generated774.BaseClass0>::Method5()
		calli default string(class Generated774.G3_C1246`1<class Generated774.BaseClass0>)
		ldstr "G3_C1246::Method5.MI.15090()"
		ldstr "class IBase1`1<class Generated774.BaseClass0> on type class Generated774.G3_C1246`1<class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated774.IBase1`1<class Generated774.BaseClass0>::Method6<object>()
		calli default string(class Generated774.G3_C1246`1<class Generated774.BaseClass0>)
		ldstr "G3_C1246::Method6.MI.15092<System.Object>()"
		ldstr "class IBase1`1<class Generated774.BaseClass0> on type class Generated774.G3_C1246`1<class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated774.G3_C1246`1<class Generated774.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::ClassMethod1327<object>()
		calli default string(class Generated774.G3_C1246`1<class Generated774.BaseClass1>)
		ldstr "G2_C293::ClassMethod1327.MI.7475<System.Object>()"
		ldstr "class G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0> on type class Generated774.G3_C1246`1<class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::ClassMethod1326()
		calli default string(class Generated774.G3_C1246`1<class Generated774.BaseClass1>)
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0> on type class Generated774.G3_C1246`1<class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::Method7<object>()
		calli default string(class Generated774.G3_C1246`1<class Generated774.BaseClass1>)
		ldstr "G2_C293::Method7.7469<System.Object>()"
		ldstr "class G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0> on type class Generated774.G3_C1246`1<class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated774.IBase2`2<class Generated774.BaseClass1,class Generated774.BaseClass1>::Method7<object>()
		calli default string(class Generated774.G3_C1246`1<class Generated774.BaseClass1>)
		ldstr "G2_C293::Method7.7469<System.Object>()"
		ldstr "class IBase2`2<class Generated774.BaseClass1,class Generated774.BaseClass1> on type class Generated774.G3_C1246`1<class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated774.IBase2`2<class Generated774.BaseClass0,class Generated774.BaseClass1>::Method7<object>()
		calli default string(class Generated774.G3_C1246`1<class Generated774.BaseClass1>)
		ldstr "G2_C293::Method7.MI.7470<System.Object>()"
		ldstr "class IBase2`2<class Generated774.BaseClass0,class Generated774.BaseClass1> on type class Generated774.G3_C1246`1<class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::ClassMethod1916<object>()
		calli default string(class Generated774.G3_C1246`1<class Generated774.BaseClass1>)
		ldstr "G2_C293::ClassMethod1916.7474<System.Object>()"
		ldstr "class G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0> on type class Generated774.G3_C1246`1<class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::ClassMethod1915<object>()
		calli default string(class Generated774.G3_C1246`1<class Generated774.BaseClass1>)
		ldstr "G3_C1246::ClassMethod1915.MI.15097<System.Object>()"
		ldstr "class G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0> on type class Generated774.G3_C1246`1<class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::ClassMethod1914()
		calli default string(class Generated774.G3_C1246`1<class Generated774.BaseClass1>)
		ldstr "G2_C293::ClassMethod1914.7472()"
		ldstr "class G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0> on type class Generated774.G3_C1246`1<class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::ClassMethod1913()
		calli default string(class Generated774.G3_C1246`1<class Generated774.BaseClass1>)
		ldstr "G3_C1246::ClassMethod1913.MI.15096()"
		ldstr "class G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0> on type class Generated774.G3_C1246`1<class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::Method7<object>()
		calli default string(class Generated774.G3_C1246`1<class Generated774.BaseClass1>)
		ldstr "G2_C293::Method7.7469<System.Object>()"
		ldstr "class G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0> on type class Generated774.G3_C1246`1<class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::ClassMethod1327<object>()
		calli default string(class Generated774.G3_C1246`1<class Generated774.BaseClass1>)
		ldstr "G2_C293::ClassMethod1327.MI.7475<System.Object>()"
		ldstr "class G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0> on type class Generated774.G3_C1246`1<class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::ClassMethod1326()
		calli default string(class Generated774.G3_C1246`1<class Generated774.BaseClass1>)
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0> on type class Generated774.G3_C1246`1<class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated774.IBase2`2<class Generated774.BaseClass0,class Generated774.BaseClass0>::Method7<object>()
		calli default string(class Generated774.G3_C1246`1<class Generated774.BaseClass1>)
		ldstr "G2_C293::Method7.MI.7470<System.Object>()"
		ldstr "class IBase2`2<class Generated774.BaseClass0,class Generated774.BaseClass0> on type class Generated774.G3_C1246`1<class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated774.G3_C1246`1<class Generated774.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated774.G3_C1246`1<class Generated774.BaseClass1>::ClassMethod3872<object>()
		calli default string(class Generated774.G3_C1246`1<class Generated774.BaseClass1>)
		ldstr "G3_C1246::ClassMethod3872.15095<System.Object>()"
		ldstr "class G3_C1246`1<class Generated774.BaseClass1> on type class Generated774.G3_C1246`1<class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated774.G3_C1246`1<class Generated774.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated774.G3_C1246`1<class Generated774.BaseClass1>::ClassMethod3871<object>()
		calli default string(class Generated774.G3_C1246`1<class Generated774.BaseClass1>)
		ldstr "G3_C1246::ClassMethod3871.15094<System.Object>()"
		ldstr "class G3_C1246`1<class Generated774.BaseClass1> on type class Generated774.G3_C1246`1<class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated774.G3_C1246`1<class Generated774.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated774.G3_C1246`1<class Generated774.BaseClass1>::ClassMethod3870()
		calli default string(class Generated774.G3_C1246`1<class Generated774.BaseClass1>)
		ldstr "G3_C1246::ClassMethod3870.15093()"
		ldstr "class G3_C1246`1<class Generated774.BaseClass1> on type class Generated774.G3_C1246`1<class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated774.G3_C1246`1<class Generated774.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated774.G3_C1246`1<class Generated774.BaseClass1>::Method6<object>()
		calli default string(class Generated774.G3_C1246`1<class Generated774.BaseClass1>)
		ldstr "G3_C1246::Method6.15091<System.Object>()"
		ldstr "class G3_C1246`1<class Generated774.BaseClass1> on type class Generated774.G3_C1246`1<class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated774.G3_C1246`1<class Generated774.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated774.G3_C1246`1<class Generated774.BaseClass1>::Method5()
		calli default string(class Generated774.G3_C1246`1<class Generated774.BaseClass1>)
		ldstr "G3_C1246::Method5.15089()"
		ldstr "class G3_C1246`1<class Generated774.BaseClass1> on type class Generated774.G3_C1246`1<class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated774.G3_C1246`1<class Generated774.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated774.G3_C1246`1<class Generated774.BaseClass1>::Method4()
		calli default string(class Generated774.G3_C1246`1<class Generated774.BaseClass1>)
		ldstr "G3_C1246::Method4.15087()"
		ldstr "class G3_C1246`1<class Generated774.BaseClass1> on type class Generated774.G3_C1246`1<class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated774.G3_C1246`1<class Generated774.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated774.G3_C1246`1<class Generated774.BaseClass1>::ClassMethod1916<object>()
		calli default string(class Generated774.G3_C1246`1<class Generated774.BaseClass1>)
		ldstr "G2_C293::ClassMethod1916.7474<System.Object>()"
		ldstr "class G3_C1246`1<class Generated774.BaseClass1> on type class Generated774.G3_C1246`1<class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated774.G3_C1246`1<class Generated774.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated774.G3_C1246`1<class Generated774.BaseClass1>::ClassMethod1915<object>()
		calli default string(class Generated774.G3_C1246`1<class Generated774.BaseClass1>)
		ldstr "G3_C1246::ClassMethod1915.MI.15097<System.Object>()"
		ldstr "class G3_C1246`1<class Generated774.BaseClass1> on type class Generated774.G3_C1246`1<class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated774.G3_C1246`1<class Generated774.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated774.G3_C1246`1<class Generated774.BaseClass1>::ClassMethod1914()
		calli default string(class Generated774.G3_C1246`1<class Generated774.BaseClass1>)
		ldstr "G2_C293::ClassMethod1914.7472()"
		ldstr "class G3_C1246`1<class Generated774.BaseClass1> on type class Generated774.G3_C1246`1<class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated774.G3_C1246`1<class Generated774.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated774.G3_C1246`1<class Generated774.BaseClass1>::ClassMethod1913()
		calli default string(class Generated774.G3_C1246`1<class Generated774.BaseClass1>)
		ldstr "G3_C1246::ClassMethod1913.MI.15096()"
		ldstr "class G3_C1246`1<class Generated774.BaseClass1> on type class Generated774.G3_C1246`1<class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated774.G3_C1246`1<class Generated774.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated774.G3_C1246`1<class Generated774.BaseClass1>::Method7<object>()
		calli default string(class Generated774.G3_C1246`1<class Generated774.BaseClass1>)
		ldstr "G2_C293::Method7.7469<System.Object>()"
		ldstr "class G3_C1246`1<class Generated774.BaseClass1> on type class Generated774.G3_C1246`1<class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated774.G3_C1246`1<class Generated774.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated774.G3_C1246`1<class Generated774.BaseClass1>::ClassMethod1327<object>()
		calli default string(class Generated774.G3_C1246`1<class Generated774.BaseClass1>)
		ldstr "G2_C293::ClassMethod1327.MI.7475<System.Object>()"
		ldstr "class G3_C1246`1<class Generated774.BaseClass1> on type class Generated774.G3_C1246`1<class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated774.G3_C1246`1<class Generated774.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated774.G3_C1246`1<class Generated774.BaseClass1>::ClassMethod1326()
		calli default string(class Generated774.G3_C1246`1<class Generated774.BaseClass1>)
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G3_C1246`1<class Generated774.BaseClass1> on type class Generated774.G3_C1246`1<class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated774.IBase1`1<class Generated774.BaseClass1>::Method4()
		calli default string(class Generated774.G3_C1246`1<class Generated774.BaseClass1>)
		ldstr "G3_C1246::Method4.MI.15088()"
		ldstr "class IBase1`1<class Generated774.BaseClass1> on type class Generated774.G3_C1246`1<class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated774.IBase1`1<class Generated774.BaseClass1>::Method5()
		calli default string(class Generated774.G3_C1246`1<class Generated774.BaseClass1>)
		ldstr "G3_C1246::Method5.MI.15090()"
		ldstr "class IBase1`1<class Generated774.BaseClass1> on type class Generated774.G3_C1246`1<class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated774.IBase1`1<class Generated774.BaseClass1>::Method6<object>()
		calli default string(class Generated774.G3_C1246`1<class Generated774.BaseClass1>)
		ldstr "G3_C1246::Method6.MI.15092<System.Object>()"
		ldstr "class IBase1`1<class Generated774.BaseClass1> on type class Generated774.G3_C1246`1<class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated774.IBase1`1<class Generated774.BaseClass0>::Method4()
		calli default string(class Generated774.G3_C1246`1<class Generated774.BaseClass1>)
		ldstr "G3_C1246::Method4.MI.15088()"
		ldstr "class IBase1`1<class Generated774.BaseClass0> on type class Generated774.G3_C1246`1<class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated774.IBase1`1<class Generated774.BaseClass0>::Method5()
		calli default string(class Generated774.G3_C1246`1<class Generated774.BaseClass1>)
		ldstr "G3_C1246::Method5.MI.15090()"
		ldstr "class IBase1`1<class Generated774.BaseClass0> on type class Generated774.G3_C1246`1<class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated774.IBase1`1<class Generated774.BaseClass0>::Method6<object>()
		calli default string(class Generated774.G3_C1246`1<class Generated774.BaseClass1>)
		ldstr "G3_C1246::Method6.MI.15092<System.Object>()"
		ldstr "class IBase1`1<class Generated774.BaseClass0> on type class Generated774.G3_C1246`1<class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::ClassMethod1327<object>()
		calli default string(class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>)
		ldstr "G2_C293::ClassMethod1327.MI.7475<System.Object>()"
		ldstr "class G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0> on type class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::ClassMethod1326()
		calli default string(class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>)
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0> on type class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::Method7<object>()
		calli default string(class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>)
		ldstr "G2_C293::Method7.7469<System.Object>()"
		ldstr "class G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0> on type class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated774.IBase2`2<class Generated774.BaseClass1,class Generated774.BaseClass1>::Method7<object>()
		calli default string(class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>)
		ldstr "G2_C293::Method7.7469<System.Object>()"
		ldstr "class IBase2`2<class Generated774.BaseClass1,class Generated774.BaseClass1> on type class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated774.IBase2`2<class Generated774.BaseClass0,class Generated774.BaseClass1>::Method7<object>()
		calli default string(class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>)
		ldstr "G2_C293::Method7.MI.7470<System.Object>()"
		ldstr "class IBase2`2<class Generated774.BaseClass0,class Generated774.BaseClass1> on type class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>::ClassMethod1916<object>()
		calli default string(class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>)
		ldstr "G2_C293::ClassMethod1916.7474<System.Object>()"
		ldstr "class G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0> on type class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>::ClassMethod1915<object>()
		calli default string(class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>)
		ldstr "G2_C293::ClassMethod1915.7473<System.Object>()"
		ldstr "class G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0> on type class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>::ClassMethod1914()
		calli default string(class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>)
		ldstr "G2_C293::ClassMethod1914.7472()"
		ldstr "class G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0> on type class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>::ClassMethod1913()
		calli default string(class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>)
		ldstr "G2_C293::ClassMethod1913.7471()"
		ldstr "class G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0> on type class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>::Method7<object>()
		calli default string(class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>)
		ldstr "G2_C293::Method7.7469<System.Object>()"
		ldstr "class G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0> on type class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>::ClassMethod1327<object>()
		calli default string(class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>)
		ldstr "G2_C293::ClassMethod1327.MI.7475<System.Object>()"
		ldstr "class G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0> on type class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>::ClassMethod1326()
		calli default string(class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>)
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0> on type class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated774.IBase2`2<class Generated774.BaseClass0,class Generated774.BaseClass0>::Method7<object>()
		calli default string(class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>)
		ldstr "G2_C293::Method7.MI.7470<System.Object>()"
		ldstr "class IBase2`2<class Generated774.BaseClass0,class Generated774.BaseClass0> on type class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass1>::ClassMethod1327<object>()
		calli default string(class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>)
		ldstr "G2_C293::ClassMethod1327.MI.7475<System.Object>()"
		ldstr "class G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass1> on type class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass1>::ClassMethod1326()
		calli default string(class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>)
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass1> on type class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass1>::Method7<object>()
		calli default string(class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>)
		ldstr "G2_C293::Method7.7469<System.Object>()"
		ldstr "class G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass1> on type class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated774.IBase2`2<class Generated774.BaseClass1,class Generated774.BaseClass1>::Method7<object>()
		calli default string(class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>)
		ldstr "G2_C293::Method7.7469<System.Object>()"
		ldstr "class IBase2`2<class Generated774.BaseClass1,class Generated774.BaseClass1> on type class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated774.IBase2`2<class Generated774.BaseClass0,class Generated774.BaseClass1>::Method7<object>()
		calli default string(class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>)
		ldstr "G2_C293::Method7.MI.7470<System.Object>()"
		ldstr "class IBase2`2<class Generated774.BaseClass0,class Generated774.BaseClass1> on type class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>::ClassMethod1916<object>()
		calli default string(class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>)
		ldstr "G2_C293::ClassMethod1916.7474<System.Object>()"
		ldstr "class G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1> on type class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>::ClassMethod1915<object>()
		calli default string(class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>)
		ldstr "G2_C293::ClassMethod1915.7473<System.Object>()"
		ldstr "class G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1> on type class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>::ClassMethod1914()
		calli default string(class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>)
		ldstr "G2_C293::ClassMethod1914.7472()"
		ldstr "class G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1> on type class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>::ClassMethod1913()
		calli default string(class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>)
		ldstr "G2_C293::ClassMethod1913.7471()"
		ldstr "class G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1> on type class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>::Method7<object>()
		calli default string(class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>)
		ldstr "G2_C293::Method7.7469<System.Object>()"
		ldstr "class G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1> on type class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>::ClassMethod1327<object>()
		calli default string(class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>)
		ldstr "G2_C293::ClassMethod1327.MI.7475<System.Object>()"
		ldstr "class G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1> on type class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>::ClassMethod1326()
		calli default string(class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>)
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1> on type class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated774.IBase2`2<class Generated774.BaseClass0,class Generated774.BaseClass0>::Method7<object>()
		calli default string(class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>)
		ldstr "G2_C293::Method7.MI.7470<System.Object>()"
		ldstr "class IBase2`2<class Generated774.BaseClass0,class Generated774.BaseClass0> on type class Generated774.G2_C293`2<class Generated774.BaseClass0,class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::ClassMethod1327<object>()
		calli default string(class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>)
		ldstr "G2_C293::ClassMethod1327.MI.7475<System.Object>()"
		ldstr "class G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0> on type class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::ClassMethod1326()
		calli default string(class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>)
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0> on type class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::Method7<object>()
		calli default string(class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>)
		ldstr "G2_C293::Method7.7469<System.Object>()"
		ldstr "class G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0> on type class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated774.IBase2`2<class Generated774.BaseClass1,class Generated774.BaseClass1>::Method7<object>()
		calli default string(class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>)
		ldstr "G2_C293::Method7.7469<System.Object>()"
		ldstr "class IBase2`2<class Generated774.BaseClass1,class Generated774.BaseClass1> on type class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated774.IBase2`2<class Generated774.BaseClass0,class Generated774.BaseClass1>::Method7<object>()
		calli default string(class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>)
		ldstr "G2_C293::Method7.MI.7470<System.Object>()"
		ldstr "class IBase2`2<class Generated774.BaseClass0,class Generated774.BaseClass1> on type class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::ClassMethod1916<object>()
		calli default string(class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>)
		ldstr "G2_C293::ClassMethod1916.7474<System.Object>()"
		ldstr "class G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0> on type class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::ClassMethod1915<object>()
		calli default string(class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>)
		ldstr "G2_C293::ClassMethod1915.7473<System.Object>()"
		ldstr "class G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0> on type class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::ClassMethod1914()
		calli default string(class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>)
		ldstr "G2_C293::ClassMethod1914.7472()"
		ldstr "class G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0> on type class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::ClassMethod1913()
		calli default string(class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>)
		ldstr "G2_C293::ClassMethod1913.7471()"
		ldstr "class G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0> on type class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::Method7<object>()
		calli default string(class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>)
		ldstr "G2_C293::Method7.7469<System.Object>()"
		ldstr "class G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0> on type class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::ClassMethod1327<object>()
		calli default string(class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>)
		ldstr "G2_C293::ClassMethod1327.MI.7475<System.Object>()"
		ldstr "class G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0> on type class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::ClassMethod1326()
		calli default string(class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>)
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0> on type class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated774.IBase2`2<class Generated774.BaseClass0,class Generated774.BaseClass0>::Method7<object>()
		calli default string(class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>)
		ldstr "G2_C293::Method7.MI.7470<System.Object>()"
		ldstr "class IBase2`2<class Generated774.BaseClass0,class Generated774.BaseClass0> on type class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass1>::ClassMethod1327<object>()
		calli default string(class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>)
		ldstr "G2_C293::ClassMethod1327.MI.7475<System.Object>()"
		ldstr "class G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass1> on type class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass1>::ClassMethod1326()
		calli default string(class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>)
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass1> on type class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass1>::Method7<object>()
		calli default string(class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>)
		ldstr "G2_C293::Method7.7469<System.Object>()"
		ldstr "class G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass1> on type class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated774.IBase2`2<class Generated774.BaseClass1,class Generated774.BaseClass1>::Method7<object>()
		calli default string(class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>)
		ldstr "G2_C293::Method7.7469<System.Object>()"
		ldstr "class IBase2`2<class Generated774.BaseClass1,class Generated774.BaseClass1> on type class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated774.IBase2`2<class Generated774.BaseClass0,class Generated774.BaseClass1>::Method7<object>()
		calli default string(class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>)
		ldstr "G2_C293::Method7.MI.7470<System.Object>()"
		ldstr "class IBase2`2<class Generated774.BaseClass0,class Generated774.BaseClass1> on type class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>::ClassMethod1916<object>()
		calli default string(class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>)
		ldstr "G2_C293::ClassMethod1916.7474<System.Object>()"
		ldstr "class G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1> on type class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>::ClassMethod1915<object>()
		calli default string(class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>)
		ldstr "G2_C293::ClassMethod1915.7473<System.Object>()"
		ldstr "class G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1> on type class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>::ClassMethod1914()
		calli default string(class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>)
		ldstr "G2_C293::ClassMethod1914.7472()"
		ldstr "class G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1> on type class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>::ClassMethod1913()
		calli default string(class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>)
		ldstr "G2_C293::ClassMethod1913.7471()"
		ldstr "class G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1> on type class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>::Method7<object>()
		calli default string(class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>)
		ldstr "G2_C293::Method7.7469<System.Object>()"
		ldstr "class G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1> on type class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>::ClassMethod1327<object>()
		calli default string(class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>)
		ldstr "G2_C293::ClassMethod1327.MI.7475<System.Object>()"
		ldstr "class G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1> on type class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>::ClassMethod1326()
		calli default string(class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>)
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1> on type class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated774.IBase2`2<class Generated774.BaseClass0,class Generated774.BaseClass0>::Method7<object>()
		calli default string(class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>)
		ldstr "G2_C293::Method7.MI.7470<System.Object>()"
		ldstr "class IBase2`2<class Generated774.BaseClass0,class Generated774.BaseClass0> on type class Generated774.G2_C293`2<class Generated774.BaseClass1,class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass0>::ClassMethod1327<object>()
		calli default string(class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass0>)
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass0> on type class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass0>::ClassMethod1326()
		calli default string(class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass0>)
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass0> on type class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass0>::Method7<object>()
		calli default string(class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass0> on type class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated774.IBase2`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::Method7<object>()
		calli default string(class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated774.BaseClass1,class Generated774.BaseClass0> on type class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated774.IBase2`2<class Generated774.BaseClass0,class Generated774.BaseClass0>::Method7<object>()
		calli default string(class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated774.BaseClass0,class Generated774.BaseClass0> on type class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated774.IBase2`2<class Generated774.BaseClass0,class Generated774.BaseClass1>::Method7<object>()
		calli default string(class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated774.BaseClass0,class Generated774.BaseClass1> on type class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated774.IBase2`2<class Generated774.BaseClass1,class Generated774.BaseClass1>::Method7<object>()
		calli default string(class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated774.BaseClass1,class Generated774.BaseClass1> on type class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass1>::ClassMethod1327<object>()
		calli default string(class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass1>)
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass1> on type class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass1>::ClassMethod1326()
		calli default string(class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass1>)
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass1> on type class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass1>::Method7<object>()
		calli default string(class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass1>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass1> on type class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated774.IBase2`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::Method7<object>()
		calli default string(class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass1>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated774.BaseClass1,class Generated774.BaseClass0> on type class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated774.IBase2`2<class Generated774.BaseClass0,class Generated774.BaseClass0>::Method7<object>()
		calli default string(class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass1>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated774.BaseClass0,class Generated774.BaseClass0> on type class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated774.IBase2`2<class Generated774.BaseClass0,class Generated774.BaseClass1>::Method7<object>()
		calli default string(class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass1>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated774.BaseClass0,class Generated774.BaseClass1> on type class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated774.IBase2`2<class Generated774.BaseClass1,class Generated774.BaseClass1>::Method7<object>()
		calli default string(class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass1>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated774.BaseClass1,class Generated774.BaseClass1> on type class Generated774.G1_C6`2<class Generated774.BaseClass0,class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::ClassMethod1327<object>()
		calli default string(class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0>)
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0> on type class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::ClassMethod1326()
		calli default string(class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0>)
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0> on type class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0>::Method7<object>()
		calli default string(class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0> on type class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated774.IBase2`2<class Generated774.BaseClass1,class Generated774.BaseClass1>::Method7<object>()
		calli default string(class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated774.BaseClass1,class Generated774.BaseClass1> on type class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated774.IBase2`2<class Generated774.BaseClass0,class Generated774.BaseClass1>::Method7<object>()
		calli default string(class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated774.BaseClass0,class Generated774.BaseClass1> on type class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass1>::ClassMethod1327<object>()
		calli default string(class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass1>)
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass1> on type class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass1>::ClassMethod1326()
		calli default string(class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass1>)
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass1> on type class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass1>::Method7<object>()
		calli default string(class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass1>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass1> on type class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated774.IBase2`2<class Generated774.BaseClass1,class Generated774.BaseClass1>::Method7<object>()
		calli default string(class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass1>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated774.BaseClass1,class Generated774.BaseClass1> on type class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated774.IBase2`2<class Generated774.BaseClass0,class Generated774.BaseClass1>::Method7<object>()
		calli default string(class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass1>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated774.BaseClass0,class Generated774.BaseClass1> on type class Generated774.G1_C6`2<class Generated774.BaseClass1,class Generated774.BaseClass1>"
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
		call void Generated774.Generated774::MethodCallingTest()
		call void Generated774.Generated774::ConstrainedCallsTest()
		call void Generated774.Generated774::StructConstrainedInterfaceCallsTest()
		call void Generated774.Generated774::CalliTest()
		ldc.i4 100
		ret
	}
}
