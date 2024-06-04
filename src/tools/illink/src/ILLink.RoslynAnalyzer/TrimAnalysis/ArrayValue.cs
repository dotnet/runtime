// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using ILLink.Shared.DataFlow;
using MultiValue = ILLink.Shared.DataFlow.ValueSet<ILLink.Shared.DataFlow.SingleValue>;

namespace ILLink.Shared.TrimAnalysis
{
	internal partial record ArrayValue
	{
		public readonly Dictionary<int, MultiValue> IndexValues;

		public static MultiValue Create (MultiValue size)
		{
			MultiValue result = MultiValueLattice.Top;
			foreach (var sizeValue in size.AsEnumerable ()) {
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

			// Here we rely on the assumption that we can't store mutable values in arrays. The only mutable value
			// which we currently support are array values, but those are not allowed in an array (to avoid complexity).
			// As such we can rely on the values to be immutable, and thus if the counts are equal
			// then the arrays are equal if items from one can be directly found in the other.
			foreach (var kvp in IndexValues)
				if (!otherArr.IndexValues.TryGetValue (kvp.Key, out MultiValue value) || !kvp.Value.Equals (value))
					return false;

			return true;
		}

		// Lattice Meet() is supposed to copy values, so we need to make a deep copy since ArrayValue is mutable through IndexValues
		public override SingleValue DeepCopy ()
		{
			var newArray = new ArrayValue (Size);
			foreach (var kvp in IndexValues) {
#if DEBUG
				// Since it's possible to store a reference to array as one of its own elements
				// simple deep copy could lead to endless recursion.
				// So instead we simply disallow arrays as element values completely - and treat that case as "too complex to analyze".
				foreach (SingleValue v in kvp.Value.AsEnumerable ()) {
					System.Diagnostics.Debug.Assert (v is not ArrayValue);
				}
#endif

				newArray.IndexValues.Add (kvp.Key, kvp.Value.DeepCopy ());
			}

			return newArray;
		}
	}
}
