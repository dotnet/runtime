namespace System.Runtime.Intrinsics.Arm;

/// VectorT Summary
public abstract partial class Sve2 : AdvSimd /// Feature: FEAT_SVE2  Category: counting
{

  /// T: [uint, int], [ulong, long]
  public static unsafe Vector<T> CountMatchingElements(Vector<T2> mask, Vector<T2> left, Vector<T2> right); // HISTCNT

  /// T: uint, ulong
  public static unsafe Vector<T> CountMatchingElements(Vector<T> mask, Vector<T> left, Vector<T> right); // HISTCNT

  public static unsafe Vector<byte> CountMatchingElementsIn128BitSegments(Vector<sbyte> left, Vector<sbyte> right); // HISTSEG

  public static unsafe Vector<byte> CountMatchingElementsIn128BitSegments(Vector<byte> left, Vector<byte> right); // HISTSEG

  /// total method signatures: 4

}


/// Full API
public abstract partial class Sve2 : AdvSimd /// Feature: FEAT_SVE2  Category: counting
{
    /// CountMatchingElements : Count matching elements

    /// svuint32_t svhistcnt[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2) : "HISTCNT Zresult.S, Pg/Z, Zop1.S, Zop2.S"
  public static unsafe Vector<uint> CountMatchingElements(Vector<int> mask, Vector<int> left, Vector<int> right);

    /// svuint64_t svhistcnt[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2) : "HISTCNT Zresult.D, Pg/Z, Zop1.D, Zop2.D"
  public static unsafe Vector<ulong> CountMatchingElements(Vector<long> mask, Vector<long> left, Vector<long> right);

    /// svuint32_t svhistcnt[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2) : "HISTCNT Zresult.S, Pg/Z, Zop1.S, Zop2.S"
  public static unsafe Vector<uint> CountMatchingElements(Vector<uint> mask, Vector<uint> left, Vector<uint> right);

    /// svuint64_t svhistcnt[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2) : "HISTCNT Zresult.D, Pg/Z, Zop1.D, Zop2.D"
  public static unsafe Vector<ulong> CountMatchingElements(Vector<ulong> mask, Vector<ulong> left, Vector<ulong> right);


    /// CountMatchingElementsIn128BitSegments : Count matching elements in 128-bit segments

    /// svuint8_t svhistseg[_s8](svint8_t op1, svint8_t op2) : "HISTSEG Zresult.B, Zop1.B, Zop2.B"
  public static unsafe Vector<byte> CountMatchingElementsIn128BitSegments(Vector<sbyte> left, Vector<sbyte> right);

    /// svuint8_t svhistseg[_u8](svuint8_t op1, svuint8_t op2) : "HISTSEG Zresult.B, Zop1.B, Zop2.B"
  public static unsafe Vector<byte> CountMatchingElementsIn128BitSegments(Vector<byte> left, Vector<byte> right);


  /// total method signatures: 6
  /// total method names:      2
}


  /// Total ACLE covered across API:      6

