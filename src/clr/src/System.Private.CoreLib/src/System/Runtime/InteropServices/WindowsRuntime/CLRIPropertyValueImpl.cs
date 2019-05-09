// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.CompilerServices;

using Internal.Runtime.CompilerServices;

namespace System.Runtime.InteropServices.WindowsRuntime
{
    internal class CLRIPropertyValueImpl : IPropertyValue
    {
        private PropertyType _type;
        private object _data;

        // Numeric scalar types which participate in coersion
        private static volatile Tuple<Type, PropertyType>[] s_numericScalarTypes;

        internal CLRIPropertyValueImpl(PropertyType type, object data)
        {
            _type = type;
            _data = data;
        }

        private static Tuple<Type, PropertyType>[] NumericScalarTypes
        {
            get
            {
                if (s_numericScalarTypes == null)
                {
                    Tuple<Type, PropertyType>[] numericScalarTypes = new Tuple<Type, PropertyType>[] {
                        new Tuple<Type, PropertyType>(typeof(byte), PropertyType.UInt8),
                        new Tuple<Type, PropertyType>(typeof(short), PropertyType.Int16),
                        new Tuple<Type, PropertyType>(typeof(ushort), PropertyType.UInt16),
                        new Tuple<Type, PropertyType>(typeof(int), PropertyType.Int32),
                        new Tuple<Type, PropertyType>(typeof(uint), PropertyType.UInt32),
                        new Tuple<Type, PropertyType>(typeof(long), PropertyType.Int64),
                        new Tuple<Type, PropertyType>(typeof(ulong), PropertyType.UInt64),
                        new Tuple<Type, PropertyType>(typeof(float), PropertyType.Single),
                        new Tuple<Type, PropertyType>(typeof(double), PropertyType.Double)
                    };

                    s_numericScalarTypes = numericScalarTypes;
                }

                return s_numericScalarTypes;
            }
        }

        public PropertyType Type
        {
            get { return _type; }
        }

        public bool IsNumericScalar
        {
            get
            {
                return IsNumericScalarImpl(_type, _data);
            }
        }

        public override string? ToString()
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

        public byte GetUInt8()
        {
            return CoerceScalarValue<byte>(PropertyType.UInt8);
        }

        public short GetInt16()
        {
            return CoerceScalarValue<short>(PropertyType.Int16);
        }

        public ushort GetUInt16()
        {
            return CoerceScalarValue<ushort>(PropertyType.UInt16);
        }

        public int GetInt32()
        {
            return CoerceScalarValue<int>(PropertyType.Int32);
        }

        public uint GetUInt32()
        {
            return CoerceScalarValue<uint>(PropertyType.UInt32);
        }

        public long GetInt64()
        {
            return CoerceScalarValue<long>(PropertyType.Int64);
        }

        public ulong GetUInt64()
        {
            return CoerceScalarValue<ulong>(PropertyType.UInt64);
        }

        public float GetSingle()
        {
            return CoerceScalarValue<float>(PropertyType.Single);
        }

        public double GetDouble()
        {
            return CoerceScalarValue<double>(PropertyType.Double);
        }

        public char GetChar16()
        {
            if (this.Type != PropertyType.Char16)
                throw new InvalidCastException(SR.Format(SR.InvalidCast_WinRTIPropertyValueElement, this.Type, "Char16"), HResults.TYPE_E_TYPEMISMATCH);
            return (char)_data;
        }

        public bool GetBoolean()
        {
            if (this.Type != PropertyType.Boolean)
                throw new InvalidCastException(SR.Format(SR.InvalidCast_WinRTIPropertyValueElement, this.Type, "Boolean"), HResults.TYPE_E_TYPEMISMATCH);
            return (bool)_data;
        }

        public string GetString()
        {
            return CoerceScalarValue<string>(PropertyType.String);
        }


        public Guid GetGuid()
        {
            return CoerceScalarValue<Guid>(PropertyType.Guid);
        }


        public DateTimeOffset GetDateTime()
        {
            if (this.Type != PropertyType.DateTime)
                throw new InvalidCastException(SR.Format(SR.InvalidCast_WinRTIPropertyValueElement, this.Type, "DateTime"), HResults.TYPE_E_TYPEMISMATCH);
            return (DateTimeOffset)_data;
        }

        public TimeSpan GetTimeSpan()
        {
            if (this.Type != PropertyType.TimeSpan)
                throw new InvalidCastException(SR.Format(SR.InvalidCast_WinRTIPropertyValueElement, this.Type, "TimeSpan"), HResults.TYPE_E_TYPEMISMATCH);
            return (TimeSpan)_data;
        }

