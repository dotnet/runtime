// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class JsonIEnumerableWithAddMethodConverter<TCollection> : JsonIEnumerableDefaultConverter<TCollection, object>
        where TCollection : IEnumerable
    {
        protected override void Add(object value, ref ReadStack state)
        {
            Debug.Assert(state.Current.AddMethodDelegate != null);
            ((Action<TCollection, object>)state.Current.AddMethodDelegate)((TCollection)state.Current.ReturnValue!, value);
        }

        protected override void CreateCollection(ref ReadStack state, JsonSerializerOptions options)
        {
            JsonClassInfo classInfo = state.Current.JsonClassInfo;

            if (classInfo.CreateObject == null)
            {
                ThrowHelper.ThrowNotSupportedException_DeserializeNoParameterlessConstructor(classInfo.Type);
            }

            state.Current.ReturnValue = classInfo.CreateObject()!;
            state.Current.AddMethodDelegate = GetOrAddEnumerableAddMethodDelegate(classInfo.Type, options);
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

            JsonConverter<object> converter = GetElementConverter(ref state);
            do
            {
                if (ShouldFlush(writer, ref state))
                {
                    state.Current.CollectionEnumerator = enumerator;
                    return false;
                }

                if (!converter.TryWrite(writer, enumerator.Current!, options, ref state))
                {
                    state.Current.CollectionEnumerator = enumerator;
                    return false;
                }

                state.Current.EndElement();
            } while (enumerator.MoveNext());

            return true;
        }

        internal override Type RuntimeType => TypeToConvert;

        private readonly ConcurrentDictionary<Type, Action<TCollection, object>> _delegates = new ConcurrentDictionary<Type, Action<TCollection, object>>();

        internal Action<TCollection, object> GetOrAddEnumerableAddMethodDelegate(Type type, JsonSerializerOptions options)
        {
            if (!_delegates.TryGetValue(type, out Action<TCollection, object>? result))
            {
                // We verified this exists when we created the converter in the enumerable converter factory.
                result = options.MemberAccessorStrategy.CreateAddMethodDelegate<TCollection>();
            }

            return result;
        }
    }
}
