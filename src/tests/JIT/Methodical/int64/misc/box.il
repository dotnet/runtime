// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.







.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'box'// as "box"
{
}
.assembly extern xunit.core {}
// MVID: {8CBD3D83-F5B6-41AC-9D7A-099A59884AC4}
.namespace JitTest_box_misc_il
{
  .class public auto ansi Test
         extends ['mscorlib']System.Object
  {
    .method public hidebysig static int32
            Main() il managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      // Code size       159 (0x9f)
      .maxstack  2
      .locals (unsigned int64 V_0,
               int64 V_1,
               int32 V_2,
               unsigned int64 V_3,
               int64 V_4)
      .try
      {
        IL_0000:  ldc.i8     0x8000123480001234
        IL_0009:  stloc.0
        IL_000a:  ldloc.0
        IL_000b:  ldloc.s   V_0
        IL_000d:  box     ['mscorlib']System.UInt64
        IL_0012:  unbox      ['mscorlib']System.UInt64
        IL_0017:  ldind.i8
        IL_0018:  stloc.3
        IL_0019:  ldloc.s   V_3
        IL_001b:  box     ['mscorlib']System.UInt64
        IL_0020:  unbox      ['mscorlib']System.UInt64
        IL_0025:  ldind.i8
        IL_0026:  stloc.3
        IL_0027:  ldloc.s   V_3
        IL_0029:  box     ['mscorlib']System.UInt64
        IL_002e:  unbox      ['mscorlib']System.UInt64
        IL_0033:  ldind.i8
        IL_0034:  beq.s      IL_0038

        IL_0036:  leave.s    IL_008f

        IL_0038:  ldc.i8     0x8000123480001234
        IL_0041:  stloc.1
        IL_0042:  ldloc.1
        IL_0043:  ldloc.s   V_1
        IL_0045:  box     ['mscorlib']System.Int64
        IL_004a:  unbox      ['mscorlib']System.Int64
        IL_004f:  ldind.i8
        IL_0050:  stloc.s    V_4
        IL_0052:  ldloc.s   V_4
        IL_0054:  box     ['mscorlib']System.Int64
        IL_0059:  unbox      ['mscorlib']System.Int64
        IL_005e:  ldind.i8
        IL_005f:  stloc.s    V_4
        IL_0061:  ldloc.s   V_4
        IL_0063:  box     ['mscorlib']System.Int64
        IL_0068:  unbox      ['mscorlib']System.Int64
        IL_006d:  ldind.i8
        IL_006e:  beq.s      IL_0072

        IL_0070:  leave.s    IL_008f

        IL_0072:  leave.s    IL_0081

      }  // end .try
      catch ['mscorlib']System.Exception 
      {
        IL_0074:  pop
        IL_0075:  ldstr      "Exception handled!"
        IL_007a:  call       void [System.Console]System.Console::WriteLine(class System.String)
        IL_007f:  leave.s    IL_008f

      }  // end handler
      IL_0081:  ldstr      "Passed"
      IL_0086:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_008b:  ldc.i4    0x64
      IL_008c:  stloc.2
      IL_008d:  br.s       IL_009d

      IL_008f:  ldstr      "Failed"
      IL_0094:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_0099:  ldc.i4.1
      IL_009a:  stloc.2
      IL_009b:  br.s       IL_009d

      IL_009d:  ldloc.2
      IL_009e:  ret
    } // end of method 'Test::Main'

    .method public hidebysig specialname rtspecialname 
            instance void .ctor() il managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void ['mscorlib']System.Object::.ctor()
      IL_0006:  ret
    } // end of method 'Test::.ctor'

  } // end of class 'Test'

} // end of namespace 'JitTest_box_misc_il'

//*********** DISASSEMBLY COMPLETE ***********************
