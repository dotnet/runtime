// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Converters
{
    // Converter for F# lists: https://fsharp.github.io/fsharp-core-docs/reference/fsharp-collections-list-1.html
    internal sealed class FSharpListConverter<TList, TElement> : IEnumerableDefaultConverter<TList, TElement, List<TElement>>
        where TList : IEnumerable<TElement>
    {
        private readonly Func<IEnumerable<TElement>, TList> _listConstructor;

        [RequiresUnreferencedCode(FSharpCoreReflectionProxy.FSharpCoreUnreferencedCodeMessage)]
        [RequiresDynamicCode(FSharpCoreReflectionProxy.FSharpCoreUnreferencedCodeMessage)]
        public FSharpListConverter()
        {
            _listConstructor = FSharpCoreReflectionProxy.Instance.CreateFSharpListConstructor<TList, TElement>();
        }

        private protected override void Add(ref List<TElement> collection, in TElement value, JsonTypeInfo collectionTypeInfo)
        {
            collection.Add(value);
        }

        internal override void ConfigureJsonTypeInfo(JsonTypeInfo jsonTypeInfo, JsonSerializerOptions options)
        {
            jsonTypeInfo.CreateObject ??= () => new List<TElement>();
        }

        private protected override bool TryConvert(ref Utf8JsonReader reader, JsonTypeInfo jsonTypeInfo, scoped ref ReadStack state, List<TElement> obj, out TList value)
        {
            value = _listConstructor(obj);
            return true;
        }
    }
}
