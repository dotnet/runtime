// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using ILLink.Shared.TypeSystemProxy;
using TypeReference = Mono.Cecil.TypeReference;


namespace ILLink.Shared.TrimAnalysis
{

	/// <summary>
	/// A value that came from a method parameter - such as the result of a ldarg.
	/// </summary>
	internal partial record MethodParameterValue
	{
		public MethodParameterValue (TypeReference? staticType, ParameterProxy param, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes)
		{
			StaticType = staticType == null ? null : new (staticType);
			DynamicallyAccessedMemberTypes = dynamicallyAccessedMemberTypes;
			Parameter = param;
		}

		public override DynamicallyAccessedMemberTypes DynamicallyAccessedMemberTypes { get; }
	}
}
