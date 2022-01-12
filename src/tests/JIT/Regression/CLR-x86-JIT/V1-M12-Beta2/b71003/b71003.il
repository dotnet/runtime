// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x5c49cb7e {}
.assembly extern xunit.core {}
.class ILGEN_0x5c49cb7e {
.method static float32  Method_0x19bd6d70(native int Arg_0x0) {
.maxstack	15
     ldc.r4 1.1
       ldc.i4 0x1c94bc1
       ldarg Arg_0x0
      mul.ovf
     conv.r4
   div 
  call float64 [mscorlib]System.Math::Round(float64)
 conv.i
conv.r8
 ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	7
.try {
 ldc.i4 0xea670e20 conv.i
 call float32  ILGEN_0x5c49cb7e::Method_0x19bd6d70(native int Arg_0x0)
 conv.i4
 pop
leave END
} catch [mscorlib]System.OverflowException {
pop
leave END
}
END:
ldc.i4 100
ret

}
}
