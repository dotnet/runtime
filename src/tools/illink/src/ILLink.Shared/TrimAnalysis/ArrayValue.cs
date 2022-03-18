// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ILLink.Shared.DataFlow;
using MultiValue = ILLink.Shared.DataFlow.ValueSet<ILLink.Shared.DataFlow.SingleValue>;

namespace ILLink.Shared.TrimAnalysis
{
	sealed partial record ArrayValue : SingleValue
	{
		static ValueSetLattice<SingleValue> MultiValueLattice => default;

		public readonly SingleValue Size;

		public partial bool TryGetValueByIndex (int index, out MultiValue value);
	}
}
