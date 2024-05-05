// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using ILLink.Shared.TypeSystemProxy;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Linker;
using Mono.Linker.Dataflow;
using Mono.Linker.Steps;
using MultiValue = ILLink.Shared.DataFlow.ValueSet<ILLink.Shared.DataFlow.SingleValue>;

namespace ILLink.Shared.TrimAnalysis
{
	internal partial struct HandleCallAction
	{
#pragma warning disable CA1822 // Mark members as static - the other partial implementations might need to be instance methods

		readonly LinkContext _context;
		readonly Instruction _operation;
		readonly MarkStep _markStep;
		readonly ReflectionMarker _reflectionMarker;
		readonly MethodDefinition _callingMethodDefinition;
		readonly MethodReference _calledMethodReference;

		public HandleCallAction (
			LinkContext context,
			Instruction operation,
			MarkStep markStep,
			ReflectionMarker reflectionMarker,
			in DiagnosticContext diagnosticContext,
			MethodDefinition callingMethodDefinition,
			MethodReference calledMethodReference)
		{
			_context = context;
			_operation = operation;
			_isNewObj = operation.OpCode == OpCodes.Newobj;
			_markStep = markStep;
			_reflectionMarker = reflectionMarker;
			_diagnosticContext = diagnosticContext;
			_callingMethodDefinition = callingMethodDefinition;
			_annotations = context.Annotations.FlowAnnotations;
			_requireDynamicallyAccessedMembersAction = new (reflectionMarker, diagnosticContext);
			_calledMethodReference = calledMethodReference;
		}

