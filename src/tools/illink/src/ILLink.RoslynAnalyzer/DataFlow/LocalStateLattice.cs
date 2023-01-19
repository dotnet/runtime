// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using ILLink.Shared.DataFlow;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis;

namespace ILLink.RoslynAnalyzer.DataFlow
{
	public readonly struct LocalKey : IEquatable<LocalKey>
	{
		readonly ILocalSymbol? Local;

		readonly CaptureId? CaptureId;

		public LocalKey (ILocalSymbol symbol) => (Local, CaptureId) = (symbol, null);

		public LocalKey (CaptureId captureId) => (Local, CaptureId) = (null, captureId);

		public bool Equals (LocalKey other) => SymbolEqualityComparer.Default.Equals (Local, other.Local) &&
			(CaptureId?.Equals (other.CaptureId) ?? other.CaptureId == null);

		public override string ToString ()
		{
			if (Local != null)
				return Local.ToString ();
			return $"capture {CaptureId.GetHashCode ()}";
		}
	}

	public struct LocalState<TValue> : IEquatable<LocalState<TValue>>
		where TValue : IEquatable<TValue>
	{
		public DefaultValueDictionary<LocalKey, TValue> Dictionary;

		// Stores any operations which are captured by reference in a FlowCaptureOperation.
		// Only stores captures which are assigned through. Captures of the values of operations
		// are tracked as part of the dictionary of values, keyed by LocalKey.
		public DefaultValueDictionary<CaptureId, CapturedReferenceValue> CapturedReferences;

		public LocalState (TValue defaultValue)
			: this (new DefaultValueDictionary<LocalKey, TValue> (defaultValue),
				new DefaultValueDictionary<CaptureId, CapturedReferenceValue> (new CapturedReferenceValue ()))
		{
		}

		public LocalState (DefaultValueDictionary<LocalKey, TValue> dictionary, DefaultValueDictionary<CaptureId, CapturedReferenceValue> capturedReferences)
		{
			Dictionary = dictionary;
			CapturedReferences = capturedReferences;
		}

		public LocalState (DefaultValueDictionary<LocalKey, TValue> dictionary)
			: this (dictionary, new DefaultValueDictionary<CaptureId, CapturedReferenceValue> (new CapturedReferenceValue ()))
		{
		}

		public bool Equals (LocalState<TValue> other) => Dictionary.Equals (other.Dictionary);

		public TValue Get (LocalKey key) => Dictionary.Get (key);

		public void Set (LocalKey key, TValue value) => Dictionary.Set (key, value);

		public override string ToString () => Dictionary.ToString ();
	}

	// Wrapper struct exists purely to substitute a concrete LocalKey for TKey of DictionaryLattice
	public readonly struct LocalStateLattice<TValue, TValueLattice> : ILattice<LocalState<TValue>>
		where TValue : struct, IEquatable<TValue>
		where TValueLattice : ILattice<TValue>
	{
		public readonly DictionaryLattice<LocalKey, TValue, TValueLattice> Lattice;
		public readonly DictionaryLattice<CaptureId, CapturedReferenceValue, CapturedReferenceLattice> CapturedReferenceLattice;

		public LocalStateLattice (TValueLattice valueLattice)
		{
			Lattice = new DictionaryLattice<LocalKey, TValue, TValueLattice> (valueLattice);
			CapturedReferenceLattice = new DictionaryLattice<CaptureId, CapturedReferenceValue, CapturedReferenceLattice> (new CapturedReferenceLattice ());
			Top = new (Lattice.Top);
		}

		public LocalState<TValue> Top { get; }

		public LocalState<TValue> Meet (LocalState<TValue> left, LocalState<TValue> right)
		{
			var dictionary = Lattice.Meet (left.Dictionary, right.Dictionary);
			var capturedProperties = CapturedReferenceLattice.Meet (left.CapturedReferences, right.CapturedReferences);
			return new LocalState<TValue> (dictionary, capturedProperties);
		}
	}
}
