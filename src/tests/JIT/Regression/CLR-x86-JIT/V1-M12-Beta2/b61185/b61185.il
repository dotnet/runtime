// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0xe71156b4 {}
.assembly extern xunit.core {}
.class ILGEN_0xe71156b4 {

.field static	unsigned int64 field_0x0
.field static	unsigned int16 field_0x1
.field static	int32 field_0x2

.method static int32 Method_0x5b58faef(int64 Arg_0x0, float32 Arg_0x1, unsigned int16 Arg_0x2, int8 Arg_0x3) {
.maxstack	9

 .locals (native int[] LOCAL_0x0)

ldc.i4 255
 newarr [mscorlib]System.IntPtr 
 stloc LOCAL_0x0

ldc.i8 0xbe8126d330be7b43 
stsfld unsigned int64 ILGEN_0xe71156b4::field_0x0
ldc.i4 0xfd796375 
stsfld unsigned int16 ILGEN_0xe71156b4::field_0x1
ldc.i4 0xbfe49d2f 
stsfld int32 ILGEN_0xe71156b4::field_0x2

 ldc.i4.8
 ldarg Arg_0x3
 ldsfld unsigned int16 ILGEN_0xe71156b4::field_0x1
 blt Branch_0x12

 ldc.i4.6
 br Branch_0x13

 Branch_0x12:

 ldc.i4.5
 Branch_0x13:

 ldsfld unsigned int16 ILGEN_0xe71156b4::field_0x1
 ldc.i4.8
 mul.ovf.un
 cgt
 sub.ovf.un
Start_Orphan_6:
 ldsfld unsigned int16 ILGEN_0xe71156b4::field_0x1
 ldc.i4.7
 ble Branch_0x16

 ldc.i4.1
 br Branch_0x17

 Branch_0x16:

 ldc.i4.5
 Branch_0x17:

 ldc.i4.6
 blt.un Branch_0x14

 ldsfld unsigned int64 ILGEN_0xe71156b4::field_0x0
 ldc.i8 0x78d696272efb5485
 ble Branch_0x18

 
 br Branch_0x19

 Branch_0x18:

 
 Branch_0x19:

 br Branch_0x15

 Branch_0x14:

 Branch_0x15:

End_Orphan_6:
 ldc.i8 0x26f901fa3e38911a
 conv.ovf.u2
 conv.ovf.i.un
 ldc.i4.6
 Branch_0x23:

pop
ldloc LOCAL_0x0
 ldc.i4.5
 ldelem.i
 Branch_0x21:
pop
pop
ldarg Arg_0x3
 Branch_0x1d:

pop
pop
ldc.i4.5
 Branch_0x39:
 Branch_0x35:

 Branch_0x11:

EOM: ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20
.try {
ldc.i8 0x57120f29e5f48bbd 
ldc.r4 float32(0x8e036649) 
ldc.i4 0xc4e041a5 
ldc.i4 0x2bc8086f 

call int32 ILGEN_0xe71156b4::Method_0x5b58faef(int64 Arg_0x0, float32 Arg_0x1, unsigned int16 Arg_0x2, int8 Arg_0x3)
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



