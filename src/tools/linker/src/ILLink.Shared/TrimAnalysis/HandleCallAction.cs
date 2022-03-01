// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
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
			MultiValue? returnValue = null;

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
				if (argumentValues[0].IsEmpty ()) {
					returnValue = MultiValueLattice.Top;
					break;
				}

				foreach (var value in argumentValues[0]) {
					if (value is RuntimeTypeHandleValue typeHandle)
						AddReturnValue (new SystemTypeValue (typeHandle.RepresentedType));
					else if (value is RuntimeTypeHandleForGenericParameterValue typeHandleForGenericParameter)
						AddReturnValue (GetGenericParameterValue (typeHandleForGenericParameter.GenericParameter));
					else
						AddReturnValue (GetMethodReturnValue (calledMethod, returnValueDynamicallyAccessedMemberTypes));
				}
				break;

			case IntrinsicId.Type_get_TypeHandle:
				if (instanceValue.IsEmpty ()) {
					returnValue = MultiValueLattice.Top;
					break;
				}

				foreach (var value in instanceValue) {
					if (value is SystemTypeValue typeValue)
						AddReturnValue (new RuntimeTypeHandleValue (typeValue.RepresentedType));
					else if (value is GenericParameterValue genericParameterValue)
						AddReturnValue (new RuntimeTypeHandleForGenericParameterValue (genericParameterValue.GenericParameter));
					else if (value == NullValue.Instance) {
						// Throws if the input is null, so no return value.
						returnValue ??= MultiValueLattice.Top;
					} else
						AddReturnValue (GetMethodReturnValue (calledMethod, returnValueDynamicallyAccessedMemberTypes));
				}
				break;

			//
			// GetInterface (String)
			// GetInterface (String, bool)
			//
			case IntrinsicId.Type_GetInterface: {
					if (instanceValue.IsEmpty () || argumentValues[0].IsEmpty ()) {
						returnValue = MultiValueLattice.Top;
						break;
					}

					var targetValue = GetMethodThisParameterValue (calledMethod, DynamicallyAccessedMemberTypesOverlay.Interfaces);
					foreach (var value in instanceValue) {
						foreach (var interfaceName in argumentValues[0]) {
							if (interfaceName == NullValue.Instance) {
								// Throws on null string, so no return value.
								returnValue ??= MultiValueLattice.Top;
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

								AddReturnValue (GetMethodReturnValue (calledMethod, returnMemberTypes));
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
							returnValue ??= MultiValueLattice.Top;
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

					var targetValue = GetMethodThisParameterValue (calledMethod, memberTypes);
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

					var targetValue = GetMethodThisParameterValue (calledMethod, memberTypes);
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

					var targetValue = GetMethodThisParameterValue (calledMethod, requiredMemberTypes);

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
					BindingFlags? bindingFlags;
					if (calledMethod.HasParameterOfType (1, "System.Reflection.BindingFlags"))
						bindingFlags = GetBindingFlagsFromValue (argumentValues[1]);
					else if (calledMethod.HasParameterOfType (2, "System.Reflection.BindingFlags"))
						bindingFlags = GetBindingFlagsFromValue (argumentValues[2]);
					else
						// Assume a default value for BindingFlags for methods that don't use BindingFlags as a parameter
						bindingFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public;

					var targetValue = GetMethodThisParameterValue (calledMethod, GetDynamicallyAccessedMemberTypesFromBindingFlagsForMethods (bindingFlags));
					foreach (var value in instanceValue) {
						if (value is SystemTypeValue systemTypeValue) {
							foreach (var stringParam in argumentValues[0]) {
								if (stringParam is KnownStringValue stringValue && !BindingFlagsAreUnsupported (bindingFlags)) {
									foreach (var methodValue in ProcessGetMethodByName (systemTypeValue.RepresentedType, stringValue.Contents, bindingFlags))
										AddReturnValue (methodValue);
								} else {
									// Otherwise fall back to the bitfield requirements
									_requireDynamicallyAccessedMembersAction.Invoke (value, targetValue);
								}
							}
						} else {
							// Otherwise fall back to the bitfield requirements
							_requireDynamicallyAccessedMembersAction.Invoke (value, targetValue);
						}
					}
				}
				break;

			//
			// GetNestedType (string)
			// GetNestedType (string, BindingFlags)
			//
			case IntrinsicId.Type_GetNestedType: {
					BindingFlags? bindingFlags;
					if (calledMethod.HasParameterOfType (1, "System.Reflection.BindingFlags"))
						bindingFlags = GetBindingFlagsFromValue (argumentValues[1]);
					else
						// Assume a default value for BindingFlags for methods that don't use BindingFlags as a parameter
						bindingFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public;

					var targetValue = GetMethodThisParameterValue (calledMethod, GetDynamicallyAccessedMemberTypesFromBindingFlagsForNestedTypes (bindingFlags));
					bool everyParentTypeHasAll = true;
					foreach (var value in instanceValue) {
						if (value is SystemTypeValue systemTypeValue) {
							foreach (var stringParam in argumentValues[0]) {
								if (stringParam is KnownStringValue stringValue && !BindingFlagsAreUnsupported (bindingFlags)) {
									foreach (var nestedTypeValue in GetNestedTypesOnType (systemTypeValue.RepresentedType, stringValue.Contents, bindingFlags)) {
										MarkType (nestedTypeValue.RepresentedType);
										AddReturnValue (nestedTypeValue);
									}
								} else {
									// Otherwise fall back to the bitfield requirements
									_requireDynamicallyAccessedMembersAction.Invoke (value, targetValue);
								}
							}
						} else {
							// Otherwise fall back to the bitfield requirements
							_requireDynamicallyAccessedMembersAction.Invoke (value, targetValue);
						}

						if (value is ValueWithDynamicallyAccessedMembers valueWithDynamicallyAccessedMembers) {
							if (valueWithDynamicallyAccessedMembers.DynamicallyAccessedMemberTypes != DynamicallyAccessedMemberTypes.All)
								everyParentTypeHasAll = false;
						} else if (!(value is NullValue || value is SystemTypeValue)) {
							// Known Type values are always OK - either they're fully resolved above and thus the return value
							// is set to the known resolved type, or if they're not resolved, they won't exist at runtime
							// and will cause exceptions - and thus don't introduce new requirements on marking.
							// nulls are intentionally ignored as they will lead to exceptions at runtime
							// and thus don't introduce new requirements on marking.
							everyParentTypeHasAll = false;
						}
					}

					// If the parent type (all the possible values) has DynamicallyAccessedMemberTypes.All it means its nested types are also fully marked
					// (see MarkStep.MarkEntireType - it will recursively mark entire type on nested types). In that case we can annotate
					// the returned type (the nested type) with DynamicallyAccessedMemberTypes.All as well.
					// Note it's OK to blindly overwrite any potential annotation on the return value from the method definition
					// since DynamicallyAccessedMemberTypes.All is a superset of any other annotation.
					if (everyParentTypeHasAll && returnValue == null)
						returnValue = GetMethodReturnValue (calledMethod, DynamicallyAccessedMemberTypes.All);
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

					BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public;
					DynamicallyAccessedMemberTypes requiredMemberTypes = getRuntimeMember switch {
						IntrinsicId.RuntimeReflectionExtensions_GetRuntimeEvent => DynamicallyAccessedMemberTypes.PublicEvents,
						IntrinsicId.RuntimeReflectionExtensions_GetRuntimeField => DynamicallyAccessedMemberTypes.PublicFields,
						IntrinsicId.RuntimeReflectionExtensions_GetRuntimeMethod => DynamicallyAccessedMemberTypes.PublicMethods,
						IntrinsicId.RuntimeReflectionExtensions_GetRuntimeProperty => DynamicallyAccessedMemberTypes.PublicProperties,
						_ => throw new ArgumentException ($"Reflection call '{calledMethod.GetDisplayName ()}' inside '{GetContainingSymbolDisplayName ()}' is of unexpected member type."),
					};

					var targetValue = GetMethodParameterValue (calledMethod, 0, requiredMemberTypes);

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
										foreach (var methodValue in ProcessGetMethodByName (systemTypeValue.RepresentedType, stringValue.Contents, bindingFlags))
											AddReturnValue (methodValue);
										break;
									case IntrinsicId.RuntimeReflectionExtensions_GetRuntimeProperty:
										MarkPropertiesOnTypeHierarchy (systemTypeValue.RepresentedType, stringValue.Contents, bindingFlags);
										break;
									default:
										throw new ArgumentException ($"Error processing reflection call '{calledMethod.GetDisplayName ()}' inside {GetContainingSymbolDisplayName ()}. Unexpected member kind.");
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
			// System.Linq.Expressions.Expression
			//
			// static Property (Expression, MethodInfo)
			//
			case IntrinsicId.Expression_Property when calledMethod.HasParameterOfType (1, "System.Reflection.MethodInfo"): {
					foreach (var value in argumentValues[1]) {
						if (value is SystemReflectionMethodBaseValue methodBaseValue) {
							// We have one of the accessors for the property. The Expression.Property will in this case search
							// for the matching PropertyInfo and store that. So to be perfectly correct we need to mark the
							// respective PropertyInfo as "accessed via reflection".
							if (MarkAssociatedProperty (methodBaseValue.MethodRepresented))
								continue;
						} else if (value == NullValue.Instance) {
							continue;
						}

						// In all other cases we may not even know which type this is about, so there's nothing we can do
						// report it as a warning.
						_diagnosticContext.AddDiagnostic (DiagnosticId.PropertyAccessorParameterInLinqExpressionsCannotBeStaticallyDetermined,
							GetMethodParameterValue (calledMethod, 1, DynamicallyAccessedMemberTypes.None).GetDiagnosticArgumentsForAnnotationMismatch ().ToArray ());
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

					var targetValue = GetMethodParameterValue (calledMethod, 1, memberTypes);
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

			// For now, if the intrinsic doesn't set a return value, fall back on the annotations.
			// Note that this will be DynamicallyAccessedMembers.None for the intrinsics which don't return types.
			returnValue ??= calledMethod.ReturnsVoid () ? MultiValueLattice.Top : GetMethodReturnValue (calledMethod, returnValueDynamicallyAccessedMemberTypes);

			// Validate that the return value has the correct annotations as per the method return value annotations
			if (returnValueDynamicallyAccessedMemberTypes != 0) {
				foreach (var uniqueValue in returnValue.Value) {
					if (uniqueValue is ValueWithDynamicallyAccessedMembers methodReturnValueWithMemberTypes) {
						if (!methodReturnValueWithMemberTypes.DynamicallyAccessedMemberTypes.HasFlag (returnValueDynamicallyAccessedMemberTypes))
							throw new InvalidOperationException ($"Internal linker error: in {GetContainingSymbolDisplayName ()} processing call to {calledMethod.GetDisplayName ()} returned value which is not correctly annotated with the expected dynamic member access kinds.");
					} else if (uniqueValue is SystemTypeValue) {
						// SystemTypeValue can fullfill any requirement, so it's always valid
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

		IEnumerable<MultiValue> ProcessGetMethodByName (TypeProxy type, string methodName, BindingFlags? bindingFlags)
		{
			bool foundAny = false;
			foreach (var method in GetMethodsOnTypeHierarchy (type, methodName, bindingFlags)) {
				MarkMethod (method.MethodRepresented);
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

		private partial bool MethodRequiresDataFlowAnalysis (MethodProxy method);

		private partial DynamicallyAccessedMemberTypes GetReturnValueAnnotation (MethodProxy method);

		private partial MethodReturnValue GetMethodReturnValue (MethodProxy method, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes);

		private partial GenericParameterValue GetGenericParameterValue (GenericParameterProxy genericParameter);

		private partial MethodThisParameterValue GetMethodThisParameterValue (MethodProxy method, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes);

		private partial MethodParameterValue GetMethodParameterValue (MethodProxy method, int parameterIndex, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes);

		private partial IEnumerable<SystemReflectionMethodBaseValue> GetMethodsOnTypeHierarchy (TypeProxy type, string name, BindingFlags? bindingFlags);

		private partial IEnumerable<SystemTypeValue> GetNestedTypesOnType (TypeProxy type, string name, BindingFlags? bindingFlags);

		private partial void MarkStaticConstructor (TypeProxy type);

		private partial void MarkEventsOnTypeHierarchy (TypeProxy type, string name, BindingFlags? bindingFlags);

		private partial void MarkFieldsOnTypeHierarchy (TypeProxy type, string name, BindingFlags? bindingFlags);

		private partial void MarkPropertiesOnTypeHierarchy (TypeProxy type, string name, BindingFlags? bindingFlags);

		private partial void MarkMethod (MethodProxy method);

		private partial void MarkType (TypeProxy type);

		private partial bool MarkAssociatedProperty (MethodProxy method);

		// Only used for internal diagnostic purposes (not even for warning messages)
		private partial string GetContainingSymbolDisplayName ();
	}
}
