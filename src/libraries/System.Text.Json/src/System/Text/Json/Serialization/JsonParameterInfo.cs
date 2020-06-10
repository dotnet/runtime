// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Reflection;
using System.Text.Json.Serialization;

namespace System.Text.Json
{
    /// <summary>
    /// Holds relevant state about a method parameter, like the default value of
    /// the parameter, and the position in the method's parameter list.
    /// </summary>
    internal abstract class JsonParameterInfo
    {

        public JsonConverter ConverterBase { get; set; } = null!;

        // The default value of the parameter. This is `DefaultValue` of the `ParameterInfo`, if specified, or the CLR `default` for the `ParameterType`.
        public object? DefaultValue { get; protected set; }

        // Options can be referenced here since all JsonPropertyInfos originate from a JsonClassInfo that is cached on JsonSerializerOptions.
        protected internal JsonSerializerOptions Options { get; set; } = null!; // initialized in Init method

        // The name of the parameter as UTF-8 bytes.
        public byte[] NameAsUtf8Bytes { get; private set; } = null!;

        // The zero-based position of the parameter in the formal parameter list.
        public int Position { get; private set; }

        private JsonClassInfo? _runtimeClassInfo;
        public JsonClassInfo RuntimeClassInfo
        {
            get
            {
                if (_runtimeClassInfo == null)
                {
                    _runtimeClassInfo = Options.GetOrAddClass(RuntimePropertyType);
                }

                return _runtimeClassInfo;
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
        }

        // Create a parameter that is ignored at run-time. It uses the same type (typeof(sbyte)) to help
        // prevent issues with unsupported types and helps ensure we don't accidently (de)serialize it.
        public static JsonParameterInfo CreateIgnoredParameterPlaceholder(
            JsonPropertyInfo matchingProperty,
            JsonSerializerOptions options)
        {
            return new JsonParameterInfo<sbyte>
            {
                RuntimePropertyType = typeof(sbyte),
                NameAsUtf8Bytes = matchingProperty.NameAsUtf8Bytes!,
                Options = options
            };
        }
    }
}
