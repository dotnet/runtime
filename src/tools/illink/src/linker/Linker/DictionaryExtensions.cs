// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Mono.Linker
{
	internal static class DictionaryExtensions
	{
		public static void AddToList<TKey, TElement> (this Dictionary<TKey, List<TElement>> me, TKey key, TElement value)
			where TKey : notnull
		{
			if (!me.TryGetValue (key, out List<TElement>? valueList)) {
				valueList = new ();
				me[key] = valueList;
			}
			valueList.Add (value);
		}

		public static U GetOrAdd<T, U> (this Dictionary<T, U> dict, T key, Func<T, U> createValue) where T : notnull
		{
			if (dict.TryGetValue (key, out var value)) {
				return value;
			}
			U val = createValue (key);
			dict.Add (key, val);
			return val;
		}
	}
}
