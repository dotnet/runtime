// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Gdip = System.Drawing.SafeNativeMethods.Gdip;

namespace System.Drawing.Imaging
{
    [StructLayout(LayoutKind.Sequential)]
    public sealed unsafe class EncoderParameter : IDisposable
    {
        private Guid _parameterGuid;                    // GUID of the parameter
        private readonly int _numberOfValues;                    // Number of the parameter values
        private readonly EncoderParameterValueType _parameterValueType;   // Value type, like ValueTypeLONG  etc.
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
            _parameterValue = Marshal.AllocHGlobal(sizeof(byte));

            *(byte*)_parameterValue = value;
            GC.KeepAlive(this);
        }

        public EncoderParameter(Encoder encoder, byte value, bool undefined)
        {
            _parameterGuid = encoder.Guid;

            _parameterValueType = undefined ? EncoderParameterValueType.ValueTypeUndefined : EncoderParameterValueType.ValueTypeByte;
            _numberOfValues = 1;
            _parameterValue = Marshal.AllocHGlobal(sizeof(byte));

            *(byte*)_parameterValue = value;
            GC.KeepAlive(this);
        }

        public EncoderParameter(Encoder encoder, short value)
        {
            _parameterGuid = encoder.Guid;

            _parameterValueType = EncoderParameterValueType.ValueTypeShort;
            _numberOfValues = 1;
            _parameterValue = Marshal.AllocHGlobal(sizeof(short));

            *(short*)_parameterValue = value;
            GC.KeepAlive(this);
        }

        public EncoderParameter(Encoder encoder, long value)
        {
            _parameterGuid = encoder.Guid;

            _parameterValueType = EncoderParameterValueType.ValueTypeLong;
            _numberOfValues = 1;
            _parameterValue = Marshal.AllocHGlobal(sizeof(int));

            *(int*)_parameterValue = unchecked((int)value);
            GC.KeepAlive(this);
        }

        public EncoderParameter(Encoder encoder, int numerator, int denominator)
        {
            _parameterGuid = encoder.Guid;

            _parameterValueType = EncoderParameterValueType.ValueTypeRational;
            _numberOfValues = 1;
            _parameterValue = Marshal.AllocHGlobal(2 * sizeof(int));

            ((int*)_parameterValue)[0] = numerator;
            ((int*)_parameterValue)[1] = denominator;
            GC.KeepAlive(this);
        }

        public EncoderParameter(Encoder encoder, long rangebegin, long rangeend)
        {
            _parameterGuid = encoder.Guid;

            _parameterValueType = EncoderParameterValueType.ValueTypeLongRange;
            _numberOfValues = 1;
            _parameterValue = Marshal.AllocHGlobal(2 * sizeof(int));

            ((int*)_parameterValue)[0] = unchecked((int)rangebegin);
            ((int*)_parameterValue)[1] = unchecked((int)rangeend);
            GC.KeepAlive(this);
        }

        public EncoderParameter(Encoder encoder,
                                int numerator1, int demoninator1,
                                int numerator2, int demoninator2)
        {
            _parameterGuid = encoder.Guid;

            _parameterValueType = EncoderParameterValueType.ValueTypeRationalRange;
            _numberOfValues = 1;
            _parameterValue = Marshal.AllocHGlobal(4 * sizeof(int));

            ((int*)_parameterValue)[0] = numerator1;
            ((int*)_parameterValue)[1] = demoninator1;
            ((int*)_parameterValue)[2] = numerator2;
            ((int*)_parameterValue)[3] = demoninator2;
            GC.KeepAlive(this);
        }

        public EncoderParameter(Encoder encoder, string value)
        {
            _parameterGuid = encoder.Guid;

            _parameterValueType = EncoderParameterValueType.ValueTypeAscii;
            _numberOfValues = value.Length;
            _parameterValue = Marshal.StringToHGlobalAnsi(value);
            GC.KeepAlive(this);
        }

