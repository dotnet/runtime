namespace System.Runtime.Intrinsics.Arm;

/// VectorT Summary
public abstract partial class Sve : AdvSimd /// Feature: FEAT_SVE  Category: gatherloads
{

  /// T: [ushort, uint], [short, uint], [ushort, ulong], [short, ulong]
  public static unsafe void GatherPrefetch16Bit(Vector<T> mask, Vector<T2> addresses, [ConstantExpected] SvePrefetchType prefetchType); // PRFH

  /// T: [ushort, int], [short, int], [ushort, uint], [short, uint], [ushort, long], [short, long], [ushort, ulong], [short, ulong]
  public static unsafe void GatherPrefetch16Bit(Vector<T> mask, void* address, Vector<T2> indices, [ConstantExpected] SvePrefetchType prefetchType); // PRFH

  /// T: [uint, uint], [int, uint], [uint, ulong], [int, ulong]
  public static unsafe void GatherPrefetch32Bit(Vector<T> mask, Vector<T2> addresses, [ConstantExpected] SvePrefetchType prefetchType); // PRFW

  /// T: [uint, int], [int, int], [uint, uint], [int, uint], [uint, long], [int, long], [uint, ulong], [int, ulong]
  public static unsafe void GatherPrefetch32Bit(Vector<T> mask, void* address, Vector<T2> indices, [ConstantExpected] SvePrefetchType prefetchType); // PRFW

  /// T: [ulong, uint], [long, uint], [ulong, ulong], [long, ulong]
  public static unsafe void GatherPrefetch64Bit(Vector<T> mask, Vector<T2> addresses, [ConstantExpected] SvePrefetchType prefetchType); // PRFD

  /// T: [ulong, int], [long, int], [ulong, uint], [long, uint], [ulong, long], [long, long], [ulong, ulong], [long, ulong]
  public static unsafe void GatherPrefetch64Bit(Vector<T> mask, void* address, Vector<T2> indices, [ConstantExpected] SvePrefetchType prefetchType); // PRFD

  /// T: [byte, uint], [sbyte, uint], [byte, ulong], [sbyte, ulong]
  public static unsafe void GatherPrefetch8Bit(Vector<T> mask, Vector<T2> addresses, [ConstantExpected] SvePrefetchType prefetchType); // PRFB

  /// T: [byte, int], [sbyte, int], [byte, uint], [sbyte, uint], [byte, long], [sbyte, long], [byte, ulong], [sbyte, ulong]
  public static unsafe void GatherPrefetch8Bit(Vector<T> mask, void* address, Vector<T2> offsets, [ConstantExpected] SvePrefetchType prefetchType); // PRFB

  /// T: [float, uint], [int, uint], [uint, uint], [double, ulong], [long, ulong], [ulong, ulong]
  public static unsafe Vector<T> GatherVector(Vector<T> mask, Vector<T2> addresses); // LD1W or LD1D

  /// T: [float, int], [int, int], [uint, int], [float, uint], [int, uint], [uint, uint], [double, long], [long, long], [ulong, long], [double, ulong], [long, ulong], [ulong, ulong]
  public static unsafe Vector<T> GatherVector(Vector<T> mask, T* address, Vector<T2> indices); // LD1W or LD1D

  /// T: [int, uint], [uint, uint], [long, ulong], [ulong, ulong]
  public static unsafe Vector<T> GatherVectorByteZeroExtend(Vector<T> mask, Vector<T2> addresses); // LD1B

  /// T: [int, int], [uint, int], [int, uint], [uint, uint], [long, long], [ulong, long], [long, ulong], [ulong, ulong]
  public static unsafe Vector<T> GatherVectorByteZeroExtend(Vector<T> mask, byte* address, Vector<T2> indices); // LD1B

  /// T: [int, uint], [uint, uint], [long, ulong], [ulong, ulong]
  public static unsafe Vector<T> GatherVectorInt16SignExtend(Vector<T> mask, Vector<T2> addresses); // LD1SH

  /// T: [int, int], [uint, int], [int, uint], [uint, uint], [long, long], [ulong, long], [long, ulong], [ulong, ulong]
  public static unsafe Vector<T> GatherVectorInt16SignExtend(Vector<T> mask, short* address, Vector<T2> indices); // LD1SH

  /// T: [int, int], [uint, int], [int, uint], [uint, uint], [long, long], [ulong, long], [long, ulong], [ulong, ulong]
  public static unsafe Vector<T> GatherVectorInt16WithByteOffsetsSignExtend(Vector<T> mask, short* address, Vector<T2> offsets); // LD1SH

  /// T: [long, ulong], [int, uint], [ulong, ulong], [uint, uint]
  public static unsafe Vector<T> GatherVectorInt32SignExtend(Vector<T> mask, Vector<T2> addresses); // LD1SW

  /// T: [long, long], [int, int], [ulong, long], [uint, int], [long, ulong], [int, uint], [ulong, ulong], [uint, uint]
  public static unsafe Vector<T> GatherVectorInt32SignExtend(Vector<T> mask, int* address, Vector<T2> indices); // LD1SW

  /// T: [long, long], [int, int], [ulong, long], [uint, int], [long, ulong], [int, uint], [ulong, ulong], [uint, uint]
  public static unsafe Vector<T> GatherVectorInt32WithByteOffsetsSignExtend(Vector<T> mask, int* address, Vector<T2> offsets); // LD1SW

  /// T: [int, uint], [uint, uint], [long, ulong], [ulong, ulong]
  public static unsafe Vector<T> GatherVectorSByteSignExtend(Vector<T> mask, Vector<T2> addresses); // LD1SB

  /// T: [int, int], [uint, int], [int, uint], [uint, uint], [long, long], [ulong, long], [long, ulong], [ulong, ulong]
  public static unsafe Vector<T> GatherVectorSByteSignExtend(Vector<T> mask, sbyte* address, Vector<T2> indices); // LD1SB

  /// T: [int, int], [uint, int], [int, uint], [uint, uint], [long, long], [ulong, long], [long, ulong], [ulong, ulong]
  public static unsafe Vector<T> GatherVectorUInt16WithByteOffsetsZeroExtend(Vector<T> mask, ushort* address, Vector<T2> offsets); // LD1H

  /// T: [int, uint], [uint, uint], [long, ulong], [ulong, ulong]
  public static unsafe Vector<T> GatherVectorUInt16ZeroExtend(Vector<T> mask, Vector<T2> addresses); // LD1H

  /// T: [int, int], [uint, int], [int, uint], [uint, uint], [long, long], [ulong, long], [long, ulong], [ulong, ulong]
  public static unsafe Vector<T> GatherVectorUInt16ZeroExtend(Vector<T> mask, ushort* address, Vector<T2> indices); // LD1H

  /// T: [long, long], [int, int], [ulong, long], [uint, int], [long, ulong], [int, uint], [ulong, ulong], [uint, uint]
  public static unsafe Vector<T> GatherVectorUInt32WithByteOffsetsZeroExtend(Vector<T> mask, uint* address, Vector<T2> offsets); // LD1W

  /// T: [long, ulong], [int, uint], [ulong, ulong], [uint, uint]
  public static unsafe Vector<T> GatherVectorUInt32ZeroExtend(Vector<T> mask, Vector<T2> addresses); // LD1W

  /// T: [long, long], [int, int], [ulong, long], [uint, int], [long, ulong], [int, uint], [ulong, ulong], [uint, uint]
  public static unsafe Vector<T> GatherVectorUInt32ZeroExtend(Vector<T> mask, uint* address, Vector<T2> indices); // LD1W

  /// T: [float, int], [int, int], [uint, int], [float, uint], [int, uint], [uint, uint], [double, long], [long, long], [ulong, long], [double, ulong], [long, ulong], [ulong, ulong]
  public static unsafe Vector<T> GatherVectorWithByteOffsets(Vector<T> mask, T* address, Vector<T2> offsets); // LD1W or LD1D


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

  /// total method signatures: 27

}


/// Full API
public abstract partial class Sve : AdvSimd /// Feature: FEAT_SVE  Category: gatherloads
{
    /// GatherPrefetch16Bit : Prefetch halfwords

    /// void svprfh_gather[_u32base](svbool_t pg, svuint32_t bases, enum svprfop op) : "PRFH op, Pg, [Zbases.S, #0]"
  public static unsafe void GatherPrefetch16Bit(Vector<ushort> mask, Vector<uint> addresses, [ConstantExpected] SvePrefetchType prefetchType);

    /// void svprfh_gather[_u32base](svbool_t pg, svuint32_t bases, enum svprfop op) : "PRFH op, Pg, [Zbases.S, #0]"
  public static unsafe void GatherPrefetch16Bit(Vector<short> mask, Vector<uint> addresses, [ConstantExpected] SvePrefetchType prefetchType);

    /// void svprfh_gather[_u64base](svbool_t pg, svuint64_t bases, enum svprfop op) : "PRFH op, Pg, [Zbases.D, #0]"
  public static unsafe void GatherPrefetch16Bit(Vector<ushort> mask, Vector<ulong> addresses, [ConstantExpected] SvePrefetchType prefetchType);

    /// void svprfh_gather[_u64base](svbool_t pg, svuint64_t bases, enum svprfop op) : "PRFH op, Pg, [Zbases.D, #0]"
  public static unsafe void GatherPrefetch16Bit(Vector<short> mask, Vector<ulong> addresses, [ConstantExpected] SvePrefetchType prefetchType);

    /// void svprfh_gather_[s32]index(svbool_t pg, const void *base, svint32_t indices, enum svprfop op) : "PRFH op, Pg, [Xbase, Zindices.S, SXTW #1]"
  public static unsafe void GatherPrefetch16Bit(Vector<ushort> mask, void* address, Vector<int> indices, [ConstantExpected] SvePrefetchType prefetchType);

    /// void svprfh_gather_[s32]index(svbool_t pg, const void *base, svint32_t indices, enum svprfop op) : "PRFH op, Pg, [Xbase, Zindices.S, SXTW #1]"
  public static unsafe void GatherPrefetch16Bit(Vector<short> mask, void* address, Vector<int> indices, [ConstantExpected] SvePrefetchType prefetchType);

    /// void svprfh_gather_[u32]index(svbool_t pg, const void *base, svuint32_t indices, enum svprfop op) : "PRFH op, Pg, [Xbase, Zindices.S, UXTW #1]"
  public static unsafe void GatherPrefetch16Bit(Vector<ushort> mask, void* address, Vector<uint> indices, [ConstantExpected] SvePrefetchType prefetchType);

    /// void svprfh_gather_[u32]index(svbool_t pg, const void *base, svuint32_t indices, enum svprfop op) : "PRFH op, Pg, [Xbase, Zindices.S, UXTW #1]"
  public static unsafe void GatherPrefetch16Bit(Vector<short> mask, void* address, Vector<uint> indices, [ConstantExpected] SvePrefetchType prefetchType);

