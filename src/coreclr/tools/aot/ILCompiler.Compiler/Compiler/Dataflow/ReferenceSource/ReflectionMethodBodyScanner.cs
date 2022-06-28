// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using ILLink.Shared;
using ILLink.Shared.DataFlow;
using ILLink.Shared.TrimAnalysis;
using ILLink.Shared.TypeSystemProxy;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Linker.Steps;
using MultiValue = ILLink.Shared.DataFlow.ValueSet<ILLink.Shared.DataFlow.SingleValue>;

namespace Mono.Linker.Dataflow
{
	class ReflectionMethodBodyScanner : MethodBodyScanner
	{
		readonly MarkStep _markStep;
		MessageOrigin _origin;
		readonly FlowAnnotations _annotations;
		readonly ReflectionMarker _reflectionMarker;

		public static bool RequiresReflectionMethodBodyScannerForCallSite (LinkContext context, MethodReference calledMethod)
		{
			MethodDefinition? methodDefinition = context.TryResolve (calledMethod);
			if (methodDefinition == null)
				return false;

			return Intrinsics.GetIntrinsicIdForMethod (methodDefinition) > IntrinsicId.RequiresReflectionBodyScanner_Sentinel ||
				context.Annotations.FlowAnnotations.RequiresDataFlowAnalysis (methodDefinition) ||
				context.Annotations.DoesMethodRequireUnreferencedCode (methodDefinition, out _) ||
				methodDefinition.IsPInvokeImpl;
		}

		public static bool RequiresReflectionMethodBodyScannerForMethodBody (LinkContext context, MethodDefinition methodDefinition)
		{
			return Intrinsics.GetIntrinsicIdForMethod (methodDefinition) > IntrinsicId.RequiresReflectionBodyScanner_Sentinel ||
				context.Annotations.FlowAnnotations.RequiresDataFlowAnalysis (methodDefinition);
		}

		public static bool RequiresReflectionMethodBodyScannerForAccess (LinkContext context, FieldReference field)
		{
			FieldDefinition? fieldDefinition = context.TryResolve (field);
			if (fieldDefinition == null)
				return false;

			return context.Annotations.FlowAnnotations.RequiresDataFlowAnalysis (fieldDefinition);
		}

		bool ShouldEnableReflectionPatternReporting (ICustomAttributeProvider? provider)
		{
			if (_markStep.ShouldSuppressAnalysisWarningsForRequiresUnreferencedCode (provider))
				return false;

			return true;
		}

		public ReflectionMethodBodyScanner (LinkContext context, MarkStep parent, MessageOrigin origin)
			: base (context)
		{
			_markStep = parent;
			_origin = origin;
			_annotations = context.Annotations.FlowAnnotations;
			_reflectionMarker = new ReflectionMarker (context, parent);
		}

		public void ScanAndProcessReturnValue (MethodBody methodBody)
		{
			Scan (methodBody);

			if (!methodBody.Method.ReturnsVoid ()) {
				var method = methodBody.Method;
				var methodReturnValue = _annotations.GetMethodReturnValue (method);
				if (methodReturnValue.DynamicallyAccessedMemberTypes != 0) {
					var diagnosticContext = new DiagnosticContext (_origin, ShouldEnableReflectionPatternReporting (_origin.Provider), _context);
					RequireDynamicallyAccessedMembers (diagnosticContext, ReturnValue, methodReturnValue);
				}
			}
		}

		public void ProcessAttributeDataflow (MethodDefinition method, IList<CustomAttributeArgument> arguments)
		{
			for (int i = 0; i < method.Parameters.Count; i++) {
				var parameterValue = _annotations.GetMethodParameterValue (method, i);
				if (parameterValue.DynamicallyAccessedMemberTypes != DynamicallyAccessedMemberTypes.None) {
					MultiValue value = GetValueNodeForCustomAttributeArgument (arguments[i]);
					var diagnosticContext = new DiagnosticContext (_origin, diagnosticsEnabled: true, _context);
					RequireDynamicallyAccessedMembers (diagnosticContext, value, parameterValue);
				}
			}
		}

