// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Converters
{
    /// <summary>
    /// Implementation of <cref>JsonObjectConverter{T}</cref> that supports the deserialization
    /// of JSON objects using parameterized constructors.
    /// </summary>
    internal sealed class LargeObjectWithParameterizedConstructorConverterWithReflection<T>
        : LargeObjectWithParameterizedConstructorConverter<T> where T : notnull
    {
        [RequiresUnreferencedCode(JsonSerializer.SerializationUnreferencedCodeMessage)]
        public LargeObjectWithParameterizedConstructorConverterWithReflection()
        {
        }

        internal override bool RequiresDynamicMemberAccessors => true;

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "The ctor is marked RequiresUnreferencedCode.")]
        internal override void Initialize(JsonSerializerOptions options, JsonTypeInfo? jsonTypeInfo = null)
        {
            Debug.Assert(jsonTypeInfo != null);
            jsonTypeInfo.CreateObjectWithArgs = options.MemberAccessorStrategy.CreateParameterizedConstructor<T>(ConstructorInfo!);
        }
    }
}
