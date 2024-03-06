namespace System.Runtime.Intrinsics.Arm;

public abstract partial class Sve : AdvSimd
{

  /// T: [short, uint], [ushort, uint], [short, ulong], [ushort, ulong]
  public static unsafe void GatherPrefetch16Bit(Vector<T> mask, Vector<T2> addresses, [ConstantExpected] SvePrefetchType prefetchType);

  /// T: [short, int], [ushort, int], [short, uint], [ushort, uint], [short, long], [ushort, long], [short, ulong], [ushort, ulong]
  public static unsafe void GatherPrefetch16Bit(Vector<T> mask, void* address, Vector<T2> indices, [ConstantExpected] SvePrefetchType prefetchType);

  /// T: [int, uint], [uint, uint], [int, ulong], [uint, ulong]
  public static unsafe void GatherPrefetch32Bit(Vector<T> mask, Vector<T2> addresses, [ConstantExpected] SvePrefetchType prefetchType);

  /// T: [int, int], [uint, int], [int, uint], [uint, uint], [int, long], [uint, long], [int, ulong], [uint, ulong]
  public static unsafe void GatherPrefetch32Bit(Vector<T> mask, void* address, Vector<T2> indices, [ConstantExpected] SvePrefetchType prefetchType);

  /// T: [long, uint], [ulong, uint], [long, ulong], [ulong, ulong]
  public static unsafe void GatherPrefetch64Bit(Vector<T> mask, Vector<T2> addresses, [ConstantExpected] SvePrefetchType prefetchType);

  /// T: [long, int], [ulong, int], [long, uint], [ulong, uint], [long, long], [ulong, long], [long, ulong], [ulong, ulong]
  public static unsafe void GatherPrefetch64Bit(Vector<T> mask, void* address, Vector<T2> indices, [ConstantExpected] SvePrefetchType prefetchType);

  /// T: [byte, uint], [sbyte, uint], [byte, ulong], [sbyte, ulong]
  public static unsafe void GatherPrefetch8Bit(Vector<T> mask, Vector<T2> addresses, [ConstantExpected] SvePrefetchType prefetchType);

  /// T: [byte, int], [sbyte, int], [byte, uint], [sbyte, uint], [byte, long], [sbyte, long], [byte, ulong], [sbyte, ulong]
  public static unsafe void GatherPrefetch8Bit(Vector<T> mask, void* address, Vector<T2> offsets, [ConstantExpected] SvePrefetchType prefetchType);

  /// T: [float, uint], [int, uint], [uint, uint], [double, ulong], [long, ulong], [ulong, ulong]
  public static unsafe Vector<T> GatherVector(Vector<T> mask, Vector<T2> addresses);

  /// T: [float, int], [int, int], [uint, int], [float, uint], [int, uint], [uint, uint], [double, long], [long, long], [ulong, long], [double, ulong], [long, ulong], [ulong, ulong]
  public static unsafe Vector<T> GatherVector(Vector<T> mask, T* address, Vector<T2> indices);

  /// T: [int, uint], [uint, uint], [long, ulong], [ulong, ulong]
  public static unsafe Vector<T> GatherVectorByteZeroExtend(Vector<T> mask, Vector<T2> addresses);

  /// T: [int, int], [uint, int], [int, uint], [uint, uint], [long, long], [ulong, long], [long, ulong], [ulong, ulong]
  public static unsafe Vector<T> GatherVectorByteZeroExtend(Vector<T> mask, byte* address, Vector<T2> indices);

  /// T: [int, uint], [uint, uint], [long, ulong], [ulong, ulong]
  public static unsafe Vector<T> GatherVectorInt16SignExtend(Vector<T> mask, Vector<T2> addresses);

  /// T: [int, int], [uint, int], [int, uint], [uint, uint], [long, long], [ulong, long], [long, ulong], [ulong, ulong]
  public static unsafe Vector<T> GatherVectorInt16SignExtend(Vector<T> mask, short* address, Vector<T2> indices);

