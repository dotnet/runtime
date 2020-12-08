// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b74608 { }
.class ILGEN_0x68ef7b55 {
.method static int32  Method_0x98671696(int16 Arg_0x2) {
.maxstack	19
.locals (unsigned int8[] LOCAL_0x6)
 ldc.i4 255
 newarr [mscorlib]System.Byte 
 stloc LOCAL_0x6
Start_Orphan_0:
   ldloc LOCAL_0x6
      ldarg Arg_0x2
     conv.ovf.i8
    conv.i4
    ldc.i4.3
   sub
  ldelema [mscorlib]System.Byte
 pop
End_Orphan_0:
 ldc.i4 55
 ret
}
.method static int32 Main() {
.entrypoint
.maxstack	7
.try {
 ldc.i4 0x7d9d8ed4 
 call int32  ILGEN_0x68ef7b55::Method_0x98671696(int16 Arg_0x2)
 leave.s IL_FAIL
} catch [mscorlib]System.IndexOutOfRangeException {
leave.s IL_END
}
IL_END:
ldc.i4.s 100
ret
IL_FAIL:
ldc.i4.s 101
ret


}
}
