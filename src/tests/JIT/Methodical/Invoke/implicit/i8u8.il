// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.





.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'i8u8'
{

  //  .custom instance void ['mscorlib']System.Diagnostics.DebuggableAttribute::.ctor(bool,
  //                                                                                  bool) = ( 01 00 00 01 00 00 ) 
  
  
}
.assembly extern xunit.core {}
// MVID: {FBE787B7-6007-478A-AA78-9465C2020C0C}
.namespace TestCase_i8u8_il
{
  .class public auto ansi Test
         extends ['mscorlib']System.Object
  {
    .method private hidebysig static int32 
            compare(unsigned int64 arg1,
                    unsigned int64 arg2) il managed
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
      // Code size       42 (0x2a)
      .maxstack  2
      .locals (int32 V_0)
      IL_0000:  ldc.i8 1
      IL_0002:  ldc.i8 -1
      IL_0004:  call       int32 TestCase_i8u8_il.Test::compare(unsigned int64,
                                                        unsigned int64)
      IL_0009:  ldc.i4.0
      IL_000a:  blt.s      IL_001a

      IL_000c:  ldstr      "FAILED"
      IL_0011:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_0016:  ldc.i4.1
      IL_0017:  stloc.0
      IL_0018:  br.s       IL_0028

      IL_001a:  ldstr      "PASSED"
      IL_001f:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_0024:  ldc.i4   0x64
      IL_0025:  stloc.0
      IL_0026:  br.s       IL_0028

      IL_0028:  ldloc.0
      IL_0029:  ret
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

} // end of namespace 'TestCase_i8u8_il'

//*********** DISASSEMBLY COMPLETE ***********************
