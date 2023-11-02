// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using ILLink.RoslynAnalyzer;
using ILLink.Shared.TypeSystemProxy;
using Microsoft.CodeAnalysis;

#nullable enable
namespace ILLink.Shared.TrimAnalysis
{
	public sealed partial class FlowAnnotations
	{
		// In the analyzer there's no stateful data the flow annotations need to store
		// so we just create a singleton on demand.
		static readonly Lazy<FlowAnnotations> _instance = new (() => new FlowAnnotations (), isThreadSafe: true);

		public static FlowAnnotations Instance { get => _instance.Value; }

		// Hide the default .ctor so that only the one singleton instance can be created
		private FlowAnnotations () { }

		public static bool RequiresDataFlowAnalysis (IMethodSymbol method)
		{
			if (GetMethodReturnValueAnnotation (method) != DynamicallyAccessedMemberTypes.None)
				return true;

			foreach (var param in method.GetParameters ()) {
				if (GetMethodParameterAnnotation (param) != DynamicallyAccessedMemberTypes.None)
					return true;
			}

			return false;
		}

		internal static DynamicallyAccessedMemberTypes GetMethodParameterAnnotation (ParameterProxy param)
		{
			IMethodSymbol method = param.Method.Method;
			if (param.IsImplicitThis)
				return method.GetDynamicallyAccessedMemberTypes ();

			IParameterSymbol parameter = param.ParameterSymbol!;
			var damt = parameter.GetDynamicallyAccessedMemberTypes ();

			var parameterMethod = (IMethodSymbol) parameter.ContainingSymbol;
			Debug.Assert (parameterMethod != null);

			// If there are conflicts between the setter and the property annotation,
			// the setter annotation wins. (But DAMT.None is ignored)

			// Is this a property setter `value` parameter?
			if (parameterMethod!.MethodKind == MethodKind.PropertySet
				&& damt == DynamicallyAccessedMemberTypes.None
				&& parameter.Ordinal == parameterMethod.Parameters.Length - 1) {
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

		public static DynamicallyAccessedMemberTypes GetTypeAnnotation(ITypeSymbol type)
		{
			var typeAnnotation = type.GetDynamicallyAccessedMemberTypes ();

			ITypeSymbol? baseType = type.BaseType;
			while (baseType != null) {
				typeAnnotation |= baseType.GetDynamicallyAccessedMemberTypes ();
				baseType = baseType.BaseType;
			}

			foreach (var interfaceType in type.AllInterfaces) {
				typeAnnotation |= interfaceType.GetDynamicallyAccessedMemberTypes ();
			}

			return typeAnnotation;
		}

#pragma warning disable CA1822 // Mark members as static - the other partial implementations might need to be instance methods

		// TODO: This is relatively expensive on the analyzer since it doesn't cache the annotation information
		// For trimming tools this is an optimization to avoid the heavy lifting of analysis if there's no point
		// it's unclear if the same optimization makes sense for the analyzer.
		internal partial bool MethodRequiresDataFlowAnalysis (MethodProxy method)
			=> RequiresDataFlowAnalysis (method.Method);

		internal partial MethodReturnValue GetMethodReturnValue (MethodProxy method, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes)
			=> new MethodReturnValue (method.Method, dynamicallyAccessedMemberTypes);

		internal partial MethodReturnValue GetMethodReturnValue (MethodProxy method)
			=> GetMethodReturnValue (method, GetMethodReturnValueAnnotation (method.Method));

		internal partial GenericParameterValue GetGenericParameterValue (GenericParameterProxy genericParameter)
			=> new GenericParameterValue (genericParameter.TypeParameterSymbol);

		internal partial MethodParameterValue GetMethodThisParameterValue (MethodProxy method, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes)
		{
			if (!method.HasImplicitThis ())
				throw new InvalidOperationException ($"Cannot get 'this' parameter of method {method.GetDisplayName ()} with no 'this' parameter.");
			return GetMethodParameterValue (new ParameterProxy (method, (ParameterIndex) 0), dynamicallyAccessedMemberTypes);
		}

		// overrideIsThis is needed for backwards compatibility with MakeGenericType/Method https://github.com/dotnet/linker/issues/2428
		internal MethodParameterValue GetMethodThisParameterValue (MethodProxy method, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes, bool overrideIsThis = false)
		{
			if (!method.HasImplicitThis () && !overrideIsThis)
				throw new InvalidOperationException ($"Cannot get 'this' parameter of method {method.GetDisplayName ()} with no 'this' parameter.");
			return new MethodParameterValue (new ParameterProxy (method, (ParameterIndex) 0), dynamicallyAccessedMemberTypes, overrideIsThis);
		}

		internal partial MethodParameterValue GetMethodThisParameterValue (MethodProxy method)
		{
			if (!method.HasImplicitThis ())
				throw new InvalidOperationException ($"Cannot get 'this' parameter of method {method.GetDisplayName ()} with no 'this' parameter.");
			ParameterProxy param = new (method, (ParameterIndex) 0);
			var damt = GetMethodParameterAnnotation (param);
			return GetMethodParameterValue (new ParameterProxy (method, (ParameterIndex) 0), damt);
		}

		internal MethodParameterValue GetMethodParameterValue (MethodProxy method, ParameterIndex parameterIndex, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes)
			=> new MethodParameterValue (new (method, parameterIndex), dynamicallyAccessedMemberTypes);

		internal partial MethodParameterValue GetMethodParameterValue (ParameterProxy param)
			=> new MethodParameterValue (param, GetMethodParameterAnnotation (param));

		internal partial MethodParameterValue GetMethodParameterValue (ParameterProxy param, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes)
			=> new MethodParameterValue (param, dynamicallyAccessedMemberTypes);
#pragma warning restore CA1822
	}
}
