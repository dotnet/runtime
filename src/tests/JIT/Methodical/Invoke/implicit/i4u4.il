// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.





.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ASSEMBLY_NAME
{

  //  .custom instance void ['mscorlib']System.Diagnostics.DebuggableAttribute::.ctor(bool,
  //                                                                                  bool) = ( 01 00 00 01 00 00 ) 
  
  
}
.assembly extern xunit.core {}
// MVID: {B4ADAE94-5F38-493D-A41E-0ED51F697371}
.namespace TestCase_i4u4_il
{
  .class public auto ansi Test
         extends ['mscorlib']System.Object
  {
    .method private hidebysig static int32 
            compare(unsigned int32 arg1,
                    unsigned int32 arg2) il managed
    {
      // Code size       22 (0x16)
      .maxstack  2
      .locals (int32 V_0)
      IL_0000:  ldarg.0
      IL_0001:  ldarg.1
      IL_0002:  bge.un.s   IL_0008

      IL_0004:  ldc.i4.m1
      IL_0005:  stloc.0
      IL_0006:  br.s       IL_0014

      IL_0008:  ldarg.0
      IL_0009:  ldarg.1
      IL_000a:  ble.un.s   IL_0010

      IL_000c:  ldc.i4.1
      IL_000d:  stloc.0
      IL_000e:  br.s       IL_0014

      IL_0010:  ldc.i4.0
      IL_0011:  stloc.0
      IL_0012:  br.s       IL_0014

      IL_0014:  ldloc.0
      IL_0015:  ret
    } // end of method 'Test::compare'

    .method public hidebysig static int32
            Main() il managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      // Code size       40 (0x28)
      .maxstack  2
      .locals (int32 V_0)
      IL_0000:  ldc.i4.1
      IL_0001:  ldc.i4.m1
      IL_0002:  call       int32 TestCase_i4u4_il.Test::compare(unsigned int32,
                                                        unsigned int32)
      IL_0007:  ldc.i4.0
      IL_0008:  blt.s      IL_0018

      IL_000a:  ldstr      "FAILED"
      IL_000f:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_0014:  ldc.i4.1
      IL_0015:  stloc.0
      IL_0016:  br.s       IL_0026

      IL_0018:  ldstr      "PASSED"
      IL_001d:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_0022:  ldc.i4    0x64
      IL_0023:  stloc.0
      IL_0024:  br.s       IL_0026

      IL_0026:  ldloc.0
      IL_0027:  ret
    } // end of method 'Test::Main'

    .method public hidebysig specialname rtspecialname 
            instance void .ctor() il managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void ['mscorlib']System.Object::.ctor()
      IL_0006:  ret
    } // end of method 'Test::.ctor'

  } // end of class 'Test'

} // end of namespace 'TestCase_i4u4_il'

//*********** DISASSEMBLY COMPLETE ***********************
