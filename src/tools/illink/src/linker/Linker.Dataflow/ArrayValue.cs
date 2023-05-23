// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using ILLink.Shared.DataFlow;
using Mono.Cecil;
using Mono.Linker.Dataflow;
using MultiValue = ILLink.Shared.DataFlow.ValueSet<ILLink.Shared.DataFlow.SingleValue>;


namespace ILLink.Shared.TrimAnalysis
{
	internal partial record ArrayValue
	{
		public static MultiValue Create (MultiValue size, TypeReference elementType)
		{
			MultiValue result = MultiValueLattice.Top;
			foreach (var sizeValue in size) {
				result = MultiValueLattice.Meet (result, new MultiValue (new ArrayValue (sizeValue, elementType)));
			}

			return result;
		}

		public static ArrayValue Create (int size, TypeReference elementType)
		{
			return new ArrayValue (new ConstIntValue (size), elementType);
		}

		/// <summary>
		/// Constructs an array value of the given size
		/// </summary>
		ArrayValue (SingleValue size, TypeReference elementType)
		{
			Size = size;
			ElementType = elementType;
			IndexValues = new Dictionary<int, ValueBasicBlockPair> ();
		}

		public TypeReference ElementType { get; }
		public Dictionary<int, ValueBasicBlockPair> IndexValues { get; }

		public partial bool TryGetValueByIndex (int index, out MultiValue value)
		{
			if (IndexValues.TryGetValue (index, out var valuePair)) {
				value = valuePair.Value;
				return true;
			}

			value = default;
			return false;
		}

		public override int GetHashCode ()
		{
			return HashCode.Combine (GetType ().GetHashCode (), Size);
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
				if (!otherArr.IndexValues.TryGetValue (kvp.Key, out ValueBasicBlockPair value) || !kvp.Value.Equals (value))
					return false;

			return true;
		}

		public override SingleValue DeepCopy ()
		{
			var newValue = new ArrayValue (Size.DeepCopy (), ElementType);
			foreach (var kvp in IndexValues) {
#if DEBUG
				// Since it's possible to store a reference to array as one of its own elements
				// simple deep copy could lead to endless recursion.
				// So instead we simply disallow arrays as element values completely - and treat that case as "too complex to analyze".
				foreach (SingleValue v in kvp.Value.Value) {
					System.Diagnostics.Debug.Assert (v is not ArrayValue);
				}
#endif

				newValue.IndexValues.Add (kvp.Key, new ValueBasicBlockPair (kvp.Value.Value.DeepCopy (), kvp.Value.BasicBlockIndex));
			}

			return newValue;
		}

		public override string ToString ()
		{
			StringBuilder result = new ();
			result.Append ("Array Size:");
			result.Append (this.ValueToString (Size));

			result.Append (", Values:(");
			bool first = true;
			foreach (var element in IndexValues) {
				if (!first) {
					result.Append (',');
					first = false;
				}

				result.Append ('(');
				result.Append (element.Key);
				result.Append (",(");
				bool firstValue = true;
				foreach (var v in element.Value.Value) {
					if (firstValue) {
						result.Append (',');
						firstValue = false;
					}

					result.Append (v.ToString ());
				}
				result.Append ("))");
			}
			result.Append (')');

			return result.ToString ();
		}
	}
}
