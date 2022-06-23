// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly isinst_call
{
}
.assembly extern xunit.core {}
.namespace JitTest_isinst_call_il
{
  .class private auto ansi beforefieldinit BaseClass
         extends [mscorlib]System.Object
  {
    .method public hidebysig specialname rtspecialname 
            instance void .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } // end of method BaseClass::.ctor

  } // end of class BaseClass

  .class public auto ansi beforefieldinit TestClass
         extends JitTest_isinst_call_il.BaseClass
  {
    .method private hidebysig static class JitTest_isinst_call_il.BaseClass 
            Method_To_Call(int32 cookie) cil managed
    {
      // Code size       52 (0x34)
      .maxstack  1
      .locals (int32 V_0)
      IL_0000:  ldarg.0
      IL_0001:  stloc.0
      IL_0002:  ldloc.0
      IL_0003:  switch     ( 
                            IL_001a,
                            IL_0020,
                            IL_0026,
                            IL_002c)
      IL_0018:  br.s       IL_0032

      IL_001a:  newobj     instance void JitTest_isinst_call_il.TestClass::.ctor()
      IL_001f:  ret

      IL_0020:  newobj     instance void JitTest_isinst_call_il.DerivedClass::.ctor()
      IL_0025:  ret

      IL_0026:  newobj     instance void JitTest_isinst_call_il.BaseClass::.ctor()
      IL_002b:  ret

      IL_002c:  newobj     instance void JitTest_isinst_call_il.OtherClass::.ctor()
      IL_0031:  ret

      IL_0032:  ldnull
      IL_0033:  ret
    } // end of method TestClass::Method_To_Call

    .method private hidebysig static bool 
            Test_CALL(int32 cookie,
                      bool flag) cil managed
    {
      // Code size       51 (0x33)
      .maxstack  2
      .locals (class JitTest_isinst_call_il.TestClass V_0,
               bool V_1)
      .try
      {
        IL_0000:  ldarg.1
        IL_0001:  brfalse.s  IL_0019

        IL_0003:  ldarg.0
        IL_0004:  call       class JitTest_isinst_call_il.BaseClass JitTest_isinst_call_il.TestClass::Method_To_Call(int32)
        IL_0009:  isinst     JitTest_isinst_call_il.TestClass
        IL_000e:  stloc.0
        IL_000f:  ldloc.0
        IL_0010:  ldnull
        IL_0011:  ceq
        IL_0013:  ldc.i4.0
        IL_0014:  ceq
        IL_0016:  stloc.1
        IL_0017:  leave.s    IL_0031

        IL_0019:  ldarg.0
        IL_001a:  call       class JitTest_isinst_call_il.BaseClass JitTest_isinst_call_il.TestClass::Method_To_Call(int32)
        IL_001f:  isinst     JitTest_isinst_call_il.TestClass
        IL_0024:  stloc.0
        IL_0025:  ldloc.0
        IL_0026:  ldnull
        IL_0027:  ceq
        IL_0029:  stloc.1
        IL_002a:  leave.s    IL_0031

      }  // end .try
      catch [mscorlib]System.Exception 
      {
        IL_002c:  pop
        IL_002d:  ldc.i4.0
        IL_002e:  stloc.1
        IL_002f:  leave.s    IL_0031

      }  // end handler
      IL_0031:  ldloc.1
      IL_0032:  ret
    } // end of method TestClass::Test_CALL

    .method public hidebysig static int32
            Main() cil managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      // Code size       123 (0x7b)
      .maxstack  2
      IL_0000:  ldc.i4.0
      IL_0001:  ldc.i4.1
      IL_0002:  call       bool JitTest_isinst_call_il.TestClass::Test_CALL(int32,
                                                             bool)
      IL_0007:  brtrue.s   IL_0016

      IL_0009:  ldstr      "Failed => 101"
      IL_000e:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0013:  ldc.i4.s   101
      IL_0015:  ret

      IL_0016:  ldc.i4.1
      IL_0017:  ldc.i4.1
      IL_0018:  call       bool JitTest_isinst_call_il.TestClass::Test_CALL(int32,
                                                             bool)
      IL_001d:  brtrue.s   IL_002c

      IL_001f:  ldstr      "Failed => 102"
      IL_0024:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0029:  ldc.i4.s   102
      IL_002b:  ret

      IL_002c:  ldc.i4.2
      IL_002d:  ldc.i4.0
      IL_002e:  call       bool JitTest_isinst_call_il.TestClass::Test_CALL(int32,
                                                             bool)
      IL_0033:  brtrue.s   IL_0042

      IL_0035:  ldstr      "Failed => 103"
      IL_003a:  call       void [System.Console]System.Console::WriteLine(string)
      IL_003f:  ldc.i4.s   103
      IL_0041:  ret

      IL_0042:  ldc.i4.3
      IL_0043:  ldc.i4.0
      IL_0044:  call       bool JitTest_isinst_call_il.TestClass::Test_CALL(int32,
                                                             bool)
      IL_0049:  brtrue.s   IL_0058

      IL_004b:  ldstr      "Failed => 104"
      IL_0050:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0055:  ldc.i4.s   104
      IL_0057:  ret

      IL_0058:  ldc.i4.4
      IL_0059:  ldc.i4.0
      IL_005a:  call       bool JitTest_isinst_call_il.TestClass::Test_CALL(int32,
                                                             bool)
      IL_005f:  brtrue.s   IL_006e

      IL_0061:  ldstr      "Failed => 105"
      IL_0066:  call       void [System.Console]System.Console::WriteLine(string)
      IL_006b:  ldc.i4.s   105
      IL_006d:  ret

      IL_006e:  ldstr      "Passed => 100"
      IL_0073:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0078:  ldc.i4.s   100
      IL_007a:  ret
    } // end of method TestClass::Main

    .method public hidebysig specialname rtspecialname 
            instance void .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void JitTest_isinst_call_il.BaseClass::.ctor()
      IL_0006:  ret
    } // end of method TestClass::.ctor

  } // end of class TestClass

  .class private auto ansi beforefieldinit DerivedClass
         extends JitTest_isinst_call_il.TestClass
  {
    .method public hidebysig specialname rtspecialname 
            instance void .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void JitTest_isinst_call_il.TestClass::.ctor()
      IL_0006:  ret
    } // end of method DerivedClass::.ctor

  } // end of class DerivedClass

  .class private auto ansi beforefieldinit OtherClass
         extends [mscorlib]System.Object
  {
    .method public hidebysig specialname rtspecialname 
            instance void .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } // end of method OtherClass::.ctor

  } // end of class OtherClass

} // end of namespace JitTest_isinst_call_il

//*********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file isinst_call.res
