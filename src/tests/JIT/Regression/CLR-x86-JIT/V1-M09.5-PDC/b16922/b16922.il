// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b16922' {}
.class public ILGEN_0x38e912e6 {

.field static	unsigned int16 field_0x1
.field static	unsigned int32 field_0x2

.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	18

 .locals (unsigned int16[] local_0x1,unsigned int32[] local_0x2,int16[] local_0x5,float64[] local_0x9,unsigned int8 local_0xa)

.try {
ldc.i4 255
 newarr [mscorlib]System.UInt16 
 stloc local_0x1
ldc.i4 255
 newarr [mscorlib]System.UInt32 
 stloc local_0x2
 ldc.i4 255
 newarr [mscorlib]System.Int16 
 stloc local_0x5
 ldc.i4 255
 newarr [mscorlib]System.Double 
 stloc local_0x9
 ldc.i4 0x581e7433  
 stloc local_0xa

ldc.i4 0x15f453d4 
stsfld unsigned int16 ILGEN_0x38e912e6::field_0x1
ldc.i4 0x2df22c78 
stsfld unsigned int32 ILGEN_0x38e912e6::field_0x2

 ldc.i4 5
 ldc.i4 5
 conv.i1
 ldc.i4 5
 newarr [mscorlib]System.Int16
 ldc.i4 5
 ldelem.i2
 ceq
 ldloc local_0x9
Start_Orphan_35:
 ldloc local_0x1
 ldloc local_0x5
 ldloc local_0xa
 ldelem.i2
 ldsflda unsigned int32 ILGEN_0x38e912e6::field_0x2
 ldind.u4
 stelem.i2
End_Orphan_35:
 ldloc local_0x9
 ldloc local_0x2
 ldc.i4.6
 ldelem.u4
 ldelem.r8
 ldsfld unsigned int16 ILGEN_0x38e912e6::field_0x1
 conv.r.un
 cgt.un
 ldelema [mscorlib]System.Double
 ldind.r8
 conv.i1
 shr.un
 clt.un
 pop
 leave Beyond
} catch [mscorlib]System.IndexOutOfRangeException {
  pop
 leave Beyond
}
Beyond:
ldc.i4 100
ret
}
}
