// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern mscorlib { .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) .ver 4:0:0:0 }

//TYPES IN FORWARDER ASSEMBLIES:

//TEST ASSEMBLY:
.assembly Generated1358 { .hash algorithm 0x00008004 }
.module Generated1358.dll

.class private BaseClass0 
{
    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
        ldarg.0
        call instance void [mscorlib]System.Object::.ctor()
        ret
    }
}
.class private BaseClass1 
        extends BaseClass0
{
    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
        ldarg.0
        call instance void BaseClass0::.ctor()
        ret
    }
}
.class private G3_C1830`1<T0> 
        extends class G2_C774`2<!T0,class BaseClass0>
{
    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
        ldarg.0
        call instance void class G2_C774`2<!T0,class BaseClass0>::.ctor()
        ret
    }
}
.class private G2_C774`2<T0, T1> 
        extends class G1_C14`2<!T0,class BaseClass0>
{
    .method public hidebysig newslot virtual instance string 'G1_C14<T0,class BaseClass0>.ClassMethod1350'<M0>() cil managed noinlining { 
        .override method instance string class G1_C14`2<!T0,class BaseClass0>::ClassMethod1350<[1]>()
        ldstr "G2_C774::ClassMethod1350.MI.12159<"
        ldtoken !!M0
        call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
        call string [mscorlib]System.String::Concat(object,object)
        ldstr ">()"
        call string [mscorlib]System.String::Concat(object,object)
        ret
    }
    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
        ldarg.0
        call instance void class G1_C14`2<!T0,class BaseClass0>::.ctor()
        ret
    }
}
.class private G1_C14`2<T0, T1> 
        implements class IBase2`2<!T1,class BaseClass1>, class IBase1`1<class BaseClass1> 
{
    .method public hidebysig newslot virtual instance string ClassMethod1350<M0>() cil managed noinlining { 
        ldstr "G1_C14::ClassMethod1350.4883<"
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
.class interface private abstract IBase2`2<+T0, -T1> 
{
}
.class interface private abstract IBase1`1<+T0> 
{
}
.class public auto ansi beforefieldinit TypeGeneratorTest1358 {
    .method static bool M.G2_C774.T.T<T0,T1,(class G2_C774`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
        .maxstack 14
        ldarga.s     0
        constrained. !!W
        callvirt     instance string class G2_C774`2<!!T0,!!T1>::ClassMethod1350<object>()
        ldarg.1
        call         bool [mscorlib]System.String::Equals(string, string)
        ret
    }
    .method public hidebysig static bool ConstrainedCallsTest() cil managed
    {
        .maxstack  10
        .locals init (object V_0)
        newobj instance void class G3_C1830`1<class BaseClass0>::.ctor()
        stloc.0
        ldloc.0
        ldstr "G2_C774::ClassMethod1350.MI.12159<System.Object>()"
        call bool TypeGeneratorTest1358::M.G2_C774.T.T<class BaseClass0,class BaseClass0,class G3_C1830`1<class BaseClass0>>(!!2,string)
        ret
    }
}
