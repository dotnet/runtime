// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
**
**
** Purpose: The CLR implementation of Variant.
**
**
===========================================================*/

using System;
using System.Reflection;
using System.Threading;
using System.Runtime.InteropServices;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Diagnostics;

namespace System
{
    internal struct Variant
    {
        //Do Not change the order of these fields.
        //They are mapped to the native VariantData * data structure.
        private object? _objref;
        private long _data;
        private int _flags;

        // The following bits have been taken up as follows
        // bits 0-15    - Type code
        // bit  16      - Array
        // bits 19-23   - Enums
        // bits 24-31   - Optional VT code (for roundtrip VT preservation)


        //What are the consequences of making this an enum?
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

        // Enum enum and Mask
        internal const int EnumI1 = 0x100000;
        internal const int EnumU1 = 0x200000;
        internal const int EnumI2 = 0x300000;
        internal const int EnumU2 = 0x400000;
        internal const int EnumI4 = 0x500000;
        internal const int EnumU4 = 0x600000;
        internal const int EnumI8 = 0x700000;
        internal const int EnumU8 = 0x800000;
        internal const int EnumMask = 0xF00000;

        internal static readonly Type[] ClassTypes = {
            typeof(System.Empty),
            typeof(void),
            typeof(bool),
            typeof(char),
            typeof(sbyte),
            typeof(byte),
            typeof(short),
            typeof(ushort),
            typeof(int),
            typeof(uint),
            typeof(long),
            typeof(ulong),
            typeof(float),
            typeof(double),
            typeof(string),
            typeof(void),           // ptr for the moment
            typeof(DateTime),
            typeof(TimeSpan),
            typeof(object),
            typeof(decimal),
            typeof(object),     // Treat enum as Object
            typeof(System.Reflection.Missing),
            typeof(System.DBNull),
        };

        internal static readonly Variant Empty = new Variant();
        internal static readonly Variant Missing = new Variant(Variant.CV_MISSING, Type.Missing, 0);
        internal static readonly Variant DBNull = new Variant(Variant.CV_NULL, System.DBNull.Value, 0);

        //
        // Native Methods
        //
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
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
            _data = (uint)BitConverter.SingleToInt32Bits(val);
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
                obj = ((DispatchWrapper)obj).WrappedObject;
            }
            else if (obj is ErrorWrapper)
            {
                vt = VarEnum.VT_ERROR;
                obj = (object)(((ErrorWrapper)obj).ErrorCode);
                Debug.Assert(obj != null, "obj != null");
            }
            else if (obj is CurrencyWrapper)
            {
                vt = VarEnum.VT_CY;
                obj = (object)(((CurrencyWrapper)obj).WrappedObject);
                Debug.Assert(obj != null, "obj != null");
            }
            else if (obj is BStrWrapper)
            {
                vt = VarEnum.VT_BSTR;
                obj = (object)(((BStrWrapper)obj).WrappedObject);
            }

            if (obj != null)
            {
                SetFieldsObject(obj);
            }

