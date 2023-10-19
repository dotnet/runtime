// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.Marshalling;
using System.Runtime.Versioning;

namespace System.Runtime.InteropServices
{
    [SupportedOSPlatform("windows")]
    internal static class BuiltInInteropVariantExtensions
    {
        private static unsafe ref T GetByRefDataRef<T>(this ref ComVariant variant)
            where T : unmanaged
        {
            Debug.Assert(variant.VarType.HasFlag(VarEnum.VT_BYREF));
            return ref Unsafe.AsRef<T>((void*)variant.GetRawDataRef<nint>());
        }

        public static unsafe void CopyFromIndirect(this ref ComVariant variant, object value)
        {
            VarEnum vt = (VarEnum)(((int)variant.VarType) & ~((int)VarEnum.VT_BYREF));

            if (value == null)
            {
                if (vt == VarEnum.VT_DISPATCH || vt == VarEnum.VT_UNKNOWN || vt == VarEnum.VT_BSTR)
                {
                    variant.GetRawDataRef<IntPtr>() = IntPtr.Zero;
                }
                return;
            }

            if ((vt & VarEnum.VT_ARRAY) != 0)
            {
                ComVariant vArray;
                Marshal.GetNativeVariantForObject(value, (IntPtr)(void*)&vArray);
                variant.GetRawDataRef<IntPtr>() = vArray.GetRawDataRef<IntPtr>();
                return;
            }

            switch (vt)
            {
                case VarEnum.VT_I1:
                    variant.GetByRefDataRef<sbyte>() = (sbyte)value;
                    break;

                case VarEnum.VT_UI1:
                    variant.GetByRefDataRef<byte>() = (byte)value;
                    break;

                case VarEnum.VT_I2:
                    variant.GetByRefDataRef<short>() = (short)value;
                    break;

                case VarEnum.VT_UI2:
                    variant.GetByRefDataRef<ushort>() = (ushort)value;
                    break;

                case VarEnum.VT_BOOL:
                    // VARIANT_TRUE  = -1
                    // VARIANT_FALSE = 0
                    variant.GetByRefDataRef<short>() = (bool)value ? (short)-1 : (short)0;
                    break;

                case VarEnum.VT_I4:
                case VarEnum.VT_INT:
                    variant.GetByRefDataRef<int>() = (int)value;
                    break;

                case VarEnum.VT_UI4:
                case VarEnum.VT_UINT:
                    variant.GetByRefDataRef<uint>() = (uint)value;
                    break;

                case VarEnum.VT_ERROR:
                    variant.GetByRefDataRef<int>() = ((ErrorWrapper)value).ErrorCode;
                    break;

                case VarEnum.VT_I8:
                    variant.GetByRefDataRef<long>() = (long)value;
                    break;

                case VarEnum.VT_UI8:
                    variant.GetByRefDataRef<ulong>() = (ulong)value;
                    break;

                case VarEnum.VT_R4:
                    variant.GetByRefDataRef<float>() = (float)value;
                    break;

                case VarEnum.VT_R8:
                    variant.GetByRefDataRef<double>() = (double)value;
                    break;

                case VarEnum.VT_DATE:
                    variant.GetByRefDataRef<double>() = ((DateTime)value).ToOADate();
                    break;

                case VarEnum.VT_UNKNOWN:
                    variant.GetByRefDataRef<IntPtr>() = Marshal.GetIUnknownForObject(value);
                    break;

                case VarEnum.VT_DISPATCH:
                    variant.GetByRefDataRef<IntPtr>() = Marshal.GetIDispatchForObject(value);
                    break;

                case VarEnum.VT_BSTR:
                    variant.GetByRefDataRef<IntPtr>() = Marshal.StringToBSTR((string)value);
                    break;

                case VarEnum.VT_CY:
                    variant.GetByRefDataRef<long>() = decimal.ToOACurrency((decimal)value);
                    break;

                case VarEnum.VT_DECIMAL:
                    variant.GetByRefDataRef<decimal>() = (decimal)value;
                    break;

                case VarEnum.VT_VARIANT:
                    Marshal.GetNativeVariantForObject(value, variant.GetRawDataRef<IntPtr>());
                    break;

                default:
                    throw new ArgumentException();
            }
        }

        /// <summary>
        /// Get the managed object representing the Variant.
        /// </summary>
        /// <returns></returns>
        public static object? ToObject(this ref ComVariant variant)
        {
            // Check the simple case upfront
            if (variant.VarType == VarEnum.VT_EMPTY)
            {
                return null;
            }

            switch (variant.VarType)
            {
                case VarEnum.VT_NULL:
                    return DBNull.Value;

                case VarEnum.VT_I1: return variant.As<sbyte>();
                case VarEnum.VT_I2: return variant.As<short>();
                case VarEnum.VT_I4: return variant.As<int>();
                case VarEnum.VT_I8: return variant.As<long>();
                case VarEnum.VT_UI1: return variant.As<byte>();
                case VarEnum.VT_UI2: return variant.As<ushort>();
                case VarEnum.VT_UI4: return variant.As<uint>();
                case VarEnum.VT_UI8: return variant.As<ulong>();
                case VarEnum.VT_INT: return variant.As<int>();
                case VarEnum.VT_UINT: return variant.As<uint>();
                case VarEnum.VT_BOOL: return variant.As<short>() != -1;
                case VarEnum.VT_ERROR: return variant.As<int>();
                case VarEnum.VT_R4: return variant.As<float>();
                case VarEnum.VT_R8: return variant.As<double>();
                case VarEnum.VT_DECIMAL: return variant.As<decimal>();
                case VarEnum.VT_CY: return decimal.FromOACurrency(variant.GetRawDataRef<long>());
                case VarEnum.VT_DATE: return variant.As<DateTime>();
                case VarEnum.VT_BSTR: return Marshal.PtrToStringBSTR(variant.GetRawDataRef<nint>());
                case VarEnum.VT_UNKNOWN: return Marshal.GetObjectForIUnknown(variant.GetRawDataRef<nint>());
                case VarEnum.VT_DISPATCH: return Marshal.GetObjectForIUnknown(variant.GetRawDataRef<nint>());

                default:
                    unsafe
                    {
                        fixed (void* pThis = &variant)
                        {
                            return Marshal.GetObjectForNativeVariant((nint)pThis);
                        }
                    }
            }
        }
    }
}
