namespace System.Runtime.Intrinsics.Arm;

/// VectorT Summary
public abstract partial class SveBitperm : AdvSimd /// Feature: FEAT_SVE_BitPerm
{

  /// T: byte, ushort, uint, ulong
  public static unsafe Vector<T> GatherLowerBitsFromPositionsSelectedByBitmask(Vector<T> left, Vector<T> right); // BEXT

  /// T: byte, ushort, uint, ulong
  public static unsafe Vector<T> GroupBitsToRightOrLeftAsSelectedByBitmask(Vector<T> left, Vector<T> right); // BGRP

  /// T: byte, ushort, uint, ulong
  public static unsafe Vector<T> ScatterLowerBitsIntoPositionsSelectedByBitmask(Vector<T> left, Vector<T> right); // BDEP

  /// total method signatures: 3


  /// Optional Entries:

  /// T: byte, ushort, uint, ulong
  public static unsafe Vector<T> GatherLowerBitsFromPositionsSelectedByBitmask(Vector<T> left, T right); // BEXT

  /// T: byte, ushort, uint, ulong
  public static unsafe Vector<T> GroupBitsToRightOrLeftAsSelectedByBitmask(Vector<T> left, T right); // BGRP

  /// T: byte, ushort, uint, ulong
  public static unsafe Vector<T> ScatterLowerBitsIntoPositionsSelectedByBitmask(Vector<T> left, T right); // BDEP

  /// total optional method signatures: 3

}


/// Full API
public abstract partial class SveBitperm : AdvSimd /// Feature: FEAT_SVE_BitPerm
{
    /// GatherLowerBitsFromPositionsSelectedByBitmask : Gather lower bits from positions selected by bitmask

    /// svuint8_t svbext[_u8](svuint8_t op1, svuint8_t op2) : "BEXT Zresult.B, Zop1.B, Zop2.B"
  public static unsafe Vector<byte> GatherLowerBitsFromPositionsSelectedByBitmask(Vector<byte> left, Vector<byte> right);

    /// svuint16_t svbext[_u16](svuint16_t op1, svuint16_t op2) : "BEXT Zresult.H, Zop1.H, Zop2.H"
  public static unsafe Vector<ushort> GatherLowerBitsFromPositionsSelectedByBitmask(Vector<ushort> left, Vector<ushort> right);

    /// svuint32_t svbext[_u32](svuint32_t op1, svuint32_t op2) : "BEXT Zresult.S, Zop1.S, Zop2.S"
  public static unsafe Vector<uint> GatherLowerBitsFromPositionsSelectedByBitmask(Vector<uint> left, Vector<uint> right);

    /// svuint64_t svbext[_u64](svuint64_t op1, svuint64_t op2) : "BEXT Zresult.D, Zop1.D, Zop2.D"
  public static unsafe Vector<ulong> GatherLowerBitsFromPositionsSelectedByBitmask(Vector<ulong> left, Vector<ulong> right);


    /// GroupBitsToRightOrLeftAsSelectedByBitmask : Group bits to right or left as selected by bitmask

    /// svuint8_t svbgrp[_u8](svuint8_t op1, svuint8_t op2) : "BGRP Zresult.B, Zop1.B, Zop2.B"
  public static unsafe Vector<byte> GroupBitsToRightOrLeftAsSelectedByBitmask(Vector<byte> left, Vector<byte> right);

    /// svuint16_t svbgrp[_u16](svuint16_t op1, svuint16_t op2) : "BGRP Zresult.H, Zop1.H, Zop2.H"
  public static unsafe Vector<ushort> GroupBitsToRightOrLeftAsSelectedByBitmask(Vector<ushort> left, Vector<ushort> right);

    /// svuint32_t svbgrp[_u32](svuint32_t op1, svuint32_t op2) : "BGRP Zresult.S, Zop1.S, Zop2.S"
  public static unsafe Vector<uint> GroupBitsToRightOrLeftAsSelectedByBitmask(Vector<uint> left, Vector<uint> right);

