// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using ILLink.Shared.TypeSystemProxy;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Mono.Linker
{
	public static class ParameterHelpers
	{
		public static ParameterIndex GetParameterIndex (MethodDefinition thisMethod, Instruction operation)
		{
			// Thank you Cecil, Operand being a ParameterDefinition instead of an integer,
			// (except for Ldarg_0 - Ldarg_3, where it's null) makes all of this really convenient...
			// NOT.
			Code code = operation.OpCode.Code;
			return code switch {
				Code.Ldarg_0
				or Code.Ldarg_1
				or Code.Ldarg_2
				or Code.Ldarg_3
				=> GetLdargParamIndex (),

				Code.Starg
				or Code.Ldarg
				or Code.Starg_S
				or Code.Ldarg_S
				or Code.Ldarga
				or Code.Ldarga_S
				=> GetParamSequence (),

				_ => throw new ArgumentException ($"{nameof (GetParameterIndex)} expected an ldarg or starg instruction, got {operation.OpCode.Name}")
			};

			ParameterIndex GetLdargParamIndex ()
			{
				return (ParameterIndex) (code - Code.Ldarg_0);
			}
			ParameterIndex GetParamSequence ()
			{
				ParameterDefinition param = (ParameterDefinition) operation.Operand;
				return (ParameterIndex) param.Sequence;
			}
		}
	}
}
