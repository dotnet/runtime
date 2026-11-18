// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class ImmutableDictionaryOfTKeyTValueConverter<TDictionary, TKey, TValue>
        : DictionaryDefaultConverter<TDictionary, TKey, TValue>
        where TDictionary : IReadOnlyDictionary<TKey, TValue>
        where TKey : notnull
    {
        protected override void Add(TKey key, in TValue value, JsonSerializerOptions options, ref ReadStack state)
        {
            Dictionary<TKey, TValue> dictionary = (Dictionary<TKey, TValue>)state.Current.ReturnValue!;

            if (options.AllowDuplicateProperties)
            {
                dictionary[key] = value;
            }
            else
            {
                if (!dictionary.TryAdd(key, value))
                {
                    ThrowHelper.ThrowJsonException_DuplicatePropertyNotAllowed();
                }
            }
        }

        internal override bool CanHaveMetadata => false;

        internal override bool SupportsCreateObjectDelegate => false;
        protected override void CreateCollection(ref Utf8JsonReader reader, scoped ref ReadStack state)
        {
            state.Current.ReturnValue = new Dictionary<TKey, TValue>();
        }

        internal override bool IsConvertibleCollection => true;
        protected override void ConvertCollection(ref ReadStack state, JsonSerializerOptions options)
        {
            Func<IEnumerable<KeyValuePair<TKey, TValue>>, TDictionary>? creator =
                (Func<IEnumerable<KeyValuePair<TKey, TValue>>, TDictionary>?)state.Current.JsonTypeInfo.CreateObjectWithArgs;
            Debug.Assert(creator is not null);
            state.Current.ReturnValue = creator((Dictionary<TKey, TValue>)state.Current.ReturnValue!);
        }

        [RequiresUnreferencedCode(IEnumerableConverterFactoryHelpers.ImmutableConvertersUnreferencedCodeMessage)]
        [RequiresDynamicCode(IEnumerableConverterFactoryHelpers.ImmutableConvertersUnreferencedCodeMessage)]
        internal override void ConfigureJsonTypeInfoUsingReflection(JsonTypeInfo jsonTypeInfo, JsonSerializerOptions options)
        {
            jsonTypeInfo.CreateObjectWithArgs = DefaultJsonTypeInfoResolver.MemberAccessor.CreateImmutableDictionaryCreateRangeDelegate<TDictionary, TKey, TValue>();
        }
    }
}
