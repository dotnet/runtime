namespace System.Runtime.Intrinsics.Arm;

/// VectorT Summary
public abstract partial class Sve : AdvSimd /// Feature: FEAT_SVE  Category: bitmanipulate
{

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> DuplicateSelectedScalarToVector(Vector<T> data, [ConstantExpected] byte index); // DUP or TBL

  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> ReverseBits(Vector<T> value); // RBIT // predicated, MOVPRFX

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> ReverseElement(Vector<T> value); // REV

  /// T: int, long, uint, ulong
  public static unsafe Vector<T> ReverseElement16(Vector<T> value); // REVH // predicated, MOVPRFX

  /// T: long, ulong
  public static unsafe Vector<T> ReverseElement32(Vector<T> value); // REVW // predicated, MOVPRFX

  /// T: short, int, long, ushort, uint, ulong
  public static unsafe Vector<T> ReverseElement8(Vector<T> value); // REVB // predicated, MOVPRFX

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> Splice(Vector<T> mask, Vector<T> left, Vector<T> right); // SPLICE // MOVPRFX

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> TransposeEven(Vector<T> left, Vector<T> right); // TRN1

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> TransposeOdd(Vector<T> left, Vector<T> right); // TRN2

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> UnzipEven(Vector<T> left, Vector<T> right); // UZP1

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> UnzipOdd(Vector<T> left, Vector<T> right); // UZP2

  /// T: [float, uint], [double, ulong], [sbyte, byte], [short, ushort], [int, uint], [long, ulong]
  public static unsafe Vector<T> VectorTableLookup(Vector<T> data, Vector<T2> indices); // TBL

  /// T: byte, ushort, uint, ulong
  public static unsafe Vector<T> VectorTableLookup(Vector<T> data, Vector<T> indices); // TBL

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> ZipHigh(Vector<T> left, Vector<T> right); // ZIP2

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> ZipLow(Vector<T> left, Vector<T> right); // ZIP1

  /// total method signatures: 15

}


/// Full API
public abstract partial class Sve : AdvSimd /// Feature: FEAT_SVE  Category: bitmanipulate
{
    /// DuplicateSelectedScalarToVector : Broadcast a scalar value

    /// svfloat32_t svdup_lane[_f32](svfloat32_t data, uint32_t index) : "DUP Zresult.S, Zdata.S[index]" or "TBL Zresult.S, Zdata.S, Zindex.S"
    /// svfloat32_t svdupq_lane[_f32](svfloat32_t data, uint64_t index) : "DUP Zresult.Q, Zdata.Q[index]" or "TBL Zresult.D, Zdata.D, Zindices_d.D"
  public static unsafe Vector<float> DuplicateSelectedScalarToVector(Vector<float> data, [ConstantExpected] byte index);

    /// svfloat64_t svdup_lane[_f64](svfloat64_t data, uint64_t index) : "DUP Zresult.D, Zdata.D[index]" or "TBL Zresult.D, Zdata.D, Zindex.D"
    /// svfloat64_t svdupq_lane[_f64](svfloat64_t data, uint64_t index) : "DUP Zresult.Q, Zdata.Q[index]" or "TBL Zresult.D, Zdata.D, Zindices_d.D"
  public static unsafe Vector<double> DuplicateSelectedScalarToVector(Vector<double> data, [ConstantExpected] byte index);

    /// svint8_t svdup_lane[_s8](svint8_t data, uint8_t index) : "DUP Zresult.B, Zdata.B[index]" or "TBL Zresult.B, Zdata.B, Zindex.B"
    /// svint8_t svdupq_lane[_s8](svint8_t data, uint64_t index) : "DUP Zresult.Q, Zdata.Q[index]" or "TBL Zresult.D, Zdata.D, Zindices_d.D"
  public static unsafe Vector<sbyte> DuplicateSelectedScalarToVector(Vector<sbyte> data, [ConstantExpected] byte index);

    /// svint16_t svdup_lane[_s16](svint16_t data, uint16_t index) : "DUP Zresult.H, Zdata.H[index]" or "TBL Zresult.H, Zdata.H, Zindex.H"
    /// svint16_t svdupq_lane[_s16](svint16_t data, uint64_t index) : "DUP Zresult.Q, Zdata.Q[index]" or "TBL Zresult.D, Zdata.D, Zindices_d.D"
  public static unsafe Vector<short> DuplicateSelectedScalarToVector(Vector<short> data, [ConstantExpected] byte index);

    /// svint32_t svdup_lane[_s32](svint32_t data, uint32_t index) : "DUP Zresult.S, Zdata.S[index]" or "TBL Zresult.S, Zdata.S, Zindex.S"
    /// svint32_t svdupq_lane[_s32](svint32_t data, uint64_t index) : "DUP Zresult.Q, Zdata.Q[index]" or "TBL Zresult.D, Zdata.D, Zindices_d.D"
  public static unsafe Vector<int> DuplicateSelectedScalarToVector(Vector<int> data, [ConstantExpected] byte index);

    /// svint64_t svdup_lane[_s64](svint64_t data, uint64_t index) : "DUP Zresult.D, Zdata.D[index]" or "TBL Zresult.D, Zdata.D, Zindex.D"
    /// svint64_t svdupq_lane[_s64](svint64_t data, uint64_t index) : "DUP Zresult.Q, Zdata.Q[index]" or "TBL Zresult.D, Zdata.D, Zindices_d.D"
  public static unsafe Vector<long> DuplicateSelectedScalarToVector(Vector<long> data, [ConstantExpected] byte index);

    /// svuint8_t svdup_lane[_u8](svuint8_t data, uint8_t index) : "DUP Zresult.B, Zdata.B[index]" or "TBL Zresult.B, Zdata.B, Zindex.B"
    /// svuint8_t svdupq_lane[_u8](svuint8_t data, uint64_t index) : "DUP Zresult.Q, Zdata.Q[index]" or "TBL Zresult.D, Zdata.D, Zindices_d.D"
  public static unsafe Vector<byte> DuplicateSelectedScalarToVector(Vector<byte> data, [ConstantExpected] byte index);

    /// svuint16_t svdup_lane[_u16](svuint16_t data, uint16_t index) : "DUP Zresult.H, Zdata.H[index]" or "TBL Zresult.H, Zdata.H, Zindex.H"
    /// svuint16_t svdupq_lane[_u16](svuint16_t data, uint64_t index) : "DUP Zresult.Q, Zdata.Q[index]" or "TBL Zresult.D, Zdata.D, Zindices_d.D"
  public static unsafe Vector<ushort> DuplicateSelectedScalarToVector(Vector<ushort> data, [ConstantExpected] byte index);

    /// svuint32_t svdup_lane[_u32](svuint32_t data, uint32_t index) : "DUP Zresult.S, Zdata.S[index]" or "TBL Zresult.S, Zdata.S, Zindex.S"
    /// svuint32_t svdupq_lane[_u32](svuint32_t data, uint64_t index) : "DUP Zresult.Q, Zdata.Q[index]" or "TBL Zresult.D, Zdata.D, Zindices_d.D"
  public static unsafe Vector<uint> DuplicateSelectedScalarToVector(Vector<uint> data, [ConstantExpected] byte index);