        public Point GetPoint()
        {
            if (this.Type != PropertyType.Point)
                throw new InvalidCastException(SR.Format(SR.InvalidCast_WinRTIPropertyValueElement, this.Type, "Point"), HResults.TYPE_E_TYPEMISMATCH);

            return Unbox<Point>(IReferenceFactory.s_pointType);
        }

        public Size GetSize()
        {
            if (this.Type != PropertyType.Size)
                throw new InvalidCastException(SR.Format(SR.InvalidCast_WinRTIPropertyValueElement, this.Type, "Size"), HResults.TYPE_E_TYPEMISMATCH);

            return Unbox<Size>(IReferenceFactory.s_sizeType);
        }

        public Rect GetRect()
        {
            if (this.Type != PropertyType.Rect)
                throw new InvalidCastException(SR.Format(SR.InvalidCast_WinRTIPropertyValueElement, this.Type, "Rect"), HResults.TYPE_E_TYPEMISMATCH);

            return Unbox<Rect>(IReferenceFactory.s_rectType);
        }

        public byte[] GetUInt8Array()
        {
            return CoerceArrayValue<byte>(PropertyType.UInt8Array);
        }

        public short[] GetInt16Array()
        {
            return CoerceArrayValue<short>(PropertyType.Int16Array);
        }

        public ushort[] GetUInt16Array()
        {
            return CoerceArrayValue<ushort>(PropertyType.UInt16Array);
        }

        public int[] GetInt32Array()
        {
            return CoerceArrayValue<int>(PropertyType.Int32Array);
        }

        public uint[] GetUInt32Array()
        {
            return CoerceArrayValue<uint>(PropertyType.UInt32Array);
        }

        public long[] GetInt64Array()
        {
            return CoerceArrayValue<long>(PropertyType.Int64Array);
        }

        public ulong[] GetUInt64Array()
        {
            return CoerceArrayValue<ulong>(PropertyType.UInt64Array);
        }

        public float[] GetSingleArray()
        {
            return CoerceArrayValue<float>(PropertyType.SingleArray);
        }

        public double[] GetDoubleArray()
        {
            return CoerceArrayValue<double>(PropertyType.DoubleArray);
        }

        public char[] GetChar16Array()
        {
            if (this.Type != PropertyType.Char16Array)
                throw new InvalidCastException(SR.Format(SR.InvalidCast_WinRTIPropertyValueElement, this.Type, "Char16[]"), HResults.TYPE_E_TYPEMISMATCH);
            return (char[])_data;
        }

        public bool[] GetBooleanArray()
        {
            if (this.Type != PropertyType.BooleanArray)
                throw new InvalidCastException(SR.Format(SR.InvalidCast_WinRTIPropertyValueElement, this.Type, "Boolean[]"), HResults.TYPE_E_TYPEMISMATCH);
            return (bool[])_data;
        }

        public string[] GetStringArray()
        {
            return CoerceArrayValue<string>(PropertyType.StringArray);
        }

        public object[] GetInspectableArray()
        {
            if (this.Type != PropertyType.InspectableArray)
                throw new InvalidCastException(SR.Format(SR.InvalidCast_WinRTIPropertyValueElement, this.Type, "Inspectable[]"), HResults.TYPE_E_TYPEMISMATCH);
            return (object[])_data;
        }

        public Guid[] GetGuidArray()
        {
            return CoerceArrayValue<Guid>(PropertyType.GuidArray);
        }

        public DateTimeOffset[] GetDateTimeArray()
        {
            if (this.Type != PropertyType.DateTimeArray)
                throw new InvalidCastException(SR.Format(SR.InvalidCast_WinRTIPropertyValueElement, this.Type, "DateTimeOffset[]"), HResults.TYPE_E_TYPEMISMATCH);
            return (DateTimeOffset[])_data;
        }

        public TimeSpan[] GetTimeSpanArray()
        {
            if (this.Type != PropertyType.TimeSpanArray)
                throw new InvalidCastException(SR.Format(SR.InvalidCast_WinRTIPropertyValueElement, this.Type, "TimeSpan[]"), HResults.TYPE_E_TYPEMISMATCH);
            return (TimeSpan[])_data;
        }

        public Point[] GetPointArray()
        {
            if (this.Type != PropertyType.PointArray)
                throw new InvalidCastException(SR.Format(SR.InvalidCast_WinRTIPropertyValueElement, this.Type, "Point[]"), HResults.TYPE_E_TYPEMISMATCH);

            return UnboxArray<Point>(IReferenceFactory.s_pointType);
        }

