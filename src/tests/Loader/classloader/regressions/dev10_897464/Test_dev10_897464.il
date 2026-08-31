// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { }
.assembly extern mscorlib
{
}

.assembly Test_dev10_897464
{
  .hash algorithm 0x00008004
  .ver 1:0:0:0
}

.module Test.dll
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY

.field public static literal valuetype Test.MyEnum LiteralValue = int32(0x00000001)

.class public auto ansi sealed Test.MyEnum
       extends [mscorlib]System.Enum
{
  .field public specialname rtspecialname int32 value__
  .field public static literal valuetype Test.MyEnum Zero = int32(0x00000000)
  .field public static literal valuetype Test.MyEnum One = int32(0x00000001)
}


// Just here so we can reference something
/*.class interface public abstract auto ansi Test.IEmpty
{
}
*/