    /// void svprfh_gather_[s64]index(svbool_t pg, const void *base, svint64_t indices, enum svprfop op) : "PRFH op, Pg, [Xbase, Zindices.D, LSL #1]"
  public static unsafe void GatherPrefetch16Bit(Vector<ushort> mask, void* address, Vector<long> indices, [ConstantExpected] SvePrefetchType prefetchType);

    /// void svprfh_gather_[s64]index(svbool_t pg, const void *base, svint64_t indices, enum svprfop op) : "PRFH op, Pg, [Xbase, Zindices.D, LSL #1]"
  public static unsafe void GatherPrefetch16Bit(Vector<short> mask, void* address, Vector<long> indices, [ConstantExpected] SvePrefetchType prefetchType);

    /// void svprfh_gather_[u64]index(svbool_t pg, const void *base, svuint64_t indices, enum svprfop op) : "PRFH op, Pg, [Xbase, Zindices.D, LSL #1]"
  public static unsafe void GatherPrefetch16Bit(Vector<ushort> mask, void* address, Vector<ulong> indices, [ConstantExpected] SvePrefetchType prefetchType);

    /// void svprfh_gather_[u64]index(svbool_t pg, const void *base, svuint64_t indices, enum svprfop op) : "PRFH op, Pg, [Xbase, Zindices.D, LSL #1]"
  public static unsafe void GatherPrefetch16Bit(Vector<short> mask, void* address, Vector<ulong> indices, [ConstantExpected] SvePrefetchType prefetchType);


    /// GatherPrefetch32Bit : Prefetch words

    /// void svprfw_gather[_u32base](svbool_t pg, svuint32_t bases, enum svprfop op) : "PRFW op, Pg, [Zbases.S, #0]"
  public static unsafe void GatherPrefetch32Bit(Vector<uint> mask, Vector<uint> addresses, [ConstantExpected] SvePrefetchType prefetchType);

    /// void svprfw_gather[_u32base](svbool_t pg, svuint32_t bases, enum svprfop op) : "PRFW op, Pg, [Zbases.S, #0]"
  public static unsafe void GatherPrefetch32Bit(Vector<int> mask, Vector<uint> addresses, [ConstantExpected] SvePrefetchType prefetchType);

    /// void svprfw_gather[_u64base](svbool_t pg, svuint64_t bases, enum svprfop op) : "PRFW op, Pg, [Zbases.D, #0]"
  public static unsafe void GatherPrefetch32Bit(Vector<uint> mask, Vector<ulong> addresses, [ConstantExpected] SvePrefetchType prefetchType);

    /// void svprfw_gather[_u64base](svbool_t pg, svuint64_t bases, enum svprfop op) : "PRFW op, Pg, [Zbases.D, #0]"
  public static unsafe void GatherPrefetch32Bit(Vector<int> mask, Vector<ulong> addresses, [ConstantExpected] SvePrefetchType prefetchType);

    /// void svprfw_gather_[s32]index(svbool_t pg, const void *base, svint32_t indices, enum svprfop op) : "PRFW op, Pg, [Xbase, Zindices.S, SXTW #2]"
  public static unsafe void GatherPrefetch32Bit(Vector<uint> mask, void* address, Vector<int> indices, [ConstantExpected] SvePrefetchType prefetchType);

    /// void svprfw_gather_[s32]index(svbool_t pg, const void *base, svint32_t indices, enum svprfop op) : "PRFW op, Pg, [Xbase, Zindices.S, SXTW #2]"
  public static unsafe void GatherPrefetch32Bit(Vector<int> mask, void* address, Vector<int> indices, [ConstantExpected] SvePrefetchType prefetchType);

    /// void svprfw_gather_[u32]index(svbool_t pg, const void *base, svuint32_t indices, enum svprfop op) : "PRFW op, Pg, [Xbase, Zindices.S, UXTW #2]"
  public static unsafe void GatherPrefetch32Bit(Vector<uint> mask, void* address, Vector<uint> indices, [ConstantExpected] SvePrefetchType prefetchType);

    /// void svprfw_gather_[u32]index(svbool_t pg, const void *base, svuint32_t indices, enum svprfop op) : "PRFW op, Pg, [Xbase, Zindices.S, UXTW #2]"
  public static unsafe void GatherPrefetch32Bit(Vector<int> mask, void* address, Vector<uint> indices, [ConstantExpected] SvePrefetchType prefetchType);

    /// void svprfw_gather_[s64]index(svbool_t pg, const void *base, svint64_t indices, enum svprfop op) : "PRFW op, Pg, [Xbase, Zindices.D, LSL #2]"
  public static unsafe void GatherPrefetch32Bit(Vector<uint> mask, void* address, Vector<long> indices, [ConstantExpected] SvePrefetchType prefetchType);

    /// void svprfw_gather_[s64]index(svbool_t pg, const void *base, svint64_t indices, enum svprfop op) : "PRFW op, Pg, [Xbase, Zindices.D, LSL #2]"
  public static unsafe void GatherPrefetch32Bit(Vector<int> mask, void* address, Vector<long> indices, [ConstantExpected] SvePrefetchType prefetchType);

    /// void svprfw_gather_[u64]index(svbool_t pg, const void *base, svuint64_t indices, enum svprfop op) : "PRFW op, Pg, [Xbase, Zindices.D, LSL #2]"
  public static unsafe void GatherPrefetch32Bit(Vector<uint> mask, void* address, Vector<ulong> indices, [ConstantExpected] SvePrefetchType prefetchType);

    /// void svprfw_gather_[u64]index(svbool_t pg, const void *base, svuint64_t indices, enum svprfop op) : "PRFW op, Pg, [Xbase, Zindices.D, LSL #2]"
  public static unsafe void GatherPrefetch32Bit(Vector<int> mask, void* address, Vector<ulong> indices, [ConstantExpected] SvePrefetchType prefetchType);


    /// GatherPrefetch64Bit : Prefetch doublewords

    /// void svprfd_gather[_u32base](svbool_t pg, svuint32_t bases, enum svprfop op) : "PRFD op, Pg, [Zbases.S, #0]"
  public static unsafe void GatherPrefetch64Bit(Vector<ulong> mask, Vector<uint> addresses, [ConstantExpected] SvePrefetchType prefetchType);

    /// void svprfd_gather[_u32base](svbool_t pg, svuint32_t bases, enum svprfop op) : "PRFD op, Pg, [Zbases.S, #0]"
  public static unsafe void GatherPrefetch64Bit(Vector<long> mask, Vector<uint> addresses, [ConstantExpected] SvePrefetchType prefetchType);

    /// void svprfd_gather[_u64base](svbool_t pg, svuint64_t bases, enum svprfop op) : "PRFD op, Pg, [Zbases.D, #0]"
  public static unsafe void GatherPrefetch64Bit(Vector<ulong> mask, Vector<ulong> addresses, [ConstantExpected] SvePrefetchType prefetchType);

    /// void svprfd_gather[_u64base](svbool_t pg, svuint64_t bases, enum svprfop op) : "PRFD op, Pg, [Zbases.D, #0]"
  public static unsafe void GatherPrefetch64Bit(Vector<long> mask, Vector<ulong> addresses, [ConstantExpected] SvePrefetchType prefetchType);

    /// void svprfd_gather_[s32]index(svbool_t pg, const void *base, svint32_t indices, enum svprfop op) : "PRFD op, Pg, [Xbase, Zindices.S, SXTW #3]"
  public static unsafe void GatherPrefetch64Bit(Vector<ulong> mask, void* address, Vector<int> indices, [ConstantExpected] SvePrefetchType prefetchType);

    /// void svprfd_gather_[s32]index(svbool_t pg, const void *base, svint32_t indices, enum svprfop op) : "PRFD op, Pg, [Xbase, Zindices.S, SXTW #3]"
  public static unsafe void GatherPrefetch64Bit(Vector<long> mask, void* address, Vector<int> indices, [ConstantExpected] SvePrefetchType prefetchType);

    /// void svprfd_gather_[u32]index(svbool_t pg, const void *base, svuint32_t indices, enum svprfop op) : "PRFD op, Pg, [Xbase, Zindices.S, UXTW #3]"
  public static unsafe void GatherPrefetch64Bit(Vector<ulong> mask, void* address, Vector<uint> indices, [ConstantExpected] SvePrefetchType prefetchType);

    /// void svprfd_gather_[u32]index(svbool_t pg, const void *base, svuint32_t indices, enum svprfop op) : "PRFD op, Pg, [Xbase, Zindices.S, UXTW #3]"
  public static unsafe void GatherPrefetch64Bit(Vector<long> mask, void* address, Vector<uint> indices, [ConstantExpected] SvePrefetchType prefetchType);

    /// void svprfd_gather_[s64]index(svbool_t pg, const void *base, svint64_t indices, enum svprfop op) : "PRFD op, Pg, [Xbase, Zindices.D, LSL #3]"
  public static unsafe void GatherPrefetch64Bit(Vector<ulong> mask, void* address, Vector<long> indices, [ConstantExpected] SvePrefetchType prefetchType);

    /// void svprfd_gather_[s64]index(svbool_t pg, const void *base, svint64_t indices, enum svprfop op) : "PRFD op, Pg, [Xbase, Zindices.D, LSL #3]"
  public static unsafe void GatherPrefetch64Bit(Vector<long> mask, void* address, Vector<long> indices, [ConstantExpected] SvePrefetchType prefetchType);

    /// void svprfd_gather_[u64]index(svbool_t pg, const void *base, svuint64_t indices, enum svprfop op) : "PRFD op, Pg, [Xbase, Zindices.D, LSL #3]"
  public static unsafe void GatherPrefetch64Bit(Vector<ulong> mask, void* address, Vector<ulong> indices, [ConstantExpected] SvePrefetchType prefetchType);

    /// void svprfd_gather_[u64]index(svbool_t pg, const void *base, svuint64_t indices, enum svprfop op) : "PRFD op, Pg, [Xbase, Zindices.D, LSL #3]"
  public static unsafe void GatherPrefetch64Bit(Vector<long> mask, void* address, Vector<ulong> indices, [ConstantExpected] SvePrefetchType prefetchType);


    /// GatherPrefetch8Bit : Prefetch bytes

    /// void svprfb_gather[_u32base](svbool_t pg, svuint32_t bases, enum svprfop op) : "PRFB op, Pg, [Zbases.S, #0]"
  public static unsafe void GatherPrefetch8Bit(Vector<byte> mask, Vector<uint> addresses, [ConstantExpected] SvePrefetchType prefetchType);

