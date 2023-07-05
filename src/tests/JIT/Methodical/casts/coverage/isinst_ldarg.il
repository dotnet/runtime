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
}
.assembly extern xunit.core {}
.namespace JitTest_isinst_ldarg_il
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
         extends JitTest_isinst_ldarg_il.BaseClass
  {
    .method private hidebysig static bool 
            Test_LDARG(object obj,
                       bool flag) cil managed
    {
      // Code size       41 (0x29)
      .maxstack  2
      .locals (class JitTest_isinst_ldarg_il.TestClass V_0,
               bool V_1)
      .try
      {
        IL_0000:  ldarg.1
        IL_0001:  brfalse.s  IL_0014

        IL_0003:  ldarg 0
        IL_0004:  isinst     JitTest_isinst_ldarg_il.TestClass
        IL_0009:  stloc.0
        IL_000a:  ldloc.0
        IL_000b:  ldnull
        IL_000c:  ceq
        IL_000e:  ldc.i4.0
        IL_000f:  ceq
        IL_0011:  stloc.1
        IL_0012:  leave.s    IL_0027

        IL_0014:  ldarg 0
        IL_0015:  isinst     JitTest_isinst_ldarg_il.TestClass
        IL_001a:  stloc.0
        IL_001b:  ldloc.0
        IL_001c:  ldnull
        IL_001d:  ceq
        IL_001f:  stloc.1
        IL_0020:  leave.s    IL_0027

      }  // end .try
      catch [mscorlib]System.Exception 
      {
        IL_0022:  pop
        IL_0023:  ldc.i4.0
        IL_0024:  stloc.1
        IL_0025:  leave.s    IL_0027

      }  // end handler
      IL_0027:  ldloc.1
      IL_0028:  ret
    } // end of method TestClass::Test_LDARG

    .method public hidebysig static int32
            Main() cil managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      // Code size       139 (0x8b)
      .maxstack  2
      IL_0000:  newobj     instance void JitTest_isinst_ldarg_il.TestClass::.ctor()
      IL_0005:  ldc.i4.1
      IL_0006:  call       bool JitTest_isinst_ldarg_il.TestClass::Test_LDARG(object,
                                                              bool)
      IL_000b:  brtrue.s   IL_001a

      IL_000d:  ldstr      "Failed => 101"
      IL_0012:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0017:  ldc.i4.s   101
      IL_0019:  ret

      IL_001a:  newobj     instance void JitTest_isinst_ldarg_il.DerivedClass::.ctor()
      IL_001f:  ldc.i4.1
      IL_0020:  call       bool JitTest_isinst_ldarg_il.TestClass::Test_LDARG(object,
                                                              bool)
      IL_0025:  brtrue.s   IL_0034

      IL_0027:  ldstr      "Failed => 102"
      IL_002c:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0031:  ldc.i4.s   102
      IL_0033:  ret

      IL_0034:  newobj     instance void JitTest_isinst_ldarg_il.BaseClass::.ctor()
      IL_0039:  ldc.i4.0
      IL_003a:  call       bool JitTest_isinst_ldarg_il.TestClass::Test_LDARG(object,
                                                              bool)
      IL_003f:  brtrue.s   IL_004e

      IL_0041:  ldstr      "Failed => 103"
      IL_0046:  call       void [System.Console]System.Console::WriteLine(string)
      IL_004b:  ldc.i4.s   103
      IL_004d:  ret

      IL_004e:  newobj     instance void JitTest_isinst_ldarg_il.OtherClass::.ctor()
      IL_0053:  ldc.i4.0
      IL_0054:  call       bool JitTest_isinst_ldarg_il.TestClass::Test_LDARG(object,
                                                              bool)
      IL_0059:  brtrue.s   IL_0068

      IL_005b:  ldstr      "Failed => 104"
      IL_0060:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0065:  ldc.i4.s   104
      IL_0067:  ret

      IL_0068:  ldnull
      IL_0069:  ldc.i4.0
      IL_006a:  call       bool JitTest_isinst_ldarg_il.TestClass::Test_LDARG(object,
                                                              bool)
      IL_006f:  brtrue.s   IL_007e

      IL_0071:  ldstr      "Failed => 105"
      IL_0076:  call       void [System.Console]System.Console::WriteLine(string)
      IL_007b:  ldc.i4.s   105
      IL_007d:  ret

      IL_007e:  ldstr      "Passed => 100"
      IL_0083:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0088:  ldc.i4.s   100
      IL_008a:  ret
    } // end of method TestClass::Main

    .method public hidebysig specialname rtspecialname 
            instance void .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void JitTest_isinst_ldarg_il.BaseClass::.ctor()
      IL_0006:  ret
    } // end of method TestClass::.ctor

  } // end of class TestClass

  .class private auto ansi beforefieldinit DerivedClass
         extends JitTest_isinst_ldarg_il.TestClass
  {
    .method public hidebysig specialname rtspecialname 
            instance void .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void JitTest_isinst_ldarg_il.TestClass::.ctor()
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

} // end of namespace JitTest_isinst_ldarg_il

//*********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file isinst_ldarg.res
