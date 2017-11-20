// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Data.Common;
using System.Data.SqlClient;
using System.IO;
using System.Runtime.CompilerServices;

namespace Microsoft.SqlServer.Server
{
    internal class SerializationHelperSql9
    {
        // Don't let anyone create an instance of this class.
        private SerializationHelperSql9() { }

        // Get the m_size of the serialized stream for this type, in bytes.
        // This method creates an instance of the type using the public
        // no-argument constructor, serializes it, and returns the m_size
        // in bytes.
        // Prevent inlining so that reflection calls are not moved to caller that may be in a different assembly that may have a different grant set.
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static int SizeInBytes(Type t) => SizeInBytes(Activator.CreateInstance(t));

        // Get the m_size of the serialized stream for this type, in bytes.
        internal static int SizeInBytes(object instance)
        {
            Type t = instance.GetType();
            Format k = GetFormat(t);
            DummyStream stream = new DummyStream();
            Serializer ser = GetSerializer(instance.GetType());
            ser.Serialize(stream, instance);
            return (int)stream.Length;
        }

        internal static void Serialize(Stream s, object instance)
        {
            GetSerializer(instance.GetType()).Serialize(s, instance);
        }

        internal static object Deserialize(Stream s, Type resultType) => GetSerializer(resultType).Deserialize(s);

        private static Format GetFormat(Type t) => GetUdtAttribute(t).Format;

        // Cache the relationship between a type and its serializer.
        // This is expensive to compute since it involves traversing the
        // custom attributes of the type using reflection.
        //
        // Use a per-thread cache, so that there are no synchronization
        // issues when accessing cache entries from multiple threads.
        [ThreadStatic]
        private static Hashtable s_types2Serializers;

        private static Serializer GetSerializer(Type t)
        {
            if (s_types2Serializers == null)
                s_types2Serializers = new Hashtable();

            Serializer s = (Serializer)s_types2Serializers[t];
            if (s == null)
            {
                s = GetNewSerializer(t);
                s_types2Serializers[t] = s;
            }
            return s;
        }

        internal static int GetUdtMaxLength(Type t)
        {
            SqlUdtInfo udtInfo = SqlUdtInfo.GetFromType(t);

            if (Format.Native == udtInfo.SerializationFormat)
            {
                // In the native format, the user does not specify the
                // max byte size, it is computed from the type definition
                return SizeInBytes(t);
            }
            else
            {
                // In all other formats, the user specifies the maximum size in bytes.
                return udtInfo.MaxByteSize;
            }
        }

        private static object[] GetCustomAttributes(Type t)
        {
            return t.GetCustomAttributes(typeof(SqlUserDefinedTypeAttribute), false);
        }

        internal static SqlUserDefinedTypeAttribute GetUdtAttribute(Type t)
        {
            SqlUserDefinedTypeAttribute udtAttr = null;
            object[] attr = GetCustomAttributes(t);

            if (attr != null && attr.Length == 1)
            {
                udtAttr = (SqlUserDefinedTypeAttribute)attr[0];
            }
            else
            {
                throw InvalidUdtException.Create(t, SR.SqlUdtReason_NoUdtAttribute);
            }
            return udtAttr;
        }

        // Create a new serializer for the given type.
        private static Serializer GetNewSerializer(Type t)
        {
            SqlUserDefinedTypeAttribute udtAttr = GetUdtAttribute(t);
            Format k = GetFormat(t);

            switch (k)
            {
                case Format.Native:
                    return new NormalizedSerializer(t);
                case Format.UserDefined:
                    return new BinarySerializeSerializer(t);
                case Format.Unknown: // should never happen, but fall through
                default:
                    throw ADP.InvalidUserDefinedTypeSerializationFormat(k);
            }
        }
    }

    // The base serializer class.
    internal abstract class Serializer
    {
        public abstract object Deserialize(Stream s);
        public abstract void Serialize(Stream s, object o);
        protected Type _type;

        protected Serializer(Type t)
        {
            _type = t;
        }
    }

    internal sealed class NormalizedSerializer : Serializer
    {
        private BinaryOrderedUdtNormalizer _normalizer;
        private bool _isFixedSize;
        private int _maxSize;

        internal NormalizedSerializer(Type t) : base(t)
        {
            SqlUserDefinedTypeAttribute udtAttr = SerializationHelperSql9.GetUdtAttribute(t);
            _normalizer = new BinaryOrderedUdtNormalizer(t, true);
            _isFixedSize = udtAttr.IsFixedLength;
            _maxSize = _normalizer.Size;
        }

        public override void Serialize(Stream s, object o)
        {
            _normalizer.NormalizeTopObject(o, s);
        }

        public override object Deserialize(Stream s) => _normalizer.DeNormalizeTopObject(_type, s);
    }

    internal sealed class BinarySerializeSerializer : Serializer
    {
        internal BinarySerializeSerializer(Type t) : base(t)
        {
        }

        public override void Serialize(Stream s, object o)
        {
            BinaryWriter w = new BinaryWriter(s);
            ((IBinarySerialize)o).Write(w);
        }

        // Prevent inlining so that reflection calls are not moved
        // to a caller that may be in a different assembly that may
        // have a different grant set.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public override object Deserialize(Stream s)
        {
            object instance = Activator.CreateInstance(_type);
            BinaryReader r = new BinaryReader(s);
            ((IBinarySerialize)instance).Read(r);
            return instance;
        }
    }

    // A dummy stream class, used to get the number of bytes written
    // to the stream.
    internal sealed class DummyStream : Stream
    {
        private long _size;

        public DummyStream()
        {
        }

        private void DontDoIt()
        {
            throw new Exception(SR.GetString(SR.Sql_InternalError));
        }

        public override bool CanRead => false;

        public override bool CanWrite => true;

        public override bool CanSeek => false;

        public override long Position
        {
            get
            {
                return _size;
            }
            set
            {
                _size = value;
            }
        }

        public override long Length => _size;

        public override void SetLength(long value)
        {
            _size = value;
        }

        public override long Seek(long value, SeekOrigin loc)
        {
            DontDoIt();
            return -1;
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            DontDoIt();
            return -1;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _size += count;
        }
    }
}