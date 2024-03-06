namespace System.Runtime.Intrinsics.Arm;

/// VectorT Summary
public abstract partial class Sve2 : AdvSimd /// Feature: FEAT_SVE2  Category: mask
{

  /// T: byte, ushort, uint, ulong
  public static unsafe Vector<T> CreateWhileGreaterThanMask(int left, int right); // WHILEGT

  /// T: byte, ushort, uint, ulong
  public static unsafe Vector<T> CreateWhileGreaterThanMask(long left, long right); // WHILEGT

  /// T: byte, ushort, uint, ulong
  public static unsafe Vector<T> CreateWhileGreaterThanMask(uint left, uint right); // WHILEHI

  /// T: byte, ushort, uint, ulong
  public static unsafe Vector<T> CreateWhileGreaterThanMask(ulong left, ulong right); // WHILEHI

  /// T: byte, ushort, uint, ulong
  public static unsafe Vector<T> CreateWhileGreaterThanOrEqualMask(int left, int right); // WHILEGE

  /// T: byte, ushort, uint, ulong
  public static unsafe Vector<T> CreateWhileGreaterThanOrEqualMask(long left, long right); // WHILEGE

  /// T: byte, ushort, uint, ulong
  public static unsafe Vector<T> CreateWhileGreaterThanOrEqualMask(uint left, uint right); // WHILEHS

  /// T: byte, ushort, uint, ulong
  public static unsafe Vector<T> CreateWhileGreaterThanOrEqualMask(ulong left, ulong right); // WHILEHS

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> CreateWhileReadAfterWriteMask(T* left, T* right); // WHILERW

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> CreateWhileWriteAfterReadMask(T* left, T* right); // WHILEWR

  /// T: sbyte, short, byte, ushort
  public static unsafe Vector<T> Match(Vector<T> mask, Vector<T> left, Vector<T> right); // MATCH

  /// T: sbyte, short, byte, ushort
  public static unsafe Vector<T> NoMatch(Vector<T> mask, Vector<T> left, Vector<T> right); // NMATCH

  /// T: [sbyte, short], [short, int], [int, long], [byte, ushort], [ushort, uint], [uint, ulong]
  public static unsafe Vector<T> SaturatingExtractNarrowingLower(Vector<T2> value); // SQXTNB or UQXTNB

  /// T: [sbyte, short], [short, int], [int, long], [byte, ushort], [ushort, uint], [uint, ulong]
  public static unsafe Vector<T> SaturatingExtractNarrowingUpper(Vector<T> even, Vector<T2> op); // SQXTNT or UQXTNT

  /// T: [byte, short], [ushort, int], [uint, long]
  public static unsafe Vector<T> SaturatingExtractUnsignedNarrowingLower(Vector<T2> value); // SQXTUNB

  /// T: [byte, short], [ushort, int], [uint, long]
  public static unsafe Vector<T> SaturatingExtractUnsignedNarrowingUpper(Vector<T> even, Vector<T2> op); // SQXTUNT

  /// total method signatures: 16

}


/// Full API
public abstract partial class Sve2 : AdvSimd /// Feature: FEAT_SVE2  Category: mask
{
    /// CreateWhileGreaterThanMask : While decrementing scalar is greater than

    /// svbool_t svwhilegt_b8[_s32](int32_t op1, int32_t op2) : "WHILEGT Presult.B, Wop1, Wop2"
  public static unsafe Vector<byte> CreateWhileGreaterThanMask(int left, int right);

    /// svbool_t svwhilegt_b8[_s64](int64_t op1, int64_t op2) : "WHILEGT Presult.B, Xop1, Xop2"
  public static unsafe Vector<byte> CreateWhileGreaterThanMask(long left, long right);

    /// svbool_t svwhilegt_b8[_u32](uint32_t op1, uint32_t op2) : "WHILEHI Presult.B, Wop1, Wop2"
  public static unsafe Vector<byte> CreateWhileGreaterThanMask(uint left, uint right);

    /// svbool_t svwhilegt_b8[_u64](uint64_t op1, uint64_t op2) : "WHILEHI Presult.B, Xop1, Xop2"
  public static unsafe Vector<byte> CreateWhileGreaterThanMask(ulong left, ulong right);

