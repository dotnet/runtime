// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern mscorlib { }
.assembly DevDiv_815941 { }
.assembly extern xunit.core {}
.module DevDiv_815941.exe

.class Repro
{
    .method private hidebysig static int32 Main() cil managed
    {
        .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
            01 00 00 00
        )
        // This testcase declares a union of two fields: double and long
        // We ensure that if a non-zero value is stored in the double field
        // the value returned for the long field is also non-zero
  
        .entrypoint
        .maxstack  2
        .locals init (valuetype DoubleConverter V_0,
            int64 V_1)
        
        ldc.r8     1.23
        ldloca.s   V_0
        call       int64 DoubleConverter::ToLong(float64, valuetype DoubleConverter&)
        stloc.1
        ldloc.1
        ldc.i8 0
        ceq
        brtrue.s   FAIL
        ldstr      "PASS!"
        call       void [System.Console]System.Console::WriteLine(string)
        ldc.i4.s   100
        ret
FAIL:
        ldstr      "FAIL!"
        call       void [System.Console]System.Console::WriteLine(string)
        ldloc.1
        call       void [System.Console]System.Console::WriteLine(int64)
        ldc.i4.s   101
        ret
    } 
}

.class public explicit sealed DoubleConverter extends [mscorlib]System.ValueType
{
    .field [0] private float64 d
    .field [0] private int64 l
    
    .method public hidebysig static int64 ToLong(float64 'value',
        valuetype DoubleConverter& converter) cil managed
    {
        .maxstack  2
        .locals init (int64 V_0)
        
        nop
        ldarg.1
        ldarg.0
        stfld      float64 DoubleConverter::d
        ldarg.1
        ldfld      int64 DoubleConverter::l
        stloc.0
        ldloc.0
        ret
    }

    .method public hidebysig static float64 ToDouble(int64 'value',
        valuetype DoubleConverter& converter) cil managed
    {
        .maxstack  2
        .locals init (float64 V_0)
        
        nop
        ldarg.1
        ldarg.0
        stfld      int64 DoubleConverter::l
        ldarg.1
        ldfld      float64 DoubleConverter::d
        stloc.0
        ldloc.0
        ret
    }

}
