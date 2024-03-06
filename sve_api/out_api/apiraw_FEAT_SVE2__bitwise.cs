namespace System.Runtime.Intrinsics.Arm;

/// VectorT Summary
public abstract partial class Sve2 : AdvSimd /// Feature: FEAT_SVE2  Category: bitwise
{

  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> BitwiseClearXor(Vector<T> xor, Vector<T> value, Vector<T> mask); // BCAX // MOVPRFX

  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> BitwiseSelect(Vector<T> select, Vector<T> left, Vector<T> right); // BSL // MOVPRFX

  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> BitwiseSelectLeftInverted(Vector<T> select, Vector<T> left, Vector<T> right); // BSL1N // MOVPRFX

  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> BitwiseSelectRightInverted(Vector<T> select, Vector<T> left, Vector<T> right); // BSL2N // MOVPRFX

  /// T: sbyte, short, int, long
  public static unsafe Vector<T> ShiftArithmeticRounded(Vector<T> value, Vector<T> count); // SRSHL or SRSHLR // predicated, MOVPRFX

  /// T: sbyte, short, int, long
  public static unsafe Vector<T> ShiftArithmeticRoundedSaturate(Vector<T> value, Vector<T> count); // SQRSHL or SQRSHLR // predicated, MOVPRFX

  /// T: sbyte, short, int, long
  public static unsafe Vector<T> ShiftArithmeticSaturate(Vector<T> value, Vector<T> count); // SQSHL or SQSHLR // predicated, MOVPRFX

  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> ShiftLeftAndInsert(Vector<T> left, Vector<T> right, [ConstantExpected] byte shift); // SLI

  /// T: [byte, sbyte], [ushort, short], [uint, int], [ulong, long]
  public static unsafe Vector<T> ShiftLeftLogicalSaturate(Vector<T> value, Vector<T2> count); // UQSHL or UQSHLR // predicated, MOVPRFX

  /// T: [byte, sbyte], [ushort, short], [uint, int], [ulong, long]
  public static unsafe Vector<T> ShiftLeftLogicalSaturateUnsigned(Vector<T2> value, [ConstantExpected] byte count); // SQSHLU // predicated, MOVPRFX

  /// T: [short, sbyte], [int, short], [long, int], [ushort, byte], [uint, ushort], [ulong, uint]
  public static unsafe Vector<T> ShiftLeftLogicalWideningEven(Vector<T2> value, [ConstantExpected] byte count); // SSHLLB or USHLLB

  /// T: [short, sbyte], [int, short], [long, int], [ushort, byte], [uint, ushort], [ulong, uint]
  public static unsafe Vector<T> ShiftLeftLogicalWideningOdd(Vector<T2> value, [ConstantExpected] byte count); // SSHLLT or USHLLT

  /// T: [byte, sbyte], [ushort, short], [uint, int], [ulong, long]
  public static unsafe Vector<T> ShiftLogicalRounded(Vector<T> value, Vector<T2> count); // URSHL or URSHLR // predicated, MOVPRFX

  /// T: [byte, sbyte], [ushort, short], [uint, int], [ulong, long]
  public static unsafe Vector<T> ShiftLogicalRoundedSaturate(Vector<T> value, Vector<T2> count); // UQRSHL or UQRSHLR // predicated, MOVPRFX

  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> ShiftRightAndInsert(Vector<T> left, Vector<T> right, [ConstantExpected] byte shift); // SRI

  /// T: sbyte, short, int, long
  public static unsafe Vector<T> ShiftRightArithmeticAdd(Vector<T> addend, Vector<T> value, [ConstantExpected] byte count); // SSRA // MOVPRFX

  /// T: [sbyte, short], [short, int], [int, long], [byte, ushort], [ushort, uint], [uint, ulong]
  public static unsafe Vector<T> ShiftRightArithmeticNarrowingSaturateEven(Vector<T2> value, [ConstantExpected] byte count); // SQSHRNB or UQSHRNB

  /// T: [sbyte, short], [short, int], [int, long], [byte, ushort], [ushort, uint], [uint, ulong]
  public static unsafe Vector<T> ShiftRightArithmeticNarrowingSaturateOdd(Vector<T> even, Vector<T2> value, [ConstantExpected] byte count); // SQSHRNT or UQSHRNT

  /// T: [byte, short], [ushort, int], [uint, long]
  public static unsafe Vector<T> ShiftRightArithmeticNarrowingSaturateUnsignedEven(Vector<T2> value, [ConstantExpected] byte count); // SQSHRUNB

  /// T: [byte, short], [ushort, int], [uint, long]
  public static unsafe Vector<T> ShiftRightArithmeticNarrowingSaturateUnsignedOdd(Vector<T> even, Vector<T2> value, [ConstantExpected] byte count); // SQSHRUNT

  /// T: sbyte, short, int, long
  public static unsafe Vector<T> ShiftRightArithmeticRounded(Vector<T> value, [ConstantExpected] byte count); // SRSHR // predicated, MOVPRFX

  /// T: sbyte, short, int, long
  public static unsafe Vector<T> ShiftRightArithmeticRoundedAdd(Vector<T> addend, Vector<T> value, [ConstantExpected] byte count); // SRSRA // MOVPRFX

  /// T: [sbyte, short], [short, int], [int, long]
  public static unsafe Vector<T> ShiftRightArithmeticRoundedNarrowingSaturateEven(Vector<T2> value, [ConstantExpected] byte count); // SQRSHRNB

  /// T: [sbyte, short], [short, int], [int, long]
  public static unsafe Vector<T> ShiftRightArithmeticRoundedNarrowingSaturateOdd(Vector<T> even, Vector<T2> value, [ConstantExpected] byte count); // SQRSHRNT

  /// T: [byte, short], [ushort, int], [uint, long]
  public static unsafe Vector<T> ShiftRightArithmeticRoundedNarrowingSaturateUnsignedEven(Vector<T2> value, [ConstantExpected] byte count); // SQRSHRUNB

  /// T: [byte, short], [ushort, int], [uint, long]
  public static unsafe Vector<T> ShiftRightArithmeticRoundedNarrowingSaturateUnsignedOdd(Vector<T> even, Vector<T2> value, [ConstantExpected] byte count); // SQRSHRUNT

  /// T: byte, ushort, uint, ulong
  public static unsafe Vector<T> ShiftRightLogicalAdd(Vector<T> addend, Vector<T> value, [ConstantExpected] byte count); // USRA // MOVPRFX

  /// T: [sbyte, short], [short, int], [int, long], [byte, ushort], [ushort, uint], [uint, ulong]
  public static unsafe Vector<T> ShiftRightLogicalNarrowingEven(Vector<T2> value, [ConstantExpected] byte count); // SHRNB

  /// T: [sbyte, short], [short, int], [int, long], [byte, ushort], [ushort, uint], [uint, ulong]
  public static unsafe Vector<T> ShiftRightLogicalNarrowingOdd(Vector<T> even, Vector<T2> value, [ConstantExpected] byte count); // SHRNT

  /// T: byte, ushort, uint, ulong
  public static unsafe Vector<T> ShiftRightLogicalRounded(Vector<T> value, [ConstantExpected] byte count); // URSHR // predicated, MOVPRFX

  /// T: byte, ushort, uint, ulong
  public static unsafe Vector<T> ShiftRightLogicalRoundedAdd(Vector<T> addend, Vector<T> value, [ConstantExpected] byte count); // URSRA // MOVPRFX

  /// T: [sbyte, short], [short, int], [int, long], [byte, ushort], [ushort, uint], [uint, ulong]
  public static unsafe Vector<T> ShiftRightLogicalRoundedNarrowingEven(Vector<T2> value, [ConstantExpected] byte count); // RSHRNB

  /// T: [sbyte, short], [short, int], [int, long], [byte, ushort], [ushort, uint], [uint, ulong]
  public static unsafe Vector<T> ShiftRightLogicalRoundedNarrowingOdd(Vector<T> even, Vector<T2> value, [ConstantExpected] byte count); // RSHRNT

  /// T: [byte, ushort], [ushort, uint], [uint, ulong]
  public static unsafe Vector<T> ShiftRightLogicalRoundedNarrowingSaturateEven(Vector<T2> value, [ConstantExpected] byte count); // UQRSHRNB

  /// T: [byte, ushort], [ushort, uint], [uint, ulong]
  public static unsafe Vector<T> ShiftRightLogicalRoundedNarrowingSaturateOdd(Vector<T> even, Vector<T2> value, [ConstantExpected] byte count); // UQRSHRNT

  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> Xor(Vector<T> value1, Vector<T> value2, Vector<T> value3); // EOR3 // MOVPRFX

  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> XorRotateRight(Vector<T> left, Vector<T> right, [ConstantExpected] byte count); // XAR // MOVPRFX

  /// total method signatures: 37

}


/// Full API
public abstract partial class Sve2 : AdvSimd /// Feature: FEAT_SVE2  Category: bitwise
{
    /// BitwiseClearXor : Bitwise clear and exclusive OR

    /// svint8_t svbcax[_s8](svint8_t op1, svint8_t op2, svint8_t op3) : "BCAX Ztied1.D, Ztied1.D, Zop2.D, Zop3.D" or "MOVPRFX Zresult, Zop1; BCAX Zresult.D, Zresult.D, Zop2.D, Zop3.D"
  public static unsafe Vector<sbyte> BitwiseClearXor(Vector<sbyte> xor, Vector<sbyte> value, Vector<sbyte> mask);

    /// svint16_t svbcax[_s16](svint16_t op1, svint16_t op2, svint16_t op3) : "BCAX Ztied1.D, Ztied1.D, Zop2.D, Zop3.D" or "MOVPRFX Zresult, Zop1; BCAX Zresult.D, Zresult.D, Zop2.D, Zop3.D"
  public static unsafe Vector<short> BitwiseClearXor(Vector<short> xor, Vector<short> value, Vector<short> mask);

    /// svint32_t svbcax[_s32](svint32_t op1, svint32_t op2, svint32_t op3) : "BCAX Ztied1.D, Ztied1.D, Zop2.D, Zop3.D" or "MOVPRFX Zresult, Zop1; BCAX Zresult.D, Zresult.D, Zop2.D, Zop3.D"
  public static unsafe Vector<int> BitwiseClearXor(Vector<int> xor, Vector<int> value, Vector<int> mask);

    /// svint64_t svbcax[_s64](svint64_t op1, svint64_t op2, svint64_t op3) : "BCAX Ztied1.D, Ztied1.D, Zop2.D, Zop3.D" or "MOVPRFX Zresult, Zop1; BCAX Zresult.D, Zresult.D, Zop2.D, Zop3.D"
  public static unsafe Vector<long> BitwiseClearXor(Vector<long> xor, Vector<long> value, Vector<long> mask);

    /// svuint8_t svbcax[_u8](svuint8_t op1, svuint8_t op2, svuint8_t op3) : "BCAX Ztied1.D, Ztied1.D, Zop2.D, Zop3.D" or "MOVPRFX Zresult, Zop1; BCAX Zresult.D, Zresult.D, Zop2.D, Zop3.D"
  public static unsafe Vector<byte> BitwiseClearXor(Vector<byte> xor, Vector<byte> value, Vector<byte> mask);

    /// svuint16_t svbcax[_u16](svuint16_t op1, svuint16_t op2, svuint16_t op3) : "BCAX Ztied1.D, Ztied1.D, Zop2.D, Zop3.D" or "MOVPRFX Zresult, Zop1; BCAX Zresult.D, Zresult.D, Zop2.D, Zop3.D"
  public static unsafe Vector<ushort> BitwiseClearXor(Vector<ushort> xor, Vector<ushort> value, Vector<ushort> mask);

    /// svuint32_t svbcax[_u32](svuint32_t op1, svuint32_t op2, svuint32_t op3) : "BCAX Ztied1.D, Ztied1.D, Zop2.D, Zop3.D" or "MOVPRFX Zresult, Zop1; BCAX Zresult.D, Zresult.D, Zop2.D, Zop3.D"
  public static unsafe Vector<uint> BitwiseClearXor(Vector<uint> xor, Vector<uint> value, Vector<uint> mask);

    /// svuint64_t svbcax[_u64](svuint64_t op1, svuint64_t op2, svuint64_t op3) : "BCAX Ztied1.D, Ztied1.D, Zop2.D, Zop3.D" or "MOVPRFX Zresult, Zop1; BCAX Zresult.D, Zresult.D, Zop2.D, Zop3.D"
  public static unsafe Vector<ulong> BitwiseClearXor(Vector<ulong> xor, Vector<ulong> value, Vector<ulong> mask);


    /// BitwiseSelect : Bitwise select

    /// svint8_t svbsl[_s8](svint8_t op1, svint8_t op2, svint8_t op3) : "BSL Ztied1.D, Ztied1.D, Zop2.D, Zop3.D" or "MOVPRFX Zresult, Zop1; BSL Zresult.D, Zresult.D, Zop2.D, Zop3.D"
  public static unsafe Vector<sbyte> BitwiseSelect(Vector<sbyte> select, Vector<sbyte> left, Vector<sbyte> right);

    /// svint16_t svbsl[_s16](svint16_t op1, svint16_t op2, svint16_t op3) : "BSL Ztied1.D, Ztied1.D, Zop2.D, Zop3.D" or "MOVPRFX Zresult, Zop1; BSL Zresult.D, Zresult.D, Zop2.D, Zop3.D"
  public static unsafe Vector<short> BitwiseSelect(Vector<short> select, Vector<short> left, Vector<short> right);

    /// svint32_t svbsl[_s32](svint32_t op1, svint32_t op2, svint32_t op3) : "BSL Ztied1.D, Ztied1.D, Zop2.D, Zop3.D" or "MOVPRFX Zresult, Zop1; BSL Zresult.D, Zresult.D, Zop2.D, Zop3.D"
  public static unsafe Vector<int> BitwiseSelect(Vector<int> select, Vector<int> left, Vector<int> right);

    /// svint64_t svbsl[_s64](svint64_t op1, svint64_t op2, svint64_t op3) : "BSL Ztied1.D, Ztied1.D, Zop2.D, Zop3.D" or "MOVPRFX Zresult, Zop1; BSL Zresult.D, Zresult.D, Zop2.D, Zop3.D"
  public static unsafe Vector<long> BitwiseSelect(Vector<long> select, Vector<long> left, Vector<long> right);

    /// svuint8_t svbsl[_u8](svuint8_t op1, svuint8_t op2, svuint8_t op3) : "BSL Ztied1.D, Ztied1.D, Zop2.D, Zop3.D" or "MOVPRFX Zresult, Zop1; BSL Zresult.D, Zresult.D, Zop2.D, Zop3.D"
  public static unsafe Vector<byte> BitwiseSelect(Vector<byte> select, Vector<byte> left, Vector<byte> right);

    /// svuint16_t svbsl[_u16](svuint16_t op1, svuint16_t op2, svuint16_t op3) : "BSL Ztied1.D, Ztied1.D, Zop2.D, Zop3.D" or "MOVPRFX Zresult, Zop1; BSL Zresult.D, Zresult.D, Zop2.D, Zop3.D"
  public static unsafe Vector<ushort> BitwiseSelect(Vector<ushort> select, Vector<ushort> left, Vector<ushort> right);