    /// svuint64_t svdup_lane[_u64](svuint64_t data, uint64_t index) : "DUP Zresult.D, Zdata.D[index]" or "TBL Zresult.D, Zdata.D, Zindex.D"
    /// svuint64_t svdupq_lane[_u64](svuint64_t data, uint64_t index) : "DUP Zresult.Q, Zdata.Q[index]" or "TBL Zresult.D, Zdata.D, Zindices_d.D"
  public static unsafe Vector<ulong> DuplicateSelectedScalarToVector(Vector<ulong> data, [ConstantExpected] byte index);


    /// ReverseBits : Reverse bits

    /// svint8_t svrbit[_s8]_m(svint8_t inactive, svbool_t pg, svint8_t op) : "RBIT Ztied.B, Pg/M, Zop.B" or "MOVPRFX Zresult, Zinactive; RBIT Zresult.B, Pg/M, Zop.B"
    /// svint8_t svrbit[_s8]_x(svbool_t pg, svint8_t op) : "RBIT Ztied.B, Pg/M, Ztied.B" or "MOVPRFX Zresult, Zop; RBIT Zresult.B, Pg/M, Zop.B"
    /// svint8_t svrbit[_s8]_z(svbool_t pg, svint8_t op) : "MOVPRFX Zresult.B, Pg/Z, Zop.B; RBIT Zresult.B, Pg/M, Zop.B"
  public static unsafe Vector<sbyte> ReverseBits(Vector<sbyte> value);

    /// svint16_t svrbit[_s16]_m(svint16_t inactive, svbool_t pg, svint16_t op) : "RBIT Ztied.H, Pg/M, Zop.H" or "MOVPRFX Zresult, Zinactive; RBIT Zresult.H, Pg/M, Zop.H"
    /// svint16_t svrbit[_s16]_x(svbool_t pg, svint16_t op) : "RBIT Ztied.H, Pg/M, Ztied.H" or "MOVPRFX Zresult, Zop; RBIT Zresult.H, Pg/M, Zop.H"
    /// svint16_t svrbit[_s16]_z(svbool_t pg, svint16_t op) : "MOVPRFX Zresult.H, Pg/Z, Zop.H; RBIT Zresult.H, Pg/M, Zop.H"
  public static unsafe Vector<short> ReverseBits(Vector<short> value);

    /// svint32_t svrbit[_s32]_m(svint32_t inactive, svbool_t pg, svint32_t op) : "RBIT Ztied.S, Pg/M, Zop.S" or "MOVPRFX Zresult, Zinactive; RBIT Zresult.S, Pg/M, Zop.S"
    /// svint32_t svrbit[_s32]_x(svbool_t pg, svint32_t op) : "RBIT Ztied.S, Pg/M, Ztied.S" or "MOVPRFX Zresult, Zop; RBIT Zresult.S, Pg/M, Zop.S"
    /// svint32_t svrbit[_s32]_z(svbool_t pg, svint32_t op) : "MOVPRFX Zresult.S, Pg/Z, Zop.S; RBIT Zresult.S, Pg/M, Zop.S"
  public static unsafe Vector<int> ReverseBits(Vector<int> value);

    /// svint64_t svrbit[_s64]_m(svint64_t inactive, svbool_t pg, svint64_t op) : "RBIT Ztied.D, Pg/M, Zop.D" or "MOVPRFX Zresult, Zinactive; RBIT Zresult.D, Pg/M, Zop.D"
    /// svint64_t svrbit[_s64]_x(svbool_t pg, svint64_t op) : "RBIT Ztied.D, Pg/M, Ztied.D" or "MOVPRFX Zresult, Zop; RBIT Zresult.D, Pg/M, Zop.D"
    /// svint64_t svrbit[_s64]_z(svbool_t pg, svint64_t op) : "MOVPRFX Zresult.D, Pg/Z, Zop.D; RBIT Zresult.D, Pg/M, Zop.D"
  public static unsafe Vector<long> ReverseBits(Vector<long> value);

    /// svuint8_t svrbit[_u8]_m(svuint8_t inactive, svbool_t pg, svuint8_t op) : "RBIT Ztied.B, Pg/M, Zop.B" or "MOVPRFX Zresult, Zinactive; RBIT Zresult.B, Pg/M, Zop.B"
    /// svuint8_t svrbit[_u8]_x(svbool_t pg, svuint8_t op) : "RBIT Ztied.B, Pg/M, Ztied.B" or "MOVPRFX Zresult, Zop; RBIT Zresult.B, Pg/M, Zop.B"
    /// svuint8_t svrbit[_u8]_z(svbool_t pg, svuint8_t op) : "MOVPRFX Zresult.B, Pg/Z, Zop.B; RBIT Zresult.B, Pg/M, Zop.B"
  public static unsafe Vector<byte> ReverseBits(Vector<byte> value);

    /// svuint16_t svrbit[_u16]_m(svuint16_t inactive, svbool_t pg, svuint16_t op) : "RBIT Ztied.H, Pg/M, Zop.H" or "MOVPRFX Zresult, Zinactive; RBIT Zresult.H, Pg/M, Zop.H"
    /// svuint16_t svrbit[_u16]_x(svbool_t pg, svuint16_t op) : "RBIT Ztied.H, Pg/M, Ztied.H" or "MOVPRFX Zresult, Zop; RBIT Zresult.H, Pg/M, Zop.H"
    /// svuint16_t svrbit[_u16]_z(svbool_t pg, svuint16_t op) : "MOVPRFX Zresult.H, Pg/Z, Zop.H; RBIT Zresult.H, Pg/M, Zop.H"
  public static unsafe Vector<ushort> ReverseBits(Vector<ushort> value);

    /// svuint32_t svrbit[_u32]_m(svuint32_t inactive, svbool_t pg, svuint32_t op) : "RBIT Ztied.S, Pg/M, Zop.S" or "MOVPRFX Zresult, Zinactive; RBIT Zresult.S, Pg/M, Zop.S"
    /// svuint32_t svrbit[_u32]_x(svbool_t pg, svuint32_t op) : "RBIT Ztied.S, Pg/M, Ztied.S" or "MOVPRFX Zresult, Zop; RBIT Zresult.S, Pg/M, Zop.S"
    /// svuint32_t svrbit[_u32]_z(svbool_t pg, svuint32_t op) : "MOVPRFX Zresult.S, Pg/Z, Zop.S; RBIT Zresult.S, Pg/M, Zop.S"
  public static unsafe Vector<uint> ReverseBits(Vector<uint> value);

    /// svuint64_t svrbit[_u64]_m(svuint64_t inactive, svbool_t pg, svuint64_t op) : "RBIT Ztied.D, Pg/M, Zop.D" or "MOVPRFX Zresult, Zinactive; RBIT Zresult.D, Pg/M, Zop.D"
    /// svuint64_t svrbit[_u64]_x(svbool_t pg, svuint64_t op) : "RBIT Ztied.D, Pg/M, Ztied.D" or "MOVPRFX Zresult, Zop; RBIT Zresult.D, Pg/M, Zop.D"
    /// svuint64_t svrbit[_u64]_z(svbool_t pg, svuint64_t op) : "MOVPRFX Zresult.D, Pg/Z, Zop.D; RBIT Zresult.D, Pg/M, Zop.D"
  public static unsafe Vector<ulong> ReverseBits(Vector<ulong> value);


