// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b31102' {}
.assembly extern xunit.core {}
.class ILGEN_0xed342785 {
.method static int32 Method_0x287e() {
.maxstack	13

 .locals (unsigned int64[] local_0x1,int8 local_0x5)
 ldc.i4 255
 newarr [mscorlib]System.UInt64 
 stloc local_0x1
 ldc.i4 0xa3b47b3 
 stloc local_0x5

Start_Orphan_9:
 ldloc local_0x1
 ldloc local_0x5
 ldloc local_0x5
 conv.i8
 stelem.i8

End_Orphan_9:
 ldloc local_0x1
 ldc.i4.2
 ldelem.u8
 conv.i2
ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	2
 .try {
 call int32 ILGEN_0xed342785::Method_0x287e()
 pop
 leave the_end
 } catch [mscorlib]System.IndexOutOfRangeException {
 pop
 leave the_end
 }
 the_end:
 ldc.i4 100
ret
}

}
//regPair >= REG_PAIR_FIRST && regPair <= REG_PAIR_LAST

//..\target.h, Line: 453

