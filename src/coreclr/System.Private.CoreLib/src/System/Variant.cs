// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
**
**
** Purpose: The CLR implementation of Variant.
**
**
===========================================================*/

using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Microsoft.Win32;

#pragma warning disable CA1416 // COM interop is only supported on Windows

namespace System
{
    internal partial struct Variant
    {
        // Do Not change the order of these fields.
        // They are mapped to the native VariantData * data structure.
        private object? _objref;
        private long _data;
        private int _flags;

        // The following bits have been taken up as follows
        // bits 0-15    - Type code
        // bit  16      - Array
        // bits 19-23   - Enums
        // bits 24-31   - Optional VT code (for roundtrip VT preservation)

        // What are the consequences of making this an enum?
        ///////////////////////////////////////////////////////////////////////
        // If you update this, update the corresponding stuff in OAVariantLib.cs,
        // OAVariant.cpp (2 tables, forwards and reverse), and perhaps OleVariant.h
        ///////////////////////////////////////////////////////////////////////
        internal const int CV_EMPTY = 0x0;
        internal const int CV_VOID = 0x1;
        internal const int CV_BOOLEAN = 0x2;
        internal const int CV_CHAR = 0x3;
        internal const int CV_I1 = 0x4;
        internal const int CV_U1 = 0x5;
        internal const int CV_I2 = 0x6;
        internal const int CV_U2 = 0x7;
        internal const int CV_I4 = 0x8;
        internal const int CV_U4 = 0x9;
        internal const int CV_I8 = 0xa;
        internal const int CV_U8 = 0xb;
        internal const int CV_R4 = 0xc;
        internal const int CV_R8 = 0xd;
        internal const int CV_STRING = 0xe;
        internal const int CV_PTR = 0xf;
        internal const int CV_DATETIME = 0x10;
        internal const int CV_TIMESPAN = 0x11;
        internal const int CV_OBJECT = 0x12;
        internal const int CV_DECIMAL = 0x13;
        internal const int CV_ENUM = 0x15;
        internal const int CV_MISSING = 0x16;
        internal const int CV_NULL = 0x17;
        internal const int CV_LAST = 0x18;

        internal const int TypeCodeBitMask = 0xffff;
        internal const int VTBitMask = unchecked((int)0xff000000);
        internal const int VTBitShift = 24;
        internal const int ArrayBitMask = 0x10000;

        internal static Variant Empty => default;
        internal static Variant Missing => new Variant(CV_MISSING, Type.Missing, 0);
        internal static Variant DBNull => new Variant(CV_NULL, System.DBNull.Value, 0);

