// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Runtime.Serialization
{
    [System.CLSCompliantAttribute(false)]
    public abstract partial class Formatter : System.Runtime.Serialization.IFormatter
    {
        protected System.Runtime.Serialization.ObjectIDGenerator m_idGenerator;
        protected System.Collections.Queue m_objectQueue;
        protected Formatter() { }
        public abstract System.Runtime.Serialization.SerializationBinder? Binder { get; set; }
        public abstract System.Runtime.Serialization.StreamingContext Context { get; set; }
        public abstract System.Runtime.Serialization.ISurrogateSelector? SurrogateSelector { get; set; }
        [System.ObsoleteAttribute("BinaryFormatter serialization is obsolete and should not be used. See https://aka.ms/binaryformatter for more information.", DiagnosticId = "SYSLIB0011", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
        public abstract object Deserialize(System.IO.Stream serializationStream);
        protected virtual object? GetNext(out long objID) { throw null; }
        protected virtual long Schedule(object? obj) { throw null; }
        [System.ObsoleteAttribute("BinaryFormatter serialization is obsolete and should not be used. See https://aka.ms/binaryformatter for more information.", DiagnosticId = "SYSLIB0011", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
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
        protected virtual void WriteMember(string memberName, object? data) { }
        protected abstract void WriteObjectRef(object? obj, string name, System.Type memberType);
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
        public object Convert(object value, System.Type type) { throw null; }
        public object Convert(object value, System.TypeCode typeCode) { throw null; }
        public bool ToBoolean(object value) { throw null; }
        public byte ToByte(object value) { throw null; }
        public char ToChar(object value) { throw null; }
        public System.DateTime ToDateTime(object value) { throw null; }
        public decimal ToDecimal(object value) { throw null; }
        public double ToDouble(object value) { throw null; }
        public short ToInt16(object value) { throw null; }
        public int ToInt32(object value) { throw null; }
        public long ToInt64(object value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public sbyte ToSByte(object value) { throw null; }
        public float ToSingle(object value) { throw null; }
        public string? ToString(object value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public ushort ToUInt16(object value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public uint ToUInt32(object value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public ulong ToUInt64(object value) { throw null; }
    }
    public static partial class FormatterServices
    {
        public static void CheckTypeSecurity(System.Type t, System.Runtime.Serialization.Formatters.TypeFilterLevel securityLevel) { }
        public static object?[] GetObjectData(object obj, System.Reflection.MemberInfo[] members) { throw null; }
        public static object GetSafeUninitializedObject(System.Type type) { throw null; }
        public static System.Reflection.MemberInfo[] GetSerializableMembers(System.Type type) { throw null; }
        public static System.Reflection.MemberInfo[] GetSerializableMembers(System.Type type, System.Runtime.Serialization.StreamingContext context) { throw null; }
        public static System.Runtime.Serialization.ISerializationSurrogate GetSurrogateForCyclicalReference(System.Runtime.Serialization.ISerializationSurrogate innerSurrogate) { throw null; }
        public static System.Type? GetTypeFromAssembly(System.Reflection.Assembly assem, string name) { throw null; }
        public static object GetUninitializedObject(System.Type type) { throw null; }
        public static object PopulateObjectMembers(object obj, System.Reflection.MemberInfo[] members, object?[] data) { throw null; }
    }
    public partial interface IFormatter
    {
        System.Runtime.Serialization.SerializationBinder? Binder { get; set; }
        System.Runtime.Serialization.StreamingContext Context { get; set; }
        System.Runtime.Serialization.ISurrogateSelector? SurrogateSelector { get; set; }
        [System.ObsoleteAttribute("BinaryFormatter serialization is obsolete and should not be used. See https://aka.ms/binaryformatter for more information.", DiagnosticId = "SYSLIB0011", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
        object Deserialize(System.IO.Stream serializationStream);
        [System.ObsoleteAttribute("BinaryFormatter serialization is obsolete and should not be used. See https://aka.ms/binaryformatter for more information.", DiagnosticId = "SYSLIB0011", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
        void Serialize(System.IO.Stream serializationStream, object graph);
    }
    public partial interface ISerializationSurrogate
    {
        void GetObjectData(object obj, System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context);
        object SetObjectData(object obj, System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context, System.Runtime.Serialization.ISurrogateSelector? selector);
    }
    public partial interface ISurrogateSelector
    {
        void ChainSelector(System.Runtime.Serialization.ISurrogateSelector selector);
        System.Runtime.Serialization.ISurrogateSelector? GetNextSelector();
        System.Runtime.Serialization.ISerializationSurrogate? GetSurrogate(System.Type type, System.Runtime.Serialization.StreamingContext context, out System.Runtime.Serialization.ISurrogateSelector selector);
    }
    public partial class ObjectIDGenerator
    {
        public ObjectIDGenerator() { }
        public virtual long GetId(object obj, out bool firstTime) { throw null; }
        public virtual long HasId(object obj, out bool firstTime) { throw null; }
    }
    public partial class ObjectManager
    {
        public ObjectManager(System.Runtime.Serialization.ISurrogateSelector? selector, System.Runtime.Serialization.StreamingContext context) { }
        public virtual void DoFixups() { }
        public virtual object? GetObject(long objectID) { throw null; }
        public virtual void RaiseDeserializationEvent() { }
        public void RaiseOnDeserializingEvent(object obj) { }
        public virtual void RecordArrayElementFixup(long arrayToBeFixed, int index, long objectRequired) { }
        public virtual void RecordArrayElementFixup(long arrayToBeFixed, int[] indices, long objectRequired) { }
        public virtual void RecordDelayedFixup(long objectToBeFixed, string memberName, long objectRequired) { }
        public virtual void RecordFixup(long objectToBeFixed, System.Reflection.MemberInfo member, long objectRequired) { }
        public virtual void RegisterObject(object obj, long objectID) { }
        public void RegisterObject(object obj, long objectID, System.Runtime.Serialization.SerializationInfo info) { }
        public void RegisterObject(object obj, long objectID, System.Runtime.Serialization.SerializationInfo? info, long idOfContainingObj, System.Reflection.MemberInfo? member) { }
        public void RegisterObject(object obj, long objectID, System.Runtime.Serialization.SerializationInfo? info, long idOfContainingObj, System.Reflection.MemberInfo? member, int[]? arrayIndex) { }
    }
    public abstract partial class SerializationBinder
    {
        protected SerializationBinder() { }
        public virtual void BindToName(System.Type serializedType, out string? assemblyName, out string? typeName) { throw null; }
        public abstract System.Type? BindToType(string assemblyName, string typeName);
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
        public virtual System.Runtime.Serialization.ISurrogateSelector? GetNextSelector() { throw null; }
        public virtual System.Runtime.Serialization.ISerializationSurrogate? GetSurrogate(System.Type type, System.Runtime.Serialization.StreamingContext context, out System.Runtime.Serialization.ISurrogateSelector selector) { throw null; }
        public virtual void RemoveSurrogate(System.Type type, System.Runtime.Serialization.StreamingContext context) { }
    }
}
namespace System.Runtime.Serialization.Formatters
{
    public enum FormatterAssemblyStyle
    {
        Simple = 0,
        Full = 1,
    }
    public enum FormatterTypeStyle
    {
        TypesWhenNeeded = 0,
        TypesAlways = 1,
        XsdString = 2,
    }
    public partial interface IFieldInfo
    {
        string[]? FieldNames { get; set; }
        System.Type[]? FieldTypes { get; set; }
    }
    public enum TypeFilterLevel
    {
        Low = 2,
        Full = 3,
    }
}
namespace System.Runtime.Serialization.Formatters.Binary
{
    public sealed partial class BinaryFormatter : System.Runtime.Serialization.IFormatter
    {
        public BinaryFormatter() { }
        public BinaryFormatter(System.Runtime.Serialization.ISurrogateSelector? selector, System.Runtime.Serialization.StreamingContext context) { }
        public System.Runtime.Serialization.Formatters.FormatterAssemblyStyle AssemblyFormat { get { throw null; } set { } }
        public System.Runtime.Serialization.SerializationBinder? Binder { get { throw null; } set { } }
        public System.Runtime.Serialization.StreamingContext Context { get { throw null; } set { } }
        public System.Runtime.Serialization.Formatters.TypeFilterLevel FilterLevel { get { throw null; } set { } }
        public System.Runtime.Serialization.ISurrogateSelector? SurrogateSelector { get { throw null; } set { } }
        public System.Runtime.Serialization.Formatters.FormatterTypeStyle TypeFormat { get { throw null; } set { } }
        [System.ObsoleteAttribute("BinaryFormatter serialization is obsolete and should not be used. See https://aka.ms/binaryformatter for more information.", DiagnosticId = "SYSLIB0011", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
        public object Deserialize(System.IO.Stream serializationStream) { throw null; }
        [System.ObsoleteAttribute("BinaryFormatter serialization is obsolete and should not be used. See https://aka.ms/binaryformatter for more information.", DiagnosticId = "SYSLIB0011", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
        public void Serialize(System.IO.Stream serializationStream, object graph) { }
    }
}