    /// void svprfb_gather[_u32base](svbool_t pg, svuint32_t bases, enum svprfop op) : "PRFB op, Pg, [Zbases.S, #0]"
  public static unsafe void GatherPrefetch8Bit(Vector<sbyte> mask, Vector<uint> addresses, [ConstantExpected] SvePrefetchType prefetchType);

    /// void svprfb_gather[_u64base](svbool_t pg, svuint64_t bases, enum svprfop op) : "PRFB op, Pg, [Zbases.D, #0]"
  public static unsafe void GatherPrefetch8Bit(Vector<byte> mask, Vector<ulong> addresses, [ConstantExpected] SvePrefetchType prefetchType);

    /// void svprfb_gather[_u64base](svbool_t pg, svuint64_t bases, enum svprfop op) : "PRFB op, Pg, [Zbases.D, #0]"
  public static unsafe void GatherPrefetch8Bit(Vector<sbyte> mask, Vector<ulong> addresses, [ConstantExpected] SvePrefetchType prefetchType);

    /// void svprfb_gather_[s32]offset(svbool_t pg, const void *base, svint32_t offsets, enum svprfop op) : "PRFB op, Pg, [Xbase, Zoffsets.S, SXTW]"
  public static unsafe void GatherPrefetch8Bit(Vector<byte> mask, void* address, Vector<int> offsets, [ConstantExpected] SvePrefetchType prefetchType);

    /// void svprfb_gather_[s32]offset(svbool_t pg, const void *base, svint32_t offsets, enum svprfop op) : "PRFB op, Pg, [Xbase, Zoffsets.S, SXTW]"
  public static unsafe void GatherPrefetch8Bit(Vector<sbyte> mask, void* address, Vector<int> offsets, [ConstantExpected] SvePrefetchType prefetchType);

    /// void svprfb_gather_[u32]offset(svbool_t pg, const void *base, svuint32_t offsets, enum svprfop op) : "PRFB op, Pg, [Xbase, Zoffsets.S, UXTW]"
  public static unsafe void GatherPrefetch8Bit(Vector<byte> mask, void* address, Vector<uint> offsets, [ConstantExpected] SvePrefetchType prefetchType);

    /// void svprfb_gather_[u32]offset(svbool_t pg, const void *base, svuint32_t offsets, enum svprfop op) : "PRFB op, Pg, [Xbase, Zoffsets.S, UXTW]"
  public static unsafe void GatherPrefetch8Bit(Vector<sbyte> mask, void* address, Vector<uint> offsets, [ConstantExpected] SvePrefetchType prefetchType);

    /// void svprfb_gather_[s64]offset(svbool_t pg, const void *base, svint64_t offsets, enum svprfop op) : "PRFB op, Pg, [Xbase, Zoffsets.D]"
  public static unsafe void GatherPrefetch8Bit(Vector<byte> mask, void* address, Vector<long> offsets, [ConstantExpected] SvePrefetchType prefetchType);

    /// void svprfb_gather_[s64]offset(svbool_t pg, const void *base, svint64_t offsets, enum svprfop op) : "PRFB op, Pg, [Xbase, Zoffsets.D]"
  public static unsafe void GatherPrefetch8Bit(Vector<sbyte> mask, void* address, Vector<long> offsets, [ConstantExpected] SvePrefetchType prefetchType);

    /// void svprfb_gather_[u64]offset(svbool_t pg, const void *base, svuint64_t offsets, enum svprfop op) : "PRFB op, Pg, [Xbase, Zoffsets.D]"
  public static unsafe void GatherPrefetch8Bit(Vector<byte> mask, void* address, Vector<ulong> offsets, [ConstantExpected] SvePrefetchType prefetchType);

    /// void svprfb_gather_[u64]offset(svbool_t pg, const void *base, svuint64_t offsets, enum svprfop op) : "PRFB op, Pg, [Xbase, Zoffsets.D]"
  public static unsafe void GatherPrefetch8Bit(Vector<sbyte> mask, void* address, Vector<ulong> offsets, [ConstantExpected] SvePrefetchType prefetchType);


    /// GatherVector : Unextended load

    /// svfloat32_t svld1_gather[_u32base]_f32(svbool_t pg, svuint32_t bases) : "LD1W Zresult.S, Pg/Z, [Zbases.S, #0]"
  public static unsafe Vector<float> GatherVector(Vector<float> mask, Vector<uint> addresses);

    /// svint32_t svld1_gather[_u32base]_s32(svbool_t pg, svuint32_t bases) : "LD1W Zresult.S, Pg/Z, [Zbases.S, #0]"
  public static unsafe Vector<int> GatherVector(Vector<int> mask, Vector<uint> addresses);

    /// svuint32_t svld1_gather[_u32base]_u32(svbool_t pg, svuint32_t bases) : "LD1W Zresult.S, Pg/Z, [Zbases.S, #0]"
  public static unsafe Vector<uint> GatherVector(Vector<uint> mask, Vector<uint> addresses);

    /// svfloat64_t svld1_gather[_u64base]_f64(svbool_t pg, svuint64_t bases) : "LD1D Zresult.D, Pg/Z, [Zbases.D, #0]"
  public static unsafe Vector<double> GatherVector(Vector<double> mask, Vector<ulong> addresses);

    /// svint64_t svld1_gather[_u64base]_s64(svbool_t pg, svuint64_t bases) : "LD1D Zresult.D, Pg/Z, [Zbases.D, #0]"
  public static unsafe Vector<long> GatherVector(Vector<long> mask, Vector<ulong> addresses);

    /// svuint64_t svld1_gather[_u64base]_u64(svbool_t pg, svuint64_t bases) : "LD1D Zresult.D, Pg/Z, [Zbases.D, #0]"
  public static unsafe Vector<ulong> GatherVector(Vector<ulong> mask, Vector<ulong> addresses);

    /// svfloat32_t svld1_gather_[s32]index[_f32](svbool_t pg, const float32_t *base, svint32_t indices) : "LD1W Zresult.S, Pg/Z, [Xbase, Zindices.S, SXTW #2]"
  public static unsafe Vector<float> GatherVector(Vector<float> mask, float* address, Vector<int> indices);

    /// svint32_t svld1_gather_[s32]index[_s32](svbool_t pg, const int32_t *base, svint32_t indices) : "LD1W Zresult.S, Pg/Z, [Xbase, Zindices.S, SXTW #2]"
  public static unsafe Vector<int> GatherVector(Vector<int> mask, int* address, Vector<int> indices);

    /// svuint32_t svld1_gather_[s32]index[_u32](svbool_t pg, const uint32_t *base, svint32_t indices) : "LD1W Zresult.S, Pg/Z, [Xbase, Zindices.S, SXTW #2]"
  public static unsafe Vector<uint> GatherVector(Vector<uint> mask, uint* address, Vector<int> indices);

    /// svfloat32_t svld1_gather_[u32]index[_f32](svbool_t pg, const float32_t *base, svuint32_t indices) : "LD1W Zresult.S, Pg/Z, [Xbase, Zindices.S, UXTW #2]"
  public static unsafe Vector<float> GatherVector(Vector<float> mask, float* address, Vector<uint> indices);

    /// svint32_t svld1_gather_[u32]index[_s32](svbool_t pg, const int32_t *base, svuint32_t indices) : "LD1W Zresult.S, Pg/Z, [Xbase, Zindices.S, UXTW #2]"
  public static unsafe Vector<int> GatherVector(Vector<int> mask, int* address, Vector<uint> indices);

    /// svuint32_t svld1_gather_[u32]index[_u32](svbool_t pg, const uint32_t *base, svuint32_t indices) : "LD1W Zresult.S, Pg/Z, [Xbase, Zindices.S, UXTW #2]"
  public static unsafe Vector<uint> GatherVector(Vector<uint> mask, uint* address, Vector<uint> indices);

    /// svfloat64_t svld1_gather_[s64]index[_f64](svbool_t pg, const float64_t *base, svint64_t indices) : "LD1D Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #3]"
  public static unsafe Vector<double> GatherVector(Vector<double> mask, double* address, Vector<long> indices);

    /// svint64_t svld1_gather_[s64]index[_s64](svbool_t pg, const int64_t *base, svint64_t indices) : "LD1D Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #3]"
  public static unsafe Vector<long> GatherVector(Vector<long> mask, long* address, Vector<long> indices);

    /// svuint64_t svld1_gather_[s64]index[_u64](svbool_t pg, const uint64_t *base, svint64_t indices) : "LD1D Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #3]"
  public static unsafe Vector<ulong> GatherVector(Vector<ulong> mask, ulong* address, Vector<long> indices);

    /// svfloat64_t svld1_gather_[u64]index[_f64](svbool_t pg, const float64_t *base, svuint64_t indices) : "LD1D Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #3]"
  public static unsafe Vector<double> GatherVector(Vector<double> mask, double* address, Vector<ulong> indices);

    /// svint64_t svld1_gather_[u64]index[_s64](svbool_t pg, const int64_t *base, svuint64_t indices) : "LD1D Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #3]"
  public static unsafe Vector<long> GatherVector(Vector<long> mask, long* address, Vector<ulong> indices);

    /// svuint64_t svld1_gather_[u64]index[_u64](svbool_t pg, const uint64_t *base, svuint64_t indices) : "LD1D Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #3]"
  public static unsafe Vector<ulong> GatherVector(Vector<ulong> mask, ulong* address, Vector<ulong> indices);


    /// GatherVectorByteZeroExtend : Load 8-bit data and zero-extend

    /// svint32_t svld1ub_gather[_u32base]_s32(svbool_t pg, svuint32_t bases) : "LD1B Zresult.S, Pg/Z, [Zbases.S, #0]"
  public static unsafe Vector<int> GatherVectorByteZeroExtend(Vector<int> mask, Vector<uint> addresses);

    /// svuint32_t svld1ub_gather[_u32base]_u32(svbool_t pg, svuint32_t bases) : "LD1B Zresult.S, Pg/Z, [Zbases.S, #0]"
  public static unsafe Vector<uint> GatherVectorByteZeroExtend(Vector<uint> mask, Vector<uint> addresses);

    /// svint64_t svld1ub_gather[_u64base]_s64(svbool_t pg, svuint64_t bases) : "LD1B Zresult.D, Pg/Z, [Zbases.D, #0]"
  public static unsafe Vector<long> GatherVectorByteZeroExtend(Vector<long> mask, Vector<ulong> addresses);

    /// svuint64_t svld1ub_gather[_u64base]_u64(svbool_t pg, svuint64_t bases) : "LD1B Zresult.D, Pg/Z, [Zbases.D, #0]"
  public static unsafe Vector<ulong> GatherVectorByteZeroExtend(Vector<ulong> mask, Vector<ulong> addresses);

