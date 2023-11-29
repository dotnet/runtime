// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using ILLink.Shared.DataFlow;
using MultiValue = ILLink.Shared.DataFlow.ValueSet<ILLink.Shared.DataFlow.SingleValue>;

// This is needed due to NativeAOT which doesn't enable nullable globally yet
#nullable enable

namespace ILLink.Shared.TrimAnalysis
{
	internal sealed partial record ArrayValue : SingleValue
	{
		private static ValueSetLattice<SingleValue> MultiValueLattice => default;

		public readonly SingleValue Size;

		public partial bool TryGetValueByIndex (int index, out MultiValue value);

		public static MultiValue SanitizeArrayElementValue (MultiValue input)
		{
			// We need to be careful about self-referencing arrays. It's easy to have an array which has one of the elements as itself:
			// var arr = new object[1];
			// arr[0] = arr;
			//
			// We can't deep copy this, as it's an infinite recursion. And we can't easily guard against it with checks to self-referencing
			// arrays - as this could be two or more levels deep.
			//
			// We need to deep copy arrays because we don't have a good way to track references (we treat everything as a value type)
			// and thus we could get bad results if the array is involved in multiple branches for example.
			// That said, it only matters for us to track arrays to be able to get integers or Type values (and we really only need relatively simple cases)
			//
			// So we will simply treat array value as an element value as "too complex to analyze" and give up by storing Unknown instead

			bool needSanitization = false;
			foreach (var v in input.AsEnumerable ()) {
				if (v is ArrayValue)
					needSanitization = true;
			}

			if (!needSanitization)
				return input;

			return new(input.AsEnumerable ().Select (v => v is ArrayValue ? UnknownValue.Instance : v));
		}
	}
}
