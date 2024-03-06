namespace System.Runtime.Intrinsics.Arm;

/// VectorT Summary
public abstract partial class Sve : AdvSimd /// Feature: FEAT_SVE  Category: bitwise
{

  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> And(Vector<T> left, Vector<T> right); // AND // predicated, MOVPRFX

  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> AndAcross(Vector<T> value); // ANDV // predicated

  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> AndNot(Vector<T> left, Vector<T> right); // NAND // predicated

  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> BitwiseClear(Vector<T> left, Vector<T> right); // BIC // predicated, MOVPRFX

  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> BooleanNot(Vector<T> value); // CNOT // predicated, MOVPRFX

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> InsertIntoShiftedVector(Vector<T> left, T right); // INSR

  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> Not(Vector<T> value); // NOT or EOR // predicated, MOVPRFX

  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> Or(Vector<T> left, Vector<T> right); // ORR // predicated, MOVPRFX

  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> OrAcross(Vector<T> value); // ORV // predicated

  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> OrNot(Vector<T> left, Vector<T> right); // NOR or ORN // predicated

  /// T: [sbyte, byte], [short, ushort], [int, uint], [long, ulong], [sbyte, ulong], [short, ulong], [int, ulong], [byte, ulong], [ushort, ulong], [uint, ulong]
  public static unsafe Vector<T> ShiftLeftLogical(Vector<T> left, Vector<T2> right); // LSL or LSLR // predicated, MOVPRFX

  /// T: byte, ushort, uint, ulong
  public static unsafe Vector<T> ShiftLeftLogical(Vector<T> left, Vector<T> right); // LSL or LSLR // predicated, MOVPRFX

  /// T: [sbyte, byte], [short, ushort], [int, uint], [long, ulong], [sbyte, ulong], [short, ulong], [int, ulong]
  public static unsafe Vector<T> ShiftRightArithmetic(Vector<T> left, Vector<T2> right); // ASR or ASRR // predicated, MOVPRFX

  /// T: sbyte, short, int, long
  public static unsafe Vector<T> ShiftRightArithmeticForDivide(Vector<T> value, [ConstantExpected] byte control); // ASRD // predicated, MOVPRFX

  /// T: byte, ushort, uint, ulong
  public static unsafe Vector<T> ShiftRightLogical(Vector<T> left, Vector<T> right); // LSR or LSRR // predicated, MOVPRFX

  /// T: [byte, ulong], [ushort, ulong], [uint, ulong]
  public static unsafe Vector<T> ShiftRightLogical(Vector<T> left, Vector<T2> right); // LSR // predicated, MOVPRFX

  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> Xor(Vector<T> left, Vector<T> right); // EOR // predicated, MOVPRFX

  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> XorAcross(Vector<T> value); // EORV // predicated

  /// total method signatures: 18


  /// Optional Entries:

  public static unsafe Vector<sbyte> ShiftRightArithmetic(Vector<sbyte> left, byte right); // ASR or ASRR // predicated, MOVPRFX

  public static unsafe Vector<short> ShiftRightArithmetic(Vector<short> left, ushort right); // ASR or ASRR // predicated, MOVPRFX

  public static unsafe Vector<int> ShiftRightArithmetic(Vector<int> left, uint right); // ASR or ASRR // predicated, MOVPRFX

  /// T: long, sbyte, short, int
  public static unsafe Vector<T> ShiftRightArithmetic(Vector<T> left, ulong right); // ASR or ASRR // predicated, MOVPRFX

  /// total optional method signatures: 4

}


/// Full API
public abstract partial class Sve : AdvSimd /// Feature: FEAT_SVE  Category: bitwise
{
    /// And : Bitwise AND

    /// svint8_t svand[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2) : "AND Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "MOVPRFX Zresult, Zop1; AND Zresult.B, Pg/M, Zresult.B, Zop2.B"
    /// svint8_t svand[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2) : "AND Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "AND Ztied2.B, Pg/M, Ztied2.B, Zop1.B" or "AND Zresult.D, Zop1.D, Zop2.D"
    /// svint8_t svand[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2) : "MOVPRFX Zresult.B, Pg/Z, Zop1.B; AND Zresult.B, Pg/M, Zresult.B, Zop2.B" or "MOVPRFX Zresult.B, Pg/Z, Zop2.B; AND Zresult.B, Pg/M, Zresult.B, Zop1.B"
    /// svbool_t svand[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2) : "AND Presult.B, Pg/Z, Pop1.B, Pop2.B"
  public static unsafe Vector<sbyte> And(Vector<sbyte> left, Vector<sbyte> right);

    /// svint16_t svand[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2) : "AND Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "MOVPRFX Zresult, Zop1; AND Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svint16_t svand[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2) : "AND Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "AND Ztied2.H, Pg/M, Ztied2.H, Zop1.H" or "AND Zresult.D, Zop1.D, Zop2.D"
    /// svint16_t svand[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2) : "MOVPRFX Zresult.H, Pg/Z, Zop1.H; AND Zresult.H, Pg/M, Zresult.H, Zop2.H" or "MOVPRFX Zresult.H, Pg/Z, Zop2.H; AND Zresult.H, Pg/M, Zresult.H, Zop1.H"
    /// svbool_t svand[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2) : "AND Presult.B, Pg/Z, Pop1.B, Pop2.B"
  public static unsafe Vector<short> And(Vector<short> left, Vector<short> right);

    /// svint32_t svand[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2) : "AND Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "MOVPRFX Zresult, Zop1; AND Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svint32_t svand[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2) : "AND Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "AND Ztied2.S, Pg/M, Ztied2.S, Zop1.S" or "AND Zresult.D, Zop1.D, Zop2.D"
    /// svint32_t svand[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2) : "MOVPRFX Zresult.S, Pg/Z, Zop1.S; AND Zresult.S, Pg/M, Zresult.S, Zop2.S" or "MOVPRFX Zresult.S, Pg/Z, Zop2.S; AND Zresult.S, Pg/M, Zresult.S, Zop1.S"
    /// svbool_t svand[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2) : "AND Presult.B, Pg/Z, Pop1.B, Pop2.B"
  public static unsafe Vector<int> And(Vector<int> left, Vector<int> right);

    /// svint64_t svand[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2) : "AND Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; AND Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svint64_t svand[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2) : "AND Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "AND Ztied2.D, Pg/M, Ztied2.D, Zop1.D" or "AND Zresult.D, Zop1.D, Zop2.D"
    /// svint64_t svand[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2) : "MOVPRFX Zresult.D, Pg/Z, Zop1.D; AND Zresult.D, Pg/M, Zresult.D, Zop2.D" or "MOVPRFX Zresult.D, Pg/Z, Zop2.D; AND Zresult.D, Pg/M, Zresult.D, Zop1.D"
    /// svbool_t svand[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2) : "AND Presult.B, Pg/Z, Pop1.B, Pop2.B"
  public static unsafe Vector<long> And(Vector<long> left, Vector<long> right);

    /// svuint8_t svand[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2) : "AND Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "MOVPRFX Zresult, Zop1; AND Zresult.B, Pg/M, Zresult.B, Zop2.B"
    /// svuint8_t svand[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2) : "AND Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "AND Ztied2.B, Pg/M, Ztied2.B, Zop1.B" or "AND Zresult.D, Zop1.D, Zop2.D"
    /// svuint8_t svand[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2) : "MOVPRFX Zresult.B, Pg/Z, Zop1.B; AND Zresult.B, Pg/M, Zresult.B, Zop2.B" or "MOVPRFX Zresult.B, Pg/Z, Zop2.B; AND Zresult.B, Pg/M, Zresult.B, Zop1.B"
    /// svbool_t svand[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2) : "AND Presult.B, Pg/Z, Pop1.B, Pop2.B"
  public static unsafe Vector<byte> And(Vector<byte> left, Vector<byte> right);

    /// svuint16_t svand[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2) : "AND Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "MOVPRFX Zresult, Zop1; AND Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svuint16_t svand[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2) : "AND Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "AND Ztied2.H, Pg/M, Ztied2.H, Zop1.H" or "AND Zresult.D, Zop1.D, Zop2.D"
    /// svuint16_t svand[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2) : "MOVPRFX Zresult.H, Pg/Z, Zop1.H; AND Zresult.H, Pg/M, Zresult.H, Zop2.H" or "MOVPRFX Zresult.H, Pg/Z, Zop2.H; AND Zresult.H, Pg/M, Zresult.H, Zop1.H"
    /// svbool_t svand[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2) : "AND Presult.B, Pg/Z, Pop1.B, Pop2.B"
  public static unsafe Vector<ushort> And(Vector<ushort> left, Vector<ushort> right);

    /// svuint32_t svand[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2) : "AND Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "MOVPRFX Zresult, Zop1; AND Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svuint32_t svand[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2) : "AND Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "AND Ztied2.S, Pg/M, Ztied2.S, Zop1.S" or "AND Zresult.D, Zop1.D, Zop2.D"
    /// svuint32_t svand[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2) : "MOVPRFX Zresult.S, Pg/Z, Zop1.S; AND Zresult.S, Pg/M, Zresult.S, Zop2.S" or "MOVPRFX Zresult.S, Pg/Z, Zop2.S; AND Zresult.S, Pg/M, Zresult.S, Zop1.S"
    /// svbool_t svand[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2) : "AND Presult.B, Pg/Z, Pop1.B, Pop2.B"
  public static unsafe Vector<uint> And(Vector<uint> left, Vector<uint> right);

    /// svuint64_t svand[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2) : "AND Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; AND Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svuint64_t svand[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2) : "AND Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "AND Ztied2.D, Pg/M, Ztied2.D, Zop1.D" or "AND Zresult.D, Zop1.D, Zop2.D"
    /// svuint64_t svand[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2) : "MOVPRFX Zresult.D, Pg/Z, Zop1.D; AND Zresult.D, Pg/M, Zresult.D, Zop2.D" or "MOVPRFX Zresult.D, Pg/Z, Zop2.D; AND Zresult.D, Pg/M, Zresult.D, Zop1.D"
    /// svbool_t svand[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2) : "AND Presult.B, Pg/Z, Pop1.B, Pop2.B"
  public static unsafe Vector<ulong> And(Vector<ulong> left, Vector<ulong> right);


    /// AndAcross : Bitwise AND reduction to scalar

    /// int8_t svandv[_s8](svbool_t pg, svint8_t op) : "ANDV Bresult, Pg, Zop.B"
  public static unsafe Vector<sbyte> AndAcross(Vector<sbyte> value);

    /// int16_t svandv[_s16](svbool_t pg, svint16_t op) : "ANDV Hresult, Pg, Zop.H"
  public static unsafe Vector<short> AndAcross(Vector<short> value);

    /// int32_t svandv[_s32](svbool_t pg, svint32_t op) : "ANDV Sresult, Pg, Zop.S"
  public static unsafe Vector<int> AndAcross(Vector<int> value);

    /// int64_t svandv[_s64](svbool_t pg, svint64_t op) : "ANDV Dresult, Pg, Zop.D"
  public static unsafe Vector<long> AndAcross(Vector<long> value);

    /// uint8_t svandv[_u8](svbool_t pg, svuint8_t op) : "ANDV Bresult, Pg, Zop.B"
  public static unsafe Vector<byte> AndAcross(Vector<byte> value);

    /// uint16_t svandv[_u16](svbool_t pg, svuint16_t op) : "ANDV Hresult, Pg, Zop.H"
  public static unsafe Vector<ushort> AndAcross(Vector<ushort> value);

    /// uint32_t svandv[_u32](svbool_t pg, svuint32_t op) : "ANDV Sresult, Pg, Zop.S"
  public static unsafe Vector<uint> AndAcross(Vector<uint> value);

    /// uint64_t svandv[_u64](svbool_t pg, svuint64_t op) : "ANDV Dresult, Pg, Zop.D"
  public static unsafe Vector<ulong> AndAcross(Vector<ulong> value);


    /// AndNot : Bitwise NAND

    /// svbool_t svnand[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2) : "NAND Presult.B, Pg/Z, Pop1.B, Pop2.B"
  public static unsafe Vector<sbyte> AndNot(Vector<sbyte> left, Vector<sbyte> right);