    /// svint32_t svld1ub_gather_[s32]offset_s32(svbool_t pg, const uint8_t *base, svint32_t offsets) : "LD1B Zresult.S, Pg/Z, [Xbase, Zoffsets.S, SXTW]"
  public static unsafe Vector<int> GatherVectorByteZeroExtend(Vector<int> mask, byte* address, Vector<int> indices);

    /// svuint32_t svld1ub_gather_[s32]offset_u32(svbool_t pg, const uint8_t *base, svint32_t offsets) : "LD1B Zresult.S, Pg/Z, [Xbase, Zoffsets.S, SXTW]"
  public static unsafe Vector<uint> GatherVectorByteZeroExtend(Vector<uint> mask, byte* address, Vector<int> indices);

    /// svint32_t svld1ub_gather_[u32]offset_s32(svbool_t pg, const uint8_t *base, svuint32_t offsets) : "LD1B Zresult.S, Pg/Z, [Xbase, Zoffsets.S, UXTW]"
  public static unsafe Vector<int> GatherVectorByteZeroExtend(Vector<int> mask, byte* address, Vector<uint> indices);

    /// svuint32_t svld1ub_gather_[u32]offset_u32(svbool_t pg, const uint8_t *base, svuint32_t offsets) : "LD1B Zresult.S, Pg/Z, [Xbase, Zoffsets.S, UXTW]"
  public static unsafe Vector<uint> GatherVectorByteZeroExtend(Vector<uint> mask, byte* address, Vector<uint> indices);

    /// svint64_t svld1ub_gather_[s64]offset_s64(svbool_t pg, const uint8_t *base, svint64_t offsets) : "LD1B Zresult.D, Pg/Z, [Xbase, Zoffsets.D]"
  public static unsafe Vector<long> GatherVectorByteZeroExtend(Vector<long> mask, byte* address, Vector<long> indices);

    /// svuint64_t svld1ub_gather_[s64]offset_u64(svbool_t pg, const uint8_t *base, svint64_t offsets) : "LD1B Zresult.D, Pg/Z, [Xbase, Zoffsets.D]"
  public static unsafe Vector<ulong> GatherVectorByteZeroExtend(Vector<ulong> mask, byte* address, Vector<long> indices);

    /// svint64_t svld1ub_gather_[u64]offset_s64(svbool_t pg, const uint8_t *base, svuint64_t offsets) : "LD1B Zresult.D, Pg/Z, [Xbase, Zoffsets.D]"
  public static unsafe Vector<long> GatherVectorByteZeroExtend(Vector<long> mask, byte* address, Vector<ulong> indices);

    /// svuint64_t svld1ub_gather_[u64]offset_u64(svbool_t pg, const uint8_t *base, svuint64_t offsets) : "LD1B Zresult.D, Pg/Z, [Xbase, Zoffsets.D]"
  public static unsafe Vector<ulong> GatherVectorByteZeroExtend(Vector<ulong> mask, byte* address, Vector<ulong> indices);


    /// GatherVectorInt16SignExtend : Load 16-bit data and sign-extend

    /// svint32_t svld1sh_gather[_u32base]_s32(svbool_t pg, svuint32_t bases) : "LD1SH Zresult.S, Pg/Z, [Zbases.S, #0]"
  public static unsafe Vector<int> GatherVectorInt16SignExtend(Vector<int> mask, Vector<uint> addresses);

    /// svuint32_t svld1sh_gather[_u32base]_u32(svbool_t pg, svuint32_t bases) : "LD1SH Zresult.S, Pg/Z, [Zbases.S, #0]"
  public static unsafe Vector<uint> GatherVectorInt16SignExtend(Vector<uint> mask, Vector<uint> addresses);

    /// svint64_t svld1sh_gather[_u64base]_s64(svbool_t pg, svuint64_t bases) : "LD1SH Zresult.D, Pg/Z, [Zbases.D, #0]"
  public static unsafe Vector<long> GatherVectorInt16SignExtend(Vector<long> mask, Vector<ulong> addresses);

    /// svuint64_t svld1sh_gather[_u64base]_u64(svbool_t pg, svuint64_t bases) : "LD1SH Zresult.D, Pg/Z, [Zbases.D, #0]"
  public static unsafe Vector<ulong> GatherVectorInt16SignExtend(Vector<ulong> mask, Vector<ulong> addresses);

    /// svint32_t svld1sh_gather_[s32]index_s32(svbool_t pg, const int16_t *base, svint32_t indices) : "LD1SH Zresult.S, Pg/Z, [Xbase, Zindices.S, SXTW #1]"
  public static unsafe Vector<int> GatherVectorInt16SignExtend(Vector<int> mask, short* address, Vector<int> indices);

    /// svuint32_t svld1sh_gather_[s32]index_u32(svbool_t pg, const int16_t *base, svint32_t indices) : "LD1SH Zresult.S, Pg/Z, [Xbase, Zindices.S, SXTW #1]"
  public static unsafe Vector<uint> GatherVectorInt16SignExtend(Vector<uint> mask, short* address, Vector<int> indices);

    /// svint32_t svld1sh_gather_[u32]index_s32(svbool_t pg, const int16_t *base, svuint32_t indices) : "LD1SH Zresult.S, Pg/Z, [Xbase, Zindices.S, UXTW #1]"
  public static unsafe Vector<int> GatherVectorInt16SignExtend(Vector<int> mask, short* address, Vector<uint> indices);

    /// svuint32_t svld1sh_gather_[u32]index_u32(svbool_t pg, const int16_t *base, svuint32_t indices) : "LD1SH Zresult.S, Pg/Z, [Xbase, Zindices.S, UXTW #1]"
  public static unsafe Vector<uint> GatherVectorInt16SignExtend(Vector<uint> mask, short* address, Vector<uint> indices);

    /// svint64_t svld1sh_gather_[s64]index_s64(svbool_t pg, const int16_t *base, svint64_t indices) : "LD1SH Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #1]"
  public static unsafe Vector<long> GatherVectorInt16SignExtend(Vector<long> mask, short* address, Vector<long> indices);

    /// svuint64_t svld1sh_gather_[s64]index_u64(svbool_t pg, const int16_t *base, svint64_t indices) : "LD1SH Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #1]"
  public static unsafe Vector<ulong> GatherVectorInt16SignExtend(Vector<ulong> mask, short* address, Vector<long> indices);

    /// svint64_t svld1sh_gather_[u64]index_s64(svbool_t pg, const int16_t *base, svuint64_t indices) : "LD1SH Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #1]"
  public static unsafe Vector<long> GatherVectorInt16SignExtend(Vector<long> mask, short* address, Vector<ulong> indices);

    /// svuint64_t svld1sh_gather_[u64]index_u64(svbool_t pg, const int16_t *base, svuint64_t indices) : "LD1SH Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #1]"
  public static unsafe Vector<ulong> GatherVectorInt16SignExtend(Vector<ulong> mask, short* address, Vector<ulong> indices);


    /// GatherVectorInt16WithByteOffsetsSignExtend : Load 16-bit data and sign-extend

    /// svint32_t svld1sh_gather_[s32]offset_s32(svbool_t pg, const int16_t *base, svint32_t offsets) : "LD1SH Zresult.S, Pg/Z, [Xbase, Zoffsets.S, SXTW]"
  public static unsafe Vector<int> GatherVectorInt16WithByteOffsetsSignExtend(Vector<int> mask, short* address, Vector<int> offsets);

    /// svuint32_t svld1sh_gather_[s32]offset_u32(svbool_t pg, const int16_t *base, svint32_t offsets) : "LD1SH Zresult.S, Pg/Z, [Xbase, Zoffsets.S, SXTW]"
  public static unsafe Vector<uint> GatherVectorInt16WithByteOffsetsSignExtend(Vector<uint> mask, short* address, Vector<int> offsets);

    /// svint32_t svld1sh_gather_[u32]offset_s32(svbool_t pg, const int16_t *base, svuint32_t offsets) : "LD1SH Zresult.S, Pg/Z, [Xbase, Zoffsets.S, UXTW]"
  public static unsafe Vector<int> GatherVectorInt16WithByteOffsetsSignExtend(Vector<int> mask, short* address, Vector<uint> offsets);

    /// svuint32_t svld1sh_gather_[u32]offset_u32(svbool_t pg, const int16_t *base, svuint32_t offsets) : "LD1SH Zresult.S, Pg/Z, [Xbase, Zoffsets.S, UXTW]"
  public static unsafe Vector<uint> GatherVectorInt16WithByteOffsetsSignExtend(Vector<uint> mask, short* address, Vector<uint> offsets);

    /// svint64_t svld1sh_gather_[s64]offset_s64(svbool_t pg, const int16_t *base, svint64_t offsets) : "LD1SH Zresult.D, Pg/Z, [Xbase, Zoffsets.D]"
  public static unsafe Vector<long> GatherVectorInt16WithByteOffsetsSignExtend(Vector<long> mask, short* address, Vector<long> offsets);

    /// svuint64_t svld1sh_gather_[s64]offset_u64(svbool_t pg, const int16_t *base, svint64_t offsets) : "LD1SH Zresult.D, Pg/Z, [Xbase, Zoffsets.D]"
  public static unsafe Vector<ulong> GatherVectorInt16WithByteOffsetsSignExtend(Vector<ulong> mask, short* address, Vector<long> offsets);

    /// svint64_t svld1sh_gather_[u64]offset_s64(svbool_t pg, const int16_t *base, svuint64_t offsets) : "LD1SH Zresult.D, Pg/Z, [Xbase, Zoffsets.D]"
  public static unsafe Vector<long> GatherVectorInt16WithByteOffsetsSignExtend(Vector<long> mask, short* address, Vector<ulong> offsets);

    /// svuint64_t svld1sh_gather_[u64]offset_u64(svbool_t pg, const int16_t *base, svuint64_t offsets) : "LD1SH Zresult.D, Pg/Z, [Xbase, Zoffsets.D]"
  public static unsafe Vector<ulong> GatherVectorInt16WithByteOffsetsSignExtend(Vector<ulong> mask, short* address, Vector<ulong> offsets);


    /// GatherVectorInt32SignExtend : Load 32-bit data and sign-extend

    /// svint64_t svld1sw_gather[_u64base]_s64(svbool_t pg, svuint64_t bases) : "LD1SW Zresult.D, Pg/Z, [Zbases.D, #0]"
  public static unsafe Vector<long> GatherVectorInt32SignExtend(Vector<long> mask, Vector<ulong> addresses);

    /// svint64_t svld1sw_gather[_u64base]_s64(svbool_t pg, svuint64_t bases) : "LD1SW Zresult.D, Pg/Z, [Zbases.D, #0]"
  public static unsafe Vector<int> GatherVectorInt32SignExtend(Vector<int> mask, Vector<uint> addresses);

