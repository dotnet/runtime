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
// MVID: {0D28B100-FF93-4AA9-8EDA-33F3D5D2A136}
.namespace Test_test1_il
{
  .class private auto ansi Base extends ['mscorlib']System.Object
  {
    .field public int32 m
    .method public hidebysig specialname rtspecialname 
            instance void .ctor() il managed
    {
      // Code size       14 (0xe)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void ['mscorlib']System.Object::.ctor()
      IL_0006:  ldarg.0
      IL_0007:  ldc.i4.1
      IL_0008:  stfld      int32 Test_test1_il.Base::m
      IL_000d:  ret
      IL_000e:  
    } // end of method 'Base::.ctor'

    .method public hidebysig newslot virtual 
            instance class Test_test1_il.Base[]  Clone(int32 numOfCopies) il managed
    {
      // Code size       33 (0x21)
      .maxstack  3
      .locals (class Test_test1_il.Base[] V_0,
               int32 V_1,
               class Test_test1_il.Base[] V_2)
      IL_0000:  ldarg.1
      IL_0001:  newarr     Test_test1_il.Base
      IL_0006:  stloc.0
      IL_0007:  ldc.i4.0
      IL_0008:  stloc.1
      IL_0009:  br.s       IL_0017

      IL_000b:  ldloc.0
      IL_000c:  ldloc.1
      IL_000d:  newobj     instance void Test_test1_il.Base::.ctor()
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

  } // end of class 'Base'

  .class public auto ansi Derived extends Test_test1_il.Base
  {
    .method public hidebysig specialname rtspecialname 
            instance void .ctor() il managed
    {
      // Code size       14 (0xe)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void Test_test1_il.Base::.ctor()
      IL_0006:  ldarg.0
      IL_0007:  ldc.i4.2
      IL_0008:  stfld      int32 Test_test1_il.Base::m
      IL_000d:  ret
      IL_000e:  
    } // end of method 'Derived::.ctor'

    .method public hidebysig virtual instance class Test_test1_il.Base[] 
            Clone(int32 numOfCopies) il managed
    {
      // Code size       33 (0x21)
      .maxstack  3
      .locals (class Test_test1_il.Derived[] V_0,
               int32 V_1,
               class Test_test1_il.Base[] V_2)
      IL_0000:  ldarg.1
      IL_0001:  newarr     Test_test1_il.Derived
      IL_0006:  stloc.0
      IL_0007:  ldc.i4.0
      IL_0008:  stloc.1
      IL_0009:  br.s       IL_0017

      IL_000b:  ldloc.0
      IL_000c:  ldloc.1
      IL_000d:  newobj     instance void Test_test1_il.Derived::.ctor()
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
    } // end of method 'Derived::Clone'

    .method public hidebysig static int32
            Main() il managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      // Code size       99 (0x63)
      .maxstack  2
      .locals (class Test_test1_il.Base V_0,
               class Test_test1_il.Derived V_1,
               int32 V_2)
      IL_0000:  newobj     instance void Test_test1_il.Derived::.ctor()
      IL_0005:  stloc.0
      IL_0006:  ldloc.0
      IL_0007:  ldc.i4.s   11
      IL_0009:  callvirt   instance class Test_test1_il.Base[] Test_test1_il.Base::Clone(int32)
      IL_000e:  ldc.i4.s   10
      IL_0010:  ldelem.ref
      IL_0011:  stloc.0
      IL_0012:  ldloc.0
      IL_0013:  ldfld      int32 Test_test1_il.Base::m
      IL_0018:  ldc.i4.2
      IL_0019:  beq.s      IL_0029

      IL_001b:  ldstr      "FAILED"
      IL_0020:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_0025:  ldc.i4.1
      IL_0026:  stloc.2
      IL_0027:  br.s       IL_0061

      IL_0029:  ldloc.0
      IL_002a:  castclass  Test_test1_il.Derived
      IL_002f:  stloc.1
      IL_0030:  ldloc.1
      IL_0031:  ldc.i4.s   11
      IL_0033:  callvirt   instance class Test_test1_il.Base[] Test_test1_il.Base::Clone(int32)
      IL_0038:  ldc.i4.s   10
      IL_003a:  ldelem.ref
      IL_003b:  stloc.0
      IL_003c:  ldloc.0
      IL_003d:  ldfld      int32 Test_test1_il.Base::m
      IL_0042:  ldc.i4.2
      IL_0043:  beq.s      IL_0053

      IL_0045:  ldstr      "FAILED"
      IL_004a:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_004f:  ldc.i4.1
      IL_0050:  stloc.2
      IL_0051:  br.s       IL_0061

      IL_0053:  ldstr      "PASSED"
      IL_0058:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_005d:  ldc.i4    0x64
      IL_005e:  stloc.2
      IL_005f:  br.s       IL_0061

      IL_0061:  ldloc.2
      IL_0062:  ret
      IL_0063:  
    } // end of method 'Derived::Main'

  } // end of class 'Derived'

} // end of namespace 'Test_test1_il'

//*********** DISASSEMBLY COMPLETE ***********************