    /// ReverseElement : Reverse all elements

    /// svfloat32_t svrev[_f32](svfloat32_t op) : "REV Zresult.S, Zop.S"
  public static unsafe Vector<float> ReverseElement(Vector<float> value);

    /// svfloat64_t svrev[_f64](svfloat64_t op) : "REV Zresult.D, Zop.D"
  public static unsafe Vector<double> ReverseElement(Vector<double> value);

    /// svint8_t svrev[_s8](svint8_t op) : "REV Zresult.B, Zop.B"
  public static unsafe Vector<sbyte> ReverseElement(Vector<sbyte> value);

    /// svint16_t svrev[_s16](svint16_t op) : "REV Zresult.H, Zop.H"
  public static unsafe Vector<short> ReverseElement(Vector<short> value);

    /// svint32_t svrev[_s32](svint32_t op) : "REV Zresult.S, Zop.S"
  public static unsafe Vector<int> ReverseElement(Vector<int> value);

    /// svint64_t svrev[_s64](svint64_t op) : "REV Zresult.D, Zop.D"
  public static unsafe Vector<long> ReverseElement(Vector<long> value);

    /// svuint8_t svrev[_u8](svuint8_t op) : "REV Zresult.B, Zop.B"
    /// svbool_t svrev_b8(svbool_t op) : "REV Presult.B, Pop.B"
  public static unsafe Vector<byte> ReverseElement(Vector<byte> value);

    /// svuint16_t svrev[_u16](svuint16_t op) : "REV Zresult.H, Zop.H"
    /// svbool_t svrev_b16(svbool_t op) : "REV Presult.H, Pop.H"
  public static unsafe Vector<ushort> ReverseElement(Vector<ushort> value);

    /// svuint32_t svrev[_u32](svuint32_t op) : "REV Zresult.S, Zop.S"
    /// svbool_t svrev_b32(svbool_t op) : "REV Presult.S, Pop.S"
  public static unsafe Vector<uint> ReverseElement(Vector<uint> value);

    /// svuint64_t svrev[_u64](svuint64_t op) : "REV Zresult.D, Zop.D"
    /// svbool_t svrev_b64(svbool_t op) : "REV Presult.D, Pop.D"
  public static unsafe Vector<ulong> ReverseElement(Vector<ulong> value);


    /// ReverseElement16 : Reverse halfwords within elements

    /// svint32_t svrevh[_s32]_m(svint32_t inactive, svbool_t pg, svint32_t op) : "REVH Ztied.S, Pg/M, Zop.S" or "MOVPRFX Zresult, Zinactive; REVH Zresult.S, Pg/M, Zop.S"
    /// svint32_t svrevh[_s32]_x(svbool_t pg, svint32_t op) : "REVH Ztied.S, Pg/M, Ztied.S" or "MOVPRFX Zresult, Zop; REVH Zresult.S, Pg/M, Zop.S"
    /// svint32_t svrevh[_s32]_z(svbool_t pg, svint32_t op) : "MOVPRFX Zresult.S, Pg/Z, Zop.S; REVH Zresult.S, Pg/M, Zop.S"
  public static unsafe Vector<int> ReverseElement16(Vector<int> value);

    /// svint64_t svrevh[_s64]_m(svint64_t inactive, svbool_t pg, svint64_t op) : "REVH Ztied.D, Pg/M, Zop.D" or "MOVPRFX Zresult, Zinactive; REVH Zresult.D, Pg/M, Zop.D"
    /// svint64_t svrevh[_s64]_x(svbool_t pg, svint64_t op) : "REVH Ztied.D, Pg/M, Ztied.D" or "MOVPRFX Zresult, Zop; REVH Zresult.D, Pg/M, Zop.D"
    /// svint64_t svrevh[_s64]_z(svbool_t pg, svint64_t op) : "MOVPRFX Zresult.D, Pg/Z, Zop.D; REVH Zresult.D, Pg/M, Zop.D"
  public static unsafe Vector<long> ReverseElement16(Vector<long> value);

    /// svuint32_t svrevh[_u32]_m(svuint32_t inactive, svbool_t pg, svuint32_t op) : "REVH Ztied.S, Pg/M, Zop.S" or "MOVPRFX Zresult, Zinactive; REVH Zresult.S, Pg/M, Zop.S"
    /// svuint32_t svrevh[_u32]_x(svbool_t pg, svuint32_t op) : "REVH Ztied.S, Pg/M, Ztied.S" or "MOVPRFX Zresult, Zop; REVH Zresult.S, Pg/M, Zop.S"
    /// svuint32_t svrevh[_u32]_z(svbool_t pg, svuint32_t op) : "MOVPRFX Zresult.S, Pg/Z, Zop.S; REVH Zresult.S, Pg/M, Zop.S"
  public static unsafe Vector<uint> ReverseElement16(Vector<uint> value);

    /// svuint64_t svrevh[_u64]_m(svuint64_t inactive, svbool_t pg, svuint64_t op) : "REVH Ztied.D, Pg/M, Zop.D" or "MOVPRFX Zresult, Zinactive; REVH Zresult.D, Pg/M, Zop.D"
    /// svuint64_t svrevh[_u64]_x(svbool_t pg, svuint64_t op) : "REVH Ztied.D, Pg/M, Ztied.D" or "MOVPRFX Zresult, Zop; REVH Zresult.D, Pg/M, Zop.D"
    /// svuint64_t svrevh[_u64]_z(svbool_t pg, svuint64_t op) : "MOVPRFX Zresult.D, Pg/Z, Zop.D; REVH Zresult.D, Pg/M, Zop.D"
  public static unsafe Vector<ulong> ReverseElement16(Vector<ulong> value);


    /// ReverseElement32 : Reverse words within elements

    /// svint64_t svrevw[_s64]_m(svint64_t inactive, svbool_t pg, svint64_t op) : "REVW Ztied.D, Pg/M, Zop.D" or "MOVPRFX Zresult, Zinactive; REVW Zresult.D, Pg/M, Zop.D"
    /// svint64_t svrevw[_s64]_x(svbool_t pg, svint64_t op) : "REVW Ztied.D, Pg/M, Ztied.D" or "MOVPRFX Zresult, Zop; REVW Zresult.D, Pg/M, Zop.D"
    /// svint64_t svrevw[_s64]_z(svbool_t pg, svint64_t op) : "MOVPRFX Zresult.D, Pg/Z, Zop.D; REVW Zresult.D, Pg/M, Zop.D"
  public static unsafe Vector<long> ReverseElement32(Vector<long> value);

    /// svuint64_t svrevw[_u64]_m(svuint64_t inactive, svbool_t pg, svuint64_t op) : "REVW Ztied.D, Pg/M, Zop.D" or "MOVPRFX Zresult, Zinactive; REVW Zresult.D, Pg/M, Zop.D"
    /// svuint64_t svrevw[_u64]_x(svbool_t pg, svuint64_t op) : "REVW Ztied.D, Pg/M, Ztied.D" or "MOVPRFX Zresult, Zop; REVW Zresult.D, Pg/M, Zop.D"
    /// svuint64_t svrevw[_u64]_z(svbool_t pg, svuint64_t op) : "MOVPRFX Zresult.D, Pg/Z, Zop.D; REVW Zresult.D, Pg/M, Zop.D"
  public static unsafe Vector<ulong> ReverseElement32(Vector<ulong> value);


