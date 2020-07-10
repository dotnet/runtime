// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0xa2df15de {}
.class ILGEN_0xa2df15de {
.field static native int[] field_0x0
.method static int32 Main() {
.entrypoint

 .locals init (native int[] LOCAL_0x2,float32 LOCAL_0x17,object LOCAL_0x19)
   ldc.i4 255
  newarr [mscorlib]System.IntPtr 
 stloc LOCAL_0x2
  ldc.r4 float32(0xab46b293) 
 stloc LOCAL_0x17
  newobj     instance void [mscorlib]System.Object::.ctor()
 stloc LOCAL_0x19

Start_Orphan_0:
 ldloca LOCAL_0x19
Start_Orphan_2:
Start_Orphan_4:
    ldsfld native int[] ILGEN_0xa2df15de::field_0x0
   stloc LOCAL_0x2
End_Orphan_4:
  ldc.r4 55.5
  ldc.r4 float32(0x3e734204) 
  rem 
 stloc LOCAL_0x17
End_Orphan_2:
  ldloca LOCAL_0x19
 ldind.ref
stind.ref
End_Orphan_0:
  ldc.i4 100
 ret
}

}
