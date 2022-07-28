// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Mono.Cecil;

namespace Mono.Linker.Dataflow
{
	static class DiagnosticUtilities
	{
		internal static IMetadataTokenProvider GetMethodParameterFromIndex (MethodDefinition method, int parameterIndex)
		{
			int declaredParameterIndex;
			if (method.HasImplicitThis ()) {
				if (parameterIndex == 0)
					return method;

				declaredParameterIndex = parameterIndex - 1;
			} else
				declaredParameterIndex = parameterIndex;

			if (declaredParameterIndex >= 0 && declaredParameterIndex < method.Parameters.Count)
				return method.Parameters[declaredParameterIndex];

			throw new InvalidOperationException ();
		}

		internal static string GetParameterNameForErrorMessage (ParameterDefinition parameterDefinition) =>
			string.IsNullOrEmpty (parameterDefinition.Name) ? $"#{parameterDefinition.Index}" : parameterDefinition.Name;

		internal static string GetGenericParameterDeclaringMemberDisplayName (GenericParameter genericParameter) =>
			genericParameter.DeclaringMethod != null ?
				genericParameter.DeclaringMethod.GetDisplayName () :
				genericParameter.DeclaringType.GetDisplayName ();

		internal static string GetMethodSignatureDisplayName (IMethodSignature methodSignature) =>
			(methodSignature is MethodReference method) ? method.GetDisplayName () : (methodSignature.ToString () ?? string.Empty);
	}
}
