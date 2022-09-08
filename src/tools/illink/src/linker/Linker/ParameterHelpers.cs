// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using ILLink.Shared;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Mono.Linker
{
	public static class ParameterHelpers
	{
		public static ILParameterIndex GetILParameterIndex (MethodDefinition thisMethod, Instruction operation)
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

				_ => throw new ArgumentException ($"{nameof (ILParameterIndex)} expected an ldarg or starg instruction, got {operation.OpCode.Name}")
			};

			ILParameterIndex GetLdargParamIndex ()
			{
				return (ILParameterIndex) (code - Code.Ldarg_0);
			}
			ILParameterIndex GetParamSequence ()
			{
				ParameterDefinition param = (ParameterDefinition) operation.Operand;
				return (ILParameterIndex) param.Sequence;
			}
		}

		/// <Summary>
		/// This enum is used when converting from an ILParameterIndex to a SourceParamterIndex to
		/// differentiate `This` parameters from other parameters.
		/// </Summary>
		public enum SourceParameterKind
		{
			This,
			Numbered
		}

		/// <summary>
		/// Used to get the SourceParameterIndex that an instruction refers to.
		/// If the return value is <see cref="SourceParameterKind.Numbered" />, the instruction refers to a numbered non-this parameter and <paramref name="sourceParameterIndex"/> will have a valid value.
		/// If the return value is <see cref="SourceParameterKind.This" />, the instruction refers to the `this` parameter, and <paramref name="sourceParameterIndex"/> will not have a valid value.
		/// </summary>
		public static SourceParameterKind GetSourceParameterIndex (MethodDefinition method, Instruction operation, out SourceParameterIndex sourceParameterIndex)
			=> GetSourceParameterIndex (method, GetILParameterIndex (method, operation), out sourceParameterIndex);

		/// <summary>
		/// Used to get the SourceParameterIndex that an ILParameterIndex refers to.
		/// If the return value is <see cref="SourceParameterKind.Numbered" />, the instruction refers to a numbered non-this parameter and <paramref name="sourceParameterIndex"/> will have a valid value.
		/// If the return value is <see cref="SourceParameterKind.This" />, the instruction refers to the `this` parameter, and <paramref name="sourceParameterIndex"/> will not have a valid value.
		/// </summary>
		public static SourceParameterKind GetSourceParameterIndex (MethodReference method, ILParameterIndex ilIndex, out SourceParameterIndex sourceParameterIndex)
		{
			sourceParameterIndex = (SourceParameterIndex) (int) ilIndex;
			if (method.HasImplicitThis ()) {
				if (ilIndex == 0) {
					return SourceParameterKind.This;
				}
				sourceParameterIndex--;
			}
			return SourceParameterKind.Numbered;
		}

		public static ILParameterIndex GetILParameterIndex (MethodReference method, SourceParameterIndex sourceIndex)
			=> method.HasImplicitThis ()
				? (ILParameterIndex) (sourceIndex + 1)
				: (ILParameterIndex) sourceIndex;
	}
}
