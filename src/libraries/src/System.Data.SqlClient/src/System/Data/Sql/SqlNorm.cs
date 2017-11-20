// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Diagnostics;
using System.Data;
using System.IO;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Text;
using System.Runtime.CompilerServices;
using System.Data.SqlTypes;

namespace Microsoft.SqlServer.Server
{
    // The class that holds the offset, field, and normalizer for
    // a particular field.
    internal sealed class FieldInfoEx : IComparable
    {
        internal readonly int Offset;
        internal readonly FieldInfo FieldInfo;
        internal readonly Normalizer Normalizer;

        internal FieldInfoEx(FieldInfo fi, int offset, Normalizer normalizer)
        {
            FieldInfo = fi;
            Offset = offset;
            Debug.Assert(normalizer != null, "normalizer argument should not be null!");
            Normalizer = normalizer;
        }

        // Sort fields by field offsets.
        public int CompareTo(object other)
        {
            FieldInfoEx otherF = other as FieldInfoEx;
            if (otherF == null)
                return -1;
            return Offset.CompareTo(otherF.Offset);
        }
    }

    // The most complex normalizer, a udt normalizer
    internal sealed class BinaryOrderedUdtNormalizer : Normalizer
    {
        internal readonly FieldInfoEx[] FieldsToNormalize;
        private int _size;
        private byte[] _padBuffer;
        internal readonly object NullInstance;
        //a boolean that tells us if a udt is a "top-level" udt,
        //i.e. one that does not require a null byte header.
        private bool _isTopLevelUdt;

        private FieldInfo[] GetFields(Type t)
        {
            return t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }

        internal BinaryOrderedUdtNormalizer(Type t, bool isTopLevelUdt)
        {
            _skipNormalize = false;
            if (_skipNormalize)
            {
                // if skipping normalization, dont write the null
                // byte header for IsNull
                _isTopLevelUdt = true;
            }

            _isTopLevelUdt = true;

            // get all the fields
            FieldInfo[] fields = GetFields(t);

            FieldsToNormalize = new FieldInfoEx[fields.Length];

            int i = 0;

            foreach (FieldInfo fi in fields)
            {
                int offset = Marshal.OffsetOf(fi.DeclaringType, fi.Name).ToInt32();
                FieldsToNormalize[i++] = new FieldInfoEx(fi, offset, GetNormalizer(fi.FieldType));
            }

            //sort by offset
            Array.Sort(FieldsToNormalize);
            //if this is not a top-level udt, do setup for null values.
            //null values need to compare less than all other values,
            //so prefix a null byte indicator.
            if (!_isTopLevelUdt)
            {
                //get the null value for this type, special case for sql types, which
                //have a null field
                if (typeof(INullable).IsAssignableFrom(t))
                {
                    PropertyInfo pi = t.GetProperty("Null",
                    BindingFlags.Public | BindingFlags.Static);
                    if (pi == null || pi.PropertyType != t)
                    {
                        FieldInfo fi = t.GetField("Null", BindingFlags.Public | BindingFlags.Static);
                        if (fi == null || fi.FieldType != t)
                            throw new Exception("could not find Null field/property in nullable type " + t);
                        else
                            NullInstance = fi.GetValue(null);
                    }
                    else
                    {
                        NullInstance = pi.GetValue(null, null);
                    }
                    //create the padding buffer
                    _padBuffer = new byte[Size - 1];
                }
            }
        }

        internal bool IsNullable => (NullInstance != null);

        // Normalize the top-level udt
        internal void NormalizeTopObject(object udt, Stream s)
        {
            Normalize(null, udt, s);
        }

        // Denormalize a top-level udt and return it
        internal object DeNormalizeTopObject(Type t, Stream s) => DeNormalizeInternal(t, s);

        // Prevent inlining so that reflection calls are not moved to caller that may be in a different assembly that may have a different grant set.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private object DeNormalizeInternal(Type t, Stream s)
        {
            object result = null;
            //if nullable and not the top object, read the null marker
            if (!_isTopLevelUdt && typeof(INullable).IsAssignableFrom(t))
            {
                byte nullByte = (byte)s.ReadByte();
                if (nullByte == 0)
                {
                    result = NullInstance;
                    s.Read(_padBuffer, 0, _padBuffer.Length);
                    return result;
                }
            }
            if (result == null)
                result = Activator.CreateInstance(t);
            foreach (FieldInfoEx myField in FieldsToNormalize)
            {
                myField.Normalizer.DeNormalize(myField.FieldInfo, result, s);
            }
            return result;
        }

