// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.





.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern xunit.core {}
.assembly extern mscorlib
{
  .ver 0:0:0:0
}
.assembly 'test_543645'
{
  .ver 0:0:0:0
}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       
.corflags 0x00000001    



.class public auto ansi beforefieldinit Overflow
       extends [mscorlib]System.Object
{
  .method public hidebysig static int32 Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .locals init (int32 r)

      ldc.i4 100
      stloc r

      .try
      {
        call int64 Overflow::test1()
        call void [System.Console]System.Console::WriteLine(int64)
        ldc.i4.1
        stloc r
        leave.s end1
      }
      catch [mscorlib]System.OverflowException
      {
        pop
        ldstr "OverflowException correctly thrown"
        call void [System.Console]System.Console::WriteLine(string)
        leave.s end1
      }

    end1:

      call int64 Overflow::test2()
      dup
      call void [System.Console]System.Console::WriteLine(int64)
      ldc.i8 0x8000000000000000
      conv.u8
      beq    end2
      ldc.i4.1
      stloc r

    end2:
      
      .try
      {
        call int64 Overflow::test3()
        call void [System.Console]System.Console::WriteLine(int64)
        ldc.i4.1
        stloc r
        leave.s end3
      }
      catch [mscorlib]System.OverflowException
      {
        pop
        ldstr "OverflowException correctly thrown"
        call void [System.Console]System.Console::WriteLine(string)
        leave.s end3
      }

    end3:

      ldloc r
      ret
  } 

  .method private hidebysig static int64
          test1() cil managed noinlining
  {
        ldc.i8     0x8000000000000000
        conv.u8
        ldc.i4.m1
        conv.i8
        mul.ovf
        ret
  }

  .method private hidebysig static int64
          test2() cil managed noinlining
  {
        ldc.i8     0x8000000000000000
        conv.u8
        ldc.i4.m1
        conv.i8
        mul
        ret
  }

  .method private hidebysig static int64
          test3() cil managed noinlining
  {
        ldc.i8     0x8000000000000000
        conv.u8
        ldc.i4.m1
        conv.i8
        div
        ret
  }
} 
