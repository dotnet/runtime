// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Converters
{
    // Converter for F# sets: https://fsharp.github.io/fsharp-core-docs/reference/fsharp-collections-fsharpset-1.html
    internal sealed class FSharpSetConverter<TSet, TElement> : IEnumerableDefaultConverter<TSet, TElement, List<TElement>>
        where TSet : IEnumerable<TElement>
    {
        private readonly Func<IEnumerable<TElement>, TSet> _setConstructor;

        [RequiresUnreferencedCode(FSharpCoreReflectionProxy.FSharpCoreUnreferencedCodeMessage)]
        [RequiresDynamicCode(FSharpCoreReflectionProxy.FSharpCoreUnreferencedCodeMessage)]
        public FSharpSetConverter()
        {
            _setConstructor = FSharpCoreReflectionProxy.Instance.CreateFSharpSetConstructor<TSet, TElement>();
        }

        private protected override void Add(ref List<TElement> collection, in TElement value, JsonTypeInfo collectionTypeInfo)
        {
            collection.Add(value);
        }

        internal override bool SupportsCreateObjectDelegate => false;

        private protected override bool TryCreateObject(ref Utf8JsonReader reader, JsonTypeInfo jsonTypeInfo, scoped ref ReadStack state, [NotNullWhen(true)] out List<TElement>? obj)
        {
            obj = new List<TElement>();
            return true;
        }

        private protected override bool TryConvert(ref Utf8JsonReader reader, JsonTypeInfo jsonTypeInfo, scoped ref ReadStack state, List<TElement> obj, out TSet value)
        {
            value = _setConstructor(obj);
            return true;
        }
    }
}
