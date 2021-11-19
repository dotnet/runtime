// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) .ver 4:0:0:0 }
.assembly extern TestFramework { .publickeytoken = ( B0 3F 5F 7F 11 D5 0A 3A )  }

//TYPES IN FORWARDER ASSEMBLIES:

//TEST ASSEMBLY:
.assembly Generated648 { .hash algorithm 0x00008004 }
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
.class public G3_C1120`1<T0> 
		extends class G2_C139`1<class BaseClass1>
		implements class IBase1`1<class BaseClass1> 
{
	.method public hidebysig newslot virtual instance string Method4() cil managed noinlining { 
		ldstr "G3_C1120::Method4.14427()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class BaseClass1>.Method4'() cil managed noinlining { 
		.override method instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G3_C1120::Method4.MI.14428()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method5() cil managed noinlining { 
		ldstr "G3_C1120::Method5.14429()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G3_C1120::Method6.14430<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod3638() cil managed noinlining { 
		ldstr "G3_C1120::ClassMethod3638.14431()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod3639() cil managed noinlining { 
		ldstr "G3_C1120::ClassMethod3639.14432()"
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class G2_C139`1<class BaseClass1>::.ctor()
		ret
	}
}
.class public abstract G2_C139`1<T0> 
		extends class G1_C10`2<class BaseClass1,class BaseClass1>
		implements class IBase1`1<!T0>, class IBase2`2<class BaseClass0,!T0> 
{
	.method public hidebysig newslot virtual instance string Method4() cil managed noinlining { 
		ldstr "G2_C139::Method4.6073()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<T0>.Method4'() cil managed noinlining { 
		.override method instance string class IBase1`1<!T0>::Method4()
		ldstr "G2_C139::Method4.MI.6074()"
		ret
	}
	.method public hidebysig virtual instance string Method5() cil managed noinlining { 
		ldstr "G2_C139::Method5.6075()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G2_C139::Method6.6076<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<T0>.Method6'<M0>() cil managed noinlining { 
		.override method instance string class IBase1`1<!T0>::Method6<[1]>()
		ldstr "G2_C139::Method6.MI.6077<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G2_C139::Method7.6078<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1600() cil managed noinlining { 
		ldstr "G2_C139::ClassMethod1600.6079()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1601<M0>() cil managed noinlining { 
		ldstr "G2_C139::ClassMethod1601.6080<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1602<M0>() cil managed noinlining { 
		ldstr "G2_C139::ClassMethod1602.6081<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C10<class BaseClass1,class BaseClass1>.ClassMethod1341'() cil managed noinlining { 
		.override method instance string class G1_C10`2<class BaseClass1,class BaseClass1>::ClassMethod1341()
		ldstr "G2_C139::ClassMethod1341.MI.6082()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C10<class BaseClass1,class BaseClass1>.ClassMethod1342'<M0>() cil managed noinlining { 
		.override method instance string class G1_C10`2<class BaseClass1,class BaseClass1>::ClassMethod1342<[1]>()
		ldstr "G2_C139::ClassMethod1342.MI.6083<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class G1_C10`2<class BaseClass1,class BaseClass1>::.ctor()
		ret
	}
}
.class interface public abstract IBase1`1<+T0> 
{
	.method public hidebysig newslot abstract virtual instance string Method4() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method5() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method6<M0>() cil managed { }
}
.class public G1_C10`2<T0, T1> 
		implements class IBase2`2<class BaseClass1,class BaseClass1>, class IBase1`1<!T0> 
{
	.method public hidebysig virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G1_C10::Method7.4844<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string Method4() cil managed noinlining { 
		ldstr "G1_C10::Method4.4845()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<T0>.Method4'() cil managed noinlining { 
		.override method instance string class IBase1`1<!T0>::Method4()
		ldstr "G1_C10::Method4.MI.4846()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method5() cil managed noinlining { 
		ldstr "G1_C10::Method5.4847()"
		ret
	}
	.method public hidebysig virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G1_C10::Method6.4848<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<T0>.Method6'<M0>() cil managed noinlining { 
		.override method instance string class IBase1`1<!T0>::Method6<[1]>()
		ldstr "G1_C10::Method6.MI.4849<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1340() cil managed noinlining { 
		ldstr "G1_C10::ClassMethod1340.4850()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1341() cil managed noinlining { 
		ldstr "G1_C10::ClassMethod1341.4851()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1342<M0>() cil managed noinlining { 
		ldstr "G1_C10::ClassMethod1342.4852<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1343<M0>() cil managed noinlining { 
		ldstr "G1_C10::ClassMethod1343.4853<"
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
.class public auto ansi beforefieldinit Generated648 {
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
	.method static void M.G3_C1120.T<T0,(class G3_C1120`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 18
		.locals init (string[] actualResults)
		ldc.i4.s    13
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1120.T<T0,(class G3_C1120`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 13
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1120`1<!!T0>::ClassMethod1340()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1120`1<!!T0>::ClassMethod1341()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1120`1<!!T0>::ClassMethod1342<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1120`1<!!T0>::ClassMethod1343<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1120`1<!!T0>::ClassMethod1600()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1120`1<!!T0>::ClassMethod1601<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1120`1<!!T0>::ClassMethod1602<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1120`1<!!T0>::ClassMethod3638()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1120`1<!!T0>::ClassMethod3639()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1120`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1120`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1120`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1120`1<!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1120.A<(class G3_C1120`1<class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 18
		.locals init (string[] actualResults)
		ldc.i4.s    13
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1120.A<(class G3_C1120`1<class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 13
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1120`1<class BaseClass0>::ClassMethod1340()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1120`1<class BaseClass0>::ClassMethod1341()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1120`1<class BaseClass0>::ClassMethod1342<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1120`1<class BaseClass0>::ClassMethod1343<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1120`1<class BaseClass0>::ClassMethod1600()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1120`1<class BaseClass0>::ClassMethod1601<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1120`1<class BaseClass0>::ClassMethod1602<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1120`1<class BaseClass0>::ClassMethod3638()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1120`1<class BaseClass0>::ClassMethod3639()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1120`1<class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1120`1<class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1120`1<class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1120`1<class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1120.B<(class G3_C1120`1<class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 18
		.locals init (string[] actualResults)
		ldc.i4.s    13
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1120.B<(class G3_C1120`1<class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 13
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1120`1<class BaseClass1>::ClassMethod1340()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1120`1<class BaseClass1>::ClassMethod1341()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1120`1<class BaseClass1>::ClassMethod1342<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1120`1<class BaseClass1>::ClassMethod1343<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1120`1<class BaseClass1>::ClassMethod1600()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1120`1<class BaseClass1>::ClassMethod1601<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1120`1<class BaseClass1>::ClassMethod1602<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1120`1<class BaseClass1>::ClassMethod3638()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1120`1<class BaseClass1>::ClassMethod3639()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1120`1<class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1120`1<class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1120`1<class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1120`1<class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C139.T<T0,(class G2_C139`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C139.T<T0,(class G2_C139`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C139`1<!!T0>::ClassMethod1340()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C139`1<!!T0>::ClassMethod1341()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C139`1<!!T0>::ClassMethod1342<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C139`1<!!T0>::ClassMethod1343<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C139`1<!!T0>::ClassMethod1600()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C139`1<!!T0>::ClassMethod1601<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C139`1<!!T0>::ClassMethod1602<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C139`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C139`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C139`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C139`1<!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C139.A<(class G2_C139`1<class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C139.A<(class G2_C139`1<class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C139`1<class BaseClass0>::ClassMethod1340()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C139`1<class BaseClass0>::ClassMethod1341()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C139`1<class BaseClass0>::ClassMethod1342<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C139`1<class BaseClass0>::ClassMethod1343<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C139`1<class BaseClass0>::ClassMethod1600()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C139`1<class BaseClass0>::ClassMethod1601<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C139`1<class BaseClass0>::ClassMethod1602<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C139`1<class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C139`1<class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C139`1<class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C139`1<class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C139.B<(class G2_C139`1<class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C139.B<(class G2_C139`1<class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C139`1<class BaseClass1>::ClassMethod1340()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C139`1<class BaseClass1>::ClassMethod1341()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C139`1<class BaseClass1>::ClassMethod1342<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C139`1<class BaseClass1>::ClassMethod1343<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C139`1<class BaseClass1>::ClassMethod1600()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C139`1<class BaseClass1>::ClassMethod1601<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C139`1<class BaseClass1>::ClassMethod1602<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C139`1<class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C139`1<class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C139`1<class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C139`1<class BaseClass1>::Method7<object>()
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
	.method static void M.G1_C10.T.T<T0,T1,(class G1_C10`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C10.T.T<T0,T1,(class G1_C10`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<!!T0,!!T1>::ClassMethod1340()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<!!T0,!!T1>::ClassMethod1341()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<!!T0,!!T1>::ClassMethod1342<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<!!T0,!!T1>::ClassMethod1343<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C10.A.T<T1,(class G1_C10`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C10.A.T<T1,(class G1_C10`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass0,!!T1>::ClassMethod1340()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass0,!!T1>::ClassMethod1341()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass0,!!T1>::ClassMethod1342<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass0,!!T1>::ClassMethod1343<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C10.A.A<(class G1_C10`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C10.A.A<(class G1_C10`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass0,class BaseClass0>::ClassMethod1340()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass0,class BaseClass0>::ClassMethod1341()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass0,class BaseClass0>::ClassMethod1342<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass0,class BaseClass0>::ClassMethod1343<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass0,class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass0,class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass0,class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass0,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C10.A.B<(class G1_C10`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C10.A.B<(class G1_C10`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass0,class BaseClass1>::ClassMethod1340()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass0,class BaseClass1>::ClassMethod1341()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass0,class BaseClass1>::ClassMethod1342<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass0,class BaseClass1>::ClassMethod1343<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass0,class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass0,class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass0,class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass0,class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C10.B.T<T1,(class G1_C10`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C10.B.T<T1,(class G1_C10`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass1,!!T1>::ClassMethod1340()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass1,!!T1>::ClassMethod1341()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass1,!!T1>::ClassMethod1342<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass1,!!T1>::ClassMethod1343<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C10.B.A<(class G1_C10`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C10.B.A<(class G1_C10`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass1,class BaseClass0>::ClassMethod1340()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass1,class BaseClass0>::ClassMethod1341()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass1,class BaseClass0>::ClassMethod1342<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass1,class BaseClass0>::ClassMethod1343<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass1,class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass1,class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass1,class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass1,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C10.B.B<(class G1_C10`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C10.B.B<(class G1_C10`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass1,class BaseClass1>::ClassMethod1340()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass1,class BaseClass1>::ClassMethod1341()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass1,class BaseClass1>::ClassMethod1342<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass1,class BaseClass1>::ClassMethod1343<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass1,class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass1,class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass1,class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C10`2<class BaseClass1,class BaseClass1>::Method7<object>()
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
		newobj instance void class G3_C1120`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass1,class BaseClass1>::ClassMethod1343<object>()
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G3_C1120`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass1,class BaseClass1>::ClassMethod1342<object>()
		ldstr "G2_C139::ClassMethod1342.MI.6083<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G3_C1120`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass1,class BaseClass1>::ClassMethod1341()
		ldstr "G2_C139::ClassMethod1341.MI.6082()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G3_C1120`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass1,class BaseClass1>::ClassMethod1340()
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G3_C1120`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass1,class BaseClass1>::Method6<object>()
		ldstr "G1_C10::Method6.4848<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G3_C1120`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass1,class BaseClass1>::Method5()
		ldstr "G2_C139::Method5.6075()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G3_C1120`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass1,class BaseClass1>::Method4()
		ldstr "G1_C10::Method4.4845()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G3_C1120`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C139::Method7.6078<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G3_C1120`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C139::Method7.6078<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1120`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G3_C1120::Method4.MI.14428()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1120`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G3_C1120::Method5.14429()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1120`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G3_C1120::Method6.14430<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1120`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C139::Method7.6078<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1120`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G3_C1120::Method4.MI.14428()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1120`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G3_C1120::Method5.14429()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1120`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G3_C1120::Method6.14430<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1120`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C139`1<class BaseClass1>
		callvirt instance string class G2_C139`1<class BaseClass1>::ClassMethod1602<object>()
		ldstr "G2_C139::ClassMethod1602.6081<System.Object>()"
		ldstr "class G2_C139`1<class BaseClass1> on type class G3_C1120`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C139`1<class BaseClass1>
		callvirt instance string class G2_C139`1<class BaseClass1>::ClassMethod1601<object>()
		ldstr "G2_C139::ClassMethod1601.6080<System.Object>()"
		ldstr "class G2_C139`1<class BaseClass1> on type class G3_C1120`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C139`1<class BaseClass1>
		callvirt instance string class G2_C139`1<class BaseClass1>::ClassMethod1600()
		ldstr "G2_C139::ClassMethod1600.6079()"
		ldstr "class G2_C139`1<class BaseClass1> on type class G3_C1120`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C139`1<class BaseClass1>
		callvirt instance string class G2_C139`1<class BaseClass1>::Method7<object>()
		ldstr "G2_C139::Method7.6078<System.Object>()"
		ldstr "class G2_C139`1<class BaseClass1> on type class G3_C1120`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C139`1<class BaseClass1>
		callvirt instance string class G2_C139`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C139::Method6.6076<System.Object>()"
		ldstr "class G2_C139`1<class BaseClass1> on type class G3_C1120`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C139`1<class BaseClass1>
		callvirt instance string class G2_C139`1<class BaseClass1>::Method5()
		ldstr "G2_C139::Method5.6075()"
		ldstr "class G2_C139`1<class BaseClass1> on type class G3_C1120`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C139`1<class BaseClass1>
		callvirt instance string class G2_C139`1<class BaseClass1>::Method4()
		ldstr "G2_C139::Method4.6073()"
		ldstr "class G2_C139`1<class BaseClass1> on type class G3_C1120`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C139`1<class BaseClass1>
		callvirt instance string class G2_C139`1<class BaseClass1>::ClassMethod1343<object>()
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G2_C139`1<class BaseClass1> on type class G3_C1120`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C139`1<class BaseClass1>
		callvirt instance string class G2_C139`1<class BaseClass1>::ClassMethod1342<object>()
		ldstr "G2_C139::ClassMethod1342.MI.6083<System.Object>()"
		ldstr "class G2_C139`1<class BaseClass1> on type class G3_C1120`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C139`1<class BaseClass1>
		callvirt instance string class G2_C139`1<class BaseClass1>::ClassMethod1341()
		ldstr "G2_C139::ClassMethod1341.MI.6082()"
		ldstr "class G2_C139`1<class BaseClass1> on type class G3_C1120`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C139`1<class BaseClass1>
		callvirt instance string class G2_C139`1<class BaseClass1>::ClassMethod1340()
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G2_C139`1<class BaseClass1> on type class G3_C1120`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G3_C1120`1<class BaseClass0>
		callvirt instance string class G3_C1120`1<class BaseClass0>::ClassMethod3639()
		ldstr "G3_C1120::ClassMethod3639.14432()"
		ldstr "class G3_C1120`1<class BaseClass0> on type class G3_C1120`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1120`1<class BaseClass0>
		callvirt instance string class G3_C1120`1<class BaseClass0>::ClassMethod3638()
		ldstr "G3_C1120::ClassMethod3638.14431()"
		ldstr "class G3_C1120`1<class BaseClass0> on type class G3_C1120`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1120`1<class BaseClass0>
		callvirt instance string class G3_C1120`1<class BaseClass0>::Method6<object>()
		ldstr "G3_C1120::Method6.14430<System.Object>()"
		ldstr "class G3_C1120`1<class BaseClass0> on type class G3_C1120`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1120`1<class BaseClass0>
		callvirt instance string class G3_C1120`1<class BaseClass0>::Method5()
		ldstr "G3_C1120::Method5.14429()"
		ldstr "class G3_C1120`1<class BaseClass0> on type class G3_C1120`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1120`1<class BaseClass0>
		callvirt instance string class G3_C1120`1<class BaseClass0>::Method4()
		ldstr "G3_C1120::Method4.14427()"
		ldstr "class G3_C1120`1<class BaseClass0> on type class G3_C1120`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1120`1<class BaseClass0>
		callvirt instance string class G3_C1120`1<class BaseClass0>::ClassMethod1602<object>()
		ldstr "G2_C139::ClassMethod1602.6081<System.Object>()"
		ldstr "class G3_C1120`1<class BaseClass0> on type class G3_C1120`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1120`1<class BaseClass0>
		callvirt instance string class G3_C1120`1<class BaseClass0>::ClassMethod1601<object>()
		ldstr "G2_C139::ClassMethod1601.6080<System.Object>()"
		ldstr "class G3_C1120`1<class BaseClass0> on type class G3_C1120`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1120`1<class BaseClass0>
		callvirt instance string class G3_C1120`1<class BaseClass0>::ClassMethod1600()
		ldstr "G2_C139::ClassMethod1600.6079()"
		ldstr "class G3_C1120`1<class BaseClass0> on type class G3_C1120`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1120`1<class BaseClass0>
		callvirt instance string class G3_C1120`1<class BaseClass0>::Method7<object>()
		ldstr "G2_C139::Method7.6078<System.Object>()"
		ldstr "class G3_C1120`1<class BaseClass0> on type class G3_C1120`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1120`1<class BaseClass0>
		callvirt instance string class G3_C1120`1<class BaseClass0>::ClassMethod1343<object>()
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G3_C1120`1<class BaseClass0> on type class G3_C1120`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1120`1<class BaseClass0>
		callvirt instance string class G3_C1120`1<class BaseClass0>::ClassMethod1342<object>()
		ldstr "G2_C139::ClassMethod1342.MI.6083<System.Object>()"
		ldstr "class G3_C1120`1<class BaseClass0> on type class G3_C1120`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1120`1<class BaseClass0>
		callvirt instance string class G3_C1120`1<class BaseClass0>::ClassMethod1341()
		ldstr "G2_C139::ClassMethod1341.MI.6082()"
		ldstr "class G3_C1120`1<class BaseClass0> on type class G3_C1120`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1120`1<class BaseClass0>
		callvirt instance string class G3_C1120`1<class BaseClass0>::ClassMethod1340()
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G3_C1120`1<class BaseClass0> on type class G3_C1120`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G3_C1120`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass1,class BaseClass1>::ClassMethod1343<object>()
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G3_C1120`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass1,class BaseClass1>::ClassMethod1342<object>()
		ldstr "G2_C139::ClassMethod1342.MI.6083<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G3_C1120`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass1,class BaseClass1>::ClassMethod1341()
		ldstr "G2_C139::ClassMethod1341.MI.6082()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G3_C1120`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass1,class BaseClass1>::ClassMethod1340()
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G3_C1120`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass1,class BaseClass1>::Method6<object>()
		ldstr "G1_C10::Method6.4848<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G3_C1120`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass1,class BaseClass1>::Method5()
		ldstr "G2_C139::Method5.6075()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G3_C1120`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass1,class BaseClass1>::Method4()
		ldstr "G1_C10::Method4.4845()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G3_C1120`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C139::Method7.6078<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G3_C1120`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C139::Method7.6078<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1120`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G3_C1120::Method4.MI.14428()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1120`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G3_C1120::Method5.14429()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1120`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G3_C1120::Method6.14430<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1120`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C139::Method7.6078<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1120`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G3_C1120::Method4.MI.14428()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1120`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G3_C1120::Method5.14429()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1120`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G3_C1120::Method6.14430<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1120`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C139`1<class BaseClass1>
		callvirt instance string class G2_C139`1<class BaseClass1>::ClassMethod1602<object>()
		ldstr "G2_C139::ClassMethod1602.6081<System.Object>()"
		ldstr "class G2_C139`1<class BaseClass1> on type class G3_C1120`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C139`1<class BaseClass1>
		callvirt instance string class G2_C139`1<class BaseClass1>::ClassMethod1601<object>()
		ldstr "G2_C139::ClassMethod1601.6080<System.Object>()"
		ldstr "class G2_C139`1<class BaseClass1> on type class G3_C1120`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C139`1<class BaseClass1>
		callvirt instance string class G2_C139`1<class BaseClass1>::ClassMethod1600()
		ldstr "G2_C139::ClassMethod1600.6079()"
		ldstr "class G2_C139`1<class BaseClass1> on type class G3_C1120`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C139`1<class BaseClass1>
		callvirt instance string class G2_C139`1<class BaseClass1>::Method7<object>()
		ldstr "G2_C139::Method7.6078<System.Object>()"
		ldstr "class G2_C139`1<class BaseClass1> on type class G3_C1120`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C139`1<class BaseClass1>
		callvirt instance string class G2_C139`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C139::Method6.6076<System.Object>()"
		ldstr "class G2_C139`1<class BaseClass1> on type class G3_C1120`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C139`1<class BaseClass1>
		callvirt instance string class G2_C139`1<class BaseClass1>::Method5()
		ldstr "G2_C139::Method5.6075()"
		ldstr "class G2_C139`1<class BaseClass1> on type class G3_C1120`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C139`1<class BaseClass1>
		callvirt instance string class G2_C139`1<class BaseClass1>::Method4()
		ldstr "G2_C139::Method4.6073()"
		ldstr "class G2_C139`1<class BaseClass1> on type class G3_C1120`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C139`1<class BaseClass1>
		callvirt instance string class G2_C139`1<class BaseClass1>::ClassMethod1343<object>()
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G2_C139`1<class BaseClass1> on type class G3_C1120`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C139`1<class BaseClass1>
		callvirt instance string class G2_C139`1<class BaseClass1>::ClassMethod1342<object>()
		ldstr "G2_C139::ClassMethod1342.MI.6083<System.Object>()"
		ldstr "class G2_C139`1<class BaseClass1> on type class G3_C1120`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C139`1<class BaseClass1>
		callvirt instance string class G2_C139`1<class BaseClass1>::ClassMethod1341()
		ldstr "G2_C139::ClassMethod1341.MI.6082()"
		ldstr "class G2_C139`1<class BaseClass1> on type class G3_C1120`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C139`1<class BaseClass1>
		callvirt instance string class G2_C139`1<class BaseClass1>::ClassMethod1340()
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G2_C139`1<class BaseClass1> on type class G3_C1120`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G3_C1120`1<class BaseClass1>
		callvirt instance string class G3_C1120`1<class BaseClass1>::ClassMethod3639()
		ldstr "G3_C1120::ClassMethod3639.14432()"
		ldstr "class G3_C1120`1<class BaseClass1> on type class G3_C1120`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1120`1<class BaseClass1>
		callvirt instance string class G3_C1120`1<class BaseClass1>::ClassMethod3638()
		ldstr "G3_C1120::ClassMethod3638.14431()"
		ldstr "class G3_C1120`1<class BaseClass1> on type class G3_C1120`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1120`1<class BaseClass1>
		callvirt instance string class G3_C1120`1<class BaseClass1>::Method6<object>()
		ldstr "G3_C1120::Method6.14430<System.Object>()"
		ldstr "class G3_C1120`1<class BaseClass1> on type class G3_C1120`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1120`1<class BaseClass1>
		callvirt instance string class G3_C1120`1<class BaseClass1>::Method5()
		ldstr "G3_C1120::Method5.14429()"
		ldstr "class G3_C1120`1<class BaseClass1> on type class G3_C1120`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1120`1<class BaseClass1>
		callvirt instance string class G3_C1120`1<class BaseClass1>::Method4()
		ldstr "G3_C1120::Method4.14427()"
		ldstr "class G3_C1120`1<class BaseClass1> on type class G3_C1120`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1120`1<class BaseClass1>
		callvirt instance string class G3_C1120`1<class BaseClass1>::ClassMethod1602<object>()
		ldstr "G2_C139::ClassMethod1602.6081<System.Object>()"
		ldstr "class G3_C1120`1<class BaseClass1> on type class G3_C1120`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1120`1<class BaseClass1>
		callvirt instance string class G3_C1120`1<class BaseClass1>::ClassMethod1601<object>()
		ldstr "G2_C139::ClassMethod1601.6080<System.Object>()"
		ldstr "class G3_C1120`1<class BaseClass1> on type class G3_C1120`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1120`1<class BaseClass1>
		callvirt instance string class G3_C1120`1<class BaseClass1>::ClassMethod1600()
		ldstr "G2_C139::ClassMethod1600.6079()"
		ldstr "class G3_C1120`1<class BaseClass1> on type class G3_C1120`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1120`1<class BaseClass1>
		callvirt instance string class G3_C1120`1<class BaseClass1>::Method7<object>()
		ldstr "G2_C139::Method7.6078<System.Object>()"
		ldstr "class G3_C1120`1<class BaseClass1> on type class G3_C1120`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1120`1<class BaseClass1>
		callvirt instance string class G3_C1120`1<class BaseClass1>::ClassMethod1343<object>()
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G3_C1120`1<class BaseClass1> on type class G3_C1120`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1120`1<class BaseClass1>
		callvirt instance string class G3_C1120`1<class BaseClass1>::ClassMethod1342<object>()
		ldstr "G2_C139::ClassMethod1342.MI.6083<System.Object>()"
		ldstr "class G3_C1120`1<class BaseClass1> on type class G3_C1120`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1120`1<class BaseClass1>
		callvirt instance string class G3_C1120`1<class BaseClass1>::ClassMethod1341()
		ldstr "G2_C139::ClassMethod1341.MI.6082()"
		ldstr "class G3_C1120`1<class BaseClass1> on type class G3_C1120`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1120`1<class BaseClass1>
		callvirt instance string class G3_C1120`1<class BaseClass1>::ClassMethod1340()
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G3_C1120`1<class BaseClass1> on type class G3_C1120`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G1_C10`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C10`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C10`2<class BaseClass0,class BaseClass0>::ClassMethod1343<object>()
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass0> on type class G1_C10`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C10`2<class BaseClass0,class BaseClass0>::ClassMethod1342<object>()
		ldstr "G1_C10::ClassMethod1342.4852<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass0> on type class G1_C10`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C10`2<class BaseClass0,class BaseClass0>::ClassMethod1341()
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass0> on type class G1_C10`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C10`2<class BaseClass0,class BaseClass0>::ClassMethod1340()
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass0> on type class G1_C10`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C10`2<class BaseClass0,class BaseClass0>::Method6<object>()
		ldstr "G1_C10::Method6.4848<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass0> on type class G1_C10`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C10`2<class BaseClass0,class BaseClass0>::Method5()
		ldstr "G1_C10::Method5.4847()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass0> on type class G1_C10`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C10`2<class BaseClass0,class BaseClass0>::Method4()
		ldstr "G1_C10::Method4.4845()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass0> on type class G1_C10`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C10`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass0> on type class G1_C10`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G1_C10`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G1_C10::Method4.MI.4846()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C10`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G1_C10::Method5.4847()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C10`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G1_C10::Method6.MI.4849<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C10`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G1_C10`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G1_C10`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass0,class BaseClass1>::ClassMethod1343<object>()
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G1_C10`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass0,class BaseClass1>::ClassMethod1342<object>()
		ldstr "G1_C10::ClassMethod1342.4852<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G1_C10`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass0,class BaseClass1>::ClassMethod1341()
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G1_C10`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass0,class BaseClass1>::ClassMethod1340()
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G1_C10`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass0,class BaseClass1>::Method6<object>()
		ldstr "G1_C10::Method6.4848<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G1_C10`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass0,class BaseClass1>::Method5()
		ldstr "G1_C10::Method5.4847()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G1_C10`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass0,class BaseClass1>::Method4()
		ldstr "G1_C10::Method4.4845()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G1_C10`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G1_C10`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G1_C10`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G1_C10::Method4.MI.4846()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C10`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G1_C10::Method5.4847()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C10`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G1_C10::Method6.MI.4849<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C10`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G1_C10`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G1_C10`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C10`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C10`2<class BaseClass1,class BaseClass0>::ClassMethod1343<object>()
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass0> on type class G1_C10`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C10`2<class BaseClass1,class BaseClass0>::ClassMethod1342<object>()
		ldstr "G1_C10::ClassMethod1342.4852<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass0> on type class G1_C10`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C10`2<class BaseClass1,class BaseClass0>::ClassMethod1341()
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass0> on type class G1_C10`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C10`2<class BaseClass1,class BaseClass0>::ClassMethod1340()
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass0> on type class G1_C10`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C10`2<class BaseClass1,class BaseClass0>::Method6<object>()
		ldstr "G1_C10::Method6.4848<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass0> on type class G1_C10`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C10`2<class BaseClass1,class BaseClass0>::Method5()
		ldstr "G1_C10::Method5.4847()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass0> on type class G1_C10`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C10`2<class BaseClass1,class BaseClass0>::Method4()
		ldstr "G1_C10::Method4.4845()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass0> on type class G1_C10`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C10`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass0> on type class G1_C10`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G1_C10`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G1_C10::Method4.MI.4846()"
		ldstr "class IBase1`1<class BaseClass1> on type class G1_C10`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G1_C10::Method5.4847()"
		ldstr "class IBase1`1<class BaseClass1> on type class G1_C10`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G1_C10::Method6.MI.4849<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G1_C10`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G1_C10`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G1_C10::Method4.MI.4846()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C10`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G1_C10::Method5.4847()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C10`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G1_C10::Method6.MI.4849<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C10`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G1_C10`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass1,class BaseClass1>::ClassMethod1343<object>()
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G1_C10`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass1,class BaseClass1>::ClassMethod1342<object>()
		ldstr "G1_C10::ClassMethod1342.4852<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G1_C10`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass1,class BaseClass1>::ClassMethod1341()
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G1_C10`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass1,class BaseClass1>::ClassMethod1340()
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G1_C10`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass1,class BaseClass1>::Method6<object>()
		ldstr "G1_C10::Method6.4848<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G1_C10`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass1,class BaseClass1>::Method5()
		ldstr "G1_C10::Method5.4847()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G1_C10`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass1,class BaseClass1>::Method4()
		ldstr "G1_C10::Method4.4845()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G1_C10`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G1_C10`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G1_C10`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G1_C10::Method4.MI.4846()"
		ldstr "class IBase1`1<class BaseClass1> on type class G1_C10`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G1_C10::Method5.4847()"
		ldstr "class IBase1`1<class BaseClass1> on type class G1_C10`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G1_C10::Method6.MI.4849<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G1_C10`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G1_C10`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G1_C10::Method4.MI.4846()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C10`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G1_C10::Method5.4847()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C10`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G1_C10::Method6.MI.4849<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C10`2<class BaseClass1,class BaseClass1>"
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
		newobj instance void class G3_C1120`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G2_C139::ClassMethod1341.MI.6082()#G2_C139::ClassMethod1342.MI.6083<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G1_C10::Method4.4845()#G2_C139::Method5.6075()#G1_C10::Method6.4848<System.Object>()#G2_C139::Method7.6078<System.Object>()#"
		call void Generated648::M.G1_C10.T.T<class BaseClass1,class BaseClass1,class G3_C1120`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G2_C139::ClassMethod1341.MI.6082()#G2_C139::ClassMethod1342.MI.6083<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G1_C10::Method4.4845()#G2_C139::Method5.6075()#G1_C10::Method6.4848<System.Object>()#G2_C139::Method7.6078<System.Object>()#"
		call void Generated648::M.G1_C10.B.T<class BaseClass1,class G3_C1120`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G2_C139::ClassMethod1341.MI.6082()#G2_C139::ClassMethod1342.MI.6083<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G1_C10::Method4.4845()#G2_C139::Method5.6075()#G1_C10::Method6.4848<System.Object>()#G2_C139::Method7.6078<System.Object>()#"
		call void Generated648::M.G1_C10.B.B<class G3_C1120`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C139::Method7.6078<System.Object>()#"
		call void Generated648::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G3_C1120`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C139::Method7.6078<System.Object>()#"
		call void Generated648::M.IBase2.B.T<class BaseClass1,class G3_C1120`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C139::Method7.6078<System.Object>()#"
		call void Generated648::M.IBase2.B.B<class G3_C1120`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1120::Method4.MI.14428()#G3_C1120::Method5.14429()#G3_C1120::Method6.14430<System.Object>()#"
		call void Generated648::M.IBase1.T<class BaseClass1,class G3_C1120`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1120::Method4.MI.14428()#G3_C1120::Method5.14429()#G3_C1120::Method6.14430<System.Object>()#"
		call void Generated648::M.IBase1.B<class G3_C1120`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C139::Method7.6078<System.Object>()#"
		call void Generated648::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G3_C1120`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C139::Method7.6078<System.Object>()#"
		call void Generated648::M.IBase2.A.T<class BaseClass1,class G3_C1120`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C139::Method7.6078<System.Object>()#"
		call void Generated648::M.IBase2.A.B<class G3_C1120`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1120::Method4.MI.14428()#G3_C1120::Method5.14429()#G3_C1120::Method6.14430<System.Object>()#"
		call void Generated648::M.IBase1.T<class BaseClass0,class G3_C1120`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1120::Method4.MI.14428()#G3_C1120::Method5.14429()#G3_C1120::Method6.14430<System.Object>()#"
		call void Generated648::M.IBase1.A<class G3_C1120`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G2_C139::ClassMethod1341.MI.6082()#G2_C139::ClassMethod1342.MI.6083<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G2_C139::ClassMethod1600.6079()#G2_C139::ClassMethod1601.6080<System.Object>()#G2_C139::ClassMethod1602.6081<System.Object>()#G2_C139::Method4.6073()#G2_C139::Method5.6075()#G2_C139::Method6.6076<System.Object>()#G2_C139::Method7.6078<System.Object>()#"
		call void Generated648::M.G2_C139.T<class BaseClass1,class G3_C1120`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G2_C139::ClassMethod1341.MI.6082()#G2_C139::ClassMethod1342.MI.6083<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G2_C139::ClassMethod1600.6079()#G2_C139::ClassMethod1601.6080<System.Object>()#G2_C139::ClassMethod1602.6081<System.Object>()#G2_C139::Method4.6073()#G2_C139::Method5.6075()#G2_C139::Method6.6076<System.Object>()#G2_C139::Method7.6078<System.Object>()#"
		call void Generated648::M.G2_C139.B<class G3_C1120`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G2_C139::ClassMethod1341.MI.6082()#G2_C139::ClassMethod1342.MI.6083<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G2_C139::ClassMethod1600.6079()#G2_C139::ClassMethod1601.6080<System.Object>()#G2_C139::ClassMethod1602.6081<System.Object>()#G3_C1120::ClassMethod3638.14431()#G3_C1120::ClassMethod3639.14432()#G3_C1120::Method4.14427()#G3_C1120::Method5.14429()#G3_C1120::Method6.14430<System.Object>()#G2_C139::Method7.6078<System.Object>()#"
		call void Generated648::M.G3_C1120.T<class BaseClass0,class G3_C1120`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G2_C139::ClassMethod1341.MI.6082()#G2_C139::ClassMethod1342.MI.6083<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G2_C139::ClassMethod1600.6079()#G2_C139::ClassMethod1601.6080<System.Object>()#G2_C139::ClassMethod1602.6081<System.Object>()#G3_C1120::ClassMethod3638.14431()#G3_C1120::ClassMethod3639.14432()#G3_C1120::Method4.14427()#G3_C1120::Method5.14429()#G3_C1120::Method6.14430<System.Object>()#G2_C139::Method7.6078<System.Object>()#"
		call void Generated648::M.G3_C1120.A<class G3_C1120`1<class BaseClass0>>(!!0,string)
		newobj instance void class G3_C1120`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G2_C139::ClassMethod1341.MI.6082()#G2_C139::ClassMethod1342.MI.6083<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G1_C10::Method4.4845()#G2_C139::Method5.6075()#G1_C10::Method6.4848<System.Object>()#G2_C139::Method7.6078<System.Object>()#"
		call void Generated648::M.G1_C10.T.T<class BaseClass1,class BaseClass1,class G3_C1120`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G2_C139::ClassMethod1341.MI.6082()#G2_C139::ClassMethod1342.MI.6083<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G1_C10::Method4.4845()#G2_C139::Method5.6075()#G1_C10::Method6.4848<System.Object>()#G2_C139::Method7.6078<System.Object>()#"
		call void Generated648::M.G1_C10.B.T<class BaseClass1,class G3_C1120`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G2_C139::ClassMethod1341.MI.6082()#G2_C139::ClassMethod1342.MI.6083<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G1_C10::Method4.4845()#G2_C139::Method5.6075()#G1_C10::Method6.4848<System.Object>()#G2_C139::Method7.6078<System.Object>()#"
		call void Generated648::M.G1_C10.B.B<class G3_C1120`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C139::Method7.6078<System.Object>()#"
		call void Generated648::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G3_C1120`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C139::Method7.6078<System.Object>()#"
		call void Generated648::M.IBase2.B.T<class BaseClass1,class G3_C1120`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C139::Method7.6078<System.Object>()#"
		call void Generated648::M.IBase2.B.B<class G3_C1120`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1120::Method4.MI.14428()#G3_C1120::Method5.14429()#G3_C1120::Method6.14430<System.Object>()#"
		call void Generated648::M.IBase1.T<class BaseClass1,class G3_C1120`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1120::Method4.MI.14428()#G3_C1120::Method5.14429()#G3_C1120::Method6.14430<System.Object>()#"
		call void Generated648::M.IBase1.B<class G3_C1120`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C139::Method7.6078<System.Object>()#"
		call void Generated648::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G3_C1120`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C139::Method7.6078<System.Object>()#"
		call void Generated648::M.IBase2.A.T<class BaseClass1,class G3_C1120`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C139::Method7.6078<System.Object>()#"
		call void Generated648::M.IBase2.A.B<class G3_C1120`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1120::Method4.MI.14428()#G3_C1120::Method5.14429()#G3_C1120::Method6.14430<System.Object>()#"
		call void Generated648::M.IBase1.T<class BaseClass0,class G3_C1120`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1120::Method4.MI.14428()#G3_C1120::Method5.14429()#G3_C1120::Method6.14430<System.Object>()#"
		call void Generated648::M.IBase1.A<class G3_C1120`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G2_C139::ClassMethod1341.MI.6082()#G2_C139::ClassMethod1342.MI.6083<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G2_C139::ClassMethod1600.6079()#G2_C139::ClassMethod1601.6080<System.Object>()#G2_C139::ClassMethod1602.6081<System.Object>()#G2_C139::Method4.6073()#G2_C139::Method5.6075()#G2_C139::Method6.6076<System.Object>()#G2_C139::Method7.6078<System.Object>()#"
		call void Generated648::M.G2_C139.T<class BaseClass1,class G3_C1120`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G2_C139::ClassMethod1341.MI.6082()#G2_C139::ClassMethod1342.MI.6083<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G2_C139::ClassMethod1600.6079()#G2_C139::ClassMethod1601.6080<System.Object>()#G2_C139::ClassMethod1602.6081<System.Object>()#G2_C139::Method4.6073()#G2_C139::Method5.6075()#G2_C139::Method6.6076<System.Object>()#G2_C139::Method7.6078<System.Object>()#"
		call void Generated648::M.G2_C139.B<class G3_C1120`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G2_C139::ClassMethod1341.MI.6082()#G2_C139::ClassMethod1342.MI.6083<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G2_C139::ClassMethod1600.6079()#G2_C139::ClassMethod1601.6080<System.Object>()#G2_C139::ClassMethod1602.6081<System.Object>()#G3_C1120::ClassMethod3638.14431()#G3_C1120::ClassMethod3639.14432()#G3_C1120::Method4.14427()#G3_C1120::Method5.14429()#G3_C1120::Method6.14430<System.Object>()#G2_C139::Method7.6078<System.Object>()#"
		call void Generated648::M.G3_C1120.T<class BaseClass1,class G3_C1120`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G2_C139::ClassMethod1341.MI.6082()#G2_C139::ClassMethod1342.MI.6083<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G2_C139::ClassMethod1600.6079()#G2_C139::ClassMethod1601.6080<System.Object>()#G2_C139::ClassMethod1602.6081<System.Object>()#G3_C1120::ClassMethod3638.14431()#G3_C1120::ClassMethod3639.14432()#G3_C1120::Method4.14427()#G3_C1120::Method5.14429()#G3_C1120::Method6.14430<System.Object>()#G2_C139::Method7.6078<System.Object>()#"
		call void Generated648::M.G3_C1120.B<class G3_C1120`1<class BaseClass1>>(!!0,string)
		newobj instance void class G1_C10`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G1_C10::ClassMethod1342.4852<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G1_C10::Method4.4845()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated648::M.G1_C10.T.T<class BaseClass0,class BaseClass0,class G1_C10`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G1_C10::ClassMethod1342.4852<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G1_C10::Method4.4845()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated648::M.G1_C10.A.T<class BaseClass0,class G1_C10`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G1_C10::ClassMethod1342.4852<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G1_C10::Method4.4845()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated648::M.G1_C10.A.A<class G1_C10`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated648::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G1_C10`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated648::M.IBase2.B.T<class BaseClass1,class G1_C10`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated648::M.IBase2.B.B<class G1_C10`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method4.MI.4846()#G1_C10::Method5.4847()#G1_C10::Method6.MI.4849<System.Object>()#"
		call void Generated648::M.IBase1.T<class BaseClass0,class G1_C10`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method4.MI.4846()#G1_C10::Method5.4847()#G1_C10::Method6.MI.4849<System.Object>()#"
		call void Generated648::M.IBase1.A<class G1_C10`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated648::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G1_C10`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated648::M.IBase2.A.T<class BaseClass1,class G1_C10`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated648::M.IBase2.A.B<class G1_C10`2<class BaseClass0,class BaseClass0>>(!!0,string)
		newobj instance void class G1_C10`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G1_C10::ClassMethod1342.4852<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G1_C10::Method4.4845()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated648::M.G1_C10.T.T<class BaseClass0,class BaseClass1,class G1_C10`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G1_C10::ClassMethod1342.4852<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G1_C10::Method4.4845()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated648::M.G1_C10.A.T<class BaseClass1,class G1_C10`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G1_C10::ClassMethod1342.4852<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G1_C10::Method4.4845()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated648::M.G1_C10.A.B<class G1_C10`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated648::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G1_C10`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated648::M.IBase2.B.T<class BaseClass1,class G1_C10`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated648::M.IBase2.B.B<class G1_C10`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method4.MI.4846()#G1_C10::Method5.4847()#G1_C10::Method6.MI.4849<System.Object>()#"
		call void Generated648::M.IBase1.T<class BaseClass0,class G1_C10`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method4.MI.4846()#G1_C10::Method5.4847()#G1_C10::Method6.MI.4849<System.Object>()#"
		call void Generated648::M.IBase1.A<class G1_C10`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated648::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G1_C10`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated648::M.IBase2.A.T<class BaseClass1,class G1_C10`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated648::M.IBase2.A.B<class G1_C10`2<class BaseClass0,class BaseClass1>>(!!0,string)
		newobj instance void class G1_C10`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G1_C10::ClassMethod1342.4852<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G1_C10::Method4.4845()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated648::M.G1_C10.T.T<class BaseClass1,class BaseClass0,class G1_C10`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G1_C10::ClassMethod1342.4852<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G1_C10::Method4.4845()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated648::M.G1_C10.B.T<class BaseClass0,class G1_C10`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G1_C10::ClassMethod1342.4852<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G1_C10::Method4.4845()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated648::M.G1_C10.B.A<class G1_C10`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated648::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G1_C10`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated648::M.IBase2.B.T<class BaseClass1,class G1_C10`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated648::M.IBase2.B.B<class G1_C10`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method4.MI.4846()#G1_C10::Method5.4847()#G1_C10::Method6.MI.4849<System.Object>()#"
		call void Generated648::M.IBase1.T<class BaseClass1,class G1_C10`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method4.MI.4846()#G1_C10::Method5.4847()#G1_C10::Method6.MI.4849<System.Object>()#"
		call void Generated648::M.IBase1.B<class G1_C10`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated648::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G1_C10`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated648::M.IBase2.A.T<class BaseClass1,class G1_C10`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated648::M.IBase2.A.B<class G1_C10`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method4.MI.4846()#G1_C10::Method5.4847()#G1_C10::Method6.MI.4849<System.Object>()#"
		call void Generated648::M.IBase1.T<class BaseClass0,class G1_C10`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method4.MI.4846()#G1_C10::Method5.4847()#G1_C10::Method6.MI.4849<System.Object>()#"
		call void Generated648::M.IBase1.A<class G1_C10`2<class BaseClass1,class BaseClass0>>(!!0,string)
		newobj instance void class G1_C10`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G1_C10::ClassMethod1342.4852<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G1_C10::Method4.4845()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated648::M.G1_C10.T.T<class BaseClass1,class BaseClass1,class G1_C10`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G1_C10::ClassMethod1342.4852<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G1_C10::Method4.4845()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated648::M.G1_C10.B.T<class BaseClass1,class G1_C10`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G1_C10::ClassMethod1342.4852<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G1_C10::Method4.4845()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated648::M.G1_C10.B.B<class G1_C10`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated648::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G1_C10`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated648::M.IBase2.B.T<class BaseClass1,class G1_C10`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated648::M.IBase2.B.B<class G1_C10`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method4.MI.4846()#G1_C10::Method5.4847()#G1_C10::Method6.MI.4849<System.Object>()#"
		call void Generated648::M.IBase1.T<class BaseClass1,class G1_C10`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method4.MI.4846()#G1_C10::Method5.4847()#G1_C10::Method6.MI.4849<System.Object>()#"
		call void Generated648::M.IBase1.B<class G1_C10`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated648::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G1_C10`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated648::M.IBase2.A.T<class BaseClass1,class G1_C10`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated648::M.IBase2.A.B<class G1_C10`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method4.MI.4846()#G1_C10::Method5.4847()#G1_C10::Method6.MI.4849<System.Object>()#"
		call void Generated648::M.IBase1.T<class BaseClass0,class G1_C10`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method4.MI.4846()#G1_C10::Method5.4847()#G1_C10::Method6.MI.4849<System.Object>()#"
		call void Generated648::M.IBase1.A<class G1_C10`2<class BaseClass1,class BaseClass1>>(!!0,string)
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
		newobj instance void class G3_C1120`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass1,class BaseClass1>::ClassMethod1343<object>()
		calli default string(class G3_C1120`1<class BaseClass0>)
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G3_C1120`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass1,class BaseClass1>::ClassMethod1342<object>()
		calli default string(class G3_C1120`1<class BaseClass0>)
		ldstr "G2_C139::ClassMethod1342.MI.6083<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G3_C1120`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass1,class BaseClass1>::ClassMethod1341()
		calli default string(class G3_C1120`1<class BaseClass0>)
		ldstr "G2_C139::ClassMethod1341.MI.6082()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G3_C1120`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass1,class BaseClass1>::ClassMethod1340()
		calli default string(class G3_C1120`1<class BaseClass0>)
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G3_C1120`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass1,class BaseClass1>::Method6<object>()
		calli default string(class G3_C1120`1<class BaseClass0>)
		ldstr "G1_C10::Method6.4848<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G3_C1120`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass1,class BaseClass1>::Method5()
		calli default string(class G3_C1120`1<class BaseClass0>)
		ldstr "G2_C139::Method5.6075()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G3_C1120`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass1,class BaseClass1>::Method4()
		calli default string(class G3_C1120`1<class BaseClass0>)
		ldstr "G1_C10::Method4.4845()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G3_C1120`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1120`1<class BaseClass0>)
		ldstr "G2_C139::Method7.6078<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G3_C1120`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1120`1<class BaseClass0>)
		ldstr "G2_C139::Method7.6078<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1120`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G3_C1120`1<class BaseClass0>)
		ldstr "G3_C1120::Method4.MI.14428()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1120`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G3_C1120`1<class BaseClass0>)
		ldstr "G3_C1120::Method5.14429()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1120`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G3_C1120`1<class BaseClass0>)
		ldstr "G3_C1120::Method6.14430<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1120`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1120`1<class BaseClass0>)
		ldstr "G2_C139::Method7.6078<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1120`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G3_C1120`1<class BaseClass0>)
		ldstr "G3_C1120::Method4.MI.14428()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1120`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G3_C1120`1<class BaseClass0>)
		ldstr "G3_C1120::Method5.14429()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1120`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1120`1<class BaseClass0>)
		ldstr "G3_C1120::Method6.14430<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1120`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C139`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C139`1<class BaseClass1>::ClassMethod1602<object>()
		calli default string(class G3_C1120`1<class BaseClass0>)
		ldstr "G2_C139::ClassMethod1602.6081<System.Object>()"
		ldstr "class G2_C139`1<class BaseClass1> on type class G3_C1120`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C139`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C139`1<class BaseClass1>::ClassMethod1601<object>()
		calli default string(class G3_C1120`1<class BaseClass0>)
		ldstr "G2_C139::ClassMethod1601.6080<System.Object>()"
		ldstr "class G2_C139`1<class BaseClass1> on type class G3_C1120`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C139`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C139`1<class BaseClass1>::ClassMethod1600()
		calli default string(class G3_C1120`1<class BaseClass0>)
		ldstr "G2_C139::ClassMethod1600.6079()"
		ldstr "class G2_C139`1<class BaseClass1> on type class G3_C1120`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C139`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C139`1<class BaseClass1>::Method7<object>()
		calli default string(class G3_C1120`1<class BaseClass0>)
		ldstr "G2_C139::Method7.6078<System.Object>()"
		ldstr "class G2_C139`1<class BaseClass1> on type class G3_C1120`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C139`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C139`1<class BaseClass1>::Method6<object>()
		calli default string(class G3_C1120`1<class BaseClass0>)
		ldstr "G2_C139::Method6.6076<System.Object>()"
		ldstr "class G2_C139`1<class BaseClass1> on type class G3_C1120`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C139`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C139`1<class BaseClass1>::Method5()
		calli default string(class G3_C1120`1<class BaseClass0>)
		ldstr "G2_C139::Method5.6075()"
		ldstr "class G2_C139`1<class BaseClass1> on type class G3_C1120`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C139`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C139`1<class BaseClass1>::Method4()
		calli default string(class G3_C1120`1<class BaseClass0>)
		ldstr "G2_C139::Method4.6073()"
		ldstr "class G2_C139`1<class BaseClass1> on type class G3_C1120`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C139`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C139`1<class BaseClass1>::ClassMethod1343<object>()
		calli default string(class G3_C1120`1<class BaseClass0>)
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G2_C139`1<class BaseClass1> on type class G3_C1120`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C139`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C139`1<class BaseClass1>::ClassMethod1342<object>()
		calli default string(class G3_C1120`1<class BaseClass0>)
		ldstr "G2_C139::ClassMethod1342.MI.6083<System.Object>()"
		ldstr "class G2_C139`1<class BaseClass1> on type class G3_C1120`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C139`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C139`1<class BaseClass1>::ClassMethod1341()
		calli default string(class G3_C1120`1<class BaseClass0>)
		ldstr "G2_C139::ClassMethod1341.MI.6082()"
		ldstr "class G2_C139`1<class BaseClass1> on type class G3_C1120`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C139`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C139`1<class BaseClass1>::ClassMethod1340()
		calli default string(class G3_C1120`1<class BaseClass0>)
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G2_C139`1<class BaseClass1> on type class G3_C1120`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1120`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1120`1<class BaseClass0>::ClassMethod3639()
		calli default string(class G3_C1120`1<class BaseClass0>)
		ldstr "G3_C1120::ClassMethod3639.14432()"
		ldstr "class G3_C1120`1<class BaseClass0> on type class G3_C1120`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1120`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1120`1<class BaseClass0>::ClassMethod3638()
		calli default string(class G3_C1120`1<class BaseClass0>)
		ldstr "G3_C1120::ClassMethod3638.14431()"
		ldstr "class G3_C1120`1<class BaseClass0> on type class G3_C1120`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1120`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1120`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1120`1<class BaseClass0>)
		ldstr "G3_C1120::Method6.14430<System.Object>()"
		ldstr "class G3_C1120`1<class BaseClass0> on type class G3_C1120`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1120`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1120`1<class BaseClass0>::Method5()
		calli default string(class G3_C1120`1<class BaseClass0>)
		ldstr "G3_C1120::Method5.14429()"
		ldstr "class G3_C1120`1<class BaseClass0> on type class G3_C1120`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1120`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1120`1<class BaseClass0>::Method4()
		calli default string(class G3_C1120`1<class BaseClass0>)
		ldstr "G3_C1120::Method4.14427()"
		ldstr "class G3_C1120`1<class BaseClass0> on type class G3_C1120`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1120`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1120`1<class BaseClass0>::ClassMethod1602<object>()
		calli default string(class G3_C1120`1<class BaseClass0>)
		ldstr "G2_C139::ClassMethod1602.6081<System.Object>()"
		ldstr "class G3_C1120`1<class BaseClass0> on type class G3_C1120`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1120`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1120`1<class BaseClass0>::ClassMethod1601<object>()
		calli default string(class G3_C1120`1<class BaseClass0>)
		ldstr "G2_C139::ClassMethod1601.6080<System.Object>()"
		ldstr "class G3_C1120`1<class BaseClass0> on type class G3_C1120`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1120`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1120`1<class BaseClass0>::ClassMethod1600()
		calli default string(class G3_C1120`1<class BaseClass0>)
		ldstr "G2_C139::ClassMethod1600.6079()"
		ldstr "class G3_C1120`1<class BaseClass0> on type class G3_C1120`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1120`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1120`1<class BaseClass0>::Method7<object>()
		calli default string(class G3_C1120`1<class BaseClass0>)
		ldstr "G2_C139::Method7.6078<System.Object>()"
		ldstr "class G3_C1120`1<class BaseClass0> on type class G3_C1120`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1120`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1120`1<class BaseClass0>::ClassMethod1343<object>()
		calli default string(class G3_C1120`1<class BaseClass0>)
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G3_C1120`1<class BaseClass0> on type class G3_C1120`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1120`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1120`1<class BaseClass0>::ClassMethod1342<object>()
		calli default string(class G3_C1120`1<class BaseClass0>)
		ldstr "G2_C139::ClassMethod1342.MI.6083<System.Object>()"
		ldstr "class G3_C1120`1<class BaseClass0> on type class G3_C1120`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1120`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1120`1<class BaseClass0>::ClassMethod1341()
		calli default string(class G3_C1120`1<class BaseClass0>)
		ldstr "G2_C139::ClassMethod1341.MI.6082()"
		ldstr "class G3_C1120`1<class BaseClass0> on type class G3_C1120`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1120`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1120`1<class BaseClass0>::ClassMethod1340()
		calli default string(class G3_C1120`1<class BaseClass0>)
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G3_C1120`1<class BaseClass0> on type class G3_C1120`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G3_C1120`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass1,class BaseClass1>::ClassMethod1343<object>()
		calli default string(class G3_C1120`1<class BaseClass1>)
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G3_C1120`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass1,class BaseClass1>::ClassMethod1342<object>()
		calli default string(class G3_C1120`1<class BaseClass1>)
		ldstr "G2_C139::ClassMethod1342.MI.6083<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G3_C1120`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass1,class BaseClass1>::ClassMethod1341()
		calli default string(class G3_C1120`1<class BaseClass1>)
		ldstr "G2_C139::ClassMethod1341.MI.6082()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G3_C1120`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass1,class BaseClass1>::ClassMethod1340()
		calli default string(class G3_C1120`1<class BaseClass1>)
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G3_C1120`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass1,class BaseClass1>::Method6<object>()
		calli default string(class G3_C1120`1<class BaseClass1>)
		ldstr "G1_C10::Method6.4848<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G3_C1120`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass1,class BaseClass1>::Method5()
		calli default string(class G3_C1120`1<class BaseClass1>)
		ldstr "G2_C139::Method5.6075()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G3_C1120`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass1,class BaseClass1>::Method4()
		calli default string(class G3_C1120`1<class BaseClass1>)
		ldstr "G1_C10::Method4.4845()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G3_C1120`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1120`1<class BaseClass1>)
		ldstr "G2_C139::Method7.6078<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G3_C1120`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1120`1<class BaseClass1>)
		ldstr "G2_C139::Method7.6078<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1120`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G3_C1120`1<class BaseClass1>)
		ldstr "G3_C1120::Method4.MI.14428()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1120`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G3_C1120`1<class BaseClass1>)
		ldstr "G3_C1120::Method5.14429()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1120`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G3_C1120`1<class BaseClass1>)
		ldstr "G3_C1120::Method6.14430<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1120`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1120`1<class BaseClass1>)
		ldstr "G2_C139::Method7.6078<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1120`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G3_C1120`1<class BaseClass1>)
		ldstr "G3_C1120::Method4.MI.14428()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1120`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G3_C1120`1<class BaseClass1>)
		ldstr "G3_C1120::Method5.14429()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1120`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1120`1<class BaseClass1>)
		ldstr "G3_C1120::Method6.14430<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1120`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C139`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C139`1<class BaseClass1>::ClassMethod1602<object>()
		calli default string(class G3_C1120`1<class BaseClass1>)
		ldstr "G2_C139::ClassMethod1602.6081<System.Object>()"
		ldstr "class G2_C139`1<class BaseClass1> on type class G3_C1120`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C139`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C139`1<class BaseClass1>::ClassMethod1601<object>()
		calli default string(class G3_C1120`1<class BaseClass1>)
		ldstr "G2_C139::ClassMethod1601.6080<System.Object>()"
		ldstr "class G2_C139`1<class BaseClass1> on type class G3_C1120`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C139`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C139`1<class BaseClass1>::ClassMethod1600()
		calli default string(class G3_C1120`1<class BaseClass1>)
		ldstr "G2_C139::ClassMethod1600.6079()"
		ldstr "class G2_C139`1<class BaseClass1> on type class G3_C1120`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C139`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C139`1<class BaseClass1>::Method7<object>()
		calli default string(class G3_C1120`1<class BaseClass1>)
		ldstr "G2_C139::Method7.6078<System.Object>()"
		ldstr "class G2_C139`1<class BaseClass1> on type class G3_C1120`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C139`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C139`1<class BaseClass1>::Method6<object>()
		calli default string(class G3_C1120`1<class BaseClass1>)
		ldstr "G2_C139::Method6.6076<System.Object>()"
		ldstr "class G2_C139`1<class BaseClass1> on type class G3_C1120`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C139`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C139`1<class BaseClass1>::Method5()
		calli default string(class G3_C1120`1<class BaseClass1>)
		ldstr "G2_C139::Method5.6075()"
		ldstr "class G2_C139`1<class BaseClass1> on type class G3_C1120`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C139`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C139`1<class BaseClass1>::Method4()
		calli default string(class G3_C1120`1<class BaseClass1>)
		ldstr "G2_C139::Method4.6073()"
		ldstr "class G2_C139`1<class BaseClass1> on type class G3_C1120`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C139`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C139`1<class BaseClass1>::ClassMethod1343<object>()
		calli default string(class G3_C1120`1<class BaseClass1>)
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G2_C139`1<class BaseClass1> on type class G3_C1120`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C139`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C139`1<class BaseClass1>::ClassMethod1342<object>()
		calli default string(class G3_C1120`1<class BaseClass1>)
		ldstr "G2_C139::ClassMethod1342.MI.6083<System.Object>()"
		ldstr "class G2_C139`1<class BaseClass1> on type class G3_C1120`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C139`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C139`1<class BaseClass1>::ClassMethod1341()
		calli default string(class G3_C1120`1<class BaseClass1>)
		ldstr "G2_C139::ClassMethod1341.MI.6082()"
		ldstr "class G2_C139`1<class BaseClass1> on type class G3_C1120`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C139`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C139`1<class BaseClass1>::ClassMethod1340()
		calli default string(class G3_C1120`1<class BaseClass1>)
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G2_C139`1<class BaseClass1> on type class G3_C1120`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1120`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1120`1<class BaseClass1>::ClassMethod3639()
		calli default string(class G3_C1120`1<class BaseClass1>)
		ldstr "G3_C1120::ClassMethod3639.14432()"
		ldstr "class G3_C1120`1<class BaseClass1> on type class G3_C1120`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1120`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1120`1<class BaseClass1>::ClassMethod3638()
		calli default string(class G3_C1120`1<class BaseClass1>)
		ldstr "G3_C1120::ClassMethod3638.14431()"
		ldstr "class G3_C1120`1<class BaseClass1> on type class G3_C1120`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1120`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1120`1<class BaseClass1>::Method6<object>()
		calli default string(class G3_C1120`1<class BaseClass1>)
		ldstr "G3_C1120::Method6.14430<System.Object>()"
		ldstr "class G3_C1120`1<class BaseClass1> on type class G3_C1120`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1120`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1120`1<class BaseClass1>::Method5()
		calli default string(class G3_C1120`1<class BaseClass1>)
		ldstr "G3_C1120::Method5.14429()"
		ldstr "class G3_C1120`1<class BaseClass1> on type class G3_C1120`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1120`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1120`1<class BaseClass1>::Method4()
		calli default string(class G3_C1120`1<class BaseClass1>)
		ldstr "G3_C1120::Method4.14427()"
		ldstr "class G3_C1120`1<class BaseClass1> on type class G3_C1120`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1120`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1120`1<class BaseClass1>::ClassMethod1602<object>()
		calli default string(class G3_C1120`1<class BaseClass1>)
		ldstr "G2_C139::ClassMethod1602.6081<System.Object>()"
		ldstr "class G3_C1120`1<class BaseClass1> on type class G3_C1120`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1120`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1120`1<class BaseClass1>::ClassMethod1601<object>()
		calli default string(class G3_C1120`1<class BaseClass1>)
		ldstr "G2_C139::ClassMethod1601.6080<System.Object>()"
		ldstr "class G3_C1120`1<class BaseClass1> on type class G3_C1120`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1120`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1120`1<class BaseClass1>::ClassMethod1600()
		calli default string(class G3_C1120`1<class BaseClass1>)
		ldstr "G2_C139::ClassMethod1600.6079()"
		ldstr "class G3_C1120`1<class BaseClass1> on type class G3_C1120`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1120`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1120`1<class BaseClass1>::Method7<object>()
		calli default string(class G3_C1120`1<class BaseClass1>)
		ldstr "G2_C139::Method7.6078<System.Object>()"
		ldstr "class G3_C1120`1<class BaseClass1> on type class G3_C1120`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1120`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1120`1<class BaseClass1>::ClassMethod1343<object>()
		calli default string(class G3_C1120`1<class BaseClass1>)
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G3_C1120`1<class BaseClass1> on type class G3_C1120`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1120`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1120`1<class BaseClass1>::ClassMethod1342<object>()
		calli default string(class G3_C1120`1<class BaseClass1>)
		ldstr "G2_C139::ClassMethod1342.MI.6083<System.Object>()"
		ldstr "class G3_C1120`1<class BaseClass1> on type class G3_C1120`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1120`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1120`1<class BaseClass1>::ClassMethod1341()
		calli default string(class G3_C1120`1<class BaseClass1>)
		ldstr "G2_C139::ClassMethod1341.MI.6082()"
		ldstr "class G3_C1120`1<class BaseClass1> on type class G3_C1120`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1120`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1120`1<class BaseClass1>::ClassMethod1340()
		calli default string(class G3_C1120`1<class BaseClass1>)
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G3_C1120`1<class BaseClass1> on type class G3_C1120`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G1_C10`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C10`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass0,class BaseClass0>::ClassMethod1343<object>()
		calli default string(class G1_C10`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass0> on type class G1_C10`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass0,class BaseClass0>::ClassMethod1342<object>()
		calli default string(class G1_C10`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C10::ClassMethod1342.4852<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass0> on type class G1_C10`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass0,class BaseClass0>::ClassMethod1341()
		calli default string(class G1_C10`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass0> on type class G1_C10`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass0,class BaseClass0>::ClassMethod1340()
		calli default string(class G1_C10`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass0> on type class G1_C10`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass0,class BaseClass0>::Method6<object>()
		calli default string(class G1_C10`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C10::Method6.4848<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass0> on type class G1_C10`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass0,class BaseClass0>::Method5()
		calli default string(class G1_C10`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C10::Method5.4847()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass0> on type class G1_C10`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass0,class BaseClass0>::Method4()
		calli default string(class G1_C10`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C10::Method4.4845()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass0> on type class G1_C10`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G1_C10`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass0> on type class G1_C10`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G1_C10`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G1_C10`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G1_C10`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C10::Method4.MI.4846()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C10`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G1_C10`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C10::Method5.4847()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C10`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G1_C10`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C10::Method6.MI.4849<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C10`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G1_C10`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G1_C10`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G1_C10`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass0,class BaseClass1>::ClassMethod1343<object>()
		calli default string(class G1_C10`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G1_C10`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass0,class BaseClass1>::ClassMethod1342<object>()
		calli default string(class G1_C10`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C10::ClassMethod1342.4852<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G1_C10`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass0,class BaseClass1>::ClassMethod1341()
		calli default string(class G1_C10`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G1_C10`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass0,class BaseClass1>::ClassMethod1340()
		calli default string(class G1_C10`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G1_C10`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass0,class BaseClass1>::Method6<object>()
		calli default string(class G1_C10`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C10::Method6.4848<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G1_C10`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass0,class BaseClass1>::Method5()
		calli default string(class G1_C10`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C10::Method5.4847()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G1_C10`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass0,class BaseClass1>::Method4()
		calli default string(class G1_C10`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C10::Method4.4845()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G1_C10`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G1_C10`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G1_C10`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G1_C10`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G1_C10`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G1_C10`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C10::Method4.MI.4846()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C10`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G1_C10`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C10::Method5.4847()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C10`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G1_C10`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C10::Method6.MI.4849<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C10`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G1_C10`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G1_C10`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G1_C10`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C10`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass1,class BaseClass0>::ClassMethod1343<object>()
		calli default string(class G1_C10`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass0> on type class G1_C10`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass1,class BaseClass0>::ClassMethod1342<object>()
		calli default string(class G1_C10`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C10::ClassMethod1342.4852<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass0> on type class G1_C10`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass1,class BaseClass0>::ClassMethod1341()
		calli default string(class G1_C10`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass0> on type class G1_C10`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass1,class BaseClass0>::ClassMethod1340()
		calli default string(class G1_C10`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass0> on type class G1_C10`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass1,class BaseClass0>::Method6<object>()
		calli default string(class G1_C10`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C10::Method6.4848<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass0> on type class G1_C10`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass1,class BaseClass0>::Method5()
		calli default string(class G1_C10`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C10::Method5.4847()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass0> on type class G1_C10`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass1,class BaseClass0>::Method4()
		calli default string(class G1_C10`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C10::Method4.4845()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass0> on type class G1_C10`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G1_C10`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass0> on type class G1_C10`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G1_C10`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G1_C10`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G1_C10`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C10::Method4.MI.4846()"
		ldstr "class IBase1`1<class BaseClass1> on type class G1_C10`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G1_C10`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C10::Method5.4847()"
		ldstr "class IBase1`1<class BaseClass1> on type class G1_C10`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G1_C10`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C10::Method6.MI.4849<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G1_C10`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G1_C10`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G1_C10`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G1_C10`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C10::Method4.MI.4846()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C10`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G1_C10`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C10::Method5.4847()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C10`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G1_C10`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C10::Method6.MI.4849<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C10`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G1_C10`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass1,class BaseClass1>::ClassMethod1343<object>()
		calli default string(class G1_C10`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G1_C10`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass1,class BaseClass1>::ClassMethod1342<object>()
		calli default string(class G1_C10`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C10::ClassMethod1342.4852<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G1_C10`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass1,class BaseClass1>::ClassMethod1341()
		calli default string(class G1_C10`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G1_C10`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass1,class BaseClass1>::ClassMethod1340()
		calli default string(class G1_C10`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G1_C10`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass1,class BaseClass1>::Method6<object>()
		calli default string(class G1_C10`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C10::Method6.4848<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G1_C10`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass1,class BaseClass1>::Method5()
		calli default string(class G1_C10`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C10::Method5.4847()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G1_C10`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass1,class BaseClass1>::Method4()
		calli default string(class G1_C10`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C10::Method4.4845()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G1_C10`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G1_C10`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass1,class BaseClass1> on type class G1_C10`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G1_C10`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G1_C10`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G1_C10`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C10::Method4.MI.4846()"
		ldstr "class IBase1`1<class BaseClass1> on type class G1_C10`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G1_C10`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C10::Method5.4847()"
		ldstr "class IBase1`1<class BaseClass1> on type class G1_C10`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G1_C10`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C10::Method6.MI.4849<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G1_C10`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G1_C10`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G1_C10`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G1_C10`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C10::Method4.MI.4846()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C10`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G1_C10`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C10::Method5.4847()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C10`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G1_C10`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C10::Method6.MI.4849<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G1_C10`2<class BaseClass1,class BaseClass1>"
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
		call void Generated648::MethodCallingTest()
		call void Generated648::ConstrainedCallsTest()
		call void Generated648::StructConstrainedInterfaceCallsTest()
		call void Generated648::CalliTest()
		ldc.i4 100
		ret
	}
}
