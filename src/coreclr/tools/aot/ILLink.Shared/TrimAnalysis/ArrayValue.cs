// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
	}
}