    /// svuint32_t svbsl[_u32](svuint32_t op1, svuint32_t op2, svuint32_t op3) : "BSL Ztied1.D, Ztied1.D, Zop2.D, Zop3.D" or "MOVPRFX Zresult, Zop1; BSL Zresult.D, Zresult.D, Zop2.D, Zop3.D"
  public static unsafe Vector<uint> BitwiseSelect(Vector<uint> select, Vector<uint> left, Vector<uint> right);

    /// svuint64_t svbsl[_u64](svuint64_t op1, svuint64_t op2, svuint64_t op3) : "BSL Ztied1.D, Ztied1.D, Zop2.D, Zop3.D" or "MOVPRFX Zresult, Zop1; BSL Zresult.D, Zresult.D, Zop2.D, Zop3.D"
  public static unsafe Vector<ulong> BitwiseSelect(Vector<ulong> select, Vector<ulong> left, Vector<ulong> right);


    /// BitwiseSelectLeftInverted : Bitwise select with first input inverted

    /// svint8_t svbsl1n[_s8](svint8_t op1, svint8_t op2, svint8_t op3) : "BSL1N Ztied1.D, Ztied1.D, Zop2.D, Zop3.D" or "MOVPRFX Zresult, Zop1; BSL1N Zresult.D, Zresult.D, Zop2.D, Zop3.D"
  public static unsafe Vector<sbyte> BitwiseSelectLeftInverted(Vector<sbyte> select, Vector<sbyte> left, Vector<sbyte> right);

    /// svint16_t svbsl1n[_s16](svint16_t op1, svint16_t op2, svint16_t op3) : "BSL1N Ztied1.D, Ztied1.D, Zop2.D, Zop3.D" or "MOVPRFX Zresult, Zop1; BSL1N Zresult.D, Zresult.D, Zop2.D, Zop3.D"
  public static unsafe Vector<short> BitwiseSelectLeftInverted(Vector<short> select, Vector<short> left, Vector<short> right);

    /// svint32_t svbsl1n[_s32](svint32_t op1, svint32_t op2, svint32_t op3) : "BSL1N Ztied1.D, Ztied1.D, Zop2.D, Zop3.D" or "MOVPRFX Zresult, Zop1; BSL1N Zresult.D, Zresult.D, Zop2.D, Zop3.D"
  public static unsafe Vector<int> BitwiseSelectLeftInverted(Vector<int> select, Vector<int> left, Vector<int> right);

    /// svint64_t svbsl1n[_s64](svint64_t op1, svint64_t op2, svint64_t op3) : "BSL1N Ztied1.D, Ztied1.D, Zop2.D, Zop3.D" or "MOVPRFX Zresult, Zop1; BSL1N Zresult.D, Zresult.D, Zop2.D, Zop3.D"
  public static unsafe Vector<long> BitwiseSelectLeftInverted(Vector<long> select, Vector<long> left, Vector<long> right);

    /// svuint8_t svbsl1n[_u8](svuint8_t op1, svuint8_t op2, svuint8_t op3) : "BSL1N Ztied1.D, Ztied1.D, Zop2.D, Zop3.D" or "MOVPRFX Zresult, Zop1; BSL1N Zresult.D, Zresult.D, Zop2.D, Zop3.D"
  public static unsafe Vector<byte> BitwiseSelectLeftInverted(Vector<byte> select, Vector<byte> left, Vector<byte> right);

    /// svuint16_t svbsl1n[_u16](svuint16_t op1, svuint16_t op2, svuint16_t op3) : "BSL1N Ztied1.D, Ztied1.D, Zop2.D, Zop3.D" or "MOVPRFX Zresult, Zop1; BSL1N Zresult.D, Zresult.D, Zop2.D, Zop3.D"
  public static unsafe Vector<ushort> BitwiseSelectLeftInverted(Vector<ushort> select, Vector<ushort> left, Vector<ushort> right);

    /// svuint32_t svbsl1n[_u32](svuint32_t op1, svuint32_t op2, svuint32_t op3) : "BSL1N Ztied1.D, Ztied1.D, Zop2.D, Zop3.D" or "MOVPRFX Zresult, Zop1; BSL1N Zresult.D, Zresult.D, Zop2.D, Zop3.D"
  public static unsafe Vector<uint> BitwiseSelectLeftInverted(Vector<uint> select, Vector<uint> left, Vector<uint> right);

    /// svuint64_t svbsl1n[_u64](svuint64_t op1, svuint64_t op2, svuint64_t op3) : "BSL1N Ztied1.D, Ztied1.D, Zop2.D, Zop3.D" or "MOVPRFX Zresult, Zop1; BSL1N Zresult.D, Zresult.D, Zop2.D, Zop3.D"
  public static unsafe Vector<ulong> BitwiseSelectLeftInverted(Vector<ulong> select, Vector<ulong> left, Vector<ulong> right);


    /// BitwiseSelectRightInverted : Bitwise select with second input inverted

    /// svint8_t svbsl2n[_s8](svint8_t op1, svint8_t op2, svint8_t op3) : "BSL2N Ztied1.D, Ztied1.D, Zop2.D, Zop3.D" or "MOVPRFX Zresult, Zop1; BSL2N Zresult.D, Zresult.D, Zop2.D, Zop3.D"
  public static unsafe Vector<sbyte> BitwiseSelectRightInverted(Vector<sbyte> select, Vector<sbyte> left, Vector<sbyte> right);

    /// svint16_t svbsl2n[_s16](svint16_t op1, svint16_t op2, svint16_t op3) : "BSL2N Ztied1.D, Ztied1.D, Zop2.D, Zop3.D" or "MOVPRFX Zresult, Zop1; BSL2N Zresult.D, Zresult.D, Zop2.D, Zop3.D"
  public static unsafe Vector<short> BitwiseSelectRightInverted(Vector<short> select, Vector<short> left, Vector<short> right);

    /// svint32_t svbsl2n[_s32](svint32_t op1, svint32_t op2, svint32_t op3) : "BSL2N Ztied1.D, Ztied1.D, Zop2.D, Zop3.D" or "MOVPRFX Zresult, Zop1; BSL2N Zresult.D, Zresult.D, Zop2.D, Zop3.D"
  public static unsafe Vector<int> BitwiseSelectRightInverted(Vector<int> select, Vector<int> left, Vector<int> right);

    /// svint64_t svbsl2n[_s64](svint64_t op1, svint64_t op2, svint64_t op3) : "BSL2N Ztied1.D, Ztied1.D, Zop2.D, Zop3.D" or "MOVPRFX Zresult, Zop1; BSL2N Zresult.D, Zresult.D, Zop2.D, Zop3.D"
  public static unsafe Vector<long> BitwiseSelectRightInverted(Vector<long> select, Vector<long> left, Vector<long> right);

    /// svuint8_t svbsl2n[_u8](svuint8_t op1, svuint8_t op2, svuint8_t op3) : "BSL2N Ztied1.D, Ztied1.D, Zop2.D, Zop3.D" or "MOVPRFX Zresult, Zop1; BSL2N Zresult.D, Zresult.D, Zop2.D, Zop3.D"
  public static unsafe Vector<byte> BitwiseSelectRightInverted(Vector<byte> select, Vector<byte> left, Vector<byte> right);

    /// svuint16_t svbsl2n[_u16](svuint16_t op1, svuint16_t op2, svuint16_t op3) : "BSL2N Ztied1.D, Ztied1.D, Zop2.D, Zop3.D" or "MOVPRFX Zresult, Zop1; BSL2N Zresult.D, Zresult.D, Zop2.D, Zop3.D"
  public static unsafe Vector<ushort> BitwiseSelectRightInverted(Vector<ushort> select, Vector<ushort> left, Vector<ushort> right);

    /// svuint32_t svbsl2n[_u32](svuint32_t op1, svuint32_t op2, svuint32_t op3) : "BSL2N Ztied1.D, Ztied1.D, Zop2.D, Zop3.D" or "MOVPRFX Zresult, Zop1; BSL2N Zresult.D, Zresult.D, Zop2.D, Zop3.D"
  public static unsafe Vector<uint> BitwiseSelectRightInverted(Vector<uint> select, Vector<uint> left, Vector<uint> right);

    /// svuint64_t svbsl2n[_u64](svuint64_t op1, svuint64_t op2, svuint64_t op3) : "BSL2N Ztied1.D, Ztied1.D, Zop2.D, Zop3.D" or "MOVPRFX Zresult, Zop1; BSL2N Zresult.D, Zresult.D, Zop2.D, Zop3.D"
  public static unsafe Vector<ulong> BitwiseSelectRightInverted(Vector<ulong> select, Vector<ulong> left, Vector<ulong> right);


    /// ShiftArithmeticRounded : Rounding shift left

    /// svint8_t svrshl[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2) : "SRSHL Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "MOVPRFX Zresult, Zop1; SRSHL Zresult.B, Pg/M, Zresult.B, Zop2.B"
    /// svint8_t svrshl[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2) : "SRSHL Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "SRSHLR Ztied2.B, Pg/M, Ztied2.B, Zop1.B" or "MOVPRFX Zresult, Zop1; SRSHL Zresult.B, Pg/M, Zresult.B, Zop2.B"
    /// svint8_t svrshl[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2) : "MOVPRFX Zresult.B, Pg/Z, Zop1.B; SRSHL Zresult.B, Pg/M, Zresult.B, Zop2.B" or "MOVPRFX Zresult.B, Pg/Z, Zop2.B; SRSHLR Zresult.B, Pg/M, Zresult.B, Zop1.B"
  public static unsafe Vector<sbyte> ShiftArithmeticRounded(Vector<sbyte> value, Vector<sbyte> count);

    /// svint16_t svrshl[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2) : "SRSHL Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "MOVPRFX Zresult, Zop1; SRSHL Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svint16_t svrshl[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2) : "SRSHL Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "SRSHLR Ztied2.H, Pg/M, Ztied2.H, Zop1.H" or "MOVPRFX Zresult, Zop1; SRSHL Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svint16_t svrshl[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2) : "MOVPRFX Zresult.H, Pg/Z, Zop1.H; SRSHL Zresult.H, Pg/M, Zresult.H, Zop2.H" or "MOVPRFX Zresult.H, Pg/Z, Zop2.H; SRSHLR Zresult.H, Pg/M, Zresult.H, Zop1.H"
  public static unsafe Vector<short> ShiftArithmeticRounded(Vector<short> value, Vector<short> count);

    /// svint32_t svrshl[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2) : "SRSHL Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "MOVPRFX Zresult, Zop1; SRSHL Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svint32_t svrshl[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2) : "SRSHL Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "SRSHLR Ztied2.S, Pg/M, Ztied2.S, Zop1.S" or "MOVPRFX Zresult, Zop1; SRSHL Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svint32_t svrshl[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2) : "MOVPRFX Zresult.S, Pg/Z, Zop1.S; SRSHL Zresult.S, Pg/M, Zresult.S, Zop2.S" or "MOVPRFX Zresult.S, Pg/Z, Zop2.S; SRSHLR Zresult.S, Pg/M, Zresult.S, Zop1.S"
  public static unsafe Vector<int> ShiftArithmeticRounded(Vector<int> value, Vector<int> count);

    /// svint64_t svrshl[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2) : "SRSHL Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; SRSHL Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svint64_t svrshl[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2) : "SRSHL Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "SRSHLR Ztied2.D, Pg/M, Ztied2.D, Zop1.D" or "MOVPRFX Zresult, Zop1; SRSHL Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svint64_t svrshl[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2) : "MOVPRFX Zresult.D, Pg/Z, Zop1.D; SRSHL Zresult.D, Pg/M, Zresult.D, Zop2.D" or "MOVPRFX Zresult.D, Pg/Z, Zop2.D; SRSHLR Zresult.D, Pg/M, Zresult.D, Zop1.D"
  public static unsafe Vector<long> ShiftArithmeticRounded(Vector<long> value, Vector<long> count);


    /// ShiftArithmeticRoundedSaturate : Saturating rounding shift left

    /// svint8_t svqrshl[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2) : "SQRSHL Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "MOVPRFX Zresult, Zop1; SQRSHL Zresult.B, Pg/M, Zresult.B, Zop2.B"
    /// svint8_t svqrshl[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2) : "SQRSHL Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "SQRSHLR Ztied2.B, Pg/M, Ztied2.B, Zop1.B" or "MOVPRFX Zresult, Zop1; SQRSHL Zresult.B, Pg/M, Zresult.B, Zop2.B"
    /// svint8_t svqrshl[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2) : "MOVPRFX Zresult.B, Pg/Z, Zop1.B; SQRSHL Zresult.B, Pg/M, Zresult.B, Zop2.B" or "MOVPRFX Zresult.B, Pg/Z, Zop2.B; SQRSHLR Zresult.B, Pg/M, Zresult.B, Zop1.B"
  public static unsafe Vector<sbyte> ShiftArithmeticRoundedSaturate(Vector<sbyte> value, Vector<sbyte> count);

    /// svint16_t svqrshl[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2) : "SQRSHL Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "MOVPRFX Zresult, Zop1; SQRSHL Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svint16_t svqrshl[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2) : "SQRSHL Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "SQRSHLR Ztied2.H, Pg/M, Ztied2.H, Zop1.H" or "MOVPRFX Zresult, Zop1; SQRSHL Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svint16_t svqrshl[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2) : "MOVPRFX Zresult.H, Pg/Z, Zop1.H; SQRSHL Zresult.H, Pg/M, Zresult.H, Zop2.H" or "MOVPRFX Zresult.H, Pg/Z, Zop2.H; SQRSHLR Zresult.H, Pg/M, Zresult.H, Zop1.H"
  public static unsafe Vector<short> ShiftArithmeticRoundedSaturate(Vector<short> value, Vector<short> count);

    /// svint32_t svqrshl[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2) : "SQRSHL Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "MOVPRFX Zresult, Zop1; SQRSHL Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svint32_t svqrshl[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2) : "SQRSHL Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "SQRSHLR Ztied2.S, Pg/M, Ztied2.S, Zop1.S" or "MOVPRFX Zresult, Zop1; SQRSHL Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svint32_t svqrshl[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2) : "MOVPRFX Zresult.S, Pg/Z, Zop1.S; SQRSHL Zresult.S, Pg/M, Zresult.S, Zop2.S" or "MOVPRFX Zresult.S, Pg/Z, Zop2.S; SQRSHLR Zresult.S, Pg/M, Zresult.S, Zop1.S"
  public static unsafe Vector<int> ShiftArithmeticRoundedSaturate(Vector<int> value, Vector<int> count);