    /// ReverseElement8 : Reverse bytes within elements

    /// svint16_t svrevb[_s16]_m(svint16_t inactive, svbool_t pg, svint16_t op) : "REVB Ztied.H, Pg/M, Zop.H" or "MOVPRFX Zresult, Zinactive; REVB Zresult.H, Pg/M, Zop.H"
    /// svint16_t svrevb[_s16]_x(svbool_t pg, svint16_t op) : "REVB Ztied.H, Pg/M, Ztied.H" or "MOVPRFX Zresult, Zop; REVB Zresult.H, Pg/M, Zop.H"
    /// svint16_t svrevb[_s16]_z(svbool_t pg, svint16_t op) : "MOVPRFX Zresult.H, Pg/Z, Zop.H; REVB Zresult.H, Pg/M, Zop.H"
  public static unsafe Vector<short> ReverseElement8(Vector<short> value);

    /// svint32_t svrevb[_s32]_m(svint32_t inactive, svbool_t pg, svint32_t op) : "REVB Ztied.S, Pg/M, Zop.S" or "MOVPRFX Zresult, Zinactive; REVB Zresult.S, Pg/M, Zop.S"
    /// svint32_t svrevb[_s32]_x(svbool_t pg, svint32_t op) : "REVB Ztied.S, Pg/M, Ztied.S" or "MOVPRFX Zresult, Zop; REVB Zresult.S, Pg/M, Zop.S"
    /// svint32_t svrevb[_s32]_z(svbool_t pg, svint32_t op) : "MOVPRFX Zresult.S, Pg/Z, Zop.S; REVB Zresult.S, Pg/M, Zop.S"
  public static unsafe Vector<int> ReverseElement8(Vector<int> value);

    /// svint64_t svrevb[_s64]_m(svint64_t inactive, svbool_t pg, svint64_t op) : "REVB Ztied.D, Pg/M, Zop.D" or "MOVPRFX Zresult, Zinactive; REVB Zresult.D, Pg/M, Zop.D"
    /// svint64_t svrevb[_s64]_x(svbool_t pg, svint64_t op) : "REVB Ztied.D, Pg/M, Ztied.D" or "MOVPRFX Zresult, Zop; REVB Zresult.D, Pg/M, Zop.D"
    /// svint64_t svrevb[_s64]_z(svbool_t pg, svint64_t op) : "MOVPRFX Zresult.D, Pg/Z, Zop.D; REVB Zresult.D, Pg/M, Zop.D"
  public static unsafe Vector<long> ReverseElement8(Vector<long> value);

    /// svuint16_t svrevb[_u16]_m(svuint16_t inactive, svbool_t pg, svuint16_t op) : "REVB Ztied.H, Pg/M, Zop.H" or "MOVPRFX Zresult, Zinactive; REVB Zresult.H, Pg/M, Zop.H"
    /// svuint16_t svrevb[_u16]_x(svbool_t pg, svuint16_t op) : "REVB Ztied.H, Pg/M, Ztied.H" or "MOVPRFX Zresult, Zop; REVB Zresult.H, Pg/M, Zop.H"
    /// svuint16_t svrevb[_u16]_z(svbool_t pg, svuint16_t op) : "MOVPRFX Zresult.H, Pg/Z, Zop.H; REVB Zresult.H, Pg/M, Zop.H"
  public static unsafe Vector<ushort> ReverseElement8(Vector<ushort> value);

    /// svuint32_t svrevb[_u32]_m(svuint32_t inactive, svbool_t pg, svuint32_t op) : "REVB Ztied.S, Pg/M, Zop.S" or "MOVPRFX Zresult, Zinactive; REVB Zresult.S, Pg/M, Zop.S"
    /// svuint32_t svrevb[_u32]_x(svbool_t pg, svuint32_t op) : "REVB Ztied.S, Pg/M, Ztied.S" or "MOVPRFX Zresult, Zop; REVB Zresult.S, Pg/M, Zop.S"
    /// svuint32_t svrevb[_u32]_z(svbool_t pg, svuint32_t op) : "MOVPRFX Zresult.S, Pg/Z, Zop.S; REVB Zresult.S, Pg/M, Zop.S"
  public static unsafe Vector<uint> ReverseElement8(Vector<uint> value);

    /// svuint64_t svrevb[_u64]_m(svuint64_t inactive, svbool_t pg, svuint64_t op) : "REVB Ztied.D, Pg/M, Zop.D" or "MOVPRFX Zresult, Zinactive; REVB Zresult.D, Pg/M, Zop.D"
    /// svuint64_t svrevb[_u64]_x(svbool_t pg, svuint64_t op) : "REVB Ztied.D, Pg/M, Ztied.D" or "MOVPRFX Zresult, Zop; REVB Zresult.D, Pg/M, Zop.D"
    /// svuint64_t svrevb[_u64]_z(svbool_t pg, svuint64_t op) : "MOVPRFX Zresult.D, Pg/Z, Zop.D; REVB Zresult.D, Pg/M, Zop.D"
  public static unsafe Vector<ulong> ReverseElement8(Vector<ulong> value);


    /// Splice : Splice two vectors under predicate control

    /// svfloat32_t svsplice[_f32](svbool_t pg, svfloat32_t op1, svfloat32_t op2) : "SPLICE Ztied1.S, Pg, Ztied1.S, Zop2.S" or "MOVPRFX Zresult, Zop1; SPLICE Zresult.S, Pg, Zresult.S, Zop2.S"
  public static unsafe Vector<float> Splice(Vector<float> mask, Vector<float> left, Vector<float> right);

    /// svfloat64_t svsplice[_f64](svbool_t pg, svfloat64_t op1, svfloat64_t op2) : "SPLICE Ztied1.D, Pg, Ztied1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; SPLICE Zresult.D, Pg, Zresult.D, Zop2.D"
  public static unsafe Vector<double> Splice(Vector<double> mask, Vector<double> left, Vector<double> right);

    /// svint8_t svsplice[_s8](svbool_t pg, svint8_t op1, svint8_t op2) : "SPLICE Ztied1.B, Pg, Ztied1.B, Zop2.B" or "MOVPRFX Zresult, Zop1; SPLICE Zresult.B, Pg, Zresult.B, Zop2.B"
  public static unsafe Vector<sbyte> Splice(Vector<sbyte> mask, Vector<sbyte> left, Vector<sbyte> right);

    /// svint16_t svsplice[_s16](svbool_t pg, svint16_t op1, svint16_t op2) : "SPLICE Ztied1.H, Pg, Ztied1.H, Zop2.H" or "MOVPRFX Zresult, Zop1; SPLICE Zresult.H, Pg, Zresult.H, Zop2.H"
  public static unsafe Vector<short> Splice(Vector<short> mask, Vector<short> left, Vector<short> right);

    /// svint32_t svsplice[_s32](svbool_t pg, svint32_t op1, svint32_t op2) : "SPLICE Ztied1.S, Pg, Ztied1.S, Zop2.S" or "MOVPRFX Zresult, Zop1; SPLICE Zresult.S, Pg, Zresult.S, Zop2.S"
  public static unsafe Vector<int> Splice(Vector<int> mask, Vector<int> left, Vector<int> right);

