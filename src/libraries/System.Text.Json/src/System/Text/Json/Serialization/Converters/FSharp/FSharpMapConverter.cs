// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Converters
{
    // Converter for F# maps: https://fsharp.github.io/fsharp-core-docs/reference/fsharp-collections-fsharpmap-2.html
    internal sealed class FSharpMapConverter<TMap, TKey, TValue> : DictionaryDefaultConverter<TMap, TKey, TValue, List<Tuple<TKey, TValue>>>
        where TMap : IEnumerable<KeyValuePair<TKey, TValue>>
        where TKey : notnull
    {
        private readonly Func<IEnumerable<Tuple<TKey, TValue>>, TMap> _mapConstructor;

        [RequiresUnreferencedCode(FSharpCoreReflectionProxy.FSharpCoreUnreferencedCodeMessage)]
        [RequiresDynamicCode(FSharpCoreReflectionProxy.FSharpCoreUnreferencedCodeMessage)]
        public FSharpMapConverter()
        {
            _mapConstructor = FSharpCoreReflectionProxy.Instance.CreateFSharpMapConstructor<TMap, TKey, TValue>();
        }

        protected override void Add(ref List<Tuple<TKey, TValue>> collection, TKey key, in TValue value, JsonSerializerOptions options)
        {
            collection.Add(new Tuple<TKey, TValue>(key, value));
        }

        internal override bool CanHaveMetadata => false;

        internal override void ConfigureJsonTypeInfo(JsonTypeInfo jsonTypeInfo, JsonSerializerOptions options)
        {
            jsonTypeInfo.CreateObject ??= () => new List<Tuple<TKey, TValue>>();
        }

        private protected override bool TryConvert(ref Utf8JsonReader reader, JsonTypeInfo jsonTypeInfo, scoped ref ReadStack state, List<Tuple<TKey, TValue>> obj, out TMap value)
        {
            value = _mapConstructor(obj);
            return true;
        }
    }
}