    /// svbool_t svnand[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2) : "NAND Presult.B, Pg/Z, Pop1.B, Pop2.B"
  public static unsafe Vector<short> AndNot(Vector<short> left, Vector<short> right);

    /// svbool_t svnand[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2) : "NAND Presult.B, Pg/Z, Pop1.B, Pop2.B"
  public static unsafe Vector<int> AndNot(Vector<int> left, Vector<int> right);

    /// svbool_t svnand[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2) : "NAND Presult.B, Pg/Z, Pop1.B, Pop2.B"
  public static unsafe Vector<long> AndNot(Vector<long> left, Vector<long> right);

    /// svbool_t svnand[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2) : "NAND Presult.B, Pg/Z, Pop1.B, Pop2.B"
  public static unsafe Vector<byte> AndNot(Vector<byte> left, Vector<byte> right);

    /// svbool_t svnand[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2) : "NAND Presult.B, Pg/Z, Pop1.B, Pop2.B"
  public static unsafe Vector<ushort> AndNot(Vector<ushort> left, Vector<ushort> right);

    /// svbool_t svnand[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2) : "NAND Presult.B, Pg/Z, Pop1.B, Pop2.B"
  public static unsafe Vector<uint> AndNot(Vector<uint> left, Vector<uint> right);

    /// svbool_t svnand[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2) : "NAND Presult.B, Pg/Z, Pop1.B, Pop2.B"
  public static unsafe Vector<ulong> AndNot(Vector<ulong> left, Vector<ulong> right);


    /// BitwiseClear : Bitwise clear

    /// svint8_t svbic[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2) : "BIC Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "MOVPRFX Zresult, Zop1; BIC Zresult.B, Pg/M, Zresult.B, Zop2.B"
    /// svint8_t svbic[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2) : "BIC Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "BIC Zresult.D, Zop1.D, Zop2.D"
    /// svint8_t svbic[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2) : "MOVPRFX Zresult.B, Pg/Z, Zop1.B; BIC Zresult.B, Pg/M, Zresult.B, Zop2.B"
    /// svbool_t svbic[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2) : "BIC Presult.B, Pg/Z, Pop1.B, Pop2.B"
  public static unsafe Vector<sbyte> BitwiseClear(Vector<sbyte> left, Vector<sbyte> right);

    /// svint16_t svbic[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2) : "BIC Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "MOVPRFX Zresult, Zop1; BIC Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svint16_t svbic[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2) : "BIC Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "BIC Zresult.D, Zop1.D, Zop2.D"
    /// svint16_t svbic[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2) : "MOVPRFX Zresult.H, Pg/Z, Zop1.H; BIC Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svbool_t svbic[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2) : "BIC Presult.B, Pg/Z, Pop1.B, Pop2.B"
  public static unsafe Vector<short> BitwiseClear(Vector<short> left, Vector<short> right);

    /// svint32_t svbic[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2) : "BIC Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "MOVPRFX Zresult, Zop1; BIC Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svint32_t svbic[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2) : "BIC Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "BIC Zresult.D, Zop1.D, Zop2.D"
    /// svint32_t svbic[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2) : "MOVPRFX Zresult.S, Pg/Z, Zop1.S; BIC Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svbool_t svbic[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2) : "BIC Presult.B, Pg/Z, Pop1.B, Pop2.B"
  public static unsafe Vector<int> BitwiseClear(Vector<int> left, Vector<int> right);

    /// svint64_t svbic[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2) : "BIC Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; BIC Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svint64_t svbic[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2) : "BIC Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "BIC Zresult.D, Zop1.D, Zop2.D"
    /// svint64_t svbic[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2) : "MOVPRFX Zresult.D, Pg/Z, Zop1.D; BIC Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svbool_t svbic[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2) : "BIC Presult.B, Pg/Z, Pop1.B, Pop2.B"
  public static unsafe Vector<long> BitwiseClear(Vector<long> left, Vector<long> right);

    /// svuint8_t svbic[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2) : "BIC Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "MOVPRFX Zresult, Zop1; BIC Zresult.B, Pg/M, Zresult.B, Zop2.B"
    /// svuint8_t svbic[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2) : "BIC Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "BIC Zresult.D, Zop1.D, Zop2.D"
    /// svuint8_t svbic[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2) : "MOVPRFX Zresult.B, Pg/Z, Zop1.B; BIC Zresult.B, Pg/M, Zresult.B, Zop2.B"
    /// svbool_t svbic[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2) : "BIC Presult.B, Pg/Z, Pop1.B, Pop2.B"
  public static unsafe Vector<byte> BitwiseClear(Vector<byte> left, Vector<byte> right);

    /// svuint16_t svbic[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2) : "BIC Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "MOVPRFX Zresult, Zop1; BIC Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svuint16_t svbic[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2) : "BIC Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "BIC Zresult.D, Zop1.D, Zop2.D"
    /// svuint16_t svbic[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2) : "MOVPRFX Zresult.H, Pg/Z, Zop1.H; BIC Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svbool_t svbic[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2) : "BIC Presult.B, Pg/Z, Pop1.B, Pop2.B"
  public static unsafe Vector<ushort> BitwiseClear(Vector<ushort> left, Vector<ushort> right);

    /// svuint32_t svbic[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2) : "BIC Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "MOVPRFX Zresult, Zop1; BIC Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svuint32_t svbic[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2) : "BIC Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "BIC Zresult.D, Zop1.D, Zop2.D"
    /// svuint32_t svbic[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2) : "MOVPRFX Zresult.S, Pg/Z, Zop1.S; BIC Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svbool_t svbic[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2) : "BIC Presult.B, Pg/Z, Pop1.B, Pop2.B"
  public static unsafe Vector<uint> BitwiseClear(Vector<uint> left, Vector<uint> right);

    /// svuint64_t svbic[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2) : "BIC Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; BIC Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svuint64_t svbic[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2) : "BIC Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "BIC Zresult.D, Zop1.D, Zop2.D"
    /// svuint64_t svbic[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2) : "MOVPRFX Zresult.D, Pg/Z, Zop1.D; BIC Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svbool_t svbic[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2) : "BIC Presult.B, Pg/Z, Pop1.B, Pop2.B"
  public static unsafe Vector<ulong> BitwiseClear(Vector<ulong> left, Vector<ulong> right);


    /// BooleanNot : Logically invert boolean condition

    /// svint8_t svcnot[_s8]_m(svint8_t inactive, svbool_t pg, svint8_t op) : "CNOT Ztied.B, Pg/M, Zop.B" or "MOVPRFX Zresult, Zinactive; CNOT Zresult.B, Pg/M, Zop.B"
    /// svint8_t svcnot[_s8]_x(svbool_t pg, svint8_t op) : "CNOT Ztied.B, Pg/M, Ztied.B" or "MOVPRFX Zresult, Zop; CNOT Zresult.B, Pg/M, Zop.B"
    /// svint8_t svcnot[_s8]_z(svbool_t pg, svint8_t op) : "MOVPRFX Zresult.B, Pg/Z, Zop.B; CNOT Zresult.B, Pg/M, Zop.B"
  public static unsafe Vector<sbyte> BooleanNot(Vector<sbyte> value);

    /// svint16_t svcnot[_s16]_m(svint16_t inactive, svbool_t pg, svint16_t op) : "CNOT Ztied.H, Pg/M, Zop.H" or "MOVPRFX Zresult, Zinactive; CNOT Zresult.H, Pg/M, Zop.H"
    /// svint16_t svcnot[_s16]_x(svbool_t pg, svint16_t op) : "CNOT Ztied.H, Pg/M, Ztied.H" or "MOVPRFX Zresult, Zop; CNOT Zresult.H, Pg/M, Zop.H"
    /// svint16_t svcnot[_s16]_z(svbool_t pg, svint16_t op) : "MOVPRFX Zresult.H, Pg/Z, Zop.H; CNOT Zresult.H, Pg/M, Zop.H"
  public static unsafe Vector<short> BooleanNot(Vector<short> value);

    /// svint32_t svcnot[_s32]_m(svint32_t inactive, svbool_t pg, svint32_t op) : "CNOT Ztied.S, Pg/M, Zop.S" or "MOVPRFX Zresult, Zinactive; CNOT Zresult.S, Pg/M, Zop.S"
    /// svint32_t svcnot[_s32]_x(svbool_t pg, svint32_t op) : "CNOT Ztied.S, Pg/M, Ztied.S" or "MOVPRFX Zresult, Zop; CNOT Zresult.S, Pg/M, Zop.S"
    /// svint32_t svcnot[_s32]_z(svbool_t pg, svint32_t op) : "MOVPRFX Zresult.S, Pg/Z, Zop.S; CNOT Zresult.S, Pg/M, Zop.S"
  public static unsafe Vector<int> BooleanNot(Vector<int> value);

    /// svint64_t svcnot[_s64]_m(svint64_t inactive, svbool_t pg, svint64_t op) : "CNOT Ztied.D, Pg/M, Zop.D" or "MOVPRFX Zresult, Zinactive; CNOT Zresult.D, Pg/M, Zop.D"
    /// svint64_t svcnot[_s64]_x(svbool_t pg, svint64_t op) : "CNOT Ztied.D, Pg/M, Ztied.D" or "MOVPRFX Zresult, Zop; CNOT Zresult.D, Pg/M, Zop.D"
    /// svint64_t svcnot[_s64]_z(svbool_t pg, svint64_t op) : "MOVPRFX Zresult.D, Pg/Z, Zop.D; CNOT Zresult.D, Pg/M, Zop.D"
  public static unsafe Vector<long> BooleanNot(Vector<long> value);

    /// svuint8_t svcnot[_u8]_m(svuint8_t inactive, svbool_t pg, svuint8_t op) : "CNOT Ztied.B, Pg/M, Zop.B" or "MOVPRFX Zresult, Zinactive; CNOT Zresult.B, Pg/M, Zop.B"
    /// svuint8_t svcnot[_u8]_x(svbool_t pg, svuint8_t op) : "CNOT Ztied.B, Pg/M, Ztied.B" or "MOVPRFX Zresult, Zop; CNOT Zresult.B, Pg/M, Zop.B"
    /// svuint8_t svcnot[_u8]_z(svbool_t pg, svuint8_t op) : "MOVPRFX Zresult.B, Pg/Z, Zop.B; CNOT Zresult.B, Pg/M, Zop.B"
  public static unsafe Vector<byte> BooleanNot(Vector<byte> value);

    /// svuint16_t svcnot[_u16]_m(svuint16_t inactive, svbool_t pg, svuint16_t op) : "CNOT Ztied.H, Pg/M, Zop.H" or "MOVPRFX Zresult, Zinactive; CNOT Zresult.H, Pg/M, Zop.H"
    /// svuint16_t svcnot[_u16]_x(svbool_t pg, svuint16_t op) : "CNOT Ztied.H, Pg/M, Ztied.H" or "MOVPRFX Zresult, Zop; CNOT Zresult.H, Pg/M, Zop.H"
    /// svuint16_t svcnot[_u16]_z(svbool_t pg, svuint16_t op) : "MOVPRFX Zresult.H, Pg/Z, Zop.H; CNOT Zresult.H, Pg/M, Zop.H"
  public static unsafe Vector<ushort> BooleanNot(Vector<ushort> value);

    /// svuint32_t svcnot[_u32]_m(svuint32_t inactive, svbool_t pg, svuint32_t op) : "CNOT Ztied.S, Pg/M, Zop.S" or "MOVPRFX Zresult, Zinactive; CNOT Zresult.S, Pg/M, Zop.S"
    /// svuint32_t svcnot[_u32]_x(svbool_t pg, svuint32_t op) : "CNOT Ztied.S, Pg/M, Ztied.S" or "MOVPRFX Zresult, Zop; CNOT Zresult.S, Pg/M, Zop.S"
    /// svuint32_t svcnot[_u32]_z(svbool_t pg, svuint32_t op) : "MOVPRFX Zresult.S, Pg/Z, Zop.S; CNOT Zresult.S, Pg/M, Zop.S"
  public static unsafe Vector<uint> BooleanNot(Vector<uint> value);

