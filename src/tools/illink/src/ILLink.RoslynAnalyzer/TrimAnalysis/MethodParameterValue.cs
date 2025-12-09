// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using ILLink.Shared.TypeSystemProxy;
using Microsoft.CodeAnalysis;

namespace ILLink.Shared.TrimAnalysis
{
	internal partial record MethodParameterValue
	{
		public MethodParameterValue (ParameterProxy parameter)
			: this (parameter, FlowAnnotations.GetMethodParameterAnnotation (parameter)) { }

		public MethodParameterValue (ParameterProxy parameter, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes, bool overrideIsThis = false)
		{
			Parameter = parameter;
			DynamicallyAccessedMemberTypes = dynamicallyAccessedMemberTypes;
			_overrideIsThis = overrideIsThis;
		}

		public override DynamicallyAccessedMemberTypes DynamicallyAccessedMemberTypes { get; }

		public IMethodSymbol MethodSymbol => Parameter.Method.Method;
	}
}