        public Size[] GetSizeArray()
        {
            if (this.Type != PropertyType.SizeArray)
                throw new InvalidCastException(SR.Format(SR.InvalidCast_WinRTIPropertyValueElement, this.Type, "Size[]"), HResults.TYPE_E_TYPEMISMATCH);


            return UnboxArray<Size>(IReferenceFactory.s_sizeType);
        }

        public Rect[] GetRectArray()
        {
            if (this.Type != PropertyType.RectArray)
                throw new InvalidCastException(SR.Format(SR.InvalidCast_WinRTIPropertyValueElement, this.Type, "Rect[]"), HResults.TYPE_E_TYPEMISMATCH);

            return UnboxArray<Rect>(IReferenceFactory.s_rectType);
        }

        private T[] CoerceArrayValue<T>(PropertyType unboxType)
        {
            // If we contain the type being looked for directly, then take the fast-path
            if (Type == unboxType)
            {
                return (T[])_data;
            }

            // Make sure we have an array to begin with
            if (!(_data is Array dataArray))
            {
                throw new InvalidCastException(SR.Format(SR.InvalidCast_WinRTIPropertyValueElement, this.Type, typeof (T).MakeArrayType().Name), HResults.TYPE_E_TYPEMISMATCH);
            }

            // Array types are 1024 larger than their equivilent scalar counterpart
            Debug.Assert((int)Type > 1024, "Unexpected array PropertyType value");
            PropertyType scalarType = Type - 1024;

            // If we do not have the correct array type, then we need to convert the array element-by-element
            // to a new array of the requested type
            T[] coercedArray = new T[dataArray.Length];
            for (int i = 0; i < dataArray.Length; ++i)
            {
                try
                {
                    coercedArray[i] = CoerceScalarValue<T>(scalarType, dataArray.GetValue(i)!);
                }
                catch (InvalidCastException elementCastException)
                {
                    Exception e = new InvalidCastException(SR.Format(SR.InvalidCast_WinRTIPropertyValueArrayCoersion, this.Type, typeof (T).MakeArrayType().Name, i, elementCastException.Message), elementCastException);
                    e.HResult = elementCastException.HResult;
                    throw e;
                }
            }

            return coercedArray;
        }

        private T CoerceScalarValue<T>(PropertyType unboxType)
        {
            // If we are just a boxed version of the requested type, then take the fast path out
            if (Type == unboxType)
            {
                return (T)_data;
            }

            return CoerceScalarValue<T>(Type, _data);
        }

        private static T CoerceScalarValue<T>(PropertyType type, object value)
        {
            // If the property type is neither one of the coercable numeric types nor IInspectable, we
            // should not attempt coersion, even if the underlying value is technically convertable
            if (!IsCoercable(type, value) && type != PropertyType.Inspectable)
            {
                throw new InvalidCastException(SR.Format(SR.InvalidCast_WinRTIPropertyValueElement, type, typeof (T).Name), HResults.TYPE_E_TYPEMISMATCH);
            }

            try
            {
                // Try to coerce:
                //  * String <--> Guid
                //  * Numeric scalars
                if (type == PropertyType.String && typeof(T) == typeof(Guid))
                {
                    return (T)(object)Guid.Parse((string)value);
                }
                else if (type == PropertyType.Guid && typeof(T) == typeof(string))
                {
                    return (T)(object)((Guid)value).ToString("D", System.Globalization.CultureInfo.InvariantCulture);
                }
                else
                {
                    // Iterate over the numeric scalars, to see if we have a match for one of the known conversions
                    foreach (Tuple<Type, PropertyType> numericScalar in NumericScalarTypes)
                    {
                        if (numericScalar.Item1 == typeof(T))
                        {
                            return (T)Convert.ChangeType(value, typeof(T), System.Globalization.CultureInfo.InvariantCulture)!;
                        }
                    }
                }
            }
            catch (FormatException)
            {
                throw new InvalidCastException(SR.Format(SR.InvalidCast_WinRTIPropertyValueElement, type, typeof (T).Name), HResults.TYPE_E_TYPEMISMATCH);
            }
            catch (InvalidCastException)
            {
                throw new InvalidCastException(SR.Format(SR.InvalidCast_WinRTIPropertyValueElement, type, typeof (T).Name), HResults.TYPE_E_TYPEMISMATCH);
            }
            catch (OverflowException)
            {
                throw new InvalidCastException(SR.Format(SR.InvalidCast_WinRTIPropertyValueCoersion, type, value, typeof (T).Name), HResults.DISP_E_OVERFLOW);
            }

            // If the property type is IInspectable, and we have a nested IPropertyValue, then we need
            // to pass along the request to coerce the value.
            if (type == PropertyType.Inspectable && value is IPropertyValue ipv)
            {
                if (typeof(T) == typeof(byte))
                {
                    return (T)(object)ipv.GetUInt8();
                }
                else if (typeof(T) == typeof(short))
                {
                    return (T)(object)ipv.GetInt16();
                }
                else if (typeof(T) == typeof(ushort))
                {
                    return (T)(object)ipv.GetUInt16();
                }
                else if (typeof(T) == typeof(int))
                {
                    return (T)(object)ipv.GetUInt32();
                }
                else if (typeof(T) == typeof(uint))
                {
                    return (T)(object)ipv.GetUInt32();
                }
                else if (typeof(T) == typeof(long))
                {
                    return (T)(object)ipv.GetInt64();
                }
                else if (typeof(T) == typeof(ulong))
                {
                    return (T)(object)ipv.GetUInt64();
                }
                else if (typeof(T) == typeof(float))
                {
                    return (T)(object)ipv.GetSingle();
                }
                else if (typeof(T) == typeof(double))
                {
                    return (T)(object)ipv.GetDouble();
                }
                else
                {
                    Debug.Fail("T in coersion function wasn't understood as a type that can be coerced - make sure that CoerceScalarValue and NumericScalarTypes are in sync");
                }
            }

            // Otherwise, this is an invalid coersion
            throw new InvalidCastException(SR.Format(SR.InvalidCast_WinRTIPropertyValueElement, type, typeof (T).Name), HResults.TYPE_E_TYPEMISMATCH);
        }