    /// svbool_t svwhilegt_b16[_s32](int32_t op1, int32_t op2) : "WHILEGT Presult.H, Wop1, Wop2"
  public static unsafe Vector<ushort> CreateWhileGreaterThanMask(int left, int right);

    /// svbool_t svwhilegt_b16[_s64](int64_t op1, int64_t op2) : "WHILEGT Presult.H, Xop1, Xop2"
  public static unsafe Vector<ushort> CreateWhileGreaterThanMask(long left, long right);

    /// svbool_t svwhilegt_b16[_u32](uint32_t op1, uint32_t op2) : "WHILEHI Presult.H, Wop1, Wop2"
  public static unsafe Vector<ushort> CreateWhileGreaterThanMask(uint left, uint right);

    /// svbool_t svwhilegt_b16[_u64](uint64_t op1, uint64_t op2) : "WHILEHI Presult.H, Xop1, Xop2"
  public static unsafe Vector<ushort> CreateWhileGreaterThanMask(ulong left, ulong right);

    /// svbool_t svwhilegt_b32[_s32](int32_t op1, int32_t op2) : "WHILEGT Presult.S, Wop1, Wop2"
  public static unsafe Vector<uint> CreateWhileGreaterThanMask(int left, int right);

    /// svbool_t svwhilegt_b32[_s64](int64_t op1, int64_t op2) : "WHILEGT Presult.S, Xop1, Xop2"
  public static unsafe Vector<uint> CreateWhileGreaterThanMask(long left, long right);

    /// svbool_t svwhilegt_b32[_u32](uint32_t op1, uint32_t op2) : "WHILEHI Presult.S, Wop1, Wop2"
  public static unsafe Vector<uint> CreateWhileGreaterThanMask(uint left, uint right);

    /// svbool_t svwhilegt_b32[_u64](uint64_t op1, uint64_t op2) : "WHILEHI Presult.S, Xop1, Xop2"
  public static unsafe Vector<uint> CreateWhileGreaterThanMask(ulong left, ulong right);

    /// svbool_t svwhilegt_b64[_s32](int32_t op1, int32_t op2) : "WHILEGT Presult.D, Wop1, Wop2"
  public static unsafe Vector<ulong> CreateWhileGreaterThanMask(int left, int right);

    /// svbool_t svwhilegt_b64[_s64](int64_t op1, int64_t op2) : "WHILEGT Presult.D, Xop1, Xop2"
  public static unsafe Vector<ulong> CreateWhileGreaterThanMask(long left, long right);

    /// svbool_t svwhilegt_b64[_u32](uint32_t op1, uint32_t op2) : "WHILEHI Presult.D, Wop1, Wop2"
  public static unsafe Vector<ulong> CreateWhileGreaterThanMask(uint left, uint right);

    /// svbool_t svwhilegt_b64[_u64](uint64_t op1, uint64_t op2) : "WHILEHI Presult.D, Xop1, Xop2"
  public static unsafe Vector<ulong> CreateWhileGreaterThanMask(ulong left, ulong right);


    /// CreateWhileGreaterThanOrEqualMask : While decrementing scalar is greater than or equal to

    /// svbool_t svwhilege_b8[_s32](int32_t op1, int32_t op2) : "WHILEGE Presult.B, Wop1, Wop2"
  public static unsafe Vector<byte> CreateWhileGreaterThanOrEqualMask(int left, int right);

    /// svbool_t svwhilege_b8[_s64](int64_t op1, int64_t op2) : "WHILEGE Presult.B, Xop1, Xop2"
  public static unsafe Vector<byte> CreateWhileGreaterThanOrEqualMask(long left, long right);

    /// svbool_t svwhilege_b8[_u32](uint32_t op1, uint32_t op2) : "WHILEHS Presult.B, Wop1, Wop2"
  public static unsafe Vector<byte> CreateWhileGreaterThanOrEqualMask(uint left, uint right);