        internal override void Normalize(FieldInfo fi, object obj, Stream s)
        {
            object inner;
            if (fi == null)
            {
                inner = obj;
            }
            else
            {
                inner = GetValue(fi, obj);
            }

            // If nullable and not the top object, write a null indicator
            if (inner is INullable oNullable && !_isTopLevelUdt)
            {
                if (oNullable.IsNull)
                {
                    s.WriteByte(0);
                    s.Write(_padBuffer, 0, _padBuffer.Length);
                    return;
                }
                else
                {
                    s.WriteByte(1);
                }
            }

            foreach (FieldInfoEx myField in FieldsToNormalize)
            {
                myField.Normalizer.Normalize(myField.FieldInfo, inner, s);
            }
        }

        internal override void DeNormalize(FieldInfo fi, object recvr, Stream s)
        {
            SetValue(fi, recvr, DeNormalizeInternal(fi.FieldType, s));
        }

        internal override int Size
        {
            get
            {
                if (_size != 0)
                    return _size;
                if (IsNullable && !_isTopLevelUdt)
                    _size = 1;
                foreach (FieldInfoEx myField in FieldsToNormalize)
                {
                    _size += myField.Normalizer.Size;
                }
                return _size;
            }
        }
    }

    internal abstract class Normalizer
    {
        protected bool _skipNormalize;

        internal static Normalizer GetNormalizer(Type t)
        {
            Normalizer n = null;
            if (t.IsPrimitive)
            {
                if (t == typeof(byte))
                    n = new ByteNormalizer();
                else if (t == typeof(sbyte))
                    n = new SByteNormalizer();
                else if (t == typeof(bool))
                    n = new BooleanNormalizer();
                else if (t == typeof(short))
                    n = new ShortNormalizer();
                else if (t == typeof(ushort))
                    n = new UShortNormalizer();
                else if (t == typeof(int))
                    n = new IntNormalizer();
                else if (t == typeof(uint))
                    n = new UIntNormalizer();
                else if (t == typeof(float))
                    n = new FloatNormalizer();
                else if (t == typeof(double))
                    n = new DoubleNormalizer();
                else if (t == typeof(long))
                    n = new LongNormalizer();
                else if (t == typeof(ulong))
                    n = new ULongNormalizer();
            }
            else if (t.IsValueType)
            {
                n = new BinaryOrderedUdtNormalizer(t, false);
            }
            if (n == null)
                throw new Exception(SR.GetString(SR.SQL_CannotCreateNormalizer, t.FullName));
            n._skipNormalize = false;
            return n;
        }

        internal abstract void Normalize(FieldInfo fi, object recvr, Stream s);

        internal abstract void DeNormalize(FieldInfo fi, object recvr, Stream s);

        protected void FlipAllBits(byte[] b)
        {
            for (int i = 0; i < b.Length; i++)
                b[i] = (byte)~b[i];
        }

        protected object GetValue(FieldInfo fi, object obj) => fi.GetValue(obj);

        protected void SetValue(FieldInfo fi, object recvr, object value)
        {
            fi.SetValue(recvr, value);
        }

        internal abstract int Size { get; }
    }

    internal sealed class BooleanNormalizer : Normalizer
    {
        internal override void Normalize(FieldInfo fi, object obj, Stream s)
        {
            bool b = (bool)GetValue(fi, obj);
            s.WriteByte((byte)(b ? 1 : 0));
        }

        internal override void DeNormalize(FieldInfo fi, object recvr, Stream s)
        {
            byte b = (byte)s.ReadByte();
            SetValue(fi, recvr, b == 1);
        }

        internal override int Size => 1;
    }

    internal sealed class SByteNormalizer : Normalizer
    {
        internal override void Normalize(FieldInfo fi, object obj, Stream s)
        {
            sbyte sb = (sbyte)GetValue(fi, obj);
            byte b;
            unchecked
            {
                b = (byte)sb;
            }
            if (!_skipNormalize)
                b ^= 0x80; // flip the sign bit
            s.WriteByte(b);
        }