    /// svuint64_t svbgrp[_u64](svuint64_t op1, svuint64_t op2) : "BGRP Zresult.D, Zop1.D, Zop2.D"
  public static unsafe Vector<ulong> GroupBitsToRightOrLeftAsSelectedByBitmask(Vector<ulong> left, Vector<ulong> right);


    /// ScatterLowerBitsIntoPositionsSelectedByBitmask : Scatter lower bits into positions selected by bitmask

    /// svuint8_t svbdep[_u8](svuint8_t op1, svuint8_t op2) : "BDEP Zresult.B, Zop1.B, Zop2.B"
  public static unsafe Vector<byte> ScatterLowerBitsIntoPositionsSelectedByBitmask(Vector<byte> left, Vector<byte> right);

    /// svuint16_t svbdep[_u16](svuint16_t op1, svuint16_t op2) : "BDEP Zresult.H, Zop1.H, Zop2.H"
  public static unsafe Vector<ushort> ScatterLowerBitsIntoPositionsSelectedByBitmask(Vector<ushort> left, Vector<ushort> right);

    /// svuint32_t svbdep[_u32](svuint32_t op1, svuint32_t op2) : "BDEP Zresult.S, Zop1.S, Zop2.S"
  public static unsafe Vector<uint> ScatterLowerBitsIntoPositionsSelectedByBitmask(Vector<uint> left, Vector<uint> right);

    /// svuint64_t svbdep[_u64](svuint64_t op1, svuint64_t op2) : "BDEP Zresult.D, Zop1.D, Zop2.D"
  public static unsafe Vector<ulong> ScatterLowerBitsIntoPositionsSelectedByBitmask(Vector<ulong> left, Vector<ulong> right);


  /// total method signatures: 12
  /// total method names:      3
}

  /// Optional Entries:
  ///   public static unsafe Vector<byte> GatherLowerBitsFromPositionsSelectedByBitmask(Vector<byte> left, byte right); // svbext[_n_u8]
  ///   public static unsafe Vector<ushort> GatherLowerBitsFromPositionsSelectedByBitmask(Vector<ushort> left, ushort right); // svbext[_n_u16]
  ///   public static unsafe Vector<uint> GatherLowerBitsFromPositionsSelectedByBitmask(Vector<uint> left, uint right); // svbext[_n_u32]
  ///   public static unsafe Vector<ulong> GatherLowerBitsFromPositionsSelectedByBitmask(Vector<ulong> left, ulong right); // svbext[_n_u64]
  ///   public static unsafe Vector<byte> GroupBitsToRightOrLeftAsSelectedByBitmask(Vector<byte> left, byte right); // svbgrp[_n_u8]
  ///   public static unsafe Vector<ushort> GroupBitsToRightOrLeftAsSelectedByBitmask(Vector<ushort> left, ushort right); // svbgrp[_n_u16]
  ///   public static unsafe Vector<uint> GroupBitsToRightOrLeftAsSelectedByBitmask(Vector<uint> left, uint right); // svbgrp[_n_u32]
  ///   public static unsafe Vector<ulong> GroupBitsToRightOrLeftAsSelectedByBitmask(Vector<ulong> left, ulong right); // svbgrp[_n_u64]
  ///   public static unsafe Vector<byte> ScatterLowerBitsIntoPositionsSelectedByBitmask(Vector<byte> left, byte right); // svbdep[_n_u8]
  ///   public static unsafe Vector<ushort> ScatterLowerBitsIntoPositionsSelectedByBitmask(Vector<ushort> left, ushort right); // svbdep[_n_u16]
  ///   public static unsafe Vector<uint> ScatterLowerBitsIntoPositionsSelectedByBitmask(Vector<uint> left, uint right); // svbdep[_n_u32]
  ///   public static unsafe Vector<ulong> ScatterLowerBitsIntoPositionsSelectedByBitmask(Vector<ulong> left, ulong right); // svbdep[_n_u64]
  ///   Total Maybe: 12

  /// Total ACLE covered across API:      24

