// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) .ver 4:0:0:0 }
.assembly extern TestFramework { .publickeytoken = ( B0 3F 5F 7F 11 D5 0A 3A )  }

//TYPES IN FORWARDER ASSEMBLIES:

//TEST ASSEMBLY:
.assembly Generated726 { .hash algorithm 0x00008004 }
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
.class public G3_C1198`1<T0> 
		extends class G2_C256`2<class BaseClass1,class BaseClass0>
		implements class IBase2`2<!T0,class BaseClass1> 
{
	.method public hidebysig newslot virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G3_C1198::Method7.14847<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase2<T0,class BaseClass1>.Method7'<M0>() cil managed noinlining { 
		.override method instance string class IBase2`2<!T0,class BaseClass1>::Method7<[1]>()
		ldstr "G3_C1198::Method7.MI.14848<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod3785() cil managed noinlining { 
		ldstr "G3_C1198::ClassMethod3785.14849()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod3786() cil managed noinlining { 
		ldstr "G3_C1198::ClassMethod3786.14850()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod3787<M0>() cil managed noinlining { 
		ldstr "G3_C1198::ClassMethod3787.14851<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod3788<M0>() cil managed noinlining { 
		ldstr "G3_C1198::ClassMethod3788.14852<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C256<class BaseClass1,class BaseClass0>.ClassMethod1848'() cil managed noinlining { 
		.override method instance string class G2_C256`2<class BaseClass1,class BaseClass0>::ClassMethod1848()
		ldstr "G3_C1198::ClassMethod1848.MI.14853()"
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class G2_C256`2<class BaseClass1,class BaseClass0>::.ctor()
		ret
	}
}
.class public abstract G2_C256`2<T0, T1> 
		extends class G1_C5`1<class BaseClass1>
		implements IBase0, class IBase1`1<class BaseClass1> 
{
	.method public hidebysig newslot virtual instance string Method0() cil managed noinlining { 
		ldstr "G2_C256::Method0.7149()"
		ret
	}
	.method public hidebysig virtual instance string Method1() cil managed noinlining { 
		ldstr "G2_C256::Method1.7150()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method2<M0>() cil managed noinlining { 
		ldstr "G2_C256::Method2.7151<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string Method3<M0>() cil managed noinlining { 
		ldstr "G2_C256::Method3.7152<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig virtual instance string Method4() cil managed noinlining { 
		ldstr "G2_C256::Method4.7153()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method5() cil managed noinlining { 
		ldstr "G2_C256::Method5.7154()"
		ret
	}
	.method public hidebysig virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G2_C256::Method6.7155<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class BaseClass1>.Method6'<M0>() cil managed noinlining { 
		.override method instance string class IBase1`1<class BaseClass1>::Method6<[1]>()
		ldstr "G2_C256::Method6.MI.7156<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1847() cil managed noinlining { 
		ldstr "G2_C256::ClassMethod1847.7157()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1848() cil managed noinlining { 
		ldstr "G2_C256::ClassMethod1848.7158()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C5<class BaseClass1>.ClassMethod1324'() cil managed noinlining { 
		.override method instance string class G1_C5`1<class BaseClass1>::ClassMethod1324()
		ldstr "G2_C256::ClassMethod1324.MI.7159()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C5<class BaseClass1>.ClassMethod1325'<M0>() cil managed noinlining { 
		.override method instance string class G1_C5`1<class BaseClass1>::ClassMethod1325<[1]>()
		ldstr "G2_C256::ClassMethod1325.MI.7160<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class G1_C5`1<class BaseClass1>::.ctor()
		ret
	}
}
.class interface public abstract IBase2`2<+T0, -T1> 
{
	.method public hidebysig newslot abstract virtual instance string Method7<M0>() cil managed { }
}
.class public G1_C5`1<T0> 
		implements class IBase2`2<class BaseClass1,class BaseClass1>, class IBase1`1<!T0> 
{
	.method public hidebysig newslot virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G1_C5::Method7.4801<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig virtual instance string Method4() cil managed noinlining { 
		ldstr "G1_C5::Method4.4802()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method5() cil managed noinlining { 
		ldstr "G1_C5::Method5.4803()"
		ret
	}
	.method public hidebysig virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G1_C5::Method6.4804<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<T0>.Method6'<M0>() cil managed noinlining { 
		.override method instance string class IBase1`1<!T0>::Method6<[1]>()
		ldstr "G1_C5::Method6.MI.4805<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1324() cil managed noinlining { 
		ldstr "G1_C5::ClassMethod1324.4806()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1325<M0>() cil managed noinlining { 
		ldstr "G1_C5::ClassMethod1325.4807<"
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
.class public auto ansi beforefieldinit Generated726 {
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
	.method static void M.G3_C1198.T<T0,(class G3_C1198`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 21
		.locals init (string[] actualResults)
		ldc.i4.s    16
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1198.T<T0,(class G3_C1198`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 16
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1198`1<!!T0>::ClassMethod1324()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1198`1<!!T0>::ClassMethod1325<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1198`1<!!T0>::ClassMethod1847()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1198`1<!!T0>::ClassMethod1848()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1198`1<!!T0>::ClassMethod3785()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1198`1<!!T0>::ClassMethod3786()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1198`1<!!T0>::ClassMethod3787<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1198`1<!!T0>::ClassMethod3788<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1198`1<!!T0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1198`1<!!T0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1198`1<!!T0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1198`1<!!T0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1198`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1198`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     14
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1198`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     15
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1198`1<!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1198.A<(class G3_C1198`1<class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 21
		.locals init (string[] actualResults)
		ldc.i4.s    16
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1198.A<(class G3_C1198`1<class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 16
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1198`1<class BaseClass0>::ClassMethod1324()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1198`1<class BaseClass0>::ClassMethod1325<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1198`1<class BaseClass0>::ClassMethod1847()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1198`1<class BaseClass0>::ClassMethod1848()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1198`1<class BaseClass0>::ClassMethod3785()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1198`1<class BaseClass0>::ClassMethod3786()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1198`1<class BaseClass0>::ClassMethod3787<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1198`1<class BaseClass0>::ClassMethod3788<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1198`1<class BaseClass0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1198`1<class BaseClass0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1198`1<class BaseClass0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1198`1<class BaseClass0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1198`1<class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1198`1<class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     14
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1198`1<class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     15
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1198`1<class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1198.B<(class G3_C1198`1<class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 21
		.locals init (string[] actualResults)
		ldc.i4.s    16
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1198.B<(class G3_C1198`1<class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 16
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1198`1<class BaseClass1>::ClassMethod1324()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1198`1<class BaseClass1>::ClassMethod1325<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1198`1<class BaseClass1>::ClassMethod1847()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1198`1<class BaseClass1>::ClassMethod1848()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1198`1<class BaseClass1>::ClassMethod3785()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1198`1<class BaseClass1>::ClassMethod3786()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1198`1<class BaseClass1>::ClassMethod3787<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1198`1<class BaseClass1>::ClassMethod3788<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1198`1<class BaseClass1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1198`1<class BaseClass1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1198`1<class BaseClass1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1198`1<class BaseClass1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1198`1<class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1198`1<class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     14
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1198`1<class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     15
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1198`1<class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C256.T.T<T0,T1,(class G2_C256`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 17
		.locals init (string[] actualResults)
		ldc.i4.s    12
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C256.T.T<T0,T1,(class G2_C256`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 12
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C256`2<!!T0,!!T1>::ClassMethod1324()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C256`2<!!T0,!!T1>::ClassMethod1325<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C256`2<!!T0,!!T1>::ClassMethod1847()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C256`2<!!T0,!!T1>::ClassMethod1848()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C256`2<!!T0,!!T1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C256`2<!!T0,!!T1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C256`2<!!T0,!!T1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C256`2<!!T0,!!T1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C256`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C256`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C256`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C256`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C256.A.T<T1,(class G2_C256`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 17
		.locals init (string[] actualResults)
		ldc.i4.s    12
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C256.A.T<T1,(class G2_C256`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 12
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C256`2<class BaseClass0,!!T1>::ClassMethod1324()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C256`2<class BaseClass0,!!T1>::ClassMethod1325<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C256`2<class BaseClass0,!!T1>::ClassMethod1847()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C256`2<class BaseClass0,!!T1>::ClassMethod1848()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C256`2<class BaseClass0,!!T1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C256`2<class BaseClass0,!!T1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C256`2<class BaseClass0,!!T1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C256`2<class BaseClass0,!!T1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C256`2<class BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C256`2<class BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C256`2<class BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C256`2<class BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C256.A.A<(class G2_C256`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 17
		.locals init (string[] actualResults)
		ldc.i4.s    12
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C256.A.A<(class G2_C256`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 12
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C256`2<class BaseClass0,class BaseClass0>::ClassMethod1324()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C256`2<class BaseClass0,class BaseClass0>::ClassMethod1325<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C256`2<class BaseClass0,class BaseClass0>::ClassMethod1847()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C256`2<class BaseClass0,class BaseClass0>::ClassMethod1848()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C256`2<class BaseClass0,class BaseClass0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C256`2<class BaseClass0,class BaseClass0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C256`2<class BaseClass0,class BaseClass0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C256`2<class BaseClass0,class BaseClass0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C256`2<class BaseClass0,class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C256`2<class BaseClass0,class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C256`2<class BaseClass0,class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C256`2<class BaseClass0,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C256.A.B<(class G2_C256`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 17
		.locals init (string[] actualResults)
		ldc.i4.s    12
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C256.A.B<(class G2_C256`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 12
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C256`2<class BaseClass0,class BaseClass1>::ClassMethod1324()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C256`2<class BaseClass0,class BaseClass1>::ClassMethod1325<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C256`2<class BaseClass0,class BaseClass1>::ClassMethod1847()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C256`2<class BaseClass0,class BaseClass1>::ClassMethod1848()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C256`2<class BaseClass0,class BaseClass1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C256`2<class BaseClass0,class BaseClass1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C256`2<class BaseClass0,class BaseClass1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C256`2<class BaseClass0,class BaseClass1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C256`2<class BaseClass0,class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C256`2<class BaseClass0,class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C256`2<class BaseClass0,class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C256`2<class BaseClass0,class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C256.B.T<T1,(class G2_C256`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 17
		.locals init (string[] actualResults)
		ldc.i4.s    12
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C256.B.T<T1,(class G2_C256`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 12
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C256`2<class BaseClass1,!!T1>::ClassMethod1324()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C256`2<class BaseClass1,!!T1>::ClassMethod1325<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C256`2<class BaseClass1,!!T1>::ClassMethod1847()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C256`2<class BaseClass1,!!T1>::ClassMethod1848()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C256`2<class BaseClass1,!!T1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C256`2<class BaseClass1,!!T1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C256`2<class BaseClass1,!!T1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C256`2<class BaseClass1,!!T1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C256`2<class BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C256`2<class BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C256`2<class BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C256`2<class BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C256.B.A<(class G2_C256`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 17
		.locals init (string[] actualResults)
		ldc.i4.s    12
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C256.B.A<(class G2_C256`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 12
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C256`2<class BaseClass1,class BaseClass0>::ClassMethod1324()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C256`2<class BaseClass1,class BaseClass0>::ClassMethod1325<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C256`2<class BaseClass1,class BaseClass0>::ClassMethod1847()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C256`2<class BaseClass1,class BaseClass0>::ClassMethod1848()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C256`2<class BaseClass1,class BaseClass0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C256`2<class BaseClass1,class BaseClass0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C256`2<class BaseClass1,class BaseClass0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C256`2<class BaseClass1,class BaseClass0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C256`2<class BaseClass1,class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C256`2<class BaseClass1,class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C256`2<class BaseClass1,class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C256`2<class BaseClass1,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C256.B.B<(class G2_C256`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 17
		.locals init (string[] actualResults)
		ldc.i4.s    12
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C256.B.B<(class G2_C256`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 12
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C256`2<class BaseClass1,class BaseClass1>::ClassMethod1324()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C256`2<class BaseClass1,class BaseClass1>::ClassMethod1325<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C256`2<class BaseClass1,class BaseClass1>::ClassMethod1847()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C256`2<class BaseClass1,class BaseClass1>::ClassMethod1848()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C256`2<class BaseClass1,class BaseClass1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C256`2<class BaseClass1,class BaseClass1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C256`2<class BaseClass1,class BaseClass1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C256`2<class BaseClass1,class BaseClass1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C256`2<class BaseClass1,class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C256`2<class BaseClass1,class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C256`2<class BaseClass1,class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C256`2<class BaseClass1,class BaseClass1>::Method7<object>()
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
	.method static void M.G1_C5.T<T0,(class G1_C5`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C5.T<T0,(class G1_C5`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C5`1<!!T0>::ClassMethod1324()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C5`1<!!T0>::ClassMethod1325<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C5`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C5`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C5`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C5`1<!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C5.A<(class G1_C5`1<class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C5.A<(class G1_C5`1<class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C5`1<class BaseClass0>::ClassMethod1324()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C5`1<class BaseClass0>::ClassMethod1325<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C5`1<class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C5`1<class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C5`1<class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C5`1<class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C5.B<(class G1_C5`1<class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C5.B<(class G1_C5`1<class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C5`1<class BaseClass1>::ClassMethod1324()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C5`1<class BaseClass1>::ClassMethod1325<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C5`1<class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C5`1<class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C5`1<class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C5`1<class BaseClass1>::Method7<object>()
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
		newobj instance void class G3_C1198`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C5`1<class BaseClass1>
		callvirt instance string class G1_C5`1<class BaseClass1>::ClassMethod1325<object>()
		ldstr "G2_C256::ClassMethod1325.MI.7160<System.Object>()"
		ldstr "class G1_C5`1<class BaseClass1> on type class G3_C1198`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C5`1<class BaseClass1>
		callvirt instance string class G1_C5`1<class BaseClass1>::ClassMethod1324()
		ldstr "G2_C256::ClassMethod1324.MI.7159()"
		ldstr "class G1_C5`1<class BaseClass1> on type class G3_C1198`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C5`1<class BaseClass1>
		callvirt instance string class G1_C5`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C256::Method6.7155<System.Object>()"
		ldstr "class G1_C5`1<class BaseClass1> on type class G3_C1198`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C5`1<class BaseClass1>
		callvirt instance string class G1_C5`1<class BaseClass1>::Method5()
		ldstr "G1_C5::Method5.4803()"
		ldstr "class G1_C5`1<class BaseClass1> on type class G3_C1198`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C5`1<class BaseClass1>
		callvirt instance string class G1_C5`1<class BaseClass1>::Method4()
		ldstr "G2_C256::Method4.7153()"
		ldstr "class G1_C5`1<class BaseClass1> on type class G3_C1198`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C5`1<class BaseClass1>
		callvirt instance string class G1_C5`1<class BaseClass1>::Method7<object>()
		ldstr "G1_C5::Method7.4801<System.Object>()"
		ldstr "class G1_C5`1<class BaseClass1> on type class G3_C1198`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C5::Method7.4801<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1198`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G2_C256::Method4.7153()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1198`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G2_C256::Method5.7154()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1198`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C256::Method6.MI.7156<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1198`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G3_C1198::Method7.MI.14848<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1198`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C256::Method4.7153()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1198`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C256::Method5.7154()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1198`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C256::Method6.MI.7156<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1198`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C256`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C256`2<class BaseClass1,class BaseClass0>::ClassMethod1848()
		ldstr "G3_C1198::ClassMethod1848.MI.14853()"
		ldstr "class G2_C256`2<class BaseClass1,class BaseClass0> on type class G3_C1198`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C256`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C256`2<class BaseClass1,class BaseClass0>::ClassMethod1847()
		ldstr "G2_C256::ClassMethod1847.7157()"
		ldstr "class G2_C256`2<class BaseClass1,class BaseClass0> on type class G3_C1198`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C256`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C256`2<class BaseClass1,class BaseClass0>::Method6<object>()
		ldstr "G2_C256::Method6.7155<System.Object>()"
		ldstr "class G2_C256`2<class BaseClass1,class BaseClass0> on type class G3_C1198`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C256`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C256`2<class BaseClass1,class BaseClass0>::Method5()
		ldstr "G2_C256::Method5.7154()"
		ldstr "class G2_C256`2<class BaseClass1,class BaseClass0> on type class G3_C1198`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C256`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C256`2<class BaseClass1,class BaseClass0>::Method4()
		ldstr "G2_C256::Method4.7153()"
		ldstr "class G2_C256`2<class BaseClass1,class BaseClass0> on type class G3_C1198`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C256`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C256`2<class BaseClass1,class BaseClass0>::Method3<object>()
		ldstr "G2_C256::Method3.7152<System.Object>()"
		ldstr "class G2_C256`2<class BaseClass1,class BaseClass0> on type class G3_C1198`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C256`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C256`2<class BaseClass1,class BaseClass0>::Method2<object>()
		ldstr "G2_C256::Method2.7151<System.Object>()"
		ldstr "class G2_C256`2<class BaseClass1,class BaseClass0> on type class G3_C1198`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C256`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C256`2<class BaseClass1,class BaseClass0>::Method1()
		ldstr "G2_C256::Method1.7150()"
		ldstr "class G2_C256`2<class BaseClass1,class BaseClass0> on type class G3_C1198`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C256`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C256`2<class BaseClass1,class BaseClass0>::Method0()
		ldstr "G2_C256::Method0.7149()"
		ldstr "class G2_C256`2<class BaseClass1,class BaseClass0> on type class G3_C1198`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C256`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C256`2<class BaseClass1,class BaseClass0>::ClassMethod1325<object>()
		ldstr "G2_C256::ClassMethod1325.MI.7160<System.Object>()"
		ldstr "class G2_C256`2<class BaseClass1,class BaseClass0> on type class G3_C1198`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C256`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C256`2<class BaseClass1,class BaseClass0>::ClassMethod1324()
		ldstr "G2_C256::ClassMethod1324.MI.7159()"
		ldstr "class G2_C256`2<class BaseClass1,class BaseClass0> on type class G3_C1198`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C256`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C256`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G1_C5::Method7.4801<System.Object>()"
		ldstr "class G2_C256`2<class BaseClass1,class BaseClass0> on type class G3_C1198`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string IBase0::Method0()
		ldstr "G2_C256::Method0.7149()"
		ldstr "IBase0 on type class G3_C1198`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method1()
		ldstr "G2_C256::Method1.7150()"
		ldstr "IBase0 on type class G3_C1198`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method2<object>()
		ldstr "G2_C256::Method2.7151<System.Object>()"
		ldstr "IBase0 on type class G3_C1198`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method3<object>()
		ldstr "G2_C256::Method3.7152<System.Object>()"
		ldstr "IBase0 on type class G3_C1198`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G3_C1198`1<class BaseClass0>
		callvirt instance string class G3_C1198`1<class BaseClass0>::ClassMethod3788<object>()
		ldstr "G3_C1198::ClassMethod3788.14852<System.Object>()"
		ldstr "class G3_C1198`1<class BaseClass0> on type class G3_C1198`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1198`1<class BaseClass0>
		callvirt instance string class G3_C1198`1<class BaseClass0>::ClassMethod3787<object>()
		ldstr "G3_C1198::ClassMethod3787.14851<System.Object>()"
		ldstr "class G3_C1198`1<class BaseClass0> on type class G3_C1198`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1198`1<class BaseClass0>
		callvirt instance string class G3_C1198`1<class BaseClass0>::ClassMethod3786()
		ldstr "G3_C1198::ClassMethod3786.14850()"
		ldstr "class G3_C1198`1<class BaseClass0> on type class G3_C1198`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1198`1<class BaseClass0>
		callvirt instance string class G3_C1198`1<class BaseClass0>::ClassMethod3785()
		ldstr "G3_C1198::ClassMethod3785.14849()"
		ldstr "class G3_C1198`1<class BaseClass0> on type class G3_C1198`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1198`1<class BaseClass0>
		callvirt instance string class G3_C1198`1<class BaseClass0>::Method7<object>()
		ldstr "G3_C1198::Method7.14847<System.Object>()"
		ldstr "class G3_C1198`1<class BaseClass0> on type class G3_C1198`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1198`1<class BaseClass0>
		callvirt instance string class G3_C1198`1<class BaseClass0>::ClassMethod1848()
		ldstr "G3_C1198::ClassMethod1848.MI.14853()"
		ldstr "class G3_C1198`1<class BaseClass0> on type class G3_C1198`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1198`1<class BaseClass0>
		callvirt instance string class G3_C1198`1<class BaseClass0>::ClassMethod1847()
		ldstr "G2_C256::ClassMethod1847.7157()"
		ldstr "class G3_C1198`1<class BaseClass0> on type class G3_C1198`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1198`1<class BaseClass0>
		callvirt instance string class G3_C1198`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C256::Method6.7155<System.Object>()"
		ldstr "class G3_C1198`1<class BaseClass0> on type class G3_C1198`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1198`1<class BaseClass0>
		callvirt instance string class G3_C1198`1<class BaseClass0>::Method5()
		ldstr "G2_C256::Method5.7154()"
		ldstr "class G3_C1198`1<class BaseClass0> on type class G3_C1198`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1198`1<class BaseClass0>
		callvirt instance string class G3_C1198`1<class BaseClass0>::Method4()
		ldstr "G2_C256::Method4.7153()"
		ldstr "class G3_C1198`1<class BaseClass0> on type class G3_C1198`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1198`1<class BaseClass0>
		callvirt instance string class G3_C1198`1<class BaseClass0>::Method3<object>()
		ldstr "G2_C256::Method3.7152<System.Object>()"
		ldstr "class G3_C1198`1<class BaseClass0> on type class G3_C1198`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1198`1<class BaseClass0>
		callvirt instance string class G3_C1198`1<class BaseClass0>::Method2<object>()
		ldstr "G2_C256::Method2.7151<System.Object>()"
		ldstr "class G3_C1198`1<class BaseClass0> on type class G3_C1198`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1198`1<class BaseClass0>
		callvirt instance string class G3_C1198`1<class BaseClass0>::Method1()
		ldstr "G2_C256::Method1.7150()"
		ldstr "class G3_C1198`1<class BaseClass0> on type class G3_C1198`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1198`1<class BaseClass0>
		callvirt instance string class G3_C1198`1<class BaseClass0>::Method0()
		ldstr "G2_C256::Method0.7149()"
		ldstr "class G3_C1198`1<class BaseClass0> on type class G3_C1198`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1198`1<class BaseClass0>
		callvirt instance string class G3_C1198`1<class BaseClass0>::ClassMethod1325<object>()
		ldstr "G2_C256::ClassMethod1325.MI.7160<System.Object>()"
		ldstr "class G3_C1198`1<class BaseClass0> on type class G3_C1198`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1198`1<class BaseClass0>
		callvirt instance string class G3_C1198`1<class BaseClass0>::ClassMethod1324()
		ldstr "G2_C256::ClassMethod1324.MI.7159()"
		ldstr "class G3_C1198`1<class BaseClass0> on type class G3_C1198`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G3_C1198`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C5`1<class BaseClass1>
		callvirt instance string class G1_C5`1<class BaseClass1>::ClassMethod1325<object>()
		ldstr "G2_C256::ClassMethod1325.MI.7160<System.Object>()"
		ldstr "class G1_C5`1<class BaseClass1> on type class G3_C1198`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C5`1<class BaseClass1>
		callvirt instance string class G1_C5`1<class BaseClass1>::ClassMethod1324()
		ldstr "G2_C256::ClassMethod1324.MI.7159()"
		ldstr "class G1_C5`1<class BaseClass1> on type class G3_C1198`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C5`1<class BaseClass1>
		callvirt instance string class G1_C5`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C256::Method6.7155<System.Object>()"
		ldstr "class G1_C5`1<class BaseClass1> on type class G3_C1198`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C5`1<class BaseClass1>
		callvirt instance string class G1_C5`1<class BaseClass1>::Method5()
		ldstr "G1_C5::Method5.4803()"
		ldstr "class G1_C5`1<class BaseClass1> on type class G3_C1198`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C5`1<class BaseClass1>
		callvirt instance string class G1_C5`1<class BaseClass1>::Method4()
		ldstr "G2_C256::Method4.7153()"
		ldstr "class G1_C5`1<class BaseClass1> on type class G3_C1198`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C5`1<class BaseClass1>
		callvirt instance string class G1_C5`1<class BaseClass1>::Method7<object>()
		ldstr "G1_C5::Method7.4801<System.Object>()"
		ldstr "class G1_C5`1<class BaseClass1> on type class G3_C1198`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G3_C1198::Method7.MI.14848<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1198`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G2_C256::Method4.7153()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1198`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G2_C256::Method5.7154()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1198`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C256::Method6.MI.7156<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1198`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G3_C1198::Method7.MI.14848<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1198`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C256::Method4.7153()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1198`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C256::Method5.7154()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1198`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C256::Method6.MI.7156<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1198`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C256`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C256`2<class BaseClass1,class BaseClass0>::ClassMethod1848()
		ldstr "G3_C1198::ClassMethod1848.MI.14853()"
		ldstr "class G2_C256`2<class BaseClass1,class BaseClass0> on type class G3_C1198`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C256`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C256`2<class BaseClass1,class BaseClass0>::ClassMethod1847()
		ldstr "G2_C256::ClassMethod1847.7157()"
		ldstr "class G2_C256`2<class BaseClass1,class BaseClass0> on type class G3_C1198`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C256`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C256`2<class BaseClass1,class BaseClass0>::Method6<object>()
		ldstr "G2_C256::Method6.7155<System.Object>()"
		ldstr "class G2_C256`2<class BaseClass1,class BaseClass0> on type class G3_C1198`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C256`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C256`2<class BaseClass1,class BaseClass0>::Method5()
		ldstr "G2_C256::Method5.7154()"
		ldstr "class G2_C256`2<class BaseClass1,class BaseClass0> on type class G3_C1198`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C256`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C256`2<class BaseClass1,class BaseClass0>::Method4()
		ldstr "G2_C256::Method4.7153()"
		ldstr "class G2_C256`2<class BaseClass1,class BaseClass0> on type class G3_C1198`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C256`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C256`2<class BaseClass1,class BaseClass0>::Method3<object>()
		ldstr "G2_C256::Method3.7152<System.Object>()"
		ldstr "class G2_C256`2<class BaseClass1,class BaseClass0> on type class G3_C1198`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C256`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C256`2<class BaseClass1,class BaseClass0>::Method2<object>()
		ldstr "G2_C256::Method2.7151<System.Object>()"
		ldstr "class G2_C256`2<class BaseClass1,class BaseClass0> on type class G3_C1198`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C256`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C256`2<class BaseClass1,class BaseClass0>::Method1()
		ldstr "G2_C256::Method1.7150()"
		ldstr "class G2_C256`2<class BaseClass1,class BaseClass0> on type class G3_C1198`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C256`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C256`2<class BaseClass1,class BaseClass0>::Method0()
		ldstr "G2_C256::Method0.7149()"
		ldstr "class G2_C256`2<class BaseClass1,class BaseClass0> on type class G3_C1198`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C256`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C256`2<class BaseClass1,class BaseClass0>::ClassMethod1325<object>()
		ldstr "G2_C256::ClassMethod1325.MI.7160<System.Object>()"
		ldstr "class G2_C256`2<class BaseClass1,class BaseClass0> on type class G3_C1198`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C256`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C256`2<class BaseClass1,class BaseClass0>::ClassMethod1324()
		ldstr "G2_C256::ClassMethod1324.MI.7159()"
		ldstr "class G2_C256`2<class BaseClass1,class BaseClass0> on type class G3_C1198`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C256`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C256`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G1_C5::Method7.4801<System.Object>()"
		ldstr "class G2_C256`2<class BaseClass1,class BaseClass0> on type class G3_C1198`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string IBase0::Method0()
		ldstr "G2_C256::Method0.7149()"
		ldstr "IBase0 on type class G3_C1198`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method1()
		ldstr "G2_C256::Method1.7150()"
		ldstr "IBase0 on type class G3_C1198`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method2<object>()
		ldstr "G2_C256::Method2.7151<System.Object>()"
		ldstr "IBase0 on type class G3_C1198`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method3<object>()
		ldstr "G2_C256::Method3.7152<System.Object>()"
		ldstr "IBase0 on type class G3_C1198`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G3_C1198`1<class BaseClass1>
		callvirt instance string class G3_C1198`1<class BaseClass1>::ClassMethod3788<object>()
		ldstr "G3_C1198::ClassMethod3788.14852<System.Object>()"
		ldstr "class G3_C1198`1<class BaseClass1> on type class G3_C1198`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1198`1<class BaseClass1>
		callvirt instance string class G3_C1198`1<class BaseClass1>::ClassMethod3787<object>()
		ldstr "G3_C1198::ClassMethod3787.14851<System.Object>()"
		ldstr "class G3_C1198`1<class BaseClass1> on type class G3_C1198`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1198`1<class BaseClass1>
		callvirt instance string class G3_C1198`1<class BaseClass1>::ClassMethod3786()
		ldstr "G3_C1198::ClassMethod3786.14850()"
		ldstr "class G3_C1198`1<class BaseClass1> on type class G3_C1198`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1198`1<class BaseClass1>
		callvirt instance string class G3_C1198`1<class BaseClass1>::ClassMethod3785()
		ldstr "G3_C1198::ClassMethod3785.14849()"
		ldstr "class G3_C1198`1<class BaseClass1> on type class G3_C1198`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1198`1<class BaseClass1>
		callvirt instance string class G3_C1198`1<class BaseClass1>::Method7<object>()
		ldstr "G3_C1198::Method7.14847<System.Object>()"
		ldstr "class G3_C1198`1<class BaseClass1> on type class G3_C1198`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1198`1<class BaseClass1>
		callvirt instance string class G3_C1198`1<class BaseClass1>::ClassMethod1848()
		ldstr "G3_C1198::ClassMethod1848.MI.14853()"
		ldstr "class G3_C1198`1<class BaseClass1> on type class G3_C1198`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1198`1<class BaseClass1>
		callvirt instance string class G3_C1198`1<class BaseClass1>::ClassMethod1847()
		ldstr "G2_C256::ClassMethod1847.7157()"
		ldstr "class G3_C1198`1<class BaseClass1> on type class G3_C1198`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1198`1<class BaseClass1>
		callvirt instance string class G3_C1198`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C256::Method6.7155<System.Object>()"
		ldstr "class G3_C1198`1<class BaseClass1> on type class G3_C1198`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1198`1<class BaseClass1>
		callvirt instance string class G3_C1198`1<class BaseClass1>::Method5()
		ldstr "G2_C256::Method5.7154()"
		ldstr "class G3_C1198`1<class BaseClass1> on type class G3_C1198`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1198`1<class BaseClass1>
		callvirt instance string class G3_C1198`1<class BaseClass1>::Method4()
		ldstr "G2_C256::Method4.7153()"
		ldstr "class G3_C1198`1<class BaseClass1> on type class G3_C1198`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1198`1<class BaseClass1>
		callvirt instance string class G3_C1198`1<class BaseClass1>::Method3<object>()
		ldstr "G2_C256::Method3.7152<System.Object>()"
		ldstr "class G3_C1198`1<class BaseClass1> on type class G3_C1198`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1198`1<class BaseClass1>
		callvirt instance string class G3_C1198`1<class BaseClass1>::Method2<object>()
		ldstr "G2_C256::Method2.7151<System.Object>()"
		ldstr "class G3_C1198`1<class BaseClass1> on type class G3_C1198`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1198`1<class BaseClass1>
		callvirt instance string class G3_C1198`1<class BaseClass1>::Method1()
		ldstr "G2_C256::Method1.7150()"
		ldstr "class G3_C1198`1<class BaseClass1> on type class G3_C1198`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1198`1<class BaseClass1>
		callvirt instance string class G3_C1198`1<class BaseClass1>::Method0()
		ldstr "G2_C256::Method0.7149()"
		ldstr "class G3_C1198`1<class BaseClass1> on type class G3_C1198`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1198`1<class BaseClass1>
		callvirt instance string class G3_C1198`1<class BaseClass1>::ClassMethod1325<object>()
		ldstr "G2_C256::ClassMethod1325.MI.7160<System.Object>()"
		ldstr "class G3_C1198`1<class BaseClass1> on type class G3_C1198`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1198`1<class BaseClass1>
		callvirt instance string class G3_C1198`1<class BaseClass1>::ClassMethod1324()
		ldstr "G2_C256::ClassMethod1324.MI.7159()"
		ldstr "class G3_C1198`1<class BaseClass1> on type class G3_C1198`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G1_C5`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C5`1<class BaseClass0>
		callvirt instance string class G1_C5`1<class BaseClass0>::ClassMethod1325<object>()
		ldstr "G1_C5::ClassMethod1325.4807<System.Object>()"
		ldstr "class G1_C5`1<class BaseClass0> on type class G1_C5`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C5`1<class BaseClass0>
		callvirt instance string class G1_C5`1<class BaseClass0>::ClassMethod1324()
		ldstr "G1_C5::ClassMethod1324.4806()"
		ldstr "class G1_C5`1<class BaseClass0> on type class G1_C5`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C5`1<class BaseClass0>
		callvirt instance string class G1_C5`1<class BaseClass0>::Method6<object>()
		ldstr "G1_C5::Method6.4804<System.Object>()"
		ldstr "class G1_C5`1<class BaseClass0> on type class G1_C5`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C5`1<class BaseClass0>
		callvirt instance string class G1_C5`1<class BaseClass0>::Method5()
		ldstr "G1_C5::Method5.4803()"
		ldstr "class G1_C5`1<class BaseClass0> on type class G1_C5`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C5`1<class BaseClass0>
		callvirt instance string class G1_C5`1<class BaseClass0>::Method4()
		ldstr "G1_C5::Method4.4802()"
		ldstr "class G1_C5`1<class BaseClass0> on type class G1_C5`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C5`1<class BaseClass0>
		callvirt instance string class G1_C5`1<class BaseClass0>::Method7<object>()
		ldstr "G1_C5::Method7.4801<System.Object>()"
		ldstr "class G1_C5`1<class BaseClass0> on type class G1_C5`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C5::Method7.4801<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G1_C5`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G1_C5::Method4.4802()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C5`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G1_C5::Method5.4803()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C5`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G1_C5::Method6.MI.4805<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C5`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G1_C5::Method7.4801<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G1_C5`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G1_C5`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C5`1<class BaseClass1>
		callvirt instance string class G1_C5`1<class BaseClass1>::ClassMethod1325<object>()
		ldstr "G1_C5::ClassMethod1325.4807<System.Object>()"
		ldstr "class G1_C5`1<class BaseClass1> on type class G1_C5`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C5`1<class BaseClass1>
		callvirt instance string class G1_C5`1<class BaseClass1>::ClassMethod1324()
		ldstr "G1_C5::ClassMethod1324.4806()"
		ldstr "class G1_C5`1<class BaseClass1> on type class G1_C5`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C5`1<class BaseClass1>
		callvirt instance string class G1_C5`1<class BaseClass1>::Method6<object>()
		ldstr "G1_C5::Method6.4804<System.Object>()"
		ldstr "class G1_C5`1<class BaseClass1> on type class G1_C5`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C5`1<class BaseClass1>
		callvirt instance string class G1_C5`1<class BaseClass1>::Method5()
		ldstr "G1_C5::Method5.4803()"
		ldstr "class G1_C5`1<class BaseClass1> on type class G1_C5`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C5`1<class BaseClass1>
		callvirt instance string class G1_C5`1<class BaseClass1>::Method4()
		ldstr "G1_C5::Method4.4802()"
		ldstr "class G1_C5`1<class BaseClass1> on type class G1_C5`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C5`1<class BaseClass1>
		callvirt instance string class G1_C5`1<class BaseClass1>::Method7<object>()
		ldstr "G1_C5::Method7.4801<System.Object>()"
		ldstr "class G1_C5`1<class BaseClass1> on type class G1_C5`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C5::Method7.4801<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G1_C5`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G1_C5::Method4.4802()"
		ldstr "class IBase1`1<class BaseClass1> on type class G1_C5`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G1_C5::Method5.4803()"
		ldstr "class IBase1`1<class BaseClass1> on type class G1_C5`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G1_C5::Method6.MI.4805<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G1_C5`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G1_C5::Method7.4801<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G1_C5`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G1_C5::Method4.4802()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C5`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G1_C5::Method5.4803()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C5`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G1_C5::Method6.MI.4805<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C5`1<class BaseClass1>"
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
		newobj instance void class G3_C1198`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C256::ClassMethod1324.MI.7159()#G2_C256::ClassMethod1325.MI.7160<System.Object>()#G2_C256::Method4.7153()#G1_C5::Method5.4803()#G2_C256::Method6.7155<System.Object>()#G1_C5::Method7.4801<System.Object>()#"
		call void Generated726::M.G1_C5.T<class BaseClass1,class G3_C1198`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C256::ClassMethod1324.MI.7159()#G2_C256::ClassMethod1325.MI.7160<System.Object>()#G2_C256::Method4.7153()#G1_C5::Method5.4803()#G2_C256::Method6.7155<System.Object>()#G1_C5::Method7.4801<System.Object>()#"
		call void Generated726::M.G1_C5.B<class G3_C1198`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C5::Method7.4801<System.Object>()#"
		call void Generated726::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G3_C1198`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C5::Method7.4801<System.Object>()#"
		call void Generated726::M.IBase2.B.T<class BaseClass1,class G3_C1198`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C5::Method7.4801<System.Object>()#"
		call void Generated726::M.IBase2.B.B<class G3_C1198`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C256::Method4.7153()#G2_C256::Method5.7154()#G2_C256::Method6.MI.7156<System.Object>()#"
		call void Generated726::M.IBase1.T<class BaseClass1,class G3_C1198`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C256::Method4.7153()#G2_C256::Method5.7154()#G2_C256::Method6.MI.7156<System.Object>()#"
		call void Generated726::M.IBase1.B<class G3_C1198`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1198::Method7.MI.14848<System.Object>()#"
		call void Generated726::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G3_C1198`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1198::Method7.MI.14848<System.Object>()#"
		call void Generated726::M.IBase2.A.T<class BaseClass1,class G3_C1198`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1198::Method7.MI.14848<System.Object>()#"
		call void Generated726::M.IBase2.A.B<class G3_C1198`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C256::Method4.7153()#G2_C256::Method5.7154()#G2_C256::Method6.MI.7156<System.Object>()#"
		call void Generated726::M.IBase1.T<class BaseClass0,class G3_C1198`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C256::Method4.7153()#G2_C256::Method5.7154()#G2_C256::Method6.MI.7156<System.Object>()#"
		call void Generated726::M.IBase1.A<class G3_C1198`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C256::ClassMethod1324.MI.7159()#G2_C256::ClassMethod1325.MI.7160<System.Object>()#G2_C256::ClassMethod1847.7157()#G3_C1198::ClassMethod1848.MI.14853()#G2_C256::Method0.7149()#G2_C256::Method1.7150()#G2_C256::Method2.7151<System.Object>()#G2_C256::Method3.7152<System.Object>()#G2_C256::Method4.7153()#G2_C256::Method5.7154()#G2_C256::Method6.7155<System.Object>()#G1_C5::Method7.4801<System.Object>()#"
		call void Generated726::M.G2_C256.T.T<class BaseClass1,class BaseClass0,class G3_C1198`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C256::ClassMethod1324.MI.7159()#G2_C256::ClassMethod1325.MI.7160<System.Object>()#G2_C256::ClassMethod1847.7157()#G3_C1198::ClassMethod1848.MI.14853()#G2_C256::Method0.7149()#G2_C256::Method1.7150()#G2_C256::Method2.7151<System.Object>()#G2_C256::Method3.7152<System.Object>()#G2_C256::Method4.7153()#G2_C256::Method5.7154()#G2_C256::Method6.7155<System.Object>()#G1_C5::Method7.4801<System.Object>()#"
		call void Generated726::M.G2_C256.B.T<class BaseClass0,class G3_C1198`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C256::ClassMethod1324.MI.7159()#G2_C256::ClassMethod1325.MI.7160<System.Object>()#G2_C256::ClassMethod1847.7157()#G3_C1198::ClassMethod1848.MI.14853()#G2_C256::Method0.7149()#G2_C256::Method1.7150()#G2_C256::Method2.7151<System.Object>()#G2_C256::Method3.7152<System.Object>()#G2_C256::Method4.7153()#G2_C256::Method5.7154()#G2_C256::Method6.7155<System.Object>()#G1_C5::Method7.4801<System.Object>()#"
		call void Generated726::M.G2_C256.B.A<class G3_C1198`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C256::Method0.7149()#G2_C256::Method1.7150()#G2_C256::Method2.7151<System.Object>()#G2_C256::Method3.7152<System.Object>()#"
		call void Generated726::M.IBase0<class G3_C1198`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C256::ClassMethod1324.MI.7159()#G2_C256::ClassMethod1325.MI.7160<System.Object>()#G2_C256::ClassMethod1847.7157()#G3_C1198::ClassMethod1848.MI.14853()#G3_C1198::ClassMethod3785.14849()#G3_C1198::ClassMethod3786.14850()#G3_C1198::ClassMethod3787.14851<System.Object>()#G3_C1198::ClassMethod3788.14852<System.Object>()#G2_C256::Method0.7149()#G2_C256::Method1.7150()#G2_C256::Method2.7151<System.Object>()#G2_C256::Method3.7152<System.Object>()#G2_C256::Method4.7153()#G2_C256::Method5.7154()#G2_C256::Method6.7155<System.Object>()#G3_C1198::Method7.14847<System.Object>()#"
		call void Generated726::M.G3_C1198.T<class BaseClass0,class G3_C1198`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C256::ClassMethod1324.MI.7159()#G2_C256::ClassMethod1325.MI.7160<System.Object>()#G2_C256::ClassMethod1847.7157()#G3_C1198::ClassMethod1848.MI.14853()#G3_C1198::ClassMethod3785.14849()#G3_C1198::ClassMethod3786.14850()#G3_C1198::ClassMethod3787.14851<System.Object>()#G3_C1198::ClassMethod3788.14852<System.Object>()#G2_C256::Method0.7149()#G2_C256::Method1.7150()#G2_C256::Method2.7151<System.Object>()#G2_C256::Method3.7152<System.Object>()#G2_C256::Method4.7153()#G2_C256::Method5.7154()#G2_C256::Method6.7155<System.Object>()#G3_C1198::Method7.14847<System.Object>()#"
		call void Generated726::M.G3_C1198.A<class G3_C1198`1<class BaseClass0>>(!!0,string)
		newobj instance void class G3_C1198`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C256::ClassMethod1324.MI.7159()#G2_C256::ClassMethod1325.MI.7160<System.Object>()#G2_C256::Method4.7153()#G1_C5::Method5.4803()#G2_C256::Method6.7155<System.Object>()#G1_C5::Method7.4801<System.Object>()#"
		call void Generated726::M.G1_C5.T<class BaseClass1,class G3_C1198`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C256::ClassMethod1324.MI.7159()#G2_C256::ClassMethod1325.MI.7160<System.Object>()#G2_C256::Method4.7153()#G1_C5::Method5.4803()#G2_C256::Method6.7155<System.Object>()#G1_C5::Method7.4801<System.Object>()#"
		call void Generated726::M.G1_C5.B<class G3_C1198`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1198::Method7.MI.14848<System.Object>()#"
		call void Generated726::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G3_C1198`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1198::Method7.MI.14848<System.Object>()#"
		call void Generated726::M.IBase2.B.T<class BaseClass1,class G3_C1198`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1198::Method7.MI.14848<System.Object>()#"
		call void Generated726::M.IBase2.B.B<class G3_C1198`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C256::Method4.7153()#G2_C256::Method5.7154()#G2_C256::Method6.MI.7156<System.Object>()#"
		call void Generated726::M.IBase1.T<class BaseClass1,class G3_C1198`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C256::Method4.7153()#G2_C256::Method5.7154()#G2_C256::Method6.MI.7156<System.Object>()#"
		call void Generated726::M.IBase1.B<class G3_C1198`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1198::Method7.MI.14848<System.Object>()#"
		call void Generated726::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G3_C1198`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1198::Method7.MI.14848<System.Object>()#"
		call void Generated726::M.IBase2.A.T<class BaseClass1,class G3_C1198`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1198::Method7.MI.14848<System.Object>()#"
		call void Generated726::M.IBase2.A.B<class G3_C1198`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C256::Method4.7153()#G2_C256::Method5.7154()#G2_C256::Method6.MI.7156<System.Object>()#"
		call void Generated726::M.IBase1.T<class BaseClass0,class G3_C1198`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C256::Method4.7153()#G2_C256::Method5.7154()#G2_C256::Method6.MI.7156<System.Object>()#"
		call void Generated726::M.IBase1.A<class G3_C1198`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C256::ClassMethod1324.MI.7159()#G2_C256::ClassMethod1325.MI.7160<System.Object>()#G2_C256::ClassMethod1847.7157()#G3_C1198::ClassMethod1848.MI.14853()#G2_C256::Method0.7149()#G2_C256::Method1.7150()#G2_C256::Method2.7151<System.Object>()#G2_C256::Method3.7152<System.Object>()#G2_C256::Method4.7153()#G2_C256::Method5.7154()#G2_C256::Method6.7155<System.Object>()#G1_C5::Method7.4801<System.Object>()#"
		call void Generated726::M.G2_C256.T.T<class BaseClass1,class BaseClass0,class G3_C1198`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C256::ClassMethod1324.MI.7159()#G2_C256::ClassMethod1325.MI.7160<System.Object>()#G2_C256::ClassMethod1847.7157()#G3_C1198::ClassMethod1848.MI.14853()#G2_C256::Method0.7149()#G2_C256::Method1.7150()#G2_C256::Method2.7151<System.Object>()#G2_C256::Method3.7152<System.Object>()#G2_C256::Method4.7153()#G2_C256::Method5.7154()#G2_C256::Method6.7155<System.Object>()#G1_C5::Method7.4801<System.Object>()#"
		call void Generated726::M.G2_C256.B.T<class BaseClass0,class G3_C1198`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C256::ClassMethod1324.MI.7159()#G2_C256::ClassMethod1325.MI.7160<System.Object>()#G2_C256::ClassMethod1847.7157()#G3_C1198::ClassMethod1848.MI.14853()#G2_C256::Method0.7149()#G2_C256::Method1.7150()#G2_C256::Method2.7151<System.Object>()#G2_C256::Method3.7152<System.Object>()#G2_C256::Method4.7153()#G2_C256::Method5.7154()#G2_C256::Method6.7155<System.Object>()#G1_C5::Method7.4801<System.Object>()#"
		call void Generated726::M.G2_C256.B.A<class G3_C1198`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C256::Method0.7149()#G2_C256::Method1.7150()#G2_C256::Method2.7151<System.Object>()#G2_C256::Method3.7152<System.Object>()#"
		call void Generated726::M.IBase0<class G3_C1198`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C256::ClassMethod1324.MI.7159()#G2_C256::ClassMethod1325.MI.7160<System.Object>()#G2_C256::ClassMethod1847.7157()#G3_C1198::ClassMethod1848.MI.14853()#G3_C1198::ClassMethod3785.14849()#G3_C1198::ClassMethod3786.14850()#G3_C1198::ClassMethod3787.14851<System.Object>()#G3_C1198::ClassMethod3788.14852<System.Object>()#G2_C256::Method0.7149()#G2_C256::Method1.7150()#G2_C256::Method2.7151<System.Object>()#G2_C256::Method3.7152<System.Object>()#G2_C256::Method4.7153()#G2_C256::Method5.7154()#G2_C256::Method6.7155<System.Object>()#G3_C1198::Method7.14847<System.Object>()#"
		call void Generated726::M.G3_C1198.T<class BaseClass1,class G3_C1198`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C256::ClassMethod1324.MI.7159()#G2_C256::ClassMethod1325.MI.7160<System.Object>()#G2_C256::ClassMethod1847.7157()#G3_C1198::ClassMethod1848.MI.14853()#G3_C1198::ClassMethod3785.14849()#G3_C1198::ClassMethod3786.14850()#G3_C1198::ClassMethod3787.14851<System.Object>()#G3_C1198::ClassMethod3788.14852<System.Object>()#G2_C256::Method0.7149()#G2_C256::Method1.7150()#G2_C256::Method2.7151<System.Object>()#G2_C256::Method3.7152<System.Object>()#G2_C256::Method4.7153()#G2_C256::Method5.7154()#G2_C256::Method6.7155<System.Object>()#G3_C1198::Method7.14847<System.Object>()#"
		call void Generated726::M.G3_C1198.B<class G3_C1198`1<class BaseClass1>>(!!0,string)
		newobj instance void class G1_C5`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C5::ClassMethod1324.4806()#G1_C5::ClassMethod1325.4807<System.Object>()#G1_C5::Method4.4802()#G1_C5::Method5.4803()#G1_C5::Method6.4804<System.Object>()#G1_C5::Method7.4801<System.Object>()#"
		call void Generated726::M.G1_C5.T<class BaseClass0,class G1_C5`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C5::ClassMethod1324.4806()#G1_C5::ClassMethod1325.4807<System.Object>()#G1_C5::Method4.4802()#G1_C5::Method5.4803()#G1_C5::Method6.4804<System.Object>()#G1_C5::Method7.4801<System.Object>()#"
		call void Generated726::M.G1_C5.A<class G1_C5`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C5::Method7.4801<System.Object>()#"
		call void Generated726::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G1_C5`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C5::Method7.4801<System.Object>()#"
		call void Generated726::M.IBase2.B.T<class BaseClass1,class G1_C5`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C5::Method7.4801<System.Object>()#"
		call void Generated726::M.IBase2.B.B<class G1_C5`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C5::Method4.4802()#G1_C5::Method5.4803()#G1_C5::Method6.MI.4805<System.Object>()#"
		call void Generated726::M.IBase1.T<class BaseClass0,class G1_C5`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C5::Method4.4802()#G1_C5::Method5.4803()#G1_C5::Method6.MI.4805<System.Object>()#"
		call void Generated726::M.IBase1.A<class G1_C5`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C5::Method7.4801<System.Object>()#"
		call void Generated726::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G1_C5`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C5::Method7.4801<System.Object>()#"
		call void Generated726::M.IBase2.A.T<class BaseClass1,class G1_C5`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C5::Method7.4801<System.Object>()#"
		call void Generated726::M.IBase2.A.B<class G1_C5`1<class BaseClass0>>(!!0,string)
		newobj instance void class G1_C5`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C5::ClassMethod1324.4806()#G1_C5::ClassMethod1325.4807<System.Object>()#G1_C5::Method4.4802()#G1_C5::Method5.4803()#G1_C5::Method6.4804<System.Object>()#G1_C5::Method7.4801<System.Object>()#"
		call void Generated726::M.G1_C5.T<class BaseClass1,class G1_C5`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C5::ClassMethod1324.4806()#G1_C5::ClassMethod1325.4807<System.Object>()#G1_C5::Method4.4802()#G1_C5::Method5.4803()#G1_C5::Method6.4804<System.Object>()#G1_C5::Method7.4801<System.Object>()#"
		call void Generated726::M.G1_C5.B<class G1_C5`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C5::Method7.4801<System.Object>()#"
		call void Generated726::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G1_C5`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C5::Method7.4801<System.Object>()#"
		call void Generated726::M.IBase2.B.T<class BaseClass1,class G1_C5`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C5::Method7.4801<System.Object>()#"
		call void Generated726::M.IBase2.B.B<class G1_C5`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C5::Method4.4802()#G1_C5::Method5.4803()#G1_C5::Method6.MI.4805<System.Object>()#"
		call void Generated726::M.IBase1.T<class BaseClass1,class G1_C5`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C5::Method4.4802()#G1_C5::Method5.4803()#G1_C5::Method6.MI.4805<System.Object>()#"
		call void Generated726::M.IBase1.B<class G1_C5`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C5::Method7.4801<System.Object>()#"
		call void Generated726::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G1_C5`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C5::Method7.4801<System.Object>()#"
		call void Generated726::M.IBase2.A.T<class BaseClass1,class G1_C5`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C5::Method7.4801<System.Object>()#"
		call void Generated726::M.IBase2.A.B<class G1_C5`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C5::Method4.4802()#G1_C5::Method5.4803()#G1_C5::Method6.MI.4805<System.Object>()#"
		call void Generated726::M.IBase1.T<class BaseClass0,class G1_C5`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C5::Method4.4802()#G1_C5::Method5.4803()#G1_C5::Method6.MI.4805<System.Object>()#"
		call void Generated726::M.IBase1.A<class G1_C5`1<class BaseClass1>>(!!0,string)
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
		newobj instance void class G3_C1198`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C5`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C5`1<class BaseClass1>::ClassMethod1325<object>()
		calli default string(class G3_C1198`1<class BaseClass0>)
		ldstr "G2_C256::ClassMethod1325.MI.7160<System.Object>()"
		ldstr "class G1_C5`1<class BaseClass1> on type class G3_C1198`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C5`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C5`1<class BaseClass1>::ClassMethod1324()
		calli default string(class G3_C1198`1<class BaseClass0>)
		ldstr "G2_C256::ClassMethod1324.MI.7159()"
		ldstr "class G1_C5`1<class BaseClass1> on type class G3_C1198`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C5`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C5`1<class BaseClass1>::Method6<object>()
		calli default string(class G3_C1198`1<class BaseClass0>)
		ldstr "G2_C256::Method6.7155<System.Object>()"
		ldstr "class G1_C5`1<class BaseClass1> on type class G3_C1198`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C5`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C5`1<class BaseClass1>::Method5()
		calli default string(class G3_C1198`1<class BaseClass0>)
		ldstr "G1_C5::Method5.4803()"
		ldstr "class G1_C5`1<class BaseClass1> on type class G3_C1198`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C5`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C5`1<class BaseClass1>::Method4()
		calli default string(class G3_C1198`1<class BaseClass0>)
		ldstr "G2_C256::Method4.7153()"
		ldstr "class G1_C5`1<class BaseClass1> on type class G3_C1198`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C5`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C5`1<class BaseClass1>::Method7<object>()
		calli default string(class G3_C1198`1<class BaseClass0>)
		ldstr "G1_C5::Method7.4801<System.Object>()"
		ldstr "class G1_C5`1<class BaseClass1> on type class G3_C1198`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1198`1<class BaseClass0>)
		ldstr "G1_C5::Method7.4801<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1198`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G3_C1198`1<class BaseClass0>)
		ldstr "G2_C256::Method4.7153()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1198`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G3_C1198`1<class BaseClass0>)
		ldstr "G2_C256::Method5.7154()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1198`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G3_C1198`1<class BaseClass0>)
		ldstr "G2_C256::Method6.MI.7156<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1198`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1198`1<class BaseClass0>)
		ldstr "G3_C1198::Method7.MI.14848<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1198`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G3_C1198`1<class BaseClass0>)
		ldstr "G2_C256::Method4.7153()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1198`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G3_C1198`1<class BaseClass0>)
		ldstr "G2_C256::Method5.7154()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1198`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1198`1<class BaseClass0>)
		ldstr "G2_C256::Method6.MI.7156<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1198`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C256`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C256`2<class BaseClass1,class BaseClass0>::ClassMethod1848()
		calli default string(class G3_C1198`1<class BaseClass0>)
		ldstr "G3_C1198::ClassMethod1848.MI.14853()"
		ldstr "class G2_C256`2<class BaseClass1,class BaseClass0> on type class G3_C1198`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C256`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C256`2<class BaseClass1,class BaseClass0>::ClassMethod1847()
		calli default string(class G3_C1198`1<class BaseClass0>)
		ldstr "G2_C256::ClassMethod1847.7157()"
		ldstr "class G2_C256`2<class BaseClass1,class BaseClass0> on type class G3_C1198`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C256`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C256`2<class BaseClass1,class BaseClass0>::Method6<object>()
		calli default string(class G3_C1198`1<class BaseClass0>)
		ldstr "G2_C256::Method6.7155<System.Object>()"
		ldstr "class G2_C256`2<class BaseClass1,class BaseClass0> on type class G3_C1198`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C256`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C256`2<class BaseClass1,class BaseClass0>::Method5()
		calli default string(class G3_C1198`1<class BaseClass0>)
		ldstr "G2_C256::Method5.7154()"
		ldstr "class G2_C256`2<class BaseClass1,class BaseClass0> on type class G3_C1198`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C256`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C256`2<class BaseClass1,class BaseClass0>::Method4()
		calli default string(class G3_C1198`1<class BaseClass0>)
		ldstr "G2_C256::Method4.7153()"
		ldstr "class G2_C256`2<class BaseClass1,class BaseClass0> on type class G3_C1198`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C256`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C256`2<class BaseClass1,class BaseClass0>::Method3<object>()
		calli default string(class G3_C1198`1<class BaseClass0>)
		ldstr "G2_C256::Method3.7152<System.Object>()"
		ldstr "class G2_C256`2<class BaseClass1,class BaseClass0> on type class G3_C1198`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C256`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C256`2<class BaseClass1,class BaseClass0>::Method2<object>()
		calli default string(class G3_C1198`1<class BaseClass0>)
		ldstr "G2_C256::Method2.7151<System.Object>()"
		ldstr "class G2_C256`2<class BaseClass1,class BaseClass0> on type class G3_C1198`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C256`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C256`2<class BaseClass1,class BaseClass0>::Method1()
		calli default string(class G3_C1198`1<class BaseClass0>)
		ldstr "G2_C256::Method1.7150()"
		ldstr "class G2_C256`2<class BaseClass1,class BaseClass0> on type class G3_C1198`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C256`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C256`2<class BaseClass1,class BaseClass0>::Method0()
		calli default string(class G3_C1198`1<class BaseClass0>)
		ldstr "G2_C256::Method0.7149()"
		ldstr "class G2_C256`2<class BaseClass1,class BaseClass0> on type class G3_C1198`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C256`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C256`2<class BaseClass1,class BaseClass0>::ClassMethod1325<object>()
		calli default string(class G3_C1198`1<class BaseClass0>)
		ldstr "G2_C256::ClassMethod1325.MI.7160<System.Object>()"
		ldstr "class G2_C256`2<class BaseClass1,class BaseClass0> on type class G3_C1198`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C256`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C256`2<class BaseClass1,class BaseClass0>::ClassMethod1324()
		calli default string(class G3_C1198`1<class BaseClass0>)
		ldstr "G2_C256::ClassMethod1324.MI.7159()"
		ldstr "class G2_C256`2<class BaseClass1,class BaseClass0> on type class G3_C1198`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C256`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C256`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1198`1<class BaseClass0>)
		ldstr "G1_C5::Method7.4801<System.Object>()"
		ldstr "class G2_C256`2<class BaseClass1,class BaseClass0> on type class G3_C1198`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method0()
		calli default string(class G3_C1198`1<class BaseClass0>)
		ldstr "G2_C256::Method0.7149()"
		ldstr "IBase0 on type class G3_C1198`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method1()
		calli default string(class G3_C1198`1<class BaseClass0>)
		ldstr "G2_C256::Method1.7150()"
		ldstr "IBase0 on type class G3_C1198`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method2<object>()
		calli default string(class G3_C1198`1<class BaseClass0>)
		ldstr "G2_C256::Method2.7151<System.Object>()"
		ldstr "IBase0 on type class G3_C1198`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method3<object>()
		calli default string(class G3_C1198`1<class BaseClass0>)
		ldstr "G2_C256::Method3.7152<System.Object>()"
		ldstr "IBase0 on type class G3_C1198`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1198`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1198`1<class BaseClass0>::ClassMethod3788<object>()
		calli default string(class G3_C1198`1<class BaseClass0>)
		ldstr "G3_C1198::ClassMethod3788.14852<System.Object>()"
		ldstr "class G3_C1198`1<class BaseClass0> on type class G3_C1198`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1198`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1198`1<class BaseClass0>::ClassMethod3787<object>()
		calli default string(class G3_C1198`1<class BaseClass0>)
		ldstr "G3_C1198::ClassMethod3787.14851<System.Object>()"
		ldstr "class G3_C1198`1<class BaseClass0> on type class G3_C1198`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1198`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1198`1<class BaseClass0>::ClassMethod3786()
		calli default string(class G3_C1198`1<class BaseClass0>)
		ldstr "G3_C1198::ClassMethod3786.14850()"
		ldstr "class G3_C1198`1<class BaseClass0> on type class G3_C1198`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1198`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1198`1<class BaseClass0>::ClassMethod3785()
		calli default string(class G3_C1198`1<class BaseClass0>)
		ldstr "G3_C1198::ClassMethod3785.14849()"
		ldstr "class G3_C1198`1<class BaseClass0> on type class G3_C1198`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1198`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1198`1<class BaseClass0>::Method7<object>()
		calli default string(class G3_C1198`1<class BaseClass0>)
		ldstr "G3_C1198::Method7.14847<System.Object>()"
		ldstr "class G3_C1198`1<class BaseClass0> on type class G3_C1198`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1198`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1198`1<class BaseClass0>::ClassMethod1848()
		calli default string(class G3_C1198`1<class BaseClass0>)
		ldstr "G3_C1198::ClassMethod1848.MI.14853()"
		ldstr "class G3_C1198`1<class BaseClass0> on type class G3_C1198`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1198`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1198`1<class BaseClass0>::ClassMethod1847()
		calli default string(class G3_C1198`1<class BaseClass0>)
		ldstr "G2_C256::ClassMethod1847.7157()"
		ldstr "class G3_C1198`1<class BaseClass0> on type class G3_C1198`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1198`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1198`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1198`1<class BaseClass0>)
		ldstr "G2_C256::Method6.7155<System.Object>()"
		ldstr "class G3_C1198`1<class BaseClass0> on type class G3_C1198`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1198`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1198`1<class BaseClass0>::Method5()
		calli default string(class G3_C1198`1<class BaseClass0>)
		ldstr "G2_C256::Method5.7154()"
		ldstr "class G3_C1198`1<class BaseClass0> on type class G3_C1198`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1198`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1198`1<class BaseClass0>::Method4()
		calli default string(class G3_C1198`1<class BaseClass0>)
		ldstr "G2_C256::Method4.7153()"
		ldstr "class G3_C1198`1<class BaseClass0> on type class G3_C1198`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1198`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1198`1<class BaseClass0>::Method3<object>()
		calli default string(class G3_C1198`1<class BaseClass0>)
		ldstr "G2_C256::Method3.7152<System.Object>()"
		ldstr "class G3_C1198`1<class BaseClass0> on type class G3_C1198`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1198`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1198`1<class BaseClass0>::Method2<object>()
		calli default string(class G3_C1198`1<class BaseClass0>)
		ldstr "G2_C256::Method2.7151<System.Object>()"
		ldstr "class G3_C1198`1<class BaseClass0> on type class G3_C1198`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1198`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1198`1<class BaseClass0>::Method1()
		calli default string(class G3_C1198`1<class BaseClass0>)
		ldstr "G2_C256::Method1.7150()"
		ldstr "class G3_C1198`1<class BaseClass0> on type class G3_C1198`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1198`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1198`1<class BaseClass0>::Method0()
		calli default string(class G3_C1198`1<class BaseClass0>)
		ldstr "G2_C256::Method0.7149()"
		ldstr "class G3_C1198`1<class BaseClass0> on type class G3_C1198`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1198`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1198`1<class BaseClass0>::ClassMethod1325<object>()
		calli default string(class G3_C1198`1<class BaseClass0>)
		ldstr "G2_C256::ClassMethod1325.MI.7160<System.Object>()"
		ldstr "class G3_C1198`1<class BaseClass0> on type class G3_C1198`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1198`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1198`1<class BaseClass0>::ClassMethod1324()
		calli default string(class G3_C1198`1<class BaseClass0>)
		ldstr "G2_C256::ClassMethod1324.MI.7159()"
		ldstr "class G3_C1198`1<class BaseClass0> on type class G3_C1198`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G3_C1198`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C5`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C5`1<class BaseClass1>::ClassMethod1325<object>()
		calli default string(class G3_C1198`1<class BaseClass1>)
		ldstr "G2_C256::ClassMethod1325.MI.7160<System.Object>()"
		ldstr "class G1_C5`1<class BaseClass1> on type class G3_C1198`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C5`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C5`1<class BaseClass1>::ClassMethod1324()
		calli default string(class G3_C1198`1<class BaseClass1>)
		ldstr "G2_C256::ClassMethod1324.MI.7159()"
		ldstr "class G1_C5`1<class BaseClass1> on type class G3_C1198`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C5`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C5`1<class BaseClass1>::Method6<object>()
		calli default string(class G3_C1198`1<class BaseClass1>)
		ldstr "G2_C256::Method6.7155<System.Object>()"
		ldstr "class G1_C5`1<class BaseClass1> on type class G3_C1198`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C5`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C5`1<class BaseClass1>::Method5()
		calli default string(class G3_C1198`1<class BaseClass1>)
		ldstr "G1_C5::Method5.4803()"
		ldstr "class G1_C5`1<class BaseClass1> on type class G3_C1198`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C5`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C5`1<class BaseClass1>::Method4()
		calli default string(class G3_C1198`1<class BaseClass1>)
		ldstr "G2_C256::Method4.7153()"
		ldstr "class G1_C5`1<class BaseClass1> on type class G3_C1198`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C5`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C5`1<class BaseClass1>::Method7<object>()
		calli default string(class G3_C1198`1<class BaseClass1>)
		ldstr "G1_C5::Method7.4801<System.Object>()"
		ldstr "class G1_C5`1<class BaseClass1> on type class G3_C1198`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1198`1<class BaseClass1>)
		ldstr "G3_C1198::Method7.MI.14848<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1198`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G3_C1198`1<class BaseClass1>)
		ldstr "G2_C256::Method4.7153()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1198`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G3_C1198`1<class BaseClass1>)
		ldstr "G2_C256::Method5.7154()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1198`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G3_C1198`1<class BaseClass1>)
		ldstr "G2_C256::Method6.MI.7156<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1198`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1198`1<class BaseClass1>)
		ldstr "G3_C1198::Method7.MI.14848<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1198`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G3_C1198`1<class BaseClass1>)
		ldstr "G2_C256::Method4.7153()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1198`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G3_C1198`1<class BaseClass1>)
		ldstr "G2_C256::Method5.7154()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1198`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1198`1<class BaseClass1>)
		ldstr "G2_C256::Method6.MI.7156<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1198`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C256`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C256`2<class BaseClass1,class BaseClass0>::ClassMethod1848()
		calli default string(class G3_C1198`1<class BaseClass1>)
		ldstr "G3_C1198::ClassMethod1848.MI.14853()"
		ldstr "class G2_C256`2<class BaseClass1,class BaseClass0> on type class G3_C1198`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C256`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C256`2<class BaseClass1,class BaseClass0>::ClassMethod1847()
		calli default string(class G3_C1198`1<class BaseClass1>)
		ldstr "G2_C256::ClassMethod1847.7157()"
		ldstr "class G2_C256`2<class BaseClass1,class BaseClass0> on type class G3_C1198`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C256`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C256`2<class BaseClass1,class BaseClass0>::Method6<object>()
		calli default string(class G3_C1198`1<class BaseClass1>)
		ldstr "G2_C256::Method6.7155<System.Object>()"
		ldstr "class G2_C256`2<class BaseClass1,class BaseClass0> on type class G3_C1198`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C256`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C256`2<class BaseClass1,class BaseClass0>::Method5()
		calli default string(class G3_C1198`1<class BaseClass1>)
		ldstr "G2_C256::Method5.7154()"
		ldstr "class G2_C256`2<class BaseClass1,class BaseClass0> on type class G3_C1198`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C256`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C256`2<class BaseClass1,class BaseClass0>::Method4()
		calli default string(class G3_C1198`1<class BaseClass1>)
		ldstr "G2_C256::Method4.7153()"
		ldstr "class G2_C256`2<class BaseClass1,class BaseClass0> on type class G3_C1198`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C256`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C256`2<class BaseClass1,class BaseClass0>::Method3<object>()
		calli default string(class G3_C1198`1<class BaseClass1>)
		ldstr "G2_C256::Method3.7152<System.Object>()"
		ldstr "class G2_C256`2<class BaseClass1,class BaseClass0> on type class G3_C1198`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C256`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C256`2<class BaseClass1,class BaseClass0>::Method2<object>()
		calli default string(class G3_C1198`1<class BaseClass1>)
		ldstr "G2_C256::Method2.7151<System.Object>()"
		ldstr "class G2_C256`2<class BaseClass1,class BaseClass0> on type class G3_C1198`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C256`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C256`2<class BaseClass1,class BaseClass0>::Method1()
		calli default string(class G3_C1198`1<class BaseClass1>)
		ldstr "G2_C256::Method1.7150()"
		ldstr "class G2_C256`2<class BaseClass1,class BaseClass0> on type class G3_C1198`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C256`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C256`2<class BaseClass1,class BaseClass0>::Method0()
		calli default string(class G3_C1198`1<class BaseClass1>)
		ldstr "G2_C256::Method0.7149()"
		ldstr "class G2_C256`2<class BaseClass1,class BaseClass0> on type class G3_C1198`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C256`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C256`2<class BaseClass1,class BaseClass0>::ClassMethod1325<object>()
		calli default string(class G3_C1198`1<class BaseClass1>)
		ldstr "G2_C256::ClassMethod1325.MI.7160<System.Object>()"
		ldstr "class G2_C256`2<class BaseClass1,class BaseClass0> on type class G3_C1198`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C256`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C256`2<class BaseClass1,class BaseClass0>::ClassMethod1324()
		calli default string(class G3_C1198`1<class BaseClass1>)
		ldstr "G2_C256::ClassMethod1324.MI.7159()"
		ldstr "class G2_C256`2<class BaseClass1,class BaseClass0> on type class G3_C1198`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C256`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C256`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1198`1<class BaseClass1>)
		ldstr "G1_C5::Method7.4801<System.Object>()"
		ldstr "class G2_C256`2<class BaseClass1,class BaseClass0> on type class G3_C1198`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method0()
		calli default string(class G3_C1198`1<class BaseClass1>)
		ldstr "G2_C256::Method0.7149()"
		ldstr "IBase0 on type class G3_C1198`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method1()
		calli default string(class G3_C1198`1<class BaseClass1>)
		ldstr "G2_C256::Method1.7150()"
		ldstr "IBase0 on type class G3_C1198`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method2<object>()
		calli default string(class G3_C1198`1<class BaseClass1>)
		ldstr "G2_C256::Method2.7151<System.Object>()"
		ldstr "IBase0 on type class G3_C1198`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method3<object>()
		calli default string(class G3_C1198`1<class BaseClass1>)
		ldstr "G2_C256::Method3.7152<System.Object>()"
		ldstr "IBase0 on type class G3_C1198`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1198`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1198`1<class BaseClass1>::ClassMethod3788<object>()
		calli default string(class G3_C1198`1<class BaseClass1>)
		ldstr "G3_C1198::ClassMethod3788.14852<System.Object>()"
		ldstr "class G3_C1198`1<class BaseClass1> on type class G3_C1198`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1198`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1198`1<class BaseClass1>::ClassMethod3787<object>()
		calli default string(class G3_C1198`1<class BaseClass1>)
		ldstr "G3_C1198::ClassMethod3787.14851<System.Object>()"
		ldstr "class G3_C1198`1<class BaseClass1> on type class G3_C1198`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1198`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1198`1<class BaseClass1>::ClassMethod3786()
		calli default string(class G3_C1198`1<class BaseClass1>)
		ldstr "G3_C1198::ClassMethod3786.14850()"
		ldstr "class G3_C1198`1<class BaseClass1> on type class G3_C1198`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1198`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1198`1<class BaseClass1>::ClassMethod3785()
		calli default string(class G3_C1198`1<class BaseClass1>)
		ldstr "G3_C1198::ClassMethod3785.14849()"
		ldstr "class G3_C1198`1<class BaseClass1> on type class G3_C1198`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1198`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1198`1<class BaseClass1>::Method7<object>()
		calli default string(class G3_C1198`1<class BaseClass1>)
		ldstr "G3_C1198::Method7.14847<System.Object>()"
		ldstr "class G3_C1198`1<class BaseClass1> on type class G3_C1198`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1198`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1198`1<class BaseClass1>::ClassMethod1848()
		calli default string(class G3_C1198`1<class BaseClass1>)
		ldstr "G3_C1198::ClassMethod1848.MI.14853()"
		ldstr "class G3_C1198`1<class BaseClass1> on type class G3_C1198`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1198`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1198`1<class BaseClass1>::ClassMethod1847()
		calli default string(class G3_C1198`1<class BaseClass1>)
		ldstr "G2_C256::ClassMethod1847.7157()"
		ldstr "class G3_C1198`1<class BaseClass1> on type class G3_C1198`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1198`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1198`1<class BaseClass1>::Method6<object>()
		calli default string(class G3_C1198`1<class BaseClass1>)
		ldstr "G2_C256::Method6.7155<System.Object>()"
		ldstr "class G3_C1198`1<class BaseClass1> on type class G3_C1198`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1198`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1198`1<class BaseClass1>::Method5()
		calli default string(class G3_C1198`1<class BaseClass1>)
		ldstr "G2_C256::Method5.7154()"
		ldstr "class G3_C1198`1<class BaseClass1> on type class G3_C1198`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1198`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1198`1<class BaseClass1>::Method4()
		calli default string(class G3_C1198`1<class BaseClass1>)
		ldstr "G2_C256::Method4.7153()"
		ldstr "class G3_C1198`1<class BaseClass1> on type class G3_C1198`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1198`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1198`1<class BaseClass1>::Method3<object>()
		calli default string(class G3_C1198`1<class BaseClass1>)
		ldstr "G2_C256::Method3.7152<System.Object>()"
		ldstr "class G3_C1198`1<class BaseClass1> on type class G3_C1198`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1198`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1198`1<class BaseClass1>::Method2<object>()
		calli default string(class G3_C1198`1<class BaseClass1>)
		ldstr "G2_C256::Method2.7151<System.Object>()"
		ldstr "class G3_C1198`1<class BaseClass1> on type class G3_C1198`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1198`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1198`1<class BaseClass1>::Method1()
		calli default string(class G3_C1198`1<class BaseClass1>)
		ldstr "G2_C256::Method1.7150()"
		ldstr "class G3_C1198`1<class BaseClass1> on type class G3_C1198`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1198`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1198`1<class BaseClass1>::Method0()
		calli default string(class G3_C1198`1<class BaseClass1>)
		ldstr "G2_C256::Method0.7149()"
		ldstr "class G3_C1198`1<class BaseClass1> on type class G3_C1198`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1198`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1198`1<class BaseClass1>::ClassMethod1325<object>()
		calli default string(class G3_C1198`1<class BaseClass1>)
		ldstr "G2_C256::ClassMethod1325.MI.7160<System.Object>()"
		ldstr "class G3_C1198`1<class BaseClass1> on type class G3_C1198`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1198`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1198`1<class BaseClass1>::ClassMethod1324()
		calli default string(class G3_C1198`1<class BaseClass1>)
		ldstr "G2_C256::ClassMethod1324.MI.7159()"
		ldstr "class G3_C1198`1<class BaseClass1> on type class G3_C1198`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G1_C5`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C5`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C5`1<class BaseClass0>::ClassMethod1325<object>()
		calli default string(class G1_C5`1<class BaseClass0>)
		ldstr "G1_C5::ClassMethod1325.4807<System.Object>()"
		ldstr "class G1_C5`1<class BaseClass0> on type class G1_C5`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C5`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C5`1<class BaseClass0>::ClassMethod1324()
		calli default string(class G1_C5`1<class BaseClass0>)
		ldstr "G1_C5::ClassMethod1324.4806()"
		ldstr "class G1_C5`1<class BaseClass0> on type class G1_C5`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C5`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C5`1<class BaseClass0>::Method6<object>()
		calli default string(class G1_C5`1<class BaseClass0>)
		ldstr "G1_C5::Method6.4804<System.Object>()"
		ldstr "class G1_C5`1<class BaseClass0> on type class G1_C5`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C5`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C5`1<class BaseClass0>::Method5()
		calli default string(class G1_C5`1<class BaseClass0>)
		ldstr "G1_C5::Method5.4803()"
		ldstr "class G1_C5`1<class BaseClass0> on type class G1_C5`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C5`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C5`1<class BaseClass0>::Method4()
		calli default string(class G1_C5`1<class BaseClass0>)
		ldstr "G1_C5::Method4.4802()"
		ldstr "class G1_C5`1<class BaseClass0> on type class G1_C5`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C5`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C5`1<class BaseClass0>::Method7<object>()
		calli default string(class G1_C5`1<class BaseClass0>)
		ldstr "G1_C5::Method7.4801<System.Object>()"
		ldstr "class G1_C5`1<class BaseClass0> on type class G1_C5`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G1_C5`1<class BaseClass0>)
		ldstr "G1_C5::Method7.4801<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G1_C5`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G1_C5`1<class BaseClass0>)
		ldstr "G1_C5::Method4.4802()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C5`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G1_C5`1<class BaseClass0>)
		ldstr "G1_C5::Method5.4803()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C5`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G1_C5`1<class BaseClass0>)
		ldstr "G1_C5::Method6.MI.4805<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C5`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G1_C5`1<class BaseClass0>)
		ldstr "G1_C5::Method7.4801<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G1_C5`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G1_C5`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C5`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C5`1<class BaseClass1>::ClassMethod1325<object>()
		calli default string(class G1_C5`1<class BaseClass1>)
		ldstr "G1_C5::ClassMethod1325.4807<System.Object>()"
		ldstr "class G1_C5`1<class BaseClass1> on type class G1_C5`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C5`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C5`1<class BaseClass1>::ClassMethod1324()
		calli default string(class G1_C5`1<class BaseClass1>)
		ldstr "G1_C5::ClassMethod1324.4806()"
		ldstr "class G1_C5`1<class BaseClass1> on type class G1_C5`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C5`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C5`1<class BaseClass1>::Method6<object>()
		calli default string(class G1_C5`1<class BaseClass1>)
		ldstr "G1_C5::Method6.4804<System.Object>()"
		ldstr "class G1_C5`1<class BaseClass1> on type class G1_C5`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C5`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C5`1<class BaseClass1>::Method5()
		calli default string(class G1_C5`1<class BaseClass1>)
		ldstr "G1_C5::Method5.4803()"
		ldstr "class G1_C5`1<class BaseClass1> on type class G1_C5`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C5`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C5`1<class BaseClass1>::Method4()
		calli default string(class G1_C5`1<class BaseClass1>)
		ldstr "G1_C5::Method4.4802()"
		ldstr "class G1_C5`1<class BaseClass1> on type class G1_C5`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C5`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C5`1<class BaseClass1>::Method7<object>()
		calli default string(class G1_C5`1<class BaseClass1>)
		ldstr "G1_C5::Method7.4801<System.Object>()"
		ldstr "class G1_C5`1<class BaseClass1> on type class G1_C5`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G1_C5`1<class BaseClass1>)
		ldstr "G1_C5::Method7.4801<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G1_C5`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G1_C5`1<class BaseClass1>)
		ldstr "G1_C5::Method4.4802()"
		ldstr "class IBase1`1<class BaseClass1> on type class G1_C5`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G1_C5`1<class BaseClass1>)
		ldstr "G1_C5::Method5.4803()"
		ldstr "class IBase1`1<class BaseClass1> on type class G1_C5`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G1_C5`1<class BaseClass1>)
		ldstr "G1_C5::Method6.MI.4805<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G1_C5`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G1_C5`1<class BaseClass1>)
		ldstr "G1_C5::Method7.4801<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G1_C5`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G1_C5`1<class BaseClass1>)
		ldstr "G1_C5::Method4.4802()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C5`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G1_C5`1<class BaseClass1>)
		ldstr "G1_C5::Method5.4803()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C5`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G1_C5`1<class BaseClass1>)
		ldstr "G1_C5::Method6.MI.4805<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C5`1<class BaseClass1>"
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
		call void Generated726::MethodCallingTest()
		call void Generated726::ConstrainedCallsTest()
		call void Generated726::StructConstrainedInterfaceCallsTest()
		call void Generated726::CalliTest()
		ldc.i4 100
		ret
	}
}