        private static bool IsCoercable(PropertyType type, object data)
        {
            // String <--> Guid is allowed
            if (type == PropertyType.Guid || type == PropertyType.String)
            {
                return true;
            }

            // All numeric scalars can also be coerced
            return IsNumericScalarImpl(type, data);
        }

        private static bool IsNumericScalarImpl(PropertyType type, object data)
        {
            if (data.GetType().IsEnum)
            {
                return true;
            }

            foreach (Tuple<Type, PropertyType> numericScalar in NumericScalarTypes)
            {
                if (numericScalar.Item2 == type)
                {
                    return true;
                }
            }

            return false;
        }

        // Unbox the data stored in the property value to a structurally equivalent type
        private unsafe T Unbox<T>(Type expectedBoxedType) where T : struct
        {
            Debug.Assert(expectedBoxedType != null);
            Debug.Assert(Marshal.SizeOf(expectedBoxedType) == Marshal.SizeOf(typeof(T)));

            if (_data.GetType() != expectedBoxedType)
            {
                throw new InvalidCastException(SR.Format(SR.InvalidCast_WinRTIPropertyValueElement, _data.GetType(), expectedBoxedType.Name), HResults.TYPE_E_TYPEMISMATCH);
            }

            T unboxed = new T();

            fixed (byte* pData = &_data.GetRawData())
            {
                byte* pUnboxed = (byte*)Unsafe.AsPointer(ref unboxed);
                Buffer.Memcpy(pUnboxed, pData, Marshal.SizeOf(unboxed));
            }

            return unboxed;
        }

        // Convert the array stored in the property value to a structurally equivilent array type
        private unsafe T[] UnboxArray<T>(Type expectedArrayElementType) where T : struct
        {
            Debug.Assert(expectedArrayElementType != null);
            Debug.Assert(Marshal.SizeOf(expectedArrayElementType) == Marshal.SizeOf(typeof(T)));

            if (!(_data is Array dataArray) || _data.GetType().GetElementType() != expectedArrayElementType)
            {
                throw new InvalidCastException(SR.Format(SR.InvalidCast_WinRTIPropertyValueElement, _data.GetType(), expectedArrayElementType.MakeArrayType().Name), HResults.TYPE_E_TYPEMISMATCH);
            }

            T[] converted = new T[dataArray.Length];

            if (converted.Length > 0)
            {
                fixed (byte* dataPin = &dataArray.GetRawData())
                fixed (byte* convertedPin = &converted.GetRawData())
                {
                    byte* pData = (byte*)Marshal.UnsafeAddrOfPinnedArrayElement(dataArray, 0);
                    byte* pConverted = (byte*)Marshal.UnsafeAddrOfPinnedArrayElement(converted, 0);

                    Buffer.Memcpy(pConverted, pData, checked(Marshal.SizeOf(typeof(T)) * converted.Length));
                }
            }

            return converted;
        }
    }
}