    /// svuint64_t svcnot[_u64]_m(svuint64_t inactive, svbool_t pg, svuint64_t op) : "CNOT Ztied.D, Pg/M, Zop.D" or "MOVPRFX Zresult, Zinactive; CNOT Zresult.D, Pg/M, Zop.D"
    /// svuint64_t svcnot[_u64]_x(svbool_t pg, svuint64_t op) : "CNOT Ztied.D, Pg/M, Ztied.D" or "MOVPRFX Zresult, Zop; CNOT Zresult.D, Pg/M, Zop.D"
    /// svuint64_t svcnot[_u64]_z(svbool_t pg, svuint64_t op) : "MOVPRFX Zresult.D, Pg/Z, Zop.D; CNOT Zresult.D, Pg/M, Zop.D"
  public static unsafe Vector<ulong> BooleanNot(Vector<ulong> value);


    /// InsertIntoShiftedVector : Insert scalar into shifted vector

    /// svfloat32_t svinsr[_n_f32](svfloat32_t op1, float32_t op2) : "INSR Ztied1.S, Wop2" or "INSR Ztied1.S, Sop2"
  public static unsafe Vector<float> InsertIntoShiftedVector(Vector<float> left, float right);

    /// svfloat64_t svinsr[_n_f64](svfloat64_t op1, float64_t op2) : "INSR Ztied1.D, Xop2" or "INSR Ztied1.D, Dop2"
  public static unsafe Vector<double> InsertIntoShiftedVector(Vector<double> left, double right);

    /// svint8_t svinsr[_n_s8](svint8_t op1, int8_t op2) : "INSR Ztied1.B, Wop2" or "INSR Ztied1.B, Bop2"
  public static unsafe Vector<sbyte> InsertIntoShiftedVector(Vector<sbyte> left, sbyte right);

    /// svint16_t svinsr[_n_s16](svint16_t op1, int16_t op2) : "INSR Ztied1.H, Wop2" or "INSR Ztied1.H, Hop2"
  public static unsafe Vector<short> InsertIntoShiftedVector(Vector<short> left, short right);

    /// svint32_t svinsr[_n_s32](svint32_t op1, int32_t op2) : "INSR Ztied1.S, Wop2" or "INSR Ztied1.S, Sop2"
  public static unsafe Vector<int> InsertIntoShiftedVector(Vector<int> left, int right);

    /// svint64_t svinsr[_n_s64](svint64_t op1, int64_t op2) : "INSR Ztied1.D, Xop2" or "INSR Ztied1.D, Dop2"
  public static unsafe Vector<long> InsertIntoShiftedVector(Vector<long> left, long right);

    /// svuint8_t svinsr[_n_u8](svuint8_t op1, uint8_t op2) : "INSR Ztied1.B, Wop2" or "INSR Ztied1.B, Bop2"
  public static unsafe Vector<byte> InsertIntoShiftedVector(Vector<byte> left, byte right);

    /// svuint16_t svinsr[_n_u16](svuint16_t op1, uint16_t op2) : "INSR Ztied1.H, Wop2" or "INSR Ztied1.H, Hop2"
  public static unsafe Vector<ushort> InsertIntoShiftedVector(Vector<ushort> left, ushort right);

    /// svuint32_t svinsr[_n_u32](svuint32_t op1, uint32_t op2) : "INSR Ztied1.S, Wop2" or "INSR Ztied1.S, Sop2"
  public static unsafe Vector<uint> InsertIntoShiftedVector(Vector<uint> left, uint right);

    /// svuint64_t svinsr[_n_u64](svuint64_t op1, uint64_t op2) : "INSR Ztied1.D, Xop2" or "INSR Ztied1.D, Dop2"
  public static unsafe Vector<ulong> InsertIntoShiftedVector(Vector<ulong> left, ulong right);


    /// Not : Bitwise invert

    /// svint8_t svnot[_s8]_m(svint8_t inactive, svbool_t pg, svint8_t op) : "NOT Ztied.B, Pg/M, Zop.B" or "MOVPRFX Zresult, Zinactive; NOT Zresult.B, Pg/M, Zop.B"
    /// svint8_t svnot[_s8]_x(svbool_t pg, svint8_t op) : "NOT Ztied.B, Pg/M, Ztied.B" or "MOVPRFX Zresult, Zop; NOT Zresult.B, Pg/M, Zop.B"
    /// svint8_t svnot[_s8]_z(svbool_t pg, svint8_t op) : "MOVPRFX Zresult.B, Pg/Z, Zop.B; NOT Zresult.B, Pg/M, Zop.B"
    /// svbool_t svnot[_b]_z(svbool_t pg, svbool_t op) : "EOR Presult.B, Pg/Z, Pop.B, Pg.B"
  public static unsafe Vector<sbyte> Not(Vector<sbyte> value);

    /// svint16_t svnot[_s16]_m(svint16_t inactive, svbool_t pg, svint16_t op) : "NOT Ztied.H, Pg/M, Zop.H" or "MOVPRFX Zresult, Zinactive; NOT Zresult.H, Pg/M, Zop.H"
    /// svint16_t svnot[_s16]_x(svbool_t pg, svint16_t op) : "NOT Ztied.H, Pg/M, Ztied.H" or "MOVPRFX Zresult, Zop; NOT Zresult.H, Pg/M, Zop.H"
    /// svint16_t svnot[_s16]_z(svbool_t pg, svint16_t op) : "MOVPRFX Zresult.H, Pg/Z, Zop.H; NOT Zresult.H, Pg/M, Zop.H"
    /// svbool_t svnot[_b]_z(svbool_t pg, svbool_t op) : "EOR Presult.B, Pg/Z, Pop.B, Pg.B"
  public static unsafe Vector<short> Not(Vector<short> value);

    /// svint32_t svnot[_s32]_m(svint32_t inactive, svbool_t pg, svint32_t op) : "NOT Ztied.S, Pg/M, Zop.S" or "MOVPRFX Zresult, Zinactive; NOT Zresult.S, Pg/M, Zop.S"
    /// svint32_t svnot[_s32]_x(svbool_t pg, svint32_t op) : "NOT Ztied.S, Pg/M, Ztied.S" or "MOVPRFX Zresult, Zop; NOT Zresult.S, Pg/M, Zop.S"
    /// svint32_t svnot[_s32]_z(svbool_t pg, svint32_t op) : "MOVPRFX Zresult.S, Pg/Z, Zop.S; NOT Zresult.S, Pg/M, Zop.S"
    /// svbool_t svnot[_b]_z(svbool_t pg, svbool_t op) : "EOR Presult.B, Pg/Z, Pop.B, Pg.B"
  public static unsafe Vector<int> Not(Vector<int> value);

    /// svint64_t svnot[_s64]_m(svint64_t inactive, svbool_t pg, svint64_t op) : "NOT Ztied.D, Pg/M, Zop.D" or "MOVPRFX Zresult, Zinactive; NOT Zresult.D, Pg/M, Zop.D"
    /// svint64_t svnot[_s64]_x(svbool_t pg, svint64_t op) : "NOT Ztied.D, Pg/M, Ztied.D" or "MOVPRFX Zresult, Zop; NOT Zresult.D, Pg/M, Zop.D"
    /// svint64_t svnot[_s64]_z(svbool_t pg, svint64_t op) : "MOVPRFX Zresult.D, Pg/Z, Zop.D; NOT Zresult.D, Pg/M, Zop.D"
    /// svbool_t svnot[_b]_z(svbool_t pg, svbool_t op) : "EOR Presult.B, Pg/Z, Pop.B, Pg.B"
  public static unsafe Vector<long> Not(Vector<long> value);

    /// svuint8_t svnot[_u8]_m(svuint8_t inactive, svbool_t pg, svuint8_t op) : "NOT Ztied.B, Pg/M, Zop.B" or "MOVPRFX Zresult, Zinactive; NOT Zresult.B, Pg/M, Zop.B"
    /// svuint8_t svnot[_u8]_x(svbool_t pg, svuint8_t op) : "NOT Ztied.B, Pg/M, Ztied.B" or "MOVPRFX Zresult, Zop; NOT Zresult.B, Pg/M, Zop.B"
    /// svuint8_t svnot[_u8]_z(svbool_t pg, svuint8_t op) : "MOVPRFX Zresult.B, Pg/Z, Zop.B; NOT Zresult.B, Pg/M, Zop.B"
    /// svbool_t svnot[_b]_z(svbool_t pg, svbool_t op) : "EOR Presult.B, Pg/Z, Pop.B, Pg.B"
  public static unsafe Vector<byte> Not(Vector<byte> value);

    /// svuint16_t svnot[_u16]_m(svuint16_t inactive, svbool_t pg, svuint16_t op) : "NOT Ztied.H, Pg/M, Zop.H" or "MOVPRFX Zresult, Zinactive; NOT Zresult.H, Pg/M, Zop.H"
    /// svuint16_t svnot[_u16]_x(svbool_t pg, svuint16_t op) : "NOT Ztied.H, Pg/M, Ztied.H" or "MOVPRFX Zresult, Zop; NOT Zresult.H, Pg/M, Zop.H"
    /// svuint16_t svnot[_u16]_z(svbool_t pg, svuint16_t op) : "MOVPRFX Zresult.H, Pg/Z, Zop.H; NOT Zresult.H, Pg/M, Zop.H"
    /// svbool_t svnot[_b]_z(svbool_t pg, svbool_t op) : "EOR Presult.B, Pg/Z, Pop.B, Pg.B"
  public static unsafe Vector<ushort> Not(Vector<ushort> value);

    /// svuint32_t svnot[_u32]_m(svuint32_t inactive, svbool_t pg, svuint32_t op) : "NOT Ztied.S, Pg/M, Zop.S" or "MOVPRFX Zresult, Zinactive; NOT Zresult.S, Pg/M, Zop.S"
    /// svuint32_t svnot[_u32]_x(svbool_t pg, svuint32_t op) : "NOT Ztied.S, Pg/M, Ztied.S" or "MOVPRFX Zresult, Zop; NOT Zresult.S, Pg/M, Zop.S"
    /// svuint32_t svnot[_u32]_z(svbool_t pg, svuint32_t op) : "MOVPRFX Zresult.S, Pg/Z, Zop.S; NOT Zresult.S, Pg/M, Zop.S"
    /// svbool_t svnot[_b]_z(svbool_t pg, svbool_t op) : "EOR Presult.B, Pg/Z, Pop.B, Pg.B"
  public static unsafe Vector<uint> Not(Vector<uint> value);

    /// svuint64_t svnot[_u64]_m(svuint64_t inactive, svbool_t pg, svuint64_t op) : "NOT Ztied.D, Pg/M, Zop.D" or "MOVPRFX Zresult, Zinactive; NOT Zresult.D, Pg/M, Zop.D"
    /// svuint64_t svnot[_u64]_x(svbool_t pg, svuint64_t op) : "NOT Ztied.D, Pg/M, Ztied.D" or "MOVPRFX Zresult, Zop; NOT Zresult.D, Pg/M, Zop.D"
    /// svuint64_t svnot[_u64]_z(svbool_t pg, svuint64_t op) : "MOVPRFX Zresult.D, Pg/Z, Zop.D; NOT Zresult.D, Pg/M, Zop.D"
    /// svbool_t svnot[_b]_z(svbool_t pg, svbool_t op) : "EOR Presult.B, Pg/Z, Pop.B, Pg.B"
  public static unsafe Vector<ulong> Not(Vector<ulong> value);


    /// Or : Bitwise inclusive OR

    /// svint8_t svorr[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2) : "ORR Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "MOVPRFX Zresult, Zop1; ORR Zresult.B, Pg/M, Zresult.B, Zop2.B"
    /// svint8_t svorr[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2) : "ORR Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "ORR Ztied2.B, Pg/M, Ztied2.B, Zop1.B" or "ORR Zresult.D, Zop1.D, Zop2.D"
    /// svint8_t svorr[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2) : "MOVPRFX Zresult.B, Pg/Z, Zop1.B; ORR Zresult.B, Pg/M, Zresult.B, Zop2.B" or "MOVPRFX Zresult.B, Pg/Z, Zop2.B; ORR Zresult.B, Pg/M, Zresult.B, Zop1.B"
    /// svbool_t svorr[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2) : "ORR Presult.B, Pg/Z, Pop1.B, Pop2.B"
  public static unsafe Vector<sbyte> Or(Vector<sbyte> left, Vector<sbyte> right);

