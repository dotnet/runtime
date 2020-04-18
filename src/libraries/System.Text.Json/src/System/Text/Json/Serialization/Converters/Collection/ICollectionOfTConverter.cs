// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Text.Json.Serialization.Converters
{
    /// <summary>
    /// Converter for <cref>System.Collections.Generic.ICollection{TElement}</cref>.
    /// </summary>
    internal sealed class ICollectionOfTConverter<TCollection, TElement>
        : IEnumerableDefaultConverter<TCollection, TElement>
        where TCollection : ICollection<TElement>
    {
        protected override void Add(TElement value, ref ReadStack state)
        {
            Debug.Assert(state.Current.ReturnValue is ICollection<TElement>);
            ((ICollection<TElement>)state.Current.ReturnValue!).Add(value);
        }

        protected override void CreateCollection(ref ReadStack state, JsonSerializerOptions options)
        {
            JsonClassInfo classInfo = state.Current.JsonClassInfo;

            if (TypeToConvert.IsInterface || TypeToConvert.IsAbstract)
            {
                if (!TypeToConvert.IsAssignableFrom(RuntimeType))
                {
                    ThrowHelper.ThrowNotSupportedException_DeserializeNoDeserializationConstructor(TypeToConvert);
                }

                state.Current.ReturnValue = new List<TElement>();
            }
            else
            {
                if (classInfo.CreateObject == null)
                {
                    ThrowHelper.ThrowNotSupportedException_DeserializeNoDeserializationConstructor(TypeToConvert);
                }

                TCollection returnValue = (TCollection)classInfo.CreateObject()!;

                if (returnValue.IsReadOnly)
                {
                    ThrowHelper.ThrowNotSupportedException_SerializationNotSupported(TypeToConvert);
                }

                state.Current.ReturnValue = returnValue;
            }
        }

        protected override bool OnWriteResume(
            Utf8JsonWriter writer,
            TCollection value,
            JsonSerializerOptions options,
            ref WriteStack state)
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

        internal override Type RuntimeType
        {
            get
            {
                if (TypeToConvert.IsAbstract || TypeToConvert.IsInterface)
                {
                    return typeof(List<TElement>);
                }

                return TypeToConvert;
            }
        }
    }
}
