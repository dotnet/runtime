// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.class public Generated794.BaseClass0 
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void [mscorlib]System.Object::.ctor()
		ret
	}
}
.class public Generated794.BaseClass1 
		extends Generated794.BaseClass0
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void Generated794.BaseClass0::.ctor()
		ret
	}
}
.class public Generated794.G3_C1266`1<T0> 
		extends class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>
		implements class Generated794.IBase1`1<class Generated794.BaseClass0> 
{
	.method public hidebysig newslot virtual instance string Method4() cil managed noinlining { 
		ldstr "G3_C1266::Method4.15209()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class Generated794.BaseClass0>.Method4'() cil managed noinlining { 
		.override method instance string class Generated794.IBase1`1<class Generated794.BaseClass0>::Method4()
		ldstr "G3_C1266::Method4.MI.15210()"
		ret
	}
	.method public hidebysig virtual instance string Method5() cil managed noinlining { 
		ldstr "G3_C1266::Method5.15211()"
		ret
	}
	.method public hidebysig virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G3_C1266::Method6.15212<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class Generated794.BaseClass0>.Method6'<M0>() cil managed noinlining { 
		.override method instance string class Generated794.IBase1`1<class Generated794.BaseClass0>::Method6<[1]>()
		ldstr "G3_C1266::Method6.MI.15213<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod3904() cil managed noinlining { 
		ldstr "G3_C1266::ClassMethod3904.15214()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod3905<M0>() cil managed noinlining { 
		ldstr "G3_C1266::ClassMethod3905.15215<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C316<class Generated794.BaseClass1,class Generated794.BaseClass1>.ClassMethod1967'() cil managed noinlining { 
		.override method instance string class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::ClassMethod1967()
		ldstr "G3_C1266::ClassMethod1967.MI.15216()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C316<class Generated794.BaseClass1,class Generated794.BaseClass1>.ClassMethod1968'<M0>() cil managed noinlining { 
		.override method instance string class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::ClassMethod1968<[1]>()
		ldstr "G3_C1266::ClassMethod1968.MI.15217<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C316<class Generated794.BaseClass1,class Generated794.BaseClass1>.ClassMethod1969'<M0>() cil managed noinlining { 
		.override method instance string class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::ClassMethod1969<[1]>()
		ldstr "G3_C1266::ClassMethod1969.MI.15218<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::.ctor()
		ret
	}
}
.class public abstract Generated794.G2_C316`2<T0, T1> 
		extends class Generated794.G1_C6`2<!T1,class Generated794.BaseClass1>
		implements Generated794.IBase0, class Generated794.IBase2`2<class Generated794.BaseClass0,class Generated794.BaseClass0> 
{
	.method public hidebysig newslot virtual instance string Method0() cil managed noinlining { 
		ldstr "G2_C316::Method0.7700()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method1() cil managed noinlining { 
		ldstr "G2_C316::Method1.7701()"
		ret
	}
	.method public hidebysig virtual instance string Method2<M0>() cil managed noinlining { 
		ldstr "G2_C316::Method2.7702<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string Method3<M0>() cil managed noinlining { 
		ldstr "G2_C316::Method3.7703<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G2_C316::Method7.7704<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1966() cil managed noinlining { 
		ldstr "G2_C316::ClassMethod1966.7705()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1967() cil managed noinlining { 
		ldstr "G2_C316::ClassMethod1967.7706()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1968<M0>() cil managed noinlining { 
		ldstr "G2_C316::ClassMethod1968.7707<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1969<M0>() cil managed noinlining { 
		ldstr "G2_C316::ClassMethod1969.7708<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class Generated794.G1_C6`2<!T1,class Generated794.BaseClass1>::.ctor()
		ret
	}
}
.class interface public abstract Generated794.IBase1`1<+T0> 
{
	.method public hidebysig newslot abstract virtual instance string Method4() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method5() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method6<M0>() cil managed { }
}
.class public Generated794.G1_C6`2<T0, T1> 
		implements class Generated794.IBase2`2<class Generated794.BaseClass1,!T0> 
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
.class interface public abstract Generated794.IBase0 
{
	.method public hidebysig newslot abstract virtual instance string Method0() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method1() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method2<M0>() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method3<M0>() cil managed { }
}
.class interface public abstract Generated794.IBase2`2<+T0, -T1> 
{
	.method public hidebysig newslot abstract virtual instance string Method7<M0>() cil managed { }
}
.class public auto ansi beforefieldinit Generated794.Generated794 {
	.method static void M.BaseClass0<(Generated794.BaseClass0)W>(!!W inst, string exp) cil managed {
		.maxstack 5
		.locals init (string[] actualResults)
		ldc.i4.s    0
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.BaseClass0<(Generated794.BaseClass0)W>(!!W inst, string exp)"
		ldc.i4.s 0
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.BaseClass1<(Generated794.BaseClass1)W>(!!W inst, string exp) cil managed {
		.maxstack 5
		.locals init (string[] actualResults)
		ldc.i4.s    0
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.BaseClass1<(Generated794.BaseClass1)W>(!!W inst, string exp)"
		ldc.i4.s 0
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1266.T<T0,(class Generated794.G3_C1266`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 21
		.locals init (string[] actualResults)
		ldc.i4.s    16
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1266.T<T0,(class Generated794.G3_C1266`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 16
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G3_C1266`1<!!T0>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G3_C1266`1<!!T0>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G3_C1266`1<!!T0>::ClassMethod1966()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G3_C1266`1<!!T0>::ClassMethod1967()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G3_C1266`1<!!T0>::ClassMethod1968<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G3_C1266`1<!!T0>::ClassMethod1969<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G3_C1266`1<!!T0>::ClassMethod3904()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G3_C1266`1<!!T0>::ClassMethod3905<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G3_C1266`1<!!T0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G3_C1266`1<!!T0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G3_C1266`1<!!T0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G3_C1266`1<!!T0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G3_C1266`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G3_C1266`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     14
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G3_C1266`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     15
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G3_C1266`1<!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1266.A<(class Generated794.G3_C1266`1<class Generated794.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 21
		.locals init (string[] actualResults)
		ldc.i4.s    16
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1266.A<(class Generated794.G3_C1266`1<class Generated794.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 16
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G3_C1266`1<class Generated794.BaseClass0>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G3_C1266`1<class Generated794.BaseClass0>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G3_C1266`1<class Generated794.BaseClass0>::ClassMethod1966()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G3_C1266`1<class Generated794.BaseClass0>::ClassMethod1967()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G3_C1266`1<class Generated794.BaseClass0>::ClassMethod1968<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G3_C1266`1<class Generated794.BaseClass0>::ClassMethod1969<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G3_C1266`1<class Generated794.BaseClass0>::ClassMethod3904()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G3_C1266`1<class Generated794.BaseClass0>::ClassMethod3905<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G3_C1266`1<class Generated794.BaseClass0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G3_C1266`1<class Generated794.BaseClass0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G3_C1266`1<class Generated794.BaseClass0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G3_C1266`1<class Generated794.BaseClass0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G3_C1266`1<class Generated794.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G3_C1266`1<class Generated794.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     14
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G3_C1266`1<class Generated794.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     15
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G3_C1266`1<class Generated794.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1266.B<(class Generated794.G3_C1266`1<class Generated794.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 21
		.locals init (string[] actualResults)
		ldc.i4.s    16
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1266.B<(class Generated794.G3_C1266`1<class Generated794.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 16
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G3_C1266`1<class Generated794.BaseClass1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G3_C1266`1<class Generated794.BaseClass1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G3_C1266`1<class Generated794.BaseClass1>::ClassMethod1966()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G3_C1266`1<class Generated794.BaseClass1>::ClassMethod1967()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G3_C1266`1<class Generated794.BaseClass1>::ClassMethod1968<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G3_C1266`1<class Generated794.BaseClass1>::ClassMethod1969<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G3_C1266`1<class Generated794.BaseClass1>::ClassMethod3904()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G3_C1266`1<class Generated794.BaseClass1>::ClassMethod3905<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G3_C1266`1<class Generated794.BaseClass1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G3_C1266`1<class Generated794.BaseClass1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G3_C1266`1<class Generated794.BaseClass1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G3_C1266`1<class Generated794.BaseClass1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G3_C1266`1<class Generated794.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G3_C1266`1<class Generated794.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     14
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G3_C1266`1<class Generated794.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     15
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G3_C1266`1<class Generated794.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C316.T.T<T0,T1,(class Generated794.G2_C316`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C316.T.T<T0,T1,(class Generated794.G2_C316`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G2_C316`2<!!T0,!!T1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G2_C316`2<!!T0,!!T1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G2_C316`2<!!T0,!!T1>::ClassMethod1966()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G2_C316`2<!!T0,!!T1>::ClassMethod1967()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G2_C316`2<!!T0,!!T1>::ClassMethod1968<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G2_C316`2<!!T0,!!T1>::ClassMethod1969<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G2_C316`2<!!T0,!!T1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G2_C316`2<!!T0,!!T1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G2_C316`2<!!T0,!!T1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G2_C316`2<!!T0,!!T1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G2_C316`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C316.A.T<T1,(class Generated794.G2_C316`2<class Generated794.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C316.A.T<T1,(class Generated794.G2_C316`2<class Generated794.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G2_C316`2<class Generated794.BaseClass0,!!T1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G2_C316`2<class Generated794.BaseClass0,!!T1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G2_C316`2<class Generated794.BaseClass0,!!T1>::ClassMethod1966()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G2_C316`2<class Generated794.BaseClass0,!!T1>::ClassMethod1967()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G2_C316`2<class Generated794.BaseClass0,!!T1>::ClassMethod1968<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G2_C316`2<class Generated794.BaseClass0,!!T1>::ClassMethod1969<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G2_C316`2<class Generated794.BaseClass0,!!T1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G2_C316`2<class Generated794.BaseClass0,!!T1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G2_C316`2<class Generated794.BaseClass0,!!T1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G2_C316`2<class Generated794.BaseClass0,!!T1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G2_C316`2<class Generated794.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C316.A.A<(class Generated794.G2_C316`2<class Generated794.BaseClass0,class Generated794.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C316.A.A<(class Generated794.G2_C316`2<class Generated794.BaseClass0,class Generated794.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G2_C316`2<class Generated794.BaseClass0,class Generated794.BaseClass0>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G2_C316`2<class Generated794.BaseClass0,class Generated794.BaseClass0>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G2_C316`2<class Generated794.BaseClass0,class Generated794.BaseClass0>::ClassMethod1966()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G2_C316`2<class Generated794.BaseClass0,class Generated794.BaseClass0>::ClassMethod1967()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G2_C316`2<class Generated794.BaseClass0,class Generated794.BaseClass0>::ClassMethod1968<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G2_C316`2<class Generated794.BaseClass0,class Generated794.BaseClass0>::ClassMethod1969<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G2_C316`2<class Generated794.BaseClass0,class Generated794.BaseClass0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G2_C316`2<class Generated794.BaseClass0,class Generated794.BaseClass0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G2_C316`2<class Generated794.BaseClass0,class Generated794.BaseClass0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G2_C316`2<class Generated794.BaseClass0,class Generated794.BaseClass0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G2_C316`2<class Generated794.BaseClass0,class Generated794.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C316.A.B<(class Generated794.G2_C316`2<class Generated794.BaseClass0,class Generated794.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C316.A.B<(class Generated794.G2_C316`2<class Generated794.BaseClass0,class Generated794.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G2_C316`2<class Generated794.BaseClass0,class Generated794.BaseClass1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G2_C316`2<class Generated794.BaseClass0,class Generated794.BaseClass1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G2_C316`2<class Generated794.BaseClass0,class Generated794.BaseClass1>::ClassMethod1966()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G2_C316`2<class Generated794.BaseClass0,class Generated794.BaseClass1>::ClassMethod1967()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G2_C316`2<class Generated794.BaseClass0,class Generated794.BaseClass1>::ClassMethod1968<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G2_C316`2<class Generated794.BaseClass0,class Generated794.BaseClass1>::ClassMethod1969<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G2_C316`2<class Generated794.BaseClass0,class Generated794.BaseClass1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G2_C316`2<class Generated794.BaseClass0,class Generated794.BaseClass1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G2_C316`2<class Generated794.BaseClass0,class Generated794.BaseClass1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G2_C316`2<class Generated794.BaseClass0,class Generated794.BaseClass1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G2_C316`2<class Generated794.BaseClass0,class Generated794.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C316.B.T<T1,(class Generated794.G2_C316`2<class Generated794.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C316.B.T<T1,(class Generated794.G2_C316`2<class Generated794.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G2_C316`2<class Generated794.BaseClass1,!!T1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G2_C316`2<class Generated794.BaseClass1,!!T1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G2_C316`2<class Generated794.BaseClass1,!!T1>::ClassMethod1966()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G2_C316`2<class Generated794.BaseClass1,!!T1>::ClassMethod1967()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G2_C316`2<class Generated794.BaseClass1,!!T1>::ClassMethod1968<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G2_C316`2<class Generated794.BaseClass1,!!T1>::ClassMethod1969<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G2_C316`2<class Generated794.BaseClass1,!!T1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G2_C316`2<class Generated794.BaseClass1,!!T1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G2_C316`2<class Generated794.BaseClass1,!!T1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G2_C316`2<class Generated794.BaseClass1,!!T1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G2_C316`2<class Generated794.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C316.B.A<(class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C316.B.A<(class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass0>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass0>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass0>::ClassMethod1966()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass0>::ClassMethod1967()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass0>::ClassMethod1968<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass0>::ClassMethod1969<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C316.B.B<(class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C316.B.B<(class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::ClassMethod1966()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::ClassMethod1967()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::ClassMethod1968<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::ClassMethod1969<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.T<T0,(class Generated794.IBase1`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.T<T0,(class Generated794.IBase1`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.IBase1`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.IBase1`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.IBase1`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.A<(class Generated794.IBase1`1<class Generated794.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.A<(class Generated794.IBase1`1<class Generated794.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.IBase1`1<class Generated794.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.IBase1`1<class Generated794.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.IBase1`1<class Generated794.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.B<(class Generated794.IBase1`1<class Generated794.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.B<(class Generated794.IBase1`1<class Generated794.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.IBase1`1<class Generated794.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.IBase1`1<class Generated794.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.IBase1`1<class Generated794.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C6.T.T<T0,T1,(class Generated794.G1_C6`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C6.T.T<T0,T1,(class Generated794.G1_C6`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G1_C6`2<!!T0,!!T1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G1_C6`2<!!T0,!!T1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G1_C6`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C6.A.T<T1,(class Generated794.G1_C6`2<class Generated794.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C6.A.T<T1,(class Generated794.G1_C6`2<class Generated794.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G1_C6`2<class Generated794.BaseClass0,!!T1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G1_C6`2<class Generated794.BaseClass0,!!T1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G1_C6`2<class Generated794.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C6.A.A<(class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C6.A.A<(class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass0>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass0>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C6.A.B<(class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C6.A.B<(class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C6.B.T<T1,(class Generated794.G1_C6`2<class Generated794.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C6.B.T<T1,(class Generated794.G1_C6`2<class Generated794.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G1_C6`2<class Generated794.BaseClass1,!!T1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G1_C6`2<class Generated794.BaseClass1,!!T1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G1_C6`2<class Generated794.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C6.B.A<(class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C6.B.A<(class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass0>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass0>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C6.B.B<(class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C6.B.B<(class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.Generated794.IBase0<(Generated794.IBase0)W>(!!W inst, string exp) cil managed {
		.maxstack 9
		.locals init (string[] actualResults)
		ldc.i4.s    4
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.Generated794.IBase0<(Generated794.IBase0)W>(!!W inst, string exp)"
		ldc.i4.s 4
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string Generated794.IBase0::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string Generated794.IBase0::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string Generated794.IBase0::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string Generated794.IBase0::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.T.T<T0,T1,(class Generated794.IBase2`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.T.T<T0,T1,(class Generated794.IBase2`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.IBase2`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.T<T1,(class Generated794.IBase2`2<class Generated794.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.T<T1,(class Generated794.IBase2`2<class Generated794.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.IBase2`2<class Generated794.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.A<(class Generated794.IBase2`2<class Generated794.BaseClass0,class Generated794.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.A<(class Generated794.IBase2`2<class Generated794.BaseClass0,class Generated794.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.IBase2`2<class Generated794.BaseClass0,class Generated794.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.B<(class Generated794.IBase2`2<class Generated794.BaseClass0,class Generated794.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.B<(class Generated794.IBase2`2<class Generated794.BaseClass0,class Generated794.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.IBase2`2<class Generated794.BaseClass0,class Generated794.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.T<T1,(class Generated794.IBase2`2<class Generated794.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.T<T1,(class Generated794.IBase2`2<class Generated794.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.IBase2`2<class Generated794.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.A<(class Generated794.IBase2`2<class Generated794.BaseClass1,class Generated794.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.A<(class Generated794.IBase2`2<class Generated794.BaseClass1,class Generated794.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.IBase2`2<class Generated794.BaseClass1,class Generated794.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.B<(class Generated794.IBase2`2<class Generated794.BaseClass1,class Generated794.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.B<(class Generated794.IBase2`2<class Generated794.BaseClass1,class Generated794.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated794.IBase2`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::Method7<object>()
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
		newobj instance void class Generated794.G3_C1266`1<class Generated794.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass1>
		callvirt instance string class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::ClassMethod1327<object>()
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass1>
		callvirt instance string class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::ClassMethod1326()
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass1>
		callvirt instance string class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated794.IBase2`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated794.BaseClass1,class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated794.IBase2`2<class Generated794.BaseClass0,class Generated794.BaseClass1>::Method7<object>()
		ldstr "G2_C316::Method7.7704<System.Object>()"
		ldstr "class IBase2`2<class Generated794.BaseClass0,class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>
		callvirt instance string class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::ClassMethod1969<object>()
		ldstr "G3_C1266::ClassMethod1969.MI.15218<System.Object>()"
		ldstr "class G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>
		callvirt instance string class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::ClassMethod1968<object>()
		ldstr "G3_C1266::ClassMethod1968.MI.15217<System.Object>()"
		ldstr "class G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>
		callvirt instance string class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::ClassMethod1967()
		ldstr "G3_C1266::ClassMethod1967.MI.15216()"
		ldstr "class G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>
		callvirt instance string class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::ClassMethod1966()
		ldstr "G2_C316::ClassMethod1966.7705()"
		ldstr "class G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>
		callvirt instance string class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::Method7<object>()
		ldstr "G2_C316::Method7.7704<System.Object>()"
		ldstr "class G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>
		callvirt instance string class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::Method3<object>()
		ldstr "G2_C316::Method3.7703<System.Object>()"
		ldstr "class G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>
		callvirt instance string class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::Method2<object>()
		ldstr "G2_C316::Method2.7702<System.Object>()"
		ldstr "class G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>
		callvirt instance string class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::Method1()
		ldstr "G2_C316::Method1.7701()"
		ldstr "class G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>
		callvirt instance string class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::Method0()
		ldstr "G2_C316::Method0.7700()"
		ldstr "class G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>
		callvirt instance string class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::ClassMethod1327<object>()
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>
		callvirt instance string class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::ClassMethod1326()
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string Generated794.IBase0::Method0()
		ldstr "G2_C316::Method0.7700()"
		ldstr "Generated794.IBase0 on type class Generated794.G3_C1266`1<class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated794.IBase0::Method1()
		ldstr "G2_C316::Method1.7701()"
		ldstr "Generated794.IBase0 on type class Generated794.G3_C1266`1<class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated794.IBase0::Method2<object>()
		ldstr "G2_C316::Method2.7702<System.Object>()"
		ldstr "Generated794.IBase0 on type class Generated794.G3_C1266`1<class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated794.IBase0::Method3<object>()
		ldstr "G2_C316::Method3.7703<System.Object>()"
		ldstr "Generated794.IBase0 on type class Generated794.G3_C1266`1<class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated794.IBase2`2<class Generated794.BaseClass0,class Generated794.BaseClass0>::Method7<object>()
		ldstr "G2_C316::Method7.7704<System.Object>()"
		ldstr "class IBase2`2<class Generated794.BaseClass0,class Generated794.BaseClass0> on type class Generated794.G3_C1266`1<class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated794.G3_C1266`1<class Generated794.BaseClass0>
		callvirt instance string class Generated794.G3_C1266`1<class Generated794.BaseClass0>::ClassMethod3905<object>()
		ldstr "G3_C1266::ClassMethod3905.15215<System.Object>()"
		ldstr "class G3_C1266`1<class Generated794.BaseClass0> on type class Generated794.G3_C1266`1<class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated794.G3_C1266`1<class Generated794.BaseClass0>
		callvirt instance string class Generated794.G3_C1266`1<class Generated794.BaseClass0>::ClassMethod3904()
		ldstr "G3_C1266::ClassMethod3904.15214()"
		ldstr "class G3_C1266`1<class Generated794.BaseClass0> on type class Generated794.G3_C1266`1<class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated794.G3_C1266`1<class Generated794.BaseClass0>
		callvirt instance string class Generated794.G3_C1266`1<class Generated794.BaseClass0>::Method6<object>()
		ldstr "G3_C1266::Method6.15212<System.Object>()"
		ldstr "class G3_C1266`1<class Generated794.BaseClass0> on type class Generated794.G3_C1266`1<class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated794.G3_C1266`1<class Generated794.BaseClass0>
		callvirt instance string class Generated794.G3_C1266`1<class Generated794.BaseClass0>::Method5()
		ldstr "G3_C1266::Method5.15211()"
		ldstr "class G3_C1266`1<class Generated794.BaseClass0> on type class Generated794.G3_C1266`1<class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated794.G3_C1266`1<class Generated794.BaseClass0>
		callvirt instance string class Generated794.G3_C1266`1<class Generated794.BaseClass0>::Method4()
		ldstr "G3_C1266::Method4.15209()"
		ldstr "class G3_C1266`1<class Generated794.BaseClass0> on type class Generated794.G3_C1266`1<class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated794.G3_C1266`1<class Generated794.BaseClass0>
		callvirt instance string class Generated794.G3_C1266`1<class Generated794.BaseClass0>::ClassMethod1969<object>()
		ldstr "G3_C1266::ClassMethod1969.MI.15218<System.Object>()"
		ldstr "class G3_C1266`1<class Generated794.BaseClass0> on type class Generated794.G3_C1266`1<class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated794.G3_C1266`1<class Generated794.BaseClass0>
		callvirt instance string class Generated794.G3_C1266`1<class Generated794.BaseClass0>::ClassMethod1968<object>()
		ldstr "G3_C1266::ClassMethod1968.MI.15217<System.Object>()"
		ldstr "class G3_C1266`1<class Generated794.BaseClass0> on type class Generated794.G3_C1266`1<class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated794.G3_C1266`1<class Generated794.BaseClass0>
		callvirt instance string class Generated794.G3_C1266`1<class Generated794.BaseClass0>::ClassMethod1967()
		ldstr "G3_C1266::ClassMethod1967.MI.15216()"
		ldstr "class G3_C1266`1<class Generated794.BaseClass0> on type class Generated794.G3_C1266`1<class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated794.G3_C1266`1<class Generated794.BaseClass0>
		callvirt instance string class Generated794.G3_C1266`1<class Generated794.BaseClass0>::ClassMethod1966()
		ldstr "G2_C316::ClassMethod1966.7705()"
		ldstr "class G3_C1266`1<class Generated794.BaseClass0> on type class Generated794.G3_C1266`1<class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated794.G3_C1266`1<class Generated794.BaseClass0>
		callvirt instance string class Generated794.G3_C1266`1<class Generated794.BaseClass0>::Method7<object>()
		ldstr "G2_C316::Method7.7704<System.Object>()"
		ldstr "class G3_C1266`1<class Generated794.BaseClass0> on type class Generated794.G3_C1266`1<class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated794.G3_C1266`1<class Generated794.BaseClass0>
		callvirt instance string class Generated794.G3_C1266`1<class Generated794.BaseClass0>::Method3<object>()
		ldstr "G2_C316::Method3.7703<System.Object>()"
		ldstr "class G3_C1266`1<class Generated794.BaseClass0> on type class Generated794.G3_C1266`1<class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated794.G3_C1266`1<class Generated794.BaseClass0>
		callvirt instance string class Generated794.G3_C1266`1<class Generated794.BaseClass0>::Method2<object>()
		ldstr "G2_C316::Method2.7702<System.Object>()"
		ldstr "class G3_C1266`1<class Generated794.BaseClass0> on type class Generated794.G3_C1266`1<class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated794.G3_C1266`1<class Generated794.BaseClass0>
		callvirt instance string class Generated794.G3_C1266`1<class Generated794.BaseClass0>::Method1()
		ldstr "G2_C316::Method1.7701()"
		ldstr "class G3_C1266`1<class Generated794.BaseClass0> on type class Generated794.G3_C1266`1<class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated794.G3_C1266`1<class Generated794.BaseClass0>
		callvirt instance string class Generated794.G3_C1266`1<class Generated794.BaseClass0>::Method0()
		ldstr "G2_C316::Method0.7700()"
		ldstr "class G3_C1266`1<class Generated794.BaseClass0> on type class Generated794.G3_C1266`1<class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated794.G3_C1266`1<class Generated794.BaseClass0>
		callvirt instance string class Generated794.G3_C1266`1<class Generated794.BaseClass0>::ClassMethod1327<object>()
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G3_C1266`1<class Generated794.BaseClass0> on type class Generated794.G3_C1266`1<class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated794.G3_C1266`1<class Generated794.BaseClass0>
		callvirt instance string class Generated794.G3_C1266`1<class Generated794.BaseClass0>::ClassMethod1326()
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G3_C1266`1<class Generated794.BaseClass0> on type class Generated794.G3_C1266`1<class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated794.IBase1`1<class Generated794.BaseClass0>::Method4()
		ldstr "G3_C1266::Method4.MI.15210()"
		ldstr "class IBase1`1<class Generated794.BaseClass0> on type class Generated794.G3_C1266`1<class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated794.IBase1`1<class Generated794.BaseClass0>::Method5()
		ldstr "G3_C1266::Method5.15211()"
		ldstr "class IBase1`1<class Generated794.BaseClass0> on type class Generated794.G3_C1266`1<class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated794.IBase1`1<class Generated794.BaseClass0>::Method6<object>()
		ldstr "G3_C1266::Method6.MI.15213<System.Object>()"
		ldstr "class IBase1`1<class Generated794.BaseClass0> on type class Generated794.G3_C1266`1<class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated794.G3_C1266`1<class Generated794.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass1>
		callvirt instance string class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::ClassMethod1327<object>()
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass1>
		callvirt instance string class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::ClassMethod1326()
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass1>
		callvirt instance string class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated794.IBase2`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated794.BaseClass1,class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated794.IBase2`2<class Generated794.BaseClass0,class Generated794.BaseClass1>::Method7<object>()
		ldstr "G2_C316::Method7.7704<System.Object>()"
		ldstr "class IBase2`2<class Generated794.BaseClass0,class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>
		callvirt instance string class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::ClassMethod1969<object>()
		ldstr "G3_C1266::ClassMethod1969.MI.15218<System.Object>()"
		ldstr "class G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>
		callvirt instance string class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::ClassMethod1968<object>()
		ldstr "G3_C1266::ClassMethod1968.MI.15217<System.Object>()"
		ldstr "class G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>
		callvirt instance string class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::ClassMethod1967()
		ldstr "G3_C1266::ClassMethod1967.MI.15216()"
		ldstr "class G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>
		callvirt instance string class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::ClassMethod1966()
		ldstr "G2_C316::ClassMethod1966.7705()"
		ldstr "class G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>
		callvirt instance string class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::Method7<object>()
		ldstr "G2_C316::Method7.7704<System.Object>()"
		ldstr "class G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>
		callvirt instance string class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::Method3<object>()
		ldstr "G2_C316::Method3.7703<System.Object>()"
		ldstr "class G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>
		callvirt instance string class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::Method2<object>()
		ldstr "G2_C316::Method2.7702<System.Object>()"
		ldstr "class G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>
		callvirt instance string class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::Method1()
		ldstr "G2_C316::Method1.7701()"
		ldstr "class G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>
		callvirt instance string class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::Method0()
		ldstr "G2_C316::Method0.7700()"
		ldstr "class G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>
		callvirt instance string class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::ClassMethod1327<object>()
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>
		callvirt instance string class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::ClassMethod1326()
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string Generated794.IBase0::Method0()
		ldstr "G2_C316::Method0.7700()"
		ldstr "Generated794.IBase0 on type class Generated794.G3_C1266`1<class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated794.IBase0::Method1()
		ldstr "G2_C316::Method1.7701()"
		ldstr "Generated794.IBase0 on type class Generated794.G3_C1266`1<class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated794.IBase0::Method2<object>()
		ldstr "G2_C316::Method2.7702<System.Object>()"
		ldstr "Generated794.IBase0 on type class Generated794.G3_C1266`1<class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated794.IBase0::Method3<object>()
		ldstr "G2_C316::Method3.7703<System.Object>()"
		ldstr "Generated794.IBase0 on type class Generated794.G3_C1266`1<class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated794.IBase2`2<class Generated794.BaseClass0,class Generated794.BaseClass0>::Method7<object>()
		ldstr "G2_C316::Method7.7704<System.Object>()"
		ldstr "class IBase2`2<class Generated794.BaseClass0,class Generated794.BaseClass0> on type class Generated794.G3_C1266`1<class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated794.G3_C1266`1<class Generated794.BaseClass1>
		callvirt instance string class Generated794.G3_C1266`1<class Generated794.BaseClass1>::ClassMethod3905<object>()
		ldstr "G3_C1266::ClassMethod3905.15215<System.Object>()"
		ldstr "class G3_C1266`1<class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated794.G3_C1266`1<class Generated794.BaseClass1>
		callvirt instance string class Generated794.G3_C1266`1<class Generated794.BaseClass1>::ClassMethod3904()
		ldstr "G3_C1266::ClassMethod3904.15214()"
		ldstr "class G3_C1266`1<class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated794.G3_C1266`1<class Generated794.BaseClass1>
		callvirt instance string class Generated794.G3_C1266`1<class Generated794.BaseClass1>::Method6<object>()
		ldstr "G3_C1266::Method6.15212<System.Object>()"
		ldstr "class G3_C1266`1<class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated794.G3_C1266`1<class Generated794.BaseClass1>
		callvirt instance string class Generated794.G3_C1266`1<class Generated794.BaseClass1>::Method5()
		ldstr "G3_C1266::Method5.15211()"
		ldstr "class G3_C1266`1<class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated794.G3_C1266`1<class Generated794.BaseClass1>
		callvirt instance string class Generated794.G3_C1266`1<class Generated794.BaseClass1>::Method4()
		ldstr "G3_C1266::Method4.15209()"
		ldstr "class G3_C1266`1<class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated794.G3_C1266`1<class Generated794.BaseClass1>
		callvirt instance string class Generated794.G3_C1266`1<class Generated794.BaseClass1>::ClassMethod1969<object>()
		ldstr "G3_C1266::ClassMethod1969.MI.15218<System.Object>()"
		ldstr "class G3_C1266`1<class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated794.G3_C1266`1<class Generated794.BaseClass1>
		callvirt instance string class Generated794.G3_C1266`1<class Generated794.BaseClass1>::ClassMethod1968<object>()
		ldstr "G3_C1266::ClassMethod1968.MI.15217<System.Object>()"
		ldstr "class G3_C1266`1<class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated794.G3_C1266`1<class Generated794.BaseClass1>
		callvirt instance string class Generated794.G3_C1266`1<class Generated794.BaseClass1>::ClassMethod1967()
		ldstr "G3_C1266::ClassMethod1967.MI.15216()"
		ldstr "class G3_C1266`1<class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated794.G3_C1266`1<class Generated794.BaseClass1>
		callvirt instance string class Generated794.G3_C1266`1<class Generated794.BaseClass1>::ClassMethod1966()
		ldstr "G2_C316::ClassMethod1966.7705()"
		ldstr "class G3_C1266`1<class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated794.G3_C1266`1<class Generated794.BaseClass1>
		callvirt instance string class Generated794.G3_C1266`1<class Generated794.BaseClass1>::Method7<object>()
		ldstr "G2_C316::Method7.7704<System.Object>()"
		ldstr "class G3_C1266`1<class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated794.G3_C1266`1<class Generated794.BaseClass1>
		callvirt instance string class Generated794.G3_C1266`1<class Generated794.BaseClass1>::Method3<object>()
		ldstr "G2_C316::Method3.7703<System.Object>()"
		ldstr "class G3_C1266`1<class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated794.G3_C1266`1<class Generated794.BaseClass1>
		callvirt instance string class Generated794.G3_C1266`1<class Generated794.BaseClass1>::Method2<object>()
		ldstr "G2_C316::Method2.7702<System.Object>()"
		ldstr "class G3_C1266`1<class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated794.G3_C1266`1<class Generated794.BaseClass1>
		callvirt instance string class Generated794.G3_C1266`1<class Generated794.BaseClass1>::Method1()
		ldstr "G2_C316::Method1.7701()"
		ldstr "class G3_C1266`1<class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated794.G3_C1266`1<class Generated794.BaseClass1>
		callvirt instance string class Generated794.G3_C1266`1<class Generated794.BaseClass1>::Method0()
		ldstr "G2_C316::Method0.7700()"
		ldstr "class G3_C1266`1<class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated794.G3_C1266`1<class Generated794.BaseClass1>
		callvirt instance string class Generated794.G3_C1266`1<class Generated794.BaseClass1>::ClassMethod1327<object>()
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G3_C1266`1<class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated794.G3_C1266`1<class Generated794.BaseClass1>
		callvirt instance string class Generated794.G3_C1266`1<class Generated794.BaseClass1>::ClassMethod1326()
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G3_C1266`1<class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated794.IBase1`1<class Generated794.BaseClass0>::Method4()
		ldstr "G3_C1266::Method4.MI.15210()"
		ldstr "class IBase1`1<class Generated794.BaseClass0> on type class Generated794.G3_C1266`1<class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated794.IBase1`1<class Generated794.BaseClass0>::Method5()
		ldstr "G3_C1266::Method5.15211()"
		ldstr "class IBase1`1<class Generated794.BaseClass0> on type class Generated794.G3_C1266`1<class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated794.IBase1`1<class Generated794.BaseClass0>::Method6<object>()
		ldstr "G3_C1266::Method6.MI.15213<System.Object>()"
		ldstr "class IBase1`1<class Generated794.BaseClass0> on type class Generated794.G3_C1266`1<class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass0>
		callvirt instance string class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass0>::ClassMethod1327<object>()
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass0> on type class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass0>
		callvirt instance string class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass0>::ClassMethod1326()
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass0> on type class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass0>
		callvirt instance string class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass0>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass0> on type class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated794.IBase2`2<class Generated794.BaseClass1,class Generated794.BaseClass0>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated794.BaseClass1,class Generated794.BaseClass0> on type class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated794.IBase2`2<class Generated794.BaseClass0,class Generated794.BaseClass0>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated794.BaseClass0,class Generated794.BaseClass0> on type class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated794.IBase2`2<class Generated794.BaseClass0,class Generated794.BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated794.BaseClass0,class Generated794.BaseClass1> on type class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated794.IBase2`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated794.BaseClass1,class Generated794.BaseClass1> on type class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass1>
		callvirt instance string class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass1>::ClassMethod1327<object>()
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass1> on type class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass1>
		callvirt instance string class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass1>::ClassMethod1326()
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass1> on type class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass1>
		callvirt instance string class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass1> on type class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated794.IBase2`2<class Generated794.BaseClass1,class Generated794.BaseClass0>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated794.BaseClass1,class Generated794.BaseClass0> on type class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated794.IBase2`2<class Generated794.BaseClass0,class Generated794.BaseClass0>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated794.BaseClass0,class Generated794.BaseClass0> on type class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated794.IBase2`2<class Generated794.BaseClass0,class Generated794.BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated794.BaseClass0,class Generated794.BaseClass1> on type class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated794.IBase2`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated794.BaseClass1,class Generated794.BaseClass1> on type class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass0>
		callvirt instance string class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass0>::ClassMethod1327<object>()
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass0> on type class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass0>
		callvirt instance string class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass0>::ClassMethod1326()
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass0> on type class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass0>
		callvirt instance string class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass0>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass0> on type class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated794.IBase2`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated794.BaseClass1,class Generated794.BaseClass1> on type class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated794.IBase2`2<class Generated794.BaseClass0,class Generated794.BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated794.BaseClass0,class Generated794.BaseClass1> on type class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass1>
		callvirt instance string class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::ClassMethod1327<object>()
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass1> on type class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass1>
		callvirt instance string class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::ClassMethod1326()
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass1> on type class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass1>
		callvirt instance string class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass1> on type class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated794.IBase2`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated794.BaseClass1,class Generated794.BaseClass1> on type class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated794.IBase2`2<class Generated794.BaseClass0,class Generated794.BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated794.BaseClass0,class Generated794.BaseClass1> on type class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass1>"
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
		newobj instance void class Generated794.G3_C1266`1<class Generated794.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated794.Generated794::M.G1_C6.T.T<class Generated794.BaseClass1,class Generated794.BaseClass1,class Generated794.G3_C1266`1<class Generated794.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated794.Generated794::M.G1_C6.B.T<class Generated794.BaseClass1,class Generated794.G3_C1266`1<class Generated794.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated794.Generated794::M.G1_C6.B.B<class Generated794.G3_C1266`1<class Generated794.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated794.Generated794::M.IBase2.T.T<class Generated794.BaseClass1,class Generated794.BaseClass1,class Generated794.G3_C1266`1<class Generated794.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated794.Generated794::M.IBase2.B.T<class Generated794.BaseClass1,class Generated794.G3_C1266`1<class Generated794.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated794.Generated794::M.IBase2.B.B<class Generated794.G3_C1266`1<class Generated794.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C316::Method7.7704<System.Object>()#"
		call void Generated794.Generated794::M.IBase2.T.T<class Generated794.BaseClass0,class Generated794.BaseClass1,class Generated794.G3_C1266`1<class Generated794.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C316::Method7.7704<System.Object>()#"
		call void Generated794.Generated794::M.IBase2.A.T<class Generated794.BaseClass1,class Generated794.G3_C1266`1<class Generated794.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C316::Method7.7704<System.Object>()#"
		call void Generated794.Generated794::M.IBase2.A.B<class Generated794.G3_C1266`1<class Generated794.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C316::ClassMethod1966.7705()#G3_C1266::ClassMethod1967.MI.15216()#G3_C1266::ClassMethod1968.MI.15217<System.Object>()#G3_C1266::ClassMethod1969.MI.15218<System.Object>()#G2_C316::Method0.7700()#G2_C316::Method1.7701()#G2_C316::Method2.7702<System.Object>()#G2_C316::Method3.7703<System.Object>()#G2_C316::Method7.7704<System.Object>()#"
		call void Generated794.Generated794::M.G2_C316.T.T<class Generated794.BaseClass1,class Generated794.BaseClass1,class Generated794.G3_C1266`1<class Generated794.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C316::ClassMethod1966.7705()#G3_C1266::ClassMethod1967.MI.15216()#G3_C1266::ClassMethod1968.MI.15217<System.Object>()#G3_C1266::ClassMethod1969.MI.15218<System.Object>()#G2_C316::Method0.7700()#G2_C316::Method1.7701()#G2_C316::Method2.7702<System.Object>()#G2_C316::Method3.7703<System.Object>()#G2_C316::Method7.7704<System.Object>()#"
		call void Generated794.Generated794::M.G2_C316.B.T<class Generated794.BaseClass1,class Generated794.G3_C1266`1<class Generated794.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C316::ClassMethod1966.7705()#G3_C1266::ClassMethod1967.MI.15216()#G3_C1266::ClassMethod1968.MI.15217<System.Object>()#G3_C1266::ClassMethod1969.MI.15218<System.Object>()#G2_C316::Method0.7700()#G2_C316::Method1.7701()#G2_C316::Method2.7702<System.Object>()#G2_C316::Method3.7703<System.Object>()#G2_C316::Method7.7704<System.Object>()#"
		call void Generated794.Generated794::M.G2_C316.B.B<class Generated794.G3_C1266`1<class Generated794.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C316::Method0.7700()#G2_C316::Method1.7701()#G2_C316::Method2.7702<System.Object>()#G2_C316::Method3.7703<System.Object>()#"
		call void Generated794.Generated794::M.Generated794.IBase0<class Generated794.G3_C1266`1<class Generated794.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C316::Method7.7704<System.Object>()#"
		call void Generated794.Generated794::M.IBase2.T.T<class Generated794.BaseClass0,class Generated794.BaseClass0,class Generated794.G3_C1266`1<class Generated794.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C316::Method7.7704<System.Object>()#"
		call void Generated794.Generated794::M.IBase2.A.T<class Generated794.BaseClass0,class Generated794.G3_C1266`1<class Generated794.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C316::Method7.7704<System.Object>()#"
		call void Generated794.Generated794::M.IBase2.A.A<class Generated794.G3_C1266`1<class Generated794.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C316::ClassMethod1966.7705()#G3_C1266::ClassMethod1967.MI.15216()#G3_C1266::ClassMethod1968.MI.15217<System.Object>()#G3_C1266::ClassMethod1969.MI.15218<System.Object>()#G3_C1266::ClassMethod3904.15214()#G3_C1266::ClassMethod3905.15215<System.Object>()#G2_C316::Method0.7700()#G2_C316::Method1.7701()#G2_C316::Method2.7702<System.Object>()#G2_C316::Method3.7703<System.Object>()#G3_C1266::Method4.15209()#G3_C1266::Method5.15211()#G3_C1266::Method6.15212<System.Object>()#G2_C316::Method7.7704<System.Object>()#"
		call void Generated794.Generated794::M.G3_C1266.T<class Generated794.BaseClass0,class Generated794.G3_C1266`1<class Generated794.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C316::ClassMethod1966.7705()#G3_C1266::ClassMethod1967.MI.15216()#G3_C1266::ClassMethod1968.MI.15217<System.Object>()#G3_C1266::ClassMethod1969.MI.15218<System.Object>()#G3_C1266::ClassMethod3904.15214()#G3_C1266::ClassMethod3905.15215<System.Object>()#G2_C316::Method0.7700()#G2_C316::Method1.7701()#G2_C316::Method2.7702<System.Object>()#G2_C316::Method3.7703<System.Object>()#G3_C1266::Method4.15209()#G3_C1266::Method5.15211()#G3_C1266::Method6.15212<System.Object>()#G2_C316::Method7.7704<System.Object>()#"
		call void Generated794.Generated794::M.G3_C1266.A<class Generated794.G3_C1266`1<class Generated794.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1266::Method4.MI.15210()#G3_C1266::Method5.15211()#G3_C1266::Method6.MI.15213<System.Object>()#"
		call void Generated794.Generated794::M.IBase1.T<class Generated794.BaseClass0,class Generated794.G3_C1266`1<class Generated794.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1266::Method4.MI.15210()#G3_C1266::Method5.15211()#G3_C1266::Method6.MI.15213<System.Object>()#"
		call void Generated794.Generated794::M.IBase1.A<class Generated794.G3_C1266`1<class Generated794.BaseClass0>>(!!0,string)
		newobj instance void class Generated794.G3_C1266`1<class Generated794.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated794.Generated794::M.G1_C6.T.T<class Generated794.BaseClass1,class Generated794.BaseClass1,class Generated794.G3_C1266`1<class Generated794.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated794.Generated794::M.G1_C6.B.T<class Generated794.BaseClass1,class Generated794.G3_C1266`1<class Generated794.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated794.Generated794::M.G1_C6.B.B<class Generated794.G3_C1266`1<class Generated794.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated794.Generated794::M.IBase2.T.T<class Generated794.BaseClass1,class Generated794.BaseClass1,class Generated794.G3_C1266`1<class Generated794.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated794.Generated794::M.IBase2.B.T<class Generated794.BaseClass1,class Generated794.G3_C1266`1<class Generated794.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated794.Generated794::M.IBase2.B.B<class Generated794.G3_C1266`1<class Generated794.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C316::Method7.7704<System.Object>()#"
		call void Generated794.Generated794::M.IBase2.T.T<class Generated794.BaseClass0,class Generated794.BaseClass1,class Generated794.G3_C1266`1<class Generated794.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C316::Method7.7704<System.Object>()#"
		call void Generated794.Generated794::M.IBase2.A.T<class Generated794.BaseClass1,class Generated794.G3_C1266`1<class Generated794.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C316::Method7.7704<System.Object>()#"
		call void Generated794.Generated794::M.IBase2.A.B<class Generated794.G3_C1266`1<class Generated794.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C316::ClassMethod1966.7705()#G3_C1266::ClassMethod1967.MI.15216()#G3_C1266::ClassMethod1968.MI.15217<System.Object>()#G3_C1266::ClassMethod1969.MI.15218<System.Object>()#G2_C316::Method0.7700()#G2_C316::Method1.7701()#G2_C316::Method2.7702<System.Object>()#G2_C316::Method3.7703<System.Object>()#G2_C316::Method7.7704<System.Object>()#"
		call void Generated794.Generated794::M.G2_C316.T.T<class Generated794.BaseClass1,class Generated794.BaseClass1,class Generated794.G3_C1266`1<class Generated794.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C316::ClassMethod1966.7705()#G3_C1266::ClassMethod1967.MI.15216()#G3_C1266::ClassMethod1968.MI.15217<System.Object>()#G3_C1266::ClassMethod1969.MI.15218<System.Object>()#G2_C316::Method0.7700()#G2_C316::Method1.7701()#G2_C316::Method2.7702<System.Object>()#G2_C316::Method3.7703<System.Object>()#G2_C316::Method7.7704<System.Object>()#"
		call void Generated794.Generated794::M.G2_C316.B.T<class Generated794.BaseClass1,class Generated794.G3_C1266`1<class Generated794.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C316::ClassMethod1966.7705()#G3_C1266::ClassMethod1967.MI.15216()#G3_C1266::ClassMethod1968.MI.15217<System.Object>()#G3_C1266::ClassMethod1969.MI.15218<System.Object>()#G2_C316::Method0.7700()#G2_C316::Method1.7701()#G2_C316::Method2.7702<System.Object>()#G2_C316::Method3.7703<System.Object>()#G2_C316::Method7.7704<System.Object>()#"
		call void Generated794.Generated794::M.G2_C316.B.B<class Generated794.G3_C1266`1<class Generated794.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C316::Method0.7700()#G2_C316::Method1.7701()#G2_C316::Method2.7702<System.Object>()#G2_C316::Method3.7703<System.Object>()#"
		call void Generated794.Generated794::M.Generated794.IBase0<class Generated794.G3_C1266`1<class Generated794.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C316::Method7.7704<System.Object>()#"
		call void Generated794.Generated794::M.IBase2.T.T<class Generated794.BaseClass0,class Generated794.BaseClass0,class Generated794.G3_C1266`1<class Generated794.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C316::Method7.7704<System.Object>()#"
		call void Generated794.Generated794::M.IBase2.A.T<class Generated794.BaseClass0,class Generated794.G3_C1266`1<class Generated794.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C316::Method7.7704<System.Object>()#"
		call void Generated794.Generated794::M.IBase2.A.A<class Generated794.G3_C1266`1<class Generated794.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C316::ClassMethod1966.7705()#G3_C1266::ClassMethod1967.MI.15216()#G3_C1266::ClassMethod1968.MI.15217<System.Object>()#G3_C1266::ClassMethod1969.MI.15218<System.Object>()#G3_C1266::ClassMethod3904.15214()#G3_C1266::ClassMethod3905.15215<System.Object>()#G2_C316::Method0.7700()#G2_C316::Method1.7701()#G2_C316::Method2.7702<System.Object>()#G2_C316::Method3.7703<System.Object>()#G3_C1266::Method4.15209()#G3_C1266::Method5.15211()#G3_C1266::Method6.15212<System.Object>()#G2_C316::Method7.7704<System.Object>()#"
		call void Generated794.Generated794::M.G3_C1266.T<class Generated794.BaseClass1,class Generated794.G3_C1266`1<class Generated794.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C316::ClassMethod1966.7705()#G3_C1266::ClassMethod1967.MI.15216()#G3_C1266::ClassMethod1968.MI.15217<System.Object>()#G3_C1266::ClassMethod1969.MI.15218<System.Object>()#G3_C1266::ClassMethod3904.15214()#G3_C1266::ClassMethod3905.15215<System.Object>()#G2_C316::Method0.7700()#G2_C316::Method1.7701()#G2_C316::Method2.7702<System.Object>()#G2_C316::Method3.7703<System.Object>()#G3_C1266::Method4.15209()#G3_C1266::Method5.15211()#G3_C1266::Method6.15212<System.Object>()#G2_C316::Method7.7704<System.Object>()#"
		call void Generated794.Generated794::M.G3_C1266.B<class Generated794.G3_C1266`1<class Generated794.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1266::Method4.MI.15210()#G3_C1266::Method5.15211()#G3_C1266::Method6.MI.15213<System.Object>()#"
		call void Generated794.Generated794::M.IBase1.T<class Generated794.BaseClass0,class Generated794.G3_C1266`1<class Generated794.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1266::Method4.MI.15210()#G3_C1266::Method5.15211()#G3_C1266::Method6.MI.15213<System.Object>()#"
		call void Generated794.Generated794::M.IBase1.A<class Generated794.G3_C1266`1<class Generated794.BaseClass1>>(!!0,string)
		newobj instance void class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated794.Generated794::M.G1_C6.T.T<class Generated794.BaseClass0,class Generated794.BaseClass0,class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated794.Generated794::M.G1_C6.A.T<class Generated794.BaseClass0,class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated794.Generated794::M.G1_C6.A.A<class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated794.Generated794::M.IBase2.T.T<class Generated794.BaseClass1,class Generated794.BaseClass0,class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated794.Generated794::M.IBase2.B.T<class Generated794.BaseClass0,class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated794.Generated794::M.IBase2.B.A<class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated794.Generated794::M.IBase2.T.T<class Generated794.BaseClass0,class Generated794.BaseClass0,class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated794.Generated794::M.IBase2.A.T<class Generated794.BaseClass0,class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated794.Generated794::M.IBase2.A.A<class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated794.Generated794::M.IBase2.T.T<class Generated794.BaseClass0,class Generated794.BaseClass1,class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated794.Generated794::M.IBase2.A.T<class Generated794.BaseClass1,class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated794.Generated794::M.IBase2.A.B<class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated794.Generated794::M.IBase2.T.T<class Generated794.BaseClass1,class Generated794.BaseClass1,class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated794.Generated794::M.IBase2.B.T<class Generated794.BaseClass1,class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated794.Generated794::M.IBase2.B.B<class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass0>>(!!0,string)
		newobj instance void class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated794.Generated794::M.G1_C6.T.T<class Generated794.BaseClass0,class Generated794.BaseClass1,class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated794.Generated794::M.G1_C6.A.T<class Generated794.BaseClass1,class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated794.Generated794::M.G1_C6.A.B<class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated794.Generated794::M.IBase2.T.T<class Generated794.BaseClass1,class Generated794.BaseClass0,class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated794.Generated794::M.IBase2.B.T<class Generated794.BaseClass0,class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated794.Generated794::M.IBase2.B.A<class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated794.Generated794::M.IBase2.T.T<class Generated794.BaseClass0,class Generated794.BaseClass0,class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated794.Generated794::M.IBase2.A.T<class Generated794.BaseClass0,class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated794.Generated794::M.IBase2.A.A<class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated794.Generated794::M.IBase2.T.T<class Generated794.BaseClass0,class Generated794.BaseClass1,class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated794.Generated794::M.IBase2.A.T<class Generated794.BaseClass1,class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated794.Generated794::M.IBase2.A.B<class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated794.Generated794::M.IBase2.T.T<class Generated794.BaseClass1,class Generated794.BaseClass1,class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated794.Generated794::M.IBase2.B.T<class Generated794.BaseClass1,class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated794.Generated794::M.IBase2.B.B<class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass1>>(!!0,string)
		newobj instance void class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated794.Generated794::M.G1_C6.T.T<class Generated794.BaseClass1,class Generated794.BaseClass0,class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated794.Generated794::M.G1_C6.B.T<class Generated794.BaseClass0,class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated794.Generated794::M.G1_C6.B.A<class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated794.Generated794::M.IBase2.T.T<class Generated794.BaseClass1,class Generated794.BaseClass1,class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated794.Generated794::M.IBase2.B.T<class Generated794.BaseClass1,class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated794.Generated794::M.IBase2.B.B<class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated794.Generated794::M.IBase2.T.T<class Generated794.BaseClass0,class Generated794.BaseClass1,class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated794.Generated794::M.IBase2.A.T<class Generated794.BaseClass1,class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated794.Generated794::M.IBase2.A.B<class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass0>>(!!0,string)
		newobj instance void class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated794.Generated794::M.G1_C6.T.T<class Generated794.BaseClass1,class Generated794.BaseClass1,class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated794.Generated794::M.G1_C6.B.T<class Generated794.BaseClass1,class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated794.Generated794::M.G1_C6.B.B<class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated794.Generated794::M.IBase2.T.T<class Generated794.BaseClass1,class Generated794.BaseClass1,class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated794.Generated794::M.IBase2.B.T<class Generated794.BaseClass1,class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated794.Generated794::M.IBase2.B.B<class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated794.Generated794::M.IBase2.T.T<class Generated794.BaseClass0,class Generated794.BaseClass1,class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated794.Generated794::M.IBase2.A.T<class Generated794.BaseClass1,class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated794.Generated794::M.IBase2.A.B<class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass1>>(!!0,string)
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
		newobj instance void class Generated794.G3_C1266`1<class Generated794.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::ClassMethod1327<object>()
		calli default string(class Generated794.G3_C1266`1<class Generated794.BaseClass0>)
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::ClassMethod1326()
		calli default string(class Generated794.G3_C1266`1<class Generated794.BaseClass0>)
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::Method7<object>()
		calli default string(class Generated794.G3_C1266`1<class Generated794.BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated794.IBase2`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::Method7<object>()
		calli default string(class Generated794.G3_C1266`1<class Generated794.BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated794.BaseClass1,class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated794.IBase2`2<class Generated794.BaseClass0,class Generated794.BaseClass1>::Method7<object>()
		calli default string(class Generated794.G3_C1266`1<class Generated794.BaseClass0>)
		ldstr "G2_C316::Method7.7704<System.Object>()"
		ldstr "class IBase2`2<class Generated794.BaseClass0,class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::ClassMethod1969<object>()
		calli default string(class Generated794.G3_C1266`1<class Generated794.BaseClass0>)
		ldstr "G3_C1266::ClassMethod1969.MI.15218<System.Object>()"
		ldstr "class G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::ClassMethod1968<object>()
		calli default string(class Generated794.G3_C1266`1<class Generated794.BaseClass0>)
		ldstr "G3_C1266::ClassMethod1968.MI.15217<System.Object>()"
		ldstr "class G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::ClassMethod1967()
		calli default string(class Generated794.G3_C1266`1<class Generated794.BaseClass0>)
		ldstr "G3_C1266::ClassMethod1967.MI.15216()"
		ldstr "class G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::ClassMethod1966()
		calli default string(class Generated794.G3_C1266`1<class Generated794.BaseClass0>)
		ldstr "G2_C316::ClassMethod1966.7705()"
		ldstr "class G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::Method7<object>()
		calli default string(class Generated794.G3_C1266`1<class Generated794.BaseClass0>)
		ldstr "G2_C316::Method7.7704<System.Object>()"
		ldstr "class G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::Method3<object>()
		calli default string(class Generated794.G3_C1266`1<class Generated794.BaseClass0>)
		ldstr "G2_C316::Method3.7703<System.Object>()"
		ldstr "class G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::Method2<object>()
		calli default string(class Generated794.G3_C1266`1<class Generated794.BaseClass0>)
		ldstr "G2_C316::Method2.7702<System.Object>()"
		ldstr "class G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::Method1()
		calli default string(class Generated794.G3_C1266`1<class Generated794.BaseClass0>)
		ldstr "G2_C316::Method1.7701()"
		ldstr "class G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::Method0()
		calli default string(class Generated794.G3_C1266`1<class Generated794.BaseClass0>)
		ldstr "G2_C316::Method0.7700()"
		ldstr "class G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::ClassMethod1327<object>()
		calli default string(class Generated794.G3_C1266`1<class Generated794.BaseClass0>)
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::ClassMethod1326()
		calli default string(class Generated794.G3_C1266`1<class Generated794.BaseClass0>)
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated794.IBase0::Method0()
		calli default string(class Generated794.G3_C1266`1<class Generated794.BaseClass0>)
		ldstr "G2_C316::Method0.7700()"
		ldstr "Generated794.IBase0 on type class Generated794.G3_C1266`1<class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated794.IBase0::Method1()
		calli default string(class Generated794.G3_C1266`1<class Generated794.BaseClass0>)
		ldstr "G2_C316::Method1.7701()"
		ldstr "Generated794.IBase0 on type class Generated794.G3_C1266`1<class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated794.IBase0::Method2<object>()
		calli default string(class Generated794.G3_C1266`1<class Generated794.BaseClass0>)
		ldstr "G2_C316::Method2.7702<System.Object>()"
		ldstr "Generated794.IBase0 on type class Generated794.G3_C1266`1<class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated794.IBase0::Method3<object>()
		calli default string(class Generated794.G3_C1266`1<class Generated794.BaseClass0>)
		ldstr "G2_C316::Method3.7703<System.Object>()"
		ldstr "Generated794.IBase0 on type class Generated794.G3_C1266`1<class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated794.IBase2`2<class Generated794.BaseClass0,class Generated794.BaseClass0>::Method7<object>()
		calli default string(class Generated794.G3_C1266`1<class Generated794.BaseClass0>)
		ldstr "G2_C316::Method7.7704<System.Object>()"
		ldstr "class IBase2`2<class Generated794.BaseClass0,class Generated794.BaseClass0> on type class Generated794.G3_C1266`1<class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated794.G3_C1266`1<class Generated794.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated794.G3_C1266`1<class Generated794.BaseClass0>::ClassMethod3905<object>()
		calli default string(class Generated794.G3_C1266`1<class Generated794.BaseClass0>)
		ldstr "G3_C1266::ClassMethod3905.15215<System.Object>()"
		ldstr "class G3_C1266`1<class Generated794.BaseClass0> on type class Generated794.G3_C1266`1<class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated794.G3_C1266`1<class Generated794.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated794.G3_C1266`1<class Generated794.BaseClass0>::ClassMethod3904()
		calli default string(class Generated794.G3_C1266`1<class Generated794.BaseClass0>)
		ldstr "G3_C1266::ClassMethod3904.15214()"
		ldstr "class G3_C1266`1<class Generated794.BaseClass0> on type class Generated794.G3_C1266`1<class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated794.G3_C1266`1<class Generated794.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated794.G3_C1266`1<class Generated794.BaseClass0>::Method6<object>()
		calli default string(class Generated794.G3_C1266`1<class Generated794.BaseClass0>)
		ldstr "G3_C1266::Method6.15212<System.Object>()"
		ldstr "class G3_C1266`1<class Generated794.BaseClass0> on type class Generated794.G3_C1266`1<class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated794.G3_C1266`1<class Generated794.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated794.G3_C1266`1<class Generated794.BaseClass0>::Method5()
		calli default string(class Generated794.G3_C1266`1<class Generated794.BaseClass0>)
		ldstr "G3_C1266::Method5.15211()"
		ldstr "class G3_C1266`1<class Generated794.BaseClass0> on type class Generated794.G3_C1266`1<class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated794.G3_C1266`1<class Generated794.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated794.G3_C1266`1<class Generated794.BaseClass0>::Method4()
		calli default string(class Generated794.G3_C1266`1<class Generated794.BaseClass0>)
		ldstr "G3_C1266::Method4.15209()"
		ldstr "class G3_C1266`1<class Generated794.BaseClass0> on type class Generated794.G3_C1266`1<class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated794.G3_C1266`1<class Generated794.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated794.G3_C1266`1<class Generated794.BaseClass0>::ClassMethod1969<object>()
		calli default string(class Generated794.G3_C1266`1<class Generated794.BaseClass0>)
		ldstr "G3_C1266::ClassMethod1969.MI.15218<System.Object>()"
		ldstr "class G3_C1266`1<class Generated794.BaseClass0> on type class Generated794.G3_C1266`1<class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated794.G3_C1266`1<class Generated794.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated794.G3_C1266`1<class Generated794.BaseClass0>::ClassMethod1968<object>()
		calli default string(class Generated794.G3_C1266`1<class Generated794.BaseClass0>)
		ldstr "G3_C1266::ClassMethod1968.MI.15217<System.Object>()"
		ldstr "class G3_C1266`1<class Generated794.BaseClass0> on type class Generated794.G3_C1266`1<class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated794.G3_C1266`1<class Generated794.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated794.G3_C1266`1<class Generated794.BaseClass0>::ClassMethod1967()
		calli default string(class Generated794.G3_C1266`1<class Generated794.BaseClass0>)
		ldstr "G3_C1266::ClassMethod1967.MI.15216()"
		ldstr "class G3_C1266`1<class Generated794.BaseClass0> on type class Generated794.G3_C1266`1<class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated794.G3_C1266`1<class Generated794.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated794.G3_C1266`1<class Generated794.BaseClass0>::ClassMethod1966()
		calli default string(class Generated794.G3_C1266`1<class Generated794.BaseClass0>)
		ldstr "G2_C316::ClassMethod1966.7705()"
		ldstr "class G3_C1266`1<class Generated794.BaseClass0> on type class Generated794.G3_C1266`1<class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated794.G3_C1266`1<class Generated794.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated794.G3_C1266`1<class Generated794.BaseClass0>::Method7<object>()
		calli default string(class Generated794.G3_C1266`1<class Generated794.BaseClass0>)
		ldstr "G2_C316::Method7.7704<System.Object>()"
		ldstr "class G3_C1266`1<class Generated794.BaseClass0> on type class Generated794.G3_C1266`1<class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated794.G3_C1266`1<class Generated794.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated794.G3_C1266`1<class Generated794.BaseClass0>::Method3<object>()
		calli default string(class Generated794.G3_C1266`1<class Generated794.BaseClass0>)
		ldstr "G2_C316::Method3.7703<System.Object>()"
		ldstr "class G3_C1266`1<class Generated794.BaseClass0> on type class Generated794.G3_C1266`1<class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated794.G3_C1266`1<class Generated794.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated794.G3_C1266`1<class Generated794.BaseClass0>::Method2<object>()
		calli default string(class Generated794.G3_C1266`1<class Generated794.BaseClass0>)
		ldstr "G2_C316::Method2.7702<System.Object>()"
		ldstr "class G3_C1266`1<class Generated794.BaseClass0> on type class Generated794.G3_C1266`1<class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated794.G3_C1266`1<class Generated794.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated794.G3_C1266`1<class Generated794.BaseClass0>::Method1()
		calli default string(class Generated794.G3_C1266`1<class Generated794.BaseClass0>)
		ldstr "G2_C316::Method1.7701()"
		ldstr "class G3_C1266`1<class Generated794.BaseClass0> on type class Generated794.G3_C1266`1<class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated794.G3_C1266`1<class Generated794.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated794.G3_C1266`1<class Generated794.BaseClass0>::Method0()
		calli default string(class Generated794.G3_C1266`1<class Generated794.BaseClass0>)
		ldstr "G2_C316::Method0.7700()"
		ldstr "class G3_C1266`1<class Generated794.BaseClass0> on type class Generated794.G3_C1266`1<class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated794.G3_C1266`1<class Generated794.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated794.G3_C1266`1<class Generated794.BaseClass0>::ClassMethod1327<object>()
		calli default string(class Generated794.G3_C1266`1<class Generated794.BaseClass0>)
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G3_C1266`1<class Generated794.BaseClass0> on type class Generated794.G3_C1266`1<class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated794.G3_C1266`1<class Generated794.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated794.G3_C1266`1<class Generated794.BaseClass0>::ClassMethod1326()
		calli default string(class Generated794.G3_C1266`1<class Generated794.BaseClass0>)
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G3_C1266`1<class Generated794.BaseClass0> on type class Generated794.G3_C1266`1<class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated794.IBase1`1<class Generated794.BaseClass0>::Method4()
		calli default string(class Generated794.G3_C1266`1<class Generated794.BaseClass0>)
		ldstr "G3_C1266::Method4.MI.15210()"
		ldstr "class IBase1`1<class Generated794.BaseClass0> on type class Generated794.G3_C1266`1<class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated794.IBase1`1<class Generated794.BaseClass0>::Method5()
		calli default string(class Generated794.G3_C1266`1<class Generated794.BaseClass0>)
		ldstr "G3_C1266::Method5.15211()"
		ldstr "class IBase1`1<class Generated794.BaseClass0> on type class Generated794.G3_C1266`1<class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated794.IBase1`1<class Generated794.BaseClass0>::Method6<object>()
		calli default string(class Generated794.G3_C1266`1<class Generated794.BaseClass0>)
		ldstr "G3_C1266::Method6.MI.15213<System.Object>()"
		ldstr "class IBase1`1<class Generated794.BaseClass0> on type class Generated794.G3_C1266`1<class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated794.G3_C1266`1<class Generated794.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::ClassMethod1327<object>()
		calli default string(class Generated794.G3_C1266`1<class Generated794.BaseClass1>)
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::ClassMethod1326()
		calli default string(class Generated794.G3_C1266`1<class Generated794.BaseClass1>)
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::Method7<object>()
		calli default string(class Generated794.G3_C1266`1<class Generated794.BaseClass1>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated794.IBase2`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::Method7<object>()
		calli default string(class Generated794.G3_C1266`1<class Generated794.BaseClass1>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated794.BaseClass1,class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated794.IBase2`2<class Generated794.BaseClass0,class Generated794.BaseClass1>::Method7<object>()
		calli default string(class Generated794.G3_C1266`1<class Generated794.BaseClass1>)
		ldstr "G2_C316::Method7.7704<System.Object>()"
		ldstr "class IBase2`2<class Generated794.BaseClass0,class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::ClassMethod1969<object>()
		calli default string(class Generated794.G3_C1266`1<class Generated794.BaseClass1>)
		ldstr "G3_C1266::ClassMethod1969.MI.15218<System.Object>()"
		ldstr "class G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::ClassMethod1968<object>()
		calli default string(class Generated794.G3_C1266`1<class Generated794.BaseClass1>)
		ldstr "G3_C1266::ClassMethod1968.MI.15217<System.Object>()"
		ldstr "class G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::ClassMethod1967()
		calli default string(class Generated794.G3_C1266`1<class Generated794.BaseClass1>)
		ldstr "G3_C1266::ClassMethod1967.MI.15216()"
		ldstr "class G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::ClassMethod1966()
		calli default string(class Generated794.G3_C1266`1<class Generated794.BaseClass1>)
		ldstr "G2_C316::ClassMethod1966.7705()"
		ldstr "class G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::Method7<object>()
		calli default string(class Generated794.G3_C1266`1<class Generated794.BaseClass1>)
		ldstr "G2_C316::Method7.7704<System.Object>()"
		ldstr "class G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::Method3<object>()
		calli default string(class Generated794.G3_C1266`1<class Generated794.BaseClass1>)
		ldstr "G2_C316::Method3.7703<System.Object>()"
		ldstr "class G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::Method2<object>()
		calli default string(class Generated794.G3_C1266`1<class Generated794.BaseClass1>)
		ldstr "G2_C316::Method2.7702<System.Object>()"
		ldstr "class G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::Method1()
		calli default string(class Generated794.G3_C1266`1<class Generated794.BaseClass1>)
		ldstr "G2_C316::Method1.7701()"
		ldstr "class G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::Method0()
		calli default string(class Generated794.G3_C1266`1<class Generated794.BaseClass1>)
		ldstr "G2_C316::Method0.7700()"
		ldstr "class G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::ClassMethod1327<object>()
		calli default string(class Generated794.G3_C1266`1<class Generated794.BaseClass1>)
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated794.G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::ClassMethod1326()
		calli default string(class Generated794.G3_C1266`1<class Generated794.BaseClass1>)
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G2_C316`2<class Generated794.BaseClass1,class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated794.IBase0::Method0()
		calli default string(class Generated794.G3_C1266`1<class Generated794.BaseClass1>)
		ldstr "G2_C316::Method0.7700()"
		ldstr "Generated794.IBase0 on type class Generated794.G3_C1266`1<class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated794.IBase0::Method1()
		calli default string(class Generated794.G3_C1266`1<class Generated794.BaseClass1>)
		ldstr "G2_C316::Method1.7701()"
		ldstr "Generated794.IBase0 on type class Generated794.G3_C1266`1<class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated794.IBase0::Method2<object>()
		calli default string(class Generated794.G3_C1266`1<class Generated794.BaseClass1>)
		ldstr "G2_C316::Method2.7702<System.Object>()"
		ldstr "Generated794.IBase0 on type class Generated794.G3_C1266`1<class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated794.IBase0::Method3<object>()
		calli default string(class Generated794.G3_C1266`1<class Generated794.BaseClass1>)
		ldstr "G2_C316::Method3.7703<System.Object>()"
		ldstr "Generated794.IBase0 on type class Generated794.G3_C1266`1<class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated794.IBase2`2<class Generated794.BaseClass0,class Generated794.BaseClass0>::Method7<object>()
		calli default string(class Generated794.G3_C1266`1<class Generated794.BaseClass1>)
		ldstr "G2_C316::Method7.7704<System.Object>()"
		ldstr "class IBase2`2<class Generated794.BaseClass0,class Generated794.BaseClass0> on type class Generated794.G3_C1266`1<class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated794.G3_C1266`1<class Generated794.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated794.G3_C1266`1<class Generated794.BaseClass1>::ClassMethod3905<object>()
		calli default string(class Generated794.G3_C1266`1<class Generated794.BaseClass1>)
		ldstr "G3_C1266::ClassMethod3905.15215<System.Object>()"
		ldstr "class G3_C1266`1<class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated794.G3_C1266`1<class Generated794.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated794.G3_C1266`1<class Generated794.BaseClass1>::ClassMethod3904()
		calli default string(class Generated794.G3_C1266`1<class Generated794.BaseClass1>)
		ldstr "G3_C1266::ClassMethod3904.15214()"
		ldstr "class G3_C1266`1<class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated794.G3_C1266`1<class Generated794.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated794.G3_C1266`1<class Generated794.BaseClass1>::Method6<object>()
		calli default string(class Generated794.G3_C1266`1<class Generated794.BaseClass1>)
		ldstr "G3_C1266::Method6.15212<System.Object>()"
		ldstr "class G3_C1266`1<class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated794.G3_C1266`1<class Generated794.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated794.G3_C1266`1<class Generated794.BaseClass1>::Method5()
		calli default string(class Generated794.G3_C1266`1<class Generated794.BaseClass1>)
		ldstr "G3_C1266::Method5.15211()"
		ldstr "class G3_C1266`1<class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated794.G3_C1266`1<class Generated794.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated794.G3_C1266`1<class Generated794.BaseClass1>::Method4()
		calli default string(class Generated794.G3_C1266`1<class Generated794.BaseClass1>)
		ldstr "G3_C1266::Method4.15209()"
		ldstr "class G3_C1266`1<class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated794.G3_C1266`1<class Generated794.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated794.G3_C1266`1<class Generated794.BaseClass1>::ClassMethod1969<object>()
		calli default string(class Generated794.G3_C1266`1<class Generated794.BaseClass1>)
		ldstr "G3_C1266::ClassMethod1969.MI.15218<System.Object>()"
		ldstr "class G3_C1266`1<class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated794.G3_C1266`1<class Generated794.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated794.G3_C1266`1<class Generated794.BaseClass1>::ClassMethod1968<object>()
		calli default string(class Generated794.G3_C1266`1<class Generated794.BaseClass1>)
		ldstr "G3_C1266::ClassMethod1968.MI.15217<System.Object>()"
		ldstr "class G3_C1266`1<class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated794.G3_C1266`1<class Generated794.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated794.G3_C1266`1<class Generated794.BaseClass1>::ClassMethod1967()
		calli default string(class Generated794.G3_C1266`1<class Generated794.BaseClass1>)
		ldstr "G3_C1266::ClassMethod1967.MI.15216()"
		ldstr "class G3_C1266`1<class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated794.G3_C1266`1<class Generated794.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated794.G3_C1266`1<class Generated794.BaseClass1>::ClassMethod1966()
		calli default string(class Generated794.G3_C1266`1<class Generated794.BaseClass1>)
		ldstr "G2_C316::ClassMethod1966.7705()"
		ldstr "class G3_C1266`1<class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated794.G3_C1266`1<class Generated794.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated794.G3_C1266`1<class Generated794.BaseClass1>::Method7<object>()
		calli default string(class Generated794.G3_C1266`1<class Generated794.BaseClass1>)
		ldstr "G2_C316::Method7.7704<System.Object>()"
		ldstr "class G3_C1266`1<class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated794.G3_C1266`1<class Generated794.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated794.G3_C1266`1<class Generated794.BaseClass1>::Method3<object>()
		calli default string(class Generated794.G3_C1266`1<class Generated794.BaseClass1>)
		ldstr "G2_C316::Method3.7703<System.Object>()"
		ldstr "class G3_C1266`1<class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated794.G3_C1266`1<class Generated794.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated794.G3_C1266`1<class Generated794.BaseClass1>::Method2<object>()
		calli default string(class Generated794.G3_C1266`1<class Generated794.BaseClass1>)
		ldstr "G2_C316::Method2.7702<System.Object>()"
		ldstr "class G3_C1266`1<class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated794.G3_C1266`1<class Generated794.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated794.G3_C1266`1<class Generated794.BaseClass1>::Method1()
		calli default string(class Generated794.G3_C1266`1<class Generated794.BaseClass1>)
		ldstr "G2_C316::Method1.7701()"
		ldstr "class G3_C1266`1<class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated794.G3_C1266`1<class Generated794.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated794.G3_C1266`1<class Generated794.BaseClass1>::Method0()
		calli default string(class Generated794.G3_C1266`1<class Generated794.BaseClass1>)
		ldstr "G2_C316::Method0.7700()"
		ldstr "class G3_C1266`1<class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated794.G3_C1266`1<class Generated794.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated794.G3_C1266`1<class Generated794.BaseClass1>::ClassMethod1327<object>()
		calli default string(class Generated794.G3_C1266`1<class Generated794.BaseClass1>)
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G3_C1266`1<class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated794.G3_C1266`1<class Generated794.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated794.G3_C1266`1<class Generated794.BaseClass1>::ClassMethod1326()
		calli default string(class Generated794.G3_C1266`1<class Generated794.BaseClass1>)
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G3_C1266`1<class Generated794.BaseClass1> on type class Generated794.G3_C1266`1<class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated794.IBase1`1<class Generated794.BaseClass0>::Method4()
		calli default string(class Generated794.G3_C1266`1<class Generated794.BaseClass1>)
		ldstr "G3_C1266::Method4.MI.15210()"
		ldstr "class IBase1`1<class Generated794.BaseClass0> on type class Generated794.G3_C1266`1<class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated794.IBase1`1<class Generated794.BaseClass0>::Method5()
		calli default string(class Generated794.G3_C1266`1<class Generated794.BaseClass1>)
		ldstr "G3_C1266::Method5.15211()"
		ldstr "class IBase1`1<class Generated794.BaseClass0> on type class Generated794.G3_C1266`1<class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated794.IBase1`1<class Generated794.BaseClass0>::Method6<object>()
		calli default string(class Generated794.G3_C1266`1<class Generated794.BaseClass1>)
		ldstr "G3_C1266::Method6.MI.15213<System.Object>()"
		ldstr "class IBase1`1<class Generated794.BaseClass0> on type class Generated794.G3_C1266`1<class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass0>::ClassMethod1327<object>()
		calli default string(class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass0>)
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass0> on type class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass0>::ClassMethod1326()
		calli default string(class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass0>)
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass0> on type class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass0>::Method7<object>()
		calli default string(class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass0> on type class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated794.IBase2`2<class Generated794.BaseClass1,class Generated794.BaseClass0>::Method7<object>()
		calli default string(class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated794.BaseClass1,class Generated794.BaseClass0> on type class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated794.IBase2`2<class Generated794.BaseClass0,class Generated794.BaseClass0>::Method7<object>()
		calli default string(class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated794.BaseClass0,class Generated794.BaseClass0> on type class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated794.IBase2`2<class Generated794.BaseClass0,class Generated794.BaseClass1>::Method7<object>()
		calli default string(class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated794.BaseClass0,class Generated794.BaseClass1> on type class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated794.IBase2`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::Method7<object>()
		calli default string(class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated794.BaseClass1,class Generated794.BaseClass1> on type class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass1>::ClassMethod1327<object>()
		calli default string(class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass1>)
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass1> on type class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass1>::ClassMethod1326()
		calli default string(class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass1>)
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass1> on type class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass1>::Method7<object>()
		calli default string(class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass1>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass1> on type class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated794.IBase2`2<class Generated794.BaseClass1,class Generated794.BaseClass0>::Method7<object>()
		calli default string(class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass1>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated794.BaseClass1,class Generated794.BaseClass0> on type class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated794.IBase2`2<class Generated794.BaseClass0,class Generated794.BaseClass0>::Method7<object>()
		calli default string(class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass1>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated794.BaseClass0,class Generated794.BaseClass0> on type class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated794.IBase2`2<class Generated794.BaseClass0,class Generated794.BaseClass1>::Method7<object>()
		calli default string(class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass1>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated794.BaseClass0,class Generated794.BaseClass1> on type class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated794.IBase2`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::Method7<object>()
		calli default string(class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass1>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated794.BaseClass1,class Generated794.BaseClass1> on type class Generated794.G1_C6`2<class Generated794.BaseClass0,class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass0>::ClassMethod1327<object>()
		calli default string(class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass0>)
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass0> on type class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass0>::ClassMethod1326()
		calli default string(class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass0>)
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass0> on type class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass0>::Method7<object>()
		calli default string(class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass0> on type class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated794.IBase2`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::Method7<object>()
		calli default string(class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated794.BaseClass1,class Generated794.BaseClass1> on type class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated794.IBase2`2<class Generated794.BaseClass0,class Generated794.BaseClass1>::Method7<object>()
		calli default string(class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated794.BaseClass0,class Generated794.BaseClass1> on type class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::ClassMethod1327<object>()
		calli default string(class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass1>)
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass1> on type class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::ClassMethod1326()
		calli default string(class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass1>)
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass1> on type class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::Method7<object>()
		calli default string(class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass1>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass1> on type class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated794.IBase2`2<class Generated794.BaseClass1,class Generated794.BaseClass1>::Method7<object>()
		calli default string(class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass1>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated794.BaseClass1,class Generated794.BaseClass1> on type class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated794.IBase2`2<class Generated794.BaseClass0,class Generated794.BaseClass1>::Method7<object>()
		calli default string(class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass1>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated794.BaseClass0,class Generated794.BaseClass1> on type class Generated794.G1_C6`2<class Generated794.BaseClass1,class Generated794.BaseClass1>"
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
		call void Generated794.Generated794::MethodCallingTest()
		call void Generated794.Generated794::ConstrainedCallsTest()
		call void Generated794.Generated794::StructConstrainedInterfaceCallsTest()
		call void Generated794.Generated794::CalliTest()
		ldc.i4 100
		ret
	}
}
