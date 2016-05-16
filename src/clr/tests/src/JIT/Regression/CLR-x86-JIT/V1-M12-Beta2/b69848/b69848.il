// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0xbffa35b3 {}
.class ILGEN_0xbffa35b3 {
.field static	unsigned int8[] field_0x0
.field static	unsigned int8[] field_0x1
.method static unsigned int16  Method_0xe384245b(unsigned int64 Arg_0x0, float64 Arg_0x1, int32 Arg_0x2, unsigned int16 Arg_0x3, unsigned int32 Arg_0x4) {
.maxstack	11
.locals (unsigned int16 LOCAL_0x0,unsigned int8 LOCAL_0x2,float32 LOCAL_0x4,int64 LOCAL_0x7)
ldc.i4 0x30139f7d 
stloc LOCAL_0x0
ldc.r4 float32(0x8d3b225c) 
stloc LOCAL_0x4
ldc.i8 0x26176e948f0578d 
stloc LOCAL_0x7
ldc.i4 255
 newarr [mscorlib]System.SByte 
 stsfld unsigned int8[] ILGEN_0xbffa35b3::field_0x0
 ldloc LOCAL_0x2
 brtrue Branch_0x0
 br Branch_0x1
Branch_0x0:
Branch_0x1:
   ldc.r4 float32(0x9d9e126c)
   conv.ovf.i2
     ldloc LOCAL_0x4
Start_Orphan_8:
    ldloc LOCAL_0x0
   starg Arg_0x3
End_Orphan_8:
     ldloc LOCAL_0x7
    conv.r.un
  clt
 cgt.un
   ldarg Arg_0x3
     ldsfld unsigned int8[] ILGEN_0xbffa35b3::field_0x0
     ldc.i4 0x4e7fd2c9
    ldelema [mscorlib]System.SByte
   ldind.i1
  sub.ovf.un
pop
pop
Branch_0x2:
ldc.i4 55
Branch_0x3:
 ret
}
.method static int32 Main() {
.entrypoint
.maxstack	11
.try {
ldc.i8 0x7cc8fbafccc8d87 
ldc.r8 float64(0x4219532ac798761) 
ldc.i4 0xab8d954c 
ldc.i4 0xd756317f 
ldc.i4 0xe299ce99 
 call unsigned int16  ILGEN_0xbffa35b3::Method_0xe384245b(unsigned int64 Arg_0x0, float64 Arg_0x1, int32 Arg_0x2, unsigned int16 Arg_0x3, unsigned int32 Arg_0x4)
conv.i4
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
