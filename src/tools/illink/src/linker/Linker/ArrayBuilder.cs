// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Mono.Linker
{
	struct ArrayBuilder<T>
	{
		private List<T> _list;

		public void Add (T value) => (_list ??= new List<T> ()).Add (value);

		public bool Any (Predicate<T> callback) => _list?.Exists (callback) == true;

		public T[]? ToArray () => _list?.ToArray ();

		public int Count => _list?.Count ?? 0;
	}
}