    /// svint64_t svqrshl[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2) : "SQRSHL Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; SQRSHL Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svint64_t svqrshl[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2) : "SQRSHL Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "SQRSHLR Ztied2.D, Pg/M, Ztied2.D, Zop1.D" or "MOVPRFX Zresult, Zop1; SQRSHL Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svint64_t svqrshl[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2) : "MOVPRFX Zresult.D, Pg/Z, Zop1.D; SQRSHL Zresult.D, Pg/M, Zresult.D, Zop2.D" or "MOVPRFX Zresult.D, Pg/Z, Zop2.D; SQRSHLR Zresult.D, Pg/M, Zresult.D, Zop1.D"
  public static unsafe Vector<long> ShiftArithmeticRoundedSaturate(Vector<long> value, Vector<long> count);


    /// ShiftArithmeticSaturate : Saturating shift left

    /// svint8_t svqshl[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2) : "SQSHL Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "MOVPRFX Zresult, Zop1; SQSHL Zresult.B, Pg/M, Zresult.B, Zop2.B"
    /// svint8_t svqshl[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2) : "SQSHL Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "SQSHLR Ztied2.B, Pg/M, Ztied2.B, Zop1.B" or "MOVPRFX Zresult, Zop1; SQSHL Zresult.B, Pg/M, Zresult.B, Zop2.B"
    /// svint8_t svqshl[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2) : "MOVPRFX Zresult.B, Pg/Z, Zop1.B; SQSHL Zresult.B, Pg/M, Zresult.B, Zop2.B" or "MOVPRFX Zresult.B, Pg/Z, Zop2.B; SQSHLR Zresult.B, Pg/M, Zresult.B, Zop1.B"
  public static unsafe Vector<sbyte> ShiftArithmeticSaturate(Vector<sbyte> value, Vector<sbyte> count);

    /// svint16_t svqshl[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2) : "SQSHL Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "MOVPRFX Zresult, Zop1; SQSHL Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svint16_t svqshl[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2) : "SQSHL Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "SQSHLR Ztied2.H, Pg/M, Ztied2.H, Zop1.H" or "MOVPRFX Zresult, Zop1; SQSHL Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svint16_t svqshl[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2) : "MOVPRFX Zresult.H, Pg/Z, Zop1.H; SQSHL Zresult.H, Pg/M, Zresult.H, Zop2.H" or "MOVPRFX Zresult.H, Pg/Z, Zop2.H; SQSHLR Zresult.H, Pg/M, Zresult.H, Zop1.H"
  public static unsafe Vector<short> ShiftArithmeticSaturate(Vector<short> value, Vector<short> count);

    /// svint32_t svqshl[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2) : "SQSHL Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "MOVPRFX Zresult, Zop1; SQSHL Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svint32_t svqshl[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2) : "SQSHL Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "SQSHLR Ztied2.S, Pg/M, Ztied2.S, Zop1.S" or "MOVPRFX Zresult, Zop1; SQSHL Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svint32_t svqshl[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2) : "MOVPRFX Zresult.S, Pg/Z, Zop1.S; SQSHL Zresult.S, Pg/M, Zresult.S, Zop2.S" or "MOVPRFX Zresult.S, Pg/Z, Zop2.S; SQSHLR Zresult.S, Pg/M, Zresult.S, Zop1.S"
  public static unsafe Vector<int> ShiftArithmeticSaturate(Vector<int> value, Vector<int> count);

    /// svint64_t svqshl[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2) : "SQSHL Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; SQSHL Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svint64_t svqshl[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2) : "SQSHL Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "SQSHLR Ztied2.D, Pg/M, Ztied2.D, Zop1.D" or "MOVPRFX Zresult, Zop1; SQSHL Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svint64_t svqshl[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2) : "MOVPRFX Zresult.D, Pg/Z, Zop1.D; SQSHL Zresult.D, Pg/M, Zresult.D, Zop2.D" or "MOVPRFX Zresult.D, Pg/Z, Zop2.D; SQSHLR Zresult.D, Pg/M, Zresult.D, Zop1.D"
  public static unsafe Vector<long> ShiftArithmeticSaturate(Vector<long> value, Vector<long> count);


    /// ShiftLeftAndInsert : Shift left and insert

    /// svint8_t svsli[_n_s8](svint8_t op1, svint8_t op2, uint64_t imm3) : "SLI Ztied1.B, Zop2.B, #imm3"
  public static unsafe Vector<sbyte> ShiftLeftAndInsert(Vector<sbyte> left, Vector<sbyte> right, [ConstantExpected] byte shift);

    /// svint16_t svsli[_n_s16](svint16_t op1, svint16_t op2, uint64_t imm3) : "SLI Ztied1.H, Zop2.H, #imm3"
  public static unsafe Vector<short> ShiftLeftAndInsert(Vector<short> left, Vector<short> right, [ConstantExpected] byte shift);

    /// svint32_t svsli[_n_s32](svint32_t op1, svint32_t op2, uint64_t imm3) : "SLI Ztied1.S, Zop2.S, #imm3"
  public static unsafe Vector<int> ShiftLeftAndInsert(Vector<int> left, Vector<int> right, [ConstantExpected] byte shift);

    /// svint64_t svsli[_n_s64](svint64_t op1, svint64_t op2, uint64_t imm3) : "SLI Ztied1.D, Zop2.D, #imm3"
  public static unsafe Vector<long> ShiftLeftAndInsert(Vector<long> left, Vector<long> right, [ConstantExpected] byte shift);

    /// svuint8_t svsli[_n_u8](svuint8_t op1, svuint8_t op2, uint64_t imm3) : "SLI Ztied1.B, Zop2.B, #imm3"
  public static unsafe Vector<byte> ShiftLeftAndInsert(Vector<byte> left, Vector<byte> right, [ConstantExpected] byte shift);

    /// svuint16_t svsli[_n_u16](svuint16_t op1, svuint16_t op2, uint64_t imm3) : "SLI Ztied1.H, Zop2.H, #imm3"
  public static unsafe Vector<ushort> ShiftLeftAndInsert(Vector<ushort> left, Vector<ushort> right, [ConstantExpected] byte shift);

    /// svuint32_t svsli[_n_u32](svuint32_t op1, svuint32_t op2, uint64_t imm3) : "SLI Ztied1.S, Zop2.S, #imm3"
  public static unsafe Vector<uint> ShiftLeftAndInsert(Vector<uint> left, Vector<uint> right, [ConstantExpected] byte shift);

    /// svuint64_t svsli[_n_u64](svuint64_t op1, svuint64_t op2, uint64_t imm3) : "SLI Ztied1.D, Zop2.D, #imm3"
  public static unsafe Vector<ulong> ShiftLeftAndInsert(Vector<ulong> left, Vector<ulong> right, [ConstantExpected] byte shift);


    /// ShiftLeftLogicalSaturate : Saturating shift left

    /// svuint8_t svqshl[_u8]_m(svbool_t pg, svuint8_t op1, svint8_t op2) : "UQSHL Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "MOVPRFX Zresult, Zop1; UQSHL Zresult.B, Pg/M, Zresult.B, Zop2.B"
    /// svuint8_t svqshl[_u8]_x(svbool_t pg, svuint8_t op1, svint8_t op2) : "UQSHL Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "UQSHLR Ztied2.B, Pg/M, Ztied2.B, Zop1.B" or "MOVPRFX Zresult, Zop1; UQSHL Zresult.B, Pg/M, Zresult.B, Zop2.B"
    /// svuint8_t svqshl[_u8]_z(svbool_t pg, svuint8_t op1, svint8_t op2) : "MOVPRFX Zresult.B, Pg/Z, Zop1.B; UQSHL Zresult.B, Pg/M, Zresult.B, Zop2.B" or "MOVPRFX Zresult.B, Pg/Z, Zop2.B; UQSHLR Zresult.B, Pg/M, Zresult.B, Zop1.B"
  public static unsafe Vector<byte> ShiftLeftLogicalSaturate(Vector<byte> value, Vector<sbyte> count);

    /// svuint16_t svqshl[_u16]_m(svbool_t pg, svuint16_t op1, svint16_t op2) : "UQSHL Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "MOVPRFX Zresult, Zop1; UQSHL Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svuint16_t svqshl[_u16]_x(svbool_t pg, svuint16_t op1, svint16_t op2) : "UQSHL Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "UQSHLR Ztied2.H, Pg/M, Ztied2.H, Zop1.H" or "MOVPRFX Zresult, Zop1; UQSHL Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svuint16_t svqshl[_u16]_z(svbool_t pg, svuint16_t op1, svint16_t op2) : "MOVPRFX Zresult.H, Pg/Z, Zop1.H; UQSHL Zresult.H, Pg/M, Zresult.H, Zop2.H" or "MOVPRFX Zresult.H, Pg/Z, Zop2.H; UQSHLR Zresult.H, Pg/M, Zresult.H, Zop1.H"
  public static unsafe Vector<ushort> ShiftLeftLogicalSaturate(Vector<ushort> value, Vector<short> count);

    /// svuint32_t svqshl[_u32]_m(svbool_t pg, svuint32_t op1, svint32_t op2) : "UQSHL Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "MOVPRFX Zresult, Zop1; UQSHL Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svuint32_t svqshl[_u32]_x(svbool_t pg, svuint32_t op1, svint32_t op2) : "UQSHL Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "UQSHLR Ztied2.S, Pg/M, Ztied2.S, Zop1.S" or "MOVPRFX Zresult, Zop1; UQSHL Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svuint32_t svqshl[_u32]_z(svbool_t pg, svuint32_t op1, svint32_t op2) : "MOVPRFX Zresult.S, Pg/Z, Zop1.S; UQSHL Zresult.S, Pg/M, Zresult.S, Zop2.S" or "MOVPRFX Zresult.S, Pg/Z, Zop2.S; UQSHLR Zresult.S, Pg/M, Zresult.S, Zop1.S"
  public static unsafe Vector<uint> ShiftLeftLogicalSaturate(Vector<uint> value, Vector<int> count);

    /// svuint64_t svqshl[_u64]_m(svbool_t pg, svuint64_t op1, svint64_t op2) : "UQSHL Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; UQSHL Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svuint64_t svqshl[_u64]_x(svbool_t pg, svuint64_t op1, svint64_t op2) : "UQSHL Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "UQSHLR Ztied2.D, Pg/M, Ztied2.D, Zop1.D" or "MOVPRFX Zresult, Zop1; UQSHL Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svuint64_t svqshl[_u64]_z(svbool_t pg, svuint64_t op1, svint64_t op2) : "MOVPRFX Zresult.D, Pg/Z, Zop1.D; UQSHL Zresult.D, Pg/M, Zresult.D, Zop2.D" or "MOVPRFX Zresult.D, Pg/Z, Zop2.D; UQSHLR Zresult.D, Pg/M, Zresult.D, Zop1.D"
  public static unsafe Vector<ulong> ShiftLeftLogicalSaturate(Vector<ulong> value, Vector<long> count);


    /// ShiftLeftLogicalSaturateUnsigned : Saturating shift left unsigned

    /// svuint8_t svqshlu[_n_s8]_m(svbool_t pg, svint8_t op1, uint64_t imm2) : "SQSHLU Ztied1.B, Pg/M, Ztied1.B, #imm2" or "MOVPRFX Zresult, Zop1; SQSHLU Zresult.B, Pg/M, Zresult.B, #imm2"
    /// svuint8_t svqshlu[_n_s8]_x(svbool_t pg, svint8_t op1, uint64_t imm2) : "SQSHLU Ztied1.B, Pg/M, Ztied1.B, #imm2" or "MOVPRFX Zresult, Zop1; SQSHLU Zresult.B, Pg/M, Zresult.B, #imm2"
    /// svuint8_t svqshlu[_n_s8]_z(svbool_t pg, svint8_t op1, uint64_t imm2) : "MOVPRFX Zresult.B, Pg/Z, Zop1.B; SQSHLU Zresult.B, Pg/M, Zresult.B, #imm2"
  public static unsafe Vector<byte> ShiftLeftLogicalSaturateUnsigned(Vector<sbyte> value, [ConstantExpected] byte count);

    /// svuint16_t svqshlu[_n_s16]_m(svbool_t pg, svint16_t op1, uint64_t imm2) : "SQSHLU Ztied1.H, Pg/M, Ztied1.H, #imm2" or "MOVPRFX Zresult, Zop1; SQSHLU Zresult.H, Pg/M, Zresult.H, #imm2"
    /// svuint16_t svqshlu[_n_s16]_x(svbool_t pg, svint16_t op1, uint64_t imm2) : "SQSHLU Ztied1.H, Pg/M, Ztied1.H, #imm2" or "MOVPRFX Zresult, Zop1; SQSHLU Zresult.H, Pg/M, Zresult.H, #imm2"
    /// svuint16_t svqshlu[_n_s16]_z(svbool_t pg, svint16_t op1, uint64_t imm2) : "MOVPRFX Zresult.H, Pg/Z, Zop1.H; SQSHLU Zresult.H, Pg/M, Zresult.H, #imm2"
  public static unsafe Vector<ushort> ShiftLeftLogicalSaturateUnsigned(Vector<short> value, [ConstantExpected] byte count);

    /// svuint32_t svqshlu[_n_s32]_m(svbool_t pg, svint32_t op1, uint64_t imm2) : "SQSHLU Ztied1.S, Pg/M, Ztied1.S, #imm2" or "MOVPRFX Zresult, Zop1; SQSHLU Zresult.S, Pg/M, Zresult.S, #imm2"
    /// svuint32_t svqshlu[_n_s32]_x(svbool_t pg, svint32_t op1, uint64_t imm2) : "SQSHLU Ztied1.S, Pg/M, Ztied1.S, #imm2" or "MOVPRFX Zresult, Zop1; SQSHLU Zresult.S, Pg/M, Zresult.S, #imm2"
    /// svuint32_t svqshlu[_n_s32]_z(svbool_t pg, svint32_t op1, uint64_t imm2) : "MOVPRFX Zresult.S, Pg/Z, Zop1.S; SQSHLU Zresult.S, Pg/M, Zresult.S, #imm2"
  public static unsafe Vector<uint> ShiftLeftLogicalSaturateUnsigned(Vector<int> value, [ConstantExpected] byte count);

    /// svuint64_t svqshlu[_n_s64]_m(svbool_t pg, svint64_t op1, uint64_t imm2) : "SQSHLU Ztied1.D, Pg/M, Ztied1.D, #imm2" or "MOVPRFX Zresult, Zop1; SQSHLU Zresult.D, Pg/M, Zresult.D, #imm2"
    /// svuint64_t svqshlu[_n_s64]_x(svbool_t pg, svint64_t op1, uint64_t imm2) : "SQSHLU Ztied1.D, Pg/M, Ztied1.D, #imm2" or "MOVPRFX Zresult, Zop1; SQSHLU Zresult.D, Pg/M, Zresult.D, #imm2"
    /// svuint64_t svqshlu[_n_s64]_z(svbool_t pg, svint64_t op1, uint64_t imm2) : "MOVPRFX Zresult.D, Pg/Z, Zop1.D; SQSHLU Zresult.D, Pg/M, Zresult.D, #imm2"
  public static unsafe Vector<ulong> ShiftLeftLogicalSaturateUnsigned(Vector<long> value, [ConstantExpected] byte count);


    /// ShiftLeftLogicalWideningEven : Shift left long (bottom)

