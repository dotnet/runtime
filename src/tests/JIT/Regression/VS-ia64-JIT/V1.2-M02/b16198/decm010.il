// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly decm010 {}
.assembly extern xunit.core {}
.class public auto ansi beforefieldinit MyClass
       extends [mscorlib]System.Object
{
  .method public hidebysig static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       47 (0x2f)
    .maxstack  6
    .locals init (valuetype [mscorlib]System.Decimal V_0,
             int32 V_1)
    IL_0000:  ldc.i4     0x98000000
    IL_0005:  ldc.i4     0x515792cb
    IL_000a:  ldc.i4     0xff4344b5
    IL_000f:  ldc.i4.0
    IL_0010:  ldc.i4.0
    IL_0011:  newobj     instance void [mscorlib]System.Decimal::.ctor(int32,
                                                                       int32,
                                                                       int32,
                                                                       bool,
                                                                       uint8)
    IL_0016:  stloc.0
    .try
    {
      IL_0017:  ldloc.0
      IL_0018:  ldc.i4.2
      IL_0019:  newobj     instance void [mscorlib]System.Decimal::.ctor(int32)
      IL_001e:  call       valuetype [mscorlib]System.Decimal [mscorlib]System.Decimal::op_Multiply(valuetype [mscorlib]System.Decimal,
                                                                                                    valuetype [mscorlib]System.Decimal)
      IL_0023:  pop
      IL_0024:  leave.s    IL_002b

    }  // end .try
    catch [mscorlib]System.OverflowException 
    {
      IL_0026:  pop
      IL_0027:  ldc.i4 100
      IL_0028:  stloc.1
      IL_0029:  leave.s    IL_002d

    }  // end handler
    IL_002b:  ldc.i4.1
    IL_002c:  ret

    IL_002d:  ldloc.1
    IL_002e:  ret
  } // end of method MyClass::Main

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method MyClass::.ctor

}
