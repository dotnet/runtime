// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices.Marshalling
{
    /// <summary>
    /// A type that represents an OLE VARIANT in managed code.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct ComVariant : IDisposable
    {
        // See definition in wtypes.h in the Windows SDK.
        private const VarEnum VT_VERSIONED_STREAM = (VarEnum)73;
        // VARIANT_BOOL constants.
        internal const short VARIANT_TRUE = -1;
        internal const short VARIANT_FALSE = 0;
#if DEBUG
        static unsafe ComVariant()
        {
            // Variant size is the size of 4 pointers (16 bytes) on a 32-bit processor,
            // and 3 pointers (24 bytes) on a 64-bit processor.
            // See definition in oaidl.h in the Windows SDK.
            int variantSize = sizeof(ComVariant);
            if (IntPtr.Size == 4)
            {
                Debug.Assert(variantSize == (4 * IntPtr.Size));
            }
            else
            {
                Debug.Assert(IntPtr.Size == 8);
                Debug.Assert(variantSize == (3 * IntPtr.Size));
            }
        }
#endif

        // Most of the data types in the Variant are carried in _typeUnion
        [FieldOffset(0)] private TypeUnion _typeUnion;

        // Decimal is the largest data type and it needs to use the space that is normally unused in TypeUnion._wReserved1, etc.
        // Hence, it is declared to completely overlap with TypeUnion. A Decimal does not use the first two bytes, and so
        // TypeUnion._vt can still be used to encode the type.
        [FieldOffset(0)] private decimal _decimal;

        [StructLayout(LayoutKind.Sequential)]
        private struct TypeUnion
        {
            // The layout of _wReserved1 and _vt fields needs to match Decimal._flags
#if BIGENDIAN
            public ushort _wReserved1;
            public ushort _vt;
#else
            public ushort _vt;
            public ushort _wReserved1;
#endif
            public ushort _wReserved2;
            public ushort _wReserved3;

            public UnionTypes _unionTypes;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Record
        {
            public IntPtr _record;
            public IntPtr _recordInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Blob
        {
            public int _size;
            public IntPtr _data;
        }

        [StructLayout(LayoutKind.Sequential)]
        private unsafe struct Vector<T> where T : unmanaged
        {
            public int _numElements;
            public T* _data;

            public Span<T> AsSpan() => new(_data, _numElements);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct VersionedStream
        {
            public Guid _version;
            public IntPtr _stream;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ClipboardData
        {
            public uint _size;
            public int _format;
            public IntPtr _data;
        }

        [StructLayout(LayoutKind.Explicit)]
        private unsafe struct UnionTypes
        {
            [FieldOffset(0)] public sbyte _i1;
            [FieldOffset(0)] public short _i2;
            [FieldOffset(0)] public int _i4;
            [FieldOffset(0)] public long _i8;
            [FieldOffset(0)] public byte _ui1;
            [FieldOffset(0)] public ushort _ui2;
            [FieldOffset(0)] public uint _ui4;
            [FieldOffset(0)] public ulong _ui8;
            [FieldOffset(0)] public int _int;
            [FieldOffset(0)] public uint _uint;
            [FieldOffset(0)] public short _bool;
            [FieldOffset(0)] public int _error;
            [FieldOffset(0)] public float _r4;
            [FieldOffset(0)] public double _r8;
            [FieldOffset(0)] public long _cy;
            [FieldOffset(0)] public double _date;
            [FieldOffset(0)] public IntPtr _bstr;
            [FieldOffset(0)] public IntPtr _unknown;
            [FieldOffset(0)] public IntPtr _dispatch;
            [FieldOffset(0)] public IntPtr _pvarVal;
            [FieldOffset(0)] public IntPtr _byref;
            [FieldOffset(0)] public Record _record;
            [FieldOffset(0)] public Blob _blob;
            [FieldOffset(0)] public VersionedStream* _versionedStream;
            [FieldOffset(0)] public ClipboardData* clipboardData;
        }

        /// <summary>
        /// Release resources owned by this <see cref="ComVariant"/> instance.
        /// </summary>
        public unsafe void Dispose()
        {
#if TARGET_WINDOWS
            fixed (void* pThis = &this)
            {
                // We are using PropVariantClear instead of VariantClear
                // as PropVariantClear covers more cases (like VT_BLOB, VT_STREAM, VT_CF, etc.)
                // than VariantClear does. We intend for users to be able to use this ComVariant type for
                // both VARIANT and PROPVARIANT scenarios, so we need to support all of the variant kinds that might be set.
                Interop.Ole32.PropVariantClear((nint)pThis);
            }
#else
            // Re-implement the same clearing semantics as PropVariantClear manually for non-Windows platforms.
            if (VarType == VarEnum.VT_BSTR)
            {
                Marshal.FreeBSTR(_typeUnion._unionTypes._bstr);
            }
            else if (VarType.HasFlag(VarEnum.VT_ARRAY))
            {
                throw new PlatformNotSupportedException(SR.ComVariant_SafeArray_PlatformNotSupported);
            }
            else if (VarType == VarEnum.VT_UNKNOWN || VarType == VarEnum.VT_DISPATCH)
            {
                if (_typeUnion._unionTypes._unknown != IntPtr.Zero)
                {
                    Marshal.Release(_typeUnion._unionTypes._unknown);
                }
            }
            else if (VarType == VarEnum.VT_RECORD)
            {
                if (_typeUnion._unionTypes._record._recordInfo != IntPtr.Zero)
                {
                    // Invoke RecordClear on the record info with the data.
                    if (_typeUnion._unionTypes._record._record != IntPtr.Zero)
                    {
                        Marshal.ThrowExceptionForHR(((delegate* unmanaged<IntPtr, IntPtr, int>)(*(*(void***)_typeUnion._unionTypes._record._recordInfo + 4 /* IRecordInfo.RecordClear slot */)))(_typeUnion._unionTypes._record._recordInfo, _typeUnion._unionTypes._record._record));
                    }
                    Marshal.Release(_typeUnion._unionTypes._record._recordInfo);
                }
            }
            else if (VarType == VarEnum.VT_LPSTR || VarType == VarEnum.VT_LPWSTR || VarType == VarEnum.VT_CLSID)
            {
                Marshal.FreeCoTaskMem(_typeUnion._unionTypes._byref);
            }
            else if (VarType == VarEnum.VT_BLOB || VarType == VarEnum.VT_BLOB_OBJECT)
            {
                Marshal.FreeCoTaskMem(_typeUnion._unionTypes._blob._data);
            }
            else if (VarType == VarEnum.VT_STREAM || VarType == VarEnum.VT_STREAMED_OBJECT || VarType == VarEnum.VT_STORAGE || VarType == VarEnum.VT_STORED_OBJECT)
            {
                if (_typeUnion._unionTypes._unknown != IntPtr.Zero)
                {
                    Marshal.Release(_typeUnion._unionTypes._unknown);
                }
            }
            else if (VarType == VT_VERSIONED_STREAM)
            {
                VersionedStream* versionedStream = _typeUnion._unionTypes._versionedStream;
                if (versionedStream != null && versionedStream->_stream != IntPtr.Zero)
                {
                    Marshal.Release(versionedStream->_stream);
                }
                Marshal.FreeCoTaskMem((nint)versionedStream);
            }
            else if (VarType == VarEnum.VT_CF)
            {
                ClipboardData* clipboardData = _typeUnion._unionTypes.clipboardData;
                if (clipboardData != null)
                {
                    Marshal.FreeCoTaskMem(clipboardData->_data);
                    Marshal.FreeCoTaskMem((nint)clipboardData);
                }
            }
            else if (VarType.HasFlag(VarEnum.VT_VECTOR))
            {
                switch (VarType & ~VarEnum.VT_VECTOR)
                {
                    case VarEnum.VT_BSTR:
                        foreach (var str in GetRawDataRef<Vector<IntPtr>>().AsSpan())
                        {
                            Marshal.FreeBSTR(str);
                        }
                        break;
                    case VarEnum.VT_LPSTR:
                    case VarEnum.VT_LPWSTR:
                        foreach (var str in GetRawDataRef<Vector<IntPtr>>().AsSpan())
                        {
                            Marshal.FreeCoTaskMem(str);
                        }
                        break;
                    case VarEnum.VT_CF:
                        foreach (var cf in GetRawDataRef<Vector<ClipboardData>>().AsSpan())
                        {
                            Marshal.FreeCoTaskMem(cf._data);
                        }
                        break;
                    case VarEnum.VT_VARIANT:
                        foreach (var variant in GetRawDataRef<Vector<ComVariant>>().AsSpan())
                        {
                            variant.Dispose();
                        }
                        break;
                    default:
                        break;
                }
                Marshal.FreeCoTaskMem((nint)GetRawDataRef<Vector<byte>>()._data);
            }

            // Clear out this ComVariant instance.
            this = default;
#endif
        }

#pragma warning disable CS0618 // We support the obsolete CurrencyWrapper type
        /// <summary>
        /// Create an <see cref="ComVariant"/> instance from the specified value.
        /// </summary>
        /// <typeparam name="T">The type of the specified value.</typeparam>
        /// <param name="value">The value to wrap in an <see cref="ComVariant"/>.</param>
        /// <returns>An <see cref="ComVariant"/> that contains the provided value.</returns>
        /// <exception cref="ArgumentException">When <typeparamref name="T"/> does not directly correspond to a <see cref="VarEnum"/> variant type.</exception>
        public static ComVariant Create<T>([DisallowNull] T value)
        {
            Unsafe.SkipInit(out ComVariant variant);
            if (typeof(T) == typeof(DBNull))
            {
                variant = Null;
            }
            else if (typeof(T) == typeof(short))
            {
                variant.VarType = VarEnum.VT_I2;
                variant._typeUnion._unionTypes._i2 = (short)(object)value;
            }
            else if (typeof(T) == typeof(int))
            {
                variant.VarType = VarEnum.VT_I4;
                variant._typeUnion._unionTypes._i4 = (int)(object)value;
            }
            else if (typeof(T) == typeof(float))
            {
                variant.VarType = VarEnum.VT_R4;
                variant._typeUnion._unionTypes._r4 = (float)(object)value;
            }
            else if (typeof(T) == typeof(double))
            {
                variant.VarType = VarEnum.VT_R8;
                variant._typeUnion._unionTypes._r8 = (double)(object)value;
            }
            else if (typeof(T) == typeof(CurrencyWrapper))
            {
                variant.VarType = VarEnum.VT_CY;
                variant._typeUnion._unionTypes._cy = decimal.ToOACurrency(((CurrencyWrapper)(object)value).WrappedObject);
            }
            else if (typeof(T) == typeof(DateTime))
            {
                variant.VarType = VarEnum.VT_DATE;
                variant._typeUnion._unionTypes._date = ((DateTime)(object)value).ToOADate();
            }
            else if (typeof(T) == typeof(BStrWrapper))
            {
                variant.VarType = VarEnum.VT_BSTR;
                variant._typeUnion._unionTypes._bstr = Marshal.StringToBSTR(((BStrWrapper)(object)value).WrappedObject);
            }
            else if (typeof(T) == typeof(string))
            {
                // We map string to VT_BSTR as that's the only valid option for a VARIANT.
                // The rest of the "string" options are only supported in TYPEDESCs and PROPVARIANTs,
                // which are different scenarios.
                // Users who want to use the ComVariant type with VT_LPSTR or VT_LPWSTR can use CreateRaw
                // to do so.
                variant.VarType = VarEnum.VT_BSTR;
                variant._typeUnion._unionTypes._bstr = Marshal.StringToBSTR((string)(object)value);
            }
            else if (typeof(T) == typeof(ErrorWrapper))
            {
                variant.VarType = VarEnum.VT_ERROR;
                variant._typeUnion._unionTypes._error = ((ErrorWrapper)(object)value).ErrorCode;
            }
            else if (typeof(T) == typeof(bool))
            {
                // bool values in OLE VARIANTs are VARIANT_BOOL values.
                variant.VarType = VarEnum.VT_BOOL;
                variant._typeUnion._unionTypes._bool = ((bool)(object)value) ? VARIANT_TRUE : VARIANT_FALSE;
            }
            else if (typeof(T) == typeof(decimal))
            {
                // Set the value first and then the type as the decimal storage
                // overlaps with the type descriminator.
                variant._decimal = (decimal)(object)value;
                variant.VarType = VarEnum.VT_DECIMAL;
            }
            else if (typeof(T) == typeof(sbyte))
            {
                variant.VarType = VarEnum.VT_I1;
                variant._typeUnion._unionTypes._i1 = (sbyte)(object)value;
            }
            else if (typeof(T) == typeof(byte))
            {
                variant.VarType = VarEnum.VT_UI1;
                variant._typeUnion._unionTypes._ui1 = (byte)(object)value;
            }
            else if (typeof(T) == typeof(ushort))
            {
                variant.VarType = VarEnum.VT_UI2;
                variant._typeUnion._unionTypes._ui2 = (ushort)(object)value;
            }
            else if (typeof(T) == typeof(uint))
            {
                variant.VarType = VarEnum.VT_UI4;
                variant._typeUnion._unionTypes._ui4 = (uint)(object)value;
            }
            else if (typeof(T) == typeof(long))
            {
                variant.VarType = VarEnum.VT_I8;
                variant._typeUnion._unionTypes._i8 = (long)(object)value;
            }
            else if (typeof(T) == typeof(ulong))
            {
                variant.VarType = VarEnum.VT_UI8;
                variant._typeUnion._unionTypes._ui8 = (ulong)(object)value;
            }
            else
            {
                throw new ArgumentException(SR.UnsupportedType, nameof(T));
            }
            // Historically, .NET built-in and dynamic-COM interop has mapped
            // VT_INT and VT_UINT to use IntPtr. This is not valid per the MS-OAUT spec.
            // The MS-OAUT spec specifies that VT_INT and VT_UINT map to 4-byte integers.
            // As a result, do not support mapping nint or nuint to VT_INT and VT_UINT respectively
            // how built-in interop traditionally has.
            // We do not map VT_BYREF automatically, nor do we map any of the array types.
            return variant;
        }
#pragma warning restore CS0618

        /// <summary>
        /// Create a <see cref="ComVariant"/> with the given type and provided value.
        /// </summary>
        /// <typeparam name="T">The type of the value to store in the variant.</typeparam>
        /// <param name="vt">The type of the variant</param>
        /// <param name="rawValue">The raw value to store in the variant without any processing</param>
        /// <returns>A variant that contains the provided value.</returns>
        /// <exception cref="ArgumentException">When the provided <paramref name="vt"/> corresponds to a variant type that is not supported in VARIANTs or is <see cref="VarEnum.VT_DECIMAL"/></exception>
        /// <exception cref="PlatformNotSupportedException">When the provided <paramref name="vt"/> specifies the <see cref="VarEnum.VT_ARRAY"/> flag for SAFEARRAYs.</exception>
        public static unsafe ComVariant CreateRaw<T>(VarEnum vt, T rawValue)
            where T : unmanaged
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(Unsafe.SizeOf<T>(), sizeof(UnionTypes), nameof(T));
            if (vt == VarEnum.VT_DECIMAL)
            {
                throw new ArgumentException(SR.ComVariant_VT_DECIMAL_NotSupported_CreateRaw, nameof(vt));
            }
            if (vt == VarEnum.VT_VARIANT)
            {
                throw new ArgumentException(SR.ComVariant_VT_VARIANT_In_Variant, nameof(vt));
            }
            if (vt.HasFlag(VarEnum.VT_ARRAY) && !OperatingSystem.IsWindows())
            {
                throw new PlatformNotSupportedException(SR.ComVariant_SafeArray_PlatformNotSupported);
            }

            Unsafe.SkipInit(out ComVariant value);
            value.VarType = vt;
            value.GetRawDataRef<T>() = (vt, sizeof(T)) switch
            {
                (VarEnum.VT_I1 or VarEnum.VT_UI1, 1) => rawValue,
                (VarEnum.VT_I2 or VarEnum.VT_UI2 or VarEnum.VT_BOOL, 2) => rawValue,
                (VarEnum.VT_ERROR or VarEnum.VT_HRESULT or VarEnum.VT_I4 or VarEnum.VT_UI4 or VarEnum.VT_R4 or VarEnum.VT_INT or VarEnum.VT_UINT, 4) => rawValue,
                (VarEnum.VT_I8 or VarEnum.VT_UI8 or VarEnum.VT_R8 or VarEnum.VT_DATE, 8) => rawValue,
                (VarEnum.VT_UNKNOWN or VarEnum.VT_DISPATCH or VarEnum.VT_LPSTR or VarEnum.VT_BSTR or VarEnum.VT_LPWSTR or VarEnum.VT_SAFEARRAY
                    or VarEnum.VT_CLSID or VarEnum.VT_STREAM or VarEnum.VT_STREAMED_OBJECT or VarEnum.VT_STORAGE or VarEnum.VT_STORED_OBJECT or VarEnum.VT_CF or VT_VERSIONED_STREAM, _) when sizeof(T) == nint.Size => rawValue,
                (VarEnum.VT_CY or VarEnum.VT_FILETIME, 8) => rawValue,
                (VarEnum.VT_RECORD, _) when sizeof(T) == sizeof(Record) => rawValue,
                _ when vt.HasFlag(VarEnum.VT_BYREF) && sizeof(T) == nint.Size => rawValue,
                _ when vt.HasFlag(VarEnum.VT_VECTOR) && sizeof(T) == sizeof(Vector<byte>) => rawValue,
                _ when vt.HasFlag(VarEnum.VT_ARRAY) && sizeof(T) == nint.Size => rawValue,
                (VarEnum.VT_BLOB or VarEnum.VT_BLOB_OBJECT, _) when sizeof(T) == sizeof(Blob) => rawValue,
                _ => throw new ArgumentException(SR.Format(SR.ComVariant_SizeMustMatchVariantSize, nameof(T), nameof(vt)))
            };

            return value;
        }

        /// <summary>
        /// A <see cref="ComVariant"/> instance that represents a null value with <see cref="VarEnum.VT_NULL"/> type.
        /// </summary>
        public static ComVariant Null { get; } = new() { VarType = VarEnum.VT_NULL };

        private readonly void ThrowIfNotVarType(params VarEnum[] requiredType)
        {
            if (Array.IndexOf(requiredType, VarType) == -1)
            {
                throw new InvalidOperationException(SR.Format(SR.ComVariant_TypeIsNotSupportedType, VarType, string.Join(", ", requiredType)));
            }
        }

#pragma warning disable CS0618 // Type or member is obsolete
        /// <summary>
        /// Create a managed value based on the value in the <see cref="ComVariant"/> instance.
        /// </summary>
        /// <typeparam name="T">The managed type to create an instance of.</typeparam>
        /// <returns>The managed value contained in this variant.</returns>
        /// <exception cref="ArgumentException">When <typeparamref name="T"/> does not match the <see cref="VarType"/> of the <see cref="ComVariant"/>.</exception>
        public readonly T? As<T>()
        {
            if (VarType == VarEnum.VT_EMPTY)
            {
                return default;
            }
            if (typeof(T) == typeof(DBNull))
            {
                ThrowIfNotVarType(VarEnum.VT_NULL);
                return (T)(object)DBNull.Value;
            }
            else if (typeof(T) == typeof(short))
            {
                ThrowIfNotVarType(VarEnum.VT_I2);
                return (T)(object)_typeUnion._unionTypes._i2;
            }
            else if (typeof(T) == typeof(int))
            {
                ThrowIfNotVarType(VarEnum.VT_I4, VarEnum.VT_ERROR, VarEnum.VT_INT);
                return (T)(object)_typeUnion._unionTypes._i4;
            }
            else if (typeof(T) == typeof(float))
            {
                ThrowIfNotVarType(VarEnum.VT_R4);
                return (T)(object)_typeUnion._unionTypes._r4;
            }
            else if (typeof(T) == typeof(double))
            {
                ThrowIfNotVarType(VarEnum.VT_R8);
                return (T)(object)_typeUnion._unionTypes._r8;
            }
            else if (typeof(T) == typeof(CurrencyWrapper))
            {
                ThrowIfNotVarType(VarEnum.VT_CY);
                return (T)(object)new CurrencyWrapper(decimal.FromOACurrency(_typeUnion._unionTypes._cy));
            }
            else if (typeof(T) == typeof(DateTime))
            {
                ThrowIfNotVarType(VarEnum.VT_DATE);
                return (T)(object)DateTime.FromOADate(_typeUnion._unionTypes._date);
            }
            else if (typeof(T) == typeof(BStrWrapper))
            {
                ThrowIfNotVarType(VarEnum.VT_BSTR);
                return (T)(object)new BStrWrapper(Marshal.PtrToStringBSTR(_typeUnion._unionTypes._bstr));
            }
            else if (typeof(T) == typeof(string))
            {
                // To match the Create method, we will only support getting a string from an ComVariant
                // when the ComVariant holds a BSTR.
                ThrowIfNotVarType(VarEnum.VT_BSTR);
                if (_typeUnion._unionTypes._bstr == IntPtr.Zero)
                {
                    return default;
                }
                return (T)(object)Marshal.PtrToStringBSTR(_typeUnion._unionTypes._bstr);
            }
            else if (typeof(T) == typeof(ErrorWrapper))
            {
                ThrowIfNotVarType(VarEnum.VT_ERROR);
                return (T)(object)new ErrorWrapper(_typeUnion._unionTypes._error);
            }
            else if (typeof(T) == typeof(bool))
            {
                // bool values in OLE VARIANTs are VARIANT_BOOL values.
                ThrowIfNotVarType(VarEnum.VT_BOOL);
                return (T)(object)(_typeUnion._unionTypes._bool != VARIANT_FALSE);
            }
            else if (typeof(T) == typeof(decimal))
            {
                // Set the value first and then the type as the decimal storage
                // overlaps with the type descriminator.
                ThrowIfNotVarType(VarEnum.VT_DECIMAL);
                // Create a second variant copy with the VarType set to VT_EMPTY.
                // This will ensure that we don't leak the VT_DECMIAL flag into the decimal value itself.
                ComVariant variantWithoutVarType = this;
                variantWithoutVarType.VarType = VarEnum.VT_EMPTY;
                return (T)(object)variantWithoutVarType._decimal;
            }
            else if (typeof(T) == typeof(sbyte))
            {
                ThrowIfNotVarType(VarEnum.VT_I1);
                return (T)(object)_typeUnion._unionTypes._i1;
            }
            else if (typeof(T) == typeof(byte))
            {
                ThrowIfNotVarType(VarEnum.VT_UI1);
                return (T)(object)_typeUnion._unionTypes._ui1;
            }
            else if (typeof(T) == typeof(ushort))
            {
                ThrowIfNotVarType(VarEnum.VT_UI2);
                return (T)(object)_typeUnion._unionTypes._ui2;
            }
            else if (typeof(T) == typeof(uint))
            {
                ThrowIfNotVarType(VarEnum.VT_UI4, VarEnum.VT_UINT);
                return (T)(object)_typeUnion._unionTypes._ui4;
            }
            else if (typeof(T) == typeof(long))
            {
                ThrowIfNotVarType(VarEnum.VT_I8);
                return (T)(object)_typeUnion._unionTypes._i8;
            }
            else if (typeof(T) == typeof(ulong))
            {
                ThrowIfNotVarType(VarEnum.VT_UI8);
                return (T)(object)_typeUnion._unionTypes._ui8;
            }
            throw new ArgumentException(SR.UnsupportedType, nameof(T));
        }
#pragma warning restore CS0618 // Type or member is obsolete

        /// <summary>
        /// The type of the data stored in this <see cref="ComVariant"/>.
        /// </summary>
        public VarEnum VarType
        {
            readonly get => (VarEnum)_typeUnion._vt;
            private set => _typeUnion._vt = (ushort)value;
        }

        /// <summary>
        /// Get a reference to the storage location within this <see cref="ComVariant"/> instance.
        /// </summary>
        /// <typeparam name="T">The type of reference to return.</typeparam>
        /// <returns>A reference to the storage location within this <see cref="ComVariant"/>.</returns>
        /// <exception cref="ArgumentException"><typeparamref name="T"/> is <see cref="decimal"/> or larger than the storage space in an <see cref="ComVariant"/>.</exception>
        [UnscopedRef]
        public unsafe ref T GetRawDataRef<T>()
            where T : unmanaged
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(Unsafe.SizeOf<T>(), sizeof(UnionTypes), nameof(T));
            if (typeof(T) == typeof(decimal))
            {
                throw new ArgumentException(SR.ComVariant_VT_DECIMAL_NotSupported_RawDataRef, nameof(T));
            }
            return ref Unsafe.As<UnionTypes, T>(ref _typeUnion._unionTypes);
        }
    }
}