		public void ProcessAttributeDataflow (FieldDefinition field, CustomAttributeArgument value)
		{
			MultiValue valueNode = GetValueNodeForCustomAttributeArgument (value);
			foreach (var fieldValueCandidate in GetFieldValue (field)) {
				if (fieldValueCandidate is not ValueWithDynamicallyAccessedMembers fieldValue)
					continue;

				var diagnosticContext = new DiagnosticContext (_origin, diagnosticsEnabled: true, _context);
				RequireDynamicallyAccessedMembers (diagnosticContext, valueNode, fieldValue);
			}
		}

		MultiValue GetValueNodeForCustomAttributeArgument (CustomAttributeArgument argument)
		{
			SingleValue value;
			if (argument.Type.Name == "Type") {
				TypeDefinition? referencedType = ResolveToTypeDefinition ((TypeReference) argument.Value);
				if (referencedType == null)
					value = UnknownValue.Instance;
				else
					value = new SystemTypeValue (referencedType);
			} else if (argument.Type.MetadataType == MetadataType.String) {
				value = new KnownStringValue ((string) argument.Value);
			} else {
				// We shouldn't have gotten a non-null annotation for this from GetParameterAnnotation
				throw new InvalidOperationException ();
			}

			Debug.Assert (value != null);
			return value;
		}

		public void ProcessGenericArgumentDataFlow (GenericParameter genericParameter, TypeReference genericArgument)
		{
			var genericParameterValue = _annotations.GetGenericParameterValue (genericParameter);
			Debug.Assert (genericParameterValue.DynamicallyAccessedMemberTypes != DynamicallyAccessedMemberTypes.None);

			MultiValue genericArgumentValue = GetTypeValueNodeFromGenericArgument (genericArgument);

			var diagnosticContext = new DiagnosticContext (_origin, ShouldEnableReflectionPatternReporting (_origin.Provider), _context);
			RequireDynamicallyAccessedMembers (diagnosticContext, genericArgumentValue, genericParameterValue);
		}

		MultiValue GetTypeValueNodeFromGenericArgument (TypeReference genericArgument)
		{
			if (genericArgument is GenericParameter inputGenericParameter) {
				// Technically this should be a new value node type as it's not a System.Type instance representation, but just the generic parameter
				// That said we only use it to perform the dynamically accessed members checks and for that purpose treating it as System.Type is perfectly valid.
				return _annotations.GetGenericParameterValue (inputGenericParameter);
			} else if (ResolveToTypeDefinition (genericArgument) is TypeDefinition genericArgumentType) {
				if (genericArgumentType.IsTypeOf (WellKnownType.System_Nullable_T)) {
					var innerGenericArgument = (genericArgument as IGenericInstance)?.GenericArguments.FirstOrDefault ();
					switch (innerGenericArgument) {
					case GenericParameter gp:
						return new NullableValueWithDynamicallyAccessedMembers (genericArgumentType,
							new GenericParameterValue (gp, _context.Annotations.FlowAnnotations.GetGenericParameterAnnotation (gp)));

					case TypeReference underlyingType:
						if (ResolveToTypeDefinition (underlyingType) is TypeDefinition underlyingTypeDefinition)
							return new NullableSystemTypeValue (genericArgumentType, new SystemTypeValue (underlyingTypeDefinition));
						else
							return UnknownValue.Instance;
					}
				}
				// All values except for Nullable<T>, including Nullable<> (with no type arguments)
				return new SystemTypeValue (genericArgumentType);
			} else {
				return UnknownValue.Instance;
			}
		}

		protected override void WarnAboutInvalidILInMethod (MethodBody method, int ilOffset)
		{
			// Serves as a debug helper to make sure valid IL is not considered invalid.
			//
			// The .NET Native compiler used to warn if it detected invalid IL during treeshaking,
			// but the warnings were often triggered in autogenerated dead code of a major game engine
			// and resulted in support calls. No point in warning. If the code gets exercised at runtime,
			// an InvalidProgramException will likely be raised.
			Debug.Fail ("Invalid IL or a bug in the scanner");
		}

