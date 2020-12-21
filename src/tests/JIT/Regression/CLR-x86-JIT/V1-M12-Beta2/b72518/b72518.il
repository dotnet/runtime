// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0xb37e58f3.exe {}
.class ILGEN_0xb37e58f3 {
.method static int8  Method_0xf9cacde1() {
.maxstack	9

 .locals (int32 LOCAL_0x0,native int LOCAL_0x4)
 ldc.i4 0x7848afe2 
 stloc LOCAL_0x0
 ldc.i4 0x352567b0 
 stloc LOCAL_0x4

Start_Orphan_0:
   ldc.i8 0x81667fe4299efe74
   ldloc LOCAL_0x4
  conv.i8
 div.un
   ldc.i8 0x2560c8f91f3ca690
 bgt Branch_0x0
   ldloc LOCAL_0x0
   ldloc LOCAL_0x4
  div.un
 pop
 br Branch_0x1
Branch_0x0:
Branch_0x1:
End_Orphan_0:
 ldc.i4 100
 ret
}
.method static int32 Main() {
.entrypoint
.maxstack	1
 call int8  ILGEN_0xb37e58f3::Method_0xf9cacde1()
 ret
}
}
