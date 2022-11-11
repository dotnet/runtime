// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using ILLink.Shared.DataFlow;
using ILLink.Shared.TypeSystemProxy;

namespace ILLink.Shared.TrimAnalysis
{
	internal sealed partial record ParameterReferenceValue (ParameterProxy Parameter)
		: ReferenceValue (Parameter.ParameterType)
	{
		public override SingleValue DeepCopy ()
		{
			return this;
		}
	}
}
