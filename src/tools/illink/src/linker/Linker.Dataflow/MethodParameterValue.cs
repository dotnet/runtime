// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using ILLink.Shared.TypeSystemProxy;
using Mono.Linker.Dataflow;
using TypeDefinition = Mono.Cecil.TypeDefinition;


namespace ILLink.Shared.TrimAnalysis
{

	/// <summary>
	/// A value that came from a method parameter - such as the result of a ldarg.
	/// </summary>
	internal partial record MethodParameterValue
	{
		public MethodParameterValue (TypeDefinition? staticType, ParameterProxy param, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes, bool overrideIsThis = false)
		{
			StaticType = staticType == null ? null : new (staticType);
			DynamicallyAccessedMemberTypes = dynamicallyAccessedMemberTypes;
			Parameter = param;
			_overrideIsThis = overrideIsThis;
		}

		public override DynamicallyAccessedMemberTypes DynamicallyAccessedMemberTypes { get; }
	}
}
