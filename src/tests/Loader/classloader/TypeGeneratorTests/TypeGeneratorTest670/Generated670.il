// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) .ver 4:0:0:0 }
.assembly extern TestFramework { .publickeytoken = ( B0 3F 5F 7F 11 D5 0A 3A )  }

//TYPES IN FORWARDER ASSEMBLIES:

//TEST ASSEMBLY:
.assembly Generated670 { .hash algorithm 0x00008004 }
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
.class public G3_C1142`1<T0> 
		extends class G2_C195`1<!T0>
		implements class IBase1`1<class BaseClass1> 
{
	.method public hidebysig newslot virtual instance string Method4() cil managed noinlining { 
		ldstr "G3_C1142::Method4.14557()"
		ret
	}
	.method public hidebysig virtual instance string Method5() cil managed noinlining { 
		ldstr "G3_C1142::Method5.14558()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class BaseClass1>.Method5'() cil managed noinlining { 
		.override method instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G3_C1142::Method5.MI.14559()"
		ret
	}
	.method public hidebysig virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G3_C1142::Method6.14560<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class BaseClass1>.Method6'<M0>() cil managed noinlining { 
		.override method instance string class IBase1`1<class BaseClass1>::Method6<[1]>()
		ldstr "G3_C1142::Method6.MI.14561<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod3680() cil managed noinlining { 
		ldstr "G3_C1142::ClassMethod3680.14562()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C195<T0>.ClassMethod1717'() cil managed noinlining { 
		.override method instance string class G2_C195`1<!T0>::ClassMethod1717()
		ldstr "G3_C1142::ClassMethod1717.MI.14563()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C195<T0>.ClassMethod1718'() cil managed noinlining { 
		.override method instance string class G2_C195`1<!T0>::ClassMethod1718()
		ldstr "G3_C1142::ClassMethod1718.MI.14564()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C195<T0>.ClassMethod1719'<M0>() cil managed noinlining { 
		.override method instance string class G2_C195`1<!T0>::ClassMethod1719<[1]>()
		ldstr "G3_C1142::ClassMethod1719.MI.14565<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class G2_C195`1<!T0>::.ctor()
		ret
	}
}
.class public G2_C195`1<T0> 
		extends class G1_C13`2<!T0,class BaseClass0>
		implements class IBase2`2<!T0,class BaseClass0>, class IBase1`1<!T0> 
{
	.method public hidebysig newslot virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G2_C195::Method7.6592<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase2<T0,class BaseClass0>.Method7'<M0>() cil managed noinlining { 
		.override method instance string class IBase2`2<!T0,class BaseClass0>::Method7<[1]>()
		ldstr "G2_C195::Method7.MI.6593<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string Method4() cil managed noinlining { 
		ldstr "G2_C195::Method4.6594()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<T0>.Method4'() cil managed noinlining { 
		.override method instance string class IBase1`1<!T0>::Method4()
		ldstr "G2_C195::Method4.MI.6595()"
		ret
	}
	.method public hidebysig virtual instance string Method5() cil managed noinlining { 
		ldstr "G2_C195::Method5.6596()"
		ret
	}
	.method public hidebysig virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G2_C195::Method6.6597<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1717() cil managed noinlining { 
		ldstr "G2_C195::ClassMethod1717.6598()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1718() cil managed noinlining { 
		ldstr "G2_C195::ClassMethod1718.6599()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1719<M0>() cil managed noinlining { 
		ldstr "G2_C195::ClassMethod1719.6600<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1720<M0>() cil managed noinlining { 
		ldstr "G2_C195::ClassMethod1720.6601<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C13<T0,class BaseClass0>.ClassMethod1348'<M0>() cil managed noinlining { 
		.override method instance string class G1_C13`2<!T0,class BaseClass0>::ClassMethod1348<[1]>()
		ldstr "G2_C195::ClassMethod1348.MI.6602<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class G1_C13`2<!T0,class BaseClass0>::.ctor()
		ret
	}
}
.class interface public abstract IBase1`1<+T0> 
{
	.method public hidebysig newslot abstract virtual instance string Method4() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method5() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method6<M0>() cil managed { }
}
.class public abstract G1_C13`2<T0, T1> 
		implements class IBase2`2<!T0,!T1>, class IBase1`1<!T0> 
{
	.method public hidebysig virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G1_C13::Method7.4871<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string Method4() cil managed noinlining { 
		ldstr "G1_C13::Method4.4872()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method5() cil managed noinlining { 
		ldstr "G1_C13::Method5.4873()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<T0>.Method5'() cil managed noinlining { 
		.override method instance string class IBase1`1<!T0>::Method5()
		ldstr "G1_C13::Method5.MI.4874()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G1_C13::Method6.4875<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1348<M0>() cil managed noinlining { 
		ldstr "G1_C13::ClassMethod1348.4876<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1349<M0>() cil managed noinlining { 
		ldstr "G1_C13::ClassMethod1349.4877<"
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
.class public auto ansi beforefieldinit Generated670 {
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
	.method static void M.G3_C1142.T<T0,(class G3_C1142`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1142.T<T0,(class G3_C1142`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1142`1<!!T0>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1142`1<!!T0>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1142`1<!!T0>::ClassMethod1717()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1142`1<!!T0>::ClassMethod1718()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1142`1<!!T0>::ClassMethod1719<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1142`1<!!T0>::ClassMethod1720<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1142`1<!!T0>::ClassMethod3680()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1142`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1142`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1142`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1142`1<!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1142.A<(class G3_C1142`1<class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1142.A<(class G3_C1142`1<class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1142`1<class BaseClass0>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1142`1<class BaseClass0>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1142`1<class BaseClass0>::ClassMethod1717()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1142`1<class BaseClass0>::ClassMethod1718()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1142`1<class BaseClass0>::ClassMethod1719<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1142`1<class BaseClass0>::ClassMethod1720<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1142`1<class BaseClass0>::ClassMethod3680()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1142`1<class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1142`1<class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1142`1<class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1142`1<class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1142.B<(class G3_C1142`1<class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1142.B<(class G3_C1142`1<class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1142`1<class BaseClass1>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1142`1<class BaseClass1>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1142`1<class BaseClass1>::ClassMethod1717()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1142`1<class BaseClass1>::ClassMethod1718()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1142`1<class BaseClass1>::ClassMethod1719<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1142`1<class BaseClass1>::ClassMethod1720<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1142`1<class BaseClass1>::ClassMethod3680()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1142`1<class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1142`1<class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1142`1<class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1142`1<class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C195.T<T0,(class G2_C195`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 15
		.locals init (string[] actualResults)
		ldc.i4.s    10
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C195.T<T0,(class G2_C195`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 10
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C195`1<!!T0>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C195`1<!!T0>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C195`1<!!T0>::ClassMethod1717()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C195`1<!!T0>::ClassMethod1718()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C195`1<!!T0>::ClassMethod1719<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C195`1<!!T0>::ClassMethod1720<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C195`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C195`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C195`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C195`1<!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C195.A<(class G2_C195`1<class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 15
		.locals init (string[] actualResults)
		ldc.i4.s    10
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C195.A<(class G2_C195`1<class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 10
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C195`1<class BaseClass0>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C195`1<class BaseClass0>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C195`1<class BaseClass0>::ClassMethod1717()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C195`1<class BaseClass0>::ClassMethod1718()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C195`1<class BaseClass0>::ClassMethod1719<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C195`1<class BaseClass0>::ClassMethod1720<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C195`1<class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C195`1<class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C195`1<class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C195`1<class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C195.B<(class G2_C195`1<class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 15
		.locals init (string[] actualResults)
		ldc.i4.s    10
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C195.B<(class G2_C195`1<class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 10
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C195`1<class BaseClass1>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C195`1<class BaseClass1>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C195`1<class BaseClass1>::ClassMethod1717()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C195`1<class BaseClass1>::ClassMethod1718()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C195`1<class BaseClass1>::ClassMethod1719<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C195`1<class BaseClass1>::ClassMethod1720<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C195`1<class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C195`1<class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C195`1<class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C195`1<class BaseClass1>::Method7<object>()
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
	.method static void M.G1_C13.T.T<T0,T1,(class G1_C13`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C13.T.T<T0,T1,(class G1_C13`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C13`2<!!T0,!!T1>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C13`2<!!T0,!!T1>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C13`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C13`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C13`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C13`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C13.A.T<T1,(class G1_C13`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C13.A.T<T1,(class G1_C13`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C13`2<class BaseClass0,!!T1>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C13`2<class BaseClass0,!!T1>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C13`2<class BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C13`2<class BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C13`2<class BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C13`2<class BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C13.A.A<(class G1_C13`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C13.A.A<(class G1_C13`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C13`2<class BaseClass0,class BaseClass0>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C13`2<class BaseClass0,class BaseClass0>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C13`2<class BaseClass0,class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C13`2<class BaseClass0,class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C13`2<class BaseClass0,class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C13`2<class BaseClass0,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C13.A.B<(class G1_C13`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C13.A.B<(class G1_C13`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C13`2<class BaseClass0,class BaseClass1>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C13`2<class BaseClass0,class BaseClass1>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C13`2<class BaseClass0,class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C13`2<class BaseClass0,class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C13`2<class BaseClass0,class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C13`2<class BaseClass0,class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C13.B.T<T1,(class G1_C13`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C13.B.T<T1,(class G1_C13`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C13`2<class BaseClass1,!!T1>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C13`2<class BaseClass1,!!T1>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C13`2<class BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C13`2<class BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C13`2<class BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C13`2<class BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C13.B.A<(class G1_C13`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C13.B.A<(class G1_C13`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C13`2<class BaseClass1,class BaseClass0>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C13`2<class BaseClass1,class BaseClass0>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C13`2<class BaseClass1,class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C13`2<class BaseClass1,class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C13`2<class BaseClass1,class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C13`2<class BaseClass1,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C13.B.B<(class G1_C13`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C13.B.B<(class G1_C13`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C13`2<class BaseClass1,class BaseClass1>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C13`2<class BaseClass1,class BaseClass1>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C13`2<class BaseClass1,class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C13`2<class BaseClass1,class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C13`2<class BaseClass1,class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C13`2<class BaseClass1,class BaseClass1>::Method7<object>()
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
		newobj instance void class G3_C1142`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C13`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C13`2<class BaseClass0,class BaseClass0>::ClassMethod1349<object>()
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass0> on type class G3_C1142`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C13`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C13`2<class BaseClass0,class BaseClass0>::ClassMethod1348<object>()
		ldstr "G2_C195::ClassMethod1348.MI.6602<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass0> on type class G3_C1142`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C13`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C13`2<class BaseClass0,class BaseClass0>::Method6<object>()
		ldstr "G3_C1142::Method6.14560<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass0> on type class G3_C1142`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C13`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C13`2<class BaseClass0,class BaseClass0>::Method5()
		ldstr "G3_C1142::Method5.14558()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass0> on type class G3_C1142`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C13`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C13`2<class BaseClass0,class BaseClass0>::Method4()
		ldstr "G1_C13::Method4.4872()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass0> on type class G3_C1142`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C13`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C13`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G1_C13::Method7.4871<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass0> on type class G3_C1142`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C195::Method7.MI.6593<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1142`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G3_C1142::Method4.14557()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1142`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G3_C1142::Method5.MI.14559()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1142`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G3_C1142::Method6.MI.14561<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1142`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C195::Method7.MI.6593<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1142`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C195`1<class BaseClass0>
		callvirt instance string class G2_C195`1<class BaseClass0>::ClassMethod1720<object>()
		ldstr "G2_C195::ClassMethod1720.6601<System.Object>()"
		ldstr "class G2_C195`1<class BaseClass0> on type class G3_C1142`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C195`1<class BaseClass0>
		callvirt instance string class G2_C195`1<class BaseClass0>::ClassMethod1719<object>()
		ldstr "G3_C1142::ClassMethod1719.MI.14565<System.Object>()"
		ldstr "class G2_C195`1<class BaseClass0> on type class G3_C1142`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C195`1<class BaseClass0>
		callvirt instance string class G2_C195`1<class BaseClass0>::ClassMethod1718()
		ldstr "G3_C1142::ClassMethod1718.MI.14564()"
		ldstr "class G2_C195`1<class BaseClass0> on type class G3_C1142`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C195`1<class BaseClass0>
		callvirt instance string class G2_C195`1<class BaseClass0>::ClassMethod1717()
		ldstr "G3_C1142::ClassMethod1717.MI.14563()"
		ldstr "class G2_C195`1<class BaseClass0> on type class G3_C1142`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C195`1<class BaseClass0>
		callvirt instance string class G2_C195`1<class BaseClass0>::Method6<object>()
		ldstr "G3_C1142::Method6.14560<System.Object>()"
		ldstr "class G2_C195`1<class BaseClass0> on type class G3_C1142`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C195`1<class BaseClass0>
		callvirt instance string class G2_C195`1<class BaseClass0>::Method5()
		ldstr "G3_C1142::Method5.14558()"
		ldstr "class G2_C195`1<class BaseClass0> on type class G3_C1142`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C195`1<class BaseClass0>
		callvirt instance string class G2_C195`1<class BaseClass0>::Method4()
		ldstr "G2_C195::Method4.6594()"
		ldstr "class G2_C195`1<class BaseClass0> on type class G3_C1142`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C195`1<class BaseClass0>
		callvirt instance string class G2_C195`1<class BaseClass0>::Method7<object>()
		ldstr "G2_C195::Method7.6592<System.Object>()"
		ldstr "class G2_C195`1<class BaseClass0> on type class G3_C1142`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C195`1<class BaseClass0>
		callvirt instance string class G2_C195`1<class BaseClass0>::ClassMethod1349<object>()
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G2_C195`1<class BaseClass0> on type class G3_C1142`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C195`1<class BaseClass0>
		callvirt instance string class G2_C195`1<class BaseClass0>::ClassMethod1348<object>()
		ldstr "G2_C195::ClassMethod1348.MI.6602<System.Object>()"
		ldstr "class G2_C195`1<class BaseClass0> on type class G3_C1142`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G3_C1142`1<class BaseClass0>
		callvirt instance string class G3_C1142`1<class BaseClass0>::ClassMethod3680()
		ldstr "G3_C1142::ClassMethod3680.14562()"
		ldstr "class G3_C1142`1<class BaseClass0> on type class G3_C1142`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1142`1<class BaseClass0>
		callvirt instance string class G3_C1142`1<class BaseClass0>::Method6<object>()
		ldstr "G3_C1142::Method6.14560<System.Object>()"
		ldstr "class G3_C1142`1<class BaseClass0> on type class G3_C1142`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1142`1<class BaseClass0>
		callvirt instance string class G3_C1142`1<class BaseClass0>::Method5()
		ldstr "G3_C1142::Method5.14558()"
		ldstr "class G3_C1142`1<class BaseClass0> on type class G3_C1142`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1142`1<class BaseClass0>
		callvirt instance string class G3_C1142`1<class BaseClass0>::Method4()
		ldstr "G3_C1142::Method4.14557()"
		ldstr "class G3_C1142`1<class BaseClass0> on type class G3_C1142`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1142`1<class BaseClass0>
		callvirt instance string class G3_C1142`1<class BaseClass0>::ClassMethod1720<object>()
		ldstr "G2_C195::ClassMethod1720.6601<System.Object>()"
		ldstr "class G3_C1142`1<class BaseClass0> on type class G3_C1142`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1142`1<class BaseClass0>
		callvirt instance string class G3_C1142`1<class BaseClass0>::ClassMethod1719<object>()
		ldstr "G3_C1142::ClassMethod1719.MI.14565<System.Object>()"
		ldstr "class G3_C1142`1<class BaseClass0> on type class G3_C1142`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1142`1<class BaseClass0>
		callvirt instance string class G3_C1142`1<class BaseClass0>::ClassMethod1718()
		ldstr "G3_C1142::ClassMethod1718.MI.14564()"
		ldstr "class G3_C1142`1<class BaseClass0> on type class G3_C1142`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1142`1<class BaseClass0>
		callvirt instance string class G3_C1142`1<class BaseClass0>::ClassMethod1717()
		ldstr "G3_C1142::ClassMethod1717.MI.14563()"
		ldstr "class G3_C1142`1<class BaseClass0> on type class G3_C1142`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1142`1<class BaseClass0>
		callvirt instance string class G3_C1142`1<class BaseClass0>::Method7<object>()
		ldstr "G2_C195::Method7.6592<System.Object>()"
		ldstr "class G3_C1142`1<class BaseClass0> on type class G3_C1142`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1142`1<class BaseClass0>
		callvirt instance string class G3_C1142`1<class BaseClass0>::ClassMethod1349<object>()
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G3_C1142`1<class BaseClass0> on type class G3_C1142`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1142`1<class BaseClass0>
		callvirt instance string class G3_C1142`1<class BaseClass0>::ClassMethod1348<object>()
		ldstr "G2_C195::ClassMethod1348.MI.6602<System.Object>()"
		ldstr "class G3_C1142`1<class BaseClass0> on type class G3_C1142`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G3_C1142::Method4.14557()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1142`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G3_C1142::Method5.MI.14559()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1142`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G3_C1142::Method6.MI.14561<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1142`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G3_C1142`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C13`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C13`2<class BaseClass1,class BaseClass0>::ClassMethod1349<object>()
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass0> on type class G3_C1142`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C13`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C13`2<class BaseClass1,class BaseClass0>::ClassMethod1348<object>()
		ldstr "G2_C195::ClassMethod1348.MI.6602<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass0> on type class G3_C1142`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C13`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C13`2<class BaseClass1,class BaseClass0>::Method6<object>()
		ldstr "G3_C1142::Method6.14560<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass0> on type class G3_C1142`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C13`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C13`2<class BaseClass1,class BaseClass0>::Method5()
		ldstr "G3_C1142::Method5.14558()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass0> on type class G3_C1142`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C13`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C13`2<class BaseClass1,class BaseClass0>::Method4()
		ldstr "G1_C13::Method4.4872()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass0> on type class G3_C1142`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C13`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C13`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G1_C13::Method7.4871<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass0> on type class G3_C1142`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G2_C195::Method7.MI.6593<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G3_C1142`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G3_C1142::Method4.14557()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1142`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G3_C1142::Method5.MI.14559()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1142`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G3_C1142::Method6.MI.14561<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1142`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C195::Method7.MI.6593<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1142`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C195::Method7.MI.6593<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1142`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C195::Method7.MI.6593<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1142`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G3_C1142::Method4.14557()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1142`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G3_C1142::Method5.MI.14559()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1142`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G3_C1142::Method6.MI.14561<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1142`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C195`1<class BaseClass1>
		callvirt instance string class G2_C195`1<class BaseClass1>::ClassMethod1720<object>()
		ldstr "G2_C195::ClassMethod1720.6601<System.Object>()"
		ldstr "class G2_C195`1<class BaseClass1> on type class G3_C1142`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C195`1<class BaseClass1>
		callvirt instance string class G2_C195`1<class BaseClass1>::ClassMethod1719<object>()
		ldstr "G3_C1142::ClassMethod1719.MI.14565<System.Object>()"
		ldstr "class G2_C195`1<class BaseClass1> on type class G3_C1142`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C195`1<class BaseClass1>
		callvirt instance string class G2_C195`1<class BaseClass1>::ClassMethod1718()
		ldstr "G3_C1142::ClassMethod1718.MI.14564()"
		ldstr "class G2_C195`1<class BaseClass1> on type class G3_C1142`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C195`1<class BaseClass1>
		callvirt instance string class G2_C195`1<class BaseClass1>::ClassMethod1717()
		ldstr "G3_C1142::ClassMethod1717.MI.14563()"
		ldstr "class G2_C195`1<class BaseClass1> on type class G3_C1142`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C195`1<class BaseClass1>
		callvirt instance string class G2_C195`1<class BaseClass1>::Method6<object>()
		ldstr "G3_C1142::Method6.14560<System.Object>()"
		ldstr "class G2_C195`1<class BaseClass1> on type class G3_C1142`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C195`1<class BaseClass1>
		callvirt instance string class G2_C195`1<class BaseClass1>::Method5()
		ldstr "G3_C1142::Method5.14558()"
		ldstr "class G2_C195`1<class BaseClass1> on type class G3_C1142`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C195`1<class BaseClass1>
		callvirt instance string class G2_C195`1<class BaseClass1>::Method4()
		ldstr "G2_C195::Method4.6594()"
		ldstr "class G2_C195`1<class BaseClass1> on type class G3_C1142`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C195`1<class BaseClass1>
		callvirt instance string class G2_C195`1<class BaseClass1>::Method7<object>()
		ldstr "G2_C195::Method7.6592<System.Object>()"
		ldstr "class G2_C195`1<class BaseClass1> on type class G3_C1142`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C195`1<class BaseClass1>
		callvirt instance string class G2_C195`1<class BaseClass1>::ClassMethod1349<object>()
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G2_C195`1<class BaseClass1> on type class G3_C1142`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C195`1<class BaseClass1>
		callvirt instance string class G2_C195`1<class BaseClass1>::ClassMethod1348<object>()
		ldstr "G2_C195::ClassMethod1348.MI.6602<System.Object>()"
		ldstr "class G2_C195`1<class BaseClass1> on type class G3_C1142`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G3_C1142`1<class BaseClass1>
		callvirt instance string class G3_C1142`1<class BaseClass1>::ClassMethod3680()
		ldstr "G3_C1142::ClassMethod3680.14562()"
		ldstr "class G3_C1142`1<class BaseClass1> on type class G3_C1142`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1142`1<class BaseClass1>
		callvirt instance string class G3_C1142`1<class BaseClass1>::Method6<object>()
		ldstr "G3_C1142::Method6.14560<System.Object>()"
		ldstr "class G3_C1142`1<class BaseClass1> on type class G3_C1142`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1142`1<class BaseClass1>
		callvirt instance string class G3_C1142`1<class BaseClass1>::Method5()
		ldstr "G3_C1142::Method5.14558()"
		ldstr "class G3_C1142`1<class BaseClass1> on type class G3_C1142`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1142`1<class BaseClass1>
		callvirt instance string class G3_C1142`1<class BaseClass1>::Method4()
		ldstr "G3_C1142::Method4.14557()"
		ldstr "class G3_C1142`1<class BaseClass1> on type class G3_C1142`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1142`1<class BaseClass1>
		callvirt instance string class G3_C1142`1<class BaseClass1>::ClassMethod1720<object>()
		ldstr "G2_C195::ClassMethod1720.6601<System.Object>()"
		ldstr "class G3_C1142`1<class BaseClass1> on type class G3_C1142`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1142`1<class BaseClass1>
		callvirt instance string class G3_C1142`1<class BaseClass1>::ClassMethod1719<object>()
		ldstr "G3_C1142::ClassMethod1719.MI.14565<System.Object>()"
		ldstr "class G3_C1142`1<class BaseClass1> on type class G3_C1142`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1142`1<class BaseClass1>
		callvirt instance string class G3_C1142`1<class BaseClass1>::ClassMethod1718()
		ldstr "G3_C1142::ClassMethod1718.MI.14564()"
		ldstr "class G3_C1142`1<class BaseClass1> on type class G3_C1142`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1142`1<class BaseClass1>
		callvirt instance string class G3_C1142`1<class BaseClass1>::ClassMethod1717()
		ldstr "G3_C1142::ClassMethod1717.MI.14563()"
		ldstr "class G3_C1142`1<class BaseClass1> on type class G3_C1142`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1142`1<class BaseClass1>
		callvirt instance string class G3_C1142`1<class BaseClass1>::Method7<object>()
		ldstr "G2_C195::Method7.6592<System.Object>()"
		ldstr "class G3_C1142`1<class BaseClass1> on type class G3_C1142`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1142`1<class BaseClass1>
		callvirt instance string class G3_C1142`1<class BaseClass1>::ClassMethod1349<object>()
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G3_C1142`1<class BaseClass1> on type class G3_C1142`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1142`1<class BaseClass1>
		callvirt instance string class G3_C1142`1<class BaseClass1>::ClassMethod1348<object>()
		ldstr "G2_C195::ClassMethod1348.MI.6602<System.Object>()"
		ldstr "class G3_C1142`1<class BaseClass1> on type class G3_C1142`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C195`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C13`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C13`2<class BaseClass0,class BaseClass0>::ClassMethod1349<object>()
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass0> on type class G2_C195`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C13`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C13`2<class BaseClass0,class BaseClass0>::ClassMethod1348<object>()
		ldstr "G2_C195::ClassMethod1348.MI.6602<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass0> on type class G2_C195`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C13`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C13`2<class BaseClass0,class BaseClass0>::Method6<object>()
		ldstr "G2_C195::Method6.6597<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass0> on type class G2_C195`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C13`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C13`2<class BaseClass0,class BaseClass0>::Method5()
		ldstr "G2_C195::Method5.6596()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass0> on type class G2_C195`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C13`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C13`2<class BaseClass0,class BaseClass0>::Method4()
		ldstr "G1_C13::Method4.4872()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass0> on type class G2_C195`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C13`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C13`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G1_C13::Method7.4871<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass0> on type class G2_C195`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C195::Method7.MI.6593<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C195`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C195::Method4.MI.6595()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C195`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C195::Method5.6596()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C195`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C195::Method6.6597<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C195`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C195::Method7.MI.6593<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C195`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C195`1<class BaseClass0>
		callvirt instance string class G2_C195`1<class BaseClass0>::ClassMethod1720<object>()
		ldstr "G2_C195::ClassMethod1720.6601<System.Object>()"
		ldstr "class G2_C195`1<class BaseClass0> on type class G2_C195`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C195`1<class BaseClass0>
		callvirt instance string class G2_C195`1<class BaseClass0>::ClassMethod1719<object>()
		ldstr "G2_C195::ClassMethod1719.6600<System.Object>()"
		ldstr "class G2_C195`1<class BaseClass0> on type class G2_C195`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C195`1<class BaseClass0>
		callvirt instance string class G2_C195`1<class BaseClass0>::ClassMethod1718()
		ldstr "G2_C195::ClassMethod1718.6599()"
		ldstr "class G2_C195`1<class BaseClass0> on type class G2_C195`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C195`1<class BaseClass0>
		callvirt instance string class G2_C195`1<class BaseClass0>::ClassMethod1717()
		ldstr "G2_C195::ClassMethod1717.6598()"
		ldstr "class G2_C195`1<class BaseClass0> on type class G2_C195`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C195`1<class BaseClass0>
		callvirt instance string class G2_C195`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C195::Method6.6597<System.Object>()"
		ldstr "class G2_C195`1<class BaseClass0> on type class G2_C195`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C195`1<class BaseClass0>
		callvirt instance string class G2_C195`1<class BaseClass0>::Method5()
		ldstr "G2_C195::Method5.6596()"
		ldstr "class G2_C195`1<class BaseClass0> on type class G2_C195`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C195`1<class BaseClass0>
		callvirt instance string class G2_C195`1<class BaseClass0>::Method4()
		ldstr "G2_C195::Method4.6594()"
		ldstr "class G2_C195`1<class BaseClass0> on type class G2_C195`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C195`1<class BaseClass0>
		callvirt instance string class G2_C195`1<class BaseClass0>::Method7<object>()
		ldstr "G2_C195::Method7.6592<System.Object>()"
		ldstr "class G2_C195`1<class BaseClass0> on type class G2_C195`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C195`1<class BaseClass0>
		callvirt instance string class G2_C195`1<class BaseClass0>::ClassMethod1349<object>()
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G2_C195`1<class BaseClass0> on type class G2_C195`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C195`1<class BaseClass0>
		callvirt instance string class G2_C195`1<class BaseClass0>::ClassMethod1348<object>()
		ldstr "G2_C195::ClassMethod1348.MI.6602<System.Object>()"
		ldstr "class G2_C195`1<class BaseClass0> on type class G2_C195`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C195`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C13`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C13`2<class BaseClass1,class BaseClass0>::ClassMethod1349<object>()
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass0> on type class G2_C195`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C13`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C13`2<class BaseClass1,class BaseClass0>::ClassMethod1348<object>()
		ldstr "G2_C195::ClassMethod1348.MI.6602<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass0> on type class G2_C195`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C13`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C13`2<class BaseClass1,class BaseClass0>::Method6<object>()
		ldstr "G2_C195::Method6.6597<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass0> on type class G2_C195`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C13`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C13`2<class BaseClass1,class BaseClass0>::Method5()
		ldstr "G2_C195::Method5.6596()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass0> on type class G2_C195`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C13`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C13`2<class BaseClass1,class BaseClass0>::Method4()
		ldstr "G1_C13::Method4.4872()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass0> on type class G2_C195`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C13`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C13`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G1_C13::Method7.4871<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass0> on type class G2_C195`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G2_C195::Method7.MI.6593<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G2_C195`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G2_C195::Method4.MI.6595()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C195`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G2_C195::Method5.6596()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C195`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C195::Method6.6597<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C195`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C195::Method7.MI.6593<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C195`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C195::Method7.MI.6593<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C195`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C195::Method7.MI.6593<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C195`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C195::Method4.MI.6595()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C195`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C195::Method5.6596()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C195`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C195::Method6.6597<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C195`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C195`1<class BaseClass1>
		callvirt instance string class G2_C195`1<class BaseClass1>::ClassMethod1720<object>()
		ldstr "G2_C195::ClassMethod1720.6601<System.Object>()"
		ldstr "class G2_C195`1<class BaseClass1> on type class G2_C195`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C195`1<class BaseClass1>
		callvirt instance string class G2_C195`1<class BaseClass1>::ClassMethod1719<object>()
		ldstr "G2_C195::ClassMethod1719.6600<System.Object>()"
		ldstr "class G2_C195`1<class BaseClass1> on type class G2_C195`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C195`1<class BaseClass1>
		callvirt instance string class G2_C195`1<class BaseClass1>::ClassMethod1718()
		ldstr "G2_C195::ClassMethod1718.6599()"
		ldstr "class G2_C195`1<class BaseClass1> on type class G2_C195`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C195`1<class BaseClass1>
		callvirt instance string class G2_C195`1<class BaseClass1>::ClassMethod1717()
		ldstr "G2_C195::ClassMethod1717.6598()"
		ldstr "class G2_C195`1<class BaseClass1> on type class G2_C195`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C195`1<class BaseClass1>
		callvirt instance string class G2_C195`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C195::Method6.6597<System.Object>()"
		ldstr "class G2_C195`1<class BaseClass1> on type class G2_C195`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C195`1<class BaseClass1>
		callvirt instance string class G2_C195`1<class BaseClass1>::Method5()
		ldstr "G2_C195::Method5.6596()"
		ldstr "class G2_C195`1<class BaseClass1> on type class G2_C195`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C195`1<class BaseClass1>
		callvirt instance string class G2_C195`1<class BaseClass1>::Method4()
		ldstr "G2_C195::Method4.6594()"
		ldstr "class G2_C195`1<class BaseClass1> on type class G2_C195`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C195`1<class BaseClass1>
		callvirt instance string class G2_C195`1<class BaseClass1>::Method7<object>()
		ldstr "G2_C195::Method7.6592<System.Object>()"
		ldstr "class G2_C195`1<class BaseClass1> on type class G2_C195`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C195`1<class BaseClass1>
		callvirt instance string class G2_C195`1<class BaseClass1>::ClassMethod1349<object>()
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G2_C195`1<class BaseClass1> on type class G2_C195`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C195`1<class BaseClass1>
		callvirt instance string class G2_C195`1<class BaseClass1>::ClassMethod1348<object>()
		ldstr "G2_C195::ClassMethod1348.MI.6602<System.Object>()"
		ldstr "class G2_C195`1<class BaseClass1> on type class G2_C195`1<class BaseClass1>"
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
		newobj instance void class G3_C1142`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C195::ClassMethod1348.MI.6602<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G1_C13::Method4.4872()#G3_C1142::Method5.14558()#G3_C1142::Method6.14560<System.Object>()#G1_C13::Method7.4871<System.Object>()#"
		call void Generated670::M.G1_C13.T.T<class BaseClass0,class BaseClass0,class G3_C1142`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C195::ClassMethod1348.MI.6602<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G1_C13::Method4.4872()#G3_C1142::Method5.14558()#G3_C1142::Method6.14560<System.Object>()#G1_C13::Method7.4871<System.Object>()#"
		call void Generated670::M.G1_C13.A.T<class BaseClass0,class G3_C1142`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C195::ClassMethod1348.MI.6602<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G1_C13::Method4.4872()#G3_C1142::Method5.14558()#G3_C1142::Method6.14560<System.Object>()#G1_C13::Method7.4871<System.Object>()#"
		call void Generated670::M.G1_C13.A.A<class G3_C1142`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C195::Method7.MI.6593<System.Object>()#"
		call void Generated670::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G3_C1142`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C195::Method7.MI.6593<System.Object>()#"
		call void Generated670::M.IBase2.A.T<class BaseClass0,class G3_C1142`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C195::Method7.MI.6593<System.Object>()#"
		call void Generated670::M.IBase2.A.A<class G3_C1142`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1142::Method4.14557()#G3_C1142::Method5.MI.14559()#G3_C1142::Method6.MI.14561<System.Object>()#"
		call void Generated670::M.IBase1.T<class BaseClass0,class G3_C1142`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1142::Method4.14557()#G3_C1142::Method5.MI.14559()#G3_C1142::Method6.MI.14561<System.Object>()#"
		call void Generated670::M.IBase1.A<class G3_C1142`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C195::Method7.MI.6593<System.Object>()#"
		call void Generated670::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G3_C1142`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C195::Method7.MI.6593<System.Object>()#"
		call void Generated670::M.IBase2.A.T<class BaseClass1,class G3_C1142`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C195::Method7.MI.6593<System.Object>()#"
		call void Generated670::M.IBase2.A.B<class G3_C1142`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C195::ClassMethod1348.MI.6602<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G3_C1142::ClassMethod1717.MI.14563()#G3_C1142::ClassMethod1718.MI.14564()#G3_C1142::ClassMethod1719.MI.14565<System.Object>()#G2_C195::ClassMethod1720.6601<System.Object>()#G2_C195::Method4.6594()#G3_C1142::Method5.14558()#G3_C1142::Method6.14560<System.Object>()#G2_C195::Method7.6592<System.Object>()#"
		call void Generated670::M.G2_C195.T<class BaseClass0,class G3_C1142`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C195::ClassMethod1348.MI.6602<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G3_C1142::ClassMethod1717.MI.14563()#G3_C1142::ClassMethod1718.MI.14564()#G3_C1142::ClassMethod1719.MI.14565<System.Object>()#G2_C195::ClassMethod1720.6601<System.Object>()#G2_C195::Method4.6594()#G3_C1142::Method5.14558()#G3_C1142::Method6.14560<System.Object>()#G2_C195::Method7.6592<System.Object>()#"
		call void Generated670::M.G2_C195.A<class G3_C1142`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C195::ClassMethod1348.MI.6602<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G3_C1142::ClassMethod1717.MI.14563()#G3_C1142::ClassMethod1718.MI.14564()#G3_C1142::ClassMethod1719.MI.14565<System.Object>()#G2_C195::ClassMethod1720.6601<System.Object>()#G3_C1142::ClassMethod3680.14562()#G3_C1142::Method4.14557()#G3_C1142::Method5.14558()#G3_C1142::Method6.14560<System.Object>()#G2_C195::Method7.6592<System.Object>()#"
		call void Generated670::M.G3_C1142.T<class BaseClass0,class G3_C1142`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C195::ClassMethod1348.MI.6602<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G3_C1142::ClassMethod1717.MI.14563()#G3_C1142::ClassMethod1718.MI.14564()#G3_C1142::ClassMethod1719.MI.14565<System.Object>()#G2_C195::ClassMethod1720.6601<System.Object>()#G3_C1142::ClassMethod3680.14562()#G3_C1142::Method4.14557()#G3_C1142::Method5.14558()#G3_C1142::Method6.14560<System.Object>()#G2_C195::Method7.6592<System.Object>()#"
		call void Generated670::M.G3_C1142.A<class G3_C1142`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1142::Method4.14557()#G3_C1142::Method5.MI.14559()#G3_C1142::Method6.MI.14561<System.Object>()#"
		call void Generated670::M.IBase1.T<class BaseClass1,class G3_C1142`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1142::Method4.14557()#G3_C1142::Method5.MI.14559()#G3_C1142::Method6.MI.14561<System.Object>()#"
		call void Generated670::M.IBase1.B<class G3_C1142`1<class BaseClass0>>(!!0,string)
		newobj instance void class G3_C1142`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C195::ClassMethod1348.MI.6602<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G1_C13::Method4.4872()#G3_C1142::Method5.14558()#G3_C1142::Method6.14560<System.Object>()#G1_C13::Method7.4871<System.Object>()#"
		call void Generated670::M.G1_C13.T.T<class BaseClass1,class BaseClass0,class G3_C1142`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C195::ClassMethod1348.MI.6602<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G1_C13::Method4.4872()#G3_C1142::Method5.14558()#G3_C1142::Method6.14560<System.Object>()#G1_C13::Method7.4871<System.Object>()#"
		call void Generated670::M.G1_C13.B.T<class BaseClass0,class G3_C1142`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C195::ClassMethod1348.MI.6602<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G1_C13::Method4.4872()#G3_C1142::Method5.14558()#G3_C1142::Method6.14560<System.Object>()#G1_C13::Method7.4871<System.Object>()#"
		call void Generated670::M.G1_C13.B.A<class G3_C1142`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C195::Method7.MI.6593<System.Object>()#"
		call void Generated670::M.IBase2.T.T<class BaseClass1,class BaseClass0,class G3_C1142`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C195::Method7.MI.6593<System.Object>()#"
		call void Generated670::M.IBase2.B.T<class BaseClass0,class G3_C1142`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C195::Method7.MI.6593<System.Object>()#"
		call void Generated670::M.IBase2.B.A<class G3_C1142`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1142::Method4.14557()#G3_C1142::Method5.MI.14559()#G3_C1142::Method6.MI.14561<System.Object>()#"
		call void Generated670::M.IBase1.T<class BaseClass1,class G3_C1142`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1142::Method4.14557()#G3_C1142::Method5.MI.14559()#G3_C1142::Method6.MI.14561<System.Object>()#"
		call void Generated670::M.IBase1.B<class G3_C1142`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C195::Method7.MI.6593<System.Object>()#"
		call void Generated670::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G3_C1142`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C195::Method7.MI.6593<System.Object>()#"
		call void Generated670::M.IBase2.A.T<class BaseClass0,class G3_C1142`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C195::Method7.MI.6593<System.Object>()#"
		call void Generated670::M.IBase2.A.A<class G3_C1142`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C195::Method7.MI.6593<System.Object>()#"
		call void Generated670::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G3_C1142`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C195::Method7.MI.6593<System.Object>()#"
		call void Generated670::M.IBase2.A.T<class BaseClass1,class G3_C1142`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C195::Method7.MI.6593<System.Object>()#"
		call void Generated670::M.IBase2.A.B<class G3_C1142`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C195::Method7.MI.6593<System.Object>()#"
		call void Generated670::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G3_C1142`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C195::Method7.MI.6593<System.Object>()#"
		call void Generated670::M.IBase2.B.T<class BaseClass1,class G3_C1142`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C195::Method7.MI.6593<System.Object>()#"
		call void Generated670::M.IBase2.B.B<class G3_C1142`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1142::Method4.14557()#G3_C1142::Method5.MI.14559()#G3_C1142::Method6.MI.14561<System.Object>()#"
		call void Generated670::M.IBase1.T<class BaseClass0,class G3_C1142`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1142::Method4.14557()#G3_C1142::Method5.MI.14559()#G3_C1142::Method6.MI.14561<System.Object>()#"
		call void Generated670::M.IBase1.A<class G3_C1142`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C195::ClassMethod1348.MI.6602<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G3_C1142::ClassMethod1717.MI.14563()#G3_C1142::ClassMethod1718.MI.14564()#G3_C1142::ClassMethod1719.MI.14565<System.Object>()#G2_C195::ClassMethod1720.6601<System.Object>()#G2_C195::Method4.6594()#G3_C1142::Method5.14558()#G3_C1142::Method6.14560<System.Object>()#G2_C195::Method7.6592<System.Object>()#"
		call void Generated670::M.G2_C195.T<class BaseClass1,class G3_C1142`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C195::ClassMethod1348.MI.6602<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G3_C1142::ClassMethod1717.MI.14563()#G3_C1142::ClassMethod1718.MI.14564()#G3_C1142::ClassMethod1719.MI.14565<System.Object>()#G2_C195::ClassMethod1720.6601<System.Object>()#G2_C195::Method4.6594()#G3_C1142::Method5.14558()#G3_C1142::Method6.14560<System.Object>()#G2_C195::Method7.6592<System.Object>()#"
		call void Generated670::M.G2_C195.B<class G3_C1142`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C195::ClassMethod1348.MI.6602<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G3_C1142::ClassMethod1717.MI.14563()#G3_C1142::ClassMethod1718.MI.14564()#G3_C1142::ClassMethod1719.MI.14565<System.Object>()#G2_C195::ClassMethod1720.6601<System.Object>()#G3_C1142::ClassMethod3680.14562()#G3_C1142::Method4.14557()#G3_C1142::Method5.14558()#G3_C1142::Method6.14560<System.Object>()#G2_C195::Method7.6592<System.Object>()#"
		call void Generated670::M.G3_C1142.T<class BaseClass1,class G3_C1142`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C195::ClassMethod1348.MI.6602<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G3_C1142::ClassMethod1717.MI.14563()#G3_C1142::ClassMethod1718.MI.14564()#G3_C1142::ClassMethod1719.MI.14565<System.Object>()#G2_C195::ClassMethod1720.6601<System.Object>()#G3_C1142::ClassMethod3680.14562()#G3_C1142::Method4.14557()#G3_C1142::Method5.14558()#G3_C1142::Method6.14560<System.Object>()#G2_C195::Method7.6592<System.Object>()#"
		call void Generated670::M.G3_C1142.B<class G3_C1142`1<class BaseClass1>>(!!0,string)
		newobj instance void class G2_C195`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C195::ClassMethod1348.MI.6602<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G1_C13::Method4.4872()#G2_C195::Method5.6596()#G2_C195::Method6.6597<System.Object>()#G1_C13::Method7.4871<System.Object>()#"
		call void Generated670::M.G1_C13.T.T<class BaseClass0,class BaseClass0,class G2_C195`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C195::ClassMethod1348.MI.6602<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G1_C13::Method4.4872()#G2_C195::Method5.6596()#G2_C195::Method6.6597<System.Object>()#G1_C13::Method7.4871<System.Object>()#"
		call void Generated670::M.G1_C13.A.T<class BaseClass0,class G2_C195`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C195::ClassMethod1348.MI.6602<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G1_C13::Method4.4872()#G2_C195::Method5.6596()#G2_C195::Method6.6597<System.Object>()#G1_C13::Method7.4871<System.Object>()#"
		call void Generated670::M.G1_C13.A.A<class G2_C195`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C195::Method7.MI.6593<System.Object>()#"
		call void Generated670::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G2_C195`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C195::Method7.MI.6593<System.Object>()#"
		call void Generated670::M.IBase2.A.T<class BaseClass0,class G2_C195`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C195::Method7.MI.6593<System.Object>()#"
		call void Generated670::M.IBase2.A.A<class G2_C195`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C195::Method4.MI.6595()#G2_C195::Method5.6596()#G2_C195::Method6.6597<System.Object>()#"
		call void Generated670::M.IBase1.T<class BaseClass0,class G2_C195`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C195::Method4.MI.6595()#G2_C195::Method5.6596()#G2_C195::Method6.6597<System.Object>()#"
		call void Generated670::M.IBase1.A<class G2_C195`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C195::Method7.MI.6593<System.Object>()#"
		call void Generated670::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C195`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C195::Method7.MI.6593<System.Object>()#"
		call void Generated670::M.IBase2.A.T<class BaseClass1,class G2_C195`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C195::Method7.MI.6593<System.Object>()#"
		call void Generated670::M.IBase2.A.B<class G2_C195`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C195::ClassMethod1348.MI.6602<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G2_C195::ClassMethod1717.6598()#G2_C195::ClassMethod1718.6599()#G2_C195::ClassMethod1719.6600<System.Object>()#G2_C195::ClassMethod1720.6601<System.Object>()#G2_C195::Method4.6594()#G2_C195::Method5.6596()#G2_C195::Method6.6597<System.Object>()#G2_C195::Method7.6592<System.Object>()#"
		call void Generated670::M.G2_C195.T<class BaseClass0,class G2_C195`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C195::ClassMethod1348.MI.6602<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G2_C195::ClassMethod1717.6598()#G2_C195::ClassMethod1718.6599()#G2_C195::ClassMethod1719.6600<System.Object>()#G2_C195::ClassMethod1720.6601<System.Object>()#G2_C195::Method4.6594()#G2_C195::Method5.6596()#G2_C195::Method6.6597<System.Object>()#G2_C195::Method7.6592<System.Object>()#"
		call void Generated670::M.G2_C195.A<class G2_C195`1<class BaseClass0>>(!!0,string)
		newobj instance void class G2_C195`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C195::ClassMethod1348.MI.6602<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G1_C13::Method4.4872()#G2_C195::Method5.6596()#G2_C195::Method6.6597<System.Object>()#G1_C13::Method7.4871<System.Object>()#"
		call void Generated670::M.G1_C13.T.T<class BaseClass1,class BaseClass0,class G2_C195`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C195::ClassMethod1348.MI.6602<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G1_C13::Method4.4872()#G2_C195::Method5.6596()#G2_C195::Method6.6597<System.Object>()#G1_C13::Method7.4871<System.Object>()#"
		call void Generated670::M.G1_C13.B.T<class BaseClass0,class G2_C195`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C195::ClassMethod1348.MI.6602<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G1_C13::Method4.4872()#G2_C195::Method5.6596()#G2_C195::Method6.6597<System.Object>()#G1_C13::Method7.4871<System.Object>()#"
		call void Generated670::M.G1_C13.B.A<class G2_C195`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C195::Method7.MI.6593<System.Object>()#"
		call void Generated670::M.IBase2.T.T<class BaseClass1,class BaseClass0,class G2_C195`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C195::Method7.MI.6593<System.Object>()#"
		call void Generated670::M.IBase2.B.T<class BaseClass0,class G2_C195`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C195::Method7.MI.6593<System.Object>()#"
		call void Generated670::M.IBase2.B.A<class G2_C195`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C195::Method4.MI.6595()#G2_C195::Method5.6596()#G2_C195::Method6.6597<System.Object>()#"
		call void Generated670::M.IBase1.T<class BaseClass1,class G2_C195`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C195::Method4.MI.6595()#G2_C195::Method5.6596()#G2_C195::Method6.6597<System.Object>()#"
		call void Generated670::M.IBase1.B<class G2_C195`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C195::Method7.MI.6593<System.Object>()#"
		call void Generated670::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G2_C195`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C195::Method7.MI.6593<System.Object>()#"
		call void Generated670::M.IBase2.A.T<class BaseClass0,class G2_C195`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C195::Method7.MI.6593<System.Object>()#"
		call void Generated670::M.IBase2.A.A<class G2_C195`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C195::Method7.MI.6593<System.Object>()#"
		call void Generated670::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C195`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C195::Method7.MI.6593<System.Object>()#"
		call void Generated670::M.IBase2.A.T<class BaseClass1,class G2_C195`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C195::Method7.MI.6593<System.Object>()#"
		call void Generated670::M.IBase2.A.B<class G2_C195`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C195::Method7.MI.6593<System.Object>()#"
		call void Generated670::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G2_C195`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C195::Method7.MI.6593<System.Object>()#"
		call void Generated670::M.IBase2.B.T<class BaseClass1,class G2_C195`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C195::Method7.MI.6593<System.Object>()#"
		call void Generated670::M.IBase2.B.B<class G2_C195`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C195::Method4.MI.6595()#G2_C195::Method5.6596()#G2_C195::Method6.6597<System.Object>()#"
		call void Generated670::M.IBase1.T<class BaseClass0,class G2_C195`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C195::Method4.MI.6595()#G2_C195::Method5.6596()#G2_C195::Method6.6597<System.Object>()#"
		call void Generated670::M.IBase1.A<class G2_C195`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C195::ClassMethod1348.MI.6602<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G2_C195::ClassMethod1717.6598()#G2_C195::ClassMethod1718.6599()#G2_C195::ClassMethod1719.6600<System.Object>()#G2_C195::ClassMethod1720.6601<System.Object>()#G2_C195::Method4.6594()#G2_C195::Method5.6596()#G2_C195::Method6.6597<System.Object>()#G2_C195::Method7.6592<System.Object>()#"
		call void Generated670::M.G2_C195.T<class BaseClass1,class G2_C195`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C195::ClassMethod1348.MI.6602<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G2_C195::ClassMethod1717.6598()#G2_C195::ClassMethod1718.6599()#G2_C195::ClassMethod1719.6600<System.Object>()#G2_C195::ClassMethod1720.6601<System.Object>()#G2_C195::Method4.6594()#G2_C195::Method5.6596()#G2_C195::Method6.6597<System.Object>()#G2_C195::Method7.6592<System.Object>()#"
		call void Generated670::M.G2_C195.B<class G2_C195`1<class BaseClass1>>(!!0,string)
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
		newobj instance void class G3_C1142`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C13`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass0,class BaseClass0>::ClassMethod1349<object>()
		calli default string(class G3_C1142`1<class BaseClass0>)
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass0> on type class G3_C1142`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C13`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass0,class BaseClass0>::ClassMethod1348<object>()
		calli default string(class G3_C1142`1<class BaseClass0>)
		ldstr "G2_C195::ClassMethod1348.MI.6602<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass0> on type class G3_C1142`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C13`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass0,class BaseClass0>::Method6<object>()
		calli default string(class G3_C1142`1<class BaseClass0>)
		ldstr "G3_C1142::Method6.14560<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass0> on type class G3_C1142`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C13`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass0,class BaseClass0>::Method5()
		calli default string(class G3_C1142`1<class BaseClass0>)
		ldstr "G3_C1142::Method5.14558()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass0> on type class G3_C1142`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C13`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass0,class BaseClass0>::Method4()
		calli default string(class G3_C1142`1<class BaseClass0>)
		ldstr "G1_C13::Method4.4872()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass0> on type class G3_C1142`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C13`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1142`1<class BaseClass0>)
		ldstr "G1_C13::Method7.4871<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass0> on type class G3_C1142`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1142`1<class BaseClass0>)
		ldstr "G2_C195::Method7.MI.6593<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1142`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G3_C1142`1<class BaseClass0>)
		ldstr "G3_C1142::Method4.14557()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1142`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G3_C1142`1<class BaseClass0>)
		ldstr "G3_C1142::Method5.MI.14559()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1142`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1142`1<class BaseClass0>)
		ldstr "G3_C1142::Method6.MI.14561<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1142`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1142`1<class BaseClass0>)
		ldstr "G2_C195::Method7.MI.6593<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1142`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C195`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C195`1<class BaseClass0>::ClassMethod1720<object>()
		calli default string(class G3_C1142`1<class BaseClass0>)
		ldstr "G2_C195::ClassMethod1720.6601<System.Object>()"
		ldstr "class G2_C195`1<class BaseClass0> on type class G3_C1142`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C195`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C195`1<class BaseClass0>::ClassMethod1719<object>()
		calli default string(class G3_C1142`1<class BaseClass0>)
		ldstr "G3_C1142::ClassMethod1719.MI.14565<System.Object>()"
		ldstr "class G2_C195`1<class BaseClass0> on type class G3_C1142`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C195`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C195`1<class BaseClass0>::ClassMethod1718()
		calli default string(class G3_C1142`1<class BaseClass0>)
		ldstr "G3_C1142::ClassMethod1718.MI.14564()"
		ldstr "class G2_C195`1<class BaseClass0> on type class G3_C1142`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C195`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C195`1<class BaseClass0>::ClassMethod1717()
		calli default string(class G3_C1142`1<class BaseClass0>)
		ldstr "G3_C1142::ClassMethod1717.MI.14563()"
		ldstr "class G2_C195`1<class BaseClass0> on type class G3_C1142`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C195`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C195`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1142`1<class BaseClass0>)
		ldstr "G3_C1142::Method6.14560<System.Object>()"
		ldstr "class G2_C195`1<class BaseClass0> on type class G3_C1142`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C195`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C195`1<class BaseClass0>::Method5()
		calli default string(class G3_C1142`1<class BaseClass0>)
		ldstr "G3_C1142::Method5.14558()"
		ldstr "class G2_C195`1<class BaseClass0> on type class G3_C1142`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C195`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C195`1<class BaseClass0>::Method4()
		calli default string(class G3_C1142`1<class BaseClass0>)
		ldstr "G2_C195::Method4.6594()"
		ldstr "class G2_C195`1<class BaseClass0> on type class G3_C1142`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C195`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C195`1<class BaseClass0>::Method7<object>()
		calli default string(class G3_C1142`1<class BaseClass0>)
		ldstr "G2_C195::Method7.6592<System.Object>()"
		ldstr "class G2_C195`1<class BaseClass0> on type class G3_C1142`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C195`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C195`1<class BaseClass0>::ClassMethod1349<object>()
		calli default string(class G3_C1142`1<class BaseClass0>)
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G2_C195`1<class BaseClass0> on type class G3_C1142`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C195`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C195`1<class BaseClass0>::ClassMethod1348<object>()
		calli default string(class G3_C1142`1<class BaseClass0>)
		ldstr "G2_C195::ClassMethod1348.MI.6602<System.Object>()"
		ldstr "class G2_C195`1<class BaseClass0> on type class G3_C1142`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1142`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1142`1<class BaseClass0>::ClassMethod3680()
		calli default string(class G3_C1142`1<class BaseClass0>)
		ldstr "G3_C1142::ClassMethod3680.14562()"
		ldstr "class G3_C1142`1<class BaseClass0> on type class G3_C1142`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1142`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1142`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1142`1<class BaseClass0>)
		ldstr "G3_C1142::Method6.14560<System.Object>()"
		ldstr "class G3_C1142`1<class BaseClass0> on type class G3_C1142`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1142`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1142`1<class BaseClass0>::Method5()
		calli default string(class G3_C1142`1<class BaseClass0>)
		ldstr "G3_C1142::Method5.14558()"
		ldstr "class G3_C1142`1<class BaseClass0> on type class G3_C1142`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1142`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1142`1<class BaseClass0>::Method4()
		calli default string(class G3_C1142`1<class BaseClass0>)
		ldstr "G3_C1142::Method4.14557()"
		ldstr "class G3_C1142`1<class BaseClass0> on type class G3_C1142`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1142`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1142`1<class BaseClass0>::ClassMethod1720<object>()
		calli default string(class G3_C1142`1<class BaseClass0>)
		ldstr "G2_C195::ClassMethod1720.6601<System.Object>()"
		ldstr "class G3_C1142`1<class BaseClass0> on type class G3_C1142`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1142`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1142`1<class BaseClass0>::ClassMethod1719<object>()
		calli default string(class G3_C1142`1<class BaseClass0>)
		ldstr "G3_C1142::ClassMethod1719.MI.14565<System.Object>()"
		ldstr "class G3_C1142`1<class BaseClass0> on type class G3_C1142`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1142`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1142`1<class BaseClass0>::ClassMethod1718()
		calli default string(class G3_C1142`1<class BaseClass0>)
		ldstr "G3_C1142::ClassMethod1718.MI.14564()"
		ldstr "class G3_C1142`1<class BaseClass0> on type class G3_C1142`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1142`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1142`1<class BaseClass0>::ClassMethod1717()
		calli default string(class G3_C1142`1<class BaseClass0>)
		ldstr "G3_C1142::ClassMethod1717.MI.14563()"
		ldstr "class G3_C1142`1<class BaseClass0> on type class G3_C1142`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1142`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1142`1<class BaseClass0>::Method7<object>()
		calli default string(class G3_C1142`1<class BaseClass0>)
		ldstr "G2_C195::Method7.6592<System.Object>()"
		ldstr "class G3_C1142`1<class BaseClass0> on type class G3_C1142`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1142`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1142`1<class BaseClass0>::ClassMethod1349<object>()
		calli default string(class G3_C1142`1<class BaseClass0>)
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G3_C1142`1<class BaseClass0> on type class G3_C1142`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1142`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1142`1<class BaseClass0>::ClassMethod1348<object>()
		calli default string(class G3_C1142`1<class BaseClass0>)
		ldstr "G2_C195::ClassMethod1348.MI.6602<System.Object>()"
		ldstr "class G3_C1142`1<class BaseClass0> on type class G3_C1142`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G3_C1142`1<class BaseClass0>)
		ldstr "G3_C1142::Method4.14557()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1142`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G3_C1142`1<class BaseClass0>)
		ldstr "G3_C1142::Method5.MI.14559()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1142`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G3_C1142`1<class BaseClass0>)
		ldstr "G3_C1142::Method6.MI.14561<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1142`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G3_C1142`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C13`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass1,class BaseClass0>::ClassMethod1349<object>()
		calli default string(class G3_C1142`1<class BaseClass1>)
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass0> on type class G3_C1142`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C13`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass1,class BaseClass0>::ClassMethod1348<object>()
		calli default string(class G3_C1142`1<class BaseClass1>)
		ldstr "G2_C195::ClassMethod1348.MI.6602<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass0> on type class G3_C1142`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C13`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass1,class BaseClass0>::Method6<object>()
		calli default string(class G3_C1142`1<class BaseClass1>)
		ldstr "G3_C1142::Method6.14560<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass0> on type class G3_C1142`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C13`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass1,class BaseClass0>::Method5()
		calli default string(class G3_C1142`1<class BaseClass1>)
		ldstr "G3_C1142::Method5.14558()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass0> on type class G3_C1142`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C13`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass1,class BaseClass0>::Method4()
		calli default string(class G3_C1142`1<class BaseClass1>)
		ldstr "G1_C13::Method4.4872()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass0> on type class G3_C1142`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C13`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1142`1<class BaseClass1>)
		ldstr "G1_C13::Method7.4871<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass0> on type class G3_C1142`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1142`1<class BaseClass1>)
		ldstr "G2_C195::Method7.MI.6593<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G3_C1142`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G3_C1142`1<class BaseClass1>)
		ldstr "G3_C1142::Method4.14557()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1142`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G3_C1142`1<class BaseClass1>)
		ldstr "G3_C1142::Method5.MI.14559()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1142`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G3_C1142`1<class BaseClass1>)
		ldstr "G3_C1142::Method6.MI.14561<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1142`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1142`1<class BaseClass1>)
		ldstr "G2_C195::Method7.MI.6593<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1142`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1142`1<class BaseClass1>)
		ldstr "G2_C195::Method7.MI.6593<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1142`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1142`1<class BaseClass1>)
		ldstr "G2_C195::Method7.MI.6593<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1142`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G3_C1142`1<class BaseClass1>)
		ldstr "G3_C1142::Method4.14557()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1142`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G3_C1142`1<class BaseClass1>)
		ldstr "G3_C1142::Method5.MI.14559()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1142`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1142`1<class BaseClass1>)
		ldstr "G3_C1142::Method6.MI.14561<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1142`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C195`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C195`1<class BaseClass1>::ClassMethod1720<object>()
		calli default string(class G3_C1142`1<class BaseClass1>)
		ldstr "G2_C195::ClassMethod1720.6601<System.Object>()"
		ldstr "class G2_C195`1<class BaseClass1> on type class G3_C1142`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C195`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C195`1<class BaseClass1>::ClassMethod1719<object>()
		calli default string(class G3_C1142`1<class BaseClass1>)
		ldstr "G3_C1142::ClassMethod1719.MI.14565<System.Object>()"
		ldstr "class G2_C195`1<class BaseClass1> on type class G3_C1142`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C195`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C195`1<class BaseClass1>::ClassMethod1718()
		calli default string(class G3_C1142`1<class BaseClass1>)
		ldstr "G3_C1142::ClassMethod1718.MI.14564()"
		ldstr "class G2_C195`1<class BaseClass1> on type class G3_C1142`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C195`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C195`1<class BaseClass1>::ClassMethod1717()
		calli default string(class G3_C1142`1<class BaseClass1>)
		ldstr "G3_C1142::ClassMethod1717.MI.14563()"
		ldstr "class G2_C195`1<class BaseClass1> on type class G3_C1142`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C195`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C195`1<class BaseClass1>::Method6<object>()
		calli default string(class G3_C1142`1<class BaseClass1>)
		ldstr "G3_C1142::Method6.14560<System.Object>()"
		ldstr "class G2_C195`1<class BaseClass1> on type class G3_C1142`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C195`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C195`1<class BaseClass1>::Method5()
		calli default string(class G3_C1142`1<class BaseClass1>)
		ldstr "G3_C1142::Method5.14558()"
		ldstr "class G2_C195`1<class BaseClass1> on type class G3_C1142`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C195`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C195`1<class BaseClass1>::Method4()
		calli default string(class G3_C1142`1<class BaseClass1>)
		ldstr "G2_C195::Method4.6594()"
		ldstr "class G2_C195`1<class BaseClass1> on type class G3_C1142`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C195`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C195`1<class BaseClass1>::Method7<object>()
		calli default string(class G3_C1142`1<class BaseClass1>)
		ldstr "G2_C195::Method7.6592<System.Object>()"
		ldstr "class G2_C195`1<class BaseClass1> on type class G3_C1142`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C195`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C195`1<class BaseClass1>::ClassMethod1349<object>()
		calli default string(class G3_C1142`1<class BaseClass1>)
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G2_C195`1<class BaseClass1> on type class G3_C1142`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C195`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C195`1<class BaseClass1>::ClassMethod1348<object>()
		calli default string(class G3_C1142`1<class BaseClass1>)
		ldstr "G2_C195::ClassMethod1348.MI.6602<System.Object>()"
		ldstr "class G2_C195`1<class BaseClass1> on type class G3_C1142`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1142`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1142`1<class BaseClass1>::ClassMethod3680()
		calli default string(class G3_C1142`1<class BaseClass1>)
		ldstr "G3_C1142::ClassMethod3680.14562()"
		ldstr "class G3_C1142`1<class BaseClass1> on type class G3_C1142`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1142`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1142`1<class BaseClass1>::Method6<object>()
		calli default string(class G3_C1142`1<class BaseClass1>)
		ldstr "G3_C1142::Method6.14560<System.Object>()"
		ldstr "class G3_C1142`1<class BaseClass1> on type class G3_C1142`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1142`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1142`1<class BaseClass1>::Method5()
		calli default string(class G3_C1142`1<class BaseClass1>)
		ldstr "G3_C1142::Method5.14558()"
		ldstr "class G3_C1142`1<class BaseClass1> on type class G3_C1142`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1142`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1142`1<class BaseClass1>::Method4()
		calli default string(class G3_C1142`1<class BaseClass1>)
		ldstr "G3_C1142::Method4.14557()"
		ldstr "class G3_C1142`1<class BaseClass1> on type class G3_C1142`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1142`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1142`1<class BaseClass1>::ClassMethod1720<object>()
		calli default string(class G3_C1142`1<class BaseClass1>)
		ldstr "G2_C195::ClassMethod1720.6601<System.Object>()"
		ldstr "class G3_C1142`1<class BaseClass1> on type class G3_C1142`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1142`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1142`1<class BaseClass1>::ClassMethod1719<object>()
		calli default string(class G3_C1142`1<class BaseClass1>)
		ldstr "G3_C1142::ClassMethod1719.MI.14565<System.Object>()"
		ldstr "class G3_C1142`1<class BaseClass1> on type class G3_C1142`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1142`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1142`1<class BaseClass1>::ClassMethod1718()
		calli default string(class G3_C1142`1<class BaseClass1>)
		ldstr "G3_C1142::ClassMethod1718.MI.14564()"
		ldstr "class G3_C1142`1<class BaseClass1> on type class G3_C1142`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1142`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1142`1<class BaseClass1>::ClassMethod1717()
		calli default string(class G3_C1142`1<class BaseClass1>)
		ldstr "G3_C1142::ClassMethod1717.MI.14563()"
		ldstr "class G3_C1142`1<class BaseClass1> on type class G3_C1142`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1142`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1142`1<class BaseClass1>::Method7<object>()
		calli default string(class G3_C1142`1<class BaseClass1>)
		ldstr "G2_C195::Method7.6592<System.Object>()"
		ldstr "class G3_C1142`1<class BaseClass1> on type class G3_C1142`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1142`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1142`1<class BaseClass1>::ClassMethod1349<object>()
		calli default string(class G3_C1142`1<class BaseClass1>)
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G3_C1142`1<class BaseClass1> on type class G3_C1142`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1142`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1142`1<class BaseClass1>::ClassMethod1348<object>()
		calli default string(class G3_C1142`1<class BaseClass1>)
		ldstr "G2_C195::ClassMethod1348.MI.6602<System.Object>()"
		ldstr "class G3_C1142`1<class BaseClass1> on type class G3_C1142`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C195`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C13`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass0,class BaseClass0>::ClassMethod1349<object>()
		calli default string(class G2_C195`1<class BaseClass0>)
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass0> on type class G2_C195`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C13`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass0,class BaseClass0>::ClassMethod1348<object>()
		calli default string(class G2_C195`1<class BaseClass0>)
		ldstr "G2_C195::ClassMethod1348.MI.6602<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass0> on type class G2_C195`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C13`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass0,class BaseClass0>::Method6<object>()
		calli default string(class G2_C195`1<class BaseClass0>)
		ldstr "G2_C195::Method6.6597<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass0> on type class G2_C195`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C13`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass0,class BaseClass0>::Method5()
		calli default string(class G2_C195`1<class BaseClass0>)
		ldstr "G2_C195::Method5.6596()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass0> on type class G2_C195`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C13`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass0,class BaseClass0>::Method4()
		calli default string(class G2_C195`1<class BaseClass0>)
		ldstr "G1_C13::Method4.4872()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass0> on type class G2_C195`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C13`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C195`1<class BaseClass0>)
		ldstr "G1_C13::Method7.4871<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass0> on type class G2_C195`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C195`1<class BaseClass0>)
		ldstr "G2_C195::Method7.MI.6593<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C195`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G2_C195`1<class BaseClass0>)
		ldstr "G2_C195::Method4.MI.6595()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C195`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G2_C195`1<class BaseClass0>)
		ldstr "G2_C195::Method5.6596()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C195`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G2_C195`1<class BaseClass0>)
		ldstr "G2_C195::Method6.6597<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C195`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C195`1<class BaseClass0>)
		ldstr "G2_C195::Method7.MI.6593<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C195`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C195`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C195`1<class BaseClass0>::ClassMethod1720<object>()
		calli default string(class G2_C195`1<class BaseClass0>)
		ldstr "G2_C195::ClassMethod1720.6601<System.Object>()"
		ldstr "class G2_C195`1<class BaseClass0> on type class G2_C195`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C195`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C195`1<class BaseClass0>::ClassMethod1719<object>()
		calli default string(class G2_C195`1<class BaseClass0>)
		ldstr "G2_C195::ClassMethod1719.6600<System.Object>()"
		ldstr "class G2_C195`1<class BaseClass0> on type class G2_C195`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C195`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C195`1<class BaseClass0>::ClassMethod1718()
		calli default string(class G2_C195`1<class BaseClass0>)
		ldstr "G2_C195::ClassMethod1718.6599()"
		ldstr "class G2_C195`1<class BaseClass0> on type class G2_C195`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C195`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C195`1<class BaseClass0>::ClassMethod1717()
		calli default string(class G2_C195`1<class BaseClass0>)
		ldstr "G2_C195::ClassMethod1717.6598()"
		ldstr "class G2_C195`1<class BaseClass0> on type class G2_C195`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C195`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C195`1<class BaseClass0>::Method6<object>()
		calli default string(class G2_C195`1<class BaseClass0>)
		ldstr "G2_C195::Method6.6597<System.Object>()"
		ldstr "class G2_C195`1<class BaseClass0> on type class G2_C195`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C195`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C195`1<class BaseClass0>::Method5()
		calli default string(class G2_C195`1<class BaseClass0>)
		ldstr "G2_C195::Method5.6596()"
		ldstr "class G2_C195`1<class BaseClass0> on type class G2_C195`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C195`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C195`1<class BaseClass0>::Method4()
		calli default string(class G2_C195`1<class BaseClass0>)
		ldstr "G2_C195::Method4.6594()"
		ldstr "class G2_C195`1<class BaseClass0> on type class G2_C195`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C195`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C195`1<class BaseClass0>::Method7<object>()
		calli default string(class G2_C195`1<class BaseClass0>)
		ldstr "G2_C195::Method7.6592<System.Object>()"
		ldstr "class G2_C195`1<class BaseClass0> on type class G2_C195`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C195`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C195`1<class BaseClass0>::ClassMethod1349<object>()
		calli default string(class G2_C195`1<class BaseClass0>)
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G2_C195`1<class BaseClass0> on type class G2_C195`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C195`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C195`1<class BaseClass0>::ClassMethod1348<object>()
		calli default string(class G2_C195`1<class BaseClass0>)
		ldstr "G2_C195::ClassMethod1348.MI.6602<System.Object>()"
		ldstr "class G2_C195`1<class BaseClass0> on type class G2_C195`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C195`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C13`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass1,class BaseClass0>::ClassMethod1349<object>()
		calli default string(class G2_C195`1<class BaseClass1>)
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass0> on type class G2_C195`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C13`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass1,class BaseClass0>::ClassMethod1348<object>()
		calli default string(class G2_C195`1<class BaseClass1>)
		ldstr "G2_C195::ClassMethod1348.MI.6602<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass0> on type class G2_C195`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C13`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass1,class BaseClass0>::Method6<object>()
		calli default string(class G2_C195`1<class BaseClass1>)
		ldstr "G2_C195::Method6.6597<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass0> on type class G2_C195`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C13`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass1,class BaseClass0>::Method5()
		calli default string(class G2_C195`1<class BaseClass1>)
		ldstr "G2_C195::Method5.6596()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass0> on type class G2_C195`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C13`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass1,class BaseClass0>::Method4()
		calli default string(class G2_C195`1<class BaseClass1>)
		ldstr "G1_C13::Method4.4872()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass0> on type class G2_C195`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C13`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G2_C195`1<class BaseClass1>)
		ldstr "G1_C13::Method7.4871<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass0> on type class G2_C195`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G2_C195`1<class BaseClass1>)
		ldstr "G2_C195::Method7.MI.6593<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G2_C195`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G2_C195`1<class BaseClass1>)
		ldstr "G2_C195::Method4.MI.6595()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C195`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G2_C195`1<class BaseClass1>)
		ldstr "G2_C195::Method5.6596()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C195`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G2_C195`1<class BaseClass1>)
		ldstr "G2_C195::Method6.6597<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C195`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C195`1<class BaseClass1>)
		ldstr "G2_C195::Method7.MI.6593<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C195`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C195`1<class BaseClass1>)
		ldstr "G2_C195::Method7.MI.6593<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C195`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C195`1<class BaseClass1>)
		ldstr "G2_C195::Method7.MI.6593<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C195`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G2_C195`1<class BaseClass1>)
		ldstr "G2_C195::Method4.MI.6595()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C195`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G2_C195`1<class BaseClass1>)
		ldstr "G2_C195::Method5.6596()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C195`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G2_C195`1<class BaseClass1>)
		ldstr "G2_C195::Method6.6597<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C195`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C195`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C195`1<class BaseClass1>::ClassMethod1720<object>()
		calli default string(class G2_C195`1<class BaseClass1>)
		ldstr "G2_C195::ClassMethod1720.6601<System.Object>()"
		ldstr "class G2_C195`1<class BaseClass1> on type class G2_C195`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C195`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C195`1<class BaseClass1>::ClassMethod1719<object>()
		calli default string(class G2_C195`1<class BaseClass1>)
		ldstr "G2_C195::ClassMethod1719.6600<System.Object>()"
		ldstr "class G2_C195`1<class BaseClass1> on type class G2_C195`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C195`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C195`1<class BaseClass1>::ClassMethod1718()
		calli default string(class G2_C195`1<class BaseClass1>)
		ldstr "G2_C195::ClassMethod1718.6599()"
		ldstr "class G2_C195`1<class BaseClass1> on type class G2_C195`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C195`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C195`1<class BaseClass1>::ClassMethod1717()
		calli default string(class G2_C195`1<class BaseClass1>)
		ldstr "G2_C195::ClassMethod1717.6598()"
		ldstr "class G2_C195`1<class BaseClass1> on type class G2_C195`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C195`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C195`1<class BaseClass1>::Method6<object>()
		calli default string(class G2_C195`1<class BaseClass1>)
		ldstr "G2_C195::Method6.6597<System.Object>()"
		ldstr "class G2_C195`1<class BaseClass1> on type class G2_C195`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C195`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C195`1<class BaseClass1>::Method5()
		calli default string(class G2_C195`1<class BaseClass1>)
		ldstr "G2_C195::Method5.6596()"
		ldstr "class G2_C195`1<class BaseClass1> on type class G2_C195`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C195`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C195`1<class BaseClass1>::Method4()
		calli default string(class G2_C195`1<class BaseClass1>)
		ldstr "G2_C195::Method4.6594()"
		ldstr "class G2_C195`1<class BaseClass1> on type class G2_C195`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C195`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C195`1<class BaseClass1>::Method7<object>()
		calli default string(class G2_C195`1<class BaseClass1>)
		ldstr "G2_C195::Method7.6592<System.Object>()"
		ldstr "class G2_C195`1<class BaseClass1> on type class G2_C195`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C195`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C195`1<class BaseClass1>::ClassMethod1349<object>()
		calli default string(class G2_C195`1<class BaseClass1>)
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G2_C195`1<class BaseClass1> on type class G2_C195`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C195`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C195`1<class BaseClass1>::ClassMethod1348<object>()
		calli default string(class G2_C195`1<class BaseClass1>)
		ldstr "G2_C195::ClassMethod1348.MI.6602<System.Object>()"
		ldstr "class G2_C195`1<class BaseClass1> on type class G2_C195`1<class BaseClass1>"
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
		call void Generated670::MethodCallingTest()
		call void Generated670::ConstrainedCallsTest()
		call void Generated670::StructConstrainedInterfaceCallsTest()
		call void Generated670::CalliTest()
		ldc.i4 100
		ret
	}
}