    /// svint16_t svorr[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2) : "ORR Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "MOVPRFX Zresult, Zop1; ORR Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svint16_t svorr[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2) : "ORR Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "ORR Ztied2.H, Pg/M, Ztied2.H, Zop1.H" or "ORR Zresult.D, Zop1.D, Zop2.D"
    /// svint16_t svorr[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2) : "MOVPRFX Zresult.H, Pg/Z, Zop1.H; ORR Zresult.H, Pg/M, Zresult.H, Zop2.H" or "MOVPRFX Zresult.H, Pg/Z, Zop2.H; ORR Zresult.H, Pg/M, Zresult.H, Zop1.H"
    /// svbool_t svorr[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2) : "ORR Presult.B, Pg/Z, Pop1.B, Pop2.B"
  public static unsafe Vector<short> Or(Vector<short> left, Vector<short> right);

    /// svint32_t svorr[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2) : "ORR Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "MOVPRFX Zresult, Zop1; ORR Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svint32_t svorr[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2) : "ORR Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "ORR Ztied2.S, Pg/M, Ztied2.S, Zop1.S" or "ORR Zresult.D, Zop1.D, Zop2.D"
    /// svint32_t svorr[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2) : "MOVPRFX Zresult.S, Pg/Z, Zop1.S; ORR Zresult.S, Pg/M, Zresult.S, Zop2.S" or "MOVPRFX Zresult.S, Pg/Z, Zop2.S; ORR Zresult.S, Pg/M, Zresult.S, Zop1.S"
    /// svbool_t svorr[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2) : "ORR Presult.B, Pg/Z, Pop1.B, Pop2.B"
  public static unsafe Vector<int> Or(Vector<int> left, Vector<int> right);

    /// svint64_t svorr[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2) : "ORR Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; ORR Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svint64_t svorr[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2) : "ORR Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "ORR Ztied2.D, Pg/M, Ztied2.D, Zop1.D" or "ORR Zresult.D, Zop1.D, Zop2.D"
    /// svint64_t svorr[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2) : "MOVPRFX Zresult.D, Pg/Z, Zop1.D; ORR Zresult.D, Pg/M, Zresult.D, Zop2.D" or "MOVPRFX Zresult.D, Pg/Z, Zop2.D; ORR Zresult.D, Pg/M, Zresult.D, Zop1.D"
    /// svbool_t svorr[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2) : "ORR Presult.B, Pg/Z, Pop1.B, Pop2.B"
  public static unsafe Vector<long> Or(Vector<long> left, Vector<long> right);

    /// svuint8_t svorr[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2) : "ORR Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "MOVPRFX Zresult, Zop1; ORR Zresult.B, Pg/M, Zresult.B, Zop2.B"
    /// svuint8_t svorr[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2) : "ORR Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "ORR Ztied2.B, Pg/M, Ztied2.B, Zop1.B" or "ORR Zresult.D, Zop1.D, Zop2.D"
    /// svuint8_t svorr[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2) : "MOVPRFX Zresult.B, Pg/Z, Zop1.B; ORR Zresult.B, Pg/M, Zresult.B, Zop2.B" or "MOVPRFX Zresult.B, Pg/Z, Zop2.B; ORR Zresult.B, Pg/M, Zresult.B, Zop1.B"
    /// svbool_t svorr[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2) : "ORR Presult.B, Pg/Z, Pop1.B, Pop2.B"
  public static unsafe Vector<byte> Or(Vector<byte> left, Vector<byte> right);

    /// svuint16_t svorr[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2) : "ORR Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "MOVPRFX Zresult, Zop1; ORR Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svuint16_t svorr[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2) : "ORR Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "ORR Ztied2.H, Pg/M, Ztied2.H, Zop1.H" or "ORR Zresult.D, Zop1.D, Zop2.D"
    /// svuint16_t svorr[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2) : "MOVPRFX Zresult.H, Pg/Z, Zop1.H; ORR Zresult.H, Pg/M, Zresult.H, Zop2.H" or "MOVPRFX Zresult.H, Pg/Z, Zop2.H; ORR Zresult.H, Pg/M, Zresult.H, Zop1.H"
    /// svbool_t svorr[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2) : "ORR Presult.B, Pg/Z, Pop1.B, Pop2.B"
  public static unsafe Vector<ushort> Or(Vector<ushort> left, Vector<ushort> right);

    /// svuint32_t svorr[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2) : "ORR Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "MOVPRFX Zresult, Zop1; ORR Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svuint32_t svorr[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2) : "ORR Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "ORR Ztied2.S, Pg/M, Ztied2.S, Zop1.S" or "ORR Zresult.D, Zop1.D, Zop2.D"
    /// svuint32_t svorr[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2) : "MOVPRFX Zresult.S, Pg/Z, Zop1.S; ORR Zresult.S, Pg/M, Zresult.S, Zop2.S" or "MOVPRFX Zresult.S, Pg/Z, Zop2.S; ORR Zresult.S, Pg/M, Zresult.S, Zop1.S"
    /// svbool_t svorr[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2) : "ORR Presult.B, Pg/Z, Pop1.B, Pop2.B"
  public static unsafe Vector<uint> Or(Vector<uint> left, Vector<uint> right);

    /// svuint64_t svorr[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2) : "ORR Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; ORR Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svuint64_t svorr[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2) : "ORR Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "ORR Ztied2.D, Pg/M, Ztied2.D, Zop1.D" or "ORR Zresult.D, Zop1.D, Zop2.D"
    /// svuint64_t svorr[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2) : "MOVPRFX Zresult.D, Pg/Z, Zop1.D; ORR Zresult.D, Pg/M, Zresult.D, Zop2.D" or "MOVPRFX Zresult.D, Pg/Z, Zop2.D; ORR Zresult.D, Pg/M, Zresult.D, Zop1.D"
    /// svbool_t svorr[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2) : "ORR Presult.B, Pg/Z, Pop1.B, Pop2.B"
  public static unsafe Vector<ulong> Or(Vector<ulong> left, Vector<ulong> right);


    /// OrAcross : Bitwise inclusive OR reduction to scalar

    /// int8_t svorv[_s8](svbool_t pg, svint8_t op) : "ORV Bresult, Pg, Zop.B"
  public static unsafe Vector<sbyte> OrAcross(Vector<sbyte> value);

    /// int16_t svorv[_s16](svbool_t pg, svint16_t op) : "ORV Hresult, Pg, Zop.H"
  public static unsafe Vector<short> OrAcross(Vector<short> value);

    /// int32_t svorv[_s32](svbool_t pg, svint32_t op) : "ORV Sresult, Pg, Zop.S"
  public static unsafe Vector<int> OrAcross(Vector<int> value);

    /// int64_t svorv[_s64](svbool_t pg, svint64_t op) : "ORV Dresult, Pg, Zop.D"
  public static unsafe Vector<long> OrAcross(Vector<long> value);

    /// uint8_t svorv[_u8](svbool_t pg, svuint8_t op) : "ORV Bresult, Pg, Zop.B"
  public static unsafe Vector<byte> OrAcross(Vector<byte> value);

    /// uint16_t svorv[_u16](svbool_t pg, svuint16_t op) : "ORV Hresult, Pg, Zop.H"
  public static unsafe Vector<ushort> OrAcross(Vector<ushort> value);

    /// uint32_t svorv[_u32](svbool_t pg, svuint32_t op) : "ORV Sresult, Pg, Zop.S"
  public static unsafe Vector<uint> OrAcross(Vector<uint> value);

    /// uint64_t svorv[_u64](svbool_t pg, svuint64_t op) : "ORV Dresult, Pg, Zop.D"
  public static unsafe Vector<ulong> OrAcross(Vector<ulong> value);


    /// OrNot : Bitwise NOR

    /// svbool_t svnor[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2) : "NOR Presult.B, Pg/Z, Pop1.B, Pop2.B"
    /// svbool_t svorn[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2) : "ORN Presult.B, Pg/Z, Pop1.B, Pop2.B"
  public static unsafe Vector<sbyte> OrNot(Vector<sbyte> left, Vector<sbyte> right);

    /// svbool_t svnor[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2) : "NOR Presult.B, Pg/Z, Pop1.B, Pop2.B"
    /// svbool_t svorn[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2) : "ORN Presult.B, Pg/Z, Pop1.B, Pop2.B"
  public static unsafe Vector<short> OrNot(Vector<short> left, Vector<short> right);

    /// svbool_t svnor[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2) : "NOR Presult.B, Pg/Z, Pop1.B, Pop2.B"
    /// svbool_t svorn[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2) : "ORN Presult.B, Pg/Z, Pop1.B, Pop2.B"
  public static unsafe Vector<int> OrNot(Vector<int> left, Vector<int> right);

    /// svbool_t svnor[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2) : "NOR Presult.B, Pg/Z, Pop1.B, Pop2.B"
    /// svbool_t svorn[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2) : "ORN Presult.B, Pg/Z, Pop1.B, Pop2.B"
  public static unsafe Vector<long> OrNot(Vector<long> left, Vector<long> right);

    /// svbool_t svnor[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2) : "NOR Presult.B, Pg/Z, Pop1.B, Pop2.B"
    /// svbool_t svorn[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2) : "ORN Presult.B, Pg/Z, Pop1.B, Pop2.B"
  public static unsafe Vector<byte> OrNot(Vector<byte> left, Vector<byte> right);

    /// svbool_t svnor[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2) : "NOR Presult.B, Pg/Z, Pop1.B, Pop2.B"
    /// svbool_t svorn[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2) : "ORN Presult.B, Pg/Z, Pop1.B, Pop2.B"
  public static unsafe Vector<ushort> OrNot(Vector<ushort> left, Vector<ushort> right);

    /// svbool_t svnor[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2) : "NOR Presult.B, Pg/Z, Pop1.B, Pop2.B"
    /// svbool_t svorn[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2) : "ORN Presult.B, Pg/Z, Pop1.B, Pop2.B"
  public static unsafe Vector<uint> OrNot(Vector<uint> left, Vector<uint> right);

    /// svbool_t svnor[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2) : "NOR Presult.B, Pg/Z, Pop1.B, Pop2.B"
    /// svbool_t svorn[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2) : "ORN Presult.B, Pg/Z, Pop1.B, Pop2.B"
  public static unsafe Vector<ulong> OrNot(Vector<ulong> left, Vector<ulong> right);


    /// ShiftLeftLogical : Logical shift left

    /// svint8_t svlsl[_s8]_m(svbool_t pg, svint8_t op1, svuint8_t op2) : "LSL Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "MOVPRFX Zresult, Zop1; LSL Zresult.B, Pg/M, Zresult.B, Zop2.B"
    /// svint8_t svlsl[_s8]_x(svbool_t pg, svint8_t op1, svuint8_t op2) : "LSL Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "LSLR Ztied2.B, Pg/M, Ztied2.B, Zop1.B" or "MOVPRFX Zresult, Zop1; LSL Zresult.B, Pg/M, Zresult.B, Zop2.B"
    /// svint8_t svlsl[_s8]_z(svbool_t pg, svint8_t op1, svuint8_t op2) : "MOVPRFX Zresult.B, Pg/Z, Zop1.B; LSL Zresult.B, Pg/M, Zresult.B, Zop2.B" or "MOVPRFX Zresult.B, Pg/Z, Zop2.B; LSLR Zresult.B, Pg/M, Zresult.B, Zop1.B"
  public static unsafe Vector<sbyte> ShiftLeftLogical(Vector<sbyte> left, Vector<byte> right);

    /// svint16_t svlsl[_s16]_m(svbool_t pg, svint16_t op1, svuint16_t op2) : "LSL Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "MOVPRFX Zresult, Zop1; LSL Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svint16_t svlsl[_s16]_x(svbool_t pg, svint16_t op1, svuint16_t op2) : "LSL Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "LSLR Ztied2.H, Pg/M, Ztied2.H, Zop1.H" or "MOVPRFX Zresult, Zop1; LSL Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svint16_t svlsl[_s16]_z(svbool_t pg, svint16_t op1, svuint16_t op2) : "MOVPRFX Zresult.H, Pg/Z, Zop1.H; LSL Zresult.H, Pg/M, Zresult.H, Zop2.H" or "MOVPRFX Zresult.H, Pg/Z, Zop2.H; LSLR Zresult.H, Pg/M, Zresult.H, Zop1.H"
  public static unsafe Vector<short> ShiftLeftLogical(Vector<short> left, Vector<ushort> right);