    /// svint64_t svsplice[_s64](svbool_t pg, svint64_t op1, svint64_t op2) : "SPLICE Ztied1.D, Pg, Ztied1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; SPLICE Zresult.D, Pg, Zresult.D, Zop2.D"
  public static unsafe Vector<long> Splice(Vector<long> mask, Vector<long> left, Vector<long> right);

    /// svuint8_t svsplice[_u8](svbool_t pg, svuint8_t op1, svuint8_t op2) : "SPLICE Ztied1.B, Pg, Ztied1.B, Zop2.B" or "MOVPRFX Zresult, Zop1; SPLICE Zresult.B, Pg, Zresult.B, Zop2.B"
  public static unsafe Vector<byte> Splice(Vector<byte> mask, Vector<byte> left, Vector<byte> right);

    /// svuint16_t svsplice[_u16](svbool_t pg, svuint16_t op1, svuint16_t op2) : "SPLICE Ztied1.H, Pg, Ztied1.H, Zop2.H" or "MOVPRFX Zresult, Zop1; SPLICE Zresult.H, Pg, Zresult.H, Zop2.H"
  public static unsafe Vector<ushort> Splice(Vector<ushort> mask, Vector<ushort> left, Vector<ushort> right);

    /// svuint32_t svsplice[_u32](svbool_t pg, svuint32_t op1, svuint32_t op2) : "SPLICE Ztied1.S, Pg, Ztied1.S, Zop2.S" or "MOVPRFX Zresult, Zop1; SPLICE Zresult.S, Pg, Zresult.S, Zop2.S"
  public static unsafe Vector<uint> Splice(Vector<uint> mask, Vector<uint> left, Vector<uint> right);

    /// svuint64_t svsplice[_u64](svbool_t pg, svuint64_t op1, svuint64_t op2) : "SPLICE Ztied1.D, Pg, Ztied1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; SPLICE Zresult.D, Pg, Zresult.D, Zop2.D"
  public static unsafe Vector<ulong> Splice(Vector<ulong> mask, Vector<ulong> left, Vector<ulong> right);


    /// TransposeEven : Interleave even elements from two inputs

    /// svfloat32_t svtrn1[_f32](svfloat32_t op1, svfloat32_t op2) : "TRN1 Zresult.S, Zop1.S, Zop2.S"
  public static unsafe Vector<float> TransposeEven(Vector<float> left, Vector<float> right);

    /// svfloat64_t svtrn1[_f64](svfloat64_t op1, svfloat64_t op2) : "TRN1 Zresult.D, Zop1.D, Zop2.D"
  public static unsafe Vector<double> TransposeEven(Vector<double> left, Vector<double> right);

    /// svint8_t svtrn1[_s8](svint8_t op1, svint8_t op2) : "TRN1 Zresult.B, Zop1.B, Zop2.B"
  public static unsafe Vector<sbyte> TransposeEven(Vector<sbyte> left, Vector<sbyte> right);

    /// svint16_t svtrn1[_s16](svint16_t op1, svint16_t op2) : "TRN1 Zresult.H, Zop1.H, Zop2.H"
  public static unsafe Vector<short> TransposeEven(Vector<short> left, Vector<short> right);

    /// svint32_t svtrn1[_s32](svint32_t op1, svint32_t op2) : "TRN1 Zresult.S, Zop1.S, Zop2.S"
  public static unsafe Vector<int> TransposeEven(Vector<int> left, Vector<int> right);

    /// svint64_t svtrn1[_s64](svint64_t op1, svint64_t op2) : "TRN1 Zresult.D, Zop1.D, Zop2.D"
  public static unsafe Vector<long> TransposeEven(Vector<long> left, Vector<long> right);

    /// svuint8_t svtrn1[_u8](svuint8_t op1, svuint8_t op2) : "TRN1 Zresult.B, Zop1.B, Zop2.B"
    /// svbool_t svtrn1_b8(svbool_t op1, svbool_t op2) : "TRN1 Presult.B, Pop1.B, Pop2.B"
  public static unsafe Vector<byte> TransposeEven(Vector<byte> left, Vector<byte> right);

    /// svuint16_t svtrn1[_u16](svuint16_t op1, svuint16_t op2) : "TRN1 Zresult.H, Zop1.H, Zop2.H"
    /// svbool_t svtrn1_b16(svbool_t op1, svbool_t op2) : "TRN1 Presult.H, Pop1.H, Pop2.H"
  public static unsafe Vector<ushort> TransposeEven(Vector<ushort> left, Vector<ushort> right);

    /// svuint32_t svtrn1[_u32](svuint32_t op1, svuint32_t op2) : "TRN1 Zresult.S, Zop1.S, Zop2.S"
    /// svbool_t svtrn1_b32(svbool_t op1, svbool_t op2) : "TRN1 Presult.S, Pop1.S, Pop2.S"
  public static unsafe Vector<uint> TransposeEven(Vector<uint> left, Vector<uint> right);

    /// svuint64_t svtrn1[_u64](svuint64_t op1, svuint64_t op2) : "TRN1 Zresult.D, Zop1.D, Zop2.D"
    /// svbool_t svtrn1_b64(svbool_t op1, svbool_t op2) : "TRN1 Presult.D, Pop1.D, Pop2.D"
  public static unsafe Vector<ulong> TransposeEven(Vector<ulong> left, Vector<ulong> right);


    /// TransposeOdd : Interleave odd elements from two inputs

    /// svfloat32_t svtrn2[_f32](svfloat32_t op1, svfloat32_t op2) : "TRN2 Zresult.S, Zop1.S, Zop2.S"
  public static unsafe Vector<float> TransposeOdd(Vector<float> left, Vector<float> right);

    /// svfloat64_t svtrn2[_f64](svfloat64_t op1, svfloat64_t op2) : "TRN2 Zresult.D, Zop1.D, Zop2.D"
  public static unsafe Vector<double> TransposeOdd(Vector<double> left, Vector<double> right);

    /// svint8_t svtrn2[_s8](svint8_t op1, svint8_t op2) : "TRN2 Zresult.B, Zop1.B, Zop2.B"
  public static unsafe Vector<sbyte> TransposeOdd(Vector<sbyte> left, Vector<sbyte> right);

    /// svint16_t svtrn2[_s16](svint16_t op1, svint16_t op2) : "TRN2 Zresult.H, Zop1.H, Zop2.H"
  public static unsafe Vector<short> TransposeOdd(Vector<short> left, Vector<short> right);

    /// svint32_t svtrn2[_s32](svint32_t op1, svint32_t op2) : "TRN2 Zresult.S, Zop1.S, Zop2.S"
  public static unsafe Vector<int> TransposeOdd(Vector<int> left, Vector<int> right);

    /// svint64_t svtrn2[_s64](svint64_t op1, svint64_t op2) : "TRN2 Zresult.D, Zop1.D, Zop2.D"
  public static unsafe Vector<long> TransposeOdd(Vector<long> left, Vector<long> right);

    /// svuint8_t svtrn2[_u8](svuint8_t op1, svuint8_t op2) : "TRN2 Zresult.B, Zop1.B, Zop2.B"
    /// svbool_t svtrn2_b8(svbool_t op1, svbool_t op2) : "TRN2 Presult.B, Pop1.B, Pop2.B"
  public static unsafe Vector<byte> TransposeOdd(Vector<byte> left, Vector<byte> right);

