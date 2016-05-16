// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

using System;
using System.Collections;

namespace System
{
    [AttributeUsage(AttributeTargets.Field, Inherited = false)]
    public sealed class NonSerializedAttribute : Attribute
    {
        public NonSerializedAttribute()
        {
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Delegate, Inherited = false)]
    public sealed class SerializableAttribute : Attribute
    {
        public SerializableAttribute()
        {
        }
    }
}

namespace System.Runtime.Serialization
{
    [CLSCompliant(false)]
    public interface IFormatterConverter
    {
        object Convert(object value, Type type);
        object Convert(object value, TypeCode typeCode);
        bool ToBoolean(object value);
        char ToChar(object value);
        [CLSCompliant(false)]
        sbyte ToSByte(object value);
        byte ToByte(object value);
        short ToInt16(object value);
        [CLSCompliant(false)]
        ushort ToUInt16(object value);
        int ToInt32(object value);
        [CLSCompliant(false)]
        uint ToUInt32(object value);
        long ToInt64(object value);
        [CLSCompliant(false)]
        ulong ToUInt64(object value);
        float ToSingle(object value);
        double ToDouble(object value);
        Decimal ToDecimal(object value);
        DateTime ToDateTime(object value);
        String ToString(object value);
    }

    public interface ISerializable
    {
        void GetObjectData(SerializationInfo info, StreamingContext context);
    }

    public interface IDeserializationCallback
    {
        void OnDeserialization(object sender);
    }

    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public struct SerializationEntry
    {
        public string Name { get { throw null; } }
        public Type ObjectType { get { throw null; } }
        public object Value { get { throw null; } }
    }

    public sealed class SerializationInfo
    {
        [CLSCompliant(false)]
        public SerializationInfo(Type type, IFormatterConverter converter) { }
        public string AssemblyName { get { throw null; } set { } }
        public string FullTypeName { get { throw null; } set { } }
        public int MemberCount { get { throw null; } }
        public Type ObjectType { get { throw null; } }
        public void AddValue(string name, bool value) { }
        public void AddValue(string name, byte value) { }
        public void AddValue(string name, char value) { }
        public void AddValue(string name, DateTime value) { }
        public void AddValue(string name, decimal value) { }
        public void AddValue(string name, double value) { }
        public void AddValue(string name, short value) { }
        public void AddValue(string name, int value) { }
        public void AddValue(string name, long value) { }
        public void AddValue(string name, object value) { }
        public void AddValue(string name, object value, Type type) { }
        [CLSCompliant(false)]
        public void AddValue(string name, sbyte value) { }
        public void AddValue(string name, float value) { }
        [CLSCompliant(false)]
        public void AddValue(string name, ushort value) { }
        [CLSCompliant(false)]
        public void AddValue(string name, uint value) { }
        [CLSCompliant(false)]
        public void AddValue(string name, ulong value) { }
        public bool GetBoolean(string name) { throw null; }
        public byte GetByte(string name) { throw null; }
        public char GetChar(string name) { throw null; }
        public DateTime GetDateTime(string name) { throw null; }
        public decimal GetDecimal(string name) { throw null; }
        public double GetDouble(string name) { throw null; }
        public SerializationInfoEnumerator GetEnumerator() { throw null; }
        public short GetInt16(string name) { throw null; }
        public int GetInt32(string name) { throw null; }
        public long GetInt64(string name) { throw null; }
        [CLSCompliant(false)]
        public sbyte GetSByte(string name) { throw null; }
        public float GetSingle(string name) { throw null; }
        public string GetString(string name) { throw null; }
        [CLSCompliant(false)]
        public ushort GetUInt16(string name) { throw null; }
        [CLSCompliant(false)]
        public uint GetUInt32(string name) { throw null; }
        [CLSCompliant(false)]
        public ulong GetUInt64(string name) { throw null; }
        public object GetValue(string name, Type type) { throw null; }
        public void SetType(Type type) { }
    }

    public sealed class SerializationInfoEnumerator : IEnumerator
    {
        private SerializationInfoEnumerator() { }
        public SerializationEntry Current { get { throw null; } }
        public string Name { get { throw null; } }
        public Type ObjectType { get { throw null; } }
        object System.Collections.IEnumerator.Current { get { throw null; } }
        public object Value { get { throw null; } }
        public bool MoveNext() { throw null; }
        public void Reset() { throw null; }
    }
}
