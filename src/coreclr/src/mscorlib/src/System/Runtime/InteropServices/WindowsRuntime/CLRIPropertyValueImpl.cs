// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//

using System;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Security;

namespace System.Runtime.InteropServices.WindowsRuntime
{
    internal class CLRIPropertyValueImpl : IPropertyValue
    {
        private PropertyType _type;
        private Object _data;

        // Numeric scalar types which participate in coersion
        private static volatile Tuple<Type, PropertyType>[] s_numericScalarTypes;

        internal CLRIPropertyValueImpl(PropertyType type, Object data)
        {
            _type = type;
            _data = data;
        }

        private static Tuple<Type, PropertyType>[] NumericScalarTypes {
            get {
                if (s_numericScalarTypes == null) {
                    Tuple<Type, PropertyType>[] numericScalarTypes = new Tuple<Type, PropertyType>[] {
                        new Tuple<Type, PropertyType>(typeof(Byte), PropertyType.UInt8),
                        new Tuple<Type, PropertyType>(typeof(Int16), PropertyType.Int16),
                        new Tuple<Type, PropertyType>(typeof(UInt16), PropertyType.UInt16),
                        new Tuple<Type, PropertyType>(typeof(Int32), PropertyType.Int32),
                        new Tuple<Type, PropertyType>(typeof(UInt32), PropertyType.UInt32),
                        new Tuple<Type, PropertyType>(typeof(Int64), PropertyType.Int64),
                        new Tuple<Type, PropertyType>(typeof(UInt64), PropertyType.UInt64),
                        new Tuple<Type, PropertyType>(typeof(Single), PropertyType.Single),
                        new Tuple<Type, PropertyType>(typeof(Double), PropertyType.Double)
                    };

                    s_numericScalarTypes = numericScalarTypes;
                }

                return s_numericScalarTypes;
            }
        }

        public PropertyType Type {
            [Pure]
            get { return _type; }
        }

        public bool IsNumericScalar {
            [Pure]
            get {
                return IsNumericScalarImpl(_type, _data);
            }
        }

        public override string ToString()
        {
            if (_data != null)
            {
                return _data.ToString();
            }
            else
            {
                return base.ToString();
            }
        }

        [Pure]
        public Byte GetUInt8()
        {
            return CoerceScalarValue<Byte>(PropertyType.UInt8);
        }

        [Pure]
        public Int16 GetInt16()
        {
            return CoerceScalarValue<Int16>(PropertyType.Int16);
        }

        public UInt16 GetUInt16()
        {
            return CoerceScalarValue<UInt16>(PropertyType.UInt16);
        }

        [Pure]
        public Int32 GetInt32()
        {
            return CoerceScalarValue<Int32>(PropertyType.Int32);
        }

        [Pure]
        public UInt32 GetUInt32()
        {
            return CoerceScalarValue<UInt32>(PropertyType.UInt32);
        }

        [Pure]
        public Int64 GetInt64()
        {
            return CoerceScalarValue<Int64>(PropertyType.Int64);
        }

        [Pure]
        public UInt64 GetUInt64()
        {
            return CoerceScalarValue<UInt64>(PropertyType.UInt64);
        }

        [Pure]
        public Single GetSingle()
        {
            return CoerceScalarValue<Single>(PropertyType.Single);
        }

        [Pure]
        public Double GetDouble()
        {
            return CoerceScalarValue<Double>(PropertyType.Double);
        }

        [Pure]
        public char GetChar16()
        {
            if (this.Type != PropertyType.Char16)
                throw new InvalidCastException(Environment.GetResourceString("InvalidCast_WinRTIPropertyValueElement", this.Type, "Char16"), __HResults.TYPE_E_TYPEMISMATCH);
            Contract.EndContractBlock();
            return (char)_data;
        }

        [Pure]
        public Boolean GetBoolean()
        {
            if (this.Type != PropertyType.Boolean)
                throw new InvalidCastException(Environment.GetResourceString("InvalidCast_WinRTIPropertyValueElement", this.Type, "Boolean"), __HResults.TYPE_E_TYPEMISMATCH);
            Contract.EndContractBlock();
            return (bool)_data;
        }

