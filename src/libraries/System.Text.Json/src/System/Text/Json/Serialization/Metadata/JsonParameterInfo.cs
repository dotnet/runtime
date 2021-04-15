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

        public JsonConverter ConverterBase { get; private set; } = null!;

        // The default value of the parameter. This is `DefaultValue` of the `ParameterInfo`, if specified, or the CLR `default` for the `ParameterType`.
        public object? DefaultValue { get; protected set; }

        public bool IgnoreDefaultValuesOnRead { get; private set; }

        // Options can be referenced here since all JsonPropertyInfos originate from a JsonTypeInfo that is cached on JsonSerializerOptions.
        public JsonSerializerOptions? Options { get; set; } // initialized in Init method

        // The name of the parameter as UTF-8 bytes.
        public byte[] NameAsUtf8Bytes { get; private set; } = null!;

        public JsonNumberHandling? NumberHandling { get; private set; }

        // The zero-based position of the parameter in the formal parameter list.
        public int Position { get; private set; }

        private JsonTypeInfo? _runtimeTypeInfo;
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
        }

        internal Type RuntimePropertyType { get; set; } = null!;

        public bool ShouldDeserialize { get; private set; }

        public virtual void Initialize(
            Type runtimePropertyType,
            ParameterInfo parameterInfo,
            JsonPropertyInfo matchingProperty,
            JsonSerializerOptions options)
        {
            RuntimePropertyType = runtimePropertyType;
            Position = parameterInfo.Position;
            NameAsUtf8Bytes = matchingProperty.NameAsUtf8Bytes!;
            Options = options;
            ShouldDeserialize = true;
            ConverterBase = matchingProperty.ConverterBase;
            IgnoreDefaultValuesOnRead = matchingProperty.IgnoreDefaultValuesOnRead;
            NumberHandling = matchingProperty.NumberHandling;
        }

        // Create a parameter that is ignored at run-time. It uses the same type (typeof(sbyte)) to help
        // prevent issues with unsupported types and helps ensure we don't accidently (de)serialize it.
        public static JsonParameterInfo CreateIgnoredParameterPlaceholder(JsonPropertyInfo matchingProperty)
        {
            return new JsonParameterInfo<sbyte>
            {
                RuntimePropertyType = typeof(sbyte),
                NameAsUtf8Bytes = matchingProperty.NameAsUtf8Bytes!,
            };
        }
    }
}