    /// svbool_t svwhilege_b8[_u64](uint64_t op1, uint64_t op2) : "WHILEHS Presult.B, Xop1, Xop2"
  public static unsafe Vector<byte> CreateWhileGreaterThanOrEqualMask(ulong left, ulong right);

    /// svbool_t svwhilege_b16[_s32](int32_t op1, int32_t op2) : "WHILEGE Presult.H, Wop1, Wop2"
  public static unsafe Vector<ushort> CreateWhileGreaterThanOrEqualMask(int left, int right);

    /// svbool_t svwhilege_b16[_s64](int64_t op1, int64_t op2) : "WHILEGE Presult.H, Xop1, Xop2"
  public static unsafe Vector<ushort> CreateWhileGreaterThanOrEqualMask(long left, long right);

    /// svbool_t svwhilege_b16[_u32](uint32_t op1, uint32_t op2) : "WHILEHS Presult.H, Wop1, Wop2"
  public static unsafe Vector<ushort> CreateWhileGreaterThanOrEqualMask(uint left, uint right);

    /// svbool_t svwhilege_b16[_u64](uint64_t op1, uint64_t op2) : "WHILEHS Presult.H, Xop1, Xop2"
  public static unsafe Vector<ushort> CreateWhileGreaterThanOrEqualMask(ulong left, ulong right);

    /// svbool_t svwhilege_b32[_s32](int32_t op1, int32_t op2) : "WHILEGE Presult.S, Wop1, Wop2"
  public static unsafe Vector<uint> CreateWhileGreaterThanOrEqualMask(int left, int right);

    /// svbool_t svwhilege_b32[_s64](int64_t op1, int64_t op2) : "WHILEGE Presult.S, Xop1, Xop2"
  public static unsafe Vector<uint> CreateWhileGreaterThanOrEqualMask(long left, long right);

    /// svbool_t svwhilege_b32[_u32](uint32_t op1, uint32_t op2) : "WHILEHS Presult.S, Wop1, Wop2"
  public static unsafe Vector<uint> CreateWhileGreaterThanOrEqualMask(uint left, uint right);

    /// svbool_t svwhilege_b32[_u64](uint64_t op1, uint64_t op2) : "WHILEHS Presult.S, Xop1, Xop2"
  public static unsafe Vector<uint> CreateWhileGreaterThanOrEqualMask(ulong left, ulong right);

    /// svbool_t svwhilege_b64[_s32](int32_t op1, int32_t op2) : "WHILEGE Presult.D, Wop1, Wop2"
  public static unsafe Vector<ulong> CreateWhileGreaterThanOrEqualMask(int left, int right);

    /// svbool_t svwhilege_b64[_s64](int64_t op1, int64_t op2) : "WHILEGE Presult.D, Xop1, Xop2"
  public static unsafe Vector<ulong> CreateWhileGreaterThanOrEqualMask(long left, long right);

    /// svbool_t svwhilege_b64[_u32](uint32_t op1, uint32_t op2) : "WHILEHS Presult.D, Wop1, Wop2"
  public static unsafe Vector<ulong> CreateWhileGreaterThanOrEqualMask(uint left, uint right);

    /// svbool_t svwhilege_b64[_u64](uint64_t op1, uint64_t op2) : "WHILEHS Presult.D, Xop1, Xop2"
  public static unsafe Vector<ulong> CreateWhileGreaterThanOrEqualMask(ulong left, ulong right);


    /// CreateWhileReadAfterWriteMask : While free of read-after-write conflicts

    /// svbool_t svwhilerw[_f32](const float32_t *op1, const float32_t *op2) : "WHILERW Presult.S, Xop1, Xop2"
  public static unsafe Vector<float> CreateWhileReadAfterWriteMask(float* left, float* right);

    /// svbool_t svwhilerw[_f64](const float64_t *op1, const float64_t *op2) : "WHILERW Presult.D, Xop1, Xop2"
  public static unsafe Vector<double> CreateWhileReadAfterWriteMask(double* left, double* right);

    /// svbool_t svwhilerw[_s8](const int8_t *op1, const int8_t *op2) : "WHILERW Presult.B, Xop1, Xop2"
  public static unsafe Vector<sbyte> CreateWhileReadAfterWriteMask(sbyte* left, sbyte* right);