        [Pure]
        public String GetString()
        {
            return CoerceScalarValue<String>(PropertyType.String);
        }

        [Pure]
        public Object GetInspectable()
        {
            if (this.Type != PropertyType.Inspectable)
                throw new InvalidCastException(Environment.GetResourceString("InvalidCast_WinRTIPropertyValueElement", this.Type, "Inspectable"), __HResults.TYPE_E_TYPEMISMATCH);
            Contract.EndContractBlock();
            return _data;
        }


        [Pure]
        public Guid GetGuid()
        {
            return CoerceScalarValue<Guid>(PropertyType.Guid);
        }


        [Pure]
        public DateTimeOffset GetDateTime()
        {
            if (this.Type != PropertyType.DateTime)
                throw new InvalidCastException(Environment.GetResourceString("InvalidCast_WinRTIPropertyValueElement", this.Type, "DateTime"), __HResults.TYPE_E_TYPEMISMATCH);
            Contract.EndContractBlock();
            return (DateTimeOffset)_data;
        }

        [Pure]
        public TimeSpan GetTimeSpan()
        {
            if (this.Type != PropertyType.TimeSpan)
                throw new InvalidCastException(Environment.GetResourceString("InvalidCast_WinRTIPropertyValueElement", this.Type, "TimeSpan"), __HResults.TYPE_E_TYPEMISMATCH);
            Contract.EndContractBlock();
            return (TimeSpan)_data;
        }

        [Pure]
        [SecuritySafeCritical]
        public Point GetPoint()
        {
            if (this.Type != PropertyType.Point)
                throw new InvalidCastException(Environment.GetResourceString("InvalidCast_WinRTIPropertyValueElement", this.Type, "Point"), __HResults.TYPE_E_TYPEMISMATCH);
            Contract.EndContractBlock();

            return Unbox<Point>(IReferenceFactory.s_pointType);
        }

        [Pure]
        [SecuritySafeCritical]
        public Size GetSize()
        {
            if (this.Type != PropertyType.Size)
                throw new InvalidCastException(Environment.GetResourceString("InvalidCast_WinRTIPropertyValueElement", this.Type, "Size"), __HResults.TYPE_E_TYPEMISMATCH);
            Contract.EndContractBlock();
            
            return Unbox<Size>(IReferenceFactory.s_sizeType);
        }

        [Pure]
        [SecuritySafeCritical]
        public Rect GetRect()
        {
            if (this.Type != PropertyType.Rect)
                throw new InvalidCastException(Environment.GetResourceString("InvalidCast_WinRTIPropertyValueElement", this.Type, "Rect"), __HResults.TYPE_E_TYPEMISMATCH);
            Contract.EndContractBlock();
            
            return Unbox<Rect>(IReferenceFactory.s_rectType);
        }

        [Pure]
        public Byte[] GetUInt8Array()
        {
            return CoerceArrayValue<Byte>(PropertyType.UInt8Array);
        }

        [Pure]
        public Int16[] GetInt16Array()
        {
            return CoerceArrayValue<Int16>(PropertyType.Int16Array);
        }

        [Pure]
        public UInt16[] GetUInt16Array()
        {
            return CoerceArrayValue<UInt16>(PropertyType.UInt16Array);
        }

        [Pure]
        public Int32[] GetInt32Array()
        {
            return CoerceArrayValue<Int32>(PropertyType.Int32Array);
        }

        [Pure]
        public UInt32[] GetUInt32Array()
        {
            return CoerceArrayValue<UInt32>(PropertyType.UInt32Array);
        }

        [Pure]
        public Int64[] GetInt64Array()
        {
            return CoerceArrayValue<Int64>(PropertyType.Int64Array);
        }

        [Pure]
        public UInt64[] GetUInt64Array()
        {
            return CoerceArrayValue<UInt64>(PropertyType.UInt64Array);
        }

        [Pure]
        public Single[] GetSingleArray()
        {
            return CoerceArrayValue<Single>(PropertyType.SingleArray);
        }

        [Pure]
        public Double[] GetDoubleArray()
        {
            return CoerceArrayValue<Double>(PropertyType.DoubleArray);
        }

