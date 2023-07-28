// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http.Json
{
    internal sealed class JsonContentUntyped : JsonContent
    {
        private protected override JsonTypeInfo JsonTypeInfo { get; }
        private protected override object? ValueCore { get; }

        [RequiresUnreferencedCode(HttpContentJsonExtensions.SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(HttpContentJsonExtensions.SerializationDynamicCodeMessage)]
        public JsonContentUntyped(
            object? inputValue,
            Type inputType,
            JsonSerializerOptions? options,
            MediaTypeHeaderValue? mediaType = null)
            : this(inputValue, GetJsonTypeInfo(inputType, options), mediaType) { }

        public JsonContentUntyped(
            object? inputValue,
            JsonTypeInfo jsonTypeInfo,
            MediaTypeHeaderValue? mediaType = null)
            : base(mediaType)
        {
            ThrowHelper.ThrowIfNull(jsonTypeInfo);

            if (inputValue != null && !jsonTypeInfo.Type.IsAssignableFrom(inputValue.GetType()))
            {
                throw new ArgumentException(SR.Format(SR.SerializeWrongType, jsonTypeInfo.Type, inputValue.GetType()));
            }

            JsonTypeInfo = jsonTypeInfo;
            ValueCore = inputValue;
        }

        private protected override Task SerializeToUtf8StreamAsync(Stream targetStream,
            CancellationToken cancellationToken)
            => JsonSerializer.SerializeAsync(targetStream, ValueCore, JsonTypeInfo,
                cancellationToken);

#if NETCOREAPP
        private protected override void SerializeToUtf8Stream(Stream targetStream)
        {
            JsonSerializer.Serialize(targetStream, ValueCore, JsonTypeInfo);
        }
#endif

        [RequiresUnreferencedCode(HttpContentJsonExtensions.SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(HttpContentJsonExtensions.SerializationDynamicCodeMessage)]
        private static JsonTypeInfo GetJsonTypeInfo(Type inputType, JsonSerializerOptions? options)
        {
            ThrowHelper.ThrowIfNull(inputType);

            // Ensure the options supports the call to GetTypeInfo
            options ??= JsonHelpers.s_defaultSerializerOptions;
            options.TypeInfoResolver ??= JsonSerializerOptions.Default.TypeInfoResolver;
            options.MakeReadOnly();

            return options.GetTypeInfo(inputType);
        }
    }
}