    /// svbool_t svwhilerw[_s16](const int16_t *op1, const int16_t *op2) : "WHILERW Presult.H, Xop1, Xop2"
  public static unsafe Vector<short> CreateWhileReadAfterWriteMask(short* left, short* right);

    /// svbool_t svwhilerw[_s32](const int32_t *op1, const int32_t *op2) : "WHILERW Presult.S, Xop1, Xop2"
  public static unsafe Vector<int> CreateWhileReadAfterWriteMask(int* left, int* right);

    /// svbool_t svwhilerw[_s64](const int64_t *op1, const int64_t *op2) : "WHILERW Presult.D, Xop1, Xop2"
  public static unsafe Vector<long> CreateWhileReadAfterWriteMask(long* left, long* right);

    /// svbool_t svwhilerw[_u8](const uint8_t *op1, const uint8_t *op2) : "WHILERW Presult.B, Xop1, Xop2"
  public static unsafe Vector<byte> CreateWhileReadAfterWriteMask(byte* left, byte* right);

    /// svbool_t svwhilerw[_u16](const uint16_t *op1, const uint16_t *op2) : "WHILERW Presult.H, Xop1, Xop2"
  public static unsafe Vector<ushort> CreateWhileReadAfterWriteMask(ushort* left, ushort* right);

    /// svbool_t svwhilerw[_u32](const uint32_t *op1, const uint32_t *op2) : "WHILERW Presult.S, Xop1, Xop2"
  public static unsafe Vector<uint> CreateWhileReadAfterWriteMask(uint* left, uint* right);

    /// svbool_t svwhilerw[_u64](const uint64_t *op1, const uint64_t *op2) : "WHILERW Presult.D, Xop1, Xop2"
  public static unsafe Vector<ulong> CreateWhileReadAfterWriteMask(ulong* left, ulong* right);


    /// CreateWhileWriteAfterReadMask : While free of write-after-read conflicts

    /// svbool_t svwhilewr[_f32](const float32_t *op1, const float32_t *op2) : "WHILEWR Presult.S, Xop1, Xop2"
  public static unsafe Vector<float> CreateWhileWriteAfterReadMask(float* left, float* right);

    /// svbool_t svwhilewr[_f64](const float64_t *op1, const float64_t *op2) : "WHILEWR Presult.D, Xop1, Xop2"
  public static unsafe Vector<double> CreateWhileWriteAfterReadMask(double* left, double* right);

    /// svbool_t svwhilewr[_s8](const int8_t *op1, const int8_t *op2) : "WHILEWR Presult.B, Xop1, Xop2"
  public static unsafe Vector<sbyte> CreateWhileWriteAfterReadMask(sbyte* left, sbyte* right);

    /// svbool_t svwhilewr[_s16](const int16_t *op1, const int16_t *op2) : "WHILEWR Presult.H, Xop1, Xop2"
  public static unsafe Vector<short> CreateWhileWriteAfterReadMask(short* left, short* right);

    /// svbool_t svwhilewr[_s32](const int32_t *op1, const int32_t *op2) : "WHILEWR Presult.S, Xop1, Xop2"
  public static unsafe Vector<int> CreateWhileWriteAfterReadMask(int* left, int* right);

    /// svbool_t svwhilewr[_s64](const int64_t *op1, const int64_t *op2) : "WHILEWR Presult.D, Xop1, Xop2"
  public static unsafe Vector<long> CreateWhileWriteAfterReadMask(long* left, long* right);

    /// svbool_t svwhilewr[_u8](const uint8_t *op1, const uint8_t *op2) : "WHILEWR Presult.B, Xop1, Xop2"
  public static unsafe Vector<byte> CreateWhileWriteAfterReadMask(byte* left, byte* right);

    /// svbool_t svwhilewr[_u16](const uint16_t *op1, const uint16_t *op2) : "WHILEWR Presult.H, Xop1, Xop2"
  public static unsafe Vector<ushort> CreateWhileWriteAfterReadMask(ushort* left, ushort* right);

