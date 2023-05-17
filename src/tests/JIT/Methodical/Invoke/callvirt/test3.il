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
// MVID: {D9CB467A-BD94-4D87-BB6B-ABB1D4B12E50}
.namespace Test
{
  .class public auto ansi Base extends ['mscorlib']System.Object
  {
    .field public float64 m
    .method public hidebysig specialname rtspecialname 
            instance void .ctor() il managed
    {
      // Code size       22 (0x16)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void ['mscorlib']System.Object::.ctor()
      IL_0006:  ldarg.0
      IL_0007:  ldc.r8     1.
      IL_0010:  stfld      float64 Test.Base::m
      IL_0015:  ret
      IL_0016:  
    } // end of method 'Base::.ctor'

    .method public hidebysig instance class Test.Base[] 
            Clone(int32 numOfCopies) il managed
    {
      // Code size       33 (0x21)
      .maxstack  3
      .locals (class Test.Base[] V_0,
               int32 V_1,
               class Test.Base[] V_2)
      IL_0000:  ldarg.1
      IL_0001:  newarr     Test.Base
      IL_0006:  stloc.0
      IL_0007:  ldc.i4.0
      IL_0008:  stloc.1
      IL_0009:  br.s       IL_0017

      IL_000b:  ldloc.0
      IL_000c:  ldloc.1
      IL_000d:  newobj     instance void Test.Base::.ctor()
      IL_0012:  stelem.ref
      IL_0013:  ldloc.1
      IL_0014:  ldc.i4.1
      IL_0015:  add
      IL_0016:  stloc.1
      IL_0017:  ldloc.1
      IL_0018:  ldarg.1
      IL_0019:  blt.s      IL_000b

      IL_001b:  ldloc.0
      IL_001c:  stloc.2
      IL_001d:  br.s       IL_001f

      IL_001f:  ldloc.2
      IL_0020:  ret
      IL_0021:  
    } // end of method 'Base::Clone'

    .method public hidebysig static int32
            Main() il managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      // Code size       65 (0x41)
      .maxstack  2
      .locals (class Test.Base V_0,
               int32 V_1)
      IL_0000:  newobj     instance void Test.Base::.ctor()
      IL_0005:  stloc.0
      IL_0006:  ldloc.0
      IL_0007:  ldc.i4.s   11
      IL_0009:  callvirt       instance class Test.Base[] Test.Base::Clone(int32)
      IL_000e:  ldc.i4.s   10
      IL_0010:  ldelem.ref
      IL_0011:  stloc.0
      IL_0012:  ldloc.0
      IL_0013:  ldfld      float64 Test.Base::m
      IL_0018:  ldc.r8     1.
      IL_0021:  beq.s      IL_0031

      IL_0023:  ldstr      "FAILED"
      IL_0028:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_002d:  ldc.i4.1
      IL_002e:  stloc.1
      IL_002f:  br.s       IL_003f

      IL_0031:  ldstr      "PASSED"
      IL_0036:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_003b:  ldc.i4    0x64
      IL_003c:  stloc.1
      IL_003d:  br.s       IL_003f

      IL_003f:  ldloc.1
      IL_0040:  ret
      IL_0041:  
    } // end of method 'Base::Main'

  } // end of class 'Base'

} // end of namespace 'Test'

//*********** DISASSEMBLY COMPLETE ***********************
