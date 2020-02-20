// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class ImmutableEnumerableOfTConverter<TCollection, TElement>
        : IEnumerableDefaultConverter<TCollection, TElement>
        where TCollection : IEnumerable<TElement>
    {
        protected override void Add(TElement value, ref ReadStack state)
        {
            Debug.Assert(state.Current.ReturnValue is List<TElement>);
            ((List<TElement>)state.Current.ReturnValue!).Add(value);
        }

        internal override bool CanHaveIdMetadata => false;

        protected override void CreateCollection(ref ReadStack state, JsonSerializerOptions options)
        {
            state.Current.ReturnValue = new List<TElement>();
        }

        protected override void ConvertCollection(ref ReadStack state, JsonSerializerOptions options)
        {
            state.Current.ReturnValue = GetCreatorDelegate(options)((List<TElement>)state.Current.ReturnValue!);
        }

        protected override bool OnWriteResume(Utf8JsonWriter writer, TCollection value, JsonSerializerOptions options, ref WriteStack state)
        {
            IEnumerator<TElement> enumerator;
            if (state.Current.CollectionEnumerator == null)
            {
                enumerator = value.GetEnumerator();
                if (!enumerator.MoveNext())
                {
                    return true;
                }
            }
            else
            {
                Debug.Assert(state.Current.CollectionEnumerator is IEnumerator<TElement>);
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

            return true;
        }

        private Func<IEnumerable<TElement>, TCollection>? _creatorDelegate;

        private Func<IEnumerable<TElement>, TCollection> GetCreatorDelegate(JsonSerializerOptions options)
        {
            if (_creatorDelegate == null)
            {
                _creatorDelegate = options.MemberAccessorStrategy.CreateImmutableEnumerableCreateRangeDelegate<TElement, TCollection>();
            }

            return _creatorDelegate;
        }
    }
}
