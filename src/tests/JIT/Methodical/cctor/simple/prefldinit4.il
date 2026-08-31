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
.assembly ASSEMBLY_NAME
{
  // --- The following custom attribute is added automatically, do not uncomment -------
  //  .custom instance void [mscorlib]System.Diagnostics.DebuggableAttribute::.ctor(bool,
  //                                                                                bool) = ( 01 00 00 01 00 00 ) 
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.assembly extern xunit.core {}

.namespace Test_prefldinit4_simple_il
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
    IL_0005:  stsfld     int32 Test_prefldinit4_simple_il.measure::a
    IL_000a:  ret
  } // end of method Test_prefldinit4_simple_il.measure::.cctor

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Test_prefldinit4_simple_il.measure::.ctor

} // end of class Test_prefldinit4_simple_il.measure

.class private auto ansi test
       extends [mscorlib]System.Object
{
  .field public static unsigned int8 b
  .method private hidebysig specialname rtspecialname static 
          void  .cctor() cil managed
  {
    // Code size       160 (0xa0)
    .maxstack  2
    IL_0000:  ldc.i4.s   12
    IL_0002:  stsfld     unsigned int8 Test_prefldinit4_simple_il.test::b
    IL_0007:  ldsfld     int32 Test_prefldinit4_simple_il.measure::a
    IL_000c:  ldc.i4     0xcc
    IL_0011:  beq.s      IL_0037

    IL_0013:  ldstr      "in .cctor(), Test_prefldinit4_simple_il.measure.a is {0}"
    IL_0018:  ldsfld     int32 Test_prefldinit4_simple_il.measure::a
    IL_001d:  box        [mscorlib]System.Int32
    IL_0022:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object)
    IL_0027:  ldstr      "FAILED"
    IL_002c:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0031:  newobj     instance void [mscorlib]System.Exception::.ctor()
    IL_0036:  throw

    IL_0037:  ldstr      "in .cctor(), Test_prefldinit4_simple_il.measure.a is {0}"
    IL_003c:  ldsfld     int32 Test_prefldinit4_simple_il.measure::a
    IL_0041:  box        [mscorlib]System.Int32
    IL_0046:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object)
    IL_004b:  ldsfld     int32 Test_prefldinit4_simple_il.measure::a
    IL_0050:  ldsfld     unsigned int8 Test_prefldinit4_simple_il.test::b
    IL_0055:  add
    IL_0056:  stsfld     int32 Test_prefldinit4_simple_il.measure::a
    IL_005b:  ldsfld     int32 Test_prefldinit4_simple_il.measure::a
    IL_0060:  ldc.i4     0xd8
    IL_0065:  beq.s      IL_008b

    IL_0067:  ldstr      "in .cctor() after Test_prefldinit4_simple_il.measure.a+=b, Test_prefldinit4_simple_il.measure.a is {0}"
    IL_006c:  ldsfld     int32 Test_prefldinit4_simple_il.measure::a
    IL_0071:  box        [mscorlib]System.Int32
    IL_0076:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object)
    IL_007b:  ldstr      "FAILED"
    IL_0080:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0085:  newobj     instance void [mscorlib]System.Exception::.ctor()
    IL_008a:  throw

    IL_008b:  ldstr      "in .cctor() after Test_prefldinit4_simple_il.measure.a=8, Test_prefldinit4_simple_il.measure.a is {0}"
    IL_0090:  ldsfld     int32 Test_prefldinit4_simple_il.measure::a
    IL_0095:  box        [mscorlib]System.Int32
    IL_009a:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object)
    IL_009f:  ret
  } // end of method Test_prefldinit4_simple_il.test::.cctor

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Test_prefldinit4_simple_il.test::.ctor

} // end of class Test_prefldinit4_simple_il.test

.class public auto ansi beforefieldinit Driver
       extends [mscorlib]System.Object
{
  .method public hidebysig static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       174 (0xae)
    .maxstack  2
    .locals init (class [mscorlib]System.Exception V_0,
             int32 V_1)
    .try
    {
      IL_0000:  ldstr      "Testing .cctor() invocation by accessing static field"
      IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
      IL_000a:  call       void [System.Console]System.Console::WriteLine()
      IL_000f:  ldstr      "Before calling static field"
      IL_0014:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0019:  ldsfld     int32 Test_prefldinit4_simple_il.measure::a
      IL_001e:  ldc.i4     0xcc
      IL_0023:  beq.s      IL_0047

      IL_0025:  ldstr      "in Main(), Test_prefldinit4_simple_il.measure.a is {0}"
      IL_002a:  ldsfld     int32 Test_prefldinit4_simple_il.measure::a
      IL_002f:  box        [mscorlib]System.Int32
      IL_0034:  call       void [System.Console]System.Console::WriteLine(string,
                                                                    object)
      IL_0039:  ldstr      "FAILED"
      IL_003e:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0043:  ldc.i4.1
      IL_0044:  stloc.1
      IL_0045:  leave.s    IL_00ac

      IL_0047:  ldc.i4.s   15
      IL_0049:  stsfld     unsigned int8 Test_prefldinit4_simple_il.test::b
      IL_004e:  ldstr      "After calling static field"
      IL_0053:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0058:  ldsfld     int32 Test_prefldinit4_simple_il.measure::a
      IL_005d:  ldc.i4     0xd8
      IL_0062:  beq.s      IL_0086

      IL_0064:  ldstr      "in Main() after f(ref b), Test_prefldinit4_simple_il.measure.a is {0}"
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      IL_0069:  ldsfld     int32 Test_prefldinit4_simple_il.measure::a
      IL_006e:  box        [mscorlib]System.Int32
      IL_0073:  call       void [System.Console]System.Console::WriteLine(string,
                                                                    object)
      IL_0078:  ldstr      "FAILED"
      IL_007d:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0082:  ldc.i4.m1
      IL_0083:  stloc.1
      IL_0084:  leave.s    IL_00ac

      IL_0086:  leave.s    IL_0098

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      IL_0088:  stloc.0
      IL_0089:  ldloc.0
      IL_008a:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_008f:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0094:  ldc.i4.m1
      IL_0095:  stloc.1
      IL_0096:  leave.s    IL_00ac

    }  // end handler
    IL_0098:  call       void [System.Console]System.Console::WriteLine()
    IL_009d:  ldstr      "PASSED"
    IL_00a2:  call       void [System.Console]System.Console::WriteLine(string)
    IL_00a7:  ldc.i4.s   100
    IL_00a9:  stloc.1
    IL_00aa:  br.s       IL_00ac

    IL_00ac:  ldloc.1
    IL_00ad:  ret
  } // end of method Test_prefldinit4_simple_il.Driver::Main

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Test_prefldinit4_simple_il.Driver::.ctor

} // end of class Test_prefldinit4_simple_il.Driver


// =============================================================

//*********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file prefldinit4.res
}
