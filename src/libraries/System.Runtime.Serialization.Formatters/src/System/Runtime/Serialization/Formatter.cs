// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Globalization;
using System.Collections;
using System.Reflection;

namespace System.Runtime.Serialization
{
    [Serializable]
    [CLSCompliant(false)]
    public abstract class Formatter : IFormatter
    {
        protected ObjectIDGenerator m_idGenerator;
        protected Queue m_objectQueue;

        protected Formatter()
        {
            m_objectQueue = new Queue();
            m_idGenerator = new ObjectIDGenerator();
        }

        public abstract object Deserialize(Stream serializationStream);

        protected virtual object GetNext(out long objID)
        {
            if (m_objectQueue.Count == 0)
            {
                objID = 0;
                return null;
            }

            object obj = m_objectQueue.Dequeue();

            bool isNew;
            objID = m_idGenerator.HasId(obj, out isNew);
            if (isNew)
            {
                throw new SerializationException(SR.Serialization_NoID);
            }

            return obj;
        }

        protected virtual long Schedule(object obj)
        {
            if (obj == null)
            {
                return 0;
            }

            bool isNew;
            long id = m_idGenerator.GetId(obj, out isNew);

            if (isNew)
            {
                m_objectQueue.Enqueue(obj);
            }
            return id;
        }

        public abstract void Serialize(Stream serializationStream, object graph);

        protected abstract void WriteArray(object obj, string name, Type memberType);

        protected abstract void WriteBoolean(bool val, string name);

        protected abstract void WriteByte(byte val, string name);

        protected abstract void WriteChar(char val, string name);

        protected abstract void WriteDateTime(DateTime val, string name);

        protected abstract void WriteDecimal(decimal val, string name);

        protected abstract void WriteDouble(double val, string name);

        protected abstract void WriteInt16(short val, string name);

        protected abstract void WriteInt32(int val, string name);

        protected abstract void WriteInt64(long val, string name);

        protected abstract void WriteObjectRef(object obj, string name, Type memberType);

        protected virtual void WriteMember(string memberName, object data)
        {
            if (data == null)
            {
                WriteObjectRef(data, memberName, typeof(object));
                return;
            }

            Type varType = data.GetType();

            if (varType == typeof(bool))
            {
                WriteBoolean(Convert.ToBoolean(data, CultureInfo.InvariantCulture), memberName);
            }
            else if (varType == typeof(char))
            {
                WriteChar(Convert.ToChar(data, CultureInfo.InvariantCulture), memberName);
            }
            else if (varType == typeof(sbyte))
            {
                WriteSByte(Convert.ToSByte(data, CultureInfo.InvariantCulture), memberName);
            }
            else if (varType == typeof(byte))
            {
                WriteByte(Convert.ToByte(data, CultureInfo.InvariantCulture), memberName);
            }
            else if (varType == typeof(short))
            {
                WriteInt16(Convert.ToInt16(data, CultureInfo.InvariantCulture), memberName);
            }
            else if (varType == typeof(int))
            {
                WriteInt32(Convert.ToInt32(data, CultureInfo.InvariantCulture), memberName);
            }
            else if (varType == typeof(long))
            {
                WriteInt64(Convert.ToInt64(data, CultureInfo.InvariantCulture), memberName);
            }
            else if (varType == typeof(float))
            {
                WriteSingle(Convert.ToSingle(data, CultureInfo.InvariantCulture), memberName);
            }
            else if (varType == typeof(double))
            {
                WriteDouble(Convert.ToDouble(data, CultureInfo.InvariantCulture), memberName);
            }
            else if (varType == typeof(DateTime))
            {
                WriteDateTime(Convert.ToDateTime(data, CultureInfo.InvariantCulture), memberName);
            }
            else if (varType == typeof(decimal))
            {
                WriteDecimal(Convert.ToDecimal(data, CultureInfo.InvariantCulture), memberName);
            }
            else if (varType == typeof(ushort))
            {
                WriteUInt16(Convert.ToUInt16(data, CultureInfo.InvariantCulture), memberName);
            }
            else if (varType == typeof(uint))
            {
                WriteUInt32(Convert.ToUInt32(data, CultureInfo.InvariantCulture), memberName);
            }
            else if (varType == typeof(ulong))
            {
                WriteUInt64(Convert.ToUInt64(data, CultureInfo.InvariantCulture), memberName);
            }
            else if (varType.IsArray)
            {
                WriteArray(data, memberName, varType);
            }
            else if (varType.IsValueType)
            {
                WriteValueType(data, memberName, varType);
            }
            else
            {
                WriteObjectRef(data, memberName, varType);
            }
        }

        [CLSCompliant(false)]
        protected abstract void WriteSByte(sbyte val, string name);

        protected abstract void WriteSingle(float val, string name);

        protected abstract void WriteTimeSpan(TimeSpan val, string name);

        [CLSCompliant(false)]
        protected abstract void WriteUInt16(ushort val, string name);

        [CLSCompliant(false)]
        protected abstract void WriteUInt32(uint val, string name);

        [CLSCompliant(false)]
        protected abstract void WriteUInt64(ulong val, string name);

        protected abstract void WriteValueType(object obj, string name, Type memberType);

        public abstract ISurrogateSelector SurrogateSelector { get; set; }

        public abstract SerializationBinder Binder { get; set; }

        public abstract StreamingContext Context { get; set; }
    }
}