    /// svint16_t svshllb[_n_s16](svint8_t op1, uint64_t imm2) : "SSHLLB Zresult.H, Zop1.B, #imm2"
  public static unsafe Vector<short> ShiftLeftLogicalWideningEven(Vector<sbyte> value, [ConstantExpected] byte count);

    /// svint32_t svshllb[_n_s32](svint16_t op1, uint64_t imm2) : "SSHLLB Zresult.S, Zop1.H, #imm2"
  public static unsafe Vector<int> ShiftLeftLogicalWideningEven(Vector<short> value, [ConstantExpected] byte count);

    /// svint64_t svshllb[_n_s64](svint32_t op1, uint64_t imm2) : "SSHLLB Zresult.D, Zop1.S, #imm2"
  public static unsafe Vector<long> ShiftLeftLogicalWideningEven(Vector<int> value, [ConstantExpected] byte count);

    /// svuint16_t svshllb[_n_u16](svuint8_t op1, uint64_t imm2) : "USHLLB Zresult.H, Zop1.B, #imm2"
  public static unsafe Vector<ushort> ShiftLeftLogicalWideningEven(Vector<byte> value, [ConstantExpected] byte count);

    /// svuint32_t svshllb[_n_u32](svuint16_t op1, uint64_t imm2) : "USHLLB Zresult.S, Zop1.H, #imm2"
  public static unsafe Vector<uint> ShiftLeftLogicalWideningEven(Vector<ushort> value, [ConstantExpected] byte count);

    /// svuint64_t svshllb[_n_u64](svuint32_t op1, uint64_t imm2) : "USHLLB Zresult.D, Zop1.S, #imm2"
  public static unsafe Vector<ulong> ShiftLeftLogicalWideningEven(Vector<uint> value, [ConstantExpected] byte count);


    /// ShiftLeftLogicalWideningOdd : Shift left long (top)

    /// svint16_t svshllt[_n_s16](svint8_t op1, uint64_t imm2) : "SSHLLT Zresult.H, Zop1.B, #imm2"
  public static unsafe Vector<short> ShiftLeftLogicalWideningOdd(Vector<sbyte> value, [ConstantExpected] byte count);

    /// svint32_t svshllt[_n_s32](svint16_t op1, uint64_t imm2) : "SSHLLT Zresult.S, Zop1.H, #imm2"
  public static unsafe Vector<int> ShiftLeftLogicalWideningOdd(Vector<short> value, [ConstantExpected] byte count);

    /// svint64_t svshllt[_n_s64](svint32_t op1, uint64_t imm2) : "SSHLLT Zresult.D, Zop1.S, #imm2"
  public static unsafe Vector<long> ShiftLeftLogicalWideningOdd(Vector<int> value, [ConstantExpected] byte count);

    /// svuint16_t svshllt[_n_u16](svuint8_t op1, uint64_t imm2) : "USHLLT Zresult.H, Zop1.B, #imm2"
  public static unsafe Vector<ushort> ShiftLeftLogicalWideningOdd(Vector<byte> value, [ConstantExpected] byte count);

    /// svuint32_t svshllt[_n_u32](svuint16_t op1, uint64_t imm2) : "USHLLT Zresult.S, Zop1.H, #imm2"
  public static unsafe Vector<uint> ShiftLeftLogicalWideningOdd(Vector<ushort> value, [ConstantExpected] byte count);

    /// svuint64_t svshllt[_n_u64](svuint32_t op1, uint64_t imm2) : "USHLLT Zresult.D, Zop1.S, #imm2"
  public static unsafe Vector<ulong> ShiftLeftLogicalWideningOdd(Vector<uint> value, [ConstantExpected] byte count);


    /// ShiftLogicalRounded : Rounding shift left

    /// svuint8_t svrshl[_u8]_m(svbool_t pg, svuint8_t op1, svint8_t op2) : "URSHL Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "MOVPRFX Zresult, Zop1; URSHL Zresult.B, Pg/M, Zresult.B, Zop2.B"
    /// svuint8_t svrshl[_u8]_x(svbool_t pg, svuint8_t op1, svint8_t op2) : "URSHL Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "URSHLR Ztied2.B, Pg/M, Ztied2.B, Zop1.B" or "MOVPRFX Zresult, Zop1; URSHL Zresult.B, Pg/M, Zresult.B, Zop2.B"
    /// svuint8_t svrshl[_u8]_z(svbool_t pg, svuint8_t op1, svint8_t op2) : "MOVPRFX Zresult.B, Pg/Z, Zop1.B; URSHL Zresult.B, Pg/M, Zresult.B, Zop2.B" or "MOVPRFX Zresult.B, Pg/Z, Zop2.B; URSHLR Zresult.B, Pg/M, Zresult.B, Zop1.B"
  public static unsafe Vector<byte> ShiftLogicalRounded(Vector<byte> value, Vector<sbyte> count);

    /// svuint16_t svrshl[_u16]_m(svbool_t pg, svuint16_t op1, svint16_t op2) : "URSHL Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "MOVPRFX Zresult, Zop1; URSHL Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svuint16_t svrshl[_u16]_x(svbool_t pg, svuint16_t op1, svint16_t op2) : "URSHL Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "URSHLR Ztied2.H, Pg/M, Ztied2.H, Zop1.H" or "MOVPRFX Zresult, Zop1; URSHL Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svuint16_t svrshl[_u16]_z(svbool_t pg, svuint16_t op1, svint16_t op2) : "MOVPRFX Zresult.H, Pg/Z, Zop1.H; URSHL Zresult.H, Pg/M, Zresult.H, Zop2.H" or "MOVPRFX Zresult.H, Pg/Z, Zop2.H; URSHLR Zresult.H, Pg/M, Zresult.H, Zop1.H"
  public static unsafe Vector<ushort> ShiftLogicalRounded(Vector<ushort> value, Vector<short> count);

    /// svuint32_t svrshl[_u32]_m(svbool_t pg, svuint32_t op1, svint32_t op2) : "URSHL Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "MOVPRFX Zresult, Zop1; URSHL Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svuint32_t svrshl[_u32]_x(svbool_t pg, svuint32_t op1, svint32_t op2) : "URSHL Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "URSHLR Ztied2.S, Pg/M, Ztied2.S, Zop1.S" or "MOVPRFX Zresult, Zop1; URSHL Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svuint32_t svrshl[_u32]_z(svbool_t pg, svuint32_t op1, svint32_t op2) : "MOVPRFX Zresult.S, Pg/Z, Zop1.S; URSHL Zresult.S, Pg/M, Zresult.S, Zop2.S" or "MOVPRFX Zresult.S, Pg/Z, Zop2.S; URSHLR Zresult.S, Pg/M, Zresult.S, Zop1.S"
  public static unsafe Vector<uint> ShiftLogicalRounded(Vector<uint> value, Vector<int> count);

    /// svuint64_t svrshl[_u64]_m(svbool_t pg, svuint64_t op1, svint64_t op2) : "URSHL Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; URSHL Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svuint64_t svrshl[_u64]_x(svbool_t pg, svuint64_t op1, svint64_t op2) : "URSHL Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "URSHLR Ztied2.D, Pg/M, Ztied2.D, Zop1.D" or "MOVPRFX Zresult, Zop1; URSHL Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svuint64_t svrshl[_u64]_z(svbool_t pg, svuint64_t op1, svint64_t op2) : "MOVPRFX Zresult.D, Pg/Z, Zop1.D; URSHL Zresult.D, Pg/M, Zresult.D, Zop2.D" or "MOVPRFX Zresult.D, Pg/Z, Zop2.D; URSHLR Zresult.D, Pg/M, Zresult.D, Zop1.D"
  public static unsafe Vector<ulong> ShiftLogicalRounded(Vector<ulong> value, Vector<long> count);


    /// ShiftLogicalRoundedSaturate : Saturating rounding shift left

    /// svuint8_t svqrshl[_u8]_m(svbool_t pg, svuint8_t op1, svint8_t op2) : "UQRSHL Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "MOVPRFX Zresult, Zop1; UQRSHL Zresult.B, Pg/M, Zresult.B, Zop2.B"
    /// svuint8_t svqrshl[_u8]_x(svbool_t pg, svuint8_t op1, svint8_t op2) : "UQRSHL Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "UQRSHLR Ztied2.B, Pg/M, Ztied2.B, Zop1.B" or "MOVPRFX Zresult, Zop1; UQRSHL Zresult.B, Pg/M, Zresult.B, Zop2.B"
    /// svuint8_t svqrshl[_u8]_z(svbool_t pg, svuint8_t op1, svint8_t op2) : "MOVPRFX Zresult.B, Pg/Z, Zop1.B; UQRSHL Zresult.B, Pg/M, Zresult.B, Zop2.B" or "MOVPRFX Zresult.B, Pg/Z, Zop2.B; UQRSHLR Zresult.B, Pg/M, Zresult.B, Zop1.B"
  public static unsafe Vector<byte> ShiftLogicalRoundedSaturate(Vector<byte> value, Vector<sbyte> count);

    /// svuint16_t svqrshl[_u16]_m(svbool_t pg, svuint16_t op1, svint16_t op2) : "UQRSHL Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "MOVPRFX Zresult, Zop1; UQRSHL Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svuint16_t svqrshl[_u16]_x(svbool_t pg, svuint16_t op1, svint16_t op2) : "UQRSHL Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "UQRSHLR Ztied2.H, Pg/M, Ztied2.H, Zop1.H" or "MOVPRFX Zresult, Zop1; UQRSHL Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svuint16_t svqrshl[_u16]_z(svbool_t pg, svuint16_t op1, svint16_t op2) : "MOVPRFX Zresult.H, Pg/Z, Zop1.H; UQRSHL Zresult.H, Pg/M, Zresult.H, Zop2.H" or "MOVPRFX Zresult.H, Pg/Z, Zop2.H; UQRSHLR Zresult.H, Pg/M, Zresult.H, Zop1.H"
  public static unsafe Vector<ushort> ShiftLogicalRoundedSaturate(Vector<ushort> value, Vector<short> count);

    /// svuint32_t svqrshl[_u32]_m(svbool_t pg, svuint32_t op1, svint32_t op2) : "UQRSHL Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "MOVPRFX Zresult, Zop1; UQRSHL Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svuint32_t svqrshl[_u32]_x(svbool_t pg, svuint32_t op1, svint32_t op2) : "UQRSHL Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "UQRSHLR Ztied2.S, Pg/M, Ztied2.S, Zop1.S" or "MOVPRFX Zresult, Zop1; UQRSHL Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svuint32_t svqrshl[_u32]_z(svbool_t pg, svuint32_t op1, svint32_t op2) : "MOVPRFX Zresult.S, Pg/Z, Zop1.S; UQRSHL Zresult.S, Pg/M, Zresult.S, Zop2.S" or "MOVPRFX Zresult.S, Pg/Z, Zop2.S; UQRSHLR Zresult.S, Pg/M, Zresult.S, Zop1.S"
  public static unsafe Vector<uint> ShiftLogicalRoundedSaturate(Vector<uint> value, Vector<int> count);

    /// svuint64_t svqrshl[_u64]_m(svbool_t pg, svuint64_t op1, svint64_t op2) : "UQRSHL Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; UQRSHL Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svuint64_t svqrshl[_u64]_x(svbool_t pg, svuint64_t op1, svint64_t op2) : "UQRSHL Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "UQRSHLR Ztied2.D, Pg/M, Ztied2.D, Zop1.D" or "MOVPRFX Zresult, Zop1; UQRSHL Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svuint64_t svqrshl[_u64]_z(svbool_t pg, svuint64_t op1, svint64_t op2) : "MOVPRFX Zresult.D, Pg/Z, Zop1.D; UQRSHL Zresult.D, Pg/M, Zresult.D, Zop2.D" or "MOVPRFX Zresult.D, Pg/Z, Zop2.D; UQRSHLR Zresult.D, Pg/M, Zresult.D, Zop1.D"
  public static unsafe Vector<ulong> ShiftLogicalRoundedSaturate(Vector<ulong> value, Vector<long> count);


    /// ShiftRightAndInsert : Shift right and insert

    /// svint8_t svsri[_n_s8](svint8_t op1, svint8_t op2, uint64_t imm3) : "SRI Ztied1.B, Zop2.B, #imm3"
  public static unsafe Vector<sbyte> ShiftRightAndInsert(Vector<sbyte> left, Vector<sbyte> right, [ConstantExpected] byte shift);

    /// svint16_t svsri[_n_s16](svint16_t op1, svint16_t op2, uint64_t imm3) : "SRI Ztied1.H, Zop2.H, #imm3"
  public static unsafe Vector<short> ShiftRightAndInsert(Vector<short> left, Vector<short> right, [ConstantExpected] byte shift);

    /// svint32_t svsri[_n_s32](svint32_t op1, svint32_t op2, uint64_t imm3) : "SRI Ztied1.S, Zop2.S, #imm3"
  public static unsafe Vector<int> ShiftRightAndInsert(Vector<int> left, Vector<int> right, [ConstantExpected] byte shift);

    /// svint64_t svsri[_n_s64](svint64_t op1, svint64_t op2, uint64_t imm3) : "SRI Ztied1.D, Zop2.D, #imm3"
  public static unsafe Vector<long> ShiftRightAndInsert(Vector<long> left, Vector<long> right, [ConstantExpected] byte shift);

    /// svuint8_t svsri[_n_u8](svuint8_t op1, svuint8_t op2, uint64_t imm3) : "SRI Ztied1.B, Zop2.B, #imm3"
  public static unsafe Vector<byte> ShiftRightAndInsert(Vector<byte> left, Vector<byte> right, [ConstantExpected] byte shift);

    /// svuint16_t svsri[_n_u16](svuint16_t op1, svuint16_t op2, uint64_t imm3) : "SRI Ztied1.H, Zop2.H, #imm3"
  public static unsafe Vector<ushort> ShiftRightAndInsert(Vector<ushort> left, Vector<ushort> right, [ConstantExpected] byte shift);

    /// svuint32_t svsri[_n_u32](svuint32_t op1, svuint32_t op2, uint64_t imm3) : "SRI Ztied1.S, Zop2.S, #imm3"
  public static unsafe Vector<uint> ShiftRightAndInsert(Vector<uint> left, Vector<uint> right, [ConstantExpected] byte shift);

    /// svuint64_t svsri[_n_u64](svuint64_t op1, svuint64_t op2, uint64_t imm3) : "SRI Ztied1.D, Zop2.D, #imm3"
  public static unsafe Vector<ulong> ShiftRightAndInsert(Vector<ulong> left, Vector<ulong> right, [ConstantExpected] byte shift);


    /// ShiftRightArithmeticAdd : Shift right and accumulate

    /// svint8_t svsra[_n_s8](svint8_t op1, svint8_t op2, uint64_t imm3) : "SSRA Ztied1.B, Zop2.B, #imm3" or "MOVPRFX Zresult, Zop1; SSRA Zresult.B, Zop2.B, #imm3"
  public static unsafe Vector<sbyte> ShiftRightArithmeticAdd(Vector<sbyte> addend, Vector<sbyte> value, [ConstantExpected] byte count);

