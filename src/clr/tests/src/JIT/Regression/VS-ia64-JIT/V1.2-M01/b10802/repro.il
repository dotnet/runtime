// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}





// Metadata version: v1.1.1919
.assembly extern mscorlib
{
  .ver 0:0:0:0
}
.assembly test
{

  // --- The following custom attribute is added automatically, do not uncomment -------
  //  .custom instance void [mscorlib]System.Diagnostics.DebuggableAttribute::.ctor(bool,
  //                                                                                bool) = ( 01 00 01 01 00 00 ) 

  .ver 0:0:0:0
}
.module convovf_i8_u.exe
// MVID: {8F1AC63C-507C-42CD-B9EC-7E585802DBD0}
.imagebase 0x00400000
.subsystem 0x00000003
.file alignment 512
.corflags 0x00000001
// Image base: 0x000006FBFF020000

// =============== CLASS MEMBERS DECLARATION ===================

.class private auto ansi Test
       extends [mscorlib]System.Object
{
  .method private hidebysig static int32 
          Main() cil managed
  {
    .entrypoint
    // Code size       1283 (0x503)
    .maxstack  5
    .try
    {
      IL_02e8:  ldstr      "test # 17"
      IL_02ed:  call       void [System.Console]System.Console::WriteLine(string)
      IL_02f2:  ldc.i4     -1
      IL_02fb:  conv.ovf.u

      IL_02fc:  pop
      IL_02fd:  leave      IL_04f7

    }  // end .try
    catch [mscorlib]System.OverflowException 
    {
      IL_0302:  pop
      IL_0303:  leave      IL_04e8

    }  // end handler

    IL_04e8:  ldc.i4     0x64
    IL_04ed:  ldstr      "passed"
    IL_04f2:  br         IL_04fd

    IL_04f7:  ldc.i4.1
    IL_04f8:  ldstr      "failed"
    IL_04fd:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0502:  ret
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


//*********** DISASSEMBLY COMPLETE ***********************
