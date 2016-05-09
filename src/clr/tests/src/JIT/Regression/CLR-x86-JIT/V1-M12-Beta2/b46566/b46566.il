// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b46566 {}
.class ILGEN_0x2a312d7 {
.field static	int64 field_0x4
.method static int32 Method_0x838bffa6(float32 Arg_0x5) {
.maxstack	19
.locals (int64 local_0x0,unsigned int64 local_0x4)

ldc.i8 0x9d01340390a29eec 
stloc local_0x0
ldc.i8 0x4b30ce02c6e3583e 
stloc local_0x4
ldc.i8 0x483da3473148c47 
stsfld int64 ILGEN_0x2a312d7::field_0x4

Start_Orphan_0:
 ldloca local_0x0
 pop

Start_Orphan_24:
 ldc.i4.0
 conv.u1
 newarr [mscorlib]System.Single
 ldc.r8 float64(0xdc0efb6f5797be45)
 ldarga Arg_0x5
 ldind.r4
 mul conv.r4
Start_Orphan_2a:
 ldsflda int64 ILGEN_0x2a312d7::field_0x4
 ldloc local_0x4
 stind.i8
End_Orphan_2a:
 ldc.r4 float32(0xf23e7b95)
 ldc.r4 float32(0x8428647a)
 div conv.r4
 ldc.r8 float64(0x648cacfbeb9dee88)
 add conv.r4
 clt.un
 ldloc local_0x0
 conv.r4
 stelem.r4
End_Orphan_24:
End_Orphan_0:
ldc.i4 55
ret
}
.method static int32 Main() {
.entrypoint
.maxstack	20
.try {
ldc.r4 float32(0x4fc4c35e) 
call int32 ILGEN_0x2a312d7::Method_0x838bffa6(float32 Arg_0x5)
pop
leave EEE
} catch [mscorlib]System.IndexOutOfRangeException {
pop
leave EEE
}
EEE:
ldc.i4 100
 ret
}

}
