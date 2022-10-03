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
.assembly 'prefldinit2'
{
  // --- The following custom attribute is added automatically, do not uncomment -------
  //  .custom instance void [mscorlib]System.Diagnostics.DebuggableAttribute::.ctor(bool,
  //                                                                                bool) = ( 01 00 00 01 00 00 ) 
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.assembly extern xunit.core {}

.namespace Test_prefldinit2_simple_il
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
    IL_0005:  stsfld     int32 Test_prefldinit2_simple_il.measure::a
    IL_000a:  ret
  } // end of method Test_prefldinit2_simple_il.measure::.cctor

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Test_prefldinit2_simple_il.measure::.ctor

} // end of class Test_prefldinit2_simple_il.measure

.class private auto ansi test
       extends [mscorlib]System.Object
{
  .method private hidebysig specialname rtspecialname static 
          void  .cctor() cil managed
  {
    // Code size       139 (0x8b)
    .maxstack  2
    IL_0000:  ldsfld     int32 Test_prefldinit2_simple_il.measure::a
    IL_0005:  ldc.i4     0xcc
    IL_000a:  beq.s      IL_0030

    IL_000c:  ldstr      "in .cctor(), Test_prefldinit2_simple_il.measure.a is {0}"
    IL_0011:  ldsfld     int32 Test_prefldinit2_simple_il.measure::a
    IL_0016:  box        [mscorlib]System.Int32
    IL_001b:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object)
    IL_0020:  ldstr      "FAILED"
    IL_0025:  call       void [System.Console]System.Console::WriteLine(string)
    IL_002a:  newobj     instance void [mscorlib]System.Exception::.ctor()
    IL_002f:  throw

    IL_0030:  ldstr      "in .cctor(), Test_prefldinit2_simple_il.measure.a is {0}"
    IL_0035:  ldsfld     int32 Test_prefldinit2_simple_il.measure::a
    IL_003a:  box        [mscorlib]System.Int32
    IL_003f:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object)
    IL_0044:  ldc.i4.8
    IL_0045:  stsfld     int32 Test_prefldinit2_simple_il.measure::a
    IL_004a:  ldsfld     int32 Test_prefldinit2_simple_il.measure::a
    IL_004f:  ldc.i4.8
    IL_0050:  beq.s      IL_0076

    IL_0052:  ldstr      "in .cctor() after Test_prefldinit2_simple_il.measure.a=8, Test_prefldinit2_simple_il.measure.a is {0}"
    IL_0057:  ldsfld     int32 Test_prefldinit2_simple_il.measure::a
    IL_005c:  box        [mscorlib]System.Int32
    IL_0061:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object)
    IL_0066:  ldstr      "FAILED"
    IL_006b:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0070:  newobj     instance void [mscorlib]System.Exception::.ctor()
    IL_0075:  throw

    IL_0076:  ldstr      "in .cctor() after Test_prefldinit2_simple_il.measure.a=8, Test_prefldinit2_simple_il.measure.a is {0}"
    IL_007b:  ldsfld     int32 Test_prefldinit2_simple_il.measure::a
    IL_0080:  box        [mscorlib]System.Int32
    IL_0085:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object)
    IL_008a:  ret
  } // end of method Test_prefldinit2_simple_il.test::.cctor

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Test_prefldinit2_simple_il.test::.ctor

} // end of class Test_prefldinit2_simple_il.test

.class public auto ansi beforefieldinit Driver
       extends [mscorlib]System.Object
{
  .method public hidebysig static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       169 (0xa9)
    .maxstack  2
    .locals init (class Test_prefldinit2_simple_il.test V_0,
             class [mscorlib]System.Exception V_1,
             int32 V_2)
    .try
    {
      IL_0000:  ldstr      "Testing .cctor() invocation by calling instance method"
      IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
      ldstr "We have beforefieldinit set and we are accessing instance method rather than static method or static field, Test_prefldinit2_simple_il.test may fail by design?"
      call       void [System.Console]System.Console::WriteLine(string)
      IL_000a:  call       void [System.Console]System.Console::WriteLine()
      IL_000f:  ldstr      "Before calling instance method"
      IL_0014:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0019:  ldsfld     int32 Test_prefldinit2_simple_il.measure::a
      IL_001e:  ldc.i4     0xcc
      IL_0023:  beq.s      IL_0047

      IL_0025:  ldstr      "in Main(), Test_prefldinit2_simple_il.measure.a is {0}"
      IL_002a:  ldsfld     int32 Test_prefldinit2_simple_il.measure::a
      IL_002f:  box        [mscorlib]System.Int32
      IL_0034:  call       void [System.Console]System.Console::WriteLine(string,
                                                                    object)
      IL_0039:  ldstr      "FAILED"
      IL_003e:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0043:  ldc.i4.1
      IL_0044:  stloc.2
      IL_0045:  leave.s    IL_00a7

      IL_0047:  newobj     instance void Test_prefldinit2_simple_il.test::.ctor()
      IL_004c:  stloc.0
      IL_004d:  ldstr      "After calling instance method"
      IL_0052:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0057:  ldsfld     int32 Test_prefldinit2_simple_il.measure::a
      IL_005c:  ldc.i4.8
      IL_005d:  beq.s      IL_0081

      IL_005f:  ldstr      "in Main() after new Test_prefldinit2_simple_il.test(), Test_prefldinit2_simple_il.measure.a is {0}"
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      IL_0064:  ldsfld     int32 Test_prefldinit2_simple_il.measure::a
      IL_0069:  box        [mscorlib]System.Int32
      IL_006e:  call       void [System.Console]System.Console::WriteLine(string,
                                                                    object)
      IL_0073:  ldstr      "FAILED"
      IL_0078:  call       void [System.Console]System.Console::WriteLine(string)
      IL_007d:  ldc.i4.m1
      IL_007e:  stloc.2
      IL_007f:  leave.s    IL_00a7

      IL_0081:  leave.s    IL_0093

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      IL_0083:  stloc.1
      IL_0084:  ldloc.1
      IL_0085:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_008a:  call       void [System.Console]System.Console::WriteLine(string)
      IL_008f:  ldc.i4.m1
      IL_0090:  stloc.2
      IL_0091:  leave.s    IL_00a7

    }  // end handler
    IL_0093:  call       void [System.Console]System.Console::WriteLine()
    IL_0098:  ldstr      "PASSED"
    IL_009d:  call       void [System.Console]System.Console::WriteLine(string)
    IL_00a2:  ldc.i4.s   100
    IL_00a4:  stloc.2
    IL_00a5:  br.s       IL_00a7

    IL_00a7:  ldloc.2
    IL_00a8:  ret
  } // end of method Test_prefldinit2_simple_il.Driver::Main

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Test_prefldinit2_simple_il.Driver::.ctor

} // end of class Test_prefldinit2_simple_il.Driver


// =============================================================

//*********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file prefldinit2.res
}
