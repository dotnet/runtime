// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern mscorlib { .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) .ver 4:0:0:0 }
.assembly extern TestFramework { .publickeytoken = ( B0 3F 5F 7F 11 D5 0A 3A )  }

//TYPES IN FORWARDER ASSEMBLIES:

//TEST ASSEMBLY:
.assembly Generated46 { .hash algorithm 0x00008004 }
.module Generated46.exe

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
.class public sequential sealed MyStruct96`1<T0> 
		extends [mscorlib]System.ValueType
		implements class IBase1`1<class BaseClass1>, class IBase2`2<class BaseClass0,!T0> 
{
	.pack 0
	.size 1
	.method public hidebysig newslot virtual instance string Method4() cil managed noinlining { 
		ldstr "MyStruct96::Method4.813()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method5() cil managed noinlining { 
		ldstr "MyStruct96::Method5.814()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class BaseClass1>.Method5'() cil managed noinlining { 
		.override method instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "MyStruct96::Method5.MI.815()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "MyStruct96::Method6.816<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class BaseClass1>.Method6'<M0>() cil managed noinlining { 
		.override method instance string class IBase1`1<class BaseClass1>::Method6<[1]>()
		ldstr "MyStruct96::Method6.MI.817<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "MyStruct96::Method7.818<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot instance string ClassMethod188<M0>() cil managed noinlining { 
		ldstr "MyStruct96::ClassMethod188.819<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot instance string ClassMethod189<M0>() cil managed noinlining { 
		ldstr "MyStruct96::ClassMethod189.820<"
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
.class interface public abstract IBase2`2<+T0, -T1> 
{
	.method public hidebysig newslot abstract virtual instance string Method7<M0>() cil managed { }
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
	.method static void M.MyStruct96.T<T0,(valuetype MyStruct96`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 9
		.locals init (string[] actualResults)
		ldc.i4.s    4
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.MyStruct96.T<T0,(valuetype MyStruct96`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 4
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. valuetype MyStruct96`1<!!T0>
		callvirt     instance string class IBase1`1<class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. valuetype MyStruct96`1<!!T0>
		callvirt     instance string class IBase1`1<class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. valuetype MyStruct96`1<!!T0>
		callvirt     instance string class IBase1`1<class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. valuetype MyStruct96`1<!!T0>
		callvirt     instance string class IBase2`2<class BaseClass0,!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.MyStruct96.A<(valuetype MyStruct96`1<class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 9
		.locals init (string[] actualResults)
		ldc.i4.s    4
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.MyStruct96.A<(valuetype MyStruct96`1<class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 4
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. valuetype MyStruct96`1<class BaseClass0>
		callvirt     instance string class IBase1`1<class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. valuetype MyStruct96`1<class BaseClass0>
		callvirt     instance string class IBase1`1<class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. valuetype MyStruct96`1<class BaseClass0>
		callvirt     instance string class IBase1`1<class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. valuetype MyStruct96`1<class BaseClass0>
		callvirt     instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.MyStruct96.B<(valuetype MyStruct96`1<class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 9
		.locals init (string[] actualResults)
		ldc.i4.s    4
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.MyStruct96.B<(valuetype MyStruct96`1<class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 4
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. valuetype MyStruct96`1<class BaseClass1>
		callvirt     instance string class IBase1`1<class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. valuetype MyStruct96`1<class BaseClass1>
		callvirt     instance string class IBase1`1<class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. valuetype MyStruct96`1<class BaseClass1>
		callvirt     instance string class IBase1`1<class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. valuetype MyStruct96`1<class BaseClass1>
		callvirt     instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
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
		.locals init (valuetype MyStruct96`1<class BaseClass0> V_1)
		ldloca V_1
		initobj valuetype MyStruct96`1<class BaseClass0>
		ldloca V_1
		dup
		call instance string valuetype MyStruct96`1<class BaseClass0>::Method4()
		ldstr "MyStruct96::Method4.813()"
		ldstr "valuetype MyStruct96`1<class BaseClass0> on type MyStruct96"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype MyStruct96`1<class BaseClass0>::Method5()
		ldstr "MyStruct96::Method5.814()"
		ldstr "valuetype MyStruct96`1<class BaseClass0> on type MyStruct96"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype MyStruct96`1<class BaseClass0>::Method6<object>()
		ldstr "MyStruct96::Method6.816<System.Object>()"
		ldstr "valuetype MyStruct96`1<class BaseClass0> on type MyStruct96"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype MyStruct96`1<class BaseClass0>::Method7<object>()
		ldstr "MyStruct96::Method7.818<System.Object>()"
		ldstr "valuetype MyStruct96`1<class BaseClass0> on type MyStruct96"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype MyStruct96`1<class BaseClass0>::ClassMethod188<object>()
		ldstr "MyStruct96::ClassMethod188.819<System.Object>()"
		ldstr "valuetype MyStruct96`1<class BaseClass0> on type MyStruct96"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype MyStruct96`1<class BaseClass0>::ClassMethod189<object>()
		ldstr "MyStruct96::ClassMethod189.820<System.Object>()"
		ldstr "valuetype MyStruct96`1<class BaseClass0> on type MyStruct96"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup ldnull call instance bool valuetype MyStruct96`1<class BaseClass0>::Equals(object) pop
		dup call instance int32 valuetype MyStruct96`1<class BaseClass0>::GetHashCode() pop
		dup call instance string valuetype MyStruct96`1<class BaseClass0>::ToString() pop
		pop
		ldloc V_1
		box valuetype MyStruct96`1<class BaseClass0>
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "MyStruct96::Method4.813()"
		ldstr "class IBase1`1<class BaseClass1> on type valuetype MyStruct96`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "MyStruct96::Method5.MI.815()"
		ldstr "class IBase1`1<class BaseClass1> on type valuetype MyStruct96`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "MyStruct96::Method6.MI.817<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type valuetype MyStruct96`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc V_1
		box valuetype MyStruct96`1<class BaseClass0>
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "MyStruct96::Method7.818<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type valuetype MyStruct96`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc V_1
		box valuetype MyStruct96`1<class BaseClass0>
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "MyStruct96::Method4.813()"
		ldstr "class IBase1`1<class BaseClass0> on type valuetype MyStruct96`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "MyStruct96::Method5.MI.815()"
		ldstr "class IBase1`1<class BaseClass0> on type valuetype MyStruct96`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "MyStruct96::Method6.MI.817<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type valuetype MyStruct96`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc V_1
		box valuetype MyStruct96`1<class BaseClass0>
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "MyStruct96::Method7.818<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type valuetype MyStruct96`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		.locals init (valuetype MyStruct96`1<class BaseClass1> V_2)
		ldloca V_2
		initobj valuetype MyStruct96`1<class BaseClass1>
		ldloca V_2
		dup
		call instance string valuetype MyStruct96`1<class BaseClass1>::Method4()
		ldstr "MyStruct96::Method4.813()"
		ldstr "valuetype MyStruct96`1<class BaseClass1> on type MyStruct96"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype MyStruct96`1<class BaseClass1>::Method5()
		ldstr "MyStruct96::Method5.814()"
		ldstr "valuetype MyStruct96`1<class BaseClass1> on type MyStruct96"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype MyStruct96`1<class BaseClass1>::Method6<object>()
		ldstr "MyStruct96::Method6.816<System.Object>()"
		ldstr "valuetype MyStruct96`1<class BaseClass1> on type MyStruct96"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype MyStruct96`1<class BaseClass1>::Method7<object>()
		ldstr "MyStruct96::Method7.818<System.Object>()"
		ldstr "valuetype MyStruct96`1<class BaseClass1> on type MyStruct96"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype MyStruct96`1<class BaseClass1>::ClassMethod188<object>()
		ldstr "MyStruct96::ClassMethod188.819<System.Object>()"
		ldstr "valuetype MyStruct96`1<class BaseClass1> on type MyStruct96"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype MyStruct96`1<class BaseClass1>::ClassMethod189<object>()
		ldstr "MyStruct96::ClassMethod189.820<System.Object>()"
		ldstr "valuetype MyStruct96`1<class BaseClass1> on type MyStruct96"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup ldnull call instance bool valuetype MyStruct96`1<class BaseClass1>::Equals(object) pop
		dup call instance int32 valuetype MyStruct96`1<class BaseClass1>::GetHashCode() pop
		dup call instance string valuetype MyStruct96`1<class BaseClass1>::ToString() pop
		pop
		ldloc V_2
		box valuetype MyStruct96`1<class BaseClass1>
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "MyStruct96::Method4.813()"
		ldstr "class IBase1`1<class BaseClass1> on type valuetype MyStruct96`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "MyStruct96::Method5.MI.815()"
		ldstr "class IBase1`1<class BaseClass1> on type valuetype MyStruct96`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "MyStruct96::Method6.MI.817<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type valuetype MyStruct96`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc V_2
		box valuetype MyStruct96`1<class BaseClass1>
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "MyStruct96::Method7.818<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type valuetype MyStruct96`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc V_2
		box valuetype MyStruct96`1<class BaseClass1>
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "MyStruct96::Method4.813()"
		ldstr "class IBase1`1<class BaseClass0> on type valuetype MyStruct96`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "MyStruct96::Method5.MI.815()"
		ldstr "class IBase1`1<class BaseClass0> on type valuetype MyStruct96`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "MyStruct96::Method6.MI.817<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type valuetype MyStruct96`1<class BaseClass1>"
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
		.locals init (valuetype MyStruct96`1<class BaseClass0> V_3)
		ldloca V_3
		initobj valuetype MyStruct96`1<class BaseClass0>
		.try {  ldloc V_3
				ldstr "MyStruct96::Method4.813()#MyStruct96::Method5.MI.815()#MyStruct96::Method6.MI.817<System.Object>()#"
				call void Framework::M.IBase1.T<class BaseClass1,valuetype MyStruct96`1<class BaseClass0>>(!!1,string) leave.s LV0
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV0} LV0:
		.try {  ldloc V_3
				ldstr "MyStruct96::Method4.813()#MyStruct96::Method5.MI.815()#MyStruct96::Method6.MI.817<System.Object>()#"
				call void Framework::M.IBase1.B<valuetype MyStruct96`1<class BaseClass0>>(!!0,string) leave.s LV1
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV1} LV1:
		.try {  ldloc V_3
				ldstr "MyStruct96::Method7.818<System.Object>()#"
				call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass0,valuetype MyStruct96`1<class BaseClass0>>(!!2,string) leave.s LV2
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV2} LV2:
		.try {  ldloc V_3
				ldstr "MyStruct96::Method7.818<System.Object>()#"
				call void Framework::M.IBase2.A.T<class BaseClass0,valuetype MyStruct96`1<class BaseClass0>>(!!1,string) leave.s LV3
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV3} LV3:
		.try {  ldloc V_3
				ldstr "MyStruct96::Method7.818<System.Object>()#"
				call void Framework::M.IBase2.A.A<valuetype MyStruct96`1<class BaseClass0>>(!!0,string) leave.s LV4
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV4} LV4:
		.try {  ldloc V_3
				ldstr "MyStruct96::Method4.813()#MyStruct96::Method5.MI.815()#MyStruct96::Method6.MI.817<System.Object>()#"
				call void Framework::M.IBase1.T<class BaseClass0,valuetype MyStruct96`1<class BaseClass0>>(!!1,string) leave.s LV5
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV5} LV5:
		.try {  ldloc V_3
				ldstr "MyStruct96::Method4.813()#MyStruct96::Method5.MI.815()#MyStruct96::Method6.MI.817<System.Object>()#"
				call void Framework::M.IBase1.A<valuetype MyStruct96`1<class BaseClass0>>(!!0,string) leave.s LV6
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV6} LV6:
		.try {  ldloc V_3
				ldstr "MyStruct96::Method7.818<System.Object>()#"
				call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,valuetype MyStruct96`1<class BaseClass0>>(!!2,string) leave.s LV7
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV7} LV7:
		.try {  ldloc V_3
				ldstr "MyStruct96::Method7.818<System.Object>()#"
				call void Framework::M.IBase2.A.T<class BaseClass1,valuetype MyStruct96`1<class BaseClass0>>(!!1,string) leave.s LV8
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV8} LV8:
		.try {  ldloc V_3
				ldstr "MyStruct96::Method7.818<System.Object>()#"
				call void Framework::M.IBase2.A.B<valuetype MyStruct96`1<class BaseClass0>>(!!0,string) leave.s LV9
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV9} LV9:
		.locals init (valuetype MyStruct96`1<class BaseClass1> V_4)
		ldloca V_4
		initobj valuetype MyStruct96`1<class BaseClass1>
		.try {  ldloc V_4
				ldstr "MyStruct96::Method4.813()#MyStruct96::Method5.MI.815()#MyStruct96::Method6.MI.817<System.Object>()#"
				call void Framework::M.IBase1.T<class BaseClass1,valuetype MyStruct96`1<class BaseClass1>>(!!1,string) leave.s LV10
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV10} LV10:
		.try {  ldloc V_4
				ldstr "MyStruct96::Method4.813()#MyStruct96::Method5.MI.815()#MyStruct96::Method6.MI.817<System.Object>()#"
				call void Framework::M.IBase1.B<valuetype MyStruct96`1<class BaseClass1>>(!!0,string) leave.s LV11
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV11} LV11:
		.try {  ldloc V_4
				ldstr "MyStruct96::Method7.818<System.Object>()#"
				call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,valuetype MyStruct96`1<class BaseClass1>>(!!2,string) leave.s LV12
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV12} LV12:
		.try {  ldloc V_4
				ldstr "MyStruct96::Method7.818<System.Object>()#"
				call void Framework::M.IBase2.A.T<class BaseClass1,valuetype MyStruct96`1<class BaseClass1>>(!!1,string) leave.s LV13
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV13} LV13:
		.try {  ldloc V_4
				ldstr "MyStruct96::Method7.818<System.Object>()#"
				call void Framework::M.IBase2.A.B<valuetype MyStruct96`1<class BaseClass1>>(!!0,string) leave.s LV14
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV14} LV14:
		.try {  ldloc V_4
				ldstr "MyStruct96::Method4.813()#MyStruct96::Method5.MI.815()#MyStruct96::Method6.MI.817<System.Object>()#"
				call void Framework::M.IBase1.T<class BaseClass0,valuetype MyStruct96`1<class BaseClass1>>(!!1,string) leave.s LV15
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV15} LV15:
		.try {  ldloc V_4
				ldstr "MyStruct96::Method4.813()#MyStruct96::Method5.MI.815()#MyStruct96::Method6.MI.817<System.Object>()#"
				call void Framework::M.IBase1.A<valuetype MyStruct96`1<class BaseClass1>>(!!0,string) leave.s LV16
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV16} LV16:
		ldstr "========================================================================\n\n"
		call void [mscorlib]System.Console::WriteLine(string)
		ret
	}
	.method public hidebysig static void StructConstrainedInterfaceCallsTest() cil managed
	{
		.maxstack  10
		ldstr "===================== Struct Constrained Interface Calls Test ====================="
		call void [mscorlib]System.Console::WriteLine(string)
		.locals init (valuetype MyStruct96`1<class BaseClass0> V_5)
		ldloca V_5
		initobj valuetype MyStruct96`1<class BaseClass0>
		.try {  ldloc V_5
				ldstr "MyStruct96::Method4.813()#" +
				      "MyStruct96::Method5.MI.815()#" +
				      "MyStruct96::Method6.MI.817<System.Object>()#" +
				      "MyStruct96::Method7.818<System.Object>()#"
				call void Framework::M.MyStruct96.T<class BaseClass0,valuetype MyStruct96`1<class BaseClass0>>(!!1,string) leave.s LV0
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV0} LV0:
		.try {  ldloc V_5
				ldstr "MyStruct96::Method4.813()#" +
				      "MyStruct96::Method5.MI.815()#" +
				      "MyStruct96::Method6.MI.817<System.Object>()#" +
				      "MyStruct96::Method7.818<System.Object>()#"
				call void Framework::M.MyStruct96.A<valuetype MyStruct96`1<class BaseClass0>>(!!0,string) leave.s LV1
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV1} LV1:
		.locals init (valuetype MyStruct96`1<class BaseClass1> V_6)
		ldloca V_6
		initobj valuetype MyStruct96`1<class BaseClass1>
		.try {  ldloc V_6
				ldstr "MyStruct96::Method4.813()#" +
				      "MyStruct96::Method5.MI.815()#" +
				      "MyStruct96::Method6.MI.817<System.Object>()#" +
				      "MyStruct96::Method7.818<System.Object>()#"
				call void Framework::M.MyStruct96.T<class BaseClass1,valuetype MyStruct96`1<class BaseClass1>>(!!1,string) leave.s LV2
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV2} LV2:
		.try {  ldloc V_6
				ldstr "MyStruct96::Method4.813()#" +
				      "MyStruct96::Method5.MI.815()#" +
				      "MyStruct96::Method6.MI.817<System.Object>()#" +
				      "MyStruct96::Method7.818<System.Object>()#"
				call void Framework::M.MyStruct96.B<valuetype MyStruct96`1<class BaseClass1>>(!!0,string) leave.s LV3
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
		.locals init (valuetype MyStruct96`1<class BaseClass0> V_7)
		ldloca V_7
		initobj valuetype MyStruct96`1<class BaseClass0>
		ldloc V_7
		box valuetype MyStruct96`1<class BaseClass0>
		ldloc V_7
		box valuetype MyStruct96`1<class BaseClass0>
		ldvirtftn instance string valuetype MyStruct96`1<class BaseClass0>::Method4()
		calli default string(object)
		ldstr "MyStruct96::Method4.813()"
		ldstr "valuetype MyStruct96`1<class BaseClass0> on type valuetype MyStruct96`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_7
		box valuetype MyStruct96`1<class BaseClass0>
		ldloc V_7
		box valuetype MyStruct96`1<class BaseClass0>
		ldvirtftn instance string valuetype MyStruct96`1<class BaseClass0>::Method5()
		calli default string(object)
		ldstr "MyStruct96::Method5.814()"
		ldstr "valuetype MyStruct96`1<class BaseClass0> on type valuetype MyStruct96`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_7
		box valuetype MyStruct96`1<class BaseClass0>
		ldloc V_7
		box valuetype MyStruct96`1<class BaseClass0>
		ldvirtftn instance string valuetype MyStruct96`1<class BaseClass0>::Method6<object>()
		calli default string(object)
		ldstr "MyStruct96::Method6.816<System.Object>()"
		ldstr "valuetype MyStruct96`1<class BaseClass0> on type valuetype MyStruct96`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_7
		box valuetype MyStruct96`1<class BaseClass0>
		ldloc V_7
		box valuetype MyStruct96`1<class BaseClass0>
		ldvirtftn instance string valuetype MyStruct96`1<class BaseClass0>::Method7<object>()
		calli default string(object)
		ldstr "MyStruct96::Method7.818<System.Object>()"
		ldstr "valuetype MyStruct96`1<class BaseClass0> on type valuetype MyStruct96`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_7
		box valuetype MyStruct96`1<class BaseClass0>
		ldloc V_7
		box valuetype MyStruct96`1<class BaseClass0>
		ldvirtftn instance string valuetype MyStruct96`1<class BaseClass0>::ClassMethod188<object>()
		calli default string(object)
		ldstr "MyStruct96::ClassMethod188.819<System.Object>()"
		ldstr "valuetype MyStruct96`1<class BaseClass0> on type valuetype MyStruct96`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_7
		box valuetype MyStruct96`1<class BaseClass0>
		ldloc V_7
		box valuetype MyStruct96`1<class BaseClass0>
		ldvirtftn instance string valuetype MyStruct96`1<class BaseClass0>::ClassMethod189<object>()
		calli default string(object)
		ldstr "MyStruct96::ClassMethod189.820<System.Object>()"
		ldstr "valuetype MyStruct96`1<class BaseClass0> on type valuetype MyStruct96`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_7 box valuetype MyStruct96`1<class BaseClass0> ldnull 
		ldloc V_7 box valuetype MyStruct96`1<class BaseClass0>
		ldvirtftn instance bool valuetype MyStruct96`1<class BaseClass0>::Equals(object) calli default bool(object,object) pop
		ldloc V_7 box valuetype MyStruct96`1<class BaseClass0>
		ldloc V_7 box valuetype MyStruct96`1<class BaseClass0>
		ldvirtftn instance int32 valuetype MyStruct96`1<class BaseClass0>::GetHashCode() calli default int32(object) pop
		ldloc V_7 box valuetype MyStruct96`1<class BaseClass0>
		ldloc V_7 box valuetype MyStruct96`1<class BaseClass0>
		ldvirtftn instance string valuetype MyStruct96`1<class BaseClass0>::ToString() calli default string(object) pop
		ldloc V_7
		box valuetype MyStruct96`1<class BaseClass0>
		ldloc V_7
		box valuetype MyStruct96`1<class BaseClass0>
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(object)
		ldstr "MyStruct96::Method4.813()"
		ldstr "class IBase1`1<class BaseClass1> on type valuetype MyStruct96`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_7
		box valuetype MyStruct96`1<class BaseClass0>
		ldloc V_7
		box valuetype MyStruct96`1<class BaseClass0>
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(object)
		ldstr "MyStruct96::Method5.MI.815()"
		ldstr "class IBase1`1<class BaseClass1> on type valuetype MyStruct96`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_7
		box valuetype MyStruct96`1<class BaseClass0>
		ldloc V_7
		box valuetype MyStruct96`1<class BaseClass0>
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(object)
		ldstr "MyStruct96::Method6.MI.817<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type valuetype MyStruct96`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_7
		box valuetype MyStruct96`1<class BaseClass0>
		ldloc V_7
		box valuetype MyStruct96`1<class BaseClass0>
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(object)
		ldstr "MyStruct96::Method7.818<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type valuetype MyStruct96`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_7
		box valuetype MyStruct96`1<class BaseClass0>
		ldloc V_7
		box valuetype MyStruct96`1<class BaseClass0>
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(object)
		ldstr "MyStruct96::Method4.813()"
		ldstr "class IBase1`1<class BaseClass0> on type valuetype MyStruct96`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_7
		box valuetype MyStruct96`1<class BaseClass0>
		ldloc V_7
		box valuetype MyStruct96`1<class BaseClass0>
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(object)
		ldstr "MyStruct96::Method5.MI.815()"
		ldstr "class IBase1`1<class BaseClass0> on type valuetype MyStruct96`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_7
		box valuetype MyStruct96`1<class BaseClass0>
		ldloc V_7
		box valuetype MyStruct96`1<class BaseClass0>
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(object)
		ldstr "MyStruct96::Method6.MI.817<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type valuetype MyStruct96`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_7
		box valuetype MyStruct96`1<class BaseClass0>
		ldloc V_7
		box valuetype MyStruct96`1<class BaseClass0>
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(object)
		ldstr "MyStruct96::Method7.818<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type valuetype MyStruct96`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		.locals init (valuetype MyStruct96`1<class BaseClass1> V_8)
		ldloca V_8
		initobj valuetype MyStruct96`1<class BaseClass1>
		ldloc V_8
		box valuetype MyStruct96`1<class BaseClass1>
		ldloc V_8
		box valuetype MyStruct96`1<class BaseClass1>
		ldvirtftn instance string valuetype MyStruct96`1<class BaseClass1>::Method4()
		calli default string(object)
		ldstr "MyStruct96::Method4.813()"
		ldstr "valuetype MyStruct96`1<class BaseClass1> on type valuetype MyStruct96`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_8
		box valuetype MyStruct96`1<class BaseClass1>
		ldloc V_8
		box valuetype MyStruct96`1<class BaseClass1>
		ldvirtftn instance string valuetype MyStruct96`1<class BaseClass1>::Method5()
		calli default string(object)
		ldstr "MyStruct96::Method5.814()"
		ldstr "valuetype MyStruct96`1<class BaseClass1> on type valuetype MyStruct96`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_8
		box valuetype MyStruct96`1<class BaseClass1>
		ldloc V_8
		box valuetype MyStruct96`1<class BaseClass1>
		ldvirtftn instance string valuetype MyStruct96`1<class BaseClass1>::Method6<object>()
		calli default string(object)
		ldstr "MyStruct96::Method6.816<System.Object>()"
		ldstr "valuetype MyStruct96`1<class BaseClass1> on type valuetype MyStruct96`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_8
		box valuetype MyStruct96`1<class BaseClass1>
		ldloc V_8
		box valuetype MyStruct96`1<class BaseClass1>
		ldvirtftn instance string valuetype MyStruct96`1<class BaseClass1>::Method7<object>()
		calli default string(object)
		ldstr "MyStruct96::Method7.818<System.Object>()"
		ldstr "valuetype MyStruct96`1<class BaseClass1> on type valuetype MyStruct96`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_8
		box valuetype MyStruct96`1<class BaseClass1>
		ldloc V_8
		box valuetype MyStruct96`1<class BaseClass1>
		ldvirtftn instance string valuetype MyStruct96`1<class BaseClass1>::ClassMethod188<object>()
		calli default string(object)
		ldstr "MyStruct96::ClassMethod188.819<System.Object>()"
		ldstr "valuetype MyStruct96`1<class BaseClass1> on type valuetype MyStruct96`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_8
		box valuetype MyStruct96`1<class BaseClass1>
		ldloc V_8
		box valuetype MyStruct96`1<class BaseClass1>
		ldvirtftn instance string valuetype MyStruct96`1<class BaseClass1>::ClassMethod189<object>()
		calli default string(object)
		ldstr "MyStruct96::ClassMethod189.820<System.Object>()"
		ldstr "valuetype MyStruct96`1<class BaseClass1> on type valuetype MyStruct96`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_8 box valuetype MyStruct96`1<class BaseClass1> ldnull 
		ldloc V_8 box valuetype MyStruct96`1<class BaseClass1>
		ldvirtftn instance bool valuetype MyStruct96`1<class BaseClass1>::Equals(object) calli default bool(object,object) pop
		ldloc V_8 box valuetype MyStruct96`1<class BaseClass1>
		ldloc V_8 box valuetype MyStruct96`1<class BaseClass1>
		ldvirtftn instance int32 valuetype MyStruct96`1<class BaseClass1>::GetHashCode() calli default int32(object) pop
		ldloc V_8 box valuetype MyStruct96`1<class BaseClass1>
		ldloc V_8 box valuetype MyStruct96`1<class BaseClass1>
		ldvirtftn instance string valuetype MyStruct96`1<class BaseClass1>::ToString() calli default string(object) pop
		ldloc V_8
		box valuetype MyStruct96`1<class BaseClass1>
		ldloc V_8
		box valuetype MyStruct96`1<class BaseClass1>
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(object)
		ldstr "MyStruct96::Method4.813()"
		ldstr "class IBase1`1<class BaseClass1> on type valuetype MyStruct96`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_8
		box valuetype MyStruct96`1<class BaseClass1>
		ldloc V_8
		box valuetype MyStruct96`1<class BaseClass1>
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(object)
		ldstr "MyStruct96::Method5.MI.815()"
		ldstr "class IBase1`1<class BaseClass1> on type valuetype MyStruct96`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_8
		box valuetype MyStruct96`1<class BaseClass1>
		ldloc V_8
		box valuetype MyStruct96`1<class BaseClass1>
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(object)
		ldstr "MyStruct96::Method6.MI.817<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type valuetype MyStruct96`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_8
		box valuetype MyStruct96`1<class BaseClass1>
		ldloc V_8
		box valuetype MyStruct96`1<class BaseClass1>
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(object)
		ldstr "MyStruct96::Method7.818<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type valuetype MyStruct96`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_8
		box valuetype MyStruct96`1<class BaseClass1>
		ldloc V_8
		box valuetype MyStruct96`1<class BaseClass1>
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(object)
		ldstr "MyStruct96::Method4.813()"
		ldstr "class IBase1`1<class BaseClass0> on type valuetype MyStruct96`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_8
		box valuetype MyStruct96`1<class BaseClass1>
		ldloc V_8
		box valuetype MyStruct96`1<class BaseClass1>
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(object)
		ldstr "MyStruct96::Method5.MI.815()"
		ldstr "class IBase1`1<class BaseClass0> on type valuetype MyStruct96`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_8
		box valuetype MyStruct96`1<class BaseClass1>
		ldloc V_8
		box valuetype MyStruct96`1<class BaseClass1>
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(object)
		ldstr "MyStruct96::Method6.MI.817<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type valuetype MyStruct96`1<class BaseClass1>"
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
