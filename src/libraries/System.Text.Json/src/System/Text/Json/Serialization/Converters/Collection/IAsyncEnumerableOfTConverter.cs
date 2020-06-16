// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;

namespace System.Text.Json.Serialization.Converters
{
    /// <summary>
    /// Converter for <cref>System.Collections.Generic.IAsyncEnumerable{TElement} where TElement is a reference type</cref>.
    /// </summary>
    internal abstract class IAsyncEnumerableOfTConverter<TCollection, TElement>
        : IEnumerableDefaultConverter<TCollection, TElement>
        where TCollection : IAsyncEnumerable<TElement>
    {
        private static T GetTaskResult<T>(ValueTask<T> valueTask)
        {
            if (valueTask.IsCompleted)
                return valueTask.Result;
            else
            {
                var task = valueTask.AsTask();

                task.ConfigureAwait(false);

                return task.GetAwaiter().GetResult();
            }
        }

        protected override void Add(TElement value, ref ReadStack state)
            => ((IList<TElement>)state.Current.ReturnValue!).Add(value);

        protected override void CreateCollection(ref Utf8JsonReader reader, ref ReadStack state, JsonSerializerOptions options)
        {
            if (TypeToConvert.IsAssignableFrom(RuntimeType))
            {
                state.Current.ReturnValue = new AsyncEnumerableList<TElement>();
            }
            else if (typeof(IList<TElement>).IsAssignableFrom(TypeToConvert) && (TypeToConvert.GetConstructor(Type.EmptyTypes) is ConstructorInfo defaultCtor))
            {
                state.Current.ReturnValue = defaultCtor.Invoke(null);
            }
            else
            {
                ThrowHelper.ThrowNotSupportedException_CannotPopulateCollection(TypeToConvert, ref reader, ref state);
            }
        }

        protected override bool OnWriteResume(Utf8JsonWriter writer, TCollection value, JsonSerializerOptions options, ref WriteStack state)
        {
            IAsyncEnumerator<TElement> enumerator;
            if (state.Current.AsyncEnumerator == null)
            {
                enumerator = value.GetAsyncEnumerator();

                if (!GetTaskResult(enumerator.MoveNextAsync()))
                {
                    return true;
                }
            }
            else
            {
                Debug.Assert(state.Current.AsyncEnumerator is IAsyncEnumerator<TElement>);
                enumerator = RetrieveEnumerator(ref state);
            }

            JsonConverter<TElement> converter = GetElementConverter(ref state);
            do
            {
                if (ShouldFlush(writer, ref state))
                {
                    StoreEnumerator(ref state, enumerator);
                    return false;
                }

                TElement element = enumerator.Current;
                if (!converter.TryWrite(writer, element, options, ref state))
                {
                    StoreEnumerator(ref state, enumerator);
                    return false;
                }
            } while (GetTaskResult(enumerator.MoveNextAsync()));

            ClearStoredEnumerator(ref state);

            return true;
        }

        protected abstract void StoreEnumerator(ref WriteStack state, IAsyncEnumerator<TElement> enumerator);
        protected abstract IAsyncEnumerator<TElement> RetrieveEnumerator(ref WriteStack state);
        protected abstract void ClearStoredEnumerator(ref WriteStack state);

        internal override Type RuntimeType => typeof(AsyncEnumerableList<TElement>);
    }
}
