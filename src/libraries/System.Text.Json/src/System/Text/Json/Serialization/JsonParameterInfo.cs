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
    [DebuggerDisplay("ParameterInfo={ParameterInfo}")]
    internal abstract class JsonParameterInfo
    {
        private Type _runtimePropertyType = null!;

        public abstract JsonConverter ConverterBase { get; }

        // The default value of the parameter. This is `DefaultValue` of the `ParameterInfo`, if specified, or the CLR `default` for the `ParameterType`.
        public object? DefaultValue { get; protected set; }

        // Options can be referenced here since all JsonPropertyInfos originate from a JsonClassInfo that is cached on JsonSerializerOptions.
        protected JsonSerializerOptions Options { get; set; } = null!; // initialized in Init method

        public ParameterInfo ParameterInfo { get; private set; } = null!;

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
                    _runtimeClassInfo = Options.GetOrAddClass(_runtimePropertyType);
                }

                return _runtimeClassInfo;
            }
        }

        public bool ShouldDeserialize { get; private set; }

        public virtual void Initialize(
            Type declaredPropertyType,
            Type runtimePropertyType,
            ParameterInfo parameterInfo,
            JsonPropertyInfo matchingProperty,
            JsonSerializerOptions options)
        {
            _runtimePropertyType = runtimePropertyType;

            Options = options;
            ParameterInfo = parameterInfo;
            Position = parameterInfo.Position;
            ShouldDeserialize = true;

            DetermineParameterName(matchingProperty);
        }

        private void DetermineParameterName(JsonPropertyInfo matchingProperty)
        {
            NameAsUtf8Bytes = matchingProperty.NameAsUtf8Bytes!;
        }

        // Create a parameter that is ignored at run-time. It uses the same type (typeof(sbyte)) to help
        // prevent issues with unsupported types and helps ensure we don't accidently (de)serialize it.
        public static JsonParameterInfo CreateIgnoredParameterPlaceholder(
            ParameterInfo parameterInfo,
            JsonPropertyInfo matchingProperty,
            JsonSerializerOptions options)
        {
            JsonParameterInfo jsonParameterInfo = new JsonParameterInfo<sbyte>();
            jsonParameterInfo.Options = options;
            jsonParameterInfo.ParameterInfo = parameterInfo;
            jsonParameterInfo.ShouldDeserialize = false;

            jsonParameterInfo.DetermineParameterName(matchingProperty);

            return jsonParameterInfo;
        }

        public abstract bool ReadJson(ref ReadStack state, ref Utf8JsonReader reader, out object? argument);
    }
}
