// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace System.Runtime.InteropServices.Marshalling
{
    [CustomMarshaller(typeof(object), MarshalMode.Default, typeof(OleVariantMarshaller))]
    [CustomMarshaller(typeof(object), MarshalMode.UnmanagedToManagedRef, typeof(RefPropogate))]
    public static partial class OleVariantMarshaller
    {
        public static OleVariant ConvertToUnmanaged(object? managed)
        {
            if (managed is null)
            {
                return default;
            }

#pragma warning disable CS0618 // Type or member is obsolete
            switch (managed)
            {
                case sbyte s:
                    return OleVariant.Create(s);
                case byte b:
                    return OleVariant.Create(b);
                case short s:
                    return OleVariant.Create(s);
                case ushort s:
                    return OleVariant.Create(s);
                case int i:
                    return OleVariant.Create(i);
                case uint i:
                    return OleVariant.Create(i);
                case long l:
                    return OleVariant.Create(l);
                case ulong l:
                    return OleVariant.Create(l);
                case float f:
                    return OleVariant.Create(f);
                case double d:
                    return OleVariant.Create(d);
                case decimal d:
                    return OleVariant.Create(d);
                case bool b:
                    return OleVariant.Create(b);
                case char c:
                    return OleVariant.Create((ushort)c);
                case string s:
                    return OleVariant.Create(s);
                case DateTime dt:
                    return OleVariant.Create(dt);
                case ErrorWrapper errorWrapper:
                    return OleVariant.Create(errorWrapper);
                case CurrencyWrapper currencyWrapper:
                    return OleVariant.Create(currencyWrapper);
                case BStrWrapper bStrWrapper:
                    return OleVariant.Create(bStrWrapper);
                case DBNull:
                    return OleVariant.Null;
            }
#pragma warning restore CS0618 // Type or member is obsolete

            if (TryCreateOleVariantForInterfaceWrapper(managed, out OleVariant variant))
            {
                return variant;
            }

            throw new ArgumentException("Type of managed object is not supported for marshalling as OleVariant.", nameof(managed));
        }

#pragma warning disable CA1416 // Validate platform compatibility
        private static unsafe bool TryCreateOleVariantForInterfaceWrapper(object managed, out OleVariant variant)
        {
            if (managed is UnknownWrapper uw)
            {
                object? wrapped = uw.WrappedObject;
                if (wrapped is null)
                {
                    variant = default;
                    return true;
                }
                variant = OleVariant.CreateRaw(VarEnum.VT_UNKNOWN, StrategyBasedComWrappers.DefaultMarshallingInstance.GetOrCreateComInterfaceForObject(wrapped, CreateComInterfaceFlags.None));
                return true;
            }
            else if (managed is not null && StrategyBasedComWrappers.DefaultIUnknownInterfaceDetailsStrategy.GetComExposedTypeDetails(managed.GetType().TypeHandle) is not null)
            {
                variant = OleVariant.CreateRaw(VarEnum.VT_UNKNOWN, StrategyBasedComWrappers.DefaultMarshallingInstance.GetOrCreateComInterfaceForObject(managed, CreateComInterfaceFlags.None));
                return true;
            }
            variant = default;
            return false;
        }
#pragma warning restore CA1416 // Validate platform compatibility

        public static unsafe object? ConvertToManaged(OleVariant unmanaged)
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
                    return unmanaged.As<CurrencyWrapper>().WrappedObject;
                case VarEnum.VT_UNKNOWN:
                case VarEnum.VT_DISPATCH:
                    return StrategyBasedComWrappers.DefaultMarshallingInstance.GetOrCreateObjectForComInstance(unmanaged.GetRawDataRef<nint>(), CreateObjectFlags.Unwrap);
                case VarEnum.VT_BYREF | VarEnum.VT_VARIANT:
                    return ConvertToManaged(*(OleVariant*)unmanaged.GetRawDataRef<nint>());
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
                    return *(short*)unmanaged.GetRawDataRef<nint>() != -1;
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
                    throw new ArgumentException("Type of unmanaged variant is not supported for marshalling to a managed object.", nameof(unmanaged));
            }
#pragma warning restore CA1416 // Validate platform compatibility
#pragma warning restore CS0618 // Type or member is obsolete
        }

        public static void Free(OleVariant unmanaged) => unmanaged.Dispose();

        public struct RefPropogate
        {
            private OleVariant _unmanaged;
            private object? _managed;

            public void FromUnmanaged(OleVariant unmanaged) => _unmanaged = unmanaged;
            public void FromManaged(object? managed) => _managed = managed;

            public unsafe OleVariant ToUnmanaged()
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
                        *(OleVariant*)_unmanaged.GetRawDataRef<nint>() = ConvertToUnmanaged(_managed);
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
                        *(short*)_unmanaged.GetRawDataRef<nint>() = b ? (short)0 : (short)-1;
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

            public object? ToManaged() => ConvertToManaged(_unmanaged);
            public void Free() => _unmanaged.Dispose();
        }
    }
}
