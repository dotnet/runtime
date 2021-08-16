// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class StackOrQueueConverterWithReflection<TCollection>
        : StackOrQueueConverter<TCollection>
        where TCollection : IEnumerable
    {
        internal override bool RequiresDynamicMemberAccessors => true;

        [RequiresUnreferencedCode(JsonSerializer.SerializationUnreferencedCodeMessage)]
        public StackOrQueueConverterWithReflection() { }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2091:UnrecognizedReflectionPattern",
            Justification = "The ctor is marked RequiresUnreferencedCode.")]
        internal override void Initialize(JsonSerializerOptions options, JsonTypeInfo? jsonTypeInfo = null)
        {
            Debug.Assert(jsonTypeInfo != null);
            jsonTypeInfo.AddMethodDelegate = options.MemberAccessorStrategy.CreateAddMethodDelegate<TCollection>();
        }
    }
}
