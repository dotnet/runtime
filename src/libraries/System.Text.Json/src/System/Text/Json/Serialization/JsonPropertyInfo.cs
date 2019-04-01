// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Text.Json.Serialization.Converters;
using System.Text.Json.Serialization.Policies;

namespace System.Text.Json.Serialization
{
    internal abstract class JsonPropertyInfo
    {
        // For now, just a global converter.
        private static JsonEnumerableConverter s_jsonEnumerableConverter = new DefaultArrayConverter();

        internal ClassType ClassType;

        internal byte[] _name = default;
        internal byte[] _escapedName = default;

        internal bool HasGetter { get; set; }
        internal bool HasSetter { get; set; }

        public ReadOnlySpan<byte> EscapedName => _escapedName;
        public ReadOnlySpan<byte> Name => _name;

        // todo: to minimize hashtable lookups, cache JsonClassInfo:
        //public JsonClassInfo ClassInfo;

        // Constructor used for internal identifiers
        internal JsonPropertyInfo() { }

        internal JsonPropertyInfo(
            Type parentClassType,
            Type declaredPropertyType,
            Type runtimePropertyType,
            PropertyInfo propertyInfo,
            Type elementType,
            JsonSerializerOptions options)
        {
            ParentClassType = parentClassType;
            DeclaredPropertyType = declaredPropertyType;
            RuntimePropertyType = runtimePropertyType;
            PropertyInfo = propertyInfo;
            ClassType = JsonClassInfo.GetClassType(runtimePropertyType);
            if (elementType != null)
            {
                ElementClassInfo = options.GetOrAddClass(elementType);
            }

            IsNullableType = runtimePropertyType.IsGenericType && runtimePropertyType.GetGenericTypeDefinition() == typeof(Nullable<>);
            CanBeNull = IsNullableType || !runtimePropertyType.IsValueType;
        }

        internal bool CanBeNull { get; private set; }
        internal JsonClassInfo ElementClassInfo { get; private set; }
        internal JsonEnumerableConverter EnumerableConverter { get; private set; }

        internal bool IsNullableType { get; private set; }

        public PropertyInfo PropertyInfo { get; private set; }

        internal Type ParentClassType { get; private set; }

        internal Type DeclaredPropertyType { get; private set; }

        internal Type RuntimePropertyType { get; private set; }

        internal virtual void GetPolicies(JsonSerializerOptions options)
        {
            if (RuntimePropertyType.IsArray)
            {
                EnumerableConverter = s_jsonEnumerableConverter;
            }
        }

        internal abstract object GetValueAsObject(object obj, JsonSerializerOptions options);

        internal bool IgnoreNullPropertyValueOnRead(JsonSerializerOptions options)
        {
            return options.IgnoreNullPropertyValueOnRead;
        }

        internal bool IgnoreNullPropertyValueOnWrite(JsonSerializerOptions options)
        {
            return options.IgnoreNullPropertyValueOnWrite;
        }

        internal abstract void Read(JsonTokenType tokenType, JsonSerializerOptions options, ref ReadStack state, ref Utf8JsonReader reader);

        internal abstract void ReadEnumerable(JsonTokenType tokenType, JsonSerializerOptions options, ref ReadStack state, ref Utf8JsonReader reader);
        internal abstract void SetValueAsObject(object obj, object value, JsonSerializerOptions options);

        internal abstract void Write(JsonSerializerOptions options, ref WriteStackFrame current, ref Utf8JsonWriter writer);

        internal abstract void WriteEnumerable(JsonSerializerOptions options, ref WriteStackFrame current, ref Utf8JsonWriter writer);
    }
}
