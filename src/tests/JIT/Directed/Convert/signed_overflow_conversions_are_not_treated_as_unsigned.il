// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { auto }
.assembly extern xunit.core {}
.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
.assembly 'signed_overflow_conversions_are_not_treated_as_unsigned' { }

.class public auto SignedOverflowConversionsAreNotTreatedAsUnsigned extends [System.Runtime]System.Object
{
  .method private static void Print(class [System.Runtime]System.String) cil managed
  {
    ldarg 0
    call void [System.Console]System.Console::WriteLine(class [System.Runtime]System.String)
    ret
  }

  .method public hidebysig static int32 Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .locals init
    (
      [0] int32 result
    )
    
    ldc.i4 100
    stloc result
    
    ldloc result
    call int32 SignedOverflowConversionsAreNotTreatedAsUnsigned::TestConv_Ovf_I1_Un_FromMinusTwoFloat32()
    add
    stloc result
    
    ldloc result
    call int32 SignedOverflowConversionsAreNotTreatedAsUnsigned::TestConv_Ovf_I2_Un_FromMinusTwoFloat32()
    add
    stloc result
    
    ldloc result
    call int32 SignedOverflowConversionsAreNotTreatedAsUnsigned::TestConv_Ovf_I4_Un_FromMinusTwoFloat32()
    add
    stloc result
    
    ldloc result
    call int32 SignedOverflowConversionsAreNotTreatedAsUnsigned::TestConv_Ovf_I8_Un_FromMinusTwoFloat32()
    add
    stloc result
    
    ldloc result
    call int32 SignedOverflowConversionsAreNotTreatedAsUnsigned::TestConv_Ovf_I1_Un_FromMinusTwoFloat64()
    add
    stloc result
    
    ldloc result
    call int32 SignedOverflowConversionsAreNotTreatedAsUnsigned::TestConv_Ovf_I2_Un_FromMinusTwoFloat64()
    add
    stloc result
    
    ldloc result
    call int32 SignedOverflowConversionsAreNotTreatedAsUnsigned::TestConv_Ovf_I4_Un_FromMinusTwoFloat64()
    add
    stloc result
    
    ldloc result
    call int32 SignedOverflowConversionsAreNotTreatedAsUnsigned::TestConv_Ovf_I8_Un_FromMinusTwoFloat64()
    add
    stloc result
    
    ldloc result
    ldloc result
    ldc.i4 100
    ceq
    brtrue SUCCESS

    ldstr "FAILED"
    call void SignedOverflowConversionsAreNotTreatedAsUnsigned::Print(class [System.Runtime]System.String)
    ret

SUCCESS:
    ret
  }

  .method private hidebysig static int32 TestConv_Ovf_I1_Un_FromMinusTwoFloat32() cil managed noinlining
  {
    .locals init
    (
      [0] float32 minusTwoFloat32,
      [1] int32 result
    )
    
    ldc.r4 -2.0
    stloc minusTwoFloat32
    ldc.i4 0
    stloc result

    .try
    {
      ldloc minusTwoFloat32
      call int32 SignedOverflowConversionsAreNotTreatedAsUnsigned::Conv_Ovf_I1_Un_FromFloat32(float32)
      pop
      leave END
    }
    catch [System.Runtime]System.OverflowException
    {
      ldc.i4 1
      stloc result
      ldstr "conv.i1.ovf.un(-2.0f) resulted in an OverflowException!"
      call void SignedOverflowConversionsAreNotTreatedAsUnsigned::Print(class [System.Runtime]System.String)
      leave END
    }

END:    
    ldloc result
    ret
  }
  
  .method private hidebysig static int32 TestConv_Ovf_I2_Un_FromMinusTwoFloat32() cil managed noinlining
  {
    .locals init
    (
      [0] float32 minusTwoFloat32,
      [1] int32 result
    )
    
    ldc.r4 -2.0
    stloc minusTwoFloat32
    ldc.i4 0
    stloc result

    .try
    {
      ldloc minusTwoFloat32
      call int32 SignedOverflowConversionsAreNotTreatedAsUnsigned::Conv_Ovf_I2_Un_FromFloat32(float32)
      pop
      leave END
    }
    catch [System.Runtime]System.OverflowException
    {
      ldc.i4 1
      stloc result
      ldstr "conv.i2.ovf.un(-2.0f) resulted in an OverflowException!"
      call void SignedOverflowConversionsAreNotTreatedAsUnsigned::Print(class [System.Runtime]System.String)
      leave END
    }

END:    
    ldloc result
    ret
  }
  
  .method private hidebysig static int32 TestConv_Ovf_I4_Un_FromMinusTwoFloat32() cil managed noinlining
  {
    .locals init
    (
      [0] float32 minusTwoFloat32,
      [1] int32 result
    )
    
    ldc.r4 -2.0
    stloc minusTwoFloat32
    ldc.i4 0
    stloc result

    .try
    {
      ldloc minusTwoFloat32
      call int32 SignedOverflowConversionsAreNotTreatedAsUnsigned::Conv_Ovf_I4_Un_FromFloat32(float32)
      pop
      leave END
    }
    catch [System.Runtime]System.OverflowException
    {
      ldc.i4 1
      stloc result
      ldstr "conv.i4.ovf.un(-2.0f) resulted in an OverflowException!"
      call void SignedOverflowConversionsAreNotTreatedAsUnsigned::Print(class [System.Runtime]System.String)
      leave END
    }

END:
    ldloc result
    ret
  }
  
  .method private hidebysig static int32 TestConv_Ovf_I8_Un_FromMinusTwoFloat32() cil managed noinlining
  {
    .locals init
    (
      [0] float32 minusTwoFloat32,
      [1] int32 result
    )
    
    ldc.r4 -2.0
    stloc minusTwoFloat32
    ldc.i4 0
    stloc result

    .try
    {
      ldloc minusTwoFloat32
      call int64 SignedOverflowConversionsAreNotTreatedAsUnsigned::Conv_Ovf_I8_Un_FromFloat32(float32)
      pop
      leave END
    }
    catch [System.Runtime]System.OverflowException
    {
      ldc.i4 1
      stloc result
      ldstr "conv.i8.ovf.un(-2.0f) resulted in an OverflowException!"
      call void SignedOverflowConversionsAreNotTreatedAsUnsigned::Print(class [System.Runtime]System.String)
      leave END
    }

END:
    ldloc result
    ret
  }
  
  .method private hidebysig static int32 TestConv_Ovf_I1_Un_FromMinusTwoFloat64() cil managed noinlining
  {
    .locals init
    (
      [0] float64 minusTwoFloat64,
      [1] int32 result
    )
    
    ldc.r8 -2.0
    stloc minusTwoFloat64
    ldc.i4 0
    stloc result

    .try
    {
      ldloc minusTwoFloat64
      call int32 SignedOverflowConversionsAreNotTreatedAsUnsigned::Conv_Ovf_I1_Un_FromFloat64(float64)
      pop
      leave END
    }
    catch [System.Runtime]System.OverflowException
    {
      ldc.i4 1
      stloc result
      ldstr "conv.i1.ovf.un(-2.0d) resulted in an OverflowException!"
      call void SignedOverflowConversionsAreNotTreatedAsUnsigned::Print(class [System.Runtime]System.String)
      leave END
    }

END:
    ldloc result
    ret
  }
  
  .method private hidebysig static int32 TestConv_Ovf_I2_Un_FromMinusTwoFloat64() cil managed noinlining
  {
    .locals init
    (
      [0] float64 minusTwoFloat64,
      [1] int32 result
    )
    
    ldc.r8 -2.0
    stloc minusTwoFloat64
    ldc.i4 0
    stloc result

    .try
    {
      ldloc minusTwoFloat64
      call int32 SignedOverflowConversionsAreNotTreatedAsUnsigned::Conv_Ovf_I2_Un_FromFloat64(float64)
      pop
      leave END
    }
    catch [System.Runtime]System.OverflowException
    {
      ldc.i4 1
      stloc result
      ldstr "conv.i2.ovf.un(-2.0d) resulted in an OverflowException!"
      call void SignedOverflowConversionsAreNotTreatedAsUnsigned::Print(class [System.Runtime]System.String)
      leave END
    }

END:
    ldloc result
    ret
  }
  
  .method private hidebysig static int32 TestConv_Ovf_I4_Un_FromMinusTwoFloat64() cil managed noinlining
  {
    .locals init
    (
      [0] float64 minusTwoFloat64,
      [1] int32 result
    )
    
    ldc.r8 -2.0
    stloc minusTwoFloat64
    ldc.i4 0
    stloc result

    .try
    {
      ldloc minusTwoFloat64
      call int32 SignedOverflowConversionsAreNotTreatedAsUnsigned::Conv_Ovf_I4_Un_FromFloat64(float64)
      pop
      leave END
    }
    catch [System.Runtime]System.OverflowException
    {
      ldc.i4 1
      stloc result
      ldstr "conv.i4.ovf.un(-2.0d) resulted in an OverflowException!"
      call void SignedOverflowConversionsAreNotTreatedAsUnsigned::Print(class [System.Runtime]System.String)
      leave END
    }

END:
    ldloc result
    ret
  }
  
  .method private hidebysig static int32 TestConv_Ovf_I8_Un_FromMinusTwoFloat64() cil managed noinlining
  {
    .locals init
    (
      [0] float64 minusTwoFloat64,
      [1] int32 result
    )
    
    ldc.r8 -2.0
    stloc minusTwoFloat64
    ldc.i4 0
    stloc result

    .try
    {
      ldloc minusTwoFloat64
      call int64 SignedOverflowConversionsAreNotTreatedAsUnsigned::Conv_Ovf_I8_Un_FromFloat64(float64)
      pop
      leave END
    }
    catch [System.Runtime]System.OverflowException
    {
      ldc.i4 1
      stloc result
      ldstr "conv.i8.ovf.un(-2.0d) resulted in an OverflowException!"
      call void SignedOverflowConversionsAreNotTreatedAsUnsigned::Print(class [System.Runtime]System.String)
      leave END
    }

END:
    ldloc result
    ret
  }

  .method private hidebysig static int32 Conv_Ovf_I1_Un_FromFloat32(float32 input) cil managed noinlining
  {
    ldarg 0
    conv.ovf.i1.un
    ret
  }
  
  .method private hidebysig static int32 Conv_Ovf_I2_Un_FromFloat32(float32 input) cil managed noinlining
  {
    ldarg 0
    conv.ovf.i2.un
    ret
  }
  
  .method private hidebysig static int32 Conv_Ovf_I4_Un_FromFloat32(float32 input) cil managed noinlining
  {
    ldarg 0
    conv.ovf.i4.un
    ret
  }
  
  .method private hidebysig static int64 Conv_Ovf_I8_Un_FromFloat32(float32 input) cil managed noinlining
  {
    ldarg 0
    conv.ovf.i8.un
    ret
  }
  
  .method private hidebysig static int32 Conv_Ovf_I1_Un_FromFloat64(float64 input) cil managed noinlining
  {
    ldarg 0
    conv.ovf.i1.un
    ret
  }
  
  .method private hidebysig static int32 Conv_Ovf_I2_Un_FromFloat64(float64 input) cil managed noinlining
  {
    ldarg 0
    conv.ovf.i2.un
    ret
  }
  
  .method private hidebysig static int32 Conv_Ovf_I4_Un_FromFloat64(float64 input) cil managed noinlining
  {
    ldarg 0
    conv.ovf.i4.un
    ret
  }
  
  .method private hidebysig static int64 Conv_Ovf_I8_Un_FromFloat64(float64 input) cil managed noinlining
  {
    ldarg 0
    conv.ovf.i8.un
    ret
  }
}
