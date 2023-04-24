// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


// Metadata version: v4.0.30319
.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )                         // .?_....:
  .ver 4:1:0:0
}
.assembly DevDiv_406160
{
}
.assembly extern xunit.core {}

.class public auto ansi beforefieldinit Bug.Program
       extends [System.Runtime]System.Object
{
  .method static char ILGEN_METHOD(int16, unsigned int16, native unsigned int)
   {
      .maxstack  65535
      .locals init (bool, int64, native unsigned int)
    IL_0000: ldarg.s 0x00
    IL_0002: ldc.i4.1
    IL_0015: clt
    IL_001b: starg.s 0x00
    IL_001d: ldloc 0x0001
    IL_0067: ldc.i8 0xc3ec93cfd869ae83
    IL_0072: ldc.r8 float64(0xb47a62a75e195a1c)
    IL_007c: conv.ovf.u8
    IL_007d: ldc.i4.1
    IL_0081: conv.ovf.i8.un
    IL_0088: div.un
    IL_0089: add.ovf.un
    IL_008c: ldloc 0x0001
    IL_009a: ldc.i8 0x97a27f9613c909c1
    IL_00a3: dup
    IL_00a4: clt
    IL_00a6: shr.un
    IL_00a7: xor
    IL_00a8: ldarg.s 0x00
    IL_00aa: conv.ovf.u8.un
    IL_00ab: and
    IL_00ac: ldloc.s 0x01
    IL_00ae: and
    IL_00af: conv.ovf.i2.un
    IL_00b0: xor
    IL_00cd: conv.i4
    IL_00ce: ret
  }

  .method public hidebysig static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       22 (0x16)
    .maxstack  8
    IL_0001:  ldc.i4.0
    IL_0002:  ldc.i4.0
     IL_0003:  ldc.i4.0
    IL_0004:  call       char   Bug.Program::ILGEN_METHOD(int16, unsigned int16, native unsigned int)
	IL_0005:  pop
    IL_0009:  ldstr      "Pass"
    IL_000e:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0013:  ldc.i4.s   100
    IL_0015:  ret
  } // end of method Program::Main

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Program::.ctor

} // end of class Bug.Program