    /// svint16_t svsra[_n_s16](svint16_t op1, svint16_t op2, uint64_t imm3) : "SSRA Ztied1.H, Zop2.H, #imm3" or "MOVPRFX Zresult, Zop1; SSRA Zresult.H, Zop2.H, #imm3"
  public static unsafe Vector<short> ShiftRightArithmeticAdd(Vector<short> addend, Vector<short> value, [ConstantExpected] byte count);

    /// svint32_t svsra[_n_s32](svint32_t op1, svint32_t op2, uint64_t imm3) : "SSRA Ztied1.S, Zop2.S, #imm3" or "MOVPRFX Zresult, Zop1; SSRA Zresult.S, Zop2.S, #imm3"
  public static unsafe Vector<int> ShiftRightArithmeticAdd(Vector<int> addend, Vector<int> value, [ConstantExpected] byte count);

    /// svint64_t svsra[_n_s64](svint64_t op1, svint64_t op2, uint64_t imm3) : "SSRA Ztied1.D, Zop2.D, #imm3" or "MOVPRFX Zresult, Zop1; SSRA Zresult.D, Zop2.D, #imm3"
  public static unsafe Vector<long> ShiftRightArithmeticAdd(Vector<long> addend, Vector<long> value, [ConstantExpected] byte count);


    /// ShiftRightArithmeticNarrowingSaturateEven : Saturating shift right narrow (bottom)

    /// svint8_t svqshrnb[_n_s16](svint16_t op1, uint64_t imm2) : "SQSHRNB Zresult.B, Zop1.H, #imm2"
  public static unsafe Vector<sbyte> ShiftRightArithmeticNarrowingSaturateEven(Vector<short> value, [ConstantExpected] byte count);

    /// svint16_t svqshrnb[_n_s32](svint32_t op1, uint64_t imm2) : "SQSHRNB Zresult.H, Zop1.S, #imm2"
  public static unsafe Vector<short> ShiftRightArithmeticNarrowingSaturateEven(Vector<int> value, [ConstantExpected] byte count);

    /// svint32_t svqshrnb[_n_s64](svint64_t op1, uint64_t imm2) : "SQSHRNB Zresult.S, Zop1.D, #imm2"
  public static unsafe Vector<int> ShiftRightArithmeticNarrowingSaturateEven(Vector<long> value, [ConstantExpected] byte count);

    /// svuint8_t svqshrnb[_n_u16](svuint16_t op1, uint64_t imm2) : "UQSHRNB Zresult.B, Zop1.H, #imm2"
  public static unsafe Vector<byte> ShiftRightArithmeticNarrowingSaturateEven(Vector<ushort> value, [ConstantExpected] byte count);

    /// svuint16_t svqshrnb[_n_u32](svuint32_t op1, uint64_t imm2) : "UQSHRNB Zresult.H, Zop1.S, #imm2"
  public static unsafe Vector<ushort> ShiftRightArithmeticNarrowingSaturateEven(Vector<uint> value, [ConstantExpected] byte count);

    /// svuint32_t svqshrnb[_n_u64](svuint64_t op1, uint64_t imm2) : "UQSHRNB Zresult.S, Zop1.D, #imm2"
  public static unsafe Vector<uint> ShiftRightArithmeticNarrowingSaturateEven(Vector<ulong> value, [ConstantExpected] byte count);


    /// ShiftRightArithmeticNarrowingSaturateOdd : Saturating shift right narrow (top)

    /// svint8_t svqshrnt[_n_s16](svint8_t even, svint16_t op1, uint64_t imm2) : "SQSHRNT Ztied.B, Zop1.H, #imm2"
  public static unsafe Vector<sbyte> ShiftRightArithmeticNarrowingSaturateOdd(Vector<sbyte> even, Vector<short> value, [ConstantExpected] byte count);

    /// svint16_t svqshrnt[_n_s32](svint16_t even, svint32_t op1, uint64_t imm2) : "SQSHRNT Ztied.H, Zop1.S, #imm2"
  public static unsafe Vector<short> ShiftRightArithmeticNarrowingSaturateOdd(Vector<short> even, Vector<int> value, [ConstantExpected] byte count);

    /// svint32_t svqshrnt[_n_s64](svint32_t even, svint64_t op1, uint64_t imm2) : "SQSHRNT Ztied.S, Zop1.D, #imm2"
  public static unsafe Vector<int> ShiftRightArithmeticNarrowingSaturateOdd(Vector<int> even, Vector<long> value, [ConstantExpected] byte count);

    /// svuint8_t svqshrnt[_n_u16](svuint8_t even, svuint16_t op1, uint64_t imm2) : "UQSHRNT Ztied.B, Zop1.H, #imm2"
  public static unsafe Vector<byte> ShiftRightArithmeticNarrowingSaturateOdd(Vector<byte> even, Vector<ushort> value, [ConstantExpected] byte count);

    /// svuint16_t svqshrnt[_n_u32](svuint16_t even, svuint32_t op1, uint64_t imm2) : "UQSHRNT Ztied.H, Zop1.S, #imm2"
  public static unsafe Vector<ushort> ShiftRightArithmeticNarrowingSaturateOdd(Vector<ushort> even, Vector<uint> value, [ConstantExpected] byte count);

    /// svuint32_t svqshrnt[_n_u64](svuint32_t even, svuint64_t op1, uint64_t imm2) : "UQSHRNT Ztied.S, Zop1.D, #imm2"
  public static unsafe Vector<uint> ShiftRightArithmeticNarrowingSaturateOdd(Vector<uint> even, Vector<ulong> value, [ConstantExpected] byte count);


    /// ShiftRightArithmeticNarrowingSaturateUnsignedEven : Saturating shift right unsigned narrow (bottom)

    /// svuint8_t svqshrunb[_n_s16](svint16_t op1, uint64_t imm2) : "SQSHRUNB Zresult.B, Zop1.H, #imm2"
  public static unsafe Vector<byte> ShiftRightArithmeticNarrowingSaturateUnsignedEven(Vector<short> value, [ConstantExpected] byte count);

    /// svuint16_t svqshrunb[_n_s32](svint32_t op1, uint64_t imm2) : "SQSHRUNB Zresult.H, Zop1.S, #imm2"
  public static unsafe Vector<ushort> ShiftRightArithmeticNarrowingSaturateUnsignedEven(Vector<int> value, [ConstantExpected] byte count);

    /// svuint32_t svqshrunb[_n_s64](svint64_t op1, uint64_t imm2) : "SQSHRUNB Zresult.S, Zop1.D, #imm2"
  public static unsafe Vector<uint> ShiftRightArithmeticNarrowingSaturateUnsignedEven(Vector<long> value, [ConstantExpected] byte count);


    /// ShiftRightArithmeticNarrowingSaturateUnsignedOdd : Saturating shift right unsigned narrow (top)

    /// svuint8_t svqshrunt[_n_s16](svuint8_t even, svint16_t op1, uint64_t imm2) : "SQSHRUNT Ztied.B, Zop1.H, #imm2"
  public static unsafe Vector<byte> ShiftRightArithmeticNarrowingSaturateUnsignedOdd(Vector<byte> even, Vector<short> value, [ConstantExpected] byte count);

    /// svuint16_t svqshrunt[_n_s32](svuint16_t even, svint32_t op1, uint64_t imm2) : "SQSHRUNT Ztied.H, Zop1.S, #imm2"
  public static unsafe Vector<ushort> ShiftRightArithmeticNarrowingSaturateUnsignedOdd(Vector<ushort> even, Vector<int> value, [ConstantExpected] byte count);

    /// svuint32_t svqshrunt[_n_s64](svuint32_t even, svint64_t op1, uint64_t imm2) : "SQSHRUNT Ztied.S, Zop1.D, #imm2"
  public static unsafe Vector<uint> ShiftRightArithmeticNarrowingSaturateUnsignedOdd(Vector<uint> even, Vector<long> value, [ConstantExpected] byte count);


    /// ShiftRightArithmeticRounded : Rounding shift right

    /// svint8_t svrshr[_n_s8]_m(svbool_t pg, svint8_t op1, uint64_t imm2) : "SRSHR Ztied1.B, Pg/M, Ztied1.B, #imm2" or "MOVPRFX Zresult, Zop1; SRSHR Zresult.B, Pg/M, Zresult.B, #imm2"
    /// svint8_t svrshr[_n_s8]_x(svbool_t pg, svint8_t op1, uint64_t imm2) : "SRSHR Ztied1.B, Pg/M, Ztied1.B, #imm2" or "MOVPRFX Zresult, Zop1; SRSHR Zresult.B, Pg/M, Zresult.B, #imm2"
    /// svint8_t svrshr[_n_s8]_z(svbool_t pg, svint8_t op1, uint64_t imm2) : "MOVPRFX Zresult.B, Pg/Z, Zop1.B; SRSHR Zresult.B, Pg/M, Zresult.B, #imm2"
  public static unsafe Vector<sbyte> ShiftRightArithmeticRounded(Vector<sbyte> value, [ConstantExpected] byte count);

    /// svint16_t svrshr[_n_s16]_m(svbool_t pg, svint16_t op1, uint64_t imm2) : "SRSHR Ztied1.H, Pg/M, Ztied1.H, #imm2" or "MOVPRFX Zresult, Zop1; SRSHR Zresult.H, Pg/M, Zresult.H, #imm2"
    /// svint16_t svrshr[_n_s16]_x(svbool_t pg, svint16_t op1, uint64_t imm2) : "SRSHR Ztied1.H, Pg/M, Ztied1.H, #imm2" or "MOVPRFX Zresult, Zop1; SRSHR Zresult.H, Pg/M, Zresult.H, #imm2"
    /// svint16_t svrshr[_n_s16]_z(svbool_t pg, svint16_t op1, uint64_t imm2) : "MOVPRFX Zresult.H, Pg/Z, Zop1.H; SRSHR Zresult.H, Pg/M, Zresult.H, #imm2"
  public static unsafe Vector<short> ShiftRightArithmeticRounded(Vector<short> value, [ConstantExpected] byte count);

    /// svint32_t svrshr[_n_s32]_m(svbool_t pg, svint32_t op1, uint64_t imm2) : "SRSHR Ztied1.S, Pg/M, Ztied1.S, #imm2" or "MOVPRFX Zresult, Zop1; SRSHR Zresult.S, Pg/M, Zresult.S, #imm2"
    /// svint32_t svrshr[_n_s32]_x(svbool_t pg, svint32_t op1, uint64_t imm2) : "SRSHR Ztied1.S, Pg/M, Ztied1.S, #imm2" or "MOVPRFX Zresult, Zop1; SRSHR Zresult.S, Pg/M, Zresult.S, #imm2"
    /// svint32_t svrshr[_n_s32]_z(svbool_t pg, svint32_t op1, uint64_t imm2) : "MOVPRFX Zresult.S, Pg/Z, Zop1.S; SRSHR Zresult.S, Pg/M, Zresult.S, #imm2"
  public static unsafe Vector<int> ShiftRightArithmeticRounded(Vector<int> value, [ConstantExpected] byte count);

    /// svint64_t svrshr[_n_s64]_m(svbool_t pg, svint64_t op1, uint64_t imm2) : "SRSHR Ztied1.D, Pg/M, Ztied1.D, #imm2" or "MOVPRFX Zresult, Zop1; SRSHR Zresult.D, Pg/M, Zresult.D, #imm2"
    /// svint64_t svrshr[_n_s64]_x(svbool_t pg, svint64_t op1, uint64_t imm2) : "SRSHR Ztied1.D, Pg/M, Ztied1.D, #imm2" or "MOVPRFX Zresult, Zop1; SRSHR Zresult.D, Pg/M, Zresult.D, #imm2"
    /// svint64_t svrshr[_n_s64]_z(svbool_t pg, svint64_t op1, uint64_t imm2) : "MOVPRFX Zresult.D, Pg/Z, Zop1.D; SRSHR Zresult.D, Pg/M, Zresult.D, #imm2"
  public static unsafe Vector<long> ShiftRightArithmeticRounded(Vector<long> value, [ConstantExpected] byte count);


    /// ShiftRightArithmeticRoundedAdd : Rounding shift right and accumulate

    /// svint8_t svrsra[_n_s8](svint8_t op1, svint8_t op2, uint64_t imm3) : "SRSRA Ztied1.B, Zop2.B, #imm3" or "MOVPRFX Zresult, Zop1; SRSRA Zresult.B, Zop2.B, #imm3"
  public static unsafe Vector<sbyte> ShiftRightArithmeticRoundedAdd(Vector<sbyte> addend, Vector<sbyte> value, [ConstantExpected] byte count);

    /// svint16_t svrsra[_n_s16](svint16_t op1, svint16_t op2, uint64_t imm3) : "SRSRA Ztied1.H, Zop2.H, #imm3" or "MOVPRFX Zresult, Zop1; SRSRA Zresult.H, Zop2.H, #imm3"
  public static unsafe Vector<short> ShiftRightArithmeticRoundedAdd(Vector<short> addend, Vector<short> value, [ConstantExpected] byte count);

    /// svint32_t svrsra[_n_s32](svint32_t op1, svint32_t op2, uint64_t imm3) : "SRSRA Ztied1.S, Zop2.S, #imm3" or "MOVPRFX Zresult, Zop1; SRSRA Zresult.S, Zop2.S, #imm3"
  public static unsafe Vector<int> ShiftRightArithmeticRoundedAdd(Vector<int> addend, Vector<int> value, [ConstantExpected] byte count);

    /// svint64_t svrsra[_n_s64](svint64_t op1, svint64_t op2, uint64_t imm3) : "SRSRA Ztied1.D, Zop2.D, #imm3" or "MOVPRFX Zresult, Zop1; SRSRA Zresult.D, Zop2.D, #imm3"
  public static unsafe Vector<long> ShiftRightArithmeticRoundedAdd(Vector<long> addend, Vector<long> value, [ConstantExpected] byte count);


    /// ShiftRightArithmeticRoundedNarrowingSaturateEven : Saturating rounding shift right narrow (bottom)

    /// svint8_t svqrshrnb[_n_s16](svint16_t op1, uint64_t imm2) : "SQRSHRNB Zresult.B, Zop1.H, #imm2"
  public static unsafe Vector<sbyte> ShiftRightArithmeticRoundedNarrowingSaturateEven(Vector<short> value, [ConstantExpected] byte count);

    /// svint16_t svqrshrnb[_n_s32](svint32_t op1, uint64_t imm2) : "SQRSHRNB Zresult.H, Zop1.S, #imm2"
  public static unsafe Vector<short> ShiftRightArithmeticRoundedNarrowingSaturateEven(Vector<int> value, [ConstantExpected] byte count);

    /// svint32_t svqrshrnb[_n_s64](svint64_t op1, uint64_t imm2) : "SQRSHRNB Zresult.S, Zop1.D, #imm2"
  public static unsafe Vector<int> ShiftRightArithmeticRoundedNarrowingSaturateEven(Vector<long> value, [ConstantExpected] byte count);


    /// ShiftRightArithmeticRoundedNarrowingSaturateOdd : Saturating rounding shift right narrow (top)

