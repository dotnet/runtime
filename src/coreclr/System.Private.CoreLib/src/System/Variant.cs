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

namespace System
{
    internal struct Variant
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
        // COMOAVariant.cpp (2 tables, forwards and reverse), and perhaps OleVariant.h
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
        internal static Variant Missing => new Variant(Variant.CV_MISSING, Type.Missing, 0);
        internal static Variant DBNull => new Variant(Variant.CV_NULL, System.DBNull.Value, 0);

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

        // Helper code for marshaling managed objects to VARIANT's (we use
        // managed variants as an intermediate type.
        internal static void MarshalHelperConvertObjectToVariant(object o, ref Variant v)
        {
            if (o == null)
            {
                v = Empty;
            }
            else if (o is IConvertible ic)
            {
                IFormatProvider provider = CultureInfo.InvariantCulture;
                v = ic.GetTypeCode() switch
                {
                    TypeCode.Empty => Empty,
                    TypeCode.Object => new Variant((object)o),
                    TypeCode.DBNull => DBNull,
                    TypeCode.Boolean => new Variant(ic.ToBoolean(provider)),
                    TypeCode.Char => new Variant(ic.ToChar(provider)),
                    TypeCode.SByte => new Variant(ic.ToSByte(provider)),
                    TypeCode.Byte => new Variant(ic.ToByte(provider)),
                    TypeCode.Int16 => new Variant(ic.ToInt16(provider)),
                    TypeCode.UInt16 => new Variant(ic.ToUInt16(provider)),
                    TypeCode.Int32 => new Variant(ic.ToInt32(provider)),
                    TypeCode.UInt32 => new Variant(ic.ToUInt32(provider)),
                    TypeCode.Int64 => new Variant(ic.ToInt64(provider)),
                    TypeCode.UInt64 => new Variant(ic.ToUInt64(provider)),
                    TypeCode.Single => new Variant(ic.ToSingle(provider)),
                    TypeCode.Double => new Variant(ic.ToDouble(provider)),
                    TypeCode.Decimal => new Variant(ic.ToDecimal(provider)),
                    TypeCode.DateTime => new Variant(ic.ToDateTime(provider)),
                    TypeCode.String => new Variant(ic.ToString(provider)),
                    _ => throw new NotSupportedException(SR.Format(SR.NotSupported_UnknownTypeCode, ic.GetTypeCode())),
                };
            }
            else
            {
                // This path should eventually go away. But until
                // the work is done to have all of our wrapper types implement
                // IConvertible, this is a cheapo way to get the work done.
                v = new Variant(o);
            }
        }

        // Helper code for marshaling VARIANTS to managed objects (we use
        // managed variants as an intermediate type.
        internal static object? MarshalHelperConvertVariantToObject(ref Variant v)
        {
            return v.ToObject();
        }

        // Helper code: on the back propagation path where a VT_BYREF VARIANT*
        // is marshaled to a "ref Object", we use this helper to force the
        // updated object back to the original type.
        internal static void MarshalHelperCastVariant(object pValue, int vt, ref Variant v)
        {
            if (!(pValue is IConvertible iv))
            {
                switch (vt)
                {
                    case 9: /*VT_DISPATCH*/
                        Debug.Assert(OperatingSystem.IsWindows());
                        v = new Variant(new DispatchWrapper(pValue));
                        break;

                    case 12: /*VT_VARIANT*/
                        v = new Variant(pValue);
                        break;

                    case 13: /*VT_UNKNOWN*/
                        v = new Variant(new UnknownWrapper(pValue));
                        break;

                    case 36: /*VT_RECORD*/
                        v = new Variant(pValue);
                        break;

                    case 8: /*VT_BSTR*/
                        if (pValue == null)
                        {
                            v = new Variant(null) { _flags = CV_STRING };
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
                v = vt switch
                {
                    0 => /*VT_EMPTY*/ Empty,
                    1 => /*VT_NULL*/ DBNull,
                    2 => /*VT_I2*/ new Variant(iv.ToInt16(provider)),
                    3 => /*VT_I4*/ new Variant(iv.ToInt32(provider)),
                    4 => /*VT_R4*/ new Variant(iv.ToSingle(provider)),
                    5 => /*VT_R8*/ new Variant(iv.ToDouble(provider)),
#pragma warning disable 0618 // CurrencyWrapper is obsolete
                    6 => /*VT_CY*/ new Variant(new CurrencyWrapper(iv.ToDecimal(provider))),
#pragma warning restore 0618
                    7 => /*VT_DATE*/ new Variant(iv.ToDateTime(provider)),
                    8 => /*VT_BSTR*/ new Variant(iv.ToString(provider)),
#pragma warning disable CA1416 // Validate platform compatibility
                    9 => /*VT_DISPATCH*/ new Variant(new DispatchWrapper((object)iv)),
#pragma warning restore CA1416
                    10 => /*VT_ERROR*/ new Variant(new ErrorWrapper(iv.ToInt32(provider))),
                    11 => /*VT_BOOL*/ new Variant(iv.ToBoolean(provider)),
                    12 => /*VT_VARIANT*/ new Variant((object)iv),
                    13 => /*VT_UNKNOWN*/ new Variant(new UnknownWrapper((object)iv)),
                    14 => /*VT_DECIMAL*/ new Variant(iv.ToDecimal(provider)),
                    // 15 => : /*unused*/ NOT SUPPORTED
                    16 => /*VT_I1*/ new Variant(iv.ToSByte(provider)),
                    17 => /*VT_UI1*/ new Variant(iv.ToByte(provider)),
                    18 => /*VT_UI2*/ new Variant(iv.ToUInt16(provider)),
                    19 => /*VT_UI4*/ new Variant(iv.ToUInt32(provider)),
                    20 => /*VT_I8*/ new Variant(iv.ToInt64(provider)),
                    21 => /*VT_UI8*/ new Variant(iv.ToUInt64(provider)),
                    22 => /*VT_INT*/ new Variant(iv.ToInt32(provider)),
                    23 => /*VT_UINT*/ new Variant(iv.ToUInt32(provider)),
                    _ => throw new InvalidCastException(SR.InvalidCast_CannotCoerceByRefVariant),
                };
            }
        }
    }
}