    /// svint32_t svlsl[_s32]_m(svbool_t pg, svint32_t op1, svuint32_t op2) : "LSL Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "MOVPRFX Zresult, Zop1; LSL Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svint32_t svlsl[_s32]_x(svbool_t pg, svint32_t op1, svuint32_t op2) : "LSL Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "LSLR Ztied2.S, Pg/M, Ztied2.S, Zop1.S" or "MOVPRFX Zresult, Zop1; LSL Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svint32_t svlsl[_s32]_z(svbool_t pg, svint32_t op1, svuint32_t op2) : "MOVPRFX Zresult.S, Pg/Z, Zop1.S; LSL Zresult.S, Pg/M, Zresult.S, Zop2.S" or "MOVPRFX Zresult.S, Pg/Z, Zop2.S; LSLR Zresult.S, Pg/M, Zresult.S, Zop1.S"
  public static unsafe Vector<int> ShiftLeftLogical(Vector<int> left, Vector<uint> right);

    /// svint64_t svlsl[_s64]_m(svbool_t pg, svint64_t op1, svuint64_t op2) : "LSL Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; LSL Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svint64_t svlsl[_s64]_x(svbool_t pg, svint64_t op1, svuint64_t op2) : "LSL Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "LSLR Ztied2.D, Pg/M, Ztied2.D, Zop1.D" or "MOVPRFX Zresult, Zop1; LSL Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svint64_t svlsl[_s64]_z(svbool_t pg, svint64_t op1, svuint64_t op2) : "MOVPRFX Zresult.D, Pg/Z, Zop1.D; LSL Zresult.D, Pg/M, Zresult.D, Zop2.D" or "MOVPRFX Zresult.D, Pg/Z, Zop2.D; LSLR Zresult.D, Pg/M, Zresult.D, Zop1.D"
  public static unsafe Vector<long> ShiftLeftLogical(Vector<long> left, Vector<ulong> right);

    /// svuint8_t svlsl[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2) : "LSL Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "MOVPRFX Zresult, Zop1; LSL Zresult.B, Pg/M, Zresult.B, Zop2.B"
    /// svuint8_t svlsl[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2) : "LSL Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "LSLR Ztied2.B, Pg/M, Ztied2.B, Zop1.B" or "MOVPRFX Zresult, Zop1; LSL Zresult.B, Pg/M, Zresult.B, Zop2.B"
    /// svuint8_t svlsl[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2) : "MOVPRFX Zresult.B, Pg/Z, Zop1.B; LSL Zresult.B, Pg/M, Zresult.B, Zop2.B" or "MOVPRFX Zresult.B, Pg/Z, Zop2.B; LSLR Zresult.B, Pg/M, Zresult.B, Zop1.B"
  public static unsafe Vector<byte> ShiftLeftLogical(Vector<byte> left, Vector<byte> right);

    /// svuint16_t svlsl[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2) : "LSL Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "MOVPRFX Zresult, Zop1; LSL Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svuint16_t svlsl[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2) : "LSL Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "LSLR Ztied2.H, Pg/M, Ztied2.H, Zop1.H" or "MOVPRFX Zresult, Zop1; LSL Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svuint16_t svlsl[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2) : "MOVPRFX Zresult.H, Pg/Z, Zop1.H; LSL Zresult.H, Pg/M, Zresult.H, Zop2.H" or "MOVPRFX Zresult.H, Pg/Z, Zop2.H; LSLR Zresult.H, Pg/M, Zresult.H, Zop1.H"
  public static unsafe Vector<ushort> ShiftLeftLogical(Vector<ushort> left, Vector<ushort> right);

    /// svuint32_t svlsl[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2) : "LSL Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "MOVPRFX Zresult, Zop1; LSL Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svuint32_t svlsl[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2) : "LSL Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "LSLR Ztied2.S, Pg/M, Ztied2.S, Zop1.S" or "MOVPRFX Zresult, Zop1; LSL Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svuint32_t svlsl[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2) : "MOVPRFX Zresult.S, Pg/Z, Zop1.S; LSL Zresult.S, Pg/M, Zresult.S, Zop2.S" or "MOVPRFX Zresult.S, Pg/Z, Zop2.S; LSLR Zresult.S, Pg/M, Zresult.S, Zop1.S"
  public static unsafe Vector<uint> ShiftLeftLogical(Vector<uint> left, Vector<uint> right);

    /// svuint64_t svlsl[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2) : "LSL Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; LSL Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svuint64_t svlsl[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2) : "LSL Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "LSLR Ztied2.D, Pg/M, Ztied2.D, Zop1.D" or "MOVPRFX Zresult, Zop1; LSL Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svuint64_t svlsl[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2) : "MOVPRFX Zresult.D, Pg/Z, Zop1.D; LSL Zresult.D, Pg/M, Zresult.D, Zop2.D" or "MOVPRFX Zresult.D, Pg/Z, Zop2.D; LSLR Zresult.D, Pg/M, Zresult.D, Zop1.D"
  public static unsafe Vector<ulong> ShiftLeftLogical(Vector<ulong> left, Vector<ulong> right);

    /// svint8_t svlsl_wide[_s8]_m(svbool_t pg, svint8_t op1, svuint64_t op2) : "LSL Ztied1.B, Pg/M, Ztied1.B, Zop2.D" or "MOVPRFX Zresult, Zop1; LSL Zresult.B, Pg/M, Zresult.B, Zop2.D"
    /// svint8_t svlsl_wide[_s8]_x(svbool_t pg, svint8_t op1, svuint64_t op2) : "LSL Ztied1.B, Pg/M, Ztied1.B, Zop2.D" or "LSL Zresult.B, Zop1.B, Zop2.D"
    /// svint8_t svlsl_wide[_s8]_z(svbool_t pg, svint8_t op1, svuint64_t op2) : "MOVPRFX Zresult.B, Pg/Z, Zop1.B; LSL Zresult.B, Pg/M, Zresult.B, Zop2.D"
  public static unsafe Vector<sbyte> ShiftLeftLogical(Vector<sbyte> left, Vector<ulong> right);

    /// svint16_t svlsl_wide[_s16]_m(svbool_t pg, svint16_t op1, svuint64_t op2) : "LSL Ztied1.H, Pg/M, Ztied1.H, Zop2.D" or "MOVPRFX Zresult, Zop1; LSL Zresult.H, Pg/M, Zresult.H, Zop2.D"
    /// svint16_t svlsl_wide[_s16]_x(svbool_t pg, svint16_t op1, svuint64_t op2) : "LSL Ztied1.H, Pg/M, Ztied1.H, Zop2.D" or "LSL Zresult.H, Zop1.H, Zop2.D"
    /// svint16_t svlsl_wide[_s16]_z(svbool_t pg, svint16_t op1, svuint64_t op2) : "MOVPRFX Zresult.H, Pg/Z, Zop1.H; LSL Zresult.H, Pg/M, Zresult.H, Zop2.D"
  public static unsafe Vector<short> ShiftLeftLogical(Vector<short> left, Vector<ulong> right);

    /// svint32_t svlsl_wide[_s32]_m(svbool_t pg, svint32_t op1, svuint64_t op2) : "LSL Ztied1.S, Pg/M, Ztied1.S, Zop2.D" or "MOVPRFX Zresult, Zop1; LSL Zresult.S, Pg/M, Zresult.S, Zop2.D"
    /// svint32_t svlsl_wide[_s32]_x(svbool_t pg, svint32_t op1, svuint64_t op2) : "LSL Ztied1.S, Pg/M, Ztied1.S, Zop2.D" or "LSL Zresult.S, Zop1.S, Zop2.D"
    /// svint32_t svlsl_wide[_s32]_z(svbool_t pg, svint32_t op1, svuint64_t op2) : "MOVPRFX Zresult.S, Pg/Z, Zop1.S; LSL Zresult.S, Pg/M, Zresult.S, Zop2.D"
  public static unsafe Vector<int> ShiftLeftLogical(Vector<int> left, Vector<ulong> right);

    /// svuint8_t svlsl_wide[_u8]_m(svbool_t pg, svuint8_t op1, svuint64_t op2) : "LSL Ztied1.B, Pg/M, Ztied1.B, Zop2.D" or "MOVPRFX Zresult, Zop1; LSL Zresult.B, Pg/M, Zresult.B, Zop2.D"
    /// svuint8_t svlsl_wide[_u8]_x(svbool_t pg, svuint8_t op1, svuint64_t op2) : "LSL Ztied1.B, Pg/M, Ztied1.B, Zop2.D" or "LSL Zresult.B, Zop1.B, Zop2.D"
    /// svuint8_t svlsl_wide[_u8]_z(svbool_t pg, svuint8_t op1, svuint64_t op2) : "MOVPRFX Zresult.B, Pg/Z, Zop1.B; LSL Zresult.B, Pg/M, Zresult.B, Zop2.D"
  public static unsafe Vector<byte> ShiftLeftLogical(Vector<byte> left, Vector<ulong> right);

    /// svuint16_t svlsl_wide[_u16]_m(svbool_t pg, svuint16_t op1, svuint64_t op2) : "LSL Ztied1.H, Pg/M, Ztied1.H, Zop2.D" or "MOVPRFX Zresult, Zop1; LSL Zresult.H, Pg/M, Zresult.H, Zop2.D"
    /// svuint16_t svlsl_wide[_u16]_x(svbool_t pg, svuint16_t op1, svuint64_t op2) : "LSL Ztied1.H, Pg/M, Ztied1.H, Zop2.D" or "LSL Zresult.H, Zop1.H, Zop2.D"
    /// svuint16_t svlsl_wide[_u16]_z(svbool_t pg, svuint16_t op1, svuint64_t op2) : "MOVPRFX Zresult.H, Pg/Z, Zop1.H; LSL Zresult.H, Pg/M, Zresult.H, Zop2.D"
  public static unsafe Vector<ushort> ShiftLeftLogical(Vector<ushort> left, Vector<ulong> right);

    /// svuint32_t svlsl_wide[_u32]_m(svbool_t pg, svuint32_t op1, svuint64_t op2) : "LSL Ztied1.S, Pg/M, Ztied1.S, Zop2.D" or "MOVPRFX Zresult, Zop1; LSL Zresult.S, Pg/M, Zresult.S, Zop2.D"
    /// svuint32_t svlsl_wide[_u32]_x(svbool_t pg, svuint32_t op1, svuint64_t op2) : "LSL Ztied1.S, Pg/M, Ztied1.S, Zop2.D" or "LSL Zresult.S, Zop1.S, Zop2.D"
    /// svuint32_t svlsl_wide[_u32]_z(svbool_t pg, svuint32_t op1, svuint64_t op2) : "MOVPRFX Zresult.S, Pg/Z, Zop1.S; LSL Zresult.S, Pg/M, Zresult.S, Zop2.D"
  public static unsafe Vector<uint> ShiftLeftLogical(Vector<uint> left, Vector<ulong> right);


    /// ShiftRightArithmetic : Arithmetic shift right

    /// svint8_t svasr[_s8]_m(svbool_t pg, svint8_t op1, svuint8_t op2) : "ASR Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "MOVPRFX Zresult, Zop1; ASR Zresult.B, Pg/M, Zresult.B, Zop2.B"
    /// svint8_t svasr[_s8]_x(svbool_t pg, svint8_t op1, svuint8_t op2) : "ASR Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "ASRR Ztied2.B, Pg/M, Ztied2.B, Zop1.B" or "MOVPRFX Zresult, Zop1; ASR Zresult.B, Pg/M, Zresult.B, Zop2.B"
    /// svint8_t svasr[_s8]_z(svbool_t pg, svint8_t op1, svuint8_t op2) : "MOVPRFX Zresult.B, Pg/Z, Zop1.B; ASR Zresult.B, Pg/M, Zresult.B, Zop2.B" or "MOVPRFX Zresult.B, Pg/Z, Zop2.B; ASRR Zresult.B, Pg/M, Zresult.B, Zop1.B"
  public static unsafe Vector<sbyte> ShiftRightArithmetic(Vector<sbyte> left, Vector<byte> right);