    /// svint8_t svqrshrnt[_n_s16](svint8_t even, svint16_t op1, uint64_t imm2) : "SQRSHRNT Ztied.B, Zop1.H, #imm2"
  public static unsafe Vector<sbyte> ShiftRightArithmeticRoundedNarrowingSaturateOdd(Vector<sbyte> even, Vector<short> value, [ConstantExpected] byte count);

    /// svint16_t svqrshrnt[_n_s32](svint16_t even, svint32_t op1, uint64_t imm2) : "SQRSHRNT Ztied.H, Zop1.S, #imm2"
  public static unsafe Vector<short> ShiftRightArithmeticRoundedNarrowingSaturateOdd(Vector<short> even, Vector<int> value, [ConstantExpected] byte count);

    /// svint32_t svqrshrnt[_n_s64](svint32_t even, svint64_t op1, uint64_t imm2) : "SQRSHRNT Ztied.S, Zop1.D, #imm2"
  public static unsafe Vector<int> ShiftRightArithmeticRoundedNarrowingSaturateOdd(Vector<int> even, Vector<long> value, [ConstantExpected] byte count);


    /// ShiftRightArithmeticRoundedNarrowingSaturateUnsignedEven : Saturating rounding shift right unsigned narrow (bottom)

    /// svuint8_t svqrshrunb[_n_s16](svint16_t op1, uint64_t imm2) : "SQRSHRUNB Zresult.B, Zop1.H, #imm2"
  public static unsafe Vector<byte> ShiftRightArithmeticRoundedNarrowingSaturateUnsignedEven(Vector<short> value, [ConstantExpected] byte count);

    /// svuint16_t svqrshrunb[_n_s32](svint32_t op1, uint64_t imm2) : "SQRSHRUNB Zresult.H, Zop1.S, #imm2"
  public static unsafe Vector<ushort> ShiftRightArithmeticRoundedNarrowingSaturateUnsignedEven(Vector<int> value, [ConstantExpected] byte count);

    /// svuint32_t svqrshrunb[_n_s64](svint64_t op1, uint64_t imm2) : "SQRSHRUNB Zresult.S, Zop1.D, #imm2"
  public static unsafe Vector<uint> ShiftRightArithmeticRoundedNarrowingSaturateUnsignedEven(Vector<long> value, [ConstantExpected] byte count);


    /// ShiftRightArithmeticRoundedNarrowingSaturateUnsignedOdd : Saturating rounding shift right unsigned narrow (top)

    /// svuint8_t svqrshrunt[_n_s16](svuint8_t even, svint16_t op1, uint64_t imm2) : "SQRSHRUNT Ztied.B, Zop1.H, #imm2"
  public static unsafe Vector<byte> ShiftRightArithmeticRoundedNarrowingSaturateUnsignedOdd(Vector<byte> even, Vector<short> value, [ConstantExpected] byte count);

    /// svuint16_t svqrshrunt[_n_s32](svuint16_t even, svint32_t op1, uint64_t imm2) : "SQRSHRUNT Ztied.H, Zop1.S, #imm2"
  public static unsafe Vector<ushort> ShiftRightArithmeticRoundedNarrowingSaturateUnsignedOdd(Vector<ushort> even, Vector<int> value, [ConstantExpected] byte count);

    /// svuint32_t svqrshrunt[_n_s64](svuint32_t even, svint64_t op1, uint64_t imm2) : "SQRSHRUNT Ztied.S, Zop1.D, #imm2"
  public static unsafe Vector<uint> ShiftRightArithmeticRoundedNarrowingSaturateUnsignedOdd(Vector<uint> even, Vector<long> value, [ConstantExpected] byte count);


    /// ShiftRightLogicalAdd : Shift right and accumulate

    /// svuint8_t svsra[_n_u8](svuint8_t op1, svuint8_t op2, uint64_t imm3) : "USRA Ztied1.B, Zop2.B, #imm3" or "MOVPRFX Zresult, Zop1; USRA Zresult.B, Zop2.B, #imm3"
  public static unsafe Vector<byte> ShiftRightLogicalAdd(Vector<byte> addend, Vector<byte> value, [ConstantExpected] byte count);

    /// svuint16_t svsra[_n_u16](svuint16_t op1, svuint16_t op2, uint64_t imm3) : "USRA Ztied1.H, Zop2.H, #imm3" or "MOVPRFX Zresult, Zop1; USRA Zresult.H, Zop2.H, #imm3"
  public static unsafe Vector<ushort> ShiftRightLogicalAdd(Vector<ushort> addend, Vector<ushort> value, [ConstantExpected] byte count);

    /// svuint32_t svsra[_n_u32](svuint32_t op1, svuint32_t op2, uint64_t imm3) : "USRA Ztied1.S, Zop2.S, #imm3" or "MOVPRFX Zresult, Zop1; USRA Zresult.S, Zop2.S, #imm3"
  public static unsafe Vector<uint> ShiftRightLogicalAdd(Vector<uint> addend, Vector<uint> value, [ConstantExpected] byte count);

    /// svuint64_t svsra[_n_u64](svuint64_t op1, svuint64_t op2, uint64_t imm3) : "USRA Ztied1.D, Zop2.D, #imm3" or "MOVPRFX Zresult, Zop1; USRA Zresult.D, Zop2.D, #imm3"
  public static unsafe Vector<ulong> ShiftRightLogicalAdd(Vector<ulong> addend, Vector<ulong> value, [ConstantExpected] byte count);


    /// ShiftRightLogicalNarrowingEven : Shift right narrow (bottom)

    /// svint8_t svshrnb[_n_s16](svint16_t op1, uint64_t imm2) : "SHRNB Zresult.B, Zop1.H, #imm2"
  public static unsafe Vector<sbyte> ShiftRightLogicalNarrowingEven(Vector<short> value, [ConstantExpected] byte count);

    /// svint16_t svshrnb[_n_s32](svint32_t op1, uint64_t imm2) : "SHRNB Zresult.H, Zop1.S, #imm2"
  public static unsafe Vector<short> ShiftRightLogicalNarrowingEven(Vector<int> value, [ConstantExpected] byte count);

    /// svint32_t svshrnb[_n_s64](svint64_t op1, uint64_t imm2) : "SHRNB Zresult.S, Zop1.D, #imm2"
  public static unsafe Vector<int> ShiftRightLogicalNarrowingEven(Vector<long> value, [ConstantExpected] byte count);

    /// svuint8_t svshrnb[_n_u16](svuint16_t op1, uint64_t imm2) : "SHRNB Zresult.B, Zop1.H, #imm2"
  public static unsafe Vector<byte> ShiftRightLogicalNarrowingEven(Vector<ushort> value, [ConstantExpected] byte count);

    /// svuint16_t svshrnb[_n_u32](svuint32_t op1, uint64_t imm2) : "SHRNB Zresult.H, Zop1.S, #imm2"
  public static unsafe Vector<ushort> ShiftRightLogicalNarrowingEven(Vector<uint> value, [ConstantExpected] byte count);

    /// svuint32_t svshrnb[_n_u64](svuint64_t op1, uint64_t imm2) : "SHRNB Zresult.S, Zop1.D, #imm2"
  public static unsafe Vector<uint> ShiftRightLogicalNarrowingEven(Vector<ulong> value, [ConstantExpected] byte count);


    /// ShiftRightLogicalNarrowingOdd : Shift right narrow (top)

    /// svint8_t svshrnt[_n_s16](svint8_t even, svint16_t op1, uint64_t imm2) : "SHRNT Ztied.B, Zop1.H, #imm2"
  public static unsafe Vector<sbyte> ShiftRightLogicalNarrowingOdd(Vector<sbyte> even, Vector<short> value, [ConstantExpected] byte count);

    /// svint16_t svshrnt[_n_s32](svint16_t even, svint32_t op1, uint64_t imm2) : "SHRNT Ztied.H, Zop1.S, #imm2"
  public static unsafe Vector<short> ShiftRightLogicalNarrowingOdd(Vector<short> even, Vector<int> value, [ConstantExpected] byte count);

    /// svint32_t svshrnt[_n_s64](svint32_t even, svint64_t op1, uint64_t imm2) : "SHRNT Ztied.S, Zop1.D, #imm2"
  public static unsafe Vector<int> ShiftRightLogicalNarrowingOdd(Vector<int> even, Vector<long> value, [ConstantExpected] byte count);

    /// svuint8_t svshrnt[_n_u16](svuint8_t even, svuint16_t op1, uint64_t imm2) : "SHRNT Ztied.B, Zop1.H, #imm2"
  public static unsafe Vector<byte> ShiftRightLogicalNarrowingOdd(Vector<byte> even, Vector<ushort> value, [ConstantExpected] byte count);

    /// svuint16_t svshrnt[_n_u32](svuint16_t even, svuint32_t op1, uint64_t imm2) : "SHRNT Ztied.H, Zop1.S, #imm2"
  public static unsafe Vector<ushort> ShiftRightLogicalNarrowingOdd(Vector<ushort> even, Vector<uint> value, [ConstantExpected] byte count);

    /// svuint32_t svshrnt[_n_u64](svuint32_t even, svuint64_t op1, uint64_t imm2) : "SHRNT Ztied.S, Zop1.D, #imm2"
  public static unsafe Vector<uint> ShiftRightLogicalNarrowingOdd(Vector<uint> even, Vector<ulong> value, [ConstantExpected] byte count);


    /// ShiftRightLogicalRounded : Rounding shift right

    /// svuint8_t svrshr[_n_u8]_m(svbool_t pg, svuint8_t op1, uint64_t imm2) : "URSHR Ztied1.B, Pg/M, Ztied1.B, #imm2" or "MOVPRFX Zresult, Zop1; URSHR Zresult.B, Pg/M, Zresult.B, #imm2"
    /// svuint8_t svrshr[_n_u8]_x(svbool_t pg, svuint8_t op1, uint64_t imm2) : "URSHR Ztied1.B, Pg/M, Ztied1.B, #imm2" or "MOVPRFX Zresult, Zop1; URSHR Zresult.B, Pg/M, Zresult.B, #imm2"
    /// svuint8_t svrshr[_n_u8]_z(svbool_t pg, svuint8_t op1, uint64_t imm2) : "MOVPRFX Zresult.B, Pg/Z, Zop1.B; URSHR Zresult.B, Pg/M, Zresult.B, #imm2"
  public static unsafe Vector<byte> ShiftRightLogicalRounded(Vector<byte> value, [ConstantExpected] byte count);

    /// svuint16_t svrshr[_n_u16]_m(svbool_t pg, svuint16_t op1, uint64_t imm2) : "URSHR Ztied1.H, Pg/M, Ztied1.H, #imm2" or "MOVPRFX Zresult, Zop1; URSHR Zresult.H, Pg/M, Zresult.H, #imm2"
    /// svuint16_t svrshr[_n_u16]_x(svbool_t pg, svuint16_t op1, uint64_t imm2) : "URSHR Ztied1.H, Pg/M, Ztied1.H, #imm2" or "MOVPRFX Zresult, Zop1; URSHR Zresult.H, Pg/M, Zresult.H, #imm2"
    /// svuint16_t svrshr[_n_u16]_z(svbool_t pg, svuint16_t op1, uint64_t imm2) : "MOVPRFX Zresult.H, Pg/Z, Zop1.H; URSHR Zresult.H, Pg/M, Zresult.H, #imm2"
  public static unsafe Vector<ushort> ShiftRightLogicalRounded(Vector<ushort> value, [ConstantExpected] byte count);

    /// svuint32_t svrshr[_n_u32]_m(svbool_t pg, svuint32_t op1, uint64_t imm2) : "URSHR Ztied1.S, Pg/M, Ztied1.S, #imm2" or "MOVPRFX Zresult, Zop1; URSHR Zresult.S, Pg/M, Zresult.S, #imm2"
    /// svuint32_t svrshr[_n_u32]_x(svbool_t pg, svuint32_t op1, uint64_t imm2) : "URSHR Ztied1.S, Pg/M, Ztied1.S, #imm2" or "MOVPRFX Zresult, Zop1; URSHR Zresult.S, Pg/M, Zresult.S, #imm2"
    /// svuint32_t svrshr[_n_u32]_z(svbool_t pg, svuint32_t op1, uint64_t imm2) : "MOVPRFX Zresult.S, Pg/Z, Zop1.S; URSHR Zresult.S, Pg/M, Zresult.S, #imm2"
  public static unsafe Vector<uint> ShiftRightLogicalRounded(Vector<uint> value, [ConstantExpected] byte count);

    /// svuint64_t svrshr[_n_u64]_m(svbool_t pg, svuint64_t op1, uint64_t imm2) : "URSHR Ztied1.D, Pg/M, Ztied1.D, #imm2" or "MOVPRFX Zresult, Zop1; URSHR Zresult.D, Pg/M, Zresult.D, #imm2"
    /// svuint64_t svrshr[_n_u64]_x(svbool_t pg, svuint64_t op1, uint64_t imm2) : "URSHR Ztied1.D, Pg/M, Ztied1.D, #imm2" or "MOVPRFX Zresult, Zop1; URSHR Zresult.D, Pg/M, Zresult.D, #imm2"
    /// svuint64_t svrshr[_n_u64]_z(svbool_t pg, svuint64_t op1, uint64_t imm2) : "MOVPRFX Zresult.D, Pg/Z, Zop1.D; URSHR Zresult.D, Pg/M, Zresult.D, #imm2"
  public static unsafe Vector<ulong> ShiftRightLogicalRounded(Vector<ulong> value, [ConstantExpected] byte count);


    /// ShiftRightLogicalRoundedAdd : Rounding shift right and accumulate

    /// svuint8_t svrsra[_n_u8](svuint8_t op1, svuint8_t op2, uint64_t imm3) : "URSRA Ztied1.B, Zop2.B, #imm3" or "MOVPRFX Zresult, Zop1; URSRA Zresult.B, Zop2.B, #imm3"
  public static unsafe Vector<byte> ShiftRightLogicalRoundedAdd(Vector<byte> addend, Vector<byte> value, [ConstantExpected] byte count);

    /// svuint16_t svrsra[_n_u16](svuint16_t op1, svuint16_t op2, uint64_t imm3) : "URSRA Ztied1.H, Zop2.H, #imm3" or "MOVPRFX Zresult, Zop1; URSRA Zresult.H, Zop2.H, #imm3"
  public static unsafe Vector<ushort> ShiftRightLogicalRoundedAdd(Vector<ushort> addend, Vector<ushort> value, [ConstantExpected] byte count);

    /// svuint32_t svrsra[_n_u32](svuint32_t op1, svuint32_t op2, uint64_t imm3) : "URSRA Ztied1.S, Zop2.S, #imm3" or "MOVPRFX Zresult, Zop1; URSRA Zresult.S, Zop2.S, #imm3"
  public static unsafe Vector<uint> ShiftRightLogicalRoundedAdd(Vector<uint> addend, Vector<uint> value, [ConstantExpected] byte count);

