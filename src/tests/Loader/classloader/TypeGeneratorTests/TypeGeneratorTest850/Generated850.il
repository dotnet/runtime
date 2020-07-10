// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) .ver 4:0:0:0 }
.assembly extern TestFramework { .publickeytoken = ( B0 3F 5F 7F 11 D5 0A 3A )  }

//TYPES IN FORWARDER ASSEMBLIES:

//TEST ASSEMBLY:
.assembly Generated850 { .hash algorithm 0x00008004 }
.module Generated850.exe

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
.class public G3_C1322`1<T0> 
		extends class G2_C354`2<!T0,!T0>
		implements class IBase2`2<class BaseClass0,!T0> 
{
	.method public hidebysig virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G3_C1322::Method7.15527<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod4022() cil managed noinlining { 
		ldstr "G3_C1322::ClassMethod4022.15528()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod4023() cil managed noinlining { 
		ldstr "G3_C1322::ClassMethod4023.15529()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod4024<M0>() cil managed noinlining { 
		ldstr "G3_C1322::ClassMethod4024.15530<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod4025<M0>() cil managed noinlining { 
		ldstr "G3_C1322::ClassMethod4025.15531<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C354<T0,T0>.ClassMethod2049'() cil managed noinlining { 
		.override method instance string class G2_C354`2<!T0,!T0>::ClassMethod2049()
		ldstr "G3_C1322::ClassMethod2049.MI.15532()"
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class G2_C354`2<!T0,!T0>::.ctor()
		ret
	}
}
.class public G2_C354`2<T0, T1> 
		extends class G1_C7`2<class BaseClass1,class BaseClass1>
		implements class IBase2`2<class BaseClass0,class BaseClass1>, class IBase1`1<class BaseClass1> 
{
	.method public hidebysig newslot virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G2_C354::Method7.8072<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase2<class BaseClass0,class BaseClass1>.Method7'<M0>() cil managed noinlining { 
		.override method instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<[1]>()
		ldstr "G2_C354::Method7.MI.8073<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig virtual instance string Method4() cil managed noinlining { 
		ldstr "G2_C354::Method4.8074()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class BaseClass1>.Method4'() cil managed noinlining { 
		.override method instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G2_C354::Method4.MI.8075()"
		ret
	}
	.method public hidebysig virtual instance string Method5() cil managed noinlining { 
		ldstr "G2_C354::Method5.8076()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class BaseClass1>.Method5'() cil managed noinlining { 
		.override method instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G2_C354::Method5.MI.8077()"
		ret
	}
	.method public hidebysig virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G2_C354::Method6.8078<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod2049() cil managed noinlining { 
		ldstr "G2_C354::ClassMethod2049.8079()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C7<class BaseClass1,class BaseClass1>.ClassMethod1328'() cil managed noinlining { 
		.override method instance string class G1_C7`2<class BaseClass1,class BaseClass1>::ClassMethod1328()
		ldstr "G2_C354::ClassMethod1328.MI.8080()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C7<class BaseClass1,class BaseClass1>.ClassMethod1329'() cil managed noinlining { 
		.override method instance string class G1_C7`2<class BaseClass1,class BaseClass1>::ClassMethod1329()
		ldstr "G2_C354::ClassMethod1329.MI.8081()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C7<class BaseClass1,class BaseClass1>.ClassMethod1331'<M0>() cil managed noinlining { 
		.override method instance string class G1_C7`2<class BaseClass1,class BaseClass1>::ClassMethod1331<[1]>()
		ldstr "G2_C354::ClassMethod1331.MI.8082<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class G1_C7`2<class BaseClass1,class BaseClass1>::.ctor()
		ret
	}
}
.class interface public abstract IBase2`2<+T0, -T1> 
{
	.method public hidebysig newslot abstract virtual instance string Method7<M0>() cil managed { }
}
.class public abstract G1_C7`2<T0, T1> 
		implements class IBase1`1<!T0>, class IBase2`2<class BaseClass1,!T0> 
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
		.override method instance string class IBase1`1<!T0>::Method6<[1]>()
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
	.method public hidebysig newslot virtual instance string 'IBase2<class BaseClass1,T0>.Method7'<M0>() cil managed noinlining { 
		.override method instance string class IBase2`2<class BaseClass1,!T0>::Method7<[1]>()
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
	.method static void M.G3_C1322.T<T0,(class G3_C1322`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 18
		.locals init (string[] actualResults)
		ldc.i4.s    13
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1322.T<T0,(class G3_C1322`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 13
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1322`1<!!T0>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1322`1<!!T0>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1322`1<!!T0>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1322`1<!!T0>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1322`1<!!T0>::ClassMethod2049()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1322`1<!!T0>::ClassMethod4022()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1322`1<!!T0>::ClassMethod4023()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1322`1<!!T0>::ClassMethod4024<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1322`1<!!T0>::ClassMethod4025<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1322`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1322`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1322`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1322`1<!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1322.A<(class G3_C1322`1<class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 18
		.locals init (string[] actualResults)
		ldc.i4.s    13
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1322.A<(class G3_C1322`1<class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 13
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1322`1<class BaseClass0>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1322`1<class BaseClass0>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1322`1<class BaseClass0>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1322`1<class BaseClass0>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1322`1<class BaseClass0>::ClassMethod2049()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1322`1<class BaseClass0>::ClassMethod4022()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1322`1<class BaseClass0>::ClassMethod4023()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1322`1<class BaseClass0>::ClassMethod4024<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1322`1<class BaseClass0>::ClassMethod4025<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1322`1<class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1322`1<class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1322`1<class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1322`1<class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1322.B<(class G3_C1322`1<class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 18
		.locals init (string[] actualResults)
		ldc.i4.s    13
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1322.B<(class G3_C1322`1<class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 13
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1322`1<class BaseClass1>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1322`1<class BaseClass1>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1322`1<class BaseClass1>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1322`1<class BaseClass1>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1322`1<class BaseClass1>::ClassMethod2049()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1322`1<class BaseClass1>::ClassMethod4022()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1322`1<class BaseClass1>::ClassMethod4023()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1322`1<class BaseClass1>::ClassMethod4024<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1322`1<class BaseClass1>::ClassMethod4025<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1322`1<class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1322`1<class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1322`1<class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1322`1<class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C354.T.T<T0,T1,(class G2_C354`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C354.T.T<T0,T1,(class G2_C354`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C354`2<!!T0,!!T1>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C354`2<!!T0,!!T1>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C354`2<!!T0,!!T1>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C354`2<!!T0,!!T1>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C354`2<!!T0,!!T1>::ClassMethod2049()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C354`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C354`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C354`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C354`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C354.A.T<T1,(class G2_C354`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C354.A.T<T1,(class G2_C354`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C354`2<class BaseClass0,!!T1>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C354`2<class BaseClass0,!!T1>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C354`2<class BaseClass0,!!T1>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C354`2<class BaseClass0,!!T1>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C354`2<class BaseClass0,!!T1>::ClassMethod2049()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C354`2<class BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C354`2<class BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C354`2<class BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C354`2<class BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C354.A.A<(class G2_C354`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C354.A.A<(class G2_C354`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C354`2<class BaseClass0,class BaseClass0>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C354`2<class BaseClass0,class BaseClass0>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C354`2<class BaseClass0,class BaseClass0>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C354`2<class BaseClass0,class BaseClass0>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C354`2<class BaseClass0,class BaseClass0>::ClassMethod2049()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C354`2<class BaseClass0,class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C354`2<class BaseClass0,class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C354`2<class BaseClass0,class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C354`2<class BaseClass0,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C354.A.B<(class G2_C354`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C354.A.B<(class G2_C354`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C354`2<class BaseClass0,class BaseClass1>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C354`2<class BaseClass0,class BaseClass1>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C354`2<class BaseClass0,class BaseClass1>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C354`2<class BaseClass0,class BaseClass1>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C354`2<class BaseClass0,class BaseClass1>::ClassMethod2049()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C354`2<class BaseClass0,class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C354`2<class BaseClass0,class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C354`2<class BaseClass0,class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C354`2<class BaseClass0,class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C354.B.T<T1,(class G2_C354`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C354.B.T<T1,(class G2_C354`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C354`2<class BaseClass1,!!T1>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C354`2<class BaseClass1,!!T1>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C354`2<class BaseClass1,!!T1>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C354`2<class BaseClass1,!!T1>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C354`2<class BaseClass1,!!T1>::ClassMethod2049()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C354`2<class BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C354`2<class BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C354`2<class BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C354`2<class BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C354.B.A<(class G2_C354`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C354.B.A<(class G2_C354`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C354`2<class BaseClass1,class BaseClass0>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C354`2<class BaseClass1,class BaseClass0>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C354`2<class BaseClass1,class BaseClass0>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C354`2<class BaseClass1,class BaseClass0>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C354`2<class BaseClass1,class BaseClass0>::ClassMethod2049()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C354`2<class BaseClass1,class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C354`2<class BaseClass1,class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C354`2<class BaseClass1,class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C354`2<class BaseClass1,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C354.B.B<(class G2_C354`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C354.B.B<(class G2_C354`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C354`2<class BaseClass1,class BaseClass1>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C354`2<class BaseClass1,class BaseClass1>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C354`2<class BaseClass1,class BaseClass1>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C354`2<class BaseClass1,class BaseClass1>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C354`2<class BaseClass1,class BaseClass1>::ClassMethod2049()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C354`2<class BaseClass1,class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C354`2<class BaseClass1,class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C354`2<class BaseClass1,class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C354`2<class BaseClass1,class BaseClass1>::Method7<object>()
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
	.method static void M.G1_C7.T.T<T0,T1,(class G1_C7`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C7.T.T<T0,T1,(class G1_C7`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<!!T0,!!T1>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<!!T0,!!T1>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<!!T0,!!T1>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<!!T0,!!T1>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C7.A.T<T1,(class G1_C7`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C7.A.T<T1,(class G1_C7`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass0,!!T1>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass0,!!T1>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass0,!!T1>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass0,!!T1>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C7.A.A<(class G1_C7`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C7.A.A<(class G1_C7`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass0,class BaseClass0>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass0,class BaseClass0>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass0,class BaseClass0>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass0,class BaseClass0>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass0,class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass0,class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass0,class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass0,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C7.A.B<(class G1_C7`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C7.A.B<(class G1_C7`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass0,class BaseClass1>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass0,class BaseClass1>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass0,class BaseClass1>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass0,class BaseClass1>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass0,class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass0,class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass0,class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass0,class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C7.B.T<T1,(class G1_C7`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C7.B.T<T1,(class G1_C7`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass1,!!T1>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass1,!!T1>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass1,!!T1>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass1,!!T1>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C7.B.A<(class G1_C7`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C7.B.A<(class G1_C7`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass1,class BaseClass0>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass1,class BaseClass0>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass1,class BaseClass0>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass1,class BaseClass0>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass1,class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass1,class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass1,class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass1,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C7.B.B<(class G1_C7`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C7.B.B<(class G1_C7`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass1,class BaseClass1>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass1,class BaseClass1>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass1,class BaseClass1>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass1,class BaseClass1>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass1,class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass1,class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass1,class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C7`2<class BaseClass1,class BaseClass1>::Method7<object>()
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
	.method public hidebysig static void MethodCallingTest() cil managed
	{
		.maxstack  10
		.locals init (object V_0)
		ldstr "========================== Method Calling Test =========================="
		call void [mscorlib]System.Console::WriteLine(string)
		newobj instance void class G3_C1322`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass1>::ClassMethod1331<object>()
		ldstr "G2_C354::ClassMethod1331.MI.8082<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G3_C1322`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass1>::ClassMethod1330<object>()
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G3_C1322`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass1>::ClassMethod1329()
		ldstr "G2_C354::ClassMethod1329.MI.8081()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G3_C1322`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass1>::ClassMethod1328()
		ldstr "G2_C354::ClassMethod1328.MI.8080()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G3_C1322`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C7::Method7.4815<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G3_C1322`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass1>::Method6<object>()
		ldstr "G2_C354::Method6.8078<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G3_C1322`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass1>::Method5()
		ldstr "G2_C354::Method5.8076()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G3_C1322`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass1>::Method4()
		ldstr "G2_C354::Method4.8074()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G3_C1322`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G2_C354::Method4.MI.8075()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1322`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G2_C354::Method5.MI.8077()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1322`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C354::Method6.8078<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1322`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C7::Method7.MI.4816<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1322`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C354::Method4.MI.8075()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1322`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C354::Method5.MI.8077()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1322`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C354::Method6.8078<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1322`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G3_C1322::Method7.15527<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1322`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C354`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C354`2<class BaseClass0,class BaseClass0>::ClassMethod2049()
		ldstr "G3_C1322::ClassMethod2049.MI.15532()"
		ldstr "class G2_C354`2<class BaseClass0,class BaseClass0> on type class G3_C1322`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C354`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C354`2<class BaseClass0,class BaseClass0>::Method6<object>()
		ldstr "G2_C354::Method6.8078<System.Object>()"
		ldstr "class G2_C354`2<class BaseClass0,class BaseClass0> on type class G3_C1322`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C354`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C354`2<class BaseClass0,class BaseClass0>::Method5()
		ldstr "G2_C354::Method5.8076()"
		ldstr "class G2_C354`2<class BaseClass0,class BaseClass0> on type class G3_C1322`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C354`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C354`2<class BaseClass0,class BaseClass0>::Method4()
		ldstr "G2_C354::Method4.8074()"
		ldstr "class G2_C354`2<class BaseClass0,class BaseClass0> on type class G3_C1322`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C354`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C354`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G3_C1322::Method7.15527<System.Object>()"
		ldstr "class G2_C354`2<class BaseClass0,class BaseClass0> on type class G3_C1322`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C354`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C354`2<class BaseClass0,class BaseClass0>::ClassMethod1331<object>()
		ldstr "G2_C354::ClassMethod1331.MI.8082<System.Object>()"
		ldstr "class G2_C354`2<class BaseClass0,class BaseClass0> on type class G3_C1322`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C354`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C354`2<class BaseClass0,class BaseClass0>::ClassMethod1330<object>()
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G2_C354`2<class BaseClass0,class BaseClass0> on type class G3_C1322`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C354`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C354`2<class BaseClass0,class BaseClass0>::ClassMethod1329()
		ldstr "G2_C354::ClassMethod1329.MI.8081()"
		ldstr "class G2_C354`2<class BaseClass0,class BaseClass0> on type class G3_C1322`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C354`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C354`2<class BaseClass0,class BaseClass0>::ClassMethod1328()
		ldstr "G2_C354::ClassMethod1328.MI.8080()"
		ldstr "class G2_C354`2<class BaseClass0,class BaseClass0> on type class G3_C1322`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G3_C1322`1<class BaseClass0>
		callvirt instance string class G3_C1322`1<class BaseClass0>::ClassMethod4025<object>()
		ldstr "G3_C1322::ClassMethod4025.15531<System.Object>()"
		ldstr "class G3_C1322`1<class BaseClass0> on type class G3_C1322`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1322`1<class BaseClass0>
		callvirt instance string class G3_C1322`1<class BaseClass0>::ClassMethod4024<object>()
		ldstr "G3_C1322::ClassMethod4024.15530<System.Object>()"
		ldstr "class G3_C1322`1<class BaseClass0> on type class G3_C1322`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1322`1<class BaseClass0>
		callvirt instance string class G3_C1322`1<class BaseClass0>::ClassMethod4023()
		ldstr "G3_C1322::ClassMethod4023.15529()"
		ldstr "class G3_C1322`1<class BaseClass0> on type class G3_C1322`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1322`1<class BaseClass0>
		callvirt instance string class G3_C1322`1<class BaseClass0>::ClassMethod4022()
		ldstr "G3_C1322::ClassMethod4022.15528()"
		ldstr "class G3_C1322`1<class BaseClass0> on type class G3_C1322`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1322`1<class BaseClass0>
		callvirt instance string class G3_C1322`1<class BaseClass0>::Method7<object>()
		ldstr "G3_C1322::Method7.15527<System.Object>()"
		ldstr "class G3_C1322`1<class BaseClass0> on type class G3_C1322`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1322`1<class BaseClass0>
		callvirt instance string class G3_C1322`1<class BaseClass0>::ClassMethod2049()
		ldstr "G3_C1322::ClassMethod2049.MI.15532()"
		ldstr "class G3_C1322`1<class BaseClass0> on type class G3_C1322`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1322`1<class BaseClass0>
		callvirt instance string class G3_C1322`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C354::Method6.8078<System.Object>()"
		ldstr "class G3_C1322`1<class BaseClass0> on type class G3_C1322`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1322`1<class BaseClass0>
		callvirt instance string class G3_C1322`1<class BaseClass0>::Method5()
		ldstr "G2_C354::Method5.8076()"
		ldstr "class G3_C1322`1<class BaseClass0> on type class G3_C1322`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1322`1<class BaseClass0>
		callvirt instance string class G3_C1322`1<class BaseClass0>::Method4()
		ldstr "G2_C354::Method4.8074()"
		ldstr "class G3_C1322`1<class BaseClass0> on type class G3_C1322`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1322`1<class BaseClass0>
		callvirt instance string class G3_C1322`1<class BaseClass0>::ClassMethod1331<object>()
		ldstr "G2_C354::ClassMethod1331.MI.8082<System.Object>()"
		ldstr "class G3_C1322`1<class BaseClass0> on type class G3_C1322`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1322`1<class BaseClass0>
		callvirt instance string class G3_C1322`1<class BaseClass0>::ClassMethod1330<object>()
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G3_C1322`1<class BaseClass0> on type class G3_C1322`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1322`1<class BaseClass0>
		callvirt instance string class G3_C1322`1<class BaseClass0>::ClassMethod1329()
		ldstr "G2_C354::ClassMethod1329.MI.8081()"
		ldstr "class G3_C1322`1<class BaseClass0> on type class G3_C1322`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1322`1<class BaseClass0>
		callvirt instance string class G3_C1322`1<class BaseClass0>::ClassMethod1328()
		ldstr "G2_C354::ClassMethod1328.MI.8080()"
		ldstr "class G3_C1322`1<class BaseClass0> on type class G3_C1322`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G3_C1322::Method7.15527<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1322`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G3_C1322`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass1>::ClassMethod1331<object>()
		ldstr "G2_C354::ClassMethod1331.MI.8082<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G3_C1322`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass1>::ClassMethod1330<object>()
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G3_C1322`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass1>::ClassMethod1329()
		ldstr "G2_C354::ClassMethod1329.MI.8081()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G3_C1322`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass1>::ClassMethod1328()
		ldstr "G2_C354::ClassMethod1328.MI.8080()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G3_C1322`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C7::Method7.4815<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G3_C1322`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass1>::Method6<object>()
		ldstr "G2_C354::Method6.8078<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G3_C1322`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass1>::Method5()
		ldstr "G2_C354::Method5.8076()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G3_C1322`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass1>::Method4()
		ldstr "G2_C354::Method4.8074()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G3_C1322`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G2_C354::Method4.MI.8075()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1322`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G2_C354::Method5.MI.8077()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1322`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C354::Method6.8078<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1322`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C7::Method7.MI.4816<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1322`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C354::Method4.MI.8075()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1322`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C354::Method5.MI.8077()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1322`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C354::Method6.8078<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1322`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G3_C1322::Method7.15527<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1322`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C354`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C354`2<class BaseClass1,class BaseClass1>::ClassMethod2049()
		ldstr "G3_C1322::ClassMethod2049.MI.15532()"
		ldstr "class G2_C354`2<class BaseClass1,class BaseClass1> on type class G3_C1322`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C354`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C354`2<class BaseClass1,class BaseClass1>::Method6<object>()
		ldstr "G2_C354::Method6.8078<System.Object>()"
		ldstr "class G2_C354`2<class BaseClass1,class BaseClass1> on type class G3_C1322`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C354`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C354`2<class BaseClass1,class BaseClass1>::Method5()
		ldstr "G2_C354::Method5.8076()"
		ldstr "class G2_C354`2<class BaseClass1,class BaseClass1> on type class G3_C1322`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C354`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C354`2<class BaseClass1,class BaseClass1>::Method4()
		ldstr "G2_C354::Method4.8074()"
		ldstr "class G2_C354`2<class BaseClass1,class BaseClass1> on type class G3_C1322`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C354`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C354`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G3_C1322::Method7.15527<System.Object>()"
		ldstr "class G2_C354`2<class BaseClass1,class BaseClass1> on type class G3_C1322`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C354`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C354`2<class BaseClass1,class BaseClass1>::ClassMethod1331<object>()
		ldstr "G2_C354::ClassMethod1331.MI.8082<System.Object>()"
		ldstr "class G2_C354`2<class BaseClass1,class BaseClass1> on type class G3_C1322`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C354`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C354`2<class BaseClass1,class BaseClass1>::ClassMethod1330<object>()
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G2_C354`2<class BaseClass1,class BaseClass1> on type class G3_C1322`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C354`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C354`2<class BaseClass1,class BaseClass1>::ClassMethod1329()
		ldstr "G2_C354::ClassMethod1329.MI.8081()"
		ldstr "class G2_C354`2<class BaseClass1,class BaseClass1> on type class G3_C1322`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C354`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C354`2<class BaseClass1,class BaseClass1>::ClassMethod1328()
		ldstr "G2_C354::ClassMethod1328.MI.8080()"
		ldstr "class G2_C354`2<class BaseClass1,class BaseClass1> on type class G3_C1322`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G3_C1322`1<class BaseClass1>
		callvirt instance string class G3_C1322`1<class BaseClass1>::ClassMethod4025<object>()
		ldstr "G3_C1322::ClassMethod4025.15531<System.Object>()"
		ldstr "class G3_C1322`1<class BaseClass1> on type class G3_C1322`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1322`1<class BaseClass1>
		callvirt instance string class G3_C1322`1<class BaseClass1>::ClassMethod4024<object>()
		ldstr "G3_C1322::ClassMethod4024.15530<System.Object>()"
		ldstr "class G3_C1322`1<class BaseClass1> on type class G3_C1322`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1322`1<class BaseClass1>
		callvirt instance string class G3_C1322`1<class BaseClass1>::ClassMethod4023()
		ldstr "G3_C1322::ClassMethod4023.15529()"
		ldstr "class G3_C1322`1<class BaseClass1> on type class G3_C1322`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1322`1<class BaseClass1>
		callvirt instance string class G3_C1322`1<class BaseClass1>::ClassMethod4022()
		ldstr "G3_C1322::ClassMethod4022.15528()"
		ldstr "class G3_C1322`1<class BaseClass1> on type class G3_C1322`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1322`1<class BaseClass1>
		callvirt instance string class G3_C1322`1<class BaseClass1>::Method7<object>()
		ldstr "G3_C1322::Method7.15527<System.Object>()"
		ldstr "class G3_C1322`1<class BaseClass1> on type class G3_C1322`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1322`1<class BaseClass1>
		callvirt instance string class G3_C1322`1<class BaseClass1>::ClassMethod2049()
		ldstr "G3_C1322::ClassMethod2049.MI.15532()"
		ldstr "class G3_C1322`1<class BaseClass1> on type class G3_C1322`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1322`1<class BaseClass1>
		callvirt instance string class G3_C1322`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C354::Method6.8078<System.Object>()"
		ldstr "class G3_C1322`1<class BaseClass1> on type class G3_C1322`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1322`1<class BaseClass1>
		callvirt instance string class G3_C1322`1<class BaseClass1>::Method5()
		ldstr "G2_C354::Method5.8076()"
		ldstr "class G3_C1322`1<class BaseClass1> on type class G3_C1322`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1322`1<class BaseClass1>
		callvirt instance string class G3_C1322`1<class BaseClass1>::Method4()
		ldstr "G2_C354::Method4.8074()"
		ldstr "class G3_C1322`1<class BaseClass1> on type class G3_C1322`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1322`1<class BaseClass1>
		callvirt instance string class G3_C1322`1<class BaseClass1>::ClassMethod1331<object>()
		ldstr "G2_C354::ClassMethod1331.MI.8082<System.Object>()"
		ldstr "class G3_C1322`1<class BaseClass1> on type class G3_C1322`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1322`1<class BaseClass1>
		callvirt instance string class G3_C1322`1<class BaseClass1>::ClassMethod1330<object>()
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G3_C1322`1<class BaseClass1> on type class G3_C1322`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1322`1<class BaseClass1>
		callvirt instance string class G3_C1322`1<class BaseClass1>::ClassMethod1329()
		ldstr "G2_C354::ClassMethod1329.MI.8081()"
		ldstr "class G3_C1322`1<class BaseClass1> on type class G3_C1322`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1322`1<class BaseClass1>
		callvirt instance string class G3_C1322`1<class BaseClass1>::ClassMethod1328()
		ldstr "G2_C354::ClassMethod1328.MI.8080()"
		ldstr "class G3_C1322`1<class BaseClass1> on type class G3_C1322`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C354`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass1>::ClassMethod1331<object>()
		ldstr "G2_C354::ClassMethod1331.MI.8082<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C354`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass1>::ClassMethod1330<object>()
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C354`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass1>::ClassMethod1329()
		ldstr "G2_C354::ClassMethod1329.MI.8081()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C354`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass1>::ClassMethod1328()
		ldstr "G2_C354::ClassMethod1328.MI.8080()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C354`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C7::Method7.4815<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C354`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass1>::Method6<object>()
		ldstr "G2_C354::Method6.8078<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C354`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass1>::Method5()
		ldstr "G2_C354::Method5.8076()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C354`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass1>::Method4()
		ldstr "G2_C354::Method4.8074()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C354`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G2_C354::Method4.MI.8075()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C354`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G2_C354::Method5.MI.8077()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C354`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C354::Method6.8078<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C354`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C7::Method7.MI.4816<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C354`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C354::Method4.MI.8075()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C354`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C354::Method5.MI.8077()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C354`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C354::Method6.8078<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C354`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C354::Method7.MI.8073<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C354`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C354`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C354`2<class BaseClass0,class BaseClass0>::ClassMethod2049()
		ldstr "G2_C354::ClassMethod2049.8079()"
		ldstr "class G2_C354`2<class BaseClass0,class BaseClass0> on type class G2_C354`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C354`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C354`2<class BaseClass0,class BaseClass0>::Method6<object>()
		ldstr "G2_C354::Method6.8078<System.Object>()"
		ldstr "class G2_C354`2<class BaseClass0,class BaseClass0> on type class G2_C354`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C354`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C354`2<class BaseClass0,class BaseClass0>::Method5()
		ldstr "G2_C354::Method5.8076()"
		ldstr "class G2_C354`2<class BaseClass0,class BaseClass0> on type class G2_C354`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C354`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C354`2<class BaseClass0,class BaseClass0>::Method4()
		ldstr "G2_C354::Method4.8074()"
		ldstr "class G2_C354`2<class BaseClass0,class BaseClass0> on type class G2_C354`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C354`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C354`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C354::Method7.8072<System.Object>()"
		ldstr "class G2_C354`2<class BaseClass0,class BaseClass0> on type class G2_C354`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C354`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C354`2<class BaseClass0,class BaseClass0>::ClassMethod1331<object>()
		ldstr "G2_C354::ClassMethod1331.MI.8082<System.Object>()"
		ldstr "class G2_C354`2<class BaseClass0,class BaseClass0> on type class G2_C354`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C354`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C354`2<class BaseClass0,class BaseClass0>::ClassMethod1330<object>()
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G2_C354`2<class BaseClass0,class BaseClass0> on type class G2_C354`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C354`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C354`2<class BaseClass0,class BaseClass0>::ClassMethod1329()
		ldstr "G2_C354::ClassMethod1329.MI.8081()"
		ldstr "class G2_C354`2<class BaseClass0,class BaseClass0> on type class G2_C354`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C354`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C354`2<class BaseClass0,class BaseClass0>::ClassMethod1328()
		ldstr "G2_C354::ClassMethod1328.MI.8080()"
		ldstr "class G2_C354`2<class BaseClass0,class BaseClass0> on type class G2_C354`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C354`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass1>::ClassMethod1331<object>()
		ldstr "G2_C354::ClassMethod1331.MI.8082<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C354`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass1>::ClassMethod1330<object>()
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C354`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass1>::ClassMethod1329()
		ldstr "G2_C354::ClassMethod1329.MI.8081()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C354`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass1>::ClassMethod1328()
		ldstr "G2_C354::ClassMethod1328.MI.8080()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C354`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C7::Method7.4815<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C354`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass1>::Method6<object>()
		ldstr "G2_C354::Method6.8078<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C354`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass1>::Method5()
		ldstr "G2_C354::Method5.8076()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C354`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass1>::Method4()
		ldstr "G2_C354::Method4.8074()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C354`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G2_C354::Method4.MI.8075()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C354`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G2_C354::Method5.MI.8077()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C354`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C354::Method6.8078<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C354`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C7::Method7.MI.4816<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C354`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C354::Method4.MI.8075()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C354`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C354::Method5.MI.8077()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C354`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C354::Method6.8078<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C354`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C354::Method7.MI.8073<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C354`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C354`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C354`2<class BaseClass0,class BaseClass1>::ClassMethod2049()
		ldstr "G2_C354::ClassMethod2049.8079()"
		ldstr "class G2_C354`2<class BaseClass0,class BaseClass1> on type class G2_C354`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C354`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C354`2<class BaseClass0,class BaseClass1>::Method6<object>()
		ldstr "G2_C354::Method6.8078<System.Object>()"
		ldstr "class G2_C354`2<class BaseClass0,class BaseClass1> on type class G2_C354`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C354`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C354`2<class BaseClass0,class BaseClass1>::Method5()
		ldstr "G2_C354::Method5.8076()"
		ldstr "class G2_C354`2<class BaseClass0,class BaseClass1> on type class G2_C354`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C354`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C354`2<class BaseClass0,class BaseClass1>::Method4()
		ldstr "G2_C354::Method4.8074()"
		ldstr "class G2_C354`2<class BaseClass0,class BaseClass1> on type class G2_C354`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C354`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C354`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C354::Method7.8072<System.Object>()"
		ldstr "class G2_C354`2<class BaseClass0,class BaseClass1> on type class G2_C354`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C354`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C354`2<class BaseClass0,class BaseClass1>::ClassMethod1331<object>()
		ldstr "G2_C354::ClassMethod1331.MI.8082<System.Object>()"
		ldstr "class G2_C354`2<class BaseClass0,class BaseClass1> on type class G2_C354`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C354`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C354`2<class BaseClass0,class BaseClass1>::ClassMethod1330<object>()
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G2_C354`2<class BaseClass0,class BaseClass1> on type class G2_C354`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C354`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C354`2<class BaseClass0,class BaseClass1>::ClassMethod1329()
		ldstr "G2_C354::ClassMethod1329.MI.8081()"
		ldstr "class G2_C354`2<class BaseClass0,class BaseClass1> on type class G2_C354`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C354`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C354`2<class BaseClass0,class BaseClass1>::ClassMethod1328()
		ldstr "G2_C354::ClassMethod1328.MI.8080()"
		ldstr "class G2_C354`2<class BaseClass0,class BaseClass1> on type class G2_C354`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C354`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass1>::ClassMethod1331<object>()
		ldstr "G2_C354::ClassMethod1331.MI.8082<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C354`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass1>::ClassMethod1330<object>()
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C354`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass1>::ClassMethod1329()
		ldstr "G2_C354::ClassMethod1329.MI.8081()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C354`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass1>::ClassMethod1328()
		ldstr "G2_C354::ClassMethod1328.MI.8080()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C354`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C7::Method7.4815<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C354`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass1>::Method6<object>()
		ldstr "G2_C354::Method6.8078<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C354`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass1>::Method5()
		ldstr "G2_C354::Method5.8076()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C354`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass1>::Method4()
		ldstr "G2_C354::Method4.8074()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C354`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G2_C354::Method4.MI.8075()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C354`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G2_C354::Method5.MI.8077()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C354`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C354::Method6.8078<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C354`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C7::Method7.MI.4816<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C354`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C354::Method4.MI.8075()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C354`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C354::Method5.MI.8077()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C354`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C354::Method6.8078<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C354`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C354::Method7.MI.8073<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C354`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C354`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C354`2<class BaseClass1,class BaseClass0>::ClassMethod2049()
		ldstr "G2_C354::ClassMethod2049.8079()"
		ldstr "class G2_C354`2<class BaseClass1,class BaseClass0> on type class G2_C354`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C354`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C354`2<class BaseClass1,class BaseClass0>::Method6<object>()
		ldstr "G2_C354::Method6.8078<System.Object>()"
		ldstr "class G2_C354`2<class BaseClass1,class BaseClass0> on type class G2_C354`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C354`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C354`2<class BaseClass1,class BaseClass0>::Method5()
		ldstr "G2_C354::Method5.8076()"
		ldstr "class G2_C354`2<class BaseClass1,class BaseClass0> on type class G2_C354`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C354`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C354`2<class BaseClass1,class BaseClass0>::Method4()
		ldstr "G2_C354::Method4.8074()"
		ldstr "class G2_C354`2<class BaseClass1,class BaseClass0> on type class G2_C354`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C354`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C354`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G2_C354::Method7.8072<System.Object>()"
		ldstr "class G2_C354`2<class BaseClass1,class BaseClass0> on type class G2_C354`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C354`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C354`2<class BaseClass1,class BaseClass0>::ClassMethod1331<object>()
		ldstr "G2_C354::ClassMethod1331.MI.8082<System.Object>()"
		ldstr "class G2_C354`2<class BaseClass1,class BaseClass0> on type class G2_C354`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C354`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C354`2<class BaseClass1,class BaseClass0>::ClassMethod1330<object>()
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G2_C354`2<class BaseClass1,class BaseClass0> on type class G2_C354`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C354`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C354`2<class BaseClass1,class BaseClass0>::ClassMethod1329()
		ldstr "G2_C354::ClassMethod1329.MI.8081()"
		ldstr "class G2_C354`2<class BaseClass1,class BaseClass0> on type class G2_C354`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C354`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C354`2<class BaseClass1,class BaseClass0>::ClassMethod1328()
		ldstr "G2_C354::ClassMethod1328.MI.8080()"
		ldstr "class G2_C354`2<class BaseClass1,class BaseClass0> on type class G2_C354`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C354`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass1>::ClassMethod1331<object>()
		ldstr "G2_C354::ClassMethod1331.MI.8082<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C354`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass1>::ClassMethod1330<object>()
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C354`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass1>::ClassMethod1329()
		ldstr "G2_C354::ClassMethod1329.MI.8081()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C354`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass1>::ClassMethod1328()
		ldstr "G2_C354::ClassMethod1328.MI.8080()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C354`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C7::Method7.4815<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C354`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass1>::Method6<object>()
		ldstr "G2_C354::Method6.8078<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C354`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass1>::Method5()
		ldstr "G2_C354::Method5.8076()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C354`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C7`2<class BaseClass1,class BaseClass1>::Method4()
		ldstr "G2_C354::Method4.8074()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C354`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G2_C354::Method4.MI.8075()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C354`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G2_C354::Method5.MI.8077()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C354`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C354::Method6.8078<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C354`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C7::Method7.MI.4816<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C354`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C354::Method4.MI.8075()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C354`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C354::Method5.MI.8077()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C354`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C354::Method6.8078<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C354`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C354::Method7.MI.8073<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C354`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C354`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C354`2<class BaseClass1,class BaseClass1>::ClassMethod2049()
		ldstr "G2_C354::ClassMethod2049.8079()"
		ldstr "class G2_C354`2<class BaseClass1,class BaseClass1> on type class G2_C354`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C354`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C354`2<class BaseClass1,class BaseClass1>::Method6<object>()
		ldstr "G2_C354::Method6.8078<System.Object>()"
		ldstr "class G2_C354`2<class BaseClass1,class BaseClass1> on type class G2_C354`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C354`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C354`2<class BaseClass1,class BaseClass1>::Method5()
		ldstr "G2_C354::Method5.8076()"
		ldstr "class G2_C354`2<class BaseClass1,class BaseClass1> on type class G2_C354`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C354`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C354`2<class BaseClass1,class BaseClass1>::Method4()
		ldstr "G2_C354::Method4.8074()"
		ldstr "class G2_C354`2<class BaseClass1,class BaseClass1> on type class G2_C354`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C354`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C354`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C354::Method7.8072<System.Object>()"
		ldstr "class G2_C354`2<class BaseClass1,class BaseClass1> on type class G2_C354`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C354`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C354`2<class BaseClass1,class BaseClass1>::ClassMethod1331<object>()
		ldstr "G2_C354::ClassMethod1331.MI.8082<System.Object>()"
		ldstr "class G2_C354`2<class BaseClass1,class BaseClass1> on type class G2_C354`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C354`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C354`2<class BaseClass1,class BaseClass1>::ClassMethod1330<object>()
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G2_C354`2<class BaseClass1,class BaseClass1> on type class G2_C354`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C354`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C354`2<class BaseClass1,class BaseClass1>::ClassMethod1329()
		ldstr "G2_C354::ClassMethod1329.MI.8081()"
		ldstr "class G2_C354`2<class BaseClass1,class BaseClass1> on type class G2_C354`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C354`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C354`2<class BaseClass1,class BaseClass1>::ClassMethod1328()
		ldstr "G2_C354::ClassMethod1328.MI.8080()"
		ldstr "class G2_C354`2<class BaseClass1,class BaseClass1> on type class G2_C354`2<class BaseClass1,class BaseClass1>"
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
		newobj instance void class G3_C1322`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C354::ClassMethod1328.MI.8080()#G2_C354::ClassMethod1329.MI.8081()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C354::ClassMethod1331.MI.8082<System.Object>()#G2_C354::Method4.8074()#G2_C354::Method5.8076()#G2_C354::Method6.8078<System.Object>()#G1_C7::Method7.4815<System.Object>()#"
		call void Framework::M.G1_C7.T.T<class BaseClass1,class BaseClass1,class G3_C1322`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C354::ClassMethod1328.MI.8080()#G2_C354::ClassMethod1329.MI.8081()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C354::ClassMethod1331.MI.8082<System.Object>()#G2_C354::Method4.8074()#G2_C354::Method5.8076()#G2_C354::Method6.8078<System.Object>()#G1_C7::Method7.4815<System.Object>()#"
		call void Framework::M.G1_C7.B.T<class BaseClass1,class G3_C1322`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C354::ClassMethod1328.MI.8080()#G2_C354::ClassMethod1329.MI.8081()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C354::ClassMethod1331.MI.8082<System.Object>()#G2_C354::Method4.8074()#G2_C354::Method5.8076()#G2_C354::Method6.8078<System.Object>()#G1_C7::Method7.4815<System.Object>()#"
		call void Framework::M.G1_C7.B.B<class G3_C1322`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C354::Method4.MI.8075()#G2_C354::Method5.MI.8077()#G2_C354::Method6.8078<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass1,class G3_C1322`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C354::Method4.MI.8075()#G2_C354::Method5.MI.8077()#G2_C354::Method6.8078<System.Object>()#"
		call void Framework::M.IBase1.B<class G3_C1322`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G3_C1322`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G3_C1322`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G3_C1322`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C354::Method4.MI.8075()#G2_C354::Method5.MI.8077()#G2_C354::Method6.8078<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G3_C1322`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C354::Method4.MI.8075()#G2_C354::Method5.MI.8077()#G2_C354::Method6.8078<System.Object>()#"
		call void Framework::M.IBase1.A<class G3_C1322`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1322::Method7.15527<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G3_C1322`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1322::Method7.15527<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G3_C1322`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1322::Method7.15527<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G3_C1322`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C354::ClassMethod1328.MI.8080()#G2_C354::ClassMethod1329.MI.8081()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C354::ClassMethod1331.MI.8082<System.Object>()#G3_C1322::ClassMethod2049.MI.15532()#G2_C354::Method4.8074()#G2_C354::Method5.8076()#G2_C354::Method6.8078<System.Object>()#G3_C1322::Method7.15527<System.Object>()#"
		call void Framework::M.G2_C354.T.T<class BaseClass0,class BaseClass0,class G3_C1322`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C354::ClassMethod1328.MI.8080()#G2_C354::ClassMethod1329.MI.8081()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C354::ClassMethod1331.MI.8082<System.Object>()#G3_C1322::ClassMethod2049.MI.15532()#G2_C354::Method4.8074()#G2_C354::Method5.8076()#G2_C354::Method6.8078<System.Object>()#G3_C1322::Method7.15527<System.Object>()#"
		call void Framework::M.G2_C354.A.T<class BaseClass0,class G3_C1322`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C354::ClassMethod1328.MI.8080()#G2_C354::ClassMethod1329.MI.8081()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C354::ClassMethod1331.MI.8082<System.Object>()#G3_C1322::ClassMethod2049.MI.15532()#G2_C354::Method4.8074()#G2_C354::Method5.8076()#G2_C354::Method6.8078<System.Object>()#G3_C1322::Method7.15527<System.Object>()#"
		call void Framework::M.G2_C354.A.A<class G3_C1322`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C354::ClassMethod1328.MI.8080()#G2_C354::ClassMethod1329.MI.8081()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C354::ClassMethod1331.MI.8082<System.Object>()#G3_C1322::ClassMethod2049.MI.15532()#G3_C1322::ClassMethod4022.15528()#G3_C1322::ClassMethod4023.15529()#G3_C1322::ClassMethod4024.15530<System.Object>()#G3_C1322::ClassMethod4025.15531<System.Object>()#G2_C354::Method4.8074()#G2_C354::Method5.8076()#G2_C354::Method6.8078<System.Object>()#G3_C1322::Method7.15527<System.Object>()#"
		call void Framework::M.G3_C1322.T<class BaseClass0,class G3_C1322`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C354::ClassMethod1328.MI.8080()#G2_C354::ClassMethod1329.MI.8081()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C354::ClassMethod1331.MI.8082<System.Object>()#G3_C1322::ClassMethod2049.MI.15532()#G3_C1322::ClassMethod4022.15528()#G3_C1322::ClassMethod4023.15529()#G3_C1322::ClassMethod4024.15530<System.Object>()#G3_C1322::ClassMethod4025.15531<System.Object>()#G2_C354::Method4.8074()#G2_C354::Method5.8076()#G2_C354::Method6.8078<System.Object>()#G3_C1322::Method7.15527<System.Object>()#"
		call void Framework::M.G3_C1322.A<class G3_C1322`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1322::Method7.15527<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G3_C1322`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1322::Method7.15527<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass0,class G3_C1322`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1322::Method7.15527<System.Object>()#"
		call void Framework::M.IBase2.A.A<class G3_C1322`1<class BaseClass0>>(!!0,string)
		newobj instance void class G3_C1322`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C354::ClassMethod1328.MI.8080()#G2_C354::ClassMethod1329.MI.8081()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C354::ClassMethod1331.MI.8082<System.Object>()#G2_C354::Method4.8074()#G2_C354::Method5.8076()#G2_C354::Method6.8078<System.Object>()#G1_C7::Method7.4815<System.Object>()#"
		call void Framework::M.G1_C7.T.T<class BaseClass1,class BaseClass1,class G3_C1322`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C354::ClassMethod1328.MI.8080()#G2_C354::ClassMethod1329.MI.8081()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C354::ClassMethod1331.MI.8082<System.Object>()#G2_C354::Method4.8074()#G2_C354::Method5.8076()#G2_C354::Method6.8078<System.Object>()#G1_C7::Method7.4815<System.Object>()#"
		call void Framework::M.G1_C7.B.T<class BaseClass1,class G3_C1322`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C354::ClassMethod1328.MI.8080()#G2_C354::ClassMethod1329.MI.8081()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C354::ClassMethod1331.MI.8082<System.Object>()#G2_C354::Method4.8074()#G2_C354::Method5.8076()#G2_C354::Method6.8078<System.Object>()#G1_C7::Method7.4815<System.Object>()#"
		call void Framework::M.G1_C7.B.B<class G3_C1322`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C354::Method4.MI.8075()#G2_C354::Method5.MI.8077()#G2_C354::Method6.8078<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass1,class G3_C1322`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C354::Method4.MI.8075()#G2_C354::Method5.MI.8077()#G2_C354::Method6.8078<System.Object>()#"
		call void Framework::M.IBase1.B<class G3_C1322`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G3_C1322`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G3_C1322`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G3_C1322`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C354::Method4.MI.8075()#G2_C354::Method5.MI.8077()#G2_C354::Method6.8078<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G3_C1322`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C354::Method4.MI.8075()#G2_C354::Method5.MI.8077()#G2_C354::Method6.8078<System.Object>()#"
		call void Framework::M.IBase1.A<class G3_C1322`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1322::Method7.15527<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G3_C1322`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1322::Method7.15527<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G3_C1322`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1322::Method7.15527<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G3_C1322`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C354::ClassMethod1328.MI.8080()#G2_C354::ClassMethod1329.MI.8081()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C354::ClassMethod1331.MI.8082<System.Object>()#G3_C1322::ClassMethod2049.MI.15532()#G2_C354::Method4.8074()#G2_C354::Method5.8076()#G2_C354::Method6.8078<System.Object>()#G3_C1322::Method7.15527<System.Object>()#"
		call void Framework::M.G2_C354.T.T<class BaseClass1,class BaseClass1,class G3_C1322`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C354::ClassMethod1328.MI.8080()#G2_C354::ClassMethod1329.MI.8081()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C354::ClassMethod1331.MI.8082<System.Object>()#G3_C1322::ClassMethod2049.MI.15532()#G2_C354::Method4.8074()#G2_C354::Method5.8076()#G2_C354::Method6.8078<System.Object>()#G3_C1322::Method7.15527<System.Object>()#"
		call void Framework::M.G2_C354.B.T<class BaseClass1,class G3_C1322`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C354::ClassMethod1328.MI.8080()#G2_C354::ClassMethod1329.MI.8081()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C354::ClassMethod1331.MI.8082<System.Object>()#G3_C1322::ClassMethod2049.MI.15532()#G2_C354::Method4.8074()#G2_C354::Method5.8076()#G2_C354::Method6.8078<System.Object>()#G3_C1322::Method7.15527<System.Object>()#"
		call void Framework::M.G2_C354.B.B<class G3_C1322`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C354::ClassMethod1328.MI.8080()#G2_C354::ClassMethod1329.MI.8081()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C354::ClassMethod1331.MI.8082<System.Object>()#G3_C1322::ClassMethod2049.MI.15532()#G3_C1322::ClassMethod4022.15528()#G3_C1322::ClassMethod4023.15529()#G3_C1322::ClassMethod4024.15530<System.Object>()#G3_C1322::ClassMethod4025.15531<System.Object>()#G2_C354::Method4.8074()#G2_C354::Method5.8076()#G2_C354::Method6.8078<System.Object>()#G3_C1322::Method7.15527<System.Object>()#"
		call void Framework::M.G3_C1322.T<class BaseClass1,class G3_C1322`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C354::ClassMethod1328.MI.8080()#G2_C354::ClassMethod1329.MI.8081()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C354::ClassMethod1331.MI.8082<System.Object>()#G3_C1322::ClassMethod2049.MI.15532()#G3_C1322::ClassMethod4022.15528()#G3_C1322::ClassMethod4023.15529()#G3_C1322::ClassMethod4024.15530<System.Object>()#G3_C1322::ClassMethod4025.15531<System.Object>()#G2_C354::Method4.8074()#G2_C354::Method5.8076()#G2_C354::Method6.8078<System.Object>()#G3_C1322::Method7.15527<System.Object>()#"
		call void Framework::M.G3_C1322.B<class G3_C1322`1<class BaseClass1>>(!!0,string)
		newobj instance void class G2_C354`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C354::ClassMethod1328.MI.8080()#G2_C354::ClassMethod1329.MI.8081()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C354::ClassMethod1331.MI.8082<System.Object>()#G2_C354::Method4.8074()#G2_C354::Method5.8076()#G2_C354::Method6.8078<System.Object>()#G1_C7::Method7.4815<System.Object>()#"
		call void Framework::M.G1_C7.T.T<class BaseClass1,class BaseClass1,class G2_C354`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C354::ClassMethod1328.MI.8080()#G2_C354::ClassMethod1329.MI.8081()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C354::ClassMethod1331.MI.8082<System.Object>()#G2_C354::Method4.8074()#G2_C354::Method5.8076()#G2_C354::Method6.8078<System.Object>()#G1_C7::Method7.4815<System.Object>()#"
		call void Framework::M.G1_C7.B.T<class BaseClass1,class G2_C354`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C354::ClassMethod1328.MI.8080()#G2_C354::ClassMethod1329.MI.8081()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C354::ClassMethod1331.MI.8082<System.Object>()#G2_C354::Method4.8074()#G2_C354::Method5.8076()#G2_C354::Method6.8078<System.Object>()#G1_C7::Method7.4815<System.Object>()#"
		call void Framework::M.G1_C7.B.B<class G2_C354`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C354::Method4.MI.8075()#G2_C354::Method5.MI.8077()#G2_C354::Method6.8078<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass1,class G2_C354`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C354::Method4.MI.8075()#G2_C354::Method5.MI.8077()#G2_C354::Method6.8078<System.Object>()#"
		call void Framework::M.IBase1.B<class G2_C354`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G2_C354`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G2_C354`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G2_C354`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C354::Method4.MI.8075()#G2_C354::Method5.MI.8077()#G2_C354::Method6.8078<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G2_C354`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C354::Method4.MI.8075()#G2_C354::Method5.MI.8077()#G2_C354::Method6.8078<System.Object>()#"
		call void Framework::M.IBase1.A<class G2_C354`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C354::Method7.MI.8073<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C354`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C354::Method7.MI.8073<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G2_C354`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C354::Method7.MI.8073<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G2_C354`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C354::ClassMethod1328.MI.8080()#G2_C354::ClassMethod1329.MI.8081()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C354::ClassMethod1331.MI.8082<System.Object>()#G2_C354::ClassMethod2049.8079()#G2_C354::Method4.8074()#G2_C354::Method5.8076()#G2_C354::Method6.8078<System.Object>()#G2_C354::Method7.8072<System.Object>()#"
		call void Framework::M.G2_C354.T.T<class BaseClass0,class BaseClass0,class G2_C354`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C354::ClassMethod1328.MI.8080()#G2_C354::ClassMethod1329.MI.8081()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C354::ClassMethod1331.MI.8082<System.Object>()#G2_C354::ClassMethod2049.8079()#G2_C354::Method4.8074()#G2_C354::Method5.8076()#G2_C354::Method6.8078<System.Object>()#G2_C354::Method7.8072<System.Object>()#"
		call void Framework::M.G2_C354.A.T<class BaseClass0,class G2_C354`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C354::ClassMethod1328.MI.8080()#G2_C354::ClassMethod1329.MI.8081()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C354::ClassMethod1331.MI.8082<System.Object>()#G2_C354::ClassMethod2049.8079()#G2_C354::Method4.8074()#G2_C354::Method5.8076()#G2_C354::Method6.8078<System.Object>()#G2_C354::Method7.8072<System.Object>()#"
		call void Framework::M.G2_C354.A.A<class G2_C354`2<class BaseClass0,class BaseClass0>>(!!0,string)
		newobj instance void class G2_C354`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C354::ClassMethod1328.MI.8080()#G2_C354::ClassMethod1329.MI.8081()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C354::ClassMethod1331.MI.8082<System.Object>()#G2_C354::Method4.8074()#G2_C354::Method5.8076()#G2_C354::Method6.8078<System.Object>()#G1_C7::Method7.4815<System.Object>()#"
		call void Framework::M.G1_C7.T.T<class BaseClass1,class BaseClass1,class G2_C354`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C354::ClassMethod1328.MI.8080()#G2_C354::ClassMethod1329.MI.8081()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C354::ClassMethod1331.MI.8082<System.Object>()#G2_C354::Method4.8074()#G2_C354::Method5.8076()#G2_C354::Method6.8078<System.Object>()#G1_C7::Method7.4815<System.Object>()#"
		call void Framework::M.G1_C7.B.T<class BaseClass1,class G2_C354`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C354::ClassMethod1328.MI.8080()#G2_C354::ClassMethod1329.MI.8081()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C354::ClassMethod1331.MI.8082<System.Object>()#G2_C354::Method4.8074()#G2_C354::Method5.8076()#G2_C354::Method6.8078<System.Object>()#G1_C7::Method7.4815<System.Object>()#"
		call void Framework::M.G1_C7.B.B<class G2_C354`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C354::Method4.MI.8075()#G2_C354::Method5.MI.8077()#G2_C354::Method6.8078<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass1,class G2_C354`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C354::Method4.MI.8075()#G2_C354::Method5.MI.8077()#G2_C354::Method6.8078<System.Object>()#"
		call void Framework::M.IBase1.B<class G2_C354`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G2_C354`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G2_C354`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G2_C354`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C354::Method4.MI.8075()#G2_C354::Method5.MI.8077()#G2_C354::Method6.8078<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G2_C354`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C354::Method4.MI.8075()#G2_C354::Method5.MI.8077()#G2_C354::Method6.8078<System.Object>()#"
		call void Framework::M.IBase1.A<class G2_C354`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C354::Method7.MI.8073<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C354`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C354::Method7.MI.8073<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G2_C354`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C354::Method7.MI.8073<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G2_C354`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C354::ClassMethod1328.MI.8080()#G2_C354::ClassMethod1329.MI.8081()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C354::ClassMethod1331.MI.8082<System.Object>()#G2_C354::ClassMethod2049.8079()#G2_C354::Method4.8074()#G2_C354::Method5.8076()#G2_C354::Method6.8078<System.Object>()#G2_C354::Method7.8072<System.Object>()#"
		call void Framework::M.G2_C354.T.T<class BaseClass0,class BaseClass1,class G2_C354`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C354::ClassMethod1328.MI.8080()#G2_C354::ClassMethod1329.MI.8081()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C354::ClassMethod1331.MI.8082<System.Object>()#G2_C354::ClassMethod2049.8079()#G2_C354::Method4.8074()#G2_C354::Method5.8076()#G2_C354::Method6.8078<System.Object>()#G2_C354::Method7.8072<System.Object>()#"
		call void Framework::M.G2_C354.A.T<class BaseClass1,class G2_C354`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C354::ClassMethod1328.MI.8080()#G2_C354::ClassMethod1329.MI.8081()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C354::ClassMethod1331.MI.8082<System.Object>()#G2_C354::ClassMethod2049.8079()#G2_C354::Method4.8074()#G2_C354::Method5.8076()#G2_C354::Method6.8078<System.Object>()#G2_C354::Method7.8072<System.Object>()#"
		call void Framework::M.G2_C354.A.B<class G2_C354`2<class BaseClass0,class BaseClass1>>(!!0,string)
		newobj instance void class G2_C354`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C354::ClassMethod1328.MI.8080()#G2_C354::ClassMethod1329.MI.8081()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C354::ClassMethod1331.MI.8082<System.Object>()#G2_C354::Method4.8074()#G2_C354::Method5.8076()#G2_C354::Method6.8078<System.Object>()#G1_C7::Method7.4815<System.Object>()#"
		call void Framework::M.G1_C7.T.T<class BaseClass1,class BaseClass1,class G2_C354`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C354::ClassMethod1328.MI.8080()#G2_C354::ClassMethod1329.MI.8081()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C354::ClassMethod1331.MI.8082<System.Object>()#G2_C354::Method4.8074()#G2_C354::Method5.8076()#G2_C354::Method6.8078<System.Object>()#G1_C7::Method7.4815<System.Object>()#"
		call void Framework::M.G1_C7.B.T<class BaseClass1,class G2_C354`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C354::ClassMethod1328.MI.8080()#G2_C354::ClassMethod1329.MI.8081()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C354::ClassMethod1331.MI.8082<System.Object>()#G2_C354::Method4.8074()#G2_C354::Method5.8076()#G2_C354::Method6.8078<System.Object>()#G1_C7::Method7.4815<System.Object>()#"
		call void Framework::M.G1_C7.B.B<class G2_C354`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C354::Method4.MI.8075()#G2_C354::Method5.MI.8077()#G2_C354::Method6.8078<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass1,class G2_C354`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C354::Method4.MI.8075()#G2_C354::Method5.MI.8077()#G2_C354::Method6.8078<System.Object>()#"
		call void Framework::M.IBase1.B<class G2_C354`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G2_C354`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G2_C354`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G2_C354`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C354::Method4.MI.8075()#G2_C354::Method5.MI.8077()#G2_C354::Method6.8078<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G2_C354`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C354::Method4.MI.8075()#G2_C354::Method5.MI.8077()#G2_C354::Method6.8078<System.Object>()#"
		call void Framework::M.IBase1.A<class G2_C354`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C354::Method7.MI.8073<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C354`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C354::Method7.MI.8073<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G2_C354`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C354::Method7.MI.8073<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G2_C354`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C354::ClassMethod1328.MI.8080()#G2_C354::ClassMethod1329.MI.8081()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C354::ClassMethod1331.MI.8082<System.Object>()#G2_C354::ClassMethod2049.8079()#G2_C354::Method4.8074()#G2_C354::Method5.8076()#G2_C354::Method6.8078<System.Object>()#G2_C354::Method7.8072<System.Object>()#"
		call void Framework::M.G2_C354.T.T<class BaseClass1,class BaseClass0,class G2_C354`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C354::ClassMethod1328.MI.8080()#G2_C354::ClassMethod1329.MI.8081()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C354::ClassMethod1331.MI.8082<System.Object>()#G2_C354::ClassMethod2049.8079()#G2_C354::Method4.8074()#G2_C354::Method5.8076()#G2_C354::Method6.8078<System.Object>()#G2_C354::Method7.8072<System.Object>()#"
		call void Framework::M.G2_C354.B.T<class BaseClass0,class G2_C354`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C354::ClassMethod1328.MI.8080()#G2_C354::ClassMethod1329.MI.8081()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C354::ClassMethod1331.MI.8082<System.Object>()#G2_C354::ClassMethod2049.8079()#G2_C354::Method4.8074()#G2_C354::Method5.8076()#G2_C354::Method6.8078<System.Object>()#G2_C354::Method7.8072<System.Object>()#"
		call void Framework::M.G2_C354.B.A<class G2_C354`2<class BaseClass1,class BaseClass0>>(!!0,string)
		newobj instance void class G2_C354`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C354::ClassMethod1328.MI.8080()#G2_C354::ClassMethod1329.MI.8081()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C354::ClassMethod1331.MI.8082<System.Object>()#G2_C354::Method4.8074()#G2_C354::Method5.8076()#G2_C354::Method6.8078<System.Object>()#G1_C7::Method7.4815<System.Object>()#"
		call void Framework::M.G1_C7.T.T<class BaseClass1,class BaseClass1,class G2_C354`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C354::ClassMethod1328.MI.8080()#G2_C354::ClassMethod1329.MI.8081()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C354::ClassMethod1331.MI.8082<System.Object>()#G2_C354::Method4.8074()#G2_C354::Method5.8076()#G2_C354::Method6.8078<System.Object>()#G1_C7::Method7.4815<System.Object>()#"
		call void Framework::M.G1_C7.B.T<class BaseClass1,class G2_C354`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C354::ClassMethod1328.MI.8080()#G2_C354::ClassMethod1329.MI.8081()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C354::ClassMethod1331.MI.8082<System.Object>()#G2_C354::Method4.8074()#G2_C354::Method5.8076()#G2_C354::Method6.8078<System.Object>()#G1_C7::Method7.4815<System.Object>()#"
		call void Framework::M.G1_C7.B.B<class G2_C354`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C354::Method4.MI.8075()#G2_C354::Method5.MI.8077()#G2_C354::Method6.8078<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass1,class G2_C354`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C354::Method4.MI.8075()#G2_C354::Method5.MI.8077()#G2_C354::Method6.8078<System.Object>()#"
		call void Framework::M.IBase1.B<class G2_C354`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G2_C354`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G2_C354`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G2_C354`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C354::Method4.MI.8075()#G2_C354::Method5.MI.8077()#G2_C354::Method6.8078<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G2_C354`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C354::Method4.MI.8075()#G2_C354::Method5.MI.8077()#G2_C354::Method6.8078<System.Object>()#"
		call void Framework::M.IBase1.A<class G2_C354`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C354::Method7.MI.8073<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C354`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C354::Method7.MI.8073<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G2_C354`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C354::Method7.MI.8073<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G2_C354`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C354::ClassMethod1328.MI.8080()#G2_C354::ClassMethod1329.MI.8081()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C354::ClassMethod1331.MI.8082<System.Object>()#G2_C354::ClassMethod2049.8079()#G2_C354::Method4.8074()#G2_C354::Method5.8076()#G2_C354::Method6.8078<System.Object>()#G2_C354::Method7.8072<System.Object>()#"
		call void Framework::M.G2_C354.T.T<class BaseClass1,class BaseClass1,class G2_C354`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C354::ClassMethod1328.MI.8080()#G2_C354::ClassMethod1329.MI.8081()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C354::ClassMethod1331.MI.8082<System.Object>()#G2_C354::ClassMethod2049.8079()#G2_C354::Method4.8074()#G2_C354::Method5.8076()#G2_C354::Method6.8078<System.Object>()#G2_C354::Method7.8072<System.Object>()#"
		call void Framework::M.G2_C354.B.T<class BaseClass1,class G2_C354`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C354::ClassMethod1328.MI.8080()#G2_C354::ClassMethod1329.MI.8081()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C354::ClassMethod1331.MI.8082<System.Object>()#G2_C354::ClassMethod2049.8079()#G2_C354::Method4.8074()#G2_C354::Method5.8076()#G2_C354::Method6.8078<System.Object>()#G2_C354::Method7.8072<System.Object>()#"
		call void Framework::M.G2_C354.B.B<class G2_C354`2<class BaseClass1,class BaseClass1>>(!!0,string)
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
		newobj instance void class G3_C1322`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass1>::ClassMethod1331<object>()
		calli default string(class G3_C1322`1<class BaseClass0>)
		ldstr "G2_C354::ClassMethod1331.MI.8082<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G3_C1322`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass1>::ClassMethod1330<object>()
		calli default string(class G3_C1322`1<class BaseClass0>)
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G3_C1322`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass1>::ClassMethod1329()
		calli default string(class G3_C1322`1<class BaseClass0>)
		ldstr "G2_C354::ClassMethod1329.MI.8081()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G3_C1322`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass1>::ClassMethod1328()
		calli default string(class G3_C1322`1<class BaseClass0>)
		ldstr "G2_C354::ClassMethod1328.MI.8080()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G3_C1322`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1322`1<class BaseClass0>)
		ldstr "G1_C7::Method7.4815<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G3_C1322`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass1>::Method6<object>()
		calli default string(class G3_C1322`1<class BaseClass0>)
		ldstr "G2_C354::Method6.8078<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G3_C1322`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass1>::Method5()
		calli default string(class G3_C1322`1<class BaseClass0>)
		ldstr "G2_C354::Method5.8076()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G3_C1322`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass1>::Method4()
		calli default string(class G3_C1322`1<class BaseClass0>)
		ldstr "G2_C354::Method4.8074()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G3_C1322`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G3_C1322`1<class BaseClass0>)
		ldstr "G2_C354::Method4.MI.8075()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1322`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G3_C1322`1<class BaseClass0>)
		ldstr "G2_C354::Method5.MI.8077()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1322`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G3_C1322`1<class BaseClass0>)
		ldstr "G2_C354::Method6.8078<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1322`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1322`1<class BaseClass0>)
		ldstr "G1_C7::Method7.MI.4816<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1322`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G3_C1322`1<class BaseClass0>)
		ldstr "G2_C354::Method4.MI.8075()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1322`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G3_C1322`1<class BaseClass0>)
		ldstr "G2_C354::Method5.MI.8077()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1322`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1322`1<class BaseClass0>)
		ldstr "G2_C354::Method6.8078<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1322`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1322`1<class BaseClass0>)
		ldstr "G3_C1322::Method7.15527<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1322`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C354`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C354`2<class BaseClass0,class BaseClass0>::ClassMethod2049()
		calli default string(class G3_C1322`1<class BaseClass0>)
		ldstr "G3_C1322::ClassMethod2049.MI.15532()"
		ldstr "class G2_C354`2<class BaseClass0,class BaseClass0> on type class G3_C1322`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C354`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C354`2<class BaseClass0,class BaseClass0>::Method6<object>()
		calli default string(class G3_C1322`1<class BaseClass0>)
		ldstr "G2_C354::Method6.8078<System.Object>()"
		ldstr "class G2_C354`2<class BaseClass0,class BaseClass0> on type class G3_C1322`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C354`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C354`2<class BaseClass0,class BaseClass0>::Method5()
		calli default string(class G3_C1322`1<class BaseClass0>)
		ldstr "G2_C354::Method5.8076()"
		ldstr "class G2_C354`2<class BaseClass0,class BaseClass0> on type class G3_C1322`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C354`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C354`2<class BaseClass0,class BaseClass0>::Method4()
		calli default string(class G3_C1322`1<class BaseClass0>)
		ldstr "G2_C354::Method4.8074()"
		ldstr "class G2_C354`2<class BaseClass0,class BaseClass0> on type class G3_C1322`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C354`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C354`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1322`1<class BaseClass0>)
		ldstr "G3_C1322::Method7.15527<System.Object>()"
		ldstr "class G2_C354`2<class BaseClass0,class BaseClass0> on type class G3_C1322`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C354`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C354`2<class BaseClass0,class BaseClass0>::ClassMethod1331<object>()
		calli default string(class G3_C1322`1<class BaseClass0>)
		ldstr "G2_C354::ClassMethod1331.MI.8082<System.Object>()"
		ldstr "class G2_C354`2<class BaseClass0,class BaseClass0> on type class G3_C1322`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C354`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C354`2<class BaseClass0,class BaseClass0>::ClassMethod1330<object>()
		calli default string(class G3_C1322`1<class BaseClass0>)
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G2_C354`2<class BaseClass0,class BaseClass0> on type class G3_C1322`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C354`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C354`2<class BaseClass0,class BaseClass0>::ClassMethod1329()
		calli default string(class G3_C1322`1<class BaseClass0>)
		ldstr "G2_C354::ClassMethod1329.MI.8081()"
		ldstr "class G2_C354`2<class BaseClass0,class BaseClass0> on type class G3_C1322`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C354`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C354`2<class BaseClass0,class BaseClass0>::ClassMethod1328()
		calli default string(class G3_C1322`1<class BaseClass0>)
		ldstr "G2_C354::ClassMethod1328.MI.8080()"
		ldstr "class G2_C354`2<class BaseClass0,class BaseClass0> on type class G3_C1322`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1322`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1322`1<class BaseClass0>::ClassMethod4025<object>()
		calli default string(class G3_C1322`1<class BaseClass0>)
		ldstr "G3_C1322::ClassMethod4025.15531<System.Object>()"
		ldstr "class G3_C1322`1<class BaseClass0> on type class G3_C1322`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1322`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1322`1<class BaseClass0>::ClassMethod4024<object>()
		calli default string(class G3_C1322`1<class BaseClass0>)
		ldstr "G3_C1322::ClassMethod4024.15530<System.Object>()"
		ldstr "class G3_C1322`1<class BaseClass0> on type class G3_C1322`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1322`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1322`1<class BaseClass0>::ClassMethod4023()
		calli default string(class G3_C1322`1<class BaseClass0>)
		ldstr "G3_C1322::ClassMethod4023.15529()"
		ldstr "class G3_C1322`1<class BaseClass0> on type class G3_C1322`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1322`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1322`1<class BaseClass0>::ClassMethod4022()
		calli default string(class G3_C1322`1<class BaseClass0>)
		ldstr "G3_C1322::ClassMethod4022.15528()"
		ldstr "class G3_C1322`1<class BaseClass0> on type class G3_C1322`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1322`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1322`1<class BaseClass0>::Method7<object>()
		calli default string(class G3_C1322`1<class BaseClass0>)
		ldstr "G3_C1322::Method7.15527<System.Object>()"
		ldstr "class G3_C1322`1<class BaseClass0> on type class G3_C1322`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1322`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1322`1<class BaseClass0>::ClassMethod2049()
		calli default string(class G3_C1322`1<class BaseClass0>)
		ldstr "G3_C1322::ClassMethod2049.MI.15532()"
		ldstr "class G3_C1322`1<class BaseClass0> on type class G3_C1322`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1322`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1322`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1322`1<class BaseClass0>)
		ldstr "G2_C354::Method6.8078<System.Object>()"
		ldstr "class G3_C1322`1<class BaseClass0> on type class G3_C1322`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1322`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1322`1<class BaseClass0>::Method5()
		calli default string(class G3_C1322`1<class BaseClass0>)
		ldstr "G2_C354::Method5.8076()"
		ldstr "class G3_C1322`1<class BaseClass0> on type class G3_C1322`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1322`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1322`1<class BaseClass0>::Method4()
		calli default string(class G3_C1322`1<class BaseClass0>)
		ldstr "G2_C354::Method4.8074()"
		ldstr "class G3_C1322`1<class BaseClass0> on type class G3_C1322`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1322`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1322`1<class BaseClass0>::ClassMethod1331<object>()
		calli default string(class G3_C1322`1<class BaseClass0>)
		ldstr "G2_C354::ClassMethod1331.MI.8082<System.Object>()"
		ldstr "class G3_C1322`1<class BaseClass0> on type class G3_C1322`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1322`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1322`1<class BaseClass0>::ClassMethod1330<object>()
		calli default string(class G3_C1322`1<class BaseClass0>)
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G3_C1322`1<class BaseClass0> on type class G3_C1322`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1322`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1322`1<class BaseClass0>::ClassMethod1329()
		calli default string(class G3_C1322`1<class BaseClass0>)
		ldstr "G2_C354::ClassMethod1329.MI.8081()"
		ldstr "class G3_C1322`1<class BaseClass0> on type class G3_C1322`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1322`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1322`1<class BaseClass0>::ClassMethod1328()
		calli default string(class G3_C1322`1<class BaseClass0>)
		ldstr "G2_C354::ClassMethod1328.MI.8080()"
		ldstr "class G3_C1322`1<class BaseClass0> on type class G3_C1322`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1322`1<class BaseClass0>)
		ldstr "G3_C1322::Method7.15527<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1322`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G3_C1322`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass1>::ClassMethod1331<object>()
		calli default string(class G3_C1322`1<class BaseClass1>)
		ldstr "G2_C354::ClassMethod1331.MI.8082<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G3_C1322`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass1>::ClassMethod1330<object>()
		calli default string(class G3_C1322`1<class BaseClass1>)
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G3_C1322`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass1>::ClassMethod1329()
		calli default string(class G3_C1322`1<class BaseClass1>)
		ldstr "G2_C354::ClassMethod1329.MI.8081()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G3_C1322`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass1>::ClassMethod1328()
		calli default string(class G3_C1322`1<class BaseClass1>)
		ldstr "G2_C354::ClassMethod1328.MI.8080()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G3_C1322`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1322`1<class BaseClass1>)
		ldstr "G1_C7::Method7.4815<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G3_C1322`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass1>::Method6<object>()
		calli default string(class G3_C1322`1<class BaseClass1>)
		ldstr "G2_C354::Method6.8078<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G3_C1322`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass1>::Method5()
		calli default string(class G3_C1322`1<class BaseClass1>)
		ldstr "G2_C354::Method5.8076()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G3_C1322`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass1>::Method4()
		calli default string(class G3_C1322`1<class BaseClass1>)
		ldstr "G2_C354::Method4.8074()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G3_C1322`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G3_C1322`1<class BaseClass1>)
		ldstr "G2_C354::Method4.MI.8075()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1322`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G3_C1322`1<class BaseClass1>)
		ldstr "G2_C354::Method5.MI.8077()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1322`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G3_C1322`1<class BaseClass1>)
		ldstr "G2_C354::Method6.8078<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1322`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1322`1<class BaseClass1>)
		ldstr "G1_C7::Method7.MI.4816<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1322`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G3_C1322`1<class BaseClass1>)
		ldstr "G2_C354::Method4.MI.8075()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1322`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G3_C1322`1<class BaseClass1>)
		ldstr "G2_C354::Method5.MI.8077()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1322`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1322`1<class BaseClass1>)
		ldstr "G2_C354::Method6.8078<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1322`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1322`1<class BaseClass1>)
		ldstr "G3_C1322::Method7.15527<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1322`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C354`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C354`2<class BaseClass1,class BaseClass1>::ClassMethod2049()
		calli default string(class G3_C1322`1<class BaseClass1>)
		ldstr "G3_C1322::ClassMethod2049.MI.15532()"
		ldstr "class G2_C354`2<class BaseClass1,class BaseClass1> on type class G3_C1322`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C354`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C354`2<class BaseClass1,class BaseClass1>::Method6<object>()
		calli default string(class G3_C1322`1<class BaseClass1>)
		ldstr "G2_C354::Method6.8078<System.Object>()"
		ldstr "class G2_C354`2<class BaseClass1,class BaseClass1> on type class G3_C1322`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C354`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C354`2<class BaseClass1,class BaseClass1>::Method5()
		calli default string(class G3_C1322`1<class BaseClass1>)
		ldstr "G2_C354::Method5.8076()"
		ldstr "class G2_C354`2<class BaseClass1,class BaseClass1> on type class G3_C1322`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C354`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C354`2<class BaseClass1,class BaseClass1>::Method4()
		calli default string(class G3_C1322`1<class BaseClass1>)
		ldstr "G2_C354::Method4.8074()"
		ldstr "class G2_C354`2<class BaseClass1,class BaseClass1> on type class G3_C1322`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C354`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C354`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1322`1<class BaseClass1>)
		ldstr "G3_C1322::Method7.15527<System.Object>()"
		ldstr "class G2_C354`2<class BaseClass1,class BaseClass1> on type class G3_C1322`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C354`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C354`2<class BaseClass1,class BaseClass1>::ClassMethod1331<object>()
		calli default string(class G3_C1322`1<class BaseClass1>)
		ldstr "G2_C354::ClassMethod1331.MI.8082<System.Object>()"
		ldstr "class G2_C354`2<class BaseClass1,class BaseClass1> on type class G3_C1322`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C354`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C354`2<class BaseClass1,class BaseClass1>::ClassMethod1330<object>()
		calli default string(class G3_C1322`1<class BaseClass1>)
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G2_C354`2<class BaseClass1,class BaseClass1> on type class G3_C1322`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C354`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C354`2<class BaseClass1,class BaseClass1>::ClassMethod1329()
		calli default string(class G3_C1322`1<class BaseClass1>)
		ldstr "G2_C354::ClassMethod1329.MI.8081()"
		ldstr "class G2_C354`2<class BaseClass1,class BaseClass1> on type class G3_C1322`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C354`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C354`2<class BaseClass1,class BaseClass1>::ClassMethod1328()
		calli default string(class G3_C1322`1<class BaseClass1>)
		ldstr "G2_C354::ClassMethod1328.MI.8080()"
		ldstr "class G2_C354`2<class BaseClass1,class BaseClass1> on type class G3_C1322`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1322`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1322`1<class BaseClass1>::ClassMethod4025<object>()
		calli default string(class G3_C1322`1<class BaseClass1>)
		ldstr "G3_C1322::ClassMethod4025.15531<System.Object>()"
		ldstr "class G3_C1322`1<class BaseClass1> on type class G3_C1322`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1322`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1322`1<class BaseClass1>::ClassMethod4024<object>()
		calli default string(class G3_C1322`1<class BaseClass1>)
		ldstr "G3_C1322::ClassMethod4024.15530<System.Object>()"
		ldstr "class G3_C1322`1<class BaseClass1> on type class G3_C1322`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1322`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1322`1<class BaseClass1>::ClassMethod4023()
		calli default string(class G3_C1322`1<class BaseClass1>)
		ldstr "G3_C1322::ClassMethod4023.15529()"
		ldstr "class G3_C1322`1<class BaseClass1> on type class G3_C1322`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1322`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1322`1<class BaseClass1>::ClassMethod4022()
		calli default string(class G3_C1322`1<class BaseClass1>)
		ldstr "G3_C1322::ClassMethod4022.15528()"
		ldstr "class G3_C1322`1<class BaseClass1> on type class G3_C1322`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1322`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1322`1<class BaseClass1>::Method7<object>()
		calli default string(class G3_C1322`1<class BaseClass1>)
		ldstr "G3_C1322::Method7.15527<System.Object>()"
		ldstr "class G3_C1322`1<class BaseClass1> on type class G3_C1322`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1322`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1322`1<class BaseClass1>::ClassMethod2049()
		calli default string(class G3_C1322`1<class BaseClass1>)
		ldstr "G3_C1322::ClassMethod2049.MI.15532()"
		ldstr "class G3_C1322`1<class BaseClass1> on type class G3_C1322`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1322`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1322`1<class BaseClass1>::Method6<object>()
		calli default string(class G3_C1322`1<class BaseClass1>)
		ldstr "G2_C354::Method6.8078<System.Object>()"
		ldstr "class G3_C1322`1<class BaseClass1> on type class G3_C1322`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1322`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1322`1<class BaseClass1>::Method5()
		calli default string(class G3_C1322`1<class BaseClass1>)
		ldstr "G2_C354::Method5.8076()"
		ldstr "class G3_C1322`1<class BaseClass1> on type class G3_C1322`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1322`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1322`1<class BaseClass1>::Method4()
		calli default string(class G3_C1322`1<class BaseClass1>)
		ldstr "G2_C354::Method4.8074()"
		ldstr "class G3_C1322`1<class BaseClass1> on type class G3_C1322`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1322`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1322`1<class BaseClass1>::ClassMethod1331<object>()
		calli default string(class G3_C1322`1<class BaseClass1>)
		ldstr "G2_C354::ClassMethod1331.MI.8082<System.Object>()"
		ldstr "class G3_C1322`1<class BaseClass1> on type class G3_C1322`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1322`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1322`1<class BaseClass1>::ClassMethod1330<object>()
		calli default string(class G3_C1322`1<class BaseClass1>)
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G3_C1322`1<class BaseClass1> on type class G3_C1322`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1322`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1322`1<class BaseClass1>::ClassMethod1329()
		calli default string(class G3_C1322`1<class BaseClass1>)
		ldstr "G2_C354::ClassMethod1329.MI.8081()"
		ldstr "class G3_C1322`1<class BaseClass1> on type class G3_C1322`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1322`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1322`1<class BaseClass1>::ClassMethod1328()
		calli default string(class G3_C1322`1<class BaseClass1>)
		ldstr "G2_C354::ClassMethod1328.MI.8080()"
		ldstr "class G3_C1322`1<class BaseClass1> on type class G3_C1322`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C354`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass1>::ClassMethod1331<object>()
		calli default string(class G2_C354`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C354::ClassMethod1331.MI.8082<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C354`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass1>::ClassMethod1330<object>()
		calli default string(class G2_C354`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C354`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass1>::ClassMethod1329()
		calli default string(class G2_C354`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C354::ClassMethod1329.MI.8081()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C354`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass1>::ClassMethod1328()
		calli default string(class G2_C354`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C354::ClassMethod1328.MI.8080()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C354`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C354`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C7::Method7.4815<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C354`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass1>::Method6<object>()
		calli default string(class G2_C354`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C354::Method6.8078<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C354`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass1>::Method5()
		calli default string(class G2_C354`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C354::Method5.8076()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C354`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass1>::Method4()
		calli default string(class G2_C354`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C354::Method4.8074()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C354`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G2_C354`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C354::Method4.MI.8075()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C354`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G2_C354`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C354::Method5.MI.8077()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C354`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G2_C354`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C354::Method6.8078<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C354`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C354`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C7::Method7.MI.4816<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C354`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G2_C354`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C354::Method4.MI.8075()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C354`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G2_C354`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C354::Method5.MI.8077()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C354`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G2_C354`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C354::Method6.8078<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C354`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C354`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C354::Method7.MI.8073<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C354`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C354`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C354`2<class BaseClass0,class BaseClass0>::ClassMethod2049()
		calli default string(class G2_C354`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C354::ClassMethod2049.8079()"
		ldstr "class G2_C354`2<class BaseClass0,class BaseClass0> on type class G2_C354`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C354`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C354`2<class BaseClass0,class BaseClass0>::Method6<object>()
		calli default string(class G2_C354`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C354::Method6.8078<System.Object>()"
		ldstr "class G2_C354`2<class BaseClass0,class BaseClass0> on type class G2_C354`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C354`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C354`2<class BaseClass0,class BaseClass0>::Method5()
		calli default string(class G2_C354`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C354::Method5.8076()"
		ldstr "class G2_C354`2<class BaseClass0,class BaseClass0> on type class G2_C354`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C354`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C354`2<class BaseClass0,class BaseClass0>::Method4()
		calli default string(class G2_C354`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C354::Method4.8074()"
		ldstr "class G2_C354`2<class BaseClass0,class BaseClass0> on type class G2_C354`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C354`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C354`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C354`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C354::Method7.8072<System.Object>()"
		ldstr "class G2_C354`2<class BaseClass0,class BaseClass0> on type class G2_C354`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C354`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C354`2<class BaseClass0,class BaseClass0>::ClassMethod1331<object>()
		calli default string(class G2_C354`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C354::ClassMethod1331.MI.8082<System.Object>()"
		ldstr "class G2_C354`2<class BaseClass0,class BaseClass0> on type class G2_C354`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C354`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C354`2<class BaseClass0,class BaseClass0>::ClassMethod1330<object>()
		calli default string(class G2_C354`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G2_C354`2<class BaseClass0,class BaseClass0> on type class G2_C354`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C354`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C354`2<class BaseClass0,class BaseClass0>::ClassMethod1329()
		calli default string(class G2_C354`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C354::ClassMethod1329.MI.8081()"
		ldstr "class G2_C354`2<class BaseClass0,class BaseClass0> on type class G2_C354`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C354`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C354`2<class BaseClass0,class BaseClass0>::ClassMethod1328()
		calli default string(class G2_C354`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C354::ClassMethod1328.MI.8080()"
		ldstr "class G2_C354`2<class BaseClass0,class BaseClass0> on type class G2_C354`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C354`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass1>::ClassMethod1331<object>()
		calli default string(class G2_C354`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C354::ClassMethod1331.MI.8082<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C354`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass1>::ClassMethod1330<object>()
		calli default string(class G2_C354`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C354`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass1>::ClassMethod1329()
		calli default string(class G2_C354`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C354::ClassMethod1329.MI.8081()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C354`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass1>::ClassMethod1328()
		calli default string(class G2_C354`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C354::ClassMethod1328.MI.8080()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C354`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C354`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C7::Method7.4815<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C354`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass1>::Method6<object>()
		calli default string(class G2_C354`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C354::Method6.8078<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C354`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass1>::Method5()
		calli default string(class G2_C354`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C354::Method5.8076()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C354`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass1>::Method4()
		calli default string(class G2_C354`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C354::Method4.8074()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C354`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G2_C354`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C354::Method4.MI.8075()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C354`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G2_C354`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C354::Method5.MI.8077()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C354`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G2_C354`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C354::Method6.8078<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C354`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C354`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C7::Method7.MI.4816<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C354`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G2_C354`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C354::Method4.MI.8075()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C354`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G2_C354`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C354::Method5.MI.8077()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C354`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G2_C354`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C354::Method6.8078<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C354`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C354`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C354::Method7.MI.8073<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C354`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C354`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C354`2<class BaseClass0,class BaseClass1>::ClassMethod2049()
		calli default string(class G2_C354`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C354::ClassMethod2049.8079()"
		ldstr "class G2_C354`2<class BaseClass0,class BaseClass1> on type class G2_C354`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C354`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C354`2<class BaseClass0,class BaseClass1>::Method6<object>()
		calli default string(class G2_C354`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C354::Method6.8078<System.Object>()"
		ldstr "class G2_C354`2<class BaseClass0,class BaseClass1> on type class G2_C354`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C354`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C354`2<class BaseClass0,class BaseClass1>::Method5()
		calli default string(class G2_C354`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C354::Method5.8076()"
		ldstr "class G2_C354`2<class BaseClass0,class BaseClass1> on type class G2_C354`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C354`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C354`2<class BaseClass0,class BaseClass1>::Method4()
		calli default string(class G2_C354`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C354::Method4.8074()"
		ldstr "class G2_C354`2<class BaseClass0,class BaseClass1> on type class G2_C354`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C354`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C354`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C354`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C354::Method7.8072<System.Object>()"
		ldstr "class G2_C354`2<class BaseClass0,class BaseClass1> on type class G2_C354`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C354`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C354`2<class BaseClass0,class BaseClass1>::ClassMethod1331<object>()
		calli default string(class G2_C354`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C354::ClassMethod1331.MI.8082<System.Object>()"
		ldstr "class G2_C354`2<class BaseClass0,class BaseClass1> on type class G2_C354`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C354`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C354`2<class BaseClass0,class BaseClass1>::ClassMethod1330<object>()
		calli default string(class G2_C354`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G2_C354`2<class BaseClass0,class BaseClass1> on type class G2_C354`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C354`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C354`2<class BaseClass0,class BaseClass1>::ClassMethod1329()
		calli default string(class G2_C354`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C354::ClassMethod1329.MI.8081()"
		ldstr "class G2_C354`2<class BaseClass0,class BaseClass1> on type class G2_C354`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C354`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C354`2<class BaseClass0,class BaseClass1>::ClassMethod1328()
		calli default string(class G2_C354`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C354::ClassMethod1328.MI.8080()"
		ldstr "class G2_C354`2<class BaseClass0,class BaseClass1> on type class G2_C354`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C354`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass1>::ClassMethod1331<object>()
		calli default string(class G2_C354`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C354::ClassMethod1331.MI.8082<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C354`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass1>::ClassMethod1330<object>()
		calli default string(class G2_C354`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C354`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass1>::ClassMethod1329()
		calli default string(class G2_C354`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C354::ClassMethod1329.MI.8081()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C354`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass1>::ClassMethod1328()
		calli default string(class G2_C354`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C354::ClassMethod1328.MI.8080()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C354`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C354`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C7::Method7.4815<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C354`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass1>::Method6<object>()
		calli default string(class G2_C354`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C354::Method6.8078<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C354`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass1>::Method5()
		calli default string(class G2_C354`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C354::Method5.8076()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C354`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass1>::Method4()
		calli default string(class G2_C354`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C354::Method4.8074()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C354`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G2_C354`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C354::Method4.MI.8075()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C354`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G2_C354`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C354::Method5.MI.8077()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C354`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G2_C354`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C354::Method6.8078<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C354`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C354`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C7::Method7.MI.4816<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C354`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G2_C354`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C354::Method4.MI.8075()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C354`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G2_C354`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C354::Method5.MI.8077()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C354`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G2_C354`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C354::Method6.8078<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C354`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C354`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C354::Method7.MI.8073<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C354`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C354`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C354`2<class BaseClass1,class BaseClass0>::ClassMethod2049()
		calli default string(class G2_C354`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C354::ClassMethod2049.8079()"
		ldstr "class G2_C354`2<class BaseClass1,class BaseClass0> on type class G2_C354`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C354`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C354`2<class BaseClass1,class BaseClass0>::Method6<object>()
		calli default string(class G2_C354`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C354::Method6.8078<System.Object>()"
		ldstr "class G2_C354`2<class BaseClass1,class BaseClass0> on type class G2_C354`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C354`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C354`2<class BaseClass1,class BaseClass0>::Method5()
		calli default string(class G2_C354`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C354::Method5.8076()"
		ldstr "class G2_C354`2<class BaseClass1,class BaseClass0> on type class G2_C354`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C354`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C354`2<class BaseClass1,class BaseClass0>::Method4()
		calli default string(class G2_C354`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C354::Method4.8074()"
		ldstr "class G2_C354`2<class BaseClass1,class BaseClass0> on type class G2_C354`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C354`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C354`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G2_C354`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C354::Method7.8072<System.Object>()"
		ldstr "class G2_C354`2<class BaseClass1,class BaseClass0> on type class G2_C354`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C354`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C354`2<class BaseClass1,class BaseClass0>::ClassMethod1331<object>()
		calli default string(class G2_C354`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C354::ClassMethod1331.MI.8082<System.Object>()"
		ldstr "class G2_C354`2<class BaseClass1,class BaseClass0> on type class G2_C354`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C354`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C354`2<class BaseClass1,class BaseClass0>::ClassMethod1330<object>()
		calli default string(class G2_C354`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G2_C354`2<class BaseClass1,class BaseClass0> on type class G2_C354`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C354`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C354`2<class BaseClass1,class BaseClass0>::ClassMethod1329()
		calli default string(class G2_C354`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C354::ClassMethod1329.MI.8081()"
		ldstr "class G2_C354`2<class BaseClass1,class BaseClass0> on type class G2_C354`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C354`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C354`2<class BaseClass1,class BaseClass0>::ClassMethod1328()
		calli default string(class G2_C354`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C354::ClassMethod1328.MI.8080()"
		ldstr "class G2_C354`2<class BaseClass1,class BaseClass0> on type class G2_C354`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C354`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass1>::ClassMethod1331<object>()
		calli default string(class G2_C354`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C354::ClassMethod1331.MI.8082<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C354`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass1>::ClassMethod1330<object>()
		calli default string(class G2_C354`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C354`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass1>::ClassMethod1329()
		calli default string(class G2_C354`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C354::ClassMethod1329.MI.8081()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C354`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass1>::ClassMethod1328()
		calli default string(class G2_C354`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C354::ClassMethod1328.MI.8080()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C354`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C354`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C7::Method7.4815<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C354`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass1>::Method6<object>()
		calli default string(class G2_C354`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C354::Method6.8078<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C354`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass1>::Method5()
		calli default string(class G2_C354`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C354::Method5.8076()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C354`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass1,class BaseClass1>::Method4()
		calli default string(class G2_C354`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C354::Method4.8074()"
		ldstr "class G1_C7`2<class BaseClass1,class BaseClass1> on type class G2_C354`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G2_C354`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C354::Method4.MI.8075()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C354`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G2_C354`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C354::Method5.MI.8077()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C354`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G2_C354`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C354::Method6.8078<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C354`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C354`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C7::Method7.MI.4816<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C354`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G2_C354`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C354::Method4.MI.8075()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C354`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G2_C354`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C354::Method5.MI.8077()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C354`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G2_C354`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C354::Method6.8078<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C354`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C354`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C354::Method7.MI.8073<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C354`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C354`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C354`2<class BaseClass1,class BaseClass1>::ClassMethod2049()
		calli default string(class G2_C354`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C354::ClassMethod2049.8079()"
		ldstr "class G2_C354`2<class BaseClass1,class BaseClass1> on type class G2_C354`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C354`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C354`2<class BaseClass1,class BaseClass1>::Method6<object>()
		calli default string(class G2_C354`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C354::Method6.8078<System.Object>()"
		ldstr "class G2_C354`2<class BaseClass1,class BaseClass1> on type class G2_C354`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C354`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C354`2<class BaseClass1,class BaseClass1>::Method5()
		calli default string(class G2_C354`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C354::Method5.8076()"
		ldstr "class G2_C354`2<class BaseClass1,class BaseClass1> on type class G2_C354`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C354`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C354`2<class BaseClass1,class BaseClass1>::Method4()
		calli default string(class G2_C354`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C354::Method4.8074()"
		ldstr "class G2_C354`2<class BaseClass1,class BaseClass1> on type class G2_C354`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C354`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C354`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C354`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C354::Method7.8072<System.Object>()"
		ldstr "class G2_C354`2<class BaseClass1,class BaseClass1> on type class G2_C354`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C354`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C354`2<class BaseClass1,class BaseClass1>::ClassMethod1331<object>()
		calli default string(class G2_C354`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C354::ClassMethod1331.MI.8082<System.Object>()"
		ldstr "class G2_C354`2<class BaseClass1,class BaseClass1> on type class G2_C354`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C354`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C354`2<class BaseClass1,class BaseClass1>::ClassMethod1330<object>()
		calli default string(class G2_C354`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G2_C354`2<class BaseClass1,class BaseClass1> on type class G2_C354`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C354`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C354`2<class BaseClass1,class BaseClass1>::ClassMethod1329()
		calli default string(class G2_C354`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C354::ClassMethod1329.MI.8081()"
		ldstr "class G2_C354`2<class BaseClass1,class BaseClass1> on type class G2_C354`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C354`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C354`2<class BaseClass1,class BaseClass1>::ClassMethod1328()
		calli default string(class G2_C354`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C354::ClassMethod1328.MI.8080()"
		ldstr "class G2_C354`2<class BaseClass1,class BaseClass1> on type class G2_C354`2<class BaseClass1,class BaseClass1>"
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
