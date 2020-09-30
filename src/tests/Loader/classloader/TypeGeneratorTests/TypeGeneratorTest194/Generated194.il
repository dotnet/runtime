// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) .ver 4:0:0:0 }
.assembly extern TestFramework { .publickeytoken = ( B0 3F 5F 7F 11 D5 0A 3A )  }

//TYPES IN FORWARDER ASSEMBLIES:

//TEST ASSEMBLY:
.assembly Generated194 { .hash algorithm 0x00008004 }
.module Generated194.exe

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
.class public sequential sealed MyStruct244`2<T0, T1> 
		extends [mscorlib]System.ValueType
		implements IBase0, class IBase1`1<!T0> 
{
	.pack 0
	.size 1
	.method public hidebysig newslot virtual instance string Method0() cil managed noinlining { 
		ldstr "MyStruct244::Method0.1810()"
		ret
	}
	.method public hidebysig virtual instance string Method1() cil managed noinlining { 
		ldstr "MyStruct244::Method1.1811()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase0.Method1'() cil managed noinlining { 
		.override method instance string IBase0::Method1()
		ldstr "MyStruct244::Method1.MI.1812()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method2<M0>() cil managed noinlining { 
		ldstr "MyStruct244::Method2.1813<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig virtual instance string Method3<M0>() cil managed noinlining { 
		ldstr "MyStruct244::Method3.1814<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase0.Method3'<M0>() cil managed noinlining { 
		.override method instance string IBase0::Method3<[1]>()
		ldstr "MyStruct244::Method3.MI.1815<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string Method4() cil managed noinlining { 
		ldstr "MyStruct244::Method4.1816()"
		ret
	}
	.method public hidebysig virtual instance string Method5() cil managed noinlining { 
		ldstr "MyStruct244::Method5.1817()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<T0>.Method5'() cil managed noinlining { 
		.override method instance string class IBase1`1<!T0>::Method5()
		ldstr "MyStruct244::Method5.MI.1818()"
		ret
	}
	.method public hidebysig virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "MyStruct244::Method6.1819<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<T0>.Method6'<M0>() cil managed noinlining { 
		.override method instance string class IBase1`1<!T0>::Method6<[1]>()
		ldstr "MyStruct244::Method6.MI.1820<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot instance string ClassMethod492() cil managed noinlining { 
		ldstr "MyStruct244::ClassMethod492.1821()"
		ret
	}
	.method public hidebysig newslot instance string ClassMethod493<M0>() cil managed noinlining { 
		ldstr "MyStruct244::ClassMethod493.1822<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot instance string ClassMethod494<M0>() cil managed noinlining { 
		ldstr "MyStruct244::ClassMethod494.1823<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig virtual instance bool Equals(object obj) cil managed { ldc.i4.0 ret }
	.method public hidebysig virtual instance int32 GetHashCode() cil managed { ldc.i4.0 ret }
	.method public hidebysig virtual instance string ToString() cil managed { ldstr "" ret }
}
.class interface public abstract IBase0 
{
	.method public hidebysig newslot abstract virtual instance string Method0() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method1() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method2<M0>() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method3<M0>() cil managed { }
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
	.method static void M.MyStruct244.T.T<T0,T1,(valuetype MyStruct244`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.MyStruct244.T.T<T0,T1,(valuetype MyStruct244`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. valuetype MyStruct244`2<!!T0,!!T1>
		callvirt     instance string IBase0::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. valuetype MyStruct244`2<!!T0,!!T1>
		callvirt     instance string IBase0::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. valuetype MyStruct244`2<!!T0,!!T1>
		callvirt     instance string IBase0::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. valuetype MyStruct244`2<!!T0,!!T1>
		callvirt     instance string IBase0::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. valuetype MyStruct244`2<!!T0,!!T1>
		callvirt     instance string class IBase1`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. valuetype MyStruct244`2<!!T0,!!T1>
		callvirt     instance string class IBase1`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. valuetype MyStruct244`2<!!T0,!!T1>
		callvirt     instance string class IBase1`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.MyStruct244.A.T<T1,(valuetype MyStruct244`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.MyStruct244.A.T<T1,(valuetype MyStruct244`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. valuetype MyStruct244`2<class BaseClass0,!!T1>
		callvirt     instance string IBase0::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. valuetype MyStruct244`2<class BaseClass0,!!T1>
		callvirt     instance string IBase0::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. valuetype MyStruct244`2<class BaseClass0,!!T1>
		callvirt     instance string IBase0::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. valuetype MyStruct244`2<class BaseClass0,!!T1>
		callvirt     instance string IBase0::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. valuetype MyStruct244`2<class BaseClass0,!!T1>
		callvirt     instance string class IBase1`1<class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. valuetype MyStruct244`2<class BaseClass0,!!T1>
		callvirt     instance string class IBase1`1<class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. valuetype MyStruct244`2<class BaseClass0,!!T1>
		callvirt     instance string class IBase1`1<class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.MyStruct244.A.A<(valuetype MyStruct244`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.MyStruct244.A.A<(valuetype MyStruct244`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. valuetype MyStruct244`2<class BaseClass0,class BaseClass0>
		callvirt     instance string IBase0::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. valuetype MyStruct244`2<class BaseClass0,class BaseClass0>
		callvirt     instance string IBase0::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. valuetype MyStruct244`2<class BaseClass0,class BaseClass0>
		callvirt     instance string IBase0::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. valuetype MyStruct244`2<class BaseClass0,class BaseClass0>
		callvirt     instance string IBase0::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. valuetype MyStruct244`2<class BaseClass0,class BaseClass0>
		callvirt     instance string class IBase1`1<class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. valuetype MyStruct244`2<class BaseClass0,class BaseClass0>
		callvirt     instance string class IBase1`1<class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. valuetype MyStruct244`2<class BaseClass0,class BaseClass0>
		callvirt     instance string class IBase1`1<class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.MyStruct244.A.B<(valuetype MyStruct244`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.MyStruct244.A.B<(valuetype MyStruct244`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. valuetype MyStruct244`2<class BaseClass0,class BaseClass1>
		callvirt     instance string IBase0::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. valuetype MyStruct244`2<class BaseClass0,class BaseClass1>
		callvirt     instance string IBase0::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. valuetype MyStruct244`2<class BaseClass0,class BaseClass1>
		callvirt     instance string IBase0::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. valuetype MyStruct244`2<class BaseClass0,class BaseClass1>
		callvirt     instance string IBase0::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. valuetype MyStruct244`2<class BaseClass0,class BaseClass1>
		callvirt     instance string class IBase1`1<class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. valuetype MyStruct244`2<class BaseClass0,class BaseClass1>
		callvirt     instance string class IBase1`1<class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. valuetype MyStruct244`2<class BaseClass0,class BaseClass1>
		callvirt     instance string class IBase1`1<class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.MyStruct244.B.T<T1,(valuetype MyStruct244`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.MyStruct244.B.T<T1,(valuetype MyStruct244`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. valuetype MyStruct244`2<class BaseClass1,!!T1>
		callvirt     instance string IBase0::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. valuetype MyStruct244`2<class BaseClass1,!!T1>
		callvirt     instance string IBase0::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. valuetype MyStruct244`2<class BaseClass1,!!T1>
		callvirt     instance string IBase0::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. valuetype MyStruct244`2<class BaseClass1,!!T1>
		callvirt     instance string IBase0::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. valuetype MyStruct244`2<class BaseClass1,!!T1>
		callvirt     instance string class IBase1`1<class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. valuetype MyStruct244`2<class BaseClass1,!!T1>
		callvirt     instance string class IBase1`1<class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. valuetype MyStruct244`2<class BaseClass1,!!T1>
		callvirt     instance string class IBase1`1<class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.MyStruct244.B.A<(valuetype MyStruct244`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.MyStruct244.B.A<(valuetype MyStruct244`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. valuetype MyStruct244`2<class BaseClass1,class BaseClass0>
		callvirt     instance string IBase0::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. valuetype MyStruct244`2<class BaseClass1,class BaseClass0>
		callvirt     instance string IBase0::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. valuetype MyStruct244`2<class BaseClass1,class BaseClass0>
		callvirt     instance string IBase0::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. valuetype MyStruct244`2<class BaseClass1,class BaseClass0>
		callvirt     instance string IBase0::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. valuetype MyStruct244`2<class BaseClass1,class BaseClass0>
		callvirt     instance string class IBase1`1<class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. valuetype MyStruct244`2<class BaseClass1,class BaseClass0>
		callvirt     instance string class IBase1`1<class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. valuetype MyStruct244`2<class BaseClass1,class BaseClass0>
		callvirt     instance string class IBase1`1<class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.MyStruct244.B.B<(valuetype MyStruct244`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.MyStruct244.B.B<(valuetype MyStruct244`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. valuetype MyStruct244`2<class BaseClass1,class BaseClass1>
		callvirt     instance string IBase0::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. valuetype MyStruct244`2<class BaseClass1,class BaseClass1>
		callvirt     instance string IBase0::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. valuetype MyStruct244`2<class BaseClass1,class BaseClass1>
		callvirt     instance string IBase0::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. valuetype MyStruct244`2<class BaseClass1,class BaseClass1>
		callvirt     instance string IBase0::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. valuetype MyStruct244`2<class BaseClass1,class BaseClass1>
		callvirt     instance string class IBase1`1<class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. valuetype MyStruct244`2<class BaseClass1,class BaseClass1>
		callvirt     instance string class IBase1`1<class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. valuetype MyStruct244`2<class BaseClass1,class BaseClass1>
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
		.locals init (valuetype MyStruct244`2<class BaseClass0,class BaseClass0> V_1)
		ldloca V_1
		initobj valuetype MyStruct244`2<class BaseClass0,class BaseClass0>
		ldloca V_1
		dup
		call instance string valuetype MyStruct244`2<class BaseClass0,class BaseClass0>::Method0()
		ldstr "MyStruct244::Method0.1810()"
		ldstr "valuetype MyStruct244`2<class BaseClass0,class BaseClass0> on type MyStruct244"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype MyStruct244`2<class BaseClass0,class BaseClass0>::Method1()
		ldstr "MyStruct244::Method1.1811()"
		ldstr "valuetype MyStruct244`2<class BaseClass0,class BaseClass0> on type MyStruct244"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype MyStruct244`2<class BaseClass0,class BaseClass0>::Method2<object>()
		ldstr "MyStruct244::Method2.1813<System.Object>()"
		ldstr "valuetype MyStruct244`2<class BaseClass0,class BaseClass0> on type MyStruct244"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype MyStruct244`2<class BaseClass0,class BaseClass0>::Method3<object>()
		ldstr "MyStruct244::Method3.1814<System.Object>()"
		ldstr "valuetype MyStruct244`2<class BaseClass0,class BaseClass0> on type MyStruct244"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype MyStruct244`2<class BaseClass0,class BaseClass0>::Method4()
		ldstr "MyStruct244::Method4.1816()"
		ldstr "valuetype MyStruct244`2<class BaseClass0,class BaseClass0> on type MyStruct244"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype MyStruct244`2<class BaseClass0,class BaseClass0>::Method5()
		ldstr "MyStruct244::Method5.1817()"
		ldstr "valuetype MyStruct244`2<class BaseClass0,class BaseClass0> on type MyStruct244"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype MyStruct244`2<class BaseClass0,class BaseClass0>::Method6<object>()
		ldstr "MyStruct244::Method6.1819<System.Object>()"
		ldstr "valuetype MyStruct244`2<class BaseClass0,class BaseClass0> on type MyStruct244"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype MyStruct244`2<class BaseClass0,class BaseClass0>::ClassMethod492()
		ldstr "MyStruct244::ClassMethod492.1821()"
		ldstr "valuetype MyStruct244`2<class BaseClass0,class BaseClass0> on type MyStruct244"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype MyStruct244`2<class BaseClass0,class BaseClass0>::ClassMethod493<object>()
		ldstr "MyStruct244::ClassMethod493.1822<System.Object>()"
		ldstr "valuetype MyStruct244`2<class BaseClass0,class BaseClass0> on type MyStruct244"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype MyStruct244`2<class BaseClass0,class BaseClass0>::ClassMethod494<object>()
		ldstr "MyStruct244::ClassMethod494.1823<System.Object>()"
		ldstr "valuetype MyStruct244`2<class BaseClass0,class BaseClass0> on type MyStruct244"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup ldnull call instance bool valuetype MyStruct244`2<class BaseClass0,class BaseClass0>::Equals(object) pop
		dup call instance int32 valuetype MyStruct244`2<class BaseClass0,class BaseClass0>::GetHashCode() pop
		dup call instance string valuetype MyStruct244`2<class BaseClass0,class BaseClass0>::ToString() pop
		pop
		ldloc V_1
		box valuetype MyStruct244`2<class BaseClass0,class BaseClass0>
		dup
		callvirt instance string IBase0::Method0()
		ldstr "MyStruct244::Method0.1810()"
		ldstr "IBase0 on type valuetype MyStruct244`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method1()
		ldstr "MyStruct244::Method1.MI.1812()"
		ldstr "IBase0 on type valuetype MyStruct244`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method2<object>()
		ldstr "MyStruct244::Method2.1813<System.Object>()"
		ldstr "IBase0 on type valuetype MyStruct244`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method3<object>()
		ldstr "MyStruct244::Method3.MI.1815<System.Object>()"
		ldstr "IBase0 on type valuetype MyStruct244`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc V_1
		box valuetype MyStruct244`2<class BaseClass0,class BaseClass0>
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "MyStruct244::Method4.1816()"
		ldstr "class IBase1`1<class BaseClass0> on type valuetype MyStruct244`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "MyStruct244::Method5.MI.1818()"
		ldstr "class IBase1`1<class BaseClass0> on type valuetype MyStruct244`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "MyStruct244::Method6.MI.1820<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type valuetype MyStruct244`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		.locals init (valuetype MyStruct244`2<class BaseClass0,class BaseClass1> V_2)
		ldloca V_2
		initobj valuetype MyStruct244`2<class BaseClass0,class BaseClass1>
		ldloca V_2
		dup
		call instance string valuetype MyStruct244`2<class BaseClass0,class BaseClass1>::Method0()
		ldstr "MyStruct244::Method0.1810()"
		ldstr "valuetype MyStruct244`2<class BaseClass0,class BaseClass1> on type MyStruct244"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype MyStruct244`2<class BaseClass0,class BaseClass1>::Method1()
		ldstr "MyStruct244::Method1.1811()"
		ldstr "valuetype MyStruct244`2<class BaseClass0,class BaseClass1> on type MyStruct244"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype MyStruct244`2<class BaseClass0,class BaseClass1>::Method2<object>()
		ldstr "MyStruct244::Method2.1813<System.Object>()"
		ldstr "valuetype MyStruct244`2<class BaseClass0,class BaseClass1> on type MyStruct244"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype MyStruct244`2<class BaseClass0,class BaseClass1>::Method3<object>()
		ldstr "MyStruct244::Method3.1814<System.Object>()"
		ldstr "valuetype MyStruct244`2<class BaseClass0,class BaseClass1> on type MyStruct244"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype MyStruct244`2<class BaseClass0,class BaseClass1>::Method4()
		ldstr "MyStruct244::Method4.1816()"
		ldstr "valuetype MyStruct244`2<class BaseClass0,class BaseClass1> on type MyStruct244"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype MyStruct244`2<class BaseClass0,class BaseClass1>::Method5()
		ldstr "MyStruct244::Method5.1817()"
		ldstr "valuetype MyStruct244`2<class BaseClass0,class BaseClass1> on type MyStruct244"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype MyStruct244`2<class BaseClass0,class BaseClass1>::Method6<object>()
		ldstr "MyStruct244::Method6.1819<System.Object>()"
		ldstr "valuetype MyStruct244`2<class BaseClass0,class BaseClass1> on type MyStruct244"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype MyStruct244`2<class BaseClass0,class BaseClass1>::ClassMethod492()
		ldstr "MyStruct244::ClassMethod492.1821()"
		ldstr "valuetype MyStruct244`2<class BaseClass0,class BaseClass1> on type MyStruct244"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype MyStruct244`2<class BaseClass0,class BaseClass1>::ClassMethod493<object>()
		ldstr "MyStruct244::ClassMethod493.1822<System.Object>()"
		ldstr "valuetype MyStruct244`2<class BaseClass0,class BaseClass1> on type MyStruct244"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype MyStruct244`2<class BaseClass0,class BaseClass1>::ClassMethod494<object>()
		ldstr "MyStruct244::ClassMethod494.1823<System.Object>()"
		ldstr "valuetype MyStruct244`2<class BaseClass0,class BaseClass1> on type MyStruct244"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup ldnull call instance bool valuetype MyStruct244`2<class BaseClass0,class BaseClass1>::Equals(object) pop
		dup call instance int32 valuetype MyStruct244`2<class BaseClass0,class BaseClass1>::GetHashCode() pop
		dup call instance string valuetype MyStruct244`2<class BaseClass0,class BaseClass1>::ToString() pop
		pop
		ldloc V_2
		box valuetype MyStruct244`2<class BaseClass0,class BaseClass1>
		dup
		callvirt instance string IBase0::Method0()
		ldstr "MyStruct244::Method0.1810()"
		ldstr "IBase0 on type valuetype MyStruct244`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method1()
		ldstr "MyStruct244::Method1.MI.1812()"
		ldstr "IBase0 on type valuetype MyStruct244`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method2<object>()
		ldstr "MyStruct244::Method2.1813<System.Object>()"
		ldstr "IBase0 on type valuetype MyStruct244`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method3<object>()
		ldstr "MyStruct244::Method3.MI.1815<System.Object>()"
		ldstr "IBase0 on type valuetype MyStruct244`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc V_2
		box valuetype MyStruct244`2<class BaseClass0,class BaseClass1>
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "MyStruct244::Method4.1816()"
		ldstr "class IBase1`1<class BaseClass0> on type valuetype MyStruct244`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "MyStruct244::Method5.MI.1818()"
		ldstr "class IBase1`1<class BaseClass0> on type valuetype MyStruct244`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "MyStruct244::Method6.MI.1820<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type valuetype MyStruct244`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		.locals init (valuetype MyStruct244`2<class BaseClass1,class BaseClass0> V_3)
		ldloca V_3
		initobj valuetype MyStruct244`2<class BaseClass1,class BaseClass0>
		ldloca V_3
		dup
		call instance string valuetype MyStruct244`2<class BaseClass1,class BaseClass0>::Method0()
		ldstr "MyStruct244::Method0.1810()"
		ldstr "valuetype MyStruct244`2<class BaseClass1,class BaseClass0> on type MyStruct244"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype MyStruct244`2<class BaseClass1,class BaseClass0>::Method1()
		ldstr "MyStruct244::Method1.1811()"
		ldstr "valuetype MyStruct244`2<class BaseClass1,class BaseClass0> on type MyStruct244"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype MyStruct244`2<class BaseClass1,class BaseClass0>::Method2<object>()
		ldstr "MyStruct244::Method2.1813<System.Object>()"
		ldstr "valuetype MyStruct244`2<class BaseClass1,class BaseClass0> on type MyStruct244"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype MyStruct244`2<class BaseClass1,class BaseClass0>::Method3<object>()
		ldstr "MyStruct244::Method3.1814<System.Object>()"
		ldstr "valuetype MyStruct244`2<class BaseClass1,class BaseClass0> on type MyStruct244"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype MyStruct244`2<class BaseClass1,class BaseClass0>::Method4()
		ldstr "MyStruct244::Method4.1816()"
		ldstr "valuetype MyStruct244`2<class BaseClass1,class BaseClass0> on type MyStruct244"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype MyStruct244`2<class BaseClass1,class BaseClass0>::Method5()
		ldstr "MyStruct244::Method5.1817()"
		ldstr "valuetype MyStruct244`2<class BaseClass1,class BaseClass0> on type MyStruct244"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype MyStruct244`2<class BaseClass1,class BaseClass0>::Method6<object>()
		ldstr "MyStruct244::Method6.1819<System.Object>()"
		ldstr "valuetype MyStruct244`2<class BaseClass1,class BaseClass0> on type MyStruct244"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype MyStruct244`2<class BaseClass1,class BaseClass0>::ClassMethod492()
		ldstr "MyStruct244::ClassMethod492.1821()"
		ldstr "valuetype MyStruct244`2<class BaseClass1,class BaseClass0> on type MyStruct244"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype MyStruct244`2<class BaseClass1,class BaseClass0>::ClassMethod493<object>()
		ldstr "MyStruct244::ClassMethod493.1822<System.Object>()"
		ldstr "valuetype MyStruct244`2<class BaseClass1,class BaseClass0> on type MyStruct244"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype MyStruct244`2<class BaseClass1,class BaseClass0>::ClassMethod494<object>()
		ldstr "MyStruct244::ClassMethod494.1823<System.Object>()"
		ldstr "valuetype MyStruct244`2<class BaseClass1,class BaseClass0> on type MyStruct244"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup ldnull call instance bool valuetype MyStruct244`2<class BaseClass1,class BaseClass0>::Equals(object) pop
		dup call instance int32 valuetype MyStruct244`2<class BaseClass1,class BaseClass0>::GetHashCode() pop
		dup call instance string valuetype MyStruct244`2<class BaseClass1,class BaseClass0>::ToString() pop
		pop
		ldloc V_3
		box valuetype MyStruct244`2<class BaseClass1,class BaseClass0>
		dup
		callvirt instance string IBase0::Method0()
		ldstr "MyStruct244::Method0.1810()"
		ldstr "IBase0 on type valuetype MyStruct244`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method1()
		ldstr "MyStruct244::Method1.MI.1812()"
		ldstr "IBase0 on type valuetype MyStruct244`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method2<object>()
		ldstr "MyStruct244::Method2.1813<System.Object>()"
		ldstr "IBase0 on type valuetype MyStruct244`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method3<object>()
		ldstr "MyStruct244::Method3.MI.1815<System.Object>()"
		ldstr "IBase0 on type valuetype MyStruct244`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc V_3
		box valuetype MyStruct244`2<class BaseClass1,class BaseClass0>
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "MyStruct244::Method4.1816()"
		ldstr "class IBase1`1<class BaseClass1> on type valuetype MyStruct244`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "MyStruct244::Method5.MI.1818()"
		ldstr "class IBase1`1<class BaseClass1> on type valuetype MyStruct244`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "MyStruct244::Method6.MI.1820<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type valuetype MyStruct244`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc V_3
		box valuetype MyStruct244`2<class BaseClass1,class BaseClass0>
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "MyStruct244::Method4.1816()"
		ldstr "class IBase1`1<class BaseClass0> on type valuetype MyStruct244`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "MyStruct244::Method5.MI.1818()"
		ldstr "class IBase1`1<class BaseClass0> on type valuetype MyStruct244`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "MyStruct244::Method6.MI.1820<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type valuetype MyStruct244`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		.locals init (valuetype MyStruct244`2<class BaseClass1,class BaseClass1> V_4)
		ldloca V_4
		initobj valuetype MyStruct244`2<class BaseClass1,class BaseClass1>
		ldloca V_4
		dup
		call instance string valuetype MyStruct244`2<class BaseClass1,class BaseClass1>::Method0()
		ldstr "MyStruct244::Method0.1810()"
		ldstr "valuetype MyStruct244`2<class BaseClass1,class BaseClass1> on type MyStruct244"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype MyStruct244`2<class BaseClass1,class BaseClass1>::Method1()
		ldstr "MyStruct244::Method1.1811()"
		ldstr "valuetype MyStruct244`2<class BaseClass1,class BaseClass1> on type MyStruct244"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype MyStruct244`2<class BaseClass1,class BaseClass1>::Method2<object>()
		ldstr "MyStruct244::Method2.1813<System.Object>()"
		ldstr "valuetype MyStruct244`2<class BaseClass1,class BaseClass1> on type MyStruct244"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype MyStruct244`2<class BaseClass1,class BaseClass1>::Method3<object>()
		ldstr "MyStruct244::Method3.1814<System.Object>()"
		ldstr "valuetype MyStruct244`2<class BaseClass1,class BaseClass1> on type MyStruct244"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype MyStruct244`2<class BaseClass1,class BaseClass1>::Method4()
		ldstr "MyStruct244::Method4.1816()"
		ldstr "valuetype MyStruct244`2<class BaseClass1,class BaseClass1> on type MyStruct244"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype MyStruct244`2<class BaseClass1,class BaseClass1>::Method5()
		ldstr "MyStruct244::Method5.1817()"
		ldstr "valuetype MyStruct244`2<class BaseClass1,class BaseClass1> on type MyStruct244"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype MyStruct244`2<class BaseClass1,class BaseClass1>::Method6<object>()
		ldstr "MyStruct244::Method6.1819<System.Object>()"
		ldstr "valuetype MyStruct244`2<class BaseClass1,class BaseClass1> on type MyStruct244"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype MyStruct244`2<class BaseClass1,class BaseClass1>::ClassMethod492()
		ldstr "MyStruct244::ClassMethod492.1821()"
		ldstr "valuetype MyStruct244`2<class BaseClass1,class BaseClass1> on type MyStruct244"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype MyStruct244`2<class BaseClass1,class BaseClass1>::ClassMethod493<object>()
		ldstr "MyStruct244::ClassMethod493.1822<System.Object>()"
		ldstr "valuetype MyStruct244`2<class BaseClass1,class BaseClass1> on type MyStruct244"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype MyStruct244`2<class BaseClass1,class BaseClass1>::ClassMethod494<object>()
		ldstr "MyStruct244::ClassMethod494.1823<System.Object>()"
		ldstr "valuetype MyStruct244`2<class BaseClass1,class BaseClass1> on type MyStruct244"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup ldnull call instance bool valuetype MyStruct244`2<class BaseClass1,class BaseClass1>::Equals(object) pop
		dup call instance int32 valuetype MyStruct244`2<class BaseClass1,class BaseClass1>::GetHashCode() pop
		dup call instance string valuetype MyStruct244`2<class BaseClass1,class BaseClass1>::ToString() pop
		pop
		ldloc V_4
		box valuetype MyStruct244`2<class BaseClass1,class BaseClass1>
		dup
		callvirt instance string IBase0::Method0()
		ldstr "MyStruct244::Method0.1810()"
		ldstr "IBase0 on type valuetype MyStruct244`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method1()
		ldstr "MyStruct244::Method1.MI.1812()"
		ldstr "IBase0 on type valuetype MyStruct244`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method2<object>()
		ldstr "MyStruct244::Method2.1813<System.Object>()"
		ldstr "IBase0 on type valuetype MyStruct244`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method3<object>()
		ldstr "MyStruct244::Method3.MI.1815<System.Object>()"
		ldstr "IBase0 on type valuetype MyStruct244`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc V_4
		box valuetype MyStruct244`2<class BaseClass1,class BaseClass1>
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "MyStruct244::Method4.1816()"
		ldstr "class IBase1`1<class BaseClass1> on type valuetype MyStruct244`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "MyStruct244::Method5.MI.1818()"
		ldstr "class IBase1`1<class BaseClass1> on type valuetype MyStruct244`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "MyStruct244::Method6.MI.1820<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type valuetype MyStruct244`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc V_4
		box valuetype MyStruct244`2<class BaseClass1,class BaseClass1>
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "MyStruct244::Method4.1816()"
		ldstr "class IBase1`1<class BaseClass0> on type valuetype MyStruct244`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "MyStruct244::Method5.MI.1818()"
		ldstr "class IBase1`1<class BaseClass0> on type valuetype MyStruct244`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "MyStruct244::Method6.MI.1820<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type valuetype MyStruct244`2<class BaseClass1,class BaseClass1>"
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
		.locals init (valuetype MyStruct244`2<class BaseClass0,class BaseClass0> V_5)
		ldloca V_5
		initobj valuetype MyStruct244`2<class BaseClass0,class BaseClass0>
		.try {  ldloc V_5
				ldstr "MyStruct244::Method0.1810()#MyStruct244::Method1.MI.1812()#MyStruct244::Method2.1813<System.Object>()#MyStruct244::Method3.MI.1815<System.Object>()#"
				call void Framework::M.IBase0<valuetype MyStruct244`2<class BaseClass0,class BaseClass0>>(!!0,string) leave.s LV0
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV0} LV0:
		.try {  ldloc V_5
				ldstr "MyStruct244::Method4.1816()#MyStruct244::Method5.MI.1818()#MyStruct244::Method6.MI.1820<System.Object>()#"
				call void Framework::M.IBase1.T<class BaseClass0,valuetype MyStruct244`2<class BaseClass0,class BaseClass0>>(!!1,string) leave.s LV1
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV1} LV1:
		.try {  ldloc V_5
				ldstr "MyStruct244::Method4.1816()#MyStruct244::Method5.MI.1818()#MyStruct244::Method6.MI.1820<System.Object>()#"
				call void Framework::M.IBase1.A<valuetype MyStruct244`2<class BaseClass0,class BaseClass0>>(!!0,string) leave.s LV2
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV2} LV2:
		.locals init (valuetype MyStruct244`2<class BaseClass0,class BaseClass1> V_6)
		ldloca V_6
		initobj valuetype MyStruct244`2<class BaseClass0,class BaseClass1>
		.try {  ldloc V_6
				ldstr "MyStruct244::Method0.1810()#MyStruct244::Method1.MI.1812()#MyStruct244::Method2.1813<System.Object>()#MyStruct244::Method3.MI.1815<System.Object>()#"
				call void Framework::M.IBase0<valuetype MyStruct244`2<class BaseClass0,class BaseClass1>>(!!0,string) leave.s LV3
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV3} LV3:
		.try {  ldloc V_6
				ldstr "MyStruct244::Method4.1816()#MyStruct244::Method5.MI.1818()#MyStruct244::Method6.MI.1820<System.Object>()#"
				call void Framework::M.IBase1.T<class BaseClass0,valuetype MyStruct244`2<class BaseClass0,class BaseClass1>>(!!1,string) leave.s LV4
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV4} LV4:
		.try {  ldloc V_6
				ldstr "MyStruct244::Method4.1816()#MyStruct244::Method5.MI.1818()#MyStruct244::Method6.MI.1820<System.Object>()#"
				call void Framework::M.IBase1.A<valuetype MyStruct244`2<class BaseClass0,class BaseClass1>>(!!0,string) leave.s LV5
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV5} LV5:
		.locals init (valuetype MyStruct244`2<class BaseClass1,class BaseClass0> V_7)
		ldloca V_7
		initobj valuetype MyStruct244`2<class BaseClass1,class BaseClass0>
		.try {  ldloc V_7
				ldstr "MyStruct244::Method0.1810()#MyStruct244::Method1.MI.1812()#MyStruct244::Method2.1813<System.Object>()#MyStruct244::Method3.MI.1815<System.Object>()#"
				call void Framework::M.IBase0<valuetype MyStruct244`2<class BaseClass1,class BaseClass0>>(!!0,string) leave.s LV6
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV6} LV6:
		.try {  ldloc V_7
				ldstr "MyStruct244::Method4.1816()#MyStruct244::Method5.MI.1818()#MyStruct244::Method6.MI.1820<System.Object>()#"
				call void Framework::M.IBase1.T<class BaseClass1,valuetype MyStruct244`2<class BaseClass1,class BaseClass0>>(!!1,string) leave.s LV7
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV7} LV7:
		.try {  ldloc V_7
				ldstr "MyStruct244::Method4.1816()#MyStruct244::Method5.MI.1818()#MyStruct244::Method6.MI.1820<System.Object>()#"
				call void Framework::M.IBase1.B<valuetype MyStruct244`2<class BaseClass1,class BaseClass0>>(!!0,string) leave.s LV8
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV8} LV8:
		.try {  ldloc V_7
				ldstr "MyStruct244::Method4.1816()#MyStruct244::Method5.MI.1818()#MyStruct244::Method6.MI.1820<System.Object>()#"
				call void Framework::M.IBase1.T<class BaseClass0,valuetype MyStruct244`2<class BaseClass1,class BaseClass0>>(!!1,string) leave.s LV9
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV9} LV9:
		.try {  ldloc V_7
				ldstr "MyStruct244::Method4.1816()#MyStruct244::Method5.MI.1818()#MyStruct244::Method6.MI.1820<System.Object>()#"
				call void Framework::M.IBase1.A<valuetype MyStruct244`2<class BaseClass1,class BaseClass0>>(!!0,string) leave.s LV10
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV10} LV10:
		.locals init (valuetype MyStruct244`2<class BaseClass1,class BaseClass1> V_8)
		ldloca V_8
		initobj valuetype MyStruct244`2<class BaseClass1,class BaseClass1>
		.try {  ldloc V_8
				ldstr "MyStruct244::Method0.1810()#MyStruct244::Method1.MI.1812()#MyStruct244::Method2.1813<System.Object>()#MyStruct244::Method3.MI.1815<System.Object>()#"
				call void Framework::M.IBase0<valuetype MyStruct244`2<class BaseClass1,class BaseClass1>>(!!0,string) leave.s LV11
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV11} LV11:
		.try {  ldloc V_8
				ldstr "MyStruct244::Method4.1816()#MyStruct244::Method5.MI.1818()#MyStruct244::Method6.MI.1820<System.Object>()#"
				call void Framework::M.IBase1.T<class BaseClass1,valuetype MyStruct244`2<class BaseClass1,class BaseClass1>>(!!1,string) leave.s LV12
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV12} LV12:
		.try {  ldloc V_8
				ldstr "MyStruct244::Method4.1816()#MyStruct244::Method5.MI.1818()#MyStruct244::Method6.MI.1820<System.Object>()#"
				call void Framework::M.IBase1.B<valuetype MyStruct244`2<class BaseClass1,class BaseClass1>>(!!0,string) leave.s LV13
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV13} LV13:
		.try {  ldloc V_8
				ldstr "MyStruct244::Method4.1816()#MyStruct244::Method5.MI.1818()#MyStruct244::Method6.MI.1820<System.Object>()#"
				call void Framework::M.IBase1.T<class BaseClass0,valuetype MyStruct244`2<class BaseClass1,class BaseClass1>>(!!1,string) leave.s LV14
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV14} LV14:
		.try {  ldloc V_8
				ldstr "MyStruct244::Method4.1816()#MyStruct244::Method5.MI.1818()#MyStruct244::Method6.MI.1820<System.Object>()#"
				call void Framework::M.IBase1.A<valuetype MyStruct244`2<class BaseClass1,class BaseClass1>>(!!0,string) leave.s LV15
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV15} LV15:
		ldstr "========================================================================\n\n"
		call void [mscorlib]System.Console::WriteLine(string)
		ret
	}
	.method public hidebysig static void StructConstrainedInterfaceCallsTest() cil managed
	{
		.maxstack  10
		ldstr "===================== Struct Constrained Interface Calls Test ====================="
		call void [mscorlib]System.Console::WriteLine(string)
		.locals init (valuetype MyStruct244`2<class BaseClass0,class BaseClass0> V_9)
		ldloca V_9
		initobj valuetype MyStruct244`2<class BaseClass0,class BaseClass0>
		.try {  ldloc V_9
				ldstr "MyStruct244::Method0.1810()#" +
				      "MyStruct244::Method1.MI.1812()#" +
				      "MyStruct244::Method2.1813<System.Object>()#" +
				      "MyStruct244::Method3.MI.1815<System.Object>()#" +
				      "MyStruct244::Method4.1816()#" +
				      "MyStruct244::Method5.MI.1818()#" +
				      "MyStruct244::Method6.MI.1820<System.Object>()#"
				call void Framework::M.MyStruct244.T.T<class BaseClass0,class BaseClass0,valuetype MyStruct244`2<class BaseClass0,class BaseClass0>>(!!2,string) leave.s LV0
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV0} LV0:
		.try {  ldloc V_9
				ldstr "MyStruct244::Method0.1810()#" +
				      "MyStruct244::Method1.MI.1812()#" +
				      "MyStruct244::Method2.1813<System.Object>()#" +
				      "MyStruct244::Method3.MI.1815<System.Object>()#" +
				      "MyStruct244::Method4.1816()#" +
				      "MyStruct244::Method5.MI.1818()#" +
				      "MyStruct244::Method6.MI.1820<System.Object>()#"
				call void Framework::M.MyStruct244.A.T<class BaseClass0,valuetype MyStruct244`2<class BaseClass0,class BaseClass0>>(!!1,string) leave.s LV1
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV1} LV1:
		.try {  ldloc V_9
				ldstr "MyStruct244::Method0.1810()#" +
				      "MyStruct244::Method1.MI.1812()#" +
				      "MyStruct244::Method2.1813<System.Object>()#" +
				      "MyStruct244::Method3.MI.1815<System.Object>()#" +
				      "MyStruct244::Method4.1816()#" +
				      "MyStruct244::Method5.MI.1818()#" +
				      "MyStruct244::Method6.MI.1820<System.Object>()#"
				call void Framework::M.MyStruct244.A.A<valuetype MyStruct244`2<class BaseClass0,class BaseClass0>>(!!0,string) leave.s LV2
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV2} LV2:
		.locals init (valuetype MyStruct244`2<class BaseClass0,class BaseClass1> V_10)
		ldloca V_10
		initobj valuetype MyStruct244`2<class BaseClass0,class BaseClass1>
		.try {  ldloc V_10
				ldstr "MyStruct244::Method0.1810()#" +
				      "MyStruct244::Method1.MI.1812()#" +
				      "MyStruct244::Method2.1813<System.Object>()#" +
				      "MyStruct244::Method3.MI.1815<System.Object>()#" +
				      "MyStruct244::Method4.1816()#" +
				      "MyStruct244::Method5.MI.1818()#" +
				      "MyStruct244::Method6.MI.1820<System.Object>()#"
				call void Framework::M.MyStruct244.T.T<class BaseClass0,class BaseClass1,valuetype MyStruct244`2<class BaseClass0,class BaseClass1>>(!!2,string) leave.s LV3
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV3} LV3:
		.try {  ldloc V_10
				ldstr "MyStruct244::Method0.1810()#" +
				      "MyStruct244::Method1.MI.1812()#" +
				      "MyStruct244::Method2.1813<System.Object>()#" +
				      "MyStruct244::Method3.MI.1815<System.Object>()#" +
				      "MyStruct244::Method4.1816()#" +
				      "MyStruct244::Method5.MI.1818()#" +
				      "MyStruct244::Method6.MI.1820<System.Object>()#"
				call void Framework::M.MyStruct244.A.T<class BaseClass1,valuetype MyStruct244`2<class BaseClass0,class BaseClass1>>(!!1,string) leave.s LV4
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV4} LV4:
		.try {  ldloc V_10
				ldstr "MyStruct244::Method0.1810()#" +
				      "MyStruct244::Method1.MI.1812()#" +
				      "MyStruct244::Method2.1813<System.Object>()#" +
				      "MyStruct244::Method3.MI.1815<System.Object>()#" +
				      "MyStruct244::Method4.1816()#" +
				      "MyStruct244::Method5.MI.1818()#" +
				      "MyStruct244::Method6.MI.1820<System.Object>()#"
				call void Framework::M.MyStruct244.A.B<valuetype MyStruct244`2<class BaseClass0,class BaseClass1>>(!!0,string) leave.s LV5
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV5} LV5:
		.locals init (valuetype MyStruct244`2<class BaseClass1,class BaseClass0> V_11)
		ldloca V_11
		initobj valuetype MyStruct244`2<class BaseClass1,class BaseClass0>
		.try {  ldloc V_11
				ldstr "MyStruct244::Method0.1810()#" +
				      "MyStruct244::Method1.MI.1812()#" +
				      "MyStruct244::Method2.1813<System.Object>()#" +
				      "MyStruct244::Method3.MI.1815<System.Object>()#" +
				      "MyStruct244::Method4.1816()#" +
				      "MyStruct244::Method5.MI.1818()#" +
				      "MyStruct244::Method6.MI.1820<System.Object>()#"
				call void Framework::M.MyStruct244.T.T<class BaseClass1,class BaseClass0,valuetype MyStruct244`2<class BaseClass1,class BaseClass0>>(!!2,string) leave.s LV6
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV6} LV6:
		.try {  ldloc V_11
				ldstr "MyStruct244::Method0.1810()#" +
				      "MyStruct244::Method1.MI.1812()#" +
				      "MyStruct244::Method2.1813<System.Object>()#" +
				      "MyStruct244::Method3.MI.1815<System.Object>()#" +
				      "MyStruct244::Method4.1816()#" +
				      "MyStruct244::Method5.MI.1818()#" +
				      "MyStruct244::Method6.MI.1820<System.Object>()#"
				call void Framework::M.MyStruct244.B.T<class BaseClass0,valuetype MyStruct244`2<class BaseClass1,class BaseClass0>>(!!1,string) leave.s LV7
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV7} LV7:
		.try {  ldloc V_11
				ldstr "MyStruct244::Method0.1810()#" +
				      "MyStruct244::Method1.MI.1812()#" +
				      "MyStruct244::Method2.1813<System.Object>()#" +
				      "MyStruct244::Method3.MI.1815<System.Object>()#" +
				      "MyStruct244::Method4.1816()#" +
				      "MyStruct244::Method5.MI.1818()#" +
				      "MyStruct244::Method6.MI.1820<System.Object>()#"
				call void Framework::M.MyStruct244.B.A<valuetype MyStruct244`2<class BaseClass1,class BaseClass0>>(!!0,string) leave.s LV8
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV8} LV8:
		.locals init (valuetype MyStruct244`2<class BaseClass1,class BaseClass1> V_12)
		ldloca V_12
		initobj valuetype MyStruct244`2<class BaseClass1,class BaseClass1>
		.try {  ldloc V_12
				ldstr "MyStruct244::Method0.1810()#" +
				      "MyStruct244::Method1.MI.1812()#" +
				      "MyStruct244::Method2.1813<System.Object>()#" +
				      "MyStruct244::Method3.MI.1815<System.Object>()#" +
				      "MyStruct244::Method4.1816()#" +
				      "MyStruct244::Method5.MI.1818()#" +
				      "MyStruct244::Method6.MI.1820<System.Object>()#"
				call void Framework::M.MyStruct244.T.T<class BaseClass1,class BaseClass1,valuetype MyStruct244`2<class BaseClass1,class BaseClass1>>(!!2,string) leave.s LV9
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV9} LV9:
		.try {  ldloc V_12
				ldstr "MyStruct244::Method0.1810()#" +
				      "MyStruct244::Method1.MI.1812()#" +
				      "MyStruct244::Method2.1813<System.Object>()#" +
				      "MyStruct244::Method3.MI.1815<System.Object>()#" +
				      "MyStruct244::Method4.1816()#" +
				      "MyStruct244::Method5.MI.1818()#" +
				      "MyStruct244::Method6.MI.1820<System.Object>()#"
				call void Framework::M.MyStruct244.B.T<class BaseClass1,valuetype MyStruct244`2<class BaseClass1,class BaseClass1>>(!!1,string) leave.s LV10
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV10} LV10:
		.try {  ldloc V_12
				ldstr "MyStruct244::Method0.1810()#" +
				      "MyStruct244::Method1.MI.1812()#" +
				      "MyStruct244::Method2.1813<System.Object>()#" +
				      "MyStruct244::Method3.MI.1815<System.Object>()#" +
				      "MyStruct244::Method4.1816()#" +
				      "MyStruct244::Method5.MI.1818()#" +
				      "MyStruct244::Method6.MI.1820<System.Object>()#"
				call void Framework::M.MyStruct244.B.B<valuetype MyStruct244`2<class BaseClass1,class BaseClass1>>(!!0,string) leave.s LV11
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV11} LV11:
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
		.locals init (valuetype MyStruct244`2<class BaseClass0,class BaseClass0> V_13)
		ldloca V_13
		initobj valuetype MyStruct244`2<class BaseClass0,class BaseClass0>
		ldloc V_13
		box valuetype MyStruct244`2<class BaseClass0,class BaseClass0>
		ldloc V_13
		box valuetype MyStruct244`2<class BaseClass0,class BaseClass0>
		ldvirtftn instance string valuetype MyStruct244`2<class BaseClass0,class BaseClass0>::Method0()
		calli default string(object)
		ldstr "MyStruct244::Method0.1810()"
		ldstr "valuetype MyStruct244`2<class BaseClass0,class BaseClass0> on type valuetype MyStruct244`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_13
		box valuetype MyStruct244`2<class BaseClass0,class BaseClass0>
		ldloc V_13
		box valuetype MyStruct244`2<class BaseClass0,class BaseClass0>
		ldvirtftn instance string valuetype MyStruct244`2<class BaseClass0,class BaseClass0>::Method1()
		calli default string(object)
		ldstr "MyStruct244::Method1.1811()"
		ldstr "valuetype MyStruct244`2<class BaseClass0,class BaseClass0> on type valuetype MyStruct244`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_13
		box valuetype MyStruct244`2<class BaseClass0,class BaseClass0>
		ldloc V_13
		box valuetype MyStruct244`2<class BaseClass0,class BaseClass0>
		ldvirtftn instance string valuetype MyStruct244`2<class BaseClass0,class BaseClass0>::Method2<object>()
		calli default string(object)
		ldstr "MyStruct244::Method2.1813<System.Object>()"
		ldstr "valuetype MyStruct244`2<class BaseClass0,class BaseClass0> on type valuetype MyStruct244`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_13
		box valuetype MyStruct244`2<class BaseClass0,class BaseClass0>
		ldloc V_13
		box valuetype MyStruct244`2<class BaseClass0,class BaseClass0>
		ldvirtftn instance string valuetype MyStruct244`2<class BaseClass0,class BaseClass0>::Method3<object>()
		calli default string(object)
		ldstr "MyStruct244::Method3.1814<System.Object>()"
		ldstr "valuetype MyStruct244`2<class BaseClass0,class BaseClass0> on type valuetype MyStruct244`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_13
		box valuetype MyStruct244`2<class BaseClass0,class BaseClass0>
		ldloc V_13
		box valuetype MyStruct244`2<class BaseClass0,class BaseClass0>
		ldvirtftn instance string valuetype MyStruct244`2<class BaseClass0,class BaseClass0>::Method4()
		calli default string(object)
		ldstr "MyStruct244::Method4.1816()"
		ldstr "valuetype MyStruct244`2<class BaseClass0,class BaseClass0> on type valuetype MyStruct244`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_13
		box valuetype MyStruct244`2<class BaseClass0,class BaseClass0>
		ldloc V_13
		box valuetype MyStruct244`2<class BaseClass0,class BaseClass0>
		ldvirtftn instance string valuetype MyStruct244`2<class BaseClass0,class BaseClass0>::Method5()
		calli default string(object)
		ldstr "MyStruct244::Method5.1817()"
		ldstr "valuetype MyStruct244`2<class BaseClass0,class BaseClass0> on type valuetype MyStruct244`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_13
		box valuetype MyStruct244`2<class BaseClass0,class BaseClass0>
		ldloc V_13
		box valuetype MyStruct244`2<class BaseClass0,class BaseClass0>
		ldvirtftn instance string valuetype MyStruct244`2<class BaseClass0,class BaseClass0>::Method6<object>()
		calli default string(object)
		ldstr "MyStruct244::Method6.1819<System.Object>()"
		ldstr "valuetype MyStruct244`2<class BaseClass0,class BaseClass0> on type valuetype MyStruct244`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_13
		box valuetype MyStruct244`2<class BaseClass0,class BaseClass0>
		ldloc V_13
		box valuetype MyStruct244`2<class BaseClass0,class BaseClass0>
		ldvirtftn instance string valuetype MyStruct244`2<class BaseClass0,class BaseClass0>::ClassMethod492()
		calli default string(object)
		ldstr "MyStruct244::ClassMethod492.1821()"
		ldstr "valuetype MyStruct244`2<class BaseClass0,class BaseClass0> on type valuetype MyStruct244`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_13
		box valuetype MyStruct244`2<class BaseClass0,class BaseClass0>
		ldloc V_13
		box valuetype MyStruct244`2<class BaseClass0,class BaseClass0>
		ldvirtftn instance string valuetype MyStruct244`2<class BaseClass0,class BaseClass0>::ClassMethod493<object>()
		calli default string(object)
		ldstr "MyStruct244::ClassMethod493.1822<System.Object>()"
		ldstr "valuetype MyStruct244`2<class BaseClass0,class BaseClass0> on type valuetype MyStruct244`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_13
		box valuetype MyStruct244`2<class BaseClass0,class BaseClass0>
		ldloc V_13
		box valuetype MyStruct244`2<class BaseClass0,class BaseClass0>
		ldvirtftn instance string valuetype MyStruct244`2<class BaseClass0,class BaseClass0>::ClassMethod494<object>()
		calli default string(object)
		ldstr "MyStruct244::ClassMethod494.1823<System.Object>()"
		ldstr "valuetype MyStruct244`2<class BaseClass0,class BaseClass0> on type valuetype MyStruct244`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_13 box valuetype MyStruct244`2<class BaseClass0,class BaseClass0> ldnull 
		ldloc V_13 box valuetype MyStruct244`2<class BaseClass0,class BaseClass0>
		ldvirtftn instance bool valuetype MyStruct244`2<class BaseClass0,class BaseClass0>::Equals(object) calli default bool(object,object) pop
		ldloc V_13 box valuetype MyStruct244`2<class BaseClass0,class BaseClass0>
		ldloc V_13 box valuetype MyStruct244`2<class BaseClass0,class BaseClass0>
		ldvirtftn instance int32 valuetype MyStruct244`2<class BaseClass0,class BaseClass0>::GetHashCode() calli default int32(object) pop
		ldloc V_13 box valuetype MyStruct244`2<class BaseClass0,class BaseClass0>
		ldloc V_13 box valuetype MyStruct244`2<class BaseClass0,class BaseClass0>
		ldvirtftn instance string valuetype MyStruct244`2<class BaseClass0,class BaseClass0>::ToString() calli default string(object) pop
		ldloc V_13
		box valuetype MyStruct244`2<class BaseClass0,class BaseClass0>
		ldloc V_13
		box valuetype MyStruct244`2<class BaseClass0,class BaseClass0>
		ldvirtftn instance string IBase0::Method0()
		calli default string(object)
		ldstr "MyStruct244::Method0.1810()"
		ldstr "IBase0 on type valuetype MyStruct244`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_13
		box valuetype MyStruct244`2<class BaseClass0,class BaseClass0>
		ldloc V_13
		box valuetype MyStruct244`2<class BaseClass0,class BaseClass0>
		ldvirtftn instance string IBase0::Method1()
		calli default string(object)
		ldstr "MyStruct244::Method1.MI.1812()"
		ldstr "IBase0 on type valuetype MyStruct244`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_13
		box valuetype MyStruct244`2<class BaseClass0,class BaseClass0>
		ldloc V_13
		box valuetype MyStruct244`2<class BaseClass0,class BaseClass0>
		ldvirtftn instance string IBase0::Method2<object>()
		calli default string(object)
		ldstr "MyStruct244::Method2.1813<System.Object>()"
		ldstr "IBase0 on type valuetype MyStruct244`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_13
		box valuetype MyStruct244`2<class BaseClass0,class BaseClass0>
		ldloc V_13
		box valuetype MyStruct244`2<class BaseClass0,class BaseClass0>
		ldvirtftn instance string IBase0::Method3<object>()
		calli default string(object)
		ldstr "MyStruct244::Method3.MI.1815<System.Object>()"
		ldstr "IBase0 on type valuetype MyStruct244`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_13
		box valuetype MyStruct244`2<class BaseClass0,class BaseClass0>
		ldloc V_13
		box valuetype MyStruct244`2<class BaseClass0,class BaseClass0>
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(object)
		ldstr "MyStruct244::Method4.1816()"
		ldstr "class IBase1`1<class BaseClass0> on type valuetype MyStruct244`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_13
		box valuetype MyStruct244`2<class BaseClass0,class BaseClass0>
		ldloc V_13
		box valuetype MyStruct244`2<class BaseClass0,class BaseClass0>
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(object)
		ldstr "MyStruct244::Method5.MI.1818()"
		ldstr "class IBase1`1<class BaseClass0> on type valuetype MyStruct244`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_13
		box valuetype MyStruct244`2<class BaseClass0,class BaseClass0>
		ldloc V_13
		box valuetype MyStruct244`2<class BaseClass0,class BaseClass0>
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(object)
		ldstr "MyStruct244::Method6.MI.1820<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type valuetype MyStruct244`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		.locals init (valuetype MyStruct244`2<class BaseClass0,class BaseClass1> V_14)
		ldloca V_14
		initobj valuetype MyStruct244`2<class BaseClass0,class BaseClass1>
		ldloc V_14
		box valuetype MyStruct244`2<class BaseClass0,class BaseClass1>
		ldloc V_14
		box valuetype MyStruct244`2<class BaseClass0,class BaseClass1>
		ldvirtftn instance string valuetype MyStruct244`2<class BaseClass0,class BaseClass1>::Method0()
		calli default string(object)
		ldstr "MyStruct244::Method0.1810()"
		ldstr "valuetype MyStruct244`2<class BaseClass0,class BaseClass1> on type valuetype MyStruct244`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_14
		box valuetype MyStruct244`2<class BaseClass0,class BaseClass1>
		ldloc V_14
		box valuetype MyStruct244`2<class BaseClass0,class BaseClass1>
		ldvirtftn instance string valuetype MyStruct244`2<class BaseClass0,class BaseClass1>::Method1()
		calli default string(object)
		ldstr "MyStruct244::Method1.1811()"
		ldstr "valuetype MyStruct244`2<class BaseClass0,class BaseClass1> on type valuetype MyStruct244`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_14
		box valuetype MyStruct244`2<class BaseClass0,class BaseClass1>
		ldloc V_14
		box valuetype MyStruct244`2<class BaseClass0,class BaseClass1>
		ldvirtftn instance string valuetype MyStruct244`2<class BaseClass0,class BaseClass1>::Method2<object>()
		calli default string(object)
		ldstr "MyStruct244::Method2.1813<System.Object>()"
		ldstr "valuetype MyStruct244`2<class BaseClass0,class BaseClass1> on type valuetype MyStruct244`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_14
		box valuetype MyStruct244`2<class BaseClass0,class BaseClass1>
		ldloc V_14
		box valuetype MyStruct244`2<class BaseClass0,class BaseClass1>
		ldvirtftn instance string valuetype MyStruct244`2<class BaseClass0,class BaseClass1>::Method3<object>()
		calli default string(object)
		ldstr "MyStruct244::Method3.1814<System.Object>()"
		ldstr "valuetype MyStruct244`2<class BaseClass0,class BaseClass1> on type valuetype MyStruct244`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_14
		box valuetype MyStruct244`2<class BaseClass0,class BaseClass1>
		ldloc V_14
		box valuetype MyStruct244`2<class BaseClass0,class BaseClass1>
		ldvirtftn instance string valuetype MyStruct244`2<class BaseClass0,class BaseClass1>::Method4()
		calli default string(object)
		ldstr "MyStruct244::Method4.1816()"
		ldstr "valuetype MyStruct244`2<class BaseClass0,class BaseClass1> on type valuetype MyStruct244`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_14
		box valuetype MyStruct244`2<class BaseClass0,class BaseClass1>
		ldloc V_14
		box valuetype MyStruct244`2<class BaseClass0,class BaseClass1>
		ldvirtftn instance string valuetype MyStruct244`2<class BaseClass0,class BaseClass1>::Method5()
		calli default string(object)
		ldstr "MyStruct244::Method5.1817()"
		ldstr "valuetype MyStruct244`2<class BaseClass0,class BaseClass1> on type valuetype MyStruct244`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_14
		box valuetype MyStruct244`2<class BaseClass0,class BaseClass1>
		ldloc V_14
		box valuetype MyStruct244`2<class BaseClass0,class BaseClass1>
		ldvirtftn instance string valuetype MyStruct244`2<class BaseClass0,class BaseClass1>::Method6<object>()
		calli default string(object)
		ldstr "MyStruct244::Method6.1819<System.Object>()"
		ldstr "valuetype MyStruct244`2<class BaseClass0,class BaseClass1> on type valuetype MyStruct244`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_14
		box valuetype MyStruct244`2<class BaseClass0,class BaseClass1>
		ldloc V_14
		box valuetype MyStruct244`2<class BaseClass0,class BaseClass1>
		ldvirtftn instance string valuetype MyStruct244`2<class BaseClass0,class BaseClass1>::ClassMethod492()
		calli default string(object)
		ldstr "MyStruct244::ClassMethod492.1821()"
		ldstr "valuetype MyStruct244`2<class BaseClass0,class BaseClass1> on type valuetype MyStruct244`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_14
		box valuetype MyStruct244`2<class BaseClass0,class BaseClass1>
		ldloc V_14
		box valuetype MyStruct244`2<class BaseClass0,class BaseClass1>
		ldvirtftn instance string valuetype MyStruct244`2<class BaseClass0,class BaseClass1>::ClassMethod493<object>()
		calli default string(object)
		ldstr "MyStruct244::ClassMethod493.1822<System.Object>()"
		ldstr "valuetype MyStruct244`2<class BaseClass0,class BaseClass1> on type valuetype MyStruct244`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_14
		box valuetype MyStruct244`2<class BaseClass0,class BaseClass1>
		ldloc V_14
		box valuetype MyStruct244`2<class BaseClass0,class BaseClass1>
		ldvirtftn instance string valuetype MyStruct244`2<class BaseClass0,class BaseClass1>::ClassMethod494<object>()
		calli default string(object)
		ldstr "MyStruct244::ClassMethod494.1823<System.Object>()"
		ldstr "valuetype MyStruct244`2<class BaseClass0,class BaseClass1> on type valuetype MyStruct244`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_14 box valuetype MyStruct244`2<class BaseClass0,class BaseClass1> ldnull 
		ldloc V_14 box valuetype MyStruct244`2<class BaseClass0,class BaseClass1>
		ldvirtftn instance bool valuetype MyStruct244`2<class BaseClass0,class BaseClass1>::Equals(object) calli default bool(object,object) pop
		ldloc V_14 box valuetype MyStruct244`2<class BaseClass0,class BaseClass1>
		ldloc V_14 box valuetype MyStruct244`2<class BaseClass0,class BaseClass1>
		ldvirtftn instance int32 valuetype MyStruct244`2<class BaseClass0,class BaseClass1>::GetHashCode() calli default int32(object) pop
		ldloc V_14 box valuetype MyStruct244`2<class BaseClass0,class BaseClass1>
		ldloc V_14 box valuetype MyStruct244`2<class BaseClass0,class BaseClass1>
		ldvirtftn instance string valuetype MyStruct244`2<class BaseClass0,class BaseClass1>::ToString() calli default string(object) pop
		ldloc V_14
		box valuetype MyStruct244`2<class BaseClass0,class BaseClass1>
		ldloc V_14
		box valuetype MyStruct244`2<class BaseClass0,class BaseClass1>
		ldvirtftn instance string IBase0::Method0()
		calli default string(object)
		ldstr "MyStruct244::Method0.1810()"
		ldstr "IBase0 on type valuetype MyStruct244`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_14
		box valuetype MyStruct244`2<class BaseClass0,class BaseClass1>
		ldloc V_14
		box valuetype MyStruct244`2<class BaseClass0,class BaseClass1>
		ldvirtftn instance string IBase0::Method1()
		calli default string(object)
		ldstr "MyStruct244::Method1.MI.1812()"
		ldstr "IBase0 on type valuetype MyStruct244`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_14
		box valuetype MyStruct244`2<class BaseClass0,class BaseClass1>
		ldloc V_14
		box valuetype MyStruct244`2<class BaseClass0,class BaseClass1>
		ldvirtftn instance string IBase0::Method2<object>()
		calli default string(object)
		ldstr "MyStruct244::Method2.1813<System.Object>()"
		ldstr "IBase0 on type valuetype MyStruct244`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_14
		box valuetype MyStruct244`2<class BaseClass0,class BaseClass1>
		ldloc V_14
		box valuetype MyStruct244`2<class BaseClass0,class BaseClass1>
		ldvirtftn instance string IBase0::Method3<object>()
		calli default string(object)
		ldstr "MyStruct244::Method3.MI.1815<System.Object>()"
		ldstr "IBase0 on type valuetype MyStruct244`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_14
		box valuetype MyStruct244`2<class BaseClass0,class BaseClass1>
		ldloc V_14
		box valuetype MyStruct244`2<class BaseClass0,class BaseClass1>
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(object)
		ldstr "MyStruct244::Method4.1816()"
		ldstr "class IBase1`1<class BaseClass0> on type valuetype MyStruct244`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_14
		box valuetype MyStruct244`2<class BaseClass0,class BaseClass1>
		ldloc V_14
		box valuetype MyStruct244`2<class BaseClass0,class BaseClass1>
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(object)
		ldstr "MyStruct244::Method5.MI.1818()"
		ldstr "class IBase1`1<class BaseClass0> on type valuetype MyStruct244`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_14
		box valuetype MyStruct244`2<class BaseClass0,class BaseClass1>
		ldloc V_14
		box valuetype MyStruct244`2<class BaseClass0,class BaseClass1>
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(object)
		ldstr "MyStruct244::Method6.MI.1820<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type valuetype MyStruct244`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		.locals init (valuetype MyStruct244`2<class BaseClass1,class BaseClass0> V_15)
		ldloca V_15
		initobj valuetype MyStruct244`2<class BaseClass1,class BaseClass0>
		ldloc V_15
		box valuetype MyStruct244`2<class BaseClass1,class BaseClass0>
		ldloc V_15
		box valuetype MyStruct244`2<class BaseClass1,class BaseClass0>
		ldvirtftn instance string valuetype MyStruct244`2<class BaseClass1,class BaseClass0>::Method0()
		calli default string(object)
		ldstr "MyStruct244::Method0.1810()"
		ldstr "valuetype MyStruct244`2<class BaseClass1,class BaseClass0> on type valuetype MyStruct244`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_15
		box valuetype MyStruct244`2<class BaseClass1,class BaseClass0>
		ldloc V_15
		box valuetype MyStruct244`2<class BaseClass1,class BaseClass0>
		ldvirtftn instance string valuetype MyStruct244`2<class BaseClass1,class BaseClass0>::Method1()
		calli default string(object)
		ldstr "MyStruct244::Method1.1811()"
		ldstr "valuetype MyStruct244`2<class BaseClass1,class BaseClass0> on type valuetype MyStruct244`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_15
		box valuetype MyStruct244`2<class BaseClass1,class BaseClass0>
		ldloc V_15
		box valuetype MyStruct244`2<class BaseClass1,class BaseClass0>
		ldvirtftn instance string valuetype MyStruct244`2<class BaseClass1,class BaseClass0>::Method2<object>()
		calli default string(object)
		ldstr "MyStruct244::Method2.1813<System.Object>()"
		ldstr "valuetype MyStruct244`2<class BaseClass1,class BaseClass0> on type valuetype MyStruct244`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_15
		box valuetype MyStruct244`2<class BaseClass1,class BaseClass0>
		ldloc V_15
		box valuetype MyStruct244`2<class BaseClass1,class BaseClass0>
		ldvirtftn instance string valuetype MyStruct244`2<class BaseClass1,class BaseClass0>::Method3<object>()
		calli default string(object)
		ldstr "MyStruct244::Method3.1814<System.Object>()"
		ldstr "valuetype MyStruct244`2<class BaseClass1,class BaseClass0> on type valuetype MyStruct244`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_15
		box valuetype MyStruct244`2<class BaseClass1,class BaseClass0>
		ldloc V_15
		box valuetype MyStruct244`2<class BaseClass1,class BaseClass0>
		ldvirtftn instance string valuetype MyStruct244`2<class BaseClass1,class BaseClass0>::Method4()
		calli default string(object)
		ldstr "MyStruct244::Method4.1816()"
		ldstr "valuetype MyStruct244`2<class BaseClass1,class BaseClass0> on type valuetype MyStruct244`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_15
		box valuetype MyStruct244`2<class BaseClass1,class BaseClass0>
		ldloc V_15
		box valuetype MyStruct244`2<class BaseClass1,class BaseClass0>
		ldvirtftn instance string valuetype MyStruct244`2<class BaseClass1,class BaseClass0>::Method5()
		calli default string(object)
		ldstr "MyStruct244::Method5.1817()"
		ldstr "valuetype MyStruct244`2<class BaseClass1,class BaseClass0> on type valuetype MyStruct244`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_15
		box valuetype MyStruct244`2<class BaseClass1,class BaseClass0>
		ldloc V_15
		box valuetype MyStruct244`2<class BaseClass1,class BaseClass0>
		ldvirtftn instance string valuetype MyStruct244`2<class BaseClass1,class BaseClass0>::Method6<object>()
		calli default string(object)
		ldstr "MyStruct244::Method6.1819<System.Object>()"
		ldstr "valuetype MyStruct244`2<class BaseClass1,class BaseClass0> on type valuetype MyStruct244`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_15
		box valuetype MyStruct244`2<class BaseClass1,class BaseClass0>
		ldloc V_15
		box valuetype MyStruct244`2<class BaseClass1,class BaseClass0>
		ldvirtftn instance string valuetype MyStruct244`2<class BaseClass1,class BaseClass0>::ClassMethod492()
		calli default string(object)
		ldstr "MyStruct244::ClassMethod492.1821()"
		ldstr "valuetype MyStruct244`2<class BaseClass1,class BaseClass0> on type valuetype MyStruct244`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_15
		box valuetype MyStruct244`2<class BaseClass1,class BaseClass0>
		ldloc V_15
		box valuetype MyStruct244`2<class BaseClass1,class BaseClass0>
		ldvirtftn instance string valuetype MyStruct244`2<class BaseClass1,class BaseClass0>::ClassMethod493<object>()
		calli default string(object)
		ldstr "MyStruct244::ClassMethod493.1822<System.Object>()"
		ldstr "valuetype MyStruct244`2<class BaseClass1,class BaseClass0> on type valuetype MyStruct244`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_15
		box valuetype MyStruct244`2<class BaseClass1,class BaseClass0>
		ldloc V_15
		box valuetype MyStruct244`2<class BaseClass1,class BaseClass0>
		ldvirtftn instance string valuetype MyStruct244`2<class BaseClass1,class BaseClass0>::ClassMethod494<object>()
		calli default string(object)
		ldstr "MyStruct244::ClassMethod494.1823<System.Object>()"
		ldstr "valuetype MyStruct244`2<class BaseClass1,class BaseClass0> on type valuetype MyStruct244`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_15 box valuetype MyStruct244`2<class BaseClass1,class BaseClass0> ldnull 
		ldloc V_15 box valuetype MyStruct244`2<class BaseClass1,class BaseClass0>
		ldvirtftn instance bool valuetype MyStruct244`2<class BaseClass1,class BaseClass0>::Equals(object) calli default bool(object,object) pop
		ldloc V_15 box valuetype MyStruct244`2<class BaseClass1,class BaseClass0>
		ldloc V_15 box valuetype MyStruct244`2<class BaseClass1,class BaseClass0>
		ldvirtftn instance int32 valuetype MyStruct244`2<class BaseClass1,class BaseClass0>::GetHashCode() calli default int32(object) pop
		ldloc V_15 box valuetype MyStruct244`2<class BaseClass1,class BaseClass0>
		ldloc V_15 box valuetype MyStruct244`2<class BaseClass1,class BaseClass0>
		ldvirtftn instance string valuetype MyStruct244`2<class BaseClass1,class BaseClass0>::ToString() calli default string(object) pop
		ldloc V_15
		box valuetype MyStruct244`2<class BaseClass1,class BaseClass0>
		ldloc V_15
		box valuetype MyStruct244`2<class BaseClass1,class BaseClass0>
		ldvirtftn instance string IBase0::Method0()
		calli default string(object)
		ldstr "MyStruct244::Method0.1810()"
		ldstr "IBase0 on type valuetype MyStruct244`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_15
		box valuetype MyStruct244`2<class BaseClass1,class BaseClass0>
		ldloc V_15
		box valuetype MyStruct244`2<class BaseClass1,class BaseClass0>
		ldvirtftn instance string IBase0::Method1()
		calli default string(object)
		ldstr "MyStruct244::Method1.MI.1812()"
		ldstr "IBase0 on type valuetype MyStruct244`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_15
		box valuetype MyStruct244`2<class BaseClass1,class BaseClass0>
		ldloc V_15
		box valuetype MyStruct244`2<class BaseClass1,class BaseClass0>
		ldvirtftn instance string IBase0::Method2<object>()
		calli default string(object)
		ldstr "MyStruct244::Method2.1813<System.Object>()"
		ldstr "IBase0 on type valuetype MyStruct244`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_15
		box valuetype MyStruct244`2<class BaseClass1,class BaseClass0>
		ldloc V_15
		box valuetype MyStruct244`2<class BaseClass1,class BaseClass0>
		ldvirtftn instance string IBase0::Method3<object>()
		calli default string(object)
		ldstr "MyStruct244::Method3.MI.1815<System.Object>()"
		ldstr "IBase0 on type valuetype MyStruct244`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_15
		box valuetype MyStruct244`2<class BaseClass1,class BaseClass0>
		ldloc V_15
		box valuetype MyStruct244`2<class BaseClass1,class BaseClass0>
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(object)
		ldstr "MyStruct244::Method4.1816()"
		ldstr "class IBase1`1<class BaseClass1> on type valuetype MyStruct244`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_15
		box valuetype MyStruct244`2<class BaseClass1,class BaseClass0>
		ldloc V_15
		box valuetype MyStruct244`2<class BaseClass1,class BaseClass0>
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(object)
		ldstr "MyStruct244::Method5.MI.1818()"
		ldstr "class IBase1`1<class BaseClass1> on type valuetype MyStruct244`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_15
		box valuetype MyStruct244`2<class BaseClass1,class BaseClass0>
		ldloc V_15
		box valuetype MyStruct244`2<class BaseClass1,class BaseClass0>
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(object)
		ldstr "MyStruct244::Method6.MI.1820<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type valuetype MyStruct244`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_15
		box valuetype MyStruct244`2<class BaseClass1,class BaseClass0>
		ldloc V_15
		box valuetype MyStruct244`2<class BaseClass1,class BaseClass0>
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(object)
		ldstr "MyStruct244::Method4.1816()"
		ldstr "class IBase1`1<class BaseClass0> on type valuetype MyStruct244`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_15
		box valuetype MyStruct244`2<class BaseClass1,class BaseClass0>
		ldloc V_15
		box valuetype MyStruct244`2<class BaseClass1,class BaseClass0>
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(object)
		ldstr "MyStruct244::Method5.MI.1818()"
		ldstr "class IBase1`1<class BaseClass0> on type valuetype MyStruct244`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_15
		box valuetype MyStruct244`2<class BaseClass1,class BaseClass0>
		ldloc V_15
		box valuetype MyStruct244`2<class BaseClass1,class BaseClass0>
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(object)
		ldstr "MyStruct244::Method6.MI.1820<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type valuetype MyStruct244`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		.locals init (valuetype MyStruct244`2<class BaseClass1,class BaseClass1> V_16)
		ldloca V_16
		initobj valuetype MyStruct244`2<class BaseClass1,class BaseClass1>
		ldloc V_16
		box valuetype MyStruct244`2<class BaseClass1,class BaseClass1>
		ldloc V_16
		box valuetype MyStruct244`2<class BaseClass1,class BaseClass1>
		ldvirtftn instance string valuetype MyStruct244`2<class BaseClass1,class BaseClass1>::Method0()
		calli default string(object)
		ldstr "MyStruct244::Method0.1810()"
		ldstr "valuetype MyStruct244`2<class BaseClass1,class BaseClass1> on type valuetype MyStruct244`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_16
		box valuetype MyStruct244`2<class BaseClass1,class BaseClass1>
		ldloc V_16
		box valuetype MyStruct244`2<class BaseClass1,class BaseClass1>
		ldvirtftn instance string valuetype MyStruct244`2<class BaseClass1,class BaseClass1>::Method1()
		calli default string(object)
		ldstr "MyStruct244::Method1.1811()"
		ldstr "valuetype MyStruct244`2<class BaseClass1,class BaseClass1> on type valuetype MyStruct244`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_16
		box valuetype MyStruct244`2<class BaseClass1,class BaseClass1>
		ldloc V_16
		box valuetype MyStruct244`2<class BaseClass1,class BaseClass1>
		ldvirtftn instance string valuetype MyStruct244`2<class BaseClass1,class BaseClass1>::Method2<object>()
		calli default string(object)
		ldstr "MyStruct244::Method2.1813<System.Object>()"
		ldstr "valuetype MyStruct244`2<class BaseClass1,class BaseClass1> on type valuetype MyStruct244`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_16
		box valuetype MyStruct244`2<class BaseClass1,class BaseClass1>
		ldloc V_16
		box valuetype MyStruct244`2<class BaseClass1,class BaseClass1>
		ldvirtftn instance string valuetype MyStruct244`2<class BaseClass1,class BaseClass1>::Method3<object>()
		calli default string(object)
		ldstr "MyStruct244::Method3.1814<System.Object>()"
		ldstr "valuetype MyStruct244`2<class BaseClass1,class BaseClass1> on type valuetype MyStruct244`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_16
		box valuetype MyStruct244`2<class BaseClass1,class BaseClass1>
		ldloc V_16
		box valuetype MyStruct244`2<class BaseClass1,class BaseClass1>
		ldvirtftn instance string valuetype MyStruct244`2<class BaseClass1,class BaseClass1>::Method4()
		calli default string(object)
		ldstr "MyStruct244::Method4.1816()"
		ldstr "valuetype MyStruct244`2<class BaseClass1,class BaseClass1> on type valuetype MyStruct244`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_16
		box valuetype MyStruct244`2<class BaseClass1,class BaseClass1>
		ldloc V_16
		box valuetype MyStruct244`2<class BaseClass1,class BaseClass1>
		ldvirtftn instance string valuetype MyStruct244`2<class BaseClass1,class BaseClass1>::Method5()
		calli default string(object)
		ldstr "MyStruct244::Method5.1817()"
		ldstr "valuetype MyStruct244`2<class BaseClass1,class BaseClass1> on type valuetype MyStruct244`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_16
		box valuetype MyStruct244`2<class BaseClass1,class BaseClass1>
		ldloc V_16
		box valuetype MyStruct244`2<class BaseClass1,class BaseClass1>
		ldvirtftn instance string valuetype MyStruct244`2<class BaseClass1,class BaseClass1>::Method6<object>()
		calli default string(object)
		ldstr "MyStruct244::Method6.1819<System.Object>()"
		ldstr "valuetype MyStruct244`2<class BaseClass1,class BaseClass1> on type valuetype MyStruct244`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_16
		box valuetype MyStruct244`2<class BaseClass1,class BaseClass1>
		ldloc V_16
		box valuetype MyStruct244`2<class BaseClass1,class BaseClass1>
		ldvirtftn instance string valuetype MyStruct244`2<class BaseClass1,class BaseClass1>::ClassMethod492()
		calli default string(object)
		ldstr "MyStruct244::ClassMethod492.1821()"
		ldstr "valuetype MyStruct244`2<class BaseClass1,class BaseClass1> on type valuetype MyStruct244`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_16
		box valuetype MyStruct244`2<class BaseClass1,class BaseClass1>
		ldloc V_16
		box valuetype MyStruct244`2<class BaseClass1,class BaseClass1>
		ldvirtftn instance string valuetype MyStruct244`2<class BaseClass1,class BaseClass1>::ClassMethod493<object>()
		calli default string(object)
		ldstr "MyStruct244::ClassMethod493.1822<System.Object>()"
		ldstr "valuetype MyStruct244`2<class BaseClass1,class BaseClass1> on type valuetype MyStruct244`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_16
		box valuetype MyStruct244`2<class BaseClass1,class BaseClass1>
		ldloc V_16
		box valuetype MyStruct244`2<class BaseClass1,class BaseClass1>
		ldvirtftn instance string valuetype MyStruct244`2<class BaseClass1,class BaseClass1>::ClassMethod494<object>()
		calli default string(object)
		ldstr "MyStruct244::ClassMethod494.1823<System.Object>()"
		ldstr "valuetype MyStruct244`2<class BaseClass1,class BaseClass1> on type valuetype MyStruct244`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_16 box valuetype MyStruct244`2<class BaseClass1,class BaseClass1> ldnull 
		ldloc V_16 box valuetype MyStruct244`2<class BaseClass1,class BaseClass1>
		ldvirtftn instance bool valuetype MyStruct244`2<class BaseClass1,class BaseClass1>::Equals(object) calli default bool(object,object) pop
		ldloc V_16 box valuetype MyStruct244`2<class BaseClass1,class BaseClass1>
		ldloc V_16 box valuetype MyStruct244`2<class BaseClass1,class BaseClass1>
		ldvirtftn instance int32 valuetype MyStruct244`2<class BaseClass1,class BaseClass1>::GetHashCode() calli default int32(object) pop
		ldloc V_16 box valuetype MyStruct244`2<class BaseClass1,class BaseClass1>
		ldloc V_16 box valuetype MyStruct244`2<class BaseClass1,class BaseClass1>
		ldvirtftn instance string valuetype MyStruct244`2<class BaseClass1,class BaseClass1>::ToString() calli default string(object) pop
		ldloc V_16
		box valuetype MyStruct244`2<class BaseClass1,class BaseClass1>
		ldloc V_16
		box valuetype MyStruct244`2<class BaseClass1,class BaseClass1>
		ldvirtftn instance string IBase0::Method0()
		calli default string(object)
		ldstr "MyStruct244::Method0.1810()"
		ldstr "IBase0 on type valuetype MyStruct244`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_16
		box valuetype MyStruct244`2<class BaseClass1,class BaseClass1>
		ldloc V_16
		box valuetype MyStruct244`2<class BaseClass1,class BaseClass1>
		ldvirtftn instance string IBase0::Method1()
		calli default string(object)
		ldstr "MyStruct244::Method1.MI.1812()"
		ldstr "IBase0 on type valuetype MyStruct244`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_16
		box valuetype MyStruct244`2<class BaseClass1,class BaseClass1>
		ldloc V_16
		box valuetype MyStruct244`2<class BaseClass1,class BaseClass1>
		ldvirtftn instance string IBase0::Method2<object>()
		calli default string(object)
		ldstr "MyStruct244::Method2.1813<System.Object>()"
		ldstr "IBase0 on type valuetype MyStruct244`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_16
		box valuetype MyStruct244`2<class BaseClass1,class BaseClass1>
		ldloc V_16
		box valuetype MyStruct244`2<class BaseClass1,class BaseClass1>
		ldvirtftn instance string IBase0::Method3<object>()
		calli default string(object)
		ldstr "MyStruct244::Method3.MI.1815<System.Object>()"
		ldstr "IBase0 on type valuetype MyStruct244`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_16
		box valuetype MyStruct244`2<class BaseClass1,class BaseClass1>
		ldloc V_16
		box valuetype MyStruct244`2<class BaseClass1,class BaseClass1>
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(object)
		ldstr "MyStruct244::Method4.1816()"
		ldstr "class IBase1`1<class BaseClass1> on type valuetype MyStruct244`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_16
		box valuetype MyStruct244`2<class BaseClass1,class BaseClass1>
		ldloc V_16
		box valuetype MyStruct244`2<class BaseClass1,class BaseClass1>
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(object)
		ldstr "MyStruct244::Method5.MI.1818()"
		ldstr "class IBase1`1<class BaseClass1> on type valuetype MyStruct244`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_16
		box valuetype MyStruct244`2<class BaseClass1,class BaseClass1>
		ldloc V_16
		box valuetype MyStruct244`2<class BaseClass1,class BaseClass1>
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(object)
		ldstr "MyStruct244::Method6.MI.1820<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type valuetype MyStruct244`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_16
		box valuetype MyStruct244`2<class BaseClass1,class BaseClass1>
		ldloc V_16
		box valuetype MyStruct244`2<class BaseClass1,class BaseClass1>
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(object)
		ldstr "MyStruct244::Method4.1816()"
		ldstr "class IBase1`1<class BaseClass0> on type valuetype MyStruct244`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_16
		box valuetype MyStruct244`2<class BaseClass1,class BaseClass1>
		ldloc V_16
		box valuetype MyStruct244`2<class BaseClass1,class BaseClass1>
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(object)
		ldstr "MyStruct244::Method5.MI.1818()"
		ldstr "class IBase1`1<class BaseClass0> on type valuetype MyStruct244`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_16
		box valuetype MyStruct244`2<class BaseClass1,class BaseClass1>
		ldloc V_16
		box valuetype MyStruct244`2<class BaseClass1,class BaseClass1>
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(object)
		ldstr "MyStruct244::Method6.MI.1820<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type valuetype MyStruct244`2<class BaseClass1,class BaseClass1>"
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
