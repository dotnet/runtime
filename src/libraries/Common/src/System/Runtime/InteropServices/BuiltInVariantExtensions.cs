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
        // VARIANT_BOOL constants.
        internal const short VARIANT_TRUE = -1;
        internal const short VARIANT_FALSE = 0;
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
                    variant.GetByRefDataRef<short>() = (bool)value ? VARIANT_TRUE : VARIANT_FALSE;
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
            return variant.VarType switch
            {
                VarEnum.VT_EMPTY => null,
                VarEnum.VT_NULL => DBNull.Value,
                VarEnum.VT_I1 => variant.As<sbyte>(),
                VarEnum.VT_I2 => variant.As<short>(),
                VarEnum.VT_I4 => variant.As<int>(),
                VarEnum.VT_I8 => variant.As<long>(),
                VarEnum.VT_UI1 => variant.As<byte>(),
                VarEnum.VT_UI2 => variant.As<ushort>(),
                VarEnum.VT_UI4 => variant.As<uint>(),
                VarEnum.VT_UI8 => variant.As<ulong>(),
                VarEnum.VT_INT => variant.As<int>(),
                VarEnum.VT_UINT => variant.As<uint>(),
                VarEnum.VT_BOOL => variant.As<bool>(),
                VarEnum.VT_ERROR => variant.As<int>(),
                VarEnum.VT_R4 => variant.As<float>(),
                VarEnum.VT_R8 => variant.As<double>(),
                VarEnum.VT_DECIMAL => variant.As<decimal>(),
                VarEnum.VT_CY => decimal.FromOACurrency(variant.GetRawDataRef<long>()),
                VarEnum.VT_DATE => variant.As<DateTime>(),
                VarEnum.VT_BSTR => Marshal.PtrToStringBSTR(variant.GetRawDataRef<nint>()),
                VarEnum.VT_UNKNOWN => Marshal.GetObjectForIUnknown(variant.GetRawDataRef<nint>()),
                VarEnum.VT_DISPATCH => Marshal.GetObjectForIUnknown(variant.GetRawDataRef<nint>()),
                _ => GetObjectFromNativeVariant(ref variant),
            };
        }

        private static unsafe object? GetObjectFromNativeVariant(ref ComVariant variant)
        {
            fixed (void* pVariant = &variant)
            {
                return Marshal.GetObjectForNativeVariant((nint)pVariant);
            }
        }
    }
}
