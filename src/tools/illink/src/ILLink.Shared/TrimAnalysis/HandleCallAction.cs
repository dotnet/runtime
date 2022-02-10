// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using ILLink.Shared.DataFlow;
using ILLink.Shared.TypeSystemProxy;
using MultiValue = ILLink.Shared.DataFlow.ValueSet<ILLink.Shared.DataFlow.SingleValue>;

namespace ILLink.Shared.TrimAnalysis
{
	[StructLayout (LayoutKind.Auto)] // A good way to avoid CS0282, we don't really care about field order
	partial struct HandleCallAction
	{
		static ValueSetLattice<SingleValue> MultiValueLattice => default;

		readonly DiagnosticContext _diagnosticContext;
		readonly RequireDynamicallyAccessedMembersAction _requireDynamicallyAccessedMembersAction;

		public bool Invoke (MethodProxy calledMethod, MultiValue instanceValue, IReadOnlyList<MultiValue> argumentValues, out MultiValue methodReturnValue)
		{
			MultiValue returnValue = default;

			bool requiresDataFlowAnalysis = MethodRequiresDataFlowAnalysis (calledMethod);
			DynamicallyAccessedMemberTypes returnValueDynamicallyAccessedMemberTypes = requiresDataFlowAnalysis ?
				GetReturnValueAnnotation (calledMethod) : 0;

			switch (Intrinsics.GetIntrinsicIdForMethod (calledMethod)) {
			case IntrinsicId.IntrospectionExtensions_GetTypeInfo:
				Debug.Assert (instanceValue.IsEmpty ());
				Debug.Assert (argumentValues.Count == 1);

				// typeof(Foo).GetTypeInfo()... will be commonly present in code targeting
				// the dead-end reflection refactoring. The call doesn't do anything and we
				// don't want to lose the annotation.
				returnValue = argumentValues[0];
				break;

			case IntrinsicId.TypeInfo_AsType:
				// someType.AsType()... will be commonly present in code targeting
				// the dead-end reflection refactoring. The call doesn't do anything and we
				// don't want to lose the annotation.
				returnValue = instanceValue;
				break;

			//
			// UnderlyingSystemType
			//
			case IntrinsicId.Type_get_UnderlyingSystemType:
				// This is identity for the purposes of the analysis.
				returnValue = instanceValue;
				break;

			case IntrinsicId.Type_GetTypeFromHandle:
				// Infrastructure piece to support "typeof(Foo)" in IL and direct calls everywhere
				foreach (var value in argumentValues[0]) {
					if (value is RuntimeTypeHandleValue typeHandle)
						AddReturnValue (new SystemTypeValue (typeHandle.RepresentedType));
					else if (value is RuntimeTypeHandleForGenericParameterValue typeHandleForGenericParameter)
						AddReturnValue (GetGenericParameterValue (typeHandleForGenericParameter.GenericParameter));
					else if (value == NullValue.Instance)
						AddReturnValue (value); // Have to return null as that represents "do nothing" value, not returning could lead to noise warnings
					else
						AddReturnValue (GetMethodReturnValue (calledMethod, returnValueDynamicallyAccessedMemberTypes));
				}
				break;

			case IntrinsicId.Type_get_TypeHandle:
				foreach (var value in instanceValue) {
					if (value is SystemTypeValue typeValue)
						AddReturnValue (new RuntimeTypeHandleValue (typeValue.RepresentedType));
					else if (value is GenericParameterValue genericParameterValue)
						AddReturnValue (new RuntimeTypeHandleForGenericParameterValue (genericParameterValue.GenericParameter));
					else if (value == NullValue.Instance) {
						AddReturnValue (value); // Have to return null as that represents "do nothing" value, not returning could lead to noise warnings
					} else
						AddReturnValue (GetMethodReturnValue (calledMethod, returnValueDynamicallyAccessedMemberTypes));
				}
				break;

			//
			// GetInterface (String)
			// GetInterface (String, bool)
			//
			case IntrinsicId.Type_GetInterface: {
					var targetValue = GetMethodThisParameterValue (calledMethod, DynamicallyAccessedMemberTypesOverlay.Interfaces);
					foreach (var value in instanceValue) {
						// For now no support for marking a single interface by name. We would have to correctly support
						// mangled names for generics to do that correctly. Simply mark all interfaces on the type for now.

						// Require Interfaces annotation
						_requireDynamicallyAccessedMembersAction.Invoke (value, targetValue);

						// Interfaces is transitive, so the return values will always have at least Interfaces annotation
						DynamicallyAccessedMemberTypes returnMemberTypes = DynamicallyAccessedMemberTypesOverlay.Interfaces;

						// Propagate All annotation across the call - All is a superset of Interfaces
						if (value is ValueWithDynamicallyAccessedMembers valueWithDynamicallyAccessedMembers
							&& valueWithDynamicallyAccessedMembers.DynamicallyAccessedMemberTypes == DynamicallyAccessedMemberTypes.All)
							returnMemberTypes = DynamicallyAccessedMemberTypes.All;

						AddReturnValue (GetMethodReturnValue (calledMethod, returnMemberTypes));
					}
				}
				break;

			//
			// AssemblyQualifiedName
			//
			case IntrinsicId.Type_get_AssemblyQualifiedName: {
					foreach (var value in instanceValue) {
						if (value is ValueWithDynamicallyAccessedMembers valueWithDynamicallyAccessedMembers) {
							// Currently we don't need to track the difference between Type and String annotated values
							// that only matters when we use them, so Type.GetType is the difference really.
							// For diagnostics we actually don't want to track the Type.AssemblyQualifiedName
							// as the annotation does not come from that call, but from its input.
							AddReturnValue (valueWithDynamicallyAccessedMembers);
						} else if (value == NullValue.Instance) {
							AddReturnValue (value); // Have to return null as that represents "do nothing" value, not returning could lead to noise warnings
						} else {
							AddReturnValue (UnknownValue.Instance);
						}
					}
				}
				break;

			//
			// System.Runtime.CompilerServices.RuntimeHelpers
			//
			// RunClassConstructor (RuntimeTypeHandle type)
			//
			case IntrinsicId.RuntimeHelpers_RunClassConstructor:
				foreach (var typeHandleValue in argumentValues[0]) {
					if (typeHandleValue is RuntimeTypeHandleValue runtimeTypeHandleValue) {
						MarkStaticConstructor (runtimeTypeHandleValue.RepresentedType);
					} else if (typeHandleValue == NullValue.Instance) {
						// Nothing to do, it should throw at runtime, but it doesn't change behavior of data flow in any way
					} else {
						_diagnosticContext.AddDiagnostic (DiagnosticId.UnrecognizedTypeInRuntimeHelpersRunClassConstructor, calledMethod.GetDisplayName ());
					}
				}
				break;

			case IntrinsicId.None:
				methodReturnValue = MultiValueLattice.Top;
				return false;

			// Disable warnings for all unimplemented intrinsics. Some intrinsic methods have annotations, but analyzing them
			// would produce unnecessary warnings even for cases that are intrinsically handled. So we disable handling these calls
			// until a proper intrinsic handling is made
			default:
				methodReturnValue = MultiValueLattice.Top;
				return true;
			}

			if (returnValue.IsEmpty () && !calledMethod.ReturnsVoid ()) {
				returnValue = GetMethodReturnValue (calledMethod, returnValueDynamicallyAccessedMemberTypes);
			}

			// Validate that the return value has the correct annotations as per the method return value annotations
			if (returnValueDynamicallyAccessedMemberTypes != 0) {
				foreach (var uniqueValue in returnValue) {
					if (uniqueValue is ValueWithDynamicallyAccessedMembers methodReturnValueWithMemberTypes) {
						if (!methodReturnValueWithMemberTypes.DynamicallyAccessedMemberTypes.HasFlag (returnValueDynamicallyAccessedMemberTypes))
							throw new InvalidOperationException ($"Internal linker error: in {GetContainingSymbolDisplayName ()} processing call to {calledMethod.GetDisplayName ()} returned value which is not correctly annotated with the expected dynamic member access kinds.");
					} else if (uniqueValue is SystemTypeValue) {
						// SystemTypeValue can fullfill any requirement, so it's always valid
						// The requirements will be applied at the point where it's consumed (passed as a method parameter, set as field value, returned from the method)
					} else {
						throw new InvalidOperationException ($"Internal linker error: in {GetContainingSymbolDisplayName ()} processing call to {calledMethod.GetDisplayName ()} returned value which is not correctly annotated with the expected dynamic member access kinds.");
					}
				}
			}

			methodReturnValue = returnValue;

			return true;

			void AddReturnValue (MultiValue value)
			{
				returnValue = MultiValueLattice.Meet (returnValue, value);
			}
		}

		private partial bool MethodRequiresDataFlowAnalysis (MethodProxy method);

		private partial DynamicallyAccessedMemberTypes GetReturnValueAnnotation (MethodProxy method);

		private partial MethodReturnValue GetMethodReturnValue (MethodProxy method, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes);

		private partial GenericParameterValue GetGenericParameterValue (GenericParameterProxy genericParameter);

		private partial MethodThisParameterValue GetMethodThisParameterValue (MethodProxy method, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes);

		private partial void MarkStaticConstructor (TypeProxy type);

		// Only used for internal diagnostic purposes (not even for warning messages)
		private partial string GetContainingSymbolDisplayName ();
	}
}