		protected override ValueWithDynamicallyAccessedMembers GetMethodParameterValue (MethodDefinition method, int parameterIndex)
			=> GetMethodParameterValue (method, parameterIndex, _context.Annotations.FlowAnnotations.GetParameterAnnotation (method, parameterIndex));

		ValueWithDynamicallyAccessedMembers GetMethodParameterValue (MethodDefinition method, int parameterIndex, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes)
		{
			if (method.HasImplicitThis ()) {
				if (parameterIndex == 0)
					return _annotations.GetMethodThisParameterValue (method, dynamicallyAccessedMemberTypes);

				parameterIndex--;
			}

			return _annotations.GetMethodParameterValue (method, parameterIndex, dynamicallyAccessedMemberTypes);
		}

		protected override MultiValue GetFieldValue (FieldDefinition field)
		{
			switch (field.Name) {
			case "EmptyTypes" when field.DeclaringType.IsTypeOf (WellKnownType.System_Type): {
					return ArrayValue.Create (0, field.DeclaringType);
				}
			case "Empty" when field.DeclaringType.IsTypeOf (WellKnownType.System_String): {
					return new KnownStringValue (string.Empty);
				}

			default: {
					DynamicallyAccessedMemberTypes memberTypes = _context.Annotations.FlowAnnotations.GetFieldAnnotation (field);
					return new FieldValue (ResolveToTypeDefinition (field.FieldType), field, memberTypes);
				}
			}
		}

		protected override void HandleStoreField (MethodDefinition method, FieldValue field, Instruction operation, MultiValue valueToStore)
		{
			if (field.DynamicallyAccessedMemberTypes != 0) {
				_origin = _origin.WithInstructionOffset (operation.Offset);
				var diagnosticContext = new DiagnosticContext (_origin, ShouldEnableReflectionPatternReporting (_origin.Provider), _context);
				RequireDynamicallyAccessedMembers (diagnosticContext, valueToStore, field);
			}
		}

		protected override void HandleStoreParameter (MethodDefinition method, MethodParameterValue parameter, Instruction operation, MultiValue valueToStore)
		{
			if (parameter.DynamicallyAccessedMemberTypes != 0) {
				_origin = _origin.WithInstructionOffset (operation.Offset);
				var diagnosticContext = new DiagnosticContext (_origin, ShouldEnableReflectionPatternReporting (_origin.Provider), _context);
				RequireDynamicallyAccessedMembers (diagnosticContext, valueToStore, parameter);
			}
		}