    /// svbool_t svwhilewr[_u32](const uint32_t *op1, const uint32_t *op2) : "WHILEWR Presult.S, Xop1, Xop2"
  public static unsafe Vector<uint> CreateWhileWriteAfterReadMask(uint* left, uint* right);

    /// svbool_t svwhilewr[_u64](const uint64_t *op1, const uint64_t *op2) : "WHILEWR Presult.D, Xop1, Xop2"
  public static unsafe Vector<ulong> CreateWhileWriteAfterReadMask(ulong* left, ulong* right);


    /// Match : Detect any matching elements

    /// svbool_t svmatch[_s8](svbool_t pg, svint8_t op1, svint8_t op2) : "MATCH Presult.B, Pg/Z, Zop1.B, Zop2.B"
  public static unsafe Vector<sbyte> Match(Vector<sbyte> mask, Vector<sbyte> left, Vector<sbyte> right);

    /// svbool_t svmatch[_s16](svbool_t pg, svint16_t op1, svint16_t op2) : "MATCH Presult.H, Pg/Z, Zop1.H, Zop2.H"
  public static unsafe Vector<short> Match(Vector<short> mask, Vector<short> left, Vector<short> right);

    /// svbool_t svmatch[_u8](svbool_t pg, svuint8_t op1, svuint8_t op2) : "MATCH Presult.B, Pg/Z, Zop1.B, Zop2.B"
  public static unsafe Vector<byte> Match(Vector<byte> mask, Vector<byte> left, Vector<byte> right);

    /// svbool_t svmatch[_u16](svbool_t pg, svuint16_t op1, svuint16_t op2) : "MATCH Presult.H, Pg/Z, Zop1.H, Zop2.H"
  public static unsafe Vector<ushort> Match(Vector<ushort> mask, Vector<ushort> left, Vector<ushort> right);


    /// NoMatch : Detect no matching elements

    /// svbool_t svnmatch[_s8](svbool_t pg, svint8_t op1, svint8_t op2) : "NMATCH Presult.B, Pg/Z, Zop1.B, Zop2.B"
  public static unsafe Vector<sbyte> NoMatch(Vector<sbyte> mask, Vector<sbyte> left, Vector<sbyte> right);

    /// svbool_t svnmatch[_s16](svbool_t pg, svint16_t op1, svint16_t op2) : "NMATCH Presult.H, Pg/Z, Zop1.H, Zop2.H"
  public static unsafe Vector<short> NoMatch(Vector<short> mask, Vector<short> left, Vector<short> right);

    /// svbool_t svnmatch[_u8](svbool_t pg, svuint8_t op1, svuint8_t op2) : "NMATCH Presult.B, Pg/Z, Zop1.B, Zop2.B"
  public static unsafe Vector<byte> NoMatch(Vector<byte> mask, Vector<byte> left, Vector<byte> right);

    /// svbool_t svnmatch[_u16](svbool_t pg, svuint16_t op1, svuint16_t op2) : "NMATCH Presult.H, Pg/Z, Zop1.H, Zop2.H"
  public static unsafe Vector<ushort> NoMatch(Vector<ushort> mask, Vector<ushort> left, Vector<ushort> right);


    /// SaturatingExtractNarrowingLower : Saturating extract narrow (bottom)

    /// svint8_t svqxtnb[_s16](svint16_t op) : "SQXTNB Zresult.B, Zop.H"
  public static unsafe Vector<sbyte> SaturatingExtractNarrowingLower(Vector<short> value);

    /// svint16_t svqxtnb[_s32](svint32_t op) : "SQXTNB Zresult.H, Zop.S"
  public static unsafe Vector<short> SaturatingExtractNarrowingLower(Vector<int> value);

    /// svint32_t svqxtnb[_s64](svint64_t op) : "SQXTNB Zresult.S, Zop.D"
  public static unsafe Vector<int> SaturatingExtractNarrowingLower(Vector<long> value);

    /// svuint8_t svqxtnb[_u16](svuint16_t op) : "UQXTNB Zresult.B, Zop.H"
  public static unsafe Vector<byte> SaturatingExtractNarrowingLower(Vector<ushort> value);

