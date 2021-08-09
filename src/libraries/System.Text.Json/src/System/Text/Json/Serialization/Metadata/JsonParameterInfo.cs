// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection;

namespace System.Text.Json.Serialization.Metadata
{
    /// <summary>
    /// Holds relevant state about a method parameter, like the default value of
    /// the parameter, and the position in the method's parameter list.
    /// </summary>
    internal abstract class JsonParameterInfo
    {
        private JsonTypeInfo? _runtimeTypeInfo;

        public JsonConverter ConverterBase { get; private set; } = null!;

        private protected bool MatchingPropertyCanBeNull { get; private set; }

        internal abstract object? ClrDefaultValue { get; }

        // The default value of the parameter. This is `DefaultValue` of the `ParameterInfo`, if specified, or the CLR `default` for the `ParameterType`.
        public object? DefaultValue { get; private protected set; }

        public bool IgnoreDefaultValuesOnRead { get; private set; }

        // Options can be referenced here since all JsonPropertyInfos originate from a JsonTypeInfo that is cached on JsonSerializerOptions.
        public JsonSerializerOptions? Options { get; set; } // initialized in Init method

        // The name of the parameter as UTF-8 bytes.
        public byte[] NameAsUtf8Bytes { get; private set; } = null!;

        public JsonNumberHandling? NumberHandling { get; private set; }

        // Using a field to avoid copy semantics.
        public JsonParameterInfoValues ClrInfo = null!;

        public JsonTypeInfo RuntimeTypeInfo
        {
            get
            {
                Debug.Assert(ShouldDeserialize);
                if (_runtimeTypeInfo == null)
                {
                    Debug.Assert(Options != null);
                    _runtimeTypeInfo = Options!.GetOrAddClass(RuntimePropertyType);
                }

                return _runtimeTypeInfo;
            }
            set
            {
                // Used by JsonMetadataServices.
                Debug.Assert(_runtimeTypeInfo == null);
                _runtimeTypeInfo = value;
            }
        }

        public Type RuntimePropertyType { get; set; } = null!;

        public bool ShouldDeserialize { get; private set; }

        public virtual void Initialize(JsonParameterInfoValues parameterInfo, JsonPropertyInfo matchingProperty, JsonSerializerOptions options)
        {
            ClrInfo = parameterInfo;
            Options = options;
            ShouldDeserialize = true;

            RuntimePropertyType = matchingProperty.RuntimePropertyType!;
            NameAsUtf8Bytes = matchingProperty.NameAsUtf8Bytes!;
            ConverterBase = matchingProperty.ConverterBase;
            IgnoreDefaultValuesOnRead = matchingProperty.IgnoreDefaultValuesOnRead;
            NumberHandling = matchingProperty.NumberHandling;
            MatchingPropertyCanBeNull = matchingProperty.PropertyTypeCanBeNull;
        }

        // Create a parameter that is ignored at run-time. It uses the same type (typeof(sbyte)) to help
        // prevent issues with unsupported types and helps ensure we don't accidently (de)serialize it.
        public static JsonParameterInfo CreateIgnoredParameterPlaceholder(JsonParameterInfoValues parameterInfo, JsonPropertyInfo matchingProperty)
        {
            JsonParameterInfo jsonParameterInfo = matchingProperty.ConverterBase.CreateJsonParameterInfo();
            jsonParameterInfo.ClrInfo = parameterInfo;
            jsonParameterInfo.RuntimePropertyType = matchingProperty.RuntimePropertyType!;
            jsonParameterInfo.NameAsUtf8Bytes = matchingProperty.NameAsUtf8Bytes!;
            jsonParameterInfo.InitializeDefaultValue(matchingProperty);
            return jsonParameterInfo;
        }

        protected abstract void InitializeDefaultValue(JsonPropertyInfo matchingProperty);
    }
}
