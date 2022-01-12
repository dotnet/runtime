// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x942c2798 {}
.assembly extern xunit.core {}
.class ILGEN_0x942c2798 {

.field static	float32 field_0x2
.field static	unsigned int32 field_0x3
.method static int32 Method_0x9787852c(unsigned int64 Arg_0x0, int64 Arg_0x1) {
.maxstack	9

 .locals (unsigned int32 LOCAL_0x2)
ldc.i4 0xc2751318 
stloc LOCAL_0x2

ldc.r4 float32(0x49473fa9) 
stsfld float32 ILGEN_0x942c2798::field_0x2
ldc.i4 0xb30b22b 
stsfld unsigned int32 ILGEN_0x942c2798::field_0x3

Start_Orphan_0:
 ldc.i4.m1
 ldc.i4.m1
 clt
 ldc.i4.2
 brfalse Branch_0x4
 ldc.i4.4
 br Branch_0x5
Branch_0x4:
 ldloc LOCAL_0x2
Branch_0x5:
 bge.un Branch_0x0
 ldarg Arg_0x1
 conv.ovf.i4
 ldsfld unsigned int32 ILGEN_0x942c2798::field_0x3
 ldc.i4.s -83
 mul.ovf.un
 ble Branch_0x8
 br Branch_0x9
Branch_0x8:
Branch_0x9:
 br Branch_0x1
Branch_0x0:
 ldc.i4 0x1fabfc35
 ldc.i4.1
 bgt Branch_0x10
 ldsfld float32 ILGEN_0x942c2798::field_0x2
 br Branch_0x11
Branch_0x10:
 ldsfld float32 ILGEN_0x942c2798::field_0x2
Branch_0x11:
 pop
Branch_0x1:
End_Orphan_0:
ldc.i4 55
EOM: ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20
.try {
ldc.i8 0xf2cb6c19344a0366 
ldc.i8 0x5ff142177ee454d6 

call int32 ILGEN_0x942c2798::Method_0x9787852c(unsigned int64 Arg_0x0, int64 Arg_0x1)
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
