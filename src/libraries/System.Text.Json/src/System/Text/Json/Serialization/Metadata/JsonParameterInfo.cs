// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

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

        /// <summary>
        /// Create a parameter that is ignored at run time. It uses the same type (typeof(sbyte)) to help
        /// prevent issues with unsupported types and helps ensure we don't accidently (de)serialize it.
        /// </summary>
        public static JsonParameterInfo CreateIgnoredParameterPlaceholder(
            JsonParameterInfoValues parameterInfo,
            JsonPropertyInfo matchingProperty,
            bool sourceGenMode)
        {
            JsonParameterInfo jsonParameterInfo = new JsonParameterInfo<sbyte>();
            jsonParameterInfo.ClrInfo = parameterInfo;
            jsonParameterInfo.RuntimePropertyType = matchingProperty.RuntimePropertyType!;
            jsonParameterInfo.NameAsUtf8Bytes = matchingProperty.NameAsUtf8Bytes!;

            // TODO: https://github.com/dotnet/runtime/issues/60082.
            // Default value initialization for params mapping to ignored properties doesn't
            // account for the default value of optional parameters. This should be fixed.

            if (sourceGenMode)
            {
                // The <T> value in the matching JsonPropertyInfo<T> instance matches the parameter type.
                jsonParameterInfo.DefaultValue = matchingProperty.DefaultValue;
            }
            else
            {
                // The <T> value in the created JsonPropertyInfo<T> instance (sbyte)
                // doesn't match the parameter type, use reflection to get the default value.
                Type parameterType = parameterInfo.ParameterType;

                GenericMethodHolder holder;
                if (matchingProperty.Options.TryGetClass(parameterType, out JsonTypeInfo? typeInfo))
                {
                    holder = typeInfo.GenericMethods;
                }
                else
                {
                    holder = GenericMethodHolder.CreateHolder(parameterInfo.ParameterType);
                }

                jsonParameterInfo.DefaultValue = holder.DefaultValue;
            }

            return jsonParameterInfo;
        }
    }
}