    /// svuint16_t svtrn2[_u16](svuint16_t op1, svuint16_t op2) : "TRN2 Zresult.H, Zop1.H, Zop2.H"
    /// svbool_t svtrn2_b16(svbool_t op1, svbool_t op2) : "TRN2 Presult.H, Pop1.H, Pop2.H"
  public static unsafe Vector<ushort> TransposeOdd(Vector<ushort> left, Vector<ushort> right);

    /// svuint32_t svtrn2[_u32](svuint32_t op1, svuint32_t op2) : "TRN2 Zresult.S, Zop1.S, Zop2.S"
    /// svbool_t svtrn2_b32(svbool_t op1, svbool_t op2) : "TRN2 Presult.S, Pop1.S, Pop2.S"
  public static unsafe Vector<uint> TransposeOdd(Vector<uint> left, Vector<uint> right);

    /// svuint64_t svtrn2[_u64](svuint64_t op1, svuint64_t op2) : "TRN2 Zresult.D, Zop1.D, Zop2.D"
    /// svbool_t svtrn2_b64(svbool_t op1, svbool_t op2) : "TRN2 Presult.D, Pop1.D, Pop2.D"
  public static unsafe Vector<ulong> TransposeOdd(Vector<ulong> left, Vector<ulong> right);


    /// UnzipEven : Concatenate even elements from two inputs

    /// svfloat32_t svuzp1[_f32](svfloat32_t op1, svfloat32_t op2) : "UZP1 Zresult.S, Zop1.S, Zop2.S"
  public static unsafe Vector<float> UnzipEven(Vector<float> left, Vector<float> right);

    /// svfloat64_t svuzp1[_f64](svfloat64_t op1, svfloat64_t op2) : "UZP1 Zresult.D, Zop1.D, Zop2.D"
  public static unsafe Vector<double> UnzipEven(Vector<double> left, Vector<double> right);

    /// svint8_t svuzp1[_s8](svint8_t op1, svint8_t op2) : "UZP1 Zresult.B, Zop1.B, Zop2.B"
  public static unsafe Vector<sbyte> UnzipEven(Vector<sbyte> left, Vector<sbyte> right);

    /// svint16_t svuzp1[_s16](svint16_t op1, svint16_t op2) : "UZP1 Zresult.H, Zop1.H, Zop2.H"
  public static unsafe Vector<short> UnzipEven(Vector<short> left, Vector<short> right);

    /// svint32_t svuzp1[_s32](svint32_t op1, svint32_t op2) : "UZP1 Zresult.S, Zop1.S, Zop2.S"
  public static unsafe Vector<int> UnzipEven(Vector<int> left, Vector<int> right);

    /// svint64_t svuzp1[_s64](svint64_t op1, svint64_t op2) : "UZP1 Zresult.D, Zop1.D, Zop2.D"
  public static unsafe Vector<long> UnzipEven(Vector<long> left, Vector<long> right);

    /// svuint8_t svuzp1[_u8](svuint8_t op1, svuint8_t op2) : "UZP1 Zresult.B, Zop1.B, Zop2.B"
    /// svbool_t svuzp1_b8(svbool_t op1, svbool_t op2) : "UZP1 Presult.B, Pop1.B, Pop2.B"
  public static unsafe Vector<byte> UnzipEven(Vector<byte> left, Vector<byte> right);

    /// svuint16_t svuzp1[_u16](svuint16_t op1, svuint16_t op2) : "UZP1 Zresult.H, Zop1.H, Zop2.H"
    /// svbool_t svuzp1_b16(svbool_t op1, svbool_t op2) : "UZP1 Presult.H, Pop1.H, Pop2.H"
  public static unsafe Vector<ushort> UnzipEven(Vector<ushort> left, Vector<ushort> right);

    /// svuint32_t svuzp1[_u32](svuint32_t op1, svuint32_t op2) : "UZP1 Zresult.S, Zop1.S, Zop2.S"
    /// svbool_t svuzp1_b32(svbool_t op1, svbool_t op2) : "UZP1 Presult.S, Pop1.S, Pop2.S"
  public static unsafe Vector<uint> UnzipEven(Vector<uint> left, Vector<uint> right);

    /// svuint64_t svuzp1[_u64](svuint64_t op1, svuint64_t op2) : "UZP1 Zresult.D, Zop1.D, Zop2.D"
    /// svbool_t svuzp1_b64(svbool_t op1, svbool_t op2) : "UZP1 Presult.D, Pop1.D, Pop2.D"
  public static unsafe Vector<ulong> UnzipEven(Vector<ulong> left, Vector<ulong> right);


    /// UnzipOdd : Concatenate odd elements from two inputs

    /// svfloat32_t svuzp2[_f32](svfloat32_t op1, svfloat32_t op2) : "UZP2 Zresult.S, Zop1.S, Zop2.S"
  public static unsafe Vector<float> UnzipOdd(Vector<float> left, Vector<float> right);

    /// svfloat64_t svuzp2[_f64](svfloat64_t op1, svfloat64_t op2) : "UZP2 Zresult.D, Zop1.D, Zop2.D"
  public static unsafe Vector<double> UnzipOdd(Vector<double> left, Vector<double> right);

    /// svint8_t svuzp2[_s8](svint8_t op1, svint8_t op2) : "UZP2 Zresult.B, Zop1.B, Zop2.B"
  public static unsafe Vector<sbyte> UnzipOdd(Vector<sbyte> left, Vector<sbyte> right);

    /// svint16_t svuzp2[_s16](svint16_t op1, svint16_t op2) : "UZP2 Zresult.H, Zop1.H, Zop2.H"
  public static unsafe Vector<short> UnzipOdd(Vector<short> left, Vector<short> right);

    /// svint32_t svuzp2[_s32](svint32_t op1, svint32_t op2) : "UZP2 Zresult.S, Zop1.S, Zop2.S"
  public static unsafe Vector<int> UnzipOdd(Vector<int> left, Vector<int> right);

    /// svint64_t svuzp2[_s64](svint64_t op1, svint64_t op2) : "UZP2 Zresult.D, Zop1.D, Zop2.D"
  public static unsafe Vector<long> UnzipOdd(Vector<long> left, Vector<long> right);

    /// svuint8_t svuzp2[_u8](svuint8_t op1, svuint8_t op2) : "UZP2 Zresult.B, Zop1.B, Zop2.B"
    /// svbool_t svuzp2_b8(svbool_t op1, svbool_t op2) : "UZP2 Presult.B, Pop1.B, Pop2.B"
  public static unsafe Vector<byte> UnzipOdd(Vector<byte> left, Vector<byte> right);

    /// svuint16_t svuzp2[_u16](svuint16_t op1, svuint16_t op2) : "UZP2 Zresult.H, Zop1.H, Zop2.H"
    /// svbool_t svuzp2_b16(svbool_t op1, svbool_t op2) : "UZP2 Presult.H, Pop1.H, Pop2.H"
  public static unsafe Vector<ushort> UnzipOdd(Vector<ushort> left, Vector<ushort> right);

