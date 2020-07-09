// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) .ver 4:0:0:0 }
.assembly extern TestFramework { .publickeytoken = ( B0 3F 5F 7F 11 D5 0A 3A )  }

//TYPES IN FORWARDER ASSEMBLIES:

//TEST ASSEMBLY:
.assembly Generated645 { .hash algorithm 0x00008004 }
.module Generated645.exe

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
.class public G3_C1117`1<T0> 
		extends class G2_C133`1<class BaseClass0>
		implements class IBase2`2<!T0,class BaseClass0> 
{
	.method public hidebysig newslot virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G3_C1117::Method7.14409<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase2<T0,class BaseClass0>.Method7'<M0>() cil managed noinlining { 
		.override method instance string class IBase2`2<!T0,class BaseClass0>::Method7<[1]>()
		ldstr "G3_C1117::Method7.MI.14410<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod3630() cil managed noinlining { 
		ldstr "G3_C1117::ClassMethod3630.14411()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod3631() cil managed noinlining { 
		ldstr "G3_C1117::ClassMethod3631.14412()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod3632<M0>() cil managed noinlining { 
		ldstr "G3_C1117::ClassMethod3632.14413<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod3633<M0>() cil managed noinlining { 
		ldstr "G3_C1117::ClassMethod3633.14414<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C133<class BaseClass0>.ClassMethod1583'<M0>() cil managed noinlining { 
		.override method instance string class G2_C133`1<class BaseClass0>::ClassMethod1583<[1]>()
		ldstr "G3_C1117::ClassMethod1583.MI.14415<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C133<class BaseClass0>.ClassMethod1584'<M0>() cil managed noinlining { 
		.override method instance string class G2_C133`1<class BaseClass0>::ClassMethod1584<[1]>()
		ldstr "G3_C1117::ClassMethod1584.MI.14416<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class G2_C133`1<class BaseClass0>::.ctor()
		ret
	}
}
.class public G2_C133`1<T0> 
		extends class G1_C10`2<class BaseClass0,class BaseClass1>
		implements class IBase2`2<!T0,class BaseClass0>, class IBase1`1<class BaseClass0> 
{
	.method public hidebysig virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G2_C133::Method7.6015<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig virtual instance string Method4() cil managed noinlining { 
		ldstr "G2_C133::Method4.6016()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class BaseClass0>.Method4'() cil managed noinlining { 
		.override method instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C133::Method4.MI.6017()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method5() cil managed noinlining { 
		ldstr "G2_C133::Method5.6018()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class BaseClass0>.Method5'() cil managed noinlining { 
		.override method instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C133::Method5.MI.6019()"
		ret
	}
	.method public hidebysig virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G2_C133::Method6.6020<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class BaseClass0>.Method6'<M0>() cil managed noinlining { 
		.override method instance string class IBase1`1<class BaseClass0>::Method6<[1]>()
		ldstr "G2_C133::Method6.MI.6021<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1582() cil managed noinlining { 
		ldstr "G2_C133::ClassMethod1582.6022()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1583<M0>() cil managed noinlining { 
		ldstr "G2_C133::ClassMethod1583.6023<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1584<M0>() cil managed noinlining { 
		ldstr "G2_C133::ClassMethod1584.6024<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C10<class BaseClass0,class BaseClass1>.ClassMethod1340'() cil managed noinlining { 
		.override method instance string class G1_C10`2<class BaseClass0,class BaseClass1>::ClassMethod1340()
		ldstr "G2_C133::ClassMethod1340.MI.6025()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C10<class BaseClass0,class BaseClass1>.ClassMethod1342'<M0>() cil managed noinlining { 
		.override method instance string class G1_C10`2<class BaseClass0,class BaseClass1>::ClassMethod1342<[1]>()
		ldstr "G2_C133::ClassMethod1342.MI.6026<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C10<class BaseClass0,class BaseClass1>.ClassMethod1343'<M0>() cil managed noinlining { 
		.override method instance string class G1_C10`2<class BaseClass0,class BaseClass1>::ClassMethod1343<[1]>()
		ldstr "G2_C133::ClassMethod1343.MI.6027<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class G1_C10`2<class BaseClass0,class BaseClass1>::.ctor()
		ret
	}
}
.class interface public abstract IBase2`2<+T0, -T1> 
{
	.method public hidebysig newslot abstract virtual instance string Method7<M0>() cil managed { }
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
	.method static void M.G3_C1117.T<T0,(class G3_C1117`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 20
		.locals init (string[] actualResults)
		ldc.i4.s    15
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1117.T<T0,(class G3_C1117`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 15
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1117`1<!!T0>::ClassMethod1340()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1117`1<!!T0>::ClassMethod1341()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1117`1<!!T0>::ClassMethod1342<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1117`1<!!T0>::ClassMethod1343<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1117`1<!!T0>::ClassMethod1582()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1117`1<!!T0>::ClassMethod1583<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1117`1<!!T0>::ClassMethod1584<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1117`1<!!T0>::ClassMethod3630()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1117`1<!!T0>::ClassMethod3631()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1117`1<!!T0>::ClassMethod3632<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1117`1<!!T0>::ClassMethod3633<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1117`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1117`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1117`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     14
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1117`1<!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1117.A<(class G3_C1117`1<class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 20
		.locals init (string[] actualResults)
		ldc.i4.s    15
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1117.A<(class G3_C1117`1<class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 15
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1117`1<class BaseClass0>::ClassMethod1340()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1117`1<class BaseClass0>::ClassMethod1341()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1117`1<class BaseClass0>::ClassMethod1342<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1117`1<class BaseClass0>::ClassMethod1343<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1117`1<class BaseClass0>::ClassMethod1582()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1117`1<class BaseClass0>::ClassMethod1583<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1117`1<class BaseClass0>::ClassMethod1584<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1117`1<class BaseClass0>::ClassMethod3630()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1117`1<class BaseClass0>::ClassMethod3631()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1117`1<class BaseClass0>::ClassMethod3632<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1117`1<class BaseClass0>::ClassMethod3633<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1117`1<class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1117`1<class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1117`1<class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     14
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1117`1<class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1117.B<(class G3_C1117`1<class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 20
		.locals init (string[] actualResults)
		ldc.i4.s    15
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1117.B<(class G3_C1117`1<class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 15
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1117`1<class BaseClass1>::ClassMethod1340()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1117`1<class BaseClass1>::ClassMethod1341()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1117`1<class BaseClass1>::ClassMethod1342<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1117`1<class BaseClass1>::ClassMethod1343<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1117`1<class BaseClass1>::ClassMethod1582()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1117`1<class BaseClass1>::ClassMethod1583<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1117`1<class BaseClass1>::ClassMethod1584<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1117`1<class BaseClass1>::ClassMethod3630()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1117`1<class BaseClass1>::ClassMethod3631()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1117`1<class BaseClass1>::ClassMethod3632<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1117`1<class BaseClass1>::ClassMethod3633<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1117`1<class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1117`1<class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1117`1<class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     14
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1117`1<class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C133.T<T0,(class G2_C133`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C133.T<T0,(class G2_C133`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C133`1<!!T0>::ClassMethod1340()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C133`1<!!T0>::ClassMethod1341()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C133`1<!!T0>::ClassMethod1342<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C133`1<!!T0>::ClassMethod1343<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C133`1<!!T0>::ClassMethod1582()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C133`1<!!T0>::ClassMethod1583<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C133`1<!!T0>::ClassMethod1584<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C133`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C133`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C133`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C133`1<!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C133.A<(class G2_C133`1<class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C133.A<(class G2_C133`1<class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C133`1<class BaseClass0>::ClassMethod1340()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C133`1<class BaseClass0>::ClassMethod1341()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C133`1<class BaseClass0>::ClassMethod1342<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C133`1<class BaseClass0>::ClassMethod1343<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C133`1<class BaseClass0>::ClassMethod1582()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C133`1<class BaseClass0>::ClassMethod1583<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C133`1<class BaseClass0>::ClassMethod1584<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C133`1<class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C133`1<class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C133`1<class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C133`1<class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C133.B<(class G2_C133`1<class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C133.B<(class G2_C133`1<class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C133`1<class BaseClass1>::ClassMethod1340()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C133`1<class BaseClass1>::ClassMethod1341()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C133`1<class BaseClass1>::ClassMethod1342<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C133`1<class BaseClass1>::ClassMethod1343<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C133`1<class BaseClass1>::ClassMethod1582()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C133`1<class BaseClass1>::ClassMethod1583<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C133`1<class BaseClass1>::ClassMethod1584<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C133`1<class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C133`1<class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C133`1<class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C133`1<class BaseClass1>::Method7<object>()
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
		newobj instance void class G3_C1117`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass0,class BaseClass1>::ClassMethod1343<object>()
		ldstr "G2_C133::ClassMethod1343.MI.6027<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G3_C1117`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass0,class BaseClass1>::ClassMethod1342<object>()
		ldstr "G2_C133::ClassMethod1342.MI.6026<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G3_C1117`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass0,class BaseClass1>::ClassMethod1341()
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G3_C1117`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass0,class BaseClass1>::ClassMethod1340()
		ldstr "G2_C133::ClassMethod1340.MI.6025()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G3_C1117`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass0,class BaseClass1>::Method6<object>()
		ldstr "G2_C133::Method6.6020<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G3_C1117`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass0,class BaseClass1>::Method5()
		ldstr "G1_C10::Method5.4847()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G3_C1117`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass0,class BaseClass1>::Method4()
		ldstr "G2_C133::Method4.6016()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G3_C1117`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C133::Method7.6015<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G3_C1117`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C133::Method7.6015<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1117`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C133::Method4.MI.6017()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1117`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C133::Method5.MI.6019()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1117`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C133::Method6.MI.6021<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1117`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G3_C1117::Method7.MI.14410<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1117`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C133`1<class BaseClass0>
		callvirt instance string class G2_C133`1<class BaseClass0>::ClassMethod1584<object>()
		ldstr "G3_C1117::ClassMethod1584.MI.14416<System.Object>()"
		ldstr "class G2_C133`1<class BaseClass0> on type class G3_C1117`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C133`1<class BaseClass0>
		callvirt instance string class G2_C133`1<class BaseClass0>::ClassMethod1583<object>()
		ldstr "G3_C1117::ClassMethod1583.MI.14415<System.Object>()"
		ldstr "class G2_C133`1<class BaseClass0> on type class G3_C1117`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C133`1<class BaseClass0>
		callvirt instance string class G2_C133`1<class BaseClass0>::ClassMethod1582()
		ldstr "G2_C133::ClassMethod1582.6022()"
		ldstr "class G2_C133`1<class BaseClass0> on type class G3_C1117`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C133`1<class BaseClass0>
		callvirt instance string class G2_C133`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C133::Method6.6020<System.Object>()"
		ldstr "class G2_C133`1<class BaseClass0> on type class G3_C1117`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C133`1<class BaseClass0>
		callvirt instance string class G2_C133`1<class BaseClass0>::Method5()
		ldstr "G2_C133::Method5.6018()"
		ldstr "class G2_C133`1<class BaseClass0> on type class G3_C1117`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C133`1<class BaseClass0>
		callvirt instance string class G2_C133`1<class BaseClass0>::Method4()
		ldstr "G2_C133::Method4.6016()"
		ldstr "class G2_C133`1<class BaseClass0> on type class G3_C1117`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C133`1<class BaseClass0>
		callvirt instance string class G2_C133`1<class BaseClass0>::Method7<object>()
		ldstr "G2_C133::Method7.6015<System.Object>()"
		ldstr "class G2_C133`1<class BaseClass0> on type class G3_C1117`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C133`1<class BaseClass0>
		callvirt instance string class G2_C133`1<class BaseClass0>::ClassMethod1343<object>()
		ldstr "G2_C133::ClassMethod1343.MI.6027<System.Object>()"
		ldstr "class G2_C133`1<class BaseClass0> on type class G3_C1117`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C133`1<class BaseClass0>
		callvirt instance string class G2_C133`1<class BaseClass0>::ClassMethod1342<object>()
		ldstr "G2_C133::ClassMethod1342.MI.6026<System.Object>()"
		ldstr "class G2_C133`1<class BaseClass0> on type class G3_C1117`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C133`1<class BaseClass0>
		callvirt instance string class G2_C133`1<class BaseClass0>::ClassMethod1341()
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G2_C133`1<class BaseClass0> on type class G3_C1117`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C133`1<class BaseClass0>
		callvirt instance string class G2_C133`1<class BaseClass0>::ClassMethod1340()
		ldstr "G2_C133::ClassMethod1340.MI.6025()"
		ldstr "class G2_C133`1<class BaseClass0> on type class G3_C1117`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G3_C1117::Method7.MI.14410<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1117`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G3_C1117`1<class BaseClass0>
		callvirt instance string class G3_C1117`1<class BaseClass0>::ClassMethod3633<object>()
		ldstr "G3_C1117::ClassMethod3633.14414<System.Object>()"
		ldstr "class G3_C1117`1<class BaseClass0> on type class G3_C1117`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1117`1<class BaseClass0>
		callvirt instance string class G3_C1117`1<class BaseClass0>::ClassMethod3632<object>()
		ldstr "G3_C1117::ClassMethod3632.14413<System.Object>()"
		ldstr "class G3_C1117`1<class BaseClass0> on type class G3_C1117`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1117`1<class BaseClass0>
		callvirt instance string class G3_C1117`1<class BaseClass0>::ClassMethod3631()
		ldstr "G3_C1117::ClassMethod3631.14412()"
		ldstr "class G3_C1117`1<class BaseClass0> on type class G3_C1117`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1117`1<class BaseClass0>
		callvirt instance string class G3_C1117`1<class BaseClass0>::ClassMethod3630()
		ldstr "G3_C1117::ClassMethod3630.14411()"
		ldstr "class G3_C1117`1<class BaseClass0> on type class G3_C1117`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1117`1<class BaseClass0>
		callvirt instance string class G3_C1117`1<class BaseClass0>::Method7<object>()
		ldstr "G3_C1117::Method7.14409<System.Object>()"
		ldstr "class G3_C1117`1<class BaseClass0> on type class G3_C1117`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1117`1<class BaseClass0>
		callvirt instance string class G3_C1117`1<class BaseClass0>::ClassMethod1584<object>()
		ldstr "G3_C1117::ClassMethod1584.MI.14416<System.Object>()"
		ldstr "class G3_C1117`1<class BaseClass0> on type class G3_C1117`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1117`1<class BaseClass0>
		callvirt instance string class G3_C1117`1<class BaseClass0>::ClassMethod1583<object>()
		ldstr "G3_C1117::ClassMethod1583.MI.14415<System.Object>()"
		ldstr "class G3_C1117`1<class BaseClass0> on type class G3_C1117`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1117`1<class BaseClass0>
		callvirt instance string class G3_C1117`1<class BaseClass0>::ClassMethod1582()
		ldstr "G2_C133::ClassMethod1582.6022()"
		ldstr "class G3_C1117`1<class BaseClass0> on type class G3_C1117`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1117`1<class BaseClass0>
		callvirt instance string class G3_C1117`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C133::Method6.6020<System.Object>()"
		ldstr "class G3_C1117`1<class BaseClass0> on type class G3_C1117`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1117`1<class BaseClass0>
		callvirt instance string class G3_C1117`1<class BaseClass0>::Method5()
		ldstr "G2_C133::Method5.6018()"
		ldstr "class G3_C1117`1<class BaseClass0> on type class G3_C1117`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1117`1<class BaseClass0>
		callvirt instance string class G3_C1117`1<class BaseClass0>::Method4()
		ldstr "G2_C133::Method4.6016()"
		ldstr "class G3_C1117`1<class BaseClass0> on type class G3_C1117`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1117`1<class BaseClass0>
		callvirt instance string class G3_C1117`1<class BaseClass0>::ClassMethod1343<object>()
		ldstr "G2_C133::ClassMethod1343.MI.6027<System.Object>()"
		ldstr "class G3_C1117`1<class BaseClass0> on type class G3_C1117`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1117`1<class BaseClass0>
		callvirt instance string class G3_C1117`1<class BaseClass0>::ClassMethod1342<object>()
		ldstr "G2_C133::ClassMethod1342.MI.6026<System.Object>()"
		ldstr "class G3_C1117`1<class BaseClass0> on type class G3_C1117`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1117`1<class BaseClass0>
		callvirt instance string class G3_C1117`1<class BaseClass0>::ClassMethod1341()
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G3_C1117`1<class BaseClass0> on type class G3_C1117`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1117`1<class BaseClass0>
		callvirt instance string class G3_C1117`1<class BaseClass0>::ClassMethod1340()
		ldstr "G2_C133::ClassMethod1340.MI.6025()"
		ldstr "class G3_C1117`1<class BaseClass0> on type class G3_C1117`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G3_C1117`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass0,class BaseClass1>::ClassMethod1343<object>()
		ldstr "G2_C133::ClassMethod1343.MI.6027<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G3_C1117`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass0,class BaseClass1>::ClassMethod1342<object>()
		ldstr "G2_C133::ClassMethod1342.MI.6026<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G3_C1117`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass0,class BaseClass1>::ClassMethod1341()
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G3_C1117`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass0,class BaseClass1>::ClassMethod1340()
		ldstr "G2_C133::ClassMethod1340.MI.6025()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G3_C1117`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass0,class BaseClass1>::Method6<object>()
		ldstr "G2_C133::Method6.6020<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G3_C1117`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass0,class BaseClass1>::Method5()
		ldstr "G1_C10::Method5.4847()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G3_C1117`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass0,class BaseClass1>::Method4()
		ldstr "G2_C133::Method4.6016()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G3_C1117`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C133::Method7.6015<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G3_C1117`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G3_C1117::Method7.MI.14410<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1117`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C133::Method4.MI.6017()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1117`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C133::Method5.MI.6019()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1117`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C133::Method6.MI.6021<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1117`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G3_C1117::Method7.MI.14410<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1117`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C133`1<class BaseClass0>
		callvirt instance string class G2_C133`1<class BaseClass0>::ClassMethod1584<object>()
		ldstr "G3_C1117::ClassMethod1584.MI.14416<System.Object>()"
		ldstr "class G2_C133`1<class BaseClass0> on type class G3_C1117`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C133`1<class BaseClass0>
		callvirt instance string class G2_C133`1<class BaseClass0>::ClassMethod1583<object>()
		ldstr "G3_C1117::ClassMethod1583.MI.14415<System.Object>()"
		ldstr "class G2_C133`1<class BaseClass0> on type class G3_C1117`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C133`1<class BaseClass0>
		callvirt instance string class G2_C133`1<class BaseClass0>::ClassMethod1582()
		ldstr "G2_C133::ClassMethod1582.6022()"
		ldstr "class G2_C133`1<class BaseClass0> on type class G3_C1117`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C133`1<class BaseClass0>
		callvirt instance string class G2_C133`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C133::Method6.6020<System.Object>()"
		ldstr "class G2_C133`1<class BaseClass0> on type class G3_C1117`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C133`1<class BaseClass0>
		callvirt instance string class G2_C133`1<class BaseClass0>::Method5()
		ldstr "G2_C133::Method5.6018()"
		ldstr "class G2_C133`1<class BaseClass0> on type class G3_C1117`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C133`1<class BaseClass0>
		callvirt instance string class G2_C133`1<class BaseClass0>::Method4()
		ldstr "G2_C133::Method4.6016()"
		ldstr "class G2_C133`1<class BaseClass0> on type class G3_C1117`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C133`1<class BaseClass0>
		callvirt instance string class G2_C133`1<class BaseClass0>::Method7<object>()
		ldstr "G2_C133::Method7.6015<System.Object>()"
		ldstr "class G2_C133`1<class BaseClass0> on type class G3_C1117`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C133`1<class BaseClass0>
		callvirt instance string class G2_C133`1<class BaseClass0>::ClassMethod1343<object>()
		ldstr "G2_C133::ClassMethod1343.MI.6027<System.Object>()"
		ldstr "class G2_C133`1<class BaseClass0> on type class G3_C1117`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C133`1<class BaseClass0>
		callvirt instance string class G2_C133`1<class BaseClass0>::ClassMethod1342<object>()
		ldstr "G2_C133::ClassMethod1342.MI.6026<System.Object>()"
		ldstr "class G2_C133`1<class BaseClass0> on type class G3_C1117`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C133`1<class BaseClass0>
		callvirt instance string class G2_C133`1<class BaseClass0>::ClassMethod1341()
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G2_C133`1<class BaseClass0> on type class G3_C1117`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C133`1<class BaseClass0>
		callvirt instance string class G2_C133`1<class BaseClass0>::ClassMethod1340()
		ldstr "G2_C133::ClassMethod1340.MI.6025()"
		ldstr "class G2_C133`1<class BaseClass0> on type class G3_C1117`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G3_C1117::Method7.MI.14410<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1117`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G3_C1117`1<class BaseClass1>
		callvirt instance string class G3_C1117`1<class BaseClass1>::ClassMethod3633<object>()
		ldstr "G3_C1117::ClassMethod3633.14414<System.Object>()"
		ldstr "class G3_C1117`1<class BaseClass1> on type class G3_C1117`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1117`1<class BaseClass1>
		callvirt instance string class G3_C1117`1<class BaseClass1>::ClassMethod3632<object>()
		ldstr "G3_C1117::ClassMethod3632.14413<System.Object>()"
		ldstr "class G3_C1117`1<class BaseClass1> on type class G3_C1117`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1117`1<class BaseClass1>
		callvirt instance string class G3_C1117`1<class BaseClass1>::ClassMethod3631()
		ldstr "G3_C1117::ClassMethod3631.14412()"
		ldstr "class G3_C1117`1<class BaseClass1> on type class G3_C1117`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1117`1<class BaseClass1>
		callvirt instance string class G3_C1117`1<class BaseClass1>::ClassMethod3630()
		ldstr "G3_C1117::ClassMethod3630.14411()"
		ldstr "class G3_C1117`1<class BaseClass1> on type class G3_C1117`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1117`1<class BaseClass1>
		callvirt instance string class G3_C1117`1<class BaseClass1>::Method7<object>()
		ldstr "G3_C1117::Method7.14409<System.Object>()"
		ldstr "class G3_C1117`1<class BaseClass1> on type class G3_C1117`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1117`1<class BaseClass1>
		callvirt instance string class G3_C1117`1<class BaseClass1>::ClassMethod1584<object>()
		ldstr "G3_C1117::ClassMethod1584.MI.14416<System.Object>()"
		ldstr "class G3_C1117`1<class BaseClass1> on type class G3_C1117`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1117`1<class BaseClass1>
		callvirt instance string class G3_C1117`1<class BaseClass1>::ClassMethod1583<object>()
		ldstr "G3_C1117::ClassMethod1583.MI.14415<System.Object>()"
		ldstr "class G3_C1117`1<class BaseClass1> on type class G3_C1117`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1117`1<class BaseClass1>
		callvirt instance string class G3_C1117`1<class BaseClass1>::ClassMethod1582()
		ldstr "G2_C133::ClassMethod1582.6022()"
		ldstr "class G3_C1117`1<class BaseClass1> on type class G3_C1117`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1117`1<class BaseClass1>
		callvirt instance string class G3_C1117`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C133::Method6.6020<System.Object>()"
		ldstr "class G3_C1117`1<class BaseClass1> on type class G3_C1117`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1117`1<class BaseClass1>
		callvirt instance string class G3_C1117`1<class BaseClass1>::Method5()
		ldstr "G2_C133::Method5.6018()"
		ldstr "class G3_C1117`1<class BaseClass1> on type class G3_C1117`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1117`1<class BaseClass1>
		callvirt instance string class G3_C1117`1<class BaseClass1>::Method4()
		ldstr "G2_C133::Method4.6016()"
		ldstr "class G3_C1117`1<class BaseClass1> on type class G3_C1117`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1117`1<class BaseClass1>
		callvirt instance string class G3_C1117`1<class BaseClass1>::ClassMethod1343<object>()
		ldstr "G2_C133::ClassMethod1343.MI.6027<System.Object>()"
		ldstr "class G3_C1117`1<class BaseClass1> on type class G3_C1117`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1117`1<class BaseClass1>
		callvirt instance string class G3_C1117`1<class BaseClass1>::ClassMethod1342<object>()
		ldstr "G2_C133::ClassMethod1342.MI.6026<System.Object>()"
		ldstr "class G3_C1117`1<class BaseClass1> on type class G3_C1117`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1117`1<class BaseClass1>
		callvirt instance string class G3_C1117`1<class BaseClass1>::ClassMethod1341()
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G3_C1117`1<class BaseClass1> on type class G3_C1117`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1117`1<class BaseClass1>
		callvirt instance string class G3_C1117`1<class BaseClass1>::ClassMethod1340()
		ldstr "G2_C133::ClassMethod1340.MI.6025()"
		ldstr "class G3_C1117`1<class BaseClass1> on type class G3_C1117`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G3_C1117::Method7.MI.14410<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G3_C1117`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C133`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass0,class BaseClass1>::ClassMethod1343<object>()
		ldstr "G2_C133::ClassMethod1343.MI.6027<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G2_C133`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass0,class BaseClass1>::ClassMethod1342<object>()
		ldstr "G2_C133::ClassMethod1342.MI.6026<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G2_C133`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass0,class BaseClass1>::ClassMethod1341()
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G2_C133`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass0,class BaseClass1>::ClassMethod1340()
		ldstr "G2_C133::ClassMethod1340.MI.6025()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G2_C133`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass0,class BaseClass1>::Method6<object>()
		ldstr "G2_C133::Method6.6020<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G2_C133`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass0,class BaseClass1>::Method5()
		ldstr "G1_C10::Method5.4847()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G2_C133`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass0,class BaseClass1>::Method4()
		ldstr "G2_C133::Method4.6016()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G2_C133`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C133::Method7.6015<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G2_C133`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C133::Method7.6015<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C133`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C133::Method4.MI.6017()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C133`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C133::Method5.MI.6019()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C133`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C133::Method6.MI.6021<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C133`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C133::Method7.6015<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C133`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C133`1<class BaseClass0>
		callvirt instance string class G2_C133`1<class BaseClass0>::ClassMethod1584<object>()
		ldstr "G2_C133::ClassMethod1584.6024<System.Object>()"
		ldstr "class G2_C133`1<class BaseClass0> on type class G2_C133`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C133`1<class BaseClass0>
		callvirt instance string class G2_C133`1<class BaseClass0>::ClassMethod1583<object>()
		ldstr "G2_C133::ClassMethod1583.6023<System.Object>()"
		ldstr "class G2_C133`1<class BaseClass0> on type class G2_C133`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C133`1<class BaseClass0>
		callvirt instance string class G2_C133`1<class BaseClass0>::ClassMethod1582()
		ldstr "G2_C133::ClassMethod1582.6022()"
		ldstr "class G2_C133`1<class BaseClass0> on type class G2_C133`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C133`1<class BaseClass0>
		callvirt instance string class G2_C133`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C133::Method6.6020<System.Object>()"
		ldstr "class G2_C133`1<class BaseClass0> on type class G2_C133`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C133`1<class BaseClass0>
		callvirt instance string class G2_C133`1<class BaseClass0>::Method5()
		ldstr "G2_C133::Method5.6018()"
		ldstr "class G2_C133`1<class BaseClass0> on type class G2_C133`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C133`1<class BaseClass0>
		callvirt instance string class G2_C133`1<class BaseClass0>::Method4()
		ldstr "G2_C133::Method4.6016()"
		ldstr "class G2_C133`1<class BaseClass0> on type class G2_C133`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C133`1<class BaseClass0>
		callvirt instance string class G2_C133`1<class BaseClass0>::Method7<object>()
		ldstr "G2_C133::Method7.6015<System.Object>()"
		ldstr "class G2_C133`1<class BaseClass0> on type class G2_C133`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C133`1<class BaseClass0>
		callvirt instance string class G2_C133`1<class BaseClass0>::ClassMethod1343<object>()
		ldstr "G2_C133::ClassMethod1343.MI.6027<System.Object>()"
		ldstr "class G2_C133`1<class BaseClass0> on type class G2_C133`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C133`1<class BaseClass0>
		callvirt instance string class G2_C133`1<class BaseClass0>::ClassMethod1342<object>()
		ldstr "G2_C133::ClassMethod1342.MI.6026<System.Object>()"
		ldstr "class G2_C133`1<class BaseClass0> on type class G2_C133`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C133`1<class BaseClass0>
		callvirt instance string class G2_C133`1<class BaseClass0>::ClassMethod1341()
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G2_C133`1<class BaseClass0> on type class G2_C133`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C133`1<class BaseClass0>
		callvirt instance string class G2_C133`1<class BaseClass0>::ClassMethod1340()
		ldstr "G2_C133::ClassMethod1340.MI.6025()"
		ldstr "class G2_C133`1<class BaseClass0> on type class G2_C133`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C133::Method7.6015<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C133`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C133`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass0,class BaseClass1>::ClassMethod1343<object>()
		ldstr "G2_C133::ClassMethod1343.MI.6027<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G2_C133`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass0,class BaseClass1>::ClassMethod1342<object>()
		ldstr "G2_C133::ClassMethod1342.MI.6026<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G2_C133`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass0,class BaseClass1>::ClassMethod1341()
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G2_C133`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass0,class BaseClass1>::ClassMethod1340()
		ldstr "G2_C133::ClassMethod1340.MI.6025()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G2_C133`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass0,class BaseClass1>::Method6<object>()
		ldstr "G2_C133::Method6.6020<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G2_C133`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass0,class BaseClass1>::Method5()
		ldstr "G1_C10::Method5.4847()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G2_C133`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass0,class BaseClass1>::Method4()
		ldstr "G2_C133::Method4.6016()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G2_C133`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C10`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C133::Method7.6015<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G2_C133`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C133::Method7.6015<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C133`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C133::Method4.MI.6017()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C133`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C133::Method5.MI.6019()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C133`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C133::Method6.MI.6021<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C133`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C133::Method7.6015<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C133`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C133`1<class BaseClass1>
		callvirt instance string class G2_C133`1<class BaseClass1>::ClassMethod1584<object>()
		ldstr "G2_C133::ClassMethod1584.6024<System.Object>()"
		ldstr "class G2_C133`1<class BaseClass1> on type class G2_C133`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C133`1<class BaseClass1>
		callvirt instance string class G2_C133`1<class BaseClass1>::ClassMethod1583<object>()
		ldstr "G2_C133::ClassMethod1583.6023<System.Object>()"
		ldstr "class G2_C133`1<class BaseClass1> on type class G2_C133`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C133`1<class BaseClass1>
		callvirt instance string class G2_C133`1<class BaseClass1>::ClassMethod1582()
		ldstr "G2_C133::ClassMethod1582.6022()"
		ldstr "class G2_C133`1<class BaseClass1> on type class G2_C133`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C133`1<class BaseClass1>
		callvirt instance string class G2_C133`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C133::Method6.6020<System.Object>()"
		ldstr "class G2_C133`1<class BaseClass1> on type class G2_C133`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C133`1<class BaseClass1>
		callvirt instance string class G2_C133`1<class BaseClass1>::Method5()
		ldstr "G2_C133::Method5.6018()"
		ldstr "class G2_C133`1<class BaseClass1> on type class G2_C133`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C133`1<class BaseClass1>
		callvirt instance string class G2_C133`1<class BaseClass1>::Method4()
		ldstr "G2_C133::Method4.6016()"
		ldstr "class G2_C133`1<class BaseClass1> on type class G2_C133`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C133`1<class BaseClass1>
		callvirt instance string class G2_C133`1<class BaseClass1>::Method7<object>()
		ldstr "G2_C133::Method7.6015<System.Object>()"
		ldstr "class G2_C133`1<class BaseClass1> on type class G2_C133`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C133`1<class BaseClass1>
		callvirt instance string class G2_C133`1<class BaseClass1>::ClassMethod1343<object>()
		ldstr "G2_C133::ClassMethod1343.MI.6027<System.Object>()"
		ldstr "class G2_C133`1<class BaseClass1> on type class G2_C133`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C133`1<class BaseClass1>
		callvirt instance string class G2_C133`1<class BaseClass1>::ClassMethod1342<object>()
		ldstr "G2_C133::ClassMethod1342.MI.6026<System.Object>()"
		ldstr "class G2_C133`1<class BaseClass1> on type class G2_C133`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C133`1<class BaseClass1>
		callvirt instance string class G2_C133`1<class BaseClass1>::ClassMethod1341()
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G2_C133`1<class BaseClass1> on type class G2_C133`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C133`1<class BaseClass1>
		callvirt instance string class G2_C133`1<class BaseClass1>::ClassMethod1340()
		ldstr "G2_C133::ClassMethod1340.MI.6025()"
		ldstr "class G2_C133`1<class BaseClass1> on type class G2_C133`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G2_C133::Method7.6015<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G2_C133`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C133::Method7.6015<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C133`1<class BaseClass1>"
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
		newobj instance void class G3_C1117`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C133::ClassMethod1340.MI.6025()#G1_C10::ClassMethod1341.4851()#G2_C133::ClassMethod1342.MI.6026<System.Object>()#G2_C133::ClassMethod1343.MI.6027<System.Object>()#G2_C133::Method4.6016()#G1_C10::Method5.4847()#G2_C133::Method6.6020<System.Object>()#G2_C133::Method7.6015<System.Object>()#"
		call void Framework::M.G1_C10.T.T<class BaseClass0,class BaseClass1,class G3_C1117`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C133::ClassMethod1340.MI.6025()#G1_C10::ClassMethod1341.4851()#G2_C133::ClassMethod1342.MI.6026<System.Object>()#G2_C133::ClassMethod1343.MI.6027<System.Object>()#G2_C133::Method4.6016()#G1_C10::Method5.4847()#G2_C133::Method6.6020<System.Object>()#G2_C133::Method7.6015<System.Object>()#"
		call void Framework::M.G1_C10.A.T<class BaseClass1,class G3_C1117`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C133::ClassMethod1340.MI.6025()#G1_C10::ClassMethod1341.4851()#G2_C133::ClassMethod1342.MI.6026<System.Object>()#G2_C133::ClassMethod1343.MI.6027<System.Object>()#G2_C133::Method4.6016()#G1_C10::Method5.4847()#G2_C133::Method6.6020<System.Object>()#G2_C133::Method7.6015<System.Object>()#"
		call void Framework::M.G1_C10.A.B<class G3_C1117`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C133::Method7.6015<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G3_C1117`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C133::Method7.6015<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G3_C1117`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C133::Method7.6015<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G3_C1117`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C133::Method4.MI.6017()#G2_C133::Method5.MI.6019()#G2_C133::Method6.MI.6021<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G3_C1117`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C133::Method4.MI.6017()#G2_C133::Method5.MI.6019()#G2_C133::Method6.MI.6021<System.Object>()#"
		call void Framework::M.IBase1.A<class G3_C1117`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1117::Method7.MI.14410<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G3_C1117`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1117::Method7.MI.14410<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G3_C1117`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1117::Method7.MI.14410<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G3_C1117`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C133::ClassMethod1340.MI.6025()#G1_C10::ClassMethod1341.4851()#G2_C133::ClassMethod1342.MI.6026<System.Object>()#G2_C133::ClassMethod1343.MI.6027<System.Object>()#G2_C133::ClassMethod1582.6022()#G3_C1117::ClassMethod1583.MI.14415<System.Object>()#G3_C1117::ClassMethod1584.MI.14416<System.Object>()#G2_C133::Method4.6016()#G2_C133::Method5.6018()#G2_C133::Method6.6020<System.Object>()#G2_C133::Method7.6015<System.Object>()#"
		call void Framework::M.G2_C133.T<class BaseClass0,class G3_C1117`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C133::ClassMethod1340.MI.6025()#G1_C10::ClassMethod1341.4851()#G2_C133::ClassMethod1342.MI.6026<System.Object>()#G2_C133::ClassMethod1343.MI.6027<System.Object>()#G2_C133::ClassMethod1582.6022()#G3_C1117::ClassMethod1583.MI.14415<System.Object>()#G3_C1117::ClassMethod1584.MI.14416<System.Object>()#G2_C133::Method4.6016()#G2_C133::Method5.6018()#G2_C133::Method6.6020<System.Object>()#G2_C133::Method7.6015<System.Object>()#"
		call void Framework::M.G2_C133.A<class G3_C1117`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1117::Method7.MI.14410<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G3_C1117`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1117::Method7.MI.14410<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass0,class G3_C1117`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1117::Method7.MI.14410<System.Object>()#"
		call void Framework::M.IBase2.A.A<class G3_C1117`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C133::ClassMethod1340.MI.6025()#G1_C10::ClassMethod1341.4851()#G2_C133::ClassMethod1342.MI.6026<System.Object>()#G2_C133::ClassMethod1343.MI.6027<System.Object>()#G2_C133::ClassMethod1582.6022()#G3_C1117::ClassMethod1583.MI.14415<System.Object>()#G3_C1117::ClassMethod1584.MI.14416<System.Object>()#G3_C1117::ClassMethod3630.14411()#G3_C1117::ClassMethod3631.14412()#G3_C1117::ClassMethod3632.14413<System.Object>()#G3_C1117::ClassMethod3633.14414<System.Object>()#G2_C133::Method4.6016()#G2_C133::Method5.6018()#G2_C133::Method6.6020<System.Object>()#G3_C1117::Method7.14409<System.Object>()#"
		call void Framework::M.G3_C1117.T<class BaseClass0,class G3_C1117`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C133::ClassMethod1340.MI.6025()#G1_C10::ClassMethod1341.4851()#G2_C133::ClassMethod1342.MI.6026<System.Object>()#G2_C133::ClassMethod1343.MI.6027<System.Object>()#G2_C133::ClassMethod1582.6022()#G3_C1117::ClassMethod1583.MI.14415<System.Object>()#G3_C1117::ClassMethod1584.MI.14416<System.Object>()#G3_C1117::ClassMethod3630.14411()#G3_C1117::ClassMethod3631.14412()#G3_C1117::ClassMethod3632.14413<System.Object>()#G3_C1117::ClassMethod3633.14414<System.Object>()#G2_C133::Method4.6016()#G2_C133::Method5.6018()#G2_C133::Method6.6020<System.Object>()#G3_C1117::Method7.14409<System.Object>()#"
		call void Framework::M.G3_C1117.A<class G3_C1117`1<class BaseClass0>>(!!0,string)
		newobj instance void class G3_C1117`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C133::ClassMethod1340.MI.6025()#G1_C10::ClassMethod1341.4851()#G2_C133::ClassMethod1342.MI.6026<System.Object>()#G2_C133::ClassMethod1343.MI.6027<System.Object>()#G2_C133::Method4.6016()#G1_C10::Method5.4847()#G2_C133::Method6.6020<System.Object>()#G2_C133::Method7.6015<System.Object>()#"
		call void Framework::M.G1_C10.T.T<class BaseClass0,class BaseClass1,class G3_C1117`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C133::ClassMethod1340.MI.6025()#G1_C10::ClassMethod1341.4851()#G2_C133::ClassMethod1342.MI.6026<System.Object>()#G2_C133::ClassMethod1343.MI.6027<System.Object>()#G2_C133::Method4.6016()#G1_C10::Method5.4847()#G2_C133::Method6.6020<System.Object>()#G2_C133::Method7.6015<System.Object>()#"
		call void Framework::M.G1_C10.A.T<class BaseClass1,class G3_C1117`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C133::ClassMethod1340.MI.6025()#G1_C10::ClassMethod1341.4851()#G2_C133::ClassMethod1342.MI.6026<System.Object>()#G2_C133::ClassMethod1343.MI.6027<System.Object>()#G2_C133::Method4.6016()#G1_C10::Method5.4847()#G2_C133::Method6.6020<System.Object>()#G2_C133::Method7.6015<System.Object>()#"
		call void Framework::M.G1_C10.A.B<class G3_C1117`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1117::Method7.MI.14410<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G3_C1117`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1117::Method7.MI.14410<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G3_C1117`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1117::Method7.MI.14410<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G3_C1117`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C133::Method4.MI.6017()#G2_C133::Method5.MI.6019()#G2_C133::Method6.MI.6021<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G3_C1117`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C133::Method4.MI.6017()#G2_C133::Method5.MI.6019()#G2_C133::Method6.MI.6021<System.Object>()#"
		call void Framework::M.IBase1.A<class G3_C1117`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1117::Method7.MI.14410<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G3_C1117`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1117::Method7.MI.14410<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G3_C1117`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1117::Method7.MI.14410<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G3_C1117`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C133::ClassMethod1340.MI.6025()#G1_C10::ClassMethod1341.4851()#G2_C133::ClassMethod1342.MI.6026<System.Object>()#G2_C133::ClassMethod1343.MI.6027<System.Object>()#G2_C133::ClassMethod1582.6022()#G3_C1117::ClassMethod1583.MI.14415<System.Object>()#G3_C1117::ClassMethod1584.MI.14416<System.Object>()#G2_C133::Method4.6016()#G2_C133::Method5.6018()#G2_C133::Method6.6020<System.Object>()#G2_C133::Method7.6015<System.Object>()#"
		call void Framework::M.G2_C133.T<class BaseClass0,class G3_C1117`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C133::ClassMethod1340.MI.6025()#G1_C10::ClassMethod1341.4851()#G2_C133::ClassMethod1342.MI.6026<System.Object>()#G2_C133::ClassMethod1343.MI.6027<System.Object>()#G2_C133::ClassMethod1582.6022()#G3_C1117::ClassMethod1583.MI.14415<System.Object>()#G3_C1117::ClassMethod1584.MI.14416<System.Object>()#G2_C133::Method4.6016()#G2_C133::Method5.6018()#G2_C133::Method6.6020<System.Object>()#G2_C133::Method7.6015<System.Object>()#"
		call void Framework::M.G2_C133.A<class G3_C1117`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1117::Method7.MI.14410<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G3_C1117`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1117::Method7.MI.14410<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass0,class G3_C1117`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1117::Method7.MI.14410<System.Object>()#"
		call void Framework::M.IBase2.A.A<class G3_C1117`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C133::ClassMethod1340.MI.6025()#G1_C10::ClassMethod1341.4851()#G2_C133::ClassMethod1342.MI.6026<System.Object>()#G2_C133::ClassMethod1343.MI.6027<System.Object>()#G2_C133::ClassMethod1582.6022()#G3_C1117::ClassMethod1583.MI.14415<System.Object>()#G3_C1117::ClassMethod1584.MI.14416<System.Object>()#G3_C1117::ClassMethod3630.14411()#G3_C1117::ClassMethod3631.14412()#G3_C1117::ClassMethod3632.14413<System.Object>()#G3_C1117::ClassMethod3633.14414<System.Object>()#G2_C133::Method4.6016()#G2_C133::Method5.6018()#G2_C133::Method6.6020<System.Object>()#G3_C1117::Method7.14409<System.Object>()#"
		call void Framework::M.G3_C1117.T<class BaseClass1,class G3_C1117`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C133::ClassMethod1340.MI.6025()#G1_C10::ClassMethod1341.4851()#G2_C133::ClassMethod1342.MI.6026<System.Object>()#G2_C133::ClassMethod1343.MI.6027<System.Object>()#G2_C133::ClassMethod1582.6022()#G3_C1117::ClassMethod1583.MI.14415<System.Object>()#G3_C1117::ClassMethod1584.MI.14416<System.Object>()#G3_C1117::ClassMethod3630.14411()#G3_C1117::ClassMethod3631.14412()#G3_C1117::ClassMethod3632.14413<System.Object>()#G3_C1117::ClassMethod3633.14414<System.Object>()#G2_C133::Method4.6016()#G2_C133::Method5.6018()#G2_C133::Method6.6020<System.Object>()#G3_C1117::Method7.14409<System.Object>()#"
		call void Framework::M.G3_C1117.B<class G3_C1117`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1117::Method7.MI.14410<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass0,class G3_C1117`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1117::Method7.MI.14410<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass0,class G3_C1117`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1117::Method7.MI.14410<System.Object>()#"
		call void Framework::M.IBase2.B.A<class G3_C1117`1<class BaseClass1>>(!!0,string)
		newobj instance void class G2_C133`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C133::ClassMethod1340.MI.6025()#G1_C10::ClassMethod1341.4851()#G2_C133::ClassMethod1342.MI.6026<System.Object>()#G2_C133::ClassMethod1343.MI.6027<System.Object>()#G2_C133::Method4.6016()#G1_C10::Method5.4847()#G2_C133::Method6.6020<System.Object>()#G2_C133::Method7.6015<System.Object>()#"
		call void Framework::M.G1_C10.T.T<class BaseClass0,class BaseClass1,class G2_C133`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C133::ClassMethod1340.MI.6025()#G1_C10::ClassMethod1341.4851()#G2_C133::ClassMethod1342.MI.6026<System.Object>()#G2_C133::ClassMethod1343.MI.6027<System.Object>()#G2_C133::Method4.6016()#G1_C10::Method5.4847()#G2_C133::Method6.6020<System.Object>()#G2_C133::Method7.6015<System.Object>()#"
		call void Framework::M.G1_C10.A.T<class BaseClass1,class G2_C133`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C133::ClassMethod1340.MI.6025()#G1_C10::ClassMethod1341.4851()#G2_C133::ClassMethod1342.MI.6026<System.Object>()#G2_C133::ClassMethod1343.MI.6027<System.Object>()#G2_C133::Method4.6016()#G1_C10::Method5.4847()#G2_C133::Method6.6020<System.Object>()#G2_C133::Method7.6015<System.Object>()#"
		call void Framework::M.G1_C10.A.B<class G2_C133`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C133::Method7.6015<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G2_C133`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C133::Method7.6015<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G2_C133`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C133::Method7.6015<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G2_C133`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C133::Method4.MI.6017()#G2_C133::Method5.MI.6019()#G2_C133::Method6.MI.6021<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G2_C133`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C133::Method4.MI.6017()#G2_C133::Method5.MI.6019()#G2_C133::Method6.MI.6021<System.Object>()#"
		call void Framework::M.IBase1.A<class G2_C133`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C133::Method7.6015<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C133`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C133::Method7.6015<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G2_C133`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C133::Method7.6015<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G2_C133`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C133::ClassMethod1340.MI.6025()#G1_C10::ClassMethod1341.4851()#G2_C133::ClassMethod1342.MI.6026<System.Object>()#G2_C133::ClassMethod1343.MI.6027<System.Object>()#G2_C133::ClassMethod1582.6022()#G2_C133::ClassMethod1583.6023<System.Object>()#G2_C133::ClassMethod1584.6024<System.Object>()#G2_C133::Method4.6016()#G2_C133::Method5.6018()#G2_C133::Method6.6020<System.Object>()#G2_C133::Method7.6015<System.Object>()#"
		call void Framework::M.G2_C133.T<class BaseClass0,class G2_C133`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C133::ClassMethod1340.MI.6025()#G1_C10::ClassMethod1341.4851()#G2_C133::ClassMethod1342.MI.6026<System.Object>()#G2_C133::ClassMethod1343.MI.6027<System.Object>()#G2_C133::ClassMethod1582.6022()#G2_C133::ClassMethod1583.6023<System.Object>()#G2_C133::ClassMethod1584.6024<System.Object>()#G2_C133::Method4.6016()#G2_C133::Method5.6018()#G2_C133::Method6.6020<System.Object>()#G2_C133::Method7.6015<System.Object>()#"
		call void Framework::M.G2_C133.A<class G2_C133`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C133::Method7.6015<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G2_C133`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C133::Method7.6015<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass0,class G2_C133`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C133::Method7.6015<System.Object>()#"
		call void Framework::M.IBase2.A.A<class G2_C133`1<class BaseClass0>>(!!0,string)
		newobj instance void class G2_C133`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C133::ClassMethod1340.MI.6025()#G1_C10::ClassMethod1341.4851()#G2_C133::ClassMethod1342.MI.6026<System.Object>()#G2_C133::ClassMethod1343.MI.6027<System.Object>()#G2_C133::Method4.6016()#G1_C10::Method5.4847()#G2_C133::Method6.6020<System.Object>()#G2_C133::Method7.6015<System.Object>()#"
		call void Framework::M.G1_C10.T.T<class BaseClass0,class BaseClass1,class G2_C133`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C133::ClassMethod1340.MI.6025()#G1_C10::ClassMethod1341.4851()#G2_C133::ClassMethod1342.MI.6026<System.Object>()#G2_C133::ClassMethod1343.MI.6027<System.Object>()#G2_C133::Method4.6016()#G1_C10::Method5.4847()#G2_C133::Method6.6020<System.Object>()#G2_C133::Method7.6015<System.Object>()#"
		call void Framework::M.G1_C10.A.T<class BaseClass1,class G2_C133`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C133::ClassMethod1340.MI.6025()#G1_C10::ClassMethod1341.4851()#G2_C133::ClassMethod1342.MI.6026<System.Object>()#G2_C133::ClassMethod1343.MI.6027<System.Object>()#G2_C133::Method4.6016()#G1_C10::Method5.4847()#G2_C133::Method6.6020<System.Object>()#G2_C133::Method7.6015<System.Object>()#"
		call void Framework::M.G1_C10.A.B<class G2_C133`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C133::Method7.6015<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G2_C133`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C133::Method7.6015<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G2_C133`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C133::Method7.6015<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G2_C133`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C133::Method4.MI.6017()#G2_C133::Method5.MI.6019()#G2_C133::Method6.MI.6021<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G2_C133`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C133::Method4.MI.6017()#G2_C133::Method5.MI.6019()#G2_C133::Method6.MI.6021<System.Object>()#"
		call void Framework::M.IBase1.A<class G2_C133`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C133::Method7.6015<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C133`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C133::Method7.6015<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G2_C133`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C133::Method7.6015<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G2_C133`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C133::ClassMethod1340.MI.6025()#G1_C10::ClassMethod1341.4851()#G2_C133::ClassMethod1342.MI.6026<System.Object>()#G2_C133::ClassMethod1343.MI.6027<System.Object>()#G2_C133::ClassMethod1582.6022()#G2_C133::ClassMethod1583.6023<System.Object>()#G2_C133::ClassMethod1584.6024<System.Object>()#G2_C133::Method4.6016()#G2_C133::Method5.6018()#G2_C133::Method6.6020<System.Object>()#G2_C133::Method7.6015<System.Object>()#"
		call void Framework::M.G2_C133.T<class BaseClass1,class G2_C133`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C133::ClassMethod1340.MI.6025()#G1_C10::ClassMethod1341.4851()#G2_C133::ClassMethod1342.MI.6026<System.Object>()#G2_C133::ClassMethod1343.MI.6027<System.Object>()#G2_C133::ClassMethod1582.6022()#G2_C133::ClassMethod1583.6023<System.Object>()#G2_C133::ClassMethod1584.6024<System.Object>()#G2_C133::Method4.6016()#G2_C133::Method5.6018()#G2_C133::Method6.6020<System.Object>()#G2_C133::Method7.6015<System.Object>()#"
		call void Framework::M.G2_C133.B<class G2_C133`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C133::Method7.6015<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass0,class G2_C133`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C133::Method7.6015<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass0,class G2_C133`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C133::Method7.6015<System.Object>()#"
		call void Framework::M.IBase2.B.A<class G2_C133`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C133::Method7.6015<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G2_C133`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C133::Method7.6015<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass0,class G2_C133`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C133::Method7.6015<System.Object>()#"
		call void Framework::M.IBase2.A.A<class G2_C133`1<class BaseClass1>>(!!0,string)
		newobj instance void class G1_C10`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G1_C10::ClassMethod1342.4852<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G1_C10::Method4.4845()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Framework::M.G1_C10.T.T<class BaseClass0,class BaseClass0,class G1_C10`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G1_C10::ClassMethod1342.4852<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G1_C10::Method4.4845()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Framework::M.G1_C10.A.T<class BaseClass0,class G1_C10`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G1_C10::ClassMethod1342.4852<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G1_C10::Method4.4845()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Framework::M.G1_C10.A.A<class G1_C10`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G1_C10`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G1_C10`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G1_C10`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method4.MI.4846()#G1_C10::Method5.4847()#G1_C10::Method6.MI.4849<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G1_C10`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method4.MI.4846()#G1_C10::Method5.4847()#G1_C10::Method6.MI.4849<System.Object>()#"
		call void Framework::M.IBase1.A<class G1_C10`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G1_C10`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G1_C10`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G1_C10`2<class BaseClass0,class BaseClass0>>(!!0,string)
		newobj instance void class G1_C10`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G1_C10::ClassMethod1342.4852<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G1_C10::Method4.4845()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Framework::M.G1_C10.T.T<class BaseClass0,class BaseClass1,class G1_C10`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G1_C10::ClassMethod1342.4852<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G1_C10::Method4.4845()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Framework::M.G1_C10.A.T<class BaseClass1,class G1_C10`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G1_C10::ClassMethod1342.4852<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G1_C10::Method4.4845()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Framework::M.G1_C10.A.B<class G1_C10`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G1_C10`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G1_C10`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G1_C10`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method4.MI.4846()#G1_C10::Method5.4847()#G1_C10::Method6.MI.4849<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G1_C10`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method4.MI.4846()#G1_C10::Method5.4847()#G1_C10::Method6.MI.4849<System.Object>()#"
		call void Framework::M.IBase1.A<class G1_C10`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G1_C10`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G1_C10`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G1_C10`2<class BaseClass0,class BaseClass1>>(!!0,string)
		newobj instance void class G1_C10`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G1_C10::ClassMethod1342.4852<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G1_C10::Method4.4845()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Framework::M.G1_C10.T.T<class BaseClass1,class BaseClass0,class G1_C10`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G1_C10::ClassMethod1342.4852<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G1_C10::Method4.4845()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Framework::M.G1_C10.B.T<class BaseClass0,class G1_C10`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G1_C10::ClassMethod1342.4852<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G1_C10::Method4.4845()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Framework::M.G1_C10.B.A<class G1_C10`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G1_C10`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G1_C10`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G1_C10`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method4.MI.4846()#G1_C10::Method5.4847()#G1_C10::Method6.MI.4849<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass1,class G1_C10`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method4.MI.4846()#G1_C10::Method5.4847()#G1_C10::Method6.MI.4849<System.Object>()#"
		call void Framework::M.IBase1.B<class G1_C10`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G1_C10`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G1_C10`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G1_C10`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method4.MI.4846()#G1_C10::Method5.4847()#G1_C10::Method6.MI.4849<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G1_C10`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method4.MI.4846()#G1_C10::Method5.4847()#G1_C10::Method6.MI.4849<System.Object>()#"
		call void Framework::M.IBase1.A<class G1_C10`2<class BaseClass1,class BaseClass0>>(!!0,string)
		newobj instance void class G1_C10`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G1_C10::ClassMethod1342.4852<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G1_C10::Method4.4845()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Framework::M.G1_C10.T.T<class BaseClass1,class BaseClass1,class G1_C10`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G1_C10::ClassMethod1342.4852<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G1_C10::Method4.4845()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Framework::M.G1_C10.B.T<class BaseClass1,class G1_C10`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G1_C10::ClassMethod1342.4852<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G1_C10::Method4.4845()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Framework::M.G1_C10.B.B<class G1_C10`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G1_C10`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G1_C10`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G1_C10`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method4.MI.4846()#G1_C10::Method5.4847()#G1_C10::Method6.MI.4849<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass1,class G1_C10`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method4.MI.4846()#G1_C10::Method5.4847()#G1_C10::Method6.MI.4849<System.Object>()#"
		call void Framework::M.IBase1.B<class G1_C10`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G1_C10`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G1_C10`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G1_C10`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method4.MI.4846()#G1_C10::Method5.4847()#G1_C10::Method6.MI.4849<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G1_C10`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method4.MI.4846()#G1_C10::Method5.4847()#G1_C10::Method6.MI.4849<System.Object>()#"
		call void Framework::M.IBase1.A<class G1_C10`2<class BaseClass1,class BaseClass1>>(!!0,string)
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
		newobj instance void class G3_C1117`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass0,class BaseClass1>::ClassMethod1343<object>()
		calli default string(class G3_C1117`1<class BaseClass0>)
		ldstr "G2_C133::ClassMethod1343.MI.6027<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G3_C1117`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass0,class BaseClass1>::ClassMethod1342<object>()
		calli default string(class G3_C1117`1<class BaseClass0>)
		ldstr "G2_C133::ClassMethod1342.MI.6026<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G3_C1117`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass0,class BaseClass1>::ClassMethod1341()
		calli default string(class G3_C1117`1<class BaseClass0>)
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G3_C1117`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass0,class BaseClass1>::ClassMethod1340()
		calli default string(class G3_C1117`1<class BaseClass0>)
		ldstr "G2_C133::ClassMethod1340.MI.6025()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G3_C1117`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass0,class BaseClass1>::Method6<object>()
		calli default string(class G3_C1117`1<class BaseClass0>)
		ldstr "G2_C133::Method6.6020<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G3_C1117`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass0,class BaseClass1>::Method5()
		calli default string(class G3_C1117`1<class BaseClass0>)
		ldstr "G1_C10::Method5.4847()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G3_C1117`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass0,class BaseClass1>::Method4()
		calli default string(class G3_C1117`1<class BaseClass0>)
		ldstr "G2_C133::Method4.6016()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G3_C1117`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1117`1<class BaseClass0>)
		ldstr "G2_C133::Method7.6015<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G3_C1117`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1117`1<class BaseClass0>)
		ldstr "G2_C133::Method7.6015<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1117`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G3_C1117`1<class BaseClass0>)
		ldstr "G2_C133::Method4.MI.6017()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1117`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G3_C1117`1<class BaseClass0>)
		ldstr "G2_C133::Method5.MI.6019()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1117`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1117`1<class BaseClass0>)
		ldstr "G2_C133::Method6.MI.6021<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1117`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1117`1<class BaseClass0>)
		ldstr "G3_C1117::Method7.MI.14410<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1117`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C133`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C133`1<class BaseClass0>::ClassMethod1584<object>()
		calli default string(class G3_C1117`1<class BaseClass0>)
		ldstr "G3_C1117::ClassMethod1584.MI.14416<System.Object>()"
		ldstr "class G2_C133`1<class BaseClass0> on type class G3_C1117`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C133`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C133`1<class BaseClass0>::ClassMethod1583<object>()
		calli default string(class G3_C1117`1<class BaseClass0>)
		ldstr "G3_C1117::ClassMethod1583.MI.14415<System.Object>()"
		ldstr "class G2_C133`1<class BaseClass0> on type class G3_C1117`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C133`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C133`1<class BaseClass0>::ClassMethod1582()
		calli default string(class G3_C1117`1<class BaseClass0>)
		ldstr "G2_C133::ClassMethod1582.6022()"
		ldstr "class G2_C133`1<class BaseClass0> on type class G3_C1117`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C133`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C133`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1117`1<class BaseClass0>)
		ldstr "G2_C133::Method6.6020<System.Object>()"
		ldstr "class G2_C133`1<class BaseClass0> on type class G3_C1117`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C133`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C133`1<class BaseClass0>::Method5()
		calli default string(class G3_C1117`1<class BaseClass0>)
		ldstr "G2_C133::Method5.6018()"
		ldstr "class G2_C133`1<class BaseClass0> on type class G3_C1117`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C133`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C133`1<class BaseClass0>::Method4()
		calli default string(class G3_C1117`1<class BaseClass0>)
		ldstr "G2_C133::Method4.6016()"
		ldstr "class G2_C133`1<class BaseClass0> on type class G3_C1117`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C133`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C133`1<class BaseClass0>::Method7<object>()
		calli default string(class G3_C1117`1<class BaseClass0>)
		ldstr "G2_C133::Method7.6015<System.Object>()"
		ldstr "class G2_C133`1<class BaseClass0> on type class G3_C1117`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C133`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C133`1<class BaseClass0>::ClassMethod1343<object>()
		calli default string(class G3_C1117`1<class BaseClass0>)
		ldstr "G2_C133::ClassMethod1343.MI.6027<System.Object>()"
		ldstr "class G2_C133`1<class BaseClass0> on type class G3_C1117`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C133`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C133`1<class BaseClass0>::ClassMethod1342<object>()
		calli default string(class G3_C1117`1<class BaseClass0>)
		ldstr "G2_C133::ClassMethod1342.MI.6026<System.Object>()"
		ldstr "class G2_C133`1<class BaseClass0> on type class G3_C1117`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C133`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C133`1<class BaseClass0>::ClassMethod1341()
		calli default string(class G3_C1117`1<class BaseClass0>)
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G2_C133`1<class BaseClass0> on type class G3_C1117`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C133`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C133`1<class BaseClass0>::ClassMethod1340()
		calli default string(class G3_C1117`1<class BaseClass0>)
		ldstr "G2_C133::ClassMethod1340.MI.6025()"
		ldstr "class G2_C133`1<class BaseClass0> on type class G3_C1117`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1117`1<class BaseClass0>)
		ldstr "G3_C1117::Method7.MI.14410<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1117`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1117`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1117`1<class BaseClass0>::ClassMethod3633<object>()
		calli default string(class G3_C1117`1<class BaseClass0>)
		ldstr "G3_C1117::ClassMethod3633.14414<System.Object>()"
		ldstr "class G3_C1117`1<class BaseClass0> on type class G3_C1117`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1117`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1117`1<class BaseClass0>::ClassMethod3632<object>()
		calli default string(class G3_C1117`1<class BaseClass0>)
		ldstr "G3_C1117::ClassMethod3632.14413<System.Object>()"
		ldstr "class G3_C1117`1<class BaseClass0> on type class G3_C1117`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1117`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1117`1<class BaseClass0>::ClassMethod3631()
		calli default string(class G3_C1117`1<class BaseClass0>)
		ldstr "G3_C1117::ClassMethod3631.14412()"
		ldstr "class G3_C1117`1<class BaseClass0> on type class G3_C1117`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1117`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1117`1<class BaseClass0>::ClassMethod3630()
		calli default string(class G3_C1117`1<class BaseClass0>)
		ldstr "G3_C1117::ClassMethod3630.14411()"
		ldstr "class G3_C1117`1<class BaseClass0> on type class G3_C1117`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1117`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1117`1<class BaseClass0>::Method7<object>()
		calli default string(class G3_C1117`1<class BaseClass0>)
		ldstr "G3_C1117::Method7.14409<System.Object>()"
		ldstr "class G3_C1117`1<class BaseClass0> on type class G3_C1117`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1117`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1117`1<class BaseClass0>::ClassMethod1584<object>()
		calli default string(class G3_C1117`1<class BaseClass0>)
		ldstr "G3_C1117::ClassMethod1584.MI.14416<System.Object>()"
		ldstr "class G3_C1117`1<class BaseClass0> on type class G3_C1117`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1117`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1117`1<class BaseClass0>::ClassMethod1583<object>()
		calli default string(class G3_C1117`1<class BaseClass0>)
		ldstr "G3_C1117::ClassMethod1583.MI.14415<System.Object>()"
		ldstr "class G3_C1117`1<class BaseClass0> on type class G3_C1117`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1117`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1117`1<class BaseClass0>::ClassMethod1582()
		calli default string(class G3_C1117`1<class BaseClass0>)
		ldstr "G2_C133::ClassMethod1582.6022()"
		ldstr "class G3_C1117`1<class BaseClass0> on type class G3_C1117`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1117`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1117`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1117`1<class BaseClass0>)
		ldstr "G2_C133::Method6.6020<System.Object>()"
		ldstr "class G3_C1117`1<class BaseClass0> on type class G3_C1117`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1117`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1117`1<class BaseClass0>::Method5()
		calli default string(class G3_C1117`1<class BaseClass0>)
		ldstr "G2_C133::Method5.6018()"
		ldstr "class G3_C1117`1<class BaseClass0> on type class G3_C1117`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1117`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1117`1<class BaseClass0>::Method4()
		calli default string(class G3_C1117`1<class BaseClass0>)
		ldstr "G2_C133::Method4.6016()"
		ldstr "class G3_C1117`1<class BaseClass0> on type class G3_C1117`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1117`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1117`1<class BaseClass0>::ClassMethod1343<object>()
		calli default string(class G3_C1117`1<class BaseClass0>)
		ldstr "G2_C133::ClassMethod1343.MI.6027<System.Object>()"
		ldstr "class G3_C1117`1<class BaseClass0> on type class G3_C1117`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1117`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1117`1<class BaseClass0>::ClassMethod1342<object>()
		calli default string(class G3_C1117`1<class BaseClass0>)
		ldstr "G2_C133::ClassMethod1342.MI.6026<System.Object>()"
		ldstr "class G3_C1117`1<class BaseClass0> on type class G3_C1117`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1117`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1117`1<class BaseClass0>::ClassMethod1341()
		calli default string(class G3_C1117`1<class BaseClass0>)
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G3_C1117`1<class BaseClass0> on type class G3_C1117`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1117`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1117`1<class BaseClass0>::ClassMethod1340()
		calli default string(class G3_C1117`1<class BaseClass0>)
		ldstr "G2_C133::ClassMethod1340.MI.6025()"
		ldstr "class G3_C1117`1<class BaseClass0> on type class G3_C1117`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G3_C1117`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass0,class BaseClass1>::ClassMethod1343<object>()
		calli default string(class G3_C1117`1<class BaseClass1>)
		ldstr "G2_C133::ClassMethod1343.MI.6027<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G3_C1117`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass0,class BaseClass1>::ClassMethod1342<object>()
		calli default string(class G3_C1117`1<class BaseClass1>)
		ldstr "G2_C133::ClassMethod1342.MI.6026<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G3_C1117`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass0,class BaseClass1>::ClassMethod1341()
		calli default string(class G3_C1117`1<class BaseClass1>)
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G3_C1117`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass0,class BaseClass1>::ClassMethod1340()
		calli default string(class G3_C1117`1<class BaseClass1>)
		ldstr "G2_C133::ClassMethod1340.MI.6025()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G3_C1117`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass0,class BaseClass1>::Method6<object>()
		calli default string(class G3_C1117`1<class BaseClass1>)
		ldstr "G2_C133::Method6.6020<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G3_C1117`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass0,class BaseClass1>::Method5()
		calli default string(class G3_C1117`1<class BaseClass1>)
		ldstr "G1_C10::Method5.4847()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G3_C1117`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass0,class BaseClass1>::Method4()
		calli default string(class G3_C1117`1<class BaseClass1>)
		ldstr "G2_C133::Method4.6016()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G3_C1117`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1117`1<class BaseClass1>)
		ldstr "G2_C133::Method7.6015<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G3_C1117`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1117`1<class BaseClass1>)
		ldstr "G3_C1117::Method7.MI.14410<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1117`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G3_C1117`1<class BaseClass1>)
		ldstr "G2_C133::Method4.MI.6017()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1117`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G3_C1117`1<class BaseClass1>)
		ldstr "G2_C133::Method5.MI.6019()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1117`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1117`1<class BaseClass1>)
		ldstr "G2_C133::Method6.MI.6021<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1117`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1117`1<class BaseClass1>)
		ldstr "G3_C1117::Method7.MI.14410<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1117`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C133`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C133`1<class BaseClass0>::ClassMethod1584<object>()
		calli default string(class G3_C1117`1<class BaseClass1>)
		ldstr "G3_C1117::ClassMethod1584.MI.14416<System.Object>()"
		ldstr "class G2_C133`1<class BaseClass0> on type class G3_C1117`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C133`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C133`1<class BaseClass0>::ClassMethod1583<object>()
		calli default string(class G3_C1117`1<class BaseClass1>)
		ldstr "G3_C1117::ClassMethod1583.MI.14415<System.Object>()"
		ldstr "class G2_C133`1<class BaseClass0> on type class G3_C1117`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C133`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C133`1<class BaseClass0>::ClassMethod1582()
		calli default string(class G3_C1117`1<class BaseClass1>)
		ldstr "G2_C133::ClassMethod1582.6022()"
		ldstr "class G2_C133`1<class BaseClass0> on type class G3_C1117`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C133`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C133`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1117`1<class BaseClass1>)
		ldstr "G2_C133::Method6.6020<System.Object>()"
		ldstr "class G2_C133`1<class BaseClass0> on type class G3_C1117`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C133`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C133`1<class BaseClass0>::Method5()
		calli default string(class G3_C1117`1<class BaseClass1>)
		ldstr "G2_C133::Method5.6018()"
		ldstr "class G2_C133`1<class BaseClass0> on type class G3_C1117`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C133`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C133`1<class BaseClass0>::Method4()
		calli default string(class G3_C1117`1<class BaseClass1>)
		ldstr "G2_C133::Method4.6016()"
		ldstr "class G2_C133`1<class BaseClass0> on type class G3_C1117`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C133`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C133`1<class BaseClass0>::Method7<object>()
		calli default string(class G3_C1117`1<class BaseClass1>)
		ldstr "G2_C133::Method7.6015<System.Object>()"
		ldstr "class G2_C133`1<class BaseClass0> on type class G3_C1117`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C133`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C133`1<class BaseClass0>::ClassMethod1343<object>()
		calli default string(class G3_C1117`1<class BaseClass1>)
		ldstr "G2_C133::ClassMethod1343.MI.6027<System.Object>()"
		ldstr "class G2_C133`1<class BaseClass0> on type class G3_C1117`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C133`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C133`1<class BaseClass0>::ClassMethod1342<object>()
		calli default string(class G3_C1117`1<class BaseClass1>)
		ldstr "G2_C133::ClassMethod1342.MI.6026<System.Object>()"
		ldstr "class G2_C133`1<class BaseClass0> on type class G3_C1117`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C133`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C133`1<class BaseClass0>::ClassMethod1341()
		calli default string(class G3_C1117`1<class BaseClass1>)
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G2_C133`1<class BaseClass0> on type class G3_C1117`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C133`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C133`1<class BaseClass0>::ClassMethod1340()
		calli default string(class G3_C1117`1<class BaseClass1>)
		ldstr "G2_C133::ClassMethod1340.MI.6025()"
		ldstr "class G2_C133`1<class BaseClass0> on type class G3_C1117`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1117`1<class BaseClass1>)
		ldstr "G3_C1117::Method7.MI.14410<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1117`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1117`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1117`1<class BaseClass1>::ClassMethod3633<object>()
		calli default string(class G3_C1117`1<class BaseClass1>)
		ldstr "G3_C1117::ClassMethod3633.14414<System.Object>()"
		ldstr "class G3_C1117`1<class BaseClass1> on type class G3_C1117`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1117`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1117`1<class BaseClass1>::ClassMethod3632<object>()
		calli default string(class G3_C1117`1<class BaseClass1>)
		ldstr "G3_C1117::ClassMethod3632.14413<System.Object>()"
		ldstr "class G3_C1117`1<class BaseClass1> on type class G3_C1117`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1117`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1117`1<class BaseClass1>::ClassMethod3631()
		calli default string(class G3_C1117`1<class BaseClass1>)
		ldstr "G3_C1117::ClassMethod3631.14412()"
		ldstr "class G3_C1117`1<class BaseClass1> on type class G3_C1117`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1117`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1117`1<class BaseClass1>::ClassMethod3630()
		calli default string(class G3_C1117`1<class BaseClass1>)
		ldstr "G3_C1117::ClassMethod3630.14411()"
		ldstr "class G3_C1117`1<class BaseClass1> on type class G3_C1117`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1117`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1117`1<class BaseClass1>::Method7<object>()
		calli default string(class G3_C1117`1<class BaseClass1>)
		ldstr "G3_C1117::Method7.14409<System.Object>()"
		ldstr "class G3_C1117`1<class BaseClass1> on type class G3_C1117`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1117`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1117`1<class BaseClass1>::ClassMethod1584<object>()
		calli default string(class G3_C1117`1<class BaseClass1>)
		ldstr "G3_C1117::ClassMethod1584.MI.14416<System.Object>()"
		ldstr "class G3_C1117`1<class BaseClass1> on type class G3_C1117`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1117`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1117`1<class BaseClass1>::ClassMethod1583<object>()
		calli default string(class G3_C1117`1<class BaseClass1>)
		ldstr "G3_C1117::ClassMethod1583.MI.14415<System.Object>()"
		ldstr "class G3_C1117`1<class BaseClass1> on type class G3_C1117`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1117`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1117`1<class BaseClass1>::ClassMethod1582()
		calli default string(class G3_C1117`1<class BaseClass1>)
		ldstr "G2_C133::ClassMethod1582.6022()"
		ldstr "class G3_C1117`1<class BaseClass1> on type class G3_C1117`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1117`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1117`1<class BaseClass1>::Method6<object>()
		calli default string(class G3_C1117`1<class BaseClass1>)
		ldstr "G2_C133::Method6.6020<System.Object>()"
		ldstr "class G3_C1117`1<class BaseClass1> on type class G3_C1117`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1117`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1117`1<class BaseClass1>::Method5()
		calli default string(class G3_C1117`1<class BaseClass1>)
		ldstr "G2_C133::Method5.6018()"
		ldstr "class G3_C1117`1<class BaseClass1> on type class G3_C1117`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1117`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1117`1<class BaseClass1>::Method4()
		calli default string(class G3_C1117`1<class BaseClass1>)
		ldstr "G2_C133::Method4.6016()"
		ldstr "class G3_C1117`1<class BaseClass1> on type class G3_C1117`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1117`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1117`1<class BaseClass1>::ClassMethod1343<object>()
		calli default string(class G3_C1117`1<class BaseClass1>)
		ldstr "G2_C133::ClassMethod1343.MI.6027<System.Object>()"
		ldstr "class G3_C1117`1<class BaseClass1> on type class G3_C1117`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1117`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1117`1<class BaseClass1>::ClassMethod1342<object>()
		calli default string(class G3_C1117`1<class BaseClass1>)
		ldstr "G2_C133::ClassMethod1342.MI.6026<System.Object>()"
		ldstr "class G3_C1117`1<class BaseClass1> on type class G3_C1117`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1117`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1117`1<class BaseClass1>::ClassMethod1341()
		calli default string(class G3_C1117`1<class BaseClass1>)
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G3_C1117`1<class BaseClass1> on type class G3_C1117`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1117`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1117`1<class BaseClass1>::ClassMethod1340()
		calli default string(class G3_C1117`1<class BaseClass1>)
		ldstr "G2_C133::ClassMethod1340.MI.6025()"
		ldstr "class G3_C1117`1<class BaseClass1> on type class G3_C1117`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1117`1<class BaseClass1>)
		ldstr "G3_C1117::Method7.MI.14410<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G3_C1117`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C133`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass0,class BaseClass1>::ClassMethod1343<object>()
		calli default string(class G2_C133`1<class BaseClass0>)
		ldstr "G2_C133::ClassMethod1343.MI.6027<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G2_C133`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass0,class BaseClass1>::ClassMethod1342<object>()
		calli default string(class G2_C133`1<class BaseClass0>)
		ldstr "G2_C133::ClassMethod1342.MI.6026<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G2_C133`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass0,class BaseClass1>::ClassMethod1341()
		calli default string(class G2_C133`1<class BaseClass0>)
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G2_C133`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass0,class BaseClass1>::ClassMethod1340()
		calli default string(class G2_C133`1<class BaseClass0>)
		ldstr "G2_C133::ClassMethod1340.MI.6025()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G2_C133`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass0,class BaseClass1>::Method6<object>()
		calli default string(class G2_C133`1<class BaseClass0>)
		ldstr "G2_C133::Method6.6020<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G2_C133`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass0,class BaseClass1>::Method5()
		calli default string(class G2_C133`1<class BaseClass0>)
		ldstr "G1_C10::Method5.4847()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G2_C133`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass0,class BaseClass1>::Method4()
		calli default string(class G2_C133`1<class BaseClass0>)
		ldstr "G2_C133::Method4.6016()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G2_C133`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C133`1<class BaseClass0>)
		ldstr "G2_C133::Method7.6015<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G2_C133`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C133`1<class BaseClass0>)
		ldstr "G2_C133::Method7.6015<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C133`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G2_C133`1<class BaseClass0>)
		ldstr "G2_C133::Method4.MI.6017()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C133`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G2_C133`1<class BaseClass0>)
		ldstr "G2_C133::Method5.MI.6019()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C133`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G2_C133`1<class BaseClass0>)
		ldstr "G2_C133::Method6.MI.6021<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C133`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C133`1<class BaseClass0>)
		ldstr "G2_C133::Method7.6015<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C133`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C133`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C133`1<class BaseClass0>::ClassMethod1584<object>()
		calli default string(class G2_C133`1<class BaseClass0>)
		ldstr "G2_C133::ClassMethod1584.6024<System.Object>()"
		ldstr "class G2_C133`1<class BaseClass0> on type class G2_C133`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C133`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C133`1<class BaseClass0>::ClassMethod1583<object>()
		calli default string(class G2_C133`1<class BaseClass0>)
		ldstr "G2_C133::ClassMethod1583.6023<System.Object>()"
		ldstr "class G2_C133`1<class BaseClass0> on type class G2_C133`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C133`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C133`1<class BaseClass0>::ClassMethod1582()
		calli default string(class G2_C133`1<class BaseClass0>)
		ldstr "G2_C133::ClassMethod1582.6022()"
		ldstr "class G2_C133`1<class BaseClass0> on type class G2_C133`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C133`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C133`1<class BaseClass0>::Method6<object>()
		calli default string(class G2_C133`1<class BaseClass0>)
		ldstr "G2_C133::Method6.6020<System.Object>()"
		ldstr "class G2_C133`1<class BaseClass0> on type class G2_C133`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C133`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C133`1<class BaseClass0>::Method5()
		calli default string(class G2_C133`1<class BaseClass0>)
		ldstr "G2_C133::Method5.6018()"
		ldstr "class G2_C133`1<class BaseClass0> on type class G2_C133`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C133`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C133`1<class BaseClass0>::Method4()
		calli default string(class G2_C133`1<class BaseClass0>)
		ldstr "G2_C133::Method4.6016()"
		ldstr "class G2_C133`1<class BaseClass0> on type class G2_C133`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C133`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C133`1<class BaseClass0>::Method7<object>()
		calli default string(class G2_C133`1<class BaseClass0>)
		ldstr "G2_C133::Method7.6015<System.Object>()"
		ldstr "class G2_C133`1<class BaseClass0> on type class G2_C133`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C133`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C133`1<class BaseClass0>::ClassMethod1343<object>()
		calli default string(class G2_C133`1<class BaseClass0>)
		ldstr "G2_C133::ClassMethod1343.MI.6027<System.Object>()"
		ldstr "class G2_C133`1<class BaseClass0> on type class G2_C133`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C133`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C133`1<class BaseClass0>::ClassMethod1342<object>()
		calli default string(class G2_C133`1<class BaseClass0>)
		ldstr "G2_C133::ClassMethod1342.MI.6026<System.Object>()"
		ldstr "class G2_C133`1<class BaseClass0> on type class G2_C133`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C133`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C133`1<class BaseClass0>::ClassMethod1341()
		calli default string(class G2_C133`1<class BaseClass0>)
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G2_C133`1<class BaseClass0> on type class G2_C133`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C133`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C133`1<class BaseClass0>::ClassMethod1340()
		calli default string(class G2_C133`1<class BaseClass0>)
		ldstr "G2_C133::ClassMethod1340.MI.6025()"
		ldstr "class G2_C133`1<class BaseClass0> on type class G2_C133`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C133`1<class BaseClass0>)
		ldstr "G2_C133::Method7.6015<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C133`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C133`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass0,class BaseClass1>::ClassMethod1343<object>()
		calli default string(class G2_C133`1<class BaseClass1>)
		ldstr "G2_C133::ClassMethod1343.MI.6027<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G2_C133`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass0,class BaseClass1>::ClassMethod1342<object>()
		calli default string(class G2_C133`1<class BaseClass1>)
		ldstr "G2_C133::ClassMethod1342.MI.6026<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G2_C133`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass0,class BaseClass1>::ClassMethod1341()
		calli default string(class G2_C133`1<class BaseClass1>)
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G2_C133`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass0,class BaseClass1>::ClassMethod1340()
		calli default string(class G2_C133`1<class BaseClass1>)
		ldstr "G2_C133::ClassMethod1340.MI.6025()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G2_C133`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass0,class BaseClass1>::Method6<object>()
		calli default string(class G2_C133`1<class BaseClass1>)
		ldstr "G2_C133::Method6.6020<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G2_C133`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass0,class BaseClass1>::Method5()
		calli default string(class G2_C133`1<class BaseClass1>)
		ldstr "G1_C10::Method5.4847()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G2_C133`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass0,class BaseClass1>::Method4()
		calli default string(class G2_C133`1<class BaseClass1>)
		ldstr "G2_C133::Method4.6016()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G2_C133`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C10`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C10`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C133`1<class BaseClass1>)
		ldstr "G2_C133::Method7.6015<System.Object>()"
		ldstr "class G1_C10`2<class BaseClass0,class BaseClass1> on type class G2_C133`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C133`1<class BaseClass1>)
		ldstr "G2_C133::Method7.6015<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C133`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G2_C133`1<class BaseClass1>)
		ldstr "G2_C133::Method4.MI.6017()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C133`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G2_C133`1<class BaseClass1>)
		ldstr "G2_C133::Method5.MI.6019()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C133`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G2_C133`1<class BaseClass1>)
		ldstr "G2_C133::Method6.MI.6021<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C133`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C133`1<class BaseClass1>)
		ldstr "G2_C133::Method7.6015<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C133`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C133`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C133`1<class BaseClass1>::ClassMethod1584<object>()
		calli default string(class G2_C133`1<class BaseClass1>)
		ldstr "G2_C133::ClassMethod1584.6024<System.Object>()"
		ldstr "class G2_C133`1<class BaseClass1> on type class G2_C133`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C133`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C133`1<class BaseClass1>::ClassMethod1583<object>()
		calli default string(class G2_C133`1<class BaseClass1>)
		ldstr "G2_C133::ClassMethod1583.6023<System.Object>()"
		ldstr "class G2_C133`1<class BaseClass1> on type class G2_C133`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C133`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C133`1<class BaseClass1>::ClassMethod1582()
		calli default string(class G2_C133`1<class BaseClass1>)
		ldstr "G2_C133::ClassMethod1582.6022()"
		ldstr "class G2_C133`1<class BaseClass1> on type class G2_C133`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C133`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C133`1<class BaseClass1>::Method6<object>()
		calli default string(class G2_C133`1<class BaseClass1>)
		ldstr "G2_C133::Method6.6020<System.Object>()"
		ldstr "class G2_C133`1<class BaseClass1> on type class G2_C133`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C133`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C133`1<class BaseClass1>::Method5()
		calli default string(class G2_C133`1<class BaseClass1>)
		ldstr "G2_C133::Method5.6018()"
		ldstr "class G2_C133`1<class BaseClass1> on type class G2_C133`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C133`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C133`1<class BaseClass1>::Method4()
		calli default string(class G2_C133`1<class BaseClass1>)
		ldstr "G2_C133::Method4.6016()"
		ldstr "class G2_C133`1<class BaseClass1> on type class G2_C133`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C133`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C133`1<class BaseClass1>::Method7<object>()
		calli default string(class G2_C133`1<class BaseClass1>)
		ldstr "G2_C133::Method7.6015<System.Object>()"
		ldstr "class G2_C133`1<class BaseClass1> on type class G2_C133`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C133`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C133`1<class BaseClass1>::ClassMethod1343<object>()
		calli default string(class G2_C133`1<class BaseClass1>)
		ldstr "G2_C133::ClassMethod1343.MI.6027<System.Object>()"
		ldstr "class G2_C133`1<class BaseClass1> on type class G2_C133`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C133`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C133`1<class BaseClass1>::ClassMethod1342<object>()
		calli default string(class G2_C133`1<class BaseClass1>)
		ldstr "G2_C133::ClassMethod1342.MI.6026<System.Object>()"
		ldstr "class G2_C133`1<class BaseClass1> on type class G2_C133`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C133`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C133`1<class BaseClass1>::ClassMethod1341()
		calli default string(class G2_C133`1<class BaseClass1>)
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G2_C133`1<class BaseClass1> on type class G2_C133`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C133`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C133`1<class BaseClass1>::ClassMethod1340()
		calli default string(class G2_C133`1<class BaseClass1>)
		ldstr "G2_C133::ClassMethod1340.MI.6025()"
		ldstr "class G2_C133`1<class BaseClass1> on type class G2_C133`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G2_C133`1<class BaseClass1>)
		ldstr "G2_C133::Method7.6015<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G2_C133`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C133`1<class BaseClass1>)
		ldstr "G2_C133::Method7.6015<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C133`1<class BaseClass1>"
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