    /// svint16_t svasr[_s16]_m(svbool_t pg, svint16_t op1, svuint16_t op2) : "ASR Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "MOVPRFX Zresult, Zop1; ASR Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svint16_t svasr[_s16]_x(svbool_t pg, svint16_t op1, svuint16_t op2) : "ASR Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "ASRR Ztied2.H, Pg/M, Ztied2.H, Zop1.H" or "MOVPRFX Zresult, Zop1; ASR Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svint16_t svasr[_s16]_z(svbool_t pg, svint16_t op1, svuint16_t op2) : "MOVPRFX Zresult.H, Pg/Z, Zop1.H; ASR Zresult.H, Pg/M, Zresult.H, Zop2.H" or "MOVPRFX Zresult.H, Pg/Z, Zop2.H; ASRR Zresult.H, Pg/M, Zresult.H, Zop1.H"
  public static unsafe Vector<short> ShiftRightArithmetic(Vector<short> left, Vector<ushort> right);

    /// svint32_t svasr[_s32]_m(svbool_t pg, svint32_t op1, svuint32_t op2) : "ASR Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "MOVPRFX Zresult, Zop1; ASR Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svint32_t svasr[_s32]_x(svbool_t pg, svint32_t op1, svuint32_t op2) : "ASR Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "ASRR Ztied2.S, Pg/M, Ztied2.S, Zop1.S" or "MOVPRFX Zresult, Zop1; ASR Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svint32_t svasr[_s32]_z(svbool_t pg, svint32_t op1, svuint32_t op2) : "MOVPRFX Zresult.S, Pg/Z, Zop1.S; ASR Zresult.S, Pg/M, Zresult.S, Zop2.S" or "MOVPRFX Zresult.S, Pg/Z, Zop2.S; ASRR Zresult.S, Pg/M, Zresult.S, Zop1.S"
  public static unsafe Vector<int> ShiftRightArithmetic(Vector<int> left, Vector<uint> right);

    /// svint64_t svasr[_s64]_m(svbool_t pg, svint64_t op1, svuint64_t op2) : "ASR Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; ASR Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svint64_t svasr[_s64]_x(svbool_t pg, svint64_t op1, svuint64_t op2) : "ASR Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "ASRR Ztied2.D, Pg/M, Ztied2.D, Zop1.D" or "MOVPRFX Zresult, Zop1; ASR Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svint64_t svasr[_s64]_z(svbool_t pg, svint64_t op1, svuint64_t op2) : "MOVPRFX Zresult.D, Pg/Z, Zop1.D; ASR Zresult.D, Pg/M, Zresult.D, Zop2.D" or "MOVPRFX Zresult.D, Pg/Z, Zop2.D; ASRR Zresult.D, Pg/M, Zresult.D, Zop1.D"
  public static unsafe Vector<long> ShiftRightArithmetic(Vector<long> left, Vector<ulong> right);

    /// svint8_t svasr_wide[_s8]_m(svbool_t pg, svint8_t op1, svuint64_t op2) : "ASR Ztied1.B, Pg/M, Ztied1.B, Zop2.D" or "MOVPRFX Zresult, Zop1; ASR Zresult.B, Pg/M, Zresult.B, Zop2.D"
    /// svint8_t svasr_wide[_s8]_x(svbool_t pg, svint8_t op1, svuint64_t op2) : "ASR Ztied1.B, Pg/M, Ztied1.B, Zop2.D" or "ASR Zresult.B, Zop1.B, Zop2.D"
    /// svint8_t svasr_wide[_s8]_z(svbool_t pg, svint8_t op1, svuint64_t op2) : "MOVPRFX Zresult.B, Pg/Z, Zop1.B; ASR Zresult.B, Pg/M, Zresult.B, Zop2.D"
  public static unsafe Vector<sbyte> ShiftRightArithmetic(Vector<sbyte> left, Vector<ulong> right);

    /// svint16_t svasr_wide[_s16]_m(svbool_t pg, svint16_t op1, svuint64_t op2) : "ASR Ztied1.H, Pg/M, Ztied1.H, Zop2.D" or "MOVPRFX Zresult, Zop1; ASR Zresult.H, Pg/M, Zresult.H, Zop2.D"
    /// svint16_t svasr_wide[_s16]_x(svbool_t pg, svint16_t op1, svuint64_t op2) : "ASR Ztied1.H, Pg/M, Ztied1.H, Zop2.D" or "ASR Zresult.H, Zop1.H, Zop2.D"
    /// svint16_t svasr_wide[_s16]_z(svbool_t pg, svint16_t op1, svuint64_t op2) : "MOVPRFX Zresult.H, Pg/Z, Zop1.H; ASR Zresult.H, Pg/M, Zresult.H, Zop2.D"
  public static unsafe Vector<short> ShiftRightArithmetic(Vector<short> left, Vector<ulong> right);

    /// svint32_t svasr_wide[_s32]_m(svbool_t pg, svint32_t op1, svuint64_t op2) : "ASR Ztied1.S, Pg/M, Ztied1.S, Zop2.D" or "MOVPRFX Zresult, Zop1; ASR Zresult.S, Pg/M, Zresult.S, Zop2.D"
    /// svint32_t svasr_wide[_s32]_x(svbool_t pg, svint32_t op1, svuint64_t op2) : "ASR Ztied1.S, Pg/M, Ztied1.S, Zop2.D" or "ASR Zresult.S, Zop1.S, Zop2.D"
    /// svint32_t svasr_wide[_s32]_z(svbool_t pg, svint32_t op1, svuint64_t op2) : "MOVPRFX Zresult.S, Pg/Z, Zop1.S; ASR Zresult.S, Pg/M, Zresult.S, Zop2.D"
  public static unsafe Vector<int> ShiftRightArithmetic(Vector<int> left, Vector<ulong> right);


    /// ShiftRightArithmeticForDivide : Arithmetic shift right for divide by immediate

    /// svint8_t svasrd[_n_s8]_m(svbool_t pg, svint8_t op1, uint64_t imm2) : "ASRD Ztied1.B, Pg/M, Ztied1.B, #imm2" or "MOVPRFX Zresult, Zop1; ASRD Zresult.B, Pg/M, Zresult.B, #imm2"
    /// svint8_t svasrd[_n_s8]_x(svbool_t pg, svint8_t op1, uint64_t imm2) : "ASRD Ztied1.B, Pg/M, Ztied1.B, #imm2" or "MOVPRFX Zresult, Zop1; ASRD Zresult.B, Pg/M, Zresult.B, #imm2"
    /// svint8_t svasrd[_n_s8]_z(svbool_t pg, svint8_t op1, uint64_t imm2) : "MOVPRFX Zresult.B, Pg/Z, Zop1.B; ASRD Zresult.B, Pg/M, Zresult.B, #imm2"
  public static unsafe Vector<sbyte> ShiftRightArithmeticForDivide(Vector<sbyte> value, [ConstantExpected] byte control);

    /// svint16_t svasrd[_n_s16]_m(svbool_t pg, svint16_t op1, uint64_t imm2) : "ASRD Ztied1.H, Pg/M, Ztied1.H, #imm2" or "MOVPRFX Zresult, Zop1; ASRD Zresult.H, Pg/M, Zresult.H, #imm2"
    /// svint16_t svasrd[_n_s16]_x(svbool_t pg, svint16_t op1, uint64_t imm2) : "ASRD Ztied1.H, Pg/M, Ztied1.H, #imm2" or "MOVPRFX Zresult, Zop1; ASRD Zresult.H, Pg/M, Zresult.H, #imm2"
    /// svint16_t svasrd[_n_s16]_z(svbool_t pg, svint16_t op1, uint64_t imm2) : "MOVPRFX Zresult.H, Pg/Z, Zop1.H; ASRD Zresult.H, Pg/M, Zresult.H, #imm2"
  public static unsafe Vector<short> ShiftRightArithmeticForDivide(Vector<short> value, [ConstantExpected] byte control);

    /// svint32_t svasrd[_n_s32]_m(svbool_t pg, svint32_t op1, uint64_t imm2) : "ASRD Ztied1.S, Pg/M, Ztied1.S, #imm2" or "MOVPRFX Zresult, Zop1; ASRD Zresult.S, Pg/M, Zresult.S, #imm2"
    /// svint32_t svasrd[_n_s32]_x(svbool_t pg, svint32_t op1, uint64_t imm2) : "ASRD Ztied1.S, Pg/M, Ztied1.S, #imm2" or "MOVPRFX Zresult, Zop1; ASRD Zresult.S, Pg/M, Zresult.S, #imm2"
    /// svint32_t svasrd[_n_s32]_z(svbool_t pg, svint32_t op1, uint64_t imm2) : "MOVPRFX Zresult.S, Pg/Z, Zop1.S; ASRD Zresult.S, Pg/M, Zresult.S, #imm2"
  public static unsafe Vector<int> ShiftRightArithmeticForDivide(Vector<int> value, [ConstantExpected] byte control);

    /// svint64_t svasrd[_n_s64]_m(svbool_t pg, svint64_t op1, uint64_t imm2) : "ASRD Ztied1.D, Pg/M, Ztied1.D, #imm2" or "MOVPRFX Zresult, Zop1; ASRD Zresult.D, Pg/M, Zresult.D, #imm2"
    /// svint64_t svasrd[_n_s64]_x(svbool_t pg, svint64_t op1, uint64_t imm2) : "ASRD Ztied1.D, Pg/M, Ztied1.D, #imm2" or "MOVPRFX Zresult, Zop1; ASRD Zresult.D, Pg/M, Zresult.D, #imm2"
    /// svint64_t svasrd[_n_s64]_z(svbool_t pg, svint64_t op1, uint64_t imm2) : "MOVPRFX Zresult.D, Pg/Z, Zop1.D; ASRD Zresult.D, Pg/M, Zresult.D, #imm2"
  public static unsafe Vector<long> ShiftRightArithmeticForDivide(Vector<long> value, [ConstantExpected] byte control);


    /// ShiftRightLogical : Logical shift right

    /// svuint8_t svlsr[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2) : "LSR Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "MOVPRFX Zresult, Zop1; LSR Zresult.B, Pg/M, Zresult.B, Zop2.B"
    /// svuint8_t svlsr[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2) : "LSR Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "LSRR Ztied2.B, Pg/M, Ztied2.B, Zop1.B" or "MOVPRFX Zresult, Zop1; LSR Zresult.B, Pg/M, Zresult.B, Zop2.B"
    /// svuint8_t svlsr[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2) : "MOVPRFX Zresult.B, Pg/Z, Zop1.B; LSR Zresult.B, Pg/M, Zresult.B, Zop2.B" or "MOVPRFX Zresult.B, Pg/Z, Zop2.B; LSRR Zresult.B, Pg/M, Zresult.B, Zop1.B"
  public static unsafe Vector<byte> ShiftRightLogical(Vector<byte> left, Vector<byte> right);

    /// svuint16_t svlsr[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2) : "LSR Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "MOVPRFX Zresult, Zop1; LSR Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svuint16_t svlsr[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2) : "LSR Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "LSRR Ztied2.H, Pg/M, Ztied2.H, Zop1.H" or "MOVPRFX Zresult, Zop1; LSR Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svuint16_t svlsr[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2) : "MOVPRFX Zresult.H, Pg/Z, Zop1.H; LSR Zresult.H, Pg/M, Zresult.H, Zop2.H" or "MOVPRFX Zresult.H, Pg/Z, Zop2.H; LSRR Zresult.H, Pg/M, Zresult.H, Zop1.H"
  public static unsafe Vector<ushort> ShiftRightLogical(Vector<ushort> left, Vector<ushort> right);

    /// svuint32_t svlsr[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2) : "LSR Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "MOVPRFX Zresult, Zop1; LSR Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svuint32_t svlsr[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2) : "LSR Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "LSRR Ztied2.S, Pg/M, Ztied2.S, Zop1.S" or "MOVPRFX Zresult, Zop1; LSR Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svuint32_t svlsr[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2) : "MOVPRFX Zresult.S, Pg/Z, Zop1.S; LSR Zresult.S, Pg/M, Zresult.S, Zop2.S" or "MOVPRFX Zresult.S, Pg/Z, Zop2.S; LSRR Zresult.S, Pg/M, Zresult.S, Zop1.S"
  public static unsafe Vector<uint> ShiftRightLogical(Vector<uint> left, Vector<uint> right);