        [Pure]
        public char[] GetChar16Array()
        {
            if (this.Type != PropertyType.Char16Array)
                throw new InvalidCastException(Environment.GetResourceString("InvalidCast_WinRTIPropertyValueElement", this.Type, "Char16[]"), __HResults.TYPE_E_TYPEMISMATCH);
            Contract.EndContractBlock();
            return (char[])_data;
        }

        [Pure]
        public Boolean[] GetBooleanArray()
        {
            if (this.Type != PropertyType.BooleanArray)
                throw new InvalidCastException(Environment.GetResourceString("InvalidCast_WinRTIPropertyValueElement", this.Type, "Boolean[]"), __HResults.TYPE_E_TYPEMISMATCH);
            Contract.EndContractBlock();
            return (bool[])_data;
        }

        [Pure]
        public String[] GetStringArray()
        {
            return CoerceArrayValue<String>(PropertyType.StringArray);
        }

        [Pure]
        public Object[] GetInspectableArray()
        {
            if (this.Type != PropertyType.InspectableArray)
                throw new InvalidCastException(Environment.GetResourceString("InvalidCast_WinRTIPropertyValueElement", this.Type, "Inspectable[]"), __HResults.TYPE_E_TYPEMISMATCH);
            Contract.EndContractBlock();
            return (Object[])_data;
        }

        [Pure]
        public Guid[] GetGuidArray()
        {
            return CoerceArrayValue<Guid>(PropertyType.GuidArray);
        }

        [Pure]
        public DateTimeOffset[] GetDateTimeArray()
        {
            if (this.Type != PropertyType.DateTimeArray)
                throw new InvalidCastException(Environment.GetResourceString("InvalidCast_WinRTIPropertyValueElement", this.Type, "DateTimeOffset[]"), __HResults.TYPE_E_TYPEMISMATCH);
            Contract.EndContractBlock();
            return (DateTimeOffset[])_data;
        }

        [Pure]
        public TimeSpan[] GetTimeSpanArray()
        {
            if (this.Type != PropertyType.TimeSpanArray)
                throw new InvalidCastException(Environment.GetResourceString("InvalidCast_WinRTIPropertyValueElement", this.Type, "TimeSpan[]"), __HResults.TYPE_E_TYPEMISMATCH);
            Contract.EndContractBlock();
            return (TimeSpan[])_data;
        }

        [Pure]
        [SecuritySafeCritical]
        public Point[] GetPointArray()
        {
            if (this.Type != PropertyType.PointArray)
                throw new InvalidCastException(Environment.GetResourceString("InvalidCast_WinRTIPropertyValueElement", this.Type, "Point[]"), __HResults.TYPE_E_TYPEMISMATCH);
            Contract.EndContractBlock();
            
            return UnboxArray<Point>(IReferenceFactory.s_pointType);
        }

        [Pure]
        [SecuritySafeCritical]
        public Size[] GetSizeArray()
        {
            if (this.Type != PropertyType.SizeArray)
                throw new InvalidCastException(Environment.GetResourceString("InvalidCast_WinRTIPropertyValueElement", this.Type, "Size[]"), __HResults.TYPE_E_TYPEMISMATCH);
            Contract.EndContractBlock();


            return UnboxArray<Size>(IReferenceFactory.s_sizeType);
        }

        [Pure]
        [SecuritySafeCritical]
        public Rect[] GetRectArray()
        {
            if (this.Type != PropertyType.RectArray)
                throw new InvalidCastException(Environment.GetResourceString("InvalidCast_WinRTIPropertyValueElement", this.Type, "Rect[]"), __HResults.TYPE_E_TYPEMISMATCH);
            Contract.EndContractBlock();

            return UnboxArray<Rect>(IReferenceFactory.s_rectType);
        }

