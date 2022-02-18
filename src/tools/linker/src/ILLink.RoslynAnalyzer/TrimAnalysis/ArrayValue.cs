// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using ILLink.Shared.DataFlow;
using MultiValue = ILLink.Shared.DataFlow.ValueSet<ILLink.Shared.DataFlow.SingleValue>;

namespace ILLink.Shared.TrimAnalysis
{
	partial record ArrayValue
	{
		public ArrayValue (SingleValue size) => Size = size;

#pragma warning disable IDE0060 // Remove unused parameter
		public partial bool TryGetValueByIndex (int index, out MultiValue value) => throw new NotImplementedException ();
#pragma warning restore IDE0060 // Remove unused parameter
	}
}
