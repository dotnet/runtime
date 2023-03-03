// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using ILLink.Shared.DataFlow;
using MultiValue = ILLink.Shared.DataFlow.ValueSet<ILLink.Shared.DataFlow.SingleValue>;

namespace ILLink.Shared.TrimAnalysis
{
	partial record ArrayValue
	{
		public readonly Dictionary<int, MultiValue> IndexValues;

		public static MultiValue Create (MultiValue size)
		{
			MultiValue result = MultiValueLattice.Top;
			foreach (var sizeValue in size) {
				result = MultiValueLattice.Meet (result, new MultiValue (new ArrayValue (sizeValue)));
			}

			return result;
		}

		public static MultiValue Create (int size) => Create (new ConstIntValue (size));

		ArrayValue (SingleValue size)
		{
			Size = size;
			IndexValues = new Dictionary<int, MultiValue> ();
		}

		public partial bool TryGetValueByIndex (int index, out MultiValue value)
		{
			if (IndexValues.TryGetValue (index, out value))
				return true;

			value = default;
			return false;
		}

		public override int GetHashCode ()
		{
			return HashUtils.Combine (GetType ().GetHashCode (), Size);
		}

		public bool Equals (ArrayValue? otherArr)
		{
			if (otherArr == null)
				return false;

			bool equals = Size.Equals (otherArr.Size);
			equals &= IndexValues.Count == otherArr.IndexValues.Count;
			if (!equals)
				return false;

			// If both sets T and O are the same size and "T intersect O" is empty, then T == O.
			HashSet<KeyValuePair<int, MultiValue>> thisValueSet = new (IndexValues);
			thisValueSet.ExceptWith (otherArr.IndexValues);
			return thisValueSet.Count == 0;
		}

		// Lattice Meet() is supposed to copy values, so we need to make a deep copy since ArrayValue is mutable through IndexValues
		public override SingleValue DeepCopy ()
		{
			var newArray = new ArrayValue (Size);
			foreach (var kvp in IndexValues) {
				newArray.IndexValues.Add (kvp.Key, kvp.Value.Clone ());
			}

			return newArray;
		}
	}
}
