// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Collections.Generic
{
	partial class ArraySortHelper<T>
	{
		public static ArraySortHelper<T> Default { get; } = new ArraySortHelper<T>();
	}

	partial class ArraySortHelper<TKey, TValue>
	{
		public static ArraySortHelper<TKey, TValue> Default { get; } = new ArraySortHelper<TKey, TValue>();
	}
}