		public override bool HandleCall (MethodBody callingMethodBody, MethodReference calledMethod, Instruction operation, ValueNodeList methodParams, out MultiValue methodReturnValue)
		{
			methodReturnValue = new ();
			MultiValue? maybeMethodReturnValue = null;

			var reflectionProcessed = _markStep.ProcessReflectionDependency (callingMethodBody, operation);
			if (reflectionProcessed)
				return false;

			var callingMethodDefinition = callingMethodBody.Method;
			var calledMethodDefinition = _context.TryResolve (calledMethod);
			if (calledMethodDefinition == null)
				return false;

			bool requiresDataFlowAnalysis = _context.Annotations.FlowAnnotations.RequiresDataFlowAnalysis (calledMethodDefinition);
			DynamicallyAccessedMemberTypes returnValueDynamicallyAccessedMemberTypes = requiresDataFlowAnalysis ?
				_context.Annotations.FlowAnnotations.GetReturnParameterAnnotation (calledMethodDefinition) : 0;

			_origin = _origin.WithInstructionOffset (operation.Offset);
			bool diagnosticsEnabled = ShouldEnableReflectionPatternReporting (_origin.Provider);
			var diagnosticContext = new DiagnosticContext (_origin, diagnosticsEnabled, _context);
			var handleCallAction = new HandleCallAction (_context, _reflectionMarker, diagnosticContext, callingMethodDefinition);
			switch (Intrinsics.GetIntrinsicIdForMethod (calledMethodDefinition)) {
			case IntrinsicId.IntrospectionExtensions_GetTypeInfo:
			case IntrinsicId.TypeInfo_AsType:
			case IntrinsicId.Type_get_UnderlyingSystemType:
			case IntrinsicId.Type_GetTypeFromHandle:
			case IntrinsicId.Type_get_TypeHandle:
			case IntrinsicId.Type_GetInterface:
			case IntrinsicId.Type_get_AssemblyQualifiedName:
			case IntrinsicId.RuntimeHelpers_RunClassConstructor:
			case var callType when (callType == IntrinsicId.Type_GetConstructors || callType == IntrinsicId.Type_GetMethods || callType == IntrinsicId.Type_GetFields ||
				callType == IntrinsicId.Type_GetProperties || callType == IntrinsicId.Type_GetEvents || callType == IntrinsicId.Type_GetNestedTypes || callType == IntrinsicId.Type_GetMembers)
				&& calledMethod.DeclaringType.IsTypeOf (WellKnownType.System_Type)
				&& calledMethod.Parameters[0].ParameterType.FullName == "System.Reflection.BindingFlags"
				&& calledMethod.HasThis:
			case var fieldPropertyOrEvent when (fieldPropertyOrEvent == IntrinsicId.Type_GetField || fieldPropertyOrEvent == IntrinsicId.Type_GetProperty || fieldPropertyOrEvent == IntrinsicId.Type_GetEvent)
				&& calledMethod.DeclaringType.IsTypeOf (WellKnownType.System_Type)
				&& calledMethod.Parameters[0].ParameterType.IsTypeOf (WellKnownType.System_String)
				&& calledMethod.HasThis:
			case var getRuntimeMember when getRuntimeMember == IntrinsicId.RuntimeReflectionExtensions_GetRuntimeEvent
				|| getRuntimeMember == IntrinsicId.RuntimeReflectionExtensions_GetRuntimeField
				|| getRuntimeMember == IntrinsicId.RuntimeReflectionExtensions_GetRuntimeMethod
				|| getRuntimeMember == IntrinsicId.RuntimeReflectionExtensions_GetRuntimeProperty:
			case IntrinsicId.Type_GetMember:
			case IntrinsicId.Type_GetMethod:
			case IntrinsicId.Type_GetNestedType:
			case IntrinsicId.Nullable_GetUnderlyingType:
			case IntrinsicId.Expression_Property when calledMethod.HasParameterOfType (1, "System.Reflection.MethodInfo"):
			case var fieldOrPropertyInstrinsic when fieldOrPropertyInstrinsic == IntrinsicId.Expression_Field || fieldOrPropertyInstrinsic == IntrinsicId.Expression_Property:
			case IntrinsicId.Type_get_BaseType:
			case IntrinsicId.Type_GetConstructor:
			case IntrinsicId.MethodBase_GetMethodFromHandle:
			case IntrinsicId.MethodBase_get_MethodHandle:
			case IntrinsicId.Type_MakeGenericType:
			case IntrinsicId.MethodInfo_MakeGenericMethod:
			case IntrinsicId.Expression_Call:
			case IntrinsicId.Expression_New:
			case IntrinsicId.Type_GetType:
			case IntrinsicId.Activator_CreateInstance_Type:
			case IntrinsicId.Activator_CreateInstance_AssemblyName_TypeName:
			case IntrinsicId.Activator_CreateInstanceFrom:
			case var appDomainCreateInstance when appDomainCreateInstance == IntrinsicId.AppDomain_CreateInstance
					|| appDomainCreateInstance == IntrinsicId.AppDomain_CreateInstanceAndUnwrap
					|| appDomainCreateInstance == IntrinsicId.AppDomain_CreateInstanceFrom
					|| appDomainCreateInstance == IntrinsicId.AppDomain_CreateInstanceFromAndUnwrap:
			case IntrinsicId.Assembly_CreateInstance: {
					var instanceValue = MultiValueLattice.Top;
					IReadOnlyList<MultiValue> parameterValues = methodParams;
					if (calledMethodDefinition.HasImplicitThis ()) {
						instanceValue = methodParams[0];
						parameterValues = parameterValues.Skip (1).ToImmutableList ();
					}
					return handleCallAction.Invoke (calledMethodDefinition, instanceValue, parameterValues, out methodReturnValue, out _);
				}

			case IntrinsicId.None: {
					if (calledMethodDefinition.IsPInvokeImpl) {
						// Is the PInvoke dangerous?
						bool comDangerousMethod = IsComInterop (calledMethodDefinition.MethodReturnType, calledMethodDefinition.ReturnType);
						foreach (ParameterDefinition pd in calledMethodDefinition.Parameters) {
							comDangerousMethod |= IsComInterop (pd, pd.ParameterType);
						}

						if (comDangerousMethod) {
							diagnosticContext.AddDiagnostic (DiagnosticId.CorrectnessOfCOMCannotBeGuaranteed, calledMethodDefinition.GetDisplayName ());
						}
					}
					_markStep.CheckAndReportRequiresUnreferencedCode (calledMethodDefinition, _origin);

					var instanceValue = MultiValueLattice.Top;
					IReadOnlyList<MultiValue> parameterValues = methodParams;
					if (calledMethodDefinition.HasImplicitThis ()) {
						instanceValue = methodParams[0];
						parameterValues = parameterValues.Skip (1).ToImmutableList ();
					}
					return handleCallAction.Invoke (calledMethodDefinition, instanceValue, parameterValues, out methodReturnValue, out _);
				}

			case IntrinsicId.TypeDelegator_Ctor: {
					// This is an identity function for analysis purposes
					if (operation.OpCode == OpCodes.Newobj)
						AddReturnValue (methodParams[1]);
				}
				break;

			case IntrinsicId.Array_Empty: {
					AddReturnValue (ArrayValue.Create (0, ((GenericInstanceMethod) calledMethod).GenericArguments[0]));
				}
				break;

			//
			// System.Object
			//
			// GetType()
			//
			case IntrinsicId.Object_GetType: {
					foreach (var valueNode in methodParams[0]) {
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

						TypeDefinition? staticType = (valueNode as IValueWithStaticType)?.StaticType;
						if (staticType is null) {
							// We don't know anything about the type GetType was called on. Track this as a usual result of a method call without any annotations
							AddReturnValue (_annotations.GetMethodReturnValue (calledMethodDefinition));
						} else if (staticType.IsSealed || staticType.IsTypeOf ("System", "Delegate")) {
							// We can treat this one the same as if it was a typeof() expression

							// We can allow Object.GetType to be modeled as System.Delegate because we keep all methods
							// on delegates anyway so reflection on something this approximation would miss is actually safe.

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
							_reflectionMarker.MarkType (_origin, staticType);

							var annotation = _markStep.DynamicallyAccessedMembersTypeHierarchy
								.ApplyDynamicallyAccessedMembersToTypeHierarchy (_reflectionMarker, staticType);

							// Return a value which is "unknown type" with annotation. For now we'll use the return value node
							// for the method, which means we're loosing the information about which staticType this
							// started with. For now we don't need it, but we can add it later on.
							AddReturnValue (_annotations.GetMethodReturnValue (calledMethodDefinition, annotation));
						}
					}
				}
				break;

			// Note about Activator.CreateInstance<T>
			// There are 2 interesting cases:
			//  - The generic argument for T is either specific type or annotated - in that case generic instantiation will handle this
			//    since from .NET 6+ the T is annotated with PublicParameterlessConstructor annotation, so the linker would apply this as for any other method.
			//  - The generic argument for T is unannotated type - the generic instantiantion handling has a special case for handling PublicParameterlessConstructor requirement
			//    in such that if the generic argument type has the "new" constraint it will not warn (as it is effectively the same thing semantically).
			//    For all other cases, the linker would have already produced a warning.

			default:
				throw new NotImplementedException ("Unhandled instrinsic");
			}

			// If we get here, we handled this as an intrinsic.  As a convenience, if the code above
			// didn't set the return value (and the method has a return value), we will set it to be an
			// unknown value with the return type of the method.
			bool returnsVoid = calledMethod.ReturnsVoid ();
			methodReturnValue = maybeMethodReturnValue ?? (returnsVoid ?
				MultiValueLattice.Top :
				_annotations.GetMethodReturnValue (calledMethodDefinition, returnValueDynamicallyAccessedMemberTypes));

			// Validate that the return value has the correct annotations as per the method return value annotations
			if (returnValueDynamicallyAccessedMemberTypes != 0) {
				foreach (var uniqueValue in methodReturnValue) {
					if (uniqueValue is ValueWithDynamicallyAccessedMembers methodReturnValueWithMemberTypes) {
						if (!methodReturnValueWithMemberTypes.DynamicallyAccessedMemberTypes.HasFlag (returnValueDynamicallyAccessedMemberTypes))
							throw new InvalidOperationException ($"Internal linker error: processing of call from {callingMethodDefinition.GetDisplayName ()} to {calledMethod.GetDisplayName ()} returned value which is not correctly annotated with the expected dynamic member access kinds.");
					} else if (uniqueValue is SystemTypeValue) {
						// SystemTypeValue can fullfill any requirement, so it's always valid
						// The requirements will be applied at the point where it's consumed (passed as a method parameter, set as field value, returned from the method)
					} else {
						throw new InvalidOperationException ($"Internal linker error: processing of call from {callingMethodDefinition.GetDisplayName ()} to {calledMethod.GetDisplayName ()} returned value which is not correctly annotated with the expected dynamic member access kinds.");
					}
				}
			}

			return true;

			void AddReturnValue (MultiValue value)
			{
				maybeMethodReturnValue = (maybeMethodReturnValue is null) ? value : MultiValueLattice.Meet ((MultiValue) maybeMethodReturnValue, value);
			}
		}

