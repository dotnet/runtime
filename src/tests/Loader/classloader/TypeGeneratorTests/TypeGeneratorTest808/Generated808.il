// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) .ver 4:0:0:0 }
.assembly extern TestFramework { .publickeytoken = ( B0 3F 5F 7F 11 D5 0A 3A )  }

//TYPES IN FORWARDER ASSEMBLIES:

//TEST ASSEMBLY:
.assembly Generated808 { .hash algorithm 0x00008004 }
.assembly extern xunit.core {}

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
.class public G3_C1280`1<T0> 
		extends class G2_C326`2<class BaseClass0,class BaseClass1>
		implements class IBase1`1<!T0> 
{
	.method public hidebysig newslot virtual instance string Method4() cil managed noinlining { 
		ldstr "G3_C1280::Method4.15304()"
		ret
	}
	.method public hidebysig virtual instance string Method5() cil managed noinlining { 
		ldstr "G3_C1280::Method5.15305()"
		ret
	}
	.method public hidebysig virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G3_C1280::Method6.15306<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<T0>.Method6'<M0>() cil managed noinlining { 
		.override method instance string class IBase1`1<!T0>::Method6<[1]>()
		ldstr "G3_C1280::Method6.MI.15307<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod3934() cil managed noinlining { 
		ldstr "G3_C1280::ClassMethod3934.15308()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod3935() cil managed noinlining { 
		ldstr "G3_C1280::ClassMethod3935.15309()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod3936<M0>() cil managed noinlining { 
		ldstr "G3_C1280::ClassMethod3936.15310<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod3937<M0>() cil managed noinlining { 
		ldstr "G3_C1280::ClassMethod3937.15311<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C326<class BaseClass0,class BaseClass1>.ClassMethod1990'() cil managed noinlining { 
		.override method instance string class G2_C326`2<class BaseClass0,class BaseClass1>::ClassMethod1990()
		ldstr "G3_C1280::ClassMethod1990.MI.15312()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C326<class BaseClass0,class BaseClass1>.ClassMethod1992'<M0>() cil managed noinlining { 
		.override method instance string class G2_C326`2<class BaseClass0,class BaseClass1>::ClassMethod1992<[1]>()
		ldstr "G3_C1280::ClassMethod1992.MI.15313<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class G2_C326`2<class BaseClass0,class BaseClass1>::.ctor()
		ret
	}
}
.class public G2_C326`2<T0, T1> 
		extends class G1_C7`2<class BaseClass0,class BaseClass0>
		implements class IBase1`1<class BaseClass1>, class IBase2`2<class BaseClass1,!T1> 
{
	.method public hidebysig virtual instance string Method4() cil managed noinlining { 
		ldstr "G2_C326::Method4.7788()"
		ret
	}
	.method public hidebysig virtual instance string Method5() cil managed noinlining { 
		ldstr "G2_C326::Method5.7789()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G2_C326::Method6.7790<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G2_C326::Method7.7791<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase2<class BaseClass1,T1>.Method7'<M0>() cil managed noinlining { 
		.override method instance string class IBase2`2<class BaseClass1,!T1>::Method7<[1]>()
		ldstr "G2_C326::Method7.MI.7792<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1990() cil managed noinlining { 
		ldstr "G2_C326::ClassMethod1990.7793()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1991<M0>() cil managed noinlining { 
		ldstr "G2_C326::ClassMethod1991.7794<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1992<M0>() cil managed noinlining { 
		ldstr "G2_C326::ClassMethod1992.7795<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C7<class BaseClass0,class BaseClass0>.ClassMethod1329'() cil managed noinlining { 
		.override method instance string class G1_C7`2<class BaseClass0,class BaseClass0>::ClassMethod1329()
		ldstr "G2_C326::ClassMethod1329.MI.7796()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C7<class BaseClass0,class BaseClass0>.ClassMethod1331'<M0>() cil managed noinlining { 
		.override method instance string class G1_C7`2<class BaseClass0,class BaseClass0>::ClassMethod1331<[1]>()
		ldstr "G2_C326::ClassMethod1331.MI.7797<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class G1_C7`2<class BaseClass0,class BaseClass0>::.ctor()
		ret
	}
}
.class interface public abstract IBase1`1<+T0> 
{
	.method public hidebysig newslot abstract virtual instance string Method4() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method5() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method6<M0>() cil managed { }
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
.class interface public abstract IBase2`2<+T0, -T1> 
{
	.method public hidebysig newslot abstract virtual instance string Method7<M0>() cil managed { }
}
.class public auto ansi beforefieldinit Generated808 {
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
	.method static void M.G3_C1280.T<T0,(class G3_C1280`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 20
		.locals init (string[] actualResults)
		ldc.i4.s    15
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1280.T<T0,(class G3_C1280`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 15
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1280`1<!!T0>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1280`1<!!T0>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1280`1<!!T0>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1280`1<!!T0>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1280`1<!!T0>::ClassMethod1990()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1280`1<!!T0>::ClassMethod1991<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1280`1<!!T0>::ClassMethod1992<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1280`1<!!T0>::ClassMethod3934()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1280`1<!!T0>::ClassMethod3935()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1280`1<!!T0>::ClassMethod3936<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1280`1<!!T0>::ClassMethod3937<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1280`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1280`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1280`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     14
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1280`1<!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1280.A<(class G3_C1280`1<class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 20
		.locals init (string[] actualResults)
		ldc.i4.s    15
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1280.A<(class G3_C1280`1<class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 15
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1280`1<class BaseClass0>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1280`1<class BaseClass0>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1280`1<class BaseClass0>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1280`1<class BaseClass0>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1280`1<class BaseClass0>::ClassMethod1990()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1280`1<class BaseClass0>::ClassMethod1991<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1280`1<class BaseClass0>::ClassMethod1992<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1280`1<class BaseClass0>::ClassMethod3934()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1280`1<class BaseClass0>::ClassMethod3935()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1280`1<class BaseClass0>::ClassMethod3936<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1280`1<class BaseClass0>::ClassMethod3937<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1280`1<class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1280`1<class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1280`1<class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     14
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1280`1<class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1280.B<(class G3_C1280`1<class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 20
		.locals init (string[] actualResults)
		ldc.i4.s    15
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1280.B<(class G3_C1280`1<class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 15
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1280`1<class BaseClass1>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1280`1<class BaseClass1>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1280`1<class BaseClass1>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1280`1<class BaseClass1>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1280`1<class BaseClass1>::ClassMethod1990()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1280`1<class BaseClass1>::ClassMethod1991<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1280`1<class BaseClass1>::ClassMethod1992<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1280`1<class BaseClass1>::ClassMethod3934()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1280`1<class BaseClass1>::ClassMethod3935()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1280`1<class BaseClass1>::ClassMethod3936<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1280`1<class BaseClass1>::ClassMethod3937<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1280`1<class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1280`1<class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1280`1<class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     14
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1280`1<class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C326.T.T<T0,T1,(class G2_C326`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C326.T.T<T0,T1,(class G2_C326`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C326`2<!!T0,!!T1>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C326`2<!!T0,!!T1>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C326`2<!!T0,!!T1>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C326`2<!!T0,!!T1>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C326`2<!!T0,!!T1>::ClassMethod1990()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C326`2<!!T0,!!T1>::ClassMethod1991<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C326`2<!!T0,!!T1>::ClassMethod1992<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C326`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C326`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C326`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C326`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C326.A.T<T1,(class G2_C326`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C326.A.T<T1,(class G2_C326`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C326`2<class BaseClass0,!!T1>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C326`2<class BaseClass0,!!T1>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C326`2<class BaseClass0,!!T1>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C326`2<class BaseClass0,!!T1>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C326`2<class BaseClass0,!!T1>::ClassMethod1990()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C326`2<class BaseClass0,!!T1>::ClassMethod1991<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C326`2<class BaseClass0,!!T1>::ClassMethod1992<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C326`2<class BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C326`2<class BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C326`2<class BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C326`2<class BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C326.A.A<(class G2_C326`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C326.A.A<(class G2_C326`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C326`2<class BaseClass0,class BaseClass0>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C326`2<class BaseClass0,class BaseClass0>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C326`2<class BaseClass0,class BaseClass0>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C326`2<class BaseClass0,class BaseClass0>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C326`2<class BaseClass0,class BaseClass0>::ClassMethod1990()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C326`2<class BaseClass0,class BaseClass0>::ClassMethod1991<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C326`2<class BaseClass0,class BaseClass0>::ClassMethod1992<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C326`2<class BaseClass0,class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C326`2<class BaseClass0,class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C326`2<class BaseClass0,class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C326`2<class BaseClass0,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C326.A.B<(class G2_C326`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C326.A.B<(class G2_C326`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C326`2<class BaseClass0,class BaseClass1>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C326`2<class BaseClass0,class BaseClass1>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C326`2<class BaseClass0,class BaseClass1>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C326`2<class BaseClass0,class BaseClass1>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C326`2<class BaseClass0,class BaseClass1>::ClassMethod1990()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C326`2<class BaseClass0,class BaseClass1>::ClassMethod1991<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C326`2<class BaseClass0,class BaseClass1>::ClassMethod1992<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C326`2<class BaseClass0,class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C326`2<class BaseClass0,class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C326`2<class BaseClass0,class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C326`2<class BaseClass0,class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C326.B.T<T1,(class G2_C326`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C326.B.T<T1,(class G2_C326`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C326`2<class BaseClass1,!!T1>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C326`2<class BaseClass1,!!T1>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C326`2<class BaseClass1,!!T1>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C326`2<class BaseClass1,!!T1>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C326`2<class BaseClass1,!!T1>::ClassMethod1990()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C326`2<class BaseClass1,!!T1>::ClassMethod1991<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C326`2<class BaseClass1,!!T1>::ClassMethod1992<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C326`2<class BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C326`2<class BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C326`2<class BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C326`2<class BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C326.B.A<(class G2_C326`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C326.B.A<(class G2_C326`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C326`2<class BaseClass1,class BaseClass0>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C326`2<class BaseClass1,class BaseClass0>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C326`2<class BaseClass1,class BaseClass0>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C326`2<class BaseClass1,class BaseClass0>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C326`2<class BaseClass1,class BaseClass0>::ClassMethod1990()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C326`2<class BaseClass1,class BaseClass0>::ClassMethod1991<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C326`2<class BaseClass1,class BaseClass0>::ClassMethod1992<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C326`2<class BaseClass1,class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C326`2<class BaseClass1,class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C326`2<class BaseClass1,class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C326`2<class BaseClass1,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C326.B.B<(class G2_C326`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C326.B.B<(class G2_C326`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C326`2<class BaseClass1,class BaseClass1>::ClassMethod1328()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C326`2<class BaseClass1,class BaseClass1>::ClassMethod1329()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C326`2<class BaseClass1,class BaseClass1>::ClassMethod1330<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C326`2<class BaseClass1,class BaseClass1>::ClassMethod1331<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C326`2<class BaseClass1,class BaseClass1>::ClassMethod1990()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C326`2<class BaseClass1,class BaseClass1>::ClassMethod1991<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C326`2<class BaseClass1,class BaseClass1>::ClassMethod1992<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C326`2<class BaseClass1,class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C326`2<class BaseClass1,class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C326`2<class BaseClass1,class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C326`2<class BaseClass1,class BaseClass1>::Method7<object>()
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
	.method public hidebysig static void MethodCallingTest() cil managed
	{
		.maxstack  10
		.locals init (object V_0)
		ldstr "========================== Method Calling Test =========================="
		call void [mscorlib]System.Console::WriteLine(string)
		newobj instance void class G3_C1280`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C7`2<class BaseClass0,class BaseClass0>::ClassMethod1331<object>()
		ldstr "G2_C326::ClassMethod1331.MI.7797<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G3_C1280`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C7`2<class BaseClass0,class BaseClass0>::ClassMethod1330<object>()
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G3_C1280`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C7`2<class BaseClass0,class BaseClass0>::ClassMethod1329()
		ldstr "G2_C326::ClassMethod1329.MI.7796()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G3_C1280`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C7`2<class BaseClass0,class BaseClass0>::ClassMethod1328()
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G3_C1280`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C7`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C326::Method7.7791<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G3_C1280`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C7`2<class BaseClass0,class BaseClass0>::Method6<object>()
		ldstr "G1_C7::Method6.4813<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G3_C1280`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C7`2<class BaseClass0,class BaseClass0>::Method5()
		ldstr "G3_C1280::Method5.15305()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G3_C1280`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C7`2<class BaseClass0,class BaseClass0>::Method4()
		ldstr "G2_C326::Method4.7788()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G3_C1280`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G3_C1280::Method4.15304()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1280`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G3_C1280::Method5.15305()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1280`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G3_C1280::Method6.MI.15307<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1280`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G1_C7::Method7.MI.4816<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G3_C1280`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G1_C7::Method7.MI.4816<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1280`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C326::Method7.MI.7792<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1280`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C326::Method7.MI.7792<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1280`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C326`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C326`2<class BaseClass0,class BaseClass1>::ClassMethod1992<object>()
		ldstr "G3_C1280::ClassMethod1992.MI.15313<System.Object>()"
		ldstr "class G2_C326`2<class BaseClass0,class BaseClass1> on type class G3_C1280`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C326`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C326`2<class BaseClass0,class BaseClass1>::ClassMethod1991<object>()
		ldstr "G2_C326::ClassMethod1991.7794<System.Object>()"
		ldstr "class G2_C326`2<class BaseClass0,class BaseClass1> on type class G3_C1280`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C326`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C326`2<class BaseClass0,class BaseClass1>::ClassMethod1990()
		ldstr "G3_C1280::ClassMethod1990.MI.15312()"
		ldstr "class G2_C326`2<class BaseClass0,class BaseClass1> on type class G3_C1280`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C326`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C326`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C326::Method7.7791<System.Object>()"
		ldstr "class G2_C326`2<class BaseClass0,class BaseClass1> on type class G3_C1280`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C326`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C326`2<class BaseClass0,class BaseClass1>::Method6<object>()
		ldstr "G3_C1280::Method6.15306<System.Object>()"
		ldstr "class G2_C326`2<class BaseClass0,class BaseClass1> on type class G3_C1280`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C326`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C326`2<class BaseClass0,class BaseClass1>::Method5()
		ldstr "G3_C1280::Method5.15305()"
		ldstr "class G2_C326`2<class BaseClass0,class BaseClass1> on type class G3_C1280`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C326`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C326`2<class BaseClass0,class BaseClass1>::Method4()
		ldstr "G2_C326::Method4.7788()"
		ldstr "class G2_C326`2<class BaseClass0,class BaseClass1> on type class G3_C1280`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C326`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C326`2<class BaseClass0,class BaseClass1>::ClassMethod1331<object>()
		ldstr "G2_C326::ClassMethod1331.MI.7797<System.Object>()"
		ldstr "class G2_C326`2<class BaseClass0,class BaseClass1> on type class G3_C1280`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C326`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C326`2<class BaseClass0,class BaseClass1>::ClassMethod1330<object>()
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G2_C326`2<class BaseClass0,class BaseClass1> on type class G3_C1280`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C326`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C326`2<class BaseClass0,class BaseClass1>::ClassMethod1329()
		ldstr "G2_C326::ClassMethod1329.MI.7796()"
		ldstr "class G2_C326`2<class BaseClass0,class BaseClass1> on type class G3_C1280`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C326`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C326`2<class BaseClass0,class BaseClass1>::ClassMethod1328()
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G2_C326`2<class BaseClass0,class BaseClass1> on type class G3_C1280`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G2_C326::Method4.7788()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1280`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G3_C1280::Method5.15305()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1280`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G3_C1280::Method6.15306<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1280`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G3_C1280`1<class BaseClass0>
		callvirt instance string class G3_C1280`1<class BaseClass0>::ClassMethod3937<object>()
		ldstr "G3_C1280::ClassMethod3937.15311<System.Object>()"
		ldstr "class G3_C1280`1<class BaseClass0> on type class G3_C1280`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1280`1<class BaseClass0>
		callvirt instance string class G3_C1280`1<class BaseClass0>::ClassMethod3936<object>()
		ldstr "G3_C1280::ClassMethod3936.15310<System.Object>()"
		ldstr "class G3_C1280`1<class BaseClass0> on type class G3_C1280`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1280`1<class BaseClass0>
		callvirt instance string class G3_C1280`1<class BaseClass0>::ClassMethod3935()
		ldstr "G3_C1280::ClassMethod3935.15309()"
		ldstr "class G3_C1280`1<class BaseClass0> on type class G3_C1280`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1280`1<class BaseClass0>
		callvirt instance string class G3_C1280`1<class BaseClass0>::ClassMethod3934()
		ldstr "G3_C1280::ClassMethod3934.15308()"
		ldstr "class G3_C1280`1<class BaseClass0> on type class G3_C1280`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1280`1<class BaseClass0>
		callvirt instance string class G3_C1280`1<class BaseClass0>::Method6<object>()
		ldstr "G3_C1280::Method6.15306<System.Object>()"
		ldstr "class G3_C1280`1<class BaseClass0> on type class G3_C1280`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1280`1<class BaseClass0>
		callvirt instance string class G3_C1280`1<class BaseClass0>::Method5()
		ldstr "G3_C1280::Method5.15305()"
		ldstr "class G3_C1280`1<class BaseClass0> on type class G3_C1280`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1280`1<class BaseClass0>
		callvirt instance string class G3_C1280`1<class BaseClass0>::Method4()
		ldstr "G3_C1280::Method4.15304()"
		ldstr "class G3_C1280`1<class BaseClass0> on type class G3_C1280`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1280`1<class BaseClass0>
		callvirt instance string class G3_C1280`1<class BaseClass0>::ClassMethod1992<object>()
		ldstr "G3_C1280::ClassMethod1992.MI.15313<System.Object>()"
		ldstr "class G3_C1280`1<class BaseClass0> on type class G3_C1280`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1280`1<class BaseClass0>
		callvirt instance string class G3_C1280`1<class BaseClass0>::ClassMethod1991<object>()
		ldstr "G2_C326::ClassMethod1991.7794<System.Object>()"
		ldstr "class G3_C1280`1<class BaseClass0> on type class G3_C1280`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1280`1<class BaseClass0>
		callvirt instance string class G3_C1280`1<class BaseClass0>::ClassMethod1990()
		ldstr "G3_C1280::ClassMethod1990.MI.15312()"
		ldstr "class G3_C1280`1<class BaseClass0> on type class G3_C1280`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1280`1<class BaseClass0>
		callvirt instance string class G3_C1280`1<class BaseClass0>::Method7<object>()
		ldstr "G2_C326::Method7.7791<System.Object>()"
		ldstr "class G3_C1280`1<class BaseClass0> on type class G3_C1280`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1280`1<class BaseClass0>
		callvirt instance string class G3_C1280`1<class BaseClass0>::ClassMethod1331<object>()
		ldstr "G2_C326::ClassMethod1331.MI.7797<System.Object>()"
		ldstr "class G3_C1280`1<class BaseClass0> on type class G3_C1280`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1280`1<class BaseClass0>
		callvirt instance string class G3_C1280`1<class BaseClass0>::ClassMethod1330<object>()
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G3_C1280`1<class BaseClass0> on type class G3_C1280`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1280`1<class BaseClass0>
		callvirt instance string class G3_C1280`1<class BaseClass0>::ClassMethod1329()
		ldstr "G2_C326::ClassMethod1329.MI.7796()"
		ldstr "class G3_C1280`1<class BaseClass0> on type class G3_C1280`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1280`1<class BaseClass0>
		callvirt instance string class G3_C1280`1<class BaseClass0>::ClassMethod1328()
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G3_C1280`1<class BaseClass0> on type class G3_C1280`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G3_C1280`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C7`2<class BaseClass0,class BaseClass0>::ClassMethod1331<object>()
		ldstr "G2_C326::ClassMethod1331.MI.7797<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G3_C1280`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C7`2<class BaseClass0,class BaseClass0>::ClassMethod1330<object>()
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G3_C1280`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C7`2<class BaseClass0,class BaseClass0>::ClassMethod1329()
		ldstr "G2_C326::ClassMethod1329.MI.7796()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G3_C1280`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C7`2<class BaseClass0,class BaseClass0>::ClassMethod1328()
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G3_C1280`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C7`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C326::Method7.7791<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G3_C1280`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C7`2<class BaseClass0,class BaseClass0>::Method6<object>()
		ldstr "G1_C7::Method6.4813<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G3_C1280`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C7`2<class BaseClass0,class BaseClass0>::Method5()
		ldstr "G3_C1280::Method5.15305()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G3_C1280`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C7`2<class BaseClass0,class BaseClass0>::Method4()
		ldstr "G2_C326::Method4.7788()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G3_C1280`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G3_C1280::Method4.15304()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1280`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G3_C1280::Method5.15305()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1280`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G3_C1280::Method6.MI.15307<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1280`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G1_C7::Method7.MI.4816<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G3_C1280`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G1_C7::Method7.MI.4816<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1280`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C326::Method7.MI.7792<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1280`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C326::Method7.MI.7792<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1280`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C326`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C326`2<class BaseClass0,class BaseClass1>::ClassMethod1992<object>()
		ldstr "G3_C1280::ClassMethod1992.MI.15313<System.Object>()"
		ldstr "class G2_C326`2<class BaseClass0,class BaseClass1> on type class G3_C1280`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C326`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C326`2<class BaseClass0,class BaseClass1>::ClassMethod1991<object>()
		ldstr "G2_C326::ClassMethod1991.7794<System.Object>()"
		ldstr "class G2_C326`2<class BaseClass0,class BaseClass1> on type class G3_C1280`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C326`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C326`2<class BaseClass0,class BaseClass1>::ClassMethod1990()
		ldstr "G3_C1280::ClassMethod1990.MI.15312()"
		ldstr "class G2_C326`2<class BaseClass0,class BaseClass1> on type class G3_C1280`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C326`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C326`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C326::Method7.7791<System.Object>()"
		ldstr "class G2_C326`2<class BaseClass0,class BaseClass1> on type class G3_C1280`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C326`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C326`2<class BaseClass0,class BaseClass1>::Method6<object>()
		ldstr "G3_C1280::Method6.15306<System.Object>()"
		ldstr "class G2_C326`2<class BaseClass0,class BaseClass1> on type class G3_C1280`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C326`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C326`2<class BaseClass0,class BaseClass1>::Method5()
		ldstr "G3_C1280::Method5.15305()"
		ldstr "class G2_C326`2<class BaseClass0,class BaseClass1> on type class G3_C1280`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C326`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C326`2<class BaseClass0,class BaseClass1>::Method4()
		ldstr "G2_C326::Method4.7788()"
		ldstr "class G2_C326`2<class BaseClass0,class BaseClass1> on type class G3_C1280`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C326`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C326`2<class BaseClass0,class BaseClass1>::ClassMethod1331<object>()
		ldstr "G2_C326::ClassMethod1331.MI.7797<System.Object>()"
		ldstr "class G2_C326`2<class BaseClass0,class BaseClass1> on type class G3_C1280`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C326`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C326`2<class BaseClass0,class BaseClass1>::ClassMethod1330<object>()
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G2_C326`2<class BaseClass0,class BaseClass1> on type class G3_C1280`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C326`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C326`2<class BaseClass0,class BaseClass1>::ClassMethod1329()
		ldstr "G2_C326::ClassMethod1329.MI.7796()"
		ldstr "class G2_C326`2<class BaseClass0,class BaseClass1> on type class G3_C1280`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C326`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C326`2<class BaseClass0,class BaseClass1>::ClassMethod1328()
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G2_C326`2<class BaseClass0,class BaseClass1> on type class G3_C1280`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G3_C1280::Method4.15304()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1280`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G3_C1280::Method5.15305()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1280`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G3_C1280::Method6.MI.15307<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1280`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G3_C1280`1<class BaseClass1>
		callvirt instance string class G3_C1280`1<class BaseClass1>::ClassMethod3937<object>()
		ldstr "G3_C1280::ClassMethod3937.15311<System.Object>()"
		ldstr "class G3_C1280`1<class BaseClass1> on type class G3_C1280`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1280`1<class BaseClass1>
		callvirt instance string class G3_C1280`1<class BaseClass1>::ClassMethod3936<object>()
		ldstr "G3_C1280::ClassMethod3936.15310<System.Object>()"
		ldstr "class G3_C1280`1<class BaseClass1> on type class G3_C1280`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1280`1<class BaseClass1>
		callvirt instance string class G3_C1280`1<class BaseClass1>::ClassMethod3935()
		ldstr "G3_C1280::ClassMethod3935.15309()"
		ldstr "class G3_C1280`1<class BaseClass1> on type class G3_C1280`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1280`1<class BaseClass1>
		callvirt instance string class G3_C1280`1<class BaseClass1>::ClassMethod3934()
		ldstr "G3_C1280::ClassMethod3934.15308()"
		ldstr "class G3_C1280`1<class BaseClass1> on type class G3_C1280`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1280`1<class BaseClass1>
		callvirt instance string class G3_C1280`1<class BaseClass1>::Method6<object>()
		ldstr "G3_C1280::Method6.15306<System.Object>()"
		ldstr "class G3_C1280`1<class BaseClass1> on type class G3_C1280`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1280`1<class BaseClass1>
		callvirt instance string class G3_C1280`1<class BaseClass1>::Method5()
		ldstr "G3_C1280::Method5.15305()"
		ldstr "class G3_C1280`1<class BaseClass1> on type class G3_C1280`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1280`1<class BaseClass1>
		callvirt instance string class G3_C1280`1<class BaseClass1>::Method4()
		ldstr "G3_C1280::Method4.15304()"
		ldstr "class G3_C1280`1<class BaseClass1> on type class G3_C1280`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1280`1<class BaseClass1>
		callvirt instance string class G3_C1280`1<class BaseClass1>::ClassMethod1992<object>()
		ldstr "G3_C1280::ClassMethod1992.MI.15313<System.Object>()"
		ldstr "class G3_C1280`1<class BaseClass1> on type class G3_C1280`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1280`1<class BaseClass1>
		callvirt instance string class G3_C1280`1<class BaseClass1>::ClassMethod1991<object>()
		ldstr "G2_C326::ClassMethod1991.7794<System.Object>()"
		ldstr "class G3_C1280`1<class BaseClass1> on type class G3_C1280`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1280`1<class BaseClass1>
		callvirt instance string class G3_C1280`1<class BaseClass1>::ClassMethod1990()
		ldstr "G3_C1280::ClassMethod1990.MI.15312()"
		ldstr "class G3_C1280`1<class BaseClass1> on type class G3_C1280`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1280`1<class BaseClass1>
		callvirt instance string class G3_C1280`1<class BaseClass1>::Method7<object>()
		ldstr "G2_C326::Method7.7791<System.Object>()"
		ldstr "class G3_C1280`1<class BaseClass1> on type class G3_C1280`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1280`1<class BaseClass1>
		callvirt instance string class G3_C1280`1<class BaseClass1>::ClassMethod1331<object>()
		ldstr "G2_C326::ClassMethod1331.MI.7797<System.Object>()"
		ldstr "class G3_C1280`1<class BaseClass1> on type class G3_C1280`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1280`1<class BaseClass1>
		callvirt instance string class G3_C1280`1<class BaseClass1>::ClassMethod1330<object>()
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G3_C1280`1<class BaseClass1> on type class G3_C1280`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1280`1<class BaseClass1>
		callvirt instance string class G3_C1280`1<class BaseClass1>::ClassMethod1329()
		ldstr "G2_C326::ClassMethod1329.MI.7796()"
		ldstr "class G3_C1280`1<class BaseClass1> on type class G3_C1280`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1280`1<class BaseClass1>
		callvirt instance string class G3_C1280`1<class BaseClass1>::ClassMethod1328()
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G3_C1280`1<class BaseClass1> on type class G3_C1280`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C326`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C7`2<class BaseClass0,class BaseClass0>::ClassMethod1331<object>()
		ldstr "G2_C326::ClassMethod1331.MI.7797<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C7`2<class BaseClass0,class BaseClass0>::ClassMethod1330<object>()
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C7`2<class BaseClass0,class BaseClass0>::ClassMethod1329()
		ldstr "G2_C326::ClassMethod1329.MI.7796()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C7`2<class BaseClass0,class BaseClass0>::ClassMethod1328()
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C7`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C326::Method7.7791<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C7`2<class BaseClass0,class BaseClass0>::Method6<object>()
		ldstr "G1_C7::Method6.4813<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C7`2<class BaseClass0,class BaseClass0>::Method5()
		ldstr "G2_C326::Method5.7789()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C7`2<class BaseClass0,class BaseClass0>::Method4()
		ldstr "G2_C326::Method4.7788()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C326::Method4.7788()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C326`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C326::Method5.7789()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C326`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C326::Method6.7790<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C326`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G2_C326::Method7.MI.7792<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G2_C326`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C326::Method7.MI.7792<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C326::Method7.MI.7792<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C326`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C326::Method7.MI.7792<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C326`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C326`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C326`2<class BaseClass0,class BaseClass0>::ClassMethod1992<object>()
		ldstr "G2_C326::ClassMethod1992.7795<System.Object>()"
		ldstr "class G2_C326`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C326`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C326`2<class BaseClass0,class BaseClass0>::ClassMethod1991<object>()
		ldstr "G2_C326::ClassMethod1991.7794<System.Object>()"
		ldstr "class G2_C326`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C326`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C326`2<class BaseClass0,class BaseClass0>::ClassMethod1990()
		ldstr "G2_C326::ClassMethod1990.7793()"
		ldstr "class G2_C326`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C326`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C326`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C326::Method7.7791<System.Object>()"
		ldstr "class G2_C326`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C326`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C326`2<class BaseClass0,class BaseClass0>::Method6<object>()
		ldstr "G2_C326::Method6.7790<System.Object>()"
		ldstr "class G2_C326`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C326`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C326`2<class BaseClass0,class BaseClass0>::Method5()
		ldstr "G2_C326::Method5.7789()"
		ldstr "class G2_C326`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C326`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C326`2<class BaseClass0,class BaseClass0>::Method4()
		ldstr "G2_C326::Method4.7788()"
		ldstr "class G2_C326`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C326`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C326`2<class BaseClass0,class BaseClass0>::ClassMethod1331<object>()
		ldstr "G2_C326::ClassMethod1331.MI.7797<System.Object>()"
		ldstr "class G2_C326`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C326`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C326`2<class BaseClass0,class BaseClass0>::ClassMethod1330<object>()
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G2_C326`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C326`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C326`2<class BaseClass0,class BaseClass0>::ClassMethod1329()
		ldstr "G2_C326::ClassMethod1329.MI.7796()"
		ldstr "class G2_C326`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C326`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C326`2<class BaseClass0,class BaseClass0>::ClassMethod1328()
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G2_C326`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G2_C326::Method4.7788()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C326`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G2_C326::Method5.7789()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C326`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C326::Method6.7790<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C326`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C326`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C7`2<class BaseClass0,class BaseClass0>::ClassMethod1331<object>()
		ldstr "G2_C326::ClassMethod1331.MI.7797<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C7`2<class BaseClass0,class BaseClass0>::ClassMethod1330<object>()
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C7`2<class BaseClass0,class BaseClass0>::ClassMethod1329()
		ldstr "G2_C326::ClassMethod1329.MI.7796()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C7`2<class BaseClass0,class BaseClass0>::ClassMethod1328()
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C7`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C326::Method7.7791<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C7`2<class BaseClass0,class BaseClass0>::Method6<object>()
		ldstr "G1_C7::Method6.4813<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C7`2<class BaseClass0,class BaseClass0>::Method5()
		ldstr "G2_C326::Method5.7789()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C7`2<class BaseClass0,class BaseClass0>::Method4()
		ldstr "G2_C326::Method4.7788()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C326::Method4.7788()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C326`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C326::Method5.7789()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C326`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C326::Method6.7790<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C326`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G1_C7::Method7.MI.4816<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G2_C326`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G1_C7::Method7.MI.4816<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C326::Method7.MI.7792<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C326`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C326::Method7.MI.7792<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C326`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C326`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C326`2<class BaseClass0,class BaseClass1>::ClassMethod1992<object>()
		ldstr "G2_C326::ClassMethod1992.7795<System.Object>()"
		ldstr "class G2_C326`2<class BaseClass0,class BaseClass1> on type class G2_C326`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C326`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C326`2<class BaseClass0,class BaseClass1>::ClassMethod1991<object>()
		ldstr "G2_C326::ClassMethod1991.7794<System.Object>()"
		ldstr "class G2_C326`2<class BaseClass0,class BaseClass1> on type class G2_C326`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C326`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C326`2<class BaseClass0,class BaseClass1>::ClassMethod1990()
		ldstr "G2_C326::ClassMethod1990.7793()"
		ldstr "class G2_C326`2<class BaseClass0,class BaseClass1> on type class G2_C326`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C326`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C326`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C326::Method7.7791<System.Object>()"
		ldstr "class G2_C326`2<class BaseClass0,class BaseClass1> on type class G2_C326`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C326`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C326`2<class BaseClass0,class BaseClass1>::Method6<object>()
		ldstr "G2_C326::Method6.7790<System.Object>()"
		ldstr "class G2_C326`2<class BaseClass0,class BaseClass1> on type class G2_C326`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C326`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C326`2<class BaseClass0,class BaseClass1>::Method5()
		ldstr "G2_C326::Method5.7789()"
		ldstr "class G2_C326`2<class BaseClass0,class BaseClass1> on type class G2_C326`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C326`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C326`2<class BaseClass0,class BaseClass1>::Method4()
		ldstr "G2_C326::Method4.7788()"
		ldstr "class G2_C326`2<class BaseClass0,class BaseClass1> on type class G2_C326`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C326`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C326`2<class BaseClass0,class BaseClass1>::ClassMethod1331<object>()
		ldstr "G2_C326::ClassMethod1331.MI.7797<System.Object>()"
		ldstr "class G2_C326`2<class BaseClass0,class BaseClass1> on type class G2_C326`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C326`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C326`2<class BaseClass0,class BaseClass1>::ClassMethod1330<object>()
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G2_C326`2<class BaseClass0,class BaseClass1> on type class G2_C326`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C326`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C326`2<class BaseClass0,class BaseClass1>::ClassMethod1329()
		ldstr "G2_C326::ClassMethod1329.MI.7796()"
		ldstr "class G2_C326`2<class BaseClass0,class BaseClass1> on type class G2_C326`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C326`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C326`2<class BaseClass0,class BaseClass1>::ClassMethod1328()
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G2_C326`2<class BaseClass0,class BaseClass1> on type class G2_C326`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G2_C326::Method4.7788()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C326`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G2_C326::Method5.7789()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C326`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C326::Method6.7790<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C326`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C326`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C7`2<class BaseClass0,class BaseClass0>::ClassMethod1331<object>()
		ldstr "G2_C326::ClassMethod1331.MI.7797<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C7`2<class BaseClass0,class BaseClass0>::ClassMethod1330<object>()
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C7`2<class BaseClass0,class BaseClass0>::ClassMethod1329()
		ldstr "G2_C326::ClassMethod1329.MI.7796()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C7`2<class BaseClass0,class BaseClass0>::ClassMethod1328()
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C7`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C326::Method7.7791<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C7`2<class BaseClass0,class BaseClass0>::Method6<object>()
		ldstr "G1_C7::Method6.4813<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C7`2<class BaseClass0,class BaseClass0>::Method5()
		ldstr "G2_C326::Method5.7789()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C7`2<class BaseClass0,class BaseClass0>::Method4()
		ldstr "G2_C326::Method4.7788()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C326::Method4.7788()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C326`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C326::Method5.7789()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C326`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C326::Method6.7790<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C326`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G2_C326::Method7.MI.7792<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G2_C326`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C326::Method7.MI.7792<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C326::Method7.MI.7792<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C326`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C326::Method7.MI.7792<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C326`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C326`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C326`2<class BaseClass1,class BaseClass0>::ClassMethod1992<object>()
		ldstr "G2_C326::ClassMethod1992.7795<System.Object>()"
		ldstr "class G2_C326`2<class BaseClass1,class BaseClass0> on type class G2_C326`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C326`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C326`2<class BaseClass1,class BaseClass0>::ClassMethod1991<object>()
		ldstr "G2_C326::ClassMethod1991.7794<System.Object>()"
		ldstr "class G2_C326`2<class BaseClass1,class BaseClass0> on type class G2_C326`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C326`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C326`2<class BaseClass1,class BaseClass0>::ClassMethod1990()
		ldstr "G2_C326::ClassMethod1990.7793()"
		ldstr "class G2_C326`2<class BaseClass1,class BaseClass0> on type class G2_C326`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C326`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C326`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G2_C326::Method7.7791<System.Object>()"
		ldstr "class G2_C326`2<class BaseClass1,class BaseClass0> on type class G2_C326`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C326`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C326`2<class BaseClass1,class BaseClass0>::Method6<object>()
		ldstr "G2_C326::Method6.7790<System.Object>()"
		ldstr "class G2_C326`2<class BaseClass1,class BaseClass0> on type class G2_C326`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C326`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C326`2<class BaseClass1,class BaseClass0>::Method5()
		ldstr "G2_C326::Method5.7789()"
		ldstr "class G2_C326`2<class BaseClass1,class BaseClass0> on type class G2_C326`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C326`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C326`2<class BaseClass1,class BaseClass0>::Method4()
		ldstr "G2_C326::Method4.7788()"
		ldstr "class G2_C326`2<class BaseClass1,class BaseClass0> on type class G2_C326`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C326`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C326`2<class BaseClass1,class BaseClass0>::ClassMethod1331<object>()
		ldstr "G2_C326::ClassMethod1331.MI.7797<System.Object>()"
		ldstr "class G2_C326`2<class BaseClass1,class BaseClass0> on type class G2_C326`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C326`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C326`2<class BaseClass1,class BaseClass0>::ClassMethod1330<object>()
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G2_C326`2<class BaseClass1,class BaseClass0> on type class G2_C326`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C326`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C326`2<class BaseClass1,class BaseClass0>::ClassMethod1329()
		ldstr "G2_C326::ClassMethod1329.MI.7796()"
		ldstr "class G2_C326`2<class BaseClass1,class BaseClass0> on type class G2_C326`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C326`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C326`2<class BaseClass1,class BaseClass0>::ClassMethod1328()
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G2_C326`2<class BaseClass1,class BaseClass0> on type class G2_C326`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G2_C326::Method4.7788()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C326`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G2_C326::Method5.7789()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C326`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C326::Method6.7790<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C326`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C326`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C7`2<class BaseClass0,class BaseClass0>::ClassMethod1331<object>()
		ldstr "G2_C326::ClassMethod1331.MI.7797<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C7`2<class BaseClass0,class BaseClass0>::ClassMethod1330<object>()
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C7`2<class BaseClass0,class BaseClass0>::ClassMethod1329()
		ldstr "G2_C326::ClassMethod1329.MI.7796()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C7`2<class BaseClass0,class BaseClass0>::ClassMethod1328()
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C7`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C326::Method7.7791<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C7`2<class BaseClass0,class BaseClass0>::Method6<object>()
		ldstr "G1_C7::Method6.4813<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C7`2<class BaseClass0,class BaseClass0>::Method5()
		ldstr "G2_C326::Method5.7789()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C7`2<class BaseClass0,class BaseClass0>::Method4()
		ldstr "G2_C326::Method4.7788()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C326::Method4.7788()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C326`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C326::Method5.7789()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C326`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C326::Method6.7790<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C326`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G1_C7::Method7.MI.4816<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G2_C326`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G1_C7::Method7.MI.4816<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C326::Method7.MI.7792<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C326`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C326::Method7.MI.7792<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C326`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C326`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C326`2<class BaseClass1,class BaseClass1>::ClassMethod1992<object>()
		ldstr "G2_C326::ClassMethod1992.7795<System.Object>()"
		ldstr "class G2_C326`2<class BaseClass1,class BaseClass1> on type class G2_C326`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C326`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C326`2<class BaseClass1,class BaseClass1>::ClassMethod1991<object>()
		ldstr "G2_C326::ClassMethod1991.7794<System.Object>()"
		ldstr "class G2_C326`2<class BaseClass1,class BaseClass1> on type class G2_C326`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C326`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C326`2<class BaseClass1,class BaseClass1>::ClassMethod1990()
		ldstr "G2_C326::ClassMethod1990.7793()"
		ldstr "class G2_C326`2<class BaseClass1,class BaseClass1> on type class G2_C326`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C326`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C326`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C326::Method7.7791<System.Object>()"
		ldstr "class G2_C326`2<class BaseClass1,class BaseClass1> on type class G2_C326`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C326`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C326`2<class BaseClass1,class BaseClass1>::Method6<object>()
		ldstr "G2_C326::Method6.7790<System.Object>()"
		ldstr "class G2_C326`2<class BaseClass1,class BaseClass1> on type class G2_C326`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C326`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C326`2<class BaseClass1,class BaseClass1>::Method5()
		ldstr "G2_C326::Method5.7789()"
		ldstr "class G2_C326`2<class BaseClass1,class BaseClass1> on type class G2_C326`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C326`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C326`2<class BaseClass1,class BaseClass1>::Method4()
		ldstr "G2_C326::Method4.7788()"
		ldstr "class G2_C326`2<class BaseClass1,class BaseClass1> on type class G2_C326`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C326`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C326`2<class BaseClass1,class BaseClass1>::ClassMethod1331<object>()
		ldstr "G2_C326::ClassMethod1331.MI.7797<System.Object>()"
		ldstr "class G2_C326`2<class BaseClass1,class BaseClass1> on type class G2_C326`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C326`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C326`2<class BaseClass1,class BaseClass1>::ClassMethod1330<object>()
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G2_C326`2<class BaseClass1,class BaseClass1> on type class G2_C326`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C326`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C326`2<class BaseClass1,class BaseClass1>::ClassMethod1329()
		ldstr "G2_C326::ClassMethod1329.MI.7796()"
		ldstr "class G2_C326`2<class BaseClass1,class BaseClass1> on type class G2_C326`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C326`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C326`2<class BaseClass1,class BaseClass1>::ClassMethod1328()
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G2_C326`2<class BaseClass1,class BaseClass1> on type class G2_C326`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G2_C326::Method4.7788()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C326`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G2_C326::Method5.7789()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C326`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C326::Method6.7790<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C326`2<class BaseClass1,class BaseClass1>"
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
		newobj instance void class G3_C1280`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C326::ClassMethod1329.MI.7796()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C326::ClassMethod1331.MI.7797<System.Object>()#G2_C326::Method4.7788()#G3_C1280::Method5.15305()#G1_C7::Method6.4813<System.Object>()#G2_C326::Method7.7791<System.Object>()#"
		call void Generated808::M.G1_C7.T.T<class BaseClass0,class BaseClass0,class G3_C1280`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C326::ClassMethod1329.MI.7796()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C326::ClassMethod1331.MI.7797<System.Object>()#G2_C326::Method4.7788()#G3_C1280::Method5.15305()#G1_C7::Method6.4813<System.Object>()#G2_C326::Method7.7791<System.Object>()#"
		call void Generated808::M.G1_C7.A.T<class BaseClass0,class G3_C1280`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C326::ClassMethod1329.MI.7796()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C326::ClassMethod1331.MI.7797<System.Object>()#G2_C326::Method4.7788()#G3_C1280::Method5.15305()#G1_C7::Method6.4813<System.Object>()#G2_C326::Method7.7791<System.Object>()#"
		call void Generated808::M.G1_C7.A.A<class G3_C1280`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1280::Method4.15304()#G3_C1280::Method5.15305()#G3_C1280::Method6.MI.15307<System.Object>()#"
		call void Generated808::M.IBase1.T<class BaseClass0,class G3_C1280`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1280::Method4.15304()#G3_C1280::Method5.15305()#G3_C1280::Method6.MI.15307<System.Object>()#"
		call void Generated808::M.IBase1.A<class G3_C1280`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated808::M.IBase2.T.T<class BaseClass1,class BaseClass0,class G3_C1280`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated808::M.IBase2.B.T<class BaseClass0,class G3_C1280`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated808::M.IBase2.B.A<class G3_C1280`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated808::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G3_C1280`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated808::M.IBase2.A.T<class BaseClass0,class G3_C1280`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated808::M.IBase2.A.A<class G3_C1280`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C326::Method7.MI.7792<System.Object>()#"
		call void Generated808::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G3_C1280`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C326::Method7.MI.7792<System.Object>()#"
		call void Generated808::M.IBase2.A.T<class BaseClass1,class G3_C1280`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C326::Method7.MI.7792<System.Object>()#"
		call void Generated808::M.IBase2.A.B<class G3_C1280`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C326::Method7.MI.7792<System.Object>()#"
		call void Generated808::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G3_C1280`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C326::Method7.MI.7792<System.Object>()#"
		call void Generated808::M.IBase2.B.T<class BaseClass1,class G3_C1280`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C326::Method7.MI.7792<System.Object>()#"
		call void Generated808::M.IBase2.B.B<class G3_C1280`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C326::ClassMethod1329.MI.7796()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C326::ClassMethod1331.MI.7797<System.Object>()#G3_C1280::ClassMethod1990.MI.15312()#G2_C326::ClassMethod1991.7794<System.Object>()#G3_C1280::ClassMethod1992.MI.15313<System.Object>()#G2_C326::Method4.7788()#G3_C1280::Method5.15305()#G3_C1280::Method6.15306<System.Object>()#G2_C326::Method7.7791<System.Object>()#"
		call void Generated808::M.G2_C326.T.T<class BaseClass0,class BaseClass1,class G3_C1280`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C326::ClassMethod1329.MI.7796()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C326::ClassMethod1331.MI.7797<System.Object>()#G3_C1280::ClassMethod1990.MI.15312()#G2_C326::ClassMethod1991.7794<System.Object>()#G3_C1280::ClassMethod1992.MI.15313<System.Object>()#G2_C326::Method4.7788()#G3_C1280::Method5.15305()#G3_C1280::Method6.15306<System.Object>()#G2_C326::Method7.7791<System.Object>()#"
		call void Generated808::M.G2_C326.A.T<class BaseClass1,class G3_C1280`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C326::ClassMethod1329.MI.7796()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C326::ClassMethod1331.MI.7797<System.Object>()#G3_C1280::ClassMethod1990.MI.15312()#G2_C326::ClassMethod1991.7794<System.Object>()#G3_C1280::ClassMethod1992.MI.15313<System.Object>()#G2_C326::Method4.7788()#G3_C1280::Method5.15305()#G3_C1280::Method6.15306<System.Object>()#G2_C326::Method7.7791<System.Object>()#"
		call void Generated808::M.G2_C326.A.B<class G3_C1280`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C326::Method4.7788()#G3_C1280::Method5.15305()#G3_C1280::Method6.15306<System.Object>()#"
		call void Generated808::M.IBase1.T<class BaseClass1,class G3_C1280`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C326::Method4.7788()#G3_C1280::Method5.15305()#G3_C1280::Method6.15306<System.Object>()#"
		call void Generated808::M.IBase1.B<class G3_C1280`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C326::ClassMethod1329.MI.7796()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C326::ClassMethod1331.MI.7797<System.Object>()#G3_C1280::ClassMethod1990.MI.15312()#G2_C326::ClassMethod1991.7794<System.Object>()#G3_C1280::ClassMethod1992.MI.15313<System.Object>()#G3_C1280::ClassMethod3934.15308()#G3_C1280::ClassMethod3935.15309()#G3_C1280::ClassMethod3936.15310<System.Object>()#G3_C1280::ClassMethod3937.15311<System.Object>()#G3_C1280::Method4.15304()#G3_C1280::Method5.15305()#G3_C1280::Method6.15306<System.Object>()#G2_C326::Method7.7791<System.Object>()#"
		call void Generated808::M.G3_C1280.T<class BaseClass0,class G3_C1280`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C326::ClassMethod1329.MI.7796()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C326::ClassMethod1331.MI.7797<System.Object>()#G3_C1280::ClassMethod1990.MI.15312()#G2_C326::ClassMethod1991.7794<System.Object>()#G3_C1280::ClassMethod1992.MI.15313<System.Object>()#G3_C1280::ClassMethod3934.15308()#G3_C1280::ClassMethod3935.15309()#G3_C1280::ClassMethod3936.15310<System.Object>()#G3_C1280::ClassMethod3937.15311<System.Object>()#G3_C1280::Method4.15304()#G3_C1280::Method5.15305()#G3_C1280::Method6.15306<System.Object>()#G2_C326::Method7.7791<System.Object>()#"
		call void Generated808::M.G3_C1280.A<class G3_C1280`1<class BaseClass0>>(!!0,string)
		newobj instance void class G3_C1280`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C326::ClassMethod1329.MI.7796()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C326::ClassMethod1331.MI.7797<System.Object>()#G2_C326::Method4.7788()#G3_C1280::Method5.15305()#G1_C7::Method6.4813<System.Object>()#G2_C326::Method7.7791<System.Object>()#"
		call void Generated808::M.G1_C7.T.T<class BaseClass0,class BaseClass0,class G3_C1280`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C326::ClassMethod1329.MI.7796()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C326::ClassMethod1331.MI.7797<System.Object>()#G2_C326::Method4.7788()#G3_C1280::Method5.15305()#G1_C7::Method6.4813<System.Object>()#G2_C326::Method7.7791<System.Object>()#"
		call void Generated808::M.G1_C7.A.T<class BaseClass0,class G3_C1280`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C326::ClassMethod1329.MI.7796()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C326::ClassMethod1331.MI.7797<System.Object>()#G2_C326::Method4.7788()#G3_C1280::Method5.15305()#G1_C7::Method6.4813<System.Object>()#G2_C326::Method7.7791<System.Object>()#"
		call void Generated808::M.G1_C7.A.A<class G3_C1280`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1280::Method4.15304()#G3_C1280::Method5.15305()#G3_C1280::Method6.MI.15307<System.Object>()#"
		call void Generated808::M.IBase1.T<class BaseClass0,class G3_C1280`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1280::Method4.15304()#G3_C1280::Method5.15305()#G3_C1280::Method6.MI.15307<System.Object>()#"
		call void Generated808::M.IBase1.A<class G3_C1280`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated808::M.IBase2.T.T<class BaseClass1,class BaseClass0,class G3_C1280`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated808::M.IBase2.B.T<class BaseClass0,class G3_C1280`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated808::M.IBase2.B.A<class G3_C1280`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated808::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G3_C1280`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated808::M.IBase2.A.T<class BaseClass0,class G3_C1280`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated808::M.IBase2.A.A<class G3_C1280`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C326::Method7.MI.7792<System.Object>()#"
		call void Generated808::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G3_C1280`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C326::Method7.MI.7792<System.Object>()#"
		call void Generated808::M.IBase2.A.T<class BaseClass1,class G3_C1280`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C326::Method7.MI.7792<System.Object>()#"
		call void Generated808::M.IBase2.A.B<class G3_C1280`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C326::Method7.MI.7792<System.Object>()#"
		call void Generated808::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G3_C1280`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C326::Method7.MI.7792<System.Object>()#"
		call void Generated808::M.IBase2.B.T<class BaseClass1,class G3_C1280`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C326::Method7.MI.7792<System.Object>()#"
		call void Generated808::M.IBase2.B.B<class G3_C1280`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C326::ClassMethod1329.MI.7796()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C326::ClassMethod1331.MI.7797<System.Object>()#G3_C1280::ClassMethod1990.MI.15312()#G2_C326::ClassMethod1991.7794<System.Object>()#G3_C1280::ClassMethod1992.MI.15313<System.Object>()#G2_C326::Method4.7788()#G3_C1280::Method5.15305()#G3_C1280::Method6.15306<System.Object>()#G2_C326::Method7.7791<System.Object>()#"
		call void Generated808::M.G2_C326.T.T<class BaseClass0,class BaseClass1,class G3_C1280`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C326::ClassMethod1329.MI.7796()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C326::ClassMethod1331.MI.7797<System.Object>()#G3_C1280::ClassMethod1990.MI.15312()#G2_C326::ClassMethod1991.7794<System.Object>()#G3_C1280::ClassMethod1992.MI.15313<System.Object>()#G2_C326::Method4.7788()#G3_C1280::Method5.15305()#G3_C1280::Method6.15306<System.Object>()#G2_C326::Method7.7791<System.Object>()#"
		call void Generated808::M.G2_C326.A.T<class BaseClass1,class G3_C1280`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C326::ClassMethod1329.MI.7796()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C326::ClassMethod1331.MI.7797<System.Object>()#G3_C1280::ClassMethod1990.MI.15312()#G2_C326::ClassMethod1991.7794<System.Object>()#G3_C1280::ClassMethod1992.MI.15313<System.Object>()#G2_C326::Method4.7788()#G3_C1280::Method5.15305()#G3_C1280::Method6.15306<System.Object>()#G2_C326::Method7.7791<System.Object>()#"
		call void Generated808::M.G2_C326.A.B<class G3_C1280`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1280::Method4.15304()#G3_C1280::Method5.15305()#G3_C1280::Method6.MI.15307<System.Object>()#"
		call void Generated808::M.IBase1.T<class BaseClass1,class G3_C1280`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1280::Method4.15304()#G3_C1280::Method5.15305()#G3_C1280::Method6.MI.15307<System.Object>()#"
		call void Generated808::M.IBase1.B<class G3_C1280`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C326::ClassMethod1329.MI.7796()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C326::ClassMethod1331.MI.7797<System.Object>()#G3_C1280::ClassMethod1990.MI.15312()#G2_C326::ClassMethod1991.7794<System.Object>()#G3_C1280::ClassMethod1992.MI.15313<System.Object>()#G3_C1280::ClassMethod3934.15308()#G3_C1280::ClassMethod3935.15309()#G3_C1280::ClassMethod3936.15310<System.Object>()#G3_C1280::ClassMethod3937.15311<System.Object>()#G3_C1280::Method4.15304()#G3_C1280::Method5.15305()#G3_C1280::Method6.15306<System.Object>()#G2_C326::Method7.7791<System.Object>()#"
		call void Generated808::M.G3_C1280.T<class BaseClass1,class G3_C1280`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C326::ClassMethod1329.MI.7796()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C326::ClassMethod1331.MI.7797<System.Object>()#G3_C1280::ClassMethod1990.MI.15312()#G2_C326::ClassMethod1991.7794<System.Object>()#G3_C1280::ClassMethod1992.MI.15313<System.Object>()#G3_C1280::ClassMethod3934.15308()#G3_C1280::ClassMethod3935.15309()#G3_C1280::ClassMethod3936.15310<System.Object>()#G3_C1280::ClassMethod3937.15311<System.Object>()#G3_C1280::Method4.15304()#G3_C1280::Method5.15305()#G3_C1280::Method6.15306<System.Object>()#G2_C326::Method7.7791<System.Object>()#"
		call void Generated808::M.G3_C1280.B<class G3_C1280`1<class BaseClass1>>(!!0,string)
		newobj instance void class G2_C326`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C326::ClassMethod1329.MI.7796()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C326::ClassMethod1331.MI.7797<System.Object>()#G2_C326::Method4.7788()#G2_C326::Method5.7789()#G1_C7::Method6.4813<System.Object>()#G2_C326::Method7.7791<System.Object>()#"
		call void Generated808::M.G1_C7.T.T<class BaseClass0,class BaseClass0,class G2_C326`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C326::ClassMethod1329.MI.7796()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C326::ClassMethod1331.MI.7797<System.Object>()#G2_C326::Method4.7788()#G2_C326::Method5.7789()#G1_C7::Method6.4813<System.Object>()#G2_C326::Method7.7791<System.Object>()#"
		call void Generated808::M.G1_C7.A.T<class BaseClass0,class G2_C326`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C326::ClassMethod1329.MI.7796()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C326::ClassMethod1331.MI.7797<System.Object>()#G2_C326::Method4.7788()#G2_C326::Method5.7789()#G1_C7::Method6.4813<System.Object>()#G2_C326::Method7.7791<System.Object>()#"
		call void Generated808::M.G1_C7.A.A<class G2_C326`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C326::Method4.7788()#G2_C326::Method5.7789()#G2_C326::Method6.7790<System.Object>()#"
		call void Generated808::M.IBase1.T<class BaseClass0,class G2_C326`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C326::Method4.7788()#G2_C326::Method5.7789()#G2_C326::Method6.7790<System.Object>()#"
		call void Generated808::M.IBase1.A<class G2_C326`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C326::Method7.MI.7792<System.Object>()#"
		call void Generated808::M.IBase2.T.T<class BaseClass1,class BaseClass0,class G2_C326`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C326::Method7.MI.7792<System.Object>()#"
		call void Generated808::M.IBase2.B.T<class BaseClass0,class G2_C326`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C326::Method7.MI.7792<System.Object>()#"
		call void Generated808::M.IBase2.B.A<class G2_C326`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C326::Method7.MI.7792<System.Object>()#"
		call void Generated808::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G2_C326`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C326::Method7.MI.7792<System.Object>()#"
		call void Generated808::M.IBase2.A.T<class BaseClass0,class G2_C326`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C326::Method7.MI.7792<System.Object>()#"
		call void Generated808::M.IBase2.A.A<class G2_C326`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C326::Method7.MI.7792<System.Object>()#"
		call void Generated808::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C326`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C326::Method7.MI.7792<System.Object>()#"
		call void Generated808::M.IBase2.A.T<class BaseClass1,class G2_C326`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C326::Method7.MI.7792<System.Object>()#"
		call void Generated808::M.IBase2.A.B<class G2_C326`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C326::Method7.MI.7792<System.Object>()#"
		call void Generated808::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G2_C326`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C326::Method7.MI.7792<System.Object>()#"
		call void Generated808::M.IBase2.B.T<class BaseClass1,class G2_C326`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C326::Method7.MI.7792<System.Object>()#"
		call void Generated808::M.IBase2.B.B<class G2_C326`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C326::ClassMethod1329.MI.7796()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C326::ClassMethod1331.MI.7797<System.Object>()#G2_C326::ClassMethod1990.7793()#G2_C326::ClassMethod1991.7794<System.Object>()#G2_C326::ClassMethod1992.7795<System.Object>()#G2_C326::Method4.7788()#G2_C326::Method5.7789()#G2_C326::Method6.7790<System.Object>()#G2_C326::Method7.7791<System.Object>()#"
		call void Generated808::M.G2_C326.T.T<class BaseClass0,class BaseClass0,class G2_C326`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C326::ClassMethod1329.MI.7796()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C326::ClassMethod1331.MI.7797<System.Object>()#G2_C326::ClassMethod1990.7793()#G2_C326::ClassMethod1991.7794<System.Object>()#G2_C326::ClassMethod1992.7795<System.Object>()#G2_C326::Method4.7788()#G2_C326::Method5.7789()#G2_C326::Method6.7790<System.Object>()#G2_C326::Method7.7791<System.Object>()#"
		call void Generated808::M.G2_C326.A.T<class BaseClass0,class G2_C326`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C326::ClassMethod1329.MI.7796()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C326::ClassMethod1331.MI.7797<System.Object>()#G2_C326::ClassMethod1990.7793()#G2_C326::ClassMethod1991.7794<System.Object>()#G2_C326::ClassMethod1992.7795<System.Object>()#G2_C326::Method4.7788()#G2_C326::Method5.7789()#G2_C326::Method6.7790<System.Object>()#G2_C326::Method7.7791<System.Object>()#"
		call void Generated808::M.G2_C326.A.A<class G2_C326`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C326::Method4.7788()#G2_C326::Method5.7789()#G2_C326::Method6.7790<System.Object>()#"
		call void Generated808::M.IBase1.T<class BaseClass1,class G2_C326`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C326::Method4.7788()#G2_C326::Method5.7789()#G2_C326::Method6.7790<System.Object>()#"
		call void Generated808::M.IBase1.B<class G2_C326`2<class BaseClass0,class BaseClass0>>(!!0,string)
		newobj instance void class G2_C326`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C326::ClassMethod1329.MI.7796()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C326::ClassMethod1331.MI.7797<System.Object>()#G2_C326::Method4.7788()#G2_C326::Method5.7789()#G1_C7::Method6.4813<System.Object>()#G2_C326::Method7.7791<System.Object>()#"
		call void Generated808::M.G1_C7.T.T<class BaseClass0,class BaseClass0,class G2_C326`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C326::ClassMethod1329.MI.7796()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C326::ClassMethod1331.MI.7797<System.Object>()#G2_C326::Method4.7788()#G2_C326::Method5.7789()#G1_C7::Method6.4813<System.Object>()#G2_C326::Method7.7791<System.Object>()#"
		call void Generated808::M.G1_C7.A.T<class BaseClass0,class G2_C326`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C326::ClassMethod1329.MI.7796()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C326::ClassMethod1331.MI.7797<System.Object>()#G2_C326::Method4.7788()#G2_C326::Method5.7789()#G1_C7::Method6.4813<System.Object>()#G2_C326::Method7.7791<System.Object>()#"
		call void Generated808::M.G1_C7.A.A<class G2_C326`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C326::Method4.7788()#G2_C326::Method5.7789()#G2_C326::Method6.7790<System.Object>()#"
		call void Generated808::M.IBase1.T<class BaseClass0,class G2_C326`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C326::Method4.7788()#G2_C326::Method5.7789()#G2_C326::Method6.7790<System.Object>()#"
		call void Generated808::M.IBase1.A<class G2_C326`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated808::M.IBase2.T.T<class BaseClass1,class BaseClass0,class G2_C326`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated808::M.IBase2.B.T<class BaseClass0,class G2_C326`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated808::M.IBase2.B.A<class G2_C326`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated808::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G2_C326`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated808::M.IBase2.A.T<class BaseClass0,class G2_C326`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated808::M.IBase2.A.A<class G2_C326`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C326::Method7.MI.7792<System.Object>()#"
		call void Generated808::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C326`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C326::Method7.MI.7792<System.Object>()#"
		call void Generated808::M.IBase2.A.T<class BaseClass1,class G2_C326`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C326::Method7.MI.7792<System.Object>()#"
		call void Generated808::M.IBase2.A.B<class G2_C326`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C326::Method7.MI.7792<System.Object>()#"
		call void Generated808::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G2_C326`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C326::Method7.MI.7792<System.Object>()#"
		call void Generated808::M.IBase2.B.T<class BaseClass1,class G2_C326`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C326::Method7.MI.7792<System.Object>()#"
		call void Generated808::M.IBase2.B.B<class G2_C326`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C326::ClassMethod1329.MI.7796()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C326::ClassMethod1331.MI.7797<System.Object>()#G2_C326::ClassMethod1990.7793()#G2_C326::ClassMethod1991.7794<System.Object>()#G2_C326::ClassMethod1992.7795<System.Object>()#G2_C326::Method4.7788()#G2_C326::Method5.7789()#G2_C326::Method6.7790<System.Object>()#G2_C326::Method7.7791<System.Object>()#"
		call void Generated808::M.G2_C326.T.T<class BaseClass0,class BaseClass1,class G2_C326`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C326::ClassMethod1329.MI.7796()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C326::ClassMethod1331.MI.7797<System.Object>()#G2_C326::ClassMethod1990.7793()#G2_C326::ClassMethod1991.7794<System.Object>()#G2_C326::ClassMethod1992.7795<System.Object>()#G2_C326::Method4.7788()#G2_C326::Method5.7789()#G2_C326::Method6.7790<System.Object>()#G2_C326::Method7.7791<System.Object>()#"
		call void Generated808::M.G2_C326.A.T<class BaseClass1,class G2_C326`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C326::ClassMethod1329.MI.7796()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C326::ClassMethod1331.MI.7797<System.Object>()#G2_C326::ClassMethod1990.7793()#G2_C326::ClassMethod1991.7794<System.Object>()#G2_C326::ClassMethod1992.7795<System.Object>()#G2_C326::Method4.7788()#G2_C326::Method5.7789()#G2_C326::Method6.7790<System.Object>()#G2_C326::Method7.7791<System.Object>()#"
		call void Generated808::M.G2_C326.A.B<class G2_C326`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C326::Method4.7788()#G2_C326::Method5.7789()#G2_C326::Method6.7790<System.Object>()#"
		call void Generated808::M.IBase1.T<class BaseClass1,class G2_C326`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C326::Method4.7788()#G2_C326::Method5.7789()#G2_C326::Method6.7790<System.Object>()#"
		call void Generated808::M.IBase1.B<class G2_C326`2<class BaseClass0,class BaseClass1>>(!!0,string)
		newobj instance void class G2_C326`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C326::ClassMethod1329.MI.7796()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C326::ClassMethod1331.MI.7797<System.Object>()#G2_C326::Method4.7788()#G2_C326::Method5.7789()#G1_C7::Method6.4813<System.Object>()#G2_C326::Method7.7791<System.Object>()#"
		call void Generated808::M.G1_C7.T.T<class BaseClass0,class BaseClass0,class G2_C326`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C326::ClassMethod1329.MI.7796()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C326::ClassMethod1331.MI.7797<System.Object>()#G2_C326::Method4.7788()#G2_C326::Method5.7789()#G1_C7::Method6.4813<System.Object>()#G2_C326::Method7.7791<System.Object>()#"
		call void Generated808::M.G1_C7.A.T<class BaseClass0,class G2_C326`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C326::ClassMethod1329.MI.7796()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C326::ClassMethod1331.MI.7797<System.Object>()#G2_C326::Method4.7788()#G2_C326::Method5.7789()#G1_C7::Method6.4813<System.Object>()#G2_C326::Method7.7791<System.Object>()#"
		call void Generated808::M.G1_C7.A.A<class G2_C326`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C326::Method4.7788()#G2_C326::Method5.7789()#G2_C326::Method6.7790<System.Object>()#"
		call void Generated808::M.IBase1.T<class BaseClass0,class G2_C326`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C326::Method4.7788()#G2_C326::Method5.7789()#G2_C326::Method6.7790<System.Object>()#"
		call void Generated808::M.IBase1.A<class G2_C326`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C326::Method7.MI.7792<System.Object>()#"
		call void Generated808::M.IBase2.T.T<class BaseClass1,class BaseClass0,class G2_C326`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C326::Method7.MI.7792<System.Object>()#"
		call void Generated808::M.IBase2.B.T<class BaseClass0,class G2_C326`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C326::Method7.MI.7792<System.Object>()#"
		call void Generated808::M.IBase2.B.A<class G2_C326`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C326::Method7.MI.7792<System.Object>()#"
		call void Generated808::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G2_C326`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C326::Method7.MI.7792<System.Object>()#"
		call void Generated808::M.IBase2.A.T<class BaseClass0,class G2_C326`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C326::Method7.MI.7792<System.Object>()#"
		call void Generated808::M.IBase2.A.A<class G2_C326`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C326::Method7.MI.7792<System.Object>()#"
		call void Generated808::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C326`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C326::Method7.MI.7792<System.Object>()#"
		call void Generated808::M.IBase2.A.T<class BaseClass1,class G2_C326`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C326::Method7.MI.7792<System.Object>()#"
		call void Generated808::M.IBase2.A.B<class G2_C326`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C326::Method7.MI.7792<System.Object>()#"
		call void Generated808::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G2_C326`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C326::Method7.MI.7792<System.Object>()#"
		call void Generated808::M.IBase2.B.T<class BaseClass1,class G2_C326`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C326::Method7.MI.7792<System.Object>()#"
		call void Generated808::M.IBase2.B.B<class G2_C326`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C326::ClassMethod1329.MI.7796()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C326::ClassMethod1331.MI.7797<System.Object>()#G2_C326::ClassMethod1990.7793()#G2_C326::ClassMethod1991.7794<System.Object>()#G2_C326::ClassMethod1992.7795<System.Object>()#G2_C326::Method4.7788()#G2_C326::Method5.7789()#G2_C326::Method6.7790<System.Object>()#G2_C326::Method7.7791<System.Object>()#"
		call void Generated808::M.G2_C326.T.T<class BaseClass1,class BaseClass0,class G2_C326`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C326::ClassMethod1329.MI.7796()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C326::ClassMethod1331.MI.7797<System.Object>()#G2_C326::ClassMethod1990.7793()#G2_C326::ClassMethod1991.7794<System.Object>()#G2_C326::ClassMethod1992.7795<System.Object>()#G2_C326::Method4.7788()#G2_C326::Method5.7789()#G2_C326::Method6.7790<System.Object>()#G2_C326::Method7.7791<System.Object>()#"
		call void Generated808::M.G2_C326.B.T<class BaseClass0,class G2_C326`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C326::ClassMethod1329.MI.7796()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C326::ClassMethod1331.MI.7797<System.Object>()#G2_C326::ClassMethod1990.7793()#G2_C326::ClassMethod1991.7794<System.Object>()#G2_C326::ClassMethod1992.7795<System.Object>()#G2_C326::Method4.7788()#G2_C326::Method5.7789()#G2_C326::Method6.7790<System.Object>()#G2_C326::Method7.7791<System.Object>()#"
		call void Generated808::M.G2_C326.B.A<class G2_C326`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C326::Method4.7788()#G2_C326::Method5.7789()#G2_C326::Method6.7790<System.Object>()#"
		call void Generated808::M.IBase1.T<class BaseClass1,class G2_C326`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C326::Method4.7788()#G2_C326::Method5.7789()#G2_C326::Method6.7790<System.Object>()#"
		call void Generated808::M.IBase1.B<class G2_C326`2<class BaseClass1,class BaseClass0>>(!!0,string)
		newobj instance void class G2_C326`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C326::ClassMethod1329.MI.7796()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C326::ClassMethod1331.MI.7797<System.Object>()#G2_C326::Method4.7788()#G2_C326::Method5.7789()#G1_C7::Method6.4813<System.Object>()#G2_C326::Method7.7791<System.Object>()#"
		call void Generated808::M.G1_C7.T.T<class BaseClass0,class BaseClass0,class G2_C326`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C326::ClassMethod1329.MI.7796()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C326::ClassMethod1331.MI.7797<System.Object>()#G2_C326::Method4.7788()#G2_C326::Method5.7789()#G1_C7::Method6.4813<System.Object>()#G2_C326::Method7.7791<System.Object>()#"
		call void Generated808::M.G1_C7.A.T<class BaseClass0,class G2_C326`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C326::ClassMethod1329.MI.7796()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C326::ClassMethod1331.MI.7797<System.Object>()#G2_C326::Method4.7788()#G2_C326::Method5.7789()#G1_C7::Method6.4813<System.Object>()#G2_C326::Method7.7791<System.Object>()#"
		call void Generated808::M.G1_C7.A.A<class G2_C326`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C326::Method4.7788()#G2_C326::Method5.7789()#G2_C326::Method6.7790<System.Object>()#"
		call void Generated808::M.IBase1.T<class BaseClass0,class G2_C326`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C326::Method4.7788()#G2_C326::Method5.7789()#G2_C326::Method6.7790<System.Object>()#"
		call void Generated808::M.IBase1.A<class G2_C326`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated808::M.IBase2.T.T<class BaseClass1,class BaseClass0,class G2_C326`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated808::M.IBase2.B.T<class BaseClass0,class G2_C326`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated808::M.IBase2.B.A<class G2_C326`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated808::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G2_C326`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated808::M.IBase2.A.T<class BaseClass0,class G2_C326`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::Method7.MI.4816<System.Object>()#"
		call void Generated808::M.IBase2.A.A<class G2_C326`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C326::Method7.MI.7792<System.Object>()#"
		call void Generated808::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C326`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C326::Method7.MI.7792<System.Object>()#"
		call void Generated808::M.IBase2.A.T<class BaseClass1,class G2_C326`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C326::Method7.MI.7792<System.Object>()#"
		call void Generated808::M.IBase2.A.B<class G2_C326`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C326::Method7.MI.7792<System.Object>()#"
		call void Generated808::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G2_C326`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C326::Method7.MI.7792<System.Object>()#"
		call void Generated808::M.IBase2.B.T<class BaseClass1,class G2_C326`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C326::Method7.MI.7792<System.Object>()#"
		call void Generated808::M.IBase2.B.B<class G2_C326`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C326::ClassMethod1329.MI.7796()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C326::ClassMethod1331.MI.7797<System.Object>()#G2_C326::ClassMethod1990.7793()#G2_C326::ClassMethod1991.7794<System.Object>()#G2_C326::ClassMethod1992.7795<System.Object>()#G2_C326::Method4.7788()#G2_C326::Method5.7789()#G2_C326::Method6.7790<System.Object>()#G2_C326::Method7.7791<System.Object>()#"
		call void Generated808::M.G2_C326.T.T<class BaseClass1,class BaseClass1,class G2_C326`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C326::ClassMethod1329.MI.7796()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C326::ClassMethod1331.MI.7797<System.Object>()#G2_C326::ClassMethod1990.7793()#G2_C326::ClassMethod1991.7794<System.Object>()#G2_C326::ClassMethod1992.7795<System.Object>()#G2_C326::Method4.7788()#G2_C326::Method5.7789()#G2_C326::Method6.7790<System.Object>()#G2_C326::Method7.7791<System.Object>()#"
		call void Generated808::M.G2_C326.B.T<class BaseClass1,class G2_C326`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C7::ClassMethod1328.4817()#G2_C326::ClassMethod1329.MI.7796()#G1_C7::ClassMethod1330.4819<System.Object>()#G2_C326::ClassMethod1331.MI.7797<System.Object>()#G2_C326::ClassMethod1990.7793()#G2_C326::ClassMethod1991.7794<System.Object>()#G2_C326::ClassMethod1992.7795<System.Object>()#G2_C326::Method4.7788()#G2_C326::Method5.7789()#G2_C326::Method6.7790<System.Object>()#G2_C326::Method7.7791<System.Object>()#"
		call void Generated808::M.G2_C326.B.B<class G2_C326`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C326::Method4.7788()#G2_C326::Method5.7789()#G2_C326::Method6.7790<System.Object>()#"
		call void Generated808::M.IBase1.T<class BaseClass1,class G2_C326`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C326::Method4.7788()#G2_C326::Method5.7789()#G2_C326::Method6.7790<System.Object>()#"
		call void Generated808::M.IBase1.B<class G2_C326`2<class BaseClass1,class BaseClass1>>(!!0,string)
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
		newobj instance void class G3_C1280`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass0,class BaseClass0>::ClassMethod1331<object>()
		calli default string(class G3_C1280`1<class BaseClass0>)
		ldstr "G2_C326::ClassMethod1331.MI.7797<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G3_C1280`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass0,class BaseClass0>::ClassMethod1330<object>()
		calli default string(class G3_C1280`1<class BaseClass0>)
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G3_C1280`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass0,class BaseClass0>::ClassMethod1329()
		calli default string(class G3_C1280`1<class BaseClass0>)
		ldstr "G2_C326::ClassMethod1329.MI.7796()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G3_C1280`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass0,class BaseClass0>::ClassMethod1328()
		calli default string(class G3_C1280`1<class BaseClass0>)
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G3_C1280`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1280`1<class BaseClass0>)
		ldstr "G2_C326::Method7.7791<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G3_C1280`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass0,class BaseClass0>::Method6<object>()
		calli default string(class G3_C1280`1<class BaseClass0>)
		ldstr "G1_C7::Method6.4813<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G3_C1280`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass0,class BaseClass0>::Method5()
		calli default string(class G3_C1280`1<class BaseClass0>)
		ldstr "G3_C1280::Method5.15305()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G3_C1280`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass0,class BaseClass0>::Method4()
		calli default string(class G3_C1280`1<class BaseClass0>)
		ldstr "G2_C326::Method4.7788()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G3_C1280`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G3_C1280`1<class BaseClass0>)
		ldstr "G3_C1280::Method4.15304()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1280`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G3_C1280`1<class BaseClass0>)
		ldstr "G3_C1280::Method5.15305()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1280`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1280`1<class BaseClass0>)
		ldstr "G3_C1280::Method6.MI.15307<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1280`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1280`1<class BaseClass0>)
		ldstr "G1_C7::Method7.MI.4816<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G3_C1280`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1280`1<class BaseClass0>)
		ldstr "G1_C7::Method7.MI.4816<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1280`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1280`1<class BaseClass0>)
		ldstr "G2_C326::Method7.MI.7792<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1280`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1280`1<class BaseClass0>)
		ldstr "G2_C326::Method7.MI.7792<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1280`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C326`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C326`2<class BaseClass0,class BaseClass1>::ClassMethod1992<object>()
		calli default string(class G3_C1280`1<class BaseClass0>)
		ldstr "G3_C1280::ClassMethod1992.MI.15313<System.Object>()"
		ldstr "class G2_C326`2<class BaseClass0,class BaseClass1> on type class G3_C1280`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C326`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C326`2<class BaseClass0,class BaseClass1>::ClassMethod1991<object>()
		calli default string(class G3_C1280`1<class BaseClass0>)
		ldstr "G2_C326::ClassMethod1991.7794<System.Object>()"
		ldstr "class G2_C326`2<class BaseClass0,class BaseClass1> on type class G3_C1280`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C326`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C326`2<class BaseClass0,class BaseClass1>::ClassMethod1990()
		calli default string(class G3_C1280`1<class BaseClass0>)
		ldstr "G3_C1280::ClassMethod1990.MI.15312()"
		ldstr "class G2_C326`2<class BaseClass0,class BaseClass1> on type class G3_C1280`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C326`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C326`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1280`1<class BaseClass0>)
		ldstr "G2_C326::Method7.7791<System.Object>()"
		ldstr "class G2_C326`2<class BaseClass0,class BaseClass1> on type class G3_C1280`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C326`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C326`2<class BaseClass0,class BaseClass1>::Method6<object>()
		calli default string(class G3_C1280`1<class BaseClass0>)
		ldstr "G3_C1280::Method6.15306<System.Object>()"
		ldstr "class G2_C326`2<class BaseClass0,class BaseClass1> on type class G3_C1280`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C326`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C326`2<class BaseClass0,class BaseClass1>::Method5()
		calli default string(class G3_C1280`1<class BaseClass0>)
		ldstr "G3_C1280::Method5.15305()"
		ldstr "class G2_C326`2<class BaseClass0,class BaseClass1> on type class G3_C1280`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C326`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C326`2<class BaseClass0,class BaseClass1>::Method4()
		calli default string(class G3_C1280`1<class BaseClass0>)
		ldstr "G2_C326::Method4.7788()"
		ldstr "class G2_C326`2<class BaseClass0,class BaseClass1> on type class G3_C1280`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C326`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C326`2<class BaseClass0,class BaseClass1>::ClassMethod1331<object>()
		calli default string(class G3_C1280`1<class BaseClass0>)
		ldstr "G2_C326::ClassMethod1331.MI.7797<System.Object>()"
		ldstr "class G2_C326`2<class BaseClass0,class BaseClass1> on type class G3_C1280`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C326`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C326`2<class BaseClass0,class BaseClass1>::ClassMethod1330<object>()
		calli default string(class G3_C1280`1<class BaseClass0>)
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G2_C326`2<class BaseClass0,class BaseClass1> on type class G3_C1280`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C326`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C326`2<class BaseClass0,class BaseClass1>::ClassMethod1329()
		calli default string(class G3_C1280`1<class BaseClass0>)
		ldstr "G2_C326::ClassMethod1329.MI.7796()"
		ldstr "class G2_C326`2<class BaseClass0,class BaseClass1> on type class G3_C1280`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C326`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C326`2<class BaseClass0,class BaseClass1>::ClassMethod1328()
		calli default string(class G3_C1280`1<class BaseClass0>)
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G2_C326`2<class BaseClass0,class BaseClass1> on type class G3_C1280`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G3_C1280`1<class BaseClass0>)
		ldstr "G2_C326::Method4.7788()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1280`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G3_C1280`1<class BaseClass0>)
		ldstr "G3_C1280::Method5.15305()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1280`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G3_C1280`1<class BaseClass0>)
		ldstr "G3_C1280::Method6.15306<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1280`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1280`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1280`1<class BaseClass0>::ClassMethod3937<object>()
		calli default string(class G3_C1280`1<class BaseClass0>)
		ldstr "G3_C1280::ClassMethod3937.15311<System.Object>()"
		ldstr "class G3_C1280`1<class BaseClass0> on type class G3_C1280`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1280`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1280`1<class BaseClass0>::ClassMethod3936<object>()
		calli default string(class G3_C1280`1<class BaseClass0>)
		ldstr "G3_C1280::ClassMethod3936.15310<System.Object>()"
		ldstr "class G3_C1280`1<class BaseClass0> on type class G3_C1280`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1280`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1280`1<class BaseClass0>::ClassMethod3935()
		calli default string(class G3_C1280`1<class BaseClass0>)
		ldstr "G3_C1280::ClassMethod3935.15309()"
		ldstr "class G3_C1280`1<class BaseClass0> on type class G3_C1280`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1280`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1280`1<class BaseClass0>::ClassMethod3934()
		calli default string(class G3_C1280`1<class BaseClass0>)
		ldstr "G3_C1280::ClassMethod3934.15308()"
		ldstr "class G3_C1280`1<class BaseClass0> on type class G3_C1280`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1280`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1280`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1280`1<class BaseClass0>)
		ldstr "G3_C1280::Method6.15306<System.Object>()"
		ldstr "class G3_C1280`1<class BaseClass0> on type class G3_C1280`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1280`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1280`1<class BaseClass0>::Method5()
		calli default string(class G3_C1280`1<class BaseClass0>)
		ldstr "G3_C1280::Method5.15305()"
		ldstr "class G3_C1280`1<class BaseClass0> on type class G3_C1280`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1280`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1280`1<class BaseClass0>::Method4()
		calli default string(class G3_C1280`1<class BaseClass0>)
		ldstr "G3_C1280::Method4.15304()"
		ldstr "class G3_C1280`1<class BaseClass0> on type class G3_C1280`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1280`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1280`1<class BaseClass0>::ClassMethod1992<object>()
		calli default string(class G3_C1280`1<class BaseClass0>)
		ldstr "G3_C1280::ClassMethod1992.MI.15313<System.Object>()"
		ldstr "class G3_C1280`1<class BaseClass0> on type class G3_C1280`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1280`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1280`1<class BaseClass0>::ClassMethod1991<object>()
		calli default string(class G3_C1280`1<class BaseClass0>)
		ldstr "G2_C326::ClassMethod1991.7794<System.Object>()"
		ldstr "class G3_C1280`1<class BaseClass0> on type class G3_C1280`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1280`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1280`1<class BaseClass0>::ClassMethod1990()
		calli default string(class G3_C1280`1<class BaseClass0>)
		ldstr "G3_C1280::ClassMethod1990.MI.15312()"
		ldstr "class G3_C1280`1<class BaseClass0> on type class G3_C1280`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1280`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1280`1<class BaseClass0>::Method7<object>()
		calli default string(class G3_C1280`1<class BaseClass0>)
		ldstr "G2_C326::Method7.7791<System.Object>()"
		ldstr "class G3_C1280`1<class BaseClass0> on type class G3_C1280`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1280`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1280`1<class BaseClass0>::ClassMethod1331<object>()
		calli default string(class G3_C1280`1<class BaseClass0>)
		ldstr "G2_C326::ClassMethod1331.MI.7797<System.Object>()"
		ldstr "class G3_C1280`1<class BaseClass0> on type class G3_C1280`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1280`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1280`1<class BaseClass0>::ClassMethod1330<object>()
		calli default string(class G3_C1280`1<class BaseClass0>)
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G3_C1280`1<class BaseClass0> on type class G3_C1280`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1280`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1280`1<class BaseClass0>::ClassMethod1329()
		calli default string(class G3_C1280`1<class BaseClass0>)
		ldstr "G2_C326::ClassMethod1329.MI.7796()"
		ldstr "class G3_C1280`1<class BaseClass0> on type class G3_C1280`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1280`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1280`1<class BaseClass0>::ClassMethod1328()
		calli default string(class G3_C1280`1<class BaseClass0>)
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G3_C1280`1<class BaseClass0> on type class G3_C1280`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G3_C1280`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass0,class BaseClass0>::ClassMethod1331<object>()
		calli default string(class G3_C1280`1<class BaseClass1>)
		ldstr "G2_C326::ClassMethod1331.MI.7797<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G3_C1280`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass0,class BaseClass0>::ClassMethod1330<object>()
		calli default string(class G3_C1280`1<class BaseClass1>)
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G3_C1280`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass0,class BaseClass0>::ClassMethod1329()
		calli default string(class G3_C1280`1<class BaseClass1>)
		ldstr "G2_C326::ClassMethod1329.MI.7796()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G3_C1280`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass0,class BaseClass0>::ClassMethod1328()
		calli default string(class G3_C1280`1<class BaseClass1>)
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G3_C1280`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1280`1<class BaseClass1>)
		ldstr "G2_C326::Method7.7791<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G3_C1280`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass0,class BaseClass0>::Method6<object>()
		calli default string(class G3_C1280`1<class BaseClass1>)
		ldstr "G1_C7::Method6.4813<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G3_C1280`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass0,class BaseClass0>::Method5()
		calli default string(class G3_C1280`1<class BaseClass1>)
		ldstr "G3_C1280::Method5.15305()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G3_C1280`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass0,class BaseClass0>::Method4()
		calli default string(class G3_C1280`1<class BaseClass1>)
		ldstr "G2_C326::Method4.7788()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G3_C1280`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G3_C1280`1<class BaseClass1>)
		ldstr "G3_C1280::Method4.15304()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1280`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G3_C1280`1<class BaseClass1>)
		ldstr "G3_C1280::Method5.15305()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1280`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1280`1<class BaseClass1>)
		ldstr "G3_C1280::Method6.MI.15307<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1280`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1280`1<class BaseClass1>)
		ldstr "G1_C7::Method7.MI.4816<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G3_C1280`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1280`1<class BaseClass1>)
		ldstr "G1_C7::Method7.MI.4816<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1280`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1280`1<class BaseClass1>)
		ldstr "G2_C326::Method7.MI.7792<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1280`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1280`1<class BaseClass1>)
		ldstr "G2_C326::Method7.MI.7792<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1280`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C326`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C326`2<class BaseClass0,class BaseClass1>::ClassMethod1992<object>()
		calli default string(class G3_C1280`1<class BaseClass1>)
		ldstr "G3_C1280::ClassMethod1992.MI.15313<System.Object>()"
		ldstr "class G2_C326`2<class BaseClass0,class BaseClass1> on type class G3_C1280`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C326`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C326`2<class BaseClass0,class BaseClass1>::ClassMethod1991<object>()
		calli default string(class G3_C1280`1<class BaseClass1>)
		ldstr "G2_C326::ClassMethod1991.7794<System.Object>()"
		ldstr "class G2_C326`2<class BaseClass0,class BaseClass1> on type class G3_C1280`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C326`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C326`2<class BaseClass0,class BaseClass1>::ClassMethod1990()
		calli default string(class G3_C1280`1<class BaseClass1>)
		ldstr "G3_C1280::ClassMethod1990.MI.15312()"
		ldstr "class G2_C326`2<class BaseClass0,class BaseClass1> on type class G3_C1280`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C326`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C326`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1280`1<class BaseClass1>)
		ldstr "G2_C326::Method7.7791<System.Object>()"
		ldstr "class G2_C326`2<class BaseClass0,class BaseClass1> on type class G3_C1280`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C326`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C326`2<class BaseClass0,class BaseClass1>::Method6<object>()
		calli default string(class G3_C1280`1<class BaseClass1>)
		ldstr "G3_C1280::Method6.15306<System.Object>()"
		ldstr "class G2_C326`2<class BaseClass0,class BaseClass1> on type class G3_C1280`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C326`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C326`2<class BaseClass0,class BaseClass1>::Method5()
		calli default string(class G3_C1280`1<class BaseClass1>)
		ldstr "G3_C1280::Method5.15305()"
		ldstr "class G2_C326`2<class BaseClass0,class BaseClass1> on type class G3_C1280`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C326`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C326`2<class BaseClass0,class BaseClass1>::Method4()
		calli default string(class G3_C1280`1<class BaseClass1>)
		ldstr "G2_C326::Method4.7788()"
		ldstr "class G2_C326`2<class BaseClass0,class BaseClass1> on type class G3_C1280`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C326`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C326`2<class BaseClass0,class BaseClass1>::ClassMethod1331<object>()
		calli default string(class G3_C1280`1<class BaseClass1>)
		ldstr "G2_C326::ClassMethod1331.MI.7797<System.Object>()"
		ldstr "class G2_C326`2<class BaseClass0,class BaseClass1> on type class G3_C1280`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C326`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C326`2<class BaseClass0,class BaseClass1>::ClassMethod1330<object>()
		calli default string(class G3_C1280`1<class BaseClass1>)
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G2_C326`2<class BaseClass0,class BaseClass1> on type class G3_C1280`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C326`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C326`2<class BaseClass0,class BaseClass1>::ClassMethod1329()
		calli default string(class G3_C1280`1<class BaseClass1>)
		ldstr "G2_C326::ClassMethod1329.MI.7796()"
		ldstr "class G2_C326`2<class BaseClass0,class BaseClass1> on type class G3_C1280`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C326`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C326`2<class BaseClass0,class BaseClass1>::ClassMethod1328()
		calli default string(class G3_C1280`1<class BaseClass1>)
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G2_C326`2<class BaseClass0,class BaseClass1> on type class G3_C1280`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G3_C1280`1<class BaseClass1>)
		ldstr "G3_C1280::Method4.15304()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1280`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G3_C1280`1<class BaseClass1>)
		ldstr "G3_C1280::Method5.15305()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1280`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G3_C1280`1<class BaseClass1>)
		ldstr "G3_C1280::Method6.MI.15307<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1280`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1280`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1280`1<class BaseClass1>::ClassMethod3937<object>()
		calli default string(class G3_C1280`1<class BaseClass1>)
		ldstr "G3_C1280::ClassMethod3937.15311<System.Object>()"
		ldstr "class G3_C1280`1<class BaseClass1> on type class G3_C1280`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1280`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1280`1<class BaseClass1>::ClassMethod3936<object>()
		calli default string(class G3_C1280`1<class BaseClass1>)
		ldstr "G3_C1280::ClassMethod3936.15310<System.Object>()"
		ldstr "class G3_C1280`1<class BaseClass1> on type class G3_C1280`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1280`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1280`1<class BaseClass1>::ClassMethod3935()
		calli default string(class G3_C1280`1<class BaseClass1>)
		ldstr "G3_C1280::ClassMethod3935.15309()"
		ldstr "class G3_C1280`1<class BaseClass1> on type class G3_C1280`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1280`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1280`1<class BaseClass1>::ClassMethod3934()
		calli default string(class G3_C1280`1<class BaseClass1>)
		ldstr "G3_C1280::ClassMethod3934.15308()"
		ldstr "class G3_C1280`1<class BaseClass1> on type class G3_C1280`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1280`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1280`1<class BaseClass1>::Method6<object>()
		calli default string(class G3_C1280`1<class BaseClass1>)
		ldstr "G3_C1280::Method6.15306<System.Object>()"
		ldstr "class G3_C1280`1<class BaseClass1> on type class G3_C1280`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1280`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1280`1<class BaseClass1>::Method5()
		calli default string(class G3_C1280`1<class BaseClass1>)
		ldstr "G3_C1280::Method5.15305()"
		ldstr "class G3_C1280`1<class BaseClass1> on type class G3_C1280`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1280`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1280`1<class BaseClass1>::Method4()
		calli default string(class G3_C1280`1<class BaseClass1>)
		ldstr "G3_C1280::Method4.15304()"
		ldstr "class G3_C1280`1<class BaseClass1> on type class G3_C1280`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1280`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1280`1<class BaseClass1>::ClassMethod1992<object>()
		calli default string(class G3_C1280`1<class BaseClass1>)
		ldstr "G3_C1280::ClassMethod1992.MI.15313<System.Object>()"
		ldstr "class G3_C1280`1<class BaseClass1> on type class G3_C1280`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1280`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1280`1<class BaseClass1>::ClassMethod1991<object>()
		calli default string(class G3_C1280`1<class BaseClass1>)
		ldstr "G2_C326::ClassMethod1991.7794<System.Object>()"
		ldstr "class G3_C1280`1<class BaseClass1> on type class G3_C1280`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1280`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1280`1<class BaseClass1>::ClassMethod1990()
		calli default string(class G3_C1280`1<class BaseClass1>)
		ldstr "G3_C1280::ClassMethod1990.MI.15312()"
		ldstr "class G3_C1280`1<class BaseClass1> on type class G3_C1280`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1280`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1280`1<class BaseClass1>::Method7<object>()
		calli default string(class G3_C1280`1<class BaseClass1>)
		ldstr "G2_C326::Method7.7791<System.Object>()"
		ldstr "class G3_C1280`1<class BaseClass1> on type class G3_C1280`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1280`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1280`1<class BaseClass1>::ClassMethod1331<object>()
		calli default string(class G3_C1280`1<class BaseClass1>)
		ldstr "G2_C326::ClassMethod1331.MI.7797<System.Object>()"
		ldstr "class G3_C1280`1<class BaseClass1> on type class G3_C1280`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1280`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1280`1<class BaseClass1>::ClassMethod1330<object>()
		calli default string(class G3_C1280`1<class BaseClass1>)
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G3_C1280`1<class BaseClass1> on type class G3_C1280`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1280`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1280`1<class BaseClass1>::ClassMethod1329()
		calli default string(class G3_C1280`1<class BaseClass1>)
		ldstr "G2_C326::ClassMethod1329.MI.7796()"
		ldstr "class G3_C1280`1<class BaseClass1> on type class G3_C1280`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1280`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1280`1<class BaseClass1>::ClassMethod1328()
		calli default string(class G3_C1280`1<class BaseClass1>)
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G3_C1280`1<class BaseClass1> on type class G3_C1280`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C326`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass0,class BaseClass0>::ClassMethod1331<object>()
		calli default string(class G2_C326`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C326::ClassMethod1331.MI.7797<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass0,class BaseClass0>::ClassMethod1330<object>()
		calli default string(class G2_C326`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass0,class BaseClass0>::ClassMethod1329()
		calli default string(class G2_C326`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C326::ClassMethod1329.MI.7796()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass0,class BaseClass0>::ClassMethod1328()
		calli default string(class G2_C326`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C326`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C326::Method7.7791<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass0,class BaseClass0>::Method6<object>()
		calli default string(class G2_C326`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C7::Method6.4813<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass0,class BaseClass0>::Method5()
		calli default string(class G2_C326`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C326::Method5.7789()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass0,class BaseClass0>::Method4()
		calli default string(class G2_C326`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C326::Method4.7788()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G2_C326`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C326::Method4.7788()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C326`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G2_C326`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C326::Method5.7789()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C326`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G2_C326`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C326::Method6.7790<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C326`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G2_C326`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C326::Method7.MI.7792<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G2_C326`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C326`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C326::Method7.MI.7792<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C326`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C326::Method7.MI.7792<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C326`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C326`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C326::Method7.MI.7792<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C326`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C326`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C326`2<class BaseClass0,class BaseClass0>::ClassMethod1992<object>()
		calli default string(class G2_C326`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C326::ClassMethod1992.7795<System.Object>()"
		ldstr "class G2_C326`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C326`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C326`2<class BaseClass0,class BaseClass0>::ClassMethod1991<object>()
		calli default string(class G2_C326`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C326::ClassMethod1991.7794<System.Object>()"
		ldstr "class G2_C326`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C326`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C326`2<class BaseClass0,class BaseClass0>::ClassMethod1990()
		calli default string(class G2_C326`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C326::ClassMethod1990.7793()"
		ldstr "class G2_C326`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C326`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C326`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C326`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C326::Method7.7791<System.Object>()"
		ldstr "class G2_C326`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C326`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C326`2<class BaseClass0,class BaseClass0>::Method6<object>()
		calli default string(class G2_C326`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C326::Method6.7790<System.Object>()"
		ldstr "class G2_C326`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C326`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C326`2<class BaseClass0,class BaseClass0>::Method5()
		calli default string(class G2_C326`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C326::Method5.7789()"
		ldstr "class G2_C326`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C326`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C326`2<class BaseClass0,class BaseClass0>::Method4()
		calli default string(class G2_C326`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C326::Method4.7788()"
		ldstr "class G2_C326`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C326`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C326`2<class BaseClass0,class BaseClass0>::ClassMethod1331<object>()
		calli default string(class G2_C326`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C326::ClassMethod1331.MI.7797<System.Object>()"
		ldstr "class G2_C326`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C326`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C326`2<class BaseClass0,class BaseClass0>::ClassMethod1330<object>()
		calli default string(class G2_C326`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G2_C326`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C326`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C326`2<class BaseClass0,class BaseClass0>::ClassMethod1329()
		calli default string(class G2_C326`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C326::ClassMethod1329.MI.7796()"
		ldstr "class G2_C326`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C326`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C326`2<class BaseClass0,class BaseClass0>::ClassMethod1328()
		calli default string(class G2_C326`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G2_C326`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G2_C326`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C326::Method4.7788()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C326`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G2_C326`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C326::Method5.7789()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C326`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G2_C326`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C326::Method6.7790<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C326`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C326`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass0,class BaseClass0>::ClassMethod1331<object>()
		calli default string(class G2_C326`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C326::ClassMethod1331.MI.7797<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass0,class BaseClass0>::ClassMethod1330<object>()
		calli default string(class G2_C326`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass0,class BaseClass0>::ClassMethod1329()
		calli default string(class G2_C326`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C326::ClassMethod1329.MI.7796()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass0,class BaseClass0>::ClassMethod1328()
		calli default string(class G2_C326`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C326`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C326::Method7.7791<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass0,class BaseClass0>::Method6<object>()
		calli default string(class G2_C326`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C7::Method6.4813<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass0,class BaseClass0>::Method5()
		calli default string(class G2_C326`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C326::Method5.7789()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass0,class BaseClass0>::Method4()
		calli default string(class G2_C326`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C326::Method4.7788()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G2_C326`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C326::Method4.7788()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C326`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G2_C326`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C326::Method5.7789()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C326`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G2_C326`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C326::Method6.7790<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C326`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G2_C326`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C7::Method7.MI.4816<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G2_C326`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C326`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C7::Method7.MI.4816<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C326`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C326::Method7.MI.7792<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C326`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C326`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C326::Method7.MI.7792<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C326`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C326`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C326`2<class BaseClass0,class BaseClass1>::ClassMethod1992<object>()
		calli default string(class G2_C326`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C326::ClassMethod1992.7795<System.Object>()"
		ldstr "class G2_C326`2<class BaseClass0,class BaseClass1> on type class G2_C326`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C326`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C326`2<class BaseClass0,class BaseClass1>::ClassMethod1991<object>()
		calli default string(class G2_C326`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C326::ClassMethod1991.7794<System.Object>()"
		ldstr "class G2_C326`2<class BaseClass0,class BaseClass1> on type class G2_C326`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C326`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C326`2<class BaseClass0,class BaseClass1>::ClassMethod1990()
		calli default string(class G2_C326`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C326::ClassMethod1990.7793()"
		ldstr "class G2_C326`2<class BaseClass0,class BaseClass1> on type class G2_C326`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C326`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C326`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C326`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C326::Method7.7791<System.Object>()"
		ldstr "class G2_C326`2<class BaseClass0,class BaseClass1> on type class G2_C326`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C326`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C326`2<class BaseClass0,class BaseClass1>::Method6<object>()
		calli default string(class G2_C326`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C326::Method6.7790<System.Object>()"
		ldstr "class G2_C326`2<class BaseClass0,class BaseClass1> on type class G2_C326`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C326`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C326`2<class BaseClass0,class BaseClass1>::Method5()
		calli default string(class G2_C326`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C326::Method5.7789()"
		ldstr "class G2_C326`2<class BaseClass0,class BaseClass1> on type class G2_C326`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C326`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C326`2<class BaseClass0,class BaseClass1>::Method4()
		calli default string(class G2_C326`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C326::Method4.7788()"
		ldstr "class G2_C326`2<class BaseClass0,class BaseClass1> on type class G2_C326`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C326`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C326`2<class BaseClass0,class BaseClass1>::ClassMethod1331<object>()
		calli default string(class G2_C326`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C326::ClassMethod1331.MI.7797<System.Object>()"
		ldstr "class G2_C326`2<class BaseClass0,class BaseClass1> on type class G2_C326`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C326`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C326`2<class BaseClass0,class BaseClass1>::ClassMethod1330<object>()
		calli default string(class G2_C326`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G2_C326`2<class BaseClass0,class BaseClass1> on type class G2_C326`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C326`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C326`2<class BaseClass0,class BaseClass1>::ClassMethod1329()
		calli default string(class G2_C326`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C326::ClassMethod1329.MI.7796()"
		ldstr "class G2_C326`2<class BaseClass0,class BaseClass1> on type class G2_C326`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C326`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C326`2<class BaseClass0,class BaseClass1>::ClassMethod1328()
		calli default string(class G2_C326`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G2_C326`2<class BaseClass0,class BaseClass1> on type class G2_C326`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G2_C326`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C326::Method4.7788()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C326`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G2_C326`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C326::Method5.7789()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C326`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G2_C326`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C326::Method6.7790<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C326`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C326`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass0,class BaseClass0>::ClassMethod1331<object>()
		calli default string(class G2_C326`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C326::ClassMethod1331.MI.7797<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass0,class BaseClass0>::ClassMethod1330<object>()
		calli default string(class G2_C326`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass0,class BaseClass0>::ClassMethod1329()
		calli default string(class G2_C326`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C326::ClassMethod1329.MI.7796()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass0,class BaseClass0>::ClassMethod1328()
		calli default string(class G2_C326`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C326`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C326::Method7.7791<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass0,class BaseClass0>::Method6<object>()
		calli default string(class G2_C326`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C7::Method6.4813<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass0,class BaseClass0>::Method5()
		calli default string(class G2_C326`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C326::Method5.7789()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass0,class BaseClass0>::Method4()
		calli default string(class G2_C326`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C326::Method4.7788()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G2_C326`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C326::Method4.7788()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C326`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G2_C326`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C326::Method5.7789()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C326`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G2_C326`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C326::Method6.7790<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C326`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G2_C326`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C326::Method7.MI.7792<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G2_C326`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C326`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C326::Method7.MI.7792<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C326`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C326::Method7.MI.7792<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C326`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C326`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C326::Method7.MI.7792<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C326`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C326`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C326`2<class BaseClass1,class BaseClass0>::ClassMethod1992<object>()
		calli default string(class G2_C326`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C326::ClassMethod1992.7795<System.Object>()"
		ldstr "class G2_C326`2<class BaseClass1,class BaseClass0> on type class G2_C326`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C326`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C326`2<class BaseClass1,class BaseClass0>::ClassMethod1991<object>()
		calli default string(class G2_C326`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C326::ClassMethod1991.7794<System.Object>()"
		ldstr "class G2_C326`2<class BaseClass1,class BaseClass0> on type class G2_C326`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C326`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C326`2<class BaseClass1,class BaseClass0>::ClassMethod1990()
		calli default string(class G2_C326`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C326::ClassMethod1990.7793()"
		ldstr "class G2_C326`2<class BaseClass1,class BaseClass0> on type class G2_C326`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C326`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C326`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G2_C326`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C326::Method7.7791<System.Object>()"
		ldstr "class G2_C326`2<class BaseClass1,class BaseClass0> on type class G2_C326`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C326`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C326`2<class BaseClass1,class BaseClass0>::Method6<object>()
		calli default string(class G2_C326`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C326::Method6.7790<System.Object>()"
		ldstr "class G2_C326`2<class BaseClass1,class BaseClass0> on type class G2_C326`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C326`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C326`2<class BaseClass1,class BaseClass0>::Method5()
		calli default string(class G2_C326`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C326::Method5.7789()"
		ldstr "class G2_C326`2<class BaseClass1,class BaseClass0> on type class G2_C326`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C326`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C326`2<class BaseClass1,class BaseClass0>::Method4()
		calli default string(class G2_C326`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C326::Method4.7788()"
		ldstr "class G2_C326`2<class BaseClass1,class BaseClass0> on type class G2_C326`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C326`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C326`2<class BaseClass1,class BaseClass0>::ClassMethod1331<object>()
		calli default string(class G2_C326`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C326::ClassMethod1331.MI.7797<System.Object>()"
		ldstr "class G2_C326`2<class BaseClass1,class BaseClass0> on type class G2_C326`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C326`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C326`2<class BaseClass1,class BaseClass0>::ClassMethod1330<object>()
		calli default string(class G2_C326`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G2_C326`2<class BaseClass1,class BaseClass0> on type class G2_C326`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C326`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C326`2<class BaseClass1,class BaseClass0>::ClassMethod1329()
		calli default string(class G2_C326`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C326::ClassMethod1329.MI.7796()"
		ldstr "class G2_C326`2<class BaseClass1,class BaseClass0> on type class G2_C326`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C326`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C326`2<class BaseClass1,class BaseClass0>::ClassMethod1328()
		calli default string(class G2_C326`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G2_C326`2<class BaseClass1,class BaseClass0> on type class G2_C326`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G2_C326`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C326::Method4.7788()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C326`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G2_C326`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C326::Method5.7789()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C326`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G2_C326`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C326::Method6.7790<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C326`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C326`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass0,class BaseClass0>::ClassMethod1331<object>()
		calli default string(class G2_C326`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C326::ClassMethod1331.MI.7797<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass0,class BaseClass0>::ClassMethod1330<object>()
		calli default string(class G2_C326`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass0,class BaseClass0>::ClassMethod1329()
		calli default string(class G2_C326`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C326::ClassMethod1329.MI.7796()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass0,class BaseClass0>::ClassMethod1328()
		calli default string(class G2_C326`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C326`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C326::Method7.7791<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass0,class BaseClass0>::Method6<object>()
		calli default string(class G2_C326`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C7::Method6.4813<System.Object>()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass0,class BaseClass0>::Method5()
		calli default string(class G2_C326`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C326::Method5.7789()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C7`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C7`2<class BaseClass0,class BaseClass0>::Method4()
		calli default string(class G2_C326`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C326::Method4.7788()"
		ldstr "class G1_C7`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G2_C326`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C326::Method4.7788()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C326`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G2_C326`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C326::Method5.7789()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C326`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G2_C326`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C326::Method6.7790<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C326`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G2_C326`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C7::Method7.MI.4816<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G2_C326`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C326`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C7::Method7.MI.4816<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C326`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C326`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C326::Method7.MI.7792<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C326`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C326`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C326::Method7.MI.7792<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C326`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C326`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C326`2<class BaseClass1,class BaseClass1>::ClassMethod1992<object>()
		calli default string(class G2_C326`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C326::ClassMethod1992.7795<System.Object>()"
		ldstr "class G2_C326`2<class BaseClass1,class BaseClass1> on type class G2_C326`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C326`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C326`2<class BaseClass1,class BaseClass1>::ClassMethod1991<object>()
		calli default string(class G2_C326`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C326::ClassMethod1991.7794<System.Object>()"
		ldstr "class G2_C326`2<class BaseClass1,class BaseClass1> on type class G2_C326`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C326`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C326`2<class BaseClass1,class BaseClass1>::ClassMethod1990()
		calli default string(class G2_C326`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C326::ClassMethod1990.7793()"
		ldstr "class G2_C326`2<class BaseClass1,class BaseClass1> on type class G2_C326`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C326`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C326`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C326`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C326::Method7.7791<System.Object>()"
		ldstr "class G2_C326`2<class BaseClass1,class BaseClass1> on type class G2_C326`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C326`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C326`2<class BaseClass1,class BaseClass1>::Method6<object>()
		calli default string(class G2_C326`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C326::Method6.7790<System.Object>()"
		ldstr "class G2_C326`2<class BaseClass1,class BaseClass1> on type class G2_C326`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C326`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C326`2<class BaseClass1,class BaseClass1>::Method5()
		calli default string(class G2_C326`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C326::Method5.7789()"
		ldstr "class G2_C326`2<class BaseClass1,class BaseClass1> on type class G2_C326`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C326`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C326`2<class BaseClass1,class BaseClass1>::Method4()
		calli default string(class G2_C326`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C326::Method4.7788()"
		ldstr "class G2_C326`2<class BaseClass1,class BaseClass1> on type class G2_C326`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C326`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C326`2<class BaseClass1,class BaseClass1>::ClassMethod1331<object>()
		calli default string(class G2_C326`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C326::ClassMethod1331.MI.7797<System.Object>()"
		ldstr "class G2_C326`2<class BaseClass1,class BaseClass1> on type class G2_C326`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C326`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C326`2<class BaseClass1,class BaseClass1>::ClassMethod1330<object>()
		calli default string(class G2_C326`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C7::ClassMethod1330.4819<System.Object>()"
		ldstr "class G2_C326`2<class BaseClass1,class BaseClass1> on type class G2_C326`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C326`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C326`2<class BaseClass1,class BaseClass1>::ClassMethod1329()
		calli default string(class G2_C326`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C326::ClassMethod1329.MI.7796()"
		ldstr "class G2_C326`2<class BaseClass1,class BaseClass1> on type class G2_C326`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C326`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C326`2<class BaseClass1,class BaseClass1>::ClassMethod1328()
		calli default string(class G2_C326`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C7::ClassMethod1328.4817()"
		ldstr "class G2_C326`2<class BaseClass1,class BaseClass1> on type class G2_C326`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G2_C326`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C326::Method4.7788()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C326`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G2_C326`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C326::Method5.7789()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C326`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G2_C326`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C326::Method6.7790<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C326`2<class BaseClass1,class BaseClass1>"
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
		call void Generated808::MethodCallingTest()
		call void Generated808::ConstrainedCallsTest()
		call void Generated808::StructConstrainedInterfaceCallsTest()
		call void Generated808::CalliTest()
		ldc.i4 100
		ret
	}
}
