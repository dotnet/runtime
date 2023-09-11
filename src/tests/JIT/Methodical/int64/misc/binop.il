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
// MVID: {E24B38CB-1AB3-45BA-AA54-5C10DD72CDBA}
.namespace JitTest_binop_misc_il
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
      // Code size       196 (0xc4)
      .maxstack  2
      .locals (int64 V_0,
               int64 V_1,
               unsigned int64 V_2,
               unsigned int64 V_3,
               int32 V_4)
      .try
      {
        IL_0000:  ldc.i8     0x7000123480001234
        IL_0009:  stloc.0
        IL_000a:  ldc.i8     0x7123400081234000
        IL_0013:  stloc.1
        IL_0014:  ldloc.0
        IL_0015:  ldloc.1
        IL_0016:  and
        IL_0017:  ldc.i8     0x7000000080000000
        IL_0020:  beq.s      IL_0027

        IL_0022:  leave      IL_00b2

        IL_0027:  ldc.i8     0x7000123480001234
        IL_0030:  stloc.0
        IL_0031:  ldc.i8     0x7123400081234000
        IL_003a:  stloc.1
        IL_003b:  ldloc.0
        IL_003c:  ldloc.1
        IL_003d:  or
        IL_003e:  ldc.i8     0x7123523481235234
        IL_0047:  beq.s      IL_004b

        IL_0049:  leave.s    IL_00b2

        IL_004b:  ldc.i8     0x8000123480001234
        IL_0054:  stloc.2
        IL_0055:  ldc.i8     0x8123400081234000
        IL_005e:  stloc.3
        IL_005f:  ldloc.2
        IL_0060:  ldloc.3
        IL_0061:  and
        IL_0062:  not
        IL_0063:  ldc.i8     0x7fffffff7fffffff
        IL_006c:  beq.s      IL_0070

        IL_006e:  leave.s    IL_00b2

        IL_0070:  ldc.i8     0x8000123480001234
        IL_0079:  stloc.2
        IL_007a:  ldc.i8     0x8123400081234000
        IL_0083:  stloc.3
        IL_0084:  ldloc.2
        IL_0085:  ldloc.3
        IL_0086:  or
        IL_0087:  ldc.i8     0x8123523481235234
        IL_0090:  beq.s      IL_0094

        IL_0092:  leave.s    IL_00b2

        IL_0094:  leave.s    IL_00a3

      }  // end .try
      catch ['mscorlib']System.Exception 
      {
        IL_0096:  pop
        IL_0097:  ldstr      "Exception handled!"
        IL_009c:  call       void [System.Console]System.Console::WriteLine(class System.String)
        IL_00a1:  leave.s    IL_00b2

      }  // end handler
      IL_00a3:  ldstr      "Passed"
      IL_00a8:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_00ad:  ldc.i4    0x64
      IL_00ae:  stloc.s    V_4
      IL_00b0:  br.s       IL_00c1

      IL_00b2:  ldstr      "Failed"
      IL_00b7:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_00bc:  ldc.i4.1
      IL_00bd:  stloc.s    V_4

      IL_00c1:  ldloc.s    V_4
      IL_00c3:  ret
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

} // end of namespace 'JitTest_binop_misc_il'

//*********** DISASSEMBLY COMPLETE ***********************
