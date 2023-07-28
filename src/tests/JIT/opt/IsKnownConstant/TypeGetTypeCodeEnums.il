// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }

.assembly TypeGetTypeCodeEnums { }

.class public auto ansi sealed CharEnum
       extends [System.Runtime]System.Enum
{
  .field public specialname rtspecialname char value__
} // end of class CharEnum

.class public auto ansi sealed BoolEnum
       extends [System.Runtime]System.Enum
{
  .field public specialname rtspecialname bool value__
} // end of class BoolEnum

.class public auto ansi sealed FloatEnum
       extends [System.Runtime]System.Enum
{
  .field public specialname rtspecialname float32 value__
} // end of class FloatEnum

.class public auto ansi sealed DoubleEnum
       extends [System.Runtime]System.Enum
{
  .field public specialname rtspecialname float64 value__
} // end of class DoubleEnum

.class public auto ansi sealed IntPtrEnum
       extends [System.Runtime]System.Enum
{
  .field public specialname rtspecialname native int value__
} // end of class IntPtrEnum

.class public auto ansi sealed UIntPtrEnum
       extends [System.Runtime]System.Enum
{
  .field public specialname rtspecialname native uint value__
} // end of class UIntPtrEnum