    /// svuint64_t svlsr[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2) : "LSR Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; LSR Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svuint64_t svlsr[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2) : "LSR Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "LSRR Ztied2.D, Pg/M, Ztied2.D, Zop1.D" or "MOVPRFX Zresult, Zop1; LSR Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svuint64_t svlsr[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2) : "MOVPRFX Zresult.D, Pg/Z, Zop1.D; LSR Zresult.D, Pg/M, Zresult.D, Zop2.D" or "MOVPRFX Zresult.D, Pg/Z, Zop2.D; LSRR Zresult.D, Pg/M, Zresult.D, Zop1.D"
  public static unsafe Vector<ulong> ShiftRightLogical(Vector<ulong> left, Vector<ulong> right);

    /// svuint8_t svlsr_wide[_u8]_m(svbool_t pg, svuint8_t op1, svuint64_t op2) : "LSR Ztied1.B, Pg/M, Ztied1.B, Zop2.D" or "MOVPRFX Zresult, Zop1; LSR Zresult.B, Pg/M, Zresult.B, Zop2.D"
    /// svuint8_t svlsr_wide[_u8]_x(svbool_t pg, svuint8_t op1, svuint64_t op2) : "LSR Ztied1.B, Pg/M, Ztied1.B, Zop2.D" or "LSR Zresult.B, Zop1.B, Zop2.D"
    /// svuint8_t svlsr_wide[_u8]_z(svbool_t pg, svuint8_t op1, svuint64_t op2) : "MOVPRFX Zresult.B, Pg/Z, Zop1.B; LSR Zresult.B, Pg/M, Zresult.B, Zop2.D"
  public static unsafe Vector<byte> ShiftRightLogical(Vector<byte> left, Vector<ulong> right);

    /// svuint16_t svlsr_wide[_u16]_m(svbool_t pg, svuint16_t op1, svuint64_t op2) : "LSR Ztied1.H, Pg/M, Ztied1.H, Zop2.D" or "MOVPRFX Zresult, Zop1; LSR Zresult.H, Pg/M, Zresult.H, Zop2.D"
    /// svuint16_t svlsr_wide[_u16]_x(svbool_t pg, svuint16_t op1, svuint64_t op2) : "LSR Ztied1.H, Pg/M, Ztied1.H, Zop2.D" or "LSR Zresult.H, Zop1.H, Zop2.D"
    /// svuint16_t svlsr_wide[_u16]_z(svbool_t pg, svuint16_t op1, svuint64_t op2) : "MOVPRFX Zresult.H, Pg/Z, Zop1.H; LSR Zresult.H, Pg/M, Zresult.H, Zop2.D"
  public static unsafe Vector<ushort> ShiftRightLogical(Vector<ushort> left, Vector<ulong> right);

    /// svuint32_t svlsr_wide[_u32]_m(svbool_t pg, svuint32_t op1, svuint64_t op2) : "LSR Ztied1.S, Pg/M, Ztied1.S, Zop2.D" or "MOVPRFX Zresult, Zop1; LSR Zresult.S, Pg/M, Zresult.S, Zop2.D"
    /// svuint32_t svlsr_wide[_u32]_x(svbool_t pg, svuint32_t op1, svuint64_t op2) : "LSR Ztied1.S, Pg/M, Ztied1.S, Zop2.D" or "LSR Zresult.S, Zop1.S, Zop2.D"
    /// svuint32_t svlsr_wide[_u32]_z(svbool_t pg, svuint32_t op1, svuint64_t op2) : "MOVPRFX Zresult.S, Pg/Z, Zop1.S; LSR Zresult.S, Pg/M, Zresult.S, Zop2.D"
  public static unsafe Vector<uint> ShiftRightLogical(Vector<uint> left, Vector<ulong> right);


    /// Xor : Bitwise exclusive OR

    /// svint8_t sveor[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2) : "EOR Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "MOVPRFX Zresult, Zop1; EOR Zresult.B, Pg/M, Zresult.B, Zop2.B"
    /// svint8_t sveor[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2) : "EOR Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "EOR Ztied2.B, Pg/M, Ztied2.B, Zop1.B" or "EOR Zresult.D, Zop1.D, Zop2.D"
    /// svint8_t sveor[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2) : "MOVPRFX Zresult.B, Pg/Z, Zop1.B; EOR Zresult.B, Pg/M, Zresult.B, Zop2.B" or "MOVPRFX Zresult.B, Pg/Z, Zop2.B; EOR Zresult.B, Pg/M, Zresult.B, Zop1.B"
    /// svbool_t sveor[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2) : "EOR Presult.B, Pg/Z, Pop1.B, Pop2.B"
  public static unsafe Vector<sbyte> Xor(Vector<sbyte> left, Vector<sbyte> right);

    /// svint16_t sveor[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2) : "EOR Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "MOVPRFX Zresult, Zop1; EOR Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svint16_t sveor[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2) : "EOR Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "EOR Ztied2.H, Pg/M, Ztied2.H, Zop1.H" or "EOR Zresult.D, Zop1.D, Zop2.D"
    /// svint16_t sveor[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2) : "MOVPRFX Zresult.H, Pg/Z, Zop1.H; EOR Zresult.H, Pg/M, Zresult.H, Zop2.H" or "MOVPRFX Zresult.H, Pg/Z, Zop2.H; EOR Zresult.H, Pg/M, Zresult.H, Zop1.H"
    /// svbool_t sveor[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2) : "EOR Presult.B, Pg/Z, Pop1.B, Pop2.B"
  public static unsafe Vector<short> Xor(Vector<short> left, Vector<short> right);

    /// svint32_t sveor[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2) : "EOR Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "MOVPRFX Zresult, Zop1; EOR Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svint32_t sveor[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2) : "EOR Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "EOR Ztied2.S, Pg/M, Ztied2.S, Zop1.S" or "EOR Zresult.D, Zop1.D, Zop2.D"
    /// svint32_t sveor[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2) : "MOVPRFX Zresult.S, Pg/Z, Zop1.S; EOR Zresult.S, Pg/M, Zresult.S, Zop2.S" or "MOVPRFX Zresult.S, Pg/Z, Zop2.S; EOR Zresult.S, Pg/M, Zresult.S, Zop1.S"
    /// svbool_t sveor[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2) : "EOR Presult.B, Pg/Z, Pop1.B, Pop2.B"
  public static unsafe Vector<int> Xor(Vector<int> left, Vector<int> right);

    /// svint64_t sveor[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2) : "EOR Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; EOR Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svint64_t sveor[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2) : "EOR Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "EOR Ztied2.D, Pg/M, Ztied2.D, Zop1.D" or "EOR Zresult.D, Zop1.D, Zop2.D"
    /// svint64_t sveor[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2) : "MOVPRFX Zresult.D, Pg/Z, Zop1.D; EOR Zresult.D, Pg/M, Zresult.D, Zop2.D" or "MOVPRFX Zresult.D, Pg/Z, Zop2.D; EOR Zresult.D, Pg/M, Zresult.D, Zop1.D"
    /// svbool_t sveor[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2) : "EOR Presult.B, Pg/Z, Pop1.B, Pop2.B"
  public static unsafe Vector<long> Xor(Vector<long> left, Vector<long> right);

    /// svuint8_t sveor[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2) : "EOR Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "MOVPRFX Zresult, Zop1; EOR Zresult.B, Pg/M, Zresult.B, Zop2.B"
    /// svuint8_t sveor[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2) : "EOR Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "EOR Ztied2.B, Pg/M, Ztied2.B, Zop1.B" or "EOR Zresult.D, Zop1.D, Zop2.D"
    /// svuint8_t sveor[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2) : "MOVPRFX Zresult.B, Pg/Z, Zop1.B; EOR Zresult.B, Pg/M, Zresult.B, Zop2.B" or "MOVPRFX Zresult.B, Pg/Z, Zop2.B; EOR Zresult.B, Pg/M, Zresult.B, Zop1.B"
    /// svbool_t sveor[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2) : "EOR Presult.B, Pg/Z, Pop1.B, Pop2.B"
  public static unsafe Vector<byte> Xor(Vector<byte> left, Vector<byte> right);

    /// svuint16_t sveor[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2) : "EOR Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "MOVPRFX Zresult, Zop1; EOR Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svuint16_t sveor[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2) : "EOR Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "EOR Ztied2.H, Pg/M, Ztied2.H, Zop1.H" or "EOR Zresult.D, Zop1.D, Zop2.D"
    /// svuint16_t sveor[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2) : "MOVPRFX Zresult.H, Pg/Z, Zop1.H; EOR Zresult.H, Pg/M, Zresult.H, Zop2.H" or "MOVPRFX Zresult.H, Pg/Z, Zop2.H; EOR Zresult.H, Pg/M, Zresult.H, Zop1.H"
    /// svbool_t sveor[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2) : "EOR Presult.B, Pg/Z, Pop1.B, Pop2.B"
  public static unsafe Vector<ushort> Xor(Vector<ushort> left, Vector<ushort> right);

    /// svuint32_t sveor[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2) : "EOR Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "MOVPRFX Zresult, Zop1; EOR Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svuint32_t sveor[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2) : "EOR Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "EOR Ztied2.S, Pg/M, Ztied2.S, Zop1.S" or "EOR Zresult.D, Zop1.D, Zop2.D"
    /// svuint32_t sveor[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2) : "MOVPRFX Zresult.S, Pg/Z, Zop1.S; EOR Zresult.S, Pg/M, Zresult.S, Zop2.S" or "MOVPRFX Zresult.S, Pg/Z, Zop2.S; EOR Zresult.S, Pg/M, Zresult.S, Zop1.S"
    /// svbool_t sveor[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2) : "EOR Presult.B, Pg/Z, Pop1.B, Pop2.B"
  public static unsafe Vector<uint> Xor(Vector<uint> left, Vector<uint> right);

    /// svuint64_t sveor[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2) : "EOR Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; EOR Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svuint64_t sveor[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2) : "EOR Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "EOR Ztied2.D, Pg/M, Ztied2.D, Zop1.D" or "EOR Zresult.D, Zop1.D, Zop2.D"
    /// svuint64_t sveor[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2) : "MOVPRFX Zresult.D, Pg/Z, Zop1.D; EOR Zresult.D, Pg/M, Zresult.D, Zop2.D" or "MOVPRFX Zresult.D, Pg/Z, Zop2.D; EOR Zresult.D, Pg/M, Zresult.D, Zop1.D"
    /// svbool_t sveor[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2) : "EOR Presult.B, Pg/Z, Pop1.B, Pop2.B"
  public static unsafe Vector<ulong> Xor(Vector<ulong> left, Vector<ulong> right);


    /// XorAcross : Bitwise exclusive OR reduction to scalar

    /// int8_t sveorv[_s8](svbool_t pg, svint8_t op) : "EORV Bresult, Pg, Zop.B"
  public static unsafe Vector<sbyte> XorAcross(Vector<sbyte> value);

    /// int16_t sveorv[_s16](svbool_t pg, svint16_t op) : "EORV Hresult, Pg, Zop.H"
  public static unsafe Vector<short> XorAcross(Vector<short> value);

    /// int32_t sveorv[_s32](svbool_t pg, svint32_t op) : "EORV Sresult, Pg, Zop.S"
  public static unsafe Vector<int> XorAcross(Vector<int> value);

    /// int64_t sveorv[_s64](svbool_t pg, svint64_t op) : "EORV Dresult, Pg, Zop.D"
  public static unsafe Vector<long> XorAcross(Vector<long> value);

    /// uint8_t sveorv[_u8](svbool_t pg, svuint8_t op) : "EORV Bresult, Pg, Zop.B"
  public static unsafe Vector<byte> XorAcross(Vector<byte> value);

    /// uint16_t sveorv[_u16](svbool_t pg, svuint16_t op) : "EORV Hresult, Pg, Zop.H"
  public static unsafe Vector<ushort> XorAcross(Vector<ushort> value);

    /// uint32_t sveorv[_u32](svbool_t pg, svuint32_t op) : "EORV Sresult, Pg, Zop.S"
  public static unsafe Vector<uint> XorAcross(Vector<uint> value);

    /// uint64_t sveorv[_u64](svbool_t pg, svuint64_t op) : "EORV Dresult, Pg, Zop.D"
  public static unsafe Vector<ulong> XorAcross(Vector<ulong> value);


