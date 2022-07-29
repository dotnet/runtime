// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using ILLink.Shared.DataFlow;
using Mono.Cecil;

namespace ILLink.Shared.TrimAnalysis
{
	public partial record FieldReferenceValue (FieldDefinition FieldDefinition) : ReferenceValue
	{
		public override SingleValue DeepCopy () => this;
	}
}
