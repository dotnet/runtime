namespace System.Runtime.Intrinsics.Arm;

/// VectorT Summary
public abstract partial class Sve : AdvSimd /// Feature: FEAT_SVE  Category: counting
{

  public static unsafe ulong Count16BitElements([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // CNTH

  public static unsafe ulong Count32BitElements([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // CNTW

  public static unsafe ulong Count64BitElements([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // CNTD

  public static unsafe ulong Count8BitElements([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // CNTB

  /// T: byte, sbyte, short, int, long, float, double, ushort, uint, ulong
  public static unsafe ulong GetActiveElementCount(Vector<T> mask, Vector<T> from); // CNTP

  /// T: [byte, sbyte], [ushort, short], [uint, int], [ulong, long]
  public static unsafe Vector<T> LeadingSignCount(Vector<T2> value); // CLS // predicated, MOVPRFX

  /// T: [byte, sbyte], [ushort, short], [uint, int], [ulong, long]
  public static unsafe Vector<T> LeadingZeroCount(Vector<T2> value); // CLZ // predicated, MOVPRFX

  /// T: byte, ushort, uint, ulong
  public static unsafe Vector<T> LeadingZeroCount(Vector<T> value); // CLZ // predicated, MOVPRFX

  /// T: [uint, float], [ulong, double], [byte, sbyte], [ushort, short], [uint, int], [ulong, long]
  public static unsafe Vector<T> PopCount(Vector<T2> value); // CNT // predicated, MOVPRFX

  /// T: byte, ushort, uint, ulong
  public static unsafe Vector<T> PopCount(Vector<T> value); // CNT // predicated, MOVPRFX

  public static unsafe int SaturatingDecrementBy16BitElementCount(int value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // SQDECH

  public static unsafe long SaturatingDecrementBy16BitElementCount(long value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // SQDECH

  public static unsafe uint SaturatingDecrementBy16BitElementCount(uint value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // UQDECH

  public static unsafe ulong SaturatingDecrementBy16BitElementCount(ulong value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // UQDECH

  /// T: short, ushort
  public static unsafe Vector<T> SaturatingDecrementBy16BitElementCount(Vector<T> value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // SQDECH or UQDECH // MOVPRFX

  public static unsafe int SaturatingDecrementBy32BitElementCount(int value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // SQDECW

  public static unsafe long SaturatingDecrementBy32BitElementCount(long value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // SQDECW

  public static unsafe uint SaturatingDecrementBy32BitElementCount(uint value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // UQDECW

  public static unsafe ulong SaturatingDecrementBy32BitElementCount(ulong value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // UQDECW

  /// T: int, uint
  public static unsafe Vector<T> SaturatingDecrementBy32BitElementCount(Vector<T> value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // SQDECW or UQDECW // MOVPRFX

  public static unsafe int SaturatingDecrementBy64BitElementCount(int value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // SQDECD

  public static unsafe long SaturatingDecrementBy64BitElementCount(long value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // SQDECD

  public static unsafe uint SaturatingDecrementBy64BitElementCount(uint value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // UQDECD

  public static unsafe ulong SaturatingDecrementBy64BitElementCount(ulong value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // UQDECD

  /// T: long, ulong
  public static unsafe Vector<T> SaturatingDecrementBy64BitElementCount(Vector<T> value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // SQDECD or UQDECD // MOVPRFX

  public static unsafe int SaturatingDecrementBy8BitElementCount(int value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // SQDECB

  public static unsafe long SaturatingDecrementBy8BitElementCount(long value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // SQDECB

  public static unsafe uint SaturatingDecrementBy8BitElementCount(uint value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // UQDECB

  public static unsafe ulong SaturatingDecrementBy8BitElementCount(ulong value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // UQDECB

  /// T: byte, ushort, uint, ulong
  public static unsafe int SaturatingDecrementByActiveElementCount(int value, Vector<T> from); // SQDECP

  /// T: byte, ushort, uint, ulong
  public static unsafe long SaturatingDecrementByActiveElementCount(long value, Vector<T> from); // SQDECP

  /// T: byte, ushort, uint, ulong
  public static unsafe uint SaturatingDecrementByActiveElementCount(uint value, Vector<T> from); // UQDECP

  /// T: byte, ushort, uint, ulong
  public static unsafe ulong SaturatingDecrementByActiveElementCount(ulong value, Vector<T> from); // UQDECP

  /// T: short, int, long, ushort, uint, ulong
  public static unsafe Vector<T> SaturatingDecrementByActiveElementCount(Vector<T> value, Vector<T> from); // SQDECP or UQDECP // MOVPRFX

  public static unsafe int SaturatingIncrementBy16BitElementCount(int value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // SQINCH

  public static unsafe long SaturatingIncrementBy16BitElementCount(long value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // SQINCH

  public static unsafe uint SaturatingIncrementBy16BitElementCount(uint value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // UQINCH

  public static unsafe ulong SaturatingIncrementBy16BitElementCount(ulong value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // UQINCH

  /// T: short, ushort
  public static unsafe Vector<T> SaturatingIncrementBy16BitElementCount(Vector<T> value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // SQINCH or UQINCH // MOVPRFX

  public static unsafe int SaturatingIncrementBy32BitElementCount(int value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // SQINCW

  public static unsafe long SaturatingIncrementBy32BitElementCount(long value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // SQINCW

  public static unsafe uint SaturatingIncrementBy32BitElementCount(uint value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // UQINCW

  public static unsafe ulong SaturatingIncrementBy32BitElementCount(ulong value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // UQINCW

  /// T: int, uint
  public static unsafe Vector<T> SaturatingIncrementBy32BitElementCount(Vector<T> value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // SQINCW or UQINCW // MOVPRFX

  public static unsafe int SaturatingIncrementBy64BitElementCount(int value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // SQINCD

  public static unsafe long SaturatingIncrementBy64BitElementCount(long value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // SQINCD

  public static unsafe uint SaturatingIncrementBy64BitElementCount(uint value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // UQINCD

  public static unsafe ulong SaturatingIncrementBy64BitElementCount(ulong value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // UQINCD

  /// T: long, ulong
  public static unsafe Vector<T> SaturatingIncrementBy64BitElementCount(Vector<T> value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // SQINCD or UQINCD // MOVPRFX

  public static unsafe int SaturatingIncrementBy8BitElementCount(int value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // SQINCB

  public static unsafe long SaturatingIncrementBy8BitElementCount(long value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // SQINCB

  public static unsafe uint SaturatingIncrementBy8BitElementCount(uint value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // UQINCB

  public static unsafe ulong SaturatingIncrementBy8BitElementCount(ulong value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // UQINCB

  /// T: byte, ushort, uint, ulong
  public static unsafe int SaturatingIncrementByActiveElementCount(int value, Vector<T> from); // SQINCP

  /// T: byte, ushort, uint, ulong
  public static unsafe long SaturatingIncrementByActiveElementCount(long value, Vector<T> from); // SQINCP

  /// T: byte, ushort, uint, ulong
  public static unsafe uint SaturatingIncrementByActiveElementCount(uint value, Vector<T> from); // UQINCP

  /// T: byte, ushort, uint, ulong
  public static unsafe ulong SaturatingIncrementByActiveElementCount(ulong value, Vector<T> from); // UQINCP

  /// T: short, int, long, ushort, uint, ulong
  public static unsafe Vector<T> SaturatingIncrementByActiveElementCount(Vector<T> value, Vector<T> from); // SQINCP or UQINCP // MOVPRFX


  // All patterns used by PTRUE.
  public enum SveMaskPattern : byte
  {
    LargestPowerOf2 = 0,   // The largest power of 2.
    VectorCount1 = 1,    // 1 element.
    VectorCount2 = 2,    // 2 elements.
    VectorCount3 = 3,    // 3 elements.
    VectorCount4 = 4,    // 4 elements.
    VectorCount5 = 5,    // 5 elements.
    VectorCount6 = 6,    // 6 elements.
    VectorCount7 = 7,    // 7 elements.
    VectorCount8 = 8,    // 8 elements.
    VectorCount16 = 9,   // 16 elements.
    VectorCount32 = 10,  // 32 elements.
    VectorCount64 = 11,  // 64 elements.
    VectorCount128 = 12, // 128 elements.
    VectorCount256 = 13, // 256 elements.
    LargestMultipleOf4 = 29,  // The largest multiple of 4.
    LargestMultipleOf3 = 30,  // The largest multiple of 3.
    All  = 31    // All available (implicitly a multiple of two).
  };

  /// total method signatures: 58

}


/// Full API
public abstract partial class Sve : AdvSimd /// Feature: FEAT_SVE  Category: counting
{
    /// Count16BitElements : Count the number of 16-bit elements in a vector

    /// uint64_t svcnth_pat(enum svpattern pattern) : "CNTH Xresult, pattern"
  public static unsafe ulong Count16BitElements([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All);


    /// Count32BitElements : Count the number of 32-bit elements in a vector

    /// uint64_t svcntw_pat(enum svpattern pattern) : "CNTW Xresult, pattern"
  public static unsafe ulong Count32BitElements([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All);


    /// Count64BitElements : Count the number of 64-bit elements in a vector

    /// uint64_t svcntd_pat(enum svpattern pattern) : "CNTD Xresult, pattern"
  public static unsafe ulong Count64BitElements([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All);


    /// Count8BitElements : Count the number of 8-bit elements in a vector

    /// uint64_t svcntb_pat(enum svpattern pattern) : "CNTB Xresult, pattern"
  public static unsafe ulong Count8BitElements([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All);


    /// GetActiveElementCount : Count set predicate bits

    /// uint64_t svcntp_b8(svbool_t pg, svbool_t op) : "CNTP Xresult, Pg, Pop.B"
  public static unsafe ulong GetActiveElementCount(Vector<byte> mask, Vector<byte> from);

    /// uint64_t svcntp_b8(svbool_t pg, svbool_t op) : "CNTP Xresult, Pg, Pop.B"
  public static unsafe ulong GetActiveElementCount(Vector<sbyte> mask, Vector<sbyte> from);

    /// uint64_t svcntp_b8(svbool_t pg, svbool_t op) : "CNTP Xresult, Pg, Pop.B"
  public static unsafe ulong GetActiveElementCount(Vector<short> mask, Vector<short> from);

    /// uint64_t svcntp_b8(svbool_t pg, svbool_t op) : "CNTP Xresult, Pg, Pop.B"
  public static unsafe ulong GetActiveElementCount(Vector<int> mask, Vector<int> from);

    /// uint64_t svcntp_b8(svbool_t pg, svbool_t op) : "CNTP Xresult, Pg, Pop.B"
  public static unsafe ulong GetActiveElementCount(Vector<long> mask, Vector<long> from);

    /// uint64_t svcntp_b8(svbool_t pg, svbool_t op) : "CNTP Xresult, Pg, Pop.B"
  public static unsafe ulong GetActiveElementCount(Vector<float> mask, Vector<float> from);

    /// uint64_t svcntp_b8(svbool_t pg, svbool_t op) : "CNTP Xresult, Pg, Pop.B"
  public static unsafe ulong GetActiveElementCount(Vector<double> mask, Vector<double> from);

    /// uint64_t svcntp_b16(svbool_t pg, svbool_t op) : "CNTP Xresult, Pg, Pop.H"
  public static unsafe ulong GetActiveElementCount(Vector<ushort> mask, Vector<ushort> from);

    /// uint64_t svcntp_b32(svbool_t pg, svbool_t op) : "CNTP Xresult, Pg, Pop.S"
  public static unsafe ulong GetActiveElementCount(Vector<uint> mask, Vector<uint> from);

    /// uint64_t svcntp_b64(svbool_t pg, svbool_t op) : "CNTP Xresult, Pg, Pop.D"
  public static unsafe ulong GetActiveElementCount(Vector<ulong> mask, Vector<ulong> from);


    /// LeadingSignCount : Count leading sign bits

    /// svuint8_t svcls[_s8]_m(svuint8_t inactive, svbool_t pg, svint8_t op) : "CLS Ztied.B, Pg/M, Zop.B" or "MOVPRFX Zresult, Zinactive; CLS Zresult.B, Pg/M, Zop.B"
    /// svuint8_t svcls[_s8]_x(svbool_t pg, svint8_t op) : "CLS Ztied.B, Pg/M, Ztied.B" or "MOVPRFX Zresult, Zop; CLS Zresult.B, Pg/M, Zop.B"
    /// svuint8_t svcls[_s8]_z(svbool_t pg, svint8_t op) : "MOVPRFX Zresult.B, Pg/Z, Zop.B; CLS Zresult.B, Pg/M, Zop.B"
  public static unsafe Vector<byte> LeadingSignCount(Vector<sbyte> value);

    /// svuint16_t svcls[_s16]_m(svuint16_t inactive, svbool_t pg, svint16_t op) : "CLS Ztied.H, Pg/M, Zop.H" or "MOVPRFX Zresult, Zinactive; CLS Zresult.H, Pg/M, Zop.H"
    /// svuint16_t svcls[_s16]_x(svbool_t pg, svint16_t op) : "CLS Ztied.H, Pg/M, Ztied.H" or "MOVPRFX Zresult, Zop; CLS Zresult.H, Pg/M, Zop.H"
    /// svuint16_t svcls[_s16]_z(svbool_t pg, svint16_t op) : "MOVPRFX Zresult.H, Pg/Z, Zop.H; CLS Zresult.H, Pg/M, Zop.H"
  public static unsafe Vector<ushort> LeadingSignCount(Vector<short> value);

    /// svuint32_t svcls[_s32]_m(svuint32_t inactive, svbool_t pg, svint32_t op) : "CLS Ztied.S, Pg/M, Zop.S" or "MOVPRFX Zresult, Zinactive; CLS Zresult.S, Pg/M, Zop.S"
    /// svuint32_t svcls[_s32]_x(svbool_t pg, svint32_t op) : "CLS Ztied.S, Pg/M, Ztied.S" or "MOVPRFX Zresult, Zop; CLS Zresult.S, Pg/M, Zop.S"
    /// svuint32_t svcls[_s32]_z(svbool_t pg, svint32_t op) : "MOVPRFX Zresult.S, Pg/Z, Zop.S; CLS Zresult.S, Pg/M, Zop.S"
  public static unsafe Vector<uint> LeadingSignCount(Vector<int> value);

    /// svuint64_t svcls[_s64]_m(svuint64_t inactive, svbool_t pg, svint64_t op) : "CLS Ztied.D, Pg/M, Zop.D" or "MOVPRFX Zresult, Zinactive; CLS Zresult.D, Pg/M, Zop.D"
    /// svuint64_t svcls[_s64]_x(svbool_t pg, svint64_t op) : "CLS Ztied.D, Pg/M, Ztied.D" or "MOVPRFX Zresult, Zop; CLS Zresult.D, Pg/M, Zop.D"
    /// svuint64_t svcls[_s64]_z(svbool_t pg, svint64_t op) : "MOVPRFX Zresult.D, Pg/Z, Zop.D; CLS Zresult.D, Pg/M, Zop.D"
  public static unsafe Vector<ulong> LeadingSignCount(Vector<long> value);


    /// LeadingZeroCount : Count leading zero bits

    /// svuint8_t svclz[_s8]_m(svuint8_t inactive, svbool_t pg, svint8_t op) : "CLZ Ztied.B, Pg/M, Zop.B" or "MOVPRFX Zresult, Zinactive; CLZ Zresult.B, Pg/M, Zop.B"
    /// svuint8_t svclz[_s8]_x(svbool_t pg, svint8_t op) : "CLZ Ztied.B, Pg/M, Ztied.B" or "MOVPRFX Zresult, Zop; CLZ Zresult.B, Pg/M, Zop.B"
    /// svuint8_t svclz[_s8]_z(svbool_t pg, svint8_t op) : "MOVPRFX Zresult.B, Pg/Z, Zop.B; CLZ Zresult.B, Pg/M, Zop.B"
  public static unsafe Vector<byte> LeadingZeroCount(Vector<sbyte> value);

    /// svuint16_t svclz[_s16]_m(svuint16_t inactive, svbool_t pg, svint16_t op) : "CLZ Ztied.H, Pg/M, Zop.H" or "MOVPRFX Zresult, Zinactive; CLZ Zresult.H, Pg/M, Zop.H"
    /// svuint16_t svclz[_s16]_x(svbool_t pg, svint16_t op) : "CLZ Ztied.H, Pg/M, Ztied.H" or "MOVPRFX Zresult, Zop; CLZ Zresult.H, Pg/M, Zop.H"
    /// svuint16_t svclz[_s16]_z(svbool_t pg, svint16_t op) : "MOVPRFX Zresult.H, Pg/Z, Zop.H; CLZ Zresult.H, Pg/M, Zop.H"
  public static unsafe Vector<ushort> LeadingZeroCount(Vector<short> value);

    /// svuint32_t svclz[_s32]_m(svuint32_t inactive, svbool_t pg, svint32_t op) : "CLZ Ztied.S, Pg/M, Zop.S" or "MOVPRFX Zresult, Zinactive; CLZ Zresult.S, Pg/M, Zop.S"
    /// svuint32_t svclz[_s32]_x(svbool_t pg, svint32_t op) : "CLZ Ztied.S, Pg/M, Ztied.S" or "MOVPRFX Zresult, Zop; CLZ Zresult.S, Pg/M, Zop.S"
    /// svuint32_t svclz[_s32]_z(svbool_t pg, svint32_t op) : "MOVPRFX Zresult.S, Pg/Z, Zop.S; CLZ Zresult.S, Pg/M, Zop.S"
  public static unsafe Vector<uint> LeadingZeroCount(Vector<int> value);

    /// svuint64_t svclz[_s64]_m(svuint64_t inactive, svbool_t pg, svint64_t op) : "CLZ Ztied.D, Pg/M, Zop.D" or "MOVPRFX Zresult, Zinactive; CLZ Zresult.D, Pg/M, Zop.D"
    /// svuint64_t svclz[_s64]_x(svbool_t pg, svint64_t op) : "CLZ Ztied.D, Pg/M, Ztied.D" or "MOVPRFX Zresult, Zop; CLZ Zresult.D, Pg/M, Zop.D"
    /// svuint64_t svclz[_s64]_z(svbool_t pg, svint64_t op) : "MOVPRFX Zresult.D, Pg/Z, Zop.D; CLZ Zresult.D, Pg/M, Zop.D"
  public static unsafe Vector<ulong> LeadingZeroCount(Vector<long> value);

    /// svuint8_t svclz[_u8]_m(svuint8_t inactive, svbool_t pg, svuint8_t op) : "CLZ Ztied.B, Pg/M, Zop.B" or "MOVPRFX Zresult, Zinactive; CLZ Zresult.B, Pg/M, Zop.B"
    /// svuint8_t svclz[_u8]_x(svbool_t pg, svuint8_t op) : "CLZ Ztied.B, Pg/M, Ztied.B" or "MOVPRFX Zresult, Zop; CLZ Zresult.B, Pg/M, Zop.B"
    /// svuint8_t svclz[_u8]_z(svbool_t pg, svuint8_t op) : "MOVPRFX Zresult.B, Pg/Z, Zop.B; CLZ Zresult.B, Pg/M, Zop.B"
  public static unsafe Vector<byte> LeadingZeroCount(Vector<byte> value);

    /// svuint16_t svclz[_u16]_m(svuint16_t inactive, svbool_t pg, svuint16_t op) : "CLZ Ztied.H, Pg/M, Zop.H" or "MOVPRFX Zresult, Zinactive; CLZ Zresult.H, Pg/M, Zop.H"
    /// svuint16_t svclz[_u16]_x(svbool_t pg, svuint16_t op) : "CLZ Ztied.H, Pg/M, Ztied.H" or "MOVPRFX Zresult, Zop; CLZ Zresult.H, Pg/M, Zop.H"
    /// svuint16_t svclz[_u16]_z(svbool_t pg, svuint16_t op) : "MOVPRFX Zresult.H, Pg/Z, Zop.H; CLZ Zresult.H, Pg/M, Zop.H"
  public static unsafe Vector<ushort> LeadingZeroCount(Vector<ushort> value);

    /// svuint32_t svclz[_u32]_m(svuint32_t inactive, svbool_t pg, svuint32_t op) : "CLZ Ztied.S, Pg/M, Zop.S" or "MOVPRFX Zresult, Zinactive; CLZ Zresult.S, Pg/M, Zop.S"
    /// svuint32_t svclz[_u32]_x(svbool_t pg, svuint32_t op) : "CLZ Ztied.S, Pg/M, Ztied.S" or "MOVPRFX Zresult, Zop; CLZ Zresult.S, Pg/M, Zop.S"
    /// svuint32_t svclz[_u32]_z(svbool_t pg, svuint32_t op) : "MOVPRFX Zresult.S, Pg/Z, Zop.S; CLZ Zresult.S, Pg/M, Zop.S"
  public static unsafe Vector<uint> LeadingZeroCount(Vector<uint> value);

    /// svuint64_t svclz[_u64]_m(svuint64_t inactive, svbool_t pg, svuint64_t op) : "CLZ Ztied.D, Pg/M, Zop.D" or "MOVPRFX Zresult, Zinactive; CLZ Zresult.D, Pg/M, Zop.D"
    /// svuint64_t svclz[_u64]_x(svbool_t pg, svuint64_t op) : "CLZ Ztied.D, Pg/M, Ztied.D" or "MOVPRFX Zresult, Zop; CLZ Zresult.D, Pg/M, Zop.D"
    /// svuint64_t svclz[_u64]_z(svbool_t pg, svuint64_t op) : "MOVPRFX Zresult.D, Pg/Z, Zop.D; CLZ Zresult.D, Pg/M, Zop.D"
  public static unsafe Vector<ulong> LeadingZeroCount(Vector<ulong> value);


    /// PopCount : Count nonzero bits

    /// svuint32_t svcnt[_f32]_m(svuint32_t inactive, svbool_t pg, svfloat32_t op) : "CNT Ztied.S, Pg/M, Zop.S" or "MOVPRFX Zresult, Zinactive; CNT Zresult.S, Pg/M, Zop.S"
    /// svuint32_t svcnt[_f32]_x(svbool_t pg, svfloat32_t op) : "CNT Ztied.S, Pg/M, Ztied.S" or "MOVPRFX Zresult, Zop; CNT Zresult.S, Pg/M, Zop.S"
    /// svuint32_t svcnt[_f32]_z(svbool_t pg, svfloat32_t op) : "MOVPRFX Zresult.S, Pg/Z, Zop.S; CNT Zresult.S, Pg/M, Zop.S"
  public static unsafe Vector<uint> PopCount(Vector<float> value);

    /// svuint64_t svcnt[_f64]_m(svuint64_t inactive, svbool_t pg, svfloat64_t op) : "CNT Ztied.D, Pg/M, Zop.D" or "MOVPRFX Zresult, Zinactive; CNT Zresult.D, Pg/M, Zop.D"
    /// svuint64_t svcnt[_f64]_x(svbool_t pg, svfloat64_t op) : "CNT Ztied.D, Pg/M, Ztied.D" or "MOVPRFX Zresult, Zop; CNT Zresult.D, Pg/M, Zop.D"
    /// svuint64_t svcnt[_f64]_z(svbool_t pg, svfloat64_t op) : "MOVPRFX Zresult.D, Pg/Z, Zop.D; CNT Zresult.D, Pg/M, Zop.D"
  public static unsafe Vector<ulong> PopCount(Vector<double> value);

    /// svuint8_t svcnt[_s8]_m(svuint8_t inactive, svbool_t pg, svint8_t op) : "CNT Ztied.B, Pg/M, Zop.B" or "MOVPRFX Zresult, Zinactive; CNT Zresult.B, Pg/M, Zop.B"
    /// svuint8_t svcnt[_s8]_x(svbool_t pg, svint8_t op) : "CNT Ztied.B, Pg/M, Ztied.B" or "MOVPRFX Zresult, Zop; CNT Zresult.B, Pg/M, Zop.B"
    /// svuint8_t svcnt[_s8]_z(svbool_t pg, svint8_t op) : "MOVPRFX Zresult.B, Pg/Z, Zop.B; CNT Zresult.B, Pg/M, Zop.B"
  public static unsafe Vector<byte> PopCount(Vector<sbyte> value);

    /// svuint16_t svcnt[_s16]_m(svuint16_t inactive, svbool_t pg, svint16_t op) : "CNT Ztied.H, Pg/M, Zop.H" or "MOVPRFX Zresult, Zinactive; CNT Zresult.H, Pg/M, Zop.H"
    /// svuint16_t svcnt[_s16]_x(svbool_t pg, svint16_t op) : "CNT Ztied.H, Pg/M, Ztied.H" or "MOVPRFX Zresult, Zop; CNT Zresult.H, Pg/M, Zop.H"
    /// svuint16_t svcnt[_s16]_z(svbool_t pg, svint16_t op) : "MOVPRFX Zresult.H, Pg/Z, Zop.H; CNT Zresult.H, Pg/M, Zop.H"
  public static unsafe Vector<ushort> PopCount(Vector<short> value);

    /// svuint32_t svcnt[_s32]_m(svuint32_t inactive, svbool_t pg, svint32_t op) : "CNT Ztied.S, Pg/M, Zop.S" or "MOVPRFX Zresult, Zinactive; CNT Zresult.S, Pg/M, Zop.S"
    /// svuint32_t svcnt[_s32]_x(svbool_t pg, svint32_t op) : "CNT Ztied.S, Pg/M, Ztied.S" or "MOVPRFX Zresult, Zop; CNT Zresult.S, Pg/M, Zop.S"
    /// svuint32_t svcnt[_s32]_z(svbool_t pg, svint32_t op) : "MOVPRFX Zresult.S, Pg/Z, Zop.S; CNT Zresult.S, Pg/M, Zop.S"
  public static unsafe Vector<uint> PopCount(Vector<int> value);

    /// svuint64_t svcnt[_s64]_m(svuint64_t inactive, svbool_t pg, svint64_t op) : "CNT Ztied.D, Pg/M, Zop.D" or "MOVPRFX Zresult, Zinactive; CNT Zresult.D, Pg/M, Zop.D"
    /// svuint64_t svcnt[_s64]_x(svbool_t pg, svint64_t op) : "CNT Ztied.D, Pg/M, Ztied.D" or "MOVPRFX Zresult, Zop; CNT Zresult.D, Pg/M, Zop.D"
    /// svuint64_t svcnt[_s64]_z(svbool_t pg, svint64_t op) : "MOVPRFX Zresult.D, Pg/Z, Zop.D; CNT Zresult.D, Pg/M, Zop.D"
  public static unsafe Vector<ulong> PopCount(Vector<long> value);

    /// svuint8_t svcnt[_u8]_m(svuint8_t inactive, svbool_t pg, svuint8_t op) : "CNT Ztied.B, Pg/M, Zop.B" or "MOVPRFX Zresult, Zinactive; CNT Zresult.B, Pg/M, Zop.B"
    /// svuint8_t svcnt[_u8]_x(svbool_t pg, svuint8_t op) : "CNT Ztied.B, Pg/M, Ztied.B" or "MOVPRFX Zresult, Zop; CNT Zresult.B, Pg/M, Zop.B"
    /// svuint8_t svcnt[_u8]_z(svbool_t pg, svuint8_t op) : "MOVPRFX Zresult.B, Pg/Z, Zop.B; CNT Zresult.B, Pg/M, Zop.B"
  public static unsafe Vector<byte> PopCount(Vector<byte> value);

    /// svuint16_t svcnt[_u16]_m(svuint16_t inactive, svbool_t pg, svuint16_t op) : "CNT Ztied.H, Pg/M, Zop.H" or "MOVPRFX Zresult, Zinactive; CNT Zresult.H, Pg/M, Zop.H"
    /// svuint16_t svcnt[_u16]_x(svbool_t pg, svuint16_t op) : "CNT Ztied.H, Pg/M, Ztied.H" or "MOVPRFX Zresult, Zop; CNT Zresult.H, Pg/M, Zop.H"
    /// svuint16_t svcnt[_u16]_z(svbool_t pg, svuint16_t op) : "MOVPRFX Zresult.H, Pg/Z, Zop.H; CNT Zresult.H, Pg/M, Zop.H"
  public static unsafe Vector<ushort> PopCount(Vector<ushort> value);

    /// svuint32_t svcnt[_u32]_m(svuint32_t inactive, svbool_t pg, svuint32_t op) : "CNT Ztied.S, Pg/M, Zop.S" or "MOVPRFX Zresult, Zinactive; CNT Zresult.S, Pg/M, Zop.S"
    /// svuint32_t svcnt[_u32]_x(svbool_t pg, svuint32_t op) : "CNT Ztied.S, Pg/M, Ztied.S" or "MOVPRFX Zresult, Zop; CNT Zresult.S, Pg/M, Zop.S"
    /// svuint32_t svcnt[_u32]_z(svbool_t pg, svuint32_t op) : "MOVPRFX Zresult.S, Pg/Z, Zop.S; CNT Zresult.S, Pg/M, Zop.S"
  public static unsafe Vector<uint> PopCount(Vector<uint> value);

    /// svuint64_t svcnt[_u64]_m(svuint64_t inactive, svbool_t pg, svuint64_t op) : "CNT Ztied.D, Pg/M, Zop.D" or "MOVPRFX Zresult, Zinactive; CNT Zresult.D, Pg/M, Zop.D"
    /// svuint64_t svcnt[_u64]_x(svbool_t pg, svuint64_t op) : "CNT Ztied.D, Pg/M, Ztied.D" or "MOVPRFX Zresult, Zop; CNT Zresult.D, Pg/M, Zop.D"
    /// svuint64_t svcnt[_u64]_z(svbool_t pg, svuint64_t op) : "MOVPRFX Zresult.D, Pg/Z, Zop.D; CNT Zresult.D, Pg/M, Zop.D"
  public static unsafe Vector<ulong> PopCount(Vector<ulong> value);


    /// SaturatingDecrementBy16BitElementCount : Saturating decrement by number of halfword elements

    /// int32_t svqdech_pat[_n_s32](int32_t op, enum svpattern pattern, uint64_t imm_factor) : "SQDECH Xtied, Wtied, pattern, MUL #imm_factor"
  public static unsafe int SaturatingDecrementBy16BitElementCount(int value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All);

    /// int64_t svqdech_pat[_n_s64](int64_t op, enum svpattern pattern, uint64_t imm_factor) : "SQDECH Xtied, pattern, MUL #imm_factor"
  public static unsafe long SaturatingDecrementBy16BitElementCount(long value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All);

    /// uint32_t svqdech_pat[_n_u32](uint32_t op, enum svpattern pattern, uint64_t imm_factor) : "UQDECH Wtied, pattern, MUL #imm_factor"
  public static unsafe uint SaturatingDecrementBy16BitElementCount(uint value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All);

    /// uint64_t svqdech_pat[_n_u64](uint64_t op, enum svpattern pattern, uint64_t imm_factor) : "UQDECH Xtied, pattern, MUL #imm_factor"
  public static unsafe ulong SaturatingDecrementBy16BitElementCount(ulong value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All);

    /// svint16_t svqdech_pat[_s16](svint16_t op, enum svpattern pattern, uint64_t imm_factor) : "SQDECH Ztied.H, pattern, MUL #imm_factor" or "MOVPRFX Zresult, Zop; SQDECH Zresult.H, pattern, MUL #imm_factor"
  public static unsafe Vector<short> SaturatingDecrementBy16BitElementCount(Vector<short> value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All);

    /// svuint16_t svqdech_pat[_u16](svuint16_t op, enum svpattern pattern, uint64_t imm_factor) : "UQDECH Ztied.H, pattern, MUL #imm_factor" or "MOVPRFX Zresult, Zop; UQDECH Zresult.H, pattern, MUL #imm_factor"
  public static unsafe Vector<ushort> SaturatingDecrementBy16BitElementCount(Vector<ushort> value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All);


    /// SaturatingDecrementBy32BitElementCount : Saturating decrement by number of word elements

    /// int32_t svqdecw_pat[_n_s32](int32_t op, enum svpattern pattern, uint64_t imm_factor) : "SQDECW Xtied, Wtied, pattern, MUL #imm_factor"
  public static unsafe int SaturatingDecrementBy32BitElementCount(int value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All);

    /// int64_t svqdecw_pat[_n_s64](int64_t op, enum svpattern pattern, uint64_t imm_factor) : "SQDECW Xtied, pattern, MUL #imm_factor"
  public static unsafe long SaturatingDecrementBy32BitElementCount(long value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All);

    /// uint32_t svqdecw_pat[_n_u32](uint32_t op, enum svpattern pattern, uint64_t imm_factor) : "UQDECW Wtied, pattern, MUL #imm_factor"
  public static unsafe uint SaturatingDecrementBy32BitElementCount(uint value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All);

    /// uint64_t svqdecw_pat[_n_u64](uint64_t op, enum svpattern pattern, uint64_t imm_factor) : "UQDECW Xtied, pattern, MUL #imm_factor"
  public static unsafe ulong SaturatingDecrementBy32BitElementCount(ulong value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All);

    /// svint32_t svqdecw_pat[_s32](svint32_t op, enum svpattern pattern, uint64_t imm_factor) : "SQDECW Ztied.S, pattern, MUL #imm_factor" or "MOVPRFX Zresult, Zop; SQDECW Zresult.S, pattern, MUL #imm_factor"
  public static unsafe Vector<int> SaturatingDecrementBy32BitElementCount(Vector<int> value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All);

    /// svuint32_t svqdecw_pat[_u32](svuint32_t op, enum svpattern pattern, uint64_t imm_factor) : "UQDECW Ztied.S, pattern, MUL #imm_factor" or "MOVPRFX Zresult, Zop; UQDECW Zresult.S, pattern, MUL #imm_factor"
  public static unsafe Vector<uint> SaturatingDecrementBy32BitElementCount(Vector<uint> value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All);


    /// SaturatingDecrementBy64BitElementCount : Saturating decrement by number of doubleword elements

    /// int32_t svqdecd_pat[_n_s32](int32_t op, enum svpattern pattern, uint64_t imm_factor) : "SQDECD Xtied, Wtied, pattern, MUL #imm_factor"
  public static unsafe int SaturatingDecrementBy64BitElementCount(int value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All);

    /// int64_t svqdecd_pat[_n_s64](int64_t op, enum svpattern pattern, uint64_t imm_factor) : "SQDECD Xtied, pattern, MUL #imm_factor"
  public static unsafe long SaturatingDecrementBy64BitElementCount(long value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All);

    /// uint32_t svqdecd_pat[_n_u32](uint32_t op, enum svpattern pattern, uint64_t imm_factor) : "UQDECD Wtied, pattern, MUL #imm_factor"
  public static unsafe uint SaturatingDecrementBy64BitElementCount(uint value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All);

    /// uint64_t svqdecd_pat[_n_u64](uint64_t op, enum svpattern pattern, uint64_t imm_factor) : "UQDECD Xtied, pattern, MUL #imm_factor"
  public static unsafe ulong SaturatingDecrementBy64BitElementCount(ulong value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All);

    /// svint64_t svqdecd_pat[_s64](svint64_t op, enum svpattern pattern, uint64_t imm_factor) : "SQDECD Ztied.D, pattern, MUL #imm_factor" or "MOVPRFX Zresult, Zop; SQDECD Zresult.D, pattern, MUL #imm_factor"
  public static unsafe Vector<long> SaturatingDecrementBy64BitElementCount(Vector<long> value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All);

    /// svuint64_t svqdecd_pat[_u64](svuint64_t op, enum svpattern pattern, uint64_t imm_factor) : "UQDECD Ztied.D, pattern, MUL #imm_factor" or "MOVPRFX Zresult, Zop; UQDECD Zresult.D, pattern, MUL #imm_factor"
  public static unsafe Vector<ulong> SaturatingDecrementBy64BitElementCount(Vector<ulong> value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All);


    /// SaturatingDecrementBy8BitElementCount : Saturating decrement by number of byte elements

    /// int32_t svqdecb_pat[_n_s32](int32_t op, enum svpattern pattern, uint64_t imm_factor) : "SQDECB Xtied, Wtied, pattern, MUL #imm_factor"
  public static unsafe int SaturatingDecrementBy8BitElementCount(int value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All);

    /// int64_t svqdecb_pat[_n_s64](int64_t op, enum svpattern pattern, uint64_t imm_factor) : "SQDECB Xtied, pattern, MUL #imm_factor"
  public static unsafe long SaturatingDecrementBy8BitElementCount(long value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All);

    /// uint32_t svqdecb_pat[_n_u32](uint32_t op, enum svpattern pattern, uint64_t imm_factor) : "UQDECB Wtied, pattern, MUL #imm_factor"
  public static unsafe uint SaturatingDecrementBy8BitElementCount(uint value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All);

    /// uint64_t svqdecb_pat[_n_u64](uint64_t op, enum svpattern pattern, uint64_t imm_factor) : "UQDECB Xtied, pattern, MUL #imm_factor"
  public static unsafe ulong SaturatingDecrementBy8BitElementCount(ulong value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All);


    /// SaturatingDecrementByActiveElementCount : Saturating decrement by active element count

    /// int32_t svqdecp[_n_s32]_b8(int32_t op, svbool_t pg) : "SQDECP Xtied, Pg.B, Wtied"
  public static unsafe int SaturatingDecrementByActiveElementCount(int value, Vector<byte> from);

    /// int32_t svqdecp[_n_s32]_b16(int32_t op, svbool_t pg) : "SQDECP Xtied, Pg.H, Wtied"
  public static unsafe int SaturatingDecrementByActiveElementCount(int value, Vector<ushort> from);

    /// int32_t svqdecp[_n_s32]_b32(int32_t op, svbool_t pg) : "SQDECP Xtied, Pg.S, Wtied"
  public static unsafe int SaturatingDecrementByActiveElementCount(int value, Vector<uint> from);

    /// int32_t svqdecp[_n_s32]_b64(int32_t op, svbool_t pg) : "SQDECP Xtied, Pg.D, Wtied"
  public static unsafe int SaturatingDecrementByActiveElementCount(int value, Vector<ulong> from);

    /// int64_t svqdecp[_n_s64]_b8(int64_t op, svbool_t pg) : "SQDECP Xtied, Pg.B"
  public static unsafe long SaturatingDecrementByActiveElementCount(long value, Vector<byte> from);

    /// int64_t svqdecp[_n_s64]_b16(int64_t op, svbool_t pg) : "SQDECP Xtied, Pg.H"
  public static unsafe long SaturatingDecrementByActiveElementCount(long value, Vector<ushort> from);

    /// int64_t svqdecp[_n_s64]_b32(int64_t op, svbool_t pg) : "SQDECP Xtied, Pg.S"
  public static unsafe long SaturatingDecrementByActiveElementCount(long value, Vector<uint> from);

    /// int64_t svqdecp[_n_s64]_b64(int64_t op, svbool_t pg) : "SQDECP Xtied, Pg.D"
  public static unsafe long SaturatingDecrementByActiveElementCount(long value, Vector<ulong> from);

    /// uint32_t svqdecp[_n_u32]_b8(uint32_t op, svbool_t pg) : "UQDECP Wtied, Pg.B"
  public static unsafe uint SaturatingDecrementByActiveElementCount(uint value, Vector<byte> from);

    /// uint32_t svqdecp[_n_u32]_b16(uint32_t op, svbool_t pg) : "UQDECP Wtied, Pg.H"
  public static unsafe uint SaturatingDecrementByActiveElementCount(uint value, Vector<ushort> from);

    /// uint32_t svqdecp[_n_u32]_b32(uint32_t op, svbool_t pg) : "UQDECP Wtied, Pg.S"
  public static unsafe uint SaturatingDecrementByActiveElementCount(uint value, Vector<uint> from);

    /// uint32_t svqdecp[_n_u32]_b64(uint32_t op, svbool_t pg) : "UQDECP Wtied, Pg.D"
  public static unsafe uint SaturatingDecrementByActiveElementCount(uint value, Vector<ulong> from);

    /// uint64_t svqdecp[_n_u64]_b8(uint64_t op, svbool_t pg) : "UQDECP Xtied, Pg.B"
  public static unsafe ulong SaturatingDecrementByActiveElementCount(ulong value, Vector<byte> from);

    /// uint64_t svqdecp[_n_u64]_b16(uint64_t op, svbool_t pg) : "UQDECP Xtied, Pg.H"
  public static unsafe ulong SaturatingDecrementByActiveElementCount(ulong value, Vector<ushort> from);

    /// uint64_t svqdecp[_n_u64]_b32(uint64_t op, svbool_t pg) : "UQDECP Xtied, Pg.S"
  public static unsafe ulong SaturatingDecrementByActiveElementCount(ulong value, Vector<uint> from);

    /// uint64_t svqdecp[_n_u64]_b64(uint64_t op, svbool_t pg) : "UQDECP Xtied, Pg.D"
  public static unsafe ulong SaturatingDecrementByActiveElementCount(ulong value, Vector<ulong> from);

    /// svint16_t svqdecp[_s16](svint16_t op, svbool_t pg) : "SQDECP Ztied.H, Pg" or "MOVPRFX Zresult, Zop; SQDECP Zresult.H, Pg"
  public static unsafe Vector<short> SaturatingDecrementByActiveElementCount(Vector<short> value, Vector<short> from);

    /// svint32_t svqdecp[_s32](svint32_t op, svbool_t pg) : "SQDECP Ztied.S, Pg" or "MOVPRFX Zresult, Zop; SQDECP Zresult.S, Pg"
  public static unsafe Vector<int> SaturatingDecrementByActiveElementCount(Vector<int> value, Vector<int> from);

    /// svint64_t svqdecp[_s64](svint64_t op, svbool_t pg) : "SQDECP Ztied.D, Pg" or "MOVPRFX Zresult, Zop; SQDECP Zresult.D, Pg"
  public static unsafe Vector<long> SaturatingDecrementByActiveElementCount(Vector<long> value, Vector<long> from);

    /// svuint16_t svqdecp[_u16](svuint16_t op, svbool_t pg) : "UQDECP Ztied.H, Pg" or "MOVPRFX Zresult, Zop; UQDECP Zresult.H, Pg"
  public static unsafe Vector<ushort> SaturatingDecrementByActiveElementCount(Vector<ushort> value, Vector<ushort> from);

    /// svuint32_t svqdecp[_u32](svuint32_t op, svbool_t pg) : "UQDECP Ztied.S, Pg" or "MOVPRFX Zresult, Zop; UQDECP Zresult.S, Pg"
  public static unsafe Vector<uint> SaturatingDecrementByActiveElementCount(Vector<uint> value, Vector<uint> from);

    /// svuint64_t svqdecp[_u64](svuint64_t op, svbool_t pg) : "UQDECP Ztied.D, Pg" or "MOVPRFX Zresult, Zop; UQDECP Zresult.D, Pg"
  public static unsafe Vector<ulong> SaturatingDecrementByActiveElementCount(Vector<ulong> value, Vector<ulong> from);


    /// SaturatingIncrementBy16BitElementCount : Saturating increment by number of halfword elements

    /// int32_t svqinch_pat[_n_s32](int32_t op, enum svpattern pattern, uint64_t imm_factor) : "SQINCH Xtied, Wtied, pattern, MUL #imm_factor"
  public static unsafe int SaturatingIncrementBy16BitElementCount(int value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All);

    /// int64_t svqinch_pat[_n_s64](int64_t op, enum svpattern pattern, uint64_t imm_factor) : "SQINCH Xtied, pattern, MUL #imm_factor"
  public static unsafe long SaturatingIncrementBy16BitElementCount(long value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All);

    /// uint32_t svqinch_pat[_n_u32](uint32_t op, enum svpattern pattern, uint64_t imm_factor) : "UQINCH Wtied, pattern, MUL #imm_factor"
  public static unsafe uint SaturatingIncrementBy16BitElementCount(uint value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All);

    /// uint64_t svqinch_pat[_n_u64](uint64_t op, enum svpattern pattern, uint64_t imm_factor) : "UQINCH Xtied, pattern, MUL #imm_factor"
  public static unsafe ulong SaturatingIncrementBy16BitElementCount(ulong value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All);

    /// svint16_t svqinch_pat[_s16](svint16_t op, enum svpattern pattern, uint64_t imm_factor) : "SQINCH Ztied.H, pattern, MUL #imm_factor" or "MOVPRFX Zresult, Zop; SQINCH Zresult.H, pattern, MUL #imm_factor"
  public static unsafe Vector<short> SaturatingIncrementBy16BitElementCount(Vector<short> value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All);

    /// svuint16_t svqinch_pat[_u16](svuint16_t op, enum svpattern pattern, uint64_t imm_factor) : "UQINCH Ztied.H, pattern, MUL #imm_factor" or "MOVPRFX Zresult, Zop; UQINCH Zresult.H, pattern, MUL #imm_factor"
  public static unsafe Vector<ushort> SaturatingIncrementBy16BitElementCount(Vector<ushort> value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All);


    /// SaturatingIncrementBy32BitElementCount : Saturating increment by number of word elements

    /// int32_t svqincw_pat[_n_s32](int32_t op, enum svpattern pattern, uint64_t imm_factor) : "SQINCW Xtied, Wtied, pattern, MUL #imm_factor"
  public static unsafe int SaturatingIncrementBy32BitElementCount(int value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All);

    /// int64_t svqincw_pat[_n_s64](int64_t op, enum svpattern pattern, uint64_t imm_factor) : "SQINCW Xtied, pattern, MUL #imm_factor"
  public static unsafe long SaturatingIncrementBy32BitElementCount(long value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All);

    /// uint32_t svqincw_pat[_n_u32](uint32_t op, enum svpattern pattern, uint64_t imm_factor) : "UQINCW Wtied, pattern, MUL #imm_factor"
  public static unsafe uint SaturatingIncrementBy32BitElementCount(uint value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All);

    /// uint64_t svqincw_pat[_n_u64](uint64_t op, enum svpattern pattern, uint64_t imm_factor) : "UQINCW Xtied, pattern, MUL #imm_factor"
  public static unsafe ulong SaturatingIncrementBy32BitElementCount(ulong value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All);

    /// svint32_t svqincw_pat[_s32](svint32_t op, enum svpattern pattern, uint64_t imm_factor) : "SQINCW Ztied.S, pattern, MUL #imm_factor" or "MOVPRFX Zresult, Zop; SQINCW Zresult.S, pattern, MUL #imm_factor"
  public static unsafe Vector<int> SaturatingIncrementBy32BitElementCount(Vector<int> value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All);

    /// svuint32_t svqincw_pat[_u32](svuint32_t op, enum svpattern pattern, uint64_t imm_factor) : "UQINCW Ztied.S, pattern, MUL #imm_factor" or "MOVPRFX Zresult, Zop; UQINCW Zresult.S, pattern, MUL #imm_factor"
  public static unsafe Vector<uint> SaturatingIncrementBy32BitElementCount(Vector<uint> value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All);


    /// SaturatingIncrementBy64BitElementCount : Saturating increment by number of doubleword elements

    /// int32_t svqincd_pat[_n_s32](int32_t op, enum svpattern pattern, uint64_t imm_factor) : "SQINCD Xtied, Wtied, pattern, MUL #imm_factor"
  public static unsafe int SaturatingIncrementBy64BitElementCount(int value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All);

    /// int64_t svqincd_pat[_n_s64](int64_t op, enum svpattern pattern, uint64_t imm_factor) : "SQINCD Xtied, pattern, MUL #imm_factor"
  public static unsafe long SaturatingIncrementBy64BitElementCount(long value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All);

    /// uint32_t svqincd_pat[_n_u32](uint32_t op, enum svpattern pattern, uint64_t imm_factor) : "UQINCD Wtied, pattern, MUL #imm_factor"
  public static unsafe uint SaturatingIncrementBy64BitElementCount(uint value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All);

    /// uint64_t svqincd_pat[_n_u64](uint64_t op, enum svpattern pattern, uint64_t imm_factor) : "UQINCD Xtied, pattern, MUL #imm_factor"
  public static unsafe ulong SaturatingIncrementBy64BitElementCount(ulong value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All);

    /// svint64_t svqincd_pat[_s64](svint64_t op, enum svpattern pattern, uint64_t imm_factor) : "SQINCD Ztied.D, pattern, MUL #imm_factor" or "MOVPRFX Zresult, Zop; SQINCD Zresult.D, pattern, MUL #imm_factor"
  public static unsafe Vector<long> SaturatingIncrementBy64BitElementCount(Vector<long> value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All);

    /// svuint64_t svqincd_pat[_u64](svuint64_t op, enum svpattern pattern, uint64_t imm_factor) : "UQINCD Ztied.D, pattern, MUL #imm_factor" or "MOVPRFX Zresult, Zop; UQINCD Zresult.D, pattern, MUL #imm_factor"
  public static unsafe Vector<ulong> SaturatingIncrementBy64BitElementCount(Vector<ulong> value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All);


    /// SaturatingIncrementBy8BitElementCount : Saturating increment by number of byte elements

    /// int32_t svqincb_pat[_n_s32](int32_t op, enum svpattern pattern, uint64_t imm_factor) : "SQINCB Xtied, Wtied, pattern, MUL #imm_factor"
  public static unsafe int SaturatingIncrementBy8BitElementCount(int value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All);

    /// int64_t svqincb_pat[_n_s64](int64_t op, enum svpattern pattern, uint64_t imm_factor) : "SQINCB Xtied, pattern, MUL #imm_factor"
  public static unsafe long SaturatingIncrementBy8BitElementCount(long value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All);

    /// uint32_t svqincb_pat[_n_u32](uint32_t op, enum svpattern pattern, uint64_t imm_factor) : "UQINCB Wtied, pattern, MUL #imm_factor"
  public static unsafe uint SaturatingIncrementBy8BitElementCount(uint value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All);

    /// uint64_t svqincb_pat[_n_u64](uint64_t op, enum svpattern pattern, uint64_t imm_factor) : "UQINCB Xtied, pattern, MUL #imm_factor"
  public static unsafe ulong SaturatingIncrementBy8BitElementCount(ulong value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All);


    /// SaturatingIncrementByActiveElementCount : Saturating increment by active element count

    /// int32_t svqincp[_n_s32]_b8(int32_t op, svbool_t pg) : "SQINCP Xtied, Pg.B, Wtied"
  public static unsafe int SaturatingIncrementByActiveElementCount(int value, Vector<byte> from);

    /// int32_t svqincp[_n_s32]_b16(int32_t op, svbool_t pg) : "SQINCP Xtied, Pg.H, Wtied"
  public static unsafe int SaturatingIncrementByActiveElementCount(int value, Vector<ushort> from);

    /// int32_t svqincp[_n_s32]_b32(int32_t op, svbool_t pg) : "SQINCP Xtied, Pg.S, Wtied"
  public static unsafe int SaturatingIncrementByActiveElementCount(int value, Vector<uint> from);

    /// int32_t svqincp[_n_s32]_b64(int32_t op, svbool_t pg) : "SQINCP Xtied, Pg.D, Wtied"
  public static unsafe int SaturatingIncrementByActiveElementCount(int value, Vector<ulong> from);

    /// int64_t svqincp[_n_s64]_b8(int64_t op, svbool_t pg) : "SQINCP Xtied, Pg.B"
  public static unsafe long SaturatingIncrementByActiveElementCount(long value, Vector<byte> from);

    /// int64_t svqincp[_n_s64]_b16(int64_t op, svbool_t pg) : "SQINCP Xtied, Pg.H"
  public static unsafe long SaturatingIncrementByActiveElementCount(long value, Vector<ushort> from);

    /// int64_t svqincp[_n_s64]_b32(int64_t op, svbool_t pg) : "SQINCP Xtied, Pg.S"
  public static unsafe long SaturatingIncrementByActiveElementCount(long value, Vector<uint> from);

    /// int64_t svqincp[_n_s64]_b64(int64_t op, svbool_t pg) : "SQINCP Xtied, Pg.D"
  public static unsafe long SaturatingIncrementByActiveElementCount(long value, Vector<ulong> from);

    /// uint32_t svqincp[_n_u32]_b8(uint32_t op, svbool_t pg) : "UQINCP Wtied, Pg.B"
  public static unsafe uint SaturatingIncrementByActiveElementCount(uint value, Vector<byte> from);

    /// uint32_t svqincp[_n_u32]_b16(uint32_t op, svbool_t pg) : "UQINCP Wtied, Pg.H"
  public static unsafe uint SaturatingIncrementByActiveElementCount(uint value, Vector<ushort> from);

    /// uint32_t svqincp[_n_u32]_b32(uint32_t op, svbool_t pg) : "UQINCP Wtied, Pg.S"
  public static unsafe uint SaturatingIncrementByActiveElementCount(uint value, Vector<uint> from);

    /// uint32_t svqincp[_n_u32]_b64(uint32_t op, svbool_t pg) : "UQINCP Wtied, Pg.D"
  public static unsafe uint SaturatingIncrementByActiveElementCount(uint value, Vector<ulong> from);

    /// uint64_t svqincp[_n_u64]_b8(uint64_t op, svbool_t pg) : "UQINCP Xtied, Pg.B"
  public static unsafe ulong SaturatingIncrementByActiveElementCount(ulong value, Vector<byte> from);

    /// uint64_t svqincp[_n_u64]_b16(uint64_t op, svbool_t pg) : "UQINCP Xtied, Pg.H"
  public static unsafe ulong SaturatingIncrementByActiveElementCount(ulong value, Vector<ushort> from);

    /// uint64_t svqincp[_n_u64]_b32(uint64_t op, svbool_t pg) : "UQINCP Xtied, Pg.S"
  public static unsafe ulong SaturatingIncrementByActiveElementCount(ulong value, Vector<uint> from);

    /// uint64_t svqincp[_n_u64]_b64(uint64_t op, svbool_t pg) : "UQINCP Xtied, Pg.D"
  public static unsafe ulong SaturatingIncrementByActiveElementCount(ulong value, Vector<ulong> from);

    /// svint16_t svqincp[_s16](svint16_t op, svbool_t pg) : "SQINCP Ztied.H, Pg" or "MOVPRFX Zresult, Zop; SQINCP Zresult.H, Pg"
  public static unsafe Vector<short> SaturatingIncrementByActiveElementCount(Vector<short> value, Vector<short> from);

    /// svint32_t svqincp[_s32](svint32_t op, svbool_t pg) : "SQINCP Ztied.S, Pg" or "MOVPRFX Zresult, Zop; SQINCP Zresult.S, Pg"
  public static unsafe Vector<int> SaturatingIncrementByActiveElementCount(Vector<int> value, Vector<int> from);

    /// svint64_t svqincp[_s64](svint64_t op, svbool_t pg) : "SQINCP Ztied.D, Pg" or "MOVPRFX Zresult, Zop; SQINCP Zresult.D, Pg"
  public static unsafe Vector<long> SaturatingIncrementByActiveElementCount(Vector<long> value, Vector<long> from);

    /// svuint16_t svqincp[_u16](svuint16_t op, svbool_t pg) : "UQINCP Ztied.H, Pg" or "MOVPRFX Zresult, Zop; UQINCP Zresult.H, Pg"
  public static unsafe Vector<ushort> SaturatingIncrementByActiveElementCount(Vector<ushort> value, Vector<ushort> from);

    /// svuint32_t svqincp[_u32](svuint32_t op, svbool_t pg) : "UQINCP Ztied.S, Pg" or "MOVPRFX Zresult, Zop; UQINCP Zresult.S, Pg"
  public static unsafe Vector<uint> SaturatingIncrementByActiveElementCount(Vector<uint> value, Vector<uint> from);

    /// svuint64_t svqincp[_u64](svuint64_t op, svbool_t pg) : "UQINCP Ztied.D, Pg" or "MOVPRFX Zresult, Zop; UQINCP Zresult.D, Pg"
  public static unsafe Vector<ulong> SaturatingIncrementByActiveElementCount(Vector<ulong> value, Vector<ulong> from);


  /// total method signatures: 124
  /// total method names:      19
}


  /// Rejected:
  ///   public static unsafe ulong Count16BitElements(); // svcnth
  ///   public static unsafe ulong Count32BitElements(); // svcntw
  ///   public static unsafe ulong Count64BitElements(); // svcntd
  ///   public static unsafe ulong Count8BitElements(); // svcntb
  ///   public static unsafe ulong CountElementsInAFullVector(Vector<float> value); // svlen[_f32]
  ///   public static unsafe ulong CountElementsInAFullVector(Vector<double> value); // svlen[_f64]
  ///   public static unsafe ulong CountElementsInAFullVector(Vector<sbyte> value); // svlen[_s8]
  ///   public static unsafe ulong CountElementsInAFullVector(Vector<short> value); // svlen[_s16]
  ///   public static unsafe ulong CountElementsInAFullVector(Vector<int> value); // svlen[_s32]
  ///   public static unsafe ulong CountElementsInAFullVector(Vector<long> value); // svlen[_s64]
  ///   public static unsafe ulong CountElementsInAFullVector(Vector<byte> value); // svlen[_u8]
  ///   public static unsafe ulong CountElementsInAFullVector(Vector<ushort> value); // svlen[_u16]
  ///   public static unsafe ulong CountElementsInAFullVector(Vector<uint> value); // svlen[_u32]
  ///   public static unsafe ulong CountElementsInAFullVector(Vector<ulong> value); // svlen[_u64]
  ///   public static unsafe int SaturatingDecrementBy16BitElementCount(int value, ulong from); // svqdech[_n_s32]
  ///   public static unsafe long SaturatingDecrementBy16BitElementCount(long value, ulong from); // svqdech[_n_s64]
  ///   public static unsafe uint SaturatingDecrementBy16BitElementCount(uint value, ulong from); // svqdech[_n_u32]
  ///   public static unsafe ulong SaturatingDecrementBy16BitElementCount(ulong value, ulong from); // svqdech[_n_u64]
  ///   public static unsafe Vector<short> SaturatingDecrementBy16BitElementCount(Vector<short> value, ulong from); // svqdech[_s16]
  ///   public static unsafe Vector<ushort> SaturatingDecrementBy16BitElementCount(Vector<ushort> value, ulong from); // svqdech[_u16]
  ///   public static unsafe int SaturatingDecrementBy32BitElementCount(int value, ulong from); // svqdecw[_n_s32]
  ///   public static unsafe long SaturatingDecrementBy32BitElementCount(long value, ulong from); // svqdecw[_n_s64]
  ///   public static unsafe uint SaturatingDecrementBy32BitElementCount(uint value, ulong from); // svqdecw[_n_u32]
  ///   public static unsafe ulong SaturatingDecrementBy32BitElementCount(ulong value, ulong from); // svqdecw[_n_u64]
  ///   public static unsafe Vector<int> SaturatingDecrementBy32BitElementCount(Vector<int> value, ulong from); // svqdecw[_s32]
  ///   public static unsafe Vector<uint> SaturatingDecrementBy32BitElementCount(Vector<uint> value, ulong from); // svqdecw[_u32]
  ///   public static unsafe int SaturatingDecrementBy64BitElementCount(int value, ulong from); // svqdecd[_n_s32]
  ///   public static unsafe long SaturatingDecrementBy64BitElementCount(long value, ulong from); // svqdecd[_n_s64]
  ///   public static unsafe uint SaturatingDecrementBy64BitElementCount(uint value, ulong from); // svqdecd[_n_u32]
  ///   public static unsafe ulong SaturatingDecrementBy64BitElementCount(ulong value, ulong from); // svqdecd[_n_u64]
  ///   public static unsafe Vector<long> SaturatingDecrementBy64BitElementCount(Vector<long> value, ulong from); // svqdecd[_s64]
  ///   public static unsafe Vector<ulong> SaturatingDecrementBy64BitElementCount(Vector<ulong> value, ulong from); // svqdecd[_u64]
  ///   public static unsafe int SaturatingDecrementBy8BitElementCount(int value, ulong from); // svqdecb[_n_s32]
  ///   public static unsafe long SaturatingDecrementBy8BitElementCount(long value, ulong from); // svqdecb[_n_s64]
  ///   public static unsafe uint SaturatingDecrementBy8BitElementCount(uint value, ulong from); // svqdecb[_n_u32]
  ///   public static unsafe ulong SaturatingDecrementBy8BitElementCount(ulong value, ulong from); // svqdecb[_n_u64]
  ///   public static unsafe int SaturatingIncrementBy16BitElementCount(int value, ulong from); // svqinch[_n_s32]
  ///   public static unsafe long SaturatingIncrementBy16BitElementCount(long value, ulong from); // svqinch[_n_s64]
  ///   public static unsafe uint SaturatingIncrementBy16BitElementCount(uint value, ulong from); // svqinch[_n_u32]
  ///   public static unsafe ulong SaturatingIncrementBy16BitElementCount(ulong value, ulong from); // svqinch[_n_u64]
  ///   public static unsafe Vector<short> SaturatingIncrementBy16BitElementCount(Vector<short> value, ulong from); // svqinch[_s16]
  ///   public static unsafe Vector<ushort> SaturatingIncrementBy16BitElementCount(Vector<ushort> value, ulong from); // svqinch[_u16]
  ///   public static unsafe int SaturatingIncrementBy32BitElementCount(int value, ulong from); // svqincw[_n_s32]
  ///   public static unsafe long SaturatingIncrementBy32BitElementCount(long value, ulong from); // svqincw[_n_s64]
  ///   public static unsafe uint SaturatingIncrementBy32BitElementCount(uint value, ulong from); // svqincw[_n_u32]
  ///   public static unsafe ulong SaturatingIncrementBy32BitElementCount(ulong value, ulong from); // svqincw[_n_u64]
  ///   public static unsafe Vector<int> SaturatingIncrementBy32BitElementCount(Vector<int> value, ulong from); // svqincw[_s32]
  ///   public static unsafe Vector<uint> SaturatingIncrementBy32BitElementCount(Vector<uint> value, ulong from); // svqincw[_u32]
  ///   public static unsafe int SaturatingIncrementBy64BitElementCount(int value, ulong from); // svqincd[_n_s32]
  ///   public static unsafe long SaturatingIncrementBy64BitElementCount(long value, ulong from); // svqincd[_n_s64]
  ///   public static unsafe uint SaturatingIncrementBy64BitElementCount(uint value, ulong from); // svqincd[_n_u32]
  ///   public static unsafe ulong SaturatingIncrementBy64BitElementCount(ulong value, ulong from); // svqincd[_n_u64]
  ///   public static unsafe Vector<long> SaturatingIncrementBy64BitElementCount(Vector<long> value, ulong from); // svqincd[_s64]
  ///   public static unsafe Vector<ulong> SaturatingIncrementBy64BitElementCount(Vector<ulong> value, ulong from); // svqincd[_u64]
  ///   public static unsafe int SaturatingIncrementBy8BitElementCount(int value, ulong from); // svqincb[_n_s32]
  ///   public static unsafe long SaturatingIncrementBy8BitElementCount(long value, ulong from); // svqincb[_n_s64]
  ///   public static unsafe uint SaturatingIncrementBy8BitElementCount(uint value, ulong from); // svqincb[_n_u32]
  ///   public static unsafe ulong SaturatingIncrementBy8BitElementCount(ulong value, ulong from); // svqincb[_n_u64]
  ///   Total Rejected: 58

  /// Total ACLE covered across API:      226

