namespace System.Runtime.Intrinsics.Arm;

/// VectorT Summary
public abstract partial class Sve : AdvSimd /// Feature: FEAT_SVE  Category: mask
{

  /// T: float, double
  public static unsafe Vector<T> AbsoluteCompareGreaterThan(Vector<T> left, Vector<T> right); // FACGT // predicated

  /// T: float, double
  public static unsafe Vector<T> AbsoluteCompareGreaterThanOrEqual(Vector<T> left, Vector<T> right); // FACGE // predicated

  /// T: float, double
  public static unsafe Vector<T> AbsoluteCompareLessThan(Vector<T> left, Vector<T> right); // FACGT // predicated

  /// T: float, double
  public static unsafe Vector<T> AbsoluteCompareLessThanOrEqual(Vector<T> left, Vector<T> right); // FACGE // predicated

  /// T: float, double, int, long, uint, ulong
  public static unsafe Vector<T> Compact(Vector<T> mask, Vector<T> value); // COMPACT

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> CompareEqual(Vector<T> left, Vector<T> right); // FCMEQ or CMPEQ // predicated

  /// T: [sbyte, long], [short, long], [int, long]
  public static unsafe Vector<T> CompareEqual(Vector<T> left, Vector<T2> right); // CMPEQ // predicated

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> CompareGreaterThan(Vector<T> left, Vector<T> right); // FCMGT or CMPGT or CMPHI // predicated

  /// T: [sbyte, long], [short, long], [int, long], [byte, ulong], [ushort, ulong], [uint, ulong]
  public static unsafe Vector<T> CompareGreaterThan(Vector<T> left, Vector<T2> right); // CMPGT or CMPHI // predicated

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> CompareGreaterThanOrEqual(Vector<T> left, Vector<T> right); // FCMGE or CMPGE or CMPHS // predicated

  /// T: [sbyte, long], [short, long], [int, long], [byte, ulong], [ushort, ulong], [uint, ulong]
  public static unsafe Vector<T> CompareGreaterThanOrEqual(Vector<T> left, Vector<T2> right); // CMPGE or CMPHS // predicated

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> CompareLessThan(Vector<T> left, Vector<T> right); // FCMGT or CMPGT or CMPHI // predicated

  /// T: [sbyte, long], [short, long], [int, long], [byte, ulong], [ushort, ulong], [uint, ulong]
  public static unsafe Vector<T> CompareLessThan(Vector<T> left, Vector<T2> right); // CMPLT or CMPLO // predicated

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> CompareLessThanOrEqual(Vector<T> left, Vector<T> right); // FCMGE or CMPGE or CMPHS // predicated

  /// T: [sbyte, long], [short, long], [int, long], [byte, ulong], [ushort, ulong], [uint, ulong]
  public static unsafe Vector<T> CompareLessThanOrEqual(Vector<T> left, Vector<T2> right); // CMPLE or CMPLS // predicated

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> CompareNotEqualTo(Vector<T> left, Vector<T> right); // FCMNE or CMPNE // predicated

  /// T: [sbyte, long], [short, long], [int, long]
  public static unsafe Vector<T> CompareNotEqualTo(Vector<T> left, Vector<T2> right); // CMPNE // predicated

  /// T: float, double
  public static unsafe Vector<T> CompareUnordered(Vector<T> left, Vector<T> right); // FCMUO // predicated

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> ConditionalExtractAfterLastActiveElement(Vector<T> mask, Vector<T> defaultValue, Vector<T> data); // CLASTA // MOVPRFX

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe T ConditionalExtractAfterLastActiveElement(Vector<T> mask, T defaultValues, Vector<T> data); // CLASTA // MOVPRFX

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> ConditionalExtractAfterLastActiveElementAndReplicate(Vector<T> mask, Vector<T> defaultScalar, Vector<T> data); // CLASTA // MOVPRFX

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> ConditionalExtractLastActiveElement(Vector<T> mask, Vector<T> defaultValue, Vector<T> data); // CLASTB // MOVPRFX

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe T ConditionalExtractLastActiveElement(Vector<T> mask, T defaultValues, Vector<T> data); // CLASTB // MOVPRFX

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> ConditionalExtractLastActiveElementAndReplicate(Vector<T> mask, Vector<T> fallback, Vector<T> data); // CLASTB // MOVPRFX

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> ConditionalSelect(Vector<T> mask, Vector<T> left, Vector<T> right); // SEL

  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> CreateBreakAfterMask(Vector<T> totalMask, Vector<T> fromMask); // BRKA // predicated

  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> CreateBreakAfterPropagateMask(Vector<T> mask, Vector<T> left, Vector<T> right); // BRKPA

  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> CreateBreakBeforeMask(Vector<T> totalMask, Vector<T> fromMask); // BRKB // predicated

  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> CreateBreakBeforePropagateMask(Vector<T> mask, Vector<T> left, Vector<T> right); // BRKPB

  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> CreateBreakPropagateMask(Vector<T> totalMask, Vector<T> fromMask); // BRKN // predicated

  public static unsafe Vector<byte> CreateFalseMaskByte(); // PFALSE

  public static unsafe Vector<double> CreateFalseMaskDouble(); // PFALSE

  public static unsafe Vector<short> CreateFalseMaskInt16(); // PFALSE

  public static unsafe Vector<int> CreateFalseMaskInt32(); // PFALSE

  public static unsafe Vector<long> CreateFalseMaskInt64(); // PFALSE

  public static unsafe Vector<sbyte> CreateFalseMaskSByte(); // PFALSE

  public static unsafe Vector<float> CreateFalseMaskSingle(); // PFALSE

  public static unsafe Vector<ushort> CreateFalseMaskUInt16(); // PFALSE

  public static unsafe Vector<uint> CreateFalseMaskUInt32(); // PFALSE

  public static unsafe Vector<ulong> CreateFalseMaskUInt64(); // PFALSE

  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> CreateMaskForFirstActiveElement(Vector<T> totalMask, Vector<T> fromMask); // PFIRST

  /// T: byte, ushort, uint, ulong
  public static unsafe Vector<T> CreateMaskForNextActiveElement(Vector<T> totalMask, Vector<T> fromMask); // PNEXT