    /// svuint32_t svuzp2[_u32](svuint32_t op1, svuint32_t op2) : "UZP2 Zresult.S, Zop1.S, Zop2.S"
    /// svbool_t svuzp2_b32(svbool_t op1, svbool_t op2) : "UZP2 Presult.S, Pop1.S, Pop2.S"
  public static unsafe Vector<uint> UnzipOdd(Vector<uint> left, Vector<uint> right);

    /// svuint64_t svuzp2[_u64](svuint64_t op1, svuint64_t op2) : "UZP2 Zresult.D, Zop1.D, Zop2.D"
    /// svbool_t svuzp2_b64(svbool_t op1, svbool_t op2) : "UZP2 Presult.D, Pop1.D, Pop2.D"
  public static unsafe Vector<ulong> UnzipOdd(Vector<ulong> left, Vector<ulong> right);


    /// VectorTableLookup : Table lookup in single-vector table

    /// svfloat32_t svtbl[_f32](svfloat32_t data, svuint32_t indices) : "TBL Zresult.S, Zdata.S, Zindices.S"
  public static unsafe Vector<float> VectorTableLookup(Vector<float> data, Vector<uint> indices);

    /// svfloat64_t svtbl[_f64](svfloat64_t data, svuint64_t indices) : "TBL Zresult.D, Zdata.D, Zindices.D"
  public static unsafe Vector<double> VectorTableLookup(Vector<double> data, Vector<ulong> indices);

    /// svint8_t svtbl[_s8](svint8_t data, svuint8_t indices) : "TBL Zresult.B, Zdata.B, Zindices.B"
  public static unsafe Vector<sbyte> VectorTableLookup(Vector<sbyte> data, Vector<byte> indices);

    /// svint16_t svtbl[_s16](svint16_t data, svuint16_t indices) : "TBL Zresult.H, Zdata.H, Zindices.H"
  public static unsafe Vector<short> VectorTableLookup(Vector<short> data, Vector<ushort> indices);

    /// svint32_t svtbl[_s32](svint32_t data, svuint32_t indices) : "TBL Zresult.S, Zdata.S, Zindices.S"
  public static unsafe Vector<int> VectorTableLookup(Vector<int> data, Vector<uint> indices);

    /// svint64_t svtbl[_s64](svint64_t data, svuint64_t indices) : "TBL Zresult.D, Zdata.D, Zindices.D"
  public static unsafe Vector<long> VectorTableLookup(Vector<long> data, Vector<ulong> indices);

    /// svuint8_t svtbl[_u8](svuint8_t data, svuint8_t indices) : "TBL Zresult.B, Zdata.B, Zindices.B"
  public static unsafe Vector<byte> VectorTableLookup(Vector<byte> data, Vector<byte> indices);

    /// svuint16_t svtbl[_u16](svuint16_t data, svuint16_t indices) : "TBL Zresult.H, Zdata.H, Zindices.H"
  public static unsafe Vector<ushort> VectorTableLookup(Vector<ushort> data, Vector<ushort> indices);

    /// svuint32_t svtbl[_u32](svuint32_t data, svuint32_t indices) : "TBL Zresult.S, Zdata.S, Zindices.S"
  public static unsafe Vector<uint> VectorTableLookup(Vector<uint> data, Vector<uint> indices);

    /// svuint64_t svtbl[_u64](svuint64_t data, svuint64_t indices) : "TBL Zresult.D, Zdata.D, Zindices.D"
  public static unsafe Vector<ulong> VectorTableLookup(Vector<ulong> data, Vector<ulong> indices);


    /// ZipHigh : Interleave elements from high halves of two inputs

    /// svfloat32_t svzip2[_f32](svfloat32_t op1, svfloat32_t op2) : "ZIP2 Zresult.S, Zop1.S, Zop2.S"
  public static unsafe Vector<float> ZipHigh(Vector<float> left, Vector<float> right);

    /// svfloat64_t svzip2[_f64](svfloat64_t op1, svfloat64_t op2) : "ZIP2 Zresult.D, Zop1.D, Zop2.D"
  public static unsafe Vector<double> ZipHigh(Vector<double> left, Vector<double> right);

    /// svint8_t svzip2[_s8](svint8_t op1, svint8_t op2) : "ZIP2 Zresult.B, Zop1.B, Zop2.B"
  public static unsafe Vector<sbyte> ZipHigh(Vector<sbyte> left, Vector<sbyte> right);

    /// svint16_t svzip2[_s16](svint16_t op1, svint16_t op2) : "ZIP2 Zresult.H, Zop1.H, Zop2.H"
  public static unsafe Vector<short> ZipHigh(Vector<short> left, Vector<short> right);

    /// svint32_t svzip2[_s32](svint32_t op1, svint32_t op2) : "ZIP2 Zresult.S, Zop1.S, Zop2.S"
  public static unsafe Vector<int> ZipHigh(Vector<int> left, Vector<int> right);

    /// svint64_t svzip2[_s64](svint64_t op1, svint64_t op2) : "ZIP2 Zresult.D, Zop1.D, Zop2.D"
  public static unsafe Vector<long> ZipHigh(Vector<long> left, Vector<long> right);

    /// svuint8_t svzip2[_u8](svuint8_t op1, svuint8_t op2) : "ZIP2 Zresult.B, Zop1.B, Zop2.B"
    /// svbool_t svzip2_b8(svbool_t op1, svbool_t op2) : "ZIP2 Presult.B, Pop1.B, Pop2.B"
  public static unsafe Vector<byte> ZipHigh(Vector<byte> left, Vector<byte> right);

    /// svuint16_t svzip2[_u16](svuint16_t op1, svuint16_t op2) : "ZIP2 Zresult.H, Zop1.H, Zop2.H"
    /// svbool_t svzip2_b16(svbool_t op1, svbool_t op2) : "ZIP2 Presult.H, Pop1.H, Pop2.H"
  public static unsafe Vector<ushort> ZipHigh(Vector<ushort> left, Vector<ushort> right);

    /// svuint32_t svzip2[_u32](svuint32_t op1, svuint32_t op2) : "ZIP2 Zresult.S, Zop1.S, Zop2.S"
    /// svbool_t svzip2_b32(svbool_t op1, svbool_t op2) : "ZIP2 Presult.S, Pop1.S, Pop2.S"
  public static unsafe Vector<uint> ZipHigh(Vector<uint> left, Vector<uint> right);

    /// svuint64_t svzip2[_u64](svuint64_t op1, svuint64_t op2) : "ZIP2 Zresult.D, Zop1.D, Zop2.D"
    /// svbool_t svzip2_b64(svbool_t op1, svbool_t op2) : "ZIP2 Presult.D, Pop1.D, Pop2.D"
  public static unsafe Vector<ulong> ZipHigh(Vector<ulong> left, Vector<ulong> right);


    /// ZipLow : Interleave elements from low halves of two inputs

    /// svfloat32_t svzip1[_f32](svfloat32_t op1, svfloat32_t op2) : "ZIP1 Zresult.S, Zop1.S, Zop2.S"
  public static unsafe Vector<float> ZipLow(Vector<float> left, Vector<float> right);

    /// svfloat64_t svzip1[_f64](svfloat64_t op1, svfloat64_t op2) : "ZIP1 Zresult.D, Zop1.D, Zop2.D"
  public static unsafe Vector<double> ZipLow(Vector<double> left, Vector<double> right);

