// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Microsoft.CSharp.RuntimeBinder.ComInterop;

namespace Microsoft.CSharp.RuntimeBinder.ComInterop
{
    internal static class DynamicVariantExtensions
    {
        /// <summary>
        /// Primitive types are the basic COM types. It includes valuetypes like ints, but also reference types
        /// like BStrs. It does not include composite types like arrays and user-defined COM types (IUnknown/IDispatch).
        /// </summary>
        public static bool IsPrimitiveType(this VarEnum varEnum)
        {
            switch (varEnum)
            {
                case VarEnum.VT_I1:
                case VarEnum.VT_I2:
                case VarEnum.VT_I4:
                case VarEnum.VT_I8:
                case VarEnum.VT_UI1:
                case VarEnum.VT_UI2:
                case VarEnum.VT_UI4:
                case VarEnum.VT_UI8:
                case VarEnum.VT_INT:
                case VarEnum.VT_UINT:
                case VarEnum.VT_BOOL:
                case VarEnum.VT_ERROR:
                case VarEnum.VT_R4:
                case VarEnum.VT_R8:
                case VarEnum.VT_DECIMAL:
                case VarEnum.VT_CY:
                case VarEnum.VT_DATE:
                case VarEnum.VT_BSTR:
                    return true;
            }

            return false;
        }

        public static void SetAsIConvertible(this ref ComVariant variant, IConvertible value)
        {
            Debug.Assert(variant.VarType == VarEnum.VT_EMPTY); // The setter can only be called once as VariantClear might be needed otherwise

            TypeCode tc = value.GetTypeCode();
            CultureInfo ci = CultureInfo.CurrentCulture;

            switch (tc)
            {
                case TypeCode.Empty: break;
                case TypeCode.Object: variant = ComVariant.CreateRaw(VarEnum.VT_UNKNOWN, Marshal.GetIUnknownForObject(value)); break;
                case TypeCode.DBNull: variant = ComVariant.Null; break;
                case TypeCode.Boolean: variant = ComVariant.Create<bool>(value.ToBoolean(ci)); break;
                case TypeCode.Char: variant = ComVariant.Create<ushort>(value.ToChar(ci)); break;
                case TypeCode.SByte: variant = ComVariant.Create<sbyte>(value.ToSByte(ci)); break;
                case TypeCode.Byte: variant = ComVariant.Create<byte>(value.ToByte(ci)); break;
                case TypeCode.Int16: variant = ComVariant.Create(value.ToInt16(ci)); break;
                case TypeCode.UInt16: variant = ComVariant.Create(value.ToUInt16(ci)); break;
                case TypeCode.Int32: variant = ComVariant.Create(value.ToInt32(ci)); break;
                case TypeCode.UInt32: variant = ComVariant.Create(value.ToUInt32(ci)); break;
                case TypeCode.Int64: variant = ComVariant.Create(value.ToInt64(ci)); break;
                case TypeCode.UInt64: variant = ComVariant.Create(value.ToInt64(ci)); break;
                case TypeCode.Single: variant = ComVariant.Create(value.ToSingle(ci)); break;
                case TypeCode.Double: variant = ComVariant.Create(value.ToDouble(ci)); break;
                case TypeCode.Decimal: variant = ComVariant.Create(value.ToDecimal(ci)); break;
                case TypeCode.DateTime: variant = ComVariant.Create(value.ToDateTime(ci)); break;
                case TypeCode.String: variant = ComVariant.Create(new BStrWrapper(value.ToString(ci))); break;

                default:
                    throw new NotSupportedException();
            }
        }
        // VT_I1

        public static void SetAsByrefI1(ref this ComVariant variant, ref sbyte value)
        {
            variant.SetAsByref(ref value, VarEnum.VT_I1);
        }

        // VT_I2

        public static void SetAsByrefI2(ref this ComVariant variant, ref short value)
        {
            variant.SetAsByref(ref value, VarEnum.VT_I2);
        }

        // VT_I4

        public static void SetAsByrefI4(ref this ComVariant variant, ref int value)
        {
            variant.SetAsByref(ref value, VarEnum.VT_I4);
        }

        // VT_I8

        public static void SetAsByrefI8(ref this ComVariant variant, ref long value)
        {
            variant.SetAsByref(ref value, VarEnum.VT_I8);
        }

        // VT_UI1

        public static void SetAsByrefUi1(ref this ComVariant variant, ref byte value)
        {
            variant.SetAsByref(ref value, VarEnum.VT_UI1);
        }

        // VT_UI2

        public static void SetAsByrefUi2(ref this ComVariant variant, ref ushort value)
        {
            variant.SetAsByref(ref value, VarEnum.VT_UI2);
        }

        // VT_UI4