    /// svuint64_t svld1sw_gather[_u64base]_u64(svbool_t pg, svuint64_t bases) : "LD1SW Zresult.D, Pg/Z, [Zbases.D, #0]"
  public static unsafe Vector<ulong> GatherVectorInt32SignExtend(Vector<ulong> mask, Vector<ulong> addresses);

    /// svuint64_t svld1sw_gather[_u64base]_u64(svbool_t pg, svuint64_t bases) : "LD1SW Zresult.D, Pg/Z, [Zbases.D, #0]"
  public static unsafe Vector<uint> GatherVectorInt32SignExtend(Vector<uint> mask, Vector<uint> addresses);

    /// svint64_t svld1sw_gather_[s64]index_s64(svbool_t pg, const int32_t *base, svint64_t indices) : "LD1SW Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #2]"
  public static unsafe Vector<long> GatherVectorInt32SignExtend(Vector<long> mask, int* address, Vector<long> indices);

    /// svint64_t svld1sw_gather_[s64]index_s64(svbool_t pg, const int32_t *base, svint64_t indices) : "LD1SW Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #2]"
  public static unsafe Vector<int> GatherVectorInt32SignExtend(Vector<int> mask, int* address, Vector<int> indices);

    /// svuint64_t svld1sw_gather_[s64]index_u64(svbool_t pg, const int32_t *base, svint64_t indices) : "LD1SW Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #2]"
  public static unsafe Vector<ulong> GatherVectorInt32SignExtend(Vector<ulong> mask, int* address, Vector<long> indices);

    /// svuint64_t svld1sw_gather_[s64]index_u64(svbool_t pg, const int32_t *base, svint64_t indices) : "LD1SW Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #2]"
  public static unsafe Vector<uint> GatherVectorInt32SignExtend(Vector<uint> mask, int* address, Vector<int> indices);

    /// svint64_t svld1sw_gather_[u64]index_s64(svbool_t pg, const int32_t *base, svuint64_t indices) : "LD1SW Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #2]"
  public static unsafe Vector<long> GatherVectorInt32SignExtend(Vector<long> mask, int* address, Vector<ulong> indices);

    /// svint64_t svld1sw_gather_[u64]index_s64(svbool_t pg, const int32_t *base, svuint64_t indices) : "LD1SW Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #2]"
  public static unsafe Vector<int> GatherVectorInt32SignExtend(Vector<int> mask, int* address, Vector<uint> indices);

    /// svuint64_t svld1sw_gather_[u64]index_u64(svbool_t pg, const int32_t *base, svuint64_t indices) : "LD1SW Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #2]"
  public static unsafe Vector<ulong> GatherVectorInt32SignExtend(Vector<ulong> mask, int* address, Vector<ulong> indices);

    /// svuint64_t svld1sw_gather_[u64]index_u64(svbool_t pg, const int32_t *base, svuint64_t indices) : "LD1SW Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #2]"
  public static unsafe Vector<uint> GatherVectorInt32SignExtend(Vector<uint> mask, int* address, Vector<uint> indices);


    /// GatherVectorInt32WithByteOffsetsSignExtend : Load 32-bit data and sign-extend

    /// svint64_t svld1sw_gather_[s64]offset_s64(svbool_t pg, const int32_t *base, svint64_t offsets) : "LD1SW Zresult.D, Pg/Z, [Xbase, Zoffsets.D]"
  public static unsafe Vector<long> GatherVectorInt32WithByteOffsetsSignExtend(Vector<long> mask, int* address, Vector<long> offsets);

    /// svint64_t svld1sw_gather_[s64]offset_s64(svbool_t pg, const int32_t *base, svint64_t offsets) : "LD1SW Zresult.D, Pg/Z, [Xbase, Zoffsets.D]"
  public static unsafe Vector<int> GatherVectorInt32WithByteOffsetsSignExtend(Vector<int> mask, int* address, Vector<int> offsets);

    /// svuint64_t svld1sw_gather_[s64]offset_u64(svbool_t pg, const int32_t *base, svint64_t offsets) : "LD1SW Zresult.D, Pg/Z, [Xbase, Zoffsets.D]"
  public static unsafe Vector<ulong> GatherVectorInt32WithByteOffsetsSignExtend(Vector<ulong> mask, int* address, Vector<long> offsets);

    /// svuint64_t svld1sw_gather_[s64]offset_u64(svbool_t pg, const int32_t *base, svint64_t offsets) : "LD1SW Zresult.D, Pg/Z, [Xbase, Zoffsets.D]"
  public static unsafe Vector<uint> GatherVectorInt32WithByteOffsetsSignExtend(Vector<uint> mask, int* address, Vector<int> offsets);

    /// svint64_t svld1sw_gather_[u64]offset_s64(svbool_t pg, const int32_t *base, svuint64_t offsets) : "LD1SW Zresult.D, Pg/Z, [Xbase, Zoffsets.D]"
  public static unsafe Vector<long> GatherVectorInt32WithByteOffsetsSignExtend(Vector<long> mask, int* address, Vector<ulong> offsets);

    /// svint64_t svld1sw_gather_[u64]offset_s64(svbool_t pg, const int32_t *base, svuint64_t offsets) : "LD1SW Zresult.D, Pg/Z, [Xbase, Zoffsets.D]"
  public static unsafe Vector<int> GatherVectorInt32WithByteOffsetsSignExtend(Vector<int> mask, int* address, Vector<uint> offsets);

    /// svuint64_t svld1sw_gather_[u64]offset_u64(svbool_t pg, const int32_t *base, svuint64_t offsets) : "LD1SW Zresult.D, Pg/Z, [Xbase, Zoffsets.D]"
  public static unsafe Vector<ulong> GatherVectorInt32WithByteOffsetsSignExtend(Vector<ulong> mask, int* address, Vector<ulong> offsets);

    /// svuint64_t svld1sw_gather_[u64]offset_u64(svbool_t pg, const int32_t *base, svuint64_t offsets) : "LD1SW Zresult.D, Pg/Z, [Xbase, Zoffsets.D]"
  public static unsafe Vector<uint> GatherVectorInt32WithByteOffsetsSignExtend(Vector<uint> mask, int* address, Vector<uint> offsets);


    /// GatherVectorSByteSignExtend : Load 8-bit data and sign-extend

    /// svint32_t svld1sb_gather[_u32base]_s32(svbool_t pg, svuint32_t bases) : "LD1SB Zresult.S, Pg/Z, [Zbases.S, #0]"
  public static unsafe Vector<int> GatherVectorSByteSignExtend(Vector<int> mask, Vector<uint> addresses);

    /// svuint32_t svld1sb_gather[_u32base]_u32(svbool_t pg, svuint32_t bases) : "LD1SB Zresult.S, Pg/Z, [Zbases.S, #0]"
  public static unsafe Vector<uint> GatherVectorSByteSignExtend(Vector<uint> mask, Vector<uint> addresses);

    /// svint64_t svld1sb_gather[_u64base]_s64(svbool_t pg, svuint64_t bases) : "LD1SB Zresult.D, Pg/Z, [Zbases.D, #0]"
  public static unsafe Vector<long> GatherVectorSByteSignExtend(Vector<long> mask, Vector<ulong> addresses);

    /// svuint64_t svld1sb_gather[_u64base]_u64(svbool_t pg, svuint64_t bases) : "LD1SB Zresult.D, Pg/Z, [Zbases.D, #0]"
  public static unsafe Vector<ulong> GatherVectorSByteSignExtend(Vector<ulong> mask, Vector<ulong> addresses);

    /// svint32_t svld1sb_gather_[s32]offset_s32(svbool_t pg, const int8_t *base, svint32_t offsets) : "LD1SB Zresult.S, Pg/Z, [Xbase, Zoffsets.S, SXTW]"
  public static unsafe Vector<int> GatherVectorSByteSignExtend(Vector<int> mask, sbyte* address, Vector<int> indices);

    /// svuint32_t svld1sb_gather_[s32]offset_u32(svbool_t pg, const int8_t *base, svint32_t offsets) : "LD1SB Zresult.S, Pg/Z, [Xbase, Zoffsets.S, SXTW]"
  public static unsafe Vector<uint> GatherVectorSByteSignExtend(Vector<uint> mask, sbyte* address, Vector<int> indices);

    /// svint32_t svld1sb_gather_[u32]offset_s32(svbool_t pg, const int8_t *base, svuint32_t offsets) : "LD1SB Zresult.S, Pg/Z, [Xbase, Zoffsets.S, UXTW]"
  public static unsafe Vector<int> GatherVectorSByteSignExtend(Vector<int> mask, sbyte* address, Vector<uint> indices);

    /// svuint32_t svld1sb_gather_[u32]offset_u32(svbool_t pg, const int8_t *base, svuint32_t offsets) : "LD1SB Zresult.S, Pg/Z, [Xbase, Zoffsets.S, UXTW]"
  public static unsafe Vector<uint> GatherVectorSByteSignExtend(Vector<uint> mask, sbyte* address, Vector<uint> indices);

    /// svint64_t svld1sb_gather_[s64]offset_s64(svbool_t pg, const int8_t *base, svint64_t offsets) : "LD1SB Zresult.D, Pg/Z, [Xbase, Zoffsets.D]"
  public static unsafe Vector<long> GatherVectorSByteSignExtend(Vector<long> mask, sbyte* address, Vector<long> indices);

    /// svuint64_t svld1sb_gather_[s64]offset_u64(svbool_t pg, const int8_t *base, svint64_t offsets) : "LD1SB Zresult.D, Pg/Z, [Xbase, Zoffsets.D]"
  public static unsafe Vector<ulong> GatherVectorSByteSignExtend(Vector<ulong> mask, sbyte* address, Vector<long> indices);

    /// svint64_t svld1sb_gather_[u64]offset_s64(svbool_t pg, const int8_t *base, svuint64_t offsets) : "LD1SB Zresult.D, Pg/Z, [Xbase, Zoffsets.D]"
  public static unsafe Vector<long> GatherVectorSByteSignExtend(Vector<long> mask, sbyte* address, Vector<ulong> indices);

    /// svuint64_t svld1sb_gather_[u64]offset_u64(svbool_t pg, const int8_t *base, svuint64_t offsets) : "LD1SB Zresult.D, Pg/Z, [Xbase, Zoffsets.D]"
  public static unsafe Vector<ulong> GatherVectorSByteSignExtend(Vector<ulong> mask, sbyte* address, Vector<ulong> indices);


    /// GatherVectorUInt16WithByteOffsetsZeroExtend : Load 16-bit data and zero-extend

    /// svint32_t svld1uh_gather_[s32]offset_s32(svbool_t pg, const uint16_t *base, svint32_t offsets) : "LD1H Zresult.S, Pg/Z, [Xbase, Zoffsets.S, SXTW]"
  public static unsafe Vector<int> GatherVectorUInt16WithByteOffsetsZeroExtend(Vector<int> mask, ushort* address, Vector<int> offsets);

