// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly ILGEN_0xb6f84258 {}
.assembly extern xunit.core {}
.assembly extern mscorlib{auto}
.class ILGEN_0xb6f84258 {
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	17
      ldc.r4 float32(0x7e43bd25)
      ldc.r8 float64(0x842bc57893d1dfcb)
     div 
		// stack contains -infinity
    conv.u1
		// stack contains 0	
      ldc.i4 6
    conv.u1
     add
   newarr [mscorlib]System.Int32
   ldc.i4.5
  ldelema [mscorlib]System.Int32
  ldc.i4.2
 stind.i4
  ldc.i4 100
 ret
}
}
