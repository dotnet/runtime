// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern mscorlib { .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) .ver 4:0:0:0 }

//TYPES IN FORWARDER ASSEMBLIES:

//TEST ASSEMBLY:
.assembly Generated612 { .hash algorithm 0x00008004 }
.module Generated612.dll

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
.class public G3_C1084`1<T0> 
        extends G2_C21
        implements class IBase2`2<class BaseClass1,!T0> 
{
    .method public hidebysig newslot virtual instance string 'G2_C21.ClassMethod1356'<M0>() cil managed noinlining { 
        .override method instance string G2_C21::ClassMethod1356<[1]>()
        ldstr "G3_C1084::ClassMethod1356.MI.14216<"
        ldtoken !!M0
        call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
        call string [mscorlib]System.String::Concat(object,object)
        ldstr ">()"
        call string [mscorlib]System.String::Concat(object,object)
        ret
    }
    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
        ldarg.0
        call instance void G2_C21::.ctor()
        ret
    }
}
.class public G2_C21 
        extends class G1_C6`2<class BaseClass0,class BaseClass0>
        implements class IBase2`2<class BaseClass0,class BaseClass1>, class IBase1`1<class BaseClass1> 
{
    .method public hidebysig newslot virtual instance string ClassMethod1356<M0>() cil managed noinlining { 
        ldstr "G2_C21::ClassMethod1356.4932<"
        ldtoken !!M0
        call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
        call string [mscorlib]System.String::Concat(object,object)
        ldstr ">()"
        call string [mscorlib]System.String::Concat(object,object)
        ret
    }
    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
        ldarg.0
        call instance void class G1_C6`2<class BaseClass0,class BaseClass0>::.ctor()
        ret
    }
}
.class interface public abstract IBase2`2<+T0, -T1> 
{
}
.class public G1_C6`2<T0, T1> 
        implements class IBase2`2<class BaseClass1,!T0> 
{
    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
        ldarg.0
        call instance void [mscorlib]System.Object::.ctor()
        ret
    }
}
.class interface public abstract IBase1`1<+T0> 
{
}
.class public auto ansi beforefieldinit TypeGeneratorTest612 {
    .method static bool M.G3_C1084.T<T0,(class G3_C1084`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
        .maxstack 14
        ldarga.s     0
        constrained. !!W
        callvirt     instance string class G3_C1084`1<!!T0>::ClassMethod1356<object>()
        ldarg.1
        call         bool [mscorlib]System.String::Equals(string, string)
        ret
    }
    .method public hidebysig static bool ConstrainedCallsTest() cil managed
    {
        .maxstack  10
        .locals init (object V_0)
        newobj instance void class G3_C1084`1<class BaseClass0>::.ctor()
        stloc.0
        ldloc.0
        ldstr "G3_C1084::ClassMethod1356.MI.14216<System.Object>()"
        call bool TypeGeneratorTest612::M.G3_C1084.T<class BaseClass0,class G3_C1084`1<class BaseClass0>>(!!1,string)
        ret
    }
}
