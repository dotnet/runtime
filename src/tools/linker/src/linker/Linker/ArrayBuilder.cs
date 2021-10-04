// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Mono.Linker
{
	struct ArrayBuilder<T>
	{
		private List<T> _list;

		public void Add (T value) => (_list ??= new List<T> ()).Add (value);

		public bool Any (Predicate<T> callback) => _list?.Exists (callback) == true;

		public T[] ToArray () => _list?.ToArray ();

		public int Count => _list?.Count ?? 0;
	}
}
