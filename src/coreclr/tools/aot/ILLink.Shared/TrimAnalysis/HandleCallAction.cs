// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using ILLink.Shared.DataFlow;
using ILLink.Shared.TypeSystemProxy;
using MultiValue = ILLink.Shared.DataFlow.ValueSet<ILLink.Shared.DataFlow.SingleValue>;

// This is needed due to NativeAOT which doesn't enable nullable globally yet
#nullable enable

namespace ILLink.Shared.TrimAnalysis
{
	[StructLayout (LayoutKind.Auto)] // A good way to avoid CS0282, we don't really care about field order
	internal partial struct HandleCallAction
	{
		private static ValueSetLattice<SingleValue> MultiValueLattice => default;

		private readonly DiagnosticContext _diagnosticContext;
		private readonly FlowAnnotations _annotations;
		private readonly RequireDynamicallyAccessedMembersAction _requireDynamicallyAccessedMembersAction;

		public bool Invoke (MethodProxy calledMethod, MultiValue instanceValue, IReadOnlyList<MultiValue> argumentValues, out MultiValue methodReturnValue, out IntrinsicId intrinsicId)
		{
			MultiValue? returnValue = null;

			bool requiresDataFlowAnalysis = _annotations.MethodRequiresDataFlowAnalysis (calledMethod);
			var annotatedMethodReturnValue = _annotations.GetMethodReturnValue (calledMethod);
			Debug.Assert (requiresDataFlowAnalysis || annotatedMethodReturnValue.DynamicallyAccessedMemberTypes == DynamicallyAccessedMemberTypes.None);

			intrinsicId = Intrinsics.GetIntrinsicIdForMethod (calledMethod);
			switch (intrinsicId) {
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
				if (argumentValues[0].IsEmpty ()) {
					returnValue = MultiValueLattice.Top;
					break;
				}

				foreach (var value in argumentValues[0]) {
					AddReturnValue (value switch {
						RuntimeTypeHandleForNullableSystemTypeValue nullableSystemType
							=> new NullableSystemTypeValue (nullableSystemType.NullableType, nullableSystemType.UnderlyingTypeValue),
						// When generating type handles from IL, the GenericParameterValue with DAM annotations is not available.
						// Once we convert it to a Value with annotations here, there is no need to convert it back in get_TypeHandle
						RuntimeTypeHandleForNullableValueWithDynamicallyAccessedMembers nullableDamType when nullableDamType.UnderlyingTypeValue is RuntimeTypeHandleForGenericParameterValue underlyingGenericParameter
							=> new NullableValueWithDynamicallyAccessedMembers (nullableDamType.NullableType, _annotations.GetGenericParameterValue (underlyingGenericParameter.GenericParameter)),
						// This should only happen if the code does something like typeof(Nullable<>).MakeGenericType(methodParameter).TypeHandle
						RuntimeTypeHandleForNullableValueWithDynamicallyAccessedMembers nullableDamType when nullableDamType.UnderlyingTypeValue is ValueWithDynamicallyAccessedMembers underlyingTypeValue
							=> new NullableValueWithDynamicallyAccessedMembers (nullableDamType.NullableType, underlyingTypeValue),
						RuntimeTypeHandleValue typeHandle
							=> new SystemTypeValue (typeHandle.RepresentedType),
						RuntimeTypeHandleForGenericParameterValue genericParam
							=> _annotations.GetGenericParameterValue (genericParam.GenericParameter),
						_ => annotatedMethodReturnValue
					});
				}
				break;

			case IntrinsicId.Type_get_TypeHandle:
				if (instanceValue.IsEmpty ()) {
					returnValue = MultiValueLattice.Top;
					break;
				}

				foreach (var value in instanceValue) {
					if (value != NullValue.Instance)
						AddReturnValue (value switch {
							NullableSystemTypeValue nullableSystemType
								=> new RuntimeTypeHandleForNullableSystemTypeValue (nullableSystemType.NullableType, nullableSystemType.UnderlyingTypeValue),
							NullableValueWithDynamicallyAccessedMembers nullableDamType when nullableDamType.UnderlyingTypeValue is GenericParameterValue genericParam
								=> new RuntimeTypeHandleForNullableValueWithDynamicallyAccessedMembers (nullableDamType.NullableType, new RuntimeTypeHandleForGenericParameterValue (genericParam.GenericParameter)),
							NullableValueWithDynamicallyAccessedMembers nullableDamType
								=> new RuntimeTypeHandleForNullableValueWithDynamicallyAccessedMembers (nullableDamType.NullableType, nullableDamType.UnderlyingTypeValue),
							SystemTypeValue typeHandle
								=> new RuntimeTypeHandleValue (typeHandle.RepresentedType),
							GenericParameterValue genericParam
								=> new RuntimeTypeHandleForGenericParameterValue (genericParam.GenericParameter),
							_ => annotatedMethodReturnValue
						});
					else
						AddReturnValue (MultiValueLattice.Top);
				}
				break;

			// System.Reflection.MethodBase.GetMethodFromHandle (RuntimeMethodHandle handle)
			// System.Reflection.MethodBase.GetMethodFromHandle (RuntimeMethodHandle handle, RuntimeTypeHandle declaringType)
			case IntrinsicId.MethodBase_GetMethodFromHandle: {
					if (argumentValues[0].IsEmpty ()) {
						returnValue = MultiValueLattice.Top;
						break;
					}

					// Infrastructure piece to support "ldtoken method -> GetMethodFromHandle"
					foreach (var value in argumentValues[0]) {
						if (value is RuntimeMethodHandleValue methodHandle)
							AddReturnValue (new SystemReflectionMethodBaseValue (methodHandle.RepresentedMethod));
						else
							AddReturnValue (annotatedMethodReturnValue);
					}
				}
				break;

			case IntrinsicId.MethodBase_get_MethodHandle: {
					if (instanceValue.IsEmpty ()) {
						returnValue = MultiValueLattice.Top;
						break;
					}

					foreach (var value in instanceValue) {
						if (value is SystemReflectionMethodBaseValue methodBaseValue)
							AddReturnValue (new RuntimeMethodHandleValue (methodBaseValue.RepresentedMethod));
						else
							AddReturnValue (annotatedMethodReturnValue);
					}
				}
				break;

			case IntrinsicId.TypeDelegator_Ctor:
			// This needs additional validation that the .ctor is called from a "newobj" instruction/operation
			// so it can't be done easily in shared code yet.
			case IntrinsicId.Array_Empty:
				// Array.Empty<T> must for now be handled by the specific implementation since it requires instantiated generic method handling
				methodReturnValue = MultiValueLattice.Top;
				return false;

			//
			// GetInterface (String)
			// GetInterface (String, bool)
			//
			case IntrinsicId.Type_GetInterface: {
					if (instanceValue.IsEmpty () || argumentValues[0].IsEmpty ()) {
						returnValue = MultiValueLattice.Top;
						break;
					}

					var targetValue = _annotations.GetMethodThisParameterValue (calledMethod, DynamicallyAccessedMemberTypesOverlay.Interfaces);
					foreach (var value in instanceValue) {
						foreach (var interfaceName in argumentValues[0]) {
							if (interfaceName == NullValue.Instance) {
								// Throws on null string, so no return value.
								AddReturnValue (MultiValueLattice.Top);
							} else if (interfaceName is KnownStringValue stringValue && stringValue.Contents.Length == 0) {
								AddReturnValue (NullValue.Instance);
							} else {
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

								AddReturnValue (_annotations.GetMethodReturnValue (calledMethod, returnMemberTypes));
							}
						}
					}
				}
				break;

			//
			// AssemblyQualifiedName
			//
			case IntrinsicId.Type_get_AssemblyQualifiedName: {
					if (instanceValue.IsEmpty ()) {
						returnValue = MultiValueLattice.Top;
						break;
					}

					foreach (var value in instanceValue) {
						if (value is ValueWithDynamicallyAccessedMembers valueWithDynamicallyAccessedMembers) {
							// Currently we don't need to track the difference between Type and String annotated values
							// that only matters when we use them, so Type.GetType is the difference really.
							// For diagnostics we actually don't want to track the Type.AssemblyQualifiedName
							// as the annotation does not come from that call, but from its input.
							AddReturnValue (valueWithDynamicallyAccessedMembers);
						} else if (value == NullValue.Instance) {
							// NullReferenceException, no return value.
							AddReturnValue (MultiValueLattice.Top);
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
				if (argumentValues[0].IsEmpty ()) {
					returnValue = MultiValueLattice.Top;
					break;
				}

				foreach (var typeHandleValue in argumentValues[0]) {
					if (typeHandleValue is RuntimeTypeHandleValue runtimeTypeHandleValue) {
						MarkStaticConstructor (runtimeTypeHandleValue.RepresentedType);
					} else {
						_diagnosticContext.AddDiagnostic (DiagnosticId.UnrecognizedTypeInRuntimeHelpersRunClassConstructor, calledMethod.GetDisplayName ());
					}
				}
				break;

			//
			// GetConstructors (BindingFlags)
			// GetMethods (BindingFlags)
			// GetFields (BindingFlags)
			// GetEvents (BindingFlags)
			// GetProperties (BindingFlags)
			// GetNestedTypes (BindingFlags)
			// GetMembers (BindingFlags)
			//
			case var callType when (callType == IntrinsicId.Type_GetConstructors || callType == IntrinsicId.Type_GetMethods || callType == IntrinsicId.Type_GetFields ||
				callType == IntrinsicId.Type_GetProperties || callType == IntrinsicId.Type_GetEvents || callType == IntrinsicId.Type_GetNestedTypes || callType == IntrinsicId.Type_GetMembers)
				&& calledMethod.IsDeclaredOnType ("System.Type")
				&& calledMethod.HasParameterOfType (0, "System.Reflection.BindingFlags")
				&& !calledMethod.IsStatic (): {

					BindingFlags? bindingFlags;
					bindingFlags = GetBindingFlagsFromValue (argumentValues[0]);
					DynamicallyAccessedMemberTypes memberTypes;
					if (BindingFlagsAreUnsupported (bindingFlags)) {
						memberTypes = callType switch {
							IntrinsicId.Type_GetConstructors => DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors,
							IntrinsicId.Type_GetMethods => DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods,
							IntrinsicId.Type_GetEvents => DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents,
							IntrinsicId.Type_GetFields => DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields,
							IntrinsicId.Type_GetProperties => DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties,
							IntrinsicId.Type_GetNestedTypes => DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes,
							IntrinsicId.Type_GetMembers => DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors |
								DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents |
								DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields |
								DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods |
								DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties |
								DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes,
							_ => throw new ArgumentException ($"Reflection call '{calledMethod.GetDisplayName ()}' inside '{GetContainingSymbolDisplayName ()}' is of unexpected member type."),
						};
					} else {
						memberTypes = callType switch {
							IntrinsicId.Type_GetConstructors => GetDynamicallyAccessedMemberTypesFromBindingFlagsForConstructors (bindingFlags),
							IntrinsicId.Type_GetMethods => GetDynamicallyAccessedMemberTypesFromBindingFlagsForMethods (bindingFlags),
							IntrinsicId.Type_GetEvents => GetDynamicallyAccessedMemberTypesFromBindingFlagsForEvents (bindingFlags),
							IntrinsicId.Type_GetFields => GetDynamicallyAccessedMemberTypesFromBindingFlagsForFields (bindingFlags),
							IntrinsicId.Type_GetProperties => GetDynamicallyAccessedMemberTypesFromBindingFlagsForProperties (bindingFlags),
							IntrinsicId.Type_GetNestedTypes => GetDynamicallyAccessedMemberTypesFromBindingFlagsForNestedTypes (bindingFlags),
							IntrinsicId.Type_GetMembers => GetDynamicallyAccessedMemberTypesFromBindingFlagsForMembers (bindingFlags),
							_ => throw new ArgumentException ($"Reflection call '{calledMethod.GetDisplayName ()}' inside '{GetContainingSymbolDisplayName ()}' is of unexpected member type."),
						};
					}

					var targetValue = _annotations.GetMethodThisParameterValue (calledMethod, memberTypes);
					_requireDynamicallyAccessedMembersAction.Invoke (instanceValue, targetValue);
				}
				break;

			//
			// GetField (string)
			// GetField (string, BindingFlags)
			// GetEvent (string)
			// GetEvent (string, BindingFlags)
			// GetProperty (string)
			// GetProperty (string, BindingFlags)
			// GetProperty (string, Type)
			// GetProperty (string, Type[])
			// GetProperty (string, Type, Type[])
			// GetProperty (string, Type, Type[], ParameterModifier[])
			// GetProperty (string, BindingFlags, Binder, Type, Type[], ParameterModifier[])
			//
			case var fieldPropertyOrEvent when (fieldPropertyOrEvent == IntrinsicId.Type_GetField || fieldPropertyOrEvent == IntrinsicId.Type_GetProperty || fieldPropertyOrEvent == IntrinsicId.Type_GetEvent)
				&& calledMethod.IsDeclaredOnType ("System.Type")
				&& calledMethod.HasParameterOfType (0, "System.String")
				&& !calledMethod.IsStatic (): {

					if (instanceValue.IsEmpty () || argumentValues[0].IsEmpty ()) {
						returnValue = MultiValueLattice.Top;
						break;
					}

					BindingFlags? bindingFlags;
					if (calledMethod.HasParameterOfType (1, "System.Reflection.BindingFlags"))
						bindingFlags = GetBindingFlagsFromValue (argumentValues[1]);
					else
						// Assume a default value for BindingFlags for methods that don't use BindingFlags as a parameter
						bindingFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public;

					DynamicallyAccessedMemberTypes memberTypes = fieldPropertyOrEvent switch {
						IntrinsicId.Type_GetEvent => GetDynamicallyAccessedMemberTypesFromBindingFlagsForEvents (bindingFlags),
						IntrinsicId.Type_GetField => GetDynamicallyAccessedMemberTypesFromBindingFlagsForFields (bindingFlags),
						IntrinsicId.Type_GetProperty => GetDynamicallyAccessedMemberTypesFromBindingFlagsForProperties (bindingFlags),
						_ => throw new ArgumentException ($"Reflection call '{calledMethod.GetDisplayName ()}' inside '{GetContainingSymbolDisplayName ()}' is of unexpected member type."),
					};

					var targetValue = _annotations.GetMethodThisParameterValue (calledMethod, memberTypes);
					foreach (var value in instanceValue) {
						if (value is SystemTypeValue systemTypeValue) {
							foreach (var stringParam in argumentValues[0]) {
								if (stringParam is KnownStringValue stringValue && !BindingFlagsAreUnsupported (bindingFlags)) {
									switch (fieldPropertyOrEvent) {
									case IntrinsicId.Type_GetEvent:
										MarkEventsOnTypeHierarchy (systemTypeValue.RepresentedType, stringValue.Contents, bindingFlags);
										break;
									case IntrinsicId.Type_GetField:
										MarkFieldsOnTypeHierarchy (systemTypeValue.RepresentedType, stringValue.Contents, bindingFlags);
										break;
									case IntrinsicId.Type_GetProperty:
										MarkPropertiesOnTypeHierarchy (systemTypeValue.RepresentedType, stringValue.Contents, bindingFlags);
										break;
									default:
										Debug.Fail ("Unreachable.");
										break;
									}
								} else {
									_requireDynamicallyAccessedMembersAction.Invoke (value, targetValue);
								}
							}
						} else {
							_requireDynamicallyAccessedMembersAction.Invoke (value, targetValue);
						}
					}
				}
				break;

			//
			// GetMember (String)
			// GetMember (String, BindingFlags)
			// GetMember (String, MemberTypes, BindingFlags)
			//
			case IntrinsicId.Type_GetMember: {
					if (instanceValue.IsEmpty ()) {
						returnValue = MultiValueLattice.Top;
						break;
					}

					BindingFlags? bindingFlags;
					if (calledMethod.HasParametersCount (1)) {
						// Assume a default value for BindingFlags for methods that don't use BindingFlags as a parameter
						bindingFlags = BindingFlags.Public | BindingFlags.Instance;
					} else if (calledMethod.HasParametersCount (2) && calledMethod.HasParameterOfType (1, "System.Reflection.BindingFlags"))
						bindingFlags = GetBindingFlagsFromValue (argumentValues[1]);
					else if (calledMethod.HasParametersCount (3) && calledMethod.HasParameterOfType (2, "System.Reflection.BindingFlags")) {
						bindingFlags = GetBindingFlagsFromValue (argumentValues[2]);
					} else // Non recognized intrinsic
						throw new ArgumentException ($"Reflection call '{calledMethod.GetDisplayName ()}' inside '{GetContainingSymbolDisplayName ()}' is an unexpected intrinsic.");

					DynamicallyAccessedMemberTypes requiredMemberTypes;
					if (BindingFlagsAreUnsupported (bindingFlags)) {
						requiredMemberTypes = DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors |
							DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents |
							DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields |
							DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods |
							DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties |
							DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes;
					} else {
						requiredMemberTypes = GetDynamicallyAccessedMemberTypesFromBindingFlagsForMembers (bindingFlags);
					}

					var targetValue = _annotations.GetMethodThisParameterValue (calledMethod, requiredMemberTypes);

					// Go over all types we've seen
					foreach (var value in instanceValue) {
						// Mark based on bitfield requirements
						_requireDynamicallyAccessedMembersAction.Invoke (value, targetValue);
					}
				}
				break;

			//
			// GetMethod (string)
			// GetMethod (string, BindingFlags)
			// GetMethod (string, Type[])
			// GetMethod (string, Type[], ParameterModifier[])
			// GetMethod (string, BindingFlags, Type[])
			// GetMethod (string, BindingFlags, Binder, Type[], ParameterModifier[])
			// GetMethod (string, BindingFlags, Binder, CallingConventions, Type[], ParameterModifier[])
			// GetMethod (string, int, Type[])
			// GetMethod (string, int, Type[], ParameterModifier[]?)
			// GetMethod (string, int, BindingFlags, Binder?, Type[], ParameterModifier[]?)
			// GetMethod (string, int, BindingFlags, Binder?, CallingConventions, Type[], ParameterModifier[]?)
			//
			case IntrinsicId.Type_GetMethod: {
					if (instanceValue.IsEmpty () || argumentValues[0].IsEmpty ()) {
						returnValue = MultiValueLattice.Top;
						break;
					}

					BindingFlags? bindingFlags;
					if (calledMethod.HasParameterOfType (1, "System.Reflection.BindingFlags"))
						bindingFlags = GetBindingFlagsFromValue (argumentValues[1]);
					else if (calledMethod.HasParameterOfType (2, "System.Reflection.BindingFlags"))
						bindingFlags = GetBindingFlagsFromValue (argumentValues[2]);
					else
						// Assume a default value for BindingFlags for methods that don't use BindingFlags as a parameter
						bindingFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public;

					var targetValue = _annotations.GetMethodThisParameterValue (calledMethod, GetDynamicallyAccessedMemberTypesFromBindingFlagsForMethods (bindingFlags));
					foreach (var value in instanceValue) {
						if (value is SystemTypeValue systemTypeValue) {
							foreach (var stringParam in argumentValues[0]) {
								if (stringParam is KnownStringValue stringValue && !BindingFlagsAreUnsupported (bindingFlags)) {
									AddReturnValue (MultiValueLattice.Top); ; // Initialize return value (so that it's not autofilled if there are no matching methods)
									foreach (var methodValue in ProcessGetMethodByName (systemTypeValue.RepresentedType, stringValue.Contents, bindingFlags))
										AddReturnValue (methodValue);
								} else if (stringParam is NullValue) {
									// GetMethod(null) throws - so track empty value set as its result
									AddReturnValue (MultiValueLattice.Top);
								} else {
									// Otherwise fall back to the bitfield requirements
									_requireDynamicallyAccessedMembersAction.Invoke (value, targetValue);
									AddReturnValue (annotatedMethodReturnValue);
								}
							}
						} else if (value is NullValue) {
							// null.GetMethod(...) throws - so track empty value set as its result
							AddReturnValue (MultiValueLattice.Top);
						} else {
							// Otherwise fall back to the bitfield requirements
							_requireDynamicallyAccessedMembersAction.Invoke (value, targetValue);
							AddReturnValue (annotatedMethodReturnValue);
						}
					}
				}
				break;

			//
			// GetNestedType (string)
			// GetNestedType (string, BindingFlags)
			//
			case IntrinsicId.Type_GetNestedType: {
					if (instanceValue.IsEmpty () || argumentValues[0].IsEmpty ()) {
						returnValue = MultiValueLattice.Top;
						break;
					}

					BindingFlags? bindingFlags;
					if (calledMethod.HasParameterOfType (1, "System.Reflection.BindingFlags"))
						bindingFlags = GetBindingFlagsFromValue (argumentValues[1]);
					else
						// Assume a default value for BindingFlags for methods that don't use BindingFlags as a parameter
						bindingFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public;

					var targetValue = _annotations.GetMethodThisParameterValue (calledMethod, GetDynamicallyAccessedMemberTypesFromBindingFlagsForNestedTypes (bindingFlags));
					foreach (var value in instanceValue) {
						if (value is SystemTypeValue systemTypeValue) {
							foreach (var stringParam in argumentValues[0]) {
								if (stringParam is KnownStringValue stringValue && !BindingFlagsAreUnsupported (bindingFlags)) {
									AddReturnValue (MultiValueLattice.Top);
									foreach (var nestedTypeValue in GetNestedTypesOnType (systemTypeValue.RepresentedType, stringValue.Contents, bindingFlags)) {
										MarkType (nestedTypeValue.RepresentedType);
										AddReturnValue (nestedTypeValue);
									}
								} else if (stringParam is NullValue) {
									AddReturnValue (MultiValueLattice.Top);
								} else {
									// Otherwise fall back to the bitfield requirements
									_requireDynamicallyAccessedMembersAction.Invoke (value, targetValue);

									// We only applied the annotation based on binding flags, so we will keep the necessary types
									// but we will not keep anything on them. So the return value has no known annotations on it
									AddReturnValue (_annotations.GetMethodReturnValue (calledMethod, DynamicallyAccessedMemberTypes.None));
								}
							}
						} else if (value is NullValue) {
							// null.GetNestedType(..) throws - so track empty value set
							AddReturnValue (MultiValueLattice.Top);
						} else {
							// Otherwise fall back to the bitfield requirements
							_requireDynamicallyAccessedMembersAction.Invoke (value, targetValue);

							// If the input is an annotated value which has All - we can propagate that to the return value
							// since All applies recursively to all nested type (see MarkStep.MarkEntireType).
							// Otherwise we only mark the nested type itself, nothing on it, so the return value has no annotation on it.
							if (value is ValueWithDynamicallyAccessedMembers { DynamicallyAccessedMemberTypes: DynamicallyAccessedMemberTypes.All })
								AddReturnValue (_annotations.GetMethodReturnValue (calledMethod, DynamicallyAccessedMemberTypes.All));
							else
								AddReturnValue (_annotations.GetMethodReturnValue (calledMethod, DynamicallyAccessedMemberTypes.None));
						}
					}
				}
				break;

			//
			// System.Reflection.RuntimeReflectionExtensions
			//
			// static GetRuntimeEvent (this Type type, string name)
			// static GetRuntimeField (this Type type, string name)
			// static GetRuntimeMethod (this Type type, string name, Type[] parameters)
			// static GetRuntimeProperty (this Type type, string name)
			//
			case var getRuntimeMember when getRuntimeMember == IntrinsicId.RuntimeReflectionExtensions_GetRuntimeEvent
				|| getRuntimeMember == IntrinsicId.RuntimeReflectionExtensions_GetRuntimeField
				|| getRuntimeMember == IntrinsicId.RuntimeReflectionExtensions_GetRuntimeMethod
				|| getRuntimeMember == IntrinsicId.RuntimeReflectionExtensions_GetRuntimeProperty: {

					if (argumentValues[0].IsEmpty () || argumentValues[1].IsEmpty ()) {
						returnValue = MultiValueLattice.Top;
						break;
					}

					BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public;
					DynamicallyAccessedMemberTypes requiredMemberTypes = getRuntimeMember switch {
						IntrinsicId.RuntimeReflectionExtensions_GetRuntimeEvent => DynamicallyAccessedMemberTypes.PublicEvents,
						IntrinsicId.RuntimeReflectionExtensions_GetRuntimeField => DynamicallyAccessedMemberTypes.PublicFields,
						IntrinsicId.RuntimeReflectionExtensions_GetRuntimeMethod => DynamicallyAccessedMemberTypes.PublicMethods,
						IntrinsicId.RuntimeReflectionExtensions_GetRuntimeProperty => DynamicallyAccessedMemberTypes.PublicProperties,
						_ => throw new ArgumentException ($"Reflection call '{calledMethod.GetDisplayName ()}' inside '{GetContainingSymbolDisplayName ()}' is of unexpected member type."),
					};

					var targetValue = _annotations.GetMethodParameterValue (calledMethod, 0, requiredMemberTypes);

					foreach (var value in argumentValues[0]) {
						if (value is SystemTypeValue systemTypeValue) {
							foreach (var stringParam in argumentValues[1]) {
								if (stringParam is KnownStringValue stringValue) {
									switch (getRuntimeMember) {
									case IntrinsicId.RuntimeReflectionExtensions_GetRuntimeEvent:
										MarkEventsOnTypeHierarchy (systemTypeValue.RepresentedType, stringValue.Contents, bindingFlags);
										break;
									case IntrinsicId.RuntimeReflectionExtensions_GetRuntimeField:
										MarkFieldsOnTypeHierarchy (systemTypeValue.RepresentedType, stringValue.Contents, bindingFlags);
										break;
									case IntrinsicId.RuntimeReflectionExtensions_GetRuntimeMethod:
										AddReturnValue (MultiValueLattice.Top); // Initialize return value (so that it's not autofilled if there are no matching methods)
										foreach (var methodValue in ProcessGetMethodByName (systemTypeValue.RepresentedType, stringValue.Contents, bindingFlags))
											AddReturnValue (methodValue);
										break;
									case IntrinsicId.RuntimeReflectionExtensions_GetRuntimeProperty:
										MarkPropertiesOnTypeHierarchy (systemTypeValue.RepresentedType, stringValue.Contents, bindingFlags);
										break;
									default:
										throw new ArgumentException ($"Error processing reflection call '{calledMethod.GetDisplayName ()}' inside {GetContainingSymbolDisplayName ()}. Unexpected member kind.");
									}
								} else if (stringParam is NullValue) {
									// GetRuntimeMethod(type, null) throws - so track empty value set as its result
									AddReturnValue (MultiValueLattice.Top);
								} else {
									_requireDynamicallyAccessedMembersAction.Invoke (value, targetValue);
									AddReturnValue (annotatedMethodReturnValue);
								}
							}
						} else if (value is NullValue) {
							// GetRuntimeMethod(null, ...) throws - so track empty value set as its result
							AddReturnValue (MultiValueLattice.Top);
						} else {
							_requireDynamicallyAccessedMembersAction.Invoke (value, targetValue);
							AddReturnValue (annotatedMethodReturnValue);
						}
					}
				}
				break;

			//
			// System.Linq.Expressions.Expression
			//
			// static New (Type)
			//
			case IntrinsicId.Expression_New: {
					var targetValue = _annotations.GetMethodParameterValue (calledMethod, 0, DynamicallyAccessedMemberTypes.PublicParameterlessConstructor);
					foreach (var value in argumentValues[0]) {
						if (value is SystemTypeValue systemTypeValue) {
							MarkConstructorsOnType (systemTypeValue.RepresentedType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, parameterCount: null);
						} else {
							_requireDynamicallyAccessedMembersAction.Invoke (value, targetValue);
						}
					}
				}
				break;

			//
			// System.Linq.Expressions.Expression
			//
			// static Property (Expression, MethodInfo)
			//
			case IntrinsicId.Expression_Property when calledMethod.HasParameterOfType (1, "System.Reflection.MethodInfo"): {
					if (argumentValues[1].IsEmpty ()) {
						returnValue = MultiValueLattice.Top;
						break;
					}

					foreach (var value in argumentValues[1]) {
						if (value is SystemReflectionMethodBaseValue methodBaseValue) {
							// We have one of the accessors for the property. The Expression.Property will in this case search
							// for the matching PropertyInfo and store that. So to be perfectly correct we need to mark the
							// respective PropertyInfo as "accessed via reflection".
							if (MarkAssociatedProperty (methodBaseValue.RepresentedMethod))
								continue;
						} else if (value == NullValue.Instance) {
							continue;
						}

						// In all other cases we may not even know which type this is about, so there's nothing we can do
						// report it as a warning.
						_diagnosticContext.AddDiagnostic (DiagnosticId.PropertyAccessorParameterInLinqExpressionsCannotBeStaticallyDetermined,
							_annotations.GetMethodParameterValue (calledMethod, 1, DynamicallyAccessedMemberTypes.None).GetDiagnosticArgumentsForAnnotationMismatch ().ToArray ());
					}
				}
				break;

			//
			// System.Linq.Expressions.Expression
			//
			// static Field (Expression, Type, String)
			// static Property (Expression, Type, String)
			//
			case var fieldOrPropertyInstrinsic when fieldOrPropertyInstrinsic == IntrinsicId.Expression_Field || fieldOrPropertyInstrinsic == IntrinsicId.Expression_Property: {
					DynamicallyAccessedMemberTypes memberTypes = fieldOrPropertyInstrinsic == IntrinsicId.Expression_Property
						? DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties
						: DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields;

					if (argumentValues[1].IsEmpty () || argumentValues[2].IsEmpty ()) {
						returnValue = MultiValueLattice.Top;
						break;
					}

					var targetValue = _annotations.GetMethodParameterValue (calledMethod, 1, memberTypes);
					foreach (var value in argumentValues[1]) {
						if (value is SystemTypeValue systemTypeValue) {
							foreach (var stringParam in argumentValues[2]) {
								if (stringParam is KnownStringValue stringValue) {
									BindingFlags bindingFlags = argumentValues[0].AsSingleValue () is NullValue ? BindingFlags.Static : BindingFlags.Default;
									if (fieldOrPropertyInstrinsic == IntrinsicId.Expression_Property) {
										MarkPropertiesOnTypeHierarchy (systemTypeValue.RepresentedType, stringValue.Contents, bindingFlags);
									} else {
										MarkFieldsOnTypeHierarchy (systemTypeValue.RepresentedType, stringValue.Contents, bindingFlags);
									}
								} else if (stringParam is NullValue) {
									// Null name will always throw, so there's nothing to do
								} else {
									_requireDynamicallyAccessedMembersAction.Invoke (value, targetValue);
								}
							}
						} else {
							_requireDynamicallyAccessedMembersAction.Invoke (value, targetValue);
						}
					}
				}
				break;

			//
			// System.Linq.Expressions.Expression
			//
			// static Call (Type, String, Type[], Expression[])
			//
			case IntrinsicId.Expression_Call: {
					BindingFlags bindingFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;

					var targetValue = _annotations.GetMethodParameterValue (
						calledMethod,
						0,
						GetDynamicallyAccessedMemberTypesFromBindingFlagsForMethods (bindingFlags));

					// This is true even if we "don't know" - so it's only false if we're sure that there are no type arguments
					bool hasTypeArguments = (argumentValues[2].AsSingleValue () as ArrayValue)?.Size.AsConstInt () != 0;
					foreach (var value in argumentValues[0]) {
						if (value is SystemTypeValue systemTypeValue) {
							foreach (var stringParam in argumentValues[1]) {
								if (stringParam is KnownStringValue stringValue) {
									foreach (var method in GetMethodsOnTypeHierarchy (systemTypeValue.RepresentedType, stringValue.Contents, bindingFlags)) {
										ValidateGenericMethodInstantiation (method.RepresentedMethod, argumentValues[2], calledMethod);
										MarkMethod (method.RepresentedMethod);
									}
								} else {
									if (hasTypeArguments) {
										// We don't know what method the `MakeGenericMethod` was called on, so we have to assume
										// that the method may have requirements which we can't fullfil -> warn.
										_diagnosticContext.AddDiagnostic (DiagnosticId.MakeGenericMethod, calledMethod.GetDisplayName ());
									}

									_requireDynamicallyAccessedMembersAction.Invoke (value, targetValue);
								}
							}
						} else {
							if (hasTypeArguments) {
								// We don't know what method the `MakeGenericMethod` was called on, so we have to assume
								// that the method may have requirements which we can't fullfil -> warn.
								_diagnosticContext.AddDiagnostic (DiagnosticId.MakeGenericMethod, calledMethod.GetDisplayName ());
							}

							_requireDynamicallyAccessedMembersAction.Invoke (value, targetValue);
						}
					}
				}
				break;

			//
			// Nullable.GetUnderlyingType(Type)
			//
			case IntrinsicId.Nullable_GetUnderlyingType:
				if (argumentValues[0].IsEmpty ()) {
					returnValue = MultiValueLattice.Top;
					break;
				}

				foreach (var singlevalue in argumentValues[0].AsEnumerable ()) {
					AddReturnValue (singlevalue switch {
						SystemTypeValue systemType =>
							systemType.RepresentedType.IsTypeOf ("System", "Nullable`1")
								// This will happen if there's typeof(Nullable<>).MakeGenericType(unknown) - we know the return value is Nullable<>
								// but we don't know of what. So we represent it as known type, but not as known nullable type.
								// Has to be special cased here, since we need to return "unknown" type.
								? annotatedMethodReturnValue
								: MultiValueLattice.Top, // This returns null at runtime, so return empty value
						NullableSystemTypeValue nullableSystemType => nullableSystemType.UnderlyingTypeValue,
						NullableValueWithDynamicallyAccessedMembers nullableDamValue => nullableDamValue.UnderlyingTypeValue,
						ValueWithDynamicallyAccessedMembers damValue => damValue,
						_ => annotatedMethodReturnValue
					});
				}
				break;

			//
			// System.Type
			//
			// GetType (string)
			// GetType (string, Boolean)
			// GetType (string, Boolean, Boolean)
			// GetType (string, Func<AssemblyName, Assembly>, Func<Assembly, String, Boolean, Type>)
			// GetType (string, Func<AssemblyName, Assembly>, Func<Assembly, String, Boolean, Type>, Boolean)
			// GetType (string, Func<AssemblyName, Assembly>, Func<Assembly, String, Boolean, Type>, Boolean, Boolean)
			//
			case IntrinsicId.Type_GetType: {
					if (argumentValues[0].IsEmpty ()) {
						returnValue = MultiValueLattice.Top;
						break;
					}

					if ((calledMethod.HasParametersCount (3) && calledMethod.HasParameterOfType (2, "System.Boolean") && argumentValues[2].AsConstInt () != 0) ||
						(calledMethod.HasParametersCount (5) && argumentValues[4].AsConstInt () != 0)) {
						_diagnosticContext.AddDiagnostic (DiagnosticId.CaseInsensitiveTypeGetTypeCallIsNotSupported, calledMethod.GetDisplayName ());
						returnValue = MultiValueLattice.Top; // This effectively disables analysis of anything which uses the return value
						break;
					}

					foreach (var typeNameValue in argumentValues[0]) {
						if (typeNameValue is KnownStringValue knownStringValue) {
							if (!_requireDynamicallyAccessedMembersAction.TryResolveTypeNameAndMark (knownStringValue.Contents, false, out TypeProxy foundType)) {
								// Intentionally ignore - it's not wrong for code to call Type.GetType on non-existing name, the code might expect null/exception back.
								AddReturnValue (MultiValueLattice.Top);
							} else {
								AddReturnValue (new SystemTypeValue (foundType));
							}
						} else if (typeNameValue == NullValue.Instance) {
							// Nothing to do - this throws at runtime
							AddReturnValue (MultiValueLattice.Top);
						} else if (typeNameValue is ValueWithDynamicallyAccessedMembers valueWithDynamicallyAccessedMembers && valueWithDynamicallyAccessedMembers.DynamicallyAccessedMemberTypes != 0) {
							// Propagate the annotation from the type name to the return value. Annotation on a string value will be fulfilled whenever a value is assigned to the string with annotation.
							// So while we don't know which type it is, we can guarantee that it will fulfill the annotation.
							AddReturnValue (_annotations.GetMethodReturnValue (calledMethod, valueWithDynamicallyAccessedMembers.DynamicallyAccessedMemberTypes));
						} else {
							_diagnosticContext.AddDiagnostic (DiagnosticId.UnrecognizedTypeNameInTypeGetType, calledMethod.GetDisplayName ());
							AddReturnValue (MultiValueLattice.Top);
						}
					}

				}
				break;

			//
			// System.Type
			//
			// Type MakeGenericType (params Type[] typeArguments)
			//
			case IntrinsicId.Type_MakeGenericType:
				if (instanceValue.IsEmpty () || argumentValues[0].IsEmpty ()) {
					returnValue = MultiValueLattice.Top;
					break;
				}

				foreach (var value in instanceValue) {
					if (value is SystemTypeValue typeValue) {
						// Special case Nullable<T>
						// Nullables without a type argument are considered SystemTypeValues
						if (typeValue.RepresentedType.IsTypeOf ("System", "Nullable`1")) {
							// Note that we're not performing any generic parameter validation
							// Special case: Nullable<T> where T : struct
							//  The struct constraint in C# implies new() constraint, but Nullable doesn't make a use of that part.
							//  There are several places even in the framework where typeof(Nullable<>).MakeGenericType would warn
							//  without any good reason to do so.

							foreach (var argumentValue in argumentValues[0]) {
								if ((argumentValue as ArrayValue)?.TryGetValueByIndex (0, out var underlyingMultiValue) == true) {
									foreach (var underlyingValue in underlyingMultiValue) {
										switch (underlyingValue) {
										// Don't warn on these types - it will throw instead
										case NullableValueWithDynamicallyAccessedMembers:
										case NullableSystemTypeValue:
										case SystemTypeValue maybeArrayValue when maybeArrayValue.RepresentedType.IsTypeOf ("System", "Array"):
											AddReturnValue (MultiValueLattice.Top);
											break;
										case SystemTypeValue systemTypeValue:
											AddReturnValue (new NullableSystemTypeValue (typeValue.RepresentedType, new SystemTypeValue (systemTypeValue.RepresentedType)));
											break;
										// Generic Parameters and method parameters with annotations
										case ValueWithDynamicallyAccessedMembers damValue:
											AddReturnValue (new NullableValueWithDynamicallyAccessedMembers (typeValue.RepresentedType, damValue));
											break;
										// Everything else assume it has no annotations
										default:
											// This returns just Nullable<> SystemTypeValue - so some things will work, but GetUnderlyingType won't propagate anything
											// It's special cased to do that.
											AddReturnValue (value);
											break;
										}
									}
								} else {
									// This returns just Nullable<> SystemTypeValue - so some things will work, but GetUnderlyingType won't propagate anything
									// It's special cased to do that.
									AddReturnValue (value);
								}
							}
							// We want to skip adding the `value` to the return Value because we have already added Nullable<value>
							continue;
						} else {
							// Any other type - perform generic parameter validation
							var genericParameterValues = GetGenericParameterValues (typeValue.RepresentedType.GetGenericParameters ());
							if (!AnalyzeGenericInstantiationTypeArray (argumentValues[0], calledMethod, genericParameterValues)) {
								_diagnosticContext.AddDiagnostic (DiagnosticId.MakeGenericType, calledMethod.GetDisplayName ());
							}
						}
					} else if (value == NullValue.Instance) {
						// At runtime this would throw - so it has no effect on analysis
						AddReturnValue (MultiValueLattice.Top);
					} else {
						// We have no way to "include more" to fix this if we don't know, so we have to warn
						_diagnosticContext.AddDiagnostic (DiagnosticId.MakeGenericType, calledMethod.GetDisplayName ());
					}

					// We don't want to lose track of the type
					// in case this is e.g. Activator.CreateInstance(typeof(Foo<>).MakeGenericType(...));
					// Note this is not called in the Nullable case - we skipt this via the 'continue'.
					AddReturnValue (value);
				}
				break;

			//
			// Type.BaseType
			//
			case IntrinsicId.Type_get_BaseType: {
					if (instanceValue.IsEmpty ()) {
						returnValue = MultiValueLattice.Top;
						break;
					}

					foreach (var value in instanceValue) {
						if (value is ValueWithDynamicallyAccessedMembers valueWithDynamicallyAccessedMembers) {
							DynamicallyAccessedMemberTypes propagatedMemberTypes = DynamicallyAccessedMemberTypes.None;
							if (valueWithDynamicallyAccessedMembers.DynamicallyAccessedMemberTypes == DynamicallyAccessedMemberTypes.All)
								propagatedMemberTypes = DynamicallyAccessedMemberTypes.All;
							else {
								// PublicConstructors are not propagated to base type

								if (valueWithDynamicallyAccessedMembers.DynamicallyAccessedMemberTypes.HasFlag (DynamicallyAccessedMemberTypes.PublicEvents))
									propagatedMemberTypes |= DynamicallyAccessedMemberTypes.PublicEvents;

								if (valueWithDynamicallyAccessedMembers.DynamicallyAccessedMemberTypes.HasFlag (DynamicallyAccessedMemberTypes.PublicFields))
									propagatedMemberTypes |= DynamicallyAccessedMemberTypes.PublicFields;

								if (valueWithDynamicallyAccessedMembers.DynamicallyAccessedMemberTypes.HasFlag (DynamicallyAccessedMemberTypes.PublicMethods))
									propagatedMemberTypes |= DynamicallyAccessedMemberTypes.PublicMethods;

								// PublicNestedTypes are not propagated to base type

								// PublicParameterlessConstructor is not propagated to base type

								if (valueWithDynamicallyAccessedMembers.DynamicallyAccessedMemberTypes.HasFlag (DynamicallyAccessedMemberTypes.PublicProperties))
									propagatedMemberTypes |= DynamicallyAccessedMemberTypes.PublicProperties;

								if (valueWithDynamicallyAccessedMembers.DynamicallyAccessedMemberTypes.HasFlag (DynamicallyAccessedMemberTypes.Interfaces))
									propagatedMemberTypes |= DynamicallyAccessedMemberTypes.Interfaces;
							}

							AddReturnValue (_annotations.GetMethodReturnValue (calledMethod, propagatedMemberTypes));
						} else if (value is SystemTypeValue systemTypeValue) {
							if (TryGetBaseType (systemTypeValue.RepresentedType, out var baseType))
								AddReturnValue (new SystemTypeValue (baseType.Value));
							else
								AddReturnValue (annotatedMethodReturnValue);
						} else if (value == NullValue.Instance) {
							// Ignore nulls - null.BaseType will fail at runtime, but it has no effect on static analysis
							AddReturnValue (MultiValueLattice.Top);
							continue;
						} else {
							// Unknown input - propagate a return value without any annotation - we know it's a Type but we know nothing about it
							AddReturnValue (annotatedMethodReturnValue);
						}
					}
				}
				break;

			//
			// GetConstructor (Type[])
			// GetConstructor (BindingFlags, Type[])
			// GetConstructor (BindingFlags, Binder, Type[], ParameterModifier [])
			// GetConstructor (BindingFlags, Binder, CallingConventions, Type[], ParameterModifier [])
			//
			case IntrinsicId.Type_GetConstructor: {
					if (instanceValue.IsEmpty ()) {
						returnValue = MultiValueLattice.Top;
						break;
					}

					BindingFlags? bindingFlags;
					if (calledMethod.HasParameterOfType (0, "System.Reflection.BindingFlags"))
						bindingFlags = GetBindingFlagsFromValue (argumentValues[0]);
					else
						// Assume a default value for BindingFlags for methods that don't use BindingFlags as a parameter
						bindingFlags = BindingFlags.Public | BindingFlags.Instance;

					int? ctorParameterCount = calledMethod.GetParametersCount () switch {
						1 => (argumentValues[0].AsSingleValue () as ArrayValue)?.Size.AsConstInt (),
						2 => (argumentValues[1].AsSingleValue () as ArrayValue)?.Size.AsConstInt (),
						4 => (argumentValues[2].AsSingleValue () as ArrayValue)?.Size.AsConstInt (),
						5 => (argumentValues[3].AsSingleValue () as ArrayValue)?.Size.AsConstInt (),
						_ => null,
					};

					// Go over all types we've seen
					foreach (var value in instanceValue) {
						if (value is SystemTypeValue systemTypeValue && !BindingFlagsAreUnsupported (bindingFlags)) {
							if (HasBindingFlag (bindingFlags, BindingFlags.Public) && !HasBindingFlag (bindingFlags, BindingFlags.NonPublic)
								&& ctorParameterCount == 0) {
								MarkPublicParameterlessConstructorOnType (systemTypeValue.RepresentedType);
							} else {
								MarkConstructorsOnType (systemTypeValue.RepresentedType, bindingFlags, parameterCount: null);
							}
						} else {
							// Otherwise fall back to the bitfield requirements
							var requiredMemberTypes = GetDynamicallyAccessedMemberTypesFromBindingFlagsForConstructors (bindingFlags);
							// We can scope down the public constructors requirement if we know the number of parameters is 0
							if (requiredMemberTypes == DynamicallyAccessedMemberTypes.PublicConstructors && ctorParameterCount == 0)
								requiredMemberTypes = DynamicallyAccessedMemberTypes.PublicParameterlessConstructor;

							var targetValue = _annotations.GetMethodThisParameterValue (calledMethod, requiredMemberTypes);
							_requireDynamicallyAccessedMembersAction.Invoke (value, targetValue);
						}
					}
				}
				break;

			//
			// System.Reflection.MethodInfo
			//
			// MakeGenericMethod (Type[] typeArguments)
			//
			case IntrinsicId.MethodInfo_MakeGenericMethod: {
					if (instanceValue.IsEmpty ()) {
						returnValue = MultiValueLattice.Top;
						break;
					}

					foreach (var methodValue in instanceValue) {
						if (methodValue is SystemReflectionMethodBaseValue methodBaseValue) {
							ValidateGenericMethodInstantiation (methodBaseValue.RepresentedMethod, argumentValues[0], calledMethod);
						} else if (methodValue == NullValue.Instance) {
							// Nothing to do
						} else {
							// We don't know what method the `MakeGenericMethod` was called on, so we have to assume
							// that the method may have requirements which we can't fullfil -> warn.
							_diagnosticContext.AddDiagnostic (DiagnosticId.MakeGenericMethod, calledMethod.GetDisplayName ());
						}
					}

					// MakeGenericMethod doesn't change the identity of the MethodBase we're tracking so propagate to the return value
					AddReturnValue (instanceValue);
				}
				break;

			//
			// System.Activator
			//
			// static CreateInstance (System.Type type)
			// static CreateInstance (System.Type type, bool nonPublic)
			// static CreateInstance (System.Type type, params object?[]? args)
			// static CreateInstance (System.Type type, object?[]? args, object?[]? activationAttributes)
			// static CreateInstance (System.Type type, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder? binder, object?[]? args, System.Globalization.CultureInfo? culture)
			// static CreateInstance (System.Type type, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder? binder, object?[]? args, System.Globalization.CultureInfo? culture, object?[]? activationAttributes) { throw null; }
			//
			case IntrinsicId.Activator_CreateInstance_Type: {
					int? ctorParameterCount = null;
					BindingFlags bindingFlags = BindingFlags.Instance;
					if (calledMethod.GetParametersCount () > 1) {
						if (calledMethod.HasParameterOfType (1, "System.Boolean")) {
							// The overload that takes a "nonPublic" bool
							bool nonPublic = argumentValues[1].AsConstInt () != 0;

							if (nonPublic)
								bindingFlags |= BindingFlags.NonPublic | BindingFlags.Public;
							else
								bindingFlags |= BindingFlags.Public;
							ctorParameterCount = 0;
						} else {
							// Overload that has the parameters as the second or fourth argument
							int argsParam = calledMethod.HasParametersCount (2) || calledMethod.HasParametersCount (3) ? 1 : 3;

							if (argumentValues.Count > argsParam) {
								if (argumentValues[argsParam].AsSingleValue () is ArrayValue arrayValue &&
									arrayValue.Size.AsConstInt () != null)
									ctorParameterCount = arrayValue.Size.AsConstInt ();
								else if (argumentValues[argsParam].AsSingleValue () is NullValue)
									ctorParameterCount = 0;
							}

							if (calledMethod.GetParametersCount () > 3) {
								if (argumentValues[1].AsConstInt () is int constInt)
									bindingFlags |= (BindingFlags) constInt;
								else
									bindingFlags |= BindingFlags.NonPublic | BindingFlags.Public;
							} else {
								bindingFlags |= BindingFlags.Public;
							}
						}
					} else {
						// The overload with a single System.Type argument
						ctorParameterCount = 0;
						bindingFlags |= BindingFlags.Public;
					}

					// Go over all types we've seen
					foreach (var value in argumentValues[0]) {
						if (value is SystemTypeValue systemTypeValue) {
							// Special case known type values as we can do better by applying exact binding flags and parameter count.
							MarkConstructorsOnType (systemTypeValue.RepresentedType, bindingFlags, ctorParameterCount);
						} else {
							// Otherwise fall back to the bitfield requirements
							var requiredMemberTypes = GetDynamicallyAccessedMemberTypesFromBindingFlagsForConstructors (bindingFlags);

							// Special case the public parameterless constructor if we know that there are 0 args passed in
							if (ctorParameterCount == 0 && requiredMemberTypes.HasFlag (DynamicallyAccessedMemberTypes.PublicConstructors)) {
								requiredMemberTypes &= ~DynamicallyAccessedMemberTypes.PublicConstructors;
								requiredMemberTypes |= DynamicallyAccessedMemberTypes.PublicParameterlessConstructor;
							}

							var targetValue = _annotations.GetMethodParameterValue (calledMethod, 0, requiredMemberTypes);

							_requireDynamicallyAccessedMembersAction.Invoke (value, targetValue);
						}
					}
				}
				break;

			//
			// System.Activator
			//
			// static CreateInstance (string assemblyName, string typeName)
			// static CreateInstance (string assemblyName, string typeName, bool ignoreCase, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder? binder, object?[]? args, System.Globalization.CultureInfo? culture, object?[]? activationAttributes)
			// static CreateInstance (string assemblyName, string typeName, object?[]? activationAttributes)
			//
			case IntrinsicId.Activator_CreateInstance_AssemblyName_TypeName:
				ProcessCreateInstanceByName (calledMethod, argumentValues);
				break;

			//
			// System.Activator
			//
			// static CreateInstanceFrom (string assemblyFile, string typeName)
			// static CreateInstanceFrom (string assemblyFile, string typeName, bool ignoreCase, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder? binder, object? []? args, System.Globalization.CultureInfo? culture, object? []? activationAttributes)
			// static CreateInstanceFrom (string assemblyFile, string typeName, object? []? activationAttributes)
			//
			case IntrinsicId.Activator_CreateInstanceFrom:
				ProcessCreateInstanceByName (calledMethod, argumentValues);
				break;

			//
			// System.AppDomain
			//
			// CreateInstance (string assemblyName, string typeName)
			// CreateInstance (string assemblyName, string typeName, bool ignoreCase, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder? binder, object? []? args, System.Globalization.CultureInfo? culture, object? []? activationAttributes)
			// CreateInstance (string assemblyName, string typeName, object? []? activationAttributes)
			//
			// CreateInstanceAndUnwrap (string assemblyName, string typeName)
			// CreateInstanceAndUnwrap (string assemblyName, string typeName, bool ignoreCase, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder? binder, object? []? args, System.Globalization.CultureInfo? culture, object? []? activationAttributes)
			// CreateInstanceAndUnwrap (string assemblyName, string typeName, object? []? activationAttributes)
			//
			// CreateInstanceFrom (string assemblyFile, string typeName)
			// CreateInstanceFrom (string assemblyFile, string typeName, bool ignoreCase, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder? binder, object? []? args, System.Globalization.CultureInfo? culture, object? []? activationAttributes)
			// CreateInstanceFrom (string assemblyFile, string typeName, object? []? activationAttributes)
			//
			// CreateInstanceFromAndUnwrap (string assemblyFile, string typeName)
			// CreateInstanceFromAndUnwrap (string assemblyFile, string typeName, bool ignoreCase, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder? binder, object? []? args, System.Globalization.CultureInfo? culture, object? []? activationAttributes)
			// CreateInstanceFromAndUnwrap (string assemblyFile, string typeName, object? []? activationAttributes)
			//
			case var appDomainCreateInstance when appDomainCreateInstance == IntrinsicId.AppDomain_CreateInstance
					|| appDomainCreateInstance == IntrinsicId.AppDomain_CreateInstanceAndUnwrap
					|| appDomainCreateInstance == IntrinsicId.AppDomain_CreateInstanceFrom
					|| appDomainCreateInstance == IntrinsicId.AppDomain_CreateInstanceFromAndUnwrap:
				ProcessCreateInstanceByName (calledMethod, argumentValues);
				break;

			//
			// System.Reflection.Assembly
			//
			// CreateInstance (string typeName)
			// CreateInstance (string typeName, bool ignoreCase)
			// CreateInstance (string typeName, bool ignoreCase, BindingFlags bindingAttr, Binder? binder, object []? args, CultureInfo? culture, object []? activationAttributes)
			//
			case IntrinsicId.Assembly_CreateInstance:
				// For now always fail since we don't track assemblies (dotnet/linker/issues/1947)
				_diagnosticContext.AddDiagnostic (DiagnosticId.ParametersOfAssemblyCreateInstanceCannotBeAnalyzed, calledMethod.GetDisplayName ());
				break;

			case IntrinsicId.None:
				// Verify the argument values match the annotations on the parameter definition
				if (requiresDataFlowAnalysis) {
					if (!calledMethod.IsStatic ()) {
						_requireDynamicallyAccessedMembersAction.Invoke (instanceValue, _annotations.GetMethodThisParameterValue (calledMethod));
					}
					for (int argumentIndex = 0; argumentIndex < argumentValues.Count; argumentIndex++) {
						if (calledMethod.ParameterReferenceKind (argumentIndex) == ReferenceKind.Out)
							continue;
						_requireDynamicallyAccessedMembersAction.Invoke (argumentValues[argumentIndex], _annotations.GetMethodParameterValue (calledMethod, argumentIndex));
					}
				}
				break;

			// Disable warnings for all unimplemented intrinsics. Some intrinsic methods have annotations, but analyzing them
			// would produce unnecessary warnings even for cases that are intrinsically handled. So we disable handling these calls
			// until a proper intrinsic handling is made
			// NOTE: Currently this is done "for the analyzer" and it relies on linker/NativeAOT to not call HandleCallAction
			// for intrinsics which linker/NativeAOT need special handling for or those which are not implemented here and only there.
			// Ideally we would run everything through HandleCallAction and it would return "false" for intrinsics it doesn't handle
			// like it already does for Activator.CreateInstance<T> for example.
			default:
				methodReturnValue = MultiValueLattice.Top;
				return true;
			}

			// For now, if the intrinsic doesn't set a return value, fall back on the annotations.
			// Note that this will be DynamicallyAccessedMembers.None for the intrinsics which don't return types.
			returnValue ??= calledMethod.ReturnsVoid () ? MultiValueLattice.Top : annotatedMethodReturnValue;

			if (MethodIsTypeConstructor (calledMethod))
				returnValue = UnknownValue.Instance;

			// Validate that the return value has the correct annotations as per the method return value annotations
			if (annotatedMethodReturnValue.DynamicallyAccessedMemberTypes != DynamicallyAccessedMemberTypes.None) {
				foreach (var uniqueValue in returnValue.Value) {
					if (uniqueValue is ValueWithDynamicallyAccessedMembers methodReturnValueWithMemberTypes) {
						if (!methodReturnValueWithMemberTypes.DynamicallyAccessedMemberTypes.HasFlag (annotatedMethodReturnValue.DynamicallyAccessedMemberTypes))
							throw new InvalidOperationException ($"Internal linker error: in {GetContainingSymbolDisplayName ()} processing call to {calledMethod.GetDisplayName ()} returned value which is not correctly annotated with the expected dynamic member access kinds.");
					} else if (uniqueValue is SystemTypeValue) {
						// SystemTypeValue can fulfill any requirement, so it's always valid
						// The requirements will be applied at the point where it's consumed (passed as a method parameter, set as field value, returned from the method)
					} else if (uniqueValue == NullValue.Instance) {
						// NullValue can fulfill any requirements because reflection access to it will typically throw.
					} else {
						throw new InvalidOperationException ($"Internal linker error: in {GetContainingSymbolDisplayName ()} processing call to {calledMethod.GetDisplayName ()} returned value which is not correctly annotated with the expected dynamic member access kinds.");
					}
				}
			}

			methodReturnValue = returnValue.Value;

			return true;

			void AddReturnValue (MultiValue value)
			{
				returnValue = (returnValue == null) ? value : MultiValueLattice.Meet (returnValue.Value, value);
			}
		}

		private IEnumerable<MultiValue> ProcessGetMethodByName (TypeProxy type, string methodName, BindingFlags? bindingFlags)
		{
			bool foundAny = false;
			foreach (var method in GetMethodsOnTypeHierarchy (type, methodName, bindingFlags)) {
				MarkMethod (method.RepresentedMethod);
				yield return method;
				foundAny = true;
			}

			// If there were no methods found the API will return null at runtime, so we should
			// track the null as a return value as well.
			// This also prevents warnings in such case, since if we don't set the return value it will be
			// "unknown" and consumers may warn.
			if (!foundAny)
				yield return NullValue.Instance;
		}

		private bool AnalyzeGenericInstantiationTypeArray (in MultiValue arrayParam, in MethodProxy calledMethod, ImmutableArray<GenericParameterValue> genericParameters)
		{
			bool hasRequirements = false;
			foreach (var genericParameter in genericParameters) {
				if (GetGenericParameterEffectiveMemberTypes (genericParameter) != DynamicallyAccessedMemberTypes.None) {
					hasRequirements = true;
					break;
				}
			}

			// If there are no requirements, then there's no point in warning
			if (!hasRequirements)
				return true;

			foreach (var typesValue in arrayParam) {
				if (typesValue is not ArrayValue array) {
					return false;
				}

				int? size = array.Size.AsConstInt ();
				if (size == null || size != genericParameters.Length) {
					return false;
				}

				bool allIndicesKnown = true;
				for (int i = 0; i < size.Value; i++) {
					if (!array.TryGetValueByIndex (i, out MultiValue value) || value.AsSingleValue () is UnknownValue) {
						allIndicesKnown = false;
						break;
					}
				}

				if (!allIndicesKnown) {
					return false;
				}

				for (int i = 0; i < size.Value; i++) {
					if (array.TryGetValueByIndex (i, out MultiValue value)) {
						// https://github.com/dotnet/linker/issues/2428
						// We need to report the target as "this" - as that was the previous behavior
						// but with the annotation from the generic parameter.
						var targetValue = _annotations.GetMethodThisParameterValue (calledMethod, GetGenericParameterEffectiveMemberTypes (genericParameters[i]));
						_requireDynamicallyAccessedMembersAction.Invoke (value, targetValue);
					}
				}
			}
			return true;

			// Returns effective annotation of a generic parameter where it incorporates the constraint into the annotation.
			// There are basically three cases where the constraint matters:
			// - NeedsNew<SpecificType> - MarkStep will simply mark the default .ctor of SpecificType in this case, it has nothing to do with reflection
			// - NeedsNew<TOuter> - this should be validated by the compiler/IL - TOuter must have matching constraints by definition, so nothing to validate
			// - typeof(NeedsNew<>).MakeGenericType(typeOuter) - for this case we have to do it by hand as it's reflection. This is where this method helps.
			static DynamicallyAccessedMemberTypes GetGenericParameterEffectiveMemberTypes (GenericParameterValue genericParameter)
			{
				DynamicallyAccessedMemberTypes result = genericParameter.DynamicallyAccessedMemberTypes;
				if (genericParameter.GenericParameter.HasDefaultConstructorConstraint ())
					result |= DynamicallyAccessedMemberTypes.PublicParameterlessConstructor;

				return result;
			}
		}

		private void ValidateGenericMethodInstantiation (
			MethodProxy genericMethod,
			in MultiValue genericParametersArray,
			MethodProxy reflectionMethod)
		{
			if (!genericMethod.HasGenericParameters ()) {
				return;
			}

			var genericParameterValues = GetGenericParameterValues (genericMethod.GetGenericParameters ());
			if (!AnalyzeGenericInstantiationTypeArray (genericParametersArray, reflectionMethod, genericParameterValues)) {
				_diagnosticContext.AddDiagnostic (DiagnosticId.MakeGenericMethod, reflectionMethod.GetDisplayName ());
			}
		}

		private ImmutableArray<GenericParameterValue> GetGenericParameterValues (ImmutableArray<GenericParameterProxy> genericParameters)
		{
			if (genericParameters.IsEmpty)
				return ImmutableArray<GenericParameterValue>.Empty;

			var builder = ImmutableArray.CreateBuilder<GenericParameterValue> (genericParameters.Length);
			foreach (var genericParameter in genericParameters) {
				builder.Add (_annotations.GetGenericParameterValue (genericParameter));
			}
			return builder.ToImmutableArray ();
		}

		private void ProcessCreateInstanceByName (MethodProxy calledMethod, IReadOnlyList<MultiValue> argumentValues)
		{
			BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
			bool parameterlessConstructor = true;
			if (calledMethod.HasParametersCount (8) && calledMethod.HasParameterOfType (2, "System.Boolean")) {
				parameterlessConstructor = false;
				bindingFlags = BindingFlags.Instance;
				if (argumentValues[3].AsConstInt () is int bindingFlagsInt)
					bindingFlags |= (BindingFlags) bindingFlagsInt;
				else
					bindingFlags |= BindingFlags.Public | BindingFlags.NonPublic;
			}

			foreach (var assemblyNameValue in argumentValues[0]) {
				if (assemblyNameValue is KnownStringValue assemblyNameStringValue) {
					if (assemblyNameStringValue.Contents is string assemblyName && assemblyName.Length == 0) {
						// Throws exception for zero-length assembly name.
						continue;
					}
					foreach (var typeNameValue in argumentValues[1]) {
						if (typeNameValue is NullValue) {
							// Throws exception for null type name.
							continue;
						}
						if (typeNameValue is KnownStringValue typeNameStringValue) {
							if (!TryResolveTypeNameForCreateInstanceAndMark (calledMethod, assemblyNameStringValue.Contents, typeNameStringValue.Contents, out TypeProxy resolvedType)) {
								// It's not wrong to have a reference to non-existing type - the code may well expect to get an exception in this case
								// Note that we did find the assembly, so it's not a linker config problem, it's either intentional, or wrong versions of assemblies
								// but linker can't know that. In case a user tries to create an array using System.Activator we should simply ignore it, the user
								// might expect an exception to be thrown.
								continue;
							}

							MarkConstructorsOnType (resolvedType, bindingFlags, parameterlessConstructor ? 0 : null);
						} else {
							_diagnosticContext.AddDiagnostic (DiagnosticId.UnrecognizedParameterInMethodCreateInstance, calledMethod.GetParameterDisplayName (1), calledMethod.GetDisplayName ());
						}
					}
				} else {
					_diagnosticContext.AddDiagnostic (DiagnosticId.UnrecognizedParameterInMethodCreateInstance, calledMethod.GetParameterDisplayName (0), calledMethod.GetDisplayName ());
				}
			}
		}

		internal static BindingFlags? GetBindingFlagsFromValue (in MultiValue parameter) => (BindingFlags?) parameter.AsConstInt ();

		internal static bool BindingFlagsAreUnsupported (BindingFlags? bindingFlags)
		{
			if (bindingFlags == null)
				return true;

			// Binding flags we understand
			const BindingFlags UnderstoodBindingFlags =
				BindingFlags.DeclaredOnly |
				BindingFlags.Instance |
				BindingFlags.Static |
				BindingFlags.Public |
				BindingFlags.NonPublic |
				BindingFlags.FlattenHierarchy |
				BindingFlags.ExactBinding;

			// Binding flags that don't affect binding outside InvokeMember (that we don't analyze).
			const BindingFlags IgnorableBindingFlags =
				BindingFlags.InvokeMethod |
				BindingFlags.CreateInstance |
				BindingFlags.GetField |
				BindingFlags.SetField |
				BindingFlags.GetProperty |
				BindingFlags.SetProperty;

			BindingFlags flags = bindingFlags.Value;
			return (flags & ~(UnderstoodBindingFlags | IgnorableBindingFlags)) != 0;
		}

		internal static bool HasBindingFlag (BindingFlags? bindingFlags, BindingFlags? search) => bindingFlags != null && (bindingFlags & search) == search;

		internal static DynamicallyAccessedMemberTypes GetDynamicallyAccessedMemberTypesFromBindingFlagsForNestedTypes (BindingFlags? bindingFlags) =>
			(HasBindingFlag (bindingFlags, BindingFlags.Public) ? DynamicallyAccessedMemberTypes.PublicNestedTypes : DynamicallyAccessedMemberTypes.None) |
			(HasBindingFlag (bindingFlags, BindingFlags.NonPublic) ? DynamicallyAccessedMemberTypes.NonPublicNestedTypes : DynamicallyAccessedMemberTypes.None) |
			(BindingFlagsAreUnsupported (bindingFlags) ? DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes : DynamicallyAccessedMemberTypes.None);

		internal static DynamicallyAccessedMemberTypes GetDynamicallyAccessedMemberTypesFromBindingFlagsForConstructors (BindingFlags? bindingFlags) =>
			(HasBindingFlag (bindingFlags, BindingFlags.Public) ? DynamicallyAccessedMemberTypes.PublicConstructors : DynamicallyAccessedMemberTypes.None) |
			(HasBindingFlag (bindingFlags, BindingFlags.NonPublic) ? DynamicallyAccessedMemberTypes.NonPublicConstructors : DynamicallyAccessedMemberTypes.None) |
			(BindingFlagsAreUnsupported (bindingFlags) ? DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors : DynamicallyAccessedMemberTypes.None);

		internal static DynamicallyAccessedMemberTypes GetDynamicallyAccessedMemberTypesFromBindingFlagsForMethods (BindingFlags? bindingFlags) =>
			(HasBindingFlag (bindingFlags, BindingFlags.Public) ? DynamicallyAccessedMemberTypes.PublicMethods : DynamicallyAccessedMemberTypes.None) |
			(HasBindingFlag (bindingFlags, BindingFlags.NonPublic) ? DynamicallyAccessedMemberTypes.NonPublicMethods : DynamicallyAccessedMemberTypes.None) |
			(BindingFlagsAreUnsupported (bindingFlags) ? DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods : DynamicallyAccessedMemberTypes.None);

		internal static DynamicallyAccessedMemberTypes GetDynamicallyAccessedMemberTypesFromBindingFlagsForFields (BindingFlags? bindingFlags) =>
			(HasBindingFlag (bindingFlags, BindingFlags.Public) ? DynamicallyAccessedMemberTypes.PublicFields : DynamicallyAccessedMemberTypes.None) |
			(HasBindingFlag (bindingFlags, BindingFlags.NonPublic) ? DynamicallyAccessedMemberTypes.NonPublicFields : DynamicallyAccessedMemberTypes.None) |
			(BindingFlagsAreUnsupported (bindingFlags) ? DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields : DynamicallyAccessedMemberTypes.None);

		internal static DynamicallyAccessedMemberTypes GetDynamicallyAccessedMemberTypesFromBindingFlagsForProperties (BindingFlags? bindingFlags) =>
			(HasBindingFlag (bindingFlags, BindingFlags.Public) ? DynamicallyAccessedMemberTypes.PublicProperties : DynamicallyAccessedMemberTypes.None) |
			(HasBindingFlag (bindingFlags, BindingFlags.NonPublic) ? DynamicallyAccessedMemberTypes.NonPublicProperties : DynamicallyAccessedMemberTypes.None) |
			(BindingFlagsAreUnsupported (bindingFlags) ? DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties : DynamicallyAccessedMemberTypes.None);

		internal static DynamicallyAccessedMemberTypes GetDynamicallyAccessedMemberTypesFromBindingFlagsForEvents (BindingFlags? bindingFlags) =>
			(HasBindingFlag (bindingFlags, BindingFlags.Public) ? DynamicallyAccessedMemberTypes.PublicEvents : DynamicallyAccessedMemberTypes.None) |
			(HasBindingFlag (bindingFlags, BindingFlags.NonPublic) ? DynamicallyAccessedMemberTypes.NonPublicEvents : DynamicallyAccessedMemberTypes.None) |
			(BindingFlagsAreUnsupported (bindingFlags) ? DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents : DynamicallyAccessedMemberTypes.None);

		internal static DynamicallyAccessedMemberTypes GetDynamicallyAccessedMemberTypesFromBindingFlagsForMembers (BindingFlags? bindingFlags) =>
			GetDynamicallyAccessedMemberTypesFromBindingFlagsForConstructors (bindingFlags) |
			GetDynamicallyAccessedMemberTypesFromBindingFlagsForEvents (bindingFlags) |
			GetDynamicallyAccessedMemberTypesFromBindingFlagsForFields (bindingFlags) |
			GetDynamicallyAccessedMemberTypesFromBindingFlagsForMethods (bindingFlags) |
			GetDynamicallyAccessedMemberTypesFromBindingFlagsForProperties (bindingFlags) |
			GetDynamicallyAccessedMemberTypesFromBindingFlagsForNestedTypes (bindingFlags);

		/// <Summary>
		/// Returns true if the method is a .ctor for System.Type or a type that derives from System.Type (i.e. fields and params of this type can have DynamicallyAccessedMembers annotations)
		/// </Summary>
		private partial bool MethodIsTypeConstructor (MethodProxy method);

		private partial IEnumerable<SystemReflectionMethodBaseValue> GetMethodsOnTypeHierarchy (TypeProxy type, string name, BindingFlags? bindingFlags);

		private partial IEnumerable<SystemTypeValue> GetNestedTypesOnType (TypeProxy type, string name, BindingFlags? bindingFlags);

		private partial bool TryGetBaseType (TypeProxy type, [NotNullWhen (true)] out TypeProxy? baseType);

		private partial bool TryResolveTypeNameForCreateInstanceAndMark (in MethodProxy calledMethod, string assemblyName, string typeName, out TypeProxy resolvedType);

		private partial void MarkStaticConstructor (TypeProxy type);

		private partial void MarkEventsOnTypeHierarchy (TypeProxy type, string name, BindingFlags? bindingFlags);

		private partial void MarkFieldsOnTypeHierarchy (TypeProxy type, string name, BindingFlags? bindingFlags);

		private partial void MarkPropertiesOnTypeHierarchy (TypeProxy type, string name, BindingFlags? bindingFlags);

		private partial void MarkPublicParameterlessConstructorOnType (TypeProxy type);

		private partial void MarkConstructorsOnType (TypeProxy type, BindingFlags? bindingFlags, int? parameterCount);

		private partial void MarkMethod (MethodProxy method);

		private partial void MarkType (TypeProxy type);

		private partial bool MarkAssociatedProperty (MethodProxy method);

		// Only used for internal diagnostic purposes (not even for warning messages)
		private partial string GetContainingSymbolDisplayName ();
	}
}