  /// total method signatures: 130
  /// total method names:      16
}

  /// Optional Entries:
  ///   public static unsafe Vector<sbyte> ShiftRightArithmetic(Vector<sbyte> left, byte right); // svasr[_n_s8]_m or svasr[_n_s8]_x or svasr[_n_s8]_z
  ///   public static unsafe Vector<short> ShiftRightArithmetic(Vector<short> left, ushort right); // svasr[_n_s16]_m or svasr[_n_s16]_x or svasr[_n_s16]_z
  ///   public static unsafe Vector<int> ShiftRightArithmetic(Vector<int> left, uint right); // svasr[_n_s32]_m or svasr[_n_s32]_x or svasr[_n_s32]_z
  ///   public static unsafe Vector<long> ShiftRightArithmetic(Vector<long> left, ulong right); // svasr[_n_s64]_m or svasr[_n_s64]_x or svasr[_n_s64]_z
  ///   public static unsafe Vector<sbyte> ShiftRightArithmetic(Vector<sbyte> left, ulong right); // svasr_wide[_n_s8]_m or svasr_wide[_n_s8]_x or svasr_wide[_n_s8]_z
  ///   public static unsafe Vector<short> ShiftRightArithmetic(Vector<short> left, ulong right); // svasr_wide[_n_s16]_m or svasr_wide[_n_s16]_x or svasr_wide[_n_s16]_z
  ///   public static unsafe Vector<int> ShiftRightArithmetic(Vector<int> left, ulong right); // svasr_wide[_n_s32]_m or svasr_wide[_n_s32]_x or svasr_wide[_n_s32]_z
  ///   Total Maybe: 7

  /// Rejected:
  ///   public static unsafe Vector<sbyte> And(Vector<sbyte> left, sbyte right); // svand[_n_s8]_m or svand[_n_s8]_x or svand[_n_s8]_z
  ///   public static unsafe Vector<short> And(Vector<short> left, short right); // svand[_n_s16]_m or svand[_n_s16]_x or svand[_n_s16]_z
  ///   public static unsafe Vector<int> And(Vector<int> left, int right); // svand[_n_s32]_m or svand[_n_s32]_x or svand[_n_s32]_z
  ///   public static unsafe Vector<long> And(Vector<long> left, long right); // svand[_n_s64]_m or svand[_n_s64]_x or svand[_n_s64]_z
  ///   public static unsafe Vector<byte> And(Vector<byte> left, byte right); // svand[_n_u8]_m or svand[_n_u8]_x or svand[_n_u8]_z
  ///   public static unsafe Vector<ushort> And(Vector<ushort> left, ushort right); // svand[_n_u16]_m or svand[_n_u16]_x or svand[_n_u16]_z
  ///   public static unsafe Vector<uint> And(Vector<uint> left, uint right); // svand[_n_u32]_m or svand[_n_u32]_x or svand[_n_u32]_z
  ///   public static unsafe Vector<ulong> And(Vector<ulong> left, ulong right); // svand[_n_u64]_m or svand[_n_u64]_x or svand[_n_u64]_z
  ///   public static unsafe Vector<sbyte> BitwiseClear(Vector<sbyte> left, sbyte right); // svbic[_n_s8]_m or svbic[_n_s8]_x or svbic[_n_s8]_z
  ///   public static unsafe Vector<short> BitwiseClear(Vector<short> left, short right); // svbic[_n_s16]_m or svbic[_n_s16]_x or svbic[_n_s16]_z
  ///   public static unsafe Vector<int> BitwiseClear(Vector<int> left, int right); // svbic[_n_s32]_m or svbic[_n_s32]_x or svbic[_n_s32]_z
  ///   public static unsafe Vector<long> BitwiseClear(Vector<long> left, long right); // svbic[_n_s64]_m or svbic[_n_s64]_x or svbic[_n_s64]_z
  ///   public static unsafe Vector<byte> BitwiseClear(Vector<byte> left, byte right); // svbic[_n_u8]_m or svbic[_n_u8]_x or svbic[_n_u8]_z
  ///   public static unsafe Vector<ushort> BitwiseClear(Vector<ushort> left, ushort right); // svbic[_n_u16]_m or svbic[_n_u16]_x or svbic[_n_u16]_z
  ///   public static unsafe Vector<uint> BitwiseClear(Vector<uint> left, uint right); // svbic[_n_u32]_m or svbic[_n_u32]_x or svbic[_n_u32]_z
  ///   public static unsafe Vector<ulong> BitwiseClear(Vector<ulong> left, ulong right); // svbic[_n_u64]_m or svbic[_n_u64]_x or svbic[_n_u64]_z
  ///   public static unsafe Vector<sbyte> Or(Vector<sbyte> left, sbyte right); // svorr[_n_s8]_m or svorr[_n_s8]_x or svorr[_n_s8]_z
  ///   public static unsafe Vector<short> Or(Vector<short> left, short right); // svorr[_n_s16]_m or svorr[_n_s16]_x or svorr[_n_s16]_z
  ///   public static unsafe Vector<int> Or(Vector<int> left, int right); // svorr[_n_s32]_m or svorr[_n_s32]_x or svorr[_n_s32]_z
  ///   public static unsafe Vector<long> Or(Vector<long> left, long right); // svorr[_n_s64]_m or svorr[_n_s64]_x or svorr[_n_s64]_z
  ///   public static unsafe Vector<byte> Or(Vector<byte> left, byte right); // svorr[_n_u8]_m or svorr[_n_u8]_x or svorr[_n_u8]_z
  ///   public static unsafe Vector<ushort> Or(Vector<ushort> left, ushort right); // svorr[_n_u16]_m or svorr[_n_u16]_x or svorr[_n_u16]_z
  ///   public static unsafe Vector<uint> Or(Vector<uint> left, uint right); // svorr[_n_u32]_m or svorr[_n_u32]_x or svorr[_n_u32]_z
  ///   public static unsafe Vector<ulong> Or(Vector<ulong> left, ulong right); // svorr[_n_u64]_m or svorr[_n_u64]_x or svorr[_n_u64]_z
  ///   public static unsafe Vector<sbyte> ShiftLeftLogical(Vector<sbyte> left, byte right); // svlsl[_n_s8]_m or svlsl[_n_s8]_x or svlsl[_n_s8]_z
  ///   public static unsafe Vector<short> ShiftLeftLogical(Vector<short> left, ushort right); // svlsl[_n_s16]_m or svlsl[_n_s16]_x or svlsl[_n_s16]_z
  ///   public static unsafe Vector<int> ShiftLeftLogical(Vector<int> left, uint right); // svlsl[_n_s32]_m or svlsl[_n_s32]_x or svlsl[_n_s32]_z
  ///   public static unsafe Vector<long> ShiftLeftLogical(Vector<long> left, ulong right); // svlsl[_n_s64]_m or svlsl[_n_s64]_x or svlsl[_n_s64]_z
  ///   public static unsafe Vector<byte> ShiftLeftLogical(Vector<byte> left, byte right); // svlsl[_n_u8]_m or svlsl[_n_u8]_x or svlsl[_n_u8]_z
  ///   public static unsafe Vector<ushort> ShiftLeftLogical(Vector<ushort> left, ushort right); // svlsl[_n_u16]_m or svlsl[_n_u16]_x or svlsl[_n_u16]_z
  ///   public static unsafe Vector<uint> ShiftLeftLogical(Vector<uint> left, uint right); // svlsl[_n_u32]_m or svlsl[_n_u32]_x or svlsl[_n_u32]_z
  ///   public static unsafe Vector<ulong> ShiftLeftLogical(Vector<ulong> left, ulong right); // svlsl[_n_u64]_m or svlsl[_n_u64]_x or svlsl[_n_u64]_z
  ///   public static unsafe Vector<sbyte> ShiftLeftLogical(Vector<sbyte> left, ulong right); // svlsl_wide[_n_s8]_m or svlsl_wide[_n_s8]_x or svlsl_wide[_n_s8]_z
  ///   public static unsafe Vector<short> ShiftLeftLogical(Vector<short> left, ulong right); // svlsl_wide[_n_s16]_m or svlsl_wide[_n_s16]_x or svlsl_wide[_n_s16]_z
  ///   public static unsafe Vector<int> ShiftLeftLogical(Vector<int> left, ulong right); // svlsl_wide[_n_s32]_m or svlsl_wide[_n_s32]_x or svlsl_wide[_n_s32]_z
  ///   public static unsafe Vector<byte> ShiftLeftLogical(Vector<byte> left, ulong right); // svlsl_wide[_n_u8]_m or svlsl_wide[_n_u8]_x or svlsl_wide[_n_u8]_z
  ///   public static unsafe Vector<ushort> ShiftLeftLogical(Vector<ushort> left, ulong right); // svlsl_wide[_n_u16]_m or svlsl_wide[_n_u16]_x or svlsl_wide[_n_u16]_z
  ///   public static unsafe Vector<uint> ShiftLeftLogical(Vector<uint> left, ulong right); // svlsl_wide[_n_u32]_m or svlsl_wide[_n_u32]_x or svlsl_wide[_n_u32]_z
  ///   public static unsafe Vector<byte> ShiftRightLogical(Vector<byte> left, byte right); // svlsr[_n_u8]_m or svlsr[_n_u8]_x or svlsr[_n_u8]_z
  ///   public static unsafe Vector<ushort> ShiftRightLogical(Vector<ushort> left, ushort right); // svlsr[_n_u16]_m or svlsr[_n_u16]_x or svlsr[_n_u16]_z
  ///   public static unsafe Vector<uint> ShiftRightLogical(Vector<uint> left, uint right); // svlsr[_n_u32]_m or svlsr[_n_u32]_x or svlsr[_n_u32]_z
  ///   public static unsafe Vector<ulong> ShiftRightLogical(Vector<ulong> left, ulong right); // svlsr[_n_u64]_m or svlsr[_n_u64]_x or svlsr[_n_u64]_z
  ///   public static unsafe Vector<byte> ShiftRightLogical(Vector<byte> left, ulong right); // svlsr_wide[_n_u8]_m or svlsr_wide[_n_u8]_x or svlsr_wide[_n_u8]_z
  ///   public static unsafe Vector<ushort> ShiftRightLogical(Vector<ushort> left, ulong right); // svlsr_wide[_n_u16]_m or svlsr_wide[_n_u16]_x or svlsr_wide[_n_u16]_z
  ///   public static unsafe Vector<uint> ShiftRightLogical(Vector<uint> left, ulong right); // svlsr_wide[_n_u32]_m or svlsr_wide[_n_u32]_x or svlsr_wide[_n_u32]_z
  ///   public static unsafe Vector<sbyte> Xor(Vector<sbyte> left, sbyte right); // sveor[_n_s8]_m or sveor[_n_s8]_x or sveor[_n_s8]_z
  ///   public static unsafe Vector<short> Xor(Vector<short> left, short right); // sveor[_n_s16]_m or sveor[_n_s16]_x or sveor[_n_s16]_z
  ///   public static unsafe Vector<int> Xor(Vector<int> left, int right); // sveor[_n_s32]_m or sveor[_n_s32]_x or sveor[_n_s32]_z
  ///   public static unsafe Vector<long> Xor(Vector<long> left, long right); // sveor[_n_s64]_m or sveor[_n_s64]_x or sveor[_n_s64]_z
  ///   public static unsafe Vector<byte> Xor(Vector<byte> left, byte right); // sveor[_n_u8]_m or sveor[_n_u8]_x or sveor[_n_u8]_z
  ///   public static unsafe Vector<ushort> Xor(Vector<ushort> left, ushort right); // sveor[_n_u16]_m or sveor[_n_u16]_x or sveor[_n_u16]_z
  ///   public static unsafe Vector<uint> Xor(Vector<uint> left, uint right); // sveor[_n_u32]_m or sveor[_n_u32]_x or sveor[_n_u32]_z
  ///   public static unsafe Vector<ulong> Xor(Vector<ulong> left, ulong right); // sveor[_n_u64]_m or sveor[_n_u64]_x or sveor[_n_u64]_z
  ///   Total Rejected: 53

  /// Total ACLE covered across API:      518

