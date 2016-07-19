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
    [System.AttributeUsageAttribute(System.AttributeTargets.Field, Inherited = false)]
    public sealed partial class OptionalFieldAttribute : System.Attribute
    {
        public OptionalFieldAttribute() { }
        public int VersionAdded { get { return default(int); } set { } }
    }
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
namespace System.Runtime.Serialization
{
    [System.CLSCompliantAttribute(false)]
    public abstract partial class Formatter : System.Runtime.Serialization.IFormatter
    {
        protected System.Runtime.Serialization.ObjectIDGenerator m_idGenerator;
        protected System.Collections.Queue m_objectQueue;
        protected Formatter() { }
        public abstract System.Runtime.Serialization.SerializationBinder Binder { get; set; }
        public abstract System.Runtime.Serialization.StreamingContext Context { get; set; }
        public abstract System.Runtime.Serialization.ISurrogateSelector SurrogateSelector { get; set; }
        public abstract object Deserialize(System.IO.Stream serializationStream);
        protected virtual object GetNext(out long objID) { objID = default(long); return default(object); }
        protected virtual long Schedule(object obj) { return default(long); }
        public abstract void Serialize(System.IO.Stream serializationStream, object graph);
        protected abstract void WriteArray(object obj, string name, System.Type memberType);
        protected abstract void WriteBoolean(bool val, string name);
        protected abstract void WriteByte(byte val, string name);
        protected abstract void WriteChar(char val, string name);
        protected abstract void WriteDateTime(System.DateTime val, string name);
        protected abstract void WriteDecimal(decimal val, string name);
        protected abstract void WriteDouble(double val, string name);
        protected abstract void WriteInt16(short val, string name);
        protected abstract void WriteInt32(int val, string name);
        protected abstract void WriteInt64(long val, string name);
        protected virtual void WriteMember(string memberName, object data) { }
        protected abstract void WriteObjectRef(object obj, string name, System.Type memberType);
        [System.CLSCompliantAttribute(false)]
        protected abstract void WriteSByte(sbyte val, string name);
        protected abstract void WriteSingle(float val, string name);
        protected abstract void WriteTimeSpan(System.TimeSpan val, string name);
        [System.CLSCompliantAttribute(false)]
        protected abstract void WriteUInt16(ushort val, string name);
        [System.CLSCompliantAttribute(false)]
        protected abstract void WriteUInt32(uint val, string name);
        [System.CLSCompliantAttribute(false)]
        protected abstract void WriteUInt64(ulong val, string name);
        protected abstract void WriteValueType(object obj, string name, System.Type memberType);
    }
    public partial class FormatterConverter : System.Runtime.Serialization.IFormatterConverter
    {
        public FormatterConverter() { }
        public object Convert(object value, System.Type type) { return default(object); }
        public object Convert(object value, System.TypeCode typeCode) { return default(object); }
        public bool ToBoolean(object value) { return default(bool); }
        public byte ToByte(object value) { return default(byte); }
        public char ToChar(object value) { return default(char); }
        public System.DateTime ToDateTime(object value) { return default(System.DateTime); }
        public decimal ToDecimal(object value) { return default(decimal); }
        public double ToDouble(object value) { return default(double); }
        public short ToInt16(object value) { return default(short); }
        public int ToInt32(object value) { return default(int); }
        public long ToInt64(object value) { return default(long); }
        [System.CLSCompliantAttribute(false)]
        public sbyte ToSByte(object value) { return default(sbyte); }
        public float ToSingle(object value) { return default(float); }
        public string ToString(object value) { return default(string); }
        [System.CLSCompliantAttribute(false)]
        public ushort ToUInt16(object value) { return default(ushort); }
        [System.CLSCompliantAttribute(false)]
        public uint ToUInt32(object value) { return default(uint); }
        [System.CLSCompliantAttribute(false)]
        public ulong ToUInt64(object value) { return default(ulong); }
    }
    public static partial class FormatterServices
    {
        public static void CheckTypeSecurity(System.Type t, System.Runtime.Serialization.Formatters.TypeFilterLevel securityLevel) { }
        public static object[] GetObjectData(object obj, System.Reflection.MemberInfo[] members) { return default(object[]); }
        public static object GetSafeUninitializedObject(System.Type type) { return default(object); }
        public static System.Reflection.MemberInfo[] GetSerializableMembers(System.Type type) { return default(System.Reflection.MemberInfo[]); }
        public static System.Reflection.MemberInfo[] GetSerializableMembers(System.Type type, System.Runtime.Serialization.StreamingContext context) { return default(System.Reflection.MemberInfo[]); }
        public static System.Runtime.Serialization.ISerializationSurrogate GetSurrogateForCyclicalReference(System.Runtime.Serialization.ISerializationSurrogate innerSurrogate) { return default(System.Runtime.Serialization.ISerializationSurrogate); }
        public static System.Type GetTypeFromAssembly(System.Reflection.Assembly assem, string name) { return default(System.Type); }
        public static object GetUninitializedObject(System.Type type) { return default(object); }
        public static object PopulateObjectMembers(object obj, System.Reflection.MemberInfo[] members, object[] data) { return default(object); }
    }
    public partial interface IFormatter
    {
        System.Runtime.Serialization.SerializationBinder Binder { get; set; }
        System.Runtime.Serialization.StreamingContext Context { get; set; }
        System.Runtime.Serialization.ISurrogateSelector SurrogateSelector { get; set; }
        object Deserialize(System.IO.Stream serializationStream);
        void Serialize(System.IO.Stream serializationStream, object graph);
    }
    public partial interface ISerializationSurrogate
    {
        void GetObjectData(object obj, System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context);
        object SetObjectData(object obj, System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context, System.Runtime.Serialization.ISurrogateSelector selector);
    }
    public partial interface ISurrogateSelector
    {
        void ChainSelector(System.Runtime.Serialization.ISurrogateSelector selector);
        System.Runtime.Serialization.ISurrogateSelector GetNextSelector();
        System.Runtime.Serialization.ISerializationSurrogate GetSurrogate(System.Type type, System.Runtime.Serialization.StreamingContext context, out System.Runtime.Serialization.ISurrogateSelector selector);
    }
    public partial class ObjectIDGenerator
    {
        public ObjectIDGenerator() { }
        public virtual long GetId(object obj, out bool firstTime) { firstTime = default(bool); return default(long); }
        public virtual long HasId(object obj, out bool firstTime) { firstTime = default(bool); return default(long); }
    }
    public partial class ObjectManager
    {
        public ObjectManager(System.Runtime.Serialization.ISurrogateSelector selector, System.Runtime.Serialization.StreamingContext context) { }
        public virtual void DoFixups() { }
        public virtual object GetObject(long objectID) { return default(object); }
        public virtual void RaiseDeserializationEvent() { }
        public void RaiseOnDeserializingEvent(object obj) { }
        public virtual void RecordArrayElementFixup(long arrayToBeFixed, int index, long objectRequired) { }
        public virtual void RecordArrayElementFixup(long arrayToBeFixed, int[] indices, long objectRequired) { }
        public virtual void RecordDelayedFixup(long objectToBeFixed, string memberName, long objectRequired) { }
        public virtual void RecordFixup(long objectToBeFixed, System.Reflection.MemberInfo member, long objectRequired) { }
        public virtual void RegisterObject(object obj, long objectID) { }
        public void RegisterObject(object obj, long objectID, System.Runtime.Serialization.SerializationInfo info) { }
        public void RegisterObject(object obj, long objectID, System.Runtime.Serialization.SerializationInfo info, long idOfContainingObj, System.Reflection.MemberInfo member) { }
        public void RegisterObject(object obj, long objectID, System.Runtime.Serialization.SerializationInfo info, long idOfContainingObj, System.Reflection.MemberInfo member, int[] arrayIndex) { }
    }
    public abstract partial class SerializationBinder
    {
        protected SerializationBinder() { }
        public virtual void BindToName(System.Type serializedType, out string assemblyName, out string typeName) { assemblyName = default(string); typeName = default(string); }
        public abstract System.Type BindToType(string assemblyName, string typeName);
    }
    public sealed partial class SerializationObjectManager
    {
        public SerializationObjectManager(System.Runtime.Serialization.StreamingContext context) { }
        public void RaiseOnSerializedEvent() { }
        public void RegisterObject(object obj) { }
    }
    public partial class SurrogateSelector : System.Runtime.Serialization.ISurrogateSelector
    {
        public SurrogateSelector() { }
        public virtual void AddSurrogate(System.Type type, System.Runtime.Serialization.StreamingContext context, System.Runtime.Serialization.ISerializationSurrogate surrogate) { }
        public virtual void ChainSelector(System.Runtime.Serialization.ISurrogateSelector selector) { }
        public virtual System.Runtime.Serialization.ISurrogateSelector GetNextSelector() { return default(System.Runtime.Serialization.ISurrogateSelector); }
        public virtual System.Runtime.Serialization.ISerializationSurrogate GetSurrogate(System.Type type, System.Runtime.Serialization.StreamingContext context, out System.Runtime.Serialization.ISurrogateSelector selector) { selector = default(System.Runtime.Serialization.ISurrogateSelector); return default(System.Runtime.Serialization.ISerializationSurrogate); }
        public virtual void RemoveSurrogate(System.Type type, System.Runtime.Serialization.StreamingContext context) { }
    }
} // end of System.Runtime.Serialization
namespace System.Runtime.Serialization.Formatters
{
    public enum FormatterAssemblyStyle
    {
        Full = 1,
        Simple = 0,
    }
    public enum FormatterTypeStyle
    {
        TypesAlways = 1,
        TypesWhenNeeded = 0,
        XsdString = 2,
    }
    public partial interface IFieldInfo
    {
        string[] FieldNames { get; set; }
        System.Type[] FieldTypes { get; set; }
    }
    public enum TypeFilterLevel
    {
        Full = 3,
        Low = 2,
    }
} // end of System.Runtime.Serialization.Formatters
namespace System.Runtime.Serialization.Formatters.Binary
{
    public sealed partial class BinaryFormatter : System.Runtime.Serialization.IFormatter
    {
        public BinaryFormatter() { }
        public BinaryFormatter(System.Runtime.Serialization.ISurrogateSelector selector, System.Runtime.Serialization.StreamingContext context) { }
        public System.Runtime.Serialization.Formatters.FormatterAssemblyStyle AssemblyFormat { get { return default(System.Runtime.Serialization.Formatters.FormatterAssemblyStyle); } set { } }
        public System.Runtime.Serialization.SerializationBinder Binder { get { return default(System.Runtime.Serialization.SerializationBinder); } set { } }
        public System.Runtime.Serialization.StreamingContext Context { get { return default(System.Runtime.Serialization.StreamingContext); } set { } }
        public System.Runtime.Serialization.Formatters.TypeFilterLevel FilterLevel { get { return default(System.Runtime.Serialization.Formatters.TypeFilterLevel); } set { } }
        public System.Runtime.Serialization.ISurrogateSelector SurrogateSelector { get { return default(System.Runtime.Serialization.ISurrogateSelector); } set { } }
        public System.Runtime.Serialization.Formatters.FormatterTypeStyle TypeFormat { get { return default(System.Runtime.Serialization.Formatters.FormatterTypeStyle); } set { } }
        public object Deserialize(System.IO.Stream serializationStream) { return default(object); }
        public object Deserialize(System.IO.Stream serializationStream, System.Runtime.Remoting.Messaging.HeaderHandler handler) { return default(object); }
        public void Serialize(System.IO.Stream serializationStream, object graph) { }
        public void Serialize(System.IO.Stream serializationStream, object graph, System.Runtime.Remoting.Messaging.Header[] headers) { }
        public object UnsafeDeserialize(System.IO.Stream serializationStream, System.Runtime.Remoting.Messaging.HeaderHandler handler) { return default(object); }
    }
} // end of System.Runtime.Serialization.Formatters.Binary
namespace System.Runtime.Remoting.Messaging
{
    public partial class Header
    {
        public string HeaderNamespace;
        public bool MustUnderstand;
        public string Name;
        public object Value;
        public Header(string _Name, object _Value) { }
        public Header(string _Name, object _Value, bool _MustUnderstand) { }
        public Header(string _Name, object _Value, bool _MustUnderstand, string _HeaderNamespace) { }
    }
    public delegate object HeaderHandler(System.Runtime.Remoting.Messaging.Header[] headers);
}