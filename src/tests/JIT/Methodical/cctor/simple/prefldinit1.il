// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}



.assembly extern mscorlib
{
}
.assembly 'prefldinit1'
{
  // --- The following custom attribute is added automatically, do not uncomment -------
  //  .custom instance void [mscorlib]System.Diagnostics.DebuggableAttribute::.ctor(bool,
  //                                                                                bool) = ( 01 00 00 01 00 00 ) 
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.assembly extern xunit.core {}

.namespace Test_prefldinit1_simple_il
{
.class private auto ansi beforefieldinit measure
       extends [mscorlib]System.Object
{
  .field public static int32 a
  .method private hidebysig specialname rtspecialname static 
          void  .cctor() cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    IL_0000:  ldc.i4     0xcc
    IL_0005:  stsfld     int32 Test_prefldinit1_simple_il.measure::a
    IL_000a:  ret
  } // end of method Test_prefldinit1_simple_il.measure::.cctor

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Test_prefldinit1_simple_il.measure::.ctor

} // end of class Test_prefldinit1_simple_il.measure

.class private auto ansi test
       extends [mscorlib]System.Object
{
  .method public hidebysig static void  f(unsigned int8& b) cil managed
  {
    // Code size       3 (0x3)
    .maxstack  0
    IL_0000:  br.s       IL_0002

    IL_0002:  ret
  } // end of method Test_prefldinit1_simple_il.test::f

  .method private hidebysig specialname rtspecialname static 
          void  .cctor() cil managed
  {
    // Code size       139 (0x8b)
    .maxstack  2
    IL_0000:  ldsfld     int32 Test_prefldinit1_simple_il.measure::a
    IL_0005:  ldc.i4     0xcc
    IL_000a:  beq.s      IL_0030

    IL_000c:  ldstr      "in .cctor(), Test_prefldinit1_simple_il.measure.a is {0}"
    IL_0011:  ldsfld     int32 Test_prefldinit1_simple_il.measure::a
    IL_0016:  box        [mscorlib]System.Int32
    IL_001b:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object)
    IL_0020:  ldstr      "FAILED"
    IL_0025:  call       void [System.Console]System.Console::WriteLine(string)
    IL_002a:  newobj     instance void [mscorlib]System.Exception::.ctor()
    IL_002f:  throw

    IL_0030:  ldstr      "in .cctor(), Test_prefldinit1_simple_il.measure.a is {0}"
    IL_0035:  ldsfld     int32 Test_prefldinit1_simple_il.measure::a
    IL_003a:  box        [mscorlib]System.Int32
    IL_003f:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object)
    IL_0044:  ldc.i4.8
    IL_0045:  stsfld     int32 Test_prefldinit1_simple_il.measure::a
    IL_004a:  ldsfld     int32 Test_prefldinit1_simple_il.measure::a
    IL_004f:  ldc.i4.8
    IL_0050:  beq.s      IL_0076

    IL_0052:  ldstr      "in .cctor() after Test_prefldinit1_simple_il.measure.a=8, Test_prefldinit1_simple_il.measure.a is {0}"
    IL_0057:  ldsfld     int32 Test_prefldinit1_simple_il.measure::a
    IL_005c:  box        [mscorlib]System.Int32
    IL_0061:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object)
    IL_0066:  ldstr      "FAILED"
    IL_006b:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0070:  newobj     instance void [mscorlib]System.Exception::.ctor()
    IL_0075:  throw

    IL_0076:  ldstr      "in .cctor() after Test_prefldinit1_simple_il.measure.a=8, Test_prefldinit1_simple_il.measure.a is {0}"
    IL_007b:  ldsfld     int32 Test_prefldinit1_simple_il.measure::a
    IL_0080:  box        [mscorlib]System.Int32
    IL_0085:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object)
    IL_008a:  ret
  } // end of method Test_prefldinit1_simple_il.test::.cctor

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Test_prefldinit1_simple_il.test::.ctor

} // end of class Test_prefldinit1_simple_il.test

.class public auto ansi beforefieldinit Driver
       extends [mscorlib]System.Object
{
  .method public hidebysig static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       173 (0xad)
    .maxstack  2
    .locals init (unsigned int8 V_0,
             class [mscorlib]System.Exception V_1,
             int32 V_2)
    .try
    {
      IL_0000:  ldc.i4.s   15
      IL_0002:  stloc.0
      IL_0003:  ldstr      "Testing .cctor() invocation by calling static method"
      IL_0008:  call       void [System.Console]System.Console::WriteLine(string)
      IL_000d:  call       void [System.Console]System.Console::WriteLine()
      IL_0012:  ldstr      "Before calling static method"
      IL_0017:  call       void [System.Console]System.Console::WriteLine(string)
      IL_001c:  ldsfld     int32 Test_prefldinit1_simple_il.measure::a
      IL_0021:  ldc.i4     0xcc
      IL_0026:  beq.s      IL_004a

      IL_0028:  ldstr      "in Main(), Test_prefldinit1_simple_il.measure.a is {0}"
      IL_002d:  ldsfld     int32 Test_prefldinit1_simple_il.measure::a
      IL_0032:  box        [mscorlib]System.Int32
      IL_0037:  call       void [System.Console]System.Console::WriteLine(string,
                                                                    object)
      IL_003c:  ldstr      "FAILED"
      IL_0041:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0046:  ldc.i4.1
      IL_0047:  stloc.2
      IL_0048:  leave.s    IL_00ab

      IL_004a:  ldloca.s   V_0
      IL_004c:  call       void Test_prefldinit1_simple_il.test::f(unsigned int8&)
      IL_0051:  ldstr      "After calling static method"
      IL_0056:  call       void [System.Console]System.Console::WriteLine(string)
      IL_005b:  ldsfld     int32 Test_prefldinit1_simple_il.measure::a
      IL_0060:  ldc.i4.8
      IL_0061:  beq.s      IL_0085

      IL_0063:  ldstr      "in Main() after f(ref b), Test_prefldinit1_simple_il.measure.a is {0}"
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      IL_0068:  ldsfld     int32 Test_prefldinit1_simple_il.measure::a
      IL_006d:  box        [mscorlib]System.Int32
      IL_0072:  call       void [System.Console]System.Console::WriteLine(string,
                                                                    object)
      IL_0077:  ldstr      "FAILED"
      IL_007c:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0081:  ldc.i4.m1
      IL_0082:  stloc.2
      IL_0083:  leave.s    IL_00ab

      IL_0085:  leave.s    IL_0097

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      IL_0087:  stloc.1
      IL_0088:  ldloc.1
      IL_0089:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_008e:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0093:  ldc.i4.m1
      IL_0094:  stloc.2
      IL_0095:  leave.s    IL_00ab

    }  // end handler
    IL_0097:  call       void [System.Console]System.Console::WriteLine()
    IL_009c:  ldstr      "PASSED"
    IL_00a1:  call       void [System.Console]System.Console::WriteLine(string)
    IL_00a6:  ldc.i4.s   100
    IL_00a8:  stloc.2
    IL_00a9:  br.s       IL_00ab

    IL_00ab:  ldloc.2
    IL_00ac:  ret
  } // end of method Test_prefldinit1_simple_il.Driver::Main

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Test_prefldinit1_simple_il.Driver::.ctor

} // end of class Test_prefldinit1_simple_il.Driver


// =============================================================

//*********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file prefldinit1.res
}
