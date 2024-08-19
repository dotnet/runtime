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

		internal static bool ShouldWarnWhenAccessedForReflection (ISymbol symbol) =>
			symbol switch {
				IMethodSymbol method => ShouldWarnWhenAccessedForReflection (method),
				IFieldSymbol field => ShouldWarnWhenAccessedForReflection (field),
				_ => false
			};

		static bool ShouldWarnWhenAccessedForReflection (IMethodSymbol method)
		{
			bool? hasParameterAnnotation = null;
			if (GetMethodReturnValueAnnotation (method) == DynamicallyAccessedMemberTypes.None) {
				if (!HasParameterAnnotation (method))
					return false;
				hasParameterAnnotation = true;
			}

			// If the method only has annotation on the return value and it's not virtual avoid warning.
			// Return value annotations are "consumed" by the caller of a method, and as such there is nothing
			// wrong calling these dynamically. The only problem can happen if something overrides a virtual
			// method with annotated return value at runtime - in this case the trimmer can't validate
			// that the method will return only types which fulfill the annotation's requirements.
			// For example:
			//   class BaseWithAnnotation
			//   {
			//       [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
			//       public abstract Type GetTypeWithFields();
			//   }
			//
			//   class UsingTheBase
			//   {
			//       public void PrintFields(Base base)
			//       {
			//            // No warning here - GetTypeWithFields is correctly annotated to allow GetFields on the return value.
			//            Console.WriteLine(string.Join(" ", base.GetTypeWithFields().GetFields().Select(f => f.Name)));
			//       }
			//   }
			//
			// If at runtime (through ref emit) something generates code like this:
			//   class DerivedAtRuntimeFromBase
			//   {
			//       // No point in adding annotation on the return value - nothing will look at it anyway
			//       // Trimming will not see this code, so there are no checks
			//       public override Type GetTypeWithFields() { return typeof(TestType); }
			//   }
			//
			// If TestType from above is trimmed, it may not have all its fields, and there would be no warnings generated.
			// But there has to be code like this somewhere in the app, in order to generate the override:
			//   class RuntimeTypeGenerator
			//   {
			//       public MethodInfo GetBaseMethod()
			//       {
			//            // This must warn - that the GetTypeWithFields has annotation on the return value
			//            return typeof(BaseWithAnnotation).GetMethod("GetTypeWithFields");
			//       }
			//   }

			return method.IsVirtual || method.IsOverride || (hasParameterAnnotation ?? HasParameterAnnotation (method));

			static bool HasParameterAnnotation (IMethodSymbol method) {
				foreach (var param in method.GetParameters ()) {
					if (GetMethodParameterAnnotation (param) != DynamicallyAccessedMemberTypes.None)
						return true;
				}
				return false;
			}
		}

		static bool ShouldWarnWhenAccessedForReflection (IFieldSymbol field)
		{
			return GetFieldAnnotation (field) != DynamicallyAccessedMemberTypes.None;
		}

		internal static DynamicallyAccessedMemberTypes GetFieldAnnotation (IFieldSymbol field)
		{
			if (!field.Type.IsTypeInterestingForDataflow (isByRef: field.RefKind is not RefKind.None))
				return DynamicallyAccessedMemberTypes.None;

			return field.GetDynamicallyAccessedMemberTypes ();
		}

		internal static DynamicallyAccessedMemberTypes GetTypeAnnotations (INamedTypeSymbol type)
		{
			DynamicallyAccessedMemberTypes typeAnnotation = type.GetDynamicallyAccessedMemberTypes ();

			// Also inherit annotation from bases
			INamedTypeSymbol? baseType = type.BaseType;
			while (baseType is not null) {
				typeAnnotation |= baseType.GetDynamicallyAccessedMemberTypes ();
				baseType = baseType.BaseType;
			}

			// And inherit them from interfaces
			foreach (INamedTypeSymbol interfaceType in type.AllInterfaces) {
				typeAnnotation |= interfaceType.GetDynamicallyAccessedMemberTypes ();
			}

			return typeAnnotation;
		}

		internal static DynamicallyAccessedMemberTypes GetMethodParameterAnnotation (ParameterProxy param)
		{
			bool isByRef = param.ParameterSymbol is IParameterSymbol paramSymbol && paramSymbol.RefKind is not RefKind.None;
			if (!param.ParameterType.Type.IsTypeInterestingForDataflow (isByRef))
				return DynamicallyAccessedMemberTypes.None;

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

		internal partial MethodReturnValue GetMethodReturnValue (MethodProxy method, bool isNewObj, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes)
			=> new MethodReturnValue (method.Method, isNewObj, dynamicallyAccessedMemberTypes);

		internal partial MethodReturnValue GetMethodReturnValue (MethodProxy method, bool isNewObj)
			=> GetMethodReturnValue (method, isNewObj, GetMethodReturnValueAnnotation (method.Method));

		internal partial GenericParameterValue GetGenericParameterValue (GenericParameterProxy genericParameter, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes)
			=> new GenericParameterValue (genericParameter.TypeParameterSymbol, dynamicallyAccessedMemberTypes);

		internal partial GenericParameterValue GetGenericParameterValue (GenericParameterProxy genericParameter)
			=> new GenericParameterValue (genericParameter.TypeParameterSymbol);

		internal partial MethodParameterValue GetMethodThisParameterValue (MethodProxy method, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes)
		{
			if (!method.HasImplicitThis ())
				throw new InvalidOperationException ($"Cannot get 'this' parameter of method {method.GetDisplayName ()} with no 'this' parameter.");
			return GetMethodParameterValue (new ParameterProxy (method, (ParameterIndex) 0), dynamicallyAccessedMemberTypes);
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
