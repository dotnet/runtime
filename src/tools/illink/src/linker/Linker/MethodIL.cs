// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

namespace Mono.Linker
{
	/// <summary>
	/// This is a wrapper which should be used by anything accessing method's body - see LinkContext.GetMethodIL for more details.
	/// Any accesses made throught this wrapper are considered "safe"/OK since the wrapper is only created
	/// once all of the optimizations are applied.
	/// </summary>
	public readonly record struct MethodIL
	{
		MethodIL (MethodBody body) => this.Body = body;

		public readonly MethodBody Body;

		public MethodDefinition Method => Body.Method;

#pragma warning disable RS0030 // Wrapper which provides safe access to the property
		public Collection<Instruction> Instructions => Body.Instructions;
		public Collection<ExceptionHandler> ExceptionHandlers => Body.ExceptionHandlers;
		public Collection<VariableDefinition> Variables => Body.Variables;
#pragma warning restore RS0030

		public static MethodIL Create (MethodBody body) => new MethodIL (body);
	}
}