		private partial bool TryHandleIntrinsic (
			MethodProxy calledMethod,
			MultiValue instanceValue,
			IReadOnlyList<MultiValue> argumentValues,
			IntrinsicId intrinsicId,
			out MultiValue? methodReturnValue)
		{
			MultiValue? maybeMethodReturnValue = methodReturnValue = null;
			Debug.Assert (calledMethod.Method == _context.Resolve (_calledMethodReference));

			switch (intrinsicId) {
			case IntrinsicId.None: {
					if (ReflectionMethodBodyScanner.IsPInvokeDangerous (calledMethod.Method, _context, out bool comDangerousMethod)) {
						Debug.Assert (comDangerousMethod); // Currently COM dangerous is the only one we detect
						_diagnosticContext.AddDiagnostic (DiagnosticId.CorrectnessOfCOMCannotBeGuaranteed, calledMethod.GetDisplayName ());
					}
					if (_context.Annotations.DoesMethodRequireUnreferencedCode (calledMethod.Method, out RequiresUnreferencedCodeAttribute? requiresUnreferencedCode))
						MarkStep.ReportRequiresUnreferencedCode (calledMethod.GetDisplayName (), requiresUnreferencedCode, _diagnosticContext);

					return TryHandleSharedIntrinsic (calledMethod, instanceValue, argumentValues, intrinsicId, out methodReturnValue);
				}

			case IntrinsicId.TypeDelegator_Ctor: {
					// This is an identity function for analysis purposes
					if (_operation.OpCode == OpCodes.Newobj)
						AddReturnValue (argumentValues[0]);
				}
				break;

			case IntrinsicId.Array_Empty: {
					AddReturnValue (ArrayValue.Create (0, ((GenericInstanceMethod) _calledMethodReference).GenericArguments[0]));
				}
				break;

			case IntrinsicId.Array_CreateInstance:
			case IntrinsicId.Enum_GetValues:
			case IntrinsicId.Marshal_SizeOf:
			case IntrinsicId.Marshal_OffsetOf:
			case IntrinsicId.Marshal_PtrToStructure:
			case IntrinsicId.Marshal_DestroyStructure:
			case IntrinsicId.Marshal_GetDelegateForFunctionPointer:
			case IntrinsicId.Assembly_get_Location:
			case IntrinsicId.Assembly_GetFile:
			case IntrinsicId.Assembly_GetFiles:
			case IntrinsicId.AssemblyName_get_CodeBase:
			case IntrinsicId.AssemblyName_get_EscapedCodeBase:
			case IntrinsicId.RuntimeReflectionExtensions_GetMethodInfo:
			case IntrinsicId.Delegate_get_Method:
				// These intrinsics are not interesting for trimmer (they are interesting for AOT and that's why they are recognized)
				break;

			//
			// System.Object
			//
			// GetType()
			//
			case IntrinsicId.Object_GetType: {
					foreach (var valueNode in instanceValue.AsEnumerable ()) {
						// Note that valueNode can be statically typed in IL as some generic argument type.
						// For example:
						//   void Method<T>(T instance) { instance.GetType().... }
						// Currently this case will end up with null StaticType - since there's no typedef for the generic argument type.
						// But it could be that T is annotated with for example PublicMethods:
						//   void Method<[DAM(PublicMethods)] T>(T instance) { instance.GetType().GetMethod("Test"); }
						// In this case it's in theory possible to handle it, by treating the T basically as a base class
						// for the actual type of "instance". But the analysis for this would be pretty complicated (as the marking
						// has to happen on the callsite, which doesn't know that GetType() will be used...).
						// For now we're intentionally ignoring this case - it will produce a warning.
						// The counter example is:
						//   Method<Base>(new Derived);
						// In this case to get correct results, trimmer would have to mark all public methods on Derived. Which
						// currently it won't do.

						TypeDefinition? staticType = (valueNode as IValueWithStaticType)?.StaticType?.Type;
						if (staticType is null) {
							// We don't know anything about the type GetType was called on. Track this as a usual result of a method call without any annotations
							AddReturnValue (_context.Annotations.FlowAnnotations.GetMethodReturnValue (calledMethod, _isNewObj));
						} else if (staticType.IsSealed || staticType.IsTypeOf ("System", "Delegate") || staticType.IsTypeOf ("System", "Array")) {
							// We can treat this one the same as if it was a typeof() expression

							// We can allow Object.GetType to be modeled as System.Delegate because we keep all methods
							// on delegates anyway so reflection on something this approximation would miss is actually safe.

							// We can also treat all arrays as "sealed" since it's not legal to derive from Array type (even though it is not sealed itself)

							// We ignore the fact that the type can be annotated (see below for handling of annotated types)
							// This means the annotations (if any) won't be applied - instead we rely on the exact knowledge
							// of the type. So for example even if the type is annotated with PublicMethods
							// but the code calls GetProperties on it - it will work - mark properties, don't mark methods
							// since we ignored the fact that it's annotated.
							// This can be seen a little bit as a violation of the annotation, but we already have similar cases
							// where a parameter is annotated and if something in the method sets a specific known type to it
							// we will also make it just work, even if the annotation doesn't match the usage.
							AddReturnValue (new SystemTypeValue (staticType));
						} else {
							// Make sure the type is marked (this will mark it as used via reflection, which is sort of true)
							// This should already be true for most cases (method params, fields, ...), but just in case
							_reflectionMarker.MarkType (_diagnosticContext.Origin, staticType);

							var annotation = _markStep.DynamicallyAccessedMembersTypeHierarchy
								.ApplyDynamicallyAccessedMembersToTypeHierarchy (staticType);

							// Return a value which is "unknown type" with annotation. For now we'll use the return value node
							// for the method, which means we're loosing the information about which staticType this
							// started with. For now we don't need it, but we can add it later on.
							AddReturnValue (_context.Annotations.FlowAnnotations.GetMethodReturnValue (calledMethod, _isNewObj, annotation));
						}
					}
				}
				break;

			// Note about Activator.CreateInstance<T>
			// There are 2 interesting cases:
			//  - The generic argument for T is either specific type or annotated - in that case generic instantiation will handle this
			//    since from .NET 6+ the T is annotated with PublicParameterlessConstructor annotation, so the trimming tools would apply this as for any other method.
			//  - The generic argument for T is unannotated type - the generic instantiantion handling has a special case for handling PublicParameterlessConstructor requirement
			//    in such that if the generic argument type has the "new" constraint it will not warn (as it is effectively the same thing semantically).
			//    For all other cases, the trimming tools would have already produced a warning.

			default:
				return false;
			}

			methodReturnValue = maybeMethodReturnValue;
			return true;

			void AddReturnValue (MultiValue value)
			{
				maybeMethodReturnValue = (maybeMethodReturnValue is null) ? value : MultiValueLattice.Meet ((MultiValue) maybeMethodReturnValue, value);
			}
		}

		private partial bool MethodIsTypeConstructor (MethodProxy method)
		{
			if (!method.Method.IsConstructor)
				return false;
			TypeDefinition? type = method.Method.DeclaringType;
			while (type is not null) {
				if (type.IsTypeOf (WellKnownType.System_Type))
					return true;
				type = _context.Resolve (type.BaseType);
			}
			return false;
		}

