// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b63730' {}
.assembly extern xunit.core {}
.class ILGEN_0x200e1753 {
.method static int32 Method_0xae74a560(unsigned int64 Arg_0x0, int32 Arg_0x1, unsigned int8 Arg_0x2) {
.maxstack	9
.locals (float64[] LOCAL_0x0,int16[] LOCAL_0x1,unsigned int32 LOCAL_0x2,int64[] LOCAL_0x3)

 ldc.i4 255
 newarr [mscorlib]System.Double 
 stloc LOCAL_0x0
 ldc.i4 255
 newarr [mscorlib]System.Int16 
 stloc LOCAL_0x1
 ldc.i4 255
 newarr [mscorlib]System.Int64 
 stloc LOCAL_0x3
 ldarg Arg_0x0
 conv.u2
 ldarg Arg_0x1
 conv.u1
 newarr [mscorlib]System.Int64
 ldc.i4.1
 ldc.i4.1
 beq Branch_0xa
 ldc.i4.s -64
 br Branch_0xb
Branch_0xa:
 ldarg Arg_0x1
Branch_0xb:
 ldelem.i8
 conv.ovf.i1
 cgt
EOM: ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20
.try {
ldc.i8 0x8e65ab5d5022841d 
ldc.i4 0x1cf3abac 
ldc.i4 0xa6854e5b 
call int32 ILGEN_0x200e1753::Method_0xae74a560(unsigned int64 Arg_0x0, int32 Arg_0x1, unsigned int8 Arg_0x2)
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