    /// svuint32_t svld1uh_gather_[s32]offset_u32(svbool_t pg, const uint16_t *base, svint32_t offsets) : "LD1H Zresult.S, Pg/Z, [Xbase, Zoffsets.S, SXTW]"
  public static unsafe Vector<uint> GatherVectorUInt16WithByteOffsetsZeroExtend(Vector<uint> mask, ushort* address, Vector<int> offsets);

    /// svint32_t svld1uh_gather_[u32]offset_s32(svbool_t pg, const uint16_t *base, svuint32_t offsets) : "LD1H Zresult.S, Pg/Z, [Xbase, Zoffsets.S, UXTW]"
  public static unsafe Vector<int> GatherVectorUInt16WithByteOffsetsZeroExtend(Vector<int> mask, ushort* address, Vector<uint> offsets);

    /// svuint32_t svld1uh_gather_[u32]offset_u32(svbool_t pg, const uint16_t *base, svuint32_t offsets) : "LD1H Zresult.S, Pg/Z, [Xbase, Zoffsets.S, UXTW]"
  public static unsafe Vector<uint> GatherVectorUInt16WithByteOffsetsZeroExtend(Vector<uint> mask, ushort* address, Vector<uint> offsets);

    /// svint64_t svld1uh_gather_[s64]offset_s64(svbool_t pg, const uint16_t *base, svint64_t offsets) : "LD1H Zresult.D, Pg/Z, [Xbase, Zoffsets.D]"
  public static unsafe Vector<long> GatherVectorUInt16WithByteOffsetsZeroExtend(Vector<long> mask, ushort* address, Vector<long> offsets);

    /// svuint64_t svld1uh_gather_[s64]offset_u64(svbool_t pg, const uint16_t *base, svint64_t offsets) : "LD1H Zresult.D, Pg/Z, [Xbase, Zoffsets.D]"
  public static unsafe Vector<ulong> GatherVectorUInt16WithByteOffsetsZeroExtend(Vector<ulong> mask, ushort* address, Vector<long> offsets);

    /// svint64_t svld1uh_gather_[u64]offset_s64(svbool_t pg, const uint16_t *base, svuint64_t offsets) : "LD1H Zresult.D, Pg/Z, [Xbase, Zoffsets.D]"
  public static unsafe Vector<long> GatherVectorUInt16WithByteOffsetsZeroExtend(Vector<long> mask, ushort* address, Vector<ulong> offsets);

    /// svuint64_t svld1uh_gather_[u64]offset_u64(svbool_t pg, const uint16_t *base, svuint64_t offsets) : "LD1H Zresult.D, Pg/Z, [Xbase, Zoffsets.D]"
  public static unsafe Vector<ulong> GatherVectorUInt16WithByteOffsetsZeroExtend(Vector<ulong> mask, ushort* address, Vector<ulong> offsets);


    /// GatherVectorUInt16ZeroExtend : Load 16-bit data and zero-extend

    /// svint32_t svld1uh_gather[_u32base]_s32(svbool_t pg, svuint32_t bases) : "LD1H Zresult.S, Pg/Z, [Zbases.S, #0]"
  public static unsafe Vector<int> GatherVectorUInt16ZeroExtend(Vector<int> mask, Vector<uint> addresses);

    /// svuint32_t svld1uh_gather[_u32base]_u32(svbool_t pg, svuint32_t bases) : "LD1H Zresult.S, Pg/Z, [Zbases.S, #0]"
  public static unsafe Vector<uint> GatherVectorUInt16ZeroExtend(Vector<uint> mask, Vector<uint> addresses);

    /// svint64_t svld1uh_gather[_u64base]_s64(svbool_t pg, svuint64_t bases) : "LD1H Zresult.D, Pg/Z, [Zbases.D, #0]"
  public static unsafe Vector<long> GatherVectorUInt16ZeroExtend(Vector<long> mask, Vector<ulong> addresses);

    /// svuint64_t svld1uh_gather[_u64base]_u64(svbool_t pg, svuint64_t bases) : "LD1H Zresult.D, Pg/Z, [Zbases.D, #0]"
  public static unsafe Vector<ulong> GatherVectorUInt16ZeroExtend(Vector<ulong> mask, Vector<ulong> addresses);

    /// svint32_t svld1uh_gather_[s32]index_s32(svbool_t pg, const uint16_t *base, svint32_t indices) : "LD1H Zresult.S, Pg/Z, [Xbase, Zindices.S, SXTW #1]"
  public static unsafe Vector<int> GatherVectorUInt16ZeroExtend(Vector<int> mask, ushort* address, Vector<int> indices);

    /// svuint32_t svld1uh_gather_[s32]index_u32(svbool_t pg, const uint16_t *base, svint32_t indices) : "LD1H Zresult.S, Pg/Z, [Xbase, Zindices.S, SXTW #1]"
  public static unsafe Vector<uint> GatherVectorUInt16ZeroExtend(Vector<uint> mask, ushort* address, Vector<int> indices);

    /// svint32_t svld1uh_gather_[u32]index_s32(svbool_t pg, const uint16_t *base, svuint32_t indices) : "LD1H Zresult.S, Pg/Z, [Xbase, Zindices.S, UXTW #1]"
  public static unsafe Vector<int> GatherVectorUInt16ZeroExtend(Vector<int> mask, ushort* address, Vector<uint> indices);

    /// svuint32_t svld1uh_gather_[u32]index_u32(svbool_t pg, const uint16_t *base, svuint32_t indices) : "LD1H Zresult.S, Pg/Z, [Xbase, Zindices.S, UXTW #1]"
  public static unsafe Vector<uint> GatherVectorUInt16ZeroExtend(Vector<uint> mask, ushort* address, Vector<uint> indices);

    /// svint64_t svld1uh_gather_[s64]index_s64(svbool_t pg, const uint16_t *base, svint64_t indices) : "LD1H Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #1]"
  public static unsafe Vector<long> GatherVectorUInt16ZeroExtend(Vector<long> mask, ushort* address, Vector<long> indices);

    /// svuint64_t svld1uh_gather_[s64]index_u64(svbool_t pg, const uint16_t *base, svint64_t indices) : "LD1H Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #1]"
  public static unsafe Vector<ulong> GatherVectorUInt16ZeroExtend(Vector<ulong> mask, ushort* address, Vector<long> indices);

    /// svint64_t svld1uh_gather_[u64]index_s64(svbool_t pg, const uint16_t *base, svuint64_t indices) : "LD1H Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #1]"
  public static unsafe Vector<long> GatherVectorUInt16ZeroExtend(Vector<long> mask, ushort* address, Vector<ulong> indices);

    /// svuint64_t svld1uh_gather_[u64]index_u64(svbool_t pg, const uint16_t *base, svuint64_t indices) : "LD1H Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #1]"
  public static unsafe Vector<ulong> GatherVectorUInt16ZeroExtend(Vector<ulong> mask, ushort* address, Vector<ulong> indices);


    /// GatherVectorUInt32WithByteOffsetsZeroExtend : Load 32-bit data and zero-extend

    /// svint64_t svld1uw_gather_[s64]offset_s64(svbool_t pg, const uint32_t *base, svint64_t offsets) : "LD1W Zresult.D, Pg/Z, [Xbase, Zoffsets.D]"
  public static unsafe Vector<long> GatherVectorUInt32WithByteOffsetsZeroExtend(Vector<long> mask, uint* address, Vector<long> offsets);

    /// svint64_t svld1uw_gather_[s64]offset_s64(svbool_t pg, const uint32_t *base, svint64_t offsets) : "LD1W Zresult.D, Pg/Z, [Xbase, Zoffsets.D]"
  public static unsafe Vector<int> GatherVectorUInt32WithByteOffsetsZeroExtend(Vector<int> mask, uint* address, Vector<int> offsets);

    /// svuint64_t svld1uw_gather_[s64]offset_u64(svbool_t pg, const uint32_t *base, svint64_t offsets) : "LD1W Zresult.D, Pg/Z, [Xbase, Zoffsets.D]"
  public static unsafe Vector<ulong> GatherVectorUInt32WithByteOffsetsZeroExtend(Vector<ulong> mask, uint* address, Vector<long> offsets);

    /// svuint64_t svld1uw_gather_[s64]offset_u64(svbool_t pg, const uint32_t *base, svint64_t offsets) : "LD1W Zresult.D, Pg/Z, [Xbase, Zoffsets.D]"
  public static unsafe Vector<uint> GatherVectorUInt32WithByteOffsetsZeroExtend(Vector<uint> mask, uint* address, Vector<int> offsets);

    /// svint64_t svld1uw_gather_[u64]offset_s64(svbool_t pg, const uint32_t *base, svuint64_t offsets) : "LD1W Zresult.D, Pg/Z, [Xbase, Zoffsets.D]"
  public static unsafe Vector<long> GatherVectorUInt32WithByteOffsetsZeroExtend(Vector<long> mask, uint* address, Vector<ulong> offsets);

    /// svint64_t svld1uw_gather_[u64]offset_s64(svbool_t pg, const uint32_t *base, svuint64_t offsets) : "LD1W Zresult.D, Pg/Z, [Xbase, Zoffsets.D]"
  public static unsafe Vector<int> GatherVectorUInt32WithByteOffsetsZeroExtend(Vector<int> mask, uint* address, Vector<uint> offsets);

    /// svuint64_t svld1uw_gather_[u64]offset_u64(svbool_t pg, const uint32_t *base, svuint64_t offsets) : "LD1W Zresult.D, Pg/Z, [Xbase, Zoffsets.D]"
  public static unsafe Vector<ulong> GatherVectorUInt32WithByteOffsetsZeroExtend(Vector<ulong> mask, uint* address, Vector<ulong> offsets);

    /// svuint64_t svld1uw_gather_[u64]offset_u64(svbool_t pg, const uint32_t *base, svuint64_t offsets) : "LD1W Zresult.D, Pg/Z, [Xbase, Zoffsets.D]"
  public static unsafe Vector<uint> GatherVectorUInt32WithByteOffsetsZeroExtend(Vector<uint> mask, uint* address, Vector<uint> offsets);


    /// GatherVectorUInt32ZeroExtend : Load 32-bit data and zero-extend

    /// svint64_t svld1uw_gather[_u64base]_s64(svbool_t pg, svuint64_t bases) : "LD1W Zresult.D, Pg/Z, [Zbases.D, #0]"
  public static unsafe Vector<long> GatherVectorUInt32ZeroExtend(Vector<long> mask, Vector<ulong> addresses);