		private partial IEnumerable<SystemReflectionMethodBaseValue> GetMethodsOnTypeHierarchy (TypeProxy type, string name, BindingFlags? bindingFlags)
		{
			foreach (var method in type.Type.GetMethodsOnTypeHierarchy (_context, m => m.Name == name, bindingFlags))
				yield return new SystemReflectionMethodBaseValue (new MethodProxy (method));
		}

		private partial IEnumerable<SystemTypeValue> GetNestedTypesOnType (TypeProxy type, string name, BindingFlags? bindingFlags)
		{
			foreach (var nestedType in type.Type.GetNestedTypesOnType (t => t.Name == name, bindingFlags))
				yield return new SystemTypeValue (new TypeProxy (nestedType));
		}

		private partial bool TryGetBaseType (TypeProxy type, out TypeProxy? baseType)
		{
			if (type.Type.BaseType is TypeReference baseTypeRef && _context.TryResolve (baseTypeRef) is TypeDefinition baseTypeDefinition) {
				baseType = new TypeProxy (baseTypeDefinition);
				return true;
			}

			baseType = null;
			return false;
		}

		private partial bool TryResolveTypeNameForCreateInstanceAndMark (in MethodProxy calledMethod, string assemblyName, string typeName, out TypeProxy resolvedType)
		{
			var resolvedAssembly = _context.TryResolve (assemblyName);
			if (resolvedAssembly == null) {
				_diagnosticContext.AddDiagnostic (DiagnosticId.UnresolvedAssemblyInCreateInstance,
					assemblyName,
					calledMethod.GetDisplayName ());
				resolvedType = default;
				return false;
			}

			if (!_reflectionMarker.TryResolveTypeNameAndMark (resolvedAssembly, typeName, _diagnosticContext, out TypeDefinition? resolvedTypeDefinition)
				|| resolvedTypeDefinition.IsTypeOf (WellKnownType.System_Array)) {
				// It's not wrong to have a reference to non-existing type - the code may well expect to get an exception in this case
				// Note that we did find the assembly, so it's not a ILLink config problem, it's either intentional, or wrong versions of assemblies
				// but ILLink can't know that. In case a user tries to create an array using System.Activator we should simply ignore it, the user
				// might expect an exception to be thrown.
				resolvedType = default;
				return false;
			}

			resolvedType = new TypeProxy (resolvedTypeDefinition);
			return true;
		}

		private partial void MarkStaticConstructor (TypeProxy type)
			=> _reflectionMarker.MarkStaticConstructor (_diagnosticContext.Origin, type.Type);

		private partial void MarkEventsOnTypeHierarchy (TypeProxy type, string name, BindingFlags? bindingFlags)
			=> _reflectionMarker.MarkEventsOnTypeHierarchy (_diagnosticContext.Origin, type.Type, e => e.Name == name, bindingFlags);

		private partial void MarkFieldsOnTypeHierarchy (TypeProxy type, string name, BindingFlags? bindingFlags)
			=> _reflectionMarker.MarkFieldsOnTypeHierarchy (_diagnosticContext.Origin, type.Type, f => f.Name == name, bindingFlags);

		private partial void MarkPropertiesOnTypeHierarchy (TypeProxy type, string name, BindingFlags? bindingFlags)
			=> _reflectionMarker.MarkPropertiesOnTypeHierarchy (_diagnosticContext.Origin, type.Type, p => p.Name == name, bindingFlags);

		private partial void MarkPublicParameterlessConstructorOnType (TypeProxy type)
			=> _reflectionMarker.MarkConstructorsOnType (_diagnosticContext.Origin, type.Type, m => m.IsPublic && !m.HasMetadataParameters ());

		private partial void MarkConstructorsOnType (TypeProxy type, BindingFlags? bindingFlags, int? parameterCount)
			=> _reflectionMarker.MarkConstructorsOnType (_diagnosticContext.Origin, type.Type, (parameterCount == null) ? null : m => m.GetMetadataParametersCount () == parameterCount, bindingFlags);

		private partial void MarkMethod (MethodProxy method)
			=> _reflectionMarker.MarkMethod (_diagnosticContext.Origin, method.Method);

		private partial void MarkType (TypeProxy type)
			=> _reflectionMarker.MarkType (_diagnosticContext.Origin, type.Type);

		private partial bool MarkAssociatedProperty (MethodProxy method)
		{
			if (method.Method.TryGetProperty (out PropertyDefinition? propertyDefinition)) {
				_reflectionMarker.MarkProperty (_diagnosticContext.Origin, propertyDefinition);
				return true;
			}

			return false;
		}

		private partial string GetContainingSymbolDisplayName () => _callingMethodDefinition.GetDisplayName ();
	}
}
