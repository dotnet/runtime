// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { }
.assembly extern xunit.core {}
// A .cctor has only one chance to run in any appdomain. 
// If it fails, the 2nd time we try to access a static field we check if .cctor has been run. 
// And it has, but failed so we fail again.

// Test throws an exception inside .cctor.
// Try to access a static method using LDFTN op code
// Expected: Should return the same exception.



// Metadata version: v2.0.50117
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 2:0:0:0
}
.assembly CctorThrowLDFTNStaticMethod
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
// MVID: {DFA6FFCF-599E-4DF2-8538-B842C6A0AD10}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x03090000


// =============== CLASS MEMBERS DECLARATION ===================

.class public auto ansi A
       extends [mscorlib]System.Object
{
  .method private hidebysig specialname rtspecialname static 
          void  .cctor() cil managed
  {
    // Code size       18 (0x12)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "Inside A.cctor"
    IL_0006:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  newobj     instance void [mscorlib]System.Exception::.ctor()
    IL_0011:  throw
  } // end of method A::.cctor

  .method public hidebysig static void  StaticMethod() cil managed
  {
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ret
  } // end of method A::StaticMethod

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method A::.ctor

} // end of class A

.class public sequential ansi sealed B
       extends [mscorlib]System.ValueType
{
  .pack 0
  .size 1
  .method private hidebysig specialname rtspecialname static 
          void  .cctor() cil managed
  {
    // Code size       18 (0x12)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "Inside B.cctor"
    IL_0006:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  newobj     instance void [mscorlib]System.Exception::.ctor()
    IL_0011:  throw
  } // end of method B::.cctor

  .method public hidebysig static void  StaticMethod() cil managed
  {
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ret
  } // end of method B::StaticMethod

} // end of class B