        public static void SetAsByrefUi4(ref this ComVariant variant, ref uint value)
        {
            variant.SetAsByref(ref value, VarEnum.VT_UI4);
        }

        // VT_UI8

        public static void SetAsByrefUi8(ref this ComVariant variant, ref ulong value)
        {
            variant.SetAsByref(ref value, VarEnum.VT_UI8);
        }

        // VT_INT

        public static void SetAsByrefInt(ref this ComVariant variant, ref int value)
        {
            variant.SetAsByref(ref value, VarEnum.VT_INT);
        }

        // VT_UINT

        public static void SetAsByrefUint(ref this ComVariant variant, ref uint value)
        {
            variant.SetAsByref(ref value, VarEnum.VT_UINT);
        }

        // VT_BOOL

        public static void SetAsByrefBool(ref this ComVariant variant, ref short value)
        {
            variant.SetAsByref(ref value, VarEnum.VT_BOOL);
        }

        // VT_ERROR

        public static void SetAsByrefError(ref this ComVariant variant, ref int value)
        {
            variant.SetAsByref(ref value, VarEnum.VT_ERROR);
        }

        // VT_R4

        public static void SetAsByrefR4(ref this ComVariant variant, ref float value)
        {
            variant.SetAsByref(ref value, VarEnum.VT_R4);
        }

        // VT_R8

        public static void SetAsByrefR8(ref this ComVariant variant, ref double value)
        {
            variant.SetAsByref(ref value, VarEnum.VT_R8);
        }

        // VT_DECIMAL

        public static void SetAsByrefDecimal(ref this ComVariant variant, ref decimal value)
        {
            variant.SetAsByref(ref value, VarEnum.VT_DECIMAL);
        }

        // VT_CY

        public static void SetAsByrefCy(ref this ComVariant variant, ref long value)
        {
            variant.SetAsByref(ref value, VarEnum.VT_CY);
        }

        // VT_DATE

        public static void SetAsByrefDate(ref this ComVariant variant, ref double value)
        {
            variant.SetAsByref(ref value, VarEnum.VT_DATE);
        }

        // VT_BSTR

        public static void SetAsByrefBstr(ref this ComVariant variant, ref IntPtr value)
        {
            variant.SetAsByref(ref value, VarEnum.VT_BSTR);
        }

        // VT_UNKNOWN

        public static void SetAsByrefUnknown(ref this ComVariant variant, ref IntPtr value)
        {
            variant.SetAsByref(ref value, VarEnum.VT_UNKNOWN);
        }

        // VT_DISPATCH

        public static void SetAsByrefDispatch(ref this ComVariant variant, ref IntPtr value)
        {
            variant.SetAsByref(ref value, VarEnum.VT_DISPATCH);
        }

        private static unsafe void SetAsByref<T>(ref this ComVariant variant, ref T value, VarEnum type)
        {
            Debug.Assert(variant.VarType == VarEnum.VT_EMPTY); // The setter can only be called once as VariantClear might be needed otherwise
            variant = ComVariant.CreateRaw(type | VarEnum.VT_BYREF, (nint)Unsafe.AsPointer(ref value));
        }

