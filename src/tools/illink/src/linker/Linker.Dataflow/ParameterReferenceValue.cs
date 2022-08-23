// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using ILLink.Shared.DataFlow;
using Mono.Cecil;
using Mono.Linker;

namespace ILLink.Shared.TrimAnalysis
{
	public partial record ParameterReferenceValue (MethodDefinition MethodDefinition, int ParameterIndex)
		: ReferenceValue (MethodDefinition.HasImplicitThis () && ParameterIndex == 0 ? MethodDefinition.DeclaringType
			: MethodDefinition.Parameters[MethodDefinition.HasImplicitThis () ? --ParameterIndex : ParameterIndex].ParameterType)
	{
		public override SingleValue DeepCopy ()
		{
			return this;
		}
	}
}
