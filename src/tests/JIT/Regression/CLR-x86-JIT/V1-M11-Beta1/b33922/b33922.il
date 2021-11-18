// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0xe32b9f8 {}
.assembly extern xunit.core {}
.class ILGEN_0xe32b9f8 {

.method static int32 Method_0x5af1(int16 Arg_0x0) {
 .maxstack	9
 .locals (int32 local_0x0)

 ldc.i4 0x3bd7287b 
 stloc local_0x0

 ldc.i4 255
 newarr [mscorlib]System.UInt16 
 ldc.i4 0x2cc87d5b
 ldelema [mscorlib]System.UInt16
 ldind.u2
 ldloc local_0x0
 ldarg Arg_0x0
 add
 ldc.i4.1
 add.ovf
 ldc.i4.2
 mul
 clt
ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20
.try {
 ldc.i4 0x54002d63 
 call int32 ILGEN_0xe32b9f8::Method_0x5af1(int16 Arg_0x0)
 pop
  leave stop
 } catch [mscorlib]System.IndexOutOfRangeException {
 pop
 leave stop
 }
 stop:
ldc.i4 100
ret
}

}
