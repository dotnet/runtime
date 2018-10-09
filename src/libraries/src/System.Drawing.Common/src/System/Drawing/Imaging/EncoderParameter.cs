// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using Gdip = System.Drawing.SafeNativeMethods.Gdip;

namespace System.Drawing.Imaging
{
    [StructLayout(LayoutKind.Sequential)]
    public sealed class EncoderParameter : IDisposable
    {
#pragma warning disable CS0618 // Legacy code: We don't care about using obsolete API's.
        [MarshalAs(UnmanagedType.Struct)]
#pragma warning restore CS0618
        private Guid _parameterGuid;                    // GUID of the parameter
        private int _numberOfValues;                    // Number of the parameter values  
        private EncoderParameterValueType _parameterValueType;   // Value type, like ValueTypeLONG  etc.
        private IntPtr _parameterValue;                 // A pointer to the parameter values

        ~EncoderParameter()
        {
            Dispose(false);
        }

        /// <summary>
        /// Gets/Sets the Encoder for the EncoderPameter.
        /// </summary>
        public Encoder Encoder
        {
            get
            {
                return new Encoder(_parameterGuid);
            }
            set
            {
                _parameterGuid = value.Guid;
            }
        }

        /// <summary>
        /// Gets the EncoderParameterValueType object from the EncoderParameter.
        /// </summary>
        public EncoderParameterValueType Type
        {
            get
            {
                return _parameterValueType;
            }
        }

        /// <summary>
        /// Gets the EncoderParameterValueType object from the EncoderParameter.
        /// </summary>
        public EncoderParameterValueType ValueType
        {
            get
            {
                return _parameterValueType;
            }
        }

        /// <summary>
        /// Gets the NumberOfValues from the EncoderParameter.
        /// </summary>
        public int NumberOfValues
        {
            get
            {
                return _numberOfValues;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.KeepAlive(this);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_parameterValue != IntPtr.Zero)
                Marshal.FreeHGlobal(_parameterValue);
            _parameterValue = IntPtr.Zero;
        }

        public EncoderParameter(Encoder encoder, byte value)
        {
            _parameterGuid = encoder.Guid;

            _parameterValueType = EncoderParameterValueType.ValueTypeByte;
            _numberOfValues = 1;
            _parameterValue = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(byte)));

            if (_parameterValue == IntPtr.Zero)
                throw Gdip.StatusException(Gdip.OutOfMemory);