        private T[] CoerceArrayValue<T>(PropertyType unboxType) {
            // If we contain the type being looked for directly, then take the fast-path
            if (Type == unboxType) {
                return (T[])_data;
            }

            // Make sure we have an array to begin with
            Array dataArray = _data as Array;
            if (dataArray == null) {
                throw new InvalidCastException(Environment.GetResourceString("InvalidCast_WinRTIPropertyValueElement", this.Type, typeof(T).MakeArrayType().Name), __HResults.TYPE_E_TYPEMISMATCH);
            }

            // Array types are 1024 larger than their equivilent scalar counterpart
            BCLDebug.Assert((int)Type > 1024, "Unexpected array PropertyType value");
            PropertyType scalarType = Type - 1024;

            // If we do not have the correct array type, then we need to convert the array element-by-element
            // to a new array of the requested type
            T[] coercedArray = new T[dataArray.Length];
            for (int i = 0; i < dataArray.Length; ++i) {
                try {
                    coercedArray[i] = CoerceScalarValue<T>(scalarType, dataArray.GetValue(i));
                } catch (InvalidCastException elementCastException) {
                    Exception e = new InvalidCastException(Environment.GetResourceString("InvalidCast_WinRTIPropertyValueArrayCoersion", this.Type, typeof(T).MakeArrayType().Name, i, elementCastException.Message), elementCastException);
                    e.SetErrorCode(elementCastException._HResult);
                    throw e;
                }
            }

            return coercedArray;
        }

        private T CoerceScalarValue<T>(PropertyType unboxType)
        {
            // If we are just a boxed version of the requested type, then take the fast path out
            if (Type == unboxType) {
                return (T)_data;
            }

            return CoerceScalarValue<T>(Type, _data);
        }

        private static T CoerceScalarValue<T>(PropertyType type, object value) {
            // If the property type is neither one of the coercable numeric types nor IInspectable, we
            // should not attempt coersion, even if the underlying value is technically convertable
            if (!IsCoercable(type, value) && type != PropertyType.Inspectable) {
                throw new InvalidCastException(Environment.GetResourceString("InvalidCast_WinRTIPropertyValueElement", type, typeof(T).Name), __HResults.TYPE_E_TYPEMISMATCH);
            }

            try {
                // Try to coerce:
                //  * String <--> Guid
                //  * Numeric scalars
                if (type == PropertyType.String && typeof(T) == typeof(Guid)) {
                    return (T)(object)Guid.Parse((string)value);
                }
                else if (type == PropertyType.Guid && typeof(T) == typeof(String)) {
                    return  (T)(object)((Guid)value).ToString("D", System.Globalization.CultureInfo.InvariantCulture);
                }
                else {
                    // Iterate over the numeric scalars, to see if we have a match for one of the known conversions
                    foreach (Tuple<Type, PropertyType> numericScalar in NumericScalarTypes) {
                        if (numericScalar.Item1 == typeof(T)) {
                            return (T)Convert.ChangeType(value, typeof(T), System.Globalization.CultureInfo.InvariantCulture);
                        }
                    }
                }
            }
            catch (FormatException) {
                throw new InvalidCastException(Environment.GetResourceString("InvalidCast_WinRTIPropertyValueElement", type, typeof(T).Name), __HResults.TYPE_E_TYPEMISMATCH);
            }
            catch (InvalidCastException) {
                throw new InvalidCastException(Environment.GetResourceString("InvalidCast_WinRTIPropertyValueElement", type, typeof(T).Name), __HResults.TYPE_E_TYPEMISMATCH);
            }
            catch (OverflowException) {
                throw new InvalidCastException(Environment.GetResourceString("InvalidCast_WinRTIPropertyValueCoersion", type, value, typeof(T).Name), __HResults.DISP_E_OVERFLOW);
            }

            // If the property type is IInspectable, and we have a nested IPropertyValue, then we need
            // to pass along the request to coerce the value.
            IPropertyValue ipv = value as IPropertyValue;
            if (type == PropertyType.Inspectable && ipv != null) {
                if (typeof(T) == typeof(Byte)) {
                    return (T)(object)ipv.GetUInt8();
                }
                else if (typeof(T) == typeof(Int16)) {
                    return (T)(object)ipv.GetInt16();
                }
                else if (typeof(T) == typeof(UInt16)) {
                    return (T)(object)ipv.GetUInt16();
                }
                else if (typeof(T) == typeof(Int32)) {
                    return (T)(object)ipv.GetUInt32();
                }
                else if (typeof(T) == typeof(UInt32)) {
                    return (T)(object)ipv.GetUInt32();
                }
                else if (typeof(T) == typeof(Int64)) {
                    return (T)(object)ipv.GetInt64();
                }
                else if (typeof(T) == typeof(UInt64)) {
                    return (T)(object)ipv.GetUInt64();
                }
                else if (typeof(T) == typeof(Single)) {
                    return (T)(object)ipv.GetSingle();
                }
                else if (typeof(T) == typeof(Double)) {
                    return (T)(object)ipv.GetDouble();
                }
                else {
                    BCLDebug.Assert(false, "T in coersion function wasn't understood as a type that can be coerced - make sure that CoerceScalarValue and NumericScalarTypes are in sync");
                }
            }

            // Otherwise, this is an invalid coersion
            throw new InvalidCastException(Environment.GetResourceString("InvalidCast_WinRTIPropertyValueElement", type, typeof(T).Name), __HResults.TYPE_E_TYPEMISMATCH);
        }

