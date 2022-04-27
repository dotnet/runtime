// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using ILLink.RoslynAnalyzer;
using ILLink.Shared.TypeSystemProxy;
using Microsoft.CodeAnalysis;

namespace ILLink.Shared.TrimAnalysis
{
	readonly partial struct FlowAnnotations
	{
		public static bool RequiresDataFlowAnalysis (IMethodSymbol method)
		{
			if (method.GetDynamicallyAccessedMemberTypes () != DynamicallyAccessedMemberTypes.None)
				return true;

			if (GetMethodReturnValueAnnotation (method) != DynamicallyAccessedMemberTypes.None)
				return true;

			foreach (var parameter in method.Parameters) {
				if (GetMethodParameterAnnotation (parameter) != DynamicallyAccessedMemberTypes.None)
					return true;
			}

			foreach (var typeParameter in method.TypeParameters) {
				if (typeParameter.GetDynamicallyAccessedMemberTypes () != DynamicallyAccessedMemberTypes.None)
					return true;
			}

			return false;
		}

		public static DynamicallyAccessedMemberTypes GetMethodParameterAnnotation (IParameterSymbol parameter)
		{
			var damt = parameter.GetDynamicallyAccessedMemberTypes ();

			// Is this a property setter parameter?
			var parameterMethod = (IMethodSymbol) parameter.ContainingSymbol;
			Debug.Assert (parameterMethod != null);
			// If there are conflicts between the setter and the property annotation,
			// the setter annotation wins. (But DAMT.None is ignored)
			if (parameterMethod!.MethodKind == MethodKind.PropertySet && damt == DynamicallyAccessedMemberTypes.None) {
				var property = (IPropertySymbol) parameterMethod.AssociatedSymbol!;
				Debug.Assert (property != null);
				damt = property!.GetDynamicallyAccessedMemberTypes ();
			}

			return damt;
		}

		public static DynamicallyAccessedMemberTypes GetMethodReturnValueAnnotation (IMethodSymbol method)
		{
			var returnDamt = method.GetDynamicallyAccessedMemberTypesOnReturnType ();

			// Is this a property getter?
			// If there are conflicts between the getter and the property annotation,
			// the getter annotation wins. (But DAMT.None is ignored)
			if (method.MethodKind is MethodKind.PropertyGet && returnDamt == DynamicallyAccessedMemberTypes.None) {
				var property = (IPropertySymbol) method.AssociatedSymbol!;
				Debug.Assert (property != null);
				returnDamt = property!.GetDynamicallyAccessedMemberTypes ();
			}

			return returnDamt;
		}

#pragma warning disable CA1822 // Mark members as static - the other partial implementations might need to be instance methods

		// TODO: This is relatively expensive on the analyzer since it doesn't cache the annotation information
		// In linker this is an optimization to avoid the heavy lifting of analysis if there's no point
		// it's unclear if the same optimization makes sense for the analyzer.
		internal partial bool MethodRequiresDataFlowAnalysis (MethodProxy method)
			=> FlowAnnotations.RequiresDataFlowAnalysis (method.Method);

		internal partial MethodReturnValue GetMethodReturnValue (MethodProxy method, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes)
			=> new MethodReturnValue (method.Method, dynamicallyAccessedMemberTypes);

		internal partial MethodReturnValue GetMethodReturnValue (MethodProxy method)
			=> GetMethodReturnValue (method, FlowAnnotations.GetMethodReturnValueAnnotation (method.Method));

		internal partial GenericParameterValue GetGenericParameterValue (GenericParameterProxy genericParameter)
			=> new GenericParameterValue (genericParameter.TypeParameterSymbol);

		internal partial MethodThisParameterValue GetMethodThisParameterValue (MethodProxy method, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes)
			=> new MethodThisParameterValue (method.Method, dynamicallyAccessedMemberTypes);

		internal partial MethodThisParameterValue GetMethodThisParameterValue (MethodProxy method)
			=> GetMethodThisParameterValue (method, method.Method.GetDynamicallyAccessedMemberTypes ());

		internal partial MethodParameterValue GetMethodParameterValue (MethodProxy method, int parameterIndex, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes)
			=> new MethodParameterValue (method.Method.Parameters[parameterIndex], dynamicallyAccessedMemberTypes);

		internal partial MethodParameterValue GetMethodParameterValue (MethodProxy method, int parameterIndex)
		{
			var annotation = FlowAnnotations.GetMethodParameterAnnotation (method.Method.Parameters[parameterIndex]);
			return GetMethodParameterValue (method, parameterIndex, annotation);
		}
#pragma warning restore CA1822
	}
}
