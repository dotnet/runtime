// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


.assembly extern mscorlib { auto }
.assembly extern xunit.core {}
.assembly extern System.Console { auto }
.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }

.assembly 'ldind_conv' { }

.class public auto Program extends [System.Runtime]System.Object
{
  .method private static void print(class [System.Runtime]System.String) cil managed
  {
    .maxstack 1
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
    .maxstack 8

    ldc.i4 0x0000007F
    ldc.i4 0x0000007F
    ldstr "Checking ldind_i1_conv_i1(0x7F) == 0x0000007F"
    call int32 Program::Check_ldind_i1_conv_i1(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFFFF80
    ldc.i4 0xFFFFFF80
    ldstr "Checking ldind_i1_conv_i1(0x80) == 0xFFFFFF80"
    call int32 Program::Check_ldind_i1_conv_i1(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x000000FF
    ldc.i4 0xFFFFFFFF
    ldstr "Checking ldind_i1_conv_i1(0xFF) == 0xFFFFFFFF"
    call int32 Program::Check_ldind_i1_conv_i1(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000007F
    ldc.i4 0x0000007F
    ldstr "Checking ldind_i1_conv_ovf_i1(0x7F) == 0x0000007F"
    call int32 Program::Check_ldind_i1_conv_ovf_i1(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFFFF80
    ldc.i4 0xFFFFFF80
    ldstr "Checking ldind_i1_conv_ovf_i1(0x80) == 0xFFFFFF80"
    call int32 Program::Check_ldind_i1_conv_ovf_i1(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x000000FF
    ldc.i4 0xFFFFFFFF
    ldstr "Checking ldind_i1_conv_ovf_i1(0xFF) == 0xFFFFFFFF"
    call int32 Program::Check_ldind_i1_conv_ovf_i1(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000007F
    ldc.i4 0x0000007F
    ldstr "Checking ldind_i1_conv_ovf_i1_un(0x7F) == 0x0000007F"
    call int32 Program::Check_ldind_i1_conv_ovf_i1_un(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFFFF80
    ldstr "Checking ldind_i1_conv_ovf_i1_un(0x80) == OverflowException"
    call int32 Program::CheckOvf_ldind_i1_conv_ovf_i1_un(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x000000FF
    ldstr "Checking ldind_i1_conv_ovf_i1_un(0xFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_i1_conv_ovf_i1_un(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000007F
    ldc.i4 0x0000007F
    ldstr "Checking ldind_i1_conv_u1(0x7F) == 0x0000007F"
    call int32 Program::Check_ldind_i1_conv_u1(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFFFF80
    ldc.i4 0x00000080
    ldstr "Checking ldind_i1_conv_u1(0x80) == 0x00000080"
    call int32 Program::Check_ldind_i1_conv_u1(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x000000FF
    ldc.i4 0x000000FF
    ldstr "Checking ldind_i1_conv_u1(0xFF) == 0x000000FF"
    call int32 Program::Check_ldind_i1_conv_u1(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000007F
    ldc.i4 0x0000007F
    ldstr "Checking ldind_i1_conv_ovf_u1(0x7F) == 0x0000007F"
    call int32 Program::Check_ldind_i1_conv_ovf_u1(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFFFF80
    ldstr "Checking ldind_i1_conv_ovf_u1(0x80) == OverflowException"
    call int32 Program::CheckOvf_ldind_i1_conv_ovf_u1(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x000000FF
    ldstr "Checking ldind_i1_conv_ovf_u1(0xFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_i1_conv_ovf_u1(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000007F
    ldc.i4 0x0000007F
    ldstr "Checking ldind_i1_conv_ovf_u1_un(0x7F) == 0x0000007F"
    call int32 Program::Check_ldind_i1_conv_ovf_u1_un(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFFFF80
    ldstr "Checking ldind_i1_conv_ovf_u1_un(0x80) == OverflowException"
    call int32 Program::CheckOvf_ldind_i1_conv_ovf_u1_un(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x000000FF
    ldstr "Checking ldind_i1_conv_ovf_u1_un(0xFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_i1_conv_ovf_u1_un(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000007F
    ldc.i4 0x0000007F
    ldstr "Checking ldind_i1_conv_i2(0x7F) == 0x0000007F"
    call int32 Program::Check_ldind_i1_conv_i2(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFFFF80
    ldc.i4 0xFFFFFF80
    ldstr "Checking ldind_i1_conv_i2(0x80) == 0xFFFFFF80"
    call int32 Program::Check_ldind_i1_conv_i2(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x000000FF
    ldc.i4 0xFFFFFFFF
    ldstr "Checking ldind_i1_conv_i2(0xFF) == 0xFFFFFFFF"
    call int32 Program::Check_ldind_i1_conv_i2(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000007F
    ldc.i4 0x0000007F
    ldstr "Checking ldind_i1_conv_ovf_i2(0x7F) == 0x0000007F"
    call int32 Program::Check_ldind_i1_conv_ovf_i2(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFFFF80
    ldc.i4 0xFFFFFF80
    ldstr "Checking ldind_i1_conv_ovf_i2(0x80) == 0xFFFFFF80"
    call int32 Program::Check_ldind_i1_conv_ovf_i2(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x000000FF
    ldc.i4 0xFFFFFFFF
    ldstr "Checking ldind_i1_conv_ovf_i2(0xFF) == 0xFFFFFFFF"
    call int32 Program::Check_ldind_i1_conv_ovf_i2(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000007F
    ldc.i4 0x0000007F
    ldstr "Checking ldind_i1_conv_ovf_i2_un(0x7F) == 0x0000007F"
    call int32 Program::Check_ldind_i1_conv_ovf_i2_un(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFFFF80
    ldstr "Checking ldind_i1_conv_ovf_i2_un(0x80) == OverflowException"
    call int32 Program::CheckOvf_ldind_i1_conv_ovf_i2_un(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x000000FF
    ldstr "Checking ldind_i1_conv_ovf_i2_un(0xFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_i1_conv_ovf_i2_un(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000007F
    ldc.i4 0x0000007F
    ldstr "Checking ldind_i1_conv_u2(0x7F) == 0x0000007F"
    call int32 Program::Check_ldind_i1_conv_u2(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFFFF80
    ldc.i4 0x0000FF80
    ldstr "Checking ldind_i1_conv_u2(0x80) == 0x0000FF80"
    call int32 Program::Check_ldind_i1_conv_u2(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x000000FF
    ldc.i4 0x0000FFFF
    ldstr "Checking ldind_i1_conv_u2(0xFF) == 0x0000FFFF"
    call int32 Program::Check_ldind_i1_conv_u2(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000007F
    ldc.i4 0x0000007F
    ldstr "Checking ldind_i1_conv_ovf_u2(0x7F) == 0x0000007F"
    call int32 Program::Check_ldind_i1_conv_ovf_u2(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFFFF80
    ldstr "Checking ldind_i1_conv_ovf_u2(0x80) == OverflowException"
    call int32 Program::CheckOvf_ldind_i1_conv_ovf_u2(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x000000FF
    ldstr "Checking ldind_i1_conv_ovf_u2(0xFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_i1_conv_ovf_u2(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000007F
    ldc.i4 0x0000007F
    ldstr "Checking ldind_i1_conv_ovf_u2_un(0x7F) == 0x0000007F"
    call int32 Program::Check_ldind_i1_conv_ovf_u2_un(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFFFF80
    ldstr "Checking ldind_i1_conv_ovf_u2_un(0x80) == OverflowException"
    call int32 Program::CheckOvf_ldind_i1_conv_ovf_u2_un(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x000000FF
    ldstr "Checking ldind_i1_conv_ovf_u2_un(0xFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_i1_conv_ovf_u2_un(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000007F
    ldc.i4 0x0000007F
    ldstr "Checking ldind_i1_conv_i4(0x7F) == 0x0000007F"
    call int32 Program::Check_ldind_i1_conv_i4(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFFFF80
    ldc.i4 0xFFFFFF80
    ldstr "Checking ldind_i1_conv_i4(0x80) == 0xFFFFFF80"
    call int32 Program::Check_ldind_i1_conv_i4(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x000000FF
    ldc.i4 0xFFFFFFFF
    ldstr "Checking ldind_i1_conv_i4(0xFF) == 0xFFFFFFFF"
    call int32 Program::Check_ldind_i1_conv_i4(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000007F
    ldc.i4 0x0000007F
    ldstr "Checking ldind_i1_conv_ovf_i4(0x7F) == 0x0000007F"
    call int32 Program::Check_ldind_i1_conv_ovf_i4(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFFFF80
    ldc.i4 0xFFFFFF80
    ldstr "Checking ldind_i1_conv_ovf_i4(0x80) == 0xFFFFFF80"
    call int32 Program::Check_ldind_i1_conv_ovf_i4(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x000000FF
    ldc.i4 0xFFFFFFFF
    ldstr "Checking ldind_i1_conv_ovf_i4(0xFF) == 0xFFFFFFFF"
    call int32 Program::Check_ldind_i1_conv_ovf_i4(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000007F
    ldc.i4 0x0000007F
    ldstr "Checking ldind_i1_conv_ovf_i4_un(0x7F) == 0x0000007F"
    call int32 Program::Check_ldind_i1_conv_ovf_i4_un(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFFFF80
    ldstr "Checking ldind_i1_conv_ovf_i4_un(0x80) == OverflowException"
    call int32 Program::CheckOvf_ldind_i1_conv_ovf_i4_un(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x000000FF
    ldstr "Checking ldind_i1_conv_ovf_i4_un(0xFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_i1_conv_ovf_i4_un(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000007F
    ldc.i4 0x0000007F
    ldstr "Checking ldind_i1_conv_u4(0x7F) == 0x0000007F"
    call int32 Program::Check_ldind_i1_conv_u4(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFFFF80
    ldc.i4 0xFFFFFF80
    ldstr "Checking ldind_i1_conv_u4(0x80) == 0xFFFFFF80"
    call int32 Program::Check_ldind_i1_conv_u4(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x000000FF
    ldc.i4 0xFFFFFFFF
    ldstr "Checking ldind_i1_conv_u4(0xFF) == 0xFFFFFFFF"
    call int32 Program::Check_ldind_i1_conv_u4(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000007F
    ldc.i4 0x0000007F
    ldstr "Checking ldind_i1_conv_ovf_u4(0x7F) == 0x0000007F"
    call int32 Program::Check_ldind_i1_conv_ovf_u4(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFFFF80
    ldstr "Checking ldind_i1_conv_ovf_u4(0x80) == OverflowException"
    call int32 Program::CheckOvf_ldind_i1_conv_ovf_u4(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x000000FF
    ldstr "Checking ldind_i1_conv_ovf_u4(0xFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_i1_conv_ovf_u4(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000007F
    ldc.i4 0x0000007F
    ldstr "Checking ldind_i1_conv_ovf_u4_un(0x7F) == 0x0000007F"
    call int32 Program::Check_ldind_i1_conv_ovf_u4_un(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFFFF80
    ldc.i4 0xFFFFFF80
    ldstr "Checking ldind_i1_conv_ovf_u4_un(0x80) == 0xFFFFFF80"
    call int32 Program::Check_ldind_i1_conv_ovf_u4_un(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x000000FF
    ldc.i4 0xFFFFFFFF
    ldstr "Checking ldind_i1_conv_ovf_u4_un(0xFF) == 0xFFFFFFFF"
    call int32 Program::Check_ldind_i1_conv_ovf_u4_un(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000007F
    ldc.i8 0x000000000000007F
    ldstr "Checking ldind_i1_conv_i8(0x7F) == 0x000000000000007F"
    call int32 Program::Check_ldind_i1_conv_i8(int32, int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFFFF80
    ldc.i8 0xFFFFFFFFFFFFFF80
    ldstr "Checking ldind_i1_conv_i8(0x80) == 0xFFFFFFFFFFFFFF80"
    call int32 Program::Check_ldind_i1_conv_i8(int32, int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x000000FF
    ldc.i8 0xFFFFFFFFFFFFFFFF
    ldstr "Checking ldind_i1_conv_i8(0xFF) == 0xFFFFFFFFFFFFFFFF"
    call int32 Program::Check_ldind_i1_conv_i8(int32, int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000007F
    ldc.i8 0x000000000000007F
    ldstr "Checking ldind_i1_conv_ovf_i8(0x7F) == 0x000000000000007F"
    call int32 Program::Check_ldind_i1_conv_ovf_i8(int32, int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFFFF80
    ldc.i8 0xFFFFFFFFFFFFFF80
    ldstr "Checking ldind_i1_conv_ovf_i8(0x80) == 0xFFFFFFFFFFFFFF80"
    call int32 Program::Check_ldind_i1_conv_ovf_i8(int32, int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x000000FF
    ldc.i8 0xFFFFFFFFFFFFFFFF
    ldstr "Checking ldind_i1_conv_ovf_i8(0xFF) == 0xFFFFFFFFFFFFFFFF"
    call int32 Program::Check_ldind_i1_conv_ovf_i8(int32, int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000007F
    ldc.i8 0x000000000000007F
    ldstr "Checking ldind_i1_conv_ovf_i8_un(0x7F) == 0x000000000000007F"
    call int32 Program::Check_ldind_i1_conv_ovf_i8_un(int32, int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFFFF80
    ldc.i8 0x00000000FFFFFF80
    ldstr "Checking ldind_i1_conv_ovf_i8_un(0x80) == 0x00000000FFFFFF80"
    call int32 Program::Check_ldind_i1_conv_ovf_i8_un(int32, int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x000000FF
    ldc.i8 0x00000000FFFFFFFF
    ldstr "Checking ldind_i1_conv_ovf_i8_un(0xFF) == 0x00000000FFFFFFFF"
    call int32 Program::Check_ldind_i1_conv_ovf_i8_un(int32, int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000007F
    ldc.i8 0x000000000000007F
    ldstr "Checking ldind_i1_conv_u8(0x7F) == 0x000000000000007F"
    call int32 Program::Check_ldind_i1_conv_u8(int32, int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFFFF80
    ldc.i8 0x00000000FFFFFF80
    ldstr "Checking ldind_i1_conv_u8(0x80) == 0x00000000FFFFFF80"
    call int32 Program::Check_ldind_i1_conv_u8(int32, int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x000000FF
    ldc.i8 0x00000000FFFFFFFF
    ldstr "Checking ldind_i1_conv_u8(0xFF) == 0x00000000FFFFFFFF"
    call int32 Program::Check_ldind_i1_conv_u8(int32, int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000007F
    ldc.i8 0x000000000000007F
    ldstr "Checking ldind_i1_conv_ovf_u8(0x7F) == 0x000000000000007F"
    call int32 Program::Check_ldind_i1_conv_ovf_u8(int32, int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFFFF80
    ldstr "Checking ldind_i1_conv_ovf_u8(0x80) == OverflowException"
    call int32 Program::CheckOvf_ldind_i1_conv_ovf_u8(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x000000FF
    ldstr "Checking ldind_i1_conv_ovf_u8(0xFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_i1_conv_ovf_u8(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000007F
    ldc.i8 0x000000000000007F
    ldstr "Checking ldind_i1_conv_ovf_u8_un(0x7F) == 0x000000000000007F"
    call int32 Program::Check_ldind_i1_conv_ovf_u8_un(int32, int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFFFF80
    ldc.i8 0x00000000FFFFFF80
    ldstr "Checking ldind_i1_conv_ovf_u8_un(0x80) == 0x00000000FFFFFF80"
    call int32 Program::Check_ldind_i1_conv_ovf_u8_un(int32, int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x000000FF
    ldc.i8 0x00000000FFFFFFFF
    ldstr "Checking ldind_i1_conv_ovf_u8_un(0xFF) == 0x00000000FFFFFFFF"
    call int32 Program::Check_ldind_i1_conv_ovf_u8_un(int32, int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x000000FF
    ldc.i4 0xFFFFFFFF
    ldstr "Checking ldind_u1_conv_i1(0xFF) == 0xFFFFFFFF"
    call int32 Program::Check_ldind_u1_conv_i1(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x000000FF
    ldstr "Checking ldind_u1_conv_ovf_i1(0xFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_u1_conv_ovf_i1(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x000000FF
    ldstr "Checking ldind_u1_conv_ovf_i1_un(0xFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_u1_conv_ovf_i1_un(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x000000FF
    ldc.i4 0x000000FF
    ldstr "Checking ldind_u1_conv_u1(0xFF) == 0x000000FF"
    call int32 Program::Check_ldind_u1_conv_u1(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x000000FF
    ldc.i4 0x000000FF
    ldstr "Checking ldind_u1_conv_ovf_u1(0xFF) == 0x000000FF"
    call int32 Program::Check_ldind_u1_conv_ovf_u1(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x000000FF
    ldc.i4 0x000000FF
    ldstr "Checking ldind_u1_conv_ovf_u1_un(0xFF) == 0x000000FF"
    call int32 Program::Check_ldind_u1_conv_ovf_u1_un(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x000000FF
    ldc.i4 0x000000FF
    ldstr "Checking ldind_u1_conv_i2(0xFF) == 0x000000FF"
    call int32 Program::Check_ldind_u1_conv_i2(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x000000FF
    ldc.i4 0x000000FF
    ldstr "Checking ldind_u1_conv_ovf_i2(0xFF) == 0x000000FF"
    call int32 Program::Check_ldind_u1_conv_ovf_i2(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x000000FF
    ldc.i4 0x000000FF
    ldstr "Checking ldind_u1_conv_ovf_i2_un(0xFF) == 0x000000FF"
    call int32 Program::Check_ldind_u1_conv_ovf_i2_un(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x000000FF
    ldc.i4 0x000000FF
    ldstr "Checking ldind_u1_conv_u2(0xFF) == 0x000000FF"
    call int32 Program::Check_ldind_u1_conv_u2(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x000000FF
    ldc.i4 0x000000FF
    ldstr "Checking ldind_u1_conv_ovf_u2(0xFF) == 0x000000FF"
    call int32 Program::Check_ldind_u1_conv_ovf_u2(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x000000FF
    ldc.i4 0x000000FF
    ldstr "Checking ldind_u1_conv_ovf_u2_un(0xFF) == 0x000000FF"
    call int32 Program::Check_ldind_u1_conv_ovf_u2_un(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x000000FF
    ldc.i4 0x000000FF
    ldstr "Checking ldind_u1_conv_i4(0xFF) == 0x000000FF"
    call int32 Program::Check_ldind_u1_conv_i4(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x000000FF
    ldc.i4 0x000000FF
    ldstr "Checking ldind_u1_conv_ovf_i4(0xFF) == 0x000000FF"
    call int32 Program::Check_ldind_u1_conv_ovf_i4(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x000000FF
    ldc.i4 0x000000FF
    ldstr "Checking ldind_u1_conv_ovf_i4_un(0xFF) == 0x000000FF"
    call int32 Program::Check_ldind_u1_conv_ovf_i4_un(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x000000FF
    ldc.i4 0x000000FF
    ldstr "Checking ldind_u1_conv_u4(0xFF) == 0x000000FF"
    call int32 Program::Check_ldind_u1_conv_u4(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x000000FF
    ldc.i4 0x000000FF
    ldstr "Checking ldind_u1_conv_ovf_u4(0xFF) == 0x000000FF"
    call int32 Program::Check_ldind_u1_conv_ovf_u4(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x000000FF
    ldc.i4 0x000000FF
    ldstr "Checking ldind_u1_conv_ovf_u4_un(0xFF) == 0x000000FF"
    call int32 Program::Check_ldind_u1_conv_ovf_u4_un(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x000000FF
    ldc.i8 0x00000000000000FF
    ldstr "Checking ldind_u1_conv_i8(0xFF) == 0x00000000000000FF"
    call int32 Program::Check_ldind_u1_conv_i8(int32, int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x000000FF
    ldc.i8 0x00000000000000FF
    ldstr "Checking ldind_u1_conv_ovf_i8(0xFF) == 0x00000000000000FF"
    call int32 Program::Check_ldind_u1_conv_ovf_i8(int32, int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x000000FF
    ldc.i8 0x00000000000000FF
    ldstr "Checking ldind_u1_conv_ovf_i8_un(0xFF) == 0x00000000000000FF"
    call int32 Program::Check_ldind_u1_conv_ovf_i8_un(int32, int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x000000FF
    ldc.i8 0x00000000000000FF
    ldstr "Checking ldind_u1_conv_u8(0xFF) == 0x00000000000000FF"
    call int32 Program::Check_ldind_u1_conv_u8(int32, int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x000000FF
    ldc.i8 0x00000000000000FF
    ldstr "Checking ldind_u1_conv_ovf_u8(0xFF) == 0x00000000000000FF"
    call int32 Program::Check_ldind_u1_conv_ovf_u8(int32, int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x000000FF
    ldc.i8 0x00000000000000FF
    ldstr "Checking ldind_u1_conv_ovf_u8_un(0xFF) == 0x00000000000000FF"
    call int32 Program::Check_ldind_u1_conv_ovf_u8_un(int32, int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x00007FFF
    ldc.i4 0xFFFFFFFF
    ldstr "Checking ldind_i2_conv_i1(0x7FFF) == 0xFFFFFFFF"
    call int32 Program::Check_ldind_i2_conv_i1(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFF8000
    ldc.i4 0x00000000
    ldstr "Checking ldind_i2_conv_i1(0x8000) == 0x00000000"
    call int32 Program::Check_ldind_i2_conv_i1(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000FFFF
    ldc.i4 0xFFFFFFFF
    ldstr "Checking ldind_i2_conv_i1(0xFFFF) == 0xFFFFFFFF"
    call int32 Program::Check_ldind_i2_conv_i1(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000007F
    ldc.i4 0x0000007F
    ldstr "Checking ldind_i2_conv_i1(0x007F) == 0x0000007F"
    call int32 Program::Check_ldind_i2_conv_i1(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFFFF80
    ldc.i4 0xFFFFFF80
    ldstr "Checking ldind_i2_conv_i1(0xFF80) == 0xFFFFFF80"
    call int32 Program::Check_ldind_i2_conv_i1(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x000000FF
    ldc.i4 0xFFFFFFFF
    ldstr "Checking ldind_i2_conv_i1(0x00FF) == 0xFFFFFFFF"
    call int32 Program::Check_ldind_i2_conv_i1(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x00007FFF
    ldstr "Checking ldind_i2_conv_ovf_i1(0x7FFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_i2_conv_ovf_i1(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFF8000
    ldstr "Checking ldind_i2_conv_ovf_i1(0x8000) == OverflowException"
    call int32 Program::CheckOvf_ldind_i2_conv_ovf_i1(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000FFFF
    ldc.i4 0xFFFFFFFF
    ldstr "Checking ldind_i2_conv_ovf_i1(0xFFFF) == 0xFFFFFFFF"
    call int32 Program::Check_ldind_i2_conv_ovf_i1(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000007F
    ldc.i4 0x0000007F
    ldstr "Checking ldind_i2_conv_ovf_i1(0x007F) == 0x0000007F"
    call int32 Program::Check_ldind_i2_conv_ovf_i1(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFFFF80
    ldc.i4 0xFFFFFF80
    ldstr "Checking ldind_i2_conv_ovf_i1(0xFF80) == 0xFFFFFF80"
    call int32 Program::Check_ldind_i2_conv_ovf_i1(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x000000FF
    ldstr "Checking ldind_i2_conv_ovf_i1(0x00FF) == OverflowException"
    call int32 Program::CheckOvf_ldind_i2_conv_ovf_i1(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x00007FFF
    ldstr "Checking ldind_i2_conv_ovf_i1_un(0x7FFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_i2_conv_ovf_i1_un(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFF8000
    ldstr "Checking ldind_i2_conv_ovf_i1_un(0x8000) == OverflowException"
    call int32 Program::CheckOvf_ldind_i2_conv_ovf_i1_un(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000FFFF
    ldstr "Checking ldind_i2_conv_ovf_i1_un(0xFFFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_i2_conv_ovf_i1_un(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000007F
    ldc.i4 0x0000007F
    ldstr "Checking ldind_i2_conv_ovf_i1_un(0x007F) == 0x0000007F"
    call int32 Program::Check_ldind_i2_conv_ovf_i1_un(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFFFF80
    ldstr "Checking ldind_i2_conv_ovf_i1_un(0xFF80) == OverflowException"
    call int32 Program::CheckOvf_ldind_i2_conv_ovf_i1_un(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x000000FF
    ldstr "Checking ldind_i2_conv_ovf_i1_un(0x00FF) == OverflowException"
    call int32 Program::CheckOvf_ldind_i2_conv_ovf_i1_un(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x00007FFF
    ldc.i4 0x000000FF
    ldstr "Checking ldind_i2_conv_u1(0x7FFF) == 0x000000FF"
    call int32 Program::Check_ldind_i2_conv_u1(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFF8000
    ldc.i4 0x00000000
    ldstr "Checking ldind_i2_conv_u1(0x8000) == 0x00000000"
    call int32 Program::Check_ldind_i2_conv_u1(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000FFFF
    ldc.i4 0x000000FF
    ldstr "Checking ldind_i2_conv_u1(0xFFFF) == 0x000000FF"
    call int32 Program::Check_ldind_i2_conv_u1(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x000000FF
    ldc.i4 0x000000FF
    ldstr "Checking ldind_i2_conv_u1(0x00FF) == 0x000000FF"
    call int32 Program::Check_ldind_i2_conv_u1(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x00007FFF
    ldstr "Checking ldind_i2_conv_ovf_u1(0x7FFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_i2_conv_ovf_u1(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFF8000
    ldstr "Checking ldind_i2_conv_ovf_u1(0x8000) == OverflowException"
    call int32 Program::CheckOvf_ldind_i2_conv_ovf_u1(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000FFFF
    ldstr "Checking ldind_i2_conv_ovf_u1(0xFFFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_i2_conv_ovf_u1(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x000000FF
    ldc.i4 0x000000FF
    ldstr "Checking ldind_i2_conv_ovf_u1(0x00FF) == 0x000000FF"
    call int32 Program::Check_ldind_i2_conv_ovf_u1(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x00007FFF
    ldstr "Checking ldind_i2_conv_ovf_u1_un(0x7FFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_i2_conv_ovf_u1_un(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFF8000
    ldstr "Checking ldind_i2_conv_ovf_u1_un(0x8000) == OverflowException"
    call int32 Program::CheckOvf_ldind_i2_conv_ovf_u1_un(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000FFFF
    ldstr "Checking ldind_i2_conv_ovf_u1_un(0xFFFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_i2_conv_ovf_u1_un(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x000000FF
    ldc.i4 0x000000FF
    ldstr "Checking ldind_i2_conv_ovf_u1_un(0x00FF) == 0x000000FF"
    call int32 Program::Check_ldind_i2_conv_ovf_u1_un(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x00007FFF
    ldc.i4 0x00007FFF
    ldstr "Checking ldind_i2_conv_i2(0x7FFF) == 0x00007FFF"
    call int32 Program::Check_ldind_i2_conv_i2(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFF8000
    ldc.i4 0xFFFF8000
    ldstr "Checking ldind_i2_conv_i2(0x8000) == 0xFFFF8000"
    call int32 Program::Check_ldind_i2_conv_i2(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000FFFF
    ldc.i4 0xFFFFFFFF
    ldstr "Checking ldind_i2_conv_i2(0xFFFF) == 0xFFFFFFFF"
    call int32 Program::Check_ldind_i2_conv_i2(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x00007FFF
    ldc.i4 0x00007FFF
    ldstr "Checking ldind_i2_conv_ovf_i2(0x7FFF) == 0x00007FFF"
    call int32 Program::Check_ldind_i2_conv_ovf_i2(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFF8000
    ldc.i4 0xFFFF8000
    ldstr "Checking ldind_i2_conv_ovf_i2(0x8000) == 0xFFFF8000"
    call int32 Program::Check_ldind_i2_conv_ovf_i2(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000FFFF
    ldc.i4 0xFFFFFFFF
    ldstr "Checking ldind_i2_conv_ovf_i2(0xFFFF) == 0xFFFFFFFF"
    call int32 Program::Check_ldind_i2_conv_ovf_i2(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x00007FFF
    ldc.i4 0x00007FFF
    ldstr "Checking ldind_i2_conv_ovf_i2_un(0x7FFF) == 0x00007FFF"
    call int32 Program::Check_ldind_i2_conv_ovf_i2_un(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFF8000
    ldstr "Checking ldind_i2_conv_ovf_i2_un(0x8000) == OverflowException"
    call int32 Program::CheckOvf_ldind_i2_conv_ovf_i2_un(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000FFFF
    ldstr "Checking ldind_i2_conv_ovf_i2_un(0xFFFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_i2_conv_ovf_i2_un(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x00007FFF
    ldc.i4 0x00007FFF
    ldstr "Checking ldind_i2_conv_u2(0x7FFF) == 0x00007FFF"
    call int32 Program::Check_ldind_i2_conv_u2(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFF8000
    ldc.i4 0x00008000
    ldstr "Checking ldind_i2_conv_u2(0x8000) == 0x00008000"
    call int32 Program::Check_ldind_i2_conv_u2(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000FFFF
    ldc.i4 0x0000FFFF
    ldstr "Checking ldind_i2_conv_u2(0xFFFF) == 0x0000FFFF"
    call int32 Program::Check_ldind_i2_conv_u2(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x00007FFF
    ldc.i4 0x00007FFF
    ldstr "Checking ldind_i2_conv_ovf_u2(0x7FFF) == 0x00007FFF"
    call int32 Program::Check_ldind_i2_conv_ovf_u2(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFF8000
    ldstr "Checking ldind_i2_conv_ovf_u2(0x8000) == OverflowException"
    call int32 Program::CheckOvf_ldind_i2_conv_ovf_u2(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000FFFF
    ldstr "Checking ldind_i2_conv_ovf_u2(0xFFFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_i2_conv_ovf_u2(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x00007FFF
    ldc.i4 0x00007FFF
    ldstr "Checking ldind_i2_conv_ovf_u2_un(0x7FFF) == 0x00007FFF"
    call int32 Program::Check_ldind_i2_conv_ovf_u2_un(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFF8000
    ldstr "Checking ldind_i2_conv_ovf_u2_un(0x8000) == OverflowException"
    call int32 Program::CheckOvf_ldind_i2_conv_ovf_u2_un(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000FFFF
    ldstr "Checking ldind_i2_conv_ovf_u2_un(0xFFFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_i2_conv_ovf_u2_un(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x00007FFF
    ldc.i4 0x00007FFF
    ldstr "Checking ldind_i2_conv_i4(0x7FFF) == 0x00007FFF"
    call int32 Program::Check_ldind_i2_conv_i4(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFF8000
    ldc.i4 0xFFFF8000
    ldstr "Checking ldind_i2_conv_i4(0x8000) == 0xFFFF8000"
    call int32 Program::Check_ldind_i2_conv_i4(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000FFFF
    ldc.i4 0xFFFFFFFF
    ldstr "Checking ldind_i2_conv_i4(0xFFFF) == 0xFFFFFFFF"
    call int32 Program::Check_ldind_i2_conv_i4(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x00007FFF
    ldc.i4 0x00007FFF
    ldstr "Checking ldind_i2_conv_ovf_i4(0x7FFF) == 0x00007FFF"
    call int32 Program::Check_ldind_i2_conv_ovf_i4(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFF8000
    ldc.i4 0xFFFF8000
    ldstr "Checking ldind_i2_conv_ovf_i4(0x8000) == 0xFFFF8000"
    call int32 Program::Check_ldind_i2_conv_ovf_i4(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000FFFF
    ldc.i4 0xFFFFFFFF
    ldstr "Checking ldind_i2_conv_ovf_i4(0xFFFF) == 0xFFFFFFFF"
    call int32 Program::Check_ldind_i2_conv_ovf_i4(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x00007FFF
    ldc.i4 0x00007FFF
    ldstr "Checking ldind_i2_conv_ovf_i4_un(0x7FFF) == 0x00007FFF"
    call int32 Program::Check_ldind_i2_conv_ovf_i4_un(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFF8000
    ldstr "Checking ldind_i2_conv_ovf_i4_un(0x8000) == OverflowException"
    call int32 Program::CheckOvf_ldind_i2_conv_ovf_i4_un(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000FFFF
    ldstr "Checking ldind_i2_conv_ovf_i4_un(0xFFFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_i2_conv_ovf_i4_un(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x00007FFF
    ldc.i4 0x00007FFF
    ldstr "Checking ldind_i2_conv_u4(0x7FFF) == 0x00007FFF"
    call int32 Program::Check_ldind_i2_conv_u4(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFF8000
    ldc.i4 0xFFFF8000
    ldstr "Checking ldind_i2_conv_u4(0x8000) == 0xFFFF8000"
    call int32 Program::Check_ldind_i2_conv_u4(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000FFFF
    ldc.i4 0xFFFFFFFF
    ldstr "Checking ldind_i2_conv_u4(0xFFFF) == 0xFFFFFFFF"
    call int32 Program::Check_ldind_i2_conv_u4(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x00007FFF
    ldc.i4 0x00007FFF
    ldstr "Checking ldind_i2_conv_ovf_u4(0x7FFF) == 0x00007FFF"
    call int32 Program::Check_ldind_i2_conv_ovf_u4(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFF8000
    ldstr "Checking ldind_i2_conv_ovf_u4(0x8000) == OverflowException"
    call int32 Program::CheckOvf_ldind_i2_conv_ovf_u4(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000FFFF
    ldstr "Checking ldind_i2_conv_ovf_u4(0xFFFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_i2_conv_ovf_u4(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x00007FFF
    ldc.i4 0x00007FFF
    ldstr "Checking ldind_i2_conv_ovf_u4_un(0x7FFF) == 0x00007FFF"
    call int32 Program::Check_ldind_i2_conv_ovf_u4_un(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFF8000
    ldc.i4 0xFFFF8000
    ldstr "Checking ldind_i2_conv_ovf_u4_un(0x8000) == 0xFFFF8000"
    call int32 Program::Check_ldind_i2_conv_ovf_u4_un(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000FFFF
    ldc.i4 0xFFFFFFFF
    ldstr "Checking ldind_i2_conv_ovf_u4_un(0xFFFF) == 0xFFFFFFFF"
    call int32 Program::Check_ldind_i2_conv_ovf_u4_un(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x00007FFF
    ldc.i8 0x0000000000007FFF
    ldstr "Checking ldind_i2_conv_i8(0x7FFF) == 0x0000000000007FFF"
    call int32 Program::Check_ldind_i2_conv_i8(int32, int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFF8000
    ldc.i8 0xFFFFFFFFFFFF8000
    ldstr "Checking ldind_i2_conv_i8(0x8000) == 0xFFFFFFFFFFFF8000"
    call int32 Program::Check_ldind_i2_conv_i8(int32, int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000FFFF
    ldc.i8 0xFFFFFFFFFFFFFFFF
    ldstr "Checking ldind_i2_conv_i8(0xFFFF) == 0xFFFFFFFFFFFFFFFF"
    call int32 Program::Check_ldind_i2_conv_i8(int32, int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x00007FFF
    ldc.i8 0x0000000000007FFF
    ldstr "Checking ldind_i2_conv_ovf_i8(0x7FFF) == 0x0000000000007FFF"
    call int32 Program::Check_ldind_i2_conv_ovf_i8(int32, int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFF8000
    ldc.i8 0xFFFFFFFFFFFF8000
    ldstr "Checking ldind_i2_conv_ovf_i8(0x8000) == 0xFFFFFFFFFFFF8000"
    call int32 Program::Check_ldind_i2_conv_ovf_i8(int32, int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000FFFF
    ldc.i8 0xFFFFFFFFFFFFFFFF
    ldstr "Checking ldind_i2_conv_ovf_i8(0xFFFF) == 0xFFFFFFFFFFFFFFFF"
    call int32 Program::Check_ldind_i2_conv_ovf_i8(int32, int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x00007FFF
    ldc.i8 0x0000000000007FFF
    ldstr "Checking ldind_i2_conv_ovf_i8_un(0x7FFF) == 0x0000000000007FFF"
    call int32 Program::Check_ldind_i2_conv_ovf_i8_un(int32, int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFF8000
    ldc.i8 0x00000000FFFF8000
    ldstr "Checking ldind_i2_conv_ovf_i8_un(0x8000) == 0x00000000FFFF8000"
    call int32 Program::Check_ldind_i2_conv_ovf_i8_un(int32, int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000FFFF
    ldc.i8 0x00000000FFFFFFFF
    ldstr "Checking ldind_i2_conv_ovf_i8_un(0xFFFF) == 0x00000000FFFFFFFF"
    call int32 Program::Check_ldind_i2_conv_ovf_i8_un(int32, int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x00007FFF
    ldc.i8 0x0000000000007FFF
    ldstr "Checking ldind_i2_conv_u8(0x7FFF) == 0x0000000000007FFF"
    call int32 Program::Check_ldind_i2_conv_u8(int32, int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFF8000
    ldc.i8 0x00000000FFFF8000
    ldstr "Checking ldind_i2_conv_u8(0x8000) == 0x00000000FFFF8000"
    call int32 Program::Check_ldind_i2_conv_u8(int32, int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000FFFF
    ldc.i8 0x00000000FFFFFFFF
    ldstr "Checking ldind_i2_conv_u8(0xFFFF) == 0x00000000FFFFFFFF"
    call int32 Program::Check_ldind_i2_conv_u8(int32, int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x00007FFF
    ldc.i8 0x0000000000007FFF
    ldstr "Checking ldind_i2_conv_ovf_u8(0x7FFF) == 0x0000000000007FFF"
    call int32 Program::Check_ldind_i2_conv_ovf_u8(int32, int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFF8000
    ldstr "Checking ldind_i2_conv_ovf_u8(0x8000) == OverflowException"
    call int32 Program::CheckOvf_ldind_i2_conv_ovf_u8(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000FFFF
    ldstr "Checking ldind_i2_conv_ovf_u8(0xFFFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_i2_conv_ovf_u8(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x00007FFF
    ldc.i8 0x0000000000007FFF
    ldstr "Checking ldind_i2_conv_ovf_u8_un(0x7FFF) == 0x0000000000007FFF"
    call int32 Program::Check_ldind_i2_conv_ovf_u8_un(int32, int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFF8000
    ldc.i8 0x00000000FFFF8000
    ldstr "Checking ldind_i2_conv_ovf_u8_un(0x8000) == 0x00000000FFFF8000"
    call int32 Program::Check_ldind_i2_conv_ovf_u8_un(int32, int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000FFFF
    ldc.i8 0x00000000FFFFFFFF
    ldstr "Checking ldind_i2_conv_ovf_u8_un(0xFFFF) == 0x00000000FFFFFFFF"
    call int32 Program::Check_ldind_i2_conv_ovf_u8_un(int32, int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000FFFF
    ldc.i4 0xFFFFFFFF
    ldstr "Checking ldind_u2_conv_i1(0xFFFF) == 0xFFFFFFFF"
    call int32 Program::Check_ldind_u2_conv_i1(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000007F
    ldc.i4 0x0000007F
    ldstr "Checking ldind_u2_conv_i1(0x007F) == 0x0000007F"
    call int32 Program::Check_ldind_u2_conv_i1(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFFFF80
    ldc.i4 0xFFFFFF80
    ldstr "Checking ldind_u2_conv_i1(0xFF80) == 0xFFFFFF80"
    call int32 Program::Check_ldind_u2_conv_i1(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x000000FF
    ldc.i4 0xFFFFFFFF
    ldstr "Checking ldind_u2_conv_i1(0x00FF) == 0xFFFFFFFF"
    call int32 Program::Check_ldind_u2_conv_i1(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000FFFF
    ldstr "Checking ldind_u2_conv_ovf_i1(0xFFFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_u2_conv_ovf_i1(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000007F
    ldc.i4 0x0000007F
    ldstr "Checking ldind_u2_conv_ovf_i1(0x007F) == 0x0000007F"
    call int32 Program::Check_ldind_u2_conv_ovf_i1(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFFFF80
    ldstr "Checking ldind_u2_conv_ovf_i1(0xFF80) == OverflowException"
    call int32 Program::CheckOvf_ldind_u2_conv_ovf_i1(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x000000FF
    ldstr "Checking ldind_u2_conv_ovf_i1(0x00FF) == OverflowException"
    call int32 Program::CheckOvf_ldind_u2_conv_ovf_i1(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000FFFF
    ldstr "Checking ldind_u2_conv_ovf_i1_un(0xFFFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_u2_conv_ovf_i1_un(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000007F
    ldc.i4 0x0000007F
    ldstr "Checking ldind_u2_conv_ovf_i1_un(0x007F) == 0x0000007F"
    call int32 Program::Check_ldind_u2_conv_ovf_i1_un(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFFFF80
    ldstr "Checking ldind_u2_conv_ovf_i1_un(0xFF80) == OverflowException"
    call int32 Program::CheckOvf_ldind_u2_conv_ovf_i1_un(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x000000FF
    ldstr "Checking ldind_u2_conv_ovf_i1_un(0x00FF) == OverflowException"
    call int32 Program::CheckOvf_ldind_u2_conv_ovf_i1_un(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000FFFF
    ldc.i4 0x000000FF
    ldstr "Checking ldind_u2_conv_u1(0xFFFF) == 0x000000FF"
    call int32 Program::Check_ldind_u2_conv_u1(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x000000FF
    ldc.i4 0x000000FF
    ldstr "Checking ldind_u2_conv_u1(0x00FF) == 0x000000FF"
    call int32 Program::Check_ldind_u2_conv_u1(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000FFFF
    ldstr "Checking ldind_u2_conv_ovf_u1(0xFFFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_u2_conv_ovf_u1(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x000000FF
    ldc.i4 0x000000FF
    ldstr "Checking ldind_u2_conv_ovf_u1(0x00FF) == 0x000000FF"
    call int32 Program::Check_ldind_u2_conv_ovf_u1(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000FFFF
    ldstr "Checking ldind_u2_conv_ovf_u1_un(0xFFFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_u2_conv_ovf_u1_un(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x000000FF
    ldc.i4 0x000000FF
    ldstr "Checking ldind_u2_conv_ovf_u1_un(0x00FF) == 0x000000FF"
    call int32 Program::Check_ldind_u2_conv_ovf_u1_un(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000FFFF
    ldc.i4 0xFFFFFFFF
    ldstr "Checking ldind_u2_conv_i2(0xFFFF) == 0xFFFFFFFF"
    call int32 Program::Check_ldind_u2_conv_i2(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000FFFF
    ldstr "Checking ldind_u2_conv_ovf_i2(0xFFFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_u2_conv_ovf_i2(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000FFFF
    ldstr "Checking ldind_u2_conv_ovf_i2_un(0xFFFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_u2_conv_ovf_i2_un(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000FFFF
    ldc.i4 0x0000FFFF
    ldstr "Checking ldind_u2_conv_u2(0xFFFF) == 0x0000FFFF"
    call int32 Program::Check_ldind_u2_conv_u2(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000FFFF
    ldc.i4 0x0000FFFF
    ldstr "Checking ldind_u2_conv_ovf_u2(0xFFFF) == 0x0000FFFF"
    call int32 Program::Check_ldind_u2_conv_ovf_u2(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000FFFF
    ldc.i4 0x0000FFFF
    ldstr "Checking ldind_u2_conv_ovf_u2_un(0xFFFF) == 0x0000FFFF"
    call int32 Program::Check_ldind_u2_conv_ovf_u2_un(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000FFFF
    ldc.i4 0x0000FFFF
    ldstr "Checking ldind_u2_conv_i4(0xFFFF) == 0x0000FFFF"
    call int32 Program::Check_ldind_u2_conv_i4(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000FFFF
    ldc.i4 0x0000FFFF
    ldstr "Checking ldind_u2_conv_ovf_i4(0xFFFF) == 0x0000FFFF"
    call int32 Program::Check_ldind_u2_conv_ovf_i4(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000FFFF
    ldc.i4 0x0000FFFF
    ldstr "Checking ldind_u2_conv_ovf_i4_un(0xFFFF) == 0x0000FFFF"
    call int32 Program::Check_ldind_u2_conv_ovf_i4_un(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000FFFF
    ldc.i4 0x0000FFFF
    ldstr "Checking ldind_u2_conv_u4(0xFFFF) == 0x0000FFFF"
    call int32 Program::Check_ldind_u2_conv_u4(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000FFFF
    ldc.i4 0x0000FFFF
    ldstr "Checking ldind_u2_conv_ovf_u4(0xFFFF) == 0x0000FFFF"
    call int32 Program::Check_ldind_u2_conv_ovf_u4(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000FFFF
    ldc.i4 0x0000FFFF
    ldstr "Checking ldind_u2_conv_ovf_u4_un(0xFFFF) == 0x0000FFFF"
    call int32 Program::Check_ldind_u2_conv_ovf_u4_un(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000FFFF
    ldc.i8 0x000000000000FFFF
    ldstr "Checking ldind_u2_conv_i8(0xFFFF) == 0x000000000000FFFF"
    call int32 Program::Check_ldind_u2_conv_i8(int32, int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000FFFF
    ldc.i8 0x000000000000FFFF
    ldstr "Checking ldind_u2_conv_ovf_i8(0xFFFF) == 0x000000000000FFFF"
    call int32 Program::Check_ldind_u2_conv_ovf_i8(int32, int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000FFFF
    ldc.i8 0x000000000000FFFF
    ldstr "Checking ldind_u2_conv_ovf_i8_un(0xFFFF) == 0x000000000000FFFF"
    call int32 Program::Check_ldind_u2_conv_ovf_i8_un(int32, int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000FFFF
    ldc.i8 0x000000000000FFFF
    ldstr "Checking ldind_u2_conv_u8(0xFFFF) == 0x000000000000FFFF"
    call int32 Program::Check_ldind_u2_conv_u8(int32, int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000FFFF
    ldc.i8 0x000000000000FFFF
    ldstr "Checking ldind_u2_conv_ovf_u8(0xFFFF) == 0x000000000000FFFF"
    call int32 Program::Check_ldind_u2_conv_ovf_u8(int32, int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000FFFF
    ldc.i8 0x000000000000FFFF
    ldstr "Checking ldind_u2_conv_ovf_u8_un(0xFFFF) == 0x000000000000FFFF"
    call int32 Program::Check_ldind_u2_conv_ovf_u8_un(int32, int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x7FFFFFFF
    ldc.i4 0xFFFFFFFF
    ldstr "Checking ldind_i4_conv_i1(0x7FFFFFFF) == 0xFFFFFFFF"
    call int32 Program::Check_ldind_i4_conv_i1(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x80000000
    ldc.i4 0x00000000
    ldstr "Checking ldind_i4_conv_i1(0x80000000) == 0x00000000"
    call int32 Program::Check_ldind_i4_conv_i1(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFFFFFF
    ldc.i4 0xFFFFFFFF
    ldstr "Checking ldind_i4_conv_i1(0xFFFFFFFF) == 0xFFFFFFFF"
    call int32 Program::Check_ldind_i4_conv_i1(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000007F
    ldc.i4 0x0000007F
    ldstr "Checking ldind_i4_conv_i1(0x0000007F) == 0x0000007F"
    call int32 Program::Check_ldind_i4_conv_i1(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFFFF80
    ldc.i4 0xFFFFFF80
    ldstr "Checking ldind_i4_conv_i1(0xFFFFFF80) == 0xFFFFFF80"
    call int32 Program::Check_ldind_i4_conv_i1(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x000000FF
    ldc.i4 0xFFFFFFFF
    ldstr "Checking ldind_i4_conv_i1(0x000000FF) == 0xFFFFFFFF"
    call int32 Program::Check_ldind_i4_conv_i1(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x7FFFFFFF
    ldstr "Checking ldind_i4_conv_ovf_i1(0x7FFFFFFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_i4_conv_ovf_i1(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x80000000
    ldstr "Checking ldind_i4_conv_ovf_i1(0x80000000) == OverflowException"
    call int32 Program::CheckOvf_ldind_i4_conv_ovf_i1(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFFFFFF
    ldc.i4 0xFFFFFFFF
    ldstr "Checking ldind_i4_conv_ovf_i1(0xFFFFFFFF) == 0xFFFFFFFF"
    call int32 Program::Check_ldind_i4_conv_ovf_i1(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000007F
    ldc.i4 0x0000007F
    ldstr "Checking ldind_i4_conv_ovf_i1(0x0000007F) == 0x0000007F"
    call int32 Program::Check_ldind_i4_conv_ovf_i1(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFFFF80
    ldc.i4 0xFFFFFF80
    ldstr "Checking ldind_i4_conv_ovf_i1(0xFFFFFF80) == 0xFFFFFF80"
    call int32 Program::Check_ldind_i4_conv_ovf_i1(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x000000FF
    ldstr "Checking ldind_i4_conv_ovf_i1(0x000000FF) == OverflowException"
    call int32 Program::CheckOvf_ldind_i4_conv_ovf_i1(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x7FFFFFFF
    ldstr "Checking ldind_i4_conv_ovf_i1_un(0x7FFFFFFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_i4_conv_ovf_i1_un(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x80000000
    ldstr "Checking ldind_i4_conv_ovf_i1_un(0x80000000) == OverflowException"
    call int32 Program::CheckOvf_ldind_i4_conv_ovf_i1_un(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFFFFFF
    ldstr "Checking ldind_i4_conv_ovf_i1_un(0xFFFFFFFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_i4_conv_ovf_i1_un(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000007F
    ldc.i4 0x0000007F
    ldstr "Checking ldind_i4_conv_ovf_i1_un(0x0000007F) == 0x0000007F"
    call int32 Program::Check_ldind_i4_conv_ovf_i1_un(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFFFF80
    ldstr "Checking ldind_i4_conv_ovf_i1_un(0xFFFFFF80) == OverflowException"
    call int32 Program::CheckOvf_ldind_i4_conv_ovf_i1_un(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x000000FF
    ldstr "Checking ldind_i4_conv_ovf_i1_un(0x000000FF) == OverflowException"
    call int32 Program::CheckOvf_ldind_i4_conv_ovf_i1_un(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x7FFFFFFF
    ldc.i4 0x000000FF
    ldstr "Checking ldind_i4_conv_u1(0x7FFFFFFF) == 0x000000FF"
    call int32 Program::Check_ldind_i4_conv_u1(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x80000000
    ldc.i4 0x00000000
    ldstr "Checking ldind_i4_conv_u1(0x80000000) == 0x00000000"
    call int32 Program::Check_ldind_i4_conv_u1(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFFFFFF
    ldc.i4 0x000000FF
    ldstr "Checking ldind_i4_conv_u1(0xFFFFFFFF) == 0x000000FF"
    call int32 Program::Check_ldind_i4_conv_u1(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x000000FF
    ldc.i4 0x000000FF
    ldstr "Checking ldind_i4_conv_u1(0x000000FF) == 0x000000FF"
    call int32 Program::Check_ldind_i4_conv_u1(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x7FFFFFFF
    ldstr "Checking ldind_i4_conv_ovf_u1(0x7FFFFFFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_i4_conv_ovf_u1(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x80000000
    ldstr "Checking ldind_i4_conv_ovf_u1(0x80000000) == OverflowException"
    call int32 Program::CheckOvf_ldind_i4_conv_ovf_u1(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFFFFFF
    ldstr "Checking ldind_i4_conv_ovf_u1(0xFFFFFFFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_i4_conv_ovf_u1(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x000000FF
    ldc.i4 0x000000FF
    ldstr "Checking ldind_i4_conv_ovf_u1(0x000000FF) == 0x000000FF"
    call int32 Program::Check_ldind_i4_conv_ovf_u1(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x7FFFFFFF
    ldstr "Checking ldind_i4_conv_ovf_u1_un(0x7FFFFFFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_i4_conv_ovf_u1_un(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x80000000
    ldstr "Checking ldind_i4_conv_ovf_u1_un(0x80000000) == OverflowException"
    call int32 Program::CheckOvf_ldind_i4_conv_ovf_u1_un(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFFFFFF
    ldstr "Checking ldind_i4_conv_ovf_u1_un(0xFFFFFFFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_i4_conv_ovf_u1_un(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x000000FF
    ldc.i4 0x000000FF
    ldstr "Checking ldind_i4_conv_ovf_u1_un(0x000000FF) == 0x000000FF"
    call int32 Program::Check_ldind_i4_conv_ovf_u1_un(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x7FFFFFFF
    ldc.i4 0xFFFFFFFF
    ldstr "Checking ldind_i4_conv_i2(0x7FFFFFFF) == 0xFFFFFFFF"
    call int32 Program::Check_ldind_i4_conv_i2(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x80000000
    ldc.i4 0x00000000
    ldstr "Checking ldind_i4_conv_i2(0x80000000) == 0x00000000"
    call int32 Program::Check_ldind_i4_conv_i2(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFFFFFF
    ldc.i4 0xFFFFFFFF
    ldstr "Checking ldind_i4_conv_i2(0xFFFFFFFF) == 0xFFFFFFFF"
    call int32 Program::Check_ldind_i4_conv_i2(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x00007FFF
    ldc.i4 0x00007FFF
    ldstr "Checking ldind_i4_conv_i2(0x00007FFF) == 0x00007FFF"
    call int32 Program::Check_ldind_i4_conv_i2(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFF8000
    ldc.i4 0xFFFF8000
    ldstr "Checking ldind_i4_conv_i2(0xFFFF8000) == 0xFFFF8000"
    call int32 Program::Check_ldind_i4_conv_i2(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000FFFF
    ldc.i4 0xFFFFFFFF
    ldstr "Checking ldind_i4_conv_i2(0x0000FFFF) == 0xFFFFFFFF"
    call int32 Program::Check_ldind_i4_conv_i2(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x7FFFFFFF
    ldstr "Checking ldind_i4_conv_ovf_i2(0x7FFFFFFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_i4_conv_ovf_i2(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x80000000
    ldstr "Checking ldind_i4_conv_ovf_i2(0x80000000) == OverflowException"
    call int32 Program::CheckOvf_ldind_i4_conv_ovf_i2(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFFFFFF
    ldc.i4 0xFFFFFFFF
    ldstr "Checking ldind_i4_conv_ovf_i2(0xFFFFFFFF) == 0xFFFFFFFF"
    call int32 Program::Check_ldind_i4_conv_ovf_i2(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x00007FFF
    ldc.i4 0x00007FFF
    ldstr "Checking ldind_i4_conv_ovf_i2(0x00007FFF) == 0x00007FFF"
    call int32 Program::Check_ldind_i4_conv_ovf_i2(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFF8000
    ldc.i4 0xFFFF8000
    ldstr "Checking ldind_i4_conv_ovf_i2(0xFFFF8000) == 0xFFFF8000"
    call int32 Program::Check_ldind_i4_conv_ovf_i2(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000FFFF
    ldstr "Checking ldind_i4_conv_ovf_i2(0x0000FFFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_i4_conv_ovf_i2(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x7FFFFFFF
    ldstr "Checking ldind_i4_conv_ovf_i2_un(0x7FFFFFFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_i4_conv_ovf_i2_un(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x80000000
    ldstr "Checking ldind_i4_conv_ovf_i2_un(0x80000000) == OverflowException"
    call int32 Program::CheckOvf_ldind_i4_conv_ovf_i2_un(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFFFFFF
    ldstr "Checking ldind_i4_conv_ovf_i2_un(0xFFFFFFFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_i4_conv_ovf_i2_un(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x00007FFF
    ldc.i4 0x00007FFF
    ldstr "Checking ldind_i4_conv_ovf_i2_un(0x00007FFF) == 0x00007FFF"
    call int32 Program::Check_ldind_i4_conv_ovf_i2_un(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFF8000
    ldstr "Checking ldind_i4_conv_ovf_i2_un(0xFFFF8000) == OverflowException"
    call int32 Program::CheckOvf_ldind_i4_conv_ovf_i2_un(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000FFFF
    ldstr "Checking ldind_i4_conv_ovf_i2_un(0x0000FFFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_i4_conv_ovf_i2_un(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x7FFFFFFF
    ldc.i4 0x0000FFFF
    ldstr "Checking ldind_i4_conv_u2(0x7FFFFFFF) == 0x0000FFFF"
    call int32 Program::Check_ldind_i4_conv_u2(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x80000000
    ldc.i4 0x00000000
    ldstr "Checking ldind_i4_conv_u2(0x80000000) == 0x00000000"
    call int32 Program::Check_ldind_i4_conv_u2(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFFFFFF
    ldc.i4 0x0000FFFF
    ldstr "Checking ldind_i4_conv_u2(0xFFFFFFFF) == 0x0000FFFF"
    call int32 Program::Check_ldind_i4_conv_u2(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000FFFF
    ldc.i4 0x0000FFFF
    ldstr "Checking ldind_i4_conv_u2(0x0000FFFF) == 0x0000FFFF"
    call int32 Program::Check_ldind_i4_conv_u2(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x7FFFFFFF
    ldstr "Checking ldind_i4_conv_ovf_u2(0x7FFFFFFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_i4_conv_ovf_u2(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x80000000
    ldstr "Checking ldind_i4_conv_ovf_u2(0x80000000) == OverflowException"
    call int32 Program::CheckOvf_ldind_i4_conv_ovf_u2(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFFFFFF
    ldstr "Checking ldind_i4_conv_ovf_u2(0xFFFFFFFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_i4_conv_ovf_u2(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000FFFF
    ldc.i4 0x0000FFFF
    ldstr "Checking ldind_i4_conv_ovf_u2(0x0000FFFF) == 0x0000FFFF"
    call int32 Program::Check_ldind_i4_conv_ovf_u2(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x7FFFFFFF
    ldstr "Checking ldind_i4_conv_ovf_u2_un(0x7FFFFFFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_i4_conv_ovf_u2_un(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x80000000
    ldstr "Checking ldind_i4_conv_ovf_u2_un(0x80000000) == OverflowException"
    call int32 Program::CheckOvf_ldind_i4_conv_ovf_u2_un(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFFFFFF
    ldstr "Checking ldind_i4_conv_ovf_u2_un(0xFFFFFFFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_i4_conv_ovf_u2_un(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000FFFF
    ldc.i4 0x0000FFFF
    ldstr "Checking ldind_i4_conv_ovf_u2_un(0x0000FFFF) == 0x0000FFFF"
    call int32 Program::Check_ldind_i4_conv_ovf_u2_un(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x7FFFFFFF
    ldc.i4 0x7FFFFFFF
    ldstr "Checking ldind_i4_conv_i4(0x7FFFFFFF) == 0x7FFFFFFF"
    call int32 Program::Check_ldind_i4_conv_i4(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x80000000
    ldc.i4 0x80000000
    ldstr "Checking ldind_i4_conv_i4(0x80000000) == 0x80000000"
    call int32 Program::Check_ldind_i4_conv_i4(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFFFFFF
    ldc.i4 0xFFFFFFFF
    ldstr "Checking ldind_i4_conv_i4(0xFFFFFFFF) == 0xFFFFFFFF"
    call int32 Program::Check_ldind_i4_conv_i4(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x7FFFFFFF
    ldc.i4 0x7FFFFFFF
    ldstr "Checking ldind_i4_conv_ovf_i4(0x7FFFFFFF) == 0x7FFFFFFF"
    call int32 Program::Check_ldind_i4_conv_ovf_i4(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x80000000
    ldc.i4 0x80000000
    ldstr "Checking ldind_i4_conv_ovf_i4(0x80000000) == 0x80000000"
    call int32 Program::Check_ldind_i4_conv_ovf_i4(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFFFFFF
    ldc.i4 0xFFFFFFFF
    ldstr "Checking ldind_i4_conv_ovf_i4(0xFFFFFFFF) == 0xFFFFFFFF"
    call int32 Program::Check_ldind_i4_conv_ovf_i4(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x7FFFFFFF
    ldc.i4 0x7FFFFFFF
    ldstr "Checking ldind_i4_conv_ovf_i4_un(0x7FFFFFFF) == 0x7FFFFFFF"
    call int32 Program::Check_ldind_i4_conv_ovf_i4_un(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x80000000
    ldstr "Checking ldind_i4_conv_ovf_i4_un(0x80000000) == OverflowException"
    call int32 Program::CheckOvf_ldind_i4_conv_ovf_i4_un(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFFFFFF
    ldstr "Checking ldind_i4_conv_ovf_i4_un(0xFFFFFFFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_i4_conv_ovf_i4_un(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x7FFFFFFF
    ldc.i4 0x7FFFFFFF
    ldstr "Checking ldind_i4_conv_u4(0x7FFFFFFF) == 0x7FFFFFFF"
    call int32 Program::Check_ldind_i4_conv_u4(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x80000000
    ldc.i4 0x80000000
    ldstr "Checking ldind_i4_conv_u4(0x80000000) == 0x80000000"
    call int32 Program::Check_ldind_i4_conv_u4(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFFFFFF
    ldc.i4 0xFFFFFFFF
    ldstr "Checking ldind_i4_conv_u4(0xFFFFFFFF) == 0xFFFFFFFF"
    call int32 Program::Check_ldind_i4_conv_u4(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x7FFFFFFF
    ldc.i4 0x7FFFFFFF
    ldstr "Checking ldind_i4_conv_ovf_u4(0x7FFFFFFF) == 0x7FFFFFFF"
    call int32 Program::Check_ldind_i4_conv_ovf_u4(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x80000000
    ldstr "Checking ldind_i4_conv_ovf_u4(0x80000000) == OverflowException"
    call int32 Program::CheckOvf_ldind_i4_conv_ovf_u4(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFFFFFF
    ldstr "Checking ldind_i4_conv_ovf_u4(0xFFFFFFFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_i4_conv_ovf_u4(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x7FFFFFFF
    ldc.i4 0x7FFFFFFF
    ldstr "Checking ldind_i4_conv_ovf_u4_un(0x7FFFFFFF) == 0x7FFFFFFF"
    call int32 Program::Check_ldind_i4_conv_ovf_u4_un(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x80000000
    ldc.i4 0x80000000
    ldstr "Checking ldind_i4_conv_ovf_u4_un(0x80000000) == 0x80000000"
    call int32 Program::Check_ldind_i4_conv_ovf_u4_un(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFFFFFF
    ldc.i4 0xFFFFFFFF
    ldstr "Checking ldind_i4_conv_ovf_u4_un(0xFFFFFFFF) == 0xFFFFFFFF"
    call int32 Program::Check_ldind_i4_conv_ovf_u4_un(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x7FFFFFFF
    ldc.i8 0x000000007FFFFFFF
    ldstr "Checking ldind_i4_conv_i8(0x7FFFFFFF) == 0x000000007FFFFFFF"
    call int32 Program::Check_ldind_i4_conv_i8(int32, int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x80000000
    ldc.i8 0xFFFFFFFF80000000
    ldstr "Checking ldind_i4_conv_i8(0x80000000) == 0xFFFFFFFF80000000"
    call int32 Program::Check_ldind_i4_conv_i8(int32, int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFFFFFF
    ldc.i8 0xFFFFFFFFFFFFFFFF
    ldstr "Checking ldind_i4_conv_i8(0xFFFFFFFF) == 0xFFFFFFFFFFFFFFFF"
    call int32 Program::Check_ldind_i4_conv_i8(int32, int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x7FFFFFFF
    ldc.i8 0x000000007FFFFFFF
    ldstr "Checking ldind_i4_conv_ovf_i8(0x7FFFFFFF) == 0x000000007FFFFFFF"
    call int32 Program::Check_ldind_i4_conv_ovf_i8(int32, int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x80000000
    ldc.i8 0xFFFFFFFF80000000
    ldstr "Checking ldind_i4_conv_ovf_i8(0x80000000) == 0xFFFFFFFF80000000"
    call int32 Program::Check_ldind_i4_conv_ovf_i8(int32, int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFFFFFF
    ldc.i8 0xFFFFFFFFFFFFFFFF
    ldstr "Checking ldind_i4_conv_ovf_i8(0xFFFFFFFF) == 0xFFFFFFFFFFFFFFFF"
    call int32 Program::Check_ldind_i4_conv_ovf_i8(int32, int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x7FFFFFFF
    ldc.i8 0x000000007FFFFFFF
    ldstr "Checking ldind_i4_conv_ovf_i8_un(0x7FFFFFFF) == 0x000000007FFFFFFF"
    call int32 Program::Check_ldind_i4_conv_ovf_i8_un(int32, int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x80000000
    ldc.i8 0x0000000080000000
    ldstr "Checking ldind_i4_conv_ovf_i8_un(0x80000000) == 0x0000000080000000"
    call int32 Program::Check_ldind_i4_conv_ovf_i8_un(int32, int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFFFFFF
    ldc.i8 0x00000000FFFFFFFF
    ldstr "Checking ldind_i4_conv_ovf_i8_un(0xFFFFFFFF) == 0x00000000FFFFFFFF"
    call int32 Program::Check_ldind_i4_conv_ovf_i8_un(int32, int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x7FFFFFFF
    ldc.i8 0x000000007FFFFFFF
    ldstr "Checking ldind_i4_conv_u8(0x7FFFFFFF) == 0x000000007FFFFFFF"
    call int32 Program::Check_ldind_i4_conv_u8(int32, int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x80000000
    ldc.i8 0x0000000080000000
    ldstr "Checking ldind_i4_conv_u8(0x80000000) == 0x0000000080000000"
    call int32 Program::Check_ldind_i4_conv_u8(int32, int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFFFFFF
    ldc.i8 0x00000000FFFFFFFF
    ldstr "Checking ldind_i4_conv_u8(0xFFFFFFFF) == 0x00000000FFFFFFFF"
    call int32 Program::Check_ldind_i4_conv_u8(int32, int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x7FFFFFFF
    ldc.i8 0x000000007FFFFFFF
    ldstr "Checking ldind_i4_conv_ovf_u8(0x7FFFFFFF) == 0x000000007FFFFFFF"
    call int32 Program::Check_ldind_i4_conv_ovf_u8(int32, int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x80000000
    ldstr "Checking ldind_i4_conv_ovf_u8(0x80000000) == OverflowException"
    call int32 Program::CheckOvf_ldind_i4_conv_ovf_u8(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFFFFFF
    ldstr "Checking ldind_i4_conv_ovf_u8(0xFFFFFFFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_i4_conv_ovf_u8(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x7FFFFFFF
    ldc.i8 0x000000007FFFFFFF
    ldstr "Checking ldind_i4_conv_ovf_u8_un(0x7FFFFFFF) == 0x000000007FFFFFFF"
    call int32 Program::Check_ldind_i4_conv_ovf_u8_un(int32, int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x80000000
    ldc.i8 0x0000000080000000
    ldstr "Checking ldind_i4_conv_ovf_u8_un(0x80000000) == 0x0000000080000000"
    call int32 Program::Check_ldind_i4_conv_ovf_u8_un(int32, int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFFFFFF
    ldc.i8 0x00000000FFFFFFFF
    ldstr "Checking ldind_i4_conv_ovf_u8_un(0xFFFFFFFF) == 0x00000000FFFFFFFF"
    call int32 Program::Check_ldind_i4_conv_ovf_u8_un(int32, int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFFFFFF
    ldc.i4 0xFFFFFFFF
    ldstr "Checking ldind_u4_conv_i1(0xFFFFFFFF) == 0xFFFFFFFF"
    call int32 Program::Check_ldind_u4_conv_i1(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000007F
    ldc.i4 0x0000007F
    ldstr "Checking ldind_u4_conv_i1(0x0000007F) == 0x0000007F"
    call int32 Program::Check_ldind_u4_conv_i1(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFFFF80
    ldc.i4 0xFFFFFF80
    ldstr "Checking ldind_u4_conv_i1(0xFFFFFF80) == 0xFFFFFF80"
    call int32 Program::Check_ldind_u4_conv_i1(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x000000FF
    ldc.i4 0xFFFFFFFF
    ldstr "Checking ldind_u4_conv_i1(0x000000FF) == 0xFFFFFFFF"
    call int32 Program::Check_ldind_u4_conv_i1(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFFFFFF
    ldc.i4 0xFFFFFFFF
    ldstr "Checking ldind_u4_conv_ovf_i1(0xFFFFFFFF) == 0xFFFFFFFF"
    call int32 Program::Check_ldind_u4_conv_ovf_i1(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000007F
    ldc.i4 0x0000007F
    ldstr "Checking ldind_u4_conv_ovf_i1(0x0000007F) == 0x0000007F"
    call int32 Program::Check_ldind_u4_conv_ovf_i1(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFFFF80
    ldc.i4 0xFFFFFF80
    ldstr "Checking ldind_u4_conv_ovf_i1(0xFFFFFF80) == 0xFFFFFF80"
    call int32 Program::Check_ldind_u4_conv_ovf_i1(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x000000FF
    ldstr "Checking ldind_u4_conv_ovf_i1(0x000000FF) == OverflowException"
    call int32 Program::CheckOvf_ldind_u4_conv_ovf_i1(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFFFFFF
    ldstr "Checking ldind_u4_conv_ovf_i1_un(0xFFFFFFFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_u4_conv_ovf_i1_un(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000007F
    ldc.i4 0x0000007F
    ldstr "Checking ldind_u4_conv_ovf_i1_un(0x0000007F) == 0x0000007F"
    call int32 Program::Check_ldind_u4_conv_ovf_i1_un(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFFFF80
    ldstr "Checking ldind_u4_conv_ovf_i1_un(0xFFFFFF80) == OverflowException"
    call int32 Program::CheckOvf_ldind_u4_conv_ovf_i1_un(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x000000FF
    ldstr "Checking ldind_u4_conv_ovf_i1_un(0x000000FF) == OverflowException"
    call int32 Program::CheckOvf_ldind_u4_conv_ovf_i1_un(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFFFFFF
    ldc.i4 0x000000FF
    ldstr "Checking ldind_u4_conv_u1(0xFFFFFFFF) == 0x000000FF"
    call int32 Program::Check_ldind_u4_conv_u1(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x000000FF
    ldc.i4 0x000000FF
    ldstr "Checking ldind_u4_conv_u1(0x000000FF) == 0x000000FF"
    call int32 Program::Check_ldind_u4_conv_u1(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFFFFFF
    ldstr "Checking ldind_u4_conv_ovf_u1(0xFFFFFFFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_u4_conv_ovf_u1(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x000000FF
    ldc.i4 0x000000FF
    ldstr "Checking ldind_u4_conv_ovf_u1(0x000000FF) == 0x000000FF"
    call int32 Program::Check_ldind_u4_conv_ovf_u1(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFFFFFF
    ldstr "Checking ldind_u4_conv_ovf_u1_un(0xFFFFFFFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_u4_conv_ovf_u1_un(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x000000FF
    ldc.i4 0x000000FF
    ldstr "Checking ldind_u4_conv_ovf_u1_un(0x000000FF) == 0x000000FF"
    call int32 Program::Check_ldind_u4_conv_ovf_u1_un(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFFFFFF
    ldc.i4 0xFFFFFFFF
    ldstr "Checking ldind_u4_conv_i2(0xFFFFFFFF) == 0xFFFFFFFF"
    call int32 Program::Check_ldind_u4_conv_i2(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x00007FFF
    ldc.i4 0x00007FFF
    ldstr "Checking ldind_u4_conv_i2(0x00007FFF) == 0x00007FFF"
    call int32 Program::Check_ldind_u4_conv_i2(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFF8000
    ldc.i4 0xFFFF8000
    ldstr "Checking ldind_u4_conv_i2(0xFFFF8000) == 0xFFFF8000"
    call int32 Program::Check_ldind_u4_conv_i2(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000FFFF
    ldc.i4 0xFFFFFFFF
    ldstr "Checking ldind_u4_conv_i2(0x0000FFFF) == 0xFFFFFFFF"
    call int32 Program::Check_ldind_u4_conv_i2(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFFFFFF
    ldc.i4 0xFFFFFFFF
    ldstr "Checking ldind_u4_conv_ovf_i2(0xFFFFFFFF) == 0xFFFFFFFF"
    call int32 Program::Check_ldind_u4_conv_ovf_i2(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x00007FFF
    ldc.i4 0x00007FFF
    ldstr "Checking ldind_u4_conv_ovf_i2(0x00007FFF) == 0x00007FFF"
    call int32 Program::Check_ldind_u4_conv_ovf_i2(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFF8000
    ldc.i4 0xFFFF8000
    ldstr "Checking ldind_u4_conv_ovf_i2(0xFFFF8000) == 0xFFFF8000"
    call int32 Program::Check_ldind_u4_conv_ovf_i2(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000FFFF
    ldstr "Checking ldind_u4_conv_ovf_i2(0x0000FFFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_u4_conv_ovf_i2(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFFFFFF
    ldstr "Checking ldind_u4_conv_ovf_i2_un(0xFFFFFFFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_u4_conv_ovf_i2_un(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x00007FFF
    ldc.i4 0x00007FFF
    ldstr "Checking ldind_u4_conv_ovf_i2_un(0x00007FFF) == 0x00007FFF"
    call int32 Program::Check_ldind_u4_conv_ovf_i2_un(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFF8000
    ldstr "Checking ldind_u4_conv_ovf_i2_un(0xFFFF8000) == OverflowException"
    call int32 Program::CheckOvf_ldind_u4_conv_ovf_i2_un(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000FFFF
    ldstr "Checking ldind_u4_conv_ovf_i2_un(0x0000FFFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_u4_conv_ovf_i2_un(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFFFFFF
    ldc.i4 0x0000FFFF
    ldstr "Checking ldind_u4_conv_u2(0xFFFFFFFF) == 0x0000FFFF"
    call int32 Program::Check_ldind_u4_conv_u2(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000FFFF
    ldc.i4 0x0000FFFF
    ldstr "Checking ldind_u4_conv_u2(0x0000FFFF) == 0x0000FFFF"
    call int32 Program::Check_ldind_u4_conv_u2(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFFFFFF
    ldstr "Checking ldind_u4_conv_ovf_u2(0xFFFFFFFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_u4_conv_ovf_u2(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000FFFF
    ldc.i4 0x0000FFFF
    ldstr "Checking ldind_u4_conv_ovf_u2(0x0000FFFF) == 0x0000FFFF"
    call int32 Program::Check_ldind_u4_conv_ovf_u2(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFFFFFF
    ldstr "Checking ldind_u4_conv_ovf_u2_un(0xFFFFFFFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_u4_conv_ovf_u2_un(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0x0000FFFF
    ldc.i4 0x0000FFFF
    ldstr "Checking ldind_u4_conv_ovf_u2_un(0x0000FFFF) == 0x0000FFFF"
    call int32 Program::Check_ldind_u4_conv_ovf_u2_un(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFFFFFF
    ldc.i4 0xFFFFFFFF
    ldstr "Checking ldind_u4_conv_i4(0xFFFFFFFF) == 0xFFFFFFFF"
    call int32 Program::Check_ldind_u4_conv_i4(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFFFFFF
    ldc.i4 0xFFFFFFFF
    ldstr "Checking ldind_u4_conv_ovf_i4(0xFFFFFFFF) == 0xFFFFFFFF"
    call int32 Program::Check_ldind_u4_conv_ovf_i4(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFFFFFF
    ldstr "Checking ldind_u4_conv_ovf_i4_un(0xFFFFFFFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_u4_conv_ovf_i4_un(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFFFFFF
    ldc.i4 0xFFFFFFFF
    ldstr "Checking ldind_u4_conv_u4(0xFFFFFFFF) == 0xFFFFFFFF"
    call int32 Program::Check_ldind_u4_conv_u4(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFFFFFF
    ldstr "Checking ldind_u4_conv_ovf_u4(0xFFFFFFFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_u4_conv_ovf_u4(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFFFFFF
    ldc.i4 0xFFFFFFFF
    ldstr "Checking ldind_u4_conv_ovf_u4_un(0xFFFFFFFF) == 0xFFFFFFFF"
    call int32 Program::Check_ldind_u4_conv_ovf_u4_un(int32, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFFFFFF
    ldc.i8 0xFFFFFFFFFFFFFFFF
    ldstr "Checking ldind_u4_conv_i8(0xFFFFFFFF) == 0xFFFFFFFFFFFFFFFF"
    call int32 Program::Check_ldind_u4_conv_i8(int32, int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFFFFFF
    ldc.i8 0xFFFFFFFFFFFFFFFF
    ldstr "Checking ldind_u4_conv_ovf_i8(0xFFFFFFFF) == 0xFFFFFFFFFFFFFFFF"
    call int32 Program::Check_ldind_u4_conv_ovf_i8(int32, int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFFFFFF
    ldc.i8 0x00000000FFFFFFFF
    ldstr "Checking ldind_u4_conv_ovf_i8_un(0xFFFFFFFF) == 0x00000000FFFFFFFF"
    call int32 Program::Check_ldind_u4_conv_ovf_i8_un(int32, int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFFFFFF
    ldc.i8 0x00000000FFFFFFFF
    ldstr "Checking ldind_u4_conv_u8(0xFFFFFFFF) == 0x00000000FFFFFFFF"
    call int32 Program::Check_ldind_u4_conv_u8(int32, int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFFFFFF
    ldstr "Checking ldind_u4_conv_ovf_u8(0xFFFFFFFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_u4_conv_ovf_u8(int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 0xFFFFFFFF
    ldc.i8 0x00000000FFFFFFFF
    ldstr "Checking ldind_u4_conv_ovf_u8_un(0xFFFFFFFF) == 0x00000000FFFFFFFF"
    call int32 Program::Check_ldind_u4_conv_ovf_u8_un(int32, int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x7FFFFFFFFFFFFFFF
    ldc.i4 0xFFFFFFFF
    ldstr "Checking ldind_i8_conv_i1(0x7FFFFFFFFFFFFFFF) == 0xFFFFFFFF"
    call int32 Program::Check_ldind_i8_conv_i1(int64, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x8000000000000000
    ldc.i4 0x00000000
    ldstr "Checking ldind_i8_conv_i1(0x8000000000000000) == 0x00000000"
    call int32 Program::Check_ldind_i8_conv_i1(int64, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0xFFFFFFFFFFFFFFFF
    ldc.i4 0xFFFFFFFF
    ldstr "Checking ldind_i8_conv_i1(0xFFFFFFFFFFFFFFFF) == 0xFFFFFFFF"
    call int32 Program::Check_ldind_i8_conv_i1(int64, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x000000000000007F
    ldc.i4 0x0000007F
    ldstr "Checking ldind_i8_conv_i1(0x000000000000007F) == 0x0000007F"
    call int32 Program::Check_ldind_i8_conv_i1(int64, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0xFFFFFFFFFFFFFF80
    ldc.i4 0xFFFFFF80
    ldstr "Checking ldind_i8_conv_i1(0xFFFFFFFFFFFFFF80) == 0xFFFFFF80"
    call int32 Program::Check_ldind_i8_conv_i1(int64, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x00000000000000FF
    ldc.i4 0xFFFFFFFF
    ldstr "Checking ldind_i8_conv_i1(0x00000000000000FF) == 0xFFFFFFFF"
    call int32 Program::Check_ldind_i8_conv_i1(int64, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x7FFFFFFFFFFFFFFF
    ldstr "Checking ldind_i8_conv_ovf_i1(0x7FFFFFFFFFFFFFFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_i8_conv_ovf_i1(int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x8000000000000000
    ldstr "Checking ldind_i8_conv_ovf_i1(0x8000000000000000) == OverflowException"
    call int32 Program::CheckOvf_ldind_i8_conv_ovf_i1(int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0xFFFFFFFFFFFFFFFF
    ldc.i4 0xFFFFFFFF
    ldstr "Checking ldind_i8_conv_ovf_i1(0xFFFFFFFFFFFFFFFF) == 0xFFFFFFFF"
    call int32 Program::Check_ldind_i8_conv_ovf_i1(int64, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x000000000000007F
    ldc.i4 0x0000007F
    ldstr "Checking ldind_i8_conv_ovf_i1(0x000000000000007F) == 0x0000007F"
    call int32 Program::Check_ldind_i8_conv_ovf_i1(int64, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0xFFFFFFFFFFFFFF80
    ldc.i4 0xFFFFFF80
    ldstr "Checking ldind_i8_conv_ovf_i1(0xFFFFFFFFFFFFFF80) == 0xFFFFFF80"
    call int32 Program::Check_ldind_i8_conv_ovf_i1(int64, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x00000000000000FF
    ldstr "Checking ldind_i8_conv_ovf_i1(0x00000000000000FF) == OverflowException"
    call int32 Program::CheckOvf_ldind_i8_conv_ovf_i1(int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x7FFFFFFFFFFFFFFF
    ldstr "Checking ldind_i8_conv_ovf_i1_un(0x7FFFFFFFFFFFFFFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_i8_conv_ovf_i1_un(int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x8000000000000000
    ldstr "Checking ldind_i8_conv_ovf_i1_un(0x8000000000000000) == OverflowException"
    call int32 Program::CheckOvf_ldind_i8_conv_ovf_i1_un(int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0xFFFFFFFFFFFFFFFF
    ldstr "Checking ldind_i8_conv_ovf_i1_un(0xFFFFFFFFFFFFFFFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_i8_conv_ovf_i1_un(int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x000000000000007F
    ldc.i4 0x0000007F
    ldstr "Checking ldind_i8_conv_ovf_i1_un(0x000000000000007F) == 0x0000007F"
    call int32 Program::Check_ldind_i8_conv_ovf_i1_un(int64, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0xFFFFFFFFFFFFFF80
    ldstr "Checking ldind_i8_conv_ovf_i1_un(0xFFFFFFFFFFFFFF80) == OverflowException"
    call int32 Program::CheckOvf_ldind_i8_conv_ovf_i1_un(int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x00000000000000FF
    ldstr "Checking ldind_i8_conv_ovf_i1_un(0x00000000000000FF) == OverflowException"
    call int32 Program::CheckOvf_ldind_i8_conv_ovf_i1_un(int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x7FFFFFFFFFFFFFFF
    ldc.i4 0x000000FF
    ldstr "Checking ldind_i8_conv_u1(0x7FFFFFFFFFFFFFFF) == 0x000000FF"
    call int32 Program::Check_ldind_i8_conv_u1(int64, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x8000000000000000
    ldc.i4 0x00000000
    ldstr "Checking ldind_i8_conv_u1(0x8000000000000000) == 0x00000000"
    call int32 Program::Check_ldind_i8_conv_u1(int64, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0xFFFFFFFFFFFFFFFF
    ldc.i4 0x000000FF
    ldstr "Checking ldind_i8_conv_u1(0xFFFFFFFFFFFFFFFF) == 0x000000FF"
    call int32 Program::Check_ldind_i8_conv_u1(int64, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x00000000000000FF
    ldc.i4 0x000000FF
    ldstr "Checking ldind_i8_conv_u1(0x00000000000000FF) == 0x000000FF"
    call int32 Program::Check_ldind_i8_conv_u1(int64, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x7FFFFFFFFFFFFFFF
    ldstr "Checking ldind_i8_conv_ovf_u1(0x7FFFFFFFFFFFFFFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_i8_conv_ovf_u1(int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x8000000000000000
    ldstr "Checking ldind_i8_conv_ovf_u1(0x8000000000000000) == OverflowException"
    call int32 Program::CheckOvf_ldind_i8_conv_ovf_u1(int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0xFFFFFFFFFFFFFFFF
    ldstr "Checking ldind_i8_conv_ovf_u1(0xFFFFFFFFFFFFFFFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_i8_conv_ovf_u1(int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x00000000000000FF
    ldc.i4 0x000000FF
    ldstr "Checking ldind_i8_conv_ovf_u1(0x00000000000000FF) == 0x000000FF"
    call int32 Program::Check_ldind_i8_conv_ovf_u1(int64, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x7FFFFFFFFFFFFFFF
    ldstr "Checking ldind_i8_conv_ovf_u1_un(0x7FFFFFFFFFFFFFFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_i8_conv_ovf_u1_un(int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x8000000000000000
    ldstr "Checking ldind_i8_conv_ovf_u1_un(0x8000000000000000) == OverflowException"
    call int32 Program::CheckOvf_ldind_i8_conv_ovf_u1_un(int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0xFFFFFFFFFFFFFFFF
    ldstr "Checking ldind_i8_conv_ovf_u1_un(0xFFFFFFFFFFFFFFFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_i8_conv_ovf_u1_un(int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x00000000000000FF
    ldc.i4 0x000000FF
    ldstr "Checking ldind_i8_conv_ovf_u1_un(0x00000000000000FF) == 0x000000FF"
    call int32 Program::Check_ldind_i8_conv_ovf_u1_un(int64, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x7FFFFFFFFFFFFFFF
    ldc.i4 0xFFFFFFFF
    ldstr "Checking ldind_i8_conv_i2(0x7FFFFFFFFFFFFFFF) == 0xFFFFFFFF"
    call int32 Program::Check_ldind_i8_conv_i2(int64, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x8000000000000000
    ldc.i4 0x00000000
    ldstr "Checking ldind_i8_conv_i2(0x8000000000000000) == 0x00000000"
    call int32 Program::Check_ldind_i8_conv_i2(int64, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0xFFFFFFFFFFFFFFFF
    ldc.i4 0xFFFFFFFF
    ldstr "Checking ldind_i8_conv_i2(0xFFFFFFFFFFFFFFFF) == 0xFFFFFFFF"
    call int32 Program::Check_ldind_i8_conv_i2(int64, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x0000000000007FFF
    ldc.i4 0x00007FFF
    ldstr "Checking ldind_i8_conv_i2(0x0000000000007FFF) == 0x00007FFF"
    call int32 Program::Check_ldind_i8_conv_i2(int64, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0xFFFFFFFFFFFF8000
    ldc.i4 0xFFFF8000
    ldstr "Checking ldind_i8_conv_i2(0xFFFFFFFFFFFF8000) == 0xFFFF8000"
    call int32 Program::Check_ldind_i8_conv_i2(int64, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x000000000000FFFF
    ldc.i4 0xFFFFFFFF
    ldstr "Checking ldind_i8_conv_i2(0x000000000000FFFF) == 0xFFFFFFFF"
    call int32 Program::Check_ldind_i8_conv_i2(int64, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x7FFFFFFFFFFFFFFF
    ldstr "Checking ldind_i8_conv_ovf_i2(0x7FFFFFFFFFFFFFFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_i8_conv_ovf_i2(int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x8000000000000000
    ldstr "Checking ldind_i8_conv_ovf_i2(0x8000000000000000) == OverflowException"
    call int32 Program::CheckOvf_ldind_i8_conv_ovf_i2(int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0xFFFFFFFFFFFFFFFF
    ldc.i4 0xFFFFFFFF
    ldstr "Checking ldind_i8_conv_ovf_i2(0xFFFFFFFFFFFFFFFF) == 0xFFFFFFFF"
    call int32 Program::Check_ldind_i8_conv_ovf_i2(int64, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x0000000000007FFF
    ldc.i4 0x00007FFF
    ldstr "Checking ldind_i8_conv_ovf_i2(0x0000000000007FFF) == 0x00007FFF"
    call int32 Program::Check_ldind_i8_conv_ovf_i2(int64, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0xFFFFFFFFFFFF8000
    ldc.i4 0xFFFF8000
    ldstr "Checking ldind_i8_conv_ovf_i2(0xFFFFFFFFFFFF8000) == 0xFFFF8000"
    call int32 Program::Check_ldind_i8_conv_ovf_i2(int64, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x000000000000FFFF
    ldstr "Checking ldind_i8_conv_ovf_i2(0x000000000000FFFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_i8_conv_ovf_i2(int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x7FFFFFFFFFFFFFFF
    ldstr "Checking ldind_i8_conv_ovf_i2_un(0x7FFFFFFFFFFFFFFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_i8_conv_ovf_i2_un(int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x8000000000000000
    ldstr "Checking ldind_i8_conv_ovf_i2_un(0x8000000000000000) == OverflowException"
    call int32 Program::CheckOvf_ldind_i8_conv_ovf_i2_un(int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0xFFFFFFFFFFFFFFFF
    ldstr "Checking ldind_i8_conv_ovf_i2_un(0xFFFFFFFFFFFFFFFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_i8_conv_ovf_i2_un(int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x0000000000007FFF
    ldc.i4 0x00007FFF
    ldstr "Checking ldind_i8_conv_ovf_i2_un(0x0000000000007FFF) == 0x00007FFF"
    call int32 Program::Check_ldind_i8_conv_ovf_i2_un(int64, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0xFFFFFFFFFFFF8000
    ldstr "Checking ldind_i8_conv_ovf_i2_un(0xFFFFFFFFFFFF8000) == OverflowException"
    call int32 Program::CheckOvf_ldind_i8_conv_ovf_i2_un(int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x000000000000FFFF
    ldstr "Checking ldind_i8_conv_ovf_i2_un(0x000000000000FFFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_i8_conv_ovf_i2_un(int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x7FFFFFFFFFFFFFFF
    ldc.i4 0x0000FFFF
    ldstr "Checking ldind_i8_conv_u2(0x7FFFFFFFFFFFFFFF) == 0x0000FFFF"
    call int32 Program::Check_ldind_i8_conv_u2(int64, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x8000000000000000
    ldc.i4 0x00000000
    ldstr "Checking ldind_i8_conv_u2(0x8000000000000000) == 0x00000000"
    call int32 Program::Check_ldind_i8_conv_u2(int64, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0xFFFFFFFFFFFFFFFF
    ldc.i4 0x0000FFFF
    ldstr "Checking ldind_i8_conv_u2(0xFFFFFFFFFFFFFFFF) == 0x0000FFFF"
    call int32 Program::Check_ldind_i8_conv_u2(int64, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x000000000000FFFF
    ldc.i4 0x0000FFFF
    ldstr "Checking ldind_i8_conv_u2(0x000000000000FFFF) == 0x0000FFFF"
    call int32 Program::Check_ldind_i8_conv_u2(int64, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x7FFFFFFFFFFFFFFF
    ldstr "Checking ldind_i8_conv_ovf_u2(0x7FFFFFFFFFFFFFFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_i8_conv_ovf_u2(int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x8000000000000000
    ldstr "Checking ldind_i8_conv_ovf_u2(0x8000000000000000) == OverflowException"
    call int32 Program::CheckOvf_ldind_i8_conv_ovf_u2(int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0xFFFFFFFFFFFFFFFF
    ldstr "Checking ldind_i8_conv_ovf_u2(0xFFFFFFFFFFFFFFFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_i8_conv_ovf_u2(int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x000000000000FFFF
    ldc.i4 0x0000FFFF
    ldstr "Checking ldind_i8_conv_ovf_u2(0x000000000000FFFF) == 0x0000FFFF"
    call int32 Program::Check_ldind_i8_conv_ovf_u2(int64, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x7FFFFFFFFFFFFFFF
    ldstr "Checking ldind_i8_conv_ovf_u2_un(0x7FFFFFFFFFFFFFFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_i8_conv_ovf_u2_un(int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x8000000000000000
    ldstr "Checking ldind_i8_conv_ovf_u2_un(0x8000000000000000) == OverflowException"
    call int32 Program::CheckOvf_ldind_i8_conv_ovf_u2_un(int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0xFFFFFFFFFFFFFFFF
    ldstr "Checking ldind_i8_conv_ovf_u2_un(0xFFFFFFFFFFFFFFFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_i8_conv_ovf_u2_un(int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x000000000000FFFF
    ldc.i4 0x0000FFFF
    ldstr "Checking ldind_i8_conv_ovf_u2_un(0x000000000000FFFF) == 0x0000FFFF"
    call int32 Program::Check_ldind_i8_conv_ovf_u2_un(int64, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x7FFFFFFFFFFFFFFF
    ldc.i4 0xFFFFFFFF
    ldstr "Checking ldind_i8_conv_i4(0x7FFFFFFFFFFFFFFF) == 0xFFFFFFFF"
    call int32 Program::Check_ldind_i8_conv_i4(int64, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x8000000000000000
    ldc.i4 0x00000000
    ldstr "Checking ldind_i8_conv_i4(0x8000000000000000) == 0x00000000"
    call int32 Program::Check_ldind_i8_conv_i4(int64, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0xFFFFFFFFFFFFFFFF
    ldc.i4 0xFFFFFFFF
    ldstr "Checking ldind_i8_conv_i4(0xFFFFFFFFFFFFFFFF) == 0xFFFFFFFF"
    call int32 Program::Check_ldind_i8_conv_i4(int64, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x000000007FFFFFFF
    ldc.i4 0x7FFFFFFF
    ldstr "Checking ldind_i8_conv_i4(0x000000007FFFFFFF) == 0x7FFFFFFF"
    call int32 Program::Check_ldind_i8_conv_i4(int64, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0xFFFFFFFF80000000
    ldc.i4 0x80000000
    ldstr "Checking ldind_i8_conv_i4(0xFFFFFFFF80000000) == 0x80000000"
    call int32 Program::Check_ldind_i8_conv_i4(int64, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x00000000FFFFFFFF
    ldc.i4 0xFFFFFFFF
    ldstr "Checking ldind_i8_conv_i4(0x00000000FFFFFFFF) == 0xFFFFFFFF"
    call int32 Program::Check_ldind_i8_conv_i4(int64, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x7FFFFFFFFFFFFFFF
    ldstr "Checking ldind_i8_conv_ovf_i4(0x7FFFFFFFFFFFFFFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_i8_conv_ovf_i4(int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x8000000000000000
    ldstr "Checking ldind_i8_conv_ovf_i4(0x8000000000000000) == OverflowException"
    call int32 Program::CheckOvf_ldind_i8_conv_ovf_i4(int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0xFFFFFFFFFFFFFFFF
    ldc.i4 0xFFFFFFFF
    ldstr "Checking ldind_i8_conv_ovf_i4(0xFFFFFFFFFFFFFFFF) == 0xFFFFFFFF"
    call int32 Program::Check_ldind_i8_conv_ovf_i4(int64, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x000000007FFFFFFF
    ldc.i4 0x7FFFFFFF
    ldstr "Checking ldind_i8_conv_ovf_i4(0x000000007FFFFFFF) == 0x7FFFFFFF"
    call int32 Program::Check_ldind_i8_conv_ovf_i4(int64, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0xFFFFFFFF80000000
    ldc.i4 0x80000000
    ldstr "Checking ldind_i8_conv_ovf_i4(0xFFFFFFFF80000000) == 0x80000000"
    call int32 Program::Check_ldind_i8_conv_ovf_i4(int64, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x00000000FFFFFFFF
    ldstr "Checking ldind_i8_conv_ovf_i4(0x00000000FFFFFFFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_i8_conv_ovf_i4(int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x7FFFFFFFFFFFFFFF
    ldstr "Checking ldind_i8_conv_ovf_i4_un(0x7FFFFFFFFFFFFFFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_i8_conv_ovf_i4_un(int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x8000000000000000
    ldstr "Checking ldind_i8_conv_ovf_i4_un(0x8000000000000000) == OverflowException"
    call int32 Program::CheckOvf_ldind_i8_conv_ovf_i4_un(int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0xFFFFFFFFFFFFFFFF
    ldstr "Checking ldind_i8_conv_ovf_i4_un(0xFFFFFFFFFFFFFFFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_i8_conv_ovf_i4_un(int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x000000007FFFFFFF
    ldc.i4 0x7FFFFFFF
    ldstr "Checking ldind_i8_conv_ovf_i4_un(0x000000007FFFFFFF) == 0x7FFFFFFF"
    call int32 Program::Check_ldind_i8_conv_ovf_i4_un(int64, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0xFFFFFFFF80000000
    ldstr "Checking ldind_i8_conv_ovf_i4_un(0xFFFFFFFF80000000) == OverflowException"
    call int32 Program::CheckOvf_ldind_i8_conv_ovf_i4_un(int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x00000000FFFFFFFF
    ldstr "Checking ldind_i8_conv_ovf_i4_un(0x00000000FFFFFFFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_i8_conv_ovf_i4_un(int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x7FFFFFFFFFFFFFFF
    ldc.i4 0xFFFFFFFF
    ldstr "Checking ldind_i8_conv_u4(0x7FFFFFFFFFFFFFFF) == 0xFFFFFFFF"
    call int32 Program::Check_ldind_i8_conv_u4(int64, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x8000000000000000
    ldc.i4 0x00000000
    ldstr "Checking ldind_i8_conv_u4(0x8000000000000000) == 0x00000000"
    call int32 Program::Check_ldind_i8_conv_u4(int64, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0xFFFFFFFFFFFFFFFF
    ldc.i4 0xFFFFFFFF
    ldstr "Checking ldind_i8_conv_u4(0xFFFFFFFFFFFFFFFF) == 0xFFFFFFFF"
    call int32 Program::Check_ldind_i8_conv_u4(int64, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x00000000FFFFFFFF
    ldc.i4 0xFFFFFFFF
    ldstr "Checking ldind_i8_conv_u4(0x00000000FFFFFFFF) == 0xFFFFFFFF"
    call int32 Program::Check_ldind_i8_conv_u4(int64, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x7FFFFFFFFFFFFFFF
    ldstr "Checking ldind_i8_conv_ovf_u4(0x7FFFFFFFFFFFFFFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_i8_conv_ovf_u4(int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x8000000000000000
    ldstr "Checking ldind_i8_conv_ovf_u4(0x8000000000000000) == OverflowException"
    call int32 Program::CheckOvf_ldind_i8_conv_ovf_u4(int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0xFFFFFFFFFFFFFFFF
    ldstr "Checking ldind_i8_conv_ovf_u4(0xFFFFFFFFFFFFFFFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_i8_conv_ovf_u4(int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x00000000FFFFFFFF
    ldc.i4 0xFFFFFFFF
    ldstr "Checking ldind_i8_conv_ovf_u4(0x00000000FFFFFFFF) == 0xFFFFFFFF"
    call int32 Program::Check_ldind_i8_conv_ovf_u4(int64, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x7FFFFFFFFFFFFFFF
    ldstr "Checking ldind_i8_conv_ovf_u4_un(0x7FFFFFFFFFFFFFFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_i8_conv_ovf_u4_un(int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x8000000000000000
    ldstr "Checking ldind_i8_conv_ovf_u4_un(0x8000000000000000) == OverflowException"
    call int32 Program::CheckOvf_ldind_i8_conv_ovf_u4_un(int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0xFFFFFFFFFFFFFFFF
    ldstr "Checking ldind_i8_conv_ovf_u4_un(0xFFFFFFFFFFFFFFFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_i8_conv_ovf_u4_un(int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x00000000FFFFFFFF
    ldc.i4 0xFFFFFFFF
    ldstr "Checking ldind_i8_conv_ovf_u4_un(0x00000000FFFFFFFF) == 0xFFFFFFFF"
    call int32 Program::Check_ldind_i8_conv_ovf_u4_un(int64, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x7FFFFFFFFFFFFFFF
    ldc.i8 0x7FFFFFFFFFFFFFFF
    ldstr "Checking ldind_i8_conv_i8(0x7FFFFFFFFFFFFFFF) == 0x7FFFFFFFFFFFFFFF"
    call int32 Program::Check_ldind_i8_conv_i8(int64, int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x8000000000000000
    ldc.i8 0x8000000000000000
    ldstr "Checking ldind_i8_conv_i8(0x8000000000000000) == 0x8000000000000000"
    call int32 Program::Check_ldind_i8_conv_i8(int64, int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0xFFFFFFFFFFFFFFFF
    ldc.i8 0xFFFFFFFFFFFFFFFF
    ldstr "Checking ldind_i8_conv_i8(0xFFFFFFFFFFFFFFFF) == 0xFFFFFFFFFFFFFFFF"
    call int32 Program::Check_ldind_i8_conv_i8(int64, int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x7FFFFFFFFFFFFFFF
    ldc.i8 0x7FFFFFFFFFFFFFFF
    ldstr "Checking ldind_i8_conv_ovf_i8(0x7FFFFFFFFFFFFFFF) == 0x7FFFFFFFFFFFFFFF"
    call int32 Program::Check_ldind_i8_conv_ovf_i8(int64, int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x8000000000000000
    ldc.i8 0x8000000000000000
    ldstr "Checking ldind_i8_conv_ovf_i8(0x8000000000000000) == 0x8000000000000000"
    call int32 Program::Check_ldind_i8_conv_ovf_i8(int64, int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0xFFFFFFFFFFFFFFFF
    ldc.i8 0xFFFFFFFFFFFFFFFF
    ldstr "Checking ldind_i8_conv_ovf_i8(0xFFFFFFFFFFFFFFFF) == 0xFFFFFFFFFFFFFFFF"
    call int32 Program::Check_ldind_i8_conv_ovf_i8(int64, int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x7FFFFFFFFFFFFFFF
    ldc.i8 0x7FFFFFFFFFFFFFFF
    ldstr "Checking ldind_i8_conv_ovf_i8_un(0x7FFFFFFFFFFFFFFF) == 0x7FFFFFFFFFFFFFFF"
    call int32 Program::Check_ldind_i8_conv_ovf_i8_un(int64, int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x8000000000000000
    ldstr "Checking ldind_i8_conv_ovf_i8_un(0x8000000000000000) == OverflowException"
    call int32 Program::CheckOvf_ldind_i8_conv_ovf_i8_un(int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0xFFFFFFFFFFFFFFFF
    ldstr "Checking ldind_i8_conv_ovf_i8_un(0xFFFFFFFFFFFFFFFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_i8_conv_ovf_i8_un(int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x7FFFFFFFFFFFFFFF
    ldc.i8 0x7FFFFFFFFFFFFFFF
    ldstr "Checking ldind_i8_conv_u8(0x7FFFFFFFFFFFFFFF) == 0x7FFFFFFFFFFFFFFF"
    call int32 Program::Check_ldind_i8_conv_u8(int64, int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x8000000000000000
    ldc.i8 0x8000000000000000
    ldstr "Checking ldind_i8_conv_u8(0x8000000000000000) == 0x8000000000000000"
    call int32 Program::Check_ldind_i8_conv_u8(int64, int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0xFFFFFFFFFFFFFFFF
    ldc.i8 0xFFFFFFFFFFFFFFFF
    ldstr "Checking ldind_i8_conv_u8(0xFFFFFFFFFFFFFFFF) == 0xFFFFFFFFFFFFFFFF"
    call int32 Program::Check_ldind_i8_conv_u8(int64, int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x7FFFFFFFFFFFFFFF
    ldc.i8 0x7FFFFFFFFFFFFFFF
    ldstr "Checking ldind_i8_conv_ovf_u8(0x7FFFFFFFFFFFFFFF) == 0x7FFFFFFFFFFFFFFF"
    call int32 Program::Check_ldind_i8_conv_ovf_u8(int64, int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x8000000000000000
    ldstr "Checking ldind_i8_conv_ovf_u8(0x8000000000000000) == OverflowException"
    call int32 Program::CheckOvf_ldind_i8_conv_ovf_u8(int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0xFFFFFFFFFFFFFFFF
    ldstr "Checking ldind_i8_conv_ovf_u8(0xFFFFFFFFFFFFFFFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_i8_conv_ovf_u8(int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x7FFFFFFFFFFFFFFF
    ldc.i8 0x7FFFFFFFFFFFFFFF
    ldstr "Checking ldind_i8_conv_ovf_u8_un(0x7FFFFFFFFFFFFFFF) == 0x7FFFFFFFFFFFFFFF"
    call int32 Program::Check_ldind_i8_conv_ovf_u8_un(int64, int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x8000000000000000
    ldc.i8 0x8000000000000000
    ldstr "Checking ldind_i8_conv_ovf_u8_un(0x8000000000000000) == 0x8000000000000000"
    call int32 Program::Check_ldind_i8_conv_ovf_u8_un(int64, int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0xFFFFFFFFFFFFFFFF
    ldc.i8 0xFFFFFFFFFFFFFFFF
    ldstr "Checking ldind_i8_conv_ovf_u8_un(0xFFFFFFFFFFFFFFFF) == 0xFFFFFFFFFFFFFFFF"
    call int32 Program::Check_ldind_i8_conv_ovf_u8_un(int64, int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0xFFFFFFFFFFFFFFFF
    ldc.i4 0xFFFFFFFF
    ldstr "Checking ldind_u8_conv_i1(0xFFFFFFFFFFFFFFFF) == 0xFFFFFFFF"
    call int32 Program::Check_ldind_u8_conv_i1(int64, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x000000000000007F
    ldc.i4 0x0000007F
    ldstr "Checking ldind_u8_conv_i1(0x000000000000007F) == 0x0000007F"
    call int32 Program::Check_ldind_u8_conv_i1(int64, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0xFFFFFFFFFFFFFF80
    ldc.i4 0xFFFFFF80
    ldstr "Checking ldind_u8_conv_i1(0xFFFFFFFFFFFFFF80) == 0xFFFFFF80"
    call int32 Program::Check_ldind_u8_conv_i1(int64, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x00000000000000FF
    ldc.i4 0xFFFFFFFF
    ldstr "Checking ldind_u8_conv_i1(0x00000000000000FF) == 0xFFFFFFFF"
    call int32 Program::Check_ldind_u8_conv_i1(int64, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0xFFFFFFFFFFFFFFFF
    ldc.i4 0xFFFFFFFF
    ldstr "Checking ldind_u8_conv_ovf_i1(0xFFFFFFFFFFFFFFFF) == 0xFFFFFFFF"
    call int32 Program::Check_ldind_u8_conv_ovf_i1(int64, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x000000000000007F
    ldc.i4 0x0000007F
    ldstr "Checking ldind_u8_conv_ovf_i1(0x000000000000007F) == 0x0000007F"
    call int32 Program::Check_ldind_u8_conv_ovf_i1(int64, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0xFFFFFFFFFFFFFF80
    ldc.i4 0xFFFFFF80
    ldstr "Checking ldind_u8_conv_ovf_i1(0xFFFFFFFFFFFFFF80) == 0xFFFFFF80"
    call int32 Program::Check_ldind_u8_conv_ovf_i1(int64, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x00000000000000FF
    ldstr "Checking ldind_u8_conv_ovf_i1(0x00000000000000FF) == OverflowException"
    call int32 Program::CheckOvf_ldind_u8_conv_ovf_i1(int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0xFFFFFFFFFFFFFFFF
    ldstr "Checking ldind_u8_conv_ovf_i1_un(0xFFFFFFFFFFFFFFFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_u8_conv_ovf_i1_un(int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x000000000000007F
    ldc.i4 0x0000007F
    ldstr "Checking ldind_u8_conv_ovf_i1_un(0x000000000000007F) == 0x0000007F"
    call int32 Program::Check_ldind_u8_conv_ovf_i1_un(int64, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0xFFFFFFFFFFFFFF80
    ldstr "Checking ldind_u8_conv_ovf_i1_un(0xFFFFFFFFFFFFFF80) == OverflowException"
    call int32 Program::CheckOvf_ldind_u8_conv_ovf_i1_un(int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x00000000000000FF
    ldstr "Checking ldind_u8_conv_ovf_i1_un(0x00000000000000FF) == OverflowException"
    call int32 Program::CheckOvf_ldind_u8_conv_ovf_i1_un(int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0xFFFFFFFFFFFFFFFF
    ldc.i4 0x000000FF
    ldstr "Checking ldind_u8_conv_u1(0xFFFFFFFFFFFFFFFF) == 0x000000FF"
    call int32 Program::Check_ldind_u8_conv_u1(int64, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x00000000000000FF
    ldc.i4 0x000000FF
    ldstr "Checking ldind_u8_conv_u1(0x00000000000000FF) == 0x000000FF"
    call int32 Program::Check_ldind_u8_conv_u1(int64, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0xFFFFFFFFFFFFFFFF
    ldstr "Checking ldind_u8_conv_ovf_u1(0xFFFFFFFFFFFFFFFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_u8_conv_ovf_u1(int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x00000000000000FF
    ldc.i4 0x000000FF
    ldstr "Checking ldind_u8_conv_ovf_u1(0x00000000000000FF) == 0x000000FF"
    call int32 Program::Check_ldind_u8_conv_ovf_u1(int64, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0xFFFFFFFFFFFFFFFF
    ldstr "Checking ldind_u8_conv_ovf_u1_un(0xFFFFFFFFFFFFFFFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_u8_conv_ovf_u1_un(int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x00000000000000FF
    ldc.i4 0x000000FF
    ldstr "Checking ldind_u8_conv_ovf_u1_un(0x00000000000000FF) == 0x000000FF"
    call int32 Program::Check_ldind_u8_conv_ovf_u1_un(int64, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0xFFFFFFFFFFFFFFFF
    ldc.i4 0xFFFFFFFF
    ldstr "Checking ldind_u8_conv_i2(0xFFFFFFFFFFFFFFFF) == 0xFFFFFFFF"
    call int32 Program::Check_ldind_u8_conv_i2(int64, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x0000000000007FFF
    ldc.i4 0x00007FFF
    ldstr "Checking ldind_u8_conv_i2(0x0000000000007FFF) == 0x00007FFF"
    call int32 Program::Check_ldind_u8_conv_i2(int64, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0xFFFFFFFFFFFF8000
    ldc.i4 0xFFFF8000
    ldstr "Checking ldind_u8_conv_i2(0xFFFFFFFFFFFF8000) == 0xFFFF8000"
    call int32 Program::Check_ldind_u8_conv_i2(int64, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x000000000000FFFF
    ldc.i4 0xFFFFFFFF
    ldstr "Checking ldind_u8_conv_i2(0x000000000000FFFF) == 0xFFFFFFFF"
    call int32 Program::Check_ldind_u8_conv_i2(int64, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0xFFFFFFFFFFFFFFFF
    ldc.i4 0xFFFFFFFF
    ldstr "Checking ldind_u8_conv_ovf_i2(0xFFFFFFFFFFFFFFFF) == 0xFFFFFFFF"
    call int32 Program::Check_ldind_u8_conv_ovf_i2(int64, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x0000000000007FFF
    ldc.i4 0x00007FFF
    ldstr "Checking ldind_u8_conv_ovf_i2(0x0000000000007FFF) == 0x00007FFF"
    call int32 Program::Check_ldind_u8_conv_ovf_i2(int64, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0xFFFFFFFFFFFF8000
    ldc.i4 0xFFFF8000
    ldstr "Checking ldind_u8_conv_ovf_i2(0xFFFFFFFFFFFF8000) == 0xFFFF8000"
    call int32 Program::Check_ldind_u8_conv_ovf_i2(int64, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x000000000000FFFF
    ldstr "Checking ldind_u8_conv_ovf_i2(0x000000000000FFFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_u8_conv_ovf_i2(int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0xFFFFFFFFFFFFFFFF
    ldstr "Checking ldind_u8_conv_ovf_i2_un(0xFFFFFFFFFFFFFFFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_u8_conv_ovf_i2_un(int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x0000000000007FFF
    ldc.i4 0x00007FFF
    ldstr "Checking ldind_u8_conv_ovf_i2_un(0x0000000000007FFF) == 0x00007FFF"
    call int32 Program::Check_ldind_u8_conv_ovf_i2_un(int64, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0xFFFFFFFFFFFF8000
    ldstr "Checking ldind_u8_conv_ovf_i2_un(0xFFFFFFFFFFFF8000) == OverflowException"
    call int32 Program::CheckOvf_ldind_u8_conv_ovf_i2_un(int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x000000000000FFFF
    ldstr "Checking ldind_u8_conv_ovf_i2_un(0x000000000000FFFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_u8_conv_ovf_i2_un(int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0xFFFFFFFFFFFFFFFF
    ldc.i4 0x0000FFFF
    ldstr "Checking ldind_u8_conv_u2(0xFFFFFFFFFFFFFFFF) == 0x0000FFFF"
    call int32 Program::Check_ldind_u8_conv_u2(int64, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x000000000000FFFF
    ldc.i4 0x0000FFFF
    ldstr "Checking ldind_u8_conv_u2(0x000000000000FFFF) == 0x0000FFFF"
    call int32 Program::Check_ldind_u8_conv_u2(int64, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0xFFFFFFFFFFFFFFFF
    ldstr "Checking ldind_u8_conv_ovf_u2(0xFFFFFFFFFFFFFFFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_u8_conv_ovf_u2(int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x000000000000FFFF
    ldc.i4 0x0000FFFF
    ldstr "Checking ldind_u8_conv_ovf_u2(0x000000000000FFFF) == 0x0000FFFF"
    call int32 Program::Check_ldind_u8_conv_ovf_u2(int64, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0xFFFFFFFFFFFFFFFF
    ldstr "Checking ldind_u8_conv_ovf_u2_un(0xFFFFFFFFFFFFFFFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_u8_conv_ovf_u2_un(int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x000000000000FFFF
    ldc.i4 0x0000FFFF
    ldstr "Checking ldind_u8_conv_ovf_u2_un(0x000000000000FFFF) == 0x0000FFFF"
    call int32 Program::Check_ldind_u8_conv_ovf_u2_un(int64, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0xFFFFFFFFFFFFFFFF
    ldc.i4 0xFFFFFFFF
    ldstr "Checking ldind_u8_conv_i4(0xFFFFFFFFFFFFFFFF) == 0xFFFFFFFF"
    call int32 Program::Check_ldind_u8_conv_i4(int64, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x000000007FFFFFFF
    ldc.i4 0x7FFFFFFF
    ldstr "Checking ldind_u8_conv_i4(0x000000007FFFFFFF) == 0x7FFFFFFF"
    call int32 Program::Check_ldind_u8_conv_i4(int64, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0xFFFFFFFF80000000
    ldc.i4 0x80000000
    ldstr "Checking ldind_u8_conv_i4(0xFFFFFFFF80000000) == 0x80000000"
    call int32 Program::Check_ldind_u8_conv_i4(int64, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x00000000FFFFFFFF
    ldc.i4 0xFFFFFFFF
    ldstr "Checking ldind_u8_conv_i4(0x00000000FFFFFFFF) == 0xFFFFFFFF"
    call int32 Program::Check_ldind_u8_conv_i4(int64, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0xFFFFFFFFFFFFFFFF
    ldc.i4 0xFFFFFFFF
    ldstr "Checking ldind_u8_conv_ovf_i4(0xFFFFFFFFFFFFFFFF) == 0xFFFFFFFF"
    call int32 Program::Check_ldind_u8_conv_ovf_i4(int64, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x000000007FFFFFFF
    ldc.i4 0x7FFFFFFF
    ldstr "Checking ldind_u8_conv_ovf_i4(0x000000007FFFFFFF) == 0x7FFFFFFF"
    call int32 Program::Check_ldind_u8_conv_ovf_i4(int64, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0xFFFFFFFF80000000
    ldc.i4 0x80000000
    ldstr "Checking ldind_u8_conv_ovf_i4(0xFFFFFFFF80000000) == 0x80000000"
    call int32 Program::Check_ldind_u8_conv_ovf_i4(int64, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x00000000FFFFFFFF
    ldstr "Checking ldind_u8_conv_ovf_i4(0x00000000FFFFFFFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_u8_conv_ovf_i4(int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0xFFFFFFFFFFFFFFFF
    ldstr "Checking ldind_u8_conv_ovf_i4_un(0xFFFFFFFFFFFFFFFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_u8_conv_ovf_i4_un(int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x000000007FFFFFFF
    ldc.i4 0x7FFFFFFF
    ldstr "Checking ldind_u8_conv_ovf_i4_un(0x000000007FFFFFFF) == 0x7FFFFFFF"
    call int32 Program::Check_ldind_u8_conv_ovf_i4_un(int64, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0xFFFFFFFF80000000
    ldstr "Checking ldind_u8_conv_ovf_i4_un(0xFFFFFFFF80000000) == OverflowException"
    call int32 Program::CheckOvf_ldind_u8_conv_ovf_i4_un(int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x00000000FFFFFFFF
    ldstr "Checking ldind_u8_conv_ovf_i4_un(0x00000000FFFFFFFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_u8_conv_ovf_i4_un(int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0xFFFFFFFFFFFFFFFF
    ldc.i4 0xFFFFFFFF
    ldstr "Checking ldind_u8_conv_u4(0xFFFFFFFFFFFFFFFF) == 0xFFFFFFFF"
    call int32 Program::Check_ldind_u8_conv_u4(int64, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x00000000FFFFFFFF
    ldc.i4 0xFFFFFFFF
    ldstr "Checking ldind_u8_conv_u4(0x00000000FFFFFFFF) == 0xFFFFFFFF"
    call int32 Program::Check_ldind_u8_conv_u4(int64, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0xFFFFFFFFFFFFFFFF
    ldstr "Checking ldind_u8_conv_ovf_u4(0xFFFFFFFFFFFFFFFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_u8_conv_ovf_u4(int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x00000000FFFFFFFF
    ldc.i4 0xFFFFFFFF
    ldstr "Checking ldind_u8_conv_ovf_u4(0x00000000FFFFFFFF) == 0xFFFFFFFF"
    call int32 Program::Check_ldind_u8_conv_ovf_u4(int64, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0xFFFFFFFFFFFFFFFF
    ldstr "Checking ldind_u8_conv_ovf_u4_un(0xFFFFFFFFFFFFFFFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_u8_conv_ovf_u4_un(int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0x00000000FFFFFFFF
    ldc.i4 0xFFFFFFFF
    ldstr "Checking ldind_u8_conv_ovf_u4_un(0x00000000FFFFFFFF) == 0xFFFFFFFF"
    call int32 Program::Check_ldind_u8_conv_ovf_u4_un(int64, int32, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0xFFFFFFFFFFFFFFFF
    ldc.i8 0xFFFFFFFFFFFFFFFF
    ldstr "Checking ldind_u8_conv_i8(0xFFFFFFFFFFFFFFFF) == 0xFFFFFFFFFFFFFFFF"
    call int32 Program::Check_ldind_u8_conv_i8(int64, int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0xFFFFFFFFFFFFFFFF
    ldc.i8 0xFFFFFFFFFFFFFFFF
    ldstr "Checking ldind_u8_conv_ovf_i8(0xFFFFFFFFFFFFFFFF) == 0xFFFFFFFFFFFFFFFF"
    call int32 Program::Check_ldind_u8_conv_ovf_i8(int64, int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0xFFFFFFFFFFFFFFFF
    ldstr "Checking ldind_u8_conv_ovf_i8_un(0xFFFFFFFFFFFFFFFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_u8_conv_ovf_i8_un(int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0xFFFFFFFFFFFFFFFF
    ldc.i8 0xFFFFFFFFFFFFFFFF
    ldstr "Checking ldind_u8_conv_u8(0xFFFFFFFFFFFFFFFF) == 0xFFFFFFFFFFFFFFFF"
    call int32 Program::Check_ldind_u8_conv_u8(int64, int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0xFFFFFFFFFFFFFFFF
    ldstr "Checking ldind_u8_conv_ovf_u8(0xFFFFFFFFFFFFFFFF) == OverflowException"
    call int32 Program::CheckOvf_ldind_u8_conv_ovf_u8(int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i8 0xFFFFFFFFFFFFFFFF
    ldc.i8 0xFFFFFFFFFFFFFFFF
    ldstr "Checking ldind_u8_conv_ovf_u8_un(0xFFFFFFFFFFFFFFFF) == 0xFFFFFFFFFFFFFFFF"
    call int32 Program::Check_ldind_u8_conv_ovf_u8_un(int64, int64, class [System.Runtime]System.String)
    brfalse FAIL

    ldc.i4 100
    ret

FAIL:
    ldstr "FAILED"
    call void Program::print(class [System.Runtime]System.String)
    ldc.i4 1
    ret
  }

  .method private hidebysig static void Test_ldind_i1_conv_i1(int8& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i1
    conv.i1
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_i1_conv_i1(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int8 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i1_conv_i1(int8&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static void Test_ldind_i1_conv_ovf_i1(int8& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i1
    conv.ovf.i1
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_i1_conv_ovf_i1(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int8 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i1_conv_ovf_i1(int8&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_i1_conv_ovf_i1(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int8 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i1_conv_ovf_i1(int8&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_i1_conv_ovf_i1_un(int8& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i1
    conv.ovf.i1.un
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_i1_conv_ovf_i1_un(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int8 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i1_conv_ovf_i1_un(int8&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_i1_conv_ovf_i1_un(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int8 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i1_conv_ovf_i1_un(int8&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_i1_conv_u1(int8& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i1
    conv.u1
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_i1_conv_u1(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int8 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i1_conv_u1(int8&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static void Test_ldind_i1_conv_ovf_u1(int8& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i1
    conv.ovf.u1
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_i1_conv_ovf_u1(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int8 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i1_conv_ovf_u1(int8&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_i1_conv_ovf_u1(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int8 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i1_conv_ovf_u1(int8&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_i1_conv_ovf_u1_un(int8& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i1
    conv.ovf.u1.un
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_i1_conv_ovf_u1_un(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int8 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i1_conv_ovf_u1_un(int8&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_i1_conv_ovf_u1_un(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int8 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i1_conv_ovf_u1_un(int8&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_i1_conv_i2(int8& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i1
    conv.i2
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_i1_conv_i2(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int8 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i1_conv_i2(int8&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static void Test_ldind_i1_conv_ovf_i2(int8& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i1
    conv.ovf.i2
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_i1_conv_ovf_i2(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int8 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i1_conv_ovf_i2(int8&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_i1_conv_ovf_i2(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int8 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i1_conv_ovf_i2(int8&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_i1_conv_ovf_i2_un(int8& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i1
    conv.ovf.i2.un
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_i1_conv_ovf_i2_un(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int8 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i1_conv_ovf_i2_un(int8&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_i1_conv_ovf_i2_un(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int8 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i1_conv_ovf_i2_un(int8&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_i1_conv_u2(int8& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i1
    conv.u2
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_i1_conv_u2(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int8 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i1_conv_u2(int8&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static void Test_ldind_i1_conv_ovf_u2(int8& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i1
    conv.ovf.u2
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_i1_conv_ovf_u2(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int8 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i1_conv_ovf_u2(int8&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_i1_conv_ovf_u2(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int8 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i1_conv_ovf_u2(int8&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_i1_conv_ovf_u2_un(int8& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i1
    conv.ovf.u2.un
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_i1_conv_ovf_u2_un(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int8 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i1_conv_ovf_u2_un(int8&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_i1_conv_ovf_u2_un(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int8 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i1_conv_ovf_u2_un(int8&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_i1_conv_i4(int8& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i1
    conv.i4
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_i1_conv_i4(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int8 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i1_conv_i4(int8&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static void Test_ldind_i1_conv_ovf_i4(int8& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i1
    conv.ovf.i4
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_i1_conv_ovf_i4(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int8 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i1_conv_ovf_i4(int8&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_i1_conv_ovf_i4(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int8 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i1_conv_ovf_i4(int8&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_i1_conv_ovf_i4_un(int8& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i1
    conv.ovf.i4.un
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_i1_conv_ovf_i4_un(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int8 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i1_conv_ovf_i4_un(int8&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_i1_conv_ovf_i4_un(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int8 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i1_conv_ovf_i4_un(int8&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_i1_conv_u4(int8& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i1
    conv.u4
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_i1_conv_u4(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int8 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i1_conv_u4(int8&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static void Test_ldind_i1_conv_ovf_u4(int8& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i1
    conv.ovf.u4
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_i1_conv_ovf_u4(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int8 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i1_conv_ovf_u4(int8&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_i1_conv_ovf_u4(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int8 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i1_conv_ovf_u4(int8&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_i1_conv_ovf_u4_un(int8& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i1
    conv.ovf.u4.un
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_i1_conv_ovf_u4_un(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int8 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i1_conv_ovf_u4_un(int8&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_i1_conv_ovf_u4_un(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int8 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i1_conv_ovf_u4_un(int8&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_i1_conv_i8(int8& src, int64& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i1
    conv.i8
    stind.i8
    ret
  }

  .method private hidebysig static int32 Check_ldind_i1_conv_i8(int32 input, int64 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int8 src, int64 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i1_conv_i8(int8&, int64&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static void Test_ldind_i1_conv_ovf_i8(int8& src, int64& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i1
    conv.ovf.i8
    stind.i8
    ret
  }

  .method private hidebysig static int32 Check_ldind_i1_conv_ovf_i8(int32 input, int64 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int8 src, int64 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i1_conv_ovf_i8(int8&, int64&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_i1_conv_ovf_i8(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int8 src, int64 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i1_conv_ovf_i8(int8&, int64&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_i1_conv_ovf_i8_un(int8& src, int64& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i1
    conv.ovf.i8.un
    stind.i8
    ret
  }

  .method private hidebysig static int32 Check_ldind_i1_conv_ovf_i8_un(int32 input, int64 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int8 src, int64 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i1_conv_ovf_i8_un(int8&, int64&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_i1_conv_ovf_i8_un(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int8 src, int64 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i1_conv_ovf_i8_un(int8&, int64&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_i1_conv_u8(int8& src, int64& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i1
    conv.u8
    stind.i8
    ret
  }

  .method private hidebysig static int32 Check_ldind_i1_conv_u8(int32 input, int64 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int8 src, int64 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i1_conv_u8(int8&, int64&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static void Test_ldind_i1_conv_ovf_u8(int8& src, int64& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i1
    conv.ovf.u8
    stind.i8
    ret
  }

  .method private hidebysig static int32 Check_ldind_i1_conv_ovf_u8(int32 input, int64 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int8 src, int64 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i1_conv_ovf_u8(int8&, int64&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_i1_conv_ovf_u8(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int8 src, int64 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i1_conv_ovf_u8(int8&, int64&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_i1_conv_ovf_u8_un(int8& src, int64& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i1
    conv.ovf.u8.un
    stind.i8
    ret
  }

  .method private hidebysig static int32 Check_ldind_i1_conv_ovf_u8_un(int32 input, int64 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int8 src, int64 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i1_conv_ovf_u8_un(int8&, int64&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_i1_conv_ovf_u8_un(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int8 src, int64 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i1_conv_ovf_u8_un(int8&, int64&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_u1_conv_i1(uint8& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u1
    conv.i1
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_u1_conv_i1(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint8 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u1_conv_i1(uint8&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static void Test_ldind_u1_conv_ovf_i1(uint8& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u1
    conv.ovf.i1
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_u1_conv_ovf_i1(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint8 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u1_conv_ovf_i1(uint8&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_u1_conv_ovf_i1(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint8 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u1_conv_ovf_i1(uint8&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_u1_conv_ovf_i1_un(uint8& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u1
    conv.ovf.i1.un
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_u1_conv_ovf_i1_un(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint8 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u1_conv_ovf_i1_un(uint8&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_u1_conv_ovf_i1_un(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint8 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u1_conv_ovf_i1_un(uint8&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_u1_conv_u1(uint8& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u1
    conv.u1
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_u1_conv_u1(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint8 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u1_conv_u1(uint8&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static void Test_ldind_u1_conv_ovf_u1(uint8& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u1
    conv.ovf.u1
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_u1_conv_ovf_u1(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint8 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u1_conv_ovf_u1(uint8&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_u1_conv_ovf_u1(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint8 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u1_conv_ovf_u1(uint8&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_u1_conv_ovf_u1_un(uint8& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u1
    conv.ovf.u1.un
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_u1_conv_ovf_u1_un(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint8 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u1_conv_ovf_u1_un(uint8&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_u1_conv_ovf_u1_un(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint8 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u1_conv_ovf_u1_un(uint8&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_u1_conv_i2(uint8& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u1
    conv.i2
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_u1_conv_i2(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint8 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u1_conv_i2(uint8&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static void Test_ldind_u1_conv_ovf_i2(uint8& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u1
    conv.ovf.i2
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_u1_conv_ovf_i2(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint8 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u1_conv_ovf_i2(uint8&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_u1_conv_ovf_i2(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint8 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u1_conv_ovf_i2(uint8&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_u1_conv_ovf_i2_un(uint8& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u1
    conv.ovf.i2.un
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_u1_conv_ovf_i2_un(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint8 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u1_conv_ovf_i2_un(uint8&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_u1_conv_ovf_i2_un(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint8 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u1_conv_ovf_i2_un(uint8&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_u1_conv_u2(uint8& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u1
    conv.u2
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_u1_conv_u2(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint8 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u1_conv_u2(uint8&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static void Test_ldind_u1_conv_ovf_u2(uint8& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u1
    conv.ovf.u2
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_u1_conv_ovf_u2(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint8 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u1_conv_ovf_u2(uint8&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_u1_conv_ovf_u2(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint8 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u1_conv_ovf_u2(uint8&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_u1_conv_ovf_u2_un(uint8& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u1
    conv.ovf.u2.un
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_u1_conv_ovf_u2_un(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint8 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u1_conv_ovf_u2_un(uint8&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_u1_conv_ovf_u2_un(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint8 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u1_conv_ovf_u2_un(uint8&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_u1_conv_i4(uint8& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u1
    conv.i4
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_u1_conv_i4(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint8 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u1_conv_i4(uint8&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static void Test_ldind_u1_conv_ovf_i4(uint8& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u1
    conv.ovf.i4
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_u1_conv_ovf_i4(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint8 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u1_conv_ovf_i4(uint8&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_u1_conv_ovf_i4(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint8 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u1_conv_ovf_i4(uint8&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_u1_conv_ovf_i4_un(uint8& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u1
    conv.ovf.i4.un
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_u1_conv_ovf_i4_un(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint8 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u1_conv_ovf_i4_un(uint8&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_u1_conv_ovf_i4_un(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint8 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u1_conv_ovf_i4_un(uint8&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_u1_conv_u4(uint8& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u1
    conv.u4
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_u1_conv_u4(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint8 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u1_conv_u4(uint8&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static void Test_ldind_u1_conv_ovf_u4(uint8& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u1
    conv.ovf.u4
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_u1_conv_ovf_u4(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint8 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u1_conv_ovf_u4(uint8&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_u1_conv_ovf_u4(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint8 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u1_conv_ovf_u4(uint8&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_u1_conv_ovf_u4_un(uint8& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u1
    conv.ovf.u4.un
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_u1_conv_ovf_u4_un(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint8 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u1_conv_ovf_u4_un(uint8&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_u1_conv_ovf_u4_un(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint8 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u1_conv_ovf_u4_un(uint8&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_u1_conv_i8(uint8& src, int64& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u1
    conv.i8
    stind.i8
    ret
  }

  .method private hidebysig static int32 Check_ldind_u1_conv_i8(int32 input, int64 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint8 src, int64 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u1_conv_i8(uint8&, int64&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static void Test_ldind_u1_conv_ovf_i8(uint8& src, int64& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u1
    conv.ovf.i8
    stind.i8
    ret
  }

  .method private hidebysig static int32 Check_ldind_u1_conv_ovf_i8(int32 input, int64 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint8 src, int64 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u1_conv_ovf_i8(uint8&, int64&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_u1_conv_ovf_i8(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint8 src, int64 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u1_conv_ovf_i8(uint8&, int64&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_u1_conv_ovf_i8_un(uint8& src, int64& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u1
    conv.ovf.i8.un
    stind.i8
    ret
  }

  .method private hidebysig static int32 Check_ldind_u1_conv_ovf_i8_un(int32 input, int64 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint8 src, int64 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u1_conv_ovf_i8_un(uint8&, int64&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_u1_conv_ovf_i8_un(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint8 src, int64 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u1_conv_ovf_i8_un(uint8&, int64&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_u1_conv_u8(uint8& src, int64& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u1
    conv.u8
    stind.i8
    ret
  }

  .method private hidebysig static int32 Check_ldind_u1_conv_u8(int32 input, int64 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint8 src, int64 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u1_conv_u8(uint8&, int64&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static void Test_ldind_u1_conv_ovf_u8(uint8& src, int64& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u1
    conv.ovf.u8
    stind.i8
    ret
  }

  .method private hidebysig static int32 Check_ldind_u1_conv_ovf_u8(int32 input, int64 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint8 src, int64 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u1_conv_ovf_u8(uint8&, int64&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_u1_conv_ovf_u8(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint8 src, int64 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u1_conv_ovf_u8(uint8&, int64&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_u1_conv_ovf_u8_un(uint8& src, int64& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u1
    conv.ovf.u8.un
    stind.i8
    ret
  }

  .method private hidebysig static int32 Check_ldind_u1_conv_ovf_u8_un(int32 input, int64 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint8 src, int64 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u1_conv_ovf_u8_un(uint8&, int64&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_u1_conv_ovf_u8_un(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint8 src, int64 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u1_conv_ovf_u8_un(uint8&, int64&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_i2_conv_i1(int16& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i2
    conv.i1
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_i2_conv_i1(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int16 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i2_conv_i1(int16&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static void Test_ldind_i2_conv_ovf_i1(int16& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i2
    conv.ovf.i1
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_i2_conv_ovf_i1(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int16 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i2_conv_ovf_i1(int16&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_i2_conv_ovf_i1(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int16 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i2_conv_ovf_i1(int16&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_i2_conv_ovf_i1_un(int16& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i2
    conv.ovf.i1.un
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_i2_conv_ovf_i1_un(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int16 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i2_conv_ovf_i1_un(int16&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_i2_conv_ovf_i1_un(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int16 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i2_conv_ovf_i1_un(int16&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_i2_conv_u1(int16& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i2
    conv.u1
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_i2_conv_u1(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int16 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i2_conv_u1(int16&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static void Test_ldind_i2_conv_ovf_u1(int16& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i2
    conv.ovf.u1
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_i2_conv_ovf_u1(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int16 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i2_conv_ovf_u1(int16&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_i2_conv_ovf_u1(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int16 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i2_conv_ovf_u1(int16&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_i2_conv_ovf_u1_un(int16& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i2
    conv.ovf.u1.un
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_i2_conv_ovf_u1_un(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int16 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i2_conv_ovf_u1_un(int16&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_i2_conv_ovf_u1_un(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int16 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i2_conv_ovf_u1_un(int16&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_i2_conv_i2(int16& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i2
    conv.i2
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_i2_conv_i2(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int16 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i2_conv_i2(int16&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static void Test_ldind_i2_conv_ovf_i2(int16& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i2
    conv.ovf.i2
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_i2_conv_ovf_i2(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int16 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i2_conv_ovf_i2(int16&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_i2_conv_ovf_i2(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int16 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i2_conv_ovf_i2(int16&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_i2_conv_ovf_i2_un(int16& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i2
    conv.ovf.i2.un
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_i2_conv_ovf_i2_un(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int16 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i2_conv_ovf_i2_un(int16&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_i2_conv_ovf_i2_un(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int16 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i2_conv_ovf_i2_un(int16&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_i2_conv_u2(int16& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i2
    conv.u2
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_i2_conv_u2(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int16 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i2_conv_u2(int16&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static void Test_ldind_i2_conv_ovf_u2(int16& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i2
    conv.ovf.u2
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_i2_conv_ovf_u2(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int16 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i2_conv_ovf_u2(int16&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_i2_conv_ovf_u2(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int16 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i2_conv_ovf_u2(int16&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_i2_conv_ovf_u2_un(int16& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i2
    conv.ovf.u2.un
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_i2_conv_ovf_u2_un(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int16 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i2_conv_ovf_u2_un(int16&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_i2_conv_ovf_u2_un(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int16 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i2_conv_ovf_u2_un(int16&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_i2_conv_i4(int16& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i2
    conv.i4
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_i2_conv_i4(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int16 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i2_conv_i4(int16&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static void Test_ldind_i2_conv_ovf_i4(int16& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i2
    conv.ovf.i4
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_i2_conv_ovf_i4(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int16 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i2_conv_ovf_i4(int16&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_i2_conv_ovf_i4(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int16 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i2_conv_ovf_i4(int16&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_i2_conv_ovf_i4_un(int16& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i2
    conv.ovf.i4.un
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_i2_conv_ovf_i4_un(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int16 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i2_conv_ovf_i4_un(int16&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_i2_conv_ovf_i4_un(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int16 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i2_conv_ovf_i4_un(int16&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_i2_conv_u4(int16& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i2
    conv.u4
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_i2_conv_u4(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int16 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i2_conv_u4(int16&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static void Test_ldind_i2_conv_ovf_u4(int16& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i2
    conv.ovf.u4
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_i2_conv_ovf_u4(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int16 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i2_conv_ovf_u4(int16&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_i2_conv_ovf_u4(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int16 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i2_conv_ovf_u4(int16&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_i2_conv_ovf_u4_un(int16& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i2
    conv.ovf.u4.un
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_i2_conv_ovf_u4_un(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int16 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i2_conv_ovf_u4_un(int16&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_i2_conv_ovf_u4_un(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int16 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i2_conv_ovf_u4_un(int16&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_i2_conv_i8(int16& src, int64& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i2
    conv.i8
    stind.i8
    ret
  }

  .method private hidebysig static int32 Check_ldind_i2_conv_i8(int32 input, int64 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int16 src, int64 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i2_conv_i8(int16&, int64&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static void Test_ldind_i2_conv_ovf_i8(int16& src, int64& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i2
    conv.ovf.i8
    stind.i8
    ret
  }

  .method private hidebysig static int32 Check_ldind_i2_conv_ovf_i8(int32 input, int64 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int16 src, int64 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i2_conv_ovf_i8(int16&, int64&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_i2_conv_ovf_i8(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int16 src, int64 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i2_conv_ovf_i8(int16&, int64&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_i2_conv_ovf_i8_un(int16& src, int64& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i2
    conv.ovf.i8.un
    stind.i8
    ret
  }

  .method private hidebysig static int32 Check_ldind_i2_conv_ovf_i8_un(int32 input, int64 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int16 src, int64 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i2_conv_ovf_i8_un(int16&, int64&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_i2_conv_ovf_i8_un(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int16 src, int64 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i2_conv_ovf_i8_un(int16&, int64&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_i2_conv_u8(int16& src, int64& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i2
    conv.u8
    stind.i8
    ret
  }

  .method private hidebysig static int32 Check_ldind_i2_conv_u8(int32 input, int64 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int16 src, int64 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i2_conv_u8(int16&, int64&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static void Test_ldind_i2_conv_ovf_u8(int16& src, int64& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i2
    conv.ovf.u8
    stind.i8
    ret
  }

  .method private hidebysig static int32 Check_ldind_i2_conv_ovf_u8(int32 input, int64 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int16 src, int64 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i2_conv_ovf_u8(int16&, int64&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_i2_conv_ovf_u8(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int16 src, int64 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i2_conv_ovf_u8(int16&, int64&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_i2_conv_ovf_u8_un(int16& src, int64& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i2
    conv.ovf.u8.un
    stind.i8
    ret
  }

  .method private hidebysig static int32 Check_ldind_i2_conv_ovf_u8_un(int32 input, int64 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int16 src, int64 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i2_conv_ovf_u8_un(int16&, int64&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_i2_conv_ovf_u8_un(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int16 src, int64 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i2_conv_ovf_u8_un(int16&, int64&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_u2_conv_i1(uint16& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u2
    conv.i1
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_u2_conv_i1(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint16 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u2_conv_i1(uint16&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static void Test_ldind_u2_conv_ovf_i1(uint16& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u2
    conv.ovf.i1
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_u2_conv_ovf_i1(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint16 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u2_conv_ovf_i1(uint16&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_u2_conv_ovf_i1(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint16 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u2_conv_ovf_i1(uint16&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_u2_conv_ovf_i1_un(uint16& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u2
    conv.ovf.i1.un
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_u2_conv_ovf_i1_un(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint16 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u2_conv_ovf_i1_un(uint16&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_u2_conv_ovf_i1_un(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint16 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u2_conv_ovf_i1_un(uint16&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_u2_conv_u1(uint16& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u2
    conv.u1
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_u2_conv_u1(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint16 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u2_conv_u1(uint16&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static void Test_ldind_u2_conv_ovf_u1(uint16& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u2
    conv.ovf.u1
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_u2_conv_ovf_u1(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint16 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u2_conv_ovf_u1(uint16&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_u2_conv_ovf_u1(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint16 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u2_conv_ovf_u1(uint16&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_u2_conv_ovf_u1_un(uint16& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u2
    conv.ovf.u1.un
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_u2_conv_ovf_u1_un(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint16 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u2_conv_ovf_u1_un(uint16&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_u2_conv_ovf_u1_un(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint16 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u2_conv_ovf_u1_un(uint16&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_u2_conv_i2(uint16& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u2
    conv.i2
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_u2_conv_i2(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint16 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u2_conv_i2(uint16&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static void Test_ldind_u2_conv_ovf_i2(uint16& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u2
    conv.ovf.i2
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_u2_conv_ovf_i2(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint16 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u2_conv_ovf_i2(uint16&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_u2_conv_ovf_i2(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint16 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u2_conv_ovf_i2(uint16&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_u2_conv_ovf_i2_un(uint16& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u2
    conv.ovf.i2.un
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_u2_conv_ovf_i2_un(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint16 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u2_conv_ovf_i2_un(uint16&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_u2_conv_ovf_i2_un(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint16 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u2_conv_ovf_i2_un(uint16&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_u2_conv_u2(uint16& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u2
    conv.u2
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_u2_conv_u2(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint16 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u2_conv_u2(uint16&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static void Test_ldind_u2_conv_ovf_u2(uint16& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u2
    conv.ovf.u2
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_u2_conv_ovf_u2(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint16 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u2_conv_ovf_u2(uint16&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_u2_conv_ovf_u2(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint16 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u2_conv_ovf_u2(uint16&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_u2_conv_ovf_u2_un(uint16& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u2
    conv.ovf.u2.un
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_u2_conv_ovf_u2_un(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint16 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u2_conv_ovf_u2_un(uint16&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_u2_conv_ovf_u2_un(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint16 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u2_conv_ovf_u2_un(uint16&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_u2_conv_i4(uint16& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u2
    conv.i4
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_u2_conv_i4(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint16 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u2_conv_i4(uint16&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static void Test_ldind_u2_conv_ovf_i4(uint16& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u2
    conv.ovf.i4
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_u2_conv_ovf_i4(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint16 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u2_conv_ovf_i4(uint16&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_u2_conv_ovf_i4(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint16 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u2_conv_ovf_i4(uint16&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_u2_conv_ovf_i4_un(uint16& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u2
    conv.ovf.i4.un
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_u2_conv_ovf_i4_un(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint16 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u2_conv_ovf_i4_un(uint16&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_u2_conv_ovf_i4_un(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint16 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u2_conv_ovf_i4_un(uint16&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_u2_conv_u4(uint16& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u2
    conv.u4
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_u2_conv_u4(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint16 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u2_conv_u4(uint16&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static void Test_ldind_u2_conv_ovf_u4(uint16& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u2
    conv.ovf.u4
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_u2_conv_ovf_u4(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint16 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u2_conv_ovf_u4(uint16&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_u2_conv_ovf_u4(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint16 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u2_conv_ovf_u4(uint16&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_u2_conv_ovf_u4_un(uint16& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u2
    conv.ovf.u4.un
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_u2_conv_ovf_u4_un(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint16 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u2_conv_ovf_u4_un(uint16&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_u2_conv_ovf_u4_un(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint16 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u2_conv_ovf_u4_un(uint16&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_u2_conv_i8(uint16& src, int64& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u2
    conv.i8
    stind.i8
    ret
  }

  .method private hidebysig static int32 Check_ldind_u2_conv_i8(int32 input, int64 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint16 src, int64 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u2_conv_i8(uint16&, int64&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static void Test_ldind_u2_conv_ovf_i8(uint16& src, int64& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u2
    conv.ovf.i8
    stind.i8
    ret
  }

  .method private hidebysig static int32 Check_ldind_u2_conv_ovf_i8(int32 input, int64 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint16 src, int64 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u2_conv_ovf_i8(uint16&, int64&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_u2_conv_ovf_i8(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint16 src, int64 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u2_conv_ovf_i8(uint16&, int64&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_u2_conv_ovf_i8_un(uint16& src, int64& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u2
    conv.ovf.i8.un
    stind.i8
    ret
  }

  .method private hidebysig static int32 Check_ldind_u2_conv_ovf_i8_un(int32 input, int64 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint16 src, int64 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u2_conv_ovf_i8_un(uint16&, int64&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_u2_conv_ovf_i8_un(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint16 src, int64 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u2_conv_ovf_i8_un(uint16&, int64&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_u2_conv_u8(uint16& src, int64& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u2
    conv.u8
    stind.i8
    ret
  }

  .method private hidebysig static int32 Check_ldind_u2_conv_u8(int32 input, int64 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint16 src, int64 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u2_conv_u8(uint16&, int64&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static void Test_ldind_u2_conv_ovf_u8(uint16& src, int64& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u2
    conv.ovf.u8
    stind.i8
    ret
  }

  .method private hidebysig static int32 Check_ldind_u2_conv_ovf_u8(int32 input, int64 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint16 src, int64 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u2_conv_ovf_u8(uint16&, int64&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_u2_conv_ovf_u8(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint16 src, int64 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u2_conv_ovf_u8(uint16&, int64&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_u2_conv_ovf_u8_un(uint16& src, int64& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u2
    conv.ovf.u8.un
    stind.i8
    ret
  }

  .method private hidebysig static int32 Check_ldind_u2_conv_ovf_u8_un(int32 input, int64 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint16 src, int64 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u2_conv_ovf_u8_un(uint16&, int64&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_u2_conv_ovf_u8_un(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint16 src, int64 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u2_conv_ovf_u8_un(uint16&, int64&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_i4_conv_i1(int32& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i4
    conv.i1
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_i4_conv_i1(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int32 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i4_conv_i1(int32&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static void Test_ldind_i4_conv_ovf_i1(int32& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i4
    conv.ovf.i1
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_i4_conv_ovf_i1(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int32 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i4_conv_ovf_i1(int32&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_i4_conv_ovf_i1(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int32 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i4_conv_ovf_i1(int32&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_i4_conv_ovf_i1_un(int32& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i4
    conv.ovf.i1.un
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_i4_conv_ovf_i1_un(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int32 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i4_conv_ovf_i1_un(int32&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_i4_conv_ovf_i1_un(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int32 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i4_conv_ovf_i1_un(int32&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_i4_conv_u1(int32& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i4
    conv.u1
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_i4_conv_u1(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int32 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i4_conv_u1(int32&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static void Test_ldind_i4_conv_ovf_u1(int32& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i4
    conv.ovf.u1
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_i4_conv_ovf_u1(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int32 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i4_conv_ovf_u1(int32&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_i4_conv_ovf_u1(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int32 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i4_conv_ovf_u1(int32&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_i4_conv_ovf_u1_un(int32& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i4
    conv.ovf.u1.un
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_i4_conv_ovf_u1_un(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int32 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i4_conv_ovf_u1_un(int32&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_i4_conv_ovf_u1_un(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int32 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i4_conv_ovf_u1_un(int32&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_i4_conv_i2(int32& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i4
    conv.i2
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_i4_conv_i2(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int32 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i4_conv_i2(int32&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static void Test_ldind_i4_conv_ovf_i2(int32& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i4
    conv.ovf.i2
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_i4_conv_ovf_i2(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int32 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i4_conv_ovf_i2(int32&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_i4_conv_ovf_i2(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int32 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i4_conv_ovf_i2(int32&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_i4_conv_ovf_i2_un(int32& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i4
    conv.ovf.i2.un
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_i4_conv_ovf_i2_un(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int32 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i4_conv_ovf_i2_un(int32&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_i4_conv_ovf_i2_un(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int32 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i4_conv_ovf_i2_un(int32&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_i4_conv_u2(int32& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i4
    conv.u2
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_i4_conv_u2(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int32 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i4_conv_u2(int32&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static void Test_ldind_i4_conv_ovf_u2(int32& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i4
    conv.ovf.u2
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_i4_conv_ovf_u2(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int32 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i4_conv_ovf_u2(int32&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_i4_conv_ovf_u2(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int32 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i4_conv_ovf_u2(int32&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_i4_conv_ovf_u2_un(int32& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i4
    conv.ovf.u2.un
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_i4_conv_ovf_u2_un(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int32 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i4_conv_ovf_u2_un(int32&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_i4_conv_ovf_u2_un(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int32 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i4_conv_ovf_u2_un(int32&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_i4_conv_i4(int32& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i4
    conv.i4
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_i4_conv_i4(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int32 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i4_conv_i4(int32&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static void Test_ldind_i4_conv_ovf_i4(int32& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i4
    conv.ovf.i4
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_i4_conv_ovf_i4(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int32 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i4_conv_ovf_i4(int32&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_i4_conv_ovf_i4(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int32 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i4_conv_ovf_i4(int32&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_i4_conv_ovf_i4_un(int32& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i4
    conv.ovf.i4.un
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_i4_conv_ovf_i4_un(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int32 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i4_conv_ovf_i4_un(int32&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_i4_conv_ovf_i4_un(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int32 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i4_conv_ovf_i4_un(int32&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_i4_conv_u4(int32& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i4
    conv.u4
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_i4_conv_u4(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int32 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i4_conv_u4(int32&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static void Test_ldind_i4_conv_ovf_u4(int32& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i4
    conv.ovf.u4
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_i4_conv_ovf_u4(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int32 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i4_conv_ovf_u4(int32&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_i4_conv_ovf_u4(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int32 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i4_conv_ovf_u4(int32&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_i4_conv_ovf_u4_un(int32& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i4
    conv.ovf.u4.un
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_i4_conv_ovf_u4_un(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int32 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i4_conv_ovf_u4_un(int32&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_i4_conv_ovf_u4_un(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int32 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i4_conv_ovf_u4_un(int32&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_i4_conv_i8(int32& src, int64& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i4
    conv.i8
    stind.i8
    ret
  }

  .method private hidebysig static int32 Check_ldind_i4_conv_i8(int32 input, int64 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int32 src, int64 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i4_conv_i8(int32&, int64&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static void Test_ldind_i4_conv_ovf_i8(int32& src, int64& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i4
    conv.ovf.i8
    stind.i8
    ret
  }

  .method private hidebysig static int32 Check_ldind_i4_conv_ovf_i8(int32 input, int64 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int32 src, int64 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i4_conv_ovf_i8(int32&, int64&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_i4_conv_ovf_i8(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int32 src, int64 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i4_conv_ovf_i8(int32&, int64&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_i4_conv_ovf_i8_un(int32& src, int64& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i4
    conv.ovf.i8.un
    stind.i8
    ret
  }

  .method private hidebysig static int32 Check_ldind_i4_conv_ovf_i8_un(int32 input, int64 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int32 src, int64 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i4_conv_ovf_i8_un(int32&, int64&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_i4_conv_ovf_i8_un(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int32 src, int64 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i4_conv_ovf_i8_un(int32&, int64&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_i4_conv_u8(int32& src, int64& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i4
    conv.u8
    stind.i8
    ret
  }

  .method private hidebysig static int32 Check_ldind_i4_conv_u8(int32 input, int64 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int32 src, int64 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i4_conv_u8(int32&, int64&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static void Test_ldind_i4_conv_ovf_u8(int32& src, int64& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i4
    conv.ovf.u8
    stind.i8
    ret
  }

  .method private hidebysig static int32 Check_ldind_i4_conv_ovf_u8(int32 input, int64 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int32 src, int64 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i4_conv_ovf_u8(int32&, int64&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_i4_conv_ovf_u8(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int32 src, int64 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i4_conv_ovf_u8(int32&, int64&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_i4_conv_ovf_u8_un(int32& src, int64& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i4
    conv.ovf.u8.un
    stind.i8
    ret
  }

  .method private hidebysig static int32 Check_ldind_i4_conv_ovf_u8_un(int32 input, int64 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int32 src, int64 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i4_conv_ovf_u8_un(int32&, int64&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_i4_conv_ovf_u8_un(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int32 src, int64 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i4_conv_ovf_u8_un(int32&, int64&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_u4_conv_i1(uint32& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u4
    conv.i1
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_u4_conv_i1(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint32 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u4_conv_i1(uint32&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static void Test_ldind_u4_conv_ovf_i1(uint32& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u4
    conv.ovf.i1
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_u4_conv_ovf_i1(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint32 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u4_conv_ovf_i1(uint32&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_u4_conv_ovf_i1(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint32 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u4_conv_ovf_i1(uint32&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_u4_conv_ovf_i1_un(uint32& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u4
    conv.ovf.i1.un
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_u4_conv_ovf_i1_un(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint32 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u4_conv_ovf_i1_un(uint32&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_u4_conv_ovf_i1_un(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint32 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u4_conv_ovf_i1_un(uint32&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_u4_conv_u1(uint32& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u4
    conv.u1
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_u4_conv_u1(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint32 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u4_conv_u1(uint32&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static void Test_ldind_u4_conv_ovf_u1(uint32& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u4
    conv.ovf.u1
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_u4_conv_ovf_u1(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint32 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u4_conv_ovf_u1(uint32&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_u4_conv_ovf_u1(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint32 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u4_conv_ovf_u1(uint32&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_u4_conv_ovf_u1_un(uint32& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u4
    conv.ovf.u1.un
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_u4_conv_ovf_u1_un(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint32 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u4_conv_ovf_u1_un(uint32&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_u4_conv_ovf_u1_un(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint32 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u4_conv_ovf_u1_un(uint32&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_u4_conv_i2(uint32& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u4
    conv.i2
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_u4_conv_i2(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint32 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u4_conv_i2(uint32&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static void Test_ldind_u4_conv_ovf_i2(uint32& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u4
    conv.ovf.i2
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_u4_conv_ovf_i2(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint32 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u4_conv_ovf_i2(uint32&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_u4_conv_ovf_i2(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint32 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u4_conv_ovf_i2(uint32&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_u4_conv_ovf_i2_un(uint32& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u4
    conv.ovf.i2.un
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_u4_conv_ovf_i2_un(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint32 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u4_conv_ovf_i2_un(uint32&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_u4_conv_ovf_i2_un(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint32 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u4_conv_ovf_i2_un(uint32&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_u4_conv_u2(uint32& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u4
    conv.u2
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_u4_conv_u2(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint32 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u4_conv_u2(uint32&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static void Test_ldind_u4_conv_ovf_u2(uint32& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u4
    conv.ovf.u2
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_u4_conv_ovf_u2(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint32 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u4_conv_ovf_u2(uint32&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_u4_conv_ovf_u2(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint32 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u4_conv_ovf_u2(uint32&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_u4_conv_ovf_u2_un(uint32& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u4
    conv.ovf.u2.un
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_u4_conv_ovf_u2_un(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint32 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u4_conv_ovf_u2_un(uint32&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_u4_conv_ovf_u2_un(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint32 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u4_conv_ovf_u2_un(uint32&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_u4_conv_i4(uint32& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u4
    conv.i4
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_u4_conv_i4(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint32 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u4_conv_i4(uint32&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static void Test_ldind_u4_conv_ovf_i4(uint32& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u4
    conv.ovf.i4
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_u4_conv_ovf_i4(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint32 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u4_conv_ovf_i4(uint32&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_u4_conv_ovf_i4(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint32 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u4_conv_ovf_i4(uint32&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_u4_conv_ovf_i4_un(uint32& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u4
    conv.ovf.i4.un
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_u4_conv_ovf_i4_un(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint32 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u4_conv_ovf_i4_un(uint32&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_u4_conv_ovf_i4_un(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint32 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u4_conv_ovf_i4_un(uint32&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_u4_conv_u4(uint32& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u4
    conv.u4
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_u4_conv_u4(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint32 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u4_conv_u4(uint32&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static void Test_ldind_u4_conv_ovf_u4(uint32& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u4
    conv.ovf.u4
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_u4_conv_ovf_u4(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint32 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u4_conv_ovf_u4(uint32&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_u4_conv_ovf_u4(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint32 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u4_conv_ovf_u4(uint32&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_u4_conv_ovf_u4_un(uint32& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u4
    conv.ovf.u4.un
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_u4_conv_ovf_u4_un(int32 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint32 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u4_conv_ovf_u4_un(uint32&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_u4_conv_ovf_u4_un(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint32 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u4_conv_ovf_u4_un(uint32&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_u4_conv_i8(uint32& src, int64& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u4
    conv.i8
    stind.i8
    ret
  }

  .method private hidebysig static int32 Check_ldind_u4_conv_i8(int32 input, int64 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint32 src, int64 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u4_conv_i8(uint32&, int64&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static void Test_ldind_u4_conv_ovf_i8(uint32& src, int64& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u4
    conv.ovf.i8
    stind.i8
    ret
  }

  .method private hidebysig static int32 Check_ldind_u4_conv_ovf_i8(int32 input, int64 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint32 src, int64 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u4_conv_ovf_i8(uint32&, int64&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_u4_conv_ovf_i8(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint32 src, int64 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u4_conv_ovf_i8(uint32&, int64&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_u4_conv_ovf_i8_un(uint32& src, int64& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u4
    conv.ovf.i8.un
    stind.i8
    ret
  }

  .method private hidebysig static int32 Check_ldind_u4_conv_ovf_i8_un(int32 input, int64 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint32 src, int64 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u4_conv_ovf_i8_un(uint32&, int64&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_u4_conv_ovf_i8_un(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint32 src, int64 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u4_conv_ovf_i8_un(uint32&, int64&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_u4_conv_u8(uint32& src, int64& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u4
    conv.u8
    stind.i8
    ret
  }

  .method private hidebysig static int32 Check_ldind_u4_conv_u8(int32 input, int64 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint32 src, int64 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u4_conv_u8(uint32&, int64&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static void Test_ldind_u4_conv_ovf_u8(uint32& src, int64& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u4
    conv.ovf.u8
    stind.i8
    ret
  }

  .method private hidebysig static int32 Check_ldind_u4_conv_ovf_u8(int32 input, int64 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint32 src, int64 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u4_conv_ovf_u8(uint32&, int64&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_u4_conv_ovf_u8(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint32 src, int64 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u4_conv_ovf_u8(uint32&, int64&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_u4_conv_ovf_u8_un(uint32& src, int64& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u4
    conv.ovf.u8.un
    stind.i8
    ret
  }

  .method private hidebysig static int32 Check_ldind_u4_conv_ovf_u8_un(int32 input, int64 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint32 src, int64 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u4_conv_ovf_u8_un(uint32&, int64&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_u4_conv_ovf_u8_un(int32 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint32 src, int64 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u4_conv_ovf_u8_un(uint32&, int64&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_i8_conv_i1(int64& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i8
    conv.i1
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_i8_conv_i1(int64 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int64 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i8_conv_i1(int64&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static void Test_ldind_i8_conv_ovf_i1(int64& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i8
    conv.ovf.i1
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_i8_conv_ovf_i1(int64 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int64 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i8_conv_ovf_i1(int64&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_i8_conv_ovf_i1(int64 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int64 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i8_conv_ovf_i1(int64&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_i8_conv_ovf_i1_un(int64& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i8
    conv.ovf.i1.un
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_i8_conv_ovf_i1_un(int64 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int64 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i8_conv_ovf_i1_un(int64&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_i8_conv_ovf_i1_un(int64 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int64 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i8_conv_ovf_i1_un(int64&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_i8_conv_u1(int64& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i8
    conv.u1
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_i8_conv_u1(int64 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int64 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i8_conv_u1(int64&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static void Test_ldind_i8_conv_ovf_u1(int64& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i8
    conv.ovf.u1
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_i8_conv_ovf_u1(int64 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int64 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i8_conv_ovf_u1(int64&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_i8_conv_ovf_u1(int64 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int64 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i8_conv_ovf_u1(int64&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_i8_conv_ovf_u1_un(int64& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i8
    conv.ovf.u1.un
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_i8_conv_ovf_u1_un(int64 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int64 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i8_conv_ovf_u1_un(int64&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_i8_conv_ovf_u1_un(int64 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int64 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i8_conv_ovf_u1_un(int64&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_i8_conv_i2(int64& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i8
    conv.i2
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_i8_conv_i2(int64 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int64 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i8_conv_i2(int64&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static void Test_ldind_i8_conv_ovf_i2(int64& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i8
    conv.ovf.i2
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_i8_conv_ovf_i2(int64 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int64 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i8_conv_ovf_i2(int64&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_i8_conv_ovf_i2(int64 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int64 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i8_conv_ovf_i2(int64&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_i8_conv_ovf_i2_un(int64& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i8
    conv.ovf.i2.un
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_i8_conv_ovf_i2_un(int64 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int64 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i8_conv_ovf_i2_un(int64&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_i8_conv_ovf_i2_un(int64 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int64 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i8_conv_ovf_i2_un(int64&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_i8_conv_u2(int64& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i8
    conv.u2
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_i8_conv_u2(int64 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int64 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i8_conv_u2(int64&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static void Test_ldind_i8_conv_ovf_u2(int64& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i8
    conv.ovf.u2
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_i8_conv_ovf_u2(int64 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int64 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i8_conv_ovf_u2(int64&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_i8_conv_ovf_u2(int64 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int64 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i8_conv_ovf_u2(int64&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_i8_conv_ovf_u2_un(int64& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i8
    conv.ovf.u2.un
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_i8_conv_ovf_u2_un(int64 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int64 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i8_conv_ovf_u2_un(int64&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_i8_conv_ovf_u2_un(int64 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int64 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i8_conv_ovf_u2_un(int64&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_i8_conv_i4(int64& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i8
    conv.i4
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_i8_conv_i4(int64 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int64 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i8_conv_i4(int64&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static void Test_ldind_i8_conv_ovf_i4(int64& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i8
    conv.ovf.i4
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_i8_conv_ovf_i4(int64 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int64 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i8_conv_ovf_i4(int64&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_i8_conv_ovf_i4(int64 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int64 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i8_conv_ovf_i4(int64&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_i8_conv_ovf_i4_un(int64& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i8
    conv.ovf.i4.un
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_i8_conv_ovf_i4_un(int64 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int64 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i8_conv_ovf_i4_un(int64&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_i8_conv_ovf_i4_un(int64 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int64 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i8_conv_ovf_i4_un(int64&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_i8_conv_u4(int64& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i8
    conv.u4
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_i8_conv_u4(int64 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int64 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i8_conv_u4(int64&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static void Test_ldind_i8_conv_ovf_u4(int64& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i8
    conv.ovf.u4
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_i8_conv_ovf_u4(int64 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int64 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i8_conv_ovf_u4(int64&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_i8_conv_ovf_u4(int64 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int64 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i8_conv_ovf_u4(int64&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_i8_conv_ovf_u4_un(int64& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i8
    conv.ovf.u4.un
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_i8_conv_ovf_u4_un(int64 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int64 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i8_conv_ovf_u4_un(int64&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_i8_conv_ovf_u4_un(int64 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int64 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i8_conv_ovf_u4_un(int64&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_i8_conv_i8(int64& src, int64& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i8
    conv.i8
    stind.i8
    ret
  }

  .method private hidebysig static int32 Check_ldind_i8_conv_i8(int64 input, int64 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int64 src, int64 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i8_conv_i8(int64&, int64&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static void Test_ldind_i8_conv_ovf_i8(int64& src, int64& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i8
    conv.ovf.i8
    stind.i8
    ret
  }

  .method private hidebysig static int32 Check_ldind_i8_conv_ovf_i8(int64 input, int64 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int64 src, int64 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i8_conv_ovf_i8(int64&, int64&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_i8_conv_ovf_i8(int64 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int64 src, int64 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i8_conv_ovf_i8(int64&, int64&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_i8_conv_ovf_i8_un(int64& src, int64& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i8
    conv.ovf.i8.un
    stind.i8
    ret
  }

  .method private hidebysig static int32 Check_ldind_i8_conv_ovf_i8_un(int64 input, int64 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int64 src, int64 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i8_conv_ovf_i8_un(int64&, int64&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_i8_conv_ovf_i8_un(int64 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int64 src, int64 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i8_conv_ovf_i8_un(int64&, int64&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_i8_conv_u8(int64& src, int64& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i8
    conv.u8
    stind.i8
    ret
  }

  .method private hidebysig static int32 Check_ldind_i8_conv_u8(int64 input, int64 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int64 src, int64 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i8_conv_u8(int64&, int64&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static void Test_ldind_i8_conv_ovf_u8(int64& src, int64& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i8
    conv.ovf.u8
    stind.i8
    ret
  }

  .method private hidebysig static int32 Check_ldind_i8_conv_ovf_u8(int64 input, int64 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int64 src, int64 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i8_conv_ovf_u8(int64&, int64&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_i8_conv_ovf_u8(int64 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int64 src, int64 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i8_conv_ovf_u8(int64&, int64&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_i8_conv_ovf_u8_un(int64& src, int64& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.i8
    conv.ovf.u8.un
    stind.i8
    ret
  }

  .method private hidebysig static int32 Check_ldind_i8_conv_ovf_u8_un(int64 input, int64 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int64 src, int64 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i8_conv_ovf_u8_un(int64&, int64&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_i8_conv_ovf_u8_un(int64 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(int64 src, int64 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_i8_conv_ovf_u8_un(int64&, int64&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_u8_conv_i1(uint64& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u8
    conv.i1
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_u8_conv_i1(int64 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint64 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u8_conv_i1(uint64&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static void Test_ldind_u8_conv_ovf_i1(uint64& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u8
    conv.ovf.i1
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_u8_conv_ovf_i1(int64 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint64 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u8_conv_ovf_i1(uint64&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_u8_conv_ovf_i1(int64 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint64 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u8_conv_ovf_i1(uint64&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_u8_conv_ovf_i1_un(uint64& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u8
    conv.ovf.i1.un
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_u8_conv_ovf_i1_un(int64 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint64 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u8_conv_ovf_i1_un(uint64&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_u8_conv_ovf_i1_un(int64 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint64 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u8_conv_ovf_i1_un(uint64&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_u8_conv_u1(uint64& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u8
    conv.u1
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_u8_conv_u1(int64 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint64 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u8_conv_u1(uint64&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static void Test_ldind_u8_conv_ovf_u1(uint64& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u8
    conv.ovf.u1
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_u8_conv_ovf_u1(int64 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint64 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u8_conv_ovf_u1(uint64&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_u8_conv_ovf_u1(int64 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint64 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u8_conv_ovf_u1(uint64&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_u8_conv_ovf_u1_un(uint64& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u8
    conv.ovf.u1.un
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_u8_conv_ovf_u1_un(int64 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint64 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u8_conv_ovf_u1_un(uint64&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_u8_conv_ovf_u1_un(int64 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint64 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u8_conv_ovf_u1_un(uint64&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_u8_conv_i2(uint64& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u8
    conv.i2
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_u8_conv_i2(int64 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint64 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u8_conv_i2(uint64&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static void Test_ldind_u8_conv_ovf_i2(uint64& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u8
    conv.ovf.i2
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_u8_conv_ovf_i2(int64 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint64 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u8_conv_ovf_i2(uint64&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_u8_conv_ovf_i2(int64 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint64 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u8_conv_ovf_i2(uint64&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_u8_conv_ovf_i2_un(uint64& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u8
    conv.ovf.i2.un
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_u8_conv_ovf_i2_un(int64 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint64 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u8_conv_ovf_i2_un(uint64&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_u8_conv_ovf_i2_un(int64 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint64 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u8_conv_ovf_i2_un(uint64&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_u8_conv_u2(uint64& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u8
    conv.u2
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_u8_conv_u2(int64 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint64 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u8_conv_u2(uint64&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static void Test_ldind_u8_conv_ovf_u2(uint64& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u8
    conv.ovf.u2
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_u8_conv_ovf_u2(int64 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint64 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u8_conv_ovf_u2(uint64&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_u8_conv_ovf_u2(int64 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint64 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u8_conv_ovf_u2(uint64&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_u8_conv_ovf_u2_un(uint64& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u8
    conv.ovf.u2.un
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_u8_conv_ovf_u2_un(int64 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint64 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u8_conv_ovf_u2_un(uint64&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_u8_conv_ovf_u2_un(int64 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint64 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u8_conv_ovf_u2_un(uint64&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_u8_conv_i4(uint64& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u8
    conv.i4
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_u8_conv_i4(int64 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint64 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u8_conv_i4(uint64&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static void Test_ldind_u8_conv_ovf_i4(uint64& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u8
    conv.ovf.i4
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_u8_conv_ovf_i4(int64 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint64 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u8_conv_ovf_i4(uint64&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_u8_conv_ovf_i4(int64 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint64 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u8_conv_ovf_i4(uint64&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_u8_conv_ovf_i4_un(uint64& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u8
    conv.ovf.i4.un
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_u8_conv_ovf_i4_un(int64 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint64 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u8_conv_ovf_i4_un(uint64&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_u8_conv_ovf_i4_un(int64 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint64 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u8_conv_ovf_i4_un(uint64&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_u8_conv_u4(uint64& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u8
    conv.u4
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_u8_conv_u4(int64 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint64 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u8_conv_u4(uint64&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static void Test_ldind_u8_conv_ovf_u4(uint64& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u8
    conv.ovf.u4
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_u8_conv_ovf_u4(int64 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint64 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u8_conv_ovf_u4(uint64&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_u8_conv_ovf_u4(int64 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint64 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u8_conv_ovf_u4(uint64&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_u8_conv_ovf_u4_un(uint64& src, int32& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u8
    conv.ovf.u4.un
    stind.i4
    ret
  }

  .method private hidebysig static int32 Check_ldind_u8_conv_ovf_u4_un(int64 input, int32 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint64 src, int32 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u8_conv_ovf_u4_un(uint64&, int32&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_u8_conv_ovf_u4_un(int64 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint64 src, int32 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u8_conv_ovf_u4_un(uint64&, int32&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_u8_conv_i8(uint64& src, int64& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u8
    conv.i8
    stind.i8
    ret
  }

  .method private hidebysig static int32 Check_ldind_u8_conv_i8(int64 input, int64 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint64 src, int64 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u8_conv_i8(uint64&, int64&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static void Test_ldind_u8_conv_ovf_i8(uint64& src, int64& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u8
    conv.ovf.i8
    stind.i8
    ret
  }

  .method private hidebysig static int32 Check_ldind_u8_conv_ovf_i8(int64 input, int64 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint64 src, int64 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u8_conv_ovf_i8(uint64&, int64&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_u8_conv_ovf_i8(int64 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint64 src, int64 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u8_conv_ovf_i8(uint64&, int64&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_u8_conv_ovf_i8_un(uint64& src, int64& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u8
    conv.ovf.i8.un
    stind.i8
    ret
  }

  .method private hidebysig static int32 Check_ldind_u8_conv_ovf_i8_un(int64 input, int64 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint64 src, int64 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u8_conv_ovf_i8_un(uint64&, int64&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_u8_conv_ovf_i8_un(int64 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint64 src, int64 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u8_conv_ovf_i8_un(uint64&, int64&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_u8_conv_u8(uint64& src, int64& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u8
    conv.u8
    stind.i8
    ret
  }

  .method private hidebysig static int32 Check_ldind_u8_conv_u8(int64 input, int64 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint64 src, int64 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u8_conv_u8(uint64&, int64&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static void Test_ldind_u8_conv_ovf_u8(uint64& src, int64& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u8
    conv.ovf.u8
    stind.i8
    ret
  }

  .method private hidebysig static int32 Check_ldind_u8_conv_ovf_u8(int64 input, int64 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint64 src, int64 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u8_conv_ovf_u8(uint64&, int64&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_u8_conv_ovf_u8(int64 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint64 src, int64 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u8_conv_ovf_u8(uint64&, int64&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }

  .method private hidebysig static void Test_ldind_u8_conv_ovf_u8_un(uint64& src, int64& dst) cil managed noinlining
  {
    .maxstack 4
    ldarg.1
    ldarg.0
    ldind.u8
    conv.ovf.u8.un
    stind.i8
    ret
  }

  .method private hidebysig static int32 Check_ldind_u8_conv_ovf_u8_un(int64 input, int64 expected, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint64 src, int64 dst)
    ldarg.2
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u8_conv_ovf_u8_un(uint64&, int64&)
    ldloc.1
    ldarg.1
    ceq
    ret
  }

  .method private hidebysig static int32 CheckOvf_ldind_u8_conv_ovf_u8_un(int64 input, class [System.Runtime]System.String desc) cil managed noinlining
  {
    .maxstack 4
    .locals init(uint64 src, int64 dst, int32 ovf)
    ldarg.1
    call void Program::print(class [System.Runtime]System.String)
    ldarg.0
    stloc 0
    .try {
    ldloca 0
    ldloca 1
    call void Program::Test_ldind_u8_conv_ovf_u8_un(uint64&, int64&)
    leave END
    } catch [System.Runtime]System.OverflowException {
    ldc.i4 1
    stloc 2
    leave END
    }
    END: ldloc.2
    ret
  }
}