        public static void SetAsByrefVariant(ref this ComVariant variant, ref ComVariant value)
        {
            variant.SetAsByref(ref value, VarEnum.VT_VARIANT);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Record
        {
            public IntPtr _record;
            public IntPtr _recordInfo;
        }

        // constructs a ByRef variant to pass contents of another variant ByRef.
        public static unsafe void SetAsByrefVariantIndirect(ref this ComVariant variant, ref ComVariant value)
        {
            Debug.Assert(variant.VarType == VarEnum.VT_EMPTY); // The setter can only be called once as VariantClear might be needed otherwise
            Debug.Assert((value.VarType & VarEnum.VT_BYREF) == 0, "double indirection");

            switch (value.VarType)
            {
                case VarEnum.VT_EMPTY:
                case VarEnum.VT_NULL:
                    // these cannot combine with VT_BYREF. Should try passing as a variant reference
                    variant.SetAsByrefVariant(ref value);
                    return;
                case VarEnum.VT_RECORD:
                    // VT_RECORD's are weird in that regardless of is the VT_BYREF flag is set or not
                    // they have the same internal representation.
                    variant = ComVariant.CreateRaw(value.VarType | VarEnum.VT_BYREF, value.GetRawDataRef<Record>());
                    break;
                case VarEnum.VT_DECIMAL:
                    // The DECIMAL value in an OLE Variant is stored at the start of the structure.
                    variant = ComVariant.CreateRaw(value.VarType | VarEnum.VT_BYREF, (nint)Unsafe.AsPointer(ref value));
                    break;
                default:
                    variant = ComVariant.CreateRaw(value.VarType | VarEnum.VT_BYREF, (nint)Unsafe.AsPointer(ref value.GetRawDataRef<nint>()));
                    break;
            }
        }

        internal static System.Reflection.MethodInfo GetByrefSetter(VarEnum varType)
        {
            switch (varType)
            {
                case VarEnum.VT_I1: return typeof(DynamicVariantExtensions).GetMethod(nameof(SetAsByrefI1));
                case VarEnum.VT_I2: return typeof(DynamicVariantExtensions).GetMethod(nameof(SetAsByrefI2));
                case VarEnum.VT_I4: return typeof(DynamicVariantExtensions).GetMethod(nameof(SetAsByrefI4));
                case VarEnum.VT_I8: return typeof(DynamicVariantExtensions).GetMethod(nameof(SetAsByrefI8));
                case VarEnum.VT_UI1: return typeof(DynamicVariantExtensions).GetMethod(nameof(SetAsByrefUi1));
                case VarEnum.VT_UI2: return typeof(DynamicVariantExtensions).GetMethod(nameof(SetAsByrefUi2));
                case VarEnum.VT_UI4: return typeof(DynamicVariantExtensions).GetMethod(nameof(SetAsByrefUi4));
                case VarEnum.VT_UI8: return typeof(DynamicVariantExtensions).GetMethod(nameof(SetAsByrefUi8));
                case VarEnum.VT_INT: return typeof(DynamicVariantExtensions).GetMethod(nameof(SetAsByrefInt));
                case VarEnum.VT_UINT: return typeof(DynamicVariantExtensions).GetMethod(nameof(SetAsByrefUint));
                case VarEnum.VT_BOOL: return typeof(DynamicVariantExtensions).GetMethod(nameof(SetAsByrefBool));
                case VarEnum.VT_ERROR: return typeof(DynamicVariantExtensions).GetMethod(nameof(SetAsByrefError));
                case VarEnum.VT_R4: return typeof(DynamicVariantExtensions).GetMethod(nameof(SetAsByrefR4));
                case VarEnum.VT_R8: return typeof(DynamicVariantExtensions).GetMethod(nameof(SetAsByrefR8));
                case VarEnum.VT_DECIMAL: return typeof(DynamicVariantExtensions).GetMethod(nameof(SetAsByrefDecimal));
                case VarEnum.VT_CY: return typeof(DynamicVariantExtensions).GetMethod(nameof(SetAsByrefCy));
                case VarEnum.VT_DATE: return typeof(DynamicVariantExtensions).GetMethod(nameof(SetAsByrefDate));
                case VarEnum.VT_BSTR: return typeof(DynamicVariantExtensions).GetMethod(nameof(SetAsByrefBstr));
                case VarEnum.VT_UNKNOWN: return typeof(DynamicVariantExtensions).GetMethod(nameof(SetAsByrefUnknown));
                case VarEnum.VT_DISPATCH: return typeof(DynamicVariantExtensions).GetMethod(nameof(SetAsByrefDispatch));

                case VarEnum.VT_VARIANT:
                    return typeof(DynamicVariantExtensions).GetMethod(nameof(SetAsByrefVariant));
                case VarEnum.VT_RECORD:
                case VarEnum.VT_ARRAY:
                    return typeof(DynamicVariantExtensions).GetMethod(nameof(SetAsByrefVariantIndirect));

                default:
                    throw new NotSupportedException();
            }
        }

        public static void SetI1(this ref ComVariant variant, sbyte value)
        {
            variant = ComVariant.Create(value);
        }

        public static void SetUi1(this ref ComVariant variant, byte value)
        {
            variant = ComVariant.Create(value);
        }

        public static void SetI2(this ref ComVariant variant, short value)
        {
            variant = ComVariant.Create(value);
        }

        public static void SetUi2(this ref ComVariant variant, ushort value)
        {
            variant = ComVariant.Create(value);
        }

        public static void SetI4(this ref ComVariant variant, int value)
        {
            variant = ComVariant.Create(value);
        }

        public static void SetUi4(this ref ComVariant variant, uint value)
        {
            variant = ComVariant.Create(value);
        }

        public static void SetI8(this ref ComVariant variant, long value)
        {
            variant = ComVariant.Create(value);
        }

        public static void SetUi8(this ref ComVariant variant, ulong value)
        {
            variant = ComVariant.Create(value);
        }

        public static void SetInt(this ref ComVariant variant, int value)
        {
            variant = ComVariant.CreateRaw(VarEnum.VT_INT, value);
        }

        public static void SetUint(this ref ComVariant variant, uint value)
        {
            variant = ComVariant.CreateRaw(VarEnum.VT_UINT, value);
        }

        public static void SetBool(this ref ComVariant variant, bool value)
        {
            variant = ComVariant.Create(value);
        }

        public static void SetR4(this ref ComVariant variant, float value)
        {
            variant = ComVariant.Create(value);
        }

        public static void SetR8(this ref ComVariant variant, double value)
        {
            variant = ComVariant.Create(value);
        }

        public static void SetDecimal(this ref ComVariant variant, decimal value)
        {
            variant = ComVariant.Create(value);
        }

        public static void SetDate(this ref ComVariant variant, DateTime value)
        {
            variant = ComVariant.Create(value);
        }

        public static void SetBstr(this ref ComVariant variant, string value)
        {
            variant = ComVariant.Create(new BStrWrapper(value));
        }

        public static void SetUnknown(this ref ComVariant variant, object value)
        {
            variant = ComVariant.CreateRaw(VarEnum.VT_UNKNOWN, Marshal.GetIUnknownForObject(value));
        }

        public static void SetDispatch(this ref ComVariant variant, object value)
        {
            variant = ComVariant.CreateRaw(VarEnum.VT_DISPATCH, Marshal.GetIDispatchForObject(value));
        }

        public static void SetError(this ref ComVariant variant, int value)
        {
            variant = ComVariant.CreateRaw(VarEnum.VT_ERROR, value);
        }

        public static void SetCy(this ref ComVariant variant, decimal value)
        {
            variant = ComVariant.CreateRaw(VarEnum.VT_CY, decimal.ToOACurrency(value));
        }

        public static unsafe void SetVariant(this ref ComVariant variant, object value)
        {
            Debug.Assert(variant.VarType == VarEnum.VT_EMPTY); // The setter can only be called once as VariantClear might be needed otherwise
            if (value != null)
            {
                UnsafeMethods.InitVariantForObject(value, ref variant);
            }
        }

        internal static System.Reflection.MethodInfo GetSetter(VarEnum varType)
        {
            switch (varType)
            {
                case VarEnum.VT_I1: return typeof(DynamicVariantExtensions).GetMethod(nameof(SetI1), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                case VarEnum.VT_I2: return typeof(DynamicVariantExtensions).GetMethod(nameof(SetI2), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                case VarEnum.VT_I4: return typeof(DynamicVariantExtensions).GetMethod(nameof(SetI4), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                case VarEnum.VT_I8: return typeof(DynamicVariantExtensions).GetMethod(nameof(SetI8), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                case VarEnum.VT_UI1: return typeof(DynamicVariantExtensions).GetMethod(nameof(SetUi1), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                case VarEnum.VT_UI2: return typeof(DynamicVariantExtensions).GetMethod(nameof(SetUi2), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                case VarEnum.VT_UI4: return typeof(DynamicVariantExtensions).GetMethod(nameof(SetUi4), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                case VarEnum.VT_UI8: return typeof(DynamicVariantExtensions).GetMethod(nameof(SetUi8), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                case VarEnum.VT_INT: return typeof(DynamicVariantExtensions).GetMethod(nameof(SetInt), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                case VarEnum.VT_UINT: return typeof(DynamicVariantExtensions).GetMethod(nameof(SetUint), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                case VarEnum.VT_BOOL: return typeof(DynamicVariantExtensions).GetMethod(nameof(SetBool), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                case VarEnum.VT_ERROR: return typeof(DynamicVariantExtensions).GetMethod(nameof(SetError), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                case VarEnum.VT_R4: return typeof(DynamicVariantExtensions).GetMethod(nameof(SetR4), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                case VarEnum.VT_R8: return typeof(DynamicVariantExtensions).GetMethod(nameof(SetR8), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                case VarEnum.VT_DECIMAL: return typeof(DynamicVariantExtensions).GetMethod(nameof(SetDecimal), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                case VarEnum.VT_CY: return typeof(DynamicVariantExtensions).GetMethod(nameof(SetCy), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                case VarEnum.VT_DATE: return typeof(DynamicVariantExtensions).GetMethod(nameof(SetDate), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                case VarEnum.VT_BSTR: return typeof(DynamicVariantExtensions).GetMethod(nameof(SetBstr), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                case VarEnum.VT_UNKNOWN: return typeof(DynamicVariantExtensions).GetMethod(nameof(SetUnknown), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                case VarEnum.VT_DISPATCH: return typeof(DynamicVariantExtensions).GetMethod(nameof(SetDispatch), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

                case VarEnum.VT_VARIANT:
                case VarEnum.VT_RECORD:
                case VarEnum.VT_ARRAY:
                    return typeof(DynamicVariantExtensions).GetMethod(nameof(SetVariant), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

                default:
                    throw new NotSupportedException();
            }
        }
    }
}
