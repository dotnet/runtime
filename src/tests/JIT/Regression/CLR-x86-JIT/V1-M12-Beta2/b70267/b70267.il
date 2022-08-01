// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b70267' {}
.assembly extern xunit.core {}
.class ILGEN_0x4cf11307 {
.field static	unsigned int64 field_0x0
.method static int32 Method_0x3417140a() {
.maxstack	5
 ldc.i8 0x50b2a7b2d73de702 
 stsfld unsigned int64 ILGEN_0x4cf11307::field_0x0
 ldc.i4.3
 ldc.i4.s 82
 rem.un
 brtrue Branch_0x2
 ldc.i8 0x9bc5723f7795fe11 
 conv.ovf.u1
 br Branch_0x3
Branch_0x2:
 ldsfld unsigned int64 ILGEN_0x4cf11307::field_0x0
 ldc.i8 0x9bc5723f7795fe11 
 cgt.un
Branch_0x3:
 ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20
call int32 ILGEN_0x4cf11307::Method_0x3417140a()
ldc.i4 100
add
 ret
}

}