.class public auto ansi beforefieldinit Test_CctorThrowLDFTNStaticMethod
       extends [mscorlib]System.Object
{
  .method public hidebysig static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       279 (0x117)
    .maxstack  2
    .locals init (bool V_0,
             class [mscorlib]System.Exception V_1,
             int32 V_2,
             bool V_3)
    IL_0000:  nop
    IL_0001:  ldc.i4.1
    IL_0002:  stloc.0
    .try
    {
      IL_0003:  nop
      IL_0004:  ldstr      "LDFTN static method of a class"
      IL_0009:  call       void [System.Console]System.Console::WriteLine(string)
      IL_000e:  nop
	

      // LDFTN static method of a class
       ldftn     void A::StaticMethod() 
       calli void ()


      IL_000f:  ldstr      "Did not catch expected TypeInitializationException"
      + " exception"
      IL_0014:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0019:  nop
      IL_001a:  ldc.i4.0
      IL_001b:  stloc.0
      IL_001c:  nop
      IL_001d:  leave.s    IL_0047

    }  // end .try
    catch [mscorlib]System.TypeInitializationException 
    {
      IL_001f:  pop
      IL_0020:  nop
      IL_0021:  ldstr      "Caught expected exception 1st time"
      IL_0026:  call       void [System.Console]System.Console::WriteLine(string)
      IL_002b:  nop
      IL_002c:  nop
      IL_002d:  leave.s    IL_0047

    }  // end handler
    catch [mscorlib]System.Exception 
    {
      IL_002f:  stloc.1
      IL_0030:  nop
      IL_0031:  ldstr      "Caught unexpected exception 1st time: "
      IL_0036:  ldloc.1
      IL_0037:  call       string [mscorlib]System.String::Concat(object,
                                                                  object)
      IL_003c:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0041:  nop
      IL_0042:  ldc.i4.0
      IL_0043:  stloc.0
      IL_0044:  nop
      IL_0045:  leave.s    IL_0047

    }  // end handler
    IL_0047:  nop
    .try
    {
      IL_0048:  nop

    // LDFTN static method of a class
       ldftn     void A::StaticMethod() 
       calli void ()

      IL_0049:  ldstr      "Did not catch expected TypeInitializationException"
      + " exception\n"
      IL_004e:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0053:  nop
      IL_0054:  ldc.i4.0
      IL_0055:  stloc.0
      IL_0056:  nop
      IL_0057:  leave.s    IL_0081

    }  // end .try
    catch [mscorlib]System.TypeInitializationException 
    {
      IL_0059:  pop
      IL_005a:  nop
      IL_005b:  ldstr      "Caught expected exception 2nd time\n"
      IL_0060:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0065:  nop
      IL_0066:  nop
      IL_0067:  leave.s    IL_0081

    }  // end handler
    catch [mscorlib]System.Exception 
    {
      IL_0069:  stloc.1
      IL_006a:  nop
      IL_006b:  ldstr      "Caught unexpected exception 2nd time: "
      IL_0070:  ldloc.1
      IL_0071:  call       string [mscorlib]System.String::Concat(object,
                                                                  object)
      IL_0076:  call       void [System.Console]System.Console::WriteLine(string)
      IL_007b:  nop
      IL_007c:  ldc.i4.0
      IL_007d:  stloc.0
      IL_007e:  nop
      IL_007f:  leave.s    IL_0081

    }  // end handler
    IL_0081:  nop
    .try
    {
      IL_0082:  nop
      IL_0083:  ldstr      "LDFTN static method of a struct"
      IL_0088:  call       void [System.Console]System.Console::WriteLine(string)
      IL_008d:  nop

      // LDFTN static method of a struct
     ldftn     void B::StaticMethod() 
     calli void ()

      IL_008e:  ldstr      "Did not catch expected TypeInitializationException"
      + " exception"
      IL_0093:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0098:  nop
      IL_0099:  ldc.i4.0
      IL_009a:  stloc.0
      IL_009b:  nop
      IL_009c:  leave.s    IL_00c6

    }  // end .try
    catch [mscorlib]System.TypeInitializationException 
    {
      IL_009e:  pop
      IL_009f:  nop
      IL_00a0:  ldstr      "Caught expected exception 1st time"
      IL_00a5:  call       void [System.Console]System.Console::WriteLine(string)
      IL_00aa:  nop
      IL_00ab:  nop
      IL_00ac:  leave.s    IL_00c6

    }  // end handler
    catch [mscorlib]System.Exception 
    {
      IL_00ae:  stloc.1
      IL_00af:  nop
      IL_00b0:  ldstr      "Caught unexpected exception 1st time: "
      IL_00b5:  ldloc.1
      IL_00b6:  call       string [mscorlib]System.String::Concat(object,
                                                                  object)
      IL_00bb:  call       void [System.Console]System.Console::WriteLine(string)
      IL_00c0:  nop
      IL_00c1:  ldc.i4.0
      IL_00c2:  stloc.0
      IL_00c3:  nop
      IL_00c4:  leave.s    IL_00c6

    }  // end handler
    IL_00c6:  nop
    .try
    {
      IL_00c7:  nop

     // LDFTN static method of a struct
     ldftn     void B::StaticMethod() 
     calli void ()

      IL_00c8:  ldstr      "Did not catch expected TypeInitializationException"
      + " exception\n"
      IL_00cd:  call       void [System.Console]System.Console::WriteLine(string)
      IL_00d2:  nop
      IL_00d3:  ldc.i4.0
      IL_00d4:  stloc.0
      IL_00d5:  nop
      IL_00d6:  leave.s    IL_0100

    }  // end .try
    catch [mscorlib]System.TypeInitializationException 
    {
      IL_00d8:  pop
      IL_00d9:  nop
      IL_00da:  ldstr      "Caught expected exception 2nd time\n"
      IL_00df:  call       void [System.Console]System.Console::WriteLine(string)
      IL_00e4:  nop
      IL_00e5:  nop
      IL_00e6:  leave.s    IL_0100

    }  // end handler
    catch [mscorlib]System.Exception 
    {
      IL_00e8:  stloc.1
      IL_00e9:  nop
      IL_00ea:  ldstr      "Caught unexpected exception 2nd time: "
      IL_00ef:  ldloc.1
      IL_00f0:  call       string [mscorlib]System.String::Concat(object,
                                                                  object)
      IL_00f5:  call       void [System.Console]System.Console::WriteLine(string)
      IL_00fa:  nop
      IL_00fb:  ldc.i4.0
      IL_00fc:  stloc.0
      IL_00fd:  nop
      IL_00fe:  leave.s    IL_0100

    }  // end handler
    IL_0100:  nop
    IL_0101:  ldloc.0
    IL_0102:  ldc.i4.0
    IL_0103:  ceq
    IL_0105:  stloc.3
    IL_0106:  ldloc.3
    IL_0107:  brtrue.s   IL_010f

    IL_0109:  nop
    IL_010a:  ldc.i4.s   100
    IL_010c:  stloc.2
    IL_010d:  br.s       IL_0115

    IL_010f:  nop
    IL_0110:  ldc.i4.s   101
    IL_0112:  stloc.2
    IL_0113:  br.s       IL_0115

    IL_0115:  ldloc.2
    IL_0116:  ret
  } // end of method Test::Main

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Test::.ctor

} // end of class Test


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file CctorThrowLDFTNStaticMethod.res