        internal override void DeNormalize(FieldInfo fi, object recvr, Stream s)
        {
            byte b = (byte)s.ReadByte();
            if (!_skipNormalize)
                b ^= 0x80; // flip the sign bit
            sbyte sb;
            unchecked
            {
                sb = (sbyte)b;
            }
            SetValue(fi, recvr, sb);
        }

        internal override int Size => 1;
    }

    internal sealed class ByteNormalizer : Normalizer
    {
        internal override void Normalize(FieldInfo fi, object obj, Stream s)
        {
            byte b = (byte)GetValue(fi, obj);
            s.WriteByte(b);
        }

        internal override void DeNormalize(FieldInfo fi, object recvr, Stream s)
        {
            byte b = (byte)s.ReadByte();
            SetValue(fi, recvr, b);
        }

        internal override int Size => 1;
    }

    internal sealed class ShortNormalizer : Normalizer
    {
        internal override void Normalize(FieldInfo fi, object obj, Stream s)
        {
            byte[] b = BitConverter.GetBytes((short)GetValue(fi, obj));
            if (!_skipNormalize)
            {
                Array.Reverse(b);
                b[0] ^= 0x80;
            }
            s.Write(b, 0, b.Length);
        }

        internal override void DeNormalize(FieldInfo fi, object recvr, Stream s)
        {
            byte[] b = new byte[2];
            s.Read(b, 0, b.Length);
            if (!_skipNormalize)
            {
                b[0] ^= 0x80;
                Array.Reverse(b);
            }
            SetValue(fi, recvr, BitConverter.ToInt16(b, 0));
        }

        internal override int Size { get { return 2; } }
    }

    internal sealed class UShortNormalizer : Normalizer
    {
        internal override void Normalize(FieldInfo fi, object obj, Stream s)
        {
            byte[] b = BitConverter.GetBytes((ushort)GetValue(fi, obj));
            if (!_skipNormalize)
            {
                Array.Reverse(b);
            }
            s.Write(b, 0, b.Length);
        }

        internal override void DeNormalize(FieldInfo fi, object recvr, Stream s)
        {
            byte[] b = new byte[2];
            s.Read(b, 0, b.Length);
            if (!_skipNormalize)
            {
                Array.Reverse(b);
            }
            SetValue(fi, recvr, BitConverter.ToUInt16(b, 0));
        }

        internal override int Size => 2;
    }

    internal sealed class IntNormalizer : Normalizer
    {
        internal override void Normalize(FieldInfo fi, object obj, Stream s)
        {
            byte[] b = BitConverter.GetBytes((int)GetValue(fi, obj));
            if (!_skipNormalize)
            {
                Array.Reverse(b);
                b[0] ^= 0x80;
            }
            s.Write(b, 0, b.Length);
        }

        internal override void DeNormalize(FieldInfo fi, object recvr, Stream s)
        {
            byte[] b = new byte[4];
            s.Read(b, 0, b.Length);
            if (!_skipNormalize)
            {
                b[0] ^= 0x80;
                Array.Reverse(b);
            }
            SetValue(fi, recvr, BitConverter.ToInt32(b, 0));
        }

        internal override int Size => 4;
    }

    internal sealed class UIntNormalizer : Normalizer
    {
        internal override void Normalize(FieldInfo fi, object obj, Stream s)
        {
            byte[] b = BitConverter.GetBytes((uint)GetValue(fi, obj));
            if (!_skipNormalize)
            {
                Array.Reverse(b);
            }
            s.Write(b, 0, b.Length);
        }

        internal override void DeNormalize(FieldInfo fi, object recvr, Stream s)
        {
            byte[] b = new byte[4];
            s.Read(b, 0, b.Length);
            if (!_skipNormalize)
            {
                Array.Reverse(b);
            }
            SetValue(fi, recvr, BitConverter.ToUInt32(b, 0));
        }

        internal override int Size => 4;
    }

