// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if !NETCOREAPP
using System.Diagnostics;
#endif
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http.Json
{
    public sealed partial class JsonContent
    {
        private sealed class JsonContentObjectSerializer : JsonContentSerializer
        {
            private readonly JsonSerializerOptions? _jsonSerializerOptions;

            public override Type ObjectType { get; }
            public override object? Value { get; }

            [RequiresUnreferencedCode(HttpContentJsonExtensions.SerializationUnreferencedCodeMessage)]
            [RequiresDynamicCode(HttpContentJsonExtensions.SerializationDynamicCodeMessage)]
            public JsonContentObjectSerializer(
                object? inputValue,
                Type inputType,
                JsonSerializerOptions? options)
            {
                ThrowHelper.ThrowIfNull(inputType);

                if (inputValue != null && !inputType.IsAssignableFrom(inputValue.GetType()))
                {
                    throw new ArgumentException(SR.Format(SR.SerializeWrongType, inputType, inputValue.GetType()));
                }

                Value = inputValue;
                ObjectType = inputType;
                _jsonSerializerOptions = options ?? JsonHelpers.s_defaultSerializerOptions;
            }

            [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
                Justification = "The ctor is annotated with RequiresUnreferencedCode.")]
            [UnconditionalSuppressMessage("AotAnalysis", "IL3050:RequiresDynamicCode",
                Justification = "The ctor is annotated with RequiresDynamicCode.")]
            public override Task SerializeToStreamAsync(Stream targetStream,
                CancellationToken cancellationToken)
                => JsonSerializer.SerializeAsync(targetStream, Value, ObjectType, _jsonSerializerOptions,
                    cancellationToken);

            [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
                Justification = "The ctor is annotated with RequiresUnreferencedCode.")]
            [UnconditionalSuppressMessage("AotAnalysis", "IL3050:RequiresDynamicCode",
                Justification = "The ctor is annotated with RequiresDynamicCode.")]
            public override void SerializeToStream(Stream targetStream)
            {
#if NETCOREAPP
                JsonSerializer.Serialize(targetStream, Value, ObjectType, _jsonSerializerOptions);
#else
                Debug.Fail("Synchronous serialization is only supported since .NET 5.0");
#endif
            }
        }
    }
}
