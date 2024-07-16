// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

#pragma warning disable CA1416 // COM interop is only supported on Windows

namespace System
{
    // Contains code for built-in marshalling of OLE VARIANT.
    internal static partial class Variant
    {
        internal static bool IsSystemDrawingColor(Type type) => type.FullName == "System.Drawing.Color"; // Matches the behavior of IsTypeRefOrDef

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "Variant_ConvertSystemColorToOleColor")]
        internal static partial uint ConvertSystemColorToOleColor(ObjectHandleOnStack obj);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "Variant_ConvertOleColorToSystemColor")]
        internal static partial void ConvertOleColorToSystemColor(ObjectHandleOnStack objret, uint value, IntPtr pMT);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "Variant_ConvertValueTypeToRecord")]
        private static partial void ConvertValueTypeToRecord(ObjectHandleOnStack obj, out ComVariant pOle);

        internal static ComVariant GetIUnknownOrIDispatchFromObject(object? obj)
        {
            IntPtr pUnk = GetIUnknownOrIDispatchForObject(ObjectHandleOnStack.Create(ref obj), out bool isIDispatch);
            return ComVariant.CreateRaw(isIDispatch ? VarEnum.VT_DISPATCH : VarEnum.VT_UNKNOWN, pUnk);
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "MarshalNative_GetIUnknownOrIDispatchForObject")]
        private static partial IntPtr GetIUnknownOrIDispatchForObject(ObjectHandleOnStack o, [MarshalAs(UnmanagedType.Bool)] out bool isIDispatch);

        private static object? GetObjectFromIUnknown(IntPtr pUnk)
        {
            return pUnk == IntPtr.Zero ? null : Marshal.GetObjectForIUnknown(pUnk);
        }

        private static unsafe object? ConvertWrappedObject(object? wrapped)
        {
            // Historically, for UnknownWrapper and DispatchWrapper, the wrapped object is passed
            // into Variant.SetFieldsObject, and the result set in objRef field is used for
            // IUnknown/IDispatch marshalling. Here the behavior is simulated.

            if (wrapped is ValueType)
            {
                // Enums are stored with underlying value in number bits, and type in objRef field.
                if (wrapped is Enum)
                    return wrapped.GetType();

                // Primitive types (ELEMENT_TYPE_BOOLEAN through ELEMENT_TYPE_STRING, IntPtr/UIntPtr
                // not included) don't have objRef set and become null.
                if (wrapped is IntPtr or UIntPtr)
                    return wrapped;

                if (wrapped.GetType().IsPrimitive)
                    return null;

                // System.Drawing.Color is converted to UInt32.
                if (IsSystemDrawingColor(wrapped.GetType()))
                    return null;

                // DateTime, TimeSpan and Currency are stored with corresponding types with
                // objectRef unset.
                if (wrapped is DateTime or TimeSpan or Currency)
                    return null;

                // Other value types are boxed as-is.
                return wrapped;
            }
            else
            {
                // Empty is stored with null objRef.
                // DBNull and Missing are stored with corresponding types, with objRef also set.
                if (wrapped is Empty)
                    return null;

                return wrapped;
            }
        }

        // Helper code for marshaling managed objects to VARIANT's
        internal static void MarshalHelperConvertObjectToVariant(object? o, out ComVariant pOle)
        {
            // Cases handled at native side: string, bool, primitives including (U)IntPtr excluding U(Int)64, array

            switch (o)
            {
                case null:
                    pOle = default;
                    break;

                case IConvertible ic when ic.GetTypeCode() != TypeCode.Object:
                    {
                        IFormatProvider provider = CultureInfo.InvariantCulture;
                        pOle = ic.GetTypeCode() switch
                        {
                            TypeCode.Empty => default,
                            TypeCode.DBNull => ComVariant.Create(DBNull.Value),
                            TypeCode.Boolean => ComVariant.Create(ic.ToBoolean(provider)),
                            TypeCode.Char => ComVariant.Create((ushort)ic.ToChar(provider)),
                            TypeCode.SByte => ComVariant.Create(ic.ToSByte(provider)),
                            TypeCode.Byte => ComVariant.Create(ic.ToByte(provider)),
                            TypeCode.Int16 => ComVariant.Create(ic.ToInt16(provider)),
                            TypeCode.UInt16 => ComVariant.Create(ic.ToUInt16(provider)),
                            TypeCode.Int32 => ComVariant.Create(ic.ToInt32(provider)),
                            TypeCode.UInt32 => ComVariant.Create(ic.ToUInt32(provider)),
                            TypeCode.Int64 => ComVariant.Create(ic.ToInt64(provider)),
                            TypeCode.UInt64 => ComVariant.Create(ic.ToUInt64(provider)),
                            TypeCode.Single => ComVariant.Create(ic.ToSingle(provider)),
                            TypeCode.Double => ComVariant.Create(ic.ToDouble(provider)),
                            TypeCode.Decimal => ComVariant.Create(ic.ToDecimal(provider)),
                            TypeCode.DateTime => ComVariant.Create(ic.ToDateTime(provider)),
                            TypeCode.String => ComVariant.Create(ic.ToString(provider)),
                            _ => throw new NotSupportedException(SR.Format(SR.NotSupported_UnknownTypeCode, ic.GetTypeCode())),
                        };
                        break;
                    }

                case Missing:
                    pOle = ComVariant.CreateRaw(VarEnum.VT_ERROR, HResults.DISP_E_PARAMNOTFOUND);
                    break;

                // Array handled by native side

                case UnknownWrapper wrapper:
                    {
                        object? wrapped = ConvertWrappedObject(wrapper.WrappedObject);
                        pOle = ComVariant.CreateRaw(VarEnum.VT_UNKNOWN,
                            wrapped is null ? IntPtr.Zero : Marshal.GetIUnknownForObject(wrapped));
                        break;
                    }
                case DispatchWrapper wrapper:
                    {
                        object? wrapped = ConvertWrappedObject(wrapper.WrappedObject);
                        pOle = ComVariant.CreateRaw(VarEnum.VT_DISPATCH,
                            wrapped is null ? IntPtr.Zero : Marshal.GetIDispatchForObject(wrapped));
                        break;
                    }

                case ErrorWrapper wrapper:
                    pOle = ComVariant.Create(wrapper);
                    break;
#pragma warning disable 0618 // CurrencyWrapper is obsolete
                case CurrencyWrapper wrapper:
                    pOle = ComVariant.Create(wrapper);
                    break;
#pragma warning restore 0618
                case BStrWrapper wrapper:
                    pOle = ComVariant.Create(wrapper);
                    break;

                case Empty:
                    pOle = default;
                    break;
                case DBNull:
                    pOle = ComVariant.Create(DBNull.Value);
                    break;

                case { } when IsSystemDrawingColor(o.GetType()):
                    // System.Drawing.Color is converted to UInt32
                    pOle = ComVariant.Create(ConvertSystemColorToOleColor(ObjectHandleOnStack.Create(ref o)));
                    break;

                // DateTime, decimal handled by IConvertible case

                case TimeSpan:
                    throw new ArgumentException(SR.ComVariant_UnsupportedSignature);
                case Currency c:
                    pOle = ComVariant.CreateRaw(VarEnum.VT_CY, c.m_value);
                    break;

                // Enums handled by IConvertible case

                case ValueType:
                    ConvertValueTypeToRecord(ObjectHandleOnStack.Create(ref o), out pOle);
                    break;

                // SafeHandle's or CriticalHandle's cannot be stored in VARIANT's.
                case SafeHandle:
                    throw new ArgumentException(SR.ComVariant_SafeHandle_In_Variant);
                case CriticalHandle:
                    throw new ArgumentException(SR.ComVariant_CriticalHandle_In_Variant);

                // VariantWrappers cannot be stored in VARIANT's.
                case VariantWrapper:
                    throw new ArgumentException(SR.ComVariant_VariantWrapper_In_Variant);

                default:
                    // We are dealing with a normal object (not a wrapper) so we will
                    // leave the VT as VT_DISPATCH for now and we will determine the actual
                    // VT when we convert the object to a COM IP.
                    pOle = GetIUnknownOrIDispatchFromObject(o);
                    break;
            }
        }

        // Helper code for marshaling VARIANTS to managed objects
        internal static unsafe object? MarshalHelperConvertVariantToObject(ref readonly ComVariant pOle)
        {
            // Invalid and common types are handled at native side
            Debug.Assert((pOle.VarType & VarEnum.VT_ARRAY) == 0, "Array should be handled at native side.");
            Debug.Assert((pOle.VarType & ~VarEnum.VT_BYREF) != VarEnum.VT_RECORD, "Records should be handled at native side.");

            switch (pOle.VarType)
            {
                case VarEnum.VT_I4:
                case VarEnum.VT_INT:
                    return pOle.As<int>();
                case VarEnum.VT_BYREF | VarEnum.VT_I4:
                case VarEnum.VT_BYREF | VarEnum.VT_INT:
                    return *(int*)pOle.GetRawDataRef<IntPtr>();

                case VarEnum.VT_UI4:
                case VarEnum.VT_UINT:
                    return pOle.As<uint>();
                case VarEnum.VT_BYREF | VarEnum.VT_UI4:
                case VarEnum.VT_BYREF | VarEnum.VT_UINT:
                    return *(uint*)pOle.GetRawDataRef<IntPtr>();

                case VarEnum.VT_I1:
                    return pOle.As<sbyte>();
                case VarEnum.VT_BYREF | VarEnum.VT_I1:
                    return *(sbyte*)pOle.GetRawDataRef<IntPtr>();

                case VarEnum.VT_UI1:
                    return pOle.As<byte>();
                case VarEnum.VT_BYREF | VarEnum.VT_UI1:
                    return *(byte*)pOle.GetRawDataRef<IntPtr>();

                case VarEnum.VT_I2:
                    return pOle.As<short>();
                case VarEnum.VT_BYREF | VarEnum.VT_I2:
                    return *(short*)pOle.GetRawDataRef<IntPtr>();

                case VarEnum.VT_UI2:
                    return pOle.As<ushort>();
                case VarEnum.VT_BYREF | VarEnum.VT_UI2:
                    return *(ushort*)pOle.GetRawDataRef<IntPtr>();

                case VarEnum.VT_I8:
                    return pOle.As<long>();
                case VarEnum.VT_BYREF | VarEnum.VT_I8:
                    return *(long*)pOle.GetRawDataRef<IntPtr>();

                case VarEnum.VT_UI8:
                    return pOle.As<ulong>();
                case VarEnum.VT_BYREF | VarEnum.VT_UI8:
                    return *(ulong*)pOle.GetRawDataRef<IntPtr>();

                case VarEnum.VT_R4:
                    return pOle.As<float>();
                case VarEnum.VT_BYREF | VarEnum.VT_R4:
                    return *(float*)pOle.GetRawDataRef<IntPtr>();

                case VarEnum.VT_R8:
                    return pOle.As<double>();
                case VarEnum.VT_BYREF | VarEnum.VT_R8:
                    return *(double*)pOle.GetRawDataRef<IntPtr>();

                case VarEnum.VT_BOOL:
                    return pOle.As<bool>();
                case VarEnum.VT_BYREF | VarEnum.VT_BOOL:
                    // VARIANT_BOOL is 2 bytes
                    return *(short*)pOle.GetRawDataRef<IntPtr>() != 0;

                case VarEnum.VT_BSTR:
                    return pOle.As<string>();

                case VarEnum.VT_BYREF | VarEnum.VT_BSTR:
                    IntPtr bstr = *(IntPtr*)pOle.GetRawDataRef<IntPtr>();
                    return bstr == IntPtr.Zero ? null : Marshal.PtrToStringBSTR(bstr);

                case VarEnum.VT_EMPTY:
                    return null;

                case VarEnum.VT_BYREF | VarEnum.VT_EMPTY:
#if TARGET_64BIT
                    return (ulong)pOle.GetRawDataRef<IntPtr>();
#else
                    return (uint)pOle.GetRawDataRef<IntPtr>();
#endif

                case VarEnum.VT_NULL:
                case VarEnum.VT_BYREF | VarEnum.VT_NULL:
                    return DBNull.Value;

                case VarEnum.VT_DATE:
                    return pOle.As<DateTime>();
                case VarEnum.VT_BYREF | VarEnum.VT_DATE:
                    return DateTime.FromOADate(*(double*)pOle.GetRawDataRef<IntPtr>());

                case VarEnum.VT_DECIMAL:
                    return pOle.As<decimal>();
                case VarEnum.VT_BYREF | VarEnum.VT_DECIMAL:
                    return *(decimal*)pOle.GetRawDataRef<IntPtr>();

                case VarEnum.VT_CY:
                    return decimal.FromOACurrency(pOle.GetRawDataRef<long>());
                case VarEnum.VT_BYREF | VarEnum.VT_CY:
                    return decimal.FromOACurrency(*(long*)pOle.GetRawDataRef<IntPtr>());

                case VarEnum.VT_UNKNOWN:
                case VarEnum.VT_DISPATCH:
                    return GetObjectFromIUnknown(pOle.GetRawDataRef<IntPtr>());
                case VarEnum.VT_BYREF | VarEnum.VT_UNKNOWN:
                case VarEnum.VT_BYREF | VarEnum.VT_DISPATCH:
                    return GetObjectFromIUnknown(*(IntPtr*)pOle.GetRawDataRef<IntPtr>());

                case VarEnum.VT_ERROR:
                    int error = pOle.GetRawDataRef<int>();
                    return error == HResults.DISP_E_PARAMNOTFOUND ? Missing.Value : error;
                case VarEnum.VT_BYREF | VarEnum.VT_ERROR:
                    int refError = *(int*)pOle.GetRawDataRef<IntPtr>();
                    return refError == HResults.DISP_E_PARAMNOTFOUND ? Missing.Value : refError;

                case VarEnum.VT_VOID:
                case VarEnum.VT_BYREF | VarEnum.VT_VOID:
                    return null; // CV_VOID

                default:
                    throw new ArgumentException(SR.ComVariant_UnsupportedType);
            }
        }

        // Helper code: on the back propagation path where a VT_BYREF VARIANT*
        // is marshaled to a "ref Object", we use this helper to force the
        // updated object back to the original type.
        internal static void MarshalHelperCastVariant(object pValue, int vt, out ComVariant v)
        {
            Debug.Assert((VarEnum)vt != VarEnum.VT_VARIANT, "Should be handled at native side.");

            if (pValue is not IConvertible iv)
            {
                switch ((VarEnum)vt)
                {
                    case VarEnum.VT_DISPATCH:
                        Debug.Assert(OperatingSystem.IsWindows());
                        v = ComVariant.CreateRaw(VarEnum.VT_DISPATCH,
                            pValue is null ? IntPtr.Zero : Marshal.GetIDispatchForObject(pValue));
                        break;

                    case VarEnum.VT_UNKNOWN:
                        v = ComVariant.CreateRaw(VarEnum.VT_UNKNOWN,
                            pValue is null ? IntPtr.Zero : Marshal.GetIUnknownForObject(pValue));
                        break;

                    case VarEnum.VT_RECORD:
                        MarshalHelperConvertObjectToVariant(pValue, out v);
                        if (v.VarType != VarEnum.VT_RECORD)
                        {
                            // v can hold disposable content like BSTR
                            v.Dispose();
                            throw new InvalidCastException(SR.InvalidCast_CannotCoerceByRefVariant);
                        }
                        break;

                    case VarEnum.VT_BSTR: /*VT_BSTR*/
                        if (pValue == null)
                        {
                            v = ComVariant.CreateRaw(VarEnum.VT_BSTR, IntPtr.Zero);
                            break;
                        }
                        goto default;

                    default:
                        throw new InvalidCastException(SR.InvalidCast_CannotCoerceByRefVariant);
                }
            }
            else
            {
                IFormatProvider provider = CultureInfo.InvariantCulture;
                v = (VarEnum)vt switch
                {
                    VarEnum.VT_EMPTY => default,
                    VarEnum.VT_NULL => ComVariant.Create(DBNull.Value),
                    VarEnum.VT_I2 => ComVariant.Create(iv.ToInt16(provider)),
                    VarEnum.VT_I4 => ComVariant.Create(iv.ToInt32(provider)),
                    VarEnum.VT_R4 => ComVariant.Create(iv.ToSingle(provider)),
                    VarEnum.VT_R8 => ComVariant.Create(iv.ToDouble(provider)),
                    VarEnum.VT_CY => ComVariant.CreateRaw(VarEnum.VT_CY, decimal.ToOACurrency(iv.ToDecimal(provider))),
                    VarEnum.VT_DATE => ComVariant.Create(iv.ToDateTime(provider)),
                    VarEnum.VT_BSTR => ComVariant.Create(iv.ToString(provider)),
                    VarEnum.VT_DISPATCH => ComVariant.CreateRaw(VarEnum.VT_DISPATCH, Marshal.GetIDispatchForObject(iv)),
                    VarEnum.VT_ERROR => ComVariant.CreateRaw(VarEnum.VT_ERROR, iv.ToInt32(provider)),
                    VarEnum.VT_BOOL => ComVariant.Create(iv.ToBoolean(provider)),
                    VarEnum.VT_UNKNOWN => ComVariant.CreateRaw(VarEnum.VT_UNKNOWN, Marshal.GetIUnknownForObject(iv)),
                    VarEnum.VT_DECIMAL => ComVariant.Create(iv.ToDecimal(provider)),
                    // 15 => : /*unused*/ NOT SUPPORTED
                    VarEnum.VT_I1 => ComVariant.Create(iv.ToSByte(provider)),
                    VarEnum.VT_UI1 => ComVariant.Create(iv.ToByte(provider)),
                    VarEnum.VT_UI2 => ComVariant.Create(iv.ToUInt16(provider)),
                    VarEnum.VT_UI4 => ComVariant.Create(iv.ToUInt32(provider)),
                    VarEnum.VT_I8 => ComVariant.Create(iv.ToInt64(provider)),
                    VarEnum.VT_UI8 => ComVariant.Create(iv.ToUInt64(provider)),
                    VarEnum.VT_INT => ComVariant.Create(iv.ToInt32(provider)),
                    VarEnum.VT_UINT => ComVariant.Create(iv.ToUInt32(provider)),
                    _ => throw new InvalidCastException(SR.InvalidCast_CannotCoerceByRefVariant),
                };
            }
        }
    }
}