    /// svuint64_t svrsra[_n_u64](svuint64_t op1, svuint64_t op2, uint64_t imm3) : "URSRA Ztied1.D, Zop2.D, #imm3" or "MOVPRFX Zresult, Zop1; URSRA Zresult.D, Zop2.D, #imm3"
  public static unsafe Vector<ulong> ShiftRightLogicalRoundedAdd(Vector<ulong> addend, Vector<ulong> value, [ConstantExpected] byte count);


    /// ShiftRightLogicalRoundedNarrowingEven : Rounding shift right narrow (bottom)

    /// svint8_t svrshrnb[_n_s16](svint16_t op1, uint64_t imm2) : "RSHRNB Zresult.B, Zop1.H, #imm2"
  public static unsafe Vector<sbyte> ShiftRightLogicalRoundedNarrowingEven(Vector<short> value, [ConstantExpected] byte count);

    /// svint16_t svrshrnb[_n_s32](svint32_t op1, uint64_t imm2) : "RSHRNB Zresult.H, Zop1.S, #imm2"
  public static unsafe Vector<short> ShiftRightLogicalRoundedNarrowingEven(Vector<int> value, [ConstantExpected] byte count);

    /// svint32_t svrshrnb[_n_s64](svint64_t op1, uint64_t imm2) : "RSHRNB Zresult.S, Zop1.D, #imm2"
  public static unsafe Vector<int> ShiftRightLogicalRoundedNarrowingEven(Vector<long> value, [ConstantExpected] byte count);

    /// svuint8_t svrshrnb[_n_u16](svuint16_t op1, uint64_t imm2) : "RSHRNB Zresult.B, Zop1.H, #imm2"
  public static unsafe Vector<byte> ShiftRightLogicalRoundedNarrowingEven(Vector<ushort> value, [ConstantExpected] byte count);

    /// svuint16_t svrshrnb[_n_u32](svuint32_t op1, uint64_t imm2) : "RSHRNB Zresult.H, Zop1.S, #imm2"
  public static unsafe Vector<ushort> ShiftRightLogicalRoundedNarrowingEven(Vector<uint> value, [ConstantExpected] byte count);

    /// svuint32_t svrshrnb[_n_u64](svuint64_t op1, uint64_t imm2) : "RSHRNB Zresult.S, Zop1.D, #imm2"
  public static unsafe Vector<uint> ShiftRightLogicalRoundedNarrowingEven(Vector<ulong> value, [ConstantExpected] byte count);


    /// ShiftRightLogicalRoundedNarrowingOdd : Rounding shift right narrow (top)

    /// svint8_t svrshrnt[_n_s16](svint8_t even, svint16_t op1, uint64_t imm2) : "RSHRNT Ztied.B, Zop1.H, #imm2"
  public static unsafe Vector<sbyte> ShiftRightLogicalRoundedNarrowingOdd(Vector<sbyte> even, Vector<short> value, [ConstantExpected] byte count);

    /// svint16_t svrshrnt[_n_s32](svint16_t even, svint32_t op1, uint64_t imm2) : "RSHRNT Ztied.H, Zop1.S, #imm2"
  public static unsafe Vector<short> ShiftRightLogicalRoundedNarrowingOdd(Vector<short> even, Vector<int> value, [ConstantExpected] byte count);

    /// svint32_t svrshrnt[_n_s64](svint32_t even, svint64_t op1, uint64_t imm2) : "RSHRNT Ztied.S, Zop1.D, #imm2"
  public static unsafe Vector<int> ShiftRightLogicalRoundedNarrowingOdd(Vector<int> even, Vector<long> value, [ConstantExpected] byte count);

    /// svuint8_t svrshrnt[_n_u16](svuint8_t even, svuint16_t op1, uint64_t imm2) : "RSHRNT Ztied.B, Zop1.H, #imm2"
  public static unsafe Vector<byte> ShiftRightLogicalRoundedNarrowingOdd(Vector<byte> even, Vector<ushort> value, [ConstantExpected] byte count);

    /// svuint16_t svrshrnt[_n_u32](svuint16_t even, svuint32_t op1, uint64_t imm2) : "RSHRNT Ztied.H, Zop1.S, #imm2"
  public static unsafe Vector<ushort> ShiftRightLogicalRoundedNarrowingOdd(Vector<ushort> even, Vector<uint> value, [ConstantExpected] byte count);

    /// svuint32_t svrshrnt[_n_u64](svuint32_t even, svuint64_t op1, uint64_t imm2) : "RSHRNT Ztied.S, Zop1.D, #imm2"
  public static unsafe Vector<uint> ShiftRightLogicalRoundedNarrowingOdd(Vector<uint> even, Vector<ulong> value, [ConstantExpected] byte count);


    /// ShiftRightLogicalRoundedNarrowingSaturateEven : Saturating rounding shift right narrow (bottom)

    /// svuint8_t svqrshrnb[_n_u16](svuint16_t op1, uint64_t imm2) : "UQRSHRNB Zresult.B, Zop1.H, #imm2"
  public static unsafe Vector<byte> ShiftRightLogicalRoundedNarrowingSaturateEven(Vector<ushort> value, [ConstantExpected] byte count);

    /// svuint16_t svqrshrnb[_n_u32](svuint32_t op1, uint64_t imm2) : "UQRSHRNB Zresult.H, Zop1.S, #imm2"
  public static unsafe Vector<ushort> ShiftRightLogicalRoundedNarrowingSaturateEven(Vector<uint> value, [ConstantExpected] byte count);

    /// svuint32_t svqrshrnb[_n_u64](svuint64_t op1, uint64_t imm2) : "UQRSHRNB Zresult.S, Zop1.D, #imm2"
  public static unsafe Vector<uint> ShiftRightLogicalRoundedNarrowingSaturateEven(Vector<ulong> value, [ConstantExpected] byte count);


    /// ShiftRightLogicalRoundedNarrowingSaturateOdd : Saturating rounding shift right narrow (top)

    /// svuint8_t svqrshrnt[_n_u16](svuint8_t even, svuint16_t op1, uint64_t imm2) : "UQRSHRNT Ztied.B, Zop1.H, #imm2"
  public static unsafe Vector<byte> ShiftRightLogicalRoundedNarrowingSaturateOdd(Vector<byte> even, Vector<ushort> value, [ConstantExpected] byte count);

    /// svuint16_t svqrshrnt[_n_u32](svuint16_t even, svuint32_t op1, uint64_t imm2) : "UQRSHRNT Ztied.H, Zop1.S, #imm2"
  public static unsafe Vector<ushort> ShiftRightLogicalRoundedNarrowingSaturateOdd(Vector<ushort> even, Vector<uint> value, [ConstantExpected] byte count);

    /// svuint32_t svqrshrnt[_n_u64](svuint32_t even, svuint64_t op1, uint64_t imm2) : "UQRSHRNT Ztied.S, Zop1.D, #imm2"
  public static unsafe Vector<uint> ShiftRightLogicalRoundedNarrowingSaturateOdd(Vector<uint> even, Vector<ulong> value, [ConstantExpected] byte count);


    /// Xor : Bitwise exclusive OR of three vectors

    /// svint8_t sveor3[_s8](svint8_t op1, svint8_t op2, svint8_t op3) : "EOR3 Ztied1.D, Ztied1.D, Zop2.D, Zop3.D" or "EOR3 Ztied2.D, Ztied2.D, Zop3.D, Zop1.D" or "EOR3 Ztied3.D, Ztied3.D, Zop1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; EOR3 Zresult.D, Zresult.D, Zop2.D, Zop3.D"
  public static unsafe Vector<sbyte> Xor(Vector<sbyte> value1, Vector<sbyte> value2, Vector<sbyte> value3);

    /// svint16_t sveor3[_s16](svint16_t op1, svint16_t op2, svint16_t op3) : "EOR3 Ztied1.D, Ztied1.D, Zop2.D, Zop3.D" or "EOR3 Ztied2.D, Ztied2.D, Zop3.D, Zop1.D" or "EOR3 Ztied3.D, Ztied3.D, Zop1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; EOR3 Zresult.D, Zresult.D, Zop2.D, Zop3.D"
  public static unsafe Vector<short> Xor(Vector<short> value1, Vector<short> value2, Vector<short> value3);

    /// svint32_t sveor3[_s32](svint32_t op1, svint32_t op2, svint32_t op3) : "EOR3 Ztied1.D, Ztied1.D, Zop2.D, Zop3.D" or "EOR3 Ztied2.D, Ztied2.D, Zop3.D, Zop1.D" or "EOR3 Ztied3.D, Ztied3.D, Zop1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; EOR3 Zresult.D, Zresult.D, Zop2.D, Zop3.D"
  public static unsafe Vector<int> Xor(Vector<int> value1, Vector<int> value2, Vector<int> value3);

    /// svint64_t sveor3[_s64](svint64_t op1, svint64_t op2, svint64_t op3) : "EOR3 Ztied1.D, Ztied1.D, Zop2.D, Zop3.D" or "EOR3 Ztied2.D, Ztied2.D, Zop3.D, Zop1.D" or "EOR3 Ztied3.D, Ztied3.D, Zop1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; EOR3 Zresult.D, Zresult.D, Zop2.D, Zop3.D"
  public static unsafe Vector<long> Xor(Vector<long> value1, Vector<long> value2, Vector<long> value3);

    /// svuint8_t sveor3[_u8](svuint8_t op1, svuint8_t op2, svuint8_t op3) : "EOR3 Ztied1.D, Ztied1.D, Zop2.D, Zop3.D" or "EOR3 Ztied2.D, Ztied2.D, Zop3.D, Zop1.D" or "EOR3 Ztied3.D, Ztied3.D, Zop1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; EOR3 Zresult.D, Zresult.D, Zop2.D, Zop3.D"
  public static unsafe Vector<byte> Xor(Vector<byte> value1, Vector<byte> value2, Vector<byte> value3);

    /// svuint16_t sveor3[_u16](svuint16_t op1, svuint16_t op2, svuint16_t op3) : "EOR3 Ztied1.D, Ztied1.D, Zop2.D, Zop3.D" or "EOR3 Ztied2.D, Ztied2.D, Zop3.D, Zop1.D" or "EOR3 Ztied3.D, Ztied3.D, Zop1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; EOR3 Zresult.D, Zresult.D, Zop2.D, Zop3.D"
  public static unsafe Vector<ushort> Xor(Vector<ushort> value1, Vector<ushort> value2, Vector<ushort> value3);

    /// svuint32_t sveor3[_u32](svuint32_t op1, svuint32_t op2, svuint32_t op3) : "EOR3 Ztied1.D, Ztied1.D, Zop2.D, Zop3.D" or "EOR3 Ztied2.D, Ztied2.D, Zop3.D, Zop1.D" or "EOR3 Ztied3.D, Ztied3.D, Zop1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; EOR3 Zresult.D, Zresult.D, Zop2.D, Zop3.D"
  public static unsafe Vector<uint> Xor(Vector<uint> value1, Vector<uint> value2, Vector<uint> value3);

    /// svuint64_t sveor3[_u64](svuint64_t op1, svuint64_t op2, svuint64_t op3) : "EOR3 Ztied1.D, Ztied1.D, Zop2.D, Zop3.D" or "EOR3 Ztied2.D, Ztied2.D, Zop3.D, Zop1.D" or "EOR3 Ztied3.D, Ztied3.D, Zop1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; EOR3 Zresult.D, Zresult.D, Zop2.D, Zop3.D"
  public static unsafe Vector<ulong> Xor(Vector<ulong> value1, Vector<ulong> value2, Vector<ulong> value3);


    /// XorRotateRight : Bitwise exclusive OR and rotate right

    /// svint8_t svxar[_n_s8](svint8_t op1, svint8_t op2, uint64_t imm3) : "XAR Ztied1.B, Ztied1.B, Zop2.B, #imm3" or "XAR Ztied2.B, Ztied2.B, Zop1.B, #imm3" or "MOVPRFX Zresult, Zop1; XAR Zresult.B, Zresult.B, Zop2.B, #imm3"
  public static unsafe Vector<sbyte> XorRotateRight(Vector<sbyte> left, Vector<sbyte> right, [ConstantExpected] byte count);

    /// svint16_t svxar[_n_s16](svint16_t op1, svint16_t op2, uint64_t imm3) : "XAR Ztied1.H, Ztied1.H, Zop2.H, #imm3" or "XAR Ztied2.H, Ztied2.H, Zop1.H, #imm3" or "MOVPRFX Zresult, Zop1; XAR Zresult.H, Zresult.H, Zop2.H, #imm3"
  public static unsafe Vector<short> XorRotateRight(Vector<short> left, Vector<short> right, [ConstantExpected] byte count);

    /// svint32_t svxar[_n_s32](svint32_t op1, svint32_t op2, uint64_t imm3) : "XAR Ztied1.S, Ztied1.S, Zop2.S, #imm3" or "XAR Ztied2.S, Ztied2.S, Zop1.S, #imm3" or "MOVPRFX Zresult, Zop1; XAR Zresult.S, Zresult.S, Zop2.S, #imm3"
  public static unsafe Vector<int> XorRotateRight(Vector<int> left, Vector<int> right, [ConstantExpected] byte count);

    /// svint64_t svxar[_n_s64](svint64_t op1, svint64_t op2, uint64_t imm3) : "XAR Ztied1.D, Ztied1.D, Zop2.D, #imm3" or "XAR Ztied2.D, Ztied2.D, Zop1.D, #imm3" or "MOVPRFX Zresult, Zop1; XAR Zresult.D, Zresult.D, Zop2.D, #imm3"
  public static unsafe Vector<long> XorRotateRight(Vector<long> left, Vector<long> right, [ConstantExpected] byte count);

    /// svuint8_t svxar[_n_u8](svuint8_t op1, svuint8_t op2, uint64_t imm3) : "XAR Ztied1.B, Ztied1.B, Zop2.B, #imm3" or "XAR Ztied2.B, Ztied2.B, Zop1.B, #imm3" or "MOVPRFX Zresult, Zop1; XAR Zresult.B, Zresult.B, Zop2.B, #imm3"
  public static unsafe Vector<byte> XorRotateRight(Vector<byte> left, Vector<byte> right, [ConstantExpected] byte count);

    /// svuint16_t svxar[_n_u16](svuint16_t op1, svuint16_t op2, uint64_t imm3) : "XAR Ztied1.H, Ztied1.H, Zop2.H, #imm3" or "XAR Ztied2.H, Ztied2.H, Zop1.H, #imm3" or "MOVPRFX Zresult, Zop1; XAR Zresult.H, Zresult.H, Zop2.H, #imm3"
  public static unsafe Vector<ushort> XorRotateRight(Vector<ushort> left, Vector<ushort> right, [ConstantExpected] byte count);

    /// svuint32_t svxar[_n_u32](svuint32_t op1, svuint32_t op2, uint64_t imm3) : "XAR Ztied1.S, Ztied1.S, Zop2.S, #imm3" or "XAR Ztied2.S, Ztied2.S, Zop1.S, #imm3" or "MOVPRFX Zresult, Zop1; XAR Zresult.S, Zresult.S, Zop2.S, #imm3"
  public static unsafe Vector<uint> XorRotateRight(Vector<uint> left, Vector<uint> right, [ConstantExpected] byte count);

