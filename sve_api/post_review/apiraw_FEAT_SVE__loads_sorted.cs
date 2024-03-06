namespace System.Runtime.Intrinsics.Arm;

/// VectorT Summary
public abstract class Sve : AdvSimd /// Feature: FEAT_SVE  Category: loads
{

  /// T: [uint, int], [ulong, long]
  public static unsafe Vector<T> Compute16BitAddresses(Vector<T> bases, Vector<T2> indices); // ADR

  /// T: uint, ulong
  public static unsafe Vector<T> Compute16BitAddresses(Vector<T> bases, Vector<T> indices); // ADR

  /// T: [uint, int], [ulong, long]
  public static unsafe Vector<T> Compute32BitAddresses(Vector<T> bases, Vector<T2> indices); // ADR

  /// T: uint, ulong
  public static unsafe Vector<T> Compute32BitAddresses(Vector<T> bases, Vector<T> indices); // ADR

  /// T: [uint, int], [ulong, long]
  public static unsafe Vector<T> Compute64BitAddresses(Vector<T> bases, Vector<T2> indices); // ADR

  /// T: uint, ulong
  public static unsafe Vector<T> Compute64BitAddresses(Vector<T> bases, Vector<T> indices); // ADR

  /// T: [uint, int], [ulong, long]
  public static unsafe Vector<T> Compute8BitAddresses(Vector<T> bases, Vector<T2> indices); // ADR

  /// T: uint, ulong
  public static unsafe Vector<T> Compute8BitAddresses(Vector<T> bases, Vector<T> indices); // ADR

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> LoadVector(T* address); // LD1W or LD1D or LD1B or LD1H

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> LoadVector128AndReplicateToVector(Vector<T> mask, T* address); // LD1RQW or LD1RQD or LD1RQB or LD1RQH

  public static unsafe Vector<short> LoadVectorByteNonFaultingZeroExtendToInt16(Vector<short> mask, byte* address); // LDNF1B

  public static unsafe Vector<int> LoadVectorByteNonFaultingZeroExtendToInt32(Vector<int> mask, byte* address); // LDNF1B

  public static unsafe Vector<long> LoadVectorByteNonFaultingZeroExtendToInt64(Vector<long> mask, byte* address); // LDNF1B

  public static unsafe Vector<ushort> LoadVectorByteNonFaultingZeroExtendToUInt16(Vector<ushort> mask, byte* address); // LDNF1B

  public static unsafe Vector<uint> LoadVectorByteNonFaultingZeroExtendToUInt32(Vector<uint> mask, byte* address); // LDNF1B

  public static unsafe Vector<ulong> LoadVectorByteNonFaultingZeroExtendToUInt64(Vector<ulong> mask, byte* address); // LDNF1B

  public static unsafe Vector<short> LoadVectorByteZeroExtendToInt16(byte* address); // LD1B

  public static unsafe Vector<int> LoadVectorByteZeroExtendToInt32(byte* address); // LD1B

  public static unsafe Vector<long> LoadVectorByteZeroExtendToInt64(byte* address); // LD1B

  public static unsafe Vector<ushort> LoadVectorByteZeroExtendToUInt16(byte* address); // LD1B

  public static unsafe Vector<uint> LoadVectorByteZeroExtendToUInt32(byte* address); // LD1B

  public static unsafe Vector<ulong> LoadVectorByteZeroExtendToUInt64(byte* address); // LD1B

  public static unsafe Vector<int> LoadVectorInt16NonFaultingSignExtendToInt32(Vector<int> mask, short* address); // LDNF1SH

  public static unsafe Vector<long> LoadVectorInt16NonFaultingSignExtendToInt64(Vector<long> mask, short* address); // LDNF1SH

  public static unsafe Vector<uint> LoadVectorInt16NonFaultingSignExtendToUInt32(Vector<uint> mask, short* address); // LDNF1SH

  public static unsafe Vector<ulong> LoadVectorInt16NonFaultingSignExtendToUInt64(Vector<ulong> mask, short* address); // LDNF1SH

  public static unsafe Vector<int> LoadVectorInt16NonFaultingZeroExtendToInt32(Vector<int> mask, ushort* address); // LDNF1H

  public static unsafe Vector<long> LoadVectorInt16NonFaultingZeroExtendToInt64(Vector<long> mask, ushort* address); // LDNF1H

