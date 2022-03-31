// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ILLink.Shared.DataFlow;

namespace ILLink.Shared.TrimAnalysis
{
	partial record RuntimeMethodHandleValue
	{
		public override SingleValue DeepCopy () => this; // immutable value

		public override string ToString () => this.ValueToString ();
	}
}