    /// svint64_t svld1uw_gather[_u64base]_s64(svbool_t pg, svuint64_t bases) : "LD1W Zresult.D, Pg/Z, [Zbases.D, #0]"
  public static unsafe Vector<int> GatherVectorUInt32ZeroExtend(Vector<int> mask, Vector<uint> addresses);

    /// svuint64_t svld1uw_gather[_u64base]_u64(svbool_t pg, svuint64_t bases) : "LD1W Zresult.D, Pg/Z, [Zbases.D, #0]"
  public static unsafe Vector<ulong> GatherVectorUInt32ZeroExtend(Vector<ulong> mask, Vector<ulong> addresses);

    /// svuint64_t svld1uw_gather[_u64base]_u64(svbool_t pg, svuint64_t bases) : "LD1W Zresult.D, Pg/Z, [Zbases.D, #0]"
  public static unsafe Vector<uint> GatherVectorUInt32ZeroExtend(Vector<uint> mask, Vector<uint> addresses);

    /// svint64_t svld1uw_gather_[s64]index_s64(svbool_t pg, const uint32_t *base, svint64_t indices) : "LD1W Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #2]"
  public static unsafe Vector<long> GatherVectorUInt32ZeroExtend(Vector<long> mask, uint* address, Vector<long> indices);

    /// svint64_t svld1uw_gather_[s64]index_s64(svbool_t pg, const uint32_t *base, svint64_t indices) : "LD1W Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #2]"
  public static unsafe Vector<int> GatherVectorUInt32ZeroExtend(Vector<int> mask, uint* address, Vector<int> indices);

    /// svuint64_t svld1uw_gather_[s64]index_u64(svbool_t pg, const uint32_t *base, svint64_t indices) : "LD1W Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #2]"
  public static unsafe Vector<ulong> GatherVectorUInt32ZeroExtend(Vector<ulong> mask, uint* address, Vector<long> indices);

    /// svuint64_t svld1uw_gather_[s64]index_u64(svbool_t pg, const uint32_t *base, svint64_t indices) : "LD1W Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #2]"
  public static unsafe Vector<uint> GatherVectorUInt32ZeroExtend(Vector<uint> mask, uint* address, Vector<int> indices);

    /// svint64_t svld1uw_gather_[u64]index_s64(svbool_t pg, const uint32_t *base, svuint64_t indices) : "LD1W Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #2]"
  public static unsafe Vector<long> GatherVectorUInt32ZeroExtend(Vector<long> mask, uint* address, Vector<ulong> indices);

    /// svint64_t svld1uw_gather_[u64]index_s64(svbool_t pg, const uint32_t *base, svuint64_t indices) : "LD1W Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #2]"
  public static unsafe Vector<int> GatherVectorUInt32ZeroExtend(Vector<int> mask, uint* address, Vector<uint> indices);

    /// svuint64_t svld1uw_gather_[u64]index_u64(svbool_t pg, const uint32_t *base, svuint64_t indices) : "LD1W Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #2]"
  public static unsafe Vector<ulong> GatherVectorUInt32ZeroExtend(Vector<ulong> mask, uint* address, Vector<ulong> indices);

    /// svuint64_t svld1uw_gather_[u64]index_u64(svbool_t pg, const uint32_t *base, svuint64_t indices) : "LD1W Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #2]"
  public static unsafe Vector<uint> GatherVectorUInt32ZeroExtend(Vector<uint> mask, uint* address, Vector<uint> indices);


    /// GatherVectorWithByteOffsets : Unextended load

    /// svfloat32_t svld1_gather_[s32]offset[_f32](svbool_t pg, const float32_t *base, svint32_t offsets) : "LD1W Zresult.S, Pg/Z, [Xbase, Zoffsets.S, SXTW]"
  public static unsafe Vector<float> GatherVectorWithByteOffsets(Vector<float> mask, float* address, Vector<int> offsets);

    /// svint32_t svld1_gather_[s32]offset[_s32](svbool_t pg, const int32_t *base, svint32_t offsets) : "LD1W Zresult.S, Pg/Z, [Xbase, Zoffsets.S, SXTW]"
  public static unsafe Vector<int> GatherVectorWithByteOffsets(Vector<int> mask, int* address, Vector<int> offsets);

    /// svuint32_t svld1_gather_[s32]offset[_u32](svbool_t pg, const uint32_t *base, svint32_t offsets) : "LD1W Zresult.S, Pg/Z, [Xbase, Zoffsets.S, SXTW]"
  public static unsafe Vector<uint> GatherVectorWithByteOffsets(Vector<uint> mask, uint* address, Vector<int> offsets);

    /// svfloat32_t svld1_gather_[u32]offset[_f32](svbool_t pg, const float32_t *base, svuint32_t offsets) : "LD1W Zresult.S, Pg/Z, [Xbase, Zoffsets.S, UXTW]"
  public static unsafe Vector<float> GatherVectorWithByteOffsets(Vector<float> mask, float* address, Vector<uint> offsets);

    /// svint32_t svld1_gather_[u32]offset[_s32](svbool_t pg, const int32_t *base, svuint32_t offsets) : "LD1W Zresult.S, Pg/Z, [Xbase, Zoffsets.S, UXTW]"
  public static unsafe Vector<int> GatherVectorWithByteOffsets(Vector<int> mask, int* address, Vector<uint> offsets);

    /// svuint32_t svld1_gather_[u32]offset[_u32](svbool_t pg, const uint32_t *base, svuint32_t offsets) : "LD1W Zresult.S, Pg/Z, [Xbase, Zoffsets.S, UXTW]"
  public static unsafe Vector<uint> GatherVectorWithByteOffsets(Vector<uint> mask, uint* address, Vector<uint> offsets);

    /// svfloat64_t svld1_gather_[s64]offset[_f64](svbool_t pg, const float64_t *base, svint64_t offsets) : "LD1D Zresult.D, Pg/Z, [Xbase, Zoffsets.D]"
  public static unsafe Vector<double> GatherVectorWithByteOffsets(Vector<double> mask, double* address, Vector<long> offsets);

    /// svint64_t svld1_gather_[s64]offset[_s64](svbool_t pg, const int64_t *base, svint64_t offsets) : "LD1D Zresult.D, Pg/Z, [Xbase, Zoffsets.D]"
  public static unsafe Vector<long> GatherVectorWithByteOffsets(Vector<long> mask, long* address, Vector<long> offsets);

    /// svuint64_t svld1_gather_[s64]offset[_u64](svbool_t pg, const uint64_t *base, svint64_t offsets) : "LD1D Zresult.D, Pg/Z, [Xbase, Zoffsets.D]"
  public static unsafe Vector<ulong> GatherVectorWithByteOffsets(Vector<ulong> mask, ulong* address, Vector<long> offsets);

    /// svfloat64_t svld1_gather_[u64]offset[_f64](svbool_t pg, const float64_t *base, svuint64_t offsets) : "LD1D Zresult.D, Pg/Z, [Xbase, Zoffsets.D]"
  public static unsafe Vector<double> GatherVectorWithByteOffsets(Vector<double> mask, double* address, Vector<ulong> offsets);

    /// svint64_t svld1_gather_[u64]offset[_s64](svbool_t pg, const int64_t *base, svuint64_t offsets) : "LD1D Zresult.D, Pg/Z, [Xbase, Zoffsets.D]"
  public static unsafe Vector<long> GatherVectorWithByteOffsets(Vector<long> mask, long* address, Vector<ulong> offsets);

    /// svuint64_t svld1_gather_[u64]offset[_u64](svbool_t pg, const uint64_t *base, svuint64_t offsets) : "LD1D Zresult.D, Pg/Z, [Xbase, Zoffsets.D]"
  public static unsafe Vector<ulong> GatherVectorWithByteOffsets(Vector<ulong> mask, ulong* address, Vector<ulong> offsets);


