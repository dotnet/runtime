// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}

.assembly 'bug2' {}
.assembly extern xunit.core {}
.assembly extern mscorlib{auto}
.class ILGEN_0xef3a9aea {

.method static int64  Method_0x40026544(native int Arg_0x1, float64 Arg_0x2) {
.maxstack	17
       ldarg Arg_0x2
        ldc.r4 22.2
       conv.r4
      bgt Branch_0x6
      ldc.i8 0x9abaadc234298901
      ldarg Arg_0x1
     brfalse Branch_0xe
     ldc.i8 0x9652fa5b635e909
     br Branch_0xf
 Branch_0xe:
     ldc.i8 0xb85e2c6058bfb361
 Branch_0xf:
    bge.un Branch_0xa
    ldc.i8 0x6f20f8413c7d23d9 
    br Branch_0xb
 Branch_0xa:
    ldc.i8 0xcdde792763aac4bd
   conv.ovf.i8.un
 Branch_0xb:
  pop
 Branch_0x6:
  ldc.i8 0x9ac29bfd47019ed5
 ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	27
.try
{
    ldc.i4 0x80178e7f conv.i
    ldc.r8 float64(0xad917f3e7f4e476) 
   call int64  ILGEN_0xef3a9aea::Method_0x40026544(native int Arg_0x1, float64 Arg_0x2)
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
