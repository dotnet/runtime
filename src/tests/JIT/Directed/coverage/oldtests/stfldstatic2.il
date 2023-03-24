// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// volatile. stfld on static field
.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly stfldstatic2 {}
.class public auto ansi beforefieldinit stfldstatic2
       extends [mscorlib]System.Object
{
  .field private static int32 s1
  .field private static uint32 s2
  .field private static int64 s3
  .field private static uint64 s4
  .field private static float32 s5
  .field private static float64 s6
  .field private static int16 s7
  .field private static uint16 s8
  .field private static uint8 s9
  .field private static valuetype [mscorlib]System.Decimal s10
  .field private static string s11
  .field private int32 a
  .method public hidebysig static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       196 (0xc4)
    .maxstack  7
    .locals init (class stfldstatic2 V_0)
    IL_0000:  newobj     instance void  stfldstatic2::.ctor()
    IL_0005:  stloc.0

    IL_0018:  ldloc.0   dup   volatile. ldfld    int32  stfldstatic2::s1
    IL_001d:  ldc.i4.1
    IL_001e:  sub
    IL_001f:  volatile. stfld     int32  stfldstatic2::s1
    IL_0024:  ldloc.0   dup   volatile. ldfld    uint32  stfldstatic2::s2
    IL_0029:  ldc.i4.1
    IL_002a:  add
    IL_002b:  volatile. stfld     uint32  stfldstatic2::s2
    IL_0030:  ldloc.0   dup   volatile. ldfld    int64  stfldstatic2::s3
    IL_0035:  ldc.i4.1
    IL_0036:  conv.i8
    IL_0037:  add
    IL_0038:  volatile. stfld     int64  stfldstatic2::s3
    IL_003d:  ldloc.0   dup   volatile. ldfld    uint64  stfldstatic2::s4
    IL_0042:  ldc.i4.1
    IL_0043:  conv.i8
    IL_0044:  add
    IL_0045:  volatile. stfld     uint64  stfldstatic2::s4
    IL_004a:  ldloc.0   dup   volatile. ldfld    float32  stfldstatic2::s5
    IL_004f:  ldc.r4     1.23
    IL_0054:  add
    IL_0055:  volatile. stfld     float32  stfldstatic2::s5
    IL_005a:  ldloc.0   dup   volatile. ldfld    float64  stfldstatic2::s6
    IL_005f:  ldc.r8     8.9000000000000004
    IL_0068:  sub
    IL_0069:  volatile. stfld     float64  stfldstatic2::s6
    IL_006e:  ldloc.0   dup   volatile. ldfld    int16  stfldstatic2::s7
    IL_0073:  ldc.i4.1
    IL_0074:  add
    IL_0075:  conv.i2
    IL_0076:  volatile. stfld     int16  stfldstatic2::s7
    IL_007b:  ldloc.0   dup   volatile. ldfld    uint16  stfldstatic2::s8
    IL_0080:  ldc.i4.1
    IL_0081:  sub
    IL_0082:  conv.u2
    IL_0083:  volatile. stfld     uint16  stfldstatic2::s8
    IL_0088:  ldloc.0   dup   volatile. ldfld    uint8  stfldstatic2::s9
    IL_008d:  ldc.i4.1
    IL_008e:  add
    IL_008f:  conv.u1
    IL_0090:  volatile. stfld     uint8  stfldstatic2::s9
    IL_0095:  ldloc.0   dup   volatile. ldfld    valuetype [mscorlib]System.Decimal  stfldstatic2::s10
    IL_009a:  ldc.i4     0x3e7
    IL_009f:  ldc.i4.0
    IL_00a0:  ldc.i4.0
    IL_00a1:  ldc.i4.0
    IL_00a2:  ldc.i4.1
    IL_00a3:  newobj     instance void [mscorlib]System.Decimal::.ctor(int32,
                                                                       int32,
                                                                       int32,
                                                                       bool,
                                                                       uint8)
    IL_00a8:  call       valuetype [mscorlib]System.Decimal [mscorlib]System.Decimal::op_Addition(valuetype [mscorlib]System.Decimal,
                                                                                                  valuetype [mscorlib]System.Decimal)
    IL_00ad:  volatile. stfld     valuetype [mscorlib]System.Decimal  stfldstatic2::s10
    IL_00b2:  ldloc.0   ldstr      "test PASSED"
    IL_00b7:  ldc.i4 5
    IL_00b8:  ldc.i4 6
    IL_00b9:  callvirt   instance string [mscorlib]System.String::Substring(int32,
                                                                            int32)
    IL_00be:  volatile. stfld     string  stfldstatic2::s11

    ldloc.0
    volatile. ldfld    string  stfldstatic2::s11
    call void [System.Console]System.Console::WriteLine(string)

    ldc.i4 100
    IL_00c3:  ret
  } // end of method  stfldstatic2::Main

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method  stfldstatic2::.ctor

}