		bool IsComInterop (IMarshalInfoProvider marshalInfoProvider, TypeReference parameterType)
		{
			// This is best effort. One can likely find ways how to get COM without triggering these alarms.
			// AsAny marshalling of a struct with an object-typed field would be one, for example.

			// This logic roughly corresponds to MarshalInfo::MarshalInfo in CoreCLR,
			// not trying to handle invalid cases and distinctions that are not interesting wrt
			// "is this COM?" question.

			NativeType nativeType = NativeType.None;
			if (marshalInfoProvider.HasMarshalInfo) {
				nativeType = marshalInfoProvider.MarshalInfo.NativeType;
			}

			if (nativeType == NativeType.IUnknown || nativeType == NativeType.IDispatch || nativeType == NativeType.IntF) {
				// This is COM by definition
				return true;
			}

			if (nativeType == NativeType.None) {
				// Resolve will look at the element type
				var parameterTypeDef = _context.TryResolve (parameterType);

				if (parameterTypeDef != null) {
					if (parameterTypeDef.IsTypeOf (WellKnownType.System_Array)) {
						// System.Array marshals as IUnknown by default
						return true;
					} else if (parameterTypeDef.IsTypeOf (WellKnownType.System_String) ||
						parameterTypeDef.IsTypeOf ("System.Text", "StringBuilder")) {
						// String and StringBuilder are special cased by interop
						return false;
					}

					if (parameterTypeDef.IsValueType) {
						// Value types don't marshal as COM
						return false;
					} else if (parameterTypeDef.IsInterface) {
						// Interface types marshal as COM by default
						return true;
					} else if (parameterTypeDef.IsMulticastDelegate ()) {
						// Delegates are special cased by interop
						return false;
					} else if (parameterTypeDef.IsSubclassOf ("System.Runtime.InteropServices", "CriticalHandle", _context)) {
						// Subclasses of CriticalHandle are special cased by interop
						return false;
					} else if (parameterTypeDef.IsSubclassOf ("System.Runtime.InteropServices", "SafeHandle", _context)) {
						// Subclasses of SafeHandle are special cased by interop
						return false;
					} else if (!parameterTypeDef.IsSequentialLayout && !parameterTypeDef.IsExplicitLayout) {
						// Rest of classes that don't have layout marshal as COM
						return true;
					}
				}
			}

			return false;
		}

		void RequireDynamicallyAccessedMembers (in DiagnosticContext diagnosticContext, in MultiValue value, ValueWithDynamicallyAccessedMembers targetValue)
		{
			var requireDynamicallyAccessedMembersAction = new RequireDynamicallyAccessedMembersAction (_context, _reflectionMarker, diagnosticContext);
			requireDynamicallyAccessedMembersAction.Invoke (value, targetValue);
		}
	}
}
