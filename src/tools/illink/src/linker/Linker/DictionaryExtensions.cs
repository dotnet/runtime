// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Mono.Linker
{
	public static class DictionaryExtensions
	{
		public static void AddToList<TKey, TList, TValueElement> (this Dictionary<TKey, TList> me, TKey key, TValueElement value) where TKey : notnull where TList : ICollection<TValueElement>, new()
		{
			if (!me.TryGetValue (key, out TList? methods)) {
				methods = new TList ();
				me[key] = methods;
			}
			methods.Add (value);
		}
	}
}
