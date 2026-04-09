// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}





// Metadata version: v2.0.50516
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 2:0:0:0
}
.assembly ASSEMBLY_NAME
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.assembly extern xunit.core {}
// MVID: {AF04A73C-C896-4A00-97F2-959EE06A9394}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x02EC0000

.namespace Test_valuetype {
// =============== CLASS MEMBERS DECLARATION ===================

.class public sequential ansi sealed beforefieldinit Dummy
       extends [mscorlib]System.ValueType
{
  .pack 0
  .size 1
  .method public hidebysig newslot virtual 
	instance string Virtual() cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "Dummy.Virtual"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method Dummy::Virtual

} // end of class Test.Dummy

.class public auto ansi sealed TestMethod
       extends [mscorlib]System.MulticastDelegate
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(object 'object',
                               native int 'method') runtime managed
  {
  } // end of method TestMethod::.ctor

  .method public hidebysig newslot virtual 
          instance string  Invoke() runtime managed
  {
  } // end of method TestMethod::Invoke

  .method public hidebysig newslot virtual 
          instance class [mscorlib]System.IAsyncResult 
          BeginInvoke(class [mscorlib]System.AsyncCallback callback,
                      object 'object') runtime managed
  {
  } // end of method TestMethod::BeginInvoke

  .method public hidebysig newslot virtual 
          instance string  EndInvoke(class [mscorlib]System.IAsyncResult result) runtime managed
  {
  } // end of method TestMethod::EndInvoke

} // end of class Test.TestMethod

.class public abstract auto ansi sealed beforefieldinit Program
       extends [mscorlib]System.Object
{
  .method public hidebysig static void  CallDummy() cil managed
  {
    // Code size       56 (0x38)
    .maxstack  4
    .locals init (valuetype Test_valuetype.Dummy V_0)
    IL_0000:  nop
    IL_0001:  ldloca.s   V_0
    IL_0003:  initobj    Test_valuetype.Dummy
    IL_0009:  ldstr      "Dummy.Virtual"
    IL_000e:  ldloca.s   V_0
    IL_0010:  call       instance string Test_valuetype.Dummy::Virtual()
    IL_0015:  call       void Test_valuetype.Assert::AreEqual(string,
                                                    string)
    IL_001a:  nop
    IL_001b:  ldstr      "Dummy.Virtual"
    IL_0020:  ldloc.0
    IL_0021:  box        Test_valuetype.Dummy
    IL_0026:  ldftn      instance string Test_valuetype.Dummy::Virtual()
    IL_002c:  newobj     instance void Test_valuetype.TestMethod::.ctor(object,
                                                              native int)
    IL_0031:  call       void Test_valuetype.Assert::AreEqual(string,
                                                    class Test_valuetype.TestMethod)
    IL_0036:  nop
    IL_0037:  ret
  } // end of method Program::CallDummy

  .method public hidebysig static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       52 (0x34)
    .maxstack  1
    .locals init (class [mscorlib]System.Exception V_0,
             int32 V_1)
    IL_0000:  nop
    .try
    {
      IL_0001:  nop
      IL_0002:  call       void Test_valuetype.Program::CallDummy()
      IL_0007:  nop
      IL_0008:  ldstr      "Test SUCCESS"
      IL_000d:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0012:  nop
      IL_0013:  ldc.i4.s   100
      IL_0015:  stloc.1
      IL_0016:  leave.s    IL_0031

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      IL_0018:  stloc.0
      IL_0019:  nop
      IL_001a:  ldloc.0
      IL_001b:  call       void [System.Console]System.Console::WriteLine(object)
      IL_0020:  nop
      IL_0021:  ldstr      "Test FAILED"
      IL_0026:  call       void [System.Console]System.Console::WriteLine(string)
      IL_002b:  nop
      IL_002c:  ldc.i4.s   101
      IL_002e:  stloc.1
      IL_002f:  leave.s    IL_0031

    }  // end handler
    IL_0031:  nop
    IL_0032:  ldloc.1
    IL_0033:  ret
  } // end of method Program::Main

} // end of class Test.Program

.class public abstract auto ansi sealed beforefieldinit Assert
       extends [mscorlib]System.Object
{
  .method public hidebysig static void  AreEqual(string left,
                                                 class Test_valuetype.TestMethod right) cil managed
  {
    // Code size       15 (0xf)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldarg.1
    IL_0003:  callvirt   instance string Test_valuetype.TestMethod::Invoke()
    IL_0008:  call       void Test_valuetype.Assert::AreEqual(string,
                                                    string)
    IL_000d:  nop
    IL_000e:  ret
  } // end of method Assert::AreEqual

  .method public hidebysig static void  AreEqual(string left,
                                                 string right) cil managed
  {
    // Code size       85 (0x55)
    .maxstack  3
    .locals init (string V_0,
             bool V_1)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  call       bool [mscorlib]System.String::IsNullOrEmpty(string)
    IL_0007:  ldc.i4.0
    IL_0008:  ceq
    IL_000a:  stloc.1
    IL_000b:  ldloc.1
    IL_000c:  brtrue.s   IL_0019

    IL_000e:  ldstr      "left"
    IL_0013:  newobj     instance void [mscorlib]System.ArgumentNullException::.ctor(string)
    IL_0018:  throw

    IL_0019:  ldarg.1
    IL_001a:  call       bool [mscorlib]System.String::IsNullOrEmpty(string)
    IL_001f:  ldc.i4.0
    IL_0020:  ceq
    IL_0022:  stloc.1
    IL_0023:  ldloc.1
    IL_0024:  brtrue.s   IL_0031

    IL_0026:  ldstr      "right"
    IL_002b:  newobj     instance void [mscorlib]System.ArgumentNullException::.ctor(string)
    IL_0030:  throw

    IL_0031:  ldarg.0
    IL_0032:  ldarg.1
    IL_0033:  call       bool [mscorlib]System.String::op_Inequality(string,
                                                                     string)
    IL_0038:  ldc.i4.0
    IL_0039:  ceq
    IL_003b:  stloc.1
    IL_003c:  ldloc.1
    IL_003d:  brtrue.s   IL_0054

    IL_003f:  nop
    IL_0040:  ldstr      "[[{0}]] != [[{1}]]"
    IL_0045:  ldarg.0
    IL_0046:  ldarg.1
    IL_0047:  call       string [mscorlib]System.String::Format(string,
                                                                object,
                                                                object)
    IL_004c:  stloc.0
    IL_004d:  ldloc.0
    IL_004e:  newobj     instance void [mscorlib]System.Exception::.ctor(string)
    IL_0053:  throw

    IL_0054:  ret
  } // end of method Assert::AreEqual

} // end of class Test.Assert
}
