// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class ImmutableEnumerableOfTConverterWithReflection<TCollection, TElement>
        : ImmutableEnumerableOfTConverter<TCollection, TElement>
        where TCollection : IEnumerable<TElement>
    {
        [RequiresUnreferencedCode(IEnumerableConverterFactoryHelpers.ImmutableConvertersUnreferencedCodeMessage)]
        public ImmutableEnumerableOfTConverterWithReflection()
        {
        }

        internal override bool RequiresDynamicMemberAccessors => true;

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "The ctor is marked RequiresUnreferencedCode.")]
        internal override void Initialize(JsonSerializerOptions options, JsonTypeInfo? jsonTypeInfo = null)
        {
            Debug.Assert(jsonTypeInfo != null);
            jsonTypeInfo.CreateObjectWithArgs = options.MemberAccessorStrategy.CreateImmutableEnumerableCreateRangeDelegate<TCollection, TElement>();
        }
    }
}