  /// total method signatures: 182
  /// total method names:      16
}


  /// Rejected:
  ///   public static unsafe void GatherPrefetch16Bit(Vector<ushort> mask, Vector<uint> addresses, long index, [ConstantExpected] SvePrefetchType prefetchType); // svprfh_gather[_u32base]_index
  ///   public static unsafe void GatherPrefetch16Bit(Vector<short> mask, Vector<uint> addresses, long index, [ConstantExpected] SvePrefetchType prefetchType); // svprfh_gather[_u32base]_index
  ///   public static unsafe void GatherPrefetch16Bit(Vector<ushort> mask, Vector<ulong> addresses, long index, [ConstantExpected] SvePrefetchType prefetchType); // svprfh_gather[_u64base]_index
  ///   public static unsafe void GatherPrefetch16Bit(Vector<short> mask, Vector<ulong> addresses, long index, [ConstantExpected] SvePrefetchType prefetchType); // svprfh_gather[_u64base]_index
  ///   public static unsafe void GatherPrefetch32Bit(Vector<uint> mask, Vector<uint> addresses, long index, [ConstantExpected] SvePrefetchType prefetchType); // svprfw_gather[_u32base]_index
  ///   public static unsafe void GatherPrefetch32Bit(Vector<int> mask, Vector<uint> addresses, long index, [ConstantExpected] SvePrefetchType prefetchType); // svprfw_gather[_u32base]_index
  ///   public static unsafe void GatherPrefetch32Bit(Vector<uint> mask, Vector<ulong> addresses, long index, [ConstantExpected] SvePrefetchType prefetchType); // svprfw_gather[_u64base]_index
  ///   public static unsafe void GatherPrefetch32Bit(Vector<int> mask, Vector<ulong> addresses, long index, [ConstantExpected] SvePrefetchType prefetchType); // svprfw_gather[_u64base]_index
  ///   public static unsafe void GatherPrefetch64Bit(Vector<ulong> mask, Vector<uint> addresses, long index, [ConstantExpected] SvePrefetchType prefetchType); // svprfd_gather[_u32base]_index
  ///   public static unsafe void GatherPrefetch64Bit(Vector<long> mask, Vector<uint> addresses, long index, [ConstantExpected] SvePrefetchType prefetchType); // svprfd_gather[_u32base]_index
  ///   public static unsafe void GatherPrefetch64Bit(Vector<ulong> mask, Vector<ulong> addresses, long index, [ConstantExpected] SvePrefetchType prefetchType); // svprfd_gather[_u64base]_index
  ///   public static unsafe void GatherPrefetch64Bit(Vector<long> mask, Vector<ulong> addresses, long index, [ConstantExpected] SvePrefetchType prefetchType); // svprfd_gather[_u64base]_index
  ///   public static unsafe void GatherPrefetch8Bit(Vector<byte> mask, Vector<uint> addresses, long offset, [ConstantExpected] SvePrefetchType prefetchType); // svprfb_gather[_u32base]_offset
  ///   public static unsafe void GatherPrefetch8Bit(Vector<sbyte> mask, Vector<uint> addresses, long offset, [ConstantExpected] SvePrefetchType prefetchType); // svprfb_gather[_u32base]_offset
  ///   public static unsafe void GatherPrefetch8Bit(Vector<byte> mask, Vector<ulong> addresses, long offset, [ConstantExpected] SvePrefetchType prefetchType); // svprfb_gather[_u64base]_offset
  ///   public static unsafe void GatherPrefetch8Bit(Vector<sbyte> mask, Vector<ulong> addresses, long offset, [ConstantExpected] SvePrefetchType prefetchType); // svprfb_gather[_u64base]_offset
  ///   public static unsafe Vector<float> GatherVector(Vector<float> mask, Vector<uint> addresses, long offset); // svld1_gather[_u32base]_offset_f32
  ///   public static unsafe Vector<int> GatherVector(Vector<int> mask, Vector<uint> addresses, long offset); // svld1_gather[_u32base]_offset_s32
  ///   public static unsafe Vector<uint> GatherVector(Vector<uint> mask, Vector<uint> addresses, long offset); // svld1_gather[_u32base]_offset_u32
  ///   public static unsafe Vector<double> GatherVector(Vector<double> mask, Vector<ulong> addresses, long offset); // svld1_gather[_u64base]_offset_f64
  ///   public static unsafe Vector<long> GatherVector(Vector<long> mask, Vector<ulong> addresses, long offset); // svld1_gather[_u64base]_offset_s64
  ///   public static unsafe Vector<ulong> GatherVector(Vector<ulong> mask, Vector<ulong> addresses, long offset); // svld1_gather[_u64base]_offset_u64
  ///   public static unsafe Vector<float> GatherVector(Vector<float> mask, Vector<uint> addresses, long index); // svld1_gather[_u32base]_index_f32
  ///   public static unsafe Vector<int> GatherVector(Vector<int> mask, Vector<uint> addresses, long index); // svld1_gather[_u32base]_index_s32
  ///   public static unsafe Vector<uint> GatherVector(Vector<uint> mask, Vector<uint> addresses, long index); // svld1_gather[_u32base]_index_u32
  ///   public static unsafe Vector<double> GatherVector(Vector<double> mask, Vector<ulong> addresses, long index); // svld1_gather[_u64base]_index_f64
  ///   public static unsafe Vector<long> GatherVector(Vector<long> mask, Vector<ulong> addresses, long index); // svld1_gather[_u64base]_index_s64
  ///   public static unsafe Vector<ulong> GatherVector(Vector<ulong> mask, Vector<ulong> addresses, long index); // svld1_gather[_u64base]_index_u64
  ///   public static unsafe Vector<int> GatherVectorByteZeroExtend(Vector<int> mask, Vector<uint> address, long indices); // svld1ub_gather[_u32base]_offset_s32
  ///   public static unsafe Vector<uint> GatherVectorByteZeroExtend(Vector<uint> mask, Vector<uint> address, long indices); // svld1ub_gather[_u32base]_offset_u32
  ///   public static unsafe Vector<long> GatherVectorByteZeroExtend(Vector<long> mask, Vector<ulong> address, long indices); // svld1ub_gather[_u64base]_offset_s64
  ///   public static unsafe Vector<ulong> GatherVectorByteZeroExtend(Vector<ulong> mask, Vector<ulong> address, long indices); // svld1ub_gather[_u64base]_offset_u64
  ///   public static unsafe Vector<int> GatherVectorInt16SignExtend(Vector<int> mask, Vector<uint> addresses, long index); // svld1sh_gather[_u32base]_index_s32
  ///   public static unsafe Vector<uint> GatherVectorInt16SignExtend(Vector<uint> mask, Vector<uint> addresses, long index); // svld1sh_gather[_u32base]_index_u32
  ///   public static unsafe Vector<long> GatherVectorInt16SignExtend(Vector<long> mask, Vector<ulong> addresses, long index); // svld1sh_gather[_u64base]_index_s64
  ///   public static unsafe Vector<ulong> GatherVectorInt16SignExtend(Vector<ulong> mask, Vector<ulong> addresses, long index); // svld1sh_gather[_u64base]_index_u64
  ///   public static unsafe Vector<int> GatherVectorInt16WithByteOffsetsSignExtend(Vector<int> mask, Vector<uint> addresses, long offset); // svld1sh_gather[_u32base]_offset_s32
  ///   public static unsafe Vector<uint> GatherVectorInt16WithByteOffsetsSignExtend(Vector<uint> mask, Vector<uint> addresses, long offset); // svld1sh_gather[_u32base]_offset_u32
  ///   public static unsafe Vector<long> GatherVectorInt16WithByteOffsetsSignExtend(Vector<long> mask, Vector<ulong> addresses, long offset); // svld1sh_gather[_u64base]_offset_s64
  ///   public static unsafe Vector<ulong> GatherVectorInt16WithByteOffsetsSignExtend(Vector<ulong> mask, Vector<ulong> addresses, long offset); // svld1sh_gather[_u64base]_offset_u64
  ///   public static unsafe Vector<long> GatherVectorInt32SignExtend(Vector<long> mask, Vector<ulong> addresses, long index); // svld1sw_gather[_u64base]_index_s64
  ///   public static unsafe Vector<int> GatherVectorInt32SignExtend(Vector<int> mask, Vector<uint> addresses, long index); // svld1sw_gather[_u64base]_index_s64
  ///   public static unsafe Vector<ulong> GatherVectorInt32SignExtend(Vector<ulong> mask, Vector<ulong> addresses, long index); // svld1sw_gather[_u64base]_index_u64
  ///   public static unsafe Vector<uint> GatherVectorInt32SignExtend(Vector<uint> mask, Vector<uint> addresses, long index); // svld1sw_gather[_u64base]_index_u64
  ///   public static unsafe Vector<long> GatherVectorInt32WithByteOffsetsSignExtend(Vector<long> mask, Vector<ulong> addresses, long offset); // svld1sw_gather[_u64base]_offset_s64
  ///   public static unsafe Vector<int> GatherVectorInt32WithByteOffsetsSignExtend(Vector<int> mask, Vector<uint> addresses, long offset); // svld1sw_gather[_u64base]_offset_s64
  ///   public static unsafe Vector<ulong> GatherVectorInt32WithByteOffsetsSignExtend(Vector<ulong> mask, Vector<ulong> addresses, long offset); // svld1sw_gather[_u64base]_offset_u64
  ///   public static unsafe Vector<uint> GatherVectorInt32WithByteOffsetsSignExtend(Vector<uint> mask, Vector<uint> addresses, long offset); // svld1sw_gather[_u64base]_offset_u64
  ///   public static unsafe Vector<int> GatherVectorSByteSignExtend(Vector<int> mask, Vector<uint> address, long indices); // svld1sb_gather[_u32base]_offset_s32
  ///   public static unsafe Vector<uint> GatherVectorSByteSignExtend(Vector<uint> mask, Vector<uint> address, long indices); // svld1sb_gather[_u32base]_offset_u32
  ///   public static unsafe Vector<long> GatherVectorSByteSignExtend(Vector<long> mask, Vector<ulong> address, long indices); // svld1sb_gather[_u64base]_offset_s64
  ///   public static unsafe Vector<ulong> GatherVectorSByteSignExtend(Vector<ulong> mask, Vector<ulong> address, long indices); // svld1sb_gather[_u64base]_offset_u64
  ///   public static unsafe Vector<int> GatherVectorUInt16WithByteOffsetsZeroExtend(Vector<int> mask, Vector<uint> addresses, long offset); // svld1uh_gather[_u32base]_offset_s32
  ///   public static unsafe Vector<uint> GatherVectorUInt16WithByteOffsetsZeroExtend(Vector<uint> mask, Vector<uint> addresses, long offset); // svld1uh_gather[_u32base]_offset_u32
  ///   public static unsafe Vector<long> GatherVectorUInt16WithByteOffsetsZeroExtend(Vector<long> mask, Vector<ulong> addresses, long offset); // svld1uh_gather[_u64base]_offset_s64
  ///   public static unsafe Vector<ulong> GatherVectorUInt16WithByteOffsetsZeroExtend(Vector<ulong> mask, Vector<ulong> addresses, long offset); // svld1uh_gather[_u64base]_offset_u64
  ///   public static unsafe Vector<int> GatherVectorUInt16ZeroExtend(Vector<int> mask, Vector<uint> addresses, long index); // svld1uh_gather[_u32base]_index_s32
  ///   public static unsafe Vector<uint> GatherVectorUInt16ZeroExtend(Vector<uint> mask, Vector<uint> addresses, long index); // svld1uh_gather[_u32base]_index_u32
  ///   public static unsafe Vector<long> GatherVectorUInt16ZeroExtend(Vector<long> mask, Vector<ulong> addresses, long index); // svld1uh_gather[_u64base]_index_s64
  ///   public static unsafe Vector<ulong> GatherVectorUInt16ZeroExtend(Vector<ulong> mask, Vector<ulong> addresses, long index); // svld1uh_gather[_u64base]_index_u64
  ///   public static unsafe Vector<long> GatherVectorUInt32WithByteOffsetsZeroExtend(Vector<long> mask, Vector<ulong> addresses, long offset); // svld1uw_gather[_u64base]_offset_s64
  ///   public static unsafe Vector<int> GatherVectorUInt32WithByteOffsetsZeroExtend(Vector<int> mask, Vector<uint> addresses, long offset); // svld1uw_gather[_u64base]_offset_s64
  ///   public static unsafe Vector<ulong> GatherVectorUInt32WithByteOffsetsZeroExtend(Vector<ulong> mask, Vector<ulong> addresses, long offset); // svld1uw_gather[_u64base]_offset_u64
  ///   public static unsafe Vector<uint> GatherVectorUInt32WithByteOffsetsZeroExtend(Vector<uint> mask, Vector<uint> addresses, long offset); // svld1uw_gather[_u64base]_offset_u64
  ///   public static unsafe Vector<long> GatherVectorUInt32ZeroExtend(Vector<long> mask, Vector<ulong> addresses, long index); // svld1uw_gather[_u64base]_index_s64
  ///   public static unsafe Vector<int> GatherVectorUInt32ZeroExtend(Vector<int> mask, Vector<uint> addresses, long index); // svld1uw_gather[_u64base]_index_s64
  ///   public static unsafe Vector<ulong> GatherVectorUInt32ZeroExtend(Vector<ulong> mask, Vector<ulong> addresses, long index); // svld1uw_gather[_u64base]_index_u64
  ///   public static unsafe Vector<uint> GatherVectorUInt32ZeroExtend(Vector<uint> mask, Vector<uint> addresses, long index); // svld1uw_gather[_u64base]_index_u64
  ///   Total Rejected: 68

  /// Total ACLE covered across API:      250