  /// T: [int, int], [uint, int], [int, uint], [uint, uint], [long, long], [ulong, long], [long, ulong], [ulong, ulong]
  public static unsafe Vector<T> GatherVectorInt16WithByteOffsetsSignExtend(Vector<T> mask, short* address, Vector<T2> offsets);

  /// T: [int, uint], [uint, uint], [long, ulong], [ulong, ulong]
  public static unsafe Vector<T> GatherVectorInt32SignExtend(Vector<T> mask, Vector<T2> addresses);

  /// T: [int, int], [uint, int], [int, uint], [uint, uint], [long, long], [ulong, long], [long, ulong], [ulong, ulong]
  public static unsafe Vector<T> GatherVectorInt32SignExtend(Vector<T> mask, short* address, Vector<T2> indices);

  /// T: [int, int], [uint, int], [int, uint], [uint, uint], [long, long], [ulong, long], [long, ulong], [ulong, ulong]
  public static unsafe Vector<T> GatherVectorInt32WithByteOffsetsSignExtend(Vector<T> mask, short* address, Vector<T2> offsets);

  /// T: [int, uint], [uint, uint], [long, ulong], [ulong, ulong]
  public static unsafe Vector<T> GatherVectorSByteSignExtend(Vector<T> mask, Vector<T2> addresses);

  /// T: [int, int], [uint, int], [int, uint], [uint, uint], [long, long], [ulong, long], [long, ulong], [ulong, ulong]
  public static unsafe Vector<T> GatherVectorSByteSignExtend(Vector<T> mask, sbyte* address, Vector<T2> indices);

  /// T: [int, int], [uint, int], [int, uint], [uint, uint], [long, long], [ulong, long], [long, ulong], [ulong, ulong]
  public static unsafe Vector<T> GatherVectorUInt16WithByteOffsetsZeroExtend(Vector<T> mask, short* address, Vector<T2> offsets);

  /// T: [int, uint], [uint, uint], [long, ulong], [ulong, ulong]
  public static unsafe Vector<T> GatherVectorUInt16ZeroExtend(Vector<T> mask, Vector<T2> addresses);

  /// T: [int, int], [uint, int], [int, uint], [uint, uint], [long, long], [ulong, long], [long, ulong], [ulong, ulong]
  public static unsafe Vector<T> GatherVectorUInt16ZeroExtend(Vector<T> mask, short* address, Vector<T2> indices);

  /// T: [int, int], [uint, int], [int, uint], [uint, uint], [long, long], [ulong, long], [long, ulong], [ulong, ulong]
  public static unsafe Vector<T> GatherVectorUInt32WithByteOffsetsZeroExtend(Vector<T> mask, short* address, Vector<T2> offsets);

  /// T: [int, uint], [uint, uint], [long, ulong], [ulong, ulong]
  public static unsafe Vector<T> GatherVectorUInt32ZeroExtend(Vector<T> mask, Vector<T2> addresses);

  /// T: [int, int], [uint, int], [int, uint], [uint, uint], [long, long], [ulong, long], [long, ulong], [ulong, ulong]
  public static unsafe Vector<T> GatherVectorUInt32ZeroExtend(Vector<T> mask, short* address, Vector<T2> indices);

  /// T: [float, int], [int, int], [uint, int], [float, uint], [int, uint], [uint, uint], [double, long], [long, long], [ulong, long], [double, ulong], [long, ulong], [ulong, ulong]
  public static unsafe Vector<T> GatherVectorWithByteOffsets(Vector<T> mask, T* address, Vector<T2> offsets);

}

public enum SvePrefetchType
{
    LoadL1Temporal = 0,
    LoadL1NonTemporal = 1,
    LoadL2Temporal = 2,
    LoadL2NonTemporal = 3,
    LoadL3Temporal = 4,
    LoadL3NonTemporal = 5,
    StoreL1Temporal = 8,
    StoreL1NonTemporal = 9,
    StoreL2Temporal = 10,
    StoreL2NonTemporal = 11,
    StoreL3Temporal = 12,
    StoreL3NonTemporal = 13
};