    /// svuint64_t svxar[_n_u64](svuint64_t op1, svuint64_t op2, uint64_t imm3) : "XAR Ztied1.D, Ztied1.D, Zop2.D, #imm3" or "XAR Ztied2.D, Ztied2.D, Zop1.D, #imm3" or "MOVPRFX Zresult, Zop1; XAR Zresult.D, Zresult.D, Zop2.D, #imm3"
  public static unsafe Vector<ulong> XorRotateRight(Vector<ulong> left, Vector<ulong> right, [ConstantExpected] byte count);


  /// total method signatures: 188
  /// total method names:      38
}


  /// Rejected:
  ///   public static unsafe Vector<sbyte> BitwiseClearXor(Vector<sbyte> xor, Vector<sbyte> value, sbyte mask); // svbcax[_n_s8]
  ///   public static unsafe Vector<short> BitwiseClearXor(Vector<short> xor, Vector<short> value, short mask); // svbcax[_n_s16]
  ///   public static unsafe Vector<int> BitwiseClearXor(Vector<int> xor, Vector<int> value, int mask); // svbcax[_n_s32]
  ///   public static unsafe Vector<long> BitwiseClearXor(Vector<long> xor, Vector<long> value, long mask); // svbcax[_n_s64]
  ///   public static unsafe Vector<byte> BitwiseClearXor(Vector<byte> xor, Vector<byte> value, byte mask); // svbcax[_n_u8]
  ///   public static unsafe Vector<ushort> BitwiseClearXor(Vector<ushort> xor, Vector<ushort> value, ushort mask); // svbcax[_n_u16]
  ///   public static unsafe Vector<uint> BitwiseClearXor(Vector<uint> xor, Vector<uint> value, uint mask); // svbcax[_n_u32]
  ///   public static unsafe Vector<ulong> BitwiseClearXor(Vector<ulong> xor, Vector<ulong> value, ulong mask); // svbcax[_n_u64]
  ///   public static unsafe Vector<sbyte> BitwiseSelect(Vector<sbyte> select, Vector<sbyte> left, sbyte right); // svbsl[_n_s8]
  ///   public static unsafe Vector<short> BitwiseSelect(Vector<short> select, Vector<short> left, short right); // svbsl[_n_s16]
  ///   public static unsafe Vector<int> BitwiseSelect(Vector<int> select, Vector<int> left, int right); // svbsl[_n_s32]
  ///   public static unsafe Vector<long> BitwiseSelect(Vector<long> select, Vector<long> left, long right); // svbsl[_n_s64]
  ///   public static unsafe Vector<byte> BitwiseSelect(Vector<byte> select, Vector<byte> left, byte right); // svbsl[_n_u8]
  ///   public static unsafe Vector<ushort> BitwiseSelect(Vector<ushort> select, Vector<ushort> left, ushort right); // svbsl[_n_u16]
  ///   public static unsafe Vector<uint> BitwiseSelect(Vector<uint> select, Vector<uint> left, uint right); // svbsl[_n_u32]
  ///   public static unsafe Vector<ulong> BitwiseSelect(Vector<ulong> select, Vector<ulong> left, ulong right); // svbsl[_n_u64]
  ///   public static unsafe Vector<sbyte> BitwiseSelectInverted(Vector<sbyte> select, Vector<sbyte> left, Vector<sbyte> right); // svnbsl[_s8]
  ///   public static unsafe Vector<short> BitwiseSelectInverted(Vector<short> select, Vector<short> left, Vector<short> right); // svnbsl[_s16]
  ///   public static unsafe Vector<int> BitwiseSelectInverted(Vector<int> select, Vector<int> left, Vector<int> right); // svnbsl[_s32]
  ///   public static unsafe Vector<long> BitwiseSelectInverted(Vector<long> select, Vector<long> left, Vector<long> right); // svnbsl[_s64]
  ///   public static unsafe Vector<byte> BitwiseSelectInverted(Vector<byte> select, Vector<byte> left, Vector<byte> right); // svnbsl[_u8]
  ///   public static unsafe Vector<ushort> BitwiseSelectInverted(Vector<ushort> select, Vector<ushort> left, Vector<ushort> right); // svnbsl[_u16]
  ///   public static unsafe Vector<uint> BitwiseSelectInverted(Vector<uint> select, Vector<uint> left, Vector<uint> right); // svnbsl[_u32]
  ///   public static unsafe Vector<ulong> BitwiseSelectInverted(Vector<ulong> select, Vector<ulong> left, Vector<ulong> right); // svnbsl[_u64]
  ///   public static unsafe Vector<sbyte> BitwiseSelectInverted(Vector<sbyte> select, Vector<sbyte> left, sbyte right); // svnbsl[_n_s8]
  ///   public static unsafe Vector<short> BitwiseSelectInverted(Vector<short> select, Vector<short> left, short right); // svnbsl[_n_s16]
  ///   public static unsafe Vector<int> BitwiseSelectInverted(Vector<int> select, Vector<int> left, int right); // svnbsl[_n_s32]
  ///   public static unsafe Vector<long> BitwiseSelectInverted(Vector<long> select, Vector<long> left, long right); // svnbsl[_n_s64]
  ///   public static unsafe Vector<byte> BitwiseSelectInverted(Vector<byte> select, Vector<byte> left, byte right); // svnbsl[_n_u8]
  ///   public static unsafe Vector<ushort> BitwiseSelectInverted(Vector<ushort> select, Vector<ushort> left, ushort right); // svnbsl[_n_u16]
  ///   public static unsafe Vector<uint> BitwiseSelectInverted(Vector<uint> select, Vector<uint> left, uint right); // svnbsl[_n_u32]
  ///   public static unsafe Vector<ulong> BitwiseSelectInverted(Vector<ulong> select, Vector<ulong> left, ulong right); // svnbsl[_n_u64]
  ///   public static unsafe Vector<sbyte> BitwiseSelectLeftInverted(Vector<sbyte> select, Vector<sbyte> left, sbyte right); // svbsl1n[_n_s8]
  ///   public static unsafe Vector<short> BitwiseSelectLeftInverted(Vector<short> select, Vector<short> left, short right); // svbsl1n[_n_s16]
  ///   public static unsafe Vector<int> BitwiseSelectLeftInverted(Vector<int> select, Vector<int> left, int right); // svbsl1n[_n_s32]
  ///   public static unsafe Vector<long> BitwiseSelectLeftInverted(Vector<long> select, Vector<long> left, long right); // svbsl1n[_n_s64]
  ///   public static unsafe Vector<byte> BitwiseSelectLeftInverted(Vector<byte> select, Vector<byte> left, byte right); // svbsl1n[_n_u8]
  ///   public static unsafe Vector<ushort> BitwiseSelectLeftInverted(Vector<ushort> select, Vector<ushort> left, ushort right); // svbsl1n[_n_u16]
  ///   public static unsafe Vector<uint> BitwiseSelectLeftInverted(Vector<uint> select, Vector<uint> left, uint right); // svbsl1n[_n_u32]
  ///   public static unsafe Vector<ulong> BitwiseSelectLeftInverted(Vector<ulong> select, Vector<ulong> left, ulong right); // svbsl1n[_n_u64]
  ///   public static unsafe Vector<sbyte> BitwiseSelectRightInverted(Vector<sbyte> select, Vector<sbyte> left, sbyte right); // svbsl2n[_n_s8]
  ///   public static unsafe Vector<short> BitwiseSelectRightInverted(Vector<short> select, Vector<short> left, short right); // svbsl2n[_n_s16]
  ///   public static unsafe Vector<int> BitwiseSelectRightInverted(Vector<int> select, Vector<int> left, int right); // svbsl2n[_n_s32]
  ///   public static unsafe Vector<long> BitwiseSelectRightInverted(Vector<long> select, Vector<long> left, long right); // svbsl2n[_n_s64]
  ///   public static unsafe Vector<byte> BitwiseSelectRightInverted(Vector<byte> select, Vector<byte> left, byte right); // svbsl2n[_n_u8]
  ///   public static unsafe Vector<ushort> BitwiseSelectRightInverted(Vector<ushort> select, Vector<ushort> left, ushort right); // svbsl2n[_n_u16]
  ///   public static unsafe Vector<uint> BitwiseSelectRightInverted(Vector<uint> select, Vector<uint> left, uint right); // svbsl2n[_n_u32]
  ///   public static unsafe Vector<ulong> BitwiseSelectRightInverted(Vector<ulong> select, Vector<ulong> left, ulong right); // svbsl2n[_n_u64]
  ///   public static unsafe Vector<sbyte> ShiftArithmeticSaturate(Vector<sbyte> value, sbyte count); // svqshl[_n_s8]_m or svqshl[_n_s8]_x or svqshl[_n_s8]_z
  ///   public static unsafe Vector<short> ShiftArithmeticSaturate(Vector<short> value, short count); // svqshl[_n_s16]_m or svqshl[_n_s16]_x or svqshl[_n_s16]_z
  ///   public static unsafe Vector<int> ShiftArithmeticSaturate(Vector<int> value, int count); // svqshl[_n_s32]_m or svqshl[_n_s32]_x or svqshl[_n_s32]_z
  ///   public static unsafe Vector<long> ShiftArithmeticSaturate(Vector<long> value, long count); // svqshl[_n_s64]_m or svqshl[_n_s64]_x or svqshl[_n_s64]_z
  ///   public static unsafe Vector<byte> ShiftLeftLogicalSaturate(Vector<byte> value, sbyte count); // svqshl[_n_u8]_m or svqshl[_n_u8]_x or svqshl[_n_u8]_z
  ///   public static unsafe Vector<ushort> ShiftLeftLogicalSaturate(Vector<ushort> value, short count); // svqshl[_n_u16]_m or svqshl[_n_u16]_x or svqshl[_n_u16]_z
  ///   public static unsafe Vector<uint> ShiftLeftLogicalSaturate(Vector<uint> value, int count); // svqshl[_n_u32]_m or svqshl[_n_u32]_x or svqshl[_n_u32]_z
  ///   public static unsafe Vector<ulong> ShiftLeftLogicalSaturate(Vector<ulong> value, long count); // svqshl[_n_u64]_m or svqshl[_n_u64]_x or svqshl[_n_u64]_z
  ///   public static unsafe Vector<sbyte> ShiftLogicalRounded(Vector<sbyte> value, sbyte count); // svrshl[_n_s8]_m or svrshl[_n_s8]_x or svrshl[_n_s8]_z
  ///   public static unsafe Vector<short> ShiftLogicalRounded(Vector<short> value, short count); // svrshl[_n_s16]_m or svrshl[_n_s16]_x or svrshl[_n_s16]_z
  ///   public static unsafe Vector<int> ShiftLogicalRounded(Vector<int> value, int count); // svrshl[_n_s32]_m or svrshl[_n_s32]_x or svrshl[_n_s32]_z
  ///   public static unsafe Vector<long> ShiftLogicalRounded(Vector<long> value, long count); // svrshl[_n_s64]_m or svrshl[_n_s64]_x or svrshl[_n_s64]_z
  ///   public static unsafe Vector<byte> ShiftLogicalRounded(Vector<byte> value, sbyte count); // svrshl[_n_u8]_m or svrshl[_n_u8]_x or svrshl[_n_u8]_z
  ///   public static unsafe Vector<ushort> ShiftLogicalRounded(Vector<ushort> value, short count); // svrshl[_n_u16]_m or svrshl[_n_u16]_x or svrshl[_n_u16]_z
  ///   public static unsafe Vector<uint> ShiftLogicalRounded(Vector<uint> value, int count); // svrshl[_n_u32]_m or svrshl[_n_u32]_x or svrshl[_n_u32]_z
  ///   public static unsafe Vector<ulong> ShiftLogicalRounded(Vector<ulong> value, long count); // svrshl[_n_u64]_m or svrshl[_n_u64]_x or svrshl[_n_u64]_z
  ///   public static unsafe Vector<sbyte> ShiftLogicalRoundedSaturate(Vector<sbyte> value, sbyte count); // svqrshl[_n_s8]_m or svqrshl[_n_s8]_x or svqrshl[_n_s8]_z
  ///   public static unsafe Vector<short> ShiftLogicalRoundedSaturate(Vector<short> value, short count); // svqrshl[_n_s16]_m or svqrshl[_n_s16]_x or svqrshl[_n_s16]_z
  ///   public static unsafe Vector<int> ShiftLogicalRoundedSaturate(Vector<int> value, int count); // svqrshl[_n_s32]_m or svqrshl[_n_s32]_x or svqrshl[_n_s32]_z
  ///   public static unsafe Vector<long> ShiftLogicalRoundedSaturate(Vector<long> value, long count); // svqrshl[_n_s64]_m or svqrshl[_n_s64]_x or svqrshl[_n_s64]_z
  ///   public static unsafe Vector<byte> ShiftLogicalRoundedSaturate(Vector<byte> value, sbyte count); // svqrshl[_n_u8]_m or svqrshl[_n_u8]_x or svqrshl[_n_u8]_z
  ///   public static unsafe Vector<ushort> ShiftLogicalRoundedSaturate(Vector<ushort> value, short count); // svqrshl[_n_u16]_m or svqrshl[_n_u16]_x or svqrshl[_n_u16]_z
  ///   public static unsafe Vector<uint> ShiftLogicalRoundedSaturate(Vector<uint> value, int count); // svqrshl[_n_u32]_m or svqrshl[_n_u32]_x or svqrshl[_n_u32]_z
  ///   public static unsafe Vector<ulong> ShiftLogicalRoundedSaturate(Vector<ulong> value, long count); // svqrshl[_n_u64]_m or svqrshl[_n_u64]_x or svqrshl[_n_u64]_z
  ///   public static unsafe Vector<sbyte> Xor(Vector<sbyte> value1, Vector<sbyte> value2, sbyte value3); // sveor3[_n_s8]
  ///   public static unsafe Vector<short> Xor(Vector<short> value1, Vector<short> value2, short value3); // sveor3[_n_s16]
  ///   public static unsafe Vector<int> Xor(Vector<int> value1, Vector<int> value2, int value3); // sveor3[_n_s32]
  ///   public static unsafe Vector<long> Xor(Vector<long> value1, Vector<long> value2, long value3); // sveor3[_n_s64]
  ///   public static unsafe Vector<byte> Xor(Vector<byte> value1, Vector<byte> value2, byte value3); // sveor3[_n_u8]
  ///   public static unsafe Vector<ushort> Xor(Vector<ushort> value1, Vector<ushort> value2, ushort value3); // sveor3[_n_u16]
  ///   public static unsafe Vector<uint> Xor(Vector<uint> value1, Vector<uint> value2, uint value3); // sveor3[_n_u32]
  ///   public static unsafe Vector<ulong> Xor(Vector<ulong> value1, Vector<ulong> value2, ulong value3); // sveor3[_n_u64]
  ///   Total Rejected: 80

  /// Total ACLE covered across API:      388

