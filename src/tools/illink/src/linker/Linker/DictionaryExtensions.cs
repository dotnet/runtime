// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Mono.Linker
{
	internal static class DictionaryExtensions
	{
		public static void AddToSet<TKey, TElement> (this Dictionary<TKey, HashSet<TElement>> me, TKey key, TElement value)
			where TKey : notnull
		{
			if (!me.TryGetValue (key, out HashSet<TElement>? valueSet)) {
				valueSet = new ();
				me[key] = valueSet;
			}
			if (valueSet.ToList().Count == 1) {

				var b = valueSet.ToList()[0]?.Equals (value);
				Debug.WriteLine(b);
			}
			valueSet.Add (value);
		}
	}
}
