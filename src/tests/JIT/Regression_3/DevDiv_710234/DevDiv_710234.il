// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { auto }
.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
.assembly extern System.Console { auto }

.assembly DevDiv_710234 { }
.assembly extern xunit.core {}

.class public auto ansi beforefieldinit DevDiv_710234
       extends [System.Runtime]System.Object
{
    .method static int64 Test(int16,int32,bool,float64,int32,int32)
    {
        .maxstack  177
        .locals init (uint16,int32,uint16,int32,int64,int8,int32,float32,int16,int16,int32,int8,int64,bool)

          ldarg.s      0x1
          ldloc.s      0x7
          ldc.r8       2.57
          clt.un      
          clt.un      
          ldloc        0x4
          ldc.i8       0x54911585c723fe1a
          mul.ovf.un  
          conv.ovf.u4.un
          clt.un      
          ldloc        0x7
          conv.ovf.i8 
          ldloc        0x5
          ldloc        0x2
          or          
          shr.un      
          conv.ovf.u1.un
          ldloc.s      0x4
          conv.ovf.i2 
          or          
          ceq         
          brtrue       Lc7

          ldarg.s      0x3
          neg         
          ldloc.s      0xB
          ldloc.s      0x3
          conv.r4     
          ldarg.s      0x5
          conv.r4     
          cgt.un      
          cgt.un      
          neg         
          stloc.s      0x2
          conv.i4     
          brtrue       La5

          ldc.r8       2.2159438665601046e+14
          ckfinite    
          ldloc        0xC
          ldarg        0x3
          conv.ovf.u8.un
          ceq         
          not         
          conv.r.un   
          div         
          stloc.s      0x7
          ldarg        0x3
          starg.s      0x3

    La5:  ldloc        0x7
          conv.u      
          ldloc        0x7
          neg         
          ldc.r8       -2.1382977803754743e-2
          clt.un      
          neg         
          ldloc        0x4
          conv.ovf.u2.un
          neg         
          shr         
          mul         
          starg        0x4

    Lc7:  ldloc.s      0x7
          ldloc        0x7
          ldloc        0x7
          rem         
          clt         
          ldloc.s      0x5
          neg         
          conv.ovf.i  
          not         
          shr         
          conv.r8     
          ckfinite    
          pop
          ldc.i8       0
          ldarg        0x3
          ckfinite    
          conv.i8     
          add  
          ret    
    }
    .method public hidebysig static int32 Main() cil managed
    {
        .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
            01 00 00 00
        )
        .entrypoint
        .locals init (int32 returnValue, int64 TestResult)
        nop
        ldc.i4.s   100
        stloc.0
        .try
        {
            ldc.i4.1
            ldc.i4.2
            ldc.i4.0
            ldc.r8     3.
            ldc.i4.4
            ldc.i4.5
            call       int64 DevDiv_710234::Test(int16,int32,bool,float64,int32,int32)
            stloc.1
            ldloc.1
            ldc.i8     3
            ceq
            brtrue     L0

            ldstr      "Test Result = "
            call       void [System.Console]System.Console::Write(string)
            ldloc.1
            call       void [System.Console]System.Console::WriteLine(int64)
            ldc.i4.m1
            stloc.0
            nop
L0:
            leave.s    L1
        }

        catch [System.Runtime]System.Object 
        {
            callvirt    instance string [mscorlib]System.Exception::get_Message()
            call        void [System.Console]System.Console::WriteLine(string)
            leave.s    L1
        }

    L1: ldloc.0
        ret
    } // end of method DevDiv_710234::Main


    .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
    {
        ldarg.0
        call       instance void [System.Runtime]System.Object::.ctor()
        ret
    } // end of method DevDiv_710234::.ctor

} // end of class DevDiv_710234