        public EncoderParameter(Encoder encoder, byte[] value)
        {
            _parameterGuid = encoder.Guid;

            _parameterValueType = EncoderParameterValueType.ValueTypeByte;
            _numberOfValues = value.Length;

            _parameterValue = Marshal.AllocHGlobal(_numberOfValues);

            Marshal.Copy(value, 0, _parameterValue, _numberOfValues);
            GC.KeepAlive(this);
        }

        public EncoderParameter(Encoder encoder, byte[] value, bool undefined)
        {
            _parameterGuid = encoder.Guid;

            _parameterValueType = undefined ? EncoderParameterValueType.ValueTypeUndefined : EncoderParameterValueType.ValueTypeByte;

            _numberOfValues = value.Length;
            _parameterValue = Marshal.AllocHGlobal(_numberOfValues);

            Marshal.Copy(value, 0, _parameterValue, _numberOfValues);
            GC.KeepAlive(this);
        }

        public EncoderParameter(Encoder encoder, short[] value)
        {
            _parameterGuid = encoder.Guid;

            _parameterValueType = EncoderParameterValueType.ValueTypeShort;
            _numberOfValues = value.Length;
            _parameterValue = Marshal.AllocHGlobal(checked(_numberOfValues * sizeof(short)));

            Marshal.Copy(value, 0, _parameterValue, _numberOfValues);
            GC.KeepAlive(this);
        }

        public EncoderParameter(Encoder encoder, long[] value)
        {
            _parameterGuid = encoder.Guid;

            _parameterValueType = EncoderParameterValueType.ValueTypeLong;
            _numberOfValues = value.Length;
            _parameterValue = Marshal.AllocHGlobal(checked(_numberOfValues * sizeof(int)));

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
            _parameterValue = Marshal.AllocHGlobal(checked(_numberOfValues * 2 * sizeof(int)));

            for (int i = 0; i < _numberOfValues; i++)
            {
                ((int*)_parameterValue)[i * 2 + 0] = numerator[i];
                ((int*)_parameterValue)[i * 2 + 1] = denominator[i];
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
            _parameterValue = Marshal.AllocHGlobal(checked(_numberOfValues * 2 * sizeof(int)));

            for (int i = 0; i < _numberOfValues; i++)
            {
                ((int*)_parameterValue)[i * 2 + 0] = unchecked((int)rangebegin[i]);
                ((int*)_parameterValue)[i * 2 + 1] = unchecked((int)rangeend[i]);
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
            _parameterValue = Marshal.AllocHGlobal(checked(_numberOfValues * 4 * sizeof(int)));

            for (int i = 0; i < _numberOfValues; i++)
            {
                ((int*)_parameterValue)[i * 4 + 0] = numerator1[i];
                ((int*)_parameterValue)[i * 4 + 1] = denominator1[i];
                ((int*)_parameterValue)[i * 4 + 2] = numerator2[i];
                ((int*)_parameterValue)[i * 4 + 3] = denominator2[i];
            }
            GC.KeepAlive(this);
        }

        [Obsolete("This constructor has been deprecated. Use EncoderParameter(Encoder encoder, int numberValues, EncoderParameterValueType type, IntPtr value) instead.")]
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

            new ReadOnlySpan<byte>((void*)Value, bytes).CopyTo(new Span<byte>((void*)_parameterValue, bytes));

            _parameterValueType = (EncoderParameterValueType)Type;
            _numberOfValues = NumberOfValues;
            _parameterGuid = encoder.Guid;
            GC.KeepAlive(this);
        }

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
                case EncoderParameterValueType.ValueTypePointer:
                    size = IntPtr.Size;
                    break;
                default:
                    throw Gdip.StatusException(Gdip.WrongState);
            }

            int bytes = checked(size * numberValues);

            _parameterValue = Marshal.AllocHGlobal(bytes);

            new ReadOnlySpan<byte>((void*)value, bytes).CopyTo(new Span<byte>((void*)_parameterValue, bytes));

            _parameterValueType = type;
            _numberOfValues = numberValues;
            _parameterGuid = encoder.Guid;
            GC.KeepAlive(this);
        }
    }
}
