// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace System.Runtime.InteropServices.Marshalling
{
    /// <summary>
    /// Marshals an <see cref="object"/> to an <see cref="ComVariant"/>.
    /// </summary>
    /// <remarks>
    /// Supports the same types as <see cref="ComVariant.Create{T}(T)"/> as well as any types with <see cref="GeneratedComClassAttribute"/> applied.
    /// </remarks>
    [CustomMarshaller(typeof(object), MarshalMode.Default, typeof(ComVariantMarshaller))]
    [CustomMarshaller(typeof(object), MarshalMode.UnmanagedToManagedRef, typeof(RefPropagate))]
    public static partial class ComVariantMarshaller
    {
        // VARIANT_BOOL constants.
        private const short VARIANT_TRUE = -1;
        private const short VARIANT_FALSE = 0;
        public static ComVariant ConvertToUnmanaged(object? managed)
        {
            if (managed is null)
            {
                return default;
            }

#pragma warning disable CS0618 // Type or member is obsolete
            switch (managed)
            {
                case sbyte s:
                    return ComVariant.Create(s);
                case byte b:
                    return ComVariant.Create(b);
                case short s:
                    return ComVariant.Create(s);
                case ushort s:
                    return ComVariant.Create(s);
                case int i:
                    return ComVariant.Create(i);
                case uint i:
                    return ComVariant.Create(i);
                case long l:
                    return ComVariant.Create(l);
                case ulong l:
                    return ComVariant.Create(l);
                case float f:
                    return ComVariant.Create(f);
                case double d:
                    return ComVariant.Create(d);
                case decimal d:
                    return ComVariant.Create(d);
                case bool b:
                    return ComVariant.Create(b);
                case string s:
                    return ComVariant.Create(s);
                case DateTime dt:
                    return ComVariant.Create(dt);
                case ErrorWrapper errorWrapper:
                    return ComVariant.Create(errorWrapper);
                case CurrencyWrapper currencyWrapper:
                    return ComVariant.Create(currencyWrapper);
                case BStrWrapper bStrWrapper:
                    return ComVariant.Create(bStrWrapper);
                case DBNull:
                    return ComVariant.Null;
            }
#pragma warning restore CS0618 // Type or member is obsolete

            if (TryCreateOleVariantForInterfaceWrapper(managed, out ComVariant variant))
            {
                return variant;
            }

            throw new ArgumentException(SR.ComVariantMarshaller_ManagedTypeNotSupported, nameof(managed));
        }

#pragma warning disable CA1416 // Validate platform compatibility
        private static unsafe bool TryCreateOleVariantForInterfaceWrapper(object managed, out ComVariant variant)
        {
            if (managed is UnknownWrapper uw)
            {
                object? wrapped = uw.WrappedObject;
                if (wrapped is null)
                {
                    variant = default;
                    return true;
                }
                variant = ComVariant.CreateRaw(VarEnum.VT_UNKNOWN, StrategyBasedComWrappers.DefaultMarshallingInstance.GetOrCreateComInterfaceForObject(wrapped, CreateComInterfaceFlags.None));
                return true;
            }
            else if (managed is not null && StrategyBasedComWrappers.DefaultIUnknownInterfaceDetailsStrategy.GetComExposedTypeDetails(managed.GetType().TypeHandle) is not null)
            {
                variant = ComVariant.CreateRaw(VarEnum.VT_UNKNOWN, StrategyBasedComWrappers.DefaultMarshallingInstance.GetOrCreateComInterfaceForObject(managed, CreateComInterfaceFlags.None));
                return true;
            }
            variant = default;
            return false;
        }
#pragma warning restore CA1416 // Validate platform compatibility

        public static unsafe object? ConvertToManaged(ComVariant unmanaged)
        {
#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable CA1416 // Validate platform compatibility
            switch (unmanaged.VarType)
            {
                case VarEnum.VT_EMPTY:
                case VarEnum.VT_BYREF | VarEnum.VT_EMPTY:
                    return null;
                case VarEnum.VT_NULL:
                case VarEnum.VT_BYREF | VarEnum.VT_NULL:
                    return DBNull.Value;
                case VarEnum.VT_I1:
                    return unmanaged.As<sbyte>();
                case VarEnum.VT_UI1:
                    return unmanaged.As<byte>();
                case VarEnum.VT_I2:
                    return unmanaged.As<short>();
                case VarEnum.VT_UI2:
                    return unmanaged.As<ushort>();
                case VarEnum.VT_INT:
                case VarEnum.VT_I4:
                    return unmanaged.As<int>();
                case VarEnum.VT_UINT:
                case VarEnum.VT_UI4:
                    return unmanaged.As<uint>();
                case VarEnum.VT_I8:
                    return unmanaged.As<long>();
                case VarEnum.VT_UI8:
                    return unmanaged.As<ulong>();
                case VarEnum.VT_R4:
                    return unmanaged.As<float>();
                case VarEnum.VT_R8:
                    return unmanaged.As<double>();
                case VarEnum.VT_DECIMAL:
                    return unmanaged.As<decimal>();
                case VarEnum.VT_BOOL:
                    return unmanaged.As<bool>();
                case VarEnum.VT_BSTR:
                    return unmanaged.As<string>();
                case VarEnum.VT_DATE:
                    return unmanaged.As<DateTime>();
                case VarEnum.VT_ERROR:
                    return unmanaged.As<int>();
                case VarEnum.VT_CY:
                    return unmanaged.As<CurrencyWrapper>()!.WrappedObject;
                case VarEnum.VT_UNKNOWN:
                case VarEnum.VT_DISPATCH:
                    return StrategyBasedComWrappers.DefaultMarshallingInstance.GetOrCreateObjectForComInstance(unmanaged.GetRawDataRef<nint>(), CreateObjectFlags.Unwrap);
                case VarEnum.VT_BYREF | VarEnum.VT_VARIANT:
                    return ConvertToManaged(*(ComVariant*)unmanaged.GetRawDataRef<nint>());
                case VarEnum.VT_BYREF | VarEnum.VT_I1:
                    return *(sbyte*)unmanaged.GetRawDataRef<nint>();
                case VarEnum.VT_BYREF | VarEnum.VT_UI1:
                    return *(byte*)unmanaged.GetRawDataRef<nint>();
                case VarEnum.VT_BYREF | VarEnum.VT_I2:
                    return *(short*)unmanaged.GetRawDataRef<nint>();
                case VarEnum.VT_BYREF | VarEnum.VT_UI2:
                    return *(ushort*)unmanaged.GetRawDataRef<nint>();
                case VarEnum.VT_BYREF | VarEnum.VT_I4:
                    return *(int*)unmanaged.GetRawDataRef<nint>();
                case VarEnum.VT_BYREF | VarEnum.VT_UI4:
                    return *(uint*)unmanaged.GetRawDataRef<nint>();
                case VarEnum.VT_BYREF | VarEnum.VT_I8:
                    return *(long*)unmanaged.GetRawDataRef<nint>();
                case VarEnum.VT_BYREF | VarEnum.VT_UI8:
                    return *(ulong*)unmanaged.GetRawDataRef<nint>();
                case VarEnum.VT_BYREF | VarEnum.VT_R4:
                    return *(float*)unmanaged.GetRawDataRef<nint>();
                case VarEnum.VT_BYREF | VarEnum.VT_R8:
                    return *(double*)unmanaged.GetRawDataRef<nint>();
                case VarEnum.VT_BYREF | VarEnum.VT_DECIMAL:
                    return *(decimal*)unmanaged.GetRawDataRef<nint>();
                case VarEnum.VT_BYREF | VarEnum.VT_BOOL:
                    return *(short*)unmanaged.GetRawDataRef<nint>() != VARIANT_FALSE;
                case VarEnum.VT_BYREF | VarEnum.VT_BSTR:
                    return Marshal.PtrToStringBSTR(*(IntPtr*)unmanaged.GetRawDataRef<nint>());
                case VarEnum.VT_BYREF | VarEnum.VT_DATE:
                    return DateTime.FromOADate(*(double*)unmanaged.GetRawDataRef<nint>());
                case VarEnum.VT_BYREF | VarEnum.VT_ERROR:
                    return *(int*)unmanaged.GetRawDataRef<nint>();
                case VarEnum.VT_BYREF | VarEnum.VT_CY:
                    return decimal.FromOACurrency(*(long*)unmanaged.GetRawDataRef<nint>());
                case VarEnum.VT_BYREF | VarEnum.VT_UNKNOWN:
                    return StrategyBasedComWrappers.DefaultMarshallingInstance.GetOrCreateObjectForComInstance(*(nint*)unmanaged.GetRawDataRef<nint>(), CreateObjectFlags.Unwrap);
                default:
                    throw new ArgumentException(SR.ComVariantMarshaller_UnmanagedTypeNotSupported, nameof(unmanaged));
            }
#pragma warning restore CA1416 // Validate platform compatibility
#pragma warning restore CS0618 // Type or member is obsolete
        }

        public static void Free(ComVariant unmanaged) => unmanaged.Dispose();

        /// <summary>
        /// Marshals a <see cref="object"/> to an <see cref="ComVariant"/>, propagating the value of the <see cref="object"/> back to the variant's
        /// existing data storage if the variant has <see cref="VarEnum.VT_BYREF"/> type.
        /// </summary>
        public struct RefPropagate
        {
            private ComVariant _unmanaged;
            private object? _managed;

            /// <summary>
            /// Initializes the marshaller with an unmanaged variant.
            /// </summary>
            /// <param name="unmanaged">The unmanaged value</param>
            public void FromUnmanaged(ComVariant unmanaged) => _unmanaged = unmanaged;

            /// <summary>
            /// Initializes the marshaller with a managed object.
            /// </summary>
            /// <param name="managed">The managed object.</param>
            public void FromManaged(object? managed) => _managed = managed;

            /// <summary>
            /// Create an unmanaged <see cref="ComVariant"/> based on the provided managed and unmanaged values.
            /// </summary>
            /// <returns>An <see cref="ComVariant"/> instance representing the marshaller's current state.</returns>
            /// <exception cref="ArgumentException">When the managed value must be propagated back to the unmanaged variant, but the managed value type cannot be converted to the variant's type.</exception>
            public unsafe ComVariant ToUnmanaged()
            {
                if (!_unmanaged.VarType.HasFlag(VarEnum.VT_BYREF))
                {
                    return ConvertToUnmanaged(_managed);
                }

                if (_managed is null
                    && (_unmanaged.VarType & ~VarEnum.VT_BYREF) is
                        VarEnum.VT_BSTR
                        or VarEnum.VT_DISPATCH
                        or VarEnum.VT_UNKNOWN)
                {
                    *(IntPtr*)_unmanaged.GetRawDataRef<nint>() = default;
                    return _unmanaged;
                }

#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable CA1416 // Validate platform compatibility
                switch ((_unmanaged.VarType & ~VarEnum.VT_BYREF, _managed))
                {
                    case (VarEnum.VT_VARIANT, _):
                        *(ComVariant*)_unmanaged.GetRawDataRef<nint>() = ConvertToUnmanaged(_managed);
                        break;
                    case (VarEnum.VT_I1 or VarEnum.VT_UI1, sbyte s):
                        *(sbyte*)_unmanaged.GetRawDataRef<nint>() = s;
                        break;
                    case (VarEnum.VT_I1 or VarEnum.VT_UI1, byte b):
                        *(byte*)_unmanaged.GetRawDataRef<nint>() = b;
                        break;
                    case (VarEnum.VT_I2 or VarEnum.VT_UI2, short s):
                        *(short*)_unmanaged.GetRawDataRef<nint>() = s;
                        break;
                    case (VarEnum.VT_I2 or VarEnum.VT_UI2, ushort u):
                        *(ushort*)_unmanaged.GetRawDataRef<nint>() = u;
                        break;
                    case (VarEnum.VT_I4 or VarEnum.VT_INT or VarEnum.VT_UI4 or VarEnum.VT_UINT or VarEnum.VT_ERROR, int i):
                        *(int*)_unmanaged.GetRawDataRef<nint>() = i;
                        break;
                    case (VarEnum.VT_I4 or VarEnum.VT_INT or VarEnum.VT_UI4 or VarEnum.VT_UINT or VarEnum.VT_ERROR, uint u):
                        *(uint*)_unmanaged.GetRawDataRef<nint>() = u;
                        break;
                    case (VarEnum.VT_I8 or VarEnum.VT_UI8, long l):
                        *(long*)_unmanaged.GetRawDataRef<nint>() = l;
                        break;
                    case (VarEnum.VT_I8 or VarEnum.VT_UI8, ulong ul):
                        *(ulong*)_unmanaged.GetRawDataRef<nint>() = ul;
                        break;
                    case (VarEnum.VT_R4, float f):
                        *(float*)_unmanaged.GetRawDataRef<nint>() = f;
                        break;
                    case (VarEnum.VT_R8, double d):
                        *(double*)_unmanaged.GetRawDataRef<nint>() = d;
                        break;
                    case (VarEnum.VT_DECIMAL, decimal d):
                        *(decimal*)_unmanaged.GetRawDataRef<nint>() = d;
                        break;
                    case (VarEnum.VT_BOOL, bool b):
                        *(short*)_unmanaged.GetRawDataRef<nint>() = b ? VARIANT_TRUE : VARIANT_FALSE;
                        break;
                    case (VarEnum.VT_BSTR, string str):
                        {
                            ref IntPtr bstrStorage = ref *(IntPtr*)_unmanaged.GetRawDataRef<nint>();
                            Marshal.FreeBSTR(bstrStorage);
                            bstrStorage = Marshal.StringToBSTR(str);
                            break;
                        }
                    case (VarEnum.VT_BSTR, BStrWrapper str):
                        {
                            ref IntPtr bstrStorage = ref *(IntPtr*)_unmanaged.GetRawDataRef<nint>();
                            Marshal.FreeBSTR(bstrStorage);
                            bstrStorage = Marshal.StringToBSTR(str.WrappedObject);
                            break;
                        }
                    case (VarEnum.VT_DATE, DateTime dt):
                        *(double*)_unmanaged.GetRawDataRef<nint>() = dt.ToOADate();
                        break;
                    case (VarEnum.VT_ERROR, ErrorWrapper error):
                        *(int*)_unmanaged.GetRawDataRef<nint>() = error.ErrorCode;
                        break;
                    case (VarEnum.VT_CY, CurrencyWrapper cy):
                        *(long*)_unmanaged.GetRawDataRef<nint>() = decimal.ToOACurrency(cy.WrappedObject);
                        break;
                    case (VarEnum.VT_UNKNOWN, object unkObj):
                        *(IntPtr*)_unmanaged.GetRawDataRef<nint>() = StrategyBasedComWrappers.DefaultMarshallingInstance.GetOrCreateComInterfaceForObject(unkObj, CreateComInterfaceFlags.None);
                        break;
                    default:
                        throw new ArgumentException("Invalid combination of unmanaged variant type and managed object type.", nameof(_managed));
                }
#pragma warning restore CA1416 // Validate platform compatibility
#pragma warning restore CS0618 // Type or member is obsolete

                return _unmanaged;
            }

            /// <summary>
            /// Create the managed value based on the provided unmanaged value.
            /// </summary>
            /// <returns>The managed value corresponding to the VARIANT.</returns>
            public object? ToManaged() => ConvertToManaged(_unmanaged);

            /// <summary>
            /// Free all resources owned by the marshaller.
            /// </summary>
            public void Free() => _unmanaged.Dispose();
        }
    }
}
