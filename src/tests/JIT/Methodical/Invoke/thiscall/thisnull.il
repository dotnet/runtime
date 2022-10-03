// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly thisnull
{
}
.assembly extern xunit.core {}
.namespace JitTest_thisnull_thiscall
{
  .class public auto ansi beforefieldinit App
         extends [mscorlib]System.Object
  {
    .field private float64 m_field
    .method private hidebysig instance float64 
            TestMethod(bool accessField) cil managed
    {
      // Code size       26 (0x1a)
      .maxstack  1
      .locals (float64 V_0)
      IL_0000:  ldarg.1
      IL_0001:  brfalse.s  IL_000c

      IL_0003:  ldarg.0
      IL_0004:  ldfld      float64 JitTest_thisnull_thiscall.App::m_field
      IL_0009:  stloc.0
      IL_000a:  br.s       IL_0018

      IL_000c:  ldc.r8     3.1415926000000001
      IL_0015:  stloc.0
      IL_0016:  br.s       IL_0018

      IL_0018:  ldloc.0
      IL_0019:  ret
    } // end of method App::TestMethod

    .method family hidebysig newslot virtual 
            instance float64  VirtTestMethod(bool accessField) cil managed
    {
      // Code size       26 (0x1a)
      .maxstack  1
      .locals (float64 V_0)
      IL_0000:  ldarg.1
      IL_0001:  brfalse.s  IL_000c

      IL_0003:  ldarg.0
      IL_0004:  ldfld      float64 JitTest_thisnull_thiscall.App::m_field
      IL_0009:  stloc.0
      IL_000a:  br.s       IL_0018

      IL_000c:  ldc.r8     3.1415926000000001
      IL_0015:  stloc.0
      IL_0016:  br.s       IL_0018

      IL_0018:  ldloc.0
      IL_0019:  ret
    } // end of method App::VirtTestMethod

    .method public hidebysig static int32
            Main() cil managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      // Code size       95 (0x5f)
      .maxstack  2
      .locals (class JitTest_thisnull_thiscall.App V_0,
               int32 V_1)
      IL_0000:  ldnull
      IL_0001:  stloc.0
      IL_0002:  ldloc.0
      IL_0003:  ldc.i4.0
      IL_0004:  call   instance float64 JitTest_thisnull_thiscall.App::TestMethod(bool)
      IL_0009:  ldc.r8     3.1415926000000001
      IL_0012:  beq.s      IL_0022

      IL_0014:  ldstr      "Failed"
      IL_0019:  call       void [System.Console]System.Console::WriteLine(string)
      IL_001e:  ldc.i4.1
      IL_001f:  stloc.1
      IL_0020:  br.s       IL_005d

      .try
      {
        IL_0022:  ldloc.0
        IL_0023:  ldc.i4.1
        IL_0024:  call   instance float64 JitTest_thisnull_thiscall.App::TestMethod(bool)
        IL_0029:  pop
        IL_002a:  ldc.i4.2
        IL_002b:  stloc.1
        IL_002c:  leave.s    IL_005d

      }  // end .try
      catch [mscorlib]System.NullReferenceException 
      {
        IL_002e:  pop
        IL_002f:  leave.s    IL_0031

      }  // end handler
      .try
      {
        IL_0031:  ldloc.0
        IL_0032:  ldc.i4.0
        IL_0033:  callvirt   instance float64 JitTest_thisnull_thiscall.App::VirtTestMethod(bool)
        IL_0038:  pop
        IL_0039:  ldc.i4.2
        IL_003a:  stloc.1
        IL_003b:  leave.s    IL_005d

      }  // end .try
      catch [mscorlib]System.NullReferenceException 
      {
        IL_003d:  pop
        IL_003e:  leave.s    IL_0040

      }  // end handler
      .try
      {
        IL_0040:  ldloc.0
        IL_0041:  ldc.i4.1
        IL_0042:  callvirt   instance float64 JitTest_thisnull_thiscall.App::VirtTestMethod(bool)
        IL_0047:  pop
        IL_0048:  ldc.i4.2
        IL_0049:  stloc.1
        IL_004a:  leave.s    IL_005d

      }  // end .try
      catch [mscorlib]System.NullReferenceException 
      {
        IL_004c:  pop
        IL_004d:  leave.s    IL_004f

      }  // end handler
      IL_004f:  ldstr      "Passed"
      IL_0054:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0059:  ldc.i4    0x64
      IL_005a:  stloc.1
      IL_005b:  br.s       IL_005d

      IL_005d:  ldloc.1
      IL_005e:  ret
    } // end of method App::Main

    .method public hidebysig specialname rtspecialname 
            instance void .ctor() cil managed
    {
      // Code size       22 (0x16)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  ldc.r8     1.
      IL_000a:  stfld      float64 JitTest_thisnull_thiscall.App::m_field
      IL_000f:  ldarg.0
      IL_0010:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0015:  ret
    } // end of method App::.ctor

  } // end of class App

} // end of namespace JitTest_thisnull_thiscall

//*********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file thisnull.res
