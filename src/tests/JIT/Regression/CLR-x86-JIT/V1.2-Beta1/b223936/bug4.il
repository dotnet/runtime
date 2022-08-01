// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}

.assembly 'bug4' {}
.assembly extern xunit.core {}
.assembly extern mscorlib{auto}
.class ILGEN_0xca498c54 {
.method static unsigned int64  Method_0x570110b9(float64 Arg_0x0, float32 Arg_0x1, unsigned int32 Arg_0x2, int8 Arg_0x3, int8 Arg_0x4, int64 Arg_0x5, unsigned int16 Arg_0x6, int64 Arg_0x7, unsigned int64 Arg_0x8, int8 Arg_0x9, int8 Arg_0xa, int8 Arg_0xb, float32 Arg_0xc, int8 Arg_0xd) {
.maxstack	11
   ldc.i4.s -126
  conv.ovf.u4
 pop
  ldc.i8 55
 ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	29
.try
{
    ldc.r8 float64(0x63a3c50863c4bb60) 
    ldc.r4 float32(0xf4e2d3f8) 
    ldc.i4 0x4db0e12f 
    ldc.i4 0x873f41c7 
    ldc.i4 0x38599105 
    ldc.i8 0xbe807358cb81937e 
    ldc.i4 0xf41d1542 
    ldc.i8 0x6defe9d363de3cca 
    ldc.i8 0x8d158419c9455a68 
    ldc.i4 0x53d7df8a 
    ldc.i4 0x9e5c4c2d 
    ldc.i4 0xeadce12d 
    ldc.r4 float32(0xc6c8a672) 
    ldc.i4 0xc0557631 
   call unsigned int64  ILGEN_0xca498c54::Method_0x570110b9(float64 Arg_0x0, float32 Arg_0x1, unsigned int32 Arg_0x2, int8 Arg_0x3, int8 Arg_0x4, int64 Arg_0x5, unsigned int16 Arg_0x6, int64 Arg_0x7, unsigned int64 Arg_0x8, int8 Arg_0x9, int8 Arg_0xa, int8 Arg_0xb, float32 Arg_0xc, int8 Arg_0xd)
   leave FAIL
}
catch [mscorlib]System.OverflowException
{
 ldstr "System.OverflowException caught as expected"
 call void [System.Console]System.Console::WriteLine(string)
 leave PASS
}

FAIL:
ldc.i4 1
 ldstr "FAIL"
 call void [System.Console]System.Console::WriteLine(string)
ret

PASS:
ldc.i4 100
 ldstr "PASS"
 call void [System.Console]System.Console::WriteLine(string)
ret

}
}