            // If the object passed in is one of the wrappers then set the VARIANT type.
            if (vt != VarEnum.VT_EMPTY)
                _flags |= ((int)vt << VTBitShift);
        }

        //This is a family-only accessor for the CVType.
        //This is never to be exposed externally.
        internal int CVType
        {
            get
            {
                return (_flags & TypeCodeBitMask);
            }
        }

        public object? ToObject()
        {
            switch (CVType)
            {
                case CV_EMPTY:
                    return null;
                case CV_BOOLEAN:
                    return (object)((int)_data != 0);
                case CV_I1:
                    return (object)((sbyte)_data);
                case CV_U1:
                    return (object)((byte)_data);
                case CV_CHAR:
                    return (object)((char)_data);
                case CV_I2:
                    return (object)((short)_data);
                case CV_U2:
                    return (object)((ushort)_data);
                case CV_I4:
                    return (object)((int)_data);
                case CV_U4:
                    return (object)((uint)_data);
                case CV_I8:
                    return (object)((long)_data);
                case CV_U8:
                    return (object)((ulong)_data);
                case CV_R4:
                    return (object)(BitConverter.Int32BitsToSingle((int)_data));
                case CV_R8:
                    return (object)(BitConverter.Int64BitsToDouble(_data));
                case CV_DATETIME:
                    return new DateTime(_data);
                case CV_TIMESPAN:
                    return new TimeSpan(_data);
                case CV_ENUM:
                    return BoxEnum();
                case CV_MISSING:
                    return Type.Missing;
                case CV_NULL:
                    return System.DBNull.Value;
                case CV_DECIMAL:
                case CV_STRING:
                case CV_OBJECT:
                default:
                    return _objref;
            }
        }

        // This routine will return an boxed enum.
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern object BoxEnum();


        // Helper code for marshaling managed objects to VARIANT's (we use
        // managed variants as an intermediate type.
        internal static void MarshalHelperConvertObjectToVariant(object o, ref Variant v)
        {
            IConvertible? ic = o as IConvertible;

            if (o == null)
            {
                v = Empty;
            }
            else if (ic == null)
            {
                // This path should eventually go away. But until
                // the work is done to have all of our wrapper types implement
                // IConvertible, this is a cheapo way to get the work done.
                v = new Variant(o);
            }
            else
            {
                IFormatProvider provider = CultureInfo.InvariantCulture;
                switch (ic.GetTypeCode())
                {
                    case TypeCode.Empty:
                        v = Empty;
                        break;

                    case TypeCode.Object:
                        v = new Variant((object)o);
                        break;

                    case TypeCode.DBNull:
                        v = DBNull;
                        break;

                    case TypeCode.Boolean:
                        v = new Variant(ic.ToBoolean(provider));
                        break;

                    case TypeCode.Char:
                        v = new Variant(ic.ToChar(provider));
                        break;

                    case TypeCode.SByte:
                        v = new Variant(ic.ToSByte(provider));
                        break;

                    case TypeCode.Byte:
                        v = new Variant(ic.ToByte(provider));
                        break;

                    case TypeCode.Int16:
                        v = new Variant(ic.ToInt16(provider));
                        break;

                    case TypeCode.UInt16:
                        v = new Variant(ic.ToUInt16(provider));
                        break;

                    case TypeCode.Int32:
                        v = new Variant(ic.ToInt32(provider));
                        break;

                    case TypeCode.UInt32:
                        v = new Variant(ic.ToUInt32(provider));
                        break;

                    case TypeCode.Int64:
                        v = new Variant(ic.ToInt64(provider));
                        break;

                    case TypeCode.UInt64:
                        v = new Variant(ic.ToUInt64(provider));
                        break;

                    case TypeCode.Single:
                        v = new Variant(ic.ToSingle(provider));
                        break;

                    case TypeCode.Double:
                        v = new Variant(ic.ToDouble(provider));
                        break;

                    case TypeCode.Decimal:
                        v = new Variant(ic.ToDecimal(provider));
                        break;

                    case TypeCode.DateTime:
                        v = new Variant(ic.ToDateTime(provider));
                        break;

                    case TypeCode.String:
                        v = new Variant(ic.ToString(provider));
                        break;

                    default:
                        throw new NotSupportedException(SR.Format(SR.NotSupported_UnknownTypeCode, ic.GetTypeCode()));
                }
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
                            v = new Variant(null);
                            v._flags = CV_STRING;
                        }
                        else
                        {
                            throw new InvalidCastException(SR.InvalidCast_CannotCoerceByRefVariant);
                        }
                        break;

                    default:
                        throw new InvalidCastException(SR.InvalidCast_CannotCoerceByRefVariant);
                }
            }
            else
            {
                IFormatProvider provider = CultureInfo.InvariantCulture;
                switch (vt)
                {
                    case 0: /*VT_EMPTY*/
                        v = Empty;
                        break;

                    case 1: /*VT_NULL*/
                        v = DBNull;
                        break;

                    case 2: /*VT_I2*/
                        v = new Variant(iv.ToInt16(provider));
                        break;

                    case 3: /*VT_I4*/
                        v = new Variant(iv.ToInt32(provider));
                        break;

                    case 4: /*VT_R4*/
                        v = new Variant(iv.ToSingle(provider));
                        break;

                    case 5: /*VT_R8*/
                        v = new Variant(iv.ToDouble(provider));
                        break;

                    case 6: /*VT_CY*/
                        v = new Variant(new CurrencyWrapper(iv.ToDecimal(provider)));
                        break;

                    case 7: /*VT_DATE*/
                        v = new Variant(iv.ToDateTime(provider));
                        break;

                    case 8: /*VT_BSTR*/
                        v = new Variant(iv.ToString(provider));
                        break;

                    case 9: /*VT_DISPATCH*/
                        v = new Variant(new DispatchWrapper((object)iv));
                        break;

                    case 10: /*VT_ERROR*/
                        v = new Variant(new ErrorWrapper(iv.ToInt32(provider)));
                        break;

                    case 11: /*VT_BOOL*/
                        v = new Variant(iv.ToBoolean(provider));
                        break;

                    case 12: /*VT_VARIANT*/
                        v = new Variant((object)iv);
                        break;

                    case 13: /*VT_UNKNOWN*/
                        v = new Variant(new UnknownWrapper((object)iv));
                        break;

                    case 14: /*VT_DECIMAL*/
                        v = new Variant(iv.ToDecimal(provider));
                        break;

                    // case 15: /*unused*/
                    //  NOT SUPPORTED

                    case 16: /*VT_I1*/
                        v = new Variant(iv.ToSByte(provider));
                        break;

                    case 17: /*VT_UI1*/
                        v = new Variant(iv.ToByte(provider));
                        break;

                    case 18: /*VT_UI2*/
                        v = new Variant(iv.ToUInt16(provider));
                        break;

                    case 19: /*VT_UI4*/
                        v = new Variant(iv.ToUInt32(provider));
                        break;

                    case 20: /*VT_I8*/
                        v = new Variant(iv.ToInt64(provider));
                        break;

                    case 21: /*VT_UI8*/
                        v = new Variant(iv.ToUInt64(provider));
                        break;

                    case 22: /*VT_INT*/
                        v = new Variant(iv.ToInt32(provider));
                        break;

                    case 23: /*VT_UINT*/
                        v = new Variant(iv.ToUInt32(provider));
                        break;

                    default:
                        throw new InvalidCastException(SR.InvalidCast_CannotCoerceByRefVariant);
                }
            }
        }
    }
}
