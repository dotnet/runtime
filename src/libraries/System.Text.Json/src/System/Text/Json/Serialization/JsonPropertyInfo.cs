// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json.Serialization.Converters;
using System.Text.Json.Serialization.Policies;

namespace System.Text.Json.Serialization
{
    internal abstract class JsonPropertyInfo
    {
        // Cache the array and enumerable converters so they don't get created for every enumerable property.
        private static readonly JsonEnumerableConverter s_jsonArrayConverter = new DefaultArrayConverter();
        private static readonly JsonEnumerableConverter s_jsonEnumerableConverter = new DefaultEnumerableConverter();

        internal ClassType ClassType;

        internal byte[] _name = default;
        internal byte[] _escapedName = default;

        internal bool HasGetter { get; set; }
        internal bool HasSetter { get; set; }
        internal bool ShouldSerialize { get; private set; }
        internal bool ShouldDeserialize { get; private set; }

        internal bool IgnoreNullValues { get; private set; }

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
                Debug.Assert(ClassType == ClassType.Enumerable);
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
            DetermineSerializationCapabilities(options);
            IgnoreNullValues = options.IgnoreNullValues;
        }

        private void DetermineSerializationCapabilities(JsonSerializerOptions options)
        {
            bool hasIgnoreAttribute = (GetAttribute<JsonIgnoreAttribute>() != null);

            if (hasIgnoreAttribute)
            {
                // We don't serialize or deserialize.
                return;
            }

            if (ClassType != ClassType.Enumerable)
            {
                // We serialize if there is a getter + no [Ignore] attribute + not ignoring readonly properties.
                ShouldSerialize = HasGetter && (HasSetter || !options.IgnoreReadOnlyProperties);

                // We deserialize if there is a setter + no [Ignore] attribute. 
                ShouldDeserialize = HasSetter;
            }
            else
            {
                if (HasGetter)
                {
                    if (HasSetter)
                    {
                        ShouldDeserialize = true;
                    }
                    else if (RuntimePropertyType.IsAssignableFrom(typeof(IList)))
                    {
                        ShouldDeserialize = true;
                    }
                    //else
                    //{
                    //    // todo: future feature that allows non-List types (e.g. from System.Collections.Immutable) to have converters.
                    //}
                }
                //else if (HasSetter)
                //{
                //    // todo: Special case where there is no getter but a setter (and an EnumerableConverter)
                //}

                if (ShouldDeserialize)
                {
                    ShouldSerialize = HasGetter;

                    if (RuntimePropertyType.IsArray)
                    {
                        EnumerableConverter = s_jsonArrayConverter;
                    }
                    else if (typeof(IEnumerable).IsAssignableFrom(RuntimePropertyType))
                    {
                        Type elementType = JsonClassInfo.GetElementType(RuntimePropertyType);

                        if (RuntimePropertyType.IsAssignableFrom(typeof(JsonEnumerableT<>).MakeGenericType(elementType)))
                        {
                            EnumerableConverter = s_jsonEnumerableConverter;
                        }
                    }
                }
                else
                {
                    ShouldSerialize = HasGetter && !options.IgnoreReadOnlyProperties;
                }
            }
        }

        internal abstract object GetValueAsObject(object obj, JsonSerializerOptions options);

        internal TAttribute GetAttribute<TAttribute>() where TAttribute : Attribute
        {
            return (TAttribute)PropertyInfo?.GetCustomAttribute(typeof(TAttribute), inherit: false);
        }

        internal abstract void Read(JsonTokenType tokenType, JsonSerializerOptions options, ref ReadStack state, ref Utf8JsonReader reader);

        internal abstract void ReadEnumerable(JsonTokenType tokenType, JsonSerializerOptions options, ref ReadStack state, ref Utf8JsonReader reader);
        internal abstract void SetValueAsObject(object obj, object value, JsonSerializerOptions options);

        internal abstract void Write(JsonSerializerOptions options, ref WriteStackFrame current, ref Utf8JsonWriter writer);

        internal abstract void WriteEnumerable(JsonSerializerOptions options, ref WriteStackFrame current, ref Utf8JsonWriter writer);
    }
}