    internal sealed class LongNormalizer : Normalizer
    {
        internal override void Normalize(FieldInfo fi, object obj, Stream s)
        {
            byte[] b = BitConverter.GetBytes((long)GetValue(fi, obj));
            if (!_skipNormalize)
            {
                Array.Reverse(b);
                b[0] ^= 0x80;
            }
            s.Write(b, 0, b.Length);
        }

        internal override void DeNormalize(FieldInfo fi, object recvr, Stream s)
        {
            byte[] b = new byte[8];
            s.Read(b, 0, b.Length);
            if (!_skipNormalize)
            {
                b[0] ^= 0x80;
                Array.Reverse(b);
            }
            SetValue(fi, recvr, BitConverter.ToInt64(b, 0));
        }

        internal override int Size => 8;
    }

    internal sealed class ULongNormalizer : Normalizer
    {
        internal override void Normalize(FieldInfo fi, object obj, Stream s)
        {
            byte[] b = BitConverter.GetBytes((ulong)GetValue(fi, obj));
            if (!_skipNormalize)
            {
                Array.Reverse(b);
            }
            s.Write(b, 0, b.Length);
        }

        internal override void DeNormalize(FieldInfo fi, object recvr, Stream s)
        {
            byte[] b = new byte[8];
            s.Read(b, 0, b.Length);
            if (!_skipNormalize)
            {
                Array.Reverse(b);
            }
            SetValue(fi, recvr, BitConverter.ToUInt64(b, 0));
        }

        internal override int Size => 8;
    }

    internal sealed class FloatNormalizer : Normalizer
    {
        internal override void Normalize(FieldInfo fi, object obj, Stream s)
        {
            float f = (float)GetValue(fi, obj);
            byte[] b = BitConverter.GetBytes(f);
            if (!_skipNormalize)
            {
                Array.Reverse(b);
                if ((b[0] & 0x80) == 0)
                {
                    // This is a positive number.
                    // Flip the highest bit
                    b[0] ^= 0x80;
                }
                else
                {
                    // This is a negative number.

                    // If all zeroes, means it was a negative zero.
                    // Treat it same as positive zero, so that
                    // the normalized key will compare equal.
                    if (f < 0)
                        FlipAllBits(b);
                }
            }
            s.Write(b, 0, b.Length);
        }

        internal override void DeNormalize(FieldInfo fi, object recvr, Stream s)
        {
            byte[] b = new byte[4];
            s.Read(b, 0, b.Length);
            if (!_skipNormalize)
            {
                if ((b[0] & 0x80) > 0)
                {
                    // This is a positive number.
                    // Flip the highest bit
                    b[0] ^= 0x80;
                }
                else
                {
                    // This is a negative number.
                    FlipAllBits(b);
                }
                Array.Reverse(b);
            }
            SetValue(fi, recvr, BitConverter.ToSingle(b, 0));
        }

        internal override int Size => 4;
    }

    internal sealed class DoubleNormalizer : Normalizer
    {
        internal override void Normalize(FieldInfo fi, object obj, Stream s)
        {
            double d = (double)GetValue(fi, obj);
            byte[] b = BitConverter.GetBytes(d);
            if (!_skipNormalize)
            {
                Array.Reverse(b);
                if ((b[0] & 0x80) == 0)
                {
                    // This is a positive number.
                    // Flip the highest bit
                    b[0] ^= 0x80;
                }
                else
                {
                    // This is a negative number.
                    if (d < 0)
                    {
                        // If all zeroes, means it was a negative zero.
                        // Treat it same as positive zero, so that
                        // the normalized key will compare equal.
                        FlipAllBits(b);
                    }
                }
            }
            s.Write(b, 0, b.Length);
        }

        internal override void DeNormalize(FieldInfo fi, object recvr, Stream s)
        {
            byte[] b = new byte[8];
            s.Read(b, 0, b.Length);
            if (!_skipNormalize)
            {
                if ((b[0] & 0x80) > 0)
                {
                    // This is a positive number.
                    // Flip the highest bit
                    b[0] ^= 0x80;
                }
                else
                {
                    // This is a negative number.
                    FlipAllBits(b);
                }
                Array.Reverse(b);
            }
            SetValue(fi, recvr, BitConverter.ToDouble(b, 0));
        }

        internal override int Size => 8;
    }
}