  public static unsafe Vector<byte> CreateTrueMaskByte([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // PTRUE

  public static unsafe Vector<double> CreateTrueMaskDouble([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // PTRUE

  public static unsafe Vector<short> CreateTrueMaskInt16([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // PTRUE

  public static unsafe Vector<int> CreateTrueMaskInt32([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // PTRUE

  public static unsafe Vector<long> CreateTrueMaskInt64([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // PTRUE

  public static unsafe Vector<sbyte> CreateTrueMaskSByte([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // PTRUE

  public static unsafe Vector<float> CreateTrueMaskSingle([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // PTRUE

  public static unsafe Vector<ushort> CreateTrueMaskUInt16([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // PTRUE

  public static unsafe Vector<uint> CreateTrueMaskUInt32([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // PTRUE

  public static unsafe Vector<ulong> CreateTrueMaskUInt64([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // PTRUE

  public static unsafe Vector<ushort> CreateWhileLessThanMask16Bit(int left, int right); // WHILELT

  public static unsafe Vector<ushort> CreateWhileLessThanMask16Bit(long left, long right); // WHILELT

  public static unsafe Vector<ushort> CreateWhileLessThanMask16Bit(uint left, uint right); // WHILELO

  public static unsafe Vector<ushort> CreateWhileLessThanMask16Bit(ulong left, ulong right); // WHILELO

  public static unsafe Vector<uint> CreateWhileLessThanMask32Bit(int left, int right); // WHILELT

  public static unsafe Vector<uint> CreateWhileLessThanMask32Bit(long left, long right); // WHILELT

  public static unsafe Vector<uint> CreateWhileLessThanMask32Bit(uint left, uint right); // WHILELO

  public static unsafe Vector<uint> CreateWhileLessThanMask32Bit(ulong left, ulong right); // WHILELO

  public static unsafe Vector<ulong> CreateWhileLessThanMask64Bit(int left, int right); // WHILELT

  public static unsafe Vector<ulong> CreateWhileLessThanMask64Bit(long left, long right); // WHILELT

  public static unsafe Vector<ulong> CreateWhileLessThanMask64Bit(uint left, uint right); // WHILELO

  public static unsafe Vector<ulong> CreateWhileLessThanMask64Bit(ulong left, ulong right); // WHILELO

  public static unsafe Vector<byte> CreateWhileLessThanMask8Bit(int left, int right); // WHILELT

  public static unsafe Vector<byte> CreateWhileLessThanMask8Bit(long left, long right); // WHILELT

  public static unsafe Vector<byte> CreateWhileLessThanMask8Bit(uint left, uint right); // WHILELO

  public static unsafe Vector<byte> CreateWhileLessThanMask8Bit(ulong left, ulong right); // WHILELO

  public static unsafe Vector<ushort> CreateWhileLessThanOrEqualMask16Bit(int left, int right); // WHILELE

  public static unsafe Vector<ushort> CreateWhileLessThanOrEqualMask16Bit(long left, long right); // WHILELE

  public static unsafe Vector<ushort> CreateWhileLessThanOrEqualMask16Bit(uint left, uint right); // WHILELS

  public static unsafe Vector<ushort> CreateWhileLessThanOrEqualMask16Bit(ulong left, ulong right); // WHILELS

  public static unsafe Vector<uint> CreateWhileLessThanOrEqualMask32Bit(int left, int right); // WHILELE

  public static unsafe Vector<uint> CreateWhileLessThanOrEqualMask32Bit(long left, long right); // WHILELE

  public static unsafe Vector<uint> CreateWhileLessThanOrEqualMask32Bit(uint left, uint right); // WHILELS

  public static unsafe Vector<uint> CreateWhileLessThanOrEqualMask32Bit(ulong left, ulong right); // WHILELS

  public static unsafe Vector<ulong> CreateWhileLessThanOrEqualMask64Bit(int left, int right); // WHILELE

  public static unsafe Vector<ulong> CreateWhileLessThanOrEqualMask64Bit(long left, long right); // WHILELE

  public static unsafe Vector<ulong> CreateWhileLessThanOrEqualMask64Bit(uint left, uint right); // WHILELS

  public static unsafe Vector<ulong> CreateWhileLessThanOrEqualMask64Bit(ulong left, ulong right); // WHILELS

  public static unsafe Vector<byte> CreateWhileLessThanOrEqualMask8Bit(int left, int right); // WHILELE

  public static unsafe Vector<byte> CreateWhileLessThanOrEqualMask8Bit(long left, long right); // WHILELE

  public static unsafe Vector<byte> CreateWhileLessThanOrEqualMask8Bit(uint left, uint right); // WHILELS

  public static unsafe Vector<byte> CreateWhileLessThanOrEqualMask8Bit(ulong left, ulong right); // WHILELS

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe T ExtractAfterLastScalar(Vector<T> value); // LASTA // predicated

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> ExtractAfterLastVector(Vector<T> value); // LASTA // predicated

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe T ExtractLastScalar(Vector<T> value); // LASTB // predicated

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> ExtractLastVector(Vector<T> value); // LASTB // predicated

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> ExtractVector(Vector<T> upper, Vector<T> lower, [ConstantExpected] byte index); // EXT // MOVPRFX

  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe bool TestAnyTrue(Vector<T> leftMask, Vector<T> rightMask); // PTEST

  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe bool TestFirstTrue(Vector<T> leftMask, Vector<T> rightMask); // PTEST

  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe bool TestLastTrue(Vector<T> leftMask, Vector<T> rightMask); // PTEST


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

  /// total method signatures: 92


  /// Optional Entries:

  /// T: float, double
  public static unsafe Vector<T> AbsoluteCompareGreaterThan(Vector<T> left, T right); // FACGT // predicated

  /// T: float, double
  public static unsafe Vector<T> AbsoluteCompareGreaterThanOrEqual(Vector<T> left, T right); // FACGE // predicated

  /// T: float, double
  public static unsafe Vector<T> AbsoluteCompareLessThan(Vector<T> left, T right); // FACGT // predicated

  /// T: float, double
  public static unsafe Vector<T> AbsoluteCompareLessThanOrEqual(Vector<T> left, T right); // FACGE // predicated

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> CompareEqual(Vector<T> left, T right); // FCMEQ or CMPEQ // predicated

  /// T: sbyte, short, int
  public static unsafe Vector<T> CompareEqual(Vector<T> left, long right); // CMPEQ // predicated

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> CompareGreaterThan(Vector<T> left, T right); // FCMGT or CMPGT or CMPHI // predicated

  /// T: sbyte, short, int
  public static unsafe Vector<T> CompareGreaterThan(Vector<T> left, long right); // CMPGT // predicated

  /// T: byte, ushort, uint
  public static unsafe Vector<T> CompareGreaterThan(Vector<T> left, ulong right); // CMPHI // predicated

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> CompareGreaterThanOrEqual(Vector<T> left, T right); // FCMGE or CMPGE or CMPHS // predicated

  /// T: sbyte, short, int
  public static unsafe Vector<T> CompareGreaterThanOrEqual(Vector<T> left, long right); // CMPGE // predicated

  /// T: byte, ushort, uint
  public static unsafe Vector<T> CompareGreaterThanOrEqual(Vector<T> left, ulong right); // CMPHS // predicated

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> CompareLessThan(Vector<T> left, T right); // FCMLT or FCMGT or CMPLT or CMPGT or CMPLO or CMPHI // predicated

  /// T: sbyte, short, int
  public static unsafe Vector<T> CompareLessThan(Vector<T> left, long right); // CMPLT // predicated

  /// T: byte, ushort, uint
  public static unsafe Vector<T> CompareLessThan(Vector<T> left, ulong right); // CMPLO // predicated

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> CompareLessThanOrEqual(Vector<T> left, T right); // FCMLE or FCMGE or CMPLE or CMPGE or CMPLS or CMPHS // predicated

  /// T: sbyte, short, int
  public static unsafe Vector<T> CompareLessThanOrEqual(Vector<T> left, long right); // CMPLE // predicated

  /// T: byte, ushort, uint
  public static unsafe Vector<T> CompareLessThanOrEqual(Vector<T> left, ulong right); // CMPLS // predicated

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> CompareNotEqualTo(Vector<T> left, T right); // FCMNE or CMPNE // predicated

  /// T: sbyte, short, int
  public static unsafe Vector<T> CompareNotEqualTo(Vector<T> left, long right); // CMPNE // predicated

  /// T: float, double
  public static unsafe Vector<T> CompareUnordered(Vector<T> left, T right); // FCMUO // predicated

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe T ConditionalExtractAfterLastActiveElement(Vector<T> mask, T defaultValue, Vector<T> data); // CLASTA

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe T ConditionalExtractAfterLastActiveElementAndReplicate(Vector<T> mask, T defaultScalar, Vector<T> data); // CLASTA

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe T ConditionalExtractLastActiveElement(Vector<T> mask, T defaultValue, Vector<T> data); // CLASTB

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe T ConditionalExtractLastActiveElementAndReplicate(Vector<T> mask, T fallback, Vector<T> data); // CLASTB

  /// total optional method signatures: 25

}


/// Full API
public abstract partial class Sve : AdvSimd /// Feature: FEAT_SVE  Category: mask
{
    /// AbsoluteCompareGreaterThan : Absolute compare greater than

    /// svbool_t svacgt[_f32](svbool_t pg, svfloat32_t op1, svfloat32_t op2) : "FACGT Presult.S, Pg/Z, Zop1.S, Zop2.S"
  public static unsafe Vector<float> AbsoluteCompareGreaterThan(Vector<float> left, Vector<float> right);

    /// svbool_t svacgt[_f64](svbool_t pg, svfloat64_t op1, svfloat64_t op2) : "FACGT Presult.D, Pg/Z, Zop1.D, Zop2.D"
  public static unsafe Vector<double> AbsoluteCompareGreaterThan(Vector<double> left, Vector<double> right);


    /// AbsoluteCompareGreaterThanOrEqual : Absolute compare greater than or equal to

    /// svbool_t svacge[_f32](svbool_t pg, svfloat32_t op1, svfloat32_t op2) : "FACGE Presult.S, Pg/Z, Zop1.S, Zop2.S"
  public static unsafe Vector<float> AbsoluteCompareGreaterThanOrEqual(Vector<float> left, Vector<float> right);

    /// svbool_t svacge[_f64](svbool_t pg, svfloat64_t op1, svfloat64_t op2) : "FACGE Presult.D, Pg/Z, Zop1.D, Zop2.D"
  public static unsafe Vector<double> AbsoluteCompareGreaterThanOrEqual(Vector<double> left, Vector<double> right);


    /// AbsoluteCompareLessThan : Absolute compare less than

    /// svbool_t svaclt[_f32](svbool_t pg, svfloat32_t op1, svfloat32_t op2) : "FACGT Presult.S, Pg/Z, Zop2.S, Zop1.S"
  public static unsafe Vector<float> AbsoluteCompareLessThan(Vector<float> left, Vector<float> right);

    /// svbool_t svaclt[_f64](svbool_t pg, svfloat64_t op1, svfloat64_t op2) : "FACGT Presult.D, Pg/Z, Zop2.D, Zop1.D"
  public static unsafe Vector<double> AbsoluteCompareLessThan(Vector<double> left, Vector<double> right);


    /// AbsoluteCompareLessThanOrEqual : Absolute compare less than or equal to

    /// svbool_t svacle[_f32](svbool_t pg, svfloat32_t op1, svfloat32_t op2) : "FACGE Presult.S, Pg/Z, Zop2.S, Zop1.S"
  public static unsafe Vector<float> AbsoluteCompareLessThanOrEqual(Vector<float> left, Vector<float> right);

    /// svbool_t svacle[_f64](svbool_t pg, svfloat64_t op1, svfloat64_t op2) : "FACGE Presult.D, Pg/Z, Zop2.D, Zop1.D"
  public static unsafe Vector<double> AbsoluteCompareLessThanOrEqual(Vector<double> left, Vector<double> right);


    /// Compact : Shuffle active elements of vector to the right and fill with zero

    /// svfloat32_t svcompact[_f32](svbool_t pg, svfloat32_t op) : "COMPACT Zresult.S, Pg, Zop.S"
  public static unsafe Vector<float> Compact(Vector<float> mask, Vector<float> value);

    /// svfloat64_t svcompact[_f64](svbool_t pg, svfloat64_t op) : "COMPACT Zresult.D, Pg, Zop.D"
  public static unsafe Vector<double> Compact(Vector<double> mask, Vector<double> value);

    /// svint32_t svcompact[_s32](svbool_t pg, svint32_t op) : "COMPACT Zresult.S, Pg, Zop.S"
  public static unsafe Vector<int> Compact(Vector<int> mask, Vector<int> value);

    /// svint64_t svcompact[_s64](svbool_t pg, svint64_t op) : "COMPACT Zresult.D, Pg, Zop.D"
  public static unsafe Vector<long> Compact(Vector<long> mask, Vector<long> value);

    /// svuint32_t svcompact[_u32](svbool_t pg, svuint32_t op) : "COMPACT Zresult.S, Pg, Zop.S"
  public static unsafe Vector<uint> Compact(Vector<uint> mask, Vector<uint> value);

    /// svuint64_t svcompact[_u64](svbool_t pg, svuint64_t op) : "COMPACT Zresult.D, Pg, Zop.D"
  public static unsafe Vector<ulong> Compact(Vector<ulong> mask, Vector<ulong> value);


    /// CompareEqual : Compare equal to

    /// svbool_t svcmpeq[_f32](svbool_t pg, svfloat32_t op1, svfloat32_t op2) : "FCMEQ Presult.S, Pg/Z, Zop1.S, Zop2.S"
  public static unsafe Vector<float> CompareEqual(Vector<float> left, Vector<float> right);

    /// svbool_t svcmpeq[_f64](svbool_t pg, svfloat64_t op1, svfloat64_t op2) : "FCMEQ Presult.D, Pg/Z, Zop1.D, Zop2.D"
  public static unsafe Vector<double> CompareEqual(Vector<double> left, Vector<double> right);

    /// svbool_t svcmpeq[_s8](svbool_t pg, svint8_t op1, svint8_t op2) : "CMPEQ Presult.B, Pg/Z, Zop1.B, Zop2.B"
  public static unsafe Vector<sbyte> CompareEqual(Vector<sbyte> left, Vector<sbyte> right);

    /// svbool_t svcmpeq[_s16](svbool_t pg, svint16_t op1, svint16_t op2) : "CMPEQ Presult.H, Pg/Z, Zop1.H, Zop2.H"
  public static unsafe Vector<short> CompareEqual(Vector<short> left, Vector<short> right);

    /// svbool_t svcmpeq[_s32](svbool_t pg, svint32_t op1, svint32_t op2) : "CMPEQ Presult.S, Pg/Z, Zop1.S, Zop2.S"
  public static unsafe Vector<int> CompareEqual(Vector<int> left, Vector<int> right);

    /// svbool_t svcmpeq[_s64](svbool_t pg, svint64_t op1, svint64_t op2) : "CMPEQ Presult.D, Pg/Z, Zop1.D, Zop2.D"
  public static unsafe Vector<long> CompareEqual(Vector<long> left, Vector<long> right);

    /// svbool_t svcmpeq[_u8](svbool_t pg, svuint8_t op1, svuint8_t op2) : "CMPEQ Presult.B, Pg/Z, Zop1.B, Zop2.B"
  public static unsafe Vector<byte> CompareEqual(Vector<byte> left, Vector<byte> right);

    /// svbool_t svcmpeq[_u16](svbool_t pg, svuint16_t op1, svuint16_t op2) : "CMPEQ Presult.H, Pg/Z, Zop1.H, Zop2.H"
  public static unsafe Vector<ushort> CompareEqual(Vector<ushort> left, Vector<ushort> right);

    /// svbool_t svcmpeq[_u32](svbool_t pg, svuint32_t op1, svuint32_t op2) : "CMPEQ Presult.S, Pg/Z, Zop1.S, Zop2.S"
  public static unsafe Vector<uint> CompareEqual(Vector<uint> left, Vector<uint> right);

    /// svbool_t svcmpeq[_u64](svbool_t pg, svuint64_t op1, svuint64_t op2) : "CMPEQ Presult.D, Pg/Z, Zop1.D, Zop2.D"
  public static unsafe Vector<ulong> CompareEqual(Vector<ulong> left, Vector<ulong> right);

    /// svbool_t svcmpeq_wide[_s8](svbool_t pg, svint8_t op1, svint64_t op2) : "CMPEQ Presult.B, Pg/Z, Zop1.B, Zop2.D"
  public static unsafe Vector<sbyte> CompareEqual(Vector<sbyte> left, Vector<long> right);

    /// svbool_t svcmpeq_wide[_s16](svbool_t pg, svint16_t op1, svint64_t op2) : "CMPEQ Presult.H, Pg/Z, Zop1.H, Zop2.D"
  public static unsafe Vector<short> CompareEqual(Vector<short> left, Vector<long> right);

    /// svbool_t svcmpeq_wide[_s32](svbool_t pg, svint32_t op1, svint64_t op2) : "CMPEQ Presult.S, Pg/Z, Zop1.S, Zop2.D"
  public static unsafe Vector<int> CompareEqual(Vector<int> left, Vector<long> right);


    /// CompareGreaterThan : Compare greater than

    /// svbool_t svcmpgt[_f32](svbool_t pg, svfloat32_t op1, svfloat32_t op2) : "FCMGT Presult.S, Pg/Z, Zop1.S, Zop2.S"
  public static unsafe Vector<float> CompareGreaterThan(Vector<float> left, Vector<float> right);

    /// svbool_t svcmpgt[_f64](svbool_t pg, svfloat64_t op1, svfloat64_t op2) : "FCMGT Presult.D, Pg/Z, Zop1.D, Zop2.D"
  public static unsafe Vector<double> CompareGreaterThan(Vector<double> left, Vector<double> right);

    /// svbool_t svcmpgt[_s8](svbool_t pg, svint8_t op1, svint8_t op2) : "CMPGT Presult.B, Pg/Z, Zop1.B, Zop2.B"
  public static unsafe Vector<sbyte> CompareGreaterThan(Vector<sbyte> left, Vector<sbyte> right);

    /// svbool_t svcmpgt[_s16](svbool_t pg, svint16_t op1, svint16_t op2) : "CMPGT Presult.H, Pg/Z, Zop1.H, Zop2.H"
  public static unsafe Vector<short> CompareGreaterThan(Vector<short> left, Vector<short> right);

    /// svbool_t svcmpgt[_s32](svbool_t pg, svint32_t op1, svint32_t op2) : "CMPGT Presult.S, Pg/Z, Zop1.S, Zop2.S"
  public static unsafe Vector<int> CompareGreaterThan(Vector<int> left, Vector<int> right);

    /// svbool_t svcmpgt[_s64](svbool_t pg, svint64_t op1, svint64_t op2) : "CMPGT Presult.D, Pg/Z, Zop1.D, Zop2.D"
  public static unsafe Vector<long> CompareGreaterThan(Vector<long> left, Vector<long> right);

    /// svbool_t svcmpgt[_u8](svbool_t pg, svuint8_t op1, svuint8_t op2) : "CMPHI Presult.B, Pg/Z, Zop1.B, Zop2.B"
  public static unsafe Vector<byte> CompareGreaterThan(Vector<byte> left, Vector<byte> right);

    /// svbool_t svcmpgt[_u16](svbool_t pg, svuint16_t op1, svuint16_t op2) : "CMPHI Presult.H, Pg/Z, Zop1.H, Zop2.H"
  public static unsafe Vector<ushort> CompareGreaterThan(Vector<ushort> left, Vector<ushort> right);

    /// svbool_t svcmpgt[_u32](svbool_t pg, svuint32_t op1, svuint32_t op2) : "CMPHI Presult.S, Pg/Z, Zop1.S, Zop2.S"
  public static unsafe Vector<uint> CompareGreaterThan(Vector<uint> left, Vector<uint> right);

    /// svbool_t svcmpgt[_u64](svbool_t pg, svuint64_t op1, svuint64_t op2) : "CMPHI Presult.D, Pg/Z, Zop1.D, Zop2.D"
  public static unsafe Vector<ulong> CompareGreaterThan(Vector<ulong> left, Vector<ulong> right);

    /// svbool_t svcmpgt_wide[_s8](svbool_t pg, svint8_t op1, svint64_t op2) : "CMPGT Presult.B, Pg/Z, Zop1.B, Zop2.D"
  public static unsafe Vector<sbyte> CompareGreaterThan(Vector<sbyte> left, Vector<long> right);

    /// svbool_t svcmpgt_wide[_s16](svbool_t pg, svint16_t op1, svint64_t op2) : "CMPGT Presult.H, Pg/Z, Zop1.H, Zop2.D"
  public static unsafe Vector<short> CompareGreaterThan(Vector<short> left, Vector<long> right);

    /// svbool_t svcmpgt_wide[_s32](svbool_t pg, svint32_t op1, svint64_t op2) : "CMPGT Presult.S, Pg/Z, Zop1.S, Zop2.D"
  public static unsafe Vector<int> CompareGreaterThan(Vector<int> left, Vector<long> right);

    /// svbool_t svcmpgt_wide[_u8](svbool_t pg, svuint8_t op1, svuint64_t op2) : "CMPHI Presult.B, Pg/Z, Zop1.B, Zop2.D"
  public static unsafe Vector<byte> CompareGreaterThan(Vector<byte> left, Vector<ulong> right);

    /// svbool_t svcmpgt_wide[_u16](svbool_t pg, svuint16_t op1, svuint64_t op2) : "CMPHI Presult.H, Pg/Z, Zop1.H, Zop2.D"
  public static unsafe Vector<ushort> CompareGreaterThan(Vector<ushort> left, Vector<ulong> right);

    /// svbool_t svcmpgt_wide[_u32](svbool_t pg, svuint32_t op1, svuint64_t op2) : "CMPHI Presult.S, Pg/Z, Zop1.S, Zop2.D"
  public static unsafe Vector<uint> CompareGreaterThan(Vector<uint> left, Vector<ulong> right);


    /// CompareGreaterThanOrEqual : Compare greater than or equal to

    /// svbool_t svcmpge[_f32](svbool_t pg, svfloat32_t op1, svfloat32_t op2) : "FCMGE Presult.S, Pg/Z, Zop1.S, Zop2.S"
  public static unsafe Vector<float> CompareGreaterThanOrEqual(Vector<float> left, Vector<float> right);

    /// svbool_t svcmpge[_f64](svbool_t pg, svfloat64_t op1, svfloat64_t op2) : "FCMGE Presult.D, Pg/Z, Zop1.D, Zop2.D"
  public static unsafe Vector<double> CompareGreaterThanOrEqual(Vector<double> left, Vector<double> right);

    /// svbool_t svcmpge[_s8](svbool_t pg, svint8_t op1, svint8_t op2) : "CMPGE Presult.B, Pg/Z, Zop1.B, Zop2.B"
  public static unsafe Vector<sbyte> CompareGreaterThanOrEqual(Vector<sbyte> left, Vector<sbyte> right);

    /// svbool_t svcmpge[_s16](svbool_t pg, svint16_t op1, svint16_t op2) : "CMPGE Presult.H, Pg/Z, Zop1.H, Zop2.H"
  public static unsafe Vector<short> CompareGreaterThanOrEqual(Vector<short> left, Vector<short> right);

    /// svbool_t svcmpge[_s32](svbool_t pg, svint32_t op1, svint32_t op2) : "CMPGE Presult.S, Pg/Z, Zop1.S, Zop2.S"
  public static unsafe Vector<int> CompareGreaterThanOrEqual(Vector<int> left, Vector<int> right);

    /// svbool_t svcmpge[_s64](svbool_t pg, svint64_t op1, svint64_t op2) : "CMPGE Presult.D, Pg/Z, Zop1.D, Zop2.D"
  public static unsafe Vector<long> CompareGreaterThanOrEqual(Vector<long> left, Vector<long> right);

    /// svbool_t svcmpge[_u8](svbool_t pg, svuint8_t op1, svuint8_t op2) : "CMPHS Presult.B, Pg/Z, Zop1.B, Zop2.B"
  public static unsafe Vector<byte> CompareGreaterThanOrEqual(Vector<byte> left, Vector<byte> right);

    /// svbool_t svcmpge[_u16](svbool_t pg, svuint16_t op1, svuint16_t op2) : "CMPHS Presult.H, Pg/Z, Zop1.H, Zop2.H"
  public static unsafe Vector<ushort> CompareGreaterThanOrEqual(Vector<ushort> left, Vector<ushort> right);

    /// svbool_t svcmpge[_u32](svbool_t pg, svuint32_t op1, svuint32_t op2) : "CMPHS Presult.S, Pg/Z, Zop1.S, Zop2.S"
  public static unsafe Vector<uint> CompareGreaterThanOrEqual(Vector<uint> left, Vector<uint> right);

    /// svbool_t svcmpge[_u64](svbool_t pg, svuint64_t op1, svuint64_t op2) : "CMPHS Presult.D, Pg/Z, Zop1.D, Zop2.D"
  public static unsafe Vector<ulong> CompareGreaterThanOrEqual(Vector<ulong> left, Vector<ulong> right);

    /// svbool_t svcmpge_wide[_s8](svbool_t pg, svint8_t op1, svint64_t op2) : "CMPGE Presult.B, Pg/Z, Zop1.B, Zop2.D"
  public static unsafe Vector<sbyte> CompareGreaterThanOrEqual(Vector<sbyte> left, Vector<long> right);

    /// svbool_t svcmpge_wide[_s16](svbool_t pg, svint16_t op1, svint64_t op2) : "CMPGE Presult.H, Pg/Z, Zop1.H, Zop2.D"
  public static unsafe Vector<short> CompareGreaterThanOrEqual(Vector<short> left, Vector<long> right);

    /// svbool_t svcmpge_wide[_s32](svbool_t pg, svint32_t op1, svint64_t op2) : "CMPGE Presult.S, Pg/Z, Zop1.S, Zop2.D"
  public static unsafe Vector<int> CompareGreaterThanOrEqual(Vector<int> left, Vector<long> right);

    /// svbool_t svcmpge_wide[_u8](svbool_t pg, svuint8_t op1, svuint64_t op2) : "CMPHS Presult.B, Pg/Z, Zop1.B, Zop2.D"
  public static unsafe Vector<byte> CompareGreaterThanOrEqual(Vector<byte> left, Vector<ulong> right);

    /// svbool_t svcmpge_wide[_u16](svbool_t pg, svuint16_t op1, svuint64_t op2) : "CMPHS Presult.H, Pg/Z, Zop1.H, Zop2.D"
  public static unsafe Vector<ushort> CompareGreaterThanOrEqual(Vector<ushort> left, Vector<ulong> right);

    /// svbool_t svcmpge_wide[_u32](svbool_t pg, svuint32_t op1, svuint64_t op2) : "CMPHS Presult.S, Pg/Z, Zop1.S, Zop2.D"
  public static unsafe Vector<uint> CompareGreaterThanOrEqual(Vector<uint> left, Vector<ulong> right);


    /// CompareLessThan : Compare less than

    /// svbool_t svcmplt[_f32](svbool_t pg, svfloat32_t op1, svfloat32_t op2) : "FCMGT Presult.S, Pg/Z, Zop2.S, Zop1.S"
  public static unsafe Vector<float> CompareLessThan(Vector<float> left, Vector<float> right);

    /// svbool_t svcmplt[_f64](svbool_t pg, svfloat64_t op1, svfloat64_t op2) : "FCMGT Presult.D, Pg/Z, Zop2.D, Zop1.D"
  public static unsafe Vector<double> CompareLessThan(Vector<double> left, Vector<double> right);

    /// svbool_t svcmplt[_s8](svbool_t pg, svint8_t op1, svint8_t op2) : "CMPGT Presult.B, Pg/Z, Zop2.B, Zop1.B"
  public static unsafe Vector<sbyte> CompareLessThan(Vector<sbyte> left, Vector<sbyte> right);

    /// svbool_t svcmplt[_s16](svbool_t pg, svint16_t op1, svint16_t op2) : "CMPGT Presult.H, Pg/Z, Zop2.H, Zop1.H"
  public static unsafe Vector<short> CompareLessThan(Vector<short> left, Vector<short> right);

    /// svbool_t svcmplt[_s32](svbool_t pg, svint32_t op1, svint32_t op2) : "CMPGT Presult.S, Pg/Z, Zop2.S, Zop1.S"
  public static unsafe Vector<int> CompareLessThan(Vector<int> left, Vector<int> right);

    /// svbool_t svcmplt[_s64](svbool_t pg, svint64_t op1, svint64_t op2) : "CMPGT Presult.D, Pg/Z, Zop2.D, Zop1.D"
  public static unsafe Vector<long> CompareLessThan(Vector<long> left, Vector<long> right);

    /// svbool_t svcmplt[_u8](svbool_t pg, svuint8_t op1, svuint8_t op2) : "CMPHI Presult.B, Pg/Z, Zop2.B, Zop1.B"
  public static unsafe Vector<byte> CompareLessThan(Vector<byte> left, Vector<byte> right);

    /// svbool_t svcmplt[_u16](svbool_t pg, svuint16_t op1, svuint16_t op2) : "CMPHI Presult.H, Pg/Z, Zop2.H, Zop1.H"
  public static unsafe Vector<ushort> CompareLessThan(Vector<ushort> left, Vector<ushort> right);

    /// svbool_t svcmplt[_u32](svbool_t pg, svuint32_t op1, svuint32_t op2) : "CMPHI Presult.S, Pg/Z, Zop2.S, Zop1.S"
  public static unsafe Vector<uint> CompareLessThan(Vector<uint> left, Vector<uint> right);

    /// svbool_t svcmplt[_u64](svbool_t pg, svuint64_t op1, svuint64_t op2) : "CMPHI Presult.D, Pg/Z, Zop2.D, Zop1.D"
  public static unsafe Vector<ulong> CompareLessThan(Vector<ulong> left, Vector<ulong> right);

    /// svbool_t svcmplt_wide[_s8](svbool_t pg, svint8_t op1, svint64_t op2) : "CMPLT Presult.B, Pg/Z, Zop1.B, Zop2.D"
  public static unsafe Vector<sbyte> CompareLessThan(Vector<sbyte> left, Vector<long> right);

    /// svbool_t svcmplt_wide[_s16](svbool_t pg, svint16_t op1, svint64_t op2) : "CMPLT Presult.H, Pg/Z, Zop1.H, Zop2.D"
  public static unsafe Vector<short> CompareLessThan(Vector<short> left, Vector<long> right);

    /// svbool_t svcmplt_wide[_s32](svbool_t pg, svint32_t op1, svint64_t op2) : "CMPLT Presult.S, Pg/Z, Zop1.S, Zop2.D"
  public static unsafe Vector<int> CompareLessThan(Vector<int> left, Vector<long> right);

    /// svbool_t svcmplt_wide[_u8](svbool_t pg, svuint8_t op1, svuint64_t op2) : "CMPLO Presult.B, Pg/Z, Zop1.B, Zop2.D"
  public static unsafe Vector<byte> CompareLessThan(Vector<byte> left, Vector<ulong> right);

    /// svbool_t svcmplt_wide[_u16](svbool_t pg, svuint16_t op1, svuint64_t op2) : "CMPLO Presult.H, Pg/Z, Zop1.H, Zop2.D"
  public static unsafe Vector<ushort> CompareLessThan(Vector<ushort> left, Vector<ulong> right);

    /// svbool_t svcmplt_wide[_u32](svbool_t pg, svuint32_t op1, svuint64_t op2) : "CMPLO Presult.S, Pg/Z, Zop1.S, Zop2.D"
  public static unsafe Vector<uint> CompareLessThan(Vector<uint> left, Vector<ulong> right);


    /// CompareLessThanOrEqual : Compare less than or equal to

    /// svbool_t svcmple[_f32](svbool_t pg, svfloat32_t op1, svfloat32_t op2) : "FCMGE Presult.S, Pg/Z, Zop2.S, Zop1.S"
  public static unsafe Vector<float> CompareLessThanOrEqual(Vector<float> left, Vector<float> right);

    /// svbool_t svcmple[_f64](svbool_t pg, svfloat64_t op1, svfloat64_t op2) : "FCMGE Presult.D, Pg/Z, Zop2.D, Zop1.D"
  public static unsafe Vector<double> CompareLessThanOrEqual(Vector<double> left, Vector<double> right);

    /// svbool_t svcmple[_s8](svbool_t pg, svint8_t op1, svint8_t op2) : "CMPGE Presult.B, Pg/Z, Zop2.B, Zop1.B"
  public static unsafe Vector<sbyte> CompareLessThanOrEqual(Vector<sbyte> left, Vector<sbyte> right);

    /// svbool_t svcmple[_s16](svbool_t pg, svint16_t op1, svint16_t op2) : "CMPGE Presult.H, Pg/Z, Zop2.H, Zop1.H"
  public static unsafe Vector<short> CompareLessThanOrEqual(Vector<short> left, Vector<short> right);

    /// svbool_t svcmple[_s32](svbool_t pg, svint32_t op1, svint32_t op2) : "CMPGE Presult.S, Pg/Z, Zop2.S, Zop1.S"
  public static unsafe Vector<int> CompareLessThanOrEqual(Vector<int> left, Vector<int> right);

    /// svbool_t svcmple[_s64](svbool_t pg, svint64_t op1, svint64_t op2) : "CMPGE Presult.D, Pg/Z, Zop2.D, Zop1.D"
  public static unsafe Vector<long> CompareLessThanOrEqual(Vector<long> left, Vector<long> right);

    /// svbool_t svcmple[_u8](svbool_t pg, svuint8_t op1, svuint8_t op2) : "CMPHS Presult.B, Pg/Z, Zop2.B, Zop1.B"
  public static unsafe Vector<byte> CompareLessThanOrEqual(Vector<byte> left, Vector<byte> right);

    /// svbool_t svcmple[_u16](svbool_t pg, svuint16_t op1, svuint16_t op2) : "CMPHS Presult.H, Pg/Z, Zop2.H, Zop1.H"
  public static unsafe Vector<ushort> CompareLessThanOrEqual(Vector<ushort> left, Vector<ushort> right);

    /// svbool_t svcmple[_u32](svbool_t pg, svuint32_t op1, svuint32_t op2) : "CMPHS Presult.S, Pg/Z, Zop2.S, Zop1.S"
  public static unsafe Vector<uint> CompareLessThanOrEqual(Vector<uint> left, Vector<uint> right);

    /// svbool_t svcmple[_u64](svbool_t pg, svuint64_t op1, svuint64_t op2) : "CMPHS Presult.D, Pg/Z, Zop2.D, Zop1.D"
  public static unsafe Vector<ulong> CompareLessThanOrEqual(Vector<ulong> left, Vector<ulong> right);

    /// svbool_t svcmple_wide[_s8](svbool_t pg, svint8_t op1, svint64_t op2) : "CMPLE Presult.B, Pg/Z, Zop1.B, Zop2.D"
  public static unsafe Vector<sbyte> CompareLessThanOrEqual(Vector<sbyte> left, Vector<long> right);

    /// svbool_t svcmple_wide[_s16](svbool_t pg, svint16_t op1, svint64_t op2) : "CMPLE Presult.H, Pg/Z, Zop1.H, Zop2.D"
  public static unsafe Vector<short> CompareLessThanOrEqual(Vector<short> left, Vector<long> right);

    /// svbool_t svcmple_wide[_s32](svbool_t pg, svint32_t op1, svint64_t op2) : "CMPLE Presult.S, Pg/Z, Zop1.S, Zop2.D"
  public static unsafe Vector<int> CompareLessThanOrEqual(Vector<int> left, Vector<long> right);

    /// svbool_t svcmple_wide[_u8](svbool_t pg, svuint8_t op1, svuint64_t op2) : "CMPLS Presult.B, Pg/Z, Zop1.B, Zop2.D"
  public static unsafe Vector<byte> CompareLessThanOrEqual(Vector<byte> left, Vector<ulong> right);

    /// svbool_t svcmple_wide[_u16](svbool_t pg, svuint16_t op1, svuint64_t op2) : "CMPLS Presult.H, Pg/Z, Zop1.H, Zop2.D"
  public static unsafe Vector<ushort> CompareLessThanOrEqual(Vector<ushort> left, Vector<ulong> right);

    /// svbool_t svcmple_wide[_u32](svbool_t pg, svuint32_t op1, svuint64_t op2) : "CMPLS Presult.S, Pg/Z, Zop1.S, Zop2.D"
  public static unsafe Vector<uint> CompareLessThanOrEqual(Vector<uint> left, Vector<ulong> right);


    /// CompareNotEqualTo : Compare not equal to

    /// svbool_t svcmpne[_f32](svbool_t pg, svfloat32_t op1, svfloat32_t op2) : "FCMNE Presult.S, Pg/Z, Zop1.S, Zop2.S"
  public static unsafe Vector<float> CompareNotEqualTo(Vector<float> left, Vector<float> right);

    /// svbool_t svcmpne[_f64](svbool_t pg, svfloat64_t op1, svfloat64_t op2) : "FCMNE Presult.D, Pg/Z, Zop1.D, Zop2.D"
  public static unsafe Vector<double> CompareNotEqualTo(Vector<double> left, Vector<double> right);

    /// svbool_t svcmpne[_s8](svbool_t pg, svint8_t op1, svint8_t op2) : "CMPNE Presult.B, Pg/Z, Zop1.B, Zop2.B"
  public static unsafe Vector<sbyte> CompareNotEqualTo(Vector<sbyte> left, Vector<sbyte> right);

    /// svbool_t svcmpne[_s16](svbool_t pg, svint16_t op1, svint16_t op2) : "CMPNE Presult.H, Pg/Z, Zop1.H, Zop2.H"
  public static unsafe Vector<short> CompareNotEqualTo(Vector<short> left, Vector<short> right);

    /// svbool_t svcmpne[_s32](svbool_t pg, svint32_t op1, svint32_t op2) : "CMPNE Presult.S, Pg/Z, Zop1.S, Zop2.S"
  public static unsafe Vector<int> CompareNotEqualTo(Vector<int> left, Vector<int> right);

    /// svbool_t svcmpne[_s64](svbool_t pg, svint64_t op1, svint64_t op2) : "CMPNE Presult.D, Pg/Z, Zop1.D, Zop2.D"
  public static unsafe Vector<long> CompareNotEqualTo(Vector<long> left, Vector<long> right);

    /// svbool_t svcmpne[_u8](svbool_t pg, svuint8_t op1, svuint8_t op2) : "CMPNE Presult.B, Pg/Z, Zop1.B, Zop2.B"
  public static unsafe Vector<byte> CompareNotEqualTo(Vector<byte> left, Vector<byte> right);

    /// svbool_t svcmpne[_u16](svbool_t pg, svuint16_t op1, svuint16_t op2) : "CMPNE Presult.H, Pg/Z, Zop1.H, Zop2.H"
  public static unsafe Vector<ushort> CompareNotEqualTo(Vector<ushort> left, Vector<ushort> right);

    /// svbool_t svcmpne[_u32](svbool_t pg, svuint32_t op1, svuint32_t op2) : "CMPNE Presult.S, Pg/Z, Zop1.S, Zop2.S"
  public static unsafe Vector<uint> CompareNotEqualTo(Vector<uint> left, Vector<uint> right);

    /// svbool_t svcmpne[_u64](svbool_t pg, svuint64_t op1, svuint64_t op2) : "CMPNE Presult.D, Pg/Z, Zop1.D, Zop2.D"
  public static unsafe Vector<ulong> CompareNotEqualTo(Vector<ulong> left, Vector<ulong> right);

    /// svbool_t svcmpne_wide[_s8](svbool_t pg, svint8_t op1, svint64_t op2) : "CMPNE Presult.B, Pg/Z, Zop1.B, Zop2.D"
  public static unsafe Vector<sbyte> CompareNotEqualTo(Vector<sbyte> left, Vector<long> right);

    /// svbool_t svcmpne_wide[_s16](svbool_t pg, svint16_t op1, svint64_t op2) : "CMPNE Presult.H, Pg/Z, Zop1.H, Zop2.D"
  public static unsafe Vector<short> CompareNotEqualTo(Vector<short> left, Vector<long> right);

    /// svbool_t svcmpne_wide[_s32](svbool_t pg, svint32_t op1, svint64_t op2) : "CMPNE Presult.S, Pg/Z, Zop1.S, Zop2.D"
  public static unsafe Vector<int> CompareNotEqualTo(Vector<int> left, Vector<long> right);


    /// CompareUnordered : Compare unordered with

    /// svbool_t svcmpuo[_f32](svbool_t pg, svfloat32_t op1, svfloat32_t op2) : "FCMUO Presult.S, Pg/Z, Zop1.S, Zop2.S"
  public static unsafe Vector<float> CompareUnordered(Vector<float> left, Vector<float> right);

    /// svbool_t svcmpuo[_f64](svbool_t pg, svfloat64_t op1, svfloat64_t op2) : "FCMUO Presult.D, Pg/Z, Zop1.D, Zop2.D"
  public static unsafe Vector<double> CompareUnordered(Vector<double> left, Vector<double> right);


    /// ConditionalExtractAfterLastActiveElement : Conditionally extract element after last

    /// svfloat32_t svclasta[_f32](svbool_t pg, svfloat32_t fallback, svfloat32_t data) : "CLASTA Ztied.S, Pg, Ztied.S, Zdata.S" or "MOVPRFX Zresult, Zfallback; CLASTA Zresult.S, Pg, Zresult.S, Zdata.S"
  public static unsafe Vector<float> ConditionalExtractAfterLastActiveElement(Vector<float> mask, Vector<float> defaultValue, Vector<float> data);

    /// svfloat32_t svclasta[_f32](svbool_t pg, svfloat32_t fallback, svfloat32_t data) : "CLASTA Ztied.S, Pg, Ztied.S, Zdata.S" or "MOVPRFX Zresult, Zfallback; CLASTA Zresult.S, Pg, Zresult.S, Zdata.S"
    /// float32_t svclasta[_n_f32](svbool_t pg, float32_t fallback, svfloat32_t data) : "CLASTA Wtied, Pg, Wtied, Zdata.S" or "CLASTA Stied, Pg, Stied, Zdata.S"
  public static unsafe float ConditionalExtractAfterLastActiveElement(Vector<float> mask, float defaultValues, Vector<float> data);

    /// svfloat64_t svclasta[_f64](svbool_t pg, svfloat64_t fallback, svfloat64_t data) : "CLASTA Ztied.D, Pg, Ztied.D, Zdata.D" or "MOVPRFX Zresult, Zfallback; CLASTA Zresult.D, Pg, Zresult.D, Zdata.D"
  public static unsafe Vector<double> ConditionalExtractAfterLastActiveElement(Vector<double> mask, Vector<double> defaultValue, Vector<double> data);

    /// svfloat64_t svclasta[_f64](svbool_t pg, svfloat64_t fallback, svfloat64_t data) : "CLASTA Ztied.D, Pg, Ztied.D, Zdata.D" or "MOVPRFX Zresult, Zfallback; CLASTA Zresult.D, Pg, Zresult.D, Zdata.D"
    /// float64_t svclasta[_n_f64](svbool_t pg, float64_t fallback, svfloat64_t data) : "CLASTA Xtied, Pg, Xtied, Zdata.D" or "CLASTA Dtied, Pg, Dtied, Zdata.D"
  public static unsafe double ConditionalExtractAfterLastActiveElement(Vector<double> mask, double defaultValues, Vector<double> data);

    /// svint8_t svclasta[_s8](svbool_t pg, svint8_t fallback, svint8_t data) : "CLASTA Ztied.B, Pg, Ztied.B, Zdata.B" or "MOVPRFX Zresult, Zfallback; CLASTA Zresult.B, Pg, Zresult.B, Zdata.B"
  public static unsafe Vector<sbyte> ConditionalExtractAfterLastActiveElement(Vector<sbyte> mask, Vector<sbyte> defaultValue, Vector<sbyte> data);

    /// svint8_t svclasta[_s8](svbool_t pg, svint8_t fallback, svint8_t data) : "CLASTA Ztied.B, Pg, Ztied.B, Zdata.B" or "MOVPRFX Zresult, Zfallback; CLASTA Zresult.B, Pg, Zresult.B, Zdata.B"
    /// int8_t svclasta[_n_s8](svbool_t pg, int8_t fallback, svint8_t data) : "CLASTA Wtied, Pg, Wtied, Zdata.B" or "CLASTA Btied, Pg, Btied, Zdata.B"
  public static unsafe sbyte ConditionalExtractAfterLastActiveElement(Vector<sbyte> mask, sbyte defaultValues, Vector<sbyte> data);

    /// svint16_t svclasta[_s16](svbool_t pg, svint16_t fallback, svint16_t data) : "CLASTA Ztied.H, Pg, Ztied.H, Zdata.H" or "MOVPRFX Zresult, Zfallback; CLASTA Zresult.H, Pg, Zresult.H, Zdata.H"
  public static unsafe Vector<short> ConditionalExtractAfterLastActiveElement(Vector<short> mask, Vector<short> defaultValue, Vector<short> data);

    /// svint16_t svclasta[_s16](svbool_t pg, svint16_t fallback, svint16_t data) : "CLASTA Ztied.H, Pg, Ztied.H, Zdata.H" or "MOVPRFX Zresult, Zfallback; CLASTA Zresult.H, Pg, Zresult.H, Zdata.H"
    /// int16_t svclasta[_n_s16](svbool_t pg, int16_t fallback, svint16_t data) : "CLASTA Wtied, Pg, Wtied, Zdata.H" or "CLASTA Htied, Pg, Htied, Zdata.H"
  public static unsafe short ConditionalExtractAfterLastActiveElement(Vector<short> mask, short defaultValues, Vector<short> data);

    /// svint32_t svclasta[_s32](svbool_t pg, svint32_t fallback, svint32_t data) : "CLASTA Ztied.S, Pg, Ztied.S, Zdata.S" or "MOVPRFX Zresult, Zfallback; CLASTA Zresult.S, Pg, Zresult.S, Zdata.S"
  public static unsafe Vector<int> ConditionalExtractAfterLastActiveElement(Vector<int> mask, Vector<int> defaultValue, Vector<int> data);

    /// svint32_t svclasta[_s32](svbool_t pg, svint32_t fallback, svint32_t data) : "CLASTA Ztied.S, Pg, Ztied.S, Zdata.S" or "MOVPRFX Zresult, Zfallback; CLASTA Zresult.S, Pg, Zresult.S, Zdata.S"
    /// int32_t svclasta[_n_s32](svbool_t pg, int32_t fallback, svint32_t data) : "CLASTA Wtied, Pg, Wtied, Zdata.S" or "CLASTA Stied, Pg, Stied, Zdata.S"
  public static unsafe int ConditionalExtractAfterLastActiveElement(Vector<int> mask, int defaultValues, Vector<int> data);

    /// svint64_t svclasta[_s64](svbool_t pg, svint64_t fallback, svint64_t data) : "CLASTA Ztied.D, Pg, Ztied.D, Zdata.D" or "MOVPRFX Zresult, Zfallback; CLASTA Zresult.D, Pg, Zresult.D, Zdata.D"
  public static unsafe Vector<long> ConditionalExtractAfterLastActiveElement(Vector<long> mask, Vector<long> defaultValue, Vector<long> data);

    /// svint64_t svclasta[_s64](svbool_t pg, svint64_t fallback, svint64_t data) : "CLASTA Ztied.D, Pg, Ztied.D, Zdata.D" or "MOVPRFX Zresult, Zfallback; CLASTA Zresult.D, Pg, Zresult.D, Zdata.D"
    /// int64_t svclasta[_n_s64](svbool_t pg, int64_t fallback, svint64_t data) : "CLASTA Xtied, Pg, Xtied, Zdata.D" or "CLASTA Dtied, Pg, Dtied, Zdata.D"
  public static unsafe long ConditionalExtractAfterLastActiveElement(Vector<long> mask, long defaultValues, Vector<long> data);

    /// svuint8_t svclasta[_u8](svbool_t pg, svuint8_t fallback, svuint8_t data) : "CLASTA Ztied.B, Pg, Ztied.B, Zdata.B" or "MOVPRFX Zresult, Zfallback; CLASTA Zresult.B, Pg, Zresult.B, Zdata.B"
  public static unsafe Vector<byte> ConditionalExtractAfterLastActiveElement(Vector<byte> mask, Vector<byte> defaultValue, Vector<byte> data);

    /// svuint8_t svclasta[_u8](svbool_t pg, svuint8_t fallback, svuint8_t data) : "CLASTA Ztied.B, Pg, Ztied.B, Zdata.B" or "MOVPRFX Zresult, Zfallback; CLASTA Zresult.B, Pg, Zresult.B, Zdata.B"
    /// uint8_t svclasta[_n_u8](svbool_t pg, uint8_t fallback, svuint8_t data) : "CLASTA Wtied, Pg, Wtied, Zdata.B" or "CLASTA Btied, Pg, Btied, Zdata.B"
  public static unsafe byte ConditionalExtractAfterLastActiveElement(Vector<byte> mask, byte defaultValues, Vector<byte> data);

    /// svuint16_t svclasta[_u16](svbool_t pg, svuint16_t fallback, svuint16_t data) : "CLASTA Ztied.H, Pg, Ztied.H, Zdata.H" or "MOVPRFX Zresult, Zfallback; CLASTA Zresult.H, Pg, Zresult.H, Zdata.H"
  public static unsafe Vector<ushort> ConditionalExtractAfterLastActiveElement(Vector<ushort> mask, Vector<ushort> defaultValue, Vector<ushort> data);

    /// svuint16_t svclasta[_u16](svbool_t pg, svuint16_t fallback, svuint16_t data) : "CLASTA Ztied.H, Pg, Ztied.H, Zdata.H" or "MOVPRFX Zresult, Zfallback; CLASTA Zresult.H, Pg, Zresult.H, Zdata.H"
    /// uint16_t svclasta[_n_u16](svbool_t pg, uint16_t fallback, svuint16_t data) : "CLASTA Wtied, Pg, Wtied, Zdata.H" or "CLASTA Htied, Pg, Htied, Zdata.H"
  public static unsafe ushort ConditionalExtractAfterLastActiveElement(Vector<ushort> mask, ushort defaultValues, Vector<ushort> data);

    /// svuint32_t svclasta[_u32](svbool_t pg, svuint32_t fallback, svuint32_t data) : "CLASTA Ztied.S, Pg, Ztied.S, Zdata.S" or "MOVPRFX Zresult, Zfallback; CLASTA Zresult.S, Pg, Zresult.S, Zdata.S"
  public static unsafe Vector<uint> ConditionalExtractAfterLastActiveElement(Vector<uint> mask, Vector<uint> defaultValue, Vector<uint> data);

    /// svuint32_t svclasta[_u32](svbool_t pg, svuint32_t fallback, svuint32_t data) : "CLASTA Ztied.S, Pg, Ztied.S, Zdata.S" or "MOVPRFX Zresult, Zfallback; CLASTA Zresult.S, Pg, Zresult.S, Zdata.S"
    /// uint32_t svclasta[_n_u32](svbool_t pg, uint32_t fallback, svuint32_t data) : "CLASTA Wtied, Pg, Wtied, Zdata.S" or "CLASTA Stied, Pg, Stied, Zdata.S"
  public static unsafe uint ConditionalExtractAfterLastActiveElement(Vector<uint> mask, uint defaultValues, Vector<uint> data);

    /// svuint64_t svclasta[_u64](svbool_t pg, svuint64_t fallback, svuint64_t data) : "CLASTA Ztied.D, Pg, Ztied.D, Zdata.D" or "MOVPRFX Zresult, Zfallback; CLASTA Zresult.D, Pg, Zresult.D, Zdata.D"
  public static unsafe Vector<ulong> ConditionalExtractAfterLastActiveElement(Vector<ulong> mask, Vector<ulong> defaultValue, Vector<ulong> data);

    /// svuint64_t svclasta[_u64](svbool_t pg, svuint64_t fallback, svuint64_t data) : "CLASTA Ztied.D, Pg, Ztied.D, Zdata.D" or "MOVPRFX Zresult, Zfallback; CLASTA Zresult.D, Pg, Zresult.D, Zdata.D"
    /// uint64_t svclasta[_n_u64](svbool_t pg, uint64_t fallback, svuint64_t data) : "CLASTA Xtied, Pg, Xtied, Zdata.D" or "CLASTA Dtied, Pg, Dtied, Zdata.D"
  public static unsafe ulong ConditionalExtractAfterLastActiveElement(Vector<ulong> mask, ulong defaultValues, Vector<ulong> data);


    /// ConditionalExtractAfterLastActiveElementAndReplicate : Conditionally extract element after last

    /// svfloat32_t svclasta[_f32](svbool_t pg, svfloat32_t fallback, svfloat32_t data) : "CLASTA Ztied.S, Pg, Ztied.S, Zdata.S" or "MOVPRFX Zresult, Zfallback; CLASTA Zresult.S, Pg, Zresult.S, Zdata.S"
  public static unsafe Vector<float> ConditionalExtractAfterLastActiveElementAndReplicate(Vector<float> mask, Vector<float> defaultScalar, Vector<float> data);

    /// svfloat64_t svclasta[_f64](svbool_t pg, svfloat64_t fallback, svfloat64_t data) : "CLASTA Ztied.D, Pg, Ztied.D, Zdata.D" or "MOVPRFX Zresult, Zfallback; CLASTA Zresult.D, Pg, Zresult.D, Zdata.D"
  public static unsafe Vector<double> ConditionalExtractAfterLastActiveElementAndReplicate(Vector<double> mask, Vector<double> defaultScalar, Vector<double> data);

    /// svint8_t svclasta[_s8](svbool_t pg, svint8_t fallback, svint8_t data) : "CLASTA Ztied.B, Pg, Ztied.B, Zdata.B" or "MOVPRFX Zresult, Zfallback; CLASTA Zresult.B, Pg, Zresult.B, Zdata.B"
  public static unsafe Vector<sbyte> ConditionalExtractAfterLastActiveElementAndReplicate(Vector<sbyte> mask, Vector<sbyte> defaultScalar, Vector<sbyte> data);

    /// svint16_t svclasta[_s16](svbool_t pg, svint16_t fallback, svint16_t data) : "CLASTA Ztied.H, Pg, Ztied.H, Zdata.H" or "MOVPRFX Zresult, Zfallback; CLASTA Zresult.H, Pg, Zresult.H, Zdata.H"
  public static unsafe Vector<short> ConditionalExtractAfterLastActiveElementAndReplicate(Vector<short> mask, Vector<short> defaultScalar, Vector<short> data);

    /// svint32_t svclasta[_s32](svbool_t pg, svint32_t fallback, svint32_t data) : "CLASTA Ztied.S, Pg, Ztied.S, Zdata.S" or "MOVPRFX Zresult, Zfallback; CLASTA Zresult.S, Pg, Zresult.S, Zdata.S"
  public static unsafe Vector<int> ConditionalExtractAfterLastActiveElementAndReplicate(Vector<int> mask, Vector<int> defaultScalar, Vector<int> data);

    /// svint64_t svclasta[_s64](svbool_t pg, svint64_t fallback, svint64_t data) : "CLASTA Ztied.D, Pg, Ztied.D, Zdata.D" or "MOVPRFX Zresult, Zfallback; CLASTA Zresult.D, Pg, Zresult.D, Zdata.D"
  public static unsafe Vector<long> ConditionalExtractAfterLastActiveElementAndReplicate(Vector<long> mask, Vector<long> defaultScalar, Vector<long> data);

    /// svuint8_t svclasta[_u8](svbool_t pg, svuint8_t fallback, svuint8_t data) : "CLASTA Ztied.B, Pg, Ztied.B, Zdata.B" or "MOVPRFX Zresult, Zfallback; CLASTA Zresult.B, Pg, Zresult.B, Zdata.B"
  public static unsafe Vector<byte> ConditionalExtractAfterLastActiveElementAndReplicate(Vector<byte> mask, Vector<byte> defaultScalar, Vector<byte> data);

    /// svuint16_t svclasta[_u16](svbool_t pg, svuint16_t fallback, svuint16_t data) : "CLASTA Ztied.H, Pg, Ztied.H, Zdata.H" or "MOVPRFX Zresult, Zfallback; CLASTA Zresult.H, Pg, Zresult.H, Zdata.H"
  public static unsafe Vector<ushort> ConditionalExtractAfterLastActiveElementAndReplicate(Vector<ushort> mask, Vector<ushort> defaultScalar, Vector<ushort> data);

    /// svuint32_t svclasta[_u32](svbool_t pg, svuint32_t fallback, svuint32_t data) : "CLASTA Ztied.S, Pg, Ztied.S, Zdata.S" or "MOVPRFX Zresult, Zfallback; CLASTA Zresult.S, Pg, Zresult.S, Zdata.S"
  public static unsafe Vector<uint> ConditionalExtractAfterLastActiveElementAndReplicate(Vector<uint> mask, Vector<uint> defaultScalar, Vector<uint> data);

    /// svuint64_t svclasta[_u64](svbool_t pg, svuint64_t fallback, svuint64_t data) : "CLASTA Ztied.D, Pg, Ztied.D, Zdata.D" or "MOVPRFX Zresult, Zfallback; CLASTA Zresult.D, Pg, Zresult.D, Zdata.D"
  public static unsafe Vector<ulong> ConditionalExtractAfterLastActiveElementAndReplicate(Vector<ulong> mask, Vector<ulong> defaultScalar, Vector<ulong> data);


    /// ConditionalExtractLastActiveElement : Conditionally extract last element

    /// svfloat32_t svclastb[_f32](svbool_t pg, svfloat32_t fallback, svfloat32_t data) : "CLASTB Ztied.S, Pg, Ztied.S, Zdata.S" or "MOVPRFX Zresult, Zfallback; CLASTB Zresult.S, Pg, Zresult.S, Zdata.S"
  public static unsafe Vector<float> ConditionalExtractLastActiveElement(Vector<float> mask, Vector<float> defaultValue, Vector<float> data);

    /// svfloat32_t svclastb[_f32](svbool_t pg, svfloat32_t fallback, svfloat32_t data) : "CLASTB Ztied.S, Pg, Ztied.S, Zdata.S" or "MOVPRFX Zresult, Zfallback; CLASTB Zresult.S, Pg, Zresult.S, Zdata.S"
    /// float32_t svclastb[_n_f32](svbool_t pg, float32_t fallback, svfloat32_t data) : "CLASTB Wtied, Pg, Wtied, Zdata.S" or "CLASTB Stied, Pg, Stied, Zdata.S"
  public static unsafe float ConditionalExtractLastActiveElement(Vector<float> mask, float defaultValues, Vector<float> data);

    /// svfloat64_t svclastb[_f64](svbool_t pg, svfloat64_t fallback, svfloat64_t data) : "CLASTB Ztied.D, Pg, Ztied.D, Zdata.D" or "MOVPRFX Zresult, Zfallback; CLASTB Zresult.D, Pg, Zresult.D, Zdata.D"
  public static unsafe Vector<double> ConditionalExtractLastActiveElement(Vector<double> mask, Vector<double> defaultValue, Vector<double> data);

    /// svfloat64_t svclastb[_f64](svbool_t pg, svfloat64_t fallback, svfloat64_t data) : "CLASTB Ztied.D, Pg, Ztied.D, Zdata.D" or "MOVPRFX Zresult, Zfallback; CLASTB Zresult.D, Pg, Zresult.D, Zdata.D"
    /// float64_t svclastb[_n_f64](svbool_t pg, float64_t fallback, svfloat64_t data) : "CLASTB Xtied, Pg, Xtied, Zdata.D" or "CLASTB Dtied, Pg, Dtied, Zdata.D"
  public static unsafe double ConditionalExtractLastActiveElement(Vector<double> mask, double defaultValues, Vector<double> data);

    /// svint8_t svclastb[_s8](svbool_t pg, svint8_t fallback, svint8_t data) : "CLASTB Ztied.B, Pg, Ztied.B, Zdata.B" or "MOVPRFX Zresult, Zfallback; CLASTB Zresult.B, Pg, Zresult.B, Zdata.B"
  public static unsafe Vector<sbyte> ConditionalExtractLastActiveElement(Vector<sbyte> mask, Vector<sbyte> defaultValue, Vector<sbyte> data);

    /// svint8_t svclastb[_s8](svbool_t pg, svint8_t fallback, svint8_t data) : "CLASTB Ztied.B, Pg, Ztied.B, Zdata.B" or "MOVPRFX Zresult, Zfallback; CLASTB Zresult.B, Pg, Zresult.B, Zdata.B"
    /// int8_t svclastb[_n_s8](svbool_t pg, int8_t fallback, svint8_t data) : "CLASTB Wtied, Pg, Wtied, Zdata.B" or "CLASTB Btied, Pg, Btied, Zdata.B"
  public static unsafe sbyte ConditionalExtractLastActiveElement(Vector<sbyte> mask, sbyte defaultValues, Vector<sbyte> data);

    /// svint16_t svclastb[_s16](svbool_t pg, svint16_t fallback, svint16_t data) : "CLASTB Ztied.H, Pg, Ztied.H, Zdata.H" or "MOVPRFX Zresult, Zfallback; CLASTB Zresult.H, Pg, Zresult.H, Zdata.H"
  public static unsafe Vector<short> ConditionalExtractLastActiveElement(Vector<short> mask, Vector<short> defaultValue, Vector<short> data);

    /// svint16_t svclastb[_s16](svbool_t pg, svint16_t fallback, svint16_t data) : "CLASTB Ztied.H, Pg, Ztied.H, Zdata.H" or "MOVPRFX Zresult, Zfallback; CLASTB Zresult.H, Pg, Zresult.H, Zdata.H"
    /// int16_t svclastb[_n_s16](svbool_t pg, int16_t fallback, svint16_t data) : "CLASTB Wtied, Pg, Wtied, Zdata.H" or "CLASTB Htied, Pg, Htied, Zdata.H"
  public static unsafe short ConditionalExtractLastActiveElement(Vector<short> mask, short defaultValues, Vector<short> data);

    /// svint32_t svclastb[_s32](svbool_t pg, svint32_t fallback, svint32_t data) : "CLASTB Ztied.S, Pg, Ztied.S, Zdata.S" or "MOVPRFX Zresult, Zfallback; CLASTB Zresult.S, Pg, Zresult.S, Zdata.S"
  public static unsafe Vector<int> ConditionalExtractLastActiveElement(Vector<int> mask, Vector<int> defaultValue, Vector<int> data);

    /// svint32_t svclastb[_s32](svbool_t pg, svint32_t fallback, svint32_t data) : "CLASTB Ztied.S, Pg, Ztied.S, Zdata.S" or "MOVPRFX Zresult, Zfallback; CLASTB Zresult.S, Pg, Zresult.S, Zdata.S"
    /// int32_t svclastb[_n_s32](svbool_t pg, int32_t fallback, svint32_t data) : "CLASTB Wtied, Pg, Wtied, Zdata.S" or "CLASTB Stied, Pg, Stied, Zdata.S"
  public static unsafe int ConditionalExtractLastActiveElement(Vector<int> mask, int defaultValues, Vector<int> data);

    /// svint64_t svclastb[_s64](svbool_t pg, svint64_t fallback, svint64_t data) : "CLASTB Ztied.D, Pg, Ztied.D, Zdata.D" or "MOVPRFX Zresult, Zfallback; CLASTB Zresult.D, Pg, Zresult.D, Zdata.D"
  public static unsafe Vector<long> ConditionalExtractLastActiveElement(Vector<long> mask, Vector<long> defaultValue, Vector<long> data);

    /// svint64_t svclastb[_s64](svbool_t pg, svint64_t fallback, svint64_t data) : "CLASTB Ztied.D, Pg, Ztied.D, Zdata.D" or "MOVPRFX Zresult, Zfallback; CLASTB Zresult.D, Pg, Zresult.D, Zdata.D"
    /// int64_t svclastb[_n_s64](svbool_t pg, int64_t fallback, svint64_t data) : "CLASTB Xtied, Pg, Xtied, Zdata.D" or "CLASTB Dtied, Pg, Dtied, Zdata.D"
  public static unsafe long ConditionalExtractLastActiveElement(Vector<long> mask, long defaultValues, Vector<long> data);

    /// svuint8_t svclastb[_u8](svbool_t pg, svuint8_t fallback, svuint8_t data) : "CLASTB Ztied.B, Pg, Ztied.B, Zdata.B" or "MOVPRFX Zresult, Zfallback; CLASTB Zresult.B, Pg, Zresult.B, Zdata.B"
  public static unsafe Vector<byte> ConditionalExtractLastActiveElement(Vector<byte> mask, Vector<byte> defaultValue, Vector<byte> data);

    /// svuint8_t svclastb[_u8](svbool_t pg, svuint8_t fallback, svuint8_t data) : "CLASTB Ztied.B, Pg, Ztied.B, Zdata.B" or "MOVPRFX Zresult, Zfallback; CLASTB Zresult.B, Pg, Zresult.B, Zdata.B"
    /// uint8_t svclastb[_n_u8](svbool_t pg, uint8_t fallback, svuint8_t data) : "CLASTB Wtied, Pg, Wtied, Zdata.B" or "CLASTB Btied, Pg, Btied, Zdata.B"
  public static unsafe byte ConditionalExtractLastActiveElement(Vector<byte> mask, byte defaultValues, Vector<byte> data);

    /// svuint16_t svclastb[_u16](svbool_t pg, svuint16_t fallback, svuint16_t data) : "CLASTB Ztied.H, Pg, Ztied.H, Zdata.H" or "MOVPRFX Zresult, Zfallback; CLASTB Zresult.H, Pg, Zresult.H, Zdata.H"
  public static unsafe Vector<ushort> ConditionalExtractLastActiveElement(Vector<ushort> mask, Vector<ushort> defaultValue, Vector<ushort> data);

    /// svuint16_t svclastb[_u16](svbool_t pg, svuint16_t fallback, svuint16_t data) : "CLASTB Ztied.H, Pg, Ztied.H, Zdata.H" or "MOVPRFX Zresult, Zfallback; CLASTB Zresult.H, Pg, Zresult.H, Zdata.H"
    /// uint16_t svclastb[_n_u16](svbool_t pg, uint16_t fallback, svuint16_t data) : "CLASTB Wtied, Pg, Wtied, Zdata.H" or "CLASTB Htied, Pg, Htied, Zdata.H"
  public static unsafe ushort ConditionalExtractLastActiveElement(Vector<ushort> mask, ushort defaultValues, Vector<ushort> data);

    /// svuint32_t svclastb[_u32](svbool_t pg, svuint32_t fallback, svuint32_t data) : "CLASTB Ztied.S, Pg, Ztied.S, Zdata.S" or "MOVPRFX Zresult, Zfallback; CLASTB Zresult.S, Pg, Zresult.S, Zdata.S"
  public static unsafe Vector<uint> ConditionalExtractLastActiveElement(Vector<uint> mask, Vector<uint> defaultValue, Vector<uint> data);

    /// svuint32_t svclastb[_u32](svbool_t pg, svuint32_t fallback, svuint32_t data) : "CLASTB Ztied.S, Pg, Ztied.S, Zdata.S" or "MOVPRFX Zresult, Zfallback; CLASTB Zresult.S, Pg, Zresult.S, Zdata.S"
    /// uint32_t svclastb[_n_u32](svbool_t pg, uint32_t fallback, svuint32_t data) : "CLASTB Wtied, Pg, Wtied, Zdata.S" or "CLASTB Stied, Pg, Stied, Zdata.S"
  public static unsafe uint ConditionalExtractLastActiveElement(Vector<uint> mask, uint defaultValues, Vector<uint> data);

    /// svuint64_t svclastb[_u64](svbool_t pg, svuint64_t fallback, svuint64_t data) : "CLASTB Ztied.D, Pg, Ztied.D, Zdata.D" or "MOVPRFX Zresult, Zfallback; CLASTB Zresult.D, Pg, Zresult.D, Zdata.D"
  public static unsafe Vector<ulong> ConditionalExtractLastActiveElement(Vector<ulong> mask, Vector<ulong> defaultValue, Vector<ulong> data);

    /// svuint64_t svclastb[_u64](svbool_t pg, svuint64_t fallback, svuint64_t data) : "CLASTB Ztied.D, Pg, Ztied.D, Zdata.D" or "MOVPRFX Zresult, Zfallback; CLASTB Zresult.D, Pg, Zresult.D, Zdata.D"
    /// uint64_t svclastb[_n_u64](svbool_t pg, uint64_t fallback, svuint64_t data) : "CLASTB Xtied, Pg, Xtied, Zdata.D" or "CLASTB Dtied, Pg, Dtied, Zdata.D"
  public static unsafe ulong ConditionalExtractLastActiveElement(Vector<ulong> mask, ulong defaultValues, Vector<ulong> data);


    /// ConditionalExtractLastActiveElementAndReplicate : Conditionally extract last element

    /// svfloat32_t svclastb[_f32](svbool_t pg, svfloat32_t fallback, svfloat32_t data) : "CLASTB Ztied.S, Pg, Ztied.S, Zdata.S" or "MOVPRFX Zresult, Zfallback; CLASTB Zresult.S, Pg, Zresult.S, Zdata.S"
  public static unsafe Vector<float> ConditionalExtractLastActiveElementAndReplicate(Vector<float> mask, Vector<float> fallback, Vector<float> data);

    /// svfloat64_t svclastb[_f64](svbool_t pg, svfloat64_t fallback, svfloat64_t data) : "CLASTB Ztied.D, Pg, Ztied.D, Zdata.D" or "MOVPRFX Zresult, Zfallback; CLASTB Zresult.D, Pg, Zresult.D, Zdata.D"
  public static unsafe Vector<double> ConditionalExtractLastActiveElementAndReplicate(Vector<double> mask, Vector<double> fallback, Vector<double> data);

    /// svint8_t svclastb[_s8](svbool_t pg, svint8_t fallback, svint8_t data) : "CLASTB Ztied.B, Pg, Ztied.B, Zdata.B" or "MOVPRFX Zresult, Zfallback; CLASTB Zresult.B, Pg, Zresult.B, Zdata.B"
  public static unsafe Vector<sbyte> ConditionalExtractLastActiveElementAndReplicate(Vector<sbyte> mask, Vector<sbyte> fallback, Vector<sbyte> data);

    /// svint16_t svclastb[_s16](svbool_t pg, svint16_t fallback, svint16_t data) : "CLASTB Ztied.H, Pg, Ztied.H, Zdata.H" or "MOVPRFX Zresult, Zfallback; CLASTB Zresult.H, Pg, Zresult.H, Zdata.H"
  public static unsafe Vector<short> ConditionalExtractLastActiveElementAndReplicate(Vector<short> mask, Vector<short> fallback, Vector<short> data);

    /// svint32_t svclastb[_s32](svbool_t pg, svint32_t fallback, svint32_t data) : "CLASTB Ztied.S, Pg, Ztied.S, Zdata.S" or "MOVPRFX Zresult, Zfallback; CLASTB Zresult.S, Pg, Zresult.S, Zdata.S"
  public static unsafe Vector<int> ConditionalExtractLastActiveElementAndReplicate(Vector<int> mask, Vector<int> fallback, Vector<int> data);

    /// svint64_t svclastb[_s64](svbool_t pg, svint64_t fallback, svint64_t data) : "CLASTB Ztied.D, Pg, Ztied.D, Zdata.D" or "MOVPRFX Zresult, Zfallback; CLASTB Zresult.D, Pg, Zresult.D, Zdata.D"
  public static unsafe Vector<long> ConditionalExtractLastActiveElementAndReplicate(Vector<long> mask, Vector<long> fallback, Vector<long> data);

    /// svuint8_t svclastb[_u8](svbool_t pg, svuint8_t fallback, svuint8_t data) : "CLASTB Ztied.B, Pg, Ztied.B, Zdata.B" or "MOVPRFX Zresult, Zfallback; CLASTB Zresult.B, Pg, Zresult.B, Zdata.B"
  public static unsafe Vector<byte> ConditionalExtractLastActiveElementAndReplicate(Vector<byte> mask, Vector<byte> fallback, Vector<byte> data);

    /// svuint16_t svclastb[_u16](svbool_t pg, svuint16_t fallback, svuint16_t data) : "CLASTB Ztied.H, Pg, Ztied.H, Zdata.H" or "MOVPRFX Zresult, Zfallback; CLASTB Zresult.H, Pg, Zresult.H, Zdata.H"
  public static unsafe Vector<ushort> ConditionalExtractLastActiveElementAndReplicate(Vector<ushort> mask, Vector<ushort> fallback, Vector<ushort> data);

    /// svuint32_t svclastb[_u32](svbool_t pg, svuint32_t fallback, svuint32_t data) : "CLASTB Ztied.S, Pg, Ztied.S, Zdata.S" or "MOVPRFX Zresult, Zfallback; CLASTB Zresult.S, Pg, Zresult.S, Zdata.S"
  public static unsafe Vector<uint> ConditionalExtractLastActiveElementAndReplicate(Vector<uint> mask, Vector<uint> fallback, Vector<uint> data);

    /// svuint64_t svclastb[_u64](svbool_t pg, svuint64_t fallback, svuint64_t data) : "CLASTB Ztied.D, Pg, Ztied.D, Zdata.D" or "MOVPRFX Zresult, Zfallback; CLASTB Zresult.D, Pg, Zresult.D, Zdata.D"
  public static unsafe Vector<ulong> ConditionalExtractLastActiveElementAndReplicate(Vector<ulong> mask, Vector<ulong> fallback, Vector<ulong> data);


    /// ConditionalSelect : Conditionally select elements

    /// svfloat32_t svsel[_f32](svbool_t pg, svfloat32_t op1, svfloat32_t op2) : "SEL Zresult.S, Pg, Zop1.S, Zop2.S"
  public static unsafe Vector<float> ConditionalSelect(Vector<float> mask, Vector<float> left, Vector<float> right);

    /// svfloat64_t svsel[_f64](svbool_t pg, svfloat64_t op1, svfloat64_t op2) : "SEL Zresult.D, Pg, Zop1.D, Zop2.D"
  public static unsafe Vector<double> ConditionalSelect(Vector<double> mask, Vector<double> left, Vector<double> right);

    /// svint8_t svsel[_s8](svbool_t pg, svint8_t op1, svint8_t op2) : "SEL Zresult.B, Pg, Zop1.B, Zop2.B"
    /// svbool_t svsel[_b](svbool_t pg, svbool_t op1, svbool_t op2) : "SEL Presult.B, Pg, Pop1.B, Pop2.B"
  public static unsafe Vector<sbyte> ConditionalSelect(Vector<sbyte> mask, Vector<sbyte> left, Vector<sbyte> right);

    /// svint16_t svsel[_s16](svbool_t pg, svint16_t op1, svint16_t op2) : "SEL Zresult.H, Pg, Zop1.H, Zop2.H"
    /// svbool_t svsel[_b](svbool_t pg, svbool_t op1, svbool_t op2) : "SEL Presult.B, Pg, Pop1.B, Pop2.B"
  public static unsafe Vector<short> ConditionalSelect(Vector<short> mask, Vector<short> left, Vector<short> right);

    /// svint32_t svsel[_s32](svbool_t pg, svint32_t op1, svint32_t op2) : "SEL Zresult.S, Pg, Zop1.S, Zop2.S"
    /// svbool_t svsel[_b](svbool_t pg, svbool_t op1, svbool_t op2) : "SEL Presult.B, Pg, Pop1.B, Pop2.B"
  public static unsafe Vector<int> ConditionalSelect(Vector<int> mask, Vector<int> left, Vector<int> right);

    /// svint64_t svsel[_s64](svbool_t pg, svint64_t op1, svint64_t op2) : "SEL Zresult.D, Pg, Zop1.D, Zop2.D"
    /// svbool_t svsel[_b](svbool_t pg, svbool_t op1, svbool_t op2) : "SEL Presult.B, Pg, Pop1.B, Pop2.B"
  public static unsafe Vector<long> ConditionalSelect(Vector<long> mask, Vector<long> left, Vector<long> right);

    /// svuint8_t svsel[_u8](svbool_t pg, svuint8_t op1, svuint8_t op2) : "SEL Zresult.B, Pg, Zop1.B, Zop2.B"
    /// svbool_t svsel[_b](svbool_t pg, svbool_t op1, svbool_t op2) : "SEL Presult.B, Pg, Pop1.B, Pop2.B"
  public static unsafe Vector<byte> ConditionalSelect(Vector<byte> mask, Vector<byte> left, Vector<byte> right);

    /// svuint16_t svsel[_u16](svbool_t pg, svuint16_t op1, svuint16_t op2) : "SEL Zresult.H, Pg, Zop1.H, Zop2.H"
    /// svbool_t svsel[_b](svbool_t pg, svbool_t op1, svbool_t op2) : "SEL Presult.B, Pg, Pop1.B, Pop2.B"
  public static unsafe Vector<ushort> ConditionalSelect(Vector<ushort> mask, Vector<ushort> left, Vector<ushort> right);

    /// svuint32_t svsel[_u32](svbool_t pg, svuint32_t op1, svuint32_t op2) : "SEL Zresult.S, Pg, Zop1.S, Zop2.S"
    /// svbool_t svsel[_b](svbool_t pg, svbool_t op1, svbool_t op2) : "SEL Presult.B, Pg, Pop1.B, Pop2.B"
  public static unsafe Vector<uint> ConditionalSelect(Vector<uint> mask, Vector<uint> left, Vector<uint> right);

    /// svuint64_t svsel[_u64](svbool_t pg, svuint64_t op1, svuint64_t op2) : "SEL Zresult.D, Pg, Zop1.D, Zop2.D"
    /// svbool_t svsel[_b](svbool_t pg, svbool_t op1, svbool_t op2) : "SEL Presult.B, Pg, Pop1.B, Pop2.B"
  public static unsafe Vector<ulong> ConditionalSelect(Vector<ulong> mask, Vector<ulong> left, Vector<ulong> right);


    /// CreateBreakAfterMask : Break after first true condition

    /// svbool_t svbrka[_b]_m(svbool_t inactive, svbool_t pg, svbool_t op) : "BRKA Ptied.B, Pg/M, Pop.B"
    /// svbool_t svbrka[_b]_z(svbool_t pg, svbool_t op) : "BRKA Presult.B, Pg/Z, Pop.B"
  public static unsafe Vector<sbyte> CreateBreakAfterMask(Vector<sbyte> totalMask, Vector<sbyte> fromMask);

    /// svbool_t svbrka[_b]_m(svbool_t inactive, svbool_t pg, svbool_t op) : "BRKA Ptied.B, Pg/M, Pop.B"
    /// svbool_t svbrka[_b]_z(svbool_t pg, svbool_t op) : "BRKA Presult.B, Pg/Z, Pop.B"
  public static unsafe Vector<short> CreateBreakAfterMask(Vector<short> totalMask, Vector<short> fromMask);

    /// svbool_t svbrka[_b]_m(svbool_t inactive, svbool_t pg, svbool_t op) : "BRKA Ptied.B, Pg/M, Pop.B"
    /// svbool_t svbrka[_b]_z(svbool_t pg, svbool_t op) : "BRKA Presult.B, Pg/Z, Pop.B"
  public static unsafe Vector<int> CreateBreakAfterMask(Vector<int> totalMask, Vector<int> fromMask);

    /// svbool_t svbrka[_b]_m(svbool_t inactive, svbool_t pg, svbool_t op) : "BRKA Ptied.B, Pg/M, Pop.B"
    /// svbool_t svbrka[_b]_z(svbool_t pg, svbool_t op) : "BRKA Presult.B, Pg/Z, Pop.B"
  public static unsafe Vector<long> CreateBreakAfterMask(Vector<long> totalMask, Vector<long> fromMask);

    /// svbool_t svbrka[_b]_m(svbool_t inactive, svbool_t pg, svbool_t op) : "BRKA Ptied.B, Pg/M, Pop.B"
    /// svbool_t svbrka[_b]_z(svbool_t pg, svbool_t op) : "BRKA Presult.B, Pg/Z, Pop.B"
  public static unsafe Vector<byte> CreateBreakAfterMask(Vector<byte> totalMask, Vector<byte> fromMask);

    /// svbool_t svbrka[_b]_m(svbool_t inactive, svbool_t pg, svbool_t op) : "BRKA Ptied.B, Pg/M, Pop.B"
    /// svbool_t svbrka[_b]_z(svbool_t pg, svbool_t op) : "BRKA Presult.B, Pg/Z, Pop.B"
  public static unsafe Vector<ushort> CreateBreakAfterMask(Vector<ushort> totalMask, Vector<ushort> fromMask);

    /// svbool_t svbrka[_b]_m(svbool_t inactive, svbool_t pg, svbool_t op) : "BRKA Ptied.B, Pg/M, Pop.B"
    /// svbool_t svbrka[_b]_z(svbool_t pg, svbool_t op) : "BRKA Presult.B, Pg/Z, Pop.B"
  public static unsafe Vector<uint> CreateBreakAfterMask(Vector<uint> totalMask, Vector<uint> fromMask);

    /// svbool_t svbrka[_b]_m(svbool_t inactive, svbool_t pg, svbool_t op) : "BRKA Ptied.B, Pg/M, Pop.B"
    /// svbool_t svbrka[_b]_z(svbool_t pg, svbool_t op) : "BRKA Presult.B, Pg/Z, Pop.B"
  public static unsafe Vector<ulong> CreateBreakAfterMask(Vector<ulong> totalMask, Vector<ulong> fromMask);


    /// CreateBreakAfterPropagateMask : Break after first true condition, propagating from previous partition

    /// svbool_t svbrkpa[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2) : "BRKPA Presult.B, Pg/Z, Pop1.B, Pop2.B"
  public static unsafe Vector<sbyte> CreateBreakAfterPropagateMask(Vector<sbyte> mask, Vector<sbyte> left, Vector<sbyte> right);

    /// svbool_t svbrkpa[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2) : "BRKPA Presult.B, Pg/Z, Pop1.B, Pop2.B"
  public static unsafe Vector<short> CreateBreakAfterPropagateMask(Vector<short> mask, Vector<short> left, Vector<short> right);

    /// svbool_t svbrkpa[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2) : "BRKPA Presult.B, Pg/Z, Pop1.B, Pop2.B"
  public static unsafe Vector<int> CreateBreakAfterPropagateMask(Vector<int> mask, Vector<int> left, Vector<int> right);

    /// svbool_t svbrkpa[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2) : "BRKPA Presult.B, Pg/Z, Pop1.B, Pop2.B"
  public static unsafe Vector<long> CreateBreakAfterPropagateMask(Vector<long> mask, Vector<long> left, Vector<long> right);

    /// svbool_t svbrkpa[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2) : "BRKPA Presult.B, Pg/Z, Pop1.B, Pop2.B"
  public static unsafe Vector<byte> CreateBreakAfterPropagateMask(Vector<byte> mask, Vector<byte> left, Vector<byte> right);

    /// svbool_t svbrkpa[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2) : "BRKPA Presult.B, Pg/Z, Pop1.B, Pop2.B"
  public static unsafe Vector<ushort> CreateBreakAfterPropagateMask(Vector<ushort> mask, Vector<ushort> left, Vector<ushort> right);

    /// svbool_t svbrkpa[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2) : "BRKPA Presult.B, Pg/Z, Pop1.B, Pop2.B"
  public static unsafe Vector<uint> CreateBreakAfterPropagateMask(Vector<uint> mask, Vector<uint> left, Vector<uint> right);

    /// svbool_t svbrkpa[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2) : "BRKPA Presult.B, Pg/Z, Pop1.B, Pop2.B"
  public static unsafe Vector<ulong> CreateBreakAfterPropagateMask(Vector<ulong> mask, Vector<ulong> left, Vector<ulong> right);


    /// CreateBreakBeforeMask : Break before first true condition

    /// svbool_t svbrkb[_b]_m(svbool_t inactive, svbool_t pg, svbool_t op) : "BRKB Ptied.B, Pg/M, Pop.B"
    /// svbool_t svbrkb[_b]_z(svbool_t pg, svbool_t op) : "BRKB Presult.B, Pg/Z, Pop.B"
  public static unsafe Vector<sbyte> CreateBreakBeforeMask(Vector<sbyte> totalMask, Vector<sbyte> fromMask);

    /// svbool_t svbrkb[_b]_m(svbool_t inactive, svbool_t pg, svbool_t op) : "BRKB Ptied.B, Pg/M, Pop.B"
    /// svbool_t svbrkb[_b]_z(svbool_t pg, svbool_t op) : "BRKB Presult.B, Pg/Z, Pop.B"
  public static unsafe Vector<short> CreateBreakBeforeMask(Vector<short> totalMask, Vector<short> fromMask);

    /// svbool_t svbrkb[_b]_m(svbool_t inactive, svbool_t pg, svbool_t op) : "BRKB Ptied.B, Pg/M, Pop.B"
    /// svbool_t svbrkb[_b]_z(svbool_t pg, svbool_t op) : "BRKB Presult.B, Pg/Z, Pop.B"
  public static unsafe Vector<int> CreateBreakBeforeMask(Vector<int> totalMask, Vector<int> fromMask);

    /// svbool_t svbrkb[_b]_m(svbool_t inactive, svbool_t pg, svbool_t op) : "BRKB Ptied.B, Pg/M, Pop.B"
    /// svbool_t svbrkb[_b]_z(svbool_t pg, svbool_t op) : "BRKB Presult.B, Pg/Z, Pop.B"
  public static unsafe Vector<long> CreateBreakBeforeMask(Vector<long> totalMask, Vector<long> fromMask);

    /// svbool_t svbrkb[_b]_m(svbool_t inactive, svbool_t pg, svbool_t op) : "BRKB Ptied.B, Pg/M, Pop.B"
    /// svbool_t svbrkb[_b]_z(svbool_t pg, svbool_t op) : "BRKB Presult.B, Pg/Z, Pop.B"
  public static unsafe Vector<byte> CreateBreakBeforeMask(Vector<byte> totalMask, Vector<byte> fromMask);

    /// svbool_t svbrkb[_b]_m(svbool_t inactive, svbool_t pg, svbool_t op) : "BRKB Ptied.B, Pg/M, Pop.B"
    /// svbool_t svbrkb[_b]_z(svbool_t pg, svbool_t op) : "BRKB Presult.B, Pg/Z, Pop.B"
  public static unsafe Vector<ushort> CreateBreakBeforeMask(Vector<ushort> totalMask, Vector<ushort> fromMask);

    /// svbool_t svbrkb[_b]_m(svbool_t inactive, svbool_t pg, svbool_t op) : "BRKB Ptied.B, Pg/M, Pop.B"
    /// svbool_t svbrkb[_b]_z(svbool_t pg, svbool_t op) : "BRKB Presult.B, Pg/Z, Pop.B"
  public static unsafe Vector<uint> CreateBreakBeforeMask(Vector<uint> totalMask, Vector<uint> fromMask);

    /// svbool_t svbrkb[_b]_m(svbool_t inactive, svbool_t pg, svbool_t op) : "BRKB Ptied.B, Pg/M, Pop.B"
    /// svbool_t svbrkb[_b]_z(svbool_t pg, svbool_t op) : "BRKB Presult.B, Pg/Z, Pop.B"
  public static unsafe Vector<ulong> CreateBreakBeforeMask(Vector<ulong> totalMask, Vector<ulong> fromMask);


    /// CreateBreakBeforePropagateMask : Break before first true condition, propagating from previous partition

    /// svbool_t svbrkpb[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2) : "BRKPB Presult.B, Pg/Z, Pop1.B, Pop2.B"
  public static unsafe Vector<sbyte> CreateBreakBeforePropagateMask(Vector<sbyte> mask, Vector<sbyte> left, Vector<sbyte> right);

    /// svbool_t svbrkpb[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2) : "BRKPB Presult.B, Pg/Z, Pop1.B, Pop2.B"
  public static unsafe Vector<short> CreateBreakBeforePropagateMask(Vector<short> mask, Vector<short> left, Vector<short> right);

    /// svbool_t svbrkpb[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2) : "BRKPB Presult.B, Pg/Z, Pop1.B, Pop2.B"
  public static unsafe Vector<int> CreateBreakBeforePropagateMask(Vector<int> mask, Vector<int> left, Vector<int> right);

    /// svbool_t svbrkpb[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2) : "BRKPB Presult.B, Pg/Z, Pop1.B, Pop2.B"
  public static unsafe Vector<long> CreateBreakBeforePropagateMask(Vector<long> mask, Vector<long> left, Vector<long> right);

    /// svbool_t svbrkpb[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2) : "BRKPB Presult.B, Pg/Z, Pop1.B, Pop2.B"
  public static unsafe Vector<byte> CreateBreakBeforePropagateMask(Vector<byte> mask, Vector<byte> left, Vector<byte> right);

    /// svbool_t svbrkpb[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2) : "BRKPB Presult.B, Pg/Z, Pop1.B, Pop2.B"
  public static unsafe Vector<ushort> CreateBreakBeforePropagateMask(Vector<ushort> mask, Vector<ushort> left, Vector<ushort> right);

    /// svbool_t svbrkpb[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2) : "BRKPB Presult.B, Pg/Z, Pop1.B, Pop2.B"
  public static unsafe Vector<uint> CreateBreakBeforePropagateMask(Vector<uint> mask, Vector<uint> left, Vector<uint> right);

    /// svbool_t svbrkpb[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2) : "BRKPB Presult.B, Pg/Z, Pop1.B, Pop2.B"
  public static unsafe Vector<ulong> CreateBreakBeforePropagateMask(Vector<ulong> mask, Vector<ulong> left, Vector<ulong> right);


    /// CreateBreakPropagateMask : Propagate break to next partition

    /// svbool_t svbrkn[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2) : "BRKN Ptied2.B, Pg/Z, Pop1.B, Ptied2.B"
  public static unsafe Vector<sbyte> CreateBreakPropagateMask(Vector<sbyte> totalMask, Vector<sbyte> fromMask);

    /// svbool_t svbrkn[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2) : "BRKN Ptied2.B, Pg/Z, Pop1.B, Ptied2.B"
  public static unsafe Vector<short> CreateBreakPropagateMask(Vector<short> totalMask, Vector<short> fromMask);

    /// svbool_t svbrkn[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2) : "BRKN Ptied2.B, Pg/Z, Pop1.B, Ptied2.B"
  public static unsafe Vector<int> CreateBreakPropagateMask(Vector<int> totalMask, Vector<int> fromMask);

    /// svbool_t svbrkn[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2) : "BRKN Ptied2.B, Pg/Z, Pop1.B, Ptied2.B"
  public static unsafe Vector<long> CreateBreakPropagateMask(Vector<long> totalMask, Vector<long> fromMask);

    /// svbool_t svbrkn[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2) : "BRKN Ptied2.B, Pg/Z, Pop1.B, Ptied2.B"
  public static unsafe Vector<byte> CreateBreakPropagateMask(Vector<byte> totalMask, Vector<byte> fromMask);

    /// svbool_t svbrkn[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2) : "BRKN Ptied2.B, Pg/Z, Pop1.B, Ptied2.B"
  public static unsafe Vector<ushort> CreateBreakPropagateMask(Vector<ushort> totalMask, Vector<ushort> fromMask);

    /// svbool_t svbrkn[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2) : "BRKN Ptied2.B, Pg/Z, Pop1.B, Ptied2.B"
  public static unsafe Vector<uint> CreateBreakPropagateMask(Vector<uint> totalMask, Vector<uint> fromMask);

    /// svbool_t svbrkn[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2) : "BRKN Ptied2.B, Pg/Z, Pop1.B, Ptied2.B"
  public static unsafe Vector<ulong> CreateBreakPropagateMask(Vector<ulong> totalMask, Vector<ulong> fromMask);


    /// CreateFalseMaskByte : Set all predicate elements to false

    /// svbool_t svpfalse[_b]() : "PFALSE Presult.B"
  public static unsafe Vector<byte> CreateFalseMaskByte();


    /// CreateFalseMaskDouble : Set all predicate elements to false

    /// svbool_t svpfalse[_b]() : "PFALSE Presult.B"
  public static unsafe Vector<double> CreateFalseMaskDouble();


    /// CreateFalseMaskInt16 : Set all predicate elements to false

    /// svbool_t svpfalse[_b]() : "PFALSE Presult.B"
  public static unsafe Vector<short> CreateFalseMaskInt16();


    /// CreateFalseMaskInt32 : Set all predicate elements to false

    /// svbool_t svpfalse[_b]() : "PFALSE Presult.B"
  public static unsafe Vector<int> CreateFalseMaskInt32();


    /// CreateFalseMaskInt64 : Set all predicate elements to false

    /// svbool_t svpfalse[_b]() : "PFALSE Presult.B"
  public static unsafe Vector<long> CreateFalseMaskInt64();


    /// CreateFalseMaskSByte : Set all predicate elements to false

    /// svbool_t svpfalse[_b]() : "PFALSE Presult.B"
  public static unsafe Vector<sbyte> CreateFalseMaskSByte();


    /// CreateFalseMaskSingle : Set all predicate elements to false

    /// svbool_t svpfalse[_b]() : "PFALSE Presult.B"
  public static unsafe Vector<float> CreateFalseMaskSingle();


    /// CreateFalseMaskUInt16 : Set all predicate elements to false

    /// svbool_t svpfalse[_b]() : "PFALSE Presult.B"
  public static unsafe Vector<ushort> CreateFalseMaskUInt16();


    /// CreateFalseMaskUInt32 : Set all predicate elements to false

    /// svbool_t svpfalse[_b]() : "PFALSE Presult.B"
  public static unsafe Vector<uint> CreateFalseMaskUInt32();


    /// CreateFalseMaskUInt64 : Set all predicate elements to false

    /// svbool_t svpfalse[_b]() : "PFALSE Presult.B"
  public static unsafe Vector<ulong> CreateFalseMaskUInt64();


    /// CreateMaskForFirstActiveElement : Set the first active predicate element to true

    /// svbool_t svpfirst[_b](svbool_t pg, svbool_t op) : "PFIRST Ptied.B, Pg, Ptied.B"
  public static unsafe Vector<sbyte> CreateMaskForFirstActiveElement(Vector<sbyte> totalMask, Vector<sbyte> fromMask);

    /// svbool_t svpfirst[_b](svbool_t pg, svbool_t op) : "PFIRST Ptied.B, Pg, Ptied.B"
  public static unsafe Vector<short> CreateMaskForFirstActiveElement(Vector<short> totalMask, Vector<short> fromMask);

    /// svbool_t svpfirst[_b](svbool_t pg, svbool_t op) : "PFIRST Ptied.B, Pg, Ptied.B"
  public static unsafe Vector<int> CreateMaskForFirstActiveElement(Vector<int> totalMask, Vector<int> fromMask);

    /// svbool_t svpfirst[_b](svbool_t pg, svbool_t op) : "PFIRST Ptied.B, Pg, Ptied.B"
  public static unsafe Vector<long> CreateMaskForFirstActiveElement(Vector<long> totalMask, Vector<long> fromMask);

    /// svbool_t svpfirst[_b](svbool_t pg, svbool_t op) : "PFIRST Ptied.B, Pg, Ptied.B"
  public static unsafe Vector<byte> CreateMaskForFirstActiveElement(Vector<byte> totalMask, Vector<byte> fromMask);

    /// svbool_t svpfirst[_b](svbool_t pg, svbool_t op) : "PFIRST Ptied.B, Pg, Ptied.B"
  public static unsafe Vector<ushort> CreateMaskForFirstActiveElement(Vector<ushort> totalMask, Vector<ushort> fromMask);

    /// svbool_t svpfirst[_b](svbool_t pg, svbool_t op) : "PFIRST Ptied.B, Pg, Ptied.B"
  public static unsafe Vector<uint> CreateMaskForFirstActiveElement(Vector<uint> totalMask, Vector<uint> fromMask);

    /// svbool_t svpfirst[_b](svbool_t pg, svbool_t op) : "PFIRST Ptied.B, Pg, Ptied.B"
  public static unsafe Vector<ulong> CreateMaskForFirstActiveElement(Vector<ulong> totalMask, Vector<ulong> fromMask);


    /// CreateMaskForNextActiveElement : Find next active predicate

    /// svbool_t svpnext_b8(svbool_t pg, svbool_t op) : "PNEXT Ptied.B, Pg, Ptied.B"
  public static unsafe Vector<byte> CreateMaskForNextActiveElement(Vector<byte> totalMask, Vector<byte> fromMask);

    /// svbool_t svpnext_b16(svbool_t pg, svbool_t op) : "PNEXT Ptied.H, Pg, Ptied.H"
  public static unsafe Vector<ushort> CreateMaskForNextActiveElement(Vector<ushort> totalMask, Vector<ushort> fromMask);

    /// svbool_t svpnext_b32(svbool_t pg, svbool_t op) : "PNEXT Ptied.S, Pg, Ptied.S"
  public static unsafe Vector<uint> CreateMaskForNextActiveElement(Vector<uint> totalMask, Vector<uint> fromMask);

    /// svbool_t svpnext_b64(svbool_t pg, svbool_t op) : "PNEXT Ptied.D, Pg, Ptied.D"
  public static unsafe Vector<ulong> CreateMaskForNextActiveElement(Vector<ulong> totalMask, Vector<ulong> fromMask);


    /// CreateTrueMaskByte : Set predicate elements to true

    /// svbool_t svptrue_pat_b8(enum svpattern pattern) : "PTRUE Presult.B, pattern"
  public static unsafe Vector<byte> CreateTrueMaskByte([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All);


    /// CreateTrueMaskDouble : Set predicate elements to true

    /// svbool_t svptrue_pat_b8(enum svpattern pattern) : "PTRUE Presult.B, pattern"
  public static unsafe Vector<double> CreateTrueMaskDouble([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All);


    /// CreateTrueMaskInt16 : Set predicate elements to true

    /// svbool_t svptrue_pat_b8(enum svpattern pattern) : "PTRUE Presult.B, pattern"
  public static unsafe Vector<short> CreateTrueMaskInt16([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All);


    /// CreateTrueMaskInt32 : Set predicate elements to true

    /// svbool_t svptrue_pat_b8(enum svpattern pattern) : "PTRUE Presult.B, pattern"
  public static unsafe Vector<int> CreateTrueMaskInt32([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All);


    /// CreateTrueMaskInt64 : Set predicate elements to true

    /// svbool_t svptrue_pat_b8(enum svpattern pattern) : "PTRUE Presult.B, pattern"
  public static unsafe Vector<long> CreateTrueMaskInt64([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All);


    /// CreateTrueMaskSByte : Set predicate elements to true

    /// svbool_t svptrue_pat_b8(enum svpattern pattern) : "PTRUE Presult.B, pattern"
  public static unsafe Vector<sbyte> CreateTrueMaskSByte([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All);


    /// CreateTrueMaskSingle : Set predicate elements to true

    /// svbool_t svptrue_pat_b8(enum svpattern pattern) : "PTRUE Presult.B, pattern"
  public static unsafe Vector<float> CreateTrueMaskSingle([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All);


    /// CreateTrueMaskUInt16 : Set predicate elements to true

    /// svbool_t svptrue_pat_b16(enum svpattern pattern) : "PTRUE Presult.H, pattern"
  public static unsafe Vector<ushort> CreateTrueMaskUInt16([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All);


    /// CreateTrueMaskUInt32 : Set predicate elements to true

    /// svbool_t svptrue_pat_b32(enum svpattern pattern) : "PTRUE Presult.S, pattern"
  public static unsafe Vector<uint> CreateTrueMaskUInt32([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All);


    /// CreateTrueMaskUInt64 : Set predicate elements to true

    /// svbool_t svptrue_pat_b64(enum svpattern pattern) : "PTRUE Presult.D, pattern"
  public static unsafe Vector<ulong> CreateTrueMaskUInt64([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All);


    /// CreateWhileLessThanMask16Bit : While incrementing scalar is less than

    /// svbool_t svwhilelt_b16[_s32](int32_t op1, int32_t op2) : "WHILELT Presult.H, Wop1, Wop2"
  public static unsafe Vector<ushort> CreateWhileLessThanMask16Bit(int left, int right);

    /// svbool_t svwhilelt_b16[_s64](int64_t op1, int64_t op2) : "WHILELT Presult.H, Xop1, Xop2"
  public static unsafe Vector<ushort> CreateWhileLessThanMask16Bit(long left, long right);

    /// svbool_t svwhilelt_b16[_u32](uint32_t op1, uint32_t op2) : "WHILELO Presult.H, Wop1, Wop2"
  public static unsafe Vector<ushort> CreateWhileLessThanMask16Bit(uint left, uint right);

    /// svbool_t svwhilelt_b16[_u64](uint64_t op1, uint64_t op2) : "WHILELO Presult.H, Xop1, Xop2"
  public static unsafe Vector<ushort> CreateWhileLessThanMask16Bit(ulong left, ulong right);


    /// CreateWhileLessThanMask32Bit : While incrementing scalar is less than

    /// svbool_t svwhilelt_b32[_s32](int32_t op1, int32_t op2) : "WHILELT Presult.S, Wop1, Wop2"
  public static unsafe Vector<uint> CreateWhileLessThanMask32Bit(int left, int right);

    /// svbool_t svwhilelt_b32[_s64](int64_t op1, int64_t op2) : "WHILELT Presult.S, Xop1, Xop2"
  public static unsafe Vector<uint> CreateWhileLessThanMask32Bit(long left, long right);

    /// svbool_t svwhilelt_b32[_u32](uint32_t op1, uint32_t op2) : "WHILELO Presult.S, Wop1, Wop2"
  public static unsafe Vector<uint> CreateWhileLessThanMask32Bit(uint left, uint right);

    /// svbool_t svwhilelt_b32[_u64](uint64_t op1, uint64_t op2) : "WHILELO Presult.S, Xop1, Xop2"
  public static unsafe Vector<uint> CreateWhileLessThanMask32Bit(ulong left, ulong right);


    /// CreateWhileLessThanMask64Bit : While incrementing scalar is less than

    /// svbool_t svwhilelt_b64[_s32](int32_t op1, int32_t op2) : "WHILELT Presult.D, Wop1, Wop2"
  public static unsafe Vector<ulong> CreateWhileLessThanMask64Bit(int left, int right);

    /// svbool_t svwhilelt_b64[_s64](int64_t op1, int64_t op2) : "WHILELT Presult.D, Xop1, Xop2"
  public static unsafe Vector<ulong> CreateWhileLessThanMask64Bit(long left, long right);

    /// svbool_t svwhilelt_b64[_u32](uint32_t op1, uint32_t op2) : "WHILELO Presult.D, Wop1, Wop2"
  public static unsafe Vector<ulong> CreateWhileLessThanMask64Bit(uint left, uint right);

    /// svbool_t svwhilelt_b64[_u64](uint64_t op1, uint64_t op2) : "WHILELO Presult.D, Xop1, Xop2"
  public static unsafe Vector<ulong> CreateWhileLessThanMask64Bit(ulong left, ulong right);


    /// CreateWhileLessThanMask8Bit : While incrementing scalar is less than

    /// svbool_t svwhilelt_b8[_s32](int32_t op1, int32_t op2) : "WHILELT Presult.B, Wop1, Wop2"
  public static unsafe Vector<byte> CreateWhileLessThanMask8Bit(int left, int right);

    /// svbool_t svwhilelt_b8[_s64](int64_t op1, int64_t op2) : "WHILELT Presult.B, Xop1, Xop2"
  public static unsafe Vector<byte> CreateWhileLessThanMask8Bit(long left, long right);

    /// svbool_t svwhilelt_b8[_u32](uint32_t op1, uint32_t op2) : "WHILELO Presult.B, Wop1, Wop2"
  public static unsafe Vector<byte> CreateWhileLessThanMask8Bit(uint left, uint right);

    /// svbool_t svwhilelt_b8[_u64](uint64_t op1, uint64_t op2) : "WHILELO Presult.B, Xop1, Xop2"
  public static unsafe Vector<byte> CreateWhileLessThanMask8Bit(ulong left, ulong right);


    /// CreateWhileLessThanOrEqualMask16Bit : While incrementing scalar is less than or equal to

    /// svbool_t svwhilele_b16[_s32](int32_t op1, int32_t op2) : "WHILELE Presult.H, Wop1, Wop2"
  public static unsafe Vector<ushort> CreateWhileLessThanOrEqualMask16Bit(int left, int right);

    /// svbool_t svwhilele_b16[_s64](int64_t op1, int64_t op2) : "WHILELE Presult.H, Xop1, Xop2"
  public static unsafe Vector<ushort> CreateWhileLessThanOrEqualMask16Bit(long left, long right);

    /// svbool_t svwhilele_b16[_u32](uint32_t op1, uint32_t op2) : "WHILELS Presult.H, Wop1, Wop2"
  public static unsafe Vector<ushort> CreateWhileLessThanOrEqualMask16Bit(uint left, uint right);

    /// svbool_t svwhilele_b16[_u64](uint64_t op1, uint64_t op2) : "WHILELS Presult.H, Xop1, Xop2"
  public static unsafe Vector<ushort> CreateWhileLessThanOrEqualMask16Bit(ulong left, ulong right);


    /// CreateWhileLessThanOrEqualMask32Bit : While incrementing scalar is less than or equal to

    /// svbool_t svwhilele_b32[_s32](int32_t op1, int32_t op2) : "WHILELE Presult.S, Wop1, Wop2"
  public static unsafe Vector<uint> CreateWhileLessThanOrEqualMask32Bit(int left, int right);

    /// svbool_t svwhilele_b32[_s64](int64_t op1, int64_t op2) : "WHILELE Presult.S, Xop1, Xop2"
  public static unsafe Vector<uint> CreateWhileLessThanOrEqualMask32Bit(long left, long right);

    /// svbool_t svwhilele_b32[_u32](uint32_t op1, uint32_t op2) : "WHILELS Presult.S, Wop1, Wop2"
  public static unsafe Vector<uint> CreateWhileLessThanOrEqualMask32Bit(uint left, uint right);

    /// svbool_t svwhilele_b32[_u64](uint64_t op1, uint64_t op2) : "WHILELS Presult.S, Xop1, Xop2"
  public static unsafe Vector<uint> CreateWhileLessThanOrEqualMask32Bit(ulong left, ulong right);


    /// CreateWhileLessThanOrEqualMask64Bit : While incrementing scalar is less than or equal to

    /// svbool_t svwhilele_b64[_s32](int32_t op1, int32_t op2) : "WHILELE Presult.D, Wop1, Wop2"
  public static unsafe Vector<ulong> CreateWhileLessThanOrEqualMask64Bit(int left, int right);

    /// svbool_t svwhilele_b64[_s64](int64_t op1, int64_t op2) : "WHILELE Presult.D, Xop1, Xop2"
  public static unsafe Vector<ulong> CreateWhileLessThanOrEqualMask64Bit(long left, long right);

    /// svbool_t svwhilele_b64[_u32](uint32_t op1, uint32_t op2) : "WHILELS Presult.D, Wop1, Wop2"
  public static unsafe Vector<ulong> CreateWhileLessThanOrEqualMask64Bit(uint left, uint right);

    /// svbool_t svwhilele_b64[_u64](uint64_t op1, uint64_t op2) : "WHILELS Presult.D, Xop1, Xop2"
  public static unsafe Vector<ulong> CreateWhileLessThanOrEqualMask64Bit(ulong left, ulong right);


    /// CreateWhileLessThanOrEqualMask8Bit : While incrementing scalar is less than or equal to

    /// svbool_t svwhilele_b8[_s32](int32_t op1, int32_t op2) : "WHILELE Presult.B, Wop1, Wop2"
  public static unsafe Vector<byte> CreateWhileLessThanOrEqualMask8Bit(int left, int right);

    /// svbool_t svwhilele_b8[_s64](int64_t op1, int64_t op2) : "WHILELE Presult.B, Xop1, Xop2"
  public static unsafe Vector<byte> CreateWhileLessThanOrEqualMask8Bit(long left, long right);

    /// svbool_t svwhilele_b8[_u32](uint32_t op1, uint32_t op2) : "WHILELS Presult.B, Wop1, Wop2"
  public static unsafe Vector<byte> CreateWhileLessThanOrEqualMask8Bit(uint left, uint right);

    /// svbool_t svwhilele_b8[_u64](uint64_t op1, uint64_t op2) : "WHILELS Presult.B, Xop1, Xop2"
  public static unsafe Vector<byte> CreateWhileLessThanOrEqualMask8Bit(ulong left, ulong right);


    /// ExtractAfterLastScalar : Extract element after last

    /// float32_t svlasta[_f32](svbool_t pg, svfloat32_t op) : "LASTA Wresult, Pg, Zop.S" or "LASTA Sresult, Pg, Zop.S"
  public static unsafe float ExtractAfterLastScalar(Vector<float> value);

    /// float64_t svlasta[_f64](svbool_t pg, svfloat64_t op) : "LASTA Xresult, Pg, Zop.D" or "LASTA Dresult, Pg, Zop.D"
  public static unsafe double ExtractAfterLastScalar(Vector<double> value);

    /// int8_t svlasta[_s8](svbool_t pg, svint8_t op) : "LASTA Wresult, Pg, Zop.B" or "LASTA Bresult, Pg, Zop.B"
  public static unsafe sbyte ExtractAfterLastScalar(Vector<sbyte> value);

    /// int16_t svlasta[_s16](svbool_t pg, svint16_t op) : "LASTA Wresult, Pg, Zop.H" or "LASTA Hresult, Pg, Zop.H"
  public static unsafe short ExtractAfterLastScalar(Vector<short> value);

    /// int32_t svlasta[_s32](svbool_t pg, svint32_t op) : "LASTA Wresult, Pg, Zop.S" or "LASTA Sresult, Pg, Zop.S"
  public static unsafe int ExtractAfterLastScalar(Vector<int> value);

    /// int64_t svlasta[_s64](svbool_t pg, svint64_t op) : "LASTA Xresult, Pg, Zop.D" or "LASTA Dresult, Pg, Zop.D"
  public static unsafe long ExtractAfterLastScalar(Vector<long> value);

    /// uint8_t svlasta[_u8](svbool_t pg, svuint8_t op) : "LASTA Wresult, Pg, Zop.B" or "LASTA Bresult, Pg, Zop.B"
  public static unsafe byte ExtractAfterLastScalar(Vector<byte> value);

    /// uint16_t svlasta[_u16](svbool_t pg, svuint16_t op) : "LASTA Wresult, Pg, Zop.H" or "LASTA Hresult, Pg, Zop.H"
  public static unsafe ushort ExtractAfterLastScalar(Vector<ushort> value);

    /// uint32_t svlasta[_u32](svbool_t pg, svuint32_t op) : "LASTA Wresult, Pg, Zop.S" or "LASTA Sresult, Pg, Zop.S"
  public static unsafe uint ExtractAfterLastScalar(Vector<uint> value);

    /// uint64_t svlasta[_u64](svbool_t pg, svuint64_t op) : "LASTA Xresult, Pg, Zop.D" or "LASTA Dresult, Pg, Zop.D"
  public static unsafe ulong ExtractAfterLastScalar(Vector<ulong> value);


    /// ExtractAfterLastVector : Extract element after last

    /// float32_t svlasta[_f32](svbool_t pg, svfloat32_t op) : "LASTA Wresult, Pg, Zop.S" or "LASTA Sresult, Pg, Zop.S"
  public static unsafe Vector<float> ExtractAfterLastVector(Vector<float> value);

    /// float64_t svlasta[_f64](svbool_t pg, svfloat64_t op) : "LASTA Xresult, Pg, Zop.D" or "LASTA Dresult, Pg, Zop.D"
  public static unsafe Vector<double> ExtractAfterLastVector(Vector<double> value);

    /// int8_t svlasta[_s8](svbool_t pg, svint8_t op) : "LASTA Wresult, Pg, Zop.B" or "LASTA Bresult, Pg, Zop.B"
  public static unsafe Vector<sbyte> ExtractAfterLastVector(Vector<sbyte> value);

    /// int16_t svlasta[_s16](svbool_t pg, svint16_t op) : "LASTA Wresult, Pg, Zop.H" or "LASTA Hresult, Pg, Zop.H"
  public static unsafe Vector<short> ExtractAfterLastVector(Vector<short> value);

    /// int32_t svlasta[_s32](svbool_t pg, svint32_t op) : "LASTA Wresult, Pg, Zop.S" or "LASTA Sresult, Pg, Zop.S"
  public static unsafe Vector<int> ExtractAfterLastVector(Vector<int> value);

    /// int64_t svlasta[_s64](svbool_t pg, svint64_t op) : "LASTA Xresult, Pg, Zop.D" or "LASTA Dresult, Pg, Zop.D"
  public static unsafe Vector<long> ExtractAfterLastVector(Vector<long> value);

    /// uint8_t svlasta[_u8](svbool_t pg, svuint8_t op) : "LASTA Wresult, Pg, Zop.B" or "LASTA Bresult, Pg, Zop.B"
  public static unsafe Vector<byte> ExtractAfterLastVector(Vector<byte> value);

    /// uint16_t svlasta[_u16](svbool_t pg, svuint16_t op) : "LASTA Wresult, Pg, Zop.H" or "LASTA Hresult, Pg, Zop.H"
  public static unsafe Vector<ushort> ExtractAfterLastVector(Vector<ushort> value);

    /// uint32_t svlasta[_u32](svbool_t pg, svuint32_t op) : "LASTA Wresult, Pg, Zop.S" or "LASTA Sresult, Pg, Zop.S"
  public static unsafe Vector<uint> ExtractAfterLastVector(Vector<uint> value);

    /// uint64_t svlasta[_u64](svbool_t pg, svuint64_t op) : "LASTA Xresult, Pg, Zop.D" or "LASTA Dresult, Pg, Zop.D"
  public static unsafe Vector<ulong> ExtractAfterLastVector(Vector<ulong> value);


    /// ExtractLastScalar : Extract last element

    /// float32_t svlastb[_f32](svbool_t pg, svfloat32_t op) : "LASTB Wresult, Pg, Zop.S" or "LASTB Sresult, Pg, Zop.S"
  public static unsafe float ExtractLastScalar(Vector<float> value);

    /// float64_t svlastb[_f64](svbool_t pg, svfloat64_t op) : "LASTB Xresult, Pg, Zop.D" or "LASTB Dresult, Pg, Zop.D"
  public static unsafe double ExtractLastScalar(Vector<double> value);

    /// int8_t svlastb[_s8](svbool_t pg, svint8_t op) : "LASTB Wresult, Pg, Zop.B" or "LASTB Bresult, Pg, Zop.B"
  public static unsafe sbyte ExtractLastScalar(Vector<sbyte> value);

    /// int16_t svlastb[_s16](svbool_t pg, svint16_t op) : "LASTB Wresult, Pg, Zop.H" or "LASTB Hresult, Pg, Zop.H"
  public static unsafe short ExtractLastScalar(Vector<short> value);

    /// int32_t svlastb[_s32](svbool_t pg, svint32_t op) : "LASTB Wresult, Pg, Zop.S" or "LASTB Sresult, Pg, Zop.S"
  public static unsafe int ExtractLastScalar(Vector<int> value);

    /// int64_t svlastb[_s64](svbool_t pg, svint64_t op) : "LASTB Xresult, Pg, Zop.D" or "LASTB Dresult, Pg, Zop.D"
  public static unsafe long ExtractLastScalar(Vector<long> value);

    /// uint8_t svlastb[_u8](svbool_t pg, svuint8_t op) : "LASTB Wresult, Pg, Zop.B" or "LASTB Bresult, Pg, Zop.B"
  public static unsafe byte ExtractLastScalar(Vector<byte> value);

    /// uint16_t svlastb[_u16](svbool_t pg, svuint16_t op) : "LASTB Wresult, Pg, Zop.H" or "LASTB Hresult, Pg, Zop.H"
  public static unsafe ushort ExtractLastScalar(Vector<ushort> value);

    /// uint32_t svlastb[_u32](svbool_t pg, svuint32_t op) : "LASTB Wresult, Pg, Zop.S" or "LASTB Sresult, Pg, Zop.S"
  public static unsafe uint ExtractLastScalar(Vector<uint> value);

    /// uint64_t svlastb[_u64](svbool_t pg, svuint64_t op) : "LASTB Xresult, Pg, Zop.D" or "LASTB Dresult, Pg, Zop.D"
  public static unsafe ulong ExtractLastScalar(Vector<ulong> value);


    /// ExtractLastVector : Extract last element

    /// float32_t svlastb[_f32](svbool_t pg, svfloat32_t op) : "LASTB Wresult, Pg, Zop.S" or "LASTB Sresult, Pg, Zop.S"
  public static unsafe Vector<float> ExtractLastVector(Vector<float> value);

    /// float64_t svlastb[_f64](svbool_t pg, svfloat64_t op) : "LASTB Xresult, Pg, Zop.D" or "LASTB Dresult, Pg, Zop.D"
  public static unsafe Vector<double> ExtractLastVector(Vector<double> value);

    /// int8_t svlastb[_s8](svbool_t pg, svint8_t op) : "LASTB Wresult, Pg, Zop.B" or "LASTB Bresult, Pg, Zop.B"
  public static unsafe Vector<sbyte> ExtractLastVector(Vector<sbyte> value);

    /// int16_t svlastb[_s16](svbool_t pg, svint16_t op) : "LASTB Wresult, Pg, Zop.H" or "LASTB Hresult, Pg, Zop.H"
  public static unsafe Vector<short> ExtractLastVector(Vector<short> value);

    /// int32_t svlastb[_s32](svbool_t pg, svint32_t op) : "LASTB Wresult, Pg, Zop.S" or "LASTB Sresult, Pg, Zop.S"
  public static unsafe Vector<int> ExtractLastVector(Vector<int> value);

    /// int64_t svlastb[_s64](svbool_t pg, svint64_t op) : "LASTB Xresult, Pg, Zop.D" or "LASTB Dresult, Pg, Zop.D"
  public static unsafe Vector<long> ExtractLastVector(Vector<long> value);

    /// uint8_t svlastb[_u8](svbool_t pg, svuint8_t op) : "LASTB Wresult, Pg, Zop.B" or "LASTB Bresult, Pg, Zop.B"
  public static unsafe Vector<byte> ExtractLastVector(Vector<byte> value);

    /// uint16_t svlastb[_u16](svbool_t pg, svuint16_t op) : "LASTB Wresult, Pg, Zop.H" or "LASTB Hresult, Pg, Zop.H"
  public static unsafe Vector<ushort> ExtractLastVector(Vector<ushort> value);

    /// uint32_t svlastb[_u32](svbool_t pg, svuint32_t op) : "LASTB Wresult, Pg, Zop.S" or "LASTB Sresult, Pg, Zop.S"
  public static unsafe Vector<uint> ExtractLastVector(Vector<uint> value);

    /// uint64_t svlastb[_u64](svbool_t pg, svuint64_t op) : "LASTB Xresult, Pg, Zop.D" or "LASTB Dresult, Pg, Zop.D"
  public static unsafe Vector<ulong> ExtractLastVector(Vector<ulong> value);


    /// ExtractVector : Extract vector from pair of vectors

    /// svfloat32_t svext[_f32](svfloat32_t op1, svfloat32_t op2, uint64_t imm3) : "EXT Ztied1.B, Ztied1.B, Zop2.B, #imm3 * 4" or "MOVPRFX Zresult, Zop1; EXT Zresult.B, Zresult.B, Zop2.B, #imm3 * 4"
  public static unsafe Vector<float> ExtractVector(Vector<float> upper, Vector<float> lower, [ConstantExpected] byte index);

    /// svfloat64_t svext[_f64](svfloat64_t op1, svfloat64_t op2, uint64_t imm3) : "EXT Ztied1.B, Ztied1.B, Zop2.B, #imm3 * 8" or "MOVPRFX Zresult, Zop1; EXT Zresult.B, Zresult.B, Zop2.B, #imm3 * 8"
  public static unsafe Vector<double> ExtractVector(Vector<double> upper, Vector<double> lower, [ConstantExpected] byte index);

    /// svint8_t svext[_s8](svint8_t op1, svint8_t op2, uint64_t imm3) : "EXT Ztied1.B, Ztied1.B, Zop2.B, #imm3" or "MOVPRFX Zresult, Zop1; EXT Zresult.B, Zresult.B, Zop2.B, #imm3"
  public static unsafe Vector<sbyte> ExtractVector(Vector<sbyte> upper, Vector<sbyte> lower, [ConstantExpected] byte index);

    /// svint16_t svext[_s16](svint16_t op1, svint16_t op2, uint64_t imm3) : "EXT Ztied1.B, Ztied1.B, Zop2.B, #imm3 * 2" or "MOVPRFX Zresult, Zop1; EXT Zresult.B, Zresult.B, Zop2.B, #imm3 * 2"
  public static unsafe Vector<short> ExtractVector(Vector<short> upper, Vector<short> lower, [ConstantExpected] byte index);

    /// svint32_t svext[_s32](svint32_t op1, svint32_t op2, uint64_t imm3) : "EXT Ztied1.B, Ztied1.B, Zop2.B, #imm3 * 4" or "MOVPRFX Zresult, Zop1; EXT Zresult.B, Zresult.B, Zop2.B, #imm3 * 4"
  public static unsafe Vector<int> ExtractVector(Vector<int> upper, Vector<int> lower, [ConstantExpected] byte index);

    /// svint64_t svext[_s64](svint64_t op1, svint64_t op2, uint64_t imm3) : "EXT Ztied1.B, Ztied1.B, Zop2.B, #imm3 * 8" or "MOVPRFX Zresult, Zop1; EXT Zresult.B, Zresult.B, Zop2.B, #imm3 * 8"
  public static unsafe Vector<long> ExtractVector(Vector<long> upper, Vector<long> lower, [ConstantExpected] byte index);

    /// svuint8_t svext[_u8](svuint8_t op1, svuint8_t op2, uint64_t imm3) : "EXT Ztied1.B, Ztied1.B, Zop2.B, #imm3" or "MOVPRFX Zresult, Zop1; EXT Zresult.B, Zresult.B, Zop2.B, #imm3"
  public static unsafe Vector<byte> ExtractVector(Vector<byte> upper, Vector<byte> lower, [ConstantExpected] byte index);

    /// svuint16_t svext[_u16](svuint16_t op1, svuint16_t op2, uint64_t imm3) : "EXT Ztied1.B, Ztied1.B, Zop2.B, #imm3 * 2" or "MOVPRFX Zresult, Zop1; EXT Zresult.B, Zresult.B, Zop2.B, #imm3 * 2"
  public static unsafe Vector<ushort> ExtractVector(Vector<ushort> upper, Vector<ushort> lower, [ConstantExpected] byte index);

    /// svuint32_t svext[_u32](svuint32_t op1, svuint32_t op2, uint64_t imm3) : "EXT Ztied1.B, Ztied1.B, Zop2.B, #imm3 * 4" or "MOVPRFX Zresult, Zop1; EXT Zresult.B, Zresult.B, Zop2.B, #imm3 * 4"
  public static unsafe Vector<uint> ExtractVector(Vector<uint> upper, Vector<uint> lower, [ConstantExpected] byte index);

    /// svuint64_t svext[_u64](svuint64_t op1, svuint64_t op2, uint64_t imm3) : "EXT Ztied1.B, Ztied1.B, Zop2.B, #imm3 * 8" or "MOVPRFX Zresult, Zop1; EXT Zresult.B, Zresult.B, Zop2.B, #imm3 * 8"
  public static unsafe Vector<ulong> ExtractVector(Vector<ulong> upper, Vector<ulong> lower, [ConstantExpected] byte index);


    /// TestAnyTrue : Test whether any active element is true

    /// bool svptest_any(svbool_t pg, svbool_t op) : PTEST
  public static unsafe bool TestAnyTrue(Vector<sbyte> leftMask, Vector<sbyte> rightMask);

    /// bool svptest_any(svbool_t pg, svbool_t op) : PTEST
  public static unsafe bool TestAnyTrue(Vector<short> leftMask, Vector<short> rightMask);

    /// bool svptest_any(svbool_t pg, svbool_t op) : PTEST
  public static unsafe bool TestAnyTrue(Vector<int> leftMask, Vector<int> rightMask);

    /// bool svptest_any(svbool_t pg, svbool_t op) : PTEST
  public static unsafe bool TestAnyTrue(Vector<long> leftMask, Vector<long> rightMask);

    /// bool svptest_any(svbool_t pg, svbool_t op) : PTEST
  public static unsafe bool TestAnyTrue(Vector<byte> leftMask, Vector<byte> rightMask);

    /// bool svptest_any(svbool_t pg, svbool_t op) : PTEST
  public static unsafe bool TestAnyTrue(Vector<ushort> leftMask, Vector<ushort> rightMask);

    /// bool svptest_any(svbool_t pg, svbool_t op) : PTEST
  public static unsafe bool TestAnyTrue(Vector<uint> leftMask, Vector<uint> rightMask);

    /// bool svptest_any(svbool_t pg, svbool_t op) : PTEST
  public static unsafe bool TestAnyTrue(Vector<ulong> leftMask, Vector<ulong> rightMask);


    /// TestFirstTrue : Test whether the first active element is true

    /// bool svptest_first(svbool_t pg, svbool_t op) : PTEST
  public static unsafe bool TestFirstTrue(Vector<sbyte> leftMask, Vector<sbyte> rightMask);

    /// bool svptest_first(svbool_t pg, svbool_t op) : PTEST
  public static unsafe bool TestFirstTrue(Vector<short> leftMask, Vector<short> rightMask);

    /// bool svptest_first(svbool_t pg, svbool_t op) : PTEST
  public static unsafe bool TestFirstTrue(Vector<int> leftMask, Vector<int> rightMask);

    /// bool svptest_first(svbool_t pg, svbool_t op) : PTEST
  public static unsafe bool TestFirstTrue(Vector<long> leftMask, Vector<long> rightMask);

    /// bool svptest_first(svbool_t pg, svbool_t op) : PTEST
  public static unsafe bool TestFirstTrue(Vector<byte> leftMask, Vector<byte> rightMask);

    /// bool svptest_first(svbool_t pg, svbool_t op) : PTEST
  public static unsafe bool TestFirstTrue(Vector<ushort> leftMask, Vector<ushort> rightMask);

    /// bool svptest_first(svbool_t pg, svbool_t op) : PTEST
  public static unsafe bool TestFirstTrue(Vector<uint> leftMask, Vector<uint> rightMask);

    /// bool svptest_first(svbool_t pg, svbool_t op) : PTEST
  public static unsafe bool TestFirstTrue(Vector<ulong> leftMask, Vector<ulong> rightMask);


    /// TestLastTrue : Test whether the last active element is true

    /// bool svptest_last(svbool_t pg, svbool_t op) : PTEST
  public static unsafe bool TestLastTrue(Vector<sbyte> leftMask, Vector<sbyte> rightMask);

    /// bool svptest_last(svbool_t pg, svbool_t op) : PTEST
  public static unsafe bool TestLastTrue(Vector<short> leftMask, Vector<short> rightMask);

    /// bool svptest_last(svbool_t pg, svbool_t op) : PTEST
  public static unsafe bool TestLastTrue(Vector<int> leftMask, Vector<int> rightMask);

    /// bool svptest_last(svbool_t pg, svbool_t op) : PTEST
  public static unsafe bool TestLastTrue(Vector<long> leftMask, Vector<long> rightMask);

    /// bool svptest_last(svbool_t pg, svbool_t op) : PTEST
  public static unsafe bool TestLastTrue(Vector<byte> leftMask, Vector<byte> rightMask);

    /// bool svptest_last(svbool_t pg, svbool_t op) : PTEST
  public static unsafe bool TestLastTrue(Vector<ushort> leftMask, Vector<ushort> rightMask);

    /// bool svptest_last(svbool_t pg, svbool_t op) : PTEST
  public static unsafe bool TestLastTrue(Vector<uint> leftMask, Vector<uint> rightMask);

    /// bool svptest_last(svbool_t pg, svbool_t op) : PTEST
  public static unsafe bool TestLastTrue(Vector<ulong> leftMask, Vector<ulong> rightMask);


  /// total method signatures: 354
  /// total method names:      60
}

  /// Optional Entries:
  ///   public static unsafe Vector<float> AbsoluteCompareGreaterThan(Vector<float> left, float right); // svacgt[_n_f32]
  ///   public static unsafe Vector<double> AbsoluteCompareGreaterThan(Vector<double> left, double right); // svacgt[_n_f64]
  ///   public static unsafe Vector<float> AbsoluteCompareGreaterThanOrEqual(Vector<float> left, float right); // svacge[_n_f32]
  ///   public static unsafe Vector<double> AbsoluteCompareGreaterThanOrEqual(Vector<double> left, double right); // svacge[_n_f64]
  ///   public static unsafe Vector<float> AbsoluteCompareLessThan(Vector<float> left, float right); // svaclt[_n_f32]
  ///   public static unsafe Vector<double> AbsoluteCompareLessThan(Vector<double> left, double right); // svaclt[_n_f64]
  ///   public static unsafe Vector<float> AbsoluteCompareLessThanOrEqual(Vector<float> left, float right); // svacle[_n_f32]
  ///   public static unsafe Vector<double> AbsoluteCompareLessThanOrEqual(Vector<double> left, double right); // svacle[_n_f64]
  ///   public static unsafe Vector<float> CompareEqual(Vector<float> left, float right); // svcmpeq[_n_f32]
  ///   public static unsafe Vector<double> CompareEqual(Vector<double> left, double right); // svcmpeq[_n_f64]
  ///   public static unsafe Vector<sbyte> CompareEqual(Vector<sbyte> left, sbyte right); // svcmpeq[_n_s8]
  ///   public static unsafe Vector<short> CompareEqual(Vector<short> left, short right); // svcmpeq[_n_s16]
  ///   public static unsafe Vector<int> CompareEqual(Vector<int> left, int right); // svcmpeq[_n_s32]
  ///   public static unsafe Vector<long> CompareEqual(Vector<long> left, long right); // svcmpeq[_n_s64]
  ///   public static unsafe Vector<byte> CompareEqual(Vector<byte> left, byte right); // svcmpeq[_n_u8]
  ///   public static unsafe Vector<ushort> CompareEqual(Vector<ushort> left, ushort right); // svcmpeq[_n_u16]
  ///   public static unsafe Vector<uint> CompareEqual(Vector<uint> left, uint right); // svcmpeq[_n_u32]
  ///   public static unsafe Vector<ulong> CompareEqual(Vector<ulong> left, ulong right); // svcmpeq[_n_u64]
  ///   public static unsafe Vector<sbyte> CompareEqual(Vector<sbyte> left, long right); // svcmpeq_wide[_n_s8]
  ///   public static unsafe Vector<short> CompareEqual(Vector<short> left, long right); // svcmpeq_wide[_n_s16]
  ///   public static unsafe Vector<int> CompareEqual(Vector<int> left, long right); // svcmpeq_wide[_n_s32]
  ///   public static unsafe Vector<float> CompareGreaterThan(Vector<float> left, float right); // svcmpgt[_n_f32]
  ///   public static unsafe Vector<double> CompareGreaterThan(Vector<double> left, double right); // svcmpgt[_n_f64]
  ///   public static unsafe Vector<sbyte> CompareGreaterThan(Vector<sbyte> left, sbyte right); // svcmpgt[_n_s8]
  ///   public static unsafe Vector<short> CompareGreaterThan(Vector<short> left, short right); // svcmpgt[_n_s16]
  ///   public static unsafe Vector<int> CompareGreaterThan(Vector<int> left, int right); // svcmpgt[_n_s32]
  ///   public static unsafe Vector<long> CompareGreaterThan(Vector<long> left, long right); // svcmpgt[_n_s64]
  ///   public static unsafe Vector<byte> CompareGreaterThan(Vector<byte> left, byte right); // svcmpgt[_n_u8]
  ///   public static unsafe Vector<ushort> CompareGreaterThan(Vector<ushort> left, ushort right); // svcmpgt[_n_u16]
  ///   public static unsafe Vector<uint> CompareGreaterThan(Vector<uint> left, uint right); // svcmpgt[_n_u32]
  ///   public static unsafe Vector<ulong> CompareGreaterThan(Vector<ulong> left, ulong right); // svcmpgt[_n_u64]
  ///   public static unsafe Vector<sbyte> CompareGreaterThan(Vector<sbyte> left, long right); // svcmpgt_wide[_n_s8]
  ///   public static unsafe Vector<short> CompareGreaterThan(Vector<short> left, long right); // svcmpgt_wide[_n_s16]
  ///   public static unsafe Vector<int> CompareGreaterThan(Vector<int> left, long right); // svcmpgt_wide[_n_s32]
  ///   public static unsafe Vector<byte> CompareGreaterThan(Vector<byte> left, ulong right); // svcmpgt_wide[_n_u8]
  ///   public static unsafe Vector<ushort> CompareGreaterThan(Vector<ushort> left, ulong right); // svcmpgt_wide[_n_u16]
  ///   public static unsafe Vector<uint> CompareGreaterThan(Vector<uint> left, ulong right); // svcmpgt_wide[_n_u32]
  ///   public static unsafe Vector<float> CompareGreaterThanOrEqual(Vector<float> left, float right); // svcmpge[_n_f32]
  ///   public static unsafe Vector<double> CompareGreaterThanOrEqual(Vector<double> left, double right); // svcmpge[_n_f64]
  ///   public static unsafe Vector<sbyte> CompareGreaterThanOrEqual(Vector<sbyte> left, sbyte right); // svcmpge[_n_s8]
  ///   public static unsafe Vector<short> CompareGreaterThanOrEqual(Vector<short> left, short right); // svcmpge[_n_s16]
  ///   public static unsafe Vector<int> CompareGreaterThanOrEqual(Vector<int> left, int right); // svcmpge[_n_s32]
  ///   public static unsafe Vector<long> CompareGreaterThanOrEqual(Vector<long> left, long right); // svcmpge[_n_s64]
  ///   public static unsafe Vector<byte> CompareGreaterThanOrEqual(Vector<byte> left, byte right); // svcmpge[_n_u8]
  ///   public static unsafe Vector<ushort> CompareGreaterThanOrEqual(Vector<ushort> left, ushort right); // svcmpge[_n_u16]
  ///   public static unsafe Vector<uint> CompareGreaterThanOrEqual(Vector<uint> left, uint right); // svcmpge[_n_u32]
  ///   public static unsafe Vector<ulong> CompareGreaterThanOrEqual(Vector<ulong> left, ulong right); // svcmpge[_n_u64]
  ///   public static unsafe Vector<sbyte> CompareGreaterThanOrEqual(Vector<sbyte> left, long right); // svcmpge_wide[_n_s8]
  ///   public static unsafe Vector<short> CompareGreaterThanOrEqual(Vector<short> left, long right); // svcmpge_wide[_n_s16]
  ///   public static unsafe Vector<int> CompareGreaterThanOrEqual(Vector<int> left, long right); // svcmpge_wide[_n_s32]
  ///   public static unsafe Vector<byte> CompareGreaterThanOrEqual(Vector<byte> left, ulong right); // svcmpge_wide[_n_u8]
  ///   public static unsafe Vector<ushort> CompareGreaterThanOrEqual(Vector<ushort> left, ulong right); // svcmpge_wide[_n_u16]
  ///   public static unsafe Vector<uint> CompareGreaterThanOrEqual(Vector<uint> left, ulong right); // svcmpge_wide[_n_u32]
  ///   public static unsafe Vector<float> CompareLessThan(Vector<float> left, float right); // svcmplt[_n_f32]
  ///   public static unsafe Vector<double> CompareLessThan(Vector<double> left, double right); // svcmplt[_n_f64]
  ///   public static unsafe Vector<sbyte> CompareLessThan(Vector<sbyte> left, sbyte right); // svcmplt[_n_s8]
  ///   public static unsafe Vector<short> CompareLessThan(Vector<short> left, short right); // svcmplt[_n_s16]
  ///   public static unsafe Vector<int> CompareLessThan(Vector<int> left, int right); // svcmplt[_n_s32]
  ///   public static unsafe Vector<long> CompareLessThan(Vector<long> left, long right); // svcmplt[_n_s64]
  ///   public static unsafe Vector<byte> CompareLessThan(Vector<byte> left, byte right); // svcmplt[_n_u8]
  ///   public static unsafe Vector<ushort> CompareLessThan(Vector<ushort> left, ushort right); // svcmplt[_n_u16]
  ///   public static unsafe Vector<uint> CompareLessThan(Vector<uint> left, uint right); // svcmplt[_n_u32]
  ///   public static unsafe Vector<ulong> CompareLessThan(Vector<ulong> left, ulong right); // svcmplt[_n_u64]
  ///   public static unsafe Vector<sbyte> CompareLessThan(Vector<sbyte> left, long right); // svcmplt_wide[_n_s8]
  ///   public static unsafe Vector<short> CompareLessThan(Vector<short> left, long right); // svcmplt_wide[_n_s16]
  ///   public static unsafe Vector<int> CompareLessThan(Vector<int> left, long right); // svcmplt_wide[_n_s32]
  ///   public static unsafe Vector<byte> CompareLessThan(Vector<byte> left, ulong right); // svcmplt_wide[_n_u8]
  ///   public static unsafe Vector<ushort> CompareLessThan(Vector<ushort> left, ulong right); // svcmplt_wide[_n_u16]
  ///   public static unsafe Vector<uint> CompareLessThan(Vector<uint> left, ulong right); // svcmplt_wide[_n_u32]
  ///   public static unsafe Vector<float> CompareLessThanOrEqual(Vector<float> left, float right); // svcmple[_n_f32]
  ///   public static unsafe Vector<double> CompareLessThanOrEqual(Vector<double> left, double right); // svcmple[_n_f64]
  ///   public static unsafe Vector<sbyte> CompareLessThanOrEqual(Vector<sbyte> left, sbyte right); // svcmple[_n_s8]
  ///   public static unsafe Vector<short> CompareLessThanOrEqual(Vector<short> left, short right); // svcmple[_n_s16]
  ///   public static unsafe Vector<int> CompareLessThanOrEqual(Vector<int> left, int right); // svcmple[_n_s32]
  ///   public static unsafe Vector<long> CompareLessThanOrEqual(Vector<long> left, long right); // svcmple[_n_s64]
  ///   public static unsafe Vector<byte> CompareLessThanOrEqual(Vector<byte> left, byte right); // svcmple[_n_u8]
  ///   public static unsafe Vector<ushort> CompareLessThanOrEqual(Vector<ushort> left, ushort right); // svcmple[_n_u16]
  ///   public static unsafe Vector<uint> CompareLessThanOrEqual(Vector<uint> left, uint right); // svcmple[_n_u32]
  ///   public static unsafe Vector<ulong> CompareLessThanOrEqual(Vector<ulong> left, ulong right); // svcmple[_n_u64]
  ///   public static unsafe Vector<sbyte> CompareLessThanOrEqual(Vector<sbyte> left, long right); // svcmple_wide[_n_s8]
  ///   public static unsafe Vector<short> CompareLessThanOrEqual(Vector<short> left, long right); // svcmple_wide[_n_s16]
  ///   public static unsafe Vector<int> CompareLessThanOrEqual(Vector<int> left, long right); // svcmple_wide[_n_s32]
  ///   public static unsafe Vector<byte> CompareLessThanOrEqual(Vector<byte> left, ulong right); // svcmple_wide[_n_u8]
  ///   public static unsafe Vector<ushort> CompareLessThanOrEqual(Vector<ushort> left, ulong right); // svcmple_wide[_n_u16]
  ///   public static unsafe Vector<uint> CompareLessThanOrEqual(Vector<uint> left, ulong right); // svcmple_wide[_n_u32]
  ///   public static unsafe Vector<float> CompareNotEqualTo(Vector<float> left, float right); // svcmpne[_n_f32]
  ///   public static unsafe Vector<double> CompareNotEqualTo(Vector<double> left, double right); // svcmpne[_n_f64]
  ///   public static unsafe Vector<sbyte> CompareNotEqualTo(Vector<sbyte> left, sbyte right); // svcmpne[_n_s8]
  ///   public static unsafe Vector<short> CompareNotEqualTo(Vector<short> left, short right); // svcmpne[_n_s16]
  ///   public static unsafe Vector<int> CompareNotEqualTo(Vector<int> left, int right); // svcmpne[_n_s32]
  ///   public static unsafe Vector<long> CompareNotEqualTo(Vector<long> left, long right); // svcmpne[_n_s64]
  ///   public static unsafe Vector<byte> CompareNotEqualTo(Vector<byte> left, byte right); // svcmpne[_n_u8]
  ///   public static unsafe Vector<ushort> CompareNotEqualTo(Vector<ushort> left, ushort right); // svcmpne[_n_u16]
  ///   public static unsafe Vector<uint> CompareNotEqualTo(Vector<uint> left, uint right); // svcmpne[_n_u32]
  ///   public static unsafe Vector<ulong> CompareNotEqualTo(Vector<ulong> left, ulong right); // svcmpne[_n_u64]
  ///   public static unsafe Vector<sbyte> CompareNotEqualTo(Vector<sbyte> left, long right); // svcmpne_wide[_n_s8]
  ///   public static unsafe Vector<short> CompareNotEqualTo(Vector<short> left, long right); // svcmpne_wide[_n_s16]
  ///   public static unsafe Vector<int> CompareNotEqualTo(Vector<int> left, long right); // svcmpne_wide[_n_s32]
  ///   public static unsafe Vector<float> CompareUnordered(Vector<float> left, float right); // svcmpuo[_n_f32]
  ///   public static unsafe Vector<double> CompareUnordered(Vector<double> left, double right); // svcmpuo[_n_f64]
  ///   public static unsafe float ConditionalExtractAfterLastActiveElement(Vector<float> mask, float defaultValue, Vector<float> data); // svclasta[_n_f32]
  ///   public static unsafe double ConditionalExtractAfterLastActiveElement(Vector<double> mask, double defaultValue, Vector<double> data); // svclasta[_n_f64]
  ///   public static unsafe sbyte ConditionalExtractAfterLastActiveElement(Vector<sbyte> mask, sbyte defaultValue, Vector<sbyte> data); // svclasta[_n_s8]
  ///   public static unsafe short ConditionalExtractAfterLastActiveElement(Vector<short> mask, short defaultValue, Vector<short> data); // svclasta[_n_s16]
  ///   public static unsafe int ConditionalExtractAfterLastActiveElement(Vector<int> mask, int defaultValue, Vector<int> data); // svclasta[_n_s32]
  ///   public static unsafe long ConditionalExtractAfterLastActiveElement(Vector<long> mask, long defaultValue, Vector<long> data); // svclasta[_n_s64]
  ///   public static unsafe byte ConditionalExtractAfterLastActiveElement(Vector<byte> mask, byte defaultValue, Vector<byte> data); // svclasta[_n_u8]
  ///   public static unsafe ushort ConditionalExtractAfterLastActiveElement(Vector<ushort> mask, ushort defaultValue, Vector<ushort> data); // svclasta[_n_u16]
  ///   public static unsafe uint ConditionalExtractAfterLastActiveElement(Vector<uint> mask, uint defaultValue, Vector<uint> data); // svclasta[_n_u32]
  ///   public static unsafe ulong ConditionalExtractAfterLastActiveElement(Vector<ulong> mask, ulong defaultValue, Vector<ulong> data); // svclasta[_n_u64]
  ///   public static unsafe float ConditionalExtractAfterLastActiveElementAndReplicate(Vector<float> mask, float defaultScalar, Vector<float> data); // svclasta[_n_f32]
  ///   public static unsafe double ConditionalExtractAfterLastActiveElementAndReplicate(Vector<double> mask, double defaultScalar, Vector<double> data); // svclasta[_n_f64]
  ///   public static unsafe sbyte ConditionalExtractAfterLastActiveElementAndReplicate(Vector<sbyte> mask, sbyte defaultScalar, Vector<sbyte> data); // svclasta[_n_s8]
  ///   public static unsafe short ConditionalExtractAfterLastActiveElementAndReplicate(Vector<short> mask, short defaultScalar, Vector<short> data); // svclasta[_n_s16]
  ///   public static unsafe int ConditionalExtractAfterLastActiveElementAndReplicate(Vector<int> mask, int defaultScalar, Vector<int> data); // svclasta[_n_s32]
  ///   public static unsafe long ConditionalExtractAfterLastActiveElementAndReplicate(Vector<long> mask, long defaultScalar, Vector<long> data); // svclasta[_n_s64]
  ///   public static unsafe byte ConditionalExtractAfterLastActiveElementAndReplicate(Vector<byte> mask, byte defaultScalar, Vector<byte> data); // svclasta[_n_u8]
  ///   public static unsafe ushort ConditionalExtractAfterLastActiveElementAndReplicate(Vector<ushort> mask, ushort defaultScalar, Vector<ushort> data); // svclasta[_n_u16]
  ///   public static unsafe uint ConditionalExtractAfterLastActiveElementAndReplicate(Vector<uint> mask, uint defaultScalar, Vector<uint> data); // svclasta[_n_u32]
  ///   public static unsafe ulong ConditionalExtractAfterLastActiveElementAndReplicate(Vector<ulong> mask, ulong defaultScalar, Vector<ulong> data); // svclasta[_n_u64]
  ///   public static unsafe float ConditionalExtractLastActiveElement(Vector<float> mask, float defaultValue, Vector<float> data); // svclastb[_n_f32]
  ///   public static unsafe double ConditionalExtractLastActiveElement(Vector<double> mask, double defaultValue, Vector<double> data); // svclastb[_n_f64]
  ///   public static unsafe sbyte ConditionalExtractLastActiveElement(Vector<sbyte> mask, sbyte defaultValue, Vector<sbyte> data); // svclastb[_n_s8]
  ///   public static unsafe short ConditionalExtractLastActiveElement(Vector<short> mask, short defaultValue, Vector<short> data); // svclastb[_n_s16]
  ///   public static unsafe int ConditionalExtractLastActiveElement(Vector<int> mask, int defaultValue, Vector<int> data); // svclastb[_n_s32]
  ///   public static unsafe long ConditionalExtractLastActiveElement(Vector<long> mask, long defaultValue, Vector<long> data); // svclastb[_n_s64]
  ///   public static unsafe byte ConditionalExtractLastActiveElement(Vector<byte> mask, byte defaultValue, Vector<byte> data); // svclastb[_n_u8]
  ///   public static unsafe ushort ConditionalExtractLastActiveElement(Vector<ushort> mask, ushort defaultValue, Vector<ushort> data); // svclastb[_n_u16]
  ///   public static unsafe uint ConditionalExtractLastActiveElement(Vector<uint> mask, uint defaultValue, Vector<uint> data); // svclastb[_n_u32]
  ///   public static unsafe ulong ConditionalExtractLastActiveElement(Vector<ulong> mask, ulong defaultValue, Vector<ulong> data); // svclastb[_n_u64]
  ///   public static unsafe float ConditionalExtractLastActiveElementAndReplicate(Vector<float> mask, float fallback, Vector<float> data); // svclastb[_n_f32]
  ///   public static unsafe double ConditionalExtractLastActiveElementAndReplicate(Vector<double> mask, double fallback, Vector<double> data); // svclastb[_n_f64]
  ///   public static unsafe sbyte ConditionalExtractLastActiveElementAndReplicate(Vector<sbyte> mask, sbyte fallback, Vector<sbyte> data); // svclastb[_n_s8]
  ///   public static unsafe short ConditionalExtractLastActiveElementAndReplicate(Vector<short> mask, short fallback, Vector<short> data); // svclastb[_n_s16]
  ///   public static unsafe int ConditionalExtractLastActiveElementAndReplicate(Vector<int> mask, int fallback, Vector<int> data); // svclastb[_n_s32]
  ///   public static unsafe long ConditionalExtractLastActiveElementAndReplicate(Vector<long> mask, long fallback, Vector<long> data); // svclastb[_n_s64]
  ///   public static unsafe byte ConditionalExtractLastActiveElementAndReplicate(Vector<byte> mask, byte fallback, Vector<byte> data); // svclastb[_n_u8]
  ///   public static unsafe ushort ConditionalExtractLastActiveElementAndReplicate(Vector<ushort> mask, ushort fallback, Vector<ushort> data); // svclastb[_n_u16]
  ///   public static unsafe uint ConditionalExtractLastActiveElementAndReplicate(Vector<uint> mask, uint fallback, Vector<uint> data); // svclastb[_n_u32]
  ///   public static unsafe ulong ConditionalExtractLastActiveElementAndReplicate(Vector<ulong> mask, ulong fallback, Vector<ulong> data); // svclastb[_n_u64]
  ///   Total Maybe: 140

  /// Rejected:
  ///   public static unsafe Vector<byte> CreateTrueMaskByte(); // svptrue_b8
  ///   public static unsafe Vector<double> CreateTrueMaskDouble(); // svptrue_b8
  ///   public static unsafe Vector<short> CreateTrueMaskInt16(); // svptrue_b8
  ///   public static unsafe Vector<int> CreateTrueMaskInt32(); // svptrue_b8
  ///   public static unsafe Vector<long> CreateTrueMaskInt64(); // svptrue_b8
  ///   public static unsafe Vector<sbyte> CreateTrueMaskSByte(); // svptrue_b8
  ///   public static unsafe Vector<float> CreateTrueMaskSingle(); // svptrue_b8
  ///   public static unsafe Vector<ushort> CreateTrueMaskUInt16(); // svptrue_b16
  ///   public static unsafe Vector<uint> CreateTrueMaskUInt32(); // svptrue_b32
  ///   public static unsafe Vector<ulong> CreateTrueMaskUInt64(); // svptrue_b64
  ///   Total Rejected: 10

  /// Total ACLE covered across API:      548

