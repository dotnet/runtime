// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b70992' {}
.assembly extern xunit.core {}
.class ILGEN_0xa483b94d {
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	13
.try {
 .locals (native int LOCAL_0x2)
 ldc.i4 0x4ee510cd 
 stloc LOCAL_0x2
   ldc.i4.5
   newarr [mscorlib]System.Byte
    ldloc LOCAL_0x2
   conv.ovf.i8
  conv.u4
 ldelema [mscorlib]System.Byte
ldind.u1
 pop
leave END
} catch [mscorlib]System.IndexOutOfRangeException {
pop
leave END
}
END:
ldc.i4 100
ret

}
}
