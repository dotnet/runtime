// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Mono.Cecil;

namespace Mono.Linker.Dataflow
{
	static class DiagnosticUtilities
	{
		internal static string GetDynamicallyAccessedMemberTypesDescription (DynamicallyAccessedMemberTypes memberTypes)
		{
			if (memberTypes == DynamicallyAccessedMemberTypes.All)
				return DynamicallyAccessedMemberTypes.All.ToString ();

			var results = new List<DynamicallyAccessedMemberTypes> ();
			if (memberTypes.HasFlag (DynamicallyAccessedMemberTypes.NonPublicConstructors))
				results.Add (DynamicallyAccessedMemberTypes.NonPublicConstructors);

			if (memberTypes.HasFlag (DynamicallyAccessedMemberTypes.PublicConstructors))
				results.Add (DynamicallyAccessedMemberTypes.PublicConstructors);
			else if (memberTypes.HasFlag (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor))
				results.Add (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor);

			if (memberTypes.HasFlag (DynamicallyAccessedMemberTypes.NonPublicMethods))
				results.Add (DynamicallyAccessedMemberTypes.NonPublicMethods);

			if (memberTypes.HasFlag (DynamicallyAccessedMemberTypes.PublicMethods))
				results.Add (DynamicallyAccessedMemberTypes.PublicMethods);

			if (memberTypes.HasFlag (DynamicallyAccessedMemberTypes.NonPublicProperties))
				results.Add (DynamicallyAccessedMemberTypes.NonPublicProperties);

			if (memberTypes.HasFlag (DynamicallyAccessedMemberTypes.PublicProperties))
				results.Add (DynamicallyAccessedMemberTypes.PublicProperties);

			if (memberTypes.HasFlag (DynamicallyAccessedMemberTypes.NonPublicFields))
				results.Add (DynamicallyAccessedMemberTypes.NonPublicFields);

			if (memberTypes.HasFlag (DynamicallyAccessedMemberTypes.PublicFields))
				results.Add (DynamicallyAccessedMemberTypes.PublicFields);

			if (memberTypes.HasFlag (DynamicallyAccessedMemberTypes.NonPublicEvents))
				results.Add (DynamicallyAccessedMemberTypes.NonPublicEvents);

			if (memberTypes.HasFlag (DynamicallyAccessedMemberTypes.PublicEvents))
				results.Add (DynamicallyAccessedMemberTypes.PublicEvents);

			if (memberTypes.HasFlag (DynamicallyAccessedMemberTypes.NonPublicNestedTypes))
				results.Add (DynamicallyAccessedMemberTypes.NonPublicNestedTypes);

			if (memberTypes.HasFlag (DynamicallyAccessedMemberTypes.PublicNestedTypes))
				results.Add (DynamicallyAccessedMemberTypes.PublicNestedTypes);

			if (results.Count == 0)
				return DynamicallyAccessedMemberTypes.None.ToString ();

			return string.Join (" | ", results.Select (r => r.ToString ()));
		}

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

			return null;
		}

		internal static string GetMetadataTokenDescriptionForErrorMessage (IMetadataTokenProvider targetContext) =>
			targetContext switch
			{
				ParameterDefinition parameterDefinition => GetParameterDescriptionForErrorMessage (parameterDefinition),
				MethodReturnType methodReturnType => $"return value of method '{GetMethodSignatureDisplayName (methodReturnType.Method)}'",
				FieldDefinition fieldDefinition => $"field '{fieldDefinition}'",
				// MethodDefinition is used to represent the "this" parameter as we don't support annotations on the method itself.
				MethodDefinition methodDefinition => $"implicit 'this' parameter of method '{methodDefinition.GetDisplayName ()}'",
				GenericParameter genericParameter => GetGenericParameterDescriptionForErrorMessage (genericParameter),
				_ => $"'{targetContext}'",
			};

		static string GetParameterDescriptionForErrorMessage (ParameterDefinition parameterDefinition)
		{
			if (string.IsNullOrEmpty (parameterDefinition.Name))
				return $"parameter #{parameterDefinition.Index} of method '{GetMethodSignatureDisplayName (parameterDefinition.Method)}'";
			else
				return $"parameter '{parameterDefinition.Name}' of method '{GetMethodSignatureDisplayName (parameterDefinition.Method)}'";
		}

		static string GetGenericParameterDescriptionForErrorMessage (GenericParameter genericParameter)
		{
			var declaringMemberName = genericParameter.DeclaringMethod != null ?
				genericParameter.DeclaringMethod.GetDisplayName () :
				genericParameter.DeclaringType.GetDisplayName ();
			return $"generic parameter '{genericParameter.Name}' from '{declaringMemberName}'";
		}

		static string GetMethodSignatureDisplayName (IMethodSignature methodSignature) =>
			(methodSignature is MethodDefinition method) ? method.GetDisplayName () : methodSignature.ToString ();
	}
}