        internal static bool IsSystemDrawingColor(Type type) => type.FullName == "System.Drawing.Color"; // Matches the behavior of IsTypeRefOrDef

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "Variant_ConvertSystemColorToOleColor")]
        internal static partial uint ConvertSystemColorToOleColor(ObjectHandleOnStack obj);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "Variant_ConvertOleColorToSystemColor")]
        internal static partial void ConvertOleColorToSystemColor(ObjectHandleOnStack objret, uint value, IntPtr pMT);

        //
        // Native Methods
        //
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal extern void SetFieldsObject(object val);

        //
        // Constructors
        //

        internal Variant(int flags, object or, long data)
        {
            _flags = flags;
            _objref = or;
            _data = data;
        }

        public Variant(bool val)
        {
            _objref = null;
            _flags = CV_BOOLEAN;
            _data = (val) ? bool.True : bool.False;
        }

        public Variant(sbyte val)
        {
            _objref = null;
            _flags = CV_I1;
            _data = val;
        }

        public Variant(byte val)
        {
            _objref = null;
            _flags = CV_U1;
            _data = val;
        }

        public Variant(short val)
        {
            _objref = null;
            _flags = CV_I2;
            _data = val;
        }

        public Variant(ushort val)
        {
            _objref = null;
            _flags = CV_U2;
            _data = val;
        }

        public Variant(char val)
        {
            _objref = null;
            _flags = CV_CHAR;
            _data = val;
        }

        public Variant(int val)
        {
            _objref = null;
            _flags = CV_I4;
            _data = val;
        }

        public Variant(uint val)
        {
            _objref = null;
            _flags = CV_U4;
            _data = val;
        }

        public Variant(long val)
        {
            _objref = null;
            _flags = CV_I8;
            _data = val;
        }

        public Variant(ulong val)
        {
            _objref = null;
            _flags = CV_U8;
            _data = (long)val;
        }

        public Variant(float val)
        {
            _objref = null;
            _flags = CV_R4;
            _data = BitConverter.SingleToUInt32Bits(val);
        }

        public Variant(double val)
        {
            _objref = null;
            _flags = CV_R8;
            _data = BitConverter.DoubleToInt64Bits(val);
        }

        public Variant(DateTime val)
        {
            _objref = null;
            _flags = CV_DATETIME;
            _data = val.Ticks;
        }

        public Variant(decimal val)
        {
            _objref = (object)val;
            _flags = CV_DECIMAL;
            _data = 0;
        }

        public Variant(object? obj)
        {
            _data = 0;

            VarEnum vt = VarEnum.VT_EMPTY;

            if (obj is DateTime)
            {
                _objref = null;
                _flags = CV_DATETIME;
                _data = ((DateTime)obj).Ticks;
                return;
            }

            if (obj is string)
            {
                _flags = CV_STRING;
                _objref = obj;
                return;
            }

            if (obj == null)
            {
                this = Empty;
                return;
            }
            if (obj == System.DBNull.Value)
            {
                this = DBNull;
                return;
            }
            if (obj == Type.Missing)
            {
                this = Missing;
                return;
            }

            if (obj is Array)
            {
                _flags = CV_OBJECT | ArrayBitMask;
                _objref = obj;
                return;
            }

            // Compiler appeasement
            _flags = CV_EMPTY;
            _objref = null;

            // Check to see if the object passed in is a wrapper object.
            if (obj is UnknownWrapper)
            {
                vt = VarEnum.VT_UNKNOWN;
                obj = ((UnknownWrapper)obj).WrappedObject;
            }
            else if (obj is DispatchWrapper)
            {
                vt = VarEnum.VT_DISPATCH;
                Debug.Assert(OperatingSystem.IsWindows());
                obj = ((DispatchWrapper)obj).WrappedObject;
            }
            else if (obj is ErrorWrapper)
            {
                vt = VarEnum.VT_ERROR;
                obj = (object)(((ErrorWrapper)obj).ErrorCode);
                Debug.Assert(obj != null, "obj != null");
            }
#pragma warning disable 0618 // CurrencyWrapper is obsolete
            else if (obj is CurrencyWrapper)
            {
                vt = VarEnum.VT_CY;
                obj = (object)(((CurrencyWrapper)obj).WrappedObject);
                Debug.Assert(obj != null, "obj != null");
            }
#pragma warning restore 0618
            else if (obj is BStrWrapper)
            {
                vt = VarEnum.VT_BSTR;
                obj = (object?)(((BStrWrapper)obj).WrappedObject);
            }

            if (obj != null)
            {
                SetFieldsObject(obj);
            }

            // If the object passed in is one of the wrappers then set the VARIANT type.
            if (vt != VarEnum.VT_EMPTY)
                _flags |= ((int)vt << VTBitShift);
        }

        // This is a family-only accessor for the CVType.
        // This is never to be exposed externally.
        internal int CVType => _flags & TypeCodeBitMask;

        public object? ToObject() =>
            CVType switch
            {
                CV_EMPTY => null,
                CV_BOOLEAN => (int)_data != 0,
                CV_I1 => (sbyte)_data,
                CV_U1 => (byte)_data,
                CV_CHAR => (char)_data,
                CV_I2 => (short)_data,
                CV_U2 => (ushort)_data,
                CV_I4 => (int)_data,
                CV_U4 => (uint)_data,
                CV_I8 => _data,
                CV_U8 => (ulong)_data,
                CV_R4 => BitConverter.UInt32BitsToSingle((uint)_data),
                CV_R8 => BitConverter.Int64BitsToDouble(_data),
                CV_DATETIME => new DateTime(_data),
                CV_TIMESPAN => new TimeSpan(_data),
                CV_ENUM => BoxEnum(),
                CV_MISSING => Type.Missing,
                CV_NULL => System.DBNull.Value,
                _ => _objref, // CV_DECIMAL, CV_STRING, CV_OBJECT
            };

        // This routine will return an boxed enum.
        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern object BoxEnum();

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
                            TypeCode.DBNull => ComVariant.Create(System.DBNull.Value),
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

                case Reflection.Missing:
                    pOle = ComVariant.CreateRaw(VarEnum.VT_ERROR, HResults.DISP_E_PARAMNOTFOUND);
                    break;

                // Array handled by native side

                case UnknownWrapper wrapper:
                    pOle = ComVariant.CreateRaw(VarEnum.VT_UNKNOWN,
                        wrapper.WrappedObject is null ? IntPtr.Zero : Marshal.GetIUnknownForObject(wrapper.WrappedObject));
                    break;
                case DispatchWrapper wrapper:
                    pOle = ComVariant.CreateRaw(VarEnum.VT_DISPATCH,
                        wrapper.WrappedObject is null ? IntPtr.Zero : Marshal.GetIDispatchForObject(wrapper.WrappedObject));
                    break;

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

                case System.Empty:
                    pOle = default;
                    break;
                case System.DBNull:
                    pOle = ComVariant.Create(System.DBNull.Value);
                    break;

                case { } when IsSystemDrawingColor(o.GetType()):
                    // System.Drawing.Color is converted to UInt32
                    pOle = ComVariant.Create(ConvertSystemColorToOleColor(ObjectHandleOnStack.Create(ref o)));
                    break;

                // DateTime, decimal handled by IConvertible case

                case TimeSpan:
                    throw new ArgumentException("IDS_EE_COM_UNSUPPORTED_SIG");
                case Currency c:
                    pOle = ComVariant.CreateRaw(VarEnum.VT_CY, c.m_value);
                    break;

                case Enum e: // TODO: Check precedence with IConvertable case
                    pOle = ComVariant.Create(((IConvertible)e).ToInt32(null));
                    break;

                case ValueType:
                    // RECORD
                    throw new NotImplementedException();

                // SafeHandle's or CriticalHandle's cannot be stored in VARIANT's.
                case SafeHandle:
                    throw new ArgumentException("IDS_EE_SH_IN_VARIANT_NOT_SUPPORTED");
                case CriticalHandle:
                    throw new ArgumentException("IDS_EE_CH_IN_VARIANT_NOT_SUPPORTED");

                // VariantWrappers cannot be stored in VARIANT's.
                case VariantWrapper:
                    throw new ArgumentException("IDS_EE_VAR_WRAP_IN_VAR_NOT_SUPPORTED");

                default:
                    // We are dealing with a normal object (not a wrapper) so we will
                    // leave the VT as VT_DISPATCH for now and we will determine the actual
                    // VT when we convert the object to a COM IP.
                    IntPtr ptr = OAVariantLib.GetIUnknownOrIDispatchForObject(ObjectHandleOnStack.Create(ref o), out bool isIDispatch);
                    pOle = ComVariant.CreateRaw(isIDispatch ? VarEnum.VT_DISPATCH : VarEnum.VT_UNKNOWN, ptr);
                    break;
            }
        }

        // Helper code for marshaling VARIANTS to managed objects
        internal static unsafe object? MarshalHelperConvertVariantToObject(ref readonly ComVariant pOle)
        {
            // Invalid and common types are handled at native side
            Debug.Assert((pOle.VarType & VarEnum.VT_ARRAY) == 0, "Array should be handled at native side.");
            Debug.Assert((pOle.VarType & ~VarEnum.VT_BYREF) is (< VarEnum.VT_I2 or > VarEnum.VT_R8) and (< VarEnum.VT_I1 or > VarEnum.VT_UI4),
                "Primitives are currently handled at native side.");
            Debug.Assert((pOle.VarType & ~VarEnum.VT_BYREF) is not (VarEnum.VT_BOOL or VarEnum.VT_BSTR or VarEnum.VT_RECORD or VarEnum.VT_VARIANT), "Should be handled at native side.");

            switch (pOle.VarType)
            {
                case VarEnum.VT_I8:
                    return pOle.As<long>();
                case VarEnum.VT_BYREF | VarEnum.VT_I8:
                    return *(long*)pOle.GetRawDataRef<IntPtr>();

                case VarEnum.VT_UI8:
                    return pOle.As<ulong>();
                case VarEnum.VT_BYREF | VarEnum.VT_UI8:
                    return *(ulong*)pOle.GetRawDataRef<IntPtr>();

                case VarEnum.VT_EMPTY:
                case VarEnum.VT_BYREF | VarEnum.VT_EMPTY:
                    return null;

                case VarEnum.VT_NULL:
                case VarEnum.VT_BYREF | VarEnum.VT_NULL:
                    return System.DBNull.Value;

                case VarEnum.VT_DATE:
                    return pOle.As<DateTime>();
                case VarEnum.VT_BYREF | VarEnum.VT_DATE:
                    return DateTime.FromOADate(*(double*)pOle.GetRawDataRef<IntPtr>());

                case VarEnum.VT_DECIMAL:
                    return pOle.As<decimal>();
                case VarEnum.VT_BYREF | VarEnum.VT_DECIMAL:
                    decimal decVal = *(decimal*)pOle.GetRawDataRef<IntPtr>();
                    // Mashaling uses the reserved value to store the variant type, so clear it out when marshaling back
                    *(ushort*)&decVal = 0;
                    return decVal;

                case VarEnum.VT_CY:
                    return decimal.FromOACurrency(pOle.GetRawDataRef<long>());
                case VarEnum.VT_BYREF | VarEnum.VT_CY:
                    return decimal.FromOACurrency(*(long*)pOle.GetRawDataRef<IntPtr>());

                case VarEnum.VT_UNKNOWN:
                case VarEnum.VT_DISPATCH:
                    return Marshal.GetObjectForIUnknown(pOle.GetRawDataRef<IntPtr>());
                case VarEnum.VT_BYREF | VarEnum.VT_UNKNOWN:
                case VarEnum.VT_BYREF | VarEnum.VT_DISPATCH:
                    IntPtr ptr = pOle.GetRawDataRef<IntPtr>();
                    return ptr == 0 ? null : Marshal.GetObjectForIUnknown(ptr);

                case VarEnum.VT_ERROR:
                    int error = pOle.GetRawDataRef<int>();
                    return error == HResults.DISP_E_PARAMNOTFOUND ? Reflection.Missing.Value : error;
                case VarEnum.VT_BYREF | VarEnum.VT_ERROR:
                    int refError = *(int*)pOle.GetRawDataRef<IntPtr>();
                    return refError == HResults.DISP_E_PARAMNOTFOUND ? Reflection.Missing.Value : refError;

                case VarEnum.VT_VOID:
                case VarEnum.VT_BYREF | VarEnum.VT_VOID:
                    return null; // CV_VOID

                default:
                    throw new ArgumentException("IDS_EE_COM_UNSUPPORTED_TYPE");
            }
        }

        // Helper code: on the back propagation path where a VT_BYREF VARIANT*
        // is marshaled to a "ref Object", we use this helper to force the
        // updated object back to the original type.
        internal static void MarshalHelperCastVariant(object pValue, int vt, out ComVariant v)
        {
            if (pValue is not IConvertible iv)
            {
                switch ((VarEnum)vt)
                {
                    case VarEnum.VT_DISPATCH:
                        Debug.Assert(OperatingSystem.IsWindows());
                        v = ComVariant.CreateRaw(VarEnum.VT_DISPATCH,
                            pValue is null ? IntPtr.Zero : Marshal.GetIDispatchForObject(pValue));
                        break;

                    case VarEnum.VT_VARIANT:
                        throw new UnreachableException("Should be handled at native side.");

                    case VarEnum.VT_UNKNOWN:
                        v = ComVariant.CreateRaw(VarEnum.VT_UNKNOWN,
                            pValue is null ? IntPtr.Zero : Marshal.GetIUnknownForObject(pValue));
                        break;

                    case VarEnum.VT_RECORD:
                        throw new NotImplementedException(); // TODO: RECORD

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
                    VarEnum.VT_NULL => ComVariant.Create(System.DBNull.Value),
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
                    VarEnum.VT_VARIANT => throw new UnreachableException("Should be handled at native side."),
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
