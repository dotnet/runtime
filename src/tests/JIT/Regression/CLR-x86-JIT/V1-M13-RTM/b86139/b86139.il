// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b86139' {}
.assembly extern xunit.core {}
.class ILGEN_0x9e05cbcc {
.field static	native int field_0x5
.method static unsigned int8  Method_0xa3eeac4e(unsigned int64 Arg_0x0, unsigned int16 Arg_0x1, int16 Arg_0x2, int8 Arg_0x3, float32 Arg_0x4, unsigned int32 Arg_0x5, float32 Arg_0x6, int32 Arg_0x7) {
.maxstack	19

ldc.i4 0xadf28692 
stsfld native int ILGEN_0x9e05cbcc::field_0x5


       ldsfld native int ILGEN_0x9e05cbcc::field_0x5
      conv.ovf.u8.un
     conv.i
 pop


        ldsfld native int ILGEN_0x9e05cbcc::field_0x5
       conv.ovf.u8
     conv.i
pop

        ldsfld native int ILGEN_0x9e05cbcc::field_0x5
       conv.ovf.u8
      conv.i
pop


 ldc.i4 55
ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	17
.try {
ldc.i8 0x5033ff76a92bc72c 
ldc.i4 0x6dcc21be 
ldc.i4 0xbecd700e 
ldc.i4 0xca303f43 
ldc.r4 float32(0x122092d4) 
ldc.i4 0xad3c487a 
ldc.r4 float32(0x3d7e5175) 
ldc.i4 0xfe0ddc33 

 call unsigned int8  ILGEN_0x9e05cbcc::Method_0xa3eeac4e(unsigned int64 Arg_0x0, unsigned int16 Arg_0x1, int16 Arg_0x2, int8 Arg_0x3, float32 Arg_0x4, unsigned int32 Arg_0x5, float32 Arg_0x6, int32 Arg_0x7)
conv.i4
leave.s IL_END

} catch [mscorlib]System.OverflowException {
 leave.s IL_END
}
IL_END:
 ldc.i4.s 100
 ret
 
}

}
