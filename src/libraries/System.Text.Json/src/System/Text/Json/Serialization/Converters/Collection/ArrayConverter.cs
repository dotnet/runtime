// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Text.Json.Serialization.Converters
{
    /// <summary>
    /// Converter for <cref>System.Array</cref>.
    /// </summary>
    internal sealed class ArrayConverter<TCollection, TElement>
        : IEnumerableDefaultConverter<TCollection, TElement>
        where TCollection: IEnumerable
    {
        internal override bool CanHaveIdMetadata => false;

        protected override void Add(TElement value, ref ReadStack state)
        {
            Debug.Assert(state.Current.ReturnValue is List<TElement>);
            ((List<TElement>)state.Current.ReturnValue!).Add(value);
        }

        protected override void CreateCollection(ref ReadStack state, JsonSerializerOptions options)
        {
            state.Current.ReturnValue = new List<TElement>();
        }

        protected override void ConvertCollection(ref ReadStack state, JsonSerializerOptions options)
        {
            Debug.Assert(state.Current.ReturnValue is List<TElement>);
            List<TElement> list = (List<TElement>)state.Current.ReturnValue!;
            state.Current.ReturnValue = list.ToArray();
        }

        protected override bool OnWriteResume(Utf8JsonWriter writer, TCollection value, JsonSerializerOptions options, ref WriteStack state)
        {
            Debug.Assert(value is TElement[]);
            TElement[] array = (TElement[])(IEnumerable)value;

            int index = state.Current.EnumeratorIndex;

            JsonConverter<TElement> elementConverter = GetElementConverter(ref state);
            if (elementConverter.CanUseDirectReadOrWrite)
            {
                // Fast path that avoids validation and extra indirection.
                for (; index < array.Length; index++)
                {
                    // TODO: https://github.com/dotnet/runtime/issues/32523
                    elementConverter.Write(writer, array[index]!, options);
                }
            }
            else
            {
                for (; index < array.Length; index++)
                {
                    TElement element = array[index];
                    if (!elementConverter.TryWrite(writer, element, options, ref state))
                    {
                        state.Current.EnumeratorIndex = index;
                        return false;
                    }

                    if (ShouldFlush(writer, ref state))
                    {
                        state.Current.EnumeratorIndex = ++index;
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
