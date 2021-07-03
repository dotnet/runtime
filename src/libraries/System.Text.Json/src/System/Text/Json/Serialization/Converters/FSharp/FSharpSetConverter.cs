// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Converters
{
    // Converter for F# sets: https://fsharp.github.io/fsharp-core-docs/reference/fsharp-collections-fsharpset-1.html
    internal sealed class FSharpSetConverter<TSet, TElement> : IEnumerableDefaultConverter<TSet, TElement>
        where TSet : IEnumerable<TElement>
    {
        private readonly Func<IEnumerable<TElement>, TSet> _setConstructor;

        [RequiresUnreferencedCode(FSharpCoreReflectionProxy.FSharpCoreUnreferencedCodeMessage)]
        public FSharpSetConverter()
        {
            _setConstructor = FSharpCoreReflectionProxy.Instance.CreateFSharpSetConstructor<TSet, TElement>();
        }

        protected override void Add(in TElement value, ref ReadStack state)
        {
            ((List<TElement>)state.Current.ReturnValue!).Add(value);
        }

        protected override void CreateCollection(ref Utf8JsonReader reader, ref ReadStack state, JsonSerializerOptions options)
        {
            state.Current.ReturnValue = new List<TElement>();
        }

        protected override void ConvertCollection(ref ReadStack state, JsonSerializerOptions options)
        {
            state.Current.ReturnValue = _setConstructor((List<TElement>)state.Current.ReturnValue!);
        }

        protected override bool OnWriteResume(Utf8JsonWriter writer, TSet value, JsonSerializerOptions options, ref WriteStack state)
        {
            IEnumerator<TElement> enumerator;
            if (state.Current.CollectionEnumerator == null)
            {
                enumerator = value.GetEnumerator();
                if (!enumerator.MoveNext())
                {
                    enumerator.Dispose();
                    return true;
                }
            }
            else
            {
                enumerator = (IEnumerator<TElement>)state.Current.CollectionEnumerator;
            }

            JsonConverter<TElement> converter = GetElementConverter(ref state);
            do
            {
                if (ShouldFlush(writer, ref state))
                {
                    state.Current.CollectionEnumerator = enumerator;
                    return false;
                }

                TElement element = enumerator.Current;
                if (!converter.TryWrite(writer, element, options, ref state))
                {
                    state.Current.CollectionEnumerator = enumerator;
                    return false;
                }
            } while (enumerator.MoveNext());

            enumerator.Dispose();
            return true;
        }
    }
}
