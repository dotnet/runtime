// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace ILLink.RoslynAnalyzer.TrimAnalysis
{
	static class FlowAnnotations
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
	}
}
