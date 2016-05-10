// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x68de21f5 {}
.class ILGEN_0x68de21f5 {
.method static int32 Method_0xe207be7a() {
.maxstack	7
.locals (int64 LOCAL_0x2)
 ldc.i8 0xdde08ac3a1c32e82 
 stloc LOCAL_0x2
Start_Orphan_0:
 ldloc LOCAL_0x2
 ldloc LOCAL_0x2
 add.ovf.un
 stloc LOCAL_0x2
End_Orphan_0:
 ldc.i4 100
 ret
}
.method static int32 Main() {
.entrypoint
.maxstack	20
.try {
call int32 ILGEN_0x68de21f5::Method_0xe207be7a()
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