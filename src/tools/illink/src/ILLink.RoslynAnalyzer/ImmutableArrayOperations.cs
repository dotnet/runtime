// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace ILLink.RoslynAnalyzer
{
	sealed class ImmutableArrayOperations
	{
		internal static bool Contains<T, TComp> (ImmutableArray<T> list, T elem, TComp comparer)
					where TComp : IEqualityComparer<T>
		{
			foreach (var e in list) {
				if (comparer.Equals (e, elem)) {
					return true;
				}
			}
			return false;
		}

		internal static TSymbol? TryGetSingleSymbol<TSymbol> (ImmutableArray<ISymbol> members) where TSymbol : class, ISymbol
		{
			TSymbol? candidate = null;
			foreach (var m in members) {
				if (m is TSymbol tsym) {
					if (candidate is null) {
						candidate = tsym;
					} else {
						return null;
					}
				}
			}
			return candidate;
		}

		internal static void AddIfNotNull<TSymbol> (ImmutableArray<TSymbol>.Builder properties, TSymbol? p) where TSymbol : class, ISymbol
		{
			if (p != null) {
				properties.Add (p);
			}
		}
	}
}
