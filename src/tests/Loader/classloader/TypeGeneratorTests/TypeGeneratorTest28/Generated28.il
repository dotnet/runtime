// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern mscorlib { .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) .ver 4:0:0:0 }
.assembly extern TestFramework { .publickeytoken = ( B0 3F 5F 7F 11 D5 0A 3A )  }

//TYPES IN FORWARDER ASSEMBLIES:

//TEST ASSEMBLY:
.assembly Generated28 { .hash algorithm 0x00008004 }
.module Generated28.exe

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
.class public sequential sealed MyStruct78`1<T0> 
		extends [mscorlib]System.ValueType
		implements class IBase1`1<!T0>, IBase0 
{
	.pack 0
	.size 1
	.method public hidebysig virtual instance string Method4() cil managed noinlining { 
		ldstr "MyStruct78::Method4.630()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<T0>.Method4'() cil managed noinlining { 
		.override method instance string class IBase1`1<!T0>::Method4()
		ldstr "MyStruct78::Method4.MI.631()"
		ret
	}
	.method public hidebysig virtual instance string Method5() cil managed noinlining { 
		ldstr "MyStruct78::Method5.632()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "MyStruct78::Method6.633<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<T0>.Method6'<M0>() cil managed noinlining { 
		.override method instance string class IBase1`1<!T0>::Method6<[1]>()
		ldstr "MyStruct78::Method6.MI.634<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string Method0() cil managed noinlining { 
		ldstr "MyStruct78::Method0.635()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method1() cil managed noinlining { 
		ldstr "MyStruct78::Method1.636()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase0.Method1'() cil managed noinlining { 
		.override method instance string IBase0::Method1()
		ldstr "MyStruct78::Method1.MI.637()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method2<M0>() cil managed noinlining { 
		ldstr "MyStruct78::Method2.638<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase0.Method2'<M0>() cil managed noinlining { 
		.override method instance string IBase0::Method2<[1]>()
		ldstr "MyStruct78::Method2.MI.639<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig virtual instance string Method3<M0>() cil managed noinlining { 
		ldstr "MyStruct78::Method3.640<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase0.Method3'<M0>() cil managed noinlining { 
		.override method instance string IBase0::Method3<[1]>()
		ldstr "MyStruct78::Method3.MI.641<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot instance string ClassMethod158<M0>() cil managed noinlining { 
		ldstr "MyStruct78::ClassMethod158.642<"
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
.class interface public abstract IBase1`1<+T0> 
{
	.method public hidebysig newslot abstract virtual instance string Method4() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method5() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method6<M0>() cil managed { }
}
.class interface public abstract IBase0 
{
	.method public hidebysig newslot abstract virtual instance string Method0() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method1() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method2<M0>() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method3<M0>() cil managed { }
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
	.method static void M.MyStruct78.T<T0,(valuetype MyStruct78`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.MyStruct78.T<T0,(valuetype MyStruct78`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. valuetype MyStruct78`1<!!T0>
		callvirt     instance string class IBase1`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. valuetype MyStruct78`1<!!T0>
		callvirt     instance string class IBase1`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. valuetype MyStruct78`1<!!T0>
		callvirt     instance string class IBase1`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. valuetype MyStruct78`1<!!T0>
		callvirt     instance string IBase0::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. valuetype MyStruct78`1<!!T0>
		callvirt     instance string IBase0::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. valuetype MyStruct78`1<!!T0>
		callvirt     instance string IBase0::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. valuetype MyStruct78`1<!!T0>
		callvirt     instance string IBase0::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.MyStruct78.A<(valuetype MyStruct78`1<class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.MyStruct78.A<(valuetype MyStruct78`1<class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. valuetype MyStruct78`1<class BaseClass0>
		callvirt     instance string class IBase1`1<class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. valuetype MyStruct78`1<class BaseClass0>
		callvirt     instance string class IBase1`1<class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. valuetype MyStruct78`1<class BaseClass0>
		callvirt     instance string class IBase1`1<class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. valuetype MyStruct78`1<class BaseClass0>
		callvirt     instance string IBase0::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. valuetype MyStruct78`1<class BaseClass0>
		callvirt     instance string IBase0::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. valuetype MyStruct78`1<class BaseClass0>
		callvirt     instance string IBase0::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. valuetype MyStruct78`1<class BaseClass0>
		callvirt     instance string IBase0::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.MyStruct78.B<(valuetype MyStruct78`1<class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.MyStruct78.B<(valuetype MyStruct78`1<class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. valuetype MyStruct78`1<class BaseClass1>
		callvirt     instance string class IBase1`1<class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. valuetype MyStruct78`1<class BaseClass1>
		callvirt     instance string class IBase1`1<class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. valuetype MyStruct78`1<class BaseClass1>
		callvirt     instance string class IBase1`1<class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. valuetype MyStruct78`1<class BaseClass1>
		callvirt     instance string IBase0::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. valuetype MyStruct78`1<class BaseClass1>
		callvirt     instance string IBase0::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. valuetype MyStruct78`1<class BaseClass1>
		callvirt     instance string IBase0::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. valuetype MyStruct78`1<class BaseClass1>
		callvirt     instance string IBase0::Method3<object>()
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
		.locals init (valuetype MyStruct78`1<class BaseClass0> V_1)
		ldloca V_1
		initobj valuetype MyStruct78`1<class BaseClass0>
		ldloca V_1
		dup
		call instance string valuetype MyStruct78`1<class BaseClass0>::Method4()
		ldstr "MyStruct78::Method4.630()"
		ldstr "valuetype MyStruct78`1<class BaseClass0> on type MyStruct78"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype MyStruct78`1<class BaseClass0>::Method5()
		ldstr "MyStruct78::Method5.632()"
		ldstr "valuetype MyStruct78`1<class BaseClass0> on type MyStruct78"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype MyStruct78`1<class BaseClass0>::Method6<object>()
		ldstr "MyStruct78::Method6.633<System.Object>()"
		ldstr "valuetype MyStruct78`1<class BaseClass0> on type MyStruct78"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype MyStruct78`1<class BaseClass0>::Method0()
		ldstr "MyStruct78::Method0.635()"
		ldstr "valuetype MyStruct78`1<class BaseClass0> on type MyStruct78"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype MyStruct78`1<class BaseClass0>::Method1()
		ldstr "MyStruct78::Method1.636()"
		ldstr "valuetype MyStruct78`1<class BaseClass0> on type MyStruct78"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype MyStruct78`1<class BaseClass0>::Method2<object>()
		ldstr "MyStruct78::Method2.638<System.Object>()"
		ldstr "valuetype MyStruct78`1<class BaseClass0> on type MyStruct78"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype MyStruct78`1<class BaseClass0>::Method3<object>()
		ldstr "MyStruct78::Method3.640<System.Object>()"
		ldstr "valuetype MyStruct78`1<class BaseClass0> on type MyStruct78"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype MyStruct78`1<class BaseClass0>::ClassMethod158<object>()
		ldstr "MyStruct78::ClassMethod158.642<System.Object>()"
		ldstr "valuetype MyStruct78`1<class BaseClass0> on type MyStruct78"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup ldnull call instance bool valuetype MyStruct78`1<class BaseClass0>::Equals(object) pop
		dup call instance int32 valuetype MyStruct78`1<class BaseClass0>::GetHashCode() pop
		dup call instance string valuetype MyStruct78`1<class BaseClass0>::ToString() pop
		pop
		ldloc V_1
		box valuetype MyStruct78`1<class BaseClass0>
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "MyStruct78::Method4.MI.631()"
		ldstr "class IBase1`1<class BaseClass0> on type valuetype MyStruct78`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "MyStruct78::Method5.632()"
		ldstr "class IBase1`1<class BaseClass0> on type valuetype MyStruct78`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "MyStruct78::Method6.MI.634<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type valuetype MyStruct78`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc V_1
		box valuetype MyStruct78`1<class BaseClass0>
		dup
		callvirt instance string IBase0::Method0()
		ldstr "MyStruct78::Method0.635()"
		ldstr "IBase0 on type valuetype MyStruct78`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method1()
		ldstr "MyStruct78::Method1.MI.637()"
		ldstr "IBase0 on type valuetype MyStruct78`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method2<object>()
		ldstr "MyStruct78::Method2.MI.639<System.Object>()"
		ldstr "IBase0 on type valuetype MyStruct78`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method3<object>()
		ldstr "MyStruct78::Method3.MI.641<System.Object>()"
		ldstr "IBase0 on type valuetype MyStruct78`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		.locals init (valuetype MyStruct78`1<class BaseClass1> V_2)
		ldloca V_2
		initobj valuetype MyStruct78`1<class BaseClass1>
		ldloca V_2
		dup
		call instance string valuetype MyStruct78`1<class BaseClass1>::Method4()
		ldstr "MyStruct78::Method4.630()"
		ldstr "valuetype MyStruct78`1<class BaseClass1> on type MyStruct78"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype MyStruct78`1<class BaseClass1>::Method5()
		ldstr "MyStruct78::Method5.632()"
		ldstr "valuetype MyStruct78`1<class BaseClass1> on type MyStruct78"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype MyStruct78`1<class BaseClass1>::Method6<object>()
		ldstr "MyStruct78::Method6.633<System.Object>()"
		ldstr "valuetype MyStruct78`1<class BaseClass1> on type MyStruct78"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype MyStruct78`1<class BaseClass1>::Method0()
		ldstr "MyStruct78::Method0.635()"
		ldstr "valuetype MyStruct78`1<class BaseClass1> on type MyStruct78"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype MyStruct78`1<class BaseClass1>::Method1()
		ldstr "MyStruct78::Method1.636()"
		ldstr "valuetype MyStruct78`1<class BaseClass1> on type MyStruct78"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype MyStruct78`1<class BaseClass1>::Method2<object>()
		ldstr "MyStruct78::Method2.638<System.Object>()"
		ldstr "valuetype MyStruct78`1<class BaseClass1> on type MyStruct78"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype MyStruct78`1<class BaseClass1>::Method3<object>()
		ldstr "MyStruct78::Method3.640<System.Object>()"
		ldstr "valuetype MyStruct78`1<class BaseClass1> on type MyStruct78"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype MyStruct78`1<class BaseClass1>::ClassMethod158<object>()
		ldstr "MyStruct78::ClassMethod158.642<System.Object>()"
		ldstr "valuetype MyStruct78`1<class BaseClass1> on type MyStruct78"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup ldnull call instance bool valuetype MyStruct78`1<class BaseClass1>::Equals(object) pop
		dup call instance int32 valuetype MyStruct78`1<class BaseClass1>::GetHashCode() pop
		dup call instance string valuetype MyStruct78`1<class BaseClass1>::ToString() pop
		pop
		ldloc V_2
		box valuetype MyStruct78`1<class BaseClass1>
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "MyStruct78::Method4.MI.631()"
		ldstr "class IBase1`1<class BaseClass1> on type valuetype MyStruct78`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "MyStruct78::Method5.632()"
		ldstr "class IBase1`1<class BaseClass1> on type valuetype MyStruct78`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "MyStruct78::Method6.MI.634<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type valuetype MyStruct78`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc V_2
		box valuetype MyStruct78`1<class BaseClass1>
		dup
		callvirt instance string IBase0::Method0()
		ldstr "MyStruct78::Method0.635()"
		ldstr "IBase0 on type valuetype MyStruct78`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method1()
		ldstr "MyStruct78::Method1.MI.637()"
		ldstr "IBase0 on type valuetype MyStruct78`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method2<object>()
		ldstr "MyStruct78::Method2.MI.639<System.Object>()"
		ldstr "IBase0 on type valuetype MyStruct78`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method3<object>()
		ldstr "MyStruct78::Method3.MI.641<System.Object>()"
		ldstr "IBase0 on type valuetype MyStruct78`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc V_2
		box valuetype MyStruct78`1<class BaseClass1>
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "MyStruct78::Method4.MI.631()"
		ldstr "class IBase1`1<class BaseClass0> on type valuetype MyStruct78`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "MyStruct78::Method5.632()"
		ldstr "class IBase1`1<class BaseClass0> on type valuetype MyStruct78`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "MyStruct78::Method6.MI.634<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type valuetype MyStruct78`1<class BaseClass1>"
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
		.locals init (valuetype MyStruct78`1<class BaseClass0> V_3)
		ldloca V_3
		initobj valuetype MyStruct78`1<class BaseClass0>
		.try {  ldloc V_3
				ldstr "MyStruct78::Method4.MI.631()#MyStruct78::Method5.632()#MyStruct78::Method6.MI.634<System.Object>()#"
				call void Framework::M.IBase1.T<class BaseClass0,valuetype MyStruct78`1<class BaseClass0>>(!!1,string) leave.s LV0
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV0} LV0:
		.try {  ldloc V_3
				ldstr "MyStruct78::Method4.MI.631()#MyStruct78::Method5.632()#MyStruct78::Method6.MI.634<System.Object>()#"
				call void Framework::M.IBase1.A<valuetype MyStruct78`1<class BaseClass0>>(!!0,string) leave.s LV1
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV1} LV1:
		.try {  ldloc V_3
				ldstr "MyStruct78::Method0.635()#MyStruct78::Method1.MI.637()#MyStruct78::Method2.MI.639<System.Object>()#MyStruct78::Method3.MI.641<System.Object>()#"
				call void Framework::M.IBase0<valuetype MyStruct78`1<class BaseClass0>>(!!0,string) leave.s LV2
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV2} LV2:
		.locals init (valuetype MyStruct78`1<class BaseClass1> V_4)
		ldloca V_4
		initobj valuetype MyStruct78`1<class BaseClass1>
		.try {  ldloc V_4
				ldstr "MyStruct78::Method4.MI.631()#MyStruct78::Method5.632()#MyStruct78::Method6.MI.634<System.Object>()#"
				call void Framework::M.IBase1.T<class BaseClass1,valuetype MyStruct78`1<class BaseClass1>>(!!1,string) leave.s LV3
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV3} LV3:
		.try {  ldloc V_4
				ldstr "MyStruct78::Method4.MI.631()#MyStruct78::Method5.632()#MyStruct78::Method6.MI.634<System.Object>()#"
				call void Framework::M.IBase1.B<valuetype MyStruct78`1<class BaseClass1>>(!!0,string) leave.s LV4
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV4} LV4:
		.try {  ldloc V_4
				ldstr "MyStruct78::Method0.635()#MyStruct78::Method1.MI.637()#MyStruct78::Method2.MI.639<System.Object>()#MyStruct78::Method3.MI.641<System.Object>()#"
				call void Framework::M.IBase0<valuetype MyStruct78`1<class BaseClass1>>(!!0,string) leave.s LV5
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV5} LV5:
		.try {  ldloc V_4
				ldstr "MyStruct78::Method4.MI.631()#MyStruct78::Method5.632()#MyStruct78::Method6.MI.634<System.Object>()#"
				call void Framework::M.IBase1.T<class BaseClass0,valuetype MyStruct78`1<class BaseClass1>>(!!1,string) leave.s LV6
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV6} LV6:
		.try {  ldloc V_4
				ldstr "MyStruct78::Method4.MI.631()#MyStruct78::Method5.632()#MyStruct78::Method6.MI.634<System.Object>()#"
				call void Framework::M.IBase1.A<valuetype MyStruct78`1<class BaseClass1>>(!!0,string) leave.s LV7
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV7} LV7:
		ldstr "========================================================================\n\n"
		call void [mscorlib]System.Console::WriteLine(string)
		ret
	}
	.method public hidebysig static void StructConstrainedInterfaceCallsTest() cil managed
	{
		.maxstack  10
		ldstr "===================== Struct Constrained Interface Calls Test ====================="
		call void [mscorlib]System.Console::WriteLine(string)
		.locals init (valuetype MyStruct78`1<class BaseClass0> V_5)
		ldloca V_5
		initobj valuetype MyStruct78`1<class BaseClass0>
		.try {  ldloc V_5
				ldstr "MyStruct78::Method4.MI.631()#" +
				      "MyStruct78::Method5.632()#" +
				      "MyStruct78::Method6.MI.634<System.Object>()#" +
				      "MyStruct78::Method0.635()#" +
				      "MyStruct78::Method1.MI.637()#" +
				      "MyStruct78::Method2.MI.639<System.Object>()#" +
				      "MyStruct78::Method3.MI.641<System.Object>()#"
				call void Framework::M.MyStruct78.T<class BaseClass0,valuetype MyStruct78`1<class BaseClass0>>(!!1,string) leave.s LV0
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV0} LV0:
		.try {  ldloc V_5
				ldstr "MyStruct78::Method4.MI.631()#" +
				      "MyStruct78::Method5.632()#" +
				      "MyStruct78::Method6.MI.634<System.Object>()#" +
				      "MyStruct78::Method0.635()#" +
				      "MyStruct78::Method1.MI.637()#" +
				      "MyStruct78::Method2.MI.639<System.Object>()#" +
				      "MyStruct78::Method3.MI.641<System.Object>()#"
				call void Framework::M.MyStruct78.A<valuetype MyStruct78`1<class BaseClass0>>(!!0,string) leave.s LV1
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV1} LV1:
		.locals init (valuetype MyStruct78`1<class BaseClass1> V_6)
		ldloca V_6
		initobj valuetype MyStruct78`1<class BaseClass1>
		.try {  ldloc V_6
				ldstr "MyStruct78::Method4.MI.631()#" +
				      "MyStruct78::Method5.632()#" +
				      "MyStruct78::Method6.MI.634<System.Object>()#" +
				      "MyStruct78::Method0.635()#" +
				      "MyStruct78::Method1.MI.637()#" +
				      "MyStruct78::Method2.MI.639<System.Object>()#" +
				      "MyStruct78::Method3.MI.641<System.Object>()#"
				call void Framework::M.MyStruct78.T<class BaseClass1,valuetype MyStruct78`1<class BaseClass1>>(!!1,string) leave.s LV2
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV2} LV2:
		.try {  ldloc V_6
				ldstr "MyStruct78::Method4.MI.631()#" +
				      "MyStruct78::Method5.632()#" +
				      "MyStruct78::Method6.MI.634<System.Object>()#" +
				      "MyStruct78::Method0.635()#" +
				      "MyStruct78::Method1.MI.637()#" +
				      "MyStruct78::Method2.MI.639<System.Object>()#" +
				      "MyStruct78::Method3.MI.641<System.Object>()#"
				call void Framework::M.MyStruct78.B<valuetype MyStruct78`1<class BaseClass1>>(!!0,string) leave.s LV3
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV3} LV3:
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
		.locals init (valuetype MyStruct78`1<class BaseClass0> V_7)
		ldloca V_7
		initobj valuetype MyStruct78`1<class BaseClass0>
		ldloc V_7
		box valuetype MyStruct78`1<class BaseClass0>
		ldloc V_7
		box valuetype MyStruct78`1<class BaseClass0>
		ldvirtftn instance string valuetype MyStruct78`1<class BaseClass0>::Method4()
		calli default string(object)
		ldstr "MyStruct78::Method4.630()"
		ldstr "valuetype MyStruct78`1<class BaseClass0> on type valuetype MyStruct78`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_7
		box valuetype MyStruct78`1<class BaseClass0>
		ldloc V_7
		box valuetype MyStruct78`1<class BaseClass0>
		ldvirtftn instance string valuetype MyStruct78`1<class BaseClass0>::Method5()
		calli default string(object)
		ldstr "MyStruct78::Method5.632()"
		ldstr "valuetype MyStruct78`1<class BaseClass0> on type valuetype MyStruct78`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_7
		box valuetype MyStruct78`1<class BaseClass0>
		ldloc V_7
		box valuetype MyStruct78`1<class BaseClass0>
		ldvirtftn instance string valuetype MyStruct78`1<class BaseClass0>::Method6<object>()
		calli default string(object)
		ldstr "MyStruct78::Method6.633<System.Object>()"
		ldstr "valuetype MyStruct78`1<class BaseClass0> on type valuetype MyStruct78`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_7
		box valuetype MyStruct78`1<class BaseClass0>
		ldloc V_7
		box valuetype MyStruct78`1<class BaseClass0>
		ldvirtftn instance string valuetype MyStruct78`1<class BaseClass0>::Method0()
		calli default string(object)
		ldstr "MyStruct78::Method0.635()"
		ldstr "valuetype MyStruct78`1<class BaseClass0> on type valuetype MyStruct78`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_7
		box valuetype MyStruct78`1<class BaseClass0>
		ldloc V_7
		box valuetype MyStruct78`1<class BaseClass0>
		ldvirtftn instance string valuetype MyStruct78`1<class BaseClass0>::Method1()
		calli default string(object)
		ldstr "MyStruct78::Method1.636()"
		ldstr "valuetype MyStruct78`1<class BaseClass0> on type valuetype MyStruct78`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_7
		box valuetype MyStruct78`1<class BaseClass0>
		ldloc V_7
		box valuetype MyStruct78`1<class BaseClass0>
		ldvirtftn instance string valuetype MyStruct78`1<class BaseClass0>::Method2<object>()
		calli default string(object)
		ldstr "MyStruct78::Method2.638<System.Object>()"
		ldstr "valuetype MyStruct78`1<class BaseClass0> on type valuetype MyStruct78`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_7
		box valuetype MyStruct78`1<class BaseClass0>
		ldloc V_7
		box valuetype MyStruct78`1<class BaseClass0>
		ldvirtftn instance string valuetype MyStruct78`1<class BaseClass0>::Method3<object>()
		calli default string(object)
		ldstr "MyStruct78::Method3.640<System.Object>()"
		ldstr "valuetype MyStruct78`1<class BaseClass0> on type valuetype MyStruct78`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_7
		box valuetype MyStruct78`1<class BaseClass0>
		ldloc V_7
		box valuetype MyStruct78`1<class BaseClass0>
		ldvirtftn instance string valuetype MyStruct78`1<class BaseClass0>::ClassMethod158<object>()
		calli default string(object)
		ldstr "MyStruct78::ClassMethod158.642<System.Object>()"
		ldstr "valuetype MyStruct78`1<class BaseClass0> on type valuetype MyStruct78`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_7 box valuetype MyStruct78`1<class BaseClass0> ldnull 
		ldloc V_7 box valuetype MyStruct78`1<class BaseClass0>
		ldvirtftn instance bool valuetype MyStruct78`1<class BaseClass0>::Equals(object) calli default bool(object,object) pop
		ldloc V_7 box valuetype MyStruct78`1<class BaseClass0>
		ldloc V_7 box valuetype MyStruct78`1<class BaseClass0>
		ldvirtftn instance int32 valuetype MyStruct78`1<class BaseClass0>::GetHashCode() calli default int32(object) pop
		ldloc V_7 box valuetype MyStruct78`1<class BaseClass0>
		ldloc V_7 box valuetype MyStruct78`1<class BaseClass0>
		ldvirtftn instance string valuetype MyStruct78`1<class BaseClass0>::ToString() calli default string(object) pop
		ldloc V_7
		box valuetype MyStruct78`1<class BaseClass0>
		ldloc V_7
		box valuetype MyStruct78`1<class BaseClass0>
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(object)
		ldstr "MyStruct78::Method4.MI.631()"
		ldstr "class IBase1`1<class BaseClass0> on type valuetype MyStruct78`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_7
		box valuetype MyStruct78`1<class BaseClass0>
		ldloc V_7
		box valuetype MyStruct78`1<class BaseClass0>
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(object)
		ldstr "MyStruct78::Method5.632()"
		ldstr "class IBase1`1<class BaseClass0> on type valuetype MyStruct78`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_7
		box valuetype MyStruct78`1<class BaseClass0>
		ldloc V_7
		box valuetype MyStruct78`1<class BaseClass0>
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(object)
		ldstr "MyStruct78::Method6.MI.634<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type valuetype MyStruct78`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_7
		box valuetype MyStruct78`1<class BaseClass0>
		ldloc V_7
		box valuetype MyStruct78`1<class BaseClass0>
		ldvirtftn instance string IBase0::Method0()
		calli default string(object)
		ldstr "MyStruct78::Method0.635()"
		ldstr "IBase0 on type valuetype MyStruct78`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_7
		box valuetype MyStruct78`1<class BaseClass0>
		ldloc V_7
		box valuetype MyStruct78`1<class BaseClass0>
		ldvirtftn instance string IBase0::Method1()
		calli default string(object)
		ldstr "MyStruct78::Method1.MI.637()"
		ldstr "IBase0 on type valuetype MyStruct78`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_7
		box valuetype MyStruct78`1<class BaseClass0>
		ldloc V_7
		box valuetype MyStruct78`1<class BaseClass0>
		ldvirtftn instance string IBase0::Method2<object>()
		calli default string(object)
		ldstr "MyStruct78::Method2.MI.639<System.Object>()"
		ldstr "IBase0 on type valuetype MyStruct78`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_7
		box valuetype MyStruct78`1<class BaseClass0>
		ldloc V_7
		box valuetype MyStruct78`1<class BaseClass0>
		ldvirtftn instance string IBase0::Method3<object>()
		calli default string(object)
		ldstr "MyStruct78::Method3.MI.641<System.Object>()"
		ldstr "IBase0 on type valuetype MyStruct78`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		.locals init (valuetype MyStruct78`1<class BaseClass1> V_8)
		ldloca V_8
		initobj valuetype MyStruct78`1<class BaseClass1>
		ldloc V_8
		box valuetype MyStruct78`1<class BaseClass1>
		ldloc V_8
		box valuetype MyStruct78`1<class BaseClass1>
		ldvirtftn instance string valuetype MyStruct78`1<class BaseClass1>::Method4()
		calli default string(object)
		ldstr "MyStruct78::Method4.630()"
		ldstr "valuetype MyStruct78`1<class BaseClass1> on type valuetype MyStruct78`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_8
		box valuetype MyStruct78`1<class BaseClass1>
		ldloc V_8
		box valuetype MyStruct78`1<class BaseClass1>
		ldvirtftn instance string valuetype MyStruct78`1<class BaseClass1>::Method5()
		calli default string(object)
		ldstr "MyStruct78::Method5.632()"
		ldstr "valuetype MyStruct78`1<class BaseClass1> on type valuetype MyStruct78`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_8
		box valuetype MyStruct78`1<class BaseClass1>
		ldloc V_8
		box valuetype MyStruct78`1<class BaseClass1>
		ldvirtftn instance string valuetype MyStruct78`1<class BaseClass1>::Method6<object>()
		calli default string(object)
		ldstr "MyStruct78::Method6.633<System.Object>()"
		ldstr "valuetype MyStruct78`1<class BaseClass1> on type valuetype MyStruct78`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_8
		box valuetype MyStruct78`1<class BaseClass1>
		ldloc V_8
		box valuetype MyStruct78`1<class BaseClass1>
		ldvirtftn instance string valuetype MyStruct78`1<class BaseClass1>::Method0()
		calli default string(object)
		ldstr "MyStruct78::Method0.635()"
		ldstr "valuetype MyStruct78`1<class BaseClass1> on type valuetype MyStruct78`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_8
		box valuetype MyStruct78`1<class BaseClass1>
		ldloc V_8
		box valuetype MyStruct78`1<class BaseClass1>
		ldvirtftn instance string valuetype MyStruct78`1<class BaseClass1>::Method1()
		calli default string(object)
		ldstr "MyStruct78::Method1.636()"
		ldstr "valuetype MyStruct78`1<class BaseClass1> on type valuetype MyStruct78`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_8
		box valuetype MyStruct78`1<class BaseClass1>
		ldloc V_8
		box valuetype MyStruct78`1<class BaseClass1>
		ldvirtftn instance string valuetype MyStruct78`1<class BaseClass1>::Method2<object>()
		calli default string(object)
		ldstr "MyStruct78::Method2.638<System.Object>()"
		ldstr "valuetype MyStruct78`1<class BaseClass1> on type valuetype MyStruct78`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_8
		box valuetype MyStruct78`1<class BaseClass1>
		ldloc V_8
		box valuetype MyStruct78`1<class BaseClass1>
		ldvirtftn instance string valuetype MyStruct78`1<class BaseClass1>::Method3<object>()
		calli default string(object)
		ldstr "MyStruct78::Method3.640<System.Object>()"
		ldstr "valuetype MyStruct78`1<class BaseClass1> on type valuetype MyStruct78`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_8
		box valuetype MyStruct78`1<class BaseClass1>
		ldloc V_8
		box valuetype MyStruct78`1<class BaseClass1>
		ldvirtftn instance string valuetype MyStruct78`1<class BaseClass1>::ClassMethod158<object>()
		calli default string(object)
		ldstr "MyStruct78::ClassMethod158.642<System.Object>()"
		ldstr "valuetype MyStruct78`1<class BaseClass1> on type valuetype MyStruct78`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_8 box valuetype MyStruct78`1<class BaseClass1> ldnull 
		ldloc V_8 box valuetype MyStruct78`1<class BaseClass1>
		ldvirtftn instance bool valuetype MyStruct78`1<class BaseClass1>::Equals(object) calli default bool(object,object) pop
		ldloc V_8 box valuetype MyStruct78`1<class BaseClass1>
		ldloc V_8 box valuetype MyStruct78`1<class BaseClass1>
		ldvirtftn instance int32 valuetype MyStruct78`1<class BaseClass1>::GetHashCode() calli default int32(object) pop
		ldloc V_8 box valuetype MyStruct78`1<class BaseClass1>
		ldloc V_8 box valuetype MyStruct78`1<class BaseClass1>
		ldvirtftn instance string valuetype MyStruct78`1<class BaseClass1>::ToString() calli default string(object) pop
		ldloc V_8
		box valuetype MyStruct78`1<class BaseClass1>
		ldloc V_8
		box valuetype MyStruct78`1<class BaseClass1>
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(object)
		ldstr "MyStruct78::Method4.MI.631()"
		ldstr "class IBase1`1<class BaseClass1> on type valuetype MyStruct78`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_8
		box valuetype MyStruct78`1<class BaseClass1>
		ldloc V_8
		box valuetype MyStruct78`1<class BaseClass1>
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(object)
		ldstr "MyStruct78::Method5.632()"
		ldstr "class IBase1`1<class BaseClass1> on type valuetype MyStruct78`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_8
		box valuetype MyStruct78`1<class BaseClass1>
		ldloc V_8
		box valuetype MyStruct78`1<class BaseClass1>
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(object)
		ldstr "MyStruct78::Method6.MI.634<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type valuetype MyStruct78`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_8
		box valuetype MyStruct78`1<class BaseClass1>
		ldloc V_8
		box valuetype MyStruct78`1<class BaseClass1>
		ldvirtftn instance string IBase0::Method0()
		calli default string(object)
		ldstr "MyStruct78::Method0.635()"
		ldstr "IBase0 on type valuetype MyStruct78`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_8
		box valuetype MyStruct78`1<class BaseClass1>
		ldloc V_8
		box valuetype MyStruct78`1<class BaseClass1>
		ldvirtftn instance string IBase0::Method1()
		calli default string(object)
		ldstr "MyStruct78::Method1.MI.637()"
		ldstr "IBase0 on type valuetype MyStruct78`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_8
		box valuetype MyStruct78`1<class BaseClass1>
		ldloc V_8
		box valuetype MyStruct78`1<class BaseClass1>
		ldvirtftn instance string IBase0::Method2<object>()
		calli default string(object)
		ldstr "MyStruct78::Method2.MI.639<System.Object>()"
		ldstr "IBase0 on type valuetype MyStruct78`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_8
		box valuetype MyStruct78`1<class BaseClass1>
		ldloc V_8
		box valuetype MyStruct78`1<class BaseClass1>
		ldvirtftn instance string IBase0::Method3<object>()
		calli default string(object)
		ldstr "MyStruct78::Method3.MI.641<System.Object>()"
		ldstr "IBase0 on type valuetype MyStruct78`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_8
		box valuetype MyStruct78`1<class BaseClass1>
		ldloc V_8
		box valuetype MyStruct78`1<class BaseClass1>
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(object)
		ldstr "MyStruct78::Method4.MI.631()"
		ldstr "class IBase1`1<class BaseClass0> on type valuetype MyStruct78`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_8
		box valuetype MyStruct78`1<class BaseClass1>
		ldloc V_8
		box valuetype MyStruct78`1<class BaseClass1>
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(object)
		ldstr "MyStruct78::Method5.632()"
		ldstr "class IBase1`1<class BaseClass0> on type valuetype MyStruct78`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_8
		box valuetype MyStruct78`1<class BaseClass1>
		ldloc V_8
		box valuetype MyStruct78`1<class BaseClass1>
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(object)
		ldstr "MyStruct78::Method6.MI.634<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type valuetype MyStruct78`1<class BaseClass1>"
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
