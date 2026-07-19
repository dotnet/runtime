// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime
{
}

.assembly ShiftTests
{
}

.class public auto ansi beforefieldinit ShiftTestsType
       extends [System.Runtime]System.Object
{
  .method public hidebysig instance int32 ShiftLeft.Int32ByInt32_Valid(int32 toBeShifted, int32 shiftBy) cil managed
  {
     ldarg.1
     ldarg.2
     shl
     ret
  }

  .method public hidebysig instance int32 ShiftRight.Int32ByInt32_Valid(int32 toBeShifted, int32 shiftBy) cil managed
  {
     ldarg.1
     ldarg.2
     shr
     ret
  }

  .method public hidebysig instance int32 ShiftRightUn.Int32ByInt32_Valid(int32 toBeShifted, int32 shiftBy) cil managed
  {
     ldarg.1
     ldarg.2
     shr.un
     ret
  }

  .method public hidebysig instance int32 ShiftLeft.Int32ByNativeInt_Valid(int32 toBeShifted, native int shiftBy) cil managed
  {
     ldarg.1
     ldarg.2
     shl
     ret
  }

  .method public hidebysig instance int64 ShiftLeft.Int64ByInt32_Valid(int64 toBeShifted, int32 shiftBy) cil managed
  {
     ldarg.1
     ldarg.2
     shl
     ret
  }

  .method public hidebysig instance int64 ShiftLeft.Int64ByNativeInt_Valid(int64 toBeShifted, native int shiftBy) cil managed
  {
     ldarg.1
     ldarg.2
     shl
     ret
  }

  .method public hidebysig instance native int ShiftLeft.NativeIntByInt32_Valid(native int toBeShifted, int32 shiftBy) cil managed
  {
     ldarg.1
     ldarg.2
     shl
     ret
  }

  .method public hidebysig instance native int ShiftLeft.NativeIntByNativeInt_Valid(native int toBeShifted, native int shiftBy) cil managed
  {
     ldarg.1
     ldarg.2
     shl
     ret
  }

  .method public hidebysig instance int32 ShiftLeft.Int32ByInt64_Invalid_StackUnexpected(int32 toBeShifted, int64 shiftBy) cil managed
  {
     ldarg.1
     ldarg.2
     shl
     ret
  }

  .method public hidebysig instance int32 ShiftLeft.Int32ByFloat64_Invalid_StackUnexpected(int32 toBeShifted, float64 shiftBy) cil managed
  {
     ldarg.1
     ldarg.2
     shl
     ret
  }

  .method public hidebysig instance int32 ShiftLeft.Int32ByInt32Ref_Invalid_StackUnexpected(int32 toBeShifted, int32& shiftBy) cil managed
  {
     ldarg.1
     ldarg.2
     shl
     ret
  }

  .method public hidebysig instance int32 ShiftLeft.Int32ByObject_Invalid_StackUnexpected(int32 toBeShifted, object shiftBy) cil managed
  {
     ldarg.1
     ldarg.2
     shl
     ret
  }

    .method public hidebysig instance int64 ShiftLeft.Int64ByInt64_Invalid_StackUnexpected(int64 toBeShifted, int64 shiftBy) cil managed
  {
     ldarg.1
     ldarg.2
     shl
     ret
  }

  .method public hidebysig instance int64 ShiftLeft.Int64ByFloat64_Invalid_StackUnexpected(int64 toBeShifted, float64 shiftBy) cil managed
  {
     ldarg.1
     ldarg.2
     shl
     ret
  }

  .method public hidebysig instance int64 ShiftLeft.Int64ByInt32Ref_Invalid_StackUnexpected(int64 toBeShifted, int32& shiftBy) cil managed
  {
     ldarg.1
     ldarg.2
     shl
     ret
  }

  .method public hidebysig instance int64 ShiftLeft.Int64ByObject_Invalid_StackUnexpected(int64 toBeShifted, object shiftBy) cil managed
  {
     ldarg.1
     ldarg.2
     shl
     ret
  }

  .method public hidebysig instance native int ShiftLeft.NativeIntByInt64_Invalid_StackUnexpected(native int toBeShifted, int64 shiftBy) cil managed
  {
     ldarg.1
     ldarg.2
     shl
     ret
  }

  .method public hidebysig instance native int ShiftLeft.NativeIntByFloat64_Invalid_StackUnexpected(native int toBeShifted, float64 shiftBy) cil managed
  {
     ldarg.1
     ldarg.2
     shl
     ret
  }

  .method public hidebysig instance native int ShiftLeft.NativeIntByInt32Ref_Invalid_StackUnexpected(native int toBeShifted, int32& shiftBy) cil managed
  {
     ldarg.1
     ldarg.2
     shl
     ret
  }

  .method public hidebysig instance native int ShiftLeft.NativeIntByObject_Invalid_StackUnexpected(native int toBeShifted, object shiftBy) cil managed
  {
     ldarg.1
     ldarg.2
     shl
     ret
  }

  .method public hidebysig instance float64 ShiftLeft.Float64ByInt32_Invalid_ExpectedIntegerType(float64 toBeShifted, int32 shiftBy) cil managed
  {
     ldarg.1
     ldarg.2
     shl
     ret
  }

  .method public hidebysig instance int32& ShiftLeft.Int32RefByInt32_Invalid_ExpectedIntegerType(int32& toBeShifted, int32 shiftBy) cil managed
  {
     ldarg.1
     ldarg.2
     shl
     ret
  }

  .method public hidebysig instance object ShiftLeft.ObjectByInt32_Invalid_ExpectedIntegerType(object toBeShifted, int32 shiftBy) cil managed
  {
     ldarg.1
     ldarg.2
     shl
     ret
  }
}
