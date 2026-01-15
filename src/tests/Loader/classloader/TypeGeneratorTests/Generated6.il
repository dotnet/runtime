// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.class public Generated6.BaseClass0 
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void [mscorlib]System.Object::.ctor()
		ret
	}
}
.class public Generated6.BaseClass1 
		extends Generated6.BaseClass0
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void Generated6.BaseClass0::.ctor()
		ret
	}
}
.class public sequential sealed Generated6.MyStruct56`1<T0> 
		extends [mscorlib]System.ValueType
		implements class Generated6.IBase2`2<class Generated6.BaseClass0,class Generated6.BaseClass1> 
{
	.pack 0
	.size 1
	.method public hidebysig newslot virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "MyStruct56::Method7.442<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase2<class Generated6.BaseClass0,class Generated6.BaseClass1>.Method7'<M0>() cil managed noinlining { 
		.override method instance string class Generated6.IBase2`2<class Generated6.BaseClass0,class Generated6.BaseClass1>::Method7<[1]>()
		ldstr "MyStruct56::Method7.MI.443<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot instance string ClassMethod121() cil managed noinlining { 
		ldstr "MyStruct56::ClassMethod121.444()"
		ret
	}
	.method public hidebysig virtual instance bool Equals(object obj) cil managed { ldc.i4.0 ret }
	.method public hidebysig virtual instance int32 GetHashCode() cil managed { ldc.i4.0 ret }
	.method public hidebysig virtual instance string ToString() cil managed { ldstr "" ret }
}
.class interface public abstract Generated6.IBase2`2<+T0, -T1> 
{
	.method public hidebysig newslot abstract virtual instance string Method7<M0>() cil managed { }
}
.class public auto ansi beforefieldinit Generated6.Generated6 {
	.method static void M.BaseClass0<(Generated6.BaseClass0)W>(!!W inst, string exp) cil managed {
		.maxstack 5
		.locals init (string[] actualResults)
		ldc.i4.s    0
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.BaseClass0<(Generated6.BaseClass0)W>(!!W inst, string exp)"
		ldc.i4.s 0
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.BaseClass1<(Generated6.BaseClass1)W>(!!W inst, string exp) cil managed {
		.maxstack 5
		.locals init (string[] actualResults)
		ldc.i4.s    0
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.BaseClass1<(Generated6.BaseClass1)W>(!!W inst, string exp)"
		ldc.i4.s 0
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.T.T<T0,T1,(class Generated6.IBase2`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.T.T<T0,T1,(class Generated6.IBase2`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated6.IBase2`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.T<T1,(class Generated6.IBase2`2<class Generated6.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.T<T1,(class Generated6.IBase2`2<class Generated6.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated6.IBase2`2<class Generated6.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.A<(class Generated6.IBase2`2<class Generated6.BaseClass0,class Generated6.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.A<(class Generated6.IBase2`2<class Generated6.BaseClass0,class Generated6.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated6.IBase2`2<class Generated6.BaseClass0,class Generated6.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.B<(class Generated6.IBase2`2<class Generated6.BaseClass0,class Generated6.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.B<(class Generated6.IBase2`2<class Generated6.BaseClass0,class Generated6.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated6.IBase2`2<class Generated6.BaseClass0,class Generated6.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.T<T1,(class Generated6.IBase2`2<class Generated6.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.T<T1,(class Generated6.IBase2`2<class Generated6.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated6.IBase2`2<class Generated6.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.A<(class Generated6.IBase2`2<class Generated6.BaseClass1,class Generated6.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.A<(class Generated6.IBase2`2<class Generated6.BaseClass1,class Generated6.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated6.IBase2`2<class Generated6.BaseClass1,class Generated6.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.B<(class Generated6.IBase2`2<class Generated6.BaseClass1,class Generated6.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.B<(class Generated6.IBase2`2<class Generated6.BaseClass1,class Generated6.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated6.IBase2`2<class Generated6.BaseClass1,class Generated6.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.MyStruct56.T<T0,(valuetype Generated6.MyStruct56`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.MyStruct56.T<T0,(valuetype Generated6.MyStruct56`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. valuetype Generated6.MyStruct56`1<!!T0>
		callvirt     instance string class Generated6.IBase2`2<class Generated6.BaseClass0,class Generated6.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.MyStruct56.A<(valuetype Generated6.MyStruct56`1<class Generated6.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.MyStruct56.A<(valuetype Generated6.MyStruct56`1<class Generated6.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. valuetype Generated6.MyStruct56`1<class Generated6.BaseClass0>
		callvirt     instance string class Generated6.IBase2`2<class Generated6.BaseClass0,class Generated6.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.MyStruct56.B<(valuetype Generated6.MyStruct56`1<class Generated6.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.MyStruct56.B<(valuetype Generated6.MyStruct56`1<class Generated6.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. valuetype Generated6.MyStruct56`1<class Generated6.BaseClass1>
		callvirt     instance string class Generated6.IBase2`2<class Generated6.BaseClass0,class Generated6.BaseClass1>::Method7<object>()
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
		.locals init (valuetype Generated6.MyStruct56`1<class Generated6.BaseClass0> V_1)
		ldloca V_1
		initobj valuetype Generated6.MyStruct56`1<class Generated6.BaseClass0>
		ldloca V_1
		dup
		call instance string valuetype Generated6.MyStruct56`1<class Generated6.BaseClass0>::Method7<object>()
		ldstr "MyStruct56::Method7.442<System.Object>()"
		ldstr "valuetype MyStruct56`1<class Generated6.BaseClass0> on type MyStruct56"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype Generated6.MyStruct56`1<class Generated6.BaseClass0>::ClassMethod121()
		ldstr "MyStruct56::ClassMethod121.444()"
		ldstr "valuetype MyStruct56`1<class Generated6.BaseClass0> on type MyStruct56"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup ldnull call instance bool valuetype Generated6.MyStruct56`1<class Generated6.BaseClass0>::Equals(object) pop
		dup call instance int32 valuetype Generated6.MyStruct56`1<class Generated6.BaseClass0>::GetHashCode() pop
		dup call instance string valuetype Generated6.MyStruct56`1<class Generated6.BaseClass0>::ToString() pop
		pop
		ldloc V_1
		box valuetype Generated6.MyStruct56`1<class Generated6.BaseClass0>
		dup
		callvirt instance string class Generated6.IBase2`2<class Generated6.BaseClass0,class Generated6.BaseClass1>::Method7<object>()
		ldstr "MyStruct56::Method7.MI.443<System.Object>()"
		ldstr "class IBase2`2<class Generated6.BaseClass0,class Generated6.BaseClass1> on type valuetype Generated6.MyStruct56`1<class Generated6.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		.locals init (valuetype Generated6.MyStruct56`1<class Generated6.BaseClass1> V_2)
		ldloca V_2
		initobj valuetype Generated6.MyStruct56`1<class Generated6.BaseClass1>
		ldloca V_2
		dup
		call instance string valuetype Generated6.MyStruct56`1<class Generated6.BaseClass1>::Method7<object>()
		ldstr "MyStruct56::Method7.442<System.Object>()"
		ldstr "valuetype MyStruct56`1<class Generated6.BaseClass1> on type MyStruct56"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		call instance string valuetype Generated6.MyStruct56`1<class Generated6.BaseClass1>::ClassMethod121()
		ldstr "MyStruct56::ClassMethod121.444()"
		ldstr "valuetype MyStruct56`1<class Generated6.BaseClass1> on type MyStruct56"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup ldnull call instance bool valuetype Generated6.MyStruct56`1<class Generated6.BaseClass1>::Equals(object) pop
		dup call instance int32 valuetype Generated6.MyStruct56`1<class Generated6.BaseClass1>::GetHashCode() pop
		dup call instance string valuetype Generated6.MyStruct56`1<class Generated6.BaseClass1>::ToString() pop
		pop
		ldloc V_2
		box valuetype Generated6.MyStruct56`1<class Generated6.BaseClass1>
		dup
		callvirt instance string class Generated6.IBase2`2<class Generated6.BaseClass0,class Generated6.BaseClass1>::Method7<object>()
		ldstr "MyStruct56::Method7.MI.443<System.Object>()"
		ldstr "class IBase2`2<class Generated6.BaseClass0,class Generated6.BaseClass1> on type valuetype Generated6.MyStruct56`1<class Generated6.BaseClass1>"
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
		.locals init (valuetype Generated6.MyStruct56`1<class Generated6.BaseClass0> V_3)
		ldloca V_3
		initobj valuetype Generated6.MyStruct56`1<class Generated6.BaseClass0>
		.try {  ldloc V_3
				ldstr "MyStruct56::Method7.MI.443<System.Object>()#"
				call void Generated6.Generated6::M.IBase2.T.T<class Generated6.BaseClass0,class Generated6.BaseClass1,valuetype Generated6.MyStruct56`1<class Generated6.BaseClass0>>(!!2,string) leave.s LV0
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV0} LV0:
		.try {  ldloc V_3
				ldstr "MyStruct56::Method7.MI.443<System.Object>()#"
				call void Generated6.Generated6::M.IBase2.A.T<class Generated6.BaseClass1,valuetype Generated6.MyStruct56`1<class Generated6.BaseClass0>>(!!1,string) leave.s LV1
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV1} LV1:
		.try {  ldloc V_3
				ldstr "MyStruct56::Method7.MI.443<System.Object>()#"
				call void Generated6.Generated6::M.IBase2.A.B<valuetype Generated6.MyStruct56`1<class Generated6.BaseClass0>>(!!0,string) leave.s LV2
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV2} LV2:
		.locals init (valuetype Generated6.MyStruct56`1<class Generated6.BaseClass1> V_4)
		ldloca V_4
		initobj valuetype Generated6.MyStruct56`1<class Generated6.BaseClass1>
		.try {  ldloc V_4
				ldstr "MyStruct56::Method7.MI.443<System.Object>()#"
				call void Generated6.Generated6::M.IBase2.T.T<class Generated6.BaseClass0,class Generated6.BaseClass1,valuetype Generated6.MyStruct56`1<class Generated6.BaseClass1>>(!!2,string) leave.s LV3
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV3} LV3:
		.try {  ldloc V_4
				ldstr "MyStruct56::Method7.MI.443<System.Object>()#"
				call void Generated6.Generated6::M.IBase2.A.T<class Generated6.BaseClass1,valuetype Generated6.MyStruct56`1<class Generated6.BaseClass1>>(!!1,string) leave.s LV4
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV4} LV4:
		.try {  ldloc V_4
				ldstr "MyStruct56::Method7.MI.443<System.Object>()#"
				call void Generated6.Generated6::M.IBase2.A.B<valuetype Generated6.MyStruct56`1<class Generated6.BaseClass1>>(!!0,string) leave.s LV5
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV5} LV5:
		ldstr "========================================================================\n\n"
		call void [mscorlib]System.Console::WriteLine(string)
		ret
	}
	.method public hidebysig static void StructConstrainedInterfaceCallsTest() cil managed
	{
		.maxstack  10
		ldstr "===================== Struct Constrained Interface Calls Test ====================="
		call void [mscorlib]System.Console::WriteLine(string)
		.locals init (valuetype Generated6.MyStruct56`1<class Generated6.BaseClass0> V_5)
		ldloca V_5
		initobj valuetype Generated6.MyStruct56`1<class Generated6.BaseClass0>
		.try {  ldloc V_5
				ldstr "MyStruct56::Method7.MI.443<System.Object>()#"
				call void Generated6.Generated6::M.MyStruct56.T<class Generated6.BaseClass0,valuetype Generated6.MyStruct56`1<class Generated6.BaseClass0>>(!!1,string) leave.s LV0
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV0} LV0:
		.try {  ldloc V_5
				ldstr "MyStruct56::Method7.MI.443<System.Object>()#"
				call void Generated6.Generated6::M.MyStruct56.A<valuetype Generated6.MyStruct56`1<class Generated6.BaseClass0>>(!!0,string) leave.s LV1
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV1} LV1:
		.locals init (valuetype Generated6.MyStruct56`1<class Generated6.BaseClass1> V_6)
		ldloca V_6
		initobj valuetype Generated6.MyStruct56`1<class Generated6.BaseClass1>
		.try {  ldloc V_6
				ldstr "MyStruct56::Method7.MI.443<System.Object>()#"
				call void Generated6.Generated6::M.MyStruct56.T<class Generated6.BaseClass1,valuetype Generated6.MyStruct56`1<class Generated6.BaseClass1>>(!!1,string) leave.s LV2
		} catch [mscorlib]System.Security.VerificationException { pop leave.s LV2} LV2:
		.try {  ldloc V_6
				ldstr "MyStruct56::Method7.MI.443<System.Object>()#"
				call void Generated6.Generated6::M.MyStruct56.B<valuetype Generated6.MyStruct56`1<class Generated6.BaseClass1>>(!!0,string) leave.s LV3
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
		.locals init (valuetype Generated6.MyStruct56`1<class Generated6.BaseClass0> V_7)
		ldloca V_7
		initobj valuetype Generated6.MyStruct56`1<class Generated6.BaseClass0>
		ldloc V_7
		box valuetype Generated6.MyStruct56`1<class Generated6.BaseClass0>
		ldloc V_7
		box valuetype Generated6.MyStruct56`1<class Generated6.BaseClass0>
		ldvirtftn instance string valuetype Generated6.MyStruct56`1<class Generated6.BaseClass0>::Method7<object>()
		calli default string(object)
		ldstr "MyStruct56::Method7.442<System.Object>()"
		ldstr "valuetype MyStruct56`1<class Generated6.BaseClass0> on type valuetype Generated6.MyStruct56`1<class Generated6.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_7
		box valuetype Generated6.MyStruct56`1<class Generated6.BaseClass0>
		ldloc V_7
		box valuetype Generated6.MyStruct56`1<class Generated6.BaseClass0>
		ldvirtftn instance string valuetype Generated6.MyStruct56`1<class Generated6.BaseClass0>::ClassMethod121()
		calli default string(object)
		ldstr "MyStruct56::ClassMethod121.444()"
		ldstr "valuetype MyStruct56`1<class Generated6.BaseClass0> on type valuetype Generated6.MyStruct56`1<class Generated6.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_7 box valuetype Generated6.MyStruct56`1<class Generated6.BaseClass0> ldnull 
		ldloc V_7 box valuetype Generated6.MyStruct56`1<class Generated6.BaseClass0>
		ldvirtftn instance bool valuetype Generated6.MyStruct56`1<class Generated6.BaseClass0>::Equals(object) calli default bool(object,object) pop
		ldloc V_7 box valuetype Generated6.MyStruct56`1<class Generated6.BaseClass0>
		ldloc V_7 box valuetype Generated6.MyStruct56`1<class Generated6.BaseClass0>
		ldvirtftn instance int32 valuetype Generated6.MyStruct56`1<class Generated6.BaseClass0>::GetHashCode() calli default int32(object) pop
		ldloc V_7 box valuetype Generated6.MyStruct56`1<class Generated6.BaseClass0>
		ldloc V_7 box valuetype Generated6.MyStruct56`1<class Generated6.BaseClass0>
		ldvirtftn instance string valuetype Generated6.MyStruct56`1<class Generated6.BaseClass0>::ToString() calli default string(object) pop
		ldloc V_7
		box valuetype Generated6.MyStruct56`1<class Generated6.BaseClass0>
		ldloc V_7
		box valuetype Generated6.MyStruct56`1<class Generated6.BaseClass0>
		ldvirtftn instance string class Generated6.IBase2`2<class Generated6.BaseClass0,class Generated6.BaseClass1>::Method7<object>()
		calli default string(object)
		ldstr "MyStruct56::Method7.MI.443<System.Object>()"
		ldstr "class IBase2`2<class Generated6.BaseClass0,class Generated6.BaseClass1> on type valuetype Generated6.MyStruct56`1<class Generated6.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		.locals init (valuetype Generated6.MyStruct56`1<class Generated6.BaseClass1> V_8)
		ldloca V_8
		initobj valuetype Generated6.MyStruct56`1<class Generated6.BaseClass1>
		ldloc V_8
		box valuetype Generated6.MyStruct56`1<class Generated6.BaseClass1>
		ldloc V_8
		box valuetype Generated6.MyStruct56`1<class Generated6.BaseClass1>
		ldvirtftn instance string valuetype Generated6.MyStruct56`1<class Generated6.BaseClass1>::Method7<object>()
		calli default string(object)
		ldstr "MyStruct56::Method7.442<System.Object>()"
		ldstr "valuetype MyStruct56`1<class Generated6.BaseClass1> on type valuetype Generated6.MyStruct56`1<class Generated6.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_8
		box valuetype Generated6.MyStruct56`1<class Generated6.BaseClass1>
		ldloc V_8
		box valuetype Generated6.MyStruct56`1<class Generated6.BaseClass1>
		ldvirtftn instance string valuetype Generated6.MyStruct56`1<class Generated6.BaseClass1>::ClassMethod121()
		calli default string(object)
		ldstr "MyStruct56::ClassMethod121.444()"
		ldstr "valuetype MyStruct56`1<class Generated6.BaseClass1> on type valuetype Generated6.MyStruct56`1<class Generated6.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc V_8 box valuetype Generated6.MyStruct56`1<class Generated6.BaseClass1> ldnull 
		ldloc V_8 box valuetype Generated6.MyStruct56`1<class Generated6.BaseClass1>
		ldvirtftn instance bool valuetype Generated6.MyStruct56`1<class Generated6.BaseClass1>::Equals(object) calli default bool(object,object) pop
		ldloc V_8 box valuetype Generated6.MyStruct56`1<class Generated6.BaseClass1>
		ldloc V_8 box valuetype Generated6.MyStruct56`1<class Generated6.BaseClass1>
		ldvirtftn instance int32 valuetype Generated6.MyStruct56`1<class Generated6.BaseClass1>::GetHashCode() calli default int32(object) pop
		ldloc V_8 box valuetype Generated6.MyStruct56`1<class Generated6.BaseClass1>
		ldloc V_8 box valuetype Generated6.MyStruct56`1<class Generated6.BaseClass1>
		ldvirtftn instance string valuetype Generated6.MyStruct56`1<class Generated6.BaseClass1>::ToString() calli default string(object) pop
		ldloc V_8
		box valuetype Generated6.MyStruct56`1<class Generated6.BaseClass1>
		ldloc V_8
		box valuetype Generated6.MyStruct56`1<class Generated6.BaseClass1>
		ldvirtftn instance string class Generated6.IBase2`2<class Generated6.BaseClass0,class Generated6.BaseClass1>::Method7<object>()
		calli default string(object)
		ldstr "MyStruct56::Method7.MI.443<System.Object>()"
		ldstr "class IBase2`2<class Generated6.BaseClass0,class Generated6.BaseClass1> on type valuetype Generated6.MyStruct56`1<class Generated6.BaseClass1>"
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
		call void Generated6.Generated6::MethodCallingTest()
		call void Generated6.Generated6::ConstrainedCallsTest()
		call void Generated6.Generated6::StructConstrainedInterfaceCallsTest()
		call void Generated6.Generated6::CalliTest()
		ldc.i4 100
		ret
	}
}
