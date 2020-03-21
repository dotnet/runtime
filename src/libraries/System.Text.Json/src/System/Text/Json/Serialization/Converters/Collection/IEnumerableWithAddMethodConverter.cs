// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Diagnostics;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class IEnumerableWithAddMethodConverter<TCollection>
        : IEnumerableDefaultConverter<TCollection, object?>
        where TCollection : IEnumerable
    {
        protected override void Add(object? value, ref ReadStack state)
        {
            Debug.Assert(state.Current.ReturnValue is TCollection);
            Debug.Assert(state.Current.AddMethodDelegate != null);
            ((Action<TCollection, object?>)state.Current.AddMethodDelegate)((TCollection)state.Current.ReturnValue!, value);
        }

        protected override void CreateCollection(ref ReadStack state, JsonSerializerOptions options)
        {
            JsonClassInfo.ConstructorDelegate? constructorDelegate = state.Current.JsonClassInfo.CreateObject;

            if (constructorDelegate == null)
            {
                ThrowHelper.ThrowNotSupportedException_DeserializeNoDeserializationConstructor(TypeToConvert);
            }

            state.Current.ReturnValue = constructorDelegate();
            state.Current.AddMethodDelegate = GetAddMethodDelegate(options);
        }

        protected override bool OnWriteResume(Utf8JsonWriter writer, TCollection value, JsonSerializerOptions options, ref WriteStack state)
        {
            IEnumerator enumerator;
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
                enumerator = state.Current.CollectionEnumerator;
            }

            JsonConverter<object?> converter = GetElementConverter(ref state);
            do
            {
                if (ShouldFlush(writer, ref state))
                {
                    state.Current.CollectionEnumerator = enumerator;
                    return false;
                }

                if (!converter.TryWrite(writer, enumerator.Current, options, ref state))
                {
                    state.Current.CollectionEnumerator = enumerator;
                    return false;
                }
            } while (enumerator.MoveNext());

            return true;
        }

        private Action<TCollection, object?>? _addMethodDelegate;

        internal Action<TCollection, object?> GetAddMethodDelegate(JsonSerializerOptions options)
        {
            if (_addMethodDelegate == null)
            {
                // We verified this exists when we created the converter in the enumerable converter factory.
                _addMethodDelegate = options.MemberAccessorStrategy.CreateAddMethodDelegate<TCollection>();
            }

            return _addMethodDelegate;
        }
    }
}