        private static bool IsCoercable(PropertyType type, object data) {
            // String <--> Guid is allowed
            if (type == PropertyType.Guid || type == PropertyType.String) {
                return true;
            }

            // All numeric scalars can also be coerced
            return IsNumericScalarImpl(type, data);
        }

        private static bool IsNumericScalarImpl(PropertyType type, object data) {
            if (data.GetType().IsEnum) {
                return true;
            }

            foreach (Tuple<Type, PropertyType> numericScalar in NumericScalarTypes) {
                if (numericScalar.Item2 == type) {
                    return true;
                }
            }

            return false;
        }

        // Unbox the data stored in the property value to a structurally equivilent type
        [Pure]
        [SecurityCritical]
        private unsafe T Unbox<T>(Type expectedBoxedType) where T : struct {
            Contract.Requires(expectedBoxedType != null);
            Contract.Requires(Marshal.SizeOf(expectedBoxedType) == Marshal.SizeOf(typeof(T)));

            if (_data.GetType() != expectedBoxedType) {
                throw new InvalidCastException(Environment.GetResourceString("InvalidCast_WinRTIPropertyValueElement", _data.GetType(), expectedBoxedType.Name), __HResults.TYPE_E_TYPEMISMATCH);
            }

            T unboxed = new T();

            fixed (byte *pData = &JitHelpers.GetPinningHelper(_data).m_data) {
                byte* pUnboxed = (byte*)JitHelpers.UnsafeCastToStackPointer(ref unboxed);
                Buffer.Memcpy(pUnboxed, pData, Marshal.SizeOf(unboxed));
            }
            
            return unboxed;
        }

        // Convert the array stored in the property value to a structurally equivilent array type
        [Pure]
        [SecurityCritical]
        private unsafe T[] UnboxArray<T>(Type expectedArrayElementType) where T : struct {
            Contract.Requires(expectedArrayElementType != null);
            Contract.Requires(Marshal.SizeOf(expectedArrayElementType) == Marshal.SizeOf(typeof(T)));

            Array dataArray = _data as Array;
            if (dataArray == null || _data.GetType().GetElementType() != expectedArrayElementType) {
                throw new InvalidCastException(Environment.GetResourceString("InvalidCast_WinRTIPropertyValueElement", _data.GetType(), expectedArrayElementType.MakeArrayType().Name), __HResults.TYPE_E_TYPEMISMATCH);
            }

            T[] converted = new T[dataArray.Length];

            if (converted.Length > 0) {
                fixed (byte * dataPin = &JitHelpers.GetPinningHelper(dataArray).m_data) {
                    fixed (byte * convertedPin = &JitHelpers.GetPinningHelper(converted).m_data) {
                        byte *pData = (byte *)Marshal.UnsafeAddrOfPinnedArrayElement(dataArray, 0);
                        byte *pConverted = (byte *)Marshal.UnsafeAddrOfPinnedArrayElement(converted, 0);

                        Buffer.Memcpy(pConverted, pData, checked(Marshal.SizeOf(typeof(T)) * converted.Length));
                    }
                }
            }

            return converted;
        }
    }
}