  public static unsafe Vector<uint> LoadVectorInt16NonFaultingZeroExtendToUInt32(Vector<uint> mask, ushort* address); // LDNF1H

  public static unsafe Vector<ulong> LoadVectorInt16NonFaultingZeroExtendToUInt64(Vector<ulong> mask, ushort* address); // LDNF1H

  public static unsafe Vector<int> LoadVectorInt16SignExtendToInt32(short* address); // LD1SH

  public static unsafe Vector<long> LoadVectorInt16SignExtendToInt64(short* address); // LD1SH

  public static unsafe Vector<uint> LoadVectorInt16SignExtendToUInt32(short* address); // LD1SH

  public static unsafe Vector<ulong> LoadVectorInt16SignExtendToUInt64(short* address); // LD1SH

  public static unsafe Vector<long> LoadVectorInt32NonFaultingSignExtendToInt64(Vector<long> mask, int* address); // LDNF1SW

  public static unsafe Vector<ulong> LoadVectorInt32NonFaultingSignExtendToUInt64(Vector<ulong> mask, int* address); // LDNF1SW

  public static unsafe Vector<long> LoadVectorInt32NonFaultingZeroExtendToInt64(Vector<long> mask, uint* address); // LDNF1W

  public static unsafe Vector<ulong> LoadVectorInt32NonFaultingZeroExtendToUInt64(Vector<ulong> mask, uint* address); // LDNF1W

  public static unsafe Vector<long> LoadVectorInt32SignExtendToInt64(int* address); // LD1SW

  public static unsafe Vector<ulong> LoadVectorInt32SignExtendToUInt64(int* address); // LD1SW

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> LoadVectorNonFaulting(Vector<T> mask, T* address); // LDNF1W or LDNF1D or LDNF1B or LDNF1H

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> LoadVectorNonTemporal(T* address); // LDNT1W or LDNT1D or LDNT1B or LDNT1H

  public static unsafe Vector<short> LoadVectorSByteNonFaultingSignExtendToInt16(Vector<short> mask, sbyte* address); // LDNF1SB

  public static unsafe Vector<int> LoadVectorSByteNonFaultingSignExtendToInt32(Vector<int> mask, sbyte* address); // LDNF1SB

  public static unsafe Vector<long> LoadVectorSByteNonFaultingSignExtendToInt64(Vector<long> mask, sbyte* address); // LDNF1SB

  public static unsafe Vector<ushort> LoadVectorSByteNonFaultingSignExtendToUInt16(Vector<ushort> mask, sbyte* address); // LDNF1SB

  public static unsafe Vector<uint> LoadVectorSByteNonFaultingSignExtendToUInt32(Vector<uint> mask, sbyte* address); // LDNF1SB

  public static unsafe Vector<ulong> LoadVectorSByteNonFaultingSignExtendToUInt64(Vector<ulong> mask, sbyte* address); // LDNF1SB

  public static unsafe Vector<short> LoadVectorSByteSignExtendToInt16(sbyte* address); // LD1SB

  public static unsafe Vector<int> LoadVectorSByteSignExtendToInt32(sbyte* address); // LD1SB

  public static unsafe Vector<long> LoadVectorByteSignExtendToInt64(sbyte* address); // LD1SB

  public static unsafe Vector<ushort> LoadVectorByteSignExtendToUInt16(sbyte* address); // LD1SB

  public static unsafe Vector<uint> LoadVectorByteSignExtendToUInt32(sbyte* address); // LD1SB

  public static unsafe Vector<ulong> LoadVectorByteSignExtendToUInt64(sbyte* address); // LD1SB

  public static unsafe Vector<int> LoadVectorInt16ZeroExtendToInt32(ushort* address); // LD1H

  public static unsafe Vector<long> LoadVectorInt16ZeroExtendToInt64(ushort* address); // LD1H

  public static unsafe Vector<uint> LoadVectorInt16ZeroExtendToUInt32(ushort* address); // LD1H

  public static unsafe Vector<ulong> LoadVectorInt16ZeroExtendToUInt64(ushort* address); // LD1H

  public static unsafe Vector<long> LoadVectorInt32ZeroExtendToInt64(uint* address); // LD1W

  public static unsafe Vector<ulong> LoadVectorInt32ZeroExtendToUInt64(uint* address); // LD1W

}