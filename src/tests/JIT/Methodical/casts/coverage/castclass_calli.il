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
.namespace JitTest_castclass_calli_il
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
         extends JitTest_castclass_calli_il.BaseClass
  {
    .method private hidebysig instance class JitTest_castclass_calli_il.BaseClass 
            Method_To_Call(int32 cookie) cil managed
    {
      // Code size       52 (0x34)
      .maxstack  1
      .locals (int32 V_0)
      IL_0000:  ldarg.1
      IL_0001:  stloc.0
      IL_0002:  ldloc.0
      IL_0003:  switch     ( 
                            IL_001a,
                            IL_0020,
                            IL_0026,
                            IL_002c)
      IL_0018:  br.s       IL_0032

      IL_001a:  newobj     instance void JitTest_castclass_calli_il.TestClass::.ctor()
      IL_001f:  ret

      IL_0020:  newobj     instance void JitTest_castclass_calli_il.DerivedClass::.ctor()
      IL_0025:  ret

      IL_0026:  newobj     instance void JitTest_castclass_calli_il.BaseClass::.ctor()
      IL_002b:  ret

      IL_002c:  newobj     instance void JitTest_castclass_calli_il.OtherClass::.ctor()
      IL_0031:  ret

      IL_0032:  ldnull
      IL_0033:  ret
    } // end of method TestClass::Method_To_Call

    .method private hidebysig static bool 
            Test_CALL(class JitTest_castclass_calli_il.TestClass _this,
                      int32 cookie,
                      bool flag) cil managed
    {
      // Code size       73 (0x49)
      .maxstack  4
      .locals (class JitTest_castclass_calli_il.TestClass V_0,
               class [mscorlib]System.Exception V_1,
               bool V_2,
               method instance class JitTest_castclass_calli_il.BaseClass*(int32)
               )
      ldarg.0
      ldvirtftn instance class JitTest_castclass_calli_il.BaseClass JitTest_castclass_calli_il.TestClass::Method_To_Call(int32)
      stloc.3
      .try
      {
        IL_0000:  ldarg.2
        IL_0001:  brfalse.s  IL_001a

        IL_0003:  ldarg.0
        IL_0004:  ldarg.1
        IL_0005:  ldloc.3   calli instance class JitTest_castclass_calli_il.BaseClass(int32)
        IL_000a:  castclass  JitTest_castclass_calli_il.TestClass
        IL_000f:  stloc.0
        IL_0010:  ldloc.0
        IL_0011:  ldnull
        IL_0012:  ceq
        IL_0014:  ldc.i4.0
        IL_0015:  ceq
        IL_0017:  stloc.2
        IL_0018:  leave.s    IL_0047

        IL_001a:  ldarg.0
        IL_001b:  ldarg.1
        IL_001c:  ldloc.3   calli instance class JitTest_castclass_calli_il.BaseClass(int32)
        IL_0021:  castclass  JitTest_castclass_calli_il.TestClass
        IL_0026:  stloc.0
        IL_0027:  ldarg.0
        IL_0028:  ldarg.1
        IL_0029:  ldloc.3   calli instance class JitTest_castclass_calli_il.BaseClass(int32)
        IL_002e:  ldnull
        IL_002f:  ceq
        IL_0031:  stloc.2
        IL_0032:  leave.s    IL_0047

      }  // end .try
      catch [mscorlib]System.Exception 
      {
        IL_0034:  stloc.1
        IL_0035:  ldarg.2
        IL_0036:  brtrue.s   IL_0043

        IL_0038:  ldloc.1
        IL_0039:  isinst     [mscorlib]System.InvalidCastException
        IL_003e:  ldnull
        IL_003f:  cgt.un
        IL_0041:  br.s       IL_0044

        IL_0043:  ldc.i4.0
        IL_0044:  stloc.2
        IL_0045:  leave.s    IL_0047

      }  // end handler
      IL_0047:  ldloc.2
      IL_0048:  ret
    } // end of method TestClass::Test_CALL

    .method public hidebysig static int32
            Main() cil managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      // Code size       134 (0x86)
      .maxstack  3
      .locals (class JitTest_castclass_calli_il.TestClass V_0)
      IL_0000:  newobj     instance void JitTest_castclass_calli_il.TestClass::.ctor()
      IL_0005:  stloc.0
      IL_0006:  ldloc.0
      IL_0007:  ldc.i4.0
      IL_0008:  ldc.i4.1
      IL_0009:  call       bool JitTest_castclass_calli_il.TestClass::Test_CALL(class JitTest_castclass_calli_il.TestClass,
                                                             int32,
                                                             bool)
      IL_000e:  brtrue.s   IL_001d

      IL_0010:  ldstr      "Failed => 101"
      IL_0015:  call       void [System.Console]System.Console::WriteLine(string)
      IL_001a:  ldc.i4.s   101
      IL_001c:  ret

      IL_001d:  ldloc.0
      IL_001e:  ldc.i4.1
      IL_001f:  ldc.i4.1
      IL_0020:  call       bool JitTest_castclass_calli_il.TestClass::Test_CALL(class JitTest_castclass_calli_il.TestClass,
                                                             int32,
                                                             bool)
      IL_0025:  brtrue.s   IL_0034

      IL_0027:  ldstr      "Failed => 102"
      IL_002c:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0031:  ldc.i4.s   102
      IL_0033:  ret

      IL_0034:  ldloc.0
      IL_0035:  ldc.i4.2
      IL_0036:  ldc.i4.0
      IL_0037:  call       bool JitTest_castclass_calli_il.TestClass::Test_CALL(class JitTest_castclass_calli_il.TestClass,
                                                             int32,
                                                             bool)
      IL_003c:  brtrue.s   IL_004b

      IL_003e:  ldstr      "Failed => 103"
      IL_0043:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0048:  ldc.i4.s   103
      IL_004a:  ret

      IL_004b:  ldloc.0
      IL_004c:  ldc.i4.3
      IL_004d:  ldc.i4.0
      IL_004e:  call       bool JitTest_castclass_calli_il.TestClass::Test_CALL(class JitTest_castclass_calli_il.TestClass,
                                                             int32,
                                                             bool)
      IL_0053:  brtrue.s   IL_0062

      IL_0055:  ldstr      "Failed => 104"
      IL_005a:  call       void [System.Console]System.Console::WriteLine(string)
      IL_005f:  ldc.i4.s   104
      IL_0061:  ret

      IL_0062:  ldloc.0
      IL_0063:  ldc.i4.4
      IL_0064:  ldc.i4.0
      IL_0065:  call       bool JitTest_castclass_calli_il.TestClass::Test_CALL(class JitTest_castclass_calli_il.TestClass,
                                                             int32,
                                                             bool)
      IL_006a:  brtrue.s   IL_0079

      IL_006c:  ldstr      "Failed => 105"
      IL_0071:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0076:  ldc.i4.s   105
      IL_0078:  ret

      IL_0079:  ldstr      "Passed => 100"
      IL_007e:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0083:  ldc.i4.s   100
      IL_0085:  ret
    } // end of method TestClass::Main

    .method public hidebysig specialname rtspecialname 
            instance void .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void JitTest_castclass_calli_il.BaseClass::.ctor()
      IL_0006:  ret
    } // end of method TestClass::.ctor

  } // end of class TestClass

  .class private auto ansi beforefieldinit DerivedClass
         extends JitTest_castclass_calli_il.TestClass
  {
    .method public hidebysig specialname rtspecialname 
            instance void .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void JitTest_castclass_calli_il.TestClass::.ctor()
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

} // end of namespace JitTest_castclass_calli_il

//*********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file castclass_call.res
