// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) .ver 4:0:0:0 }
.assembly extern TestFramework { .publickeytoken = ( B0 3F 5F 7F 11 D5 0A 3A )  }

//TYPES IN FORWARDER ASSEMBLIES:

//TEST ASSEMBLY:
.assembly extern xunit.core {}

.class public Generated222.BaseClass0 
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void [mscorlib]System.Object::.ctor()
		ret
	}
}
.class public Generated222.BaseClass1 
		extends Generated222.BaseClass0
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void Generated222.BaseClass0::.ctor()
		ret
	}
}
.class public sequential sealed Generated222.MyStruct272`2<T0, T1> 
		extends [mscorlib]System.ValueType
		implements class Generated222.IBase1`1<!T0>, class Generated222.IBase1`1<class Generated222.BaseClass0> 
{
	.pack 0
	.size 1
	.method public hidebysig virtual instance string Method4() cil managed noinlining { 
		ldstr "MyStruct272::Method4.2132()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<T0>.Method4'() cil managed noinlining { 
		.override method instance string class Generated222.IBase1`1<!T0>::Method4()
		ldstr "MyStruct272::Method4.MI.2133()"
		ret
	}
	.method public hidebysig virtual instance string Method5() cil managed noinlining { 
		ldstr "MyStruct272::Method5.2134()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<T0>.Method5'() cil managed noinlining { 
		.override method instance string class Generated222.IBase1`1<!T0>::Method5()
		ldstr "MyStruct272::Method5.MI.2135()"
		ret
	}
	.method public hidebysig virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "MyStruct272::Method6.2136<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<T0>.Method6'<M0>() cil managed noinlining { 
		.override method instance string class Generated222.IBase1`1<!T0>::Method6<[1]>()
		ldstr "MyStruct272::Method6.MI.2137<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class Generated222.BaseClass0>.Method4'() cil managed noinlining { 
		.override method instance string class Generated222.IBase1`1<class Generated222.BaseClass0>::Method4()
		ldstr "MyStruct272::Method4.MI.2139()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class Generated222.BaseClass0>.Method5'() cil managed noinlining { 
		.override method instance string class Generated222.IBase1`1<class Generated222.BaseClass0>::Method5()
		ldstr "MyStruct272::Method5.MI.2141()"
		ret
	}
	.method public hidebysig virtual instance bool Equals(object obj) cil managed { ldc.i4.0 ret }
	.method public hidebysig virtual instance int32 GetHashCode() cil managed { ldc.i4.0 ret }
	.method public hidebysig virtual instance string ToString() cil managed { ldstr "" ret }
}
.class interface public abstract Generated222.IBase1`1<+T0> 
{
	.method public hidebysig newslot abstract virtual instance string Method4() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method5() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method6<M0>() cil managed { }
}
.class public auto ansi beforefieldinit Generated222.Generated222 {
	.method static void M.BaseClass0<(Generated222.BaseClass0)W>(!!W inst, string exp) cil managed {
		.maxstack 5
		.locals init (string[] actualResults)
		ldc.i4.s    0
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.BaseClass0<(Generated222.BaseClass0)W>(!!W inst, string exp)"
		ldc.i4.s 0
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.BaseClass1<(Generated222.BaseClass1)W>(!!W inst, string exp) cil managed {
		.maxstack 5
		.locals init (string[] actualResults)
		ldc.i4.s    0
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.BaseClass1<(Generated222.BaseClass1)W>(!!W inst, string exp)"
		ldc.i4.s 0
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.T<T0,(class Generated222.IBase1`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.T<T0,(class Generated222.IBase1`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated222.IBase1`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated222.IBase1`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated222.IBase1`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.A<(class Generated222.IBase1`1<class Generated222.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.A<(class Generated222.IBase1`1<class Generated222.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated222.IBase1`1<class Generated222.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated222.IBase1`1<class Generated222.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated222.IBase1`1<class Generated222.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.B<(class Generated222.IBase1`1<class Generated222.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.B<(class Generated222.IBase1`1<class Generated222.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated222.IBase1`1<class Generated222.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated222.IBase1`1<class Generated222.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated222.IBase1`1<class Generated222.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.MyStruct272.T.T<T0,T1,(valuetype Generated222.MyStruct272`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.MyStruct272.T.T<T0,T1,(valuetype Generated222.MyStruct272`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. valuetype Generated222.MyStruct272`2<!!T0,!!T1>
		callvirt     instance string class Generated222.IBase1`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. valuetype Generated222.MyStruct272`2<!!T0,!!T1>
		callvirt     instance string class Generated222.IBase1`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. valuetype Generated222.MyStruct272`2<!!T0,!!T1>
		callvirt     instance string class Generated222.IBase1`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. valuetype Generated222.MyStruct272`2<!!T0,!!T1>
		callvirt     instance string class Generated222.IBase1`1<class Generated222.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. valuetype Generated222.MyStruct272`2<!!T0,!!T1>
		callvirt     instance string class Generated222.IBase1`1<class Generated222.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. valuetype Generated222.MyStruct272`2<!!T0,!!T1>
		callvirt     instance string class Generated222.IBase1`1<class Generated222.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.MyStruct272.B.T<T1,(valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.MyStruct272.B.T<T1,(valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,!!T1>
		callvirt     instance string class Generated222.IBase1`1<class Generated222.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,!!T1>
		callvirt     instance string class Generated222.IBase1`1<class Generated222.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,!!T1>
		callvirt     instance string class Generated222.IBase1`1<class Generated222.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,!!T1>
		callvirt     instance string class Generated222.IBase1`1<class Generated222.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,!!T1>
		callvirt     instance string class Generated222.IBase1`1<class Generated222.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,!!T1>
		callvirt     instance string class Generated222.IBase1`1<class Generated222.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.MyStruct272.B.A<(valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.MyStruct272.B.A<(valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass0>
		callvirt     instance string class Generated222.IBase1`1<class Generated222.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass0>
		callvirt     instance string class Generated222.IBase1`1<class Generated222.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass0>
		callvirt     instance string class Generated222.IBase1`1<class Generated222.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass0>
		callvirt     instance string class Generated222.IBase1`1<class Generated222.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass0>
		callvirt     instance string class Generated222.IBase1`1<class Generated222.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass0>
		callvirt     instance string class Generated222.IBase1`1<class Generated222.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.MyStruct272.B.B<(valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.MyStruct272.B.B<(valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass1>
		callvirt     instance string class Generated222.IBase1`1<class Generated222.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass1>
		callvirt     instance string class Generated222.IBase1`1<class Generated222.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass1>
		callvirt     instance string class Generated222.IBase1`1<class Generated222.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass1>
		callvirt     instance string class Generated222.IBase1`1<class Generated222.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass1>
		callvirt     instance string class Generated222.IBase1`1<class Generated222.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass1>
		callvirt     instance string class Generated222.IBase1`1<class Generated222.BaseClass0>::Method6<object>()
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
		.locals init (valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass0> V_3)
		ldloca V_3
		initobj valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass0>
		ldloca V_3
		dup
		call instance string valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass0>::Method4()
		ldstr "MyStruct272::Method4.2132()"
		ldstr "valuetype MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass0> on type MyStruct272"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass0>::Method5()
		ldstr "MyStruct272::Method5.2134()"
		ldstr "valuetype MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass0> on type MyStruct272"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass0>::Method6<object>()
		ldstr "MyStruct272::Method6.2136<System.Object>()"
		ldstr "valuetype MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass0> on type MyStruct272"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup ldnull call instance bool valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass0>::Equals(object) pop
		dup call instance int32 valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass0>::GetHashCode() pop
		dup call instance string valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass0>::ToString() pop
		pop
		ldloc V_3
		box valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass0>
		dup
		callvirt instance string class Generated222.IBase1`1<class Generated222.BaseClass1>::Method4()
		ldstr "MyStruct272::Method4.MI.2133()"
		ldstr "class IBase1`1<class Generated222.BaseClass1> on type valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated222.IBase1`1<class Generated222.BaseClass1>::Method5()
		ldstr "MyStruct272::Method5.MI.2135()"
		ldstr "class IBase1`1<class Generated222.BaseClass1> on type valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated222.IBase1`1<class Generated222.BaseClass1>::Method6<object>()
		ldstr "MyStruct272::Method6.MI.2137<System.Object>()"
		ldstr "class IBase1`1<class Generated222.BaseClass1> on type valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc V_3
		box valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass0>
		dup
		callvirt instance string class Generated222.IBase1`1<class Generated222.BaseClass0>::Method4()
		ldstr "MyStruct272::Method4.MI.2139()"
		ldstr "class IBase1`1<class Generated222.BaseClass0> on type valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated222.IBase1`1<class Generated222.BaseClass0>::Method5()
		ldstr "MyStruct272::Method5.MI.2141()"
		ldstr "class IBase1`1<class Generated222.BaseClass0> on type valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated222.IBase1`1<class Generated222.BaseClass0>::Method6<object>()
		ldstr "MyStruct272::Method6.2136<System.Object>()"
		ldstr "class IBase1`1<class Generated222.BaseClass0> on type valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		.locals init (valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass1> V_4)
		ldloca V_4
		initobj valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass1>
		ldloca V_4
		dup
		call instance string valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass1>::Method4()
		ldstr "MyStruct272::Method4.2132()"
		ldstr "valuetype MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass1> on type MyStruct272"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass1>::Method5()
		ldstr "MyStruct272::Method5.2134()"
		ldstr "valuetype MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass1> on type MyStruct272"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass1>::Method6<object>()
		ldstr "MyStruct272::Method6.2136<System.Object>()"
		ldstr "valuetype MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass1> on type MyStruct272"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup ldnull call instance bool valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass1>::Equals(object) pop
		dup call instance int32 valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass1>::GetHashCode() pop
		dup call instance string valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass1>::ToString() pop
		pop
		ldloc V_4
		box valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass1>
		dup
		callvirt instance string class Generated222.IBase1`1<class Generated222.BaseClass1>::Method4()
		ldstr "MyStruct272::Method4.MI.2133()"
		ldstr "class IBase1`1<class Generated222.BaseClass1> on type valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated222.IBase1`1<class Generated222.BaseClass1>::Method5()
		ldstr "MyStruct272::Method5.MI.2135()"
		ldstr "class IBase1`1<class Generated222.BaseClass1> on type valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated222.IBase1`1<class Generated222.BaseClass1>::Method6<object>()
		ldstr "MyStruct272::Method6.MI.2137<System.Object>()"
		ldstr "class IBase1`1<class Generated222.BaseClass1> on type valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc V_4
		box valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass1>
		dup
		callvirt instance string class Generated222.IBase1`1<class Generated222.BaseClass0>::Method4()
		ldstr "MyStruct272::Method4.MI.2139()"
		ldstr "class IBase1`1<class Generated222.BaseClass0> on type valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated222.IBase1`1<class Generated222.BaseClass0>::Method5()
		ldstr "MyStruct272::Method5.MI.2141()"
		ldstr "class IBase1`1<class Generated222.BaseClass0> on type valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated222.IBase1`1<class Generated222.BaseClass0>::Method6<object>()
		ldstr "MyStruct272::Method6.2136<System.Object>()"
		ldstr "class IBase1`1<class Generated222.BaseClass0> on type valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass1>"
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
		.locals init (valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass0> V_7)
		ldloca V_7
		initobj valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass0>
		.try {  ldloc V_7
				ldstr "MyStruct272::Method4.MI.2133()#MyStruct272::Method5.MI.2135()#MyStruct272::Method6.MI.2137<System.Object>()#"
				call void Generated222.Generated222::M.IBase1.T<class Generated222.BaseClass1,valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass0>>(!!1,string) leave.s LV0
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV0} LV0:
		.try {  ldloc V_7
				ldstr "MyStruct272::Method4.MI.2133()#MyStruct272::Method5.MI.2135()#MyStruct272::Method6.MI.2137<System.Object>()#"
				call void Generated222.Generated222::M.IBase1.B<valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass0>>(!!0,string) leave.s LV1
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV1} LV1:
		.try {  ldloc V_7
				ldstr "MyStruct272::Method4.MI.2139()#MyStruct272::Method5.MI.2141()#MyStruct272::Method6.2136<System.Object>()#"
				call void Generated222.Generated222::M.IBase1.T<class Generated222.BaseClass0,valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass0>>(!!1,string) leave.s LV2
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV2} LV2:
		.try {  ldloc V_7
				ldstr "MyStruct272::Method4.MI.2139()#MyStruct272::Method5.MI.2141()#MyStruct272::Method6.2136<System.Object>()#"
				call void Generated222.Generated222::M.IBase1.A<valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass0>>(!!0,string) leave.s LV3
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV3} LV3:
		.locals init (valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass1> V_8)
		ldloca V_8
		initobj valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass1>
		.try {  ldloc V_8
				ldstr "MyStruct272::Method4.MI.2133()#MyStruct272::Method5.MI.2135()#MyStruct272::Method6.MI.2137<System.Object>()#"
				call void Generated222.Generated222::M.IBase1.T<class Generated222.BaseClass1,valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass1>>(!!1,string) leave.s LV4
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV4} LV4:
		.try {  ldloc V_8
				ldstr "MyStruct272::Method4.MI.2133()#MyStruct272::Method5.MI.2135()#MyStruct272::Method6.MI.2137<System.Object>()#"
				call void Generated222.Generated222::M.IBase1.B<valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass1>>(!!0,string) leave.s LV5
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV5} LV5:
		.try {  ldloc V_8
				ldstr "MyStruct272::Method4.MI.2139()#MyStruct272::Method5.MI.2141()#MyStruct272::Method6.2136<System.Object>()#"
				call void Generated222.Generated222::M.IBase1.T<class Generated222.BaseClass0,valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass1>>(!!1,string) leave.s LV6
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV6} LV6:
		.try {  ldloc V_8
				ldstr "MyStruct272::Method4.MI.2139()#MyStruct272::Method5.MI.2141()#MyStruct272::Method6.2136<System.Object>()#"
				call void Generated222.Generated222::M.IBase1.A<valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass1>>(!!0,string) leave.s LV7
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
		.locals init (valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass0> V_11)
		ldloca V_11
		initobj valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass0>
		.try {  ldloc V_11
				ldstr "MyStruct272::Method4.MI.2133()#" +
				      "MyStruct272::Method5.MI.2135()#" +
				      "MyStruct272::Method6.MI.2137<System.Object>()#" +
				      "MyStruct272::Method4.MI.2139()#" +
				      "MyStruct272::Method5.MI.2141()#" +
				      "MyStruct272::Method6.2136<System.Object>()#"
				call void Generated222.Generated222::M.MyStruct272.T.T<class Generated222.BaseClass1,class Generated222.BaseClass0,valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass0>>(!!2,string) leave.s LV0
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV0} LV0:
		.try {  ldloc V_11
				ldstr "MyStruct272::Method4.MI.2133()#" +
				      "MyStruct272::Method5.MI.2135()#" +
				      "MyStruct272::Method6.MI.2137<System.Object>()#" +
				      "MyStruct272::Method4.MI.2139()#" +
				      "MyStruct272::Method5.MI.2141()#" +
				      "MyStruct272::Method6.2136<System.Object>()#"
				call void Generated222.Generated222::M.MyStruct272.B.T<class Generated222.BaseClass0,valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass0>>(!!1,string) leave.s LV1
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV1} LV1:
		.try {  ldloc V_11
				ldstr "MyStruct272::Method4.MI.2133()#" +
				      "MyStruct272::Method5.MI.2135()#" +
				      "MyStruct272::Method6.MI.2137<System.Object>()#" +
				      "MyStruct272::Method4.MI.2139()#" +
				      "MyStruct272::Method5.MI.2141()#" +
				      "MyStruct272::Method6.2136<System.Object>()#"
				call void Generated222.Generated222::M.MyStruct272.B.A<valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass0>>(!!0,string) leave.s LV2
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV2} LV2:
		.locals init (valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass1> V_12)
		ldloca V_12
		initobj valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass1>
		.try {  ldloc V_12
				ldstr "MyStruct272::Method4.MI.2133()#" +
				      "MyStruct272::Method5.MI.2135()#" +
				      "MyStruct272::Method6.MI.2137<System.Object>()#" +
				      "MyStruct272::Method4.MI.2139()#" +
				      "MyStruct272::Method5.MI.2141()#" +
				      "MyStruct272::Method6.2136<System.Object>()#"
				call void Generated222.Generated222::M.MyStruct272.T.T<class Generated222.BaseClass1,class Generated222.BaseClass1,valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass1>>(!!2,string) leave.s LV3
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV3} LV3:
		.try {  ldloc V_12
				ldstr "MyStruct272::Method4.MI.2133()#" +
				      "MyStruct272::Method5.MI.2135()#" +
				      "MyStruct272::Method6.MI.2137<System.Object>()#" +
				      "MyStruct272::Method4.MI.2139()#" +
				      "MyStruct272::Method5.MI.2141()#" +
				      "MyStruct272::Method6.2136<System.Object>()#"
				call void Generated222.Generated222::M.MyStruct272.B.T<class Generated222.BaseClass1,valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass1>>(!!1,string) leave.s LV4
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV4} LV4:
		.try {  ldloc V_12
				ldstr "MyStruct272::Method4.MI.2133()#" +
				      "MyStruct272::Method5.MI.2135()#" +
				      "MyStruct272::Method6.MI.2137<System.Object>()#" +
				      "MyStruct272::Method4.MI.2139()#" +
				      "MyStruct272::Method5.MI.2141()#" +
				      "MyStruct272::Method6.2136<System.Object>()#"
				call void Generated222.Generated222::M.MyStruct272.B.B<valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass1>>(!!0,string) leave.s LV5
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV5} LV5:
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
		.locals init (valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass0> V_15)
		ldloca V_15
		initobj valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass0>
		ldloc V_15
		box valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass0>
		ldloc V_15
		box valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass0>
		ldvirtftn instance string valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass0>::Method4()
		calli default string(object)
		ldstr "MyStruct272::Method4.2132()"
		ldstr "valuetype MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass0> on type valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_15
		box valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass0>
		ldloc V_15
		box valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass0>
		ldvirtftn instance string valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass0>::Method5()
		calli default string(object)
		ldstr "MyStruct272::Method5.2134()"
		ldstr "valuetype MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass0> on type valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_15
		box valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass0>
		ldloc V_15
		box valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass0>
		ldvirtftn instance string valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass0>::Method6<object>()
		calli default string(object)
		ldstr "MyStruct272::Method6.2136<System.Object>()"
		ldstr "valuetype MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass0> on type valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_15 box valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass0> ldnull 
		ldloc V_15 box valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass0>
		ldvirtftn instance bool valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass0>::Equals(object) calli default bool(object,object) pop
		ldloc V_15 box valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass0>
		ldloc V_15 box valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass0>
		ldvirtftn instance int32 valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass0>::GetHashCode() calli default int32(object) pop
		ldloc V_15 box valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass0>
		ldloc V_15 box valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass0>
		ldvirtftn instance string valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass0>::ToString() calli default string(object) pop
		ldloc V_15
		box valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass0>
		ldloc V_15
		box valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass0>
		ldvirtftn instance string class Generated222.IBase1`1<class Generated222.BaseClass1>::Method4()
		calli default string(object)
		ldstr "MyStruct272::Method4.MI.2133()"
		ldstr "class IBase1`1<class Generated222.BaseClass1> on type valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_15
		box valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass0>
		ldloc V_15
		box valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass0>
		ldvirtftn instance string class Generated222.IBase1`1<class Generated222.BaseClass1>::Method5()
		calli default string(object)
		ldstr "MyStruct272::Method5.MI.2135()"
		ldstr "class IBase1`1<class Generated222.BaseClass1> on type valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_15
		box valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass0>
		ldloc V_15
		box valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass0>
		ldvirtftn instance string class Generated222.IBase1`1<class Generated222.BaseClass1>::Method6<object>()
		calli default string(object)
		ldstr "MyStruct272::Method6.MI.2137<System.Object>()"
		ldstr "class IBase1`1<class Generated222.BaseClass1> on type valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_15
		box valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass0>
		ldloc V_15
		box valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass0>
		ldvirtftn instance string class Generated222.IBase1`1<class Generated222.BaseClass0>::Method4()
		calli default string(object)
		ldstr "MyStruct272::Method4.MI.2139()"
		ldstr "class IBase1`1<class Generated222.BaseClass0> on type valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_15
		box valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass0>
		ldloc V_15
		box valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass0>
		ldvirtftn instance string class Generated222.IBase1`1<class Generated222.BaseClass0>::Method5()
		calli default string(object)
		ldstr "MyStruct272::Method5.MI.2141()"
		ldstr "class IBase1`1<class Generated222.BaseClass0> on type valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_15
		box valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass0>
		ldloc V_15
		box valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass0>
		ldvirtftn instance string class Generated222.IBase1`1<class Generated222.BaseClass0>::Method6<object>()
		calli default string(object)
		ldstr "MyStruct272::Method6.2136<System.Object>()"
		ldstr "class IBase1`1<class Generated222.BaseClass0> on type valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		.locals init (valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass1> V_16)
		ldloca V_16
		initobj valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass1>
		ldloc V_16
		box valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass1>
		ldloc V_16
		box valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass1>
		ldvirtftn instance string valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass1>::Method4()
		calli default string(object)
		ldstr "MyStruct272::Method4.2132()"
		ldstr "valuetype MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass1> on type valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_16
		box valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass1>
		ldloc V_16
		box valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass1>
		ldvirtftn instance string valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass1>::Method5()
		calli default string(object)
		ldstr "MyStruct272::Method5.2134()"
		ldstr "valuetype MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass1> on type valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_16
		box valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass1>
		ldloc V_16
		box valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass1>
		ldvirtftn instance string valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass1>::Method6<object>()
		calli default string(object)
		ldstr "MyStruct272::Method6.2136<System.Object>()"
		ldstr "valuetype MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass1> on type valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_16 box valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass1> ldnull 
		ldloc V_16 box valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass1>
		ldvirtftn instance bool valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass1>::Equals(object) calli default bool(object,object) pop
		ldloc V_16 box valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass1>
		ldloc V_16 box valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass1>
		ldvirtftn instance int32 valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass1>::GetHashCode() calli default int32(object) pop
		ldloc V_16 box valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass1>
		ldloc V_16 box valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass1>
		ldvirtftn instance string valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass1>::ToString() calli default string(object) pop
		ldloc V_16
		box valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass1>
		ldloc V_16
		box valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass1>
		ldvirtftn instance string class Generated222.IBase1`1<class Generated222.BaseClass1>::Method4()
		calli default string(object)
		ldstr "MyStruct272::Method4.MI.2133()"
		ldstr "class IBase1`1<class Generated222.BaseClass1> on type valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_16
		box valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass1>
		ldloc V_16
		box valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass1>
		ldvirtftn instance string class Generated222.IBase1`1<class Generated222.BaseClass1>::Method5()
		calli default string(object)
		ldstr "MyStruct272::Method5.MI.2135()"
		ldstr "class IBase1`1<class Generated222.BaseClass1> on type valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_16
		box valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass1>
		ldloc V_16
		box valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass1>
		ldvirtftn instance string class Generated222.IBase1`1<class Generated222.BaseClass1>::Method6<object>()
		calli default string(object)
		ldstr "MyStruct272::Method6.MI.2137<System.Object>()"
		ldstr "class IBase1`1<class Generated222.BaseClass1> on type valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_16
		box valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass1>
		ldloc V_16
		box valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass1>
		ldvirtftn instance string class Generated222.IBase1`1<class Generated222.BaseClass0>::Method4()
		calli default string(object)
		ldstr "MyStruct272::Method4.MI.2139()"
		ldstr "class IBase1`1<class Generated222.BaseClass0> on type valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_16
		box valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass1>
		ldloc V_16
		box valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass1>
		ldvirtftn instance string class Generated222.IBase1`1<class Generated222.BaseClass0>::Method5()
		calli default string(object)
		ldstr "MyStruct272::Method5.MI.2141()"
		ldstr "class IBase1`1<class Generated222.BaseClass0> on type valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_16
		box valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass1>
		ldloc V_16
		box valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass1>
		ldvirtftn instance string class Generated222.IBase1`1<class Generated222.BaseClass0>::Method6<object>()
		calli default string(object)
		ldstr "MyStruct272::Method6.2136<System.Object>()"
		ldstr "class IBase1`1<class Generated222.BaseClass0> on type valuetype Generated222.MyStruct272`2<class Generated222.BaseClass1,class Generated222.BaseClass1>"
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
		call void Generated222.Generated222::MethodCallingTest()
		call void Generated222.Generated222::ConstrainedCallsTest()
		call void Generated222.Generated222::StructConstrainedInterfaceCallsTest()
		call void Generated222.Generated222::CalliTest()
		ldc.i4 100
		ret
	}
}