    /// svuint16_t svqxtnb[_u32](svuint32_t op) : "UQXTNB Zresult.H, Zop.S"
  public static unsafe Vector<ushort> SaturatingExtractNarrowingLower(Vector<uint> value);

    /// svuint32_t svqxtnb[_u64](svuint64_t op) : "UQXTNB Zresult.S, Zop.D"
  public static unsafe Vector<uint> SaturatingExtractNarrowingLower(Vector<ulong> value);


    /// SaturatingExtractNarrowingUpper : Saturating extract narrow (top)

    /// svint8_t svqxtnt[_s16](svint8_t even, svint16_t op) : "SQXTNT Ztied.B, Zop.H"
  public static unsafe Vector<sbyte> SaturatingExtractNarrowingUpper(Vector<sbyte> even, Vector<short> op);

    /// svint16_t svqxtnt[_s32](svint16_t even, svint32_t op) : "SQXTNT Ztied.H, Zop.S"
  public static unsafe Vector<short> SaturatingExtractNarrowingUpper(Vector<short> even, Vector<int> op);

    /// svint32_t svqxtnt[_s64](svint32_t even, svint64_t op) : "SQXTNT Ztied.S, Zop.D"
  public static unsafe Vector<int> SaturatingExtractNarrowingUpper(Vector<int> even, Vector<long> op);

    /// svuint8_t svqxtnt[_u16](svuint8_t even, svuint16_t op) : "UQXTNT Ztied.B, Zop.H"
  public static unsafe Vector<byte> SaturatingExtractNarrowingUpper(Vector<byte> even, Vector<ushort> op);

    /// svuint16_t svqxtnt[_u32](svuint16_t even, svuint32_t op) : "UQXTNT Ztied.H, Zop.S"
  public static unsafe Vector<ushort> SaturatingExtractNarrowingUpper(Vector<ushort> even, Vector<uint> op);

    /// svuint32_t svqxtnt[_u64](svuint32_t even, svuint64_t op) : "UQXTNT Ztied.S, Zop.D"
  public static unsafe Vector<uint> SaturatingExtractNarrowingUpper(Vector<uint> even, Vector<ulong> op);


    /// SaturatingExtractUnsignedNarrowingLower : Saturating extract unsigned narrow (bottom)

    /// svuint8_t svqxtunb[_s16](svint16_t op) : "SQXTUNB Zresult.B, Zop.H"
  public static unsafe Vector<byte> SaturatingExtractUnsignedNarrowingLower(Vector<short> value);

    /// svuint16_t svqxtunb[_s32](svint32_t op) : "SQXTUNB Zresult.H, Zop.S"
  public static unsafe Vector<ushort> SaturatingExtractUnsignedNarrowingLower(Vector<int> value);

    /// svuint32_t svqxtunb[_s64](svint64_t op) : "SQXTUNB Zresult.S, Zop.D"
  public static unsafe Vector<uint> SaturatingExtractUnsignedNarrowingLower(Vector<long> value);


    /// SaturatingExtractUnsignedNarrowingUpper : Saturating extract unsigned narrow (top)

    /// svuint8_t svqxtunt[_s16](svuint8_t even, svint16_t op) : "SQXTUNT Ztied.B, Zop.H"
  public static unsafe Vector<byte> SaturatingExtractUnsignedNarrowingUpper(Vector<byte> even, Vector<short> op);

    /// svuint16_t svqxtunt[_s32](svuint16_t even, svint32_t op) : "SQXTUNT Ztied.H, Zop.S"
  public static unsafe Vector<ushort> SaturatingExtractUnsignedNarrowingUpper(Vector<ushort> even, Vector<int> op);

    /// svuint32_t svqxtunt[_s64](svuint32_t even, svint64_t op) : "SQXTUNT Ztied.S, Zop.D"
  public static unsafe Vector<uint> SaturatingExtractUnsignedNarrowingUpper(Vector<uint> even, Vector<long> op);


  /// total method signatures: 78
  /// total method names:      10
}


  /// Total ACLE covered across API:      78

