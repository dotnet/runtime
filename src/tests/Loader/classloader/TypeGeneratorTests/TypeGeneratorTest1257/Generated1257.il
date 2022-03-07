// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) .ver 4:0:0:0 }
.assembly extern TestFramework { .publickeytoken = ( B0 3F 5F 7F 11 D5 0A 3A )  }

//TYPES IN FORWARDER ASSEMBLIES:

//TEST ASSEMBLY:
.assembly Generated1257 { .hash algorithm 0x00008004 }
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
.class public G3_C1729`1<T0> 
		extends class G2_C699`2<class BaseClass1,class BaseClass0>
		implements class IBase2`2<class BaseClass1,class BaseClass1> 
{
	.method public hidebysig virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G3_C1729::Method7.17678<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase2<class BaseClass1,class BaseClass1>.Method7'<M0>() cil managed noinlining { 
		.override method instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<[1]>()
		ldstr "G3_C1729::Method7.MI.17679<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod4857<M0>() cil managed noinlining { 
		ldstr "G3_C1729::ClassMethod4857.17680<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C699<class BaseClass1,class BaseClass0>.ClassMethod2762'() cil managed noinlining { 
		.override method instance string class G2_C699`2<class BaseClass1,class BaseClass0>::ClassMethod2762()
		ldstr "G3_C1729::ClassMethod2762.MI.17681()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C699<class BaseClass1,class BaseClass0>.ClassMethod2763'<M0>() cil managed noinlining { 
		.override method instance string class G2_C699`2<class BaseClass1,class BaseClass0>::ClassMethod2763<[1]>()
		ldstr "G3_C1729::ClassMethod2763.MI.17682<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C699<class BaseClass1,class BaseClass0>.ClassMethod2764'<M0>() cil managed noinlining { 
		.override method instance string class G2_C699`2<class BaseClass1,class BaseClass0>::ClassMethod2764<[1]>()
		ldstr "G3_C1729::ClassMethod2764.MI.17683<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class G2_C699`2<class BaseClass1,class BaseClass0>::.ctor()
		ret
	}
}
.class public G2_C699`2<T0, T1> 
		extends class G1_C13`2<class BaseClass0,!T1>
		implements class IBase1`1<!T0>, class IBase2`2<class BaseClass1,!T1> 
{
	.method public hidebysig newslot virtual instance string Method4() cil managed noinlining { 
		ldstr "G2_C699::Method4.11445()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<T0>.Method4'() cil managed noinlining { 
		.override method instance string class IBase1`1<!T0>::Method4()
		ldstr "G2_C699::Method4.MI.11446()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method5() cil managed noinlining { 
		ldstr "G2_C699::Method5.11447()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<T0>.Method5'() cil managed noinlining { 
		.override method instance string class IBase1`1<!T0>::Method5()
		ldstr "G2_C699::Method5.MI.11448()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G2_C699::Method6.11449<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G2_C699::Method7.11450<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod2761() cil managed noinlining { 
		ldstr "G2_C699::ClassMethod2761.11451()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod2762() cil managed noinlining { 
		ldstr "G2_C699::ClassMethod2762.11452()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod2763<M0>() cil managed noinlining { 
		ldstr "G2_C699::ClassMethod2763.11453<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod2764<M0>() cil managed noinlining { 
		ldstr "G2_C699::ClassMethod2764.11454<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C13<class BaseClass0,T1>.ClassMethod1349'<M0>() cil managed noinlining { 
		.override method instance string class G1_C13`2<class BaseClass0,!T1>::ClassMethod1349<[1]>()
		ldstr "G2_C699::ClassMethod1349.MI.11455<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class G1_C13`2<class BaseClass0,!T1>::.ctor()
		ret
	}
}
.class interface public abstract IBase2`2<+T0, -T1> 
{
	.method public hidebysig newslot abstract virtual instance string Method7<M0>() cil managed { }
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
.class interface public abstract IBase1`1<+T0> 
{
	.method public hidebysig newslot abstract virtual instance string Method4() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method5() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method6<M0>() cil managed { }
}
.class public auto ansi beforefieldinit Generated1257 {
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
	.method static void M.G3_C1729.T<T0,(class G3_C1729`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1729.T<T0,(class G3_C1729`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1729`1<!!T0>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1729`1<!!T0>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1729`1<!!T0>::ClassMethod2761()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1729`1<!!T0>::ClassMethod2762()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1729`1<!!T0>::ClassMethod2763<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1729`1<!!T0>::ClassMethod2764<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1729`1<!!T0>::ClassMethod4857<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1729`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1729`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1729`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1729`1<!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1729.A<(class G3_C1729`1<class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1729.A<(class G3_C1729`1<class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1729`1<class BaseClass0>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1729`1<class BaseClass0>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1729`1<class BaseClass0>::ClassMethod2761()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1729`1<class BaseClass0>::ClassMethod2762()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1729`1<class BaseClass0>::ClassMethod2763<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1729`1<class BaseClass0>::ClassMethod2764<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1729`1<class BaseClass0>::ClassMethod4857<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1729`1<class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1729`1<class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1729`1<class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1729`1<class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1729.B<(class G3_C1729`1<class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1729.B<(class G3_C1729`1<class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1729`1<class BaseClass1>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1729`1<class BaseClass1>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1729`1<class BaseClass1>::ClassMethod2761()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1729`1<class BaseClass1>::ClassMethod2762()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1729`1<class BaseClass1>::ClassMethod2763<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1729`1<class BaseClass1>::ClassMethod2764<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1729`1<class BaseClass1>::ClassMethod4857<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1729`1<class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1729`1<class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1729`1<class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1729`1<class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C699.T.T<T0,T1,(class G2_C699`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 15
		.locals init (string[] actualResults)
		ldc.i4.s    10
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C699.T.T<T0,T1,(class G2_C699`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 10
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C699`2<!!T0,!!T1>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C699`2<!!T0,!!T1>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C699`2<!!T0,!!T1>::ClassMethod2761()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C699`2<!!T0,!!T1>::ClassMethod2762()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C699`2<!!T0,!!T1>::ClassMethod2763<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C699`2<!!T0,!!T1>::ClassMethod2764<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C699`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C699`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C699`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C699`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C699.A.T<T1,(class G2_C699`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 15
		.locals init (string[] actualResults)
		ldc.i4.s    10
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C699.A.T<T1,(class G2_C699`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 10
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C699`2<class BaseClass0,!!T1>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C699`2<class BaseClass0,!!T1>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C699`2<class BaseClass0,!!T1>::ClassMethod2761()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C699`2<class BaseClass0,!!T1>::ClassMethod2762()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C699`2<class BaseClass0,!!T1>::ClassMethod2763<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C699`2<class BaseClass0,!!T1>::ClassMethod2764<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C699`2<class BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C699`2<class BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C699`2<class BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C699`2<class BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C699.A.A<(class G2_C699`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 15
		.locals init (string[] actualResults)
		ldc.i4.s    10
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C699.A.A<(class G2_C699`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 10
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C699`2<class BaseClass0,class BaseClass0>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C699`2<class BaseClass0,class BaseClass0>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C699`2<class BaseClass0,class BaseClass0>::ClassMethod2761()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C699`2<class BaseClass0,class BaseClass0>::ClassMethod2762()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C699`2<class BaseClass0,class BaseClass0>::ClassMethod2763<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C699`2<class BaseClass0,class BaseClass0>::ClassMethod2764<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C699`2<class BaseClass0,class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C699`2<class BaseClass0,class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C699`2<class BaseClass0,class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C699`2<class BaseClass0,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C699.A.B<(class G2_C699`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 15
		.locals init (string[] actualResults)
		ldc.i4.s    10
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C699.A.B<(class G2_C699`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 10
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C699`2<class BaseClass0,class BaseClass1>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C699`2<class BaseClass0,class BaseClass1>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C699`2<class BaseClass0,class BaseClass1>::ClassMethod2761()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C699`2<class BaseClass0,class BaseClass1>::ClassMethod2762()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C699`2<class BaseClass0,class BaseClass1>::ClassMethod2763<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C699`2<class BaseClass0,class BaseClass1>::ClassMethod2764<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C699`2<class BaseClass0,class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C699`2<class BaseClass0,class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C699`2<class BaseClass0,class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C699`2<class BaseClass0,class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C699.B.T<T1,(class G2_C699`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 15
		.locals init (string[] actualResults)
		ldc.i4.s    10
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C699.B.T<T1,(class G2_C699`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 10
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C699`2<class BaseClass1,!!T1>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C699`2<class BaseClass1,!!T1>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C699`2<class BaseClass1,!!T1>::ClassMethod2761()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C699`2<class BaseClass1,!!T1>::ClassMethod2762()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C699`2<class BaseClass1,!!T1>::ClassMethod2763<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C699`2<class BaseClass1,!!T1>::ClassMethod2764<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C699`2<class BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C699`2<class BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C699`2<class BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C699`2<class BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C699.B.A<(class G2_C699`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 15
		.locals init (string[] actualResults)
		ldc.i4.s    10
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C699.B.A<(class G2_C699`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 10
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C699`2<class BaseClass1,class BaseClass0>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C699`2<class BaseClass1,class BaseClass0>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C699`2<class BaseClass1,class BaseClass0>::ClassMethod2761()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C699`2<class BaseClass1,class BaseClass0>::ClassMethod2762()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C699`2<class BaseClass1,class BaseClass0>::ClassMethod2763<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C699`2<class BaseClass1,class BaseClass0>::ClassMethod2764<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C699`2<class BaseClass1,class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C699`2<class BaseClass1,class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C699`2<class BaseClass1,class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C699`2<class BaseClass1,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C699.B.B<(class G2_C699`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 15
		.locals init (string[] actualResults)
		ldc.i4.s    10
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C699.B.B<(class G2_C699`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 10
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C699`2<class BaseClass1,class BaseClass1>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C699`2<class BaseClass1,class BaseClass1>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C699`2<class BaseClass1,class BaseClass1>::ClassMethod2761()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C699`2<class BaseClass1,class BaseClass1>::ClassMethod2762()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C699`2<class BaseClass1,class BaseClass1>::ClassMethod2763<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C699`2<class BaseClass1,class BaseClass1>::ClassMethod2764<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C699`2<class BaseClass1,class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C699`2<class BaseClass1,class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C699`2<class BaseClass1,class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C699`2<class BaseClass1,class BaseClass1>::Method7<object>()
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
		newobj instance void class G3_C1729`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C13`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C13`2<class BaseClass0,class BaseClass0>::ClassMethod1349<object>()
		ldstr "G2_C699::ClassMethod1349.MI.11455<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass0> on type class G3_C1729`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C13`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C13`2<class BaseClass0,class BaseClass0>::ClassMethod1348<object>()
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass0> on type class G3_C1729`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C13`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C13`2<class BaseClass0,class BaseClass0>::Method6<object>()
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass0> on type class G3_C1729`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C13`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C13`2<class BaseClass0,class BaseClass0>::Method5()
		ldstr "G1_C13::Method5.4873()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass0> on type class G3_C1729`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C13`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C13`2<class BaseClass0,class BaseClass0>::Method4()
		ldstr "G1_C13::Method4.4872()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass0> on type class G3_C1729`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C13`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C13`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G1_C13::Method7.4871<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass0> on type class G3_C1729`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G3_C1729::Method7.17678<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1729`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C699::Method4.MI.11446()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1729`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C699::Method5.MI.11448()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1729`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C699::Method6.11449<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1729`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G3_C1729::Method7.MI.17679<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1729`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C699`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C699`2<class BaseClass1,class BaseClass0>::ClassMethod2764<object>()
		ldstr "G3_C1729::ClassMethod2764.MI.17683<System.Object>()"
		ldstr "class G2_C699`2<class BaseClass1,class BaseClass0> on type class G3_C1729`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C699`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C699`2<class BaseClass1,class BaseClass0>::ClassMethod2763<object>()
		ldstr "G3_C1729::ClassMethod2763.MI.17682<System.Object>()"
		ldstr "class G2_C699`2<class BaseClass1,class BaseClass0> on type class G3_C1729`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C699`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C699`2<class BaseClass1,class BaseClass0>::ClassMethod2762()
		ldstr "G3_C1729::ClassMethod2762.MI.17681()"
		ldstr "class G2_C699`2<class BaseClass1,class BaseClass0> on type class G3_C1729`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C699`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C699`2<class BaseClass1,class BaseClass0>::ClassMethod2761()
		ldstr "G2_C699::ClassMethod2761.11451()"
		ldstr "class G2_C699`2<class BaseClass1,class BaseClass0> on type class G3_C1729`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C699`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C699`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G3_C1729::Method7.17678<System.Object>()"
		ldstr "class G2_C699`2<class BaseClass1,class BaseClass0> on type class G3_C1729`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C699`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C699`2<class BaseClass1,class BaseClass0>::Method6<object>()
		ldstr "G2_C699::Method6.11449<System.Object>()"
		ldstr "class G2_C699`2<class BaseClass1,class BaseClass0> on type class G3_C1729`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C699`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C699`2<class BaseClass1,class BaseClass0>::Method5()
		ldstr "G2_C699::Method5.11447()"
		ldstr "class G2_C699`2<class BaseClass1,class BaseClass0> on type class G3_C1729`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C699`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C699`2<class BaseClass1,class BaseClass0>::Method4()
		ldstr "G2_C699::Method4.11445()"
		ldstr "class G2_C699`2<class BaseClass1,class BaseClass0> on type class G3_C1729`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C699`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C699`2<class BaseClass1,class BaseClass0>::ClassMethod1349<object>()
		ldstr "G2_C699::ClassMethod1349.MI.11455<System.Object>()"
		ldstr "class G2_C699`2<class BaseClass1,class BaseClass0> on type class G3_C1729`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C699`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C699`2<class BaseClass1,class BaseClass0>::ClassMethod1348<object>()
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G2_C699`2<class BaseClass1,class BaseClass0> on type class G3_C1729`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G2_C699::Method4.MI.11446()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1729`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G2_C699::Method5.MI.11448()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1729`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C699::Method6.11449<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1729`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G3_C1729::Method7.17678<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G3_C1729`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G3_C1729::Method7.MI.17679<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1729`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G3_C1729`1<class BaseClass0>
		callvirt instance string class G3_C1729`1<class BaseClass0>::ClassMethod4857<object>()
		ldstr "G3_C1729::ClassMethod4857.17680<System.Object>()"
		ldstr "class G3_C1729`1<class BaseClass0> on type class G3_C1729`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1729`1<class BaseClass0>
		callvirt instance string class G3_C1729`1<class BaseClass0>::Method7<object>()
		ldstr "G3_C1729::Method7.17678<System.Object>()"
		ldstr "class G3_C1729`1<class BaseClass0> on type class G3_C1729`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1729`1<class BaseClass0>
		callvirt instance string class G3_C1729`1<class BaseClass0>::ClassMethod2764<object>()
		ldstr "G3_C1729::ClassMethod2764.MI.17683<System.Object>()"
		ldstr "class G3_C1729`1<class BaseClass0> on type class G3_C1729`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1729`1<class BaseClass0>
		callvirt instance string class G3_C1729`1<class BaseClass0>::ClassMethod2763<object>()
		ldstr "G3_C1729::ClassMethod2763.MI.17682<System.Object>()"
		ldstr "class G3_C1729`1<class BaseClass0> on type class G3_C1729`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1729`1<class BaseClass0>
		callvirt instance string class G3_C1729`1<class BaseClass0>::ClassMethod2762()
		ldstr "G3_C1729::ClassMethod2762.MI.17681()"
		ldstr "class G3_C1729`1<class BaseClass0> on type class G3_C1729`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1729`1<class BaseClass0>
		callvirt instance string class G3_C1729`1<class BaseClass0>::ClassMethod2761()
		ldstr "G2_C699::ClassMethod2761.11451()"
		ldstr "class G3_C1729`1<class BaseClass0> on type class G3_C1729`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1729`1<class BaseClass0>
		callvirt instance string class G3_C1729`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C699::Method6.11449<System.Object>()"
		ldstr "class G3_C1729`1<class BaseClass0> on type class G3_C1729`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1729`1<class BaseClass0>
		callvirt instance string class G3_C1729`1<class BaseClass0>::Method5()
		ldstr "G2_C699::Method5.11447()"
		ldstr "class G3_C1729`1<class BaseClass0> on type class G3_C1729`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1729`1<class BaseClass0>
		callvirt instance string class G3_C1729`1<class BaseClass0>::Method4()
		ldstr "G2_C699::Method4.11445()"
		ldstr "class G3_C1729`1<class BaseClass0> on type class G3_C1729`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1729`1<class BaseClass0>
		callvirt instance string class G3_C1729`1<class BaseClass0>::ClassMethod1349<object>()
		ldstr "G2_C699::ClassMethod1349.MI.11455<System.Object>()"
		ldstr "class G3_C1729`1<class BaseClass0> on type class G3_C1729`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1729`1<class BaseClass0>
		callvirt instance string class G3_C1729`1<class BaseClass0>::ClassMethod1348<object>()
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G3_C1729`1<class BaseClass0> on type class G3_C1729`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G3_C1729`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C13`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C13`2<class BaseClass0,class BaseClass0>::ClassMethod1349<object>()
		ldstr "G2_C699::ClassMethod1349.MI.11455<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass0> on type class G3_C1729`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C13`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C13`2<class BaseClass0,class BaseClass0>::ClassMethod1348<object>()
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass0> on type class G3_C1729`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C13`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C13`2<class BaseClass0,class BaseClass0>::Method6<object>()
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass0> on type class G3_C1729`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C13`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C13`2<class BaseClass0,class BaseClass0>::Method5()
		ldstr "G1_C13::Method5.4873()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass0> on type class G3_C1729`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C13`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C13`2<class BaseClass0,class BaseClass0>::Method4()
		ldstr "G1_C13::Method4.4872()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass0> on type class G3_C1729`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C13`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C13`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G1_C13::Method7.4871<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass0> on type class G3_C1729`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G3_C1729::Method7.17678<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1729`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C699::Method4.MI.11446()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1729`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C699::Method5.MI.11448()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1729`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C699::Method6.11449<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1729`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G3_C1729::Method7.MI.17679<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1729`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C699`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C699`2<class BaseClass1,class BaseClass0>::ClassMethod2764<object>()
		ldstr "G3_C1729::ClassMethod2764.MI.17683<System.Object>()"
		ldstr "class G2_C699`2<class BaseClass1,class BaseClass0> on type class G3_C1729`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C699`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C699`2<class BaseClass1,class BaseClass0>::ClassMethod2763<object>()
		ldstr "G3_C1729::ClassMethod2763.MI.17682<System.Object>()"
		ldstr "class G2_C699`2<class BaseClass1,class BaseClass0> on type class G3_C1729`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C699`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C699`2<class BaseClass1,class BaseClass0>::ClassMethod2762()
		ldstr "G3_C1729::ClassMethod2762.MI.17681()"
		ldstr "class G2_C699`2<class BaseClass1,class BaseClass0> on type class G3_C1729`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C699`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C699`2<class BaseClass1,class BaseClass0>::ClassMethod2761()
		ldstr "G2_C699::ClassMethod2761.11451()"
		ldstr "class G2_C699`2<class BaseClass1,class BaseClass0> on type class G3_C1729`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C699`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C699`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G3_C1729::Method7.17678<System.Object>()"
		ldstr "class G2_C699`2<class BaseClass1,class BaseClass0> on type class G3_C1729`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C699`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C699`2<class BaseClass1,class BaseClass0>::Method6<object>()
		ldstr "G2_C699::Method6.11449<System.Object>()"
		ldstr "class G2_C699`2<class BaseClass1,class BaseClass0> on type class G3_C1729`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C699`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C699`2<class BaseClass1,class BaseClass0>::Method5()
		ldstr "G2_C699::Method5.11447()"
		ldstr "class G2_C699`2<class BaseClass1,class BaseClass0> on type class G3_C1729`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C699`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C699`2<class BaseClass1,class BaseClass0>::Method4()
		ldstr "G2_C699::Method4.11445()"
		ldstr "class G2_C699`2<class BaseClass1,class BaseClass0> on type class G3_C1729`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C699`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C699`2<class BaseClass1,class BaseClass0>::ClassMethod1349<object>()
		ldstr "G2_C699::ClassMethod1349.MI.11455<System.Object>()"
		ldstr "class G2_C699`2<class BaseClass1,class BaseClass0> on type class G3_C1729`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C699`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C699`2<class BaseClass1,class BaseClass0>::ClassMethod1348<object>()
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G2_C699`2<class BaseClass1,class BaseClass0> on type class G3_C1729`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G2_C699::Method4.MI.11446()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1729`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G2_C699::Method5.MI.11448()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1729`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C699::Method6.11449<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1729`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G3_C1729::Method7.17678<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G3_C1729`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G3_C1729::Method7.MI.17679<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1729`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G3_C1729`1<class BaseClass1>
		callvirt instance string class G3_C1729`1<class BaseClass1>::ClassMethod4857<object>()
		ldstr "G3_C1729::ClassMethod4857.17680<System.Object>()"
		ldstr "class G3_C1729`1<class BaseClass1> on type class G3_C1729`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1729`1<class BaseClass1>
		callvirt instance string class G3_C1729`1<class BaseClass1>::Method7<object>()
		ldstr "G3_C1729::Method7.17678<System.Object>()"
		ldstr "class G3_C1729`1<class BaseClass1> on type class G3_C1729`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1729`1<class BaseClass1>
		callvirt instance string class G3_C1729`1<class BaseClass1>::ClassMethod2764<object>()
		ldstr "G3_C1729::ClassMethod2764.MI.17683<System.Object>()"
		ldstr "class G3_C1729`1<class BaseClass1> on type class G3_C1729`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1729`1<class BaseClass1>
		callvirt instance string class G3_C1729`1<class BaseClass1>::ClassMethod2763<object>()
		ldstr "G3_C1729::ClassMethod2763.MI.17682<System.Object>()"
		ldstr "class G3_C1729`1<class BaseClass1> on type class G3_C1729`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1729`1<class BaseClass1>
		callvirt instance string class G3_C1729`1<class BaseClass1>::ClassMethod2762()
		ldstr "G3_C1729::ClassMethod2762.MI.17681()"
		ldstr "class G3_C1729`1<class BaseClass1> on type class G3_C1729`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1729`1<class BaseClass1>
		callvirt instance string class G3_C1729`1<class BaseClass1>::ClassMethod2761()
		ldstr "G2_C699::ClassMethod2761.11451()"
		ldstr "class G3_C1729`1<class BaseClass1> on type class G3_C1729`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1729`1<class BaseClass1>
		callvirt instance string class G3_C1729`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C699::Method6.11449<System.Object>()"
		ldstr "class G3_C1729`1<class BaseClass1> on type class G3_C1729`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1729`1<class BaseClass1>
		callvirt instance string class G3_C1729`1<class BaseClass1>::Method5()
		ldstr "G2_C699::Method5.11447()"
		ldstr "class G3_C1729`1<class BaseClass1> on type class G3_C1729`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1729`1<class BaseClass1>
		callvirt instance string class G3_C1729`1<class BaseClass1>::Method4()
		ldstr "G2_C699::Method4.11445()"
		ldstr "class G3_C1729`1<class BaseClass1> on type class G3_C1729`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1729`1<class BaseClass1>
		callvirt instance string class G3_C1729`1<class BaseClass1>::ClassMethod1349<object>()
		ldstr "G2_C699::ClassMethod1349.MI.11455<System.Object>()"
		ldstr "class G3_C1729`1<class BaseClass1> on type class G3_C1729`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1729`1<class BaseClass1>
		callvirt instance string class G3_C1729`1<class BaseClass1>::ClassMethod1348<object>()
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G3_C1729`1<class BaseClass1> on type class G3_C1729`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C699`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C13`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C13`2<class BaseClass0,class BaseClass0>::ClassMethod1349<object>()
		ldstr "G2_C699::ClassMethod1349.MI.11455<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass0> on type class G2_C699`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C13`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C13`2<class BaseClass0,class BaseClass0>::ClassMethod1348<object>()
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass0> on type class G2_C699`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C13`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C13`2<class BaseClass0,class BaseClass0>::Method6<object>()
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass0> on type class G2_C699`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C13`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C13`2<class BaseClass0,class BaseClass0>::Method5()
		ldstr "G1_C13::Method5.4873()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass0> on type class G2_C699`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C13`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C13`2<class BaseClass0,class BaseClass0>::Method4()
		ldstr "G1_C13::Method4.4872()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass0> on type class G2_C699`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C13`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C13`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G1_C13::Method7.4871<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass0> on type class G2_C699`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C699::Method7.11450<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C699`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C699::Method4.MI.11446()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C699`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C699::Method5.MI.11448()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C699`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C699::Method6.11449<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C699`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C699::Method7.11450<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C699`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C699`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C699`2<class BaseClass0,class BaseClass0>::ClassMethod2764<object>()
		ldstr "G2_C699::ClassMethod2764.11454<System.Object>()"
		ldstr "class G2_C699`2<class BaseClass0,class BaseClass0> on type class G2_C699`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C699`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C699`2<class BaseClass0,class BaseClass0>::ClassMethod2763<object>()
		ldstr "G2_C699::ClassMethod2763.11453<System.Object>()"
		ldstr "class G2_C699`2<class BaseClass0,class BaseClass0> on type class G2_C699`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C699`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C699`2<class BaseClass0,class BaseClass0>::ClassMethod2762()
		ldstr "G2_C699::ClassMethod2762.11452()"
		ldstr "class G2_C699`2<class BaseClass0,class BaseClass0> on type class G2_C699`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C699`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C699`2<class BaseClass0,class BaseClass0>::ClassMethod2761()
		ldstr "G2_C699::ClassMethod2761.11451()"
		ldstr "class G2_C699`2<class BaseClass0,class BaseClass0> on type class G2_C699`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C699`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C699`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C699::Method7.11450<System.Object>()"
		ldstr "class G2_C699`2<class BaseClass0,class BaseClass0> on type class G2_C699`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C699`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C699`2<class BaseClass0,class BaseClass0>::Method6<object>()
		ldstr "G2_C699::Method6.11449<System.Object>()"
		ldstr "class G2_C699`2<class BaseClass0,class BaseClass0> on type class G2_C699`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C699`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C699`2<class BaseClass0,class BaseClass0>::Method5()
		ldstr "G2_C699::Method5.11447()"
		ldstr "class G2_C699`2<class BaseClass0,class BaseClass0> on type class G2_C699`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C699`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C699`2<class BaseClass0,class BaseClass0>::Method4()
		ldstr "G2_C699::Method4.11445()"
		ldstr "class G2_C699`2<class BaseClass0,class BaseClass0> on type class G2_C699`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C699`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C699`2<class BaseClass0,class BaseClass0>::ClassMethod1349<object>()
		ldstr "G2_C699::ClassMethod1349.MI.11455<System.Object>()"
		ldstr "class G2_C699`2<class BaseClass0,class BaseClass0> on type class G2_C699`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C699`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C699`2<class BaseClass0,class BaseClass0>::ClassMethod1348<object>()
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G2_C699`2<class BaseClass0,class BaseClass0> on type class G2_C699`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G2_C699::Method7.11450<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G2_C699`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C699::Method7.11450<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C699`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C699`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C13`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C13`2<class BaseClass0,class BaseClass1>::ClassMethod1349<object>()
		ldstr "G2_C699::ClassMethod1349.MI.11455<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass1> on type class G2_C699`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C13`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C13`2<class BaseClass0,class BaseClass1>::ClassMethod1348<object>()
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass1> on type class G2_C699`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C13`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C13`2<class BaseClass0,class BaseClass1>::Method6<object>()
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass1> on type class G2_C699`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C13`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C13`2<class BaseClass0,class BaseClass1>::Method5()
		ldstr "G1_C13::Method5.4873()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass1> on type class G2_C699`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C13`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C13`2<class BaseClass0,class BaseClass1>::Method4()
		ldstr "G1_C13::Method4.4872()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass1> on type class G2_C699`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C13`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C13`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G1_C13::Method7.4871<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass1> on type class G2_C699`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C699::Method7.11450<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C699`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C699::Method4.MI.11446()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C699`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C699::Method5.MI.11448()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C699`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C699::Method6.11449<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C699`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C699`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C699`2<class BaseClass0,class BaseClass1>::ClassMethod2764<object>()
		ldstr "G2_C699::ClassMethod2764.11454<System.Object>()"
		ldstr "class G2_C699`2<class BaseClass0,class BaseClass1> on type class G2_C699`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C699`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C699`2<class BaseClass0,class BaseClass1>::ClassMethod2763<object>()
		ldstr "G2_C699::ClassMethod2763.11453<System.Object>()"
		ldstr "class G2_C699`2<class BaseClass0,class BaseClass1> on type class G2_C699`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C699`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C699`2<class BaseClass0,class BaseClass1>::ClassMethod2762()
		ldstr "G2_C699::ClassMethod2762.11452()"
		ldstr "class G2_C699`2<class BaseClass0,class BaseClass1> on type class G2_C699`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C699`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C699`2<class BaseClass0,class BaseClass1>::ClassMethod2761()
		ldstr "G2_C699::ClassMethod2761.11451()"
		ldstr "class G2_C699`2<class BaseClass0,class BaseClass1> on type class G2_C699`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C699`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C699`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C699::Method7.11450<System.Object>()"
		ldstr "class G2_C699`2<class BaseClass0,class BaseClass1> on type class G2_C699`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C699`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C699`2<class BaseClass0,class BaseClass1>::Method6<object>()
		ldstr "G2_C699::Method6.11449<System.Object>()"
		ldstr "class G2_C699`2<class BaseClass0,class BaseClass1> on type class G2_C699`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C699`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C699`2<class BaseClass0,class BaseClass1>::Method5()
		ldstr "G2_C699::Method5.11447()"
		ldstr "class G2_C699`2<class BaseClass0,class BaseClass1> on type class G2_C699`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C699`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C699`2<class BaseClass0,class BaseClass1>::Method4()
		ldstr "G2_C699::Method4.11445()"
		ldstr "class G2_C699`2<class BaseClass0,class BaseClass1> on type class G2_C699`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C699`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C699`2<class BaseClass0,class BaseClass1>::ClassMethod1349<object>()
		ldstr "G2_C699::ClassMethod1349.MI.11455<System.Object>()"
		ldstr "class G2_C699`2<class BaseClass0,class BaseClass1> on type class G2_C699`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C699`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C699`2<class BaseClass0,class BaseClass1>::ClassMethod1348<object>()
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G2_C699`2<class BaseClass0,class BaseClass1> on type class G2_C699`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C699::Method7.11450<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C699`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C699`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C13`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C13`2<class BaseClass0,class BaseClass0>::ClassMethod1349<object>()
		ldstr "G2_C699::ClassMethod1349.MI.11455<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass0> on type class G2_C699`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C13`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C13`2<class BaseClass0,class BaseClass0>::ClassMethod1348<object>()
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass0> on type class G2_C699`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C13`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C13`2<class BaseClass0,class BaseClass0>::Method6<object>()
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass0> on type class G2_C699`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C13`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C13`2<class BaseClass0,class BaseClass0>::Method5()
		ldstr "G1_C13::Method5.4873()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass0> on type class G2_C699`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C13`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C13`2<class BaseClass0,class BaseClass0>::Method4()
		ldstr "G1_C13::Method4.4872()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass0> on type class G2_C699`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C13`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C13`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G1_C13::Method7.4871<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass0> on type class G2_C699`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C699::Method7.11450<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C699`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C699::Method4.MI.11446()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C699`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C699::Method5.MI.11448()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C699`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C699::Method6.11449<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C699`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C699::Method7.11450<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C699`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C699`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C699`2<class BaseClass1,class BaseClass0>::ClassMethod2764<object>()
		ldstr "G2_C699::ClassMethod2764.11454<System.Object>()"
		ldstr "class G2_C699`2<class BaseClass1,class BaseClass0> on type class G2_C699`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C699`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C699`2<class BaseClass1,class BaseClass0>::ClassMethod2763<object>()
		ldstr "G2_C699::ClassMethod2763.11453<System.Object>()"
		ldstr "class G2_C699`2<class BaseClass1,class BaseClass0> on type class G2_C699`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C699`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C699`2<class BaseClass1,class BaseClass0>::ClassMethod2762()
		ldstr "G2_C699::ClassMethod2762.11452()"
		ldstr "class G2_C699`2<class BaseClass1,class BaseClass0> on type class G2_C699`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C699`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C699`2<class BaseClass1,class BaseClass0>::ClassMethod2761()
		ldstr "G2_C699::ClassMethod2761.11451()"
		ldstr "class G2_C699`2<class BaseClass1,class BaseClass0> on type class G2_C699`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C699`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C699`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G2_C699::Method7.11450<System.Object>()"
		ldstr "class G2_C699`2<class BaseClass1,class BaseClass0> on type class G2_C699`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C699`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C699`2<class BaseClass1,class BaseClass0>::Method6<object>()
		ldstr "G2_C699::Method6.11449<System.Object>()"
		ldstr "class G2_C699`2<class BaseClass1,class BaseClass0> on type class G2_C699`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C699`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C699`2<class BaseClass1,class BaseClass0>::Method5()
		ldstr "G2_C699::Method5.11447()"
		ldstr "class G2_C699`2<class BaseClass1,class BaseClass0> on type class G2_C699`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C699`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C699`2<class BaseClass1,class BaseClass0>::Method4()
		ldstr "G2_C699::Method4.11445()"
		ldstr "class G2_C699`2<class BaseClass1,class BaseClass0> on type class G2_C699`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C699`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C699`2<class BaseClass1,class BaseClass0>::ClassMethod1349<object>()
		ldstr "G2_C699::ClassMethod1349.MI.11455<System.Object>()"
		ldstr "class G2_C699`2<class BaseClass1,class BaseClass0> on type class G2_C699`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C699`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C699`2<class BaseClass1,class BaseClass0>::ClassMethod1348<object>()
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G2_C699`2<class BaseClass1,class BaseClass0> on type class G2_C699`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G2_C699::Method4.MI.11446()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C699`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G2_C699::Method5.MI.11448()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C699`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C699::Method6.11449<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C699`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G2_C699::Method7.11450<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G2_C699`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C699::Method7.11450<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C699`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C699`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C13`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C13`2<class BaseClass0,class BaseClass1>::ClassMethod1349<object>()
		ldstr "G2_C699::ClassMethod1349.MI.11455<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass1> on type class G2_C699`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C13`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C13`2<class BaseClass0,class BaseClass1>::ClassMethod1348<object>()
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass1> on type class G2_C699`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C13`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C13`2<class BaseClass0,class BaseClass1>::Method6<object>()
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass1> on type class G2_C699`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C13`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C13`2<class BaseClass0,class BaseClass1>::Method5()
		ldstr "G1_C13::Method5.4873()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass1> on type class G2_C699`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C13`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C13`2<class BaseClass0,class BaseClass1>::Method4()
		ldstr "G1_C13::Method4.4872()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass1> on type class G2_C699`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C13`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C13`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G1_C13::Method7.4871<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass1> on type class G2_C699`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C699::Method7.11450<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C699`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C699::Method4.MI.11446()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C699`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C699::Method5.MI.11448()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C699`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C699::Method6.11449<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C699`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C699`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C699`2<class BaseClass1,class BaseClass1>::ClassMethod2764<object>()
		ldstr "G2_C699::ClassMethod2764.11454<System.Object>()"
		ldstr "class G2_C699`2<class BaseClass1,class BaseClass1> on type class G2_C699`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C699`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C699`2<class BaseClass1,class BaseClass1>::ClassMethod2763<object>()
		ldstr "G2_C699::ClassMethod2763.11453<System.Object>()"
		ldstr "class G2_C699`2<class BaseClass1,class BaseClass1> on type class G2_C699`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C699`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C699`2<class BaseClass1,class BaseClass1>::ClassMethod2762()
		ldstr "G2_C699::ClassMethod2762.11452()"
		ldstr "class G2_C699`2<class BaseClass1,class BaseClass1> on type class G2_C699`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C699`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C699`2<class BaseClass1,class BaseClass1>::ClassMethod2761()
		ldstr "G2_C699::ClassMethod2761.11451()"
		ldstr "class G2_C699`2<class BaseClass1,class BaseClass1> on type class G2_C699`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C699`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C699`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C699::Method7.11450<System.Object>()"
		ldstr "class G2_C699`2<class BaseClass1,class BaseClass1> on type class G2_C699`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C699`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C699`2<class BaseClass1,class BaseClass1>::Method6<object>()
		ldstr "G2_C699::Method6.11449<System.Object>()"
		ldstr "class G2_C699`2<class BaseClass1,class BaseClass1> on type class G2_C699`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C699`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C699`2<class BaseClass1,class BaseClass1>::Method5()
		ldstr "G2_C699::Method5.11447()"
		ldstr "class G2_C699`2<class BaseClass1,class BaseClass1> on type class G2_C699`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C699`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C699`2<class BaseClass1,class BaseClass1>::Method4()
		ldstr "G2_C699::Method4.11445()"
		ldstr "class G2_C699`2<class BaseClass1,class BaseClass1> on type class G2_C699`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C699`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C699`2<class BaseClass1,class BaseClass1>::ClassMethod1349<object>()
		ldstr "G2_C699::ClassMethod1349.MI.11455<System.Object>()"
		ldstr "class G2_C699`2<class BaseClass1,class BaseClass1> on type class G2_C699`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C699`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C699`2<class BaseClass1,class BaseClass1>::ClassMethod1348<object>()
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G2_C699`2<class BaseClass1,class BaseClass1> on type class G2_C699`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G2_C699::Method4.MI.11446()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C699`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G2_C699::Method5.MI.11448()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C699`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C699::Method6.11449<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C699`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C699::Method7.11450<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C699`2<class BaseClass1,class BaseClass1>"
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
		newobj instance void class G3_C1729`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G2_C699::ClassMethod1349.MI.11455<System.Object>()#G1_C13::Method4.4872()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G1_C13::Method7.4871<System.Object>()#"
		call void Generated1257::M.G1_C13.T.T<class BaseClass0,class BaseClass0,class G3_C1729`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G2_C699::ClassMethod1349.MI.11455<System.Object>()#G1_C13::Method4.4872()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G1_C13::Method7.4871<System.Object>()#"
		call void Generated1257::M.G1_C13.A.T<class BaseClass0,class G3_C1729`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G2_C699::ClassMethod1349.MI.11455<System.Object>()#G1_C13::Method4.4872()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G1_C13::Method7.4871<System.Object>()#"
		call void Generated1257::M.G1_C13.A.A<class G3_C1729`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1729::Method7.17678<System.Object>()#"
		call void Generated1257::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G3_C1729`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1729::Method7.17678<System.Object>()#"
		call void Generated1257::M.IBase2.A.T<class BaseClass0,class G3_C1729`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1729::Method7.17678<System.Object>()#"
		call void Generated1257::M.IBase2.A.A<class G3_C1729`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C699::Method4.MI.11446()#G2_C699::Method5.MI.11448()#G2_C699::Method6.11449<System.Object>()#"
		call void Generated1257::M.IBase1.T<class BaseClass0,class G3_C1729`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C699::Method4.MI.11446()#G2_C699::Method5.MI.11448()#G2_C699::Method6.11449<System.Object>()#"
		call void Generated1257::M.IBase1.A<class G3_C1729`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1729::Method7.MI.17679<System.Object>()#"
		call void Generated1257::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G3_C1729`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1729::Method7.MI.17679<System.Object>()#"
		call void Generated1257::M.IBase2.A.T<class BaseClass1,class G3_C1729`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1729::Method7.MI.17679<System.Object>()#"
		call void Generated1257::M.IBase2.A.B<class G3_C1729`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G2_C699::ClassMethod1349.MI.11455<System.Object>()#G2_C699::ClassMethod2761.11451()#G3_C1729::ClassMethod2762.MI.17681()#G3_C1729::ClassMethod2763.MI.17682<System.Object>()#G3_C1729::ClassMethod2764.MI.17683<System.Object>()#G2_C699::Method4.11445()#G2_C699::Method5.11447()#G2_C699::Method6.11449<System.Object>()#G3_C1729::Method7.17678<System.Object>()#"
		call void Generated1257::M.G2_C699.T.T<class BaseClass1,class BaseClass0,class G3_C1729`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G2_C699::ClassMethod1349.MI.11455<System.Object>()#G2_C699::ClassMethod2761.11451()#G3_C1729::ClassMethod2762.MI.17681()#G3_C1729::ClassMethod2763.MI.17682<System.Object>()#G3_C1729::ClassMethod2764.MI.17683<System.Object>()#G2_C699::Method4.11445()#G2_C699::Method5.11447()#G2_C699::Method6.11449<System.Object>()#G3_C1729::Method7.17678<System.Object>()#"
		call void Generated1257::M.G2_C699.B.T<class BaseClass0,class G3_C1729`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G2_C699::ClassMethod1349.MI.11455<System.Object>()#G2_C699::ClassMethod2761.11451()#G3_C1729::ClassMethod2762.MI.17681()#G3_C1729::ClassMethod2763.MI.17682<System.Object>()#G3_C1729::ClassMethod2764.MI.17683<System.Object>()#G2_C699::Method4.11445()#G2_C699::Method5.11447()#G2_C699::Method6.11449<System.Object>()#G3_C1729::Method7.17678<System.Object>()#"
		call void Generated1257::M.G2_C699.B.A<class G3_C1729`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C699::Method4.MI.11446()#G2_C699::Method5.MI.11448()#G2_C699::Method6.11449<System.Object>()#"
		call void Generated1257::M.IBase1.T<class BaseClass1,class G3_C1729`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C699::Method4.MI.11446()#G2_C699::Method5.MI.11448()#G2_C699::Method6.11449<System.Object>()#"
		call void Generated1257::M.IBase1.B<class G3_C1729`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1729::Method7.17678<System.Object>()#"
		call void Generated1257::M.IBase2.T.T<class BaseClass1,class BaseClass0,class G3_C1729`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1729::Method7.17678<System.Object>()#"
		call void Generated1257::M.IBase2.B.T<class BaseClass0,class G3_C1729`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1729::Method7.17678<System.Object>()#"
		call void Generated1257::M.IBase2.B.A<class G3_C1729`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1729::Method7.MI.17679<System.Object>()#"
		call void Generated1257::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G3_C1729`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1729::Method7.MI.17679<System.Object>()#"
		call void Generated1257::M.IBase2.B.T<class BaseClass1,class G3_C1729`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1729::Method7.MI.17679<System.Object>()#"
		call void Generated1257::M.IBase2.B.B<class G3_C1729`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G2_C699::ClassMethod1349.MI.11455<System.Object>()#G2_C699::ClassMethod2761.11451()#G3_C1729::ClassMethod2762.MI.17681()#G3_C1729::ClassMethod2763.MI.17682<System.Object>()#G3_C1729::ClassMethod2764.MI.17683<System.Object>()#G3_C1729::ClassMethod4857.17680<System.Object>()#G2_C699::Method4.11445()#G2_C699::Method5.11447()#G2_C699::Method6.11449<System.Object>()#G3_C1729::Method7.17678<System.Object>()#"
		call void Generated1257::M.G3_C1729.T<class BaseClass0,class G3_C1729`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G2_C699::ClassMethod1349.MI.11455<System.Object>()#G2_C699::ClassMethod2761.11451()#G3_C1729::ClassMethod2762.MI.17681()#G3_C1729::ClassMethod2763.MI.17682<System.Object>()#G3_C1729::ClassMethod2764.MI.17683<System.Object>()#G3_C1729::ClassMethod4857.17680<System.Object>()#G2_C699::Method4.11445()#G2_C699::Method5.11447()#G2_C699::Method6.11449<System.Object>()#G3_C1729::Method7.17678<System.Object>()#"
		call void Generated1257::M.G3_C1729.A<class G3_C1729`1<class BaseClass0>>(!!0,string)
		newobj instance void class G3_C1729`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G2_C699::ClassMethod1349.MI.11455<System.Object>()#G1_C13::Method4.4872()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G1_C13::Method7.4871<System.Object>()#"
		call void Generated1257::M.G1_C13.T.T<class BaseClass0,class BaseClass0,class G3_C1729`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G2_C699::ClassMethod1349.MI.11455<System.Object>()#G1_C13::Method4.4872()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G1_C13::Method7.4871<System.Object>()#"
		call void Generated1257::M.G1_C13.A.T<class BaseClass0,class G3_C1729`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G2_C699::ClassMethod1349.MI.11455<System.Object>()#G1_C13::Method4.4872()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G1_C13::Method7.4871<System.Object>()#"
		call void Generated1257::M.G1_C13.A.A<class G3_C1729`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1729::Method7.17678<System.Object>()#"
		call void Generated1257::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G3_C1729`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1729::Method7.17678<System.Object>()#"
		call void Generated1257::M.IBase2.A.T<class BaseClass0,class G3_C1729`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1729::Method7.17678<System.Object>()#"
		call void Generated1257::M.IBase2.A.A<class G3_C1729`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C699::Method4.MI.11446()#G2_C699::Method5.MI.11448()#G2_C699::Method6.11449<System.Object>()#"
		call void Generated1257::M.IBase1.T<class BaseClass0,class G3_C1729`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C699::Method4.MI.11446()#G2_C699::Method5.MI.11448()#G2_C699::Method6.11449<System.Object>()#"
		call void Generated1257::M.IBase1.A<class G3_C1729`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1729::Method7.MI.17679<System.Object>()#"
		call void Generated1257::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G3_C1729`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1729::Method7.MI.17679<System.Object>()#"
		call void Generated1257::M.IBase2.A.T<class BaseClass1,class G3_C1729`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1729::Method7.MI.17679<System.Object>()#"
		call void Generated1257::M.IBase2.A.B<class G3_C1729`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G2_C699::ClassMethod1349.MI.11455<System.Object>()#G2_C699::ClassMethod2761.11451()#G3_C1729::ClassMethod2762.MI.17681()#G3_C1729::ClassMethod2763.MI.17682<System.Object>()#G3_C1729::ClassMethod2764.MI.17683<System.Object>()#G2_C699::Method4.11445()#G2_C699::Method5.11447()#G2_C699::Method6.11449<System.Object>()#G3_C1729::Method7.17678<System.Object>()#"
		call void Generated1257::M.G2_C699.T.T<class BaseClass1,class BaseClass0,class G3_C1729`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G2_C699::ClassMethod1349.MI.11455<System.Object>()#G2_C699::ClassMethod2761.11451()#G3_C1729::ClassMethod2762.MI.17681()#G3_C1729::ClassMethod2763.MI.17682<System.Object>()#G3_C1729::ClassMethod2764.MI.17683<System.Object>()#G2_C699::Method4.11445()#G2_C699::Method5.11447()#G2_C699::Method6.11449<System.Object>()#G3_C1729::Method7.17678<System.Object>()#"
		call void Generated1257::M.G2_C699.B.T<class BaseClass0,class G3_C1729`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G2_C699::ClassMethod1349.MI.11455<System.Object>()#G2_C699::ClassMethod2761.11451()#G3_C1729::ClassMethod2762.MI.17681()#G3_C1729::ClassMethod2763.MI.17682<System.Object>()#G3_C1729::ClassMethod2764.MI.17683<System.Object>()#G2_C699::Method4.11445()#G2_C699::Method5.11447()#G2_C699::Method6.11449<System.Object>()#G3_C1729::Method7.17678<System.Object>()#"
		call void Generated1257::M.G2_C699.B.A<class G3_C1729`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C699::Method4.MI.11446()#G2_C699::Method5.MI.11448()#G2_C699::Method6.11449<System.Object>()#"
		call void Generated1257::M.IBase1.T<class BaseClass1,class G3_C1729`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C699::Method4.MI.11446()#G2_C699::Method5.MI.11448()#G2_C699::Method6.11449<System.Object>()#"
		call void Generated1257::M.IBase1.B<class G3_C1729`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1729::Method7.17678<System.Object>()#"
		call void Generated1257::M.IBase2.T.T<class BaseClass1,class BaseClass0,class G3_C1729`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1729::Method7.17678<System.Object>()#"
		call void Generated1257::M.IBase2.B.T<class BaseClass0,class G3_C1729`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1729::Method7.17678<System.Object>()#"
		call void Generated1257::M.IBase2.B.A<class G3_C1729`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1729::Method7.MI.17679<System.Object>()#"
		call void Generated1257::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G3_C1729`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1729::Method7.MI.17679<System.Object>()#"
		call void Generated1257::M.IBase2.B.T<class BaseClass1,class G3_C1729`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1729::Method7.MI.17679<System.Object>()#"
		call void Generated1257::M.IBase2.B.B<class G3_C1729`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G2_C699::ClassMethod1349.MI.11455<System.Object>()#G2_C699::ClassMethod2761.11451()#G3_C1729::ClassMethod2762.MI.17681()#G3_C1729::ClassMethod2763.MI.17682<System.Object>()#G3_C1729::ClassMethod2764.MI.17683<System.Object>()#G3_C1729::ClassMethod4857.17680<System.Object>()#G2_C699::Method4.11445()#G2_C699::Method5.11447()#G2_C699::Method6.11449<System.Object>()#G3_C1729::Method7.17678<System.Object>()#"
		call void Generated1257::M.G3_C1729.T<class BaseClass1,class G3_C1729`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G2_C699::ClassMethod1349.MI.11455<System.Object>()#G2_C699::ClassMethod2761.11451()#G3_C1729::ClassMethod2762.MI.17681()#G3_C1729::ClassMethod2763.MI.17682<System.Object>()#G3_C1729::ClassMethod2764.MI.17683<System.Object>()#G3_C1729::ClassMethod4857.17680<System.Object>()#G2_C699::Method4.11445()#G2_C699::Method5.11447()#G2_C699::Method6.11449<System.Object>()#G3_C1729::Method7.17678<System.Object>()#"
		call void Generated1257::M.G3_C1729.B<class G3_C1729`1<class BaseClass1>>(!!0,string)
		newobj instance void class G2_C699`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G2_C699::ClassMethod1349.MI.11455<System.Object>()#G1_C13::Method4.4872()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G1_C13::Method7.4871<System.Object>()#"
		call void Generated1257::M.G1_C13.T.T<class BaseClass0,class BaseClass0,class G2_C699`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G2_C699::ClassMethod1349.MI.11455<System.Object>()#G1_C13::Method4.4872()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G1_C13::Method7.4871<System.Object>()#"
		call void Generated1257::M.G1_C13.A.T<class BaseClass0,class G2_C699`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G2_C699::ClassMethod1349.MI.11455<System.Object>()#G1_C13::Method4.4872()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G1_C13::Method7.4871<System.Object>()#"
		call void Generated1257::M.G1_C13.A.A<class G2_C699`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C699::Method7.11450<System.Object>()#"
		call void Generated1257::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G2_C699`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C699::Method7.11450<System.Object>()#"
		call void Generated1257::M.IBase2.A.T<class BaseClass0,class G2_C699`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C699::Method7.11450<System.Object>()#"
		call void Generated1257::M.IBase2.A.A<class G2_C699`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C699::Method4.MI.11446()#G2_C699::Method5.MI.11448()#G2_C699::Method6.11449<System.Object>()#"
		call void Generated1257::M.IBase1.T<class BaseClass0,class G2_C699`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C699::Method4.MI.11446()#G2_C699::Method5.MI.11448()#G2_C699::Method6.11449<System.Object>()#"
		call void Generated1257::M.IBase1.A<class G2_C699`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C699::Method7.11450<System.Object>()#"
		call void Generated1257::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C699`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C699::Method7.11450<System.Object>()#"
		call void Generated1257::M.IBase2.A.T<class BaseClass1,class G2_C699`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C699::Method7.11450<System.Object>()#"
		call void Generated1257::M.IBase2.A.B<class G2_C699`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G2_C699::ClassMethod1349.MI.11455<System.Object>()#G2_C699::ClassMethod2761.11451()#G2_C699::ClassMethod2762.11452()#G2_C699::ClassMethod2763.11453<System.Object>()#G2_C699::ClassMethod2764.11454<System.Object>()#G2_C699::Method4.11445()#G2_C699::Method5.11447()#G2_C699::Method6.11449<System.Object>()#G2_C699::Method7.11450<System.Object>()#"
		call void Generated1257::M.G2_C699.T.T<class BaseClass0,class BaseClass0,class G2_C699`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G2_C699::ClassMethod1349.MI.11455<System.Object>()#G2_C699::ClassMethod2761.11451()#G2_C699::ClassMethod2762.11452()#G2_C699::ClassMethod2763.11453<System.Object>()#G2_C699::ClassMethod2764.11454<System.Object>()#G2_C699::Method4.11445()#G2_C699::Method5.11447()#G2_C699::Method6.11449<System.Object>()#G2_C699::Method7.11450<System.Object>()#"
		call void Generated1257::M.G2_C699.A.T<class BaseClass0,class G2_C699`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G2_C699::ClassMethod1349.MI.11455<System.Object>()#G2_C699::ClassMethod2761.11451()#G2_C699::ClassMethod2762.11452()#G2_C699::ClassMethod2763.11453<System.Object>()#G2_C699::ClassMethod2764.11454<System.Object>()#G2_C699::Method4.11445()#G2_C699::Method5.11447()#G2_C699::Method6.11449<System.Object>()#G2_C699::Method7.11450<System.Object>()#"
		call void Generated1257::M.G2_C699.A.A<class G2_C699`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C699::Method7.11450<System.Object>()#"
		call void Generated1257::M.IBase2.T.T<class BaseClass1,class BaseClass0,class G2_C699`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C699::Method7.11450<System.Object>()#"
		call void Generated1257::M.IBase2.B.T<class BaseClass0,class G2_C699`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C699::Method7.11450<System.Object>()#"
		call void Generated1257::M.IBase2.B.A<class G2_C699`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C699::Method7.11450<System.Object>()#"
		call void Generated1257::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G2_C699`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C699::Method7.11450<System.Object>()#"
		call void Generated1257::M.IBase2.B.T<class BaseClass1,class G2_C699`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C699::Method7.11450<System.Object>()#"
		call void Generated1257::M.IBase2.B.B<class G2_C699`2<class BaseClass0,class BaseClass0>>(!!0,string)
		newobj instance void class G2_C699`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G2_C699::ClassMethod1349.MI.11455<System.Object>()#G1_C13::Method4.4872()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G1_C13::Method7.4871<System.Object>()#"
		call void Generated1257::M.G1_C13.T.T<class BaseClass0,class BaseClass1,class G2_C699`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G2_C699::ClassMethod1349.MI.11455<System.Object>()#G1_C13::Method4.4872()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G1_C13::Method7.4871<System.Object>()#"
		call void Generated1257::M.G1_C13.A.T<class BaseClass1,class G2_C699`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G2_C699::ClassMethod1349.MI.11455<System.Object>()#G1_C13::Method4.4872()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G1_C13::Method7.4871<System.Object>()#"
		call void Generated1257::M.G1_C13.A.B<class G2_C699`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C699::Method7.11450<System.Object>()#"
		call void Generated1257::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C699`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C699::Method7.11450<System.Object>()#"
		call void Generated1257::M.IBase2.A.T<class BaseClass1,class G2_C699`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C699::Method7.11450<System.Object>()#"
		call void Generated1257::M.IBase2.A.B<class G2_C699`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C699::Method4.MI.11446()#G2_C699::Method5.MI.11448()#G2_C699::Method6.11449<System.Object>()#"
		call void Generated1257::M.IBase1.T<class BaseClass0,class G2_C699`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C699::Method4.MI.11446()#G2_C699::Method5.MI.11448()#G2_C699::Method6.11449<System.Object>()#"
		call void Generated1257::M.IBase1.A<class G2_C699`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G2_C699::ClassMethod1349.MI.11455<System.Object>()#G2_C699::ClassMethod2761.11451()#G2_C699::ClassMethod2762.11452()#G2_C699::ClassMethod2763.11453<System.Object>()#G2_C699::ClassMethod2764.11454<System.Object>()#G2_C699::Method4.11445()#G2_C699::Method5.11447()#G2_C699::Method6.11449<System.Object>()#G2_C699::Method7.11450<System.Object>()#"
		call void Generated1257::M.G2_C699.T.T<class BaseClass0,class BaseClass1,class G2_C699`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G2_C699::ClassMethod1349.MI.11455<System.Object>()#G2_C699::ClassMethod2761.11451()#G2_C699::ClassMethod2762.11452()#G2_C699::ClassMethod2763.11453<System.Object>()#G2_C699::ClassMethod2764.11454<System.Object>()#G2_C699::Method4.11445()#G2_C699::Method5.11447()#G2_C699::Method6.11449<System.Object>()#G2_C699::Method7.11450<System.Object>()#"
		call void Generated1257::M.G2_C699.A.T<class BaseClass1,class G2_C699`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G2_C699::ClassMethod1349.MI.11455<System.Object>()#G2_C699::ClassMethod2761.11451()#G2_C699::ClassMethod2762.11452()#G2_C699::ClassMethod2763.11453<System.Object>()#G2_C699::ClassMethod2764.11454<System.Object>()#G2_C699::Method4.11445()#G2_C699::Method5.11447()#G2_C699::Method6.11449<System.Object>()#G2_C699::Method7.11450<System.Object>()#"
		call void Generated1257::M.G2_C699.A.B<class G2_C699`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C699::Method7.11450<System.Object>()#"
		call void Generated1257::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G2_C699`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C699::Method7.11450<System.Object>()#"
		call void Generated1257::M.IBase2.B.T<class BaseClass1,class G2_C699`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C699::Method7.11450<System.Object>()#"
		call void Generated1257::M.IBase2.B.B<class G2_C699`2<class BaseClass0,class BaseClass1>>(!!0,string)
		newobj instance void class G2_C699`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G2_C699::ClassMethod1349.MI.11455<System.Object>()#G1_C13::Method4.4872()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G1_C13::Method7.4871<System.Object>()#"
		call void Generated1257::M.G1_C13.T.T<class BaseClass0,class BaseClass0,class G2_C699`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G2_C699::ClassMethod1349.MI.11455<System.Object>()#G1_C13::Method4.4872()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G1_C13::Method7.4871<System.Object>()#"
		call void Generated1257::M.G1_C13.A.T<class BaseClass0,class G2_C699`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G2_C699::ClassMethod1349.MI.11455<System.Object>()#G1_C13::Method4.4872()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G1_C13::Method7.4871<System.Object>()#"
		call void Generated1257::M.G1_C13.A.A<class G2_C699`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C699::Method7.11450<System.Object>()#"
		call void Generated1257::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G2_C699`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C699::Method7.11450<System.Object>()#"
		call void Generated1257::M.IBase2.A.T<class BaseClass0,class G2_C699`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C699::Method7.11450<System.Object>()#"
		call void Generated1257::M.IBase2.A.A<class G2_C699`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C699::Method4.MI.11446()#G2_C699::Method5.MI.11448()#G2_C699::Method6.11449<System.Object>()#"
		call void Generated1257::M.IBase1.T<class BaseClass0,class G2_C699`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C699::Method4.MI.11446()#G2_C699::Method5.MI.11448()#G2_C699::Method6.11449<System.Object>()#"
		call void Generated1257::M.IBase1.A<class G2_C699`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C699::Method7.11450<System.Object>()#"
		call void Generated1257::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C699`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C699::Method7.11450<System.Object>()#"
		call void Generated1257::M.IBase2.A.T<class BaseClass1,class G2_C699`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C699::Method7.11450<System.Object>()#"
		call void Generated1257::M.IBase2.A.B<class G2_C699`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G2_C699::ClassMethod1349.MI.11455<System.Object>()#G2_C699::ClassMethod2761.11451()#G2_C699::ClassMethod2762.11452()#G2_C699::ClassMethod2763.11453<System.Object>()#G2_C699::ClassMethod2764.11454<System.Object>()#G2_C699::Method4.11445()#G2_C699::Method5.11447()#G2_C699::Method6.11449<System.Object>()#G2_C699::Method7.11450<System.Object>()#"
		call void Generated1257::M.G2_C699.T.T<class BaseClass1,class BaseClass0,class G2_C699`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G2_C699::ClassMethod1349.MI.11455<System.Object>()#G2_C699::ClassMethod2761.11451()#G2_C699::ClassMethod2762.11452()#G2_C699::ClassMethod2763.11453<System.Object>()#G2_C699::ClassMethod2764.11454<System.Object>()#G2_C699::Method4.11445()#G2_C699::Method5.11447()#G2_C699::Method6.11449<System.Object>()#G2_C699::Method7.11450<System.Object>()#"
		call void Generated1257::M.G2_C699.B.T<class BaseClass0,class G2_C699`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G2_C699::ClassMethod1349.MI.11455<System.Object>()#G2_C699::ClassMethod2761.11451()#G2_C699::ClassMethod2762.11452()#G2_C699::ClassMethod2763.11453<System.Object>()#G2_C699::ClassMethod2764.11454<System.Object>()#G2_C699::Method4.11445()#G2_C699::Method5.11447()#G2_C699::Method6.11449<System.Object>()#G2_C699::Method7.11450<System.Object>()#"
		call void Generated1257::M.G2_C699.B.A<class G2_C699`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C699::Method4.MI.11446()#G2_C699::Method5.MI.11448()#G2_C699::Method6.11449<System.Object>()#"
		call void Generated1257::M.IBase1.T<class BaseClass1,class G2_C699`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C699::Method4.MI.11446()#G2_C699::Method5.MI.11448()#G2_C699::Method6.11449<System.Object>()#"
		call void Generated1257::M.IBase1.B<class G2_C699`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C699::Method7.11450<System.Object>()#"
		call void Generated1257::M.IBase2.T.T<class BaseClass1,class BaseClass0,class G2_C699`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C699::Method7.11450<System.Object>()#"
		call void Generated1257::M.IBase2.B.T<class BaseClass0,class G2_C699`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C699::Method7.11450<System.Object>()#"
		call void Generated1257::M.IBase2.B.A<class G2_C699`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C699::Method7.11450<System.Object>()#"
		call void Generated1257::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G2_C699`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C699::Method7.11450<System.Object>()#"
		call void Generated1257::M.IBase2.B.T<class BaseClass1,class G2_C699`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C699::Method7.11450<System.Object>()#"
		call void Generated1257::M.IBase2.B.B<class G2_C699`2<class BaseClass1,class BaseClass0>>(!!0,string)
		newobj instance void class G2_C699`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G2_C699::ClassMethod1349.MI.11455<System.Object>()#G1_C13::Method4.4872()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G1_C13::Method7.4871<System.Object>()#"
		call void Generated1257::M.G1_C13.T.T<class BaseClass0,class BaseClass1,class G2_C699`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G2_C699::ClassMethod1349.MI.11455<System.Object>()#G1_C13::Method4.4872()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G1_C13::Method7.4871<System.Object>()#"
		call void Generated1257::M.G1_C13.A.T<class BaseClass1,class G2_C699`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G2_C699::ClassMethod1349.MI.11455<System.Object>()#G1_C13::Method4.4872()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G1_C13::Method7.4871<System.Object>()#"
		call void Generated1257::M.G1_C13.A.B<class G2_C699`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C699::Method7.11450<System.Object>()#"
		call void Generated1257::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C699`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C699::Method7.11450<System.Object>()#"
		call void Generated1257::M.IBase2.A.T<class BaseClass1,class G2_C699`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C699::Method7.11450<System.Object>()#"
		call void Generated1257::M.IBase2.A.B<class G2_C699`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C699::Method4.MI.11446()#G2_C699::Method5.MI.11448()#G2_C699::Method6.11449<System.Object>()#"
		call void Generated1257::M.IBase1.T<class BaseClass0,class G2_C699`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C699::Method4.MI.11446()#G2_C699::Method5.MI.11448()#G2_C699::Method6.11449<System.Object>()#"
		call void Generated1257::M.IBase1.A<class G2_C699`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G2_C699::ClassMethod1349.MI.11455<System.Object>()#G2_C699::ClassMethod2761.11451()#G2_C699::ClassMethod2762.11452()#G2_C699::ClassMethod2763.11453<System.Object>()#G2_C699::ClassMethod2764.11454<System.Object>()#G2_C699::Method4.11445()#G2_C699::Method5.11447()#G2_C699::Method6.11449<System.Object>()#G2_C699::Method7.11450<System.Object>()#"
		call void Generated1257::M.G2_C699.T.T<class BaseClass1,class BaseClass1,class G2_C699`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G2_C699::ClassMethod1349.MI.11455<System.Object>()#G2_C699::ClassMethod2761.11451()#G2_C699::ClassMethod2762.11452()#G2_C699::ClassMethod2763.11453<System.Object>()#G2_C699::ClassMethod2764.11454<System.Object>()#G2_C699::Method4.11445()#G2_C699::Method5.11447()#G2_C699::Method6.11449<System.Object>()#G2_C699::Method7.11450<System.Object>()#"
		call void Generated1257::M.G2_C699.B.T<class BaseClass1,class G2_C699`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G2_C699::ClassMethod1349.MI.11455<System.Object>()#G2_C699::ClassMethod2761.11451()#G2_C699::ClassMethod2762.11452()#G2_C699::ClassMethod2763.11453<System.Object>()#G2_C699::ClassMethod2764.11454<System.Object>()#G2_C699::Method4.11445()#G2_C699::Method5.11447()#G2_C699::Method6.11449<System.Object>()#G2_C699::Method7.11450<System.Object>()#"
		call void Generated1257::M.G2_C699.B.B<class G2_C699`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C699::Method4.MI.11446()#G2_C699::Method5.MI.11448()#G2_C699::Method6.11449<System.Object>()#"
		call void Generated1257::M.IBase1.T<class BaseClass1,class G2_C699`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C699::Method4.MI.11446()#G2_C699::Method5.MI.11448()#G2_C699::Method6.11449<System.Object>()#"
		call void Generated1257::M.IBase1.B<class G2_C699`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C699::Method7.11450<System.Object>()#"
		call void Generated1257::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G2_C699`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C699::Method7.11450<System.Object>()#"
		call void Generated1257::M.IBase2.B.T<class BaseClass1,class G2_C699`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C699::Method7.11450<System.Object>()#"
		call void Generated1257::M.IBase2.B.B<class G2_C699`2<class BaseClass1,class BaseClass1>>(!!0,string)
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
		newobj instance void class G3_C1729`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C13`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass0,class BaseClass0>::ClassMethod1349<object>()
		calli default string(class G3_C1729`1<class BaseClass0>)
		ldstr "G2_C699::ClassMethod1349.MI.11455<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass0> on type class G3_C1729`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C13`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass0,class BaseClass0>::ClassMethod1348<object>()
		calli default string(class G3_C1729`1<class BaseClass0>)
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass0> on type class G3_C1729`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C13`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass0,class BaseClass0>::Method6<object>()
		calli default string(class G3_C1729`1<class BaseClass0>)
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass0> on type class G3_C1729`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C13`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass0,class BaseClass0>::Method5()
		calli default string(class G3_C1729`1<class BaseClass0>)
		ldstr "G1_C13::Method5.4873()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass0> on type class G3_C1729`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C13`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass0,class BaseClass0>::Method4()
		calli default string(class G3_C1729`1<class BaseClass0>)
		ldstr "G1_C13::Method4.4872()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass0> on type class G3_C1729`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C13`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1729`1<class BaseClass0>)
		ldstr "G1_C13::Method7.4871<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass0> on type class G3_C1729`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1729`1<class BaseClass0>)
		ldstr "G3_C1729::Method7.17678<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1729`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G3_C1729`1<class BaseClass0>)
		ldstr "G2_C699::Method4.MI.11446()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1729`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G3_C1729`1<class BaseClass0>)
		ldstr "G2_C699::Method5.MI.11448()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1729`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1729`1<class BaseClass0>)
		ldstr "G2_C699::Method6.11449<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1729`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1729`1<class BaseClass0>)
		ldstr "G3_C1729::Method7.MI.17679<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1729`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C699`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C699`2<class BaseClass1,class BaseClass0>::ClassMethod2764<object>()
		calli default string(class G3_C1729`1<class BaseClass0>)
		ldstr "G3_C1729::ClassMethod2764.MI.17683<System.Object>()"
		ldstr "class G2_C699`2<class BaseClass1,class BaseClass0> on type class G3_C1729`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C699`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C699`2<class BaseClass1,class BaseClass0>::ClassMethod2763<object>()
		calli default string(class G3_C1729`1<class BaseClass0>)
		ldstr "G3_C1729::ClassMethod2763.MI.17682<System.Object>()"
		ldstr "class G2_C699`2<class BaseClass1,class BaseClass0> on type class G3_C1729`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C699`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C699`2<class BaseClass1,class BaseClass0>::ClassMethod2762()
		calli default string(class G3_C1729`1<class BaseClass0>)
		ldstr "G3_C1729::ClassMethod2762.MI.17681()"
		ldstr "class G2_C699`2<class BaseClass1,class BaseClass0> on type class G3_C1729`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C699`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C699`2<class BaseClass1,class BaseClass0>::ClassMethod2761()
		calli default string(class G3_C1729`1<class BaseClass0>)
		ldstr "G2_C699::ClassMethod2761.11451()"
		ldstr "class G2_C699`2<class BaseClass1,class BaseClass0> on type class G3_C1729`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C699`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C699`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1729`1<class BaseClass0>)
		ldstr "G3_C1729::Method7.17678<System.Object>()"
		ldstr "class G2_C699`2<class BaseClass1,class BaseClass0> on type class G3_C1729`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C699`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C699`2<class BaseClass1,class BaseClass0>::Method6<object>()
		calli default string(class G3_C1729`1<class BaseClass0>)
		ldstr "G2_C699::Method6.11449<System.Object>()"
		ldstr "class G2_C699`2<class BaseClass1,class BaseClass0> on type class G3_C1729`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C699`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C699`2<class BaseClass1,class BaseClass0>::Method5()
		calli default string(class G3_C1729`1<class BaseClass0>)
		ldstr "G2_C699::Method5.11447()"
		ldstr "class G2_C699`2<class BaseClass1,class BaseClass0> on type class G3_C1729`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C699`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C699`2<class BaseClass1,class BaseClass0>::Method4()
		calli default string(class G3_C1729`1<class BaseClass0>)
		ldstr "G2_C699::Method4.11445()"
		ldstr "class G2_C699`2<class BaseClass1,class BaseClass0> on type class G3_C1729`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C699`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C699`2<class BaseClass1,class BaseClass0>::ClassMethod1349<object>()
		calli default string(class G3_C1729`1<class BaseClass0>)
		ldstr "G2_C699::ClassMethod1349.MI.11455<System.Object>()"
		ldstr "class G2_C699`2<class BaseClass1,class BaseClass0> on type class G3_C1729`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C699`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C699`2<class BaseClass1,class BaseClass0>::ClassMethod1348<object>()
		calli default string(class G3_C1729`1<class BaseClass0>)
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G2_C699`2<class BaseClass1,class BaseClass0> on type class G3_C1729`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G3_C1729`1<class BaseClass0>)
		ldstr "G2_C699::Method4.MI.11446()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1729`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G3_C1729`1<class BaseClass0>)
		ldstr "G2_C699::Method5.MI.11448()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1729`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G3_C1729`1<class BaseClass0>)
		ldstr "G2_C699::Method6.11449<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1729`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1729`1<class BaseClass0>)
		ldstr "G3_C1729::Method7.17678<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G3_C1729`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1729`1<class BaseClass0>)
		ldstr "G3_C1729::Method7.MI.17679<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1729`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1729`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1729`1<class BaseClass0>::ClassMethod4857<object>()
		calli default string(class G3_C1729`1<class BaseClass0>)
		ldstr "G3_C1729::ClassMethod4857.17680<System.Object>()"
		ldstr "class G3_C1729`1<class BaseClass0> on type class G3_C1729`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1729`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1729`1<class BaseClass0>::Method7<object>()
		calli default string(class G3_C1729`1<class BaseClass0>)
		ldstr "G3_C1729::Method7.17678<System.Object>()"
		ldstr "class G3_C1729`1<class BaseClass0> on type class G3_C1729`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1729`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1729`1<class BaseClass0>::ClassMethod2764<object>()
		calli default string(class G3_C1729`1<class BaseClass0>)
		ldstr "G3_C1729::ClassMethod2764.MI.17683<System.Object>()"
		ldstr "class G3_C1729`1<class BaseClass0> on type class G3_C1729`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1729`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1729`1<class BaseClass0>::ClassMethod2763<object>()
		calli default string(class G3_C1729`1<class BaseClass0>)
		ldstr "G3_C1729::ClassMethod2763.MI.17682<System.Object>()"
		ldstr "class G3_C1729`1<class BaseClass0> on type class G3_C1729`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1729`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1729`1<class BaseClass0>::ClassMethod2762()
		calli default string(class G3_C1729`1<class BaseClass0>)
		ldstr "G3_C1729::ClassMethod2762.MI.17681()"
		ldstr "class G3_C1729`1<class BaseClass0> on type class G3_C1729`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1729`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1729`1<class BaseClass0>::ClassMethod2761()
		calli default string(class G3_C1729`1<class BaseClass0>)
		ldstr "G2_C699::ClassMethod2761.11451()"
		ldstr "class G3_C1729`1<class BaseClass0> on type class G3_C1729`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1729`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1729`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1729`1<class BaseClass0>)
		ldstr "G2_C699::Method6.11449<System.Object>()"
		ldstr "class G3_C1729`1<class BaseClass0> on type class G3_C1729`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1729`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1729`1<class BaseClass0>::Method5()
		calli default string(class G3_C1729`1<class BaseClass0>)
		ldstr "G2_C699::Method5.11447()"
		ldstr "class G3_C1729`1<class BaseClass0> on type class G3_C1729`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1729`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1729`1<class BaseClass0>::Method4()
		calli default string(class G3_C1729`1<class BaseClass0>)
		ldstr "G2_C699::Method4.11445()"
		ldstr "class G3_C1729`1<class BaseClass0> on type class G3_C1729`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1729`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1729`1<class BaseClass0>::ClassMethod1349<object>()
		calli default string(class G3_C1729`1<class BaseClass0>)
		ldstr "G2_C699::ClassMethod1349.MI.11455<System.Object>()"
		ldstr "class G3_C1729`1<class BaseClass0> on type class G3_C1729`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1729`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1729`1<class BaseClass0>::ClassMethod1348<object>()
		calli default string(class G3_C1729`1<class BaseClass0>)
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G3_C1729`1<class BaseClass0> on type class G3_C1729`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G3_C1729`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C13`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass0,class BaseClass0>::ClassMethod1349<object>()
		calli default string(class G3_C1729`1<class BaseClass1>)
		ldstr "G2_C699::ClassMethod1349.MI.11455<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass0> on type class G3_C1729`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C13`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass0,class BaseClass0>::ClassMethod1348<object>()
		calli default string(class G3_C1729`1<class BaseClass1>)
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass0> on type class G3_C1729`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C13`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass0,class BaseClass0>::Method6<object>()
		calli default string(class G3_C1729`1<class BaseClass1>)
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass0> on type class G3_C1729`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C13`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass0,class BaseClass0>::Method5()
		calli default string(class G3_C1729`1<class BaseClass1>)
		ldstr "G1_C13::Method5.4873()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass0> on type class G3_C1729`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C13`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass0,class BaseClass0>::Method4()
		calli default string(class G3_C1729`1<class BaseClass1>)
		ldstr "G1_C13::Method4.4872()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass0> on type class G3_C1729`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C13`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1729`1<class BaseClass1>)
		ldstr "G1_C13::Method7.4871<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass0> on type class G3_C1729`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1729`1<class BaseClass1>)
		ldstr "G3_C1729::Method7.17678<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1729`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G3_C1729`1<class BaseClass1>)
		ldstr "G2_C699::Method4.MI.11446()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1729`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G3_C1729`1<class BaseClass1>)
		ldstr "G2_C699::Method5.MI.11448()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1729`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1729`1<class BaseClass1>)
		ldstr "G2_C699::Method6.11449<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1729`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1729`1<class BaseClass1>)
		ldstr "G3_C1729::Method7.MI.17679<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1729`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C699`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C699`2<class BaseClass1,class BaseClass0>::ClassMethod2764<object>()
		calli default string(class G3_C1729`1<class BaseClass1>)
		ldstr "G3_C1729::ClassMethod2764.MI.17683<System.Object>()"
		ldstr "class G2_C699`2<class BaseClass1,class BaseClass0> on type class G3_C1729`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C699`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C699`2<class BaseClass1,class BaseClass0>::ClassMethod2763<object>()
		calli default string(class G3_C1729`1<class BaseClass1>)
		ldstr "G3_C1729::ClassMethod2763.MI.17682<System.Object>()"
		ldstr "class G2_C699`2<class BaseClass1,class BaseClass0> on type class G3_C1729`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C699`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C699`2<class BaseClass1,class BaseClass0>::ClassMethod2762()
		calli default string(class G3_C1729`1<class BaseClass1>)
		ldstr "G3_C1729::ClassMethod2762.MI.17681()"
		ldstr "class G2_C699`2<class BaseClass1,class BaseClass0> on type class G3_C1729`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C699`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C699`2<class BaseClass1,class BaseClass0>::ClassMethod2761()
		calli default string(class G3_C1729`1<class BaseClass1>)
		ldstr "G2_C699::ClassMethod2761.11451()"
		ldstr "class G2_C699`2<class BaseClass1,class BaseClass0> on type class G3_C1729`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C699`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C699`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1729`1<class BaseClass1>)
		ldstr "G3_C1729::Method7.17678<System.Object>()"
		ldstr "class G2_C699`2<class BaseClass1,class BaseClass0> on type class G3_C1729`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C699`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C699`2<class BaseClass1,class BaseClass0>::Method6<object>()
		calli default string(class G3_C1729`1<class BaseClass1>)
		ldstr "G2_C699::Method6.11449<System.Object>()"
		ldstr "class G2_C699`2<class BaseClass1,class BaseClass0> on type class G3_C1729`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C699`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C699`2<class BaseClass1,class BaseClass0>::Method5()
		calli default string(class G3_C1729`1<class BaseClass1>)
		ldstr "G2_C699::Method5.11447()"
		ldstr "class G2_C699`2<class BaseClass1,class BaseClass0> on type class G3_C1729`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C699`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C699`2<class BaseClass1,class BaseClass0>::Method4()
		calli default string(class G3_C1729`1<class BaseClass1>)
		ldstr "G2_C699::Method4.11445()"
		ldstr "class G2_C699`2<class BaseClass1,class BaseClass0> on type class G3_C1729`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C699`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C699`2<class BaseClass1,class BaseClass0>::ClassMethod1349<object>()
		calli default string(class G3_C1729`1<class BaseClass1>)
		ldstr "G2_C699::ClassMethod1349.MI.11455<System.Object>()"
		ldstr "class G2_C699`2<class BaseClass1,class BaseClass0> on type class G3_C1729`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C699`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C699`2<class BaseClass1,class BaseClass0>::ClassMethod1348<object>()
		calli default string(class G3_C1729`1<class BaseClass1>)
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G2_C699`2<class BaseClass1,class BaseClass0> on type class G3_C1729`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G3_C1729`1<class BaseClass1>)
		ldstr "G2_C699::Method4.MI.11446()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1729`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G3_C1729`1<class BaseClass1>)
		ldstr "G2_C699::Method5.MI.11448()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1729`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G3_C1729`1<class BaseClass1>)
		ldstr "G2_C699::Method6.11449<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1729`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1729`1<class BaseClass1>)
		ldstr "G3_C1729::Method7.17678<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G3_C1729`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1729`1<class BaseClass1>)
		ldstr "G3_C1729::Method7.MI.17679<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1729`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1729`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1729`1<class BaseClass1>::ClassMethod4857<object>()
		calli default string(class G3_C1729`1<class BaseClass1>)
		ldstr "G3_C1729::ClassMethod4857.17680<System.Object>()"
		ldstr "class G3_C1729`1<class BaseClass1> on type class G3_C1729`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1729`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1729`1<class BaseClass1>::Method7<object>()
		calli default string(class G3_C1729`1<class BaseClass1>)
		ldstr "G3_C1729::Method7.17678<System.Object>()"
		ldstr "class G3_C1729`1<class BaseClass1> on type class G3_C1729`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1729`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1729`1<class BaseClass1>::ClassMethod2764<object>()
		calli default string(class G3_C1729`1<class BaseClass1>)
		ldstr "G3_C1729::ClassMethod2764.MI.17683<System.Object>()"
		ldstr "class G3_C1729`1<class BaseClass1> on type class G3_C1729`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1729`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1729`1<class BaseClass1>::ClassMethod2763<object>()
		calli default string(class G3_C1729`1<class BaseClass1>)
		ldstr "G3_C1729::ClassMethod2763.MI.17682<System.Object>()"
		ldstr "class G3_C1729`1<class BaseClass1> on type class G3_C1729`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1729`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1729`1<class BaseClass1>::ClassMethod2762()
		calli default string(class G3_C1729`1<class BaseClass1>)
		ldstr "G3_C1729::ClassMethod2762.MI.17681()"
		ldstr "class G3_C1729`1<class BaseClass1> on type class G3_C1729`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1729`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1729`1<class BaseClass1>::ClassMethod2761()
		calli default string(class G3_C1729`1<class BaseClass1>)
		ldstr "G2_C699::ClassMethod2761.11451()"
		ldstr "class G3_C1729`1<class BaseClass1> on type class G3_C1729`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1729`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1729`1<class BaseClass1>::Method6<object>()
		calli default string(class G3_C1729`1<class BaseClass1>)
		ldstr "G2_C699::Method6.11449<System.Object>()"
		ldstr "class G3_C1729`1<class BaseClass1> on type class G3_C1729`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1729`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1729`1<class BaseClass1>::Method5()
		calli default string(class G3_C1729`1<class BaseClass1>)
		ldstr "G2_C699::Method5.11447()"
		ldstr "class G3_C1729`1<class BaseClass1> on type class G3_C1729`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1729`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1729`1<class BaseClass1>::Method4()
		calli default string(class G3_C1729`1<class BaseClass1>)
		ldstr "G2_C699::Method4.11445()"
		ldstr "class G3_C1729`1<class BaseClass1> on type class G3_C1729`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1729`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1729`1<class BaseClass1>::ClassMethod1349<object>()
		calli default string(class G3_C1729`1<class BaseClass1>)
		ldstr "G2_C699::ClassMethod1349.MI.11455<System.Object>()"
		ldstr "class G3_C1729`1<class BaseClass1> on type class G3_C1729`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1729`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1729`1<class BaseClass1>::ClassMethod1348<object>()
		calli default string(class G3_C1729`1<class BaseClass1>)
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G3_C1729`1<class BaseClass1> on type class G3_C1729`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C699`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C13`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass0,class BaseClass0>::ClassMethod1349<object>()
		calli default string(class G2_C699`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C699::ClassMethod1349.MI.11455<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass0> on type class G2_C699`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C13`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass0,class BaseClass0>::ClassMethod1348<object>()
		calli default string(class G2_C699`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass0> on type class G2_C699`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C13`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass0,class BaseClass0>::Method6<object>()
		calli default string(class G2_C699`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass0> on type class G2_C699`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C13`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass0,class BaseClass0>::Method5()
		calli default string(class G2_C699`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C13::Method5.4873()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass0> on type class G2_C699`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C13`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass0,class BaseClass0>::Method4()
		calli default string(class G2_C699`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C13::Method4.4872()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass0> on type class G2_C699`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C13`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C699`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C13::Method7.4871<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass0> on type class G2_C699`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C699`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C699::Method7.11450<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C699`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G2_C699`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C699::Method4.MI.11446()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C699`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G2_C699`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C699::Method5.MI.11448()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C699`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G2_C699`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C699::Method6.11449<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C699`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C699`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C699::Method7.11450<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C699`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C699`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C699`2<class BaseClass0,class BaseClass0>::ClassMethod2764<object>()
		calli default string(class G2_C699`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C699::ClassMethod2764.11454<System.Object>()"
		ldstr "class G2_C699`2<class BaseClass0,class BaseClass0> on type class G2_C699`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C699`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C699`2<class BaseClass0,class BaseClass0>::ClassMethod2763<object>()
		calli default string(class G2_C699`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C699::ClassMethod2763.11453<System.Object>()"
		ldstr "class G2_C699`2<class BaseClass0,class BaseClass0> on type class G2_C699`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C699`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C699`2<class BaseClass0,class BaseClass0>::ClassMethod2762()
		calli default string(class G2_C699`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C699::ClassMethod2762.11452()"
		ldstr "class G2_C699`2<class BaseClass0,class BaseClass0> on type class G2_C699`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C699`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C699`2<class BaseClass0,class BaseClass0>::ClassMethod2761()
		calli default string(class G2_C699`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C699::ClassMethod2761.11451()"
		ldstr "class G2_C699`2<class BaseClass0,class BaseClass0> on type class G2_C699`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C699`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C699`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C699`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C699::Method7.11450<System.Object>()"
		ldstr "class G2_C699`2<class BaseClass0,class BaseClass0> on type class G2_C699`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C699`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C699`2<class BaseClass0,class BaseClass0>::Method6<object>()
		calli default string(class G2_C699`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C699::Method6.11449<System.Object>()"
		ldstr "class G2_C699`2<class BaseClass0,class BaseClass0> on type class G2_C699`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C699`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C699`2<class BaseClass0,class BaseClass0>::Method5()
		calli default string(class G2_C699`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C699::Method5.11447()"
		ldstr "class G2_C699`2<class BaseClass0,class BaseClass0> on type class G2_C699`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C699`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C699`2<class BaseClass0,class BaseClass0>::Method4()
		calli default string(class G2_C699`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C699::Method4.11445()"
		ldstr "class G2_C699`2<class BaseClass0,class BaseClass0> on type class G2_C699`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C699`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C699`2<class BaseClass0,class BaseClass0>::ClassMethod1349<object>()
		calli default string(class G2_C699`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C699::ClassMethod1349.MI.11455<System.Object>()"
		ldstr "class G2_C699`2<class BaseClass0,class BaseClass0> on type class G2_C699`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C699`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C699`2<class BaseClass0,class BaseClass0>::ClassMethod1348<object>()
		calli default string(class G2_C699`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G2_C699`2<class BaseClass0,class BaseClass0> on type class G2_C699`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G2_C699`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C699::Method7.11450<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G2_C699`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C699`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C699::Method7.11450<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C699`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C699`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C13`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass0,class BaseClass1>::ClassMethod1349<object>()
		calli default string(class G2_C699`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C699::ClassMethod1349.MI.11455<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass1> on type class G2_C699`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C13`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass0,class BaseClass1>::ClassMethod1348<object>()
		calli default string(class G2_C699`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass1> on type class G2_C699`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C13`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass0,class BaseClass1>::Method6<object>()
		calli default string(class G2_C699`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass1> on type class G2_C699`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C13`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass0,class BaseClass1>::Method5()
		calli default string(class G2_C699`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C13::Method5.4873()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass1> on type class G2_C699`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C13`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass0,class BaseClass1>::Method4()
		calli default string(class G2_C699`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C13::Method4.4872()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass1> on type class G2_C699`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C13`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C699`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C13::Method7.4871<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass1> on type class G2_C699`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C699`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C699::Method7.11450<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C699`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G2_C699`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C699::Method4.MI.11446()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C699`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G2_C699`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C699::Method5.MI.11448()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C699`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G2_C699`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C699::Method6.11449<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C699`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C699`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C699`2<class BaseClass0,class BaseClass1>::ClassMethod2764<object>()
		calli default string(class G2_C699`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C699::ClassMethod2764.11454<System.Object>()"
		ldstr "class G2_C699`2<class BaseClass0,class BaseClass1> on type class G2_C699`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C699`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C699`2<class BaseClass0,class BaseClass1>::ClassMethod2763<object>()
		calli default string(class G2_C699`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C699::ClassMethod2763.11453<System.Object>()"
		ldstr "class G2_C699`2<class BaseClass0,class BaseClass1> on type class G2_C699`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C699`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C699`2<class BaseClass0,class BaseClass1>::ClassMethod2762()
		calli default string(class G2_C699`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C699::ClassMethod2762.11452()"
		ldstr "class G2_C699`2<class BaseClass0,class BaseClass1> on type class G2_C699`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C699`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C699`2<class BaseClass0,class BaseClass1>::ClassMethod2761()
		calli default string(class G2_C699`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C699::ClassMethod2761.11451()"
		ldstr "class G2_C699`2<class BaseClass0,class BaseClass1> on type class G2_C699`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C699`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C699`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C699`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C699::Method7.11450<System.Object>()"
		ldstr "class G2_C699`2<class BaseClass0,class BaseClass1> on type class G2_C699`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C699`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C699`2<class BaseClass0,class BaseClass1>::Method6<object>()
		calli default string(class G2_C699`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C699::Method6.11449<System.Object>()"
		ldstr "class G2_C699`2<class BaseClass0,class BaseClass1> on type class G2_C699`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C699`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C699`2<class BaseClass0,class BaseClass1>::Method5()
		calli default string(class G2_C699`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C699::Method5.11447()"
		ldstr "class G2_C699`2<class BaseClass0,class BaseClass1> on type class G2_C699`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C699`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C699`2<class BaseClass0,class BaseClass1>::Method4()
		calli default string(class G2_C699`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C699::Method4.11445()"
		ldstr "class G2_C699`2<class BaseClass0,class BaseClass1> on type class G2_C699`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C699`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C699`2<class BaseClass0,class BaseClass1>::ClassMethod1349<object>()
		calli default string(class G2_C699`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C699::ClassMethod1349.MI.11455<System.Object>()"
		ldstr "class G2_C699`2<class BaseClass0,class BaseClass1> on type class G2_C699`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C699`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C699`2<class BaseClass0,class BaseClass1>::ClassMethod1348<object>()
		calli default string(class G2_C699`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G2_C699`2<class BaseClass0,class BaseClass1> on type class G2_C699`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C699`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C699::Method7.11450<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C699`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C699`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C13`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass0,class BaseClass0>::ClassMethod1349<object>()
		calli default string(class G2_C699`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C699::ClassMethod1349.MI.11455<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass0> on type class G2_C699`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C13`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass0,class BaseClass0>::ClassMethod1348<object>()
		calli default string(class G2_C699`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass0> on type class G2_C699`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C13`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass0,class BaseClass0>::Method6<object>()
		calli default string(class G2_C699`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass0> on type class G2_C699`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C13`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass0,class BaseClass0>::Method5()
		calli default string(class G2_C699`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C13::Method5.4873()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass0> on type class G2_C699`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C13`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass0,class BaseClass0>::Method4()
		calli default string(class G2_C699`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C13::Method4.4872()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass0> on type class G2_C699`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C13`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C699`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C13::Method7.4871<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass0> on type class G2_C699`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C699`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C699::Method7.11450<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C699`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G2_C699`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C699::Method4.MI.11446()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C699`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G2_C699`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C699::Method5.MI.11448()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C699`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G2_C699`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C699::Method6.11449<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C699`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C699`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C699::Method7.11450<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C699`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C699`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C699`2<class BaseClass1,class BaseClass0>::ClassMethod2764<object>()
		calli default string(class G2_C699`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C699::ClassMethod2764.11454<System.Object>()"
		ldstr "class G2_C699`2<class BaseClass1,class BaseClass0> on type class G2_C699`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C699`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C699`2<class BaseClass1,class BaseClass0>::ClassMethod2763<object>()
		calli default string(class G2_C699`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C699::ClassMethod2763.11453<System.Object>()"
		ldstr "class G2_C699`2<class BaseClass1,class BaseClass0> on type class G2_C699`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C699`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C699`2<class BaseClass1,class BaseClass0>::ClassMethod2762()
		calli default string(class G2_C699`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C699::ClassMethod2762.11452()"
		ldstr "class G2_C699`2<class BaseClass1,class BaseClass0> on type class G2_C699`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C699`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C699`2<class BaseClass1,class BaseClass0>::ClassMethod2761()
		calli default string(class G2_C699`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C699::ClassMethod2761.11451()"
		ldstr "class G2_C699`2<class BaseClass1,class BaseClass0> on type class G2_C699`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C699`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C699`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G2_C699`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C699::Method7.11450<System.Object>()"
		ldstr "class G2_C699`2<class BaseClass1,class BaseClass0> on type class G2_C699`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C699`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C699`2<class BaseClass1,class BaseClass0>::Method6<object>()
		calli default string(class G2_C699`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C699::Method6.11449<System.Object>()"
		ldstr "class G2_C699`2<class BaseClass1,class BaseClass0> on type class G2_C699`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C699`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C699`2<class BaseClass1,class BaseClass0>::Method5()
		calli default string(class G2_C699`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C699::Method5.11447()"
		ldstr "class G2_C699`2<class BaseClass1,class BaseClass0> on type class G2_C699`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C699`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C699`2<class BaseClass1,class BaseClass0>::Method4()
		calli default string(class G2_C699`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C699::Method4.11445()"
		ldstr "class G2_C699`2<class BaseClass1,class BaseClass0> on type class G2_C699`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C699`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C699`2<class BaseClass1,class BaseClass0>::ClassMethod1349<object>()
		calli default string(class G2_C699`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C699::ClassMethod1349.MI.11455<System.Object>()"
		ldstr "class G2_C699`2<class BaseClass1,class BaseClass0> on type class G2_C699`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C699`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C699`2<class BaseClass1,class BaseClass0>::ClassMethod1348<object>()
		calli default string(class G2_C699`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G2_C699`2<class BaseClass1,class BaseClass0> on type class G2_C699`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G2_C699`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C699::Method4.MI.11446()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C699`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G2_C699`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C699::Method5.MI.11448()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C699`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G2_C699`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C699::Method6.11449<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C699`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G2_C699`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C699::Method7.11450<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G2_C699`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C699`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C699::Method7.11450<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C699`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C699`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C13`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass0,class BaseClass1>::ClassMethod1349<object>()
		calli default string(class G2_C699`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C699::ClassMethod1349.MI.11455<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass1> on type class G2_C699`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C13`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass0,class BaseClass1>::ClassMethod1348<object>()
		calli default string(class G2_C699`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass1> on type class G2_C699`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C13`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass0,class BaseClass1>::Method6<object>()
		calli default string(class G2_C699`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass1> on type class G2_C699`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C13`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass0,class BaseClass1>::Method5()
		calli default string(class G2_C699`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C13::Method5.4873()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass1> on type class G2_C699`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C13`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass0,class BaseClass1>::Method4()
		calli default string(class G2_C699`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C13::Method4.4872()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass1> on type class G2_C699`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C13`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C699`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C13::Method7.4871<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass0,class BaseClass1> on type class G2_C699`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C699`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C699::Method7.11450<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C699`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G2_C699`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C699::Method4.MI.11446()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C699`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G2_C699`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C699::Method5.MI.11448()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C699`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G2_C699`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C699::Method6.11449<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C699`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C699`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C699`2<class BaseClass1,class BaseClass1>::ClassMethod2764<object>()
		calli default string(class G2_C699`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C699::ClassMethod2764.11454<System.Object>()"
		ldstr "class G2_C699`2<class BaseClass1,class BaseClass1> on type class G2_C699`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C699`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C699`2<class BaseClass1,class BaseClass1>::ClassMethod2763<object>()
		calli default string(class G2_C699`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C699::ClassMethod2763.11453<System.Object>()"
		ldstr "class G2_C699`2<class BaseClass1,class BaseClass1> on type class G2_C699`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C699`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C699`2<class BaseClass1,class BaseClass1>::ClassMethod2762()
		calli default string(class G2_C699`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C699::ClassMethod2762.11452()"
		ldstr "class G2_C699`2<class BaseClass1,class BaseClass1> on type class G2_C699`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C699`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C699`2<class BaseClass1,class BaseClass1>::ClassMethod2761()
		calli default string(class G2_C699`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C699::ClassMethod2761.11451()"
		ldstr "class G2_C699`2<class BaseClass1,class BaseClass1> on type class G2_C699`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C699`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C699`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C699`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C699::Method7.11450<System.Object>()"
		ldstr "class G2_C699`2<class BaseClass1,class BaseClass1> on type class G2_C699`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C699`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C699`2<class BaseClass1,class BaseClass1>::Method6<object>()
		calli default string(class G2_C699`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C699::Method6.11449<System.Object>()"
		ldstr "class G2_C699`2<class BaseClass1,class BaseClass1> on type class G2_C699`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C699`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C699`2<class BaseClass1,class BaseClass1>::Method5()
		calli default string(class G2_C699`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C699::Method5.11447()"
		ldstr "class G2_C699`2<class BaseClass1,class BaseClass1> on type class G2_C699`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C699`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C699`2<class BaseClass1,class BaseClass1>::Method4()
		calli default string(class G2_C699`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C699::Method4.11445()"
		ldstr "class G2_C699`2<class BaseClass1,class BaseClass1> on type class G2_C699`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C699`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C699`2<class BaseClass1,class BaseClass1>::ClassMethod1349<object>()
		calli default string(class G2_C699`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C699::ClassMethod1349.MI.11455<System.Object>()"
		ldstr "class G2_C699`2<class BaseClass1,class BaseClass1> on type class G2_C699`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C699`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C699`2<class BaseClass1,class BaseClass1>::ClassMethod1348<object>()
		calli default string(class G2_C699`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G2_C699`2<class BaseClass1,class BaseClass1> on type class G2_C699`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G2_C699`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C699::Method4.MI.11446()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C699`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G2_C699`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C699::Method5.MI.11448()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C699`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G2_C699`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C699::Method6.11449<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C699`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C699`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C699::Method7.11450<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C699`2<class BaseClass1,class BaseClass1>"
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
		call void Generated1257::MethodCallingTest()
		call void Generated1257::ConstrainedCallsTest()
		call void Generated1257::StructConstrainedInterfaceCallsTest()
		call void Generated1257::CalliTest()
		ldc.i4 100
		ret
	}
}
