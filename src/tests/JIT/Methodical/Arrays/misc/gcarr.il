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
.assembly 'gcarr'// as "gcarr"
{
}
.assembly extern xunit.core {}
.namespace GCTest_gcarr_il
{
  .class public auto ansi Test
         extends [mscorlib]System.Object
  {
    .field private int32 m_magic
    .method public hidebysig newslot virtual instance void CheckValid() il managed
    {
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  ldfld      int32 GCTest_gcarr_il.Test::m_magic
      IL_0006:  ldc.i4     0x12345678
      IL_000b:  beq.s      IL_0013

      IL_000d:  newobj     instance void [mscorlib]System.Exception::.ctor()
      IL_0012:  throw

      IL_0013:  ret
    } // end of method 'Test::CheckValid'

    .method public hidebysig static int32
            Main() il managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      .maxstack  3
      .locals (class GCTest_gcarr_il.Test[-10...10] V_0,
               int32 V_1,
               int32 V_2,
               int32 V_3)
      			ldc.i4 -10
      			ldc.i4 21
      			newobj      instance void class GCTest_gcarr_il.Test[-10...10]::.ctor(int32, int32)
      IL_0007:  stloc.0
      IL_0008:  ldc.i4	-10
      IL_0009:  stloc.1
      IL_000a:  br.s       IL_0018

      IL_000c:  ldloc.0
      IL_000d:  ldloc.1
      			dup
      			call		void [System.Console]System.Console::WriteLine(int32)
      IL_000e:  newobj     instance void GCTest_gcarr_il.Test::.ctor()
      			callvirt   instance void class GCTest_gcarr_il.Test[-10...10]::Set(int32, class GCTest_gcarr_il.Test)
      IL_0014:  ldloc.1
      IL_0015:  ldc.i4.1
      IL_0016:  add
      IL_0017:  stloc.1
      IL_0018:  ldloc.1
      IL_0019:  ldc.i4.s   11
      IL_001b:  blt.s      IL_000c

      IL_001d:  call       void [mscorlib]System.GC::Collect()
      IL_0022:  call       void [mscorlib]System.GC::WaitForPendingFinalizers()
      IL_0027:  call       void [mscorlib]System.GC::Collect()
      IL_002c:  ldc.i4   -10
      IL_002d:  stloc.2
      IL_002e:  br.s       IL_003c

      IL_0030:  ldloc.0
      IL_0031:  ldloc.2
      			callvirt   instance class GCTest_gcarr_il.Test class GCTest_gcarr_il.Test[-10...10]::Get(int32)
      			
      IL_0033:  callvirt   instance void GCTest_gcarr_il.Test::CheckValid()
      IL_0038:  ldloc.2
      IL_0039:  ldc.i4.1
      IL_003a:  add
      IL_003b:  stloc.2
      IL_003c:  ldloc.2
      IL_003d:  ldc.i4.s   11
      IL_003f:  blt.s      IL_0030

      IL_0041:  ldnull
      IL_0042:  stloc.0
      IL_0043:  call       void [mscorlib]System.GC::Collect()
      IL_0048:  call       void [mscorlib]System.GC::WaitForPendingFinalizers()
      IL_004d:  call       void [mscorlib]System.GC::Collect()
      IL_0052:  ldstr      "Test passed."
      IL_0057:  call       void [System.Console]System.Console::WriteLine(string)
      IL_005c:  ldc.i4    0x64
      IL_005d:  stloc.3
      IL_005e:  br.s       IL_0060

      IL_0060:  ldloc.3
      IL_0061:  ret
    } // end of method 'Test::Main'

    .method public hidebysig specialname rtspecialname 
            instance void .ctor() il managed
    {
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  ldc.i4     0x12345678
      IL_0006:  stfld      int32 GCTest_gcarr_il.Test::m_magic
      IL_000b:  ldarg.0
      IL_000c:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0011:  ret
    } // end of method 'Test::.ctor'

  } // end of class 'Test'

} // end of namespace 'GCTest_gcarr_il'

