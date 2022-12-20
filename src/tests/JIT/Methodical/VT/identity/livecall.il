// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.




.file alignment 512

.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly livecall // as "livecall"
{
}
.assembly extern xunit.core {}
// MVID: {6300FB9B-B24E-45DA-9A61-2556746FEEEF}
.namespace MS_livecall_il
{
  .class value public auto ansi sealed VT
         extends [mscorlib]System.ValueType
  {
    .field private float64 v
    .method private hidebysig static void 
            DoAnything(value class MS_livecall_il.VT& vt) il managed
    {
      // Code size       16 (0x10)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  ldc.r8     20.
      IL_000a:  stfld      float64 MS_livecall_il.VT::v
      IL_000f:  ret
    } // end of method VT::DoAnything

    .method public hidebysig static int32
            Main() il managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      // Code size       74 (0x4a)
      .maxstack  2
      .locals (value class MS_livecall_il.VT V_0, value class MS_livecall_il.VT V_1)
      IL_0000:  ldloca.s   V_0
      IL_0002:  initobj    MS_livecall_il.VT
      IL_0008:  ldloca.s   V_0
      IL_000a:  ldc.r8     1.
      IL_0013:  stfld      float64 MS_livecall_il.VT::v
      			ldloc.0
      IL_0018:  ldloca.s   V_0
      IL_001a:  call       void MS_livecall_il.VT::DoAnything(value class MS_livecall_il.VT&)
      
      			stloc.1
      IL_001f:  ldloca.s   V_1
      IL_0021:  ldfld      float64 MS_livecall_il.VT::v
      IL_0026:  ldc.r8     1.
      IL_002f:  bne.un.s   IL_003e

      IL_0031:  ldstr      "PASSED"
      IL_0036:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_003b:  ldc.i4.s   100
      			ret

      IL_003e:  ldstr      "FAILED"
      IL_0043:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_0048:  ldc.i4.1
      IL_0049:  ret
    } // end of method VT::Main

  } // end of class VT

} // end of namespace MS_livecall_il

//*********** DISASSEMBLY COMPLETE ***********************