    /// svint8_t svzip1[_s8](svint8_t op1, svint8_t op2) : "ZIP1 Zresult.B, Zop1.B, Zop2.B"
  public static unsafe Vector<sbyte> ZipLow(Vector<sbyte> left, Vector<sbyte> right);

    /// svint16_t svzip1[_s16](svint16_t op1, svint16_t op2) : "ZIP1 Zresult.H, Zop1.H, Zop2.H"
  public static unsafe Vector<short> ZipLow(Vector<short> left, Vector<short> right);

    /// svint32_t svzip1[_s32](svint32_t op1, svint32_t op2) : "ZIP1 Zresult.S, Zop1.S, Zop2.S"
  public static unsafe Vector<int> ZipLow(Vector<int> left, Vector<int> right);

    /// svint64_t svzip1[_s64](svint64_t op1, svint64_t op2) : "ZIP1 Zresult.D, Zop1.D, Zop2.D"
  public static unsafe Vector<long> ZipLow(Vector<long> left, Vector<long> right);

    /// svuint8_t svzip1[_u8](svuint8_t op1, svuint8_t op2) : "ZIP1 Zresult.B, Zop1.B, Zop2.B"
    /// svbool_t svzip1_b8(svbool_t op1, svbool_t op2) : "ZIP1 Presult.B, Pop1.B, Pop2.B"
  public static unsafe Vector<byte> ZipLow(Vector<byte> left, Vector<byte> right);

    /// svuint16_t svzip1[_u16](svuint16_t op1, svuint16_t op2) : "ZIP1 Zresult.H, Zop1.H, Zop2.H"
    /// svbool_t svzip1_b16(svbool_t op1, svbool_t op2) : "ZIP1 Presult.H, Pop1.H, Pop2.H"
  public static unsafe Vector<ushort> ZipLow(Vector<ushort> left, Vector<ushort> right);

    /// svuint32_t svzip1[_u32](svuint32_t op1, svuint32_t op2) : "ZIP1 Zresult.S, Zop1.S, Zop2.S"
    /// svbool_t svzip1_b32(svbool_t op1, svbool_t op2) : "ZIP1 Presult.S, Pop1.S, Pop2.S"
  public static unsafe Vector<uint> ZipLow(Vector<uint> left, Vector<uint> right);

    /// svuint64_t svzip1[_u64](svuint64_t op1, svuint64_t op2) : "ZIP1 Zresult.D, Zop1.D, Zop2.D"
    /// svbool_t svzip1_b64(svbool_t op1, svbool_t op2) : "ZIP1 Presult.D, Pop1.D, Pop2.D"
  public static unsafe Vector<ulong> ZipLow(Vector<ulong> left, Vector<ulong> right);


  /// total method signatures: 120
  /// total method names:      16
}


  /// Rejected:
  ///   public static unsafe Vector<sbyte> CreateSeries(sbyte base, sbyte step); // svindex_s8
  ///   public static unsafe Vector<short> CreateSeries(short base, short step); // svindex_s16
  ///   public static unsafe Vector<int> CreateSeries(int base, int step); // svindex_s32
  ///   public static unsafe Vector<long> CreateSeries(long base, long step); // svindex_s64
  ///   public static unsafe Vector<byte> CreateSeries(byte base, byte step); // svindex_u8
  ///   public static unsafe Vector<ushort> CreateSeries(ushort base, ushort step); // svindex_u16
  ///   public static unsafe Vector<uint> CreateSeries(uint base, uint step); // svindex_u32
  ///   public static unsafe Vector<ulong> CreateSeries(ulong base, ulong step); // svindex_u64
  ///   public static unsafe Vector<float> DuplicateSelectedScalarToVector(float value); // svdup[_n]_f32 or svdup[_n]_f32_m or svdup[_n]_f32_x or svdup[_n]_f32_z
  ///   public static unsafe Vector<double> DuplicateSelectedScalarToVector(double value); // svdup[_n]_f64 or svdup[_n]_f64_m or svdup[_n]_f64_x or svdup[_n]_f64_z
  ///   public static unsafe Vector<sbyte> DuplicateSelectedScalarToVector(sbyte value); // svdup[_n]_s8 or svdup[_n]_s8_m or svdup[_n]_s8_x or svdup[_n]_s8_z
  ///   public static unsafe Vector<short> DuplicateSelectedScalarToVector(short value); // svdup[_n]_s16 or svdup[_n]_s16_m or svdup[_n]_s16_x or svdup[_n]_s16_z
  ///   public static unsafe Vector<int> DuplicateSelectedScalarToVector(int value); // svdup[_n]_s32 or svdup[_n]_s32_m or svdup[_n]_s32_x or svdup[_n]_s32_z
  ///   public static unsafe Vector<long> DuplicateSelectedScalarToVector(long value); // svdup[_n]_s64 or svdup[_n]_s64_m or svdup[_n]_s64_x or svdup[_n]_s64_z
  ///   public static unsafe Vector<byte> DuplicateSelectedScalarToVector(byte value); // svdup[_n]_u8 or svdup[_n]_u8_m or svdup[_n]_u8_x or svdup[_n]_u8_z
  ///   public static unsafe Vector<ushort> DuplicateSelectedScalarToVector(ushort value); // svdup[_n]_u16 or svdup[_n]_u16_m or svdup[_n]_u16_x or svdup[_n]_u16_z
  ///   public static unsafe Vector<uint> DuplicateSelectedScalarToVector(uint value); // svdup[_n]_u32 or svdup[_n]_u32_m or svdup[_n]_u32_x or svdup[_n]_u32_z
  ///   public static unsafe Vector<ulong> DuplicateSelectedScalarToVector(ulong value); // svdup[_n]_u64 or svdup[_n]_u64_m or svdup[_n]_u64_x or svdup[_n]_u64_z
  ///   public static unsafe Vector<byte> DuplicateSelectedScalarToVector(bool value); // svdup[_n]_b8
  ///   public static unsafe Vector<ushort> DuplicateSelectedScalarToVector(bool value); // svdup[_n]_b16
  ///   public static unsafe Vector<uint> DuplicateSelectedScalarToVector(bool value); // svdup[_n]_b32
  ///   public static unsafe Vector<ulong> DuplicateSelectedScalarToVector(bool value); // svdup[_n]_b64
  ///   public static unsafe Vector<sbyte> Move(Vector<sbyte> value); // svmov[_b]_z
  ///   public static unsafe Vector<short> Move(Vector<short> value); // svmov[_b]_z
  ///   public static unsafe Vector<int> Move(Vector<int> value); // svmov[_b]_z
  ///   public static unsafe Vector<long> Move(Vector<long> value); // svmov[_b]_z
  ///   public static unsafe Vector<byte> Move(Vector<byte> value); // svmov[_b]_z
  ///   public static unsafe Vector<ushort> Move(Vector<ushort> value); // svmov[_b]_z
  ///   public static unsafe Vector<uint> Move(Vector<uint> value); // svmov[_b]_z
  ///   public static unsafe Vector<ulong> Move(Vector<ulong> value); // svmov[_b]_z
  ///   Total Rejected: 30

  /// Total ACLE covered across API:      258