            Marshal.WriteByte(_parameterValue, value);
            GC.KeepAlive(this);
        }

        public EncoderParameter(Encoder encoder, byte value, bool undefined)
        {
            _parameterGuid = encoder.Guid;

            if (undefined == true)
                _parameterValueType = EncoderParameterValueType.ValueTypeUndefined;
            else
                _parameterValueType = EncoderParameterValueType.ValueTypeByte;
            _numberOfValues = 1;
            _parameterValue = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(byte)));

            if (_parameterValue == IntPtr.Zero)
                throw Gdip.StatusException(Gdip.OutOfMemory);

            Marshal.WriteByte(_parameterValue, value);
            GC.KeepAlive(this);
        }

        public EncoderParameter(Encoder encoder, short value)
        {
            _parameterGuid = encoder.Guid;

            _parameterValueType = EncoderParameterValueType.ValueTypeShort;
            _numberOfValues = 1;
            _parameterValue = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(short)));

            if (_parameterValue == IntPtr.Zero)
                throw Gdip.StatusException(Gdip.OutOfMemory);

            Marshal.WriteInt16(_parameterValue, value);
            GC.KeepAlive(this);
        }

        public EncoderParameter(Encoder encoder, long value)
        {
            _parameterGuid = encoder.Guid;

            _parameterValueType = EncoderParameterValueType.ValueTypeLong;
            _numberOfValues = 1;
            _parameterValue = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(int)));

            if (_parameterValue == IntPtr.Zero)
                throw Gdip.StatusException(Gdip.OutOfMemory);

            Marshal.WriteInt32(_parameterValue, unchecked((int)value));
            GC.KeepAlive(this);
        }

        public EncoderParameter(Encoder encoder, int numerator, int denominator)
        {
            _parameterGuid = encoder.Guid;

            _parameterValueType = EncoderParameterValueType.ValueTypeRational;
            _numberOfValues = 1;
            int size = Marshal.SizeOf(typeof(int));
            _parameterValue = Marshal.AllocHGlobal(2 * size);

            if (_parameterValue == IntPtr.Zero)
                throw Gdip.StatusException(Gdip.OutOfMemory);

            Marshal.WriteInt32(_parameterValue, numerator);
            Marshal.WriteInt32(Add(_parameterValue, size), denominator);
            GC.KeepAlive(this);
        }

        public EncoderParameter(Encoder encoder, long rangebegin, long rangeend)
        {
            _parameterGuid = encoder.Guid;

            _parameterValueType = EncoderParameterValueType.ValueTypeLongRange;
            _numberOfValues = 1;
            int size = Marshal.SizeOf(typeof(int));
            _parameterValue = Marshal.AllocHGlobal(2 * size);

            if (_parameterValue == IntPtr.Zero)
                throw Gdip.StatusException(Gdip.OutOfMemory);

            Marshal.WriteInt32(_parameterValue, unchecked((int)rangebegin));
            Marshal.WriteInt32(Add(_parameterValue, size), unchecked((int)rangeend));
            GC.KeepAlive(this);
        }

        public EncoderParameter(Encoder encoder,
                                int numerator1, int demoninator1,
                                int numerator2, int demoninator2)
        {
            _parameterGuid = encoder.Guid;

            _parameterValueType = EncoderParameterValueType.ValueTypeRationalRange;
            _numberOfValues = 1;
            int size = Marshal.SizeOf(typeof(int));
            _parameterValue = Marshal.AllocHGlobal(4 * size);

            if (_parameterValue == IntPtr.Zero)
                throw Gdip.StatusException(Gdip.OutOfMemory);

            Marshal.WriteInt32(_parameterValue, numerator1);
            Marshal.WriteInt32(Add(_parameterValue, size), demoninator1);
            Marshal.WriteInt32(Add(_parameterValue, 2 * size), numerator2);
            Marshal.WriteInt32(Add(_parameterValue, 3 * size), demoninator2);
            GC.KeepAlive(this);
        }

        public EncoderParameter(Encoder encoder, string value)
        {
            _parameterGuid = encoder.Guid;

            _parameterValueType = EncoderParameterValueType.ValueTypeAscii;
            _numberOfValues = value.Length;
            _parameterValue = Marshal.StringToHGlobalAnsi(value);
            GC.KeepAlive(this);

            if (_parameterValue == IntPtr.Zero)
                throw Gdip.StatusException(Gdip.OutOfMemory);
        }

        public EncoderParameter(Encoder encoder, byte[] value)
        {
            _parameterGuid = encoder.Guid;

            _parameterValueType = EncoderParameterValueType.ValueTypeByte;
            _numberOfValues = value.Length;

            _parameterValue = Marshal.AllocHGlobal(_numberOfValues);

            if (_parameterValue == IntPtr.Zero)
                throw Gdip.StatusException(Gdip.OutOfMemory);

            Marshal.Copy(value, 0, _parameterValue, _numberOfValues);
            GC.KeepAlive(this);
        }

        public EncoderParameter(Encoder encoder, byte[] value, bool undefined)
        {
            _parameterGuid = encoder.Guid;

            if (undefined == true)
                _parameterValueType = EncoderParameterValueType.ValueTypeUndefined;
            else
                _parameterValueType = EncoderParameterValueType.ValueTypeByte;

            _numberOfValues = value.Length;
            _parameterValue = Marshal.AllocHGlobal(_numberOfValues);

            if (_parameterValue == IntPtr.Zero)
                throw Gdip.StatusException(Gdip.OutOfMemory);

            Marshal.Copy(value, 0, _parameterValue, _numberOfValues);
            GC.KeepAlive(this);
        }

        public EncoderParameter(Encoder encoder, short[] value)
        {
            _parameterGuid = encoder.Guid;

            _parameterValueType = EncoderParameterValueType.ValueTypeShort;
            _numberOfValues = value.Length;
            int size = Marshal.SizeOf(typeof(short));

            _parameterValue = Marshal.AllocHGlobal(checked(_numberOfValues * size));

            if (_parameterValue == IntPtr.Zero)
                throw Gdip.StatusException(Gdip.OutOfMemory);

            Marshal.Copy(value, 0, _parameterValue, _numberOfValues);
            GC.KeepAlive(this);
        }

        public unsafe EncoderParameter(Encoder encoder, long[] value)
        {
            _parameterGuid = encoder.Guid;

            _parameterValueType = EncoderParameterValueType.ValueTypeLong;
            _numberOfValues = value.Length;
            int size = Marshal.SizeOf(typeof(int));

            _parameterValue = Marshal.AllocHGlobal(checked(_numberOfValues * size));

            if (_parameterValue == IntPtr.Zero)
                throw Gdip.StatusException(Gdip.OutOfMemory);

            int* dest = (int*)_parameterValue;
            fixed (long* source = value)
            {
                for (int i = 0; i < value.Length; i++)
                {
                    dest[i] = unchecked((int)source[i]);
                }
            }
            GC.KeepAlive(this);
        }

        public EncoderParameter(Encoder encoder, int[] numerator, int[] denominator)
        {
            _parameterGuid = encoder.Guid;

            if (numerator.Length != denominator.Length)
                throw Gdip.StatusException(Gdip.InvalidParameter);

            _parameterValueType = EncoderParameterValueType.ValueTypeRational;
            _numberOfValues = numerator.Length;
            int size = Marshal.SizeOf(typeof(int));

            _parameterValue = Marshal.AllocHGlobal(checked(_numberOfValues * 2 * size));

            if (_parameterValue == IntPtr.Zero)
                throw Gdip.StatusException(Gdip.OutOfMemory);

            for (int i = 0; i < _numberOfValues; i++)
            {
                Marshal.WriteInt32(Add(i * 2 * size, _parameterValue), (int)numerator[i]);
                Marshal.WriteInt32(Add((i * 2 + 1) * size, _parameterValue), (int)denominator[i]);
            }
            GC.KeepAlive(this);
        }

        public EncoderParameter(Encoder encoder, long[] rangebegin, long[] rangeend)
        {
            _parameterGuid = encoder.Guid;

            if (rangebegin.Length != rangeend.Length)
                throw Gdip.StatusException(Gdip.InvalidParameter);

            _parameterValueType = EncoderParameterValueType.ValueTypeLongRange;
            _numberOfValues = rangebegin.Length;
            int size = Marshal.SizeOf(typeof(int));

            _parameterValue = Marshal.AllocHGlobal(checked(_numberOfValues * 2 * size));

            if (_parameterValue == IntPtr.Zero)
                throw Gdip.StatusException(Gdip.OutOfMemory);

            for (int i = 0; i < _numberOfValues; i++)
            {
                Marshal.WriteInt32(Add(i * 2 * size, _parameterValue), unchecked((int)rangebegin[i]));
                Marshal.WriteInt32(Add((i * 2 + 1) * size, _parameterValue), unchecked((int)rangeend[i]));
            }
            GC.KeepAlive(this);
        }

        public EncoderParameter(Encoder encoder,
                                int[] numerator1, int[] denominator1,
                                int[] numerator2, int[] denominator2)
        {
            _parameterGuid = encoder.Guid;

            if (numerator1.Length != denominator1.Length ||
                numerator1.Length != denominator2.Length ||
                denominator1.Length != denominator2.Length)
                throw Gdip.StatusException(Gdip.InvalidParameter);

            _parameterValueType = EncoderParameterValueType.ValueTypeRationalRange;
            _numberOfValues = numerator1.Length;
            int size = Marshal.SizeOf(typeof(int));

            _parameterValue = Marshal.AllocHGlobal(checked(_numberOfValues * 4 * size));

            if (_parameterValue == IntPtr.Zero)
                throw Gdip.StatusException(Gdip.OutOfMemory);

            for (int i = 0; i < _numberOfValues; i++)
            {
                Marshal.WriteInt32(Add(_parameterValue, 4 * i * size), numerator1[i]);
                Marshal.WriteInt32(Add(_parameterValue, (4 * i + 1) * size), denominator1[i]);
                Marshal.WriteInt32(Add(_parameterValue, (4 * i + 2) * size), numerator2[i]);
                Marshal.WriteInt32(Add(_parameterValue, (4 * i + 3) * size), denominator2[i]);
            }
            GC.KeepAlive(this);
        }

        [Obsolete("This constructor has been deprecated. Use EncoderParameter(Encoder encoder, int numberValues, EncoderParameterValueType type, IntPtr value) instead.  https://go.microsoft.com/fwlink/?linkid=14202")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public EncoderParameter(Encoder encoder, int NumberOfValues, int Type, int Value)
        {
            int size;

            switch ((EncoderParameterValueType)Type)
            {
                case EncoderParameterValueType.ValueTypeByte:
                case EncoderParameterValueType.ValueTypeAscii:
                    size = 1;
                    break;
                case EncoderParameterValueType.ValueTypeShort:
                    size = 2;
                    break;
                case EncoderParameterValueType.ValueTypeLong:
                    size = 4;
                    break;
                case EncoderParameterValueType.ValueTypeRational:
                case EncoderParameterValueType.ValueTypeLongRange:
                    size = 2 * 4;
                    break;
                case EncoderParameterValueType.ValueTypeUndefined:
                    size = 1;
                    break;
                case EncoderParameterValueType.ValueTypeRationalRange:
                    size = 2 * 2 * 4;
                    break;
                default:
                    throw Gdip.StatusException(Gdip.WrongState);
            }

            int bytes = checked(size * NumberOfValues);

            _parameterValue = Marshal.AllocHGlobal(bytes);

            if (_parameterValue == IntPtr.Zero)
                throw Gdip.StatusException(Gdip.OutOfMemory);

            for (int i = 0; i < bytes; i++)
            {
                Marshal.WriteByte(Add(_parameterValue, i), Marshal.ReadByte((IntPtr)(Value + i)));
            }

            _parameterValueType = (EncoderParameterValueType)Type;
            _numberOfValues = NumberOfValues;
            _parameterGuid = encoder.Guid;
            GC.KeepAlive(this);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2004:RemoveCallsToGCKeepAlive")]
        public EncoderParameter(Encoder encoder, int numberValues, EncoderParameterValueType type, IntPtr value)
        {
            int size;

            switch (type)
            {
                case EncoderParameterValueType.ValueTypeByte:
                case EncoderParameterValueType.ValueTypeAscii:
                    size = 1;
                    break;
                case EncoderParameterValueType.ValueTypeShort:
                    size = 2;
                    break;
                case EncoderParameterValueType.ValueTypeLong:
                    size = 4;
                    break;
                case EncoderParameterValueType.ValueTypeRational:
                case EncoderParameterValueType.ValueTypeLongRange:
                    size = 2 * 4;
                    break;
                case EncoderParameterValueType.ValueTypeUndefined:
                    size = 1;
                    break;
                case EncoderParameterValueType.ValueTypeRationalRange:
                    size = 2 * 2 * 4;
                    break;
                default:
                    throw Gdip.StatusException(Gdip.WrongState);
            }

            int bytes = checked(size * numberValues);

            _parameterValue = Marshal.AllocHGlobal(bytes);

            if (_parameterValue == IntPtr.Zero)
                throw Gdip.StatusException(Gdip.OutOfMemory);

            for (int i = 0; i < bytes; i++)
            {
                Marshal.WriteByte(Add(_parameterValue, i), Marshal.ReadByte((IntPtr)(value + i)));
            }

            _parameterValueType = type;
            _numberOfValues = numberValues;
            _parameterGuid = encoder.Guid;
            GC.KeepAlive(this);
        }

        private static IntPtr Add(IntPtr a, int b)
        {
            return (IntPtr)((long)a + (long)b);
        }

        private static IntPtr Add(int a, IntPtr b)
        {
            return (IntPtr)((long)a + (long)b);
        }
    }
}